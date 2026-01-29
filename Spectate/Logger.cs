using BepInEx.Logging;
using TenCC.Utils;

namespace Spectate;

#nullable disable

internal static class Logger {
	private static ManualLogSource _mLogSource;
	public static bool Ready => _mLogSource != null;

	public static void Setup() {
		_mLogSource = BepInEx.Logging.Logger.CreateLogSource(Plugin.GUID);
	}

	public static void SetupFromInit(ManualLogSource logSource) => _mLogSource = logSource;

	private static string Format(object data) => data.ToString();

	public static void Debug(object msg) {
		if (ConfigMgr.Debug) {
			_mLogSource.LogInfo("[DEBUG] " + Format(msg));
		} else {
			_mLogSource.LogDebug(Format(msg));
		}
	}

	public static void Info(object msg) => _mLogSource.LogInfo(Format(msg));

	public static void Warn(object msg) => _mLogSource.LogWarning(Format(msg));

	public static void Error(object msg) => _mLogSource.LogError(Format(msg));

	public static void Fatal(object msg) => _mLogSource.LogFatal(Format(msg));
}
