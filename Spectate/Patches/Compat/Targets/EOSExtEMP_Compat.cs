using HarmonyLib;

namespace Spectate.Patches.Compat.Targets;

[CompatTarget("Inas.EOSExt.EMP")]
public class EOSExtEMP_Compat {
	[CompatPatch(
		HarmonyPatchType.Prefix,
		"EOSExt.EMP.Impl.Handlers.EMPPlayerHudHandler",
		"OnTick"
	)]
	private static bool EMPPlayerHudHandler_OnTick(object __instance) {
		return !(SpectateCam.Instance?.Active ?? false);
	}
}
