using BepInEx;
using BepInEx.Configuration;

namespace Spectate.Config;

internal static class ConfigMgr {
	private static ConfigFile _conf;
	private static bool _configDirty = false;

	private static FileSystemWatcher? _configWatcher;

	private static readonly Dictionary<string, eDevOpts> _devOptsStr2Enum = new() {
		["--any"] = eDevOpts.AllowSpectatingAnytime,
		["--exp"] = eDevOpts.ExperimentalFeatures,
	};

	// DEBUG
	private static ConfigEntryExtended<bool> _debug;
	private static ConfigEntryExtended<string> _devOpts;
	public static bool Debug => _debug.Value;
	private static eDevOpts _devOptsVal = 0;
	public static bool DevEnables(eDevOpts opt) => _devOptsVal.HasFlag(opt);

	// Behavior
	private static ConfigEntryExtended<bool> _switchOnDeath;
	private static ConfigEntryExtended<bool> _defaultFreecamView;
	private static ConfigEntryExtended<bool> _autoTransitionToFollowView;
	private static ConfigEntryExtended<float> _autoTransitionDelay;
	public static bool SwitchOnDeath => _switchOnDeath.Value;
	public static bool DefaultFreecamView => _defaultFreecamView.Value;
	public static bool AutoTransitionToFollowView => _autoTransitionToFollowView.Value;
	public static float AutoTransitionDelay => _autoTransitionDelay.Value;

	// Sensitivity
	private static ConfigEntryExtended<float> _scrollSensitivity;
	private static ConfigEntryExtended<float> _freecamSensitivity;
	private static ConfigEntryExtended<float> _freecamLerpGain;
	public static float ScrollSensitivity => _scrollSensitivity.Value;
	public static float FreecamSensitivity => _freecamSensitivity.Value;
	public static float FreecamLerpGain => _freecamLerpGain.Value;

	// Camera
	private static ConfigEntryExtended<float> _cameraDistance;
	private static ConfigEntryExtended<float> _cameraOrbitVerticalOffset;
	private static ConfigEntryExtended<float> _cameraPitchAngleDeg;
	private static float _cameraDistanceCache = float.NaN;
	private static float _cameraOrbitVerticalOffsetCache = float.NaN;
	private static float _cameraPitchAngleDegCache = float.NaN;

	public static float CameraDistance {
		get => _cameraDistanceCache;
		set {
			_cameraDistanceCache = value;
			_configDirty = true;
		}
	}

	public static float CameraOrbitVerticalOffset {
		get => _cameraOrbitVerticalOffsetCache;
		set {
			_cameraOrbitVerticalOffsetCache = value;
			_configDirty = true;
		}
	}

	public static float CameraPitchAngleDeg {
		get => _cameraPitchAngleDegCache;
		set {
			_cameraPitchAngleDegCache = value;
			_configDirty = true;
		}
	}

	public static void Process() {
		Logger.Info($"debug={Debug}");
		_cameraDistanceCache = _cameraDistance.Value;
		_cameraOrbitVerticalOffsetCache = _cameraOrbitVerticalOffset.Value;
		_cameraPitchAngleDegCache = _cameraPitchAngleDeg.Value;
		_devOptsVal = eDevOpts.None;
		foreach (var optStr in _devOpts.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
			if (_devOptsStr2Enum.TryGetValue(optStr.Trim().ToLower(), out var optEnum)) {
				_devOptsVal |= optEnum;
			}
		}
	}

