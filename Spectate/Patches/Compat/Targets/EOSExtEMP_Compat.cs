using HarmonyLib;

namespace Spectate.Patches.Compat.Targets;

[HarmonyPatch]
public class EOSExtEMP_Compat {
	[HarmonyPatch(
		typeof(EOSExt.EMP.Impl.Handlers.EMPPlayerHudHandler),
		"OnTick"
	)]
	[HarmonyPrefix]
	private static bool EMPPlayerHudHandler_OnTick(object __instance) {
		return !(SpectateCam.Instance?.Active ?? false);
	}
}

[CompatTarget(Plugin.GUID_EOSExtEMP)]
public class EOSExtEMP_Compat__Reflection {
	[CompatPatch(
		HarmonyPatchType.Prefix,
		"EOSExt.EMP.Impl.Handlers.EMPPlayerHudHandler",
		"OnTick"
	)]
	private static bool EMPPlayerHudHandler_OnTick(object __instance) {
		return !(SpectateCam.Instance?.Active ?? false);
	}
}
