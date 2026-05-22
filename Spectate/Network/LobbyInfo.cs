using SNetwork;
using Spectate.Interop;
using UnityEngine;
using UnityEngine.Networking.Match;

namespace Spectate.Network;

/// <summary>
/// Manages information about players in lobby regarding spectate support and version.
/// </summary>
[RegisterIl2Cpp]
public class LobbyInfo : MonoBehaviour {
	public static LobbyInfo? Instance { get; private set; }
	private Dictionary<ulong, PlayerSpectateInfo> PlayerInfos { get; } = new();

	private const float LobbyInfoUpdateInterval = 4.0f;
	private float _timeSinceLastUpdate = 0.0f;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		} else if (Instance != this) {
			Destroy(this);
			Logger.Warn("Multiple instances of LobbyInfo detected, this should not happen!");
			return;
		}

		Net.RegisterHandler(NetImpl.PacketIdxClientInfoExchange, HandlePacket);
		Events.OnPlayerJoinLobby += SendPlayerInfoRequestSafe;
	}

	private void OnDestroy() {
		if (Instance == this) {
			Instance = null;
		}

		Net.UnregisterHandler(NetImpl.PacketIdxClientInfoExchange);
		Events.OnPlayerJoinLobby -= SendPlayerInfoRequestSafe;
	}

	private void Update() {
		_timeSinceLastUpdate += Time.deltaTime;
		if (_timeSinceLastUpdate >= LobbyInfoUpdateInterval) {
			UpdateLobbyInfo();
			_timeSinceLastUpdate = 0.0f;
		}
	}

	public static bool HasSpectate(SNet_Player player) {
		if (Instance == null) return false;
		return Instance.PlayerInfos.TryGetValue(player.Lookup, out var info) && info.HasSpectate;
	}

	public static bool DefinitelyDontHaveSpectate(SNet_Player player) {
		if (Instance == null) return false;
		return Instance.PlayerInfos.TryGetValue(player.Lookup, out var info) && info.DefinitelyDontHaveSpectate;
	}

	private void UpdateLobbyInfo() {
		SNet_SessionHub sessionHub = SNet.SessionHub;

		// Cleanup stale player infos for players that have left the lobby
		foreach (var id in PlayerInfos.Keys.ToList()) {
			if (!sessionHub.IsPlayerInHub(PlayerInfos[id].Player)) {
				Logger.Warn($"Player '{PlayerInfos[id].Player?.NickName ?? "???"}' ({id}) is not in lobby anymore, removing their info.");
				PlayerInfos.Remove(id);
			}
		}

		if (!SNet.IsInLobby) {
			return;
		}

		// Request information from clients
		NetImpl.InvokeWithAllPlayers(RequestInfoFromPlayer);
	}

	private void RequestInfoFromPlayer(SNet_Player player) {
		if (PlayerInfos.TryGetValue(player.Lookup, out var info)) {
			if (info.HasSpectate || info.MaxRequestsReached) {
				// if we already know a player has Spectate support
				// OR max requests for client info response reached
				return;
			}
		}

		SendPlayerInfoRequestSafe(player);
	}

	/// <summary>
	/// Sends a request to a player for player info, creates local entry if doesn't exist yet.
	/// Does nothing if player is bot or local player.
	/// </summary>
	/// <param name="player">player to send request to</param>
	private void SendPlayerInfoRequestSafe(SNet_Player player) {
		if (player.IsBot || player.IsLocal) return;
		if (!PlayerInfos.TryGetValue(player.Lookup, out var info)) {
			info = new PlayerSpectateInfo();
			PlayerInfos[player.Lookup] = info;
		}

		NetImpl.SendClientInfoRequest(player);
		info.IncrementRequestCount();
		info.Player ??= player;

		Logger.Debug($"Sent request for info to player '{player.GetName()}' ({player.Lookup}), RequestCount({info.RequestCount} of limit:{PlayerSpectateInfo.MaxRequestCount})");
	}

	/// <summary>
	/// Handler for network packet
	/// </summary>
	private void HandlePacket(byte[] data, SNet_Player? sender) {
		if (sender == null) {
			Logger.Error("Received client info exchange packet with null sender, ignoring!");
			return;
		}

		// client info exchange key
		switch (data[0]) {
			case NetImpl.ClientInfoExchangeKeyRequest:
				NetImpl.SendClientInfoResponse(sender);
				return;

			case NetImpl.ClientInfoExchangeKeyResponse:
				if (data.Length < 4) {
					Logger.Error($"Received client info response of invalid length {data.Length}, expected 4.");
					return;
				}

				if (!PlayerInfos.TryGetValue(sender.Lookup, out var info)) {
					info = new PlayerSpectateInfo();
					PlayerInfos[sender.Lookup] = info;
				}

				info.HasSpectate = data[0] == NetImpl.ClientInfoExchangeKeyResponse;
				info.VersionMajor = data[1];
				info.VersionMinor = data[2];
				info.VersionPatch = data[3];
				info.Player = sender;

				Logger.Info($"Received client info response from player '{sender.GetName()}' ({sender.Lookup}), HasSpectate={info.HasSpectate}, Version={info.VersionMajor}.{info.VersionMinor}.{info.VersionPatch}");
				break;
		}
	}
}

internal class PlayerSpectateInfo {
	public const byte MaxRequestCount = 7;

	public bool HasSpectate = false;
	public byte VersionMajor = 0;
	public byte VersionMinor = 0;
	public byte VersionPatch = 0;
	public SNet_Player? Player = null;

	public byte RequestCount { get; private set; } = 0;
	public bool MaxRequestsReached => RequestCount >= MaxRequestCount;

	/// <summary>
	/// True if player doesn't no spectate support recorded and max requests for client info reached.
	/// </summary>
	public bool DefinitelyDontHaveSpectate => !HasSpectate && MaxRequestsReached;

	public void IncrementRequestCount() {
		RequestCount++;
	}
}
