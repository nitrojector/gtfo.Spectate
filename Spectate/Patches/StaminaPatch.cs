using System.Text.RegularExpressions;
using HarmonyLib;
using Player;
using PlayerSync.Sync.Stamina;
using TMPro;
using UnityEngine;

namespace Spectate.Patches;

/// <summary>
/// Patches for showing spectating player stamina.
/// </summary>
[HarmonyPatch]
public class StaminaPatch {
	// NOTE/TODO: this is pretty jank... also does not work with PreciseUI to show percentage
	//  perhaps there is a better way...

	private static readonly Regex BpmSpriteRegex = new Regex(
		"<sprite name=\"heartbeat\" color=#[A-Z0-9]{8}>",
		RegexOptions.Compiled
	);

	private static float _bpm = 0f;

	// NOTE: cannot patch this, crashes the game immediately.
	// [HarmonyPatch(
	// 	typeof(PlayerStamina),
	// 	"get_Stamina"
	// )]
	// [HarmonyPrefix]
	// private static bool Prefix__PlayerStamina_get_Stamina(ref float __result) {
	// 	if ((SpectateCam.Instance?.TargetReady ?? false) &&
	// 	    SpectateCam.Instance.Active) {
	// 		var player = SpectateCam.Instance.Target!.SAgent;
	// 		if (player.IsLocal) return true;
	//
	// 		if (player.IsBot) {
	// 			__result = 1.0f;
	// 			return false;
	// 		}
	//
	// 		if (PlayerSyncPeerInfoMgr.Supported(player) &&
	// 		     StaminaSync.GetStaminaInfo(player, out var info)) {
	// 			__result = info.Stamina;
	// 			return false;
	// 		}
	// 	}
	//
	// 	return true;
	// }

	[HarmonyPatch(
		typeof(PUI_LocalPlayerStatus),
		nameof(PUI_LocalPlayerStatus.UpdateStamina)
	)]
	[HarmonyPrefix]
	private static void Prefix__PUI_LPS_UpdateStamina(ref float stamina) {
		if ((SpectateCam.Instance?.TargetReady ?? false) &&
		    SpectateCam.Instance.Active) {
			var player = SpectateCam.Instance.Target!.SAgent;
			if (player.IsLocal) return;

			if (player.IsBot) {
				stamina = 1.0f;
				return;
			}

			if (PlayerSync.Network.Impl.PeerInfoManager.Supported(player) &&
			    StaminaSync.GetStaminaInfo(player, out var info)) {
				stamina = info.Stamina;
			}
		}
	}

	[HarmonyPatch(
		typeof(PUI_LocalPlayerStatus),
		nameof(PUI_LocalPlayerStatus.UpdateBPM)
	)]
	[HarmonyPrefix]
	private static void Prefix__PUI_LPS_UpdateBPM(PUI_LocalPlayerStatus __instance, float stamina) {
		if (!(SpectateCam.Instance?.Active ?? false)) return;

		_bpm = Mathf.Lerp(__instance.m_BPMMax, __instance.m_BPMMin, stamina);

		__instance.m_currentBPM = _bpm;
		__instance.m_BPMLastTextUpdateTime = 0f;
	}

	[HarmonyPatch(
		typeof(PUI_LocalPlayerStatus),
		nameof(PUI_LocalPlayerStatus.UpdateBPM)
	)]
	[HarmonyPostfix]
	private static void Postfix__PUI_LPS_UpdateBPM(PUI_LocalPlayerStatus __instance, float stamina) {
		if (!(SpectateCam.Instance?.Active ?? false)) return;

		SetBpmText(__instance.m_pulseText, _bpm);
	}

	public static void RevertStaminaBpmDisplay(PUI_LocalPlayerStatus? lps) {
		if (lps == null) return;

		float stam = PlayerManager.GetLocalPlayerAgent()?.Stamina.Stamina ?? 1.0f;

		var bpm = Mathf.Lerp(lps.m_BPMMax, lps.m_BPMMin, stam);

		lps.m_currentBPM = bpm;
		lps.m_BPMLastTextUpdateTime = 0f;
		SetBpmText(lps.m_pulseText, bpm);
	}

	private static void SetBpmText(TMP_Text pulseText, float bpm)
	{
		if (pulseText == null) return;
		string existing = pulseText.text;
		Match match = BpmSpriteRegex.Match(existing);

		string sprite = match.Success ? match.Value : "";
		string number = bpm.ToString("N0");

		pulseText.SetText($"PULSE:{sprite}{number}");
	}
}
