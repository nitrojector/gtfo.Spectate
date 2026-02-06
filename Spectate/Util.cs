using UnityEngine;

namespace Spectate;

public static class Util {
	public const float GOOD_ENOUGH_DEG_EPS = 0.2f;

	public static bool GoodEnoughDeg(float a, float b) {
		return NearlyEqual(a, b, GOOD_ENOUGH_DEG_EPS);
	}

	public static bool NearlyEqual(float a, float b, float eps = 0.0001f) {
		return Mathf.Abs(a - b) < eps;
	}

	public static void SetTargetActiveIfDiff(GameObject? obj, bool active) {
		if (obj == null) return;
		if (obj.activeSelf != active) {
			obj.SetActive(active);
		}
	}

	public static void SetTargetActiveIfDiff(Behaviour? beh, bool active) {
		if (beh == null) return;
		if (beh.enabled != active) {
			beh.enabled = active;
		}
	}

	public static void FindAndSortGfxParts(GameObject root, out GameObject[] gfxHead, out GameObject[] gfxArms,
		out GameObject[] gfxTorso, out GameObject[] gfxLegs) {
		PlayerGfxPart[] componentsInChildren = root.GetComponentsInChildren<PlayerGfxPart>(true);
		List<GameObject> list = new List<GameObject>();
		List<GameObject> list2 = new List<GameObject>();
		List<GameObject> list3 = new List<GameObject>();
		List<GameObject> list4 = new List<GameObject>();
		foreach (PlayerGfxPart playerGfxPart in componentsInChildren) {
			switch (playerGfxPart.m_type) {
				case PlayerGFXType.Head:
					list.Add(playerGfxPart.gameObject);
					break;
				case PlayerGFXType.Arms:
				case PlayerGFXType.Gloves:
					list2.Add(playerGfxPart.gameObject);
					break;
				case PlayerGFXType.Torso:
				case PlayerGFXType.Backpack:
					list3.Add(playerGfxPart.gameObject);
					break;
				case PlayerGFXType.Legs:
					list4.Add(playerGfxPart.gameObject);
					break;
			}
		}

		gfxHead = list.ToArray();
		gfxArms = list2.ToArray();
		gfxTorso = list3.ToArray();
		gfxLegs = list4.ToArray();
	}
}
