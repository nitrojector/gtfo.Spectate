using TMPro;
using UnityEngine;
using Object = System.Object;

namespace Spectate;

public class SpectateUI : MonoBehaviour {
	public static SpectateUI? Instance { get; private set; }

	private bool _wasUIActive = false;

	private const string SpectateTargetTextName = $"{Plugin.GUID}_SpectateTargetText";
	private TextMeshPro? _specTargetTmp;
	private readonly string _specTargetTextColor = "FFFFFF";

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
		_specTargetTmp.name = SpectateTargetTextName;
		_specTargetTmp.fontSize = 26f;
		_specTargetTmp.color = new(1.0f, 1.0f, 1.0f, 0.8f);
		_specTargetTmp.enableAutoSizing = false;
		_specTargetTmp.rectTransform.SetParent(pstatus.transform.parent); // set parent to MovementRoot
		_specTargetTmp.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchoredPosition = new Vector2(0f, -200f);
		_specTargetTmp.gameObject.SetActive(false);
		return true;
	}

	private void Update() {
		if ((!SpectateCam.Instance?.Active ?? true) ||
		    !SpectateCam.Instance.TargetReady) {
			SetUIActive(false);

			return;
		}

		if (!CheckOrUpdateTMP()) return; // ensure TMP is valid
		SetUIActive(true);

		string playerName = SpectateCam.Instance.Target?.Agent.PlayerName ?? "<#FFDE21>Unknown</color>";
		string spectateText = $"<#{_specTargetTextColor}>Spectating\n<size=36>{playerName}</size></color>";
		if (spectateText != _specTargetTmp!.text) {
			_specTargetTmp!.text = spectateText;
			ForceTMPUpdate(_specTargetTmp);
		}
	}

	void SetUIActive(bool active) {
		if (_wasUIActive == active) return;
		Util.SetObjActiveIfChanged(_specTargetTmp, active);
		_wasUIActive = active;
	}

	void ForceTMPUpdate(TextMeshPro tmp) {
		if (tmp == null) return;
		tmp.SetAllDirty();
		tmp.ForceMeshUpdate();
	}

	public void WarnFreecamNoAdjustPitch() {
		// TODO:
	}
}
