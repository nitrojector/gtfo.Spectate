using System.Runtime.CompilerServices;
using Player;
using SNetwork;
using UnityEngine;
using Spectate.Config;
using Spectate.UI;
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
	private float _eyeY = 0f;
	private float _eyeYTarget = 0f;
	private Vector3 _eyeXZ = Vector3.zero;
	private Vector3 _eyeXZTarget = Vector3.zero;
	private Vector3 _eyePosComputed = Vector3.zero;
	public Vector3 CameraPos => Active ? _eyePosComputed : _self?.FPSCamera?.Position ?? Vector3.zero;

	public const float DefaultCameraYPositionLerpGain = 11.0f;
	public const float DefaultCameraXZPositionLerpGain = 15.0f;
	public const float DefaultCameraLerpGain = 6f;
	public const float DefaultOrbitCenterVerticalOffset = 0.325f;
	public const float DefaultPitchAngleDeg = -18.75f;
	public const float DefaultDistanceFromEye = 0.625f;
	public const float DefaultScrollSensitivity = 0.5f;
	public const float DefaultFreecamSensitivity = 1.0f;

	public const float OrbitCenterVerticalOffsetMin = -1.0f;
	public const float OrbitCenterVerticalOffsetMax = 5.0f;
	public const float DistanceMin = 0.1f;
	public const float DistanceMax = 5.0f;
	public const float PitchAngleDegMin = -89f;
	public const float PitchAngleDegMax = 89f;

	private AgentTarget? _self = null;
	private AgentTarget? _target = null;

	public AgentTarget? Self {
		get {
			if (_self == null) {
				_self = new AgentTarget(PlayerManager.GetLocalPlayerAgent());
			}

			return _self;
		}
	}

	public AgentTarget? Target => _target;

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

#if DEBUG
		Logger.Debug("SpectateCam: Load");
