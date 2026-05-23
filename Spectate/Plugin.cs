using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Globals;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Spectate.Config;
using Spectate.Interop;
using Spectate.Network;
using Spectate.Network.Impl;
using Spectate.Patches;
using Spectate.Patches.Compat;
using Spectate.UI;

namespace Spectate;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(GUID_PlayerSync, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(GUID_EOSExtEMP, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(GUID_EEC, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin {
	public const string NAME = "Spectate";
	public const string GUID = "io.takina.gtfo." + NAME;
	public const string VERSION = "1.6.0";

	public const string GUID_PlayerSync = "io.takina.gtfo.PlayerSync";
	public const string GUID_EOSExtEMP = "Inas.EOSExt.EMP";
	public const string GUID_EEC = "GTFO.EECustomization";

	public static readonly PlugVersion PlugVersion = new(VERSION);

	public event Action? OnManagersSetup;
	public static GameObject? PluginObject;


	public override void Load() {
		Logger.Setup();
		Logger.Info($"{NAME} [{GUID} @ {VERSION}]");

		Harmony h = new Harmony(GUID);
		ConfigMgr.Process();

		RegisterIl2CppTypes();

		OnManagersSetup += Initialize;
		Global.add_OnAllManagersSetup(OnManagersSetup);

		Logger.Info("Patching...");

		ApplyPatches(h);

		CompatPatcher.PatchAll(h);

		// CompatPatcher throws exception when patching EEC, great
		if (IL2CPPChainloader.Instance.Plugins.ContainsKey(GUID_EEC)){
			ApplyPatch<Patches.Compat.Targets.ECC_Compat>(h);
		}

		Logger.Info("Finished Patching");
	}

	private static void ApplyPatches(Harmony h)
	{
		ApplyPatch<EventPatch>(h);
		ApplyPatch<UIPatch>(h);
		ApplyPatch<CameraPatch>(h);
		ApplyPatch<AnimationPatch>(h);
		ApplyPatch<PouncerPatch>(h);
		ApplyPatch<Net>(h);
	}

	private static void ApplyPatch<T>(Harmony h) {
		h.PatchAll(typeof(T));
	}

	internal static void RegisterIl2CppTypes()
	{
		foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (type.GetCustomAttribute<RegisterIl2CppAttribute>() is not null)
				ClassInjector.RegisterTypeInIl2Cpp(type);
		}
	}

	private void Initialize() {
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
