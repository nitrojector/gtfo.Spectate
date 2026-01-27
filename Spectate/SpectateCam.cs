using Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spectate;

public class SpectateCam : MonoBehaviour {
	public static SpectateCam Instance { get; set; }

	public bool Ready => _self != null;
	public bool Active { get; private set; } = false;

	public Vector3 CurrentOffset = Vector3.zero;
	public readonly Vector3 IdealOffset = new(0.0f, 2.0f, -2.0f);

	private Transform? _selfParent = null;
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
		_selfParent = _self.Agent.FPSCamera.transform.parent;
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

	public bool Attach() {
		if (_self == null) return false;
		if (_self.FPSCamera == null) return false;
		if (_target == null && !TrySetAnyNonLocalTarget()) return false; // TODO: TrySet is for testing only

		SetRelatedActive(_self, false);

		_self.FPSCamera.transform.parent = _target!.Transform;
		_self.FPSCamera.transform.localPosition = IdealOffset;
		_self.FPSCamera.transform.localRotation = Quaternion.identity;
		UpdateCull();
		Logger.Debug("Attach");
		return true;
	}

	public bool Detach() {
		if (_self == null || _self.FPSCamera == null) return false;
		_self.FPSCamera.transform.parent = _selfParent;
		SetRelatedActive(_self, true);
		RevertCull();
		Logger.Debug("Detach");
		return true;
	}

	public void SetActive(bool active) {
		Active = active;
	}

	private void Update() {
		if (!enabled || !gameObject.activeInHierarchy) return;

		if (Active) {
			UpdateCamPos();
			UpdateCull();
		}

		if (!Input.GetKeyDown(KeyCode.V)) return;

		if (Active) {
			if (Detach()) SetActive(false);
			else Logger.Error("Failed to detach SpecCam.");
		} else {
			if (Attach()) SetActive(true);
			else Logger.Error("Failed to attach SpecCam.");
		}
	}

	// ReSharper disable Unity.PerformanceAnalysis
	static void SetRelatedActive(SpectateTarget target, bool active) {
		target.Locomotion.enabled = active;

		// if (target.FPSCamera != null) target.FPSCamera.enabled = active;

		// TODO: What to enable, what to disable ?

		var fpsCamera = target.FPSCamera;
		if (fpsCamera != null) {
			fpsCamera.MouseLookEnabled = active;
			fpsCamera.PlayerAgentRotationEnabled = active;
			fpsCamera.PlayerMoveEnabled = active;
		}

		// target.Inventory.enabled = active;
		// target.FPSCamera?.m_holder.GetComponentInChildren<FirstPersonItemHolder>()?.gameObject.SetActive(active);
		// target.Agent.PlayerSyncModel.gameObject.SetActive(active);
	}

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
		Vector3 eyePos = _target.Agent.Position;
		eyePos.y = _target.Agent.m_eyePosition.y;
		eyePos += IdealOffset.z * _target.Transform.forward;
		eyePos += IdealOffset.y * Vector3.up;

		_self.FPSCamera.OverridePositionAndRotation(eyePos,
			Quaternion.LookRotation(_target.Transform.forward));
	}

	void UpdateCull() {
		if (_target == null || _self == null || _self.FPSCamera == null) {
			Logger.Error("SpectateCam: UpdateCull failed - target or self is null.");
			return;
		}

		Vector3 targetCullPosition = _target.Agent.Position;
		if (Physics.Raycast(targetCullPosition, Vector3.down, out var hit, 64f, LayerManager.MASK_WORLD))
			targetCullPosition = hit.m_Point;

		CameraManager.CullingPosition = targetCullPosition;
		CameraManager.CullingDirection = _self.FPSCamera.Forward;

		_self.Agent.m_movingCuller.UpdatePosition(_self.Agent.m_dimensionIndex, targetCullPosition);
		if (_self.Agent.m_movingCuller.CurrentNode != _target.Agent.CourseNode.m_cullNode)
			_self.Agent.m_movingCuller.SetCurrentNode(_target.Agent.CourseNode.m_cullNode);
	}

	void RevertCull() {
		// TODO:
	}
}
