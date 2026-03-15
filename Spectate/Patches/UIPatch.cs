using CellMenu;
using HarmonyLib;
using Player;
using Vector3 = UnityEngine.Vector3;

namespace Spectate.Patches;

[HarmonyPatch]
public class UIPatch {
	/// <summary>
	/// Prevents health GUI updates from local player when spectating
	/// </summary>
	[HarmonyPatch(
		typeof(PlayerGuiLayer),
		nameof(PlayerGuiLayer.UpdateHealth)
	)]
	[HarmonyPrefix]
	private static bool PlayerGuiLayer_UpdateHealth(PlayerGuiLayer __instance) {
		return !(SpectateCam.Instance?.Active ?? false);
	}

	/// <summary>
	/// Prevents infection GUI updates from local player when spectating
	/// </summary>
	[HarmonyPatch(
		typeof(PlayerGuiLayer),
		nameof(PlayerGuiLayer.UpdateInfection)
	)]
	[HarmonyPrefix]
	private static bool PlayerGuiLayer_UpdateInfection(PlayerGuiLayer __instance) {
		return !(SpectateCam.Instance?.Active ?? false);
	}

	/// <summary>
	/// Ensure certain UIs stay hidden when spectating.
	/// </summary>
	[HarmonyPatch(
		typeof(GuiManager),
		nameof(GuiManager.OnFocusStateChanged)
	)]
	[HarmonyPostfix]
	private static void FocusStateChange(GuiManager __instance, eFocusState state) {
		if (!(SpectateCam.Instance?.Active ?? false)) return;

		// Ensure inventory PUI is disabled
		GuiManager.PlayerLayer.Inventory.SetVisible(false);
	}

	/// <summary>
	/// Ensure certain UIs stay hidden when spectating.
	/// </summary>
	[HarmonyPatch(
		typeof(PlayerGuiLayer),
		nameof(PlayerGuiLayer.UpdateGUIElementsVisibility)
	)]
	[HarmonyPostfix]
	private static void UpdateGUIElementsVisibility(GuiManager __instance, eFocusState currentState) {
		if (!(SpectateCam.Instance?.Active ?? false)) return;

		// Ensure inventory PUI is disabled
		GuiManager.PlayerLayer.Inventory.SetVisible(false);
	}

	/// <summary>
	/// Disable the weird UI offset when players are downed
	/// </summary>
	[HarmonyPatch(
		typeof(PlayerGuiLayer),
		nameof(PlayerGuiLayer.ApplyMovementSway)
	)]
	[HarmonyPrefix]
	private static void PlayerGuiLayer_ApplyMovementSway(PlayerGuiLayer __instance, ref Vector3 sway) {
		var player = PlayerManager.GetLocalPlayerAgent();
		if (player != null && player.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed) {
			sway = Vector3.zero;
		}
	}

	/// <summary>
	/// Auto center map on player when spectating, respecting vanilla setting
	/// </summary>
	[HarmonyPatch(
		typeof(CM_PageMap),
		nameof(CM_PageMap.OnEnable)
	)]
	[HarmonyPostfix]
	private static void CM_PageMap_OnEnable(CM_PageMap __instance) {
		if ((SpectateCam.Instance?.Active ?? false) &&
		    CellSettingsManager.GetBoolValue(eCellSettingID.HUD_MapAutoCenterOnPlayer)) {
			int slot = SpectateCam.Instance.Target?.SAgent.PlayerSlotIndex() ?? -1;
			if (slot == -1) return; // unlikely but whatever
			Vector3 position = __instance.m_syncedPlayers[slot].transform.position;
			Vector3 vector = __instance.m_mapHolder.transform.InverseTransformPoint(position);
			__instance.m_mapMover.transform.localPosition -= vector;
		}
	}
}
