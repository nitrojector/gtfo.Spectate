using HarmonyLib;

namespace Spectate.Patches.Compat.Targets;

[CompatTarget(Plugin.GUID_EOSExtEMP)]
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
