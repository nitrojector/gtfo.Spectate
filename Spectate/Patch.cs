using HarmonyLib;
using Player;
using Spectate.Config;

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
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.Enter)
	)]
	[HarmonyPostfix]
	public static void PLOC_Downed_Enter(PLOC_Downed __instance) {
		if (__instance.m_owner.IsLocallyOwned && SpectateCam.Instance != null) {
			if (ConfigMgr.SwitchOnDeath && !SpectateCam.Instance.Active) {
				SpectateCam.Instance.Attach();
			}
		}
	}

	[HarmonyPatch(
		typeof(PLOC_Downed),
		nameof(PLOC_Downed.Exit)
	)]
	[HarmonyPostfix]
	public static void PLOC_Downed_Exit(PLOC_Downed __instance) {
		if (__instance.m_owner.IsLocallyOwned && SpectateCam.Instance != null) {
			if (!ConfigMgr.DevEnables(eDevOpts.AllowSpectatingAnytime) && SpectateCam.Instance.Active) {
				SpectateCam.Instance.Detach();
			}
		}
	}
}
