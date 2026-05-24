using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Globals;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Spectate.Assets;
using UnityEngine;
using Spectate.Config;
using Spectate.Interop;
using Spectate.Network;
using Spectate.Network.Impl;
using Spectate.Patches;
using Spectate.Patches.Compat;
using Spectate.Patches.Compat.Targets;
using Spectate.UI;
using Spectate.UI.Support;

namespace Spectate;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(GUID_PlayerSync, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(GUID_EOSExtEMP, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(GUID_EEC, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(GUID_EOSAmor, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin {
	public const string NAME = "Spectate";
	public const string GUID = "io.takina.gtfo." + NAME;
	public const string VERSION = "1.6.0";

	public const string GUID_PlayerSync = "io.takina.gtfo.PlayerSync";
	public const string GUID_EOSExtEMP = "Inas.EOSExt.EMP";
	public const string GUID_EOSAmor = "Amor.ExcellentObjectiveSetup";
	public const string GUID_EEC = "GTFO.EECustomization";
	public const string ASM_ClonesoftJson = "Clonesoft.Json";

	internal static string PluginFolder { get; private set; } = "";

	public static readonly PlugVersion PlugVersion = new(VERSION);

	public event Action? OnManagersSetup;
	public static GameObject? PluginObject;


	public override void Load() {
		Logger.Setup();
		Logger.Info($"{NAME} [{GUID} @ {VERSION}]");

		Harmony h = new Harmony(GUID);
		ConfigMgr.Process();

		{
			try {
				_ = Assembly.Load(ASM_ClonesoftJson);
				Logger.Info($"ASM({ASM_ClonesoftJson}) loaded.");
			} catch (Exception ex) {
				Logger.Error($"Exception while loading assembly ({ASM_ClonesoftJson}): {ex}");
				Logger.Fatal($"Failed to load assembly ({ASM_ClonesoftJson}). " +
				             $"This is required for Spectate to function. Aborting...");
				return;
			}
		}

		PluginFolder = Path.GetDirectoryName(IL2CPPChainloader.Instance.Plugins[GUID].Location) ?? "";

		RegisterIl2CppTypes();

		OnManagersSetup += Initialize;
		Global.add_OnAllManagersSetup(OnManagersSetup);

		Logger.Info("Patching...");

		ApplyPatches(h);

		ApplyCompatPatches(h);

		Logger.Info("Finished Patching");
	}

	private static void ApplyPatches(Harmony h)
	{
		ApplyPatch<EventPatch>(h);
		ApplyPatch<UIPatch>(h);
		ApplyPatch<CameraPatch>(h);
		ApplyPatch<AnimationPatch>(h);
		ApplyPatch<PouncerPatch>(h);
		ApplyPatch<TooltipFixPatch>(h);

		ApplyPatch<SpectateSupportDisplay>(h);
		ApplyPatch<Net>(h);
	}

	private static void ApplyCompatPatches(Harmony h) {
		if (ConfigMgr.DevEnables(eDevOpts.CompatPatchUsingReflection) ||
		    ConfigMgr.CompatPatchUsingReflection) {
			CompatPatcher.PatchAll(h);
			return;
		}

		ApplyPatchIfLoaded<EOSExtEMP_Compat>(h, GUID_EOSExtEMP);
		ApplyPatchIfLoaded<ECC_Compat>(h, GUID_EEC);
		ApplyPatchIfLoaded<EOSAmor_Compat>(h, GUID_EOSAmor);
	}

	private static void ApplyPatchIfLoaded<T>(Harmony h, string guid) {
		if (IL2CPPChainloader.Instance.Plugins.ContainsKey(guid)){
			ApplyPatch<T>(h);
			Logger.Info($"Applied compat patches for plugin with GUID {guid}.");
			return;
		}
		Logger.Info($"Plugin with GUID {guid} not found, skipping compat patches for it.");
	}

	private static void ApplyPatch<T>(Harmony h) {
		h.PatchAll(typeof(T));
	}

	private static void RegisterIl2CppTypes()
	{
		foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (type.GetCustomAttribute<RegisterIl2CppAttribute>() is not null)
				ClassInjector.RegisterTypeInIl2Cpp(type);
		}
	}

	private void Initialize() {
		Logger.Debug("Loading Assets...");
		SharedAssetLibrary.Load();
		EmojiLibrary.Load();

		Logger.Debug("Initializing Spectate GameObject's...");
		PluginObject = new GameObject(GUID);
		UnityEngine.Object.DontDestroyOnLoad(PluginObject);
		var ui = PluginObject.AddComponent<SpectateUI>();
		PluginObject.AddComponent<SpectateCam>();
		PluginObject.AddComponent<SpectateConfigUpdater>();
		PluginObject.AddComponent<PouncerTracker>();
		PluginObject.AddComponent<SpectatorCountUI>();
		PluginObject.AddComponent<PeerInfoManager>();
		PluginObject.AddComponent<Wm>();
		ui.ReplicateUI();
	}

	public override bool Unload() {
		ConfigMgr.WriteConfigIfDirty();
		return true;
	}
}
