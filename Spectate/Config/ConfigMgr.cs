using BepInEx;
using BepInEx.Configuration;
using Spectate.UI;
using UnityEngine;

namespace Spectate.Config;

internal static class ConfigMgr {
	private static readonly ConfigFile Conf;
	private static bool _configDirty = false;

	private static readonly FileSystemWatcher? ConfigWatcher;

	private static readonly Dictionary<string, eDevOpts> DevOptsStr2Enum = new() {
		["--any"] = eDevOpts.AllowSpectatingAnytime,
		["--exp"] = eDevOpts.ExperimentalFeatures,
	};

	// DEBUG
	private static readonly ConfigEntryExtended<bool> DebugConf;
	private static readonly ConfigEntryExtended<string> DevOptsConf;
	public static bool Debug => DebugConf.Value;
	private static eDevOpts _devOptsVal = 0;
	public static bool DevEnables(eDevOpts opt) => _devOptsVal.HasFlag(opt);

	// Behavior
	private static readonly ConfigEntryExtended<bool> SwitchOnDeathConf;
	private static readonly ConfigEntryExtended<bool> DefaultFreecamViewConf;
	private static readonly ConfigEntryExtended<bool> AutoTransitionToFollowViewConf;
	private static readonly ConfigEntryExtended<bool> NoPosLerpOnSwitchTargetConf;
	private static readonly ConfigEntryExtended<bool> ShowPlayerBodyWhenSpectatingConf;
	private static readonly ConfigEntryExtended<float> AutoTransitionDelayConf;
	private static bool _autoTransitionToFollowViewCache = false;
	public static bool SwitchOnDeath => SwitchOnDeathConf.Value;
	public static bool DefaultFreecamView => DefaultFreecamViewConf.Value;

	public static bool AutoTransitionToFollowView {
		get => _autoTransitionToFollowViewCache;
		set {
			_autoTransitionToFollowViewCache = value;
			_configDirty = true;
		}
	}

	public static bool NoPosLerpOnSwitchTarget => NoPosLerpOnSwitchTargetConf.Value;
	public static bool ShowPlayerBodyWhenSpectating => ShowPlayerBodyWhenSpectatingConf.Value;
	public static float AutoTransitionDelay => AutoTransitionDelayConf.Value;

	// Sensitivity
	private static readonly ConfigEntryExtended<float> ScrollSensitivityConf;
	private static readonly ConfigEntryExtended<float> FreecamSensitivityConf;
	private static readonly ConfigEntryExtended<float> FreecamLerpGainConf;
	private static readonly ConfigEntryExtended<float> CameraXZLerpGainConf;
	private static readonly ConfigEntryExtended<float> CameraYLerpGainConf;
	public static float ScrollSensitivity => ScrollSensitivityConf.Value;
	public static float FreecamSensitivity => FreecamSensitivityConf.Value;
	public static float FreecamLerpGain => FreecamLerpGainConf.Value;
	public static float CameraXZLerpGain => CameraXZLerpGainConf.Value;
	public static float CameraYLerpGain => CameraYLerpGainConf.Value;

	// Camera
	private static readonly ConfigEntryExtended<float> CameraDistanceConf;
	private static readonly ConfigEntryExtended<float> CameraOrbitVerticalOffsetConf;
	private static readonly ConfigEntryExtended<float> CameraPitchAngleDegConf;
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

	// Keybinds
	private static readonly Dictionary<SpectateInputAction, ConfigEntryExtended<KeyCode>> KeybindConfs = new();

	private static readonly Dictionary<SpectateInputAction, KeybindSetting> KeybindSettingConfs = new() {
		[SpectateInputAction.ToggleSpectate] = new KeybindSetting {
			Name = "Toggle Spectate",
			Description = "toggle spectate active/inactive",
			DefaultKey = KeyCode.V,
		},
		[SpectateInputAction.ToggleFreecam] = new KeybindSetting {
			Name = "Toggle Free-Look View",
			Description = "toggle free-look spectate view on/off",
			DefaultKey = KeyCode.F,
		},
		[SpectateInputAction.ToggleAutoFollow] = new KeybindSetting {
			Name = "Toggle Auto Follow View",
			Description = "toggle automatic transition to temporary follow view on/off",
			DefaultKey = KeyCode.T,
		},
		[SpectateInputAction.ToggleMenu] = new KeybindSetting {
			Name = "Toggle Spectate Menu",
			Description = "toggle spectate action menu on/off",
			DefaultKey = KeyCode.Backslash,
		},
	};

