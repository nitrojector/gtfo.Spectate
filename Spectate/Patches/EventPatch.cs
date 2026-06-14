using CellMenu;
using GameData;
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

	[HarmonyPatch(
		typeof(SNet_SessionHub),
		nameof(SNet_SessionHub.OnJoinedLobby)
	)]
	[HarmonyPrefix]
	private static void PlayerJoin(SNet_Player player) {
		Events.RaisePlayerJoinLobby(player);
	}

	[HarmonyPatch(
		typeof(CheckpointManager),
		nameof(CheckpointManager.OnStateChange)
	)]
	[HarmonyPrefix]
	private static void CheckpointStateChange(pCheckpointState oldState, pCheckpointState newState) {
		if (newState.lastInteraction == eCheckpointInteractionType.ReloadCheckpoint) {
			Events.RaiseCheckpointReload();
		}
	}

	[HarmonyPatch(
		typeof(CM_PageLoadout),
		nameof(CM_PageLoadout.UpdatePlayerList)
	)]
	[HarmonyPostfix]
	private static void CM_PageLoadout_UpdatePlayerList() {
		Events.RaiseLoadoutPlayerListUpdate();
	}

	[HarmonyPatch(
		typeof(GameDataInit),
		nameof(GameDataInit.Initialize)
	)]
	[HarmonyPostfix]
	public static void Postfix__GameDataInit_Initialize() {
		Events.RaiseGameDataInitialized();
	}
}
