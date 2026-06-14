using Player;
using SNetwork;
using Spectate.UI;

namespace Spectate.Network.Impl;

/// <summary>
/// Provides implementation for network data sends.
/// </summary>
public static class NetImpl {
	public const byte PacketIdxSendSpectatorTargetState = 0xFE;

	/// <summary>
	/// Sends a spectate target state update to a player.
	/// <br/>
	/// Encoding:
	/// [ <see cref="SNet_Player.Lookup"/> 8B ]
	/// </summary>
	public static void SendSpectateTargetState(SNet_Player sendTarget, SNet_Player currentlySpectating) {
		if (sendTarget == null) return;
		if (sendTarget.IsBot) return;
		var lookupData = BitConverter.GetBytes(currentlySpectating.Lookup);
		Net.SendBytes(lookupData, PacketIdxSendSpectatorTargetState, sendTarget);
	}

}
