using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Spectate;

[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BasePlugin {
	public const string NAME = "Spectate";
	public const string GUID = "io.takina.gtfo." + NAME;
	public const string VERSION = "1.0.0";

	public override void Load() {
		Logger.Setup();

		Logger.Info($"{NAME} [{GUID} @ {VERSION}]");
		Logger.Info("Patching...");
		Harmony h = new Harmony(GUID);
		ConfigMgr.Process();
		AddComponent<SpectateCam>();
		h.PatchAll(typeof(Patch));
		Logger.Info("Finished Patching");
	}
}
