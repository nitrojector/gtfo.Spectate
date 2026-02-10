using HarmonyLib;

namespace Spectate.Patches;

[HarmonyPatch]
public class UIPatch {
	/// <summary>
	/// Prevents health GUI updates from local player when spectating
	/// </summary>
	[HarmonyPatch(
		typeof(Dam_PlayerDamageLocal),
		nameof(Dam_PlayerDamageLocal.UpdateHealthGui)
	)]
	[HarmonyPrefix]
	public static bool Dam_PlayerDamageLocal_UpdateHealthGui(Dam_PlayerDamageLocal __instance) {
		if (SpectateCam.Instance?.Active ?? false) {
			return false;
		}

		return true;
	}

	/// <summary>
	/// Ensure certain UIs stay hidden when spectating.
	/// </summary>
	[HarmonyPatch(
		typeof(GuiManager),
		nameof(GuiManager.OnFocusStateChanged)
	)]
	[HarmonyPostfix]
	public static void FocusStateChange(GuiManager __instance, eFocusState state) {
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
	public static void UpdateGUIElementsVisibility(GuiManager __instance, eFocusState currentState) {
		if (!(SpectateCam.Instance?.Active ?? false)) return;

		// Ensure inventory PUI is disabled
		GuiManager.PlayerLayer.Inventory.SetVisible(false);
	}

	/// <summary>
	/// Prevent local player damage animation from playing when spectating
	/// </summary>
	[HarmonyPatch(
		typeof(PUI_LocalPlayerStatus),
		nameof(PUI_LocalPlayerStatus.SetDamageAnim)
	)]
	[HarmonyPrefix]
	public static bool PUI_LocalPlayerStatus_SetDamageAnim(PUI_LocalPlayerStatus __instance) {
		if (SpectateCam.Instance?.Active ?? false) {
			return false;
		}

		return true;
	}
}
