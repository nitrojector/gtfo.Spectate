using Player;
using UnityEngine;
using Spectate.Config;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Spectate;

public class SpectateCam : MonoBehaviour {
	public static SpectateCam? Instance { get; set; }

	public bool SelfReady => _self != null && _self.FPSCamera != null;
	public bool TargetReady => _target != null;
	public bool Active { get; private set; } = false;
	private bool _wasActive = false;

	public event Action? OnActive;

	private bool _freecam = ConfigMgr.DefaultFreecamView;
	private bool _freecamFollow = ConfigMgr.AutoTransitionToFollowView;
	private float _freeLookReturnTimer = 0f;

	private float _pitch = ConfigMgr.CameraPitchAngleDeg;
	private float _yaw = 0f;
	private float _pitchTarget = ConfigMgr.CameraPitchAngleDeg;
	private float _yawTarget = 0f;

	public const float DefaultCameraLerpGain = 6f;
	public const float DefaultOrbitCenterVerticalOffset = 0.325f;
	public const float DefaultPitchAngleDeg = -18.75f;
	public const float DefaultDistanceFromEye = 0.625f;
	public const float DefaultScrollSensitivity = 0.5f;
	public const float DefaultFreecamSensitivity = 1.0f;

	public const float OrbitCenterVerticalOffsetMin = -5.0f;
	public const float OrbitCenterVerticalOffsetMax = 5.0f;
	public const float DistanceMin = 0.1f;
	public const float DistanceMax = 5.0f;
	public const float PitchAngleDegMin = -89f;
	public const float PitchAngleDegMax = 89f;

	private SpectateTarget? _self = null;
	private SpectateTarget? _target = null;

	public SpectateTarget? Target => _target;

