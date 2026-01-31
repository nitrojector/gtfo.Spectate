using System.Runtime.CompilerServices;
using Player;
using SNetwork;
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

	private int _lastTargetPlayerIdx = -1;

	private bool _freecam = ConfigMgr.DefaultFreecamView;
	private bool _freecamFollow = ConfigMgr.AutoTransitionToFollowView;
	private float _freeLookReturnTimer = 0f;
	public bool Freecam => _freecam;

	public Vector3 LastCamDir = Vector3.forward;

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
			Logger.Error("SpectateCam: Failed to load - local player agent is null");
			return false;
		}

		_self = new SpectateTarget(localAgent);
		return _self.FPSCamera != null;
	}

	public bool Unload() {
		Active = false;
		_self = null;
		_target = null;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTarget(PlayerAgent agent) {
		_target = new SpectateTarget(agent);
	}

	public bool Attach() {
		if (!SelfReady && !Load()) {
			Logger.Error("SpectateCam: Attach failed - self not ready and cannot be loaded");
			return false;
		}

		if (!TargetReady && !TrySetAnyNonLocalTarget()) {
			Logger.Warn("SpectateCam: Attach failed - no valid target available");
			return false;
		}

		LastCamDir = _self!.FPSCamera!.Forward;

		GuiManager.CrosshairLayer.ShowPrecisionDot();
		SetRelatedActive(true);
		UpdateCull();
		SetActive(true);
		Logger.Debug("Attach");
		return true;
	}

	public bool Detach() {
		if (!SelfReady && !Load()) {
			Logger.Error("SpectateCam: Detach failed - self not ready and cannot be loaded");
			Logger.Info("SpectateCam: Detach falling back to Unload");
			Unload();
			return false;
		}

		GuiManager.CrosshairLayer.ShowSpreadCircle(_self!.FPHolder?.WieldedItem.HipFireCrosshairSize ?? 40.0f);
		SpectateUI.Instance?.UpdatePlayerStatus(_self!);
		SetRelatedActive(false);
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

	void SetRelatedActive(bool spectateActive) {
		// TODO: transition to/from certain UIs reset the state of some elements (e.g. crosshair), we want them to stay disabled
		// Patch FocusStateManager.ChangeState ?
		if (!SelfReady) {
			Logger.Error("SpectateCam: SetRelatedActive failed - self not ready");
			return;
		}

		_self!.SetRigActive(!spectateActive);
		// NOTE: we don't want to disable Locomotion, we are
		// _self.Locomotion.enabled = active;
		_self.Agent.DeadDebugMode = spectateActive;
		_self.Inventory.enabled = !spectateActive;
		_self.FPHolder?.gameObject.SetActive(!spectateActive);
		// NOTE: we choose to change the style of crosshair instead of disabling it
		// GuiManager.CrosshairLayer?.m_circleCrosshair?.transform.parent.gameObject.SetActive(active);

		// TODO: When we disable spectate, hand is invisible.

		var fpsCamera = _self.FPSCamera;
		if (fpsCamera != null) {
			fpsCamera.MouseLookEnabled = !spectateActive;
			fpsCamera.PlayerAgentRotationEnabled = !spectateActive;
			fpsCamera.PlayerMoveEnabled = !spectateActive;
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

	void ProcessInput() {
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS))
			return;

		// Universal inputs
		bool allowKeySwitch = ConfigMgr.DevEnables(eDevOpts.AllowSpectatingAnytime) || (SelfReady && _self!.IsDowned);
		if (allowKeySwitch && Input.GetKeyDown(KeyCode.V)) {
			if (Active) {
				if (!Detach()) Logger.Warn("SpectateCam: Failed to detach SpecCam");
			} else {
				if (!Attach()) Logger.Warn("SpectateCam: Failed to attach SpecCam");
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

		// control yaw pitch with arrow keys
		if (_freecam) {
			// adjust free can with arrow keys
			if (Input.GetKey(KeyCode.UpArrow)) {
				AdjustPitch(ConfigMgr.FreecamSensitivity);
			}

			if (Input.GetKey(KeyCode.DownArrow)) {
				AdjustPitch(-ConfigMgr.FreecamSensitivity);
			}

			if (Input.GetKey(KeyCode.LeftArrow)) {
				AdjustYaw(-ConfigMgr.FreecamSensitivity);
			}

			if (Input.GetKey(KeyCode.RightArrow)) {
				AdjustYaw(ConfigMgr.FreecamSensitivity);
			}
		}

		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			NextTarget();
		}

		if (Input.GetKeyDown(KeyCode.Mouse1)) {
			PreviousTarget();
		}

		int idx = InputHelper.GetAlphaNumKeyDown();
		if (idx > 0) TrySetTargetByIdx(idx - 1);

		// Camera fixed view adjust
		float scrollDelta = Input.mouseScrollDelta.y * ConfigMgr.ScrollSensitivity;
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS_CommunicationDialog) &&
		    Mathf.Abs(scrollDelta) > 0f) {
			if (InputHelper.OnlyModifies(KeyCode.LeftShift, KeyCode.RightShift)) {
				// adjust pitch
				if (_freecam) SpectateUI.Instance?.WarnFreecamNoAdjustPitch();
				else ConfigMgr.CameraPitchAngleDeg -= 0.5f * scrollDelta;
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
		// TODO: Perhaps spherecast for better clipping avoidance
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

	bool TrySetAnyNonLocalTarget() {
		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		_lastTargetPlayerIdx = 0;
		for (int i = 0; i < players.Count; i++) {
			if (!players[i].IsLocal) {
				SetTarget(players[i].PlayerAgent.Cast<PlayerAgent>());
				_lastTargetPlayerIdx = i;
				return true;
			}
		}

		return false;
	}

	void NextTarget() {
		int limit = SNet.Slots?.SlottedPlayers?.Count ?? -1;
		if (limit <= 0) return;

		for (int offset = 1; offset <= limit; offset++) {
			int tryIdx = (_lastTargetPlayerIdx + offset) % limit;
			if (TrySetTargetByIdx(tryIdx)) {
				_lastTargetPlayerIdx = tryIdx;
				return;
			}
		}
	}

	void PreviousTarget() {
		int limit = SNet.Slots?.SlottedPlayers?.Count ?? -1;
		if (limit <= 0) return;

		for (int offset = 1; offset <= limit; offset++) {
			int tryIdx = (_lastTargetPlayerIdx - offset + limit) % limit;
			if (TrySetTargetByIdx(tryIdx)) {
				_lastTargetPlayerIdx = tryIdx;
				return;
			}
		}
	}

	bool TrySetTargetByIdx(int playerIdx) {
		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		if (playerIdx > 0 && playerIdx < players.Count) {
			if (!players[playerIdx].IsLocal) {
				SetTarget(players[playerIdx].PlayerAgent.Cast<PlayerAgent>());
				_lastTargetPlayerIdx = playerIdx;
				return true;
			}
		}

		return false;
	}

	void OnFollow2Free() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFreecam failed - target not ready");
			return;
		}

		SpectateUI.Instance?.UpdateMenu(_freecam);
		UpdateYawPitchWithFollowView(true);
	}

	void OnFree2Follow() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFollow failed - target not ready");
			return;
		}

		SpectateUI.Instance?.UpdateMenu(_freecam);
		UpdateYawPitchWithFollowView(true);
	}

	// WARNING: should not be called without check TargetReady
	void UpdateYawPitchWithFollowView(bool instant) {
		SetYaw(Vector3.SignedAngle(Vector3.forward, _target!.Agent.Forward, Vector3.up), instant);
		SetPitch(ConfigMgr.CameraPitchAngleDeg, instant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void AdjustPitch(float deltaPitch, bool instant = false) {
		SetPitch(_pitchTarget + deltaPitch, instant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void AdjustYaw(float deltaYaw, bool instant = false) {
		SetYaw(_yawTarget + deltaYaw, instant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void SetPitch(float pitch, bool instant = false) {
		pitch = Mathf.Clamp(pitch, PitchAngleDegMin, PitchAngleDegMax);
		_pitchTarget = pitch;
		if (instant) _pitch = pitch;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
