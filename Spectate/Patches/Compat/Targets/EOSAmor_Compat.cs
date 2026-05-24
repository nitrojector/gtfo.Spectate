using HarmonyLib;

using EOSEMPHandler = EOS.Modules.World.EMP.Handlers;

namespace Spectate.Patches.Compat.Targets;

[HarmonyPatch]
public class EOSAmor_Compat {
	[HarmonyPatch(
		typeof(EOSEMPHandler.EMPPlayerHudHandler),
		"DeviceOn"
	)]
	[HarmonyPrefix]
	private static bool Prefix__EMPPlayerHudHandler_DeviceOn() {
		return !(SpectateCam.Instance?.Active ?? false);
	}

	[HarmonyPatch(
		typeof(EOSEMPHandler.EMPPlayerHudHandler),
		"FlickerDevice"
	)]
	[HarmonyPrefix]
	private static bool Prefix__EMPPlayerHudHandler_FlickerDevice() {
		return !(SpectateCam.Instance?.Active ?? false);
	}
}

[CompatTarget(Plugin.GUID_EOSAmor)]
public class EOSAmor_Compat__Reflection {
	[CompatPatch(
		HarmonyPatchType.Prefix,
		"EOS.Modules.World.EMP.Handlers.EMPPlayerHudHandler",
		"DeviceOn"
	)]
	private static bool Prefix__EMPPlayerHudHandler_DeviceOn() {
		return !(SpectateCam.Instance?.Active ?? false);
	}

	[CompatPatch(
		HarmonyPatchType.Prefix,
		"EOS.Modules.World.EMP.Handlers.EMPPlayerHudHandler",
		"FlickerDevice"
	)]
	private static bool Prefix__EMPPlayerHudHandler_FlickerDevice() {
		return !(SpectateCam.Instance?.Active ?? false);
	}
}
