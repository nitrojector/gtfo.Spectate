using BepInEx;
using BepInEx.Unity.IL2CPP;
using Globals;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Spectate;

[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BasePlugin {
	public const string NAME = "Spectate";
	public const string GUID = "io.takina.gtfo." + NAME;
	public const string VERSION = "1.0.0";

	public event Action OnManagersSetup = () => { };
	public static GameObject? PluginObject;

	public override void Load() {
		Logger.Setup();
		Logger.Info($"{NAME} [{GUID} @ {VERSION}]");
		Logger.Info("Patching...");

		Harmony h = new Harmony(GUID);
		ConfigMgr.Process();

		// ClassInjector.RegisterTypeInIl2Cpp<SpectateTarget>();
		ClassInjector.RegisterTypeInIl2Cpp<SpectateCam>();
		ClassInjector.RegisterTypeInIl2Cpp<SpectateUI>();

		// TODO: ??? why does this not work
		// Initialize();
		OnManagersSetup += Initialize;
		Global.add_OnAllManagersSetup(OnManagersSetup);

		h.PatchAll(typeof(Patch));
		Logger.Info("Finished Patching");
	}

	private void Initialize() {
		Logger.Info("Initializing Plugin GameObject...");
		PluginObject = new GameObject(GUID);
		UnityEngine.Object.DontDestroyOnLoad(PluginObject);
		PluginObject.AddComponent<SpectateCam>();
		PluginObject.AddComponent<SpectateUI>();
	}
}
