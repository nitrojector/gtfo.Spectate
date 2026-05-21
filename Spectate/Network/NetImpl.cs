using Player;
using SNetwork;
using Spectate.UI;

namespace Spectate.Network;

/// <summary>
/// Provides implementation for network data sends.
/// </summary>
public static class NetImpl {
	// === Spectator Target State ===
	public const byte PacketIdxSendSpectatorTargetState = 0xFE;

	/// <summary>
	/// Sends a spectate target state update to a player.
	/// </summary>
	/// <param name="spectating"></param>
	/// <param name="player"></param>
	public static void SendSpectateTargetState(bool spectating, SNet_Player player) {
		if (player == null) return;
		if (player.IsBot) return;

		byte[] data = { (byte)(spectating ? 0x01 : 0x00) };
		Net.SendBytes(data, PacketIdxSendSpectatorTargetState, player);
	}
}
