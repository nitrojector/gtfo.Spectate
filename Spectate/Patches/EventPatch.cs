using HarmonyLib;
using SNetwork;

namespace Spectate.Patches;

[HarmonyPatch]
public class EventPatch {
	[HarmonyPatch(
		typeof(GS_ReadyToStopElevatorRide),
		nameof(GS_ReadyToStopElevatorRide.Enter)
	)]
	[HarmonyPostfix]
	private static void GS_ReadyToStopElevatorRide_Enter() {
		Events.RaiseSessionStart();
	}

	[HarmonyPatch(
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.SyncEnter)
	)]
	[HarmonyPostfix]
	private static void PLOC_Downed_SyncEnter() {
		Events.RaiseAnyPlayerDeath();
	}

	[HarmonyPatch(
		typeof(RundownManager),
		nameof(RundownManager.EndGameSession)
	)]
	[HarmonyPrefix]
	private static void EndGameSession() {
		Events.RaiseSessionEnd();
	}

	[HarmonyPatch(
		typeof(SNet_SessionHub),
		nameof(SNet_SessionHub.LeaveHub)
	)]
	[HarmonyPrefix]
	private static void LeaveHub() {
		Events.RaiseSessionEnd();
	}
}
