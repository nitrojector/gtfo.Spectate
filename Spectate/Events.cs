using SNetwork;

namespace Spectate;

public static class Events {
	public static event Action? OnSessionEnd;
	public static event Action? OnSessionStart;
	public static event Action? OnAnyPlayerDeath;
	public static event Action? OnCheckpointReload;

	// === Lobby ===
	public static event Action<SNet_Player>? OnPlayerJoinLobby;

	internal static void RaiseSessionEnd() => OnSessionEnd?.Invoke();
	internal static void RaiseSessionStart() => OnSessionStart?.Invoke();
	internal static void RaiseAnyPlayerDeath() => OnAnyPlayerDeath?.Invoke();
	internal static void RaiseCheckpointReload() => OnCheckpointReload?.Invoke();
	internal static void RaisePlayerJoinLobby(SNet_Player player) => OnPlayerJoinLobby?.Invoke(player);
}
