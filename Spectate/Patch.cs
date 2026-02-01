using HarmonyLib;
using Player;
using SNetwork;
using Spectate.Config;
using UnityEngine;

namespace Spectate;

[HarmonyPatch]
public class Patch {
	[HarmonyPatch(
		typeof(GS_ReadyToStopElevatorRide),
		nameof(GS_ReadyToStopElevatorRide.Enter)
	)]
	[HarmonyPostfix]
	public static void GS_ReadyToStopElevatorRide_Enter(GS_ReadyToStopElevatorRide __instance) {
		SpectateCam.Instance?.Load();
	}

	[HarmonyPatch(
		typeof(RundownManager),
		nameof(RundownManager.EndGameSession)
	)]
	[HarmonyPrefix]
	private static void EndGameSession() {
		SpectateCam.Instance?.Unload();
	}

	[HarmonyPatch(
		typeof(SNet_SessionHub),
		nameof(SNet_SessionHub.LeaveHub)
	)]
	[HarmonyPrefix]
	private static void LeaveHub() {
		SpectateCam.Instance?.Unload();
	}

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

	[HarmonyPatch(
		typeof(PlayerAgent),
		nameof(PlayerAgent.GetDetectionMod)
	)]
	[HarmonyPrefix]
	public static bool PlayerAgent_GetDetectionMod(PlayerAgent __instance, ref Vector3 dir, float distance) {
		if (SpectateCam.Instance?.Active ?? false) {
			dir = SpectateCam.Instance.LastCamDir;
		}

		return true;
	}

	[HarmonyPatch(
		typeof(PlayerSync),
		nameof(PlayerSync.SendLocomotion)
	)]
	[HarmonyPrefix]
	public static bool PlayerSync_SendLocomotion(PlayerSync __instance, PlayerLocomotion.PLOC_State state, Vector3 pos,
		ref Vector3 lookDir, float velFwd, float velRight) {
#if DEBUG
		if (!__instance.m_agent.IsLocallyOwned) return true;
#endif
		if (SpectateCam.Instance?.Active ?? false) {
			lookDir = SpectateCam.Instance.LastCamDir;
		}

		return true;
	}

	[HarmonyPatch(
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.Enter)
	)]
	[HarmonyPostfix]
	public static void PLOC_Downed_Enter(PLOC_Downed __instance) {
		if (!__instance.m_owner.IsLocallyOwned || SpectateCam.Instance == null) return;

		if (ConfigMgr.SwitchOnDeath && !SpectateCam.Instance.Active) {
			SpectateCam.Instance.Attach();
		}
	}

	[HarmonyPatch(
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.Exit)
	)]
	[HarmonyPostfix]
	public static void PLOC_Downed_Exit(PLOC_Downed __instance) {
		if (!__instance.m_owner.IsLocallyOwned || SpectateCam.Instance == null) return;

		if (SpectateCam.Instance.Active) {
			SpectateCam.Instance.Detach();
		}
	}
}
