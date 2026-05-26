using SNetwork;
using Spectate.Interop;
using Spectate.Network;
using Spectate.Network.Impl;
using Spectate.Assets;
using UnityEngine;

namespace Spectate.UI;

/// <summary>
/// Manages UI for displaying number of players watching the spectated player.
/// </summary>
[RegisterIl2Cpp]
public class SpectatorCountUI : MonoBehaviour {
	/// <summary>
	/// Time (in seconds) after a spectator's last active spectate packet is
	/// received before they are removed from the spectator count.
	/// </summary>
	private const float SpectateTTL = 1.0f;

	private readonly Dictionary<SNet_Player, float> _timeSinceLastActiveSpectate = new();

	private void Awake() {
		Net.RegisterHandler(NetImpl.PacketIdxSendSpectatorTargetState, HandlePacket);
	}

	private void OnDestroy() {
		Net.UnregisterHandler(NetImpl.PacketIdxSendSpectatorTargetState);
	}

	private void Update() {
		foreach (var player in _timeSinceLastActiveSpectate.Keys) {
			_timeSinceLastActiveSpectate[player] += Time.unscaledDeltaTime;
			if (_timeSinceLastActiveSpectate[player] > SpectateTTL) {
				_timeSinceLastActiveSpectate.Remove(player);
			}
		}
	}

	/// <summary>
	/// Replicates existing elements for use
	/// </summary>
	public void CreateUI() {

	}

	private int GetSpectatorCount() {
		return _timeSinceLastActiveSpectate.Count;
	}

	private void HandlePacket(byte[] data, SNet_Player? sender) {
		if (data.Length != 1) {
			Logger.Warn("Expected Spectate");
		}

		if (sender == null) {
			Logger.Warn("Received spectate packet with null sender");
			return;
		}

		bool isActiveSpectate = data[0] != 0;
		if (isActiveSpectate) {
			Logger.Debug($"Player '{sender.GetName()}' ({sender.Lookup}) is spectating local player.");
			_timeSinceLastActiveSpectate[sender] = 0.0f;
		} else {
			_timeSinceLastActiveSpectate.Remove(sender);
		}
	}
}
