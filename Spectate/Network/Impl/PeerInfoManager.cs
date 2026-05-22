using SNetwork;
using Spectate.Interop;
using UnityEngine;

namespace Spectate.Network.Impl;


/// <summary>
/// Manages information about peers and their support for our plugin.
/// </summary>
[RegisterIl2Cpp]
public class PeerInfoManager : MonoBehaviour {
	public enum PeerSupport {
		Unknown,
		Supported,
		NotSupported
	}

	public class PeerInfo {
		public PeerSupport Support = PeerSupport.Unknown;
		public PlugVersion PlugVersion = new();
		public SNet_Player? Player = null;

		public byte RequestCount { get; private set; } = 0;
		public bool MaxRequestsReached => RequestCount >= MaxRequestCount;

		public void IncrementRequestCount() {
			RequestCount++;
		}
	}

	public const byte PacketId = 0xFF;
	public const byte KeyRequest = 0x69;
	public const byte KeyResponse = 0x67;
	public const int MaxRequestCount = 7;

	public static PeerInfoManager? Instance { get; private set; }
	private Dictionary<ulong, PeerInfo> PeerInfos { get; } = new();

	private const float PeerInfoUpdateInterval = 4.0f;
	private float _timeSinceLastUpdate = 0.0f;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		} else if (Instance != this) {
			Destroy(this);
			Logger.Warn("Multiple instances of LobbyInfo detected, this should not happen!");
			return;
		}

		Net.RegisterHandler(PacketId, HandlePacket);
		Events.OnPlayerJoinLobby += SendPeerInfoRequestSafe;
	}

	private void OnDestroy() {
		if (Instance == this) {
			Instance = null;
		}

		Net.UnregisterHandler(PacketId);
		Events.OnPlayerJoinLobby -= SendPeerInfoRequestSafe;
	}

	private void Update() {
		_timeSinceLastUpdate += Time.deltaTime;
		if (_timeSinceLastUpdate >= PeerInfoUpdateInterval) {
			UpdateLobbyInfo();
			_timeSinceLastUpdate = 0.0f;
		}
	}

	/// <summary>
	/// Returns whether a given player is supported.
	/// </summary>
	/// <param name="player">player to query</param>
	/// <returns>true if player is supported</returns>
	public static bool Supported(SNet_Player player) {
		if (Instance == null) return false;
		return Instance.PeerInfos.TryGetValue(player.Lookup, out var info) && info.Support == PeerSupport.Supported;
	}

	/// <summary>
	/// Returns whether a support for given player is unknown, meaning we have not
	/// received a response but also have not reached max request count yet.
	/// </summary>
	/// <param name="player">player to query</param>
	/// <returns>true if support is unknown</returns>
	public static bool SupportUnknown(SNet_Player player) {
		if (Instance == null) return false;
		return Instance.PeerInfos.TryGetValue(player.Lookup, out var info) && info.Support == PeerSupport.Unknown;
	}

	/// <summary>
	/// Returns whether a given player is definitely not supported.
	/// <br/>
	/// Criteria for this is no response after <see cref="MaxRequestCount"/> is reached.
	/// </summary>
	/// <param name="player">player to query</param>
	/// <returns>true if player is definitely not supported</returns>
	public static bool Unsupported(SNet_Player player) {
		if (Instance == null) return false;
		return Instance.PeerInfos.TryGetValue(player.Lookup, out var info) && info.Support == PeerSupport.NotSupported;
	}

	/// <summary>
	/// Cleanup and sends request for info to players in lobby.
	/// </summary>
	private void UpdateLobbyInfo() {
		SNet_SessionHub sessionHub = SNet.SessionHub;

		// Cleanup stale player infos for players that have left the lobby
		foreach (var id in PeerInfos.Keys.ToList()) {
			if (!sessionHub.IsPlayerInHub(PeerInfos[id].Player)) {
				Logger.Warn($"Player '{PeerInfos[id].Player?.NickName ?? "???"}' ({id}) is not in lobby anymore, removing their info.");
				PeerInfos.Remove(id);
			}
		}

		if (!SNet.IsInLobby) {
			return;
		}

		// Request information from clients
		NetHelper.InvokeWithAllPlayers(RequestInfoFromPlayer);
	}

	/// <summary>
	/// Requests peer info from a player and updates local state side effects
	/// for request in context.
	/// </summary>
	/// <param name="player">player to request info</param>
	private void RequestInfoFromPlayer(SNet_Player player) {
		if (PeerInfos.TryGetValue(player.Lookup, out var info)) {
			if (info.Support == PeerSupport.Supported) {
				return;
			}

			if (info.MaxRequestsReached) {
				info.Support = PeerSupport.NotSupported;
				Logger.Info($"Player '{player.GetName()}' ({player.Lookup}) reached max request count without response, marking as not supported.");
				return;
			}
		}

		SendPeerInfoRequestSafe(player);
	}

	/// <summary>
	/// Sends a request to a player for peer info, creates local entry if doesn't exist yet.
	/// Does nothing if player is bot or local player.
	/// </summary>
	/// <param name="player">player to send request to</param>
	private void SendPeerInfoRequestSafe(SNet_Player player) {
		if (player.IsBot || player.IsLocal) return;
		if (!PeerInfos.TryGetValue(player.Lookup, out var info)) {
			info = new PeerInfo();
			PeerInfos[player.Lookup] = info;
		}

		SendPeerInfoRequest(player);
		info.IncrementRequestCount();
		info.Player ??= player;

		Logger.Debug($"Sent request for info to player '{player.GetName()}' ({player.Lookup}), RequestCount({info.RequestCount} of {MaxRequestCount})");
	}

	/// <summary>
	/// Sends a client request for peer info.
	/// <br/>
	/// Encoding:
	/// [ <see cref="KeyRequest"/> 1B ]
	/// </summary>
	/// <param name="player"></param>
	private static void SendPeerInfoRequest(SNet_Player player) {
		if (player == null) return;
		if (player.IsBot) return;

		byte[] data = { KeyRequest };
		Net.SendBytes(data, PacketId, player);
	}

	/// <summary>
	/// Sends a response with peer info.
	/// <br/>
	/// Encoding:
	/// [ <see cref="KeyResponse"/> 1B ] [ ver major 1B ] [ ver minor 1B ] [ ver patch 1B ]
	/// </summary>
	/// <param name="player"></param>
	private static void SendPeerInfoResponse(SNet_Player player) {
		if (player == null) return;
		if (player.IsBot) return;

		byte[] data = new byte[4];
		data[0] = KeyResponse;
		Array.Copy(Plugin.PlugVersion.ToByteArray(), 0, data, 1, 3);
		Net.SendBytes(data, PacketId, player);
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
			case KeyRequest:
				SendPeerInfoResponse(sender);
				return;

			case KeyResponse:
				if (data.Length < 4) {
					Logger.Error($"Received peer info response of invalid length {data.Length}, expected 4.");
					return;
				}

				if (!PeerInfos.TryGetValue(sender.Lookup, out var info)) {
					info = new PeerInfo();
					PeerInfos[sender.Lookup] = info;
				}

				info.Support = PeerSupport.Supported;
				info.PlugVersion = new PlugVersion(data, 1);
				info.Player = sender;

				Logger.Info($"Received peer info response from player '{sender.GetName()}' ({sender.Lookup}) version={info.PlugVersion}");
				break;
		}
	}
}
