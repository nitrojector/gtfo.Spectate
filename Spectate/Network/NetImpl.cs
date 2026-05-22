using Player;
using SNetwork;
using Spectate.UI;

namespace Spectate.Network;

/// <summary>
/// Provides implementation for network data sends.
/// </summary>
public static class NetImpl {
	// === Client Info Exchange ===
	public const byte PacketIdxClientInfoExchange = 0xFF;
	public const byte ClientInfoExchangeKeyRequest = 0x69;
	public const byte ClientInfoExchangeKeyResponse = 0x67;

	// === Spectator Target State ===
	public const byte PacketIdxSendSpectatorTargetState = 0xFE;

	/// <summary>
	/// Sends a spectate target state update to a player.
	/// <br/>
	/// Encoding:
	/// [ <see cref="spectating"/> 1B ]
	/// </summary>
	/// <param name="spectating"></param>
	/// <param name="player"></param>
	public static void SendSpectateTargetState(bool spectating, SNet_Player player) {
		if (player == null) return;
		if (player.IsBot) return;

		byte[] data = { (byte)(spectating ? 0x01 : 0x00) };
		Net.SendBytes(data, PacketIdxSendSpectatorTargetState, player);
	}

	/// <summary>
	/// Sends a client request for client info.
	/// <br/>
	/// Encoding:
	/// [ <see cref="ClientInfoExchangeKeyRequest"/> 1B ]
	/// </summary>
	/// <param name="player"></param>
	public static void SendClientInfoRequest(SNet_Player player) {
		if (player == null) return;
		if (player.IsBot) return;

		byte[] data = { ClientInfoExchangeKeyRequest };
		Net.SendBytes(data, PacketIdxClientInfoExchange, player);
	}

	/// <summary>
	/// Sends a response with client info.
	/// <br/>
	/// Encoding:
	/// [ <see cref="ClientInfoExchangeKeyResponse"/> 1B ] [ ver major 1B ] [ ver minor 1B ] [ ver patch 1B ]
	/// </summary>
	/// <param name="player"></param>
	public static void SendClientInfoResponse(SNet_Player player) {
		if (player == null) return;
		if (player.IsBot) return;

		byte[] versions = Plugin.VERSION.Split('.').Select(byte.Parse).ToArray();
		byte[] data = new byte[1 + versions.Length];
		data[0] = ClientInfoExchangeKeyResponse;
		Array.Copy(versions, 0, data, 1, versions.Length);
		Net.SendBytes(data, PacketIdxClientInfoExchange, player);
	}

	/// <summary>
	/// Invokes a given send method for all non-local, non-bot players in the level.
	/// </summary>
	/// <param name="sendMethod">method to perform</param>
	public static void InvokeWithAllPlayers(Action<SNet_Player> sendMethod) {
		if (!SNet.IsInLobby) return;

		foreach (SNet_Player agent in SNet.Lobby.Players) {
			if (agent == null) continue;

			if (agent.IsBot) continue;
			if (agent.IsLocal) continue;
			sendMethod.Invoke(agent);
		}
	}
}
