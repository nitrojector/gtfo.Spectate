using Player;
using SNetwork;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Spectate;

public class SpectateCam : MonoBehaviour {
	public static SpectateCam? Instance { get; set; }

	public bool SelfReady => _self != null && _self.FPSCamera != null;
	public bool TargetReady => _target != null;
	public bool Active { get; private set; } = false;

	[NonSerialized] private Vector3 _orbitCenterOffset = new(0.0f, 0.325f, 0.0f);
	[NonSerialized] private Vector3 _pitchAdjustOffset = new(0.0f, 0.45f, 0.0f);
	[NonSerialized] private float _distanceFromEye = 0.625f;
	[NonSerialized] private float _scrollSensitivity = 0.5f;

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
		return true;
	}

	public bool Unload() {
		_self = null;
		_target = null;
		return true;
	}

	public void SetTarget(PlayerAgent agent) {
		_target = new SpectateTarget(agent);
	}

	bool Attach() {
		if (!SelfReady && !Load()) return false;
		if (!TargetReady && !TrySetAnyNonLocalTarget()) return false; // TODO: TrySet is for testing only

		SetRelatedActive(false);
		UpdateCull();
		Logger.Debug("Attach");
		return true;
	}

	bool Detach() {
		if (!SelfReady) return false;

		SetRelatedActive(true);
		RevertCull();
		Logger.Debug("Detach");
		return true;
	}

	public void SetActive(bool active) {
		Active = active;
	}

	private void Update() {
		if (!enabled || !gameObject.activeInHierarchy) return;

		if (Input.GetKeyDown(KeyCode.V) && InputMapper.Current.FocusStateFilterPass(eFocusState.FPS)) {
			if (Active) {
				if (Detach()) SetActive(false);
				else Logger.Error("Failed to detach SpecCam.");
			} else {
				if (Attach()) SetActive(true);
				else Logger.Error("Failed to attach SpecCam.");
			}
		}

		if (Active) {
			if (_target == null) {
				SetActive(false);
				return;
			}

			int idx = InputHelper.GetAlphaNumKeyDown();
			var agents = PlayerManager.PlayerAgentsInLevel;
			if (idx > 0 && idx - 1 < agents.Count) {
				if (!agents[idx - 1].IsLocallyOwned)
					SetTarget(agents[idx - 1]);
			}

			ProcessInput();
			UpdateCamPos();
			UpdateCull();
		}
	}

	// ReSharper disable Unity.PerformanceAnalysis
	void SetRelatedActive(bool active) {
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: SetRelatedActive failed - target or self not ready");
			return;
		}

		_self!.SetRigActive(active);
		_self.Locomotion.enabled = active;
		_self.Inventory.enabled = active;
		_self.FPHolder?.gameObject.SetActive(active);

		var fpsCamera = _self.FPSCamera;
		if (fpsCamera != null) {
			fpsCamera.MouseLookEnabled = active;
			fpsCamera.PlayerAgentRotationEnabled = active;
			fpsCamera.PlayerMoveEnabled = active;
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

	void UpdateCamPos() {
		if (!SelfReady || !TargetReady) {
			Logger.Error("SpectateCam: UpdateCull failed - target or self not ready");
			return;
		}

		// calculated desired view direction
		Vector3 dir = _target!.Agent.Forward;
		dir -= _pitchAdjustOffset;
		dir.Normalize();

		// raycast to avoid clipping into walls
		Vector3 orbitCenter = _target!.Agent.m_eyePosition + _orbitCenterOffset;
		Vector3 eyePos = orbitCenter - dir * _distanceFromEye;
		if (Physics.Raycast(orbitCenter, -dir, out var hit, _distanceFromEye, LayerManager.MASK_WORLD)) {
			eyePos = hit.m_Point + dir * 0.1f;
		}

		_self!.FPSCamera!.OverridePositionAndRotation(eyePos, Quaternion.LookRotation(dir));
	}

	void ProcessInput() {
		float scrollDelta = Input.mouseScrollDelta.y * _scrollSensitivity;
		if (Mathf.Abs(scrollDelta) > 0f) {
			if (InputHelper.OnlyModifies(KeyCode.LeftShift, KeyCode.RightShift)) {
				// adjust top down
				// TODO: this is for testing optimal angle only, final version should use mouse delta instead
				_pitchAdjustOffset.y += 0.05f * scrollDelta;
			} else if (InputHelper.OnlyModifies(KeyCode.LeftControl, KeyCode.RightControl)) {
				// adjust center vertical offset
				_orbitCenterOffset.y += 0.05f * scrollDelta;
			} else {
				// adjust distance
				_distanceFromEye -= 0.05f * scrollDelta;
			}
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

	void RevertCull() {
		// TODO:
	}
}