	public SpectateCam(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this);
	}

	public bool Load() {
		PlayerAgent localAgent = PlayerManager.GetLocalPlayerAgent();
		if (localAgent == null) {
			Logger.Error("SpectateCam: Failed to load - local player agent is null.");
			return false;
		}

		_self = new SpectateTarget(localAgent);
		return _self.FPSCamera != null;
	}

	public bool Unload() {
		_self = null;
		_target = null;
		return true;
	}

	public void SetTarget(PlayerAgent agent) {
		_target = new SpectateTarget(agent);
	}

	public bool Attach() {
		if (!SelfReady && !Load()) return false;
		if (!TargetReady && !TrySetAnyNonLocalTarget())
			return false; // TODO: TrySet is for testing only // combine with UI to switch

		SetRelatedActive(false);
		UpdateCull();
		SetActive(true);
		Logger.Debug("Attach");
		return true;
	}

	public bool Detach() {
		if (!SelfReady) return false;

		SetRelatedActive(true);
		RevertCull();
		SetActive(false);
		Logger.Debug("Detach");
		return true;
	}

	void SetActive(bool active) {
		if (active && !_wasActive) {
			OnActive?.Invoke();
		}

		_wasActive = Active;
		Active = active;
		if (!Active) {
			_yaw = 0f;
			_pitch = ConfigMgr.CameraPitchAngleDeg;
			_freeLookReturnTimer = 0f;
			_freecamFollow = false;
		}
	}

	void SetRelatedActive(bool active) {
		// TODO: let locomotion run for a bit? so that when player is mid jump while switching back, there is no lerping
		// TODO: transition to/from certain UIs reset the state of some elements (e.g. crosshair), we want them to stay disabled
		// Patch FocusStateManager.ChangeState ?
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: SetRelatedActive failed - target or self not ready");
			return;
		}

		_self!.SetRigActive(active);
		_self.Locomotion.enabled = active;
		_self.Inventory.enabled = active;
		_self.FPHolder?.gameObject.SetActive(active);
		GuiManager.CrosshairLayer?.m_circleCrosshair?.transform.parent.gameObject.SetActive(active);

		var fpsCamera = _self.FPSCamera;
		if (fpsCamera != null) {
			fpsCamera.MouseLookEnabled = active;
			fpsCamera.PlayerAgentRotationEnabled = active;
			fpsCamera.PlayerMoveEnabled = active;
		}
	}

	private void Update() {
		if (!enabled || !gameObject.activeInHierarchy) return;

		ProcessInput();
		UpdateTransitions();

		if (Active) {
			if (_target == null) {
				Detach();
				return;
			}

			if (_freecam) {
				UpdateYawPitch();
			}

			UpdateCamPos();
			UpdateCull();
		}
	}

	private void UpdateTransitions() {
		if (!SelfReady) {
			return;
		}

		// transition OnDown/OnUnDown moved to patches

		// Active only
		if (!Active)
			return;

		if (ConfigMgr.AutoTransitionToFollowView && _freeLookReturnTimer > 0.0f) {
			_freeLookReturnTimer -= Time.deltaTime;
		}
	}

	// TESTING purpose only
	bool TrySetAnyNonLocalTarget() {
		foreach (var agent in PlayerManager.PlayerAgentsInLevel) {
			if (!agent.IsLocallyOwned) {
				SetTarget(agent);
				return true;
			}
		}

		return false;
	}

	void ProcessInput() {
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS))
			return;

		// Universal inputs
		bool allowKeySwitch = ConfigMgr.DevEnables(eDevOpts.AllowSpectatingAnytime) ||
		                      (SelfReady && _self!.IsDowned); // BUG: can't switch while downed
		if (allowKeySwitch && Input.GetKeyDown(KeyCode.V)) {
			if (Active) {
				if (!Detach()) Logger.Error("Failed to detach SpecCam.");
			} else {
				if (!Attach()) Logger.Error("Failed to attach SpecCam.");
			}
		}

		// Active-only inputs
		if (!Active) return;

		if (Input.GetKeyDown(KeyCode.F)) {
			if (_freecam) {
				_freecam = false;
				OnFree2Follow();
			} else {
				_freecam = true;
				OnFollow2Free();
			}
		}

		Vector2 mouseDelta = InputHelper.GetMouseDelta();
		if (_freecam && mouseDelta != Vector2.zero) {
			AdjustYaw(mouseDelta.x * ConfigMgr.FreecamSensitivity);
			AdjustPitch(mouseDelta.y * ConfigMgr.FreecamSensitivity);

			if (ConfigMgr.AutoTransitionToFollowView) {
				_freecamFollow = false;
				_freeLookReturnTimer = ConfigMgr.AutoTransitionDelay;
			}
		}

		int idx = InputHelper.GetAlphaNumKeyDown();
		var agents = PlayerManager.PlayerAgentsInLevel;
		if (idx > 0 && idx - 1 < agents.Count) {
			if (!agents[idx - 1].IsLocallyOwned)
				SetTarget(agents[idx - 1]);
		}

		// Camera fixed view adjust
		float scrollDelta = Input.mouseScrollDelta.y * ConfigMgr.ScrollSensitivity;
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS_CommunicationDialog) &&
		    Mathf.Abs(scrollDelta) > 0f) {
			if (InputHelper.OnlyModifies(KeyCode.LeftShift, KeyCode.RightShift)) {
				// adjust pitch
				if (_freecam) SpectateUI.Instance?.WarnFreecamNoAdjustPitch();
				else ConfigMgr.CameraPitchAngleDeg += 0.5f * scrollDelta;
			} else if (InputHelper.OnlyModifies(KeyCode.LeftControl, KeyCode.RightControl)) {
				// adjust center vertical offset
				ConfigMgr.CameraOrbitVerticalOffset = Mathf.Clamp(
					ConfigMgr.CameraOrbitVerticalOffset + 0.05f * scrollDelta,
					OrbitCenterVerticalOffsetMin,
					OrbitCenterVerticalOffsetMax);
			} else {
				// adjust distance
				ConfigMgr.CameraDistance = Mathf.Clamp(
					ConfigMgr.CameraDistance - 0.05f * scrollDelta,
					DistanceMin,
					DistanceMax);
			}
		}
	}

	void UpdateCamPos() {
		// TODO: Use spherecast for better clipping avoidance
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: UpdateCull failed - target or self not ready");
			return;
		}

		if (ConfigMgr.AutoTransitionToFollowView && _freecam) {
			if (_freecamFollow) {
				UpdateYawPitchWithFollowView(false);
			} else if (_freeLookReturnTimer < 0.0f) {
				_freecamFollow = true;
				UpdateYawPitchWithFollowView(false);
			}
		}

		Vector3 forward = _target!.Agent.Forward.normalized;
		if (_freecam) {
			Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
			forward = yawRot * Vector3.forward;
		}

		// calculated desired view direction
		float pitchRad = Mathf.Deg2Rad * (_freecam ? _pitch : ConfigMgr.CameraPitchAngleDeg);
		Vector3 dir = forward + Vector3.up * Mathf.Tan(pitchRad);
		dir.Normalize();

		// raycast to avoid clipping into walls
		Vector3 orbitCenter = _target!.Agent.m_eyePosition + ConfigMgr.CameraOrbitVerticalOffset * Vector3.up;
		Vector3 eyePos = orbitCenter - dir * ConfigMgr.CameraDistance;
		if (Physics.Raycast(orbitCenter, -dir, out var hit, ConfigMgr.CameraDistance, LayerManager.MASK_WORLD)) {
			eyePos = hit.m_Point + dir * 0.1f;
		}

		// TODO: perhaps lerp this to avoid jitter on high ping?
		_self!.FPSCamera!.OverridePositionAndRotation(eyePos, Quaternion.LookRotation(dir));
	}

	void UpdateYawPitch() {
		if (!Util.GoodEnoughDeg(_yaw, _yawTarget)) {
			_yaw = Mathf.LerpAngle(_yaw, _yawTarget, Time.deltaTime * ConfigMgr.FreecamLerpGain);
			float yawDiff = _yawTarget - _yaw;
			_yaw = Mathf.Repeat(_yaw, 360f);
			_yawTarget = _yaw + yawDiff;
		} else {
			_yaw = _yawTarget;
		}

		if (!Util.GoodEnoughDeg(_pitch, _pitchTarget)) {
			_pitch = Mathf.LerpAngle(_pitch, _pitchTarget, Time.deltaTime * ConfigMgr.FreecamLerpGain);
		} else {
			_pitch = _pitchTarget;
		}
	}

	void UpdateCull() {
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: UpdateCull failed - target or self not ready");
			return;
		}

		Vector3 targetCullPosition = _target!.Agent.Position;
		if (Physics.Raycast(targetCullPosition, Vector3.down, out var hit, 64f, LayerManager.MASK_WORLD))
			targetCullPosition = hit.m_Point;

		CameraManager.CullingPosition = targetCullPosition;
		CameraManager.CullingDirection = _self!.FPSCamera!.Forward;

		_self.Agent.m_movingCuller.UpdatePosition(_self.Agent.m_dimensionIndex, targetCullPosition);
		if (_self.Agent.m_movingCuller.CurrentNode != _target.Agent.CourseNode.m_cullNode)
			_self.Agent.m_movingCuller.SetCurrentNode(_target.Agent.CourseNode.m_cullNode);
	}

	void OnFollow2Free() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFreecam failed - target not ready");
			return;
		}

		UpdateYawPitchWithFollowView(true);
	}

	void OnFree2Follow() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFollow failed - target not ready");
			return;
		}

		UpdateYawPitchWithFollowView(true);
	}

	// WARNING: should not be called without check TargetReady
	void UpdateYawPitchWithFollowView(bool instant) {
		SetYaw(Vector3.SignedAngle(Vector3.forward, _target!.Agent.Forward, Vector3.up), instant);
		SetPitch(ConfigMgr.CameraPitchAngleDeg, instant);
	}

	void AdjustPitch(float deltaPitch, bool instant = false) {
		SetPitch(_pitchTarget + deltaPitch, instant);
	}

	void AdjustYaw(float deltaYaw, bool instant = false) {
		SetYaw(_yawTarget + deltaYaw, instant);
	}

	void SetPitch(float pitch, bool instant = false) {
		pitch = Mathf.Clamp(pitch, PitchAngleDegMin, PitchAngleDegMax);
		_pitchTarget = pitch;
		if (instant) _pitch = pitch;
	}

	void SetYaw(float yaw, bool instant = false) {
		_yawTarget = yaw;
		if (instant) _yaw = yaw;
	}

	void RevertCull() {
		if (!SelfReady) {
			Logger.Error("SpectateCam: RevertCull failed - self is not ready");
			return;
		}

		Vector3 targetCullPosition = _self!.Agent.Position;
		if (Physics.Raycast(targetCullPosition, Vector3.down, out var hit, 64f, LayerManager.MASK_WORLD))
			targetCullPosition = hit.m_Point;

		_self.Agent.m_movingCuller.UpdatePosition(_self.Agent.m_dimensionIndex, targetCullPosition);
		if (_self.Agent.m_movingCuller.CurrentNode != _self.Agent.CourseNode.m_cullNode)
			_self.Agent.m_movingCuller.SetCurrentNode(_self.Agent.CourseNode.m_cullNode);
	}

	private void OnApplicationQuit() {
		ConfigMgr.WriteConfigIfDirty();
	}
}