	public static KeyCode GetKeybind(SpectateInputAction action) {
		if (KeybindConfs.TryGetValue(action, out var conf)) {
			return conf.Value;
		}

		Logger.Error($"GetKeybind: no config found for action {action}");
		return KeyCode.None;
	}

	public static void Process() {
		Logger.Info($"debug={Debug}");
		_cameraDistanceCache = CameraDistanceConf.Value;
		_cameraOrbitVerticalOffsetCache = CameraOrbitVerticalOffsetConf.Value;
		_cameraPitchAngleDegCache = CameraPitchAngleDegConf.Value;
		_autoTransitionToFollowViewCache = AutoTransitionToFollowViewConf.Value;
		_devOptsVal = eDevOpts.None;
		foreach (var optStr in DevOptsConf.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
			if (DevOptsStr2Enum.TryGetValue(optStr.Trim().ToLower(), out var optEnum)) {
				_devOptsVal |= optEnum;
			}
		}

		// Notify UI of config changes, since keybinds and some settings affect it
		SpectateUI.Instance?.MarkUIDirty();
	}

	static ConfigMgr() {
		string cfgFileName = $"{Plugin.NAME}.cfg";
		string cfgPath = Path.Combine(Paths.ConfigPath, cfgFileName);
		Logger.Info($"cfgPath = {cfgPath}");
		Conf = new ConfigFile(cfgPath, true);

		ConfigWatcher = new FileSystemWatcher(Paths.ConfigPath, cfgFileName) {
			NotifyFilter = NotifyFilters.LastWrite,
			EnableRaisingEvents = true
		};
		ConfigWatcher.Changed += async (_, _) => {
			ConfigWatcher.EnableRaisingEvents = false;
			await Task.Delay(500);
			Logger.Debug("Reloading config file due to external change...");
			Conf.Reload();
			await Task.Delay(250);
			Process();
			await Task.Delay(250);
			ConfigWatcher.EnableRaisingEvents = true;
		};


		string sectionHeader;
		int section = 1;

		sectionHeader = $"({section++}) Behavior";

		SwitchOnDeathConf = Conf.Bind(
			sectionHeader,
			"Switch on Death",
			false,
			"automatically switch to spectate upon death");

		DefaultFreecamViewConf = Conf.Bind(
			sectionHeader,
			"Default to Free-Look View",
			true,
			"start game sessions with free-look enabled by default");

		AutoTransitionToFollowViewConf = Conf.Bind(
			sectionHeader,
			"Auto Transition to Temporary Follow View",
			true,
			"automatically transition to temporary follow view after idling in free-look");

		NoPosLerpOnSwitchTargetConf = Conf.Bind(
			sectionHeader,
			"No Position Smoothing on Target Switch",
			false,
			"disable camera position smoothing when switching spectate targets in free-look");

		ShowPlayerBodyWhenSpectatingConf = Conf.Bind(
			sectionHeader,
			"Show Local Player Body When Spectating",
			false,
			"render the local player's body when spectating in third-person views (note: the player body has no head or arms)");

		AutoTransitionDelayConf = Conf.Bind(
			sectionHeader,
			"Auto Transition Delay",
			3.0f,
			"delay after no mouse input in seconds before automatically transitioning to temporary follow view (min: 0.2)");
		AutoTransitionDelayConf.AddRule(ConfigEntryRule.Min, 0.2f);

		sectionHeader = $"({section++}) Sensitivity";

		ScrollSensitivityConf = Conf.Bind(
			sectionHeader,
			"Scroll Sensitivity",
			SpectateCam.DefaultScrollSensitivity,
			"sensitivity for scroll wheel based adjustments (camera distance, camera pitch, orbit center offset) (min: 0.01)");
		ScrollSensitivityConf.AddRule(ConfigEntryRule.Min, 0.01f);

		FreecamSensitivityConf = Conf.Bind(
			sectionHeader,
			"Free-Look Sensitivity",
			SpectateCam.DefaultFreecamSensitivity,
			"sensitivity for free-look mouse movement (min: 0.01)");
		FreecamSensitivityConf.AddRule(ConfigEntryRule.Min, 0.01f);

		FreecamLerpGainConf = Conf.Bind(
			sectionHeader,
			"Free-Look Smoothing Rate",
			SpectateCam.DefaultCameraLerpGain,
			"rate at which camera snaps to target: higher is snappier, lower is smoother (min: 1.0)");
		FreecamLerpGainConf.AddRule(ConfigEntryRule.Min, 1.0f);

		CameraXZLerpGainConf = Conf.Bind(
			sectionHeader,
			"Camera XZ-Position Smoothing Rate",
			SpectateCam.DefaultCameraXZPositionLerpGain,
			"rate at which camera horizontal position snaps to target: higher is snappier, lower is smoother (min: 7.0)");
		CameraXZLerpGainConf.AddRule(ConfigEntryRule.Min, 7.0f);

		CameraYLerpGainConf = Conf.Bind(
			sectionHeader,
			"Camera Y-Position Smoothing Rate",
			SpectateCam.DefaultCameraYPositionLerpGain,
			"rate at which camera vertical position snaps to target: higher is snappier, " +
			"lower is smoother (min: 7.0)");
		CameraYLerpGainConf.AddRule(ConfigEntryRule.Min, 7.0f);

		sectionHeader = $"({section++}) Camera Settings <SYNCED>";

		CameraOrbitVerticalOffsetConf = Conf.Bind(
			sectionHeader,
			"Camera Orbit Vertical Offset",
			SpectateCam.DefaultOrbitCenterVerticalOffset,
			$"vertical offset of the camera orbit center/point from the player's position " +
			$"(min: {SpectateCam.OrbitCenterVerticalOffsetMin:F2}, max: {SpectateCam.OrbitCenterVerticalOffsetMax:F2})");
		CameraOrbitVerticalOffsetConf
			.AddRule(ConfigEntryRule.Min, SpectateCam.OrbitCenterVerticalOffsetMin);
		CameraOrbitVerticalOffsetConf
			.AddRule(ConfigEntryRule.Max, SpectateCam.OrbitCenterVerticalOffsetMax);

		CameraDistanceConf = Conf.Bind(
			sectionHeader,
			"Camera Distance",
			SpectateCam.DefaultDistanceFromEye,
			$"distance of the camera from the orbit point " +
			$"(min: {SpectateCam.DistanceMin:F2}, max: {SpectateCam.DistanceMax:F2})");
		CameraDistanceConf.AddRule(ConfigEntryRule.Min, SpectateCam.DistanceMin);
		CameraDistanceConf.AddRule(ConfigEntryRule.Max, SpectateCam.DistanceMax);

		CameraPitchAngleDegConf = Conf.Bind(
			sectionHeader,
			"Camera Pitch Angle (deg)",
			SpectateCam.DefaultPitchAngleDeg,
			$"pitch angle of the camera in degrees " +
			$"(min: {SpectateCam.PitchAngleDegMin:F2}, max: {SpectateCam.PitchAngleDegMax:F2})");
		CameraPitchAngleDegConf.AddRule(ConfigEntryRule.Min, SpectateCam.PitchAngleDegMin);
		CameraPitchAngleDegConf.AddRule(ConfigEntryRule.Max, SpectateCam.PitchAngleDegMax);

		sectionHeader = $"({section++}) Keybinds";

		foreach (var (action, setting) in KeybindSettingConfs) {
			KeybindConfs[action] = Conf.Bind(
				sectionHeader,
				setting.Name,
				setting.DefaultKey,
				setting.Description);
		}

		sectionHeader = "(Z) Dev";

		DebugConf = Conf.Bind(
			sectionHeader,
			"Enable Debug Logs",
			false,
			"debug logging");

		DevOptsConf = Conf.Bind(
			sectionHeader,
			"Dev Options",
			"",
			"if you know you know");
	}

	internal static void WriteConfigIfDirty() {
		if (!_configDirty)
			return;

		CameraDistanceConf.Value = _cameraDistanceCache;
		CameraOrbitVerticalOffsetConf.Value = _cameraOrbitVerticalOffsetCache;
		CameraPitchAngleDegConf.Value = _cameraPitchAngleDegCache;
		AutoTransitionToFollowViewConf.Value = _autoTransitionToFollowViewCache;

		Conf.Save();
		_configDirty = false;
	}
}
