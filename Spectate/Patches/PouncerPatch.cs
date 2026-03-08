using Enemies;
using HarmonyLib;
using UnityEngine;

namespace Spectate.Patches;

[HarmonyPatch]
public class PouncerPatch : MonoBehaviour {
	[HarmonyPatch(
		typeof(PouncerBehaviour),
		nameof(PouncerBehaviour.Setup)
	)]
	[HarmonyPostfix]
	private static void PouncerBehaviour_Setup__Postfix(PouncerBehaviour __instance) {
		if (__instance == null) {
			return;
		}

		PouncerTracker.Instance?.RegisterPouncer(__instance);
	}
}
