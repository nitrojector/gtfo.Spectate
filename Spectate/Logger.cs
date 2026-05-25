using BepInEx.Logging;
using Spectate.Config;

namespace Spectate;

#nullable disable

internal static class Logger {
	private static ManualLogSource _mLogSource;
	public static bool Ready => _mLogSource != null;

	public static void Setup()
	{
		_mLogSource = BepInEx.Logging.Logger.CreateLogSource(Plugin.GUID);
	}

	public static void SetupFromInit(ManualLogSource logSource) => _mLogSource = logSource;

	private static string Format(object data) => data.ToString();

	public static void Debug(object msg)
	{
#if DEBUG
		_mLogSource.LogInfo(" [DEBUG] " + Format(msg));
#else
		if (ConfigMgr.Debug) {
			_mLogSource.LogDebug(Format(msg));
		}
#endif
	}

	public static void Debug(string fmt, params object[] args)
	{
#if DEBUG
		_mLogSource.LogInfo("[DEBUG] " + Format(string.Format(fmt, args)));
#else
		if (ConfigMgr.Debug) {
			_mLogSource.LogDebug(Format(string.Format(fmt, args)));
		}
#endif
 	}

	public static void Info(object msg) => _mLogSource.LogInfo(Format(msg));

	public static void Info(string fmt, params object[] args) =>
		_mLogSource.LogInfo(Format(string.Format(fmt, args)));

	public static void Warn(object msg) => _mLogSource.LogWarning(Format(msg));

	public static void Warn(string fmt, params object[] args) =>
		_mLogSource.LogWarning(Format(string.Format(fmt, args)));

	public static void Error(object msg) => _mLogSource.LogError(Format(msg));

	public static void Error(string fmt, params object[] args) =>
		_mLogSource.LogError(Format(string.Format(fmt, args)));

	public static void Fatal(object msg) => _mLogSource.LogFatal(Format(msg));

	public static void Fatal(string fmt, params object[] args) =>
		_mLogSource.LogFatal(Format(string.Format(fmt, args)));
}
