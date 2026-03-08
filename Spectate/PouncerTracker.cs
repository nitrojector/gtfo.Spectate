using Enemies;
using Player;
using UnityEngine;

namespace Spectate;

public class PouncerTracker : MonoBehaviour {
	public static PouncerTracker? Instance { get; private set; }
	public bool IsLocalPlayerSnatched { get; private set; } = false;

	private List<PouncerBehaviour> ActivePouncers { get; } = new();
	private const float CleanupDelay = 10.0f;
	private float _timeUntilCleanup = 0.0f;

	public PouncerTracker(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		} else if (Instance != this) {
			Destroy(this);
			return;
		}
	}

	private void Update() {
		if (_timeUntilCleanup > 0.0f) {
			_timeUntilCleanup -= Time.deltaTime;
			if (!(_timeUntilCleanup <= 0.0f)) return;

			CleanupPouncers();
			_timeUntilCleanup = CleanupDelay;
		}
	}

	public void RegisterPouncer(PouncerBehaviour pouncer) {
		if (!ActivePouncers.Contains(pouncer)) {
			ActivePouncers.Add(pouncer);
			pouncer.gameObject.AddComponent<PouncerTrackingDart>();
			IsLocalPlayerSnatched = true;
		}
	}

	public void UnregisterPouncer(GameObject pouncer) {
		foreach (PouncerBehaviour p in ActivePouncers) {
			if (p.gameObject == pouncer || p.gameObject.Pointer == pouncer.Pointer) {
				ActivePouncers.Remove(p);
				break;
			}
		}
	}

	public bool IsCaptured(PlayerAgent agent) {
		foreach (var pouncer in ActivePouncers) {
			if (pouncer == null || pouncer.CapturedPlayer == null) continue;
			if (pouncer.CapturedPlayer.Pointer == agent.Pointer) {
				return true;
			}
		}

		return false;
	}

	private void CleanupPouncers() {
		ActivePouncers.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);
	}
}

public class PouncerTrackingDart : MonoBehaviour {
	public PouncerTrackingDart(IntPtr ptr) : base(ptr) {
	}

	private void OnDestroy() {
		if (PouncerTracker.Instance != null) {
			PouncerTracker.Instance.UnregisterPouncer(this.gameObject);
		}
	}
}
