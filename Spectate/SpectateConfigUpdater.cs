using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Spectate.Config;
using Spectate.Interop;
using UnityEngine;

namespace Spectate;

[RegisterIl2Cpp]
public class SpectateConfigUpdater : MonoBehaviour {
	public const float UpdateIntervalSeconds = 5f;
	Coroutine _configUpdateCoroutine = null!;

	public SpectateConfigUpdater(IntPtr ptr) : base(ptr) {
	}

	void Awake() {
		_configUpdateCoroutine = StartCoroutine(ConfigUpdateCoroutine().WrapToIl2Cpp());
	}

	private IEnumerator ConfigUpdateCoroutine() {
		while (true) {
			yield return new WaitForSeconds(UpdateIntervalSeconds);
			ConfigMgr.WriteConfigIfDirty();
		}
	}

	private void OnApplicationQuit() {
		ConfigMgr.WriteConfigIfDirty();
	}
}
