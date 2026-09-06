using SNetwork;
using Spectate.Interop;
using Spectate.Network;
using Spectate.Network.Impl;
using Spectate.Assets;
using Spectate.Utility;
using Spectate.Utility.Ext;
using TMPro;
using UnityEngine;

namespace Spectate.UI;

/// <summary>
/// Manages UI for displaying number of players watching the spectated player.
/// </summary>
[RegisterIl2Cpp]
public class SpectatorCountUI : MonoBehaviour {
	private class SpectatorData {
		public ulong Lookup { get; set; }
		public float TimeSinceLastUpdate { get; set; }

		public bool IsSpectating(SNet_Player player) {
			if (player == null) return false;
			return player.Lookup == Lookup;
		}
	}

	/// <summary>
	/// Time (in seconds) after a spectator's last active spectate packet is
	/// received before they are removed from the spectator count.
	/// </summary>
	private const float SpectateTtl = 1.0f;

	private readonly Dictionary<ulong, SpectatorData> _spectatorData = new();

	private const float IconSize = 24.0f;
	private const float IconTextSpacing = 18.0f;
	private const float TextContainerWidth = 48.0f;
	private const float UIGroupOffsetFromTop = 142.0f;

	private RectTransform? _uiContainer = null;
	private SpriteRenderer? _iconSr = null;
	private TMP_Text? _spectatorCountTxt = null;


	private void Awake() {
		Net.RegisterHandler(NetImpl.PacketIdxSendSpectatorTargetState, HandlePacket);
		CreateUI();
	}

	private void OnDestroy() {
		Net.UnregisterHandler(NetImpl.PacketIdxSendSpectatorTargetState);
	}

	private void Update() {
		foreach (var player in _spectatorData.Keys) {
			_spectatorData[player].TimeSinceLastUpdate += Time.unscaledDeltaTime;
			if (_spectatorData[player].TimeSinceLastUpdate > SpectateTtl) {
				_spectatorData.Remove(player);
			}
		}

		var spectating = SpectateCam.Instance?.Active ?? false;

		if (spectating && !(SpectateCam.Instance?.TargetReady ?? false)) {
			_spectatorCountTxt!.text = "0";
			return;
		}

		SNet_Player target = spectating ? SpectateCam.Instance!.Target!.SAgent : SNet.LocalPlayer;

		int count = GetSpectatorCount(target) + (spectating ? 1 : 0); // if spectating, include self in count
		_spectatorCountTxt!.text = count.ToString();
		// only show when spectating or when there are spectators on the local player
		_uiContainer?.gameObject.SetActive(spectating || count > 0);
	}

	/// <summary>
	/// Replicates existing elements for use
	/// </summary>
	public bool CreateUI() {
		// TMPUtils.CreateTMP("Spectator Count")
		Transform? mvmtRootTrans = GuiManager.PlayerLayer?.CustomComponentRoot;
		if (mvmtRootTrans == null) return false;

		_uiContainer = new GameObject("SpectatorCountUI").AddComponent<RectTransform>();
		_uiContainer.SetParent(mvmtRootTrans, false);
		_uiContainer.pivot = new Vector2(0.5f, 1.0f);
		_uiContainer.sizeDelta = new Vector2(IconSize + IconTextSpacing + TextContainerWidth, IconSize);
		_uiContainer.anchorMin = new Vector2(0.5f, 1.0f);
		_uiContainer.anchorMax = new Vector2(0.5f, 1.0f);
		_uiContainer.anchoredPosition = new Vector2(0.0f, -UIGroupOffsetFromTop);

		_uiContainer.gameObject.layer = LayerManager.LAYER_UI;

		{
			_iconSr = new GameObject("Icon").AddComponent<SpriteRenderer>();
			_iconSr.transform.SetParent(_uiContainer, false);
			var rt = _iconSr.gameObject.AddComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(1.0f, 0.5f);
			rt.anchoredPosition = new Vector2(-IconTextSpacing / 3.0f, 0.0f);
			rt.sizeDelta = new Vector2(IconSize, IconSize);
			_iconSr.sprite = SharedAssetLibrary.SpectateIconSprite;
			_iconSr.transform.localScale = IconSize.ToVector3();

			_iconSr.gameObject.layer = LayerManager.LAYER_UI;
		}

		if (TMPUtils.CreateTMP("SpectatorCountText", _uiContainer, out var tmp)) {
			tmp.enableAutoSizing = true;
			tmp.autoSizeTextContainer = false;
			tmp.enableWordWrapping = false;

			tmp.overflowMode = TextOverflowModes.Ellipsis;
			tmp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			tmp.rectTransform.pivot = new Vector2(0.0f, 0.5f);
			tmp.rectTransform.anchoredPosition = new Vector2(IconTextSpacing * 2.0f / 3.0f, 0.0f);
			tmp.rectTransform.sizeDelta = new Vector2(TextContainerWidth, IconSize);
			tmp.color = Color.white;
			tmp.alignment = TextAlignmentOptions.Left;

			tmp.gameObject.layer = LayerManager.LAYER_UI;

			_spectatorCountTxt = tmp;
			_spectatorCountTxt.gameObject.SetActive(true);
		}

		return true;
	}

	/// <summary>
	/// Gets the number of spectators on a player.
	/// If <see cref="player"/> is null, returns the number of spectators on the local player.
	/// </summary>
	/// <param name="player">player to query</param>
	/// <returns>number of spectators</returns>
	private int GetSpectatorCount(SNet_Player? player = null) {
		int count = 0;
		SNet_Player queryTarget = player == null ? SNet.LocalPlayer : player;
		foreach (var data in _spectatorData.Values) {
			if (data.IsSpectating(queryTarget)) {
				count++;
			}
		}

		return count;
	}

	private void HandlePacket(byte[] data, SNet_Player? sender) {
		if (data.Length != sizeof(ulong)) {
			Logger.Warn($"Expected packet of size {sizeof(ulong)}, got {data.Length}");
			return;
		}

		if (sender == null) {
			Logger.Warn("Received spectate packet with null sender");
			return;
		}

		ulong lookup = BitConverter.ToUInt64(data, 0);
		_spectatorData[sender.Lookup] = new SpectatorData {
			Lookup = lookup,
			TimeSinceLastUpdate = 0.0f
		};
	}
}
