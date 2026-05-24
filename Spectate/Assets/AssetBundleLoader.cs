using BepInEx;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace Spectate.Assets;

public static class AssetBundleLoader {
	private static readonly Dictionary<string, AssetBundle> Cache = new();

	public static AssetBundle? Load(string bundlePath) {
		if (Cache.TryGetValue(bundlePath, out AssetBundle? cached))
			return cached;

		string realPath = Path.Combine(Paths.BepInExRootPath, "Assets", Plugin.NAME, bundlePath);

		Logger.Debug($"Loading bundle at '{bundlePath}' ==resolved=> '{realPath}'");

		AssetBundle bundle = AssetBundle.LoadFromFile(realPath);
		if (bundle == null) {
			Logger.Error($"Failed to load bundle at {bundlePath}");
			return null;
		}

		Cache[bundlePath] = bundle;
		return bundle;
	}

	public static T? LoadAsset<T>(string bundlePath, string assetPath) where T : Il2CppObjectBase {
		return Load(bundlePath)?.LoadAsset(assetPath)?.TryCast<T>();
	}

	public static void Unload(string bundlePath, bool unloadAllObjects = false) {
		if (!Cache.TryGetValue(bundlePath, out AssetBundle? bundle))
			return;

		bundle.Unload(unloadAllObjects);
		Cache.Remove(bundlePath);
	}

	public static void UnloadAll(bool unloadAllObjects = false) {
		foreach (AssetBundle bundle in Cache.Values)
			bundle.Unload(unloadAllObjects);
		Cache.Clear();
	}
}
