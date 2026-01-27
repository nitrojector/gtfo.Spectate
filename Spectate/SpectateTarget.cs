using AIGraph;
using Player;
using SNetwork;
using UnityEngine;

namespace Spectate;

public class SpectateTarget {
	public PlayerAgent Agent;

	public Transform Transform => Agent.transform;

	public FPSCamera? FPSCamera => Agent.FPSCamera;

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

}