#endif
		_self = new AgentTarget(localAgent);
		return _self.FPSCamera != null;
	}

	public bool Unload() {
		Active = false;
		_self = null;
		_target = null;

#if DEBUG
		Logger.Debug("SpectateCam: Unload");
#endif
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTarget(PlayerAgent agent) {
		_target = new AgentTarget(agent);
	}

	public bool Attach() {
		if (Active) return true;
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
		SpectateUI.Instance?.UpdateForAttach();
		SetRelatedActive(true);
		UpdateCull();
		SetActive(true);
		Logger.Debug("Attach");
		return true;
	}

	public bool Detach() {
		if (!Active) return true;
		if (!SelfReady && !Load()) {
			Logger.Error("SpectateCam: Detach failed - self not ready and cannot be loaded");
			Logger.Info("SpectateCam: Detach falling back to Unload");
			Unload();
			return false;
		}

		GuiManager.CrosshairLayer?.ShowSpreadCircle(_self!.FPHolder?.WieldedItem?.HipFireCrosshairSize ?? 40.0f);
		SpectateUI.Instance?.UpdateForDetach();
		SetRelatedActive(false);
		RevertCull();
		SetActive(false);
		Logger.Debug("Detach");
		return true;
	}

	private void SetActive(bool active) {
		if (active && !_wasActive) {
			OnActive?.Invoke();
		}

		_wasActive = Active;
		Active = active;
		if (!Active) {
			_yaw = 0f;
			_pitch = ConfigMgr.CameraPitchAngleDeg;
			_freeLookReturnTimer = 0f;
		}
	}

	private void SetRelatedActive(bool spectateActive) {
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
		Util.SetTargetActiveIfDiff(_self.Inventory, !spectateActive);
		Util.SetTargetActiveIfDiff(_self.Inventory?.m_flashlight.gameObject, !spectateActive);
		Util.SetTargetActiveIfDiff(_self.FPHolder?.gameObject, !spectateActive);

		// NOTE: we choose to change the style of crosshair instead of disabling it, in Attach/Detach
		// GuiManager.CrosshairLayer?.m_circleCrosshair?.transform.parent.gameObject.SetActive(active);

		_self.FPHolder?.FPSArms?.SetVisible(!spectateActive && !_self.IsDowned);

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

			UpdateLerp(_freecam);
			UpdateCamera();
			UpdateCull();
		}
	}

	private void UpdateTransitions() {
		if (!SelfReady) {
			return;
		}

		// TODO: NOTE: This might not be necessary.. more so a sanity check. Let's say there are 0
		//   meaningful performance impacts
		if (Active && !_self!.IsDowned && !ConfigMgr.DevEnables(eDevOpts.AllowSpectatingAnytime)) {
			Detach();
			return;
		}

		// transition OnDown/OnUnDown moved to patches

		// Active only
		if (!Active)
			return;

		if (ConfigMgr.AutoTransitionToFollowView && _freeLookReturnTimer >= 0.0f) {
			_freeLookReturnTimer -= Time.deltaTime;
		}
	}

	private void ProcessInput() {
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

		if (_freecam && Input.GetKeyDown(KeyCode.T)) {
			ConfigMgr.AutoTransitionToFollowView = !ConfigMgr.AutoTransitionToFollowView;
			SpectateUI.Instance?.MarkUIDirty();
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
			TrySetNextTarget();
		}

		if (Input.GetKeyDown(KeyCode.Mouse1)) {
			TrySetPreviousTarget();
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

	private void UpdateCamera() {
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: UpdateCull failed - target or self not ready");
			return;
		}

		if (ConfigMgr.AutoTransitionToFollowView && _freecam) {
			if (_freeLookReturnTimer < 0.001f) {
				_freecamFollow = true;
			}

			if (_freecamFollow) {
				UpdateYawPitchWithFollowView(false);
			}
		}

		// OLD: perhaps just use UpdateYawPitchWithFollowView(false); for follow as well.
		//  This would smooth follow view which may be desirable.
		// NOTE: we are not doing this because the snappiness might be desired for follow.
		//   if not, users can just use auto-follow in freecam mode.

		SetEye(GetTargetOrbitCenter());

		Vector3 orbitCenter = _eyeXZ + Vector3.up * _eyeY;

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
		_eyePosComputed = orbitCenter - dir * ConfigMgr.CameraDistance;
		if (Physics.Raycast(orbitCenter, -dir, out var hit, ConfigMgr.CameraDistance, LayerManager.MASK_WORLD)) {
			_eyePosComputed = hit.m_Point + dir * 0.1f;
		}

		_self!.FPSCamera!.OverridePositionAndRotation(_eyePosComputed, Quaternion.LookRotation(dir));
		_self!.FPSCamera!.OverrideFieldOfView(CellSettingsManager.GetIntValue(eCellSettingID.Video_WorldFOV));
	}

	private void UpdateLerp(bool freecamEnabled) {
		_eyeXZ = Vector3.Lerp(_eyeXZ, _eyeXZTarget, Time.deltaTime * ConfigMgr.CameraXZLerpGain);
		_eyeY = Mathf.Lerp(_eyeY, _eyeYTarget, Time.deltaTime * ConfigMgr.CameraYLerpGain);

		if (!freecamEnabled) return;

		_yaw = Mathf.LerpAngle(_yaw, _yawTarget, Time.deltaTime * ConfigMgr.FreecamLerpGain);
		float yawDiff = _yawTarget - _yaw;
		_yaw = Mathf.Repeat(_yaw, 360f);
		_yawTarget = _yaw + yawDiff;

		_pitch = Mathf.LerpAngle(_pitch, _pitchTarget, Time.deltaTime * ConfigMgr.FreecamLerpGain);
	}

	private void UpdateCull() {
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
		var curCullNode = _self.Agent.m_movingCuller.CurrentNode;
		var targetNode = _target.CourseNode?.m_cullNode;
		if (targetNode != null) {
			if (curCullNode.Pointer != targetNode.Pointer) {
				_self.Agent.m_movingCuller.SetCurrentNode(targetNode);
			}
		} else {
			Logger.Warn("SpectateCam: UpdateCull - failed to sync cull nodes, target node is null");
		}
	}

	private void RevertCull() {
		if (!SelfReady) {
			Logger.Error("SpectateCam: RevertCull failed - self is not ready");
			return;
		}

		Vector3 targetCullPosition = _self!.Agent.Position;
		if (Physics.Raycast(targetCullPosition, Vector3.down, out var hit, 64f, LayerManager.MASK_WORLD))
			targetCullPosition = hit.m_Point;

		CameraManager.CullingPosition = targetCullPosition;
		CameraManager.CullingDirection = _self!.FPSCamera!.Forward;

		_self.Agent.m_movingCuller.UpdatePosition(_self.Agent.m_dimensionIndex, targetCullPosition);
		var curCullNode = _self.Agent.m_movingCuller.CurrentNode;
		var targetNode = _self.CourseNode?.m_cullNode;
		if (targetNode != null) {
#if DEBUG
			Logger.Debug($"RevertCull reverting to \"{targetNode.CourseNode.Name}\"");
#endif
			if (curCullNode?.Pointer != targetNode?.Pointer) {
				_self.Agent.m_movingCuller.SetCurrentNode(targetNode);
			}
		} else {
			Logger.Warn("SpectateCam: RevertCull - failed to sync cull nodes self or target node is null");
		}
	}

	private void OnApplicationQuit() {
		ConfigMgr.WriteConfigIfDirty();
	}

	bool TrySetAnyNonLocalTarget() {
		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		_lastTargetPlayerIdx = 0;
		for (int i = 0; i < players.Count; i++) {
			if (TrySetTargetByIdx(i)) {
				return true;
			}
		}

		return false;
	}

	private bool TrySetNextTarget() {
		int limit = SNet.Slots?.SlottedPlayers?.Count ?? -1;
		if (limit <= 0) return false;

		for (int offset = 1; offset <= limit; offset++) {
			int tryIdx = (_lastTargetPlayerIdx + offset) % limit;
			if (TrySetTargetByIdx(tryIdx)) {
				return true;
			}
		}

		return false;
	}

	private bool TrySetPreviousTarget() {
		int limit = SNet.Slots?.SlottedPlayers?.Count ?? -1;
		if (limit <= 0) return false;

		for (int offset = 1; offset <= limit; offset++) {
			int tryIdx = (_lastTargetPlayerIdx - offset + limit) % limit;
			if (TrySetTargetByIdx(tryIdx)) {
				return true;
			}
		}

		return false;
	}

	private bool TrySetTargetByIdx(int playerIdx) {
		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		if (playerIdx >= 0 && playerIdx < players.Count) {
			if (!players[playerIdx].IsLocal) {
				SetTarget(players[playerIdx].PlayerAgent.Cast<PlayerAgent>());
				if ((ConfigMgr.NoPosLerpOnSwitchTarget && playerIdx != _lastTargetPlayerIdx) ||
				    !_freecam) {
					SetEye(GetTargetOrbitCenter(), true);
				}

				_lastTargetPlayerIdx = playerIdx;
				return true;
			}
		}

		return false;
	}

	private Vector3 GetTargetOrbitCenter() {
		var eyeTmp = _target?.Agent.m_eyePosition ?? Vector3.zero;
		eyeTmp.y += ConfigMgr.CameraOrbitVerticalOffset;
		return eyeTmp;
	}

	private void OnFollow2Free() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFreecam failed - target not ready");
			return;
		}

		SpectateUI.Instance?.MarkUIDirty();
	}

	private void OnFree2Follow() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFollow failed - target not ready");
			return;
		}

		UpdateYawPitchWithFollowView(true);
		SpectateUI.Instance?.MarkUIDirty();
	}

	// WARNING: should not be called without check TargetReady
	private void UpdateYawPitchWithFollowView(bool instant) {
		SetYaw(Vector3.SignedAngle(Vector3.forward, _target!.Agent.Forward, Vector3.up), instant);
		SetPitch(ConfigMgr.CameraPitchAngleDeg, instant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void AdjustPitch(float deltaPitch, bool instant = false) {
		SetPitch(_pitchTarget + deltaPitch, instant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void AdjustYaw(float deltaYaw, bool instant = false) {
		SetYaw(_yawTarget + deltaYaw, instant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetPitch(float pitch, bool instant = false) {
		pitch = Mathf.Clamp(pitch, PitchAngleDegMin, PitchAngleDegMax);
		_pitchTarget = pitch;
		if (instant) _pitch = pitch;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetYaw(float yaw, bool instant = false) {
		_yawTarget = yaw;
		if (instant) _yaw = yaw;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetEyeXZ(Vector3 eyeXZ, bool instant = false) {
		eyeXZ.y = 0f;
		_eyeXZTarget = eyeXZ;
		if (instant) _eyeXZ = eyeXZ;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetEyeY(float eyeY, bool instant = false) {
		_eyeYTarget = eyeY;
		if (instant) _eyeY = eyeY;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetEye(Vector3 pos, bool instant = false) {
		SetEyeXZ(pos, instant);
		SetEyeY(pos.y, instant);
	}
}