	static ConfigMgr() {
		string cfgFileName = $"{Plugin.NAME}.cfg";
		string cfgPath = Path.Combine(Paths.ConfigPath, cfgFileName);
		Logger.Info($"cfgPath = {cfgPath}");
		_conf = new ConfigFile(cfgPath, true);

		_configWatcher = new FileSystemWatcher(Paths.ConfigPath, cfgFileName) {
			NotifyFilter = NotifyFilters.LastWrite,
			EnableRaisingEvents = true
		};
		_configWatcher.Changed += async (_, _) => {
			_configWatcher.EnableRaisingEvents = false;
			await Task.Delay(500);
			Logger.Debug("Reloading config file due to external change...");
			_conf.Reload();
			await Task.Delay(250);
			Process();
			await Task.Delay(250);
			_configWatcher.EnableRaisingEvents = true;
		};


		string sectionHeader;
		int section = 1;

		sectionHeader = $"({section++}) Behavior";

		_switchOnDeath = _conf.Bind(
			sectionHeader,
			"Switch On Death",
			false,
			"automatically switch to spectate upon death");

		_defaultFreecamView = _conf.Bind(
			sectionHeader,
			"Default to Free-Look View",
			false,
			"start game sessions with free-look enabled by default");

		_autoTransitionToFollowView = _conf.Bind(
			sectionHeader,
			"Auto Transition To Temporary Follow View",
			true,
			"automatically transition to temporary follow view after idling in free-look");

		_autoTransitionDelay = _conf.Bind(
			sectionHeader,
			"Auto Transition Delay",
			3.0f,
			"delay after no mouse input in seconds before automatically transitioning to temporary follow view (min: 0.2)");
		_autoTransitionDelay.AddRule(ConfigEntryRule.Min, 0.2f);

		sectionHeader = $"({section++}) Sensitivity";

		_scrollSensitivity = _conf.Bind(
			sectionHeader,
			"Scroll Sensitivity",
			SpectateCam.DefaultScrollSensitivity,
			"sensitivity for scroll wheel based adjustments (camera distance, camera pitch, orbit center offset) (min: 0.01)");
		_scrollSensitivity.AddRule(ConfigEntryRule.Min, 0.01f);

		_freecamSensitivity = _conf.Bind(
			sectionHeader,
			"Free-Look Sensitivity",
			SpectateCam.DefaultFreecamSensitivity,
			"sensitivity for free-look mouse movement (min: 0.01)");
		_freecamSensitivity.AddRule(ConfigEntryRule.Min, 0.01f);

		_freecamLerpGain = _conf.Bind(
			sectionHeader,
			"Free-Look Smoothing Rate",
			SpectateCam.DefaultCameraLerpGain,
			"rate at which camera snaps to target: higher is snappier, lower is smoother (min: 1.0)");
		_freecamLerpGain.AddRule(ConfigEntryRule.Min, 1.0f);

		sectionHeader = $"({section++}) Camera Settings <SYNCED>";

		_cameraOrbitVerticalOffset = _conf.Bind(
			sectionHeader,
			"Camera Orbit Vertical Offset",
			SpectateCam.DefaultOrbitCenterVerticalOffset,
			$"vertical offset of the camera orbit center/point from the player's position (min: {SpectateCam.OrbitCenterVerticalOffsetMin:F2}, max: {SpectateCam.OrbitCenterVerticalOffsetMax:F2})");
		_cameraOrbitVerticalOffset.AddRule(ConfigEntryRule.Min, SpectateCam.OrbitCenterVerticalOffsetMin);
		_cameraOrbitVerticalOffset.AddRule(ConfigEntryRule.Max, SpectateCam.OrbitCenterVerticalOffsetMax);

		_cameraDistance = _conf.Bind(
			sectionHeader,
			"Camera Distance",
			SpectateCam.DefaultDistanceFromEye,
			$"distance of the camera from the orbit point (min: {SpectateCam.DistanceMin:F2}, max: {SpectateCam.DistanceMax:F2})");
		_cameraDistance.AddRule(ConfigEntryRule.Min, SpectateCam.DistanceMin);
		_cameraDistance.AddRule(ConfigEntryRule.Max, SpectateCam.DistanceMax);

		_cameraPitchAngleDeg = _conf.Bind(
			sectionHeader,
			"Camera Pitch Angle (deg)",
			SpectateCam.DefaultPitchAngleDeg,
			$"pitch angle of the camera in degrees (min: {SpectateCam.PitchAngleDegMin:F2}, max: {SpectateCam.PitchAngleDegMax:F2})");
		_cameraPitchAngleDeg.AddRule(ConfigEntryRule.Min, SpectateCam.PitchAngleDegMin);
		_cameraPitchAngleDeg.AddRule(ConfigEntryRule.Max, SpectateCam.PitchAngleDegMax);

		sectionHeader = "(Z) Dev";

		_debug = _conf.Bind(
			sectionHeader,
			"Enable Debug Logs",
			false,
			"debug logging");

		_devOpts = _conf.Bind(
			sectionHeader,
			"Dev Options",
			"",
			"if you know you know");
	}

	internal static void WriteConfigIfDirty() {
		if (!_configDirty)
			return;

		_cameraDistance.Value = _cameraDistanceCache;
		_cameraOrbitVerticalOffset.Value = _cameraOrbitVerticalOffsetCache;
		_cameraPitchAngleDeg.Value = _cameraPitchAngleDegCache;

		_conf.Save();
		_configDirty = false;
	}
}
