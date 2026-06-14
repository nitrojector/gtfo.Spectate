using System.Text.Json.Serialization;
using Clonesoft.Json;
using GameData;
using Localization;
using Spectate.Data;
using Spectate.Utility;
using UnityEngine;

namespace Spectate.Localization;

/// <summary>
/// Localization service
/// </summary>
public static class Loc {
	/// <summary>
	/// Language key for fallback language, used when the current language doesn't have a translation for a key.
	/// </summary>
	public const string FallbackLocale = "en";

	/// <summary>
	/// Base id for our localization keys in GTFO's localizer.
	/// </summary>
	public const uint BaseId = 69696969u;

	/// <summary>
	/// Gets the current locale key, as converted from GTFO's current language enum.
	/// </summary>
	public static string CurrentLocale {
		get {
			var cur = Text.TextLocalizationService?.CurrentLanguage;
			return  LocaleUtils.LocaleKeyFromEnum(cur ?? Language.English);
		}
	}

	/// <summary>
	/// Dictionary of all translations, keyed by locale and then by localization key.
	/// </summary>
	private static readonly Dictionary<string /* locale */, Dictionary<string /* key */, string /* text */>> Translations = new();

	/// <summary>
	/// Dictionary mapping localization keys to GTFO localizer key ids.
	/// </summary>
	private static readonly Dictionary<string /* key */, uint /* db id */> DataBlockIds = new();

	static Loc() {
		Load();
	}

	/// <summary>
	/// Gets the corresponding <see cref="TextDataBlock"/> persistent id for the given localization key.
	/// </summary>
	/// <returns>localization id for GTFO localizer</returns>
	public static uint ID(string key) => DataBlockIds.GetValueOrDefault(key, 0u);

	/// <summary>
	/// Gets the localized string for the given key.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public static string T(string key) {
		string loc = CurrentLocale;
		if (!Translations.ContainsKey(loc)) {
			Logger.Debug($"[Loc] no translations found for locale '{loc}', falling back to '{FallbackLocale}'.");
			loc = FallbackLocale;
		}

		if (!Translations.TryGetValue(loc, out var trans)) {
			Logger.Error($"[Loc] no translations available for fallback locale '{FallbackLocale}'!");
			return $"NO_LOCALE[{key}]";
		}

		if (!trans.TryGetValue(key, out var text)) {
			Logger.Warn($"[Loc] no translation found for key '{key}' in locale '{loc}'!");
			return $"UNKNOWN[{loc}:{key}]";
		}

		return text;
	}

	/// <summary>
	/// Tries to add or set the text setter for a given <see cref="Behaviour"/> target,
	/// using the translation for the given key. Does nothing if the target doesn't have an
	/// <see cref="ILocalizedTextSetter"/> component.
	/// </summary>
	/// <param name="target">localized target to modify</param>
	/// <param name="key">locale key</param>
	/// <returns>true if set</returns>
	public static bool TrySet(Component target, string key) {
		ILocalizedTextSetter? targetSetter = target.GetComponent<ILocalizedTextSetter>();
		if (targetSetter == null) {
			return false;
		}

		Text.SetTextSetter(targetSetter, ID(key));
		return true;
	}

	/// <summary>
	/// Tries to update <see cref="target"/> to use translation for given key,
	/// given that it contains a <see cref="TMP_Localizer"/> component.
	/// </summary>
	/// <param name="target">localized target to modify</param>
	/// <param name="key">locale key</param>
	/// <returns>true if successful, false if component not found</returns>
	public static bool TrySetTMP(Component target, string key) {
		TMP_Localizer? tmpLocalizer = target.GetComponentInChildren<TMP_Localizer>();
		if (tmpLocalizer == null) {
			return false;
		}

		tmpLocalizer.m_blockId = ID(key);
		return true;
	}

	/// <summary>
	/// Loads localization data from files.
	/// </summary>
	private static void Load() {
		foreach (var locFile in FileUtils.FilesWithExtensionRecursive(ModData.LocalizationFolder, ".json")) {
			var locale = Path.GetFileNameWithoutExtension(locFile);

			if (!File.Exists(locFile)) {
				Logger.Warn($"[JsonUtils] File not found: '{locFile}'");
				continue;
			}

			var trans = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(locFile));
			if (trans != null) {
				Translations[locale] = trans;
			}
		}
	}

	/// <summary>
	/// Inserts our localizations into GTFO's TextDataBlock.
	/// </summary>
	internal static void InsertToGtfoDB() {
		uint id = BaseId;

		uint GetNextId() {
			id++;
			while (TextDataBlock.HasBlock(id)) {
				id++;
			}

			return id;
		}

		// ==========================================================

		if (!Translations.TryGetValue(FallbackLocale, out var fbTrans)) {
			Logger.Error($"[Loc] no translations found for fallback locale '{FallbackLocale}', cannot insert to TextDataBlock!");
			return;
		}

		var keys = fbTrans.Keys.ToList();
		var locales = Translations.Keys.ToList();

		foreach (var key in keys) {
			uint dbId = GetNextId();

			var textDb = new TextDataBlock {
				name = $"{Plugin.GUID}.{key}",
				internalEnabled = true,
				persistentID = dbId
			};

			foreach (var loc in locales) {
				if (Translations[loc].TryGetValue(key, out var text)) {
					LocaleUtils.SetTranslation(textDb, loc, text);
				}
			}

			TextDataBlock.AddBlock(textDb);
			DataBlockIds[key] = dbId;
		}

	}
}
