using BepInEx;
using BepInEx.Unity.IL2CPP;
using Globals;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Spectate.Config;
using Spectate.Patches;
using Spectate.Patches.Compat;
using Spectate.UI;

namespace Spectate;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(GUID_EOSExtEMP, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(GUID_EEC, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin {
	public const string NAME = "Spectate";
	public const string GUID = "io.takina.gtfo." + NAME;
	public const string VERSION = "1.5.12";

	public const string GUID_EOSExtEMP = "Inas.EOSExt.EMP";
	public const string GUID_EEC = "GTFO.EECustomization";

	public event Action? OnManagersSetup;
	public static GameObject? PluginObject;


	public override void Load() {
		Logger.Setup();
		Logger.Info($"{NAME} [{GUID} @ {VERSION}]");

		Harmony h = new Harmony(GUID);
		ConfigMgr.Process();

		// ClassInjector.RegisterTypeInIl2Cpp<SpectateTarget>();
		ClassInjector.RegisterTypeInIl2Cpp<SpectateCam>();
		ClassInjector.RegisterTypeInIl2Cpp<SpectateUI>();
		ClassInjector.RegisterTypeInIl2Cpp<SpectateConfigUpdater>();
		ClassInjector.RegisterTypeInIl2Cpp<PouncerTracker>();
		ClassInjector.RegisterTypeInIl2Cpp<PouncerTrackingDart>();
		ClassInjector.RegisterTypeInIl2Cpp<Wm>();

		OnManagersSetup += Initialize;
		Global.add_OnAllManagersSetup(OnManagersSetup);

		Logger.Info("Patching...");

		ApplyPatch<EventPatch>(h);
		ApplyPatch<UIPatch>(h);
		ApplyPatch<CameraPatch>(h);
		ApplyPatch<AnimationPatch>(h);
		ApplyPatch<PouncerPatch>(h);

		CompatPatcher.PatchAll(h);

		// CompatPatcher throws exception when patching EEC, great
		if (IL2CPPChainloader.Instance.Plugins.ContainsKey(GUID_EEC)){
			ApplyPatch<Patches.Compat.Targets.ECC_Compat>(h);
		}

		Logger.Info("Finished Patching");
	}

	private static void ApplyPatch<T>(Harmony h) {
		h.PatchAll(typeof(T));
	}

	private void Initialize() {
		Logger.Debug("Initializing Spectate GameObject's...");
		PluginObject = new GameObject(GUID);
		UnityEngine.Object.DontDestroyOnLoad(PluginObject);
		var ui = PluginObject.AddComponent<SpectateUI>();
		PluginObject.AddComponent<SpectateCam>();
		PluginObject.AddComponent<SpectateConfigUpdater>();
		PluginObject.AddComponent<PouncerTracker>();
		PluginObject.AddComponent<Wm>();
		ui.ReplicateUI();
	}

	public override bool Unload() {
		ConfigMgr.WriteConfigIfDirty();
		return true;
	}
}
