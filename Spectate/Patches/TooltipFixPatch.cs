using CellMenu;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Spectate.Patches;

/// <summary>
/// This patch fixes a bug where <see cref="CM_Tooltip"/>,
/// after each call to <see cref="CM_Tooltip.UpdateTooltipSizes"/>
/// will grow in height indefinitely.
/// </summary>
[HarmonyPatch]
public class TooltipFixPatch {
	[HarmonyPatch(
		typeof(CM_Tooltip),
		nameof(CM_Tooltip.UpdateTooltipSizes)
	)]
	[HarmonyPrefix]
	private static bool UpdateTooltipSizes_Prefix(CM_Tooltip __instance) {
		var headerText = __instance.m_headerText;
		var text = __instance.m_text;
		var background = __instance.m_background;
		var outline = __instance.m_outline;

		text.rectTransform.SetLeft(__instance.PaddingLeft);
		text.rectTransform.SetRight(__instance.PaddingRight);
		text.rectTransform.SetBottom(__instance.PaddingBot);

		bool noHeader = string.IsNullOrWhiteSpace(headerText.text);
		if (!noHeader) {
			headerText.rectTransform.SetLeft(__instance.PaddingLeft);
			headerText.rectTransform.SetRight(__instance.PaddingRight);
			headerText.rectTransform.SetTop(__instance.PaddingTop);
		}

		text.ForceMeshUpdate();
		headerText.ForceMeshUpdate();

		float textPreferredHeight = text.preferredHeight;
		float headerPreferredHeight = headerText.preferredHeight;
		float headerPreferredWidth = headerText.preferredWidth;
		float textPreferredWidth = text.preferredWidth;

		if (noHeader) {
			text.rectTransform.SetTop(__instance.PaddingTop);
		} else {
			headerText.rectTransform.SetBottom(
				textPreferredHeight + __instance.PaddingBot + __instance.HeaderAndTextSpacing);
			text.rectTransform.SetTop(
				headerPreferredHeight + __instance.PaddingTop + __instance.HeaderAndTextSpacing);
		}

		float maxContentWidth = Mathf.Max(textPreferredWidth, headerPreferredWidth);
		if (__instance.TrimWidth && maxContentWidth < __instance.MaxWidth) {
			float trimAmount = __instance.MaxWidth - maxContentWidth - __instance.PaddingLeft - __instance.PaddingRight;
			if (background != null) background.Rect.SetRight(trimAmount);
			if (outline != null) outline.Rect.SetRight(trimAmount + __instance.m_outlinePadding);
		} else {
			if (background != null) background.Rect.SetRight(0f);
			if (outline != null) outline.Rect.SetRight(__instance.m_outlinePadding);
		}

		float totalHeight = textPreferredHeight + headerPreferredHeight
		                                        + __instance.PaddingBot + __instance.PaddingTop
		                                        + __instance.HeaderAndTextSpacing * 2f;
		__instance.SetSize(new Vector2(__instance.GetSize().x, totalHeight));

		return false;
	}
}
