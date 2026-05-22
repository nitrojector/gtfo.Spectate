using SNetwork;

namespace Spectate.Network;

/// <summary>
/// Helper methods for network operations.
/// </summary>
public static class NetHelper {
	/// <summary>
	/// Finds a player by their lookup ID (i.e. steamid) from the list of players in the current level.
	/// </summary>
	/// <param name="id">player lookup (i.e. steamid)</param>
	/// <returns>player matched, otherwise false</returns>
	public static SNet_Player? GetPlayerByID(ulong id) {
		foreach (var agent in SNet.LobbyPlayers) {
			if (agent.Lookup == id) {
				return agent;
			}
		}

		return null;
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
