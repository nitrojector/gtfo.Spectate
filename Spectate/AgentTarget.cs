using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using SNetwork;
using UnityEngine;

namespace Spectate;

public class AgentTarget {
	public readonly PlayerAgent Agent;

	public Transform Transform => Agent.transform;

	public FPSCamera? FPSCamera => Agent.FPSCamera;

	public FirstPersonItemHolder? FPHolder => Agent.FPItemHolder;

	public PlayerLocomotion Locomotion => Agent.Locomotion;
	public PlayerLocomotion.PLOC_State LocomotionStateEnum => Locomotion.m_currentStateEnum;
	public bool IsDowned => LocomotionStateEnum == PlayerLocomotion.PLOC_State.Downed;

	public PlayerSyncModelData PlayerModel => Agent.PlayerSyncModel;

	public SNet_Player SAgent => Agent.Owner;
	public ulong Lookup => SAgent.Lookup;
	public bool IsBot => SAgent.IsBot;
	public bool IsLocal => SAgent.IsLocal;

	public Dam_PlayerDamageBase Damage => Agent.Damage;
	public float Health => Damage.Health / Damage.HealthMax;
	public float HealthPercent => Health * 100f;
	public float Infection => Damage.Infection;
	public float InfectionPercent => Infection * 100f;

	public PlayerInventoryBase? Inventory => Agent.Inventory;
	public InventorySlot? ActiveItemSlot => Inventory?.WieldedSlot;

	public AIG_CourseNode? CourseNode {
		get {
			if (IsCaptured && (PouncerTracker.Instance?.TryGetCapturingPouncer(
				    Agent, out var p
			    ) ?? false)) {
				return p?.m_ai?.m_enemyAgent?.CourseNode ?? Agent.CourseNode;
			}

			return Agent.CourseNode;
		}
	}

	public PlayerBackpack? Backpack => PlayerBackpackManager.GetBackpack(Agent.Owner);

	public PlayerAmmoStorage? AmmoStorage => Backpack?.AmmoStorage;

	public bool HasClipData {
		get {
			if (IsLocal) return true;
			if (PlayerManager.GetLocalPlayerAgent().Owner.IsMaster && IsBot) return true;
			return false;
		}
	}

	public bool IsCaptured => PouncerTracker.Instance != null && PouncerTracker.Instance.IsCaptured(Agent);

	public static bool CanSpectate(PlayerAgent agent) {
		bool isDowned = agent.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed;
		bool isCaptured = PouncerTracker.Instance != null && PouncerTracker.Instance.IsCaptured(agent);
		return isDowned && !isCaptured;
	}

	public AgentTarget(PlayerAgent agent) {
		Agent = agent;
	}

	public void SetRigActive(bool active) {
		Util.SetTargetActiveIfDiff(Agent.PlayerSyncModel.gameObject, active);
	}

	public void SetHostHiddenRigActive(bool active) {
		var psm = Agent.PlayerSyncModel;

		if (psm.m_gfxHead.Count == 0 || psm.m_gfxArms.Count == 0) {
			Util.FindAndSortGfxParts(psm.gameObject, out var gfxHead, out var gfxArms, out var gfxTorso,
				out var gfxLegs);
			psm.m_gfxHead = gfxHead;
			psm.m_gfxArms = gfxArms;
			psm.m_gfxTorso = gfxTorso;
			psm.m_gfxLegs = gfxLegs;
		}

		psm.SetHeadVisible(active, active);
		psm.SetArmsVisible(active, active);
	}

	public void SetRigTorsoLegsActive(bool active) {
		var psm = Agent.PlayerSyncModel;
		psm.SetGFXVisible(psm.m_gfxLegs, active, active);
		psm.SetGFXVisible(psm.m_gfxTorso, active, active);
	}

	public static bool operator ==(AgentTarget? a, AgentTarget? b) {
		if (ReferenceEquals(a, b))
			return true;
		if (ReferenceEquals(a, null))
			return b is null || b.Agent == null;
		if (ReferenceEquals(b, null))
			return a.Agent == null;
		return a.Agent == b.Agent;
	}

	public static bool operator !=(AgentTarget? a, AgentTarget? b) {
		return !(a == b);
	}

	public override bool Equals(object? other) {
		return other is AgentTarget target && this == target;
	}

	public override int GetHashCode() {
		return Agent.GetHashCode();
	}
}
