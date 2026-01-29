using TMPro;
using UnityEngine;
using Object = System.Object;

namespace Spectate;

public class SpectateUI : MonoBehaviour {
	public static SpectateUI? Instance { get; private set; }

	private const string SpectateTargetTextName = $"{Plugin.GUID}_SpectateTargetText";
	private TextMeshPro? _specTargetTmp;

	public SpectateUI(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(this.gameObject);
		} else {
			Instance = this;
		}
	}

	bool CheckOrUpdateTMP() {
		if (_specTargetTmp != null) return true;

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		if (pstatus == null) return false;

		_specTargetTmp = Instantiate(pstatus.m_healthText);
		_specTargetTmp.alignment = TextAlignmentOptions.Center;
		_specTargetTmp.verticalAlignment = VerticalAlignmentOptions.Top;
		_specTargetTmp.name = SpectateTargetTextName;
		_specTargetTmp.fontSize = 18f;
		_specTargetTmp.enableAutoSizing = false;
		_specTargetTmp.transform.SetParent(pstatus.transform);
		_specTargetTmp.transform.localPosition = new Vector3(0f, -12f, 0f);
		_specTargetTmp.gameObject.SetActive(false);
		return true;
	}

	private void Update() {
		if ((!SpectateCam.Instance?.Active ?? true) ||
		    !SpectateCam.Instance.TargetReady) {
			if (_specTargetTmp != null && _specTargetTmp.gameObject.activeSelf) {
				_specTargetTmp.gameObject.SetActive(false);
			}

			return;
		}

		if (!CheckOrUpdateTMP()) return;
		if (!(_specTargetTmp!.gameObject.activeSelf)) {
			_specTargetTmp!.gameObject.SetActive(true);
		}

		string playerName = SpectateCam.Instance.Target?.Agent.PlayerName ?? "<#FFDE21>Unknown</color>";
		string spectateText = $"Spectating\n{playerName}";
		if (spectateText != _specTargetTmp!.text) {
			_specTargetTmp!.text = spectateText;
			ForceTMPUpdate();
		}
	}

	void ForceTMPUpdate() {
		if (_specTargetTmp == null) return;
		_specTargetTmp.SetAllDirty();
		_specTargetTmp.ForceMeshUpdate();
	}
}
