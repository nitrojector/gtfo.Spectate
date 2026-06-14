using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;

namespace Spectate.Utility;

/// <summary>
/// Utilities for TextMeshPro
/// </summary>
public class TMPUtils {
	/// <summary>
	/// Create a world space TMP object by cloning the in-game one.
	/// </summary>
	public static bool CreateTMP(string name, Transform? parent, [NotNullWhen(true)] out TextMeshPro? tp)
	{
		TextMeshPro? tRef = GuiManager.WatermarkLayer?.m_watermark?.m_fpsText;
		if (parent == null || tRef == null) {
			tp = null;
			return false;
		}

		tp = UnityEngine.Object.Instantiate(tRef, parent, false);
		tp.name = name;
		tp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		tp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		tp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		tp.rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
		tp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
		tp.enableAutoSizing = false;
		tp.overflowMode = TextOverflowModes.Overflow;
		tp.alignment = TextAlignmentOptions.Left;
		tp.fontSize = 16.0f;
		return true;
	}

	/// <summary>
	/// Create a screen space UGUI TMP object.
	/// </summary>
	public static bool CreateTMPUGUI(string name, Transform? parent, [NotNullWhen(true)] out TextMeshProUGUI? tp)
	{
		TextMeshPro? tRef = GuiManager.WatermarkLayer?.m_watermark?.m_fpsText;

		var go = new GameObject();
		go.transform.SetParent(parent, false);
		tp = go.AddComponent<TextMeshProUGUI>();
		tp.name = name;
		tp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		tp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		tp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		tp.rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
		tp.rectTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
		tp.enableAutoSizing = false;
		tp.overflowMode = TextOverflowModes.Overflow;
		tp.alignment = TextAlignmentOptions.Center;
		tp.fontSize = 16.0f;
		if (tRef != null) {
			tp.font = tRef.font;
			tp.fontSharedMaterial = tRef.fontSharedMaterial;
		}

		return true;
	}

}
