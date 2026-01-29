using UnityEngine;

namespace Spectate;

public static class Util {
	public const float GOOD_ENOUGH_EPS = 0.004f;

	public static bool GoodEnough(float a, float b) {
		return NearlyEqual(a, b, GOOD_ENOUGH_EPS);
	}

	public static bool NearlyEqual(float a, float b, float eps = 0.0001f) {
		return Mathf.Abs(a - b) < eps;
	}

	public static void SetObjActiveIfChanged(GameObject? obj, bool active) {
		if (obj == null) return;
		if (obj.activeSelf != active) {
			obj.SetActive(active);
		}
	}

	public static void SetObjActiveIfChanged(Component? comp, bool active) {
		if (comp == null) return;
		if (comp.gameObject.activeSelf != active) {
			comp.gameObject.SetActive(active);
		}
	}
}
