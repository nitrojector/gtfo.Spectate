using HarmonyLib;
using SNetwork;
using Spectate.UI;

namespace Spectate.Patches;

[HarmonyPatch]
public class EventPatch {
	/// <summary>
	/// Load spectate instance when ready to stop elevator ride
	/// </summary>
	[HarmonyPatch(
		typeof(GS_ReadyToStopElevatorRide),
		nameof(GS_ReadyToStopElevatorRide.Enter)
	)]
	[HarmonyPostfix]
	private static void GS_ReadyToStopElevatorRide_Enter() {
		SpectateCam.Instance?.Load();
	}

	/// <summary>
	/// Unload spectate instance when ending game
	/// </summary>
	[HarmonyPatch(
		typeof(RundownManager),
		nameof(RundownManager.EndGameSession)
	)]
	[HarmonyPrefix]
	private static void EndGameSession() {
		SpectateCam.Instance?.Unload();
		SpectateUI.Instance?.Unload();
	}

	/// <summary>
	/// Unload spectate instance when leaving game
	/// </summary>
	[HarmonyPatch(
		typeof(SNet_SessionHub),
		nameof(SNet_SessionHub.LeaveHub)
	)]
	[HarmonyPrefix]
	private static void LeaveHub() {
		SpectateCam.Instance?.Unload();
		SpectateUI.Instance?.Unload();
	}

	public static void Apply(Harmony h) {
		h.PatchAll(typeof(EventPatch));
	}
}
