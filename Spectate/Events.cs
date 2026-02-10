namespace Spectate;

public static class Events {
	public static event Action? OnSessionEnd;
	public static event Action? OnSessionStart;
	public static event Action? OnAnyPlayerDeath;

	internal static void RaiseSessionEnd() => OnSessionEnd?.Invoke();
	internal static void RaiseSessionStart() => OnSessionStart?.Invoke();
	internal static void RaiseAnyPlayerDeath() => OnAnyPlayerDeath?.Invoke();
}
