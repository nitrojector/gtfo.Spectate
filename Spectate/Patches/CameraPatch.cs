using HarmonyLib;
using Player;
using UnityEngine;

namespace Spectate.Patches;

[HarmonyPatch]
public class CameraPatch {
	/// <summary>
	/// Player pings uses LocalPlayerAgent.CamPos for ray cast origin,
	/// but our method of updating FPSCamera position isn't reflected in CamPos.
	/// Updates CamPos to match our camera position when spectating,
	/// so that player pings work as expected.
	/// This fixes pings for PingEverything
	/// </summary>
	[HarmonyPatch(
		typeof(LocalPlayerAgent),
		"get_CamPos"
	)]
	[HarmonyPrefix]
	private static bool PlayerAgent_get_CamPos(ref Vector3 __result) {
		if (SpectateCam.Instance?.Active ?? false) {
			__result = SpectateCam.Instance.CameraPos;
			return false;
		}

		return true;
	}

	/// <summary>
	/// See <see cref="PlayerAgent_get_CamPos"/>.
	/// This patch fixes pings for the vanilla ping system.
	/// </summary>
	[HarmonyPatch(
		typeof(LocalPlayerAgent),
		nameof(LocalPlayerAgent.UpdateGlobalInput)
	)]
	[HarmonyPrefix]
	private static void PlayerAgent_UpdateGlobalInput(LocalPlayerAgent __instance) {
		// TODO: We could have just patched the getter for m_camPos
		if (SpectateCam.Instance?.Active ?? false) {
			__instance.m_camPos = SpectateCam.Instance.CameraPos;
		}
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
	private static bool PlayerAgent_GetDetectionMod(PlayerAgent __instance, ref Vector3 dir, float distance) {
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
	private static bool PlayerSync_SendLocomotion(PlayerSync __instance, PlayerLocomotion.PLOC_State state, Vector3 pos,
		ref Vector3 lookDir, float velFwd, float velRight) {
#if DEBUG
		if (!__instance.m_agent.IsLocallyOwned) return true;
#endif
		if (SpectateCam.Instance?.Active ?? false) {
			lookDir = SpectateCam.Instance.DiegeticCamDir;
		}

		return true;
	}
}
