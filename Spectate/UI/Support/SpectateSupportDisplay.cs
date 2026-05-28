using CellMenu;
using HarmonyLib;
using SNetwork;
using Spectate.Assets;
using Spectate.Interop;
using Spectate.Network.Impl;
using Spectate.Utility.Ext;
using UnityEngine;

namespace Spectate.UI.Support;

[RegisterIl2Cpp]
[HarmonyPatch]
public class SpectateSupportDisplay : MonoBehaviour {
	/// <summary>
	/// Reference size of the player select icon of GTFO,
	/// found in-game. Use as basis for offsetting our icon since it is
	/// to the left of this player select guix icon.
	/// </summary>
	private const float PlayerSelectIconSize = 28.0f * 1.16f;

	private static readonly Color ColorSupported = new(0.02f, 0.69f, 0.4f, 1.0f);
	private static readonly Color ColorUnknown = new(0.5f, 0.1f, 0.5f, 1.0f);
	private static readonly Color ColorUnsupported = new(0.8f, 0.8f, 0.0f, 1.0f);
	private static readonly Color ColorNa = new(0.7f, 0.7f, 0.7f, 1.0f);

	/// <summary>
	/// If this display has been properly configured
	/// </summary>
	public bool SetupComplete { get; private set; } = false;

	/// <summary>
	/// The player slot index this support display is for.
	/// </summary>
	public int SlotIndex { get; private set; } = -1;

	private SpriteRenderer _iconSpriteRenderer = null!;
	private RectTransform _container = null!;
	private GameObject _containerGo = null!;
	private CM_Item _cmItem = null!;
	private BoxCollider2D _boxCollider = null!;

	private CM_PlayerLobbyBar _lobbyBar = null!;

	private void Awake() {
		_lobbyBar = GetComponent<CM_PlayerLobbyBar>();
		Setup(_lobbyBar);
		PeerInfoManager.OnPeerInfoUpdated += UpdateSupportInfo;
		Events.OnPageLoadoutPlayerListUpdate += UpdateSupportInfo;
	}

	private void OnDestroy() {
		PeerInfoManager.OnPeerInfoUpdated -= UpdateSupportInfo;
		Events.OnPageLoadoutPlayerListUpdate -= UpdateSupportInfo;
	}

	private void Setup(CM_PlayerLobbyBar? lobbyBar) {
		if (lobbyBar == null) {
			Logger.Error("SpectateSupportDisplay cannot be Setup with null CM_PlayerLobbyBar, aborting setup");
			return;
		}

		_lobbyBar = lobbyBar;
		SlotIndex = _lobbyBar.PlayerSlotIndex;

		Transform refTrans = _lobbyBar.m_nickNameGuix.transform;

		_containerGo = new GameObject("SupportDisplay");
		_container = _containerGo.AddComponent<RectTransform>();
		_containerGo.layer = LayerManager.LAYER_UI;
		_container.SetParent(refTrans.parent, worldPositionStays: false);
		_container.localPosition = refTrans.localPosition - new Vector3(PlayerSelectIconSize * 1.5f, 0.0f, 0.0f);
		// _container.localScale = refTrans.localScale;
		_container.sizeDelta = new Vector2(refTrans.localScale.x, refTrans.localScale.y);

		var width = refTrans.localScale.x;

		{
			_cmItem = _containerGo.AddComponent<CM_Item>();

			_cmItem.OnBtnPressCallback = new Action<int>(_ => { });

			_boxCollider = _containerGo.AddComponent<BoxCollider2D>();
			_boxCollider.size = width.ToVector2();

			_lobbyBar.m_parentPage.UpdateCellMenuCursorItems();
		}

		{
			_iconSpriteRenderer = new GameObject("SupportIcon").AddComponent<SpriteRenderer>();
			_iconSpriteRenderer.gameObject.layer = LayerManager.LAYER_UI;
			_iconSpriteRenderer.transform.SetParent(_container, worldPositionStays: false);
			_iconSpriteRenderer.transform.localPosition = Vector3.zero;
			_iconSpriteRenderer.sprite = SharedAssetLibrary.SpectateIconSprite;
			_iconSpriteRenderer.transform.localScale = width.ToVector3();
		}

		UpdateSupportInfo();

		SetupComplete = true;
	}

