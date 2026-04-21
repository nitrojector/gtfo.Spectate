using UnityEngine;
using Array = Il2CppSystem.Array;
using Il2CppArrays = Il2CppInterop.Runtime.InteropTypes.Arrays;
using GlsParticleData = GlassLiquidSystem.particleData;

namespace Spectate;

public static class Util {
	public const float GOOD_ENOUGH_DEG_EPS = 0.2f;
	public const int PARTICLE_BUFFER_SIZE = 53248 / 0x34;
	public static readonly Array EmptyParticleBuffer;

	static Util() {
		Il2CppArrays.Il2CppStructArray<GlsParticleData> il2Array = new GlsParticleData[PARTICLE_BUFFER_SIZE];
		EmptyParticleBuffer = il2Array.Cast<Array>();
	}

	/// <summary>
	/// Whether two angles/numbers are close enough, according to GOOD_ENOUGH_DEG_EPS.
	/// </summary>
	/// <param name="a">value a</param>
	/// <param name="b">value b</param>
	/// <returns>true if the angle/numbers are close enough</returns>
	public static bool GoodEnoughDeg(float a, float b) {
		return NearlyEqual(a, b, GOOD_ENOUGH_DEG_EPS);
	}

	/// <summary>
	/// Whether two floats are close enough, according to a given epsilon.
	/// </summary>
	/// <param name="a">value a</param>
	/// <param name="b">value b</param>
	/// <param name="eps">epsilon (difference), default is 0.0001f</param>
	/// <returns>true if the numbers are close enough</returns>
	public static bool NearlyEqual(float a, float b, float eps = 0.0001f) {
		return Mathf.Abs(a - b) < eps;
	}

	/// <summary>
	/// Sets the target active if its current active state is different from the desired state for a given GameObject.
	/// </summary>
	/// <param name="obj">GameObject to set the state for</param>
	/// <param name="active">desired active state</param>
	/// <returns>false if object is null, otherwise whether the object is now active</returns>
	public static bool SetTargetActiveIfDiff(GameObject? obj, bool active) {
		if (obj == null) return false;
		if (obj.activeSelf != active) {
			obj.SetActive(active);
		}

		return active;
	}

	/// <summary>
	/// Sets the target active if its current active state is different from the desired state on a given Behaviour.
	/// </summary>
	/// <param name="beh">Behaviour to set the state for</param>
	/// <param name="active">desired active state</param>
	/// <returns>false if object is null, otherwise whether the object is now active</returns>
	public static void SetTargetActiveIfDiff(Behaviour? beh, bool active) {
		if (beh == null) return;
		if (beh.enabled != active) {
			beh.enabled = active;
		}
	}

	/// <summary>
	/// Ported from R6 mono. Finds all PlayerGfxParts and categorizes them in output.
	/// </summary>
	/// <param name="root">the room game object to start searching from</param>
	/// <param name="gfxHead">head parts</param>
	/// <param name="gfxArms">arms parts</param>
	/// <param name="gfxTorso">torso parts</param>
	/// <param name="gfxLegs">legs parts</param>
	/// <param name="includeInactive">whether to include inactive game objects in the search</param>
	public static void FindAndSortGfxParts(GameObject root, out GameObject[] gfxHead, out GameObject[] gfxArms,
		out GameObject[] gfxTorso, out GameObject[] gfxLegs, bool includeInactive = true) {
		PlayerGfxPart[] componentsInChildren = root.GetComponentsInChildren<PlayerGfxPart>(includeInactive);
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

	/// <summary>
	/// Clears visor liquids for <see cref="GlassLiquidSystem"/>.
	/// </summary>
	public static void ClearGlassLiquid() {
		if (!ScreenLiquidManager.hasSystem) return;
		var gls = ScreenLiquidManager.currentSystem;

		ScreenLiquidManager.Clear();

		gls.cbParticles.SetData(EmptyParticleBuffer);
		gls.particleWrap = 0;

		RenderTexture prev = RenderTexture.active;
		foreach (var rt in new[] { gls.rtParams, gls.rtColor, gls.rtParams_db, gls.rtColor_db })
		{
			RenderTexture.active = rt;
			GL.Clear(false, true, Color.clear);
		}
		RenderTexture.active = prev;
	}
}
