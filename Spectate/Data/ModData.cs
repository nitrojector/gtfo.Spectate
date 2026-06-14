using BepInEx.Unity.IL2CPP;

namespace Spectate.Data;

public static class ModData {
	public static readonly string PluginFolder;
	public static readonly string ResourcesFolder;
	public static readonly string LocalizationFolder;

	static ModData() {
		PluginFolder = Path.GetDirectoryName(IL2CPPChainloader.Instance.Plugins[Plugin.GUID].Location)!;
		ResourcesFolder = Path.Combine(PluginFolder, "Resources");
		LocalizationFolder = Path.Combine(ResourcesFolder, "Localization");
	}
}
