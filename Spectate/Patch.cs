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
	/// <summary>
	/// The delay after player downed before we do a in-game player update (unknown process)
	/// </summary>
	public const float PreStop = 1.0f;

	/// <summary>
	/// The number of frames to wait for the in-game player update post downed
	/// to correct player rig
	/// </summary>
	public const int IntervalStopFrames = 10;

	/// <summary>
	/// Load spectate instance when ready to stop elevator ride
	/// </summary>
	/// <param name="__instance"></param>
	[HarmonyPatch(
		typeof(GS_ReadyToStopElevatorRide),
		nameof(GS_ReadyToStopElevatorRide.Enter)
	)]
	[HarmonyPostfix]
	public static void GS_ReadyToStopElevatorRide_Enter(GS_ReadyToStopElevatorRide __instance) {
		SpectateCam.Instance?.Load();
	}

	/// <summary>
	/// Unload spectate instance when leaving game
	/// </summary>
	[HarmonyPatch(
		typeof(RundownManager),
		nameof(RundownManager.EndGameSession)
	)]
	[HarmonyPrefix]
	private static void EndGameSession() {
		SpectateCam.Instance?.Unload();
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
	}

	/// <summary>
	/// Player pings uses LocalPlayerAgent.CamPos for ray cast origin,
	/// but our method of updating FPSCamera position isn't reflected in CamPos.
	/// Updates CamPos to match our camera position when spectating,
	/// so that player pings work as expected.
	/// </summary>
	[HarmonyPatch(
		typeof(LocalPlayerAgent),
		"get_CamPos"
	)]
	[HarmonyPrefix]
	public static bool PlayerAgent_get_CamPos(ref Vector3 __result) {
		if (SpectateCam.Instance?.Active ?? false) {
			__result = SpectateCam.Instance.CameraPos;
			return false;
		}

		return true;
	}

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

	/// <summary>
	/// Corrects local player flashlight effective direction when spectating
	/// </summary>
	/// <returns></returns>
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

	/// <summary>
	/// Ensure clients receive updates of our real camera direction
	/// </summary>
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

	/// <summary>
	/// Perform relevant adjustments to local player model when downed.
	/// Also initiates spectate cam attach if auto attach enabled in config.
	/// </summary>
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

	/// <summary>
	/// A pretty bad solution to solve weird player rig attitude transitions
	/// when attached to spectate cam.
	/// Not game breaking, just visually out of place.
	/// This is only possible in dev mode.
	/// </summary>
	/// <returns></returns>
	private static IEnumerator BumF__kRandomSolutionRoutine() {
		yield return new WaitForSeconds(PreStop);

		SpectateCam.Instance?.SetRelatedActive(false);
		for (int i = 0; i < IntervalStopFrames; ++i) {
			yield return new WaitForEndOfFrame();
		}

		SpectateCam.Instance?.SetRelatedActive(SpectateCam.Instance.Active);
	}

	/// <summary>
	/// Revert changes and detaches spectate cam when player is revived.
	/// </summary>
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

	/// <summary>
	/// Transitions player to downed animation. Which the game does not do
	/// on the local player.
	/// </summary>
	private static void Play_Player_PLOC_Down_Animation(PLOC_Downed instance) {
		instance.m_owner.AnimatorBody.Play("Dead", 1);
		instance.m_owner.AnimatorArms.SetLayerWeight(7, 0f);
		instance.m_owner.AnimatorArms.SetLayerWeight(8, 0f);
	}
}
