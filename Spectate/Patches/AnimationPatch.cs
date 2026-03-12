using HarmonyLib;
using UnityEngine;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Spectate.Config;

namespace Spectate.Patches;

[HarmonyPatch]
public class AnimationPatch {
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
	/// Perform relevant adjustments to local player model when downed.
	/// Also initiates spectate cam attach if auto attach enabled in config.
	/// </summary>
	[HarmonyPatch(
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.Enter)
	)]
	[HarmonyPostfix]
	public static void PLOC_Downed_Enter(PLOC_Downed __instance) {
		Events.RaiseAnyPlayerDeath();
		if (!__instance.m_owner.IsLocallyOwned || SpectateCam.Instance == null) return;

		// NOTE: We transition player to downed posture FOR REAL
		//   hide legs to align with vanilla feel
		Play_Player_PLOC_Down_Animation(__instance);
		if (!SpectateCam.Instance.Active) {
			var psm = __instance.m_owner.PlayerSyncModel;
			psm.SetGFXVisible(psm.m_gfxTorso, false, false);
			psm.SetGFXVisible(psm.m_gfxLegs, false, false);
		} else {
			// TODO: NOTE: We are doing this half assed solution to force update player rig posture/position
			__instance.m_owner.StartCoroutine(BumF__kRandomSolutionRoutine().WrapToIl2Cpp());
		}

		if (ConfigMgr.SwitchOnDeath && SpectateCam.Instance.CanSpectate && !SpectateCam.Instance.Active) {
			SpectateCam.Instance.TryAttachDelayed(SpectateCam.DownToSpectateDelay);
		}
	}


	/// <summary>
	/// Reset player spectate target when they are revived.
	/// </summary>
	[HarmonyPatch(
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.Exit)
	)]
	[HarmonyPostfix]
	public static void PLOC_Downed_Exit(PLOC_Downed __instance) {
		SpectateCam.Instance?.ClearTarget();
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
		psm.SetGFXVisible(psm.m_gfxTorso, true, true);
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
