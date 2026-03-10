using System.Runtime.CompilerServices;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using CullingSystem;
using Enemies;
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
	/// <summary>
	/// Singleton instance of SpectateCam.
	/// </summary>
	public static SpectateCam? Instance { get; set; }

	/// <summary>
	/// Whether the local player self is ready for manipulation
	/// </summary>
	public bool SelfReady => _self != null && _self.FPSCamera != null;

	/// <summary>
	/// Whether the target player is ready for spectating (i.e. not null, has necessary components)
	/// </summary>
	public bool TargetReady => _target != null;

	/// <summary>
	/// Whether the spectate camera is currently active
	/// </summary>
	public bool Active { get; private set; } = false;

	/// <summary>
	/// Whether the spectate camera was active, used for triggering <see cref="OnActive"/> events
	/// </summary>
	private bool _wasActive = false;

	/// <summary>
	/// Called when the spectate camera transitions from inactive to active i.e. when the camera is first attached
	/// </summary>
	public event Action? OnActive;

	/// <summary>
	/// target player index that has been switched to, used for target switching based on next/prev
	/// </summary>
	private int _lastTargetPlayerIdx = -1;

	/// <summary>
	/// Whether spectate camera is in freecam mode
	/// </summary>
	private bool _freecam = ConfigMgr.DefaultFreecamView;

	/// <summary>
	/// Whether freecam mode auto follow is enabled
	/// </summary>
	private bool _freecamFollow = ConfigMgr.AutoTransitionToFollowView;

	/// <summary>
	/// Time until freecam automatically transitions to temporary follow view
	/// Used only if <see cref="_freecamFollow"/> is true
	/// </summary>
	private float _freeLookReturnTimer = 0f;

	private PouncerBehaviour? _lastPouncer = null;

	/// <summary>
	/// Public accessor for whether freecam mode is enabled, used for UI and input processing
	/// </summary>
	public bool Freecam => _freecam;

	/// <summary>
	/// Last recorded course node of the local player
	/// </summary>
	public C_Node? LastLocalCullNode { get; private set; } = null;

	/// <summary>
	/// The real camera position of the local player
	/// </summary>
	public Vector3 DiegeticCamDir { get; private set; } = Vector3.forward;

	/// <summary>
	/// The real local scale of the player rig, used for scaling back when detaching.
	/// Funny enough, this is supposedly the non-diegetic scale, because players should all be scaled the same.
	/// </summary>
	public Vector3 DiegeticPlayerRigScale { get; private set; } = Vector3.one;

	/// <summary>
	/// The delay after going down before trying to attach the spectate camera. This solves player rig positioning issues.
	/// </summary>
	public const float DownToSpectateDelay = 1.0f;

	/// <summary>
	/// The world-pitch of the spectate camera for render
	/// </summary>
	private float _pitch = ConfigMgr.CameraPitchAngleDeg;

	/// <summary>
	/// The world-yaw of the spectate camera for render
	/// </summary>
	private float _yaw = 0f;

	/// <summary>
	/// The target world-pitch that <see cref="_pitch"/> lerps towards
	/// </summary>
	private float _pitchTarget = ConfigMgr.CameraPitchAngleDeg;

	/// <summary>
	/// The target world-yaw that <see cref="_yaw"/> lerps towards
	/// </summary>
	private float _yawTarget = 0f;

	/// <summary>
	/// The Y position of the player eye for render
	/// </summary>
	private float _eyeY = 0f;

	/// <summary>
	/// The target Y position of the player eye that <see cref="_eyeY"/> lerps towards
	/// </summary>
	private float _eyeYTarget = 0f;

	/// <summary>
	/// The XZ (i.e. horizontal) position of the player eye for render
	/// </summary>
	private Vector3 _eyeXZ = Vector3.zero;

	/// <summary>
	/// The target XZ (i.e. horizontal) position of the player eye that <see cref="_eyeXZ"/> lerps towards
	/// </summary>
	private Vector3 _eyeXZTarget = Vector3.zero;

	/// <summary>
	/// The final computer camera position of the spectate camera for render, after applying orbit and raycast clipping.
	/// </summary>
	private Vector3 _camPosComputed = Vector3.zero;

	/// <summary>
	/// Public safe accessor for the camera position, which returns <see cref="_camPosComputed"/> if
	/// <see cref="Active"/>, and falls back to the local player's FPS camera position if not active.
	/// </summary>
	public Vector3 CameraPos => Active ? _camPosComputed : _self?.FPSCamera?.Position ?? Vector3.zero;

	/// <summary>
	/// The default lerp gain for the Y position of the camera.
	/// </summary>
	public const float DefaultCameraYPositionLerpGain = 11.0f;

	/// <summary>
	/// The default lerp gain for the XZ position of the camera.
	/// </summary>
	public const float DefaultCameraXZPositionLerpGain = 15.0f;

	/// <summary>
	/// The default lerp gain for the freecam yaw and pitch.
	/// </summary>
	public const float DefaultCameraLerpGain = 6f;

	/// <summary>
	/// The default vertical offset of the camera orbit center from the player's eye position.
	/// </summary>
	public const float DefaultOrbitCenterVerticalOffset = 0.325f;

	/// <summary>
	/// The default pitch angle of the camera view direction in degrees. Negative
	/// means looking down.
	/// </summary>
	public const float DefaultPitchAngleDeg = -18.75f;

	/// <summary>
	/// The default distance of the spectate camera from the orbit center.
	/// </summary>
	public const float DefaultDistanceFromEye = 0.625f;

	/// <summary>
	/// The default scroll sensitivity for adjusting camera parameters with mouse scroll
	/// </summary>
	public const float DefaultScrollSensitivity = 0.5f;

	/// <summary>
	/// The default sensitivity for freecam mouse look.
	/// </summary>
	public const float DefaultFreecamSensitivity = 1.0f;

	/// <summary>
	/// Minimum allowed orbit center vertical offset to prevent camera from going underground
	/// </summary>
	public const float OrbitCenterVerticalOffsetMin = -1.0f;

	/// <summary>
	/// Maximum allowed orbit center vertical offset
	/// </summary>
	public const float OrbitCenterVerticalOffsetMax = 5.0f;

	/// <summary>
	/// Minimum allowed distance of the spectate camera from the orbit center
	/// </summary>
	public const float DistanceMin = 0.1f;

	/// <summary>
	/// Maximum allowed distance of the spectate camera from the orbit center
	/// </summary>
	public const float DistanceMax = 5.0f;

	/// <summary>
	/// The additional distance offset for camera when spectating a pouncer (which captured a teammate)
	/// </summary>
	public const float DistanceOffsetPouncer = 1.3f;

	/// <summary>
	/// Minimum allowed pitch angle of the camera view direction in degrees.
	/// </summary>
	public const float PitchAngleDegMin = -89f;

	/// <summary>
	/// Maximum allowed pitch angle of the camera view direction in degrees.
	/// </summary>
	public const float PitchAngleDegMax = 89f;

	/// <summary>
	/// The current local player instance.
	/// </summary>
	private AgentTarget? _self = null;

	/// <summary>
	/// The current spectating player. null implies no target.
	/// </summary>
	private AgentTarget? _target = null;

	/// <summary>
	/// A safe accessor for Self that tries it's best to return a valid <see cref="AgentTarget"/> for the local player,
	/// this should never fail in-game
	/// </summary>
	public AgentTarget? Self {
		get {
			if (_self == null) {
				_self = new AgentTarget(PlayerManager.GetLocalPlayerAgent());
			}

			return _self;
		}
	}

	/// <summary>
	/// Public accessor for the current spectating target, can be null if no valid target is set
	/// </summary>
	public AgentTarget? Target => _target;

	/// <summary>
	/// Returns whether the player can spectate.
	/// NOTE: This is NOT behavior safe!!! If dev options is enabled, pouncer issues can occur!!
	///   For safe behavior, use <see cref="AgentTarget.CanSpectate"/>!!
	/// </summary>
	public bool CanSpectate => ConfigMgr.DevEnables(eDevOpts.AllowSpectatingAnytime) ||
	                           (SelfReady && _self!.CanSpectate);

	/// <summary>
	/// IL2CPP compatibility constructor
	/// </summary>
	public SpectateCam(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this);

		Events.OnSessionStart += () => Load();
		Events.OnSessionEnd += () => Unload();
		Events.OnAnyPlayerDeath += () => {
			if (ConfigMgr.PreferSpectateAlive && (_target == null || _target.IsDowned)) {
				TrySetAnyNonLocalTarget();
			}
		};
	}

	/// <summary>
	/// Loads the local player agent
	/// </summary>
	/// <returns>true if local self creation was successful</returns>
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

	/// <summary>
	/// Unloads local and target agent. Sets <see cref="Active"/> to false.
	/// </summary>
	/// <returns>true always (currently)</returns>
	public bool Unload() {
		SetActive(false);
		_self = null;
		_target = null;

#if DEBUG
		Logger.Debug("SpectateCam: Unload");
#endif
		return true;
	}

	/// <summary>
	/// Sets the spectate target to a given agent.
	/// </summary>
	/// <param name="agent">target to spectate</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTarget(PlayerAgent agent) {
		_target = new AgentTarget(agent);
	}

	/// <summary>
	/// Attempts to attach the spectate camera after a delay.
	/// </summary>
	/// <param name="delay">time before attach is attempted</param>
	public void TryAttachDelayed(float delay) {
		StartCoroutine(TryAttachDelayedCoroutine(delay).WrapToIl2Cpp());
	}

	/// <summary>
	/// Coroutine for delayed attach, see <see cref="TryAttachDelayed(float)"/>
	/// </summary>
	/// <param name="delay">delay for the coroutine before calling <see cref="Attach"/></param>
	/// <returns></returns>
	private IEnumerator TryAttachDelayedCoroutine(float delay) {
		yield return new WaitForSeconds(delay);
		if (!Attach()) {
			Logger.Warn("SpectateCam: TryAttachDelayed failed to attach after delay");
		}
	}

	/// <summary>
	/// Attaches the spectate camera to current target, sets up necessary states and UI.
	/// </summary>
	/// <returns>true if camera is successfully attached</returns>
	public bool Attach() {
		if (Active) return true;
		if (!SelfReady && !Load()) {
			Logger.Error("SpectateCam: Attach failed - self not ready and cannot be loaded");
			return false;
		}

		// NOTE: we do a redundant check here just for safety.
		if (!CanSpectate)
			return false;

		if (!TargetReady && !TrySetAnyNonLocalTarget()) {
			Logger.Warn("SpectateCam: Attach failed - no valid target available");
			return false;
		}

		DiegeticCamDir = _self!.FPSCamera!.Forward;
		DiegeticPlayerRigScale = _self!.PlayerModel.gameObject.transform.localScale;
		LastLocalCullNode = _self?.CourseNode?.m_cullNode;

		_self!.PlayerModel.gameObject.transform.localScale = Vector3.one;
		GuiManager.PlayerLayer.ApplyMovementSway(Vector3.zero);
		GuiManager.CrosshairLayer.ShowPrecisionDot();
		SpectateUI.Instance?.UpdateForAttach();
		SetRelatedActive(true);
		UpdateCull();
		SetActive(true);
#if DEBUG
		Logger.Debug("Attach");
#endif
		return true;
	}

	/// <summary>
	/// Detaches the spectate camera, reverts necessary states and UI.
	/// </summary>
	/// <returns>true when detached normally, false if forcefully (i.e. <see cref="Unload"/> is called).</returns>
	public bool Detach() {
		if (!Active) return true;
		if (!SelfReady && !Load()) {
			Logger.Error("SpectateCam: Detach failed - self not ready and cannot be loaded");
			Logger.Info("SpectateCam: Detach falling back to Unload");
			Unload();
			return false;
		}

		SetActive(false);
		SetRelatedActive(false);
		RevertCull();
		SpectateUI.Instance?.UpdateForDetach();
		GuiManager.CrosshairLayer?.ShowSpreadCircle(_self!.FPHolder?.WieldedItem?.HipFireCrosshairSize ?? 40.0f);
		GuiManager.PlayerLayer?.m_playerStatus?.ResetDamageAnimation();
		_self!.PlayerModel.gameObject.transform.localScale = DiegeticPlayerRigScale;
#if DEBUG
		Logger.Debug("Detach");
#endif
		return true;
	}

	/// <summary>
	/// Sets the active state of the spectate camera, and triggers <see cref="OnActive"/> if transitioning from inactive to active.
	/// </summary>
	/// <param name="active">the active state to transition to</param>
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

	/// <summary>
	/// Sets related game component states for our spectate state.
	/// </summary>
	/// <param name="spectateActive">whether spectate is active</param>
	internal void SetRelatedActive(bool spectateActive) {
		if (!SelfReady) {
			Logger.Error("SpectateCam: SetRelatedActive failed - self not ready");
			return;
		}

		if (ConfigMgr.ShowPlayerBodyWhenSpectating) {
			_self!.SetHostHiddenRigActive(spectateActive);
			if (!Self!.IsDowned && !spectateActive) {
				// NOTE: for the case of dev options allowing spectating anytime,
				//   we want to show legs when not spectating even if not downed.
				_self.SetRigTorsoLegsActive(true);
			} else {
				_self.SetRigTorsoLegsActive(spectateActive);
			}
		} else {
			_self!.SetRigActive(!spectateActive);
		}

		// NOTE: we don't want to disable Locomotion, we are
		// _self.Locomotion.enabled = active;
		_self!.Agent.DeadDebugMode = spectateActive;
		_self!.PlayerModel.GhostEnabled = spectateActive;
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
			// NOTE: Turned off to let player model update correctly
			// fpsCamera.PlayerMoveEnabled = !spectateActive;
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

	/// <summary>
	/// Updates camera transitions such as timer updates.
	/// Not a well-defined function, just a place for code that runs every frame regardless of <see cref="Active"/>.
	/// </summary>
	private void UpdateTransitions() {
		if (!SelfReady) {
			return;
		}

		// NOTE: This might not be necessary.. more so a sanity check. Let's say there are 0
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

	/// <summary>
	/// Processes player inputs for toggling spectate states and adjusting camera parameters.
	/// </summary>
	private void ProcessInput() {
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS))
			return;

		// Universal inputs
		if (CanSpectate && Input.GetKeyDown(ConfigMgr.GetKeybind(SpectateInputAction.ToggleSpectate))) {
			if (Active) {
				if (!Detach()) Logger.Warn("SpectateCam: Failed to detach SpecCam");
			} else {
				if (!Attach()) Logger.Warn("SpectateCam: Failed to attach SpecCam");
			}
		}

		// Active-only inputs
		if (!Active) return;

		if (Input.GetKeyDown(ConfigMgr.GetKeybind(SpectateInputAction.ToggleFreecam))) {
			if (_freecam) {
				_freecam = false;
				OnFree2Follow();
			} else {
				_freecam = true;
				OnFollow2Free();
			}
		}

		if (_freecam && Input.GetKeyDown(ConfigMgr.GetKeybind(SpectateInputAction.ToggleAutoFollow))) {
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

		if (FocusStateManager.Current.m_currentState != eFocusState.FPS_CommunicationDialog) {
			int idx = InputHelper.GetAlphaNumKeyDown();
			if (idx > 0) TrySetTargetByIdx(idx - 1, overridePreferAlive: true);
		}

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

	/// <summary>
	/// Updates the camera position and rotation based on the currently configured parameters. Updates <see cref="_camPosComputed"/>
	/// </summary>
	private void UpdateCamera() {
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: UpdateCamera failed - target or self not ready");
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
		float dist = ConfigMgr.CameraDistance;
		if (_target.IsCaptured)
			dist = Mathf.Clamp(dist + DistanceOffsetPouncer, DistanceMin, DistanceMax);
		_camPosComputed = orbitCenter - dir * dist;
		if (Physics.Raycast(orbitCenter, -dir, out var hit, dist, LayerManager.MASK_WORLD)) {
			_camPosComputed = hit.m_Point + dir * 0.1f;
		}

		_self!.FPSCamera!.OverridePositionAndRotation(_camPosComputed, Quaternion.LookRotation(dir));
		_self!.FPSCamera!.OverrideFieldOfView(CellSettingsManager.GetIntValue(eCellSettingID.Video_WorldFOV));
	}

	/// <summary>
	/// Lerp transitions for eye position and freecam yaw/pitch. Called every frame in Update when Active.
	/// </summary>
	/// <param name="freecamEnabled">if udpating for freecam mode</param>
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

	/// <summary>
	/// Updates in-game culling system to cull based on our camera
	/// </summary>
	private void UpdateCull() {
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: UpdateCull failed - target or self not ready");
			return;
		}

		Vector3 targetCullPosition = GetTargetOrbitCenter();
		if (Physics.Raycast(targetCullPosition, Vector3.down, out var hit, 64f, LayerManager.MASK_WORLD))
			targetCullPosition = hit.m_Point;

		CameraManager.CullingPosition = targetCullPosition;
		CameraManager.CullingDirection = _self!.FPSCamera!.Forward;

		_self.Agent.m_movingCuller.UpdatePosition(_self.Agent.m_dimensionIndex, targetCullPosition);
		var curCullNode = _self.Agent.m_movingCuller.CurrentNode;
		var targetNode = _target!.CourseNode?.m_cullNode;
		if (targetNode != null) {
			if (curCullNode.Pointer != targetNode.Pointer) {
				_self.Agent.m_movingCuller.SetCurrentNode(targetNode);
			}
		} else {
			Logger.Warn("SpectateCam: UpdateCull - failed to sync cull nodes, target node is null");
		}
	}

	/// <summary>
	/// Reverts culling changes made in <see cref="UpdateCull"/>. Called in Detach to revert cull back to self.
	/// </summary>
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
		// TODO: NOTE: This assumes that the local player doesn't move to a different node while spectating
		if (LastLocalCullNode != null) {
			if (curCullNode?.Pointer != LastLocalCullNode?.Pointer) {
				_self.Agent.m_movingCuller.SetCurrentNode(LastLocalCullNode);
			}
		} else {
			Logger.Warn("SpectateCam: RevertCull - failed to sync cull nodes self or target node is null");
		}
	}

	/// <summary>
	/// Clears the current spectate target.
	/// Used for spectate closest on first spectate after down logic
	/// </summary>
	public void ClearTarget() {
		_target = null;
		_lastTargetPlayerIdx = -1;
	}

	/// <summary>
	/// Tries to set the spectate target to any valid non-local player.
	/// </summary>
	/// <returns>true if a valid target was set</returns>
	bool TrySetAnyNonLocalTarget() {
		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		float closestDist = float.MaxValue;

#if DEBUG
		Logger.Debug("TrySetNonLocalTarget");
#endif

		_lastTargetPlayerIdx = -1;
		for (int i = 0; i < players.Count; i++) {
			if (SimulateSetTargetIdx(i, out var agent)) {
				float dist = Vector3.Distance(agent!.Position, _self!.Agent.Position);
				if (dist < closestDist) {
#if DEBUG
					Logger.Debug("TrySetNonLocalTarget - found closer target: " + agent.name + " at distance " + dist);
#endif
					closestDist = dist;
					_lastTargetPlayerIdx = i;
				}
			}
		}

		return _lastTargetPlayerIdx != -1 && TrySetTargetByIdx(_lastTargetPlayerIdx);
	}

	/// <summary>
	/// Tries to set the spectate target to the next valid non-local player, starting from <see cref="_lastTargetPlayerIdx"/>.
	/// </summary>
	/// <returns>true if a target is set (can be same as current), false otherwise</returns>
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

	/// <summary>
	/// Tries to set the spectate target to the previous valid non-local player, starting from <see cref="_lastTargetPlayerIdx"/>.
	/// </summary>
	/// <returns>true if a target is set (can be same as current), false otherwise</returns>
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

	/// <summary>
	/// Checks if a player in a given slot is spectatable
	/// </summary>
	/// <param name="playerIdx">player slot index</param>
	/// <param name="agent">player agent of simulated set if successful</param>
	/// <param name="overridePreferAlive">whether method should succeed even if player is downed</param>
	/// <returns>true if slotted player is spectatable</returns>
	private bool SimulateSetTargetIdx(int playerIdx,
		[NotNullWhen(true)] out PlayerAgent? agent,
		bool overridePreferAlive = false) {
		agent = null;

		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		if (playerIdx >= 0 && playerIdx < players.Count) {
			if (players[playerIdx].IsLocal) return false;

			agent = players[playerIdx].PlayerAgent.Cast<PlayerAgent>();

			if (!overridePreferAlive && ConfigMgr.PreferSpectateAlive &&
			    agent.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
				return false;

			return true;
		}

		return false;
	}

	/// <summary>
	/// Tries to set the spectate target to the player in a given slot
	/// </summary>
	/// <param name="playerIdx">player slot index</param>
	/// <param name="overridePreferAlive">whether method should succeed even if player is downed</param>
	/// <returns>true if successfully set</returns>
	private bool TrySetTargetByIdx(int playerIdx, bool overridePreferAlive = false) {
		var players = SNet.Slots?.SlottedPlayers;
		if (players == null || players.Count == 0) return false;

		if (playerIdx >= 0 && playerIdx < players.Count) {
			if (players[playerIdx].IsLocal) return false;

			PlayerAgent agent = players[playerIdx].PlayerAgent.Cast<PlayerAgent>();

			if (!overridePreferAlive && ConfigMgr.PreferSpectateAlive &&
			    agent.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
				return false;

			SetTarget(agent);

			bool diffTarget = playerIdx != _lastTargetPlayerIdx;
			if ((ConfigMgr.NoPosLerpOnSwitchTarget && diffTarget) ||
			    !_freecam) {
				SetEye(GetTargetOrbitCenter(), true);
			}

			_lastTargetPlayerIdx = playerIdx;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Gets the current target orbit center, which is based on the target player's eye position with a vertical offset.
	/// </summary>
	/// <returns>target orbit center position, or vertical if target is null</returns>
	private Vector3 GetTargetOrbitCenter() {
		var eyeTmp = Vector3.zero;
		if (_target != null) {
			var ag = _target.Agent;
			if (_target.IsCaptured) {
				if ((_lastPouncer == null || (_lastPouncer.CapturedPlayer?.Pointer ?? IntPtr.Zero) != ag.Pointer) &&
				    (PouncerTracker.Instance?.TryGetCapturingPouncer(ag, out var p) ?? false)) {
					_lastPouncer = p;
				}

				eyeTmp = _lastPouncer == null ? ag.m_eyePosition : _lastPouncer.transform.position;
			} else {
				eyeTmp = _target.Agent.m_eyePosition;
			}
		}

		eyeTmp.y += ConfigMgr.CameraOrbitVerticalOffset;
		return eyeTmp;
	}

	/// <summary>
	/// Behavior when transitioning from follow to freecam. Called when <see cref="_freecam"/> is toggled on.
	/// </summary>
	private void OnFollow2Free() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFreecam failed - target not ready");
			return;
		}

		UpdateYawPitchWithFollowView(true);
		SpectateUI.Instance?.MarkUIDirty();
	}

	/// <summary>
	/// Behavior when transitioning from freecam to follow. Called when <see cref="_freecam"/> is toggled off.
	/// </summary>
	private void OnFree2Follow() {
		if (!TargetReady) {
			Logger.Error("SpectateCam: OnTransitionToFollow failed - target not ready");
			return;
		}

		UpdateYawPitchWithFollowView(true);
		SpectateUI.Instance?.MarkUIDirty();
	}

	/// <summary>
	/// Updates the yaw and pitch to match the target's forward direction and configured camera pitch angle.
	/// It assumes _target is not null and has necessary components, so it should only be called when <see cref="TargetReady"/> is true.
	/// </summary>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	private void UpdateYawPitchWithFollowView(bool instant) {
		SetYaw(Vector3.SignedAngle(Vector3.forward, _target!.Agent.Forward, Vector3.up), instant);
		SetPitch(ConfigMgr.CameraPitchAngleDeg, instant);
	}

	/// <summary>
	/// Adjusts the pitch by a given delta, with optional instant application. Clamps the result within allowed limits.
	/// </summary>
	/// <param name="deltaPitch">pitch amount to adjust with</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void AdjustPitch(float deltaPitch, bool instant = false) {
		SetPitch(_pitchTarget + deltaPitch, instant);
	}

	/// <summary>
	/// Adjusts the yaw by a given delta, with optional instant application. Clamps the result within allowed limits.
	/// </summary>
	/// <param name="deltaYaw">yaw amount to adjust with</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void AdjustYaw(float deltaYaw, bool instant = false) {
		SetYaw(_yawTarget + deltaYaw, instant);
	}

	/// <summary>
	/// Sets the pitch to a given value, with optional instant application. Clamps the result within allowed limits.
	/// </summary>
	/// <param name="pitch">desired pitch</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetPitch(float pitch, bool instant = false) {
		pitch = Mathf.Clamp(pitch, PitchAngleDegMin, PitchAngleDegMax);
		_pitchTarget = pitch;
		if (instant) _pitch = pitch;
	}

	/// <summary>
	/// Sets the yaw to a given value, with optional instant application. Clamps the result within allowed limits.
	/// </summary>
	/// <param name="yaw">desired yaw</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetYaw(float yaw, bool instant = false) {
		_yawTarget = yaw;
		if (instant) _yaw = yaw;
	}

	/// <summary>
	/// Sets the XZ position of the eye to a given value, with optional instant application. Y component of the input is ignored.
	/// </summary>
	/// <param name="eyeXZ">vector to take XZ position from</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetEyeXZ(Vector3 eyeXZ, bool instant = false) {
		eyeXZ.y = 0f;
		_eyeXZTarget = eyeXZ;
		if (instant) _eyeXZ = eyeXZ;
	}

	/// <summary>
	/// Sets the Y position of the eye to a given value, with optional instant application.
	/// </summary>
	/// <param name="eyeY">desired eye Y position</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetEyeY(float eyeY, bool instant = false) {
		_eyeYTarget = eyeY;
		if (instant) _eyeY = eyeY;
	}

	/// <summary>
	/// Sets the eye position using a given vector, with optional instant application. XZ and Y components are handled separately.
	/// </summary>
	/// <param name="pos">desired eye position</param>
	/// <param name="instant">whether the camera should instantly snap to the updated position</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetEye(Vector3 pos, bool instant = false) {
		SetEyeXZ(pos, instant);
		SetEyeY(pos.y, instant);
	}
}
