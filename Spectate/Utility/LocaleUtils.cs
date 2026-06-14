using GameData;
using Localization;
using Player;

namespace Spectate.Utility;

public class LocaleUtils {
	public static readonly Dictionary<string, string> LocaleKeyToDisplayName = new() {
		{ "en", "English" },
		{ "fr", "Français" },
		{ "de", "Deutsch" },
		{ "es", "Español" },
		{ "it", "Italiano" },
		{ "ja", "日本語" },
		{ "ko", "한국어" },
		{ "pt-BR", "Português (Brasil)" },
		{ "ru", "Русский" },
		{ "pl", "Polski" },
		{ "zh-Hans", "简体中文" },
		{ "zh-Hant", "繁體中文" },
	};

	/// <summary>
	/// Converts a <see cref="Language"/> enum value to its corresponding locale key string.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">if <see cref="key"/> is unknown</exception>
	public static string LocaleKeyFromEnum(Language key) {
		return key switch {
			Language.English => "en",
			Language.French => "fr",
			Language.German => "de",
			Language.Spanish => "es",
			Language.Italian => "it",
			Language.Japanese => "ja",
			Language.Korean => "ko",
			Language.Portuguese_Brazil => "pt-BR",
			Language.Russian => "ru",
			Language.Polish => "pl",
			Language.Chinese_Simplified => "zh-Hans",
			Language.Chinese_Traditional => "zh-Hant",
			_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
		};
	}

	/// <summary>
	/// Sets the translation for a given locale in a <see cref="TextDataBlock"/>.
	/// Does nothing if <see cref="locale"/> is not a supported locale key.
	/// </summary>
	/// <param name="db">datablock to modify</param>
	/// <param name="locale">locale to set</param>
	/// <param name="text">text to set</param>
	public static void SetTranslation(TextDataBlock db, string locale, string text) {
		switch (locale) {
			case "en":
				db.English = text;
				break;
			case "fr":
				db.French = text;
				break;
			case "de":
				db.German = text;
				break;
			case "es":
				db.Spanish = text;
				break;
			case "it":
				db.Italian = text;
				break;
			case "ja":
				db.Japanese = text;
				break;
			case "ko":
				db.Korean = text;
				break;
			case "pt-BR":
				db.Portuguese_Brazil = text;
				break;
			case "ru":
				db.Russian = text;
				break;
			case "pl":
				db.Polish = text;
				break;
			case "zh-Hans":
				db.Chinese_Simplified = text;
				break;
			case "zh-Hant":
				db.Chinese_Traditional = text;
				break;
		}
	}

	/// <summary>
	/// Returns the localization datablock id for the gear select text corresponding to the given <see cref="InventorySlot"/>.
	/// Only works for Standard, Special, Class, and Melee gear slots. Returns 0 for unsupported slots.
	/// </summary>
	/// <param name="slot">slot to convert</param>
	/// <returns><see cref="TextDataBlock"/> Persistent ID</returns>
	public static uint GetSlotGearSelectTextDbId(InventorySlot slot) {
		switch (slot) {
		case InventorySlot.GearStandard:
			return 487u;

		case InventorySlot.GearSpecial:
			return 488u;

		case InventorySlot.GearClass:
			return 489u;

		case InventorySlot.GearMelee:
			return 490u;

		default:
			return 0u;
		}
	}

	/// <summary>
	/// Returns the localized gear select text for the given <see cref="InventorySlot"/>.
	/// </summary>
	/// <param name="slot">slot to get</param>
	/// <returns>text, or placeholder for 0 for unknown slots</returns>
	public static string GetSlotGearSelectLocalizedText(InventorySlot slot) {
		return Text.Get(GetSlotGearSelectTextDbId(slot));
	}
}
