using AIGraph;
using Player;
using SNetwork;
using UnityEngine;

namespace Spectate;

public class SpectateTarget {
	public readonly PlayerAgent Agent;

	public Transform Transform => Agent.transform;

	public FPSCamera? FPSCamera => Agent.FPSCamera;

	public FirstPersonItemHolder? FPHolder => Agent.FPItemHolder;

	public PlayerLocomotion Locomotion => Agent.Locomotion;

	public SNet_Player SAgent => Agent.Owner;
	public bool IsBot => SAgent.IsBot;
	public bool IsLocal => SAgent.IsLocal;

	public Dam_PlayerDamageBase Damage => Agent.Damage;
	public float Health => Damage.Health / Damage.HealthMax;
	public float HealthPercent => Health * 100f;

	public PlayerInventoryBase Inventory => Agent.Inventory;
	public InventorySlot ActiveItemSlot => Inventory.WieldedSlot;

	public AIG_CourseNode CourseNode => Agent.CourseNode;

	public SpectateTarget(PlayerAgent agent) {
		Agent = agent;
	}

	public static bool operator ==(SpectateTarget? a, SpectateTarget? b) {
		if (ReferenceEquals(a, b))
			return true;
		if (ReferenceEquals(a, null))
			return b is null || b.Agent == null;
		if (ReferenceEquals(b, null))
			return a.Agent == null;
		return a.Agent == b.Agent;
	}

	public static bool operator !=(SpectateTarget? a, SpectateTarget? b) {
		return !(a == b);
	}

	public override bool Equals(object? other) {
		return other is SpectateTarget target && this == target;
	}

	public override int GetHashCode() {
		return Agent.GetHashCode();
	}

	public void SetRigActive(bool active) {
		Agent.PlayerSyncModel.gameObject.SetActive(active);
	}
}