	public void UpdateSupportInfo() {
		if (SlotIndex >= SNet.Slots.PlayerSlots.Length || SlotIndex < 0) {
			Logger.Warn($"Invalid slot index={SlotIndex} for support display, did we call Setup()?");
			return;
		}

		SNet_Player player = SNet.Slots.PlayerSlots[SlotIndex].player;

		var (tooltipInfo, spriteColor) = GetDisplayInfo(player);

		_cmItem.TooltipInfo = tooltipInfo;
		_iconSpriteRenderer.color = spriteColor;

		// update existing tooltip
		var tooltip = _lobbyBar.m_parentPage.m_tooltip;
		if (tooltip != null && tooltip.transform.parent == _container) {
			tooltip.SetTooltip(tooltipInfo);
#if DEBUG
			Logger.Debug(
				$"Updated existing tooltip for player '{player?.GetName() ?? "null"}' with support={tooltipInfo.TooltipHeader}\n{new System.Diagnostics.StackTrace(true)}");
#endif
		}
	}

	private (TooltipInfo, Color) GetDisplayInfo(SNet_Player player) {
		string CHex(Color color) {
			return ColorUtility.ToHtmlStringRGB(color);
		}

		TooltipInfo ti = new TooltipInfo {
			PositionType = TooltipPositionType.UnderElement,
			TooltipHeader = "Spectate",
			TooltipText = "",
			UseTooptip = true
		};

		// no player in slot
		// NOTE: honestly this should never show up, since the part we are attaching
		//  to is only active when there is a player in slot.
		if (player == null) {
			ti.TooltipHeader += $" <#{CHex(ColorNa)}>(N/A)</color>";
			ti.TooltipText = "No player in slot";
			return (ti, ColorNa);
		}

		if (!(SNet.SessionHub?.IsPlayerInHub(player) ?? false)) {
			ti.TooltipHeader += $" <#{CHex(ColorNa)}>(N/A)</color>";
			ti.TooltipText = "<color=red>[ERROR] player not in SessionHub</color>";
			return (ti, ColorNa);
		}

		if (player.IsBot) {
			ti.TooltipHeader += $" <#{CHex(ColorNa)}>(Bot)</color>";
			ti.TooltipText = "Beep boop, I'm a bot!";
			return (ti, ColorNa);
		}

		if (!PeerInfoManager.TryGetPeerInfo(player, out var info)) {
			Logger.Error(
				$"[SpectateSupportDisplay] can't get info for player (slot={SlotIndex}) '{player.NickName}' ({player.Lookup}) " +
				$"IsBot={player.IsBot} IsLocal={player.IsLocal} IsInLobby={player.IsInLobby} IsInSessionHub={player.IsInSessionHub}");
			ti.TooltipHeader += $" <#{CHex(ColorUnknown)}>(Unknown)</color>";
			ti.TooltipText = "<color=red>[ERROR] player in SessionHub but has no PeerInfo</color>";
			return (ti, ColorUnknown);
		}

		ti.TooltipHeader = "Spectate" + info.Support switch {
			PeerInfoManager.PeerSupport.Supported => $" <#{CHex(ColorSupported)}>(Supported)</color>",
			PeerInfoManager.PeerSupport.NotSupported => $" <#{CHex(ColorUnsupported)}>(Unsupported)</color>",
			PeerInfoManager.PeerSupport.Unknown => $" <#{CHex(ColorUnknown)}>(Unknown)</color>",
			_ => ""
		};
		ti.TooltipText = info.Support switch {
			PeerInfoManager.PeerSupport.Supported => $"<#63DBD5>v{info.PlugVersion}</color>\n" +
			                                         (player.IsLocal
				                                         ? "This is you!"
				                                         : "Complete features available with this player."),
			PeerInfoManager.PeerSupport.NotSupported => "Some features are limited.",
			PeerInfoManager.PeerSupport.Unknown => "Waiting for player info...\n" +
			                                       $"<#{CHex(ColorUnsupported)}>Attempts ({info.RequestCount} of {PeerInfoManager.MaxRequestCount})</color>",
			_ => "N/A"
		};
		_cmItem.TooltipInfo = ti;

		var spriteColor = info.Support switch {
			PeerInfoManager.PeerSupport.Supported => ColorSupported,
			PeerInfoManager.PeerSupport.NotSupported => ColorUnsupported,
			PeerInfoManager.PeerSupport.Unknown => ColorUnknown,
			_ => ColorNa
		};

		return (ti, spriteColor);
	}

	[HarmonyPatch(
		typeof(CM_PlayerLobbyBar),
		nameof(CM_PlayerLobbyBar.SetupFromPage)
	)]
	[HarmonyPostfix]
	private static void OnLobbyBarSetupFromPage(CM_PlayerLobbyBar __instance) {
		__instance.gameObject.AddComponent<SpectateSupportDisplay>();
	}
}
