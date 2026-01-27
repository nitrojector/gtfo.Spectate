using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Spectate;

internal static class ConfigMgr {
	private static ConfigFile _conf;

	private static ConfigEntry<bool> _debug;
	public static bool Debug => _debug.Value;

	public static void Process() {
		Logger.Info($"debug={Debug}");
	}

	static ConfigMgr() {
		string cfgPath = Path.Combine(Paths.ConfigPath, $"{Plugin.NAME}.cfg");
		Logger.Debug($"cfgPath = {cfgPath}");
		_conf = new ConfigFile(cfgPath, true);

		string sectionHeader;
		int section = 1;

		sectionHeader = $"({section++}) InLevelCarry Item Assist";

		sectionHeader = $"(Z) Debug";
		_debug = _conf.Bind(
			sectionHeader,
			"Enable Debug Logs",
			false,
			"debug logs for development purposes");
	}
}
