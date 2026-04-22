using HarmonyLib;

namespace Spectate.Patches.Compat.Targets;

using EECEMP = EEC.CustomAbilities.EMP.Handlers;

[HarmonyPatch]
public class ECC_Compat {
	[HarmonyPatch(
		typeof(EECEMP.EMPPlayerHudHandler),
		"DeviceOn"
	)]
	[HarmonyPrefix]
	private static bool Prefix__EMPPlayerHudHandler_DeviceOn() {
		return !(SpectateCam.Instance?.Active ?? false);
	}

	[HarmonyPatch(
		typeof(EECEMP.EMPPlayerHudHandler),
		"FlickerDevice"
	)]
	[HarmonyPrefix]
	private static bool Prefix__EMPPlayerHudHandler_FlickerDevice() {
		return !(SpectateCam.Instance?.Active ?? false);
	}
}

// [CompatTarget(Plugin.GUID_EEC)]
// public class ECC_Compat {
// 	[CompatPatch(
// 		HarmonyPatchType.Prefix,
// 		"EEC.CustomAbilities.EMP.Handlers.EMPPlayerHudHandler",
// 		"DeviceOn"
// 	)]
// 	private static bool Prefix__EMPPlayerHudHandler_DeviceOn() {
// 		return !(SpectateCam.Instance?.Active ?? false);
// 	}
//
// 	[CompatPatch(
// 		HarmonyPatchType.Prefix,
// 		"EEC.CustomAbilities.EMP.Handlers.EMPPlayerHudHandler",
// 		"FlickerDevice"
// 	)]
// 	private static bool Prefix__EMPPlayerHudHandler_FlickerDevice() {
// 		return !(SpectateCam.Instance?.Active ?? false);
// 	}
// }
