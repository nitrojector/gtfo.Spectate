using HarmonyLib;
using Player;
using SNetwork;
using Spectate.Config;
using UnityEngine;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace Spectate;

[HarmonyPatch]
public class Patch {
	public const float PreStop = 1.0f;
	public const int IntervalStopFrames = 10;

	[HarmonyPatch(
		typeof(GS_ReadyToStopElevatorRide),
		nameof(GS_ReadyToStopElevatorRide.Enter)
	)]
	[HarmonyPostfix]
	public static void GS_ReadyToStopElevatorRide_Enter(GS_ReadyToStopElevatorRide __instance) {
		SpectateCam.Instance?.Load();
	}

	[HarmonyPatch(
		typeof(LocalPlayerAgent),
		"get_CamPos"
	)]
	[HarmonyPrefix]
	// NOTE: Player pings uses LocalPlayerAgent.CamPos for ray cast origin,
	//   but our method of updating FPSCamera position isn't reflected in CamPos.
	public static bool PlayerAgent_get_CamPos(ref Vector3 __result) {
		if (SpectateCam.Instance?.Active ?? false) {
			__result = SpectateCam.Instance.CameraPos;
			return false;
		}

		return true;
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
			dir = SpectateCam.Instance.DiegeticCamDir;
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
			lookDir = SpectateCam.Instance.DiegeticCamDir;
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

		// NOTE: We transition player to downed posture FOR REAL
		//   hide legs to align with vanilla feel
		Play_Player_PLOC_Down_Animation(__instance);
		if (!SpectateCam.Instance.Active) {
			var psm = __instance.m_owner.PlayerSyncModel;
			psm.SetGFXVisible(psm.m_gfxLegs, false, false);
		} else {
			// TODO: NOTE: We are doing this half assed solution to force update player rig posture/position
			__instance.m_owner.StartCoroutine(BumF__kRandomSolutionRoutine().WrapToIl2Cpp());
		}

		if (ConfigMgr.SwitchOnDeath && !SpectateCam.Instance.Active) {
			SpectateCam.Instance.TryAttachDelayed(SpectateCam.DownToSpectateDelay);
		}
	}

	private static IEnumerator BumF__kRandomSolutionRoutine() {
		yield return new WaitForSeconds(PreStop);

		SpectateCam.Instance?.SetRelatedActive(false);
		for (int i = 0; i < IntervalStopFrames; ++i) {
			yield return new WaitForEndOfFrame();
		}

		SpectateCam.Instance?.SetRelatedActive(SpectateCam.Instance.Active);
	}

	// NOTE: This method is superseded by Dam_PlayerDamageLocal_OnRevive,
	//   since it happens the instant the player is revived.
	// [HarmonyPatch(
	// 	typeof(PLOC_Downed),
	// 	nameof(PLOC_Downed.Exit)
	// )]
	// [HarmonyPostfix]
	// public static void PLOC_Downed_Exit(PLOC_Downed __instance) {
	// 	if (!__instance.m_owner.IsLocallyOwned || SpectateCam.Instance == null) return;
	//
	// 	if (SpectateCam.Instance.Active) {
	// 		SpectateCam.Instance.Detach();
	// 	}
	// }

	[HarmonyPatch(
		typeof(Dam_PlayerDamageLocal),
		nameof(Dam_PlayerDamageLocal.OnRevive)
	)]
	[HarmonyPostfix]
	public static void Dam_PlayerDamageLocal_OnRevive(Dam_PlayerDamageLocal __instance) {
		if (!__instance.Owner.IsLocallyOwned || SpectateCam.Instance == null) return;

		if (SpectateCam.Instance.Active) {
			SpectateCam.Instance.Detach();
		}

		var psm = __instance.Owner.PlayerSyncModel;
		psm.SetGFXVisible(psm.m_gfxLegs, true, true);
		__instance.Owner.AnimatorBody.Play("Rifle_Movement");
	}

	public static void Play_Player_PLOC_Down_Animation(PLOC_Downed instance) {
		instance.m_owner.AnimatorBody.Play("Dead", 1);
		instance.m_owner.AnimatorArms.SetLayerWeight(7, 0f);
		instance.m_owner.AnimatorArms.SetLayerWeight(8, 0f);
	}
}
