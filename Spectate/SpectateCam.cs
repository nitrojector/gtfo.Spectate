using Player;
using SNetwork;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spectate;

public class SpectateCam : MonoBehaviour {
	public static SpectateCam Instance { get; set; }

	public bool SelfReady => _self != null && _self.FPSCamera != null;
	public bool TargetReady => _target != null;
	public bool Active { get; private set; } = false;

	public Vector3 CurrentOffset = Vector3.zero;
	public readonly Vector3 IdealOffset = new(0.0f, 2.0f, -2.0f);

	private SpectateTarget? _self = null;
	private SpectateTarget? _target = null;

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
		_target!.Locomotion.enabled = active;

		var fpsCamera = _target.FPSCamera;
		if (fpsCamera != null) {
			fpsCamera.MouseLookEnabled = active;
			fpsCamera.PlayerAgentRotationEnabled = active;
			fpsCamera.PlayerMoveEnabled = active;
		}

		// target.Inventory.enabled = active;
		// target.FPSCamera?.m_holder.GetComponentInChildren<FirstPersonItemHolder>()?.gameObject.SetActive(active);
		// target.Agent.PlayerSyncModel.gameObject.SetActive(active);
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

		Vector3 eyePos = _target!.Agent.Position;
		eyePos.y = _target.Agent.m_eyePosition.y;
		eyePos += IdealOffset.z * _target.Transform.forward;
		eyePos += IdealOffset.y * Vector3.up;

		_self!.FPSCamera!.OverridePositionAndRotation(eyePos, Quaternion.LookRotation(_target.Transform.forward));
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
