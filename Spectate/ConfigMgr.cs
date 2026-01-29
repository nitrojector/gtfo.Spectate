using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Spectate;

// TODO: Implement value clamping
internal static class ConfigMgr {
	private static ConfigFile _conf;
	private static bool _configDirty = false;

	// DEBUG
	private static ConfigEntry<bool> _debug;
	public static bool Debug => _debug.Value;

	// Behavior
	private static ConfigEntry<bool> _switchOnDeath;
	private static ConfigEntry<bool> _defaultFreecamView;
	private static ConfigEntry<bool> _autoTransitionToDefaultView;
	private static ConfigEntry<float> _autoTransitionDelay;
	private static ConfigEntry<float> _scrollSensitivity;
	private static ConfigEntry<float> _freecamSensitivity;
	public static bool SwitchOnDeath => _switchOnDeath.Value;
	public static bool DefaultFreecamView => _defaultFreecamView.Value;
	public static bool AutoTransitionToDefaultView => _autoTransitionToDefaultView.Value;
	public static float AutuoTransitionDelay => _autoTransitionDelay.Value;
	public static float ScrollSensitivity => _scrollSensitivity.Value;
	public static float FreecamSensitivity => _freecamSensitivity.Value;

	// Camera
	private static ConfigEntry<float> _cameraDistance;
	private static ConfigEntry<float> _cameraOrbitVerticalOffset;
	private static ConfigEntry<float> _cameraPitchAngleDeg;
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
	}

	static ConfigMgr() {
		// TODO: periodically save dirty config
		string cfgPath = Path.Combine(Paths.ConfigPath, $"{Plugin.NAME}.cfg");
		Logger.Debug($"cfgPath = {cfgPath}");
		_conf = new ConfigFile(cfgPath, true);

		string sectionHeader;
		int section = 1;

		sectionHeader = $"({section++}) Behavior";

		_switchOnDeath = _conf.Bind(
			sectionHeader,
			"Switch On Death",
			true,
			"automatically switch to spectate upon death");

		_defaultFreecamView = _conf.Bind(
			sectionHeader,
			"Default Freecam View",
			true,
			"start game sessions with free-look enabled by default");

		_autoTransitionToDefaultView = _conf.Bind(
			sectionHeader,
			"Auto Transition To Default View",
			true,
			"automatically transition back to default view after idling in free-look");

		_autoTransitionDelay = _conf.Bind(
			sectionHeader,
			"Auto Transition Delay",
			5.0f,
			"delay in seconds before automatically transitioning back to default view");

		_scrollSensitivity = _conf.Bind(
			sectionHeader,
			"Scroll Sensitivity",
			SpectateCam.DefaultScrollSensitivity,
			"sensitivity for scroll wheel based adjustments (camera distance, camera pitch, orbit center offset)");

		_freecamSensitivity = _conf.Bind(
			sectionHeader,
			"Freecam Sensitivity",
			SpectateCam.DefaultFreecamSensitivity,
			"sensitivity for freecam mouse movement");


		sectionHeader = $"({section++}) Camera Settings {{SYNCED}}";

		_cameraOrbitVerticalOffset = _conf.Bind(
			sectionHeader,
			"Camera Orbit Vertical Offset",
			SpectateCam.DefaultOrbitCenterVerticalOffset,
			"vertical offset of the camera orbit center/point from the player's position");

		_cameraDistance = _conf.Bind(
			sectionHeader,
			"Camera Distance",
			SpectateCam.DefaultDistanceFromEye,
			"distance of the camera from the orbit point");

		_cameraPitchAngleDeg = _conf.Bind(
			sectionHeader,
			"Camera Pitch Angle (deg)",
			SpectateCam.DefaultPitchAngleDeg,
			"pitch angle of the camera in degrees");

		sectionHeader = $"(Z) Debug";

		_debug = _conf.Bind(
			sectionHeader,
			"Enable Debug Logs",
			false,
			"debug logs for development purposes");
	}
}
