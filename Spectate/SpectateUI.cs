using Spectate.Config;
using TMPro;
using UnityEngine;

namespace Spectate;

// TODO: BUG: UI doesn't scale correctly across different resolutions / aspect ratios
// perhaps test resolutions? 1080p
public class SpectateUI : MonoBehaviour {
	public static SpectateUI? Instance { get; private set; }

	private bool _wasUIActive = false;
	private eSpectateUIState _uiState = eSpectateUIState.HideMenu;
	private eSpectateUIState _uiStatePrev = eSpectateUIState.ShowMenu;

	private bool _freecamPrev = ConfigMgr.DefaultFreecamView;

	private TextMeshPro? _specTargetTmp;
	private readonly string _specTargetTextColor = "FFFFFF";

	private GameObject? _menuObj;
	private TextMeshPro? _menuTitleTmp;
	private TextMeshPro? _menuOptionsTmp;
	private TextMeshPro? _menuKeybindsTmp;
	private SpriteRenderer? _menuBackground;

	private string _menuTitleStr = "";
	private string _menuOptionsStr = "";
	private string _menuKeybindsStr = "";

	private readonly Dictionary<eSpectateMenuItem, ValueTuple<string, string>> _menuItems = new() {
		[eSpectateMenuItem.ShowMenu] = ("Show Menu", "\\"),
		[eSpectateMenuItem.HideMenu] = ("Hide Menu", "\\"),
		[eSpectateMenuItem.ToggleSpectate] = ("Toggle Spectate", "V"),
		[eSpectateMenuItem.ToggleFreecam] = ("Toggle Free-Look", "F"),
		[eSpectateMenuItem.SwitchPlayer] = ("Switch Player", "LMB / RMB"),
		[eSpectateMenuItem.SelectPlayer] = ("Select Player", "1 - 8"),
		[eSpectateMenuItem.AdjustDistance] = ("Camera Distance", "Scroll"),
		[eSpectateMenuItem.AdjustOrbitCenterHeight] = ("Camera Vertical Offset", "Ctrl + Scroll"),
		[eSpectateMenuItem.AdjustFollowPitch] = ("Camera Pitch", "Shift + Scroll"),
	};

	public SpectateUI(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(this.gameObject);
		} else {
			Instance = this;
		}
	}

	bool CheckOrCreateTMP() {
		if (_specTargetTmp != null && _menuObj != null) return true;

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		if (pstatus == null) return false;

		if (_specTargetTmp != null) Destroy(_specTargetTmp.gameObject);

		_specTargetTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_TargetText");
		_specTargetTmp.rectTransform.SetParent(pstatus.transform.parent); // set parent to MovementRoot
		_specTargetTmp.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchoredPosition3D = new Vector3(0f, -200f, 0f);
		_specTargetTmp.alignment = TextAlignmentOptions.Center;
		_specTargetTmp.gameObject.SetActive(false);

		if (_menuObj != null) Destroy(_menuObj);

		_menuObj = new GameObject($"{Plugin.GUID}_SpectateMenu");
		RectTransform menuRt = _menuObj.AddComponent<RectTransform>();
		menuRt.SetParent(pstatus.transform.parent); // set parent to MovementRoot
		menuRt.anchorMin = new Vector2(0.0f, 0.5f);
		menuRt.anchorMax = new Vector2(0.0f, 0.5f);
		menuRt.anchoredPosition3D = new Vector3(300f, 0f, 0f);

		_menuTitleTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_MenuTitle");
		_menuTitleTmp.rectTransform.SetParent(_menuObj.transform);
		_menuTitleTmp.rectTransform.pivot = new Vector2(1.0f, 0.0f);
		_menuTitleTmp.rectTransform.anchoredPosition3D = new Vector3(0f, 13f, 0f);
		_menuTitleTmp.autoSizeTextContainer = true;
		_menuTitleTmp.alignment = TextAlignmentOptions.BottomRight;
		_menuTitleTmp.fontSize = 50;

		_menuOptionsTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_MenuOptions");
		_menuOptionsTmp.rectTransform.SetParent(_menuObj.transform);
		_menuOptionsTmp.rectTransform.pivot = new Vector2(1.0f, 1.0f);
		_menuOptionsTmp.rectTransform.anchoredPosition3D = new Vector3(0f, 0f, 0f);
		_menuOptionsTmp.alignment = TextAlignmentOptions.TopRight;
		_menuOptionsTmp.autoSizeTextContainer = true;
		_menuOptionsTmp.fontSize = 25;

		_menuKeybindsTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_MenuKeybinds");
		_menuKeybindsTmp.rectTransform.SetParent(_menuObj.transform);
		_menuKeybindsTmp.rectTransform.pivot = new Vector2(0.0f, 1.0f);
		_menuKeybindsTmp.rectTransform.anchoredPosition3D = new Vector3(1f, 0f, 0f);
		_menuKeybindsTmp.alignment = TextAlignmentOptions.TopLeft;
		_menuKeybindsTmp.autoSizeTextContainer = true;
		_menuKeybindsTmp.fontSize = 25;

		UpdateMenu();

		var bgTrans = pstatus.transform.parent.Find("PUI_CommunicationMenu(Clone)/Root/Backround");
		if (bgTrans == null) {
			Logger.Warn(
				$"SpectateUI: Could not find background transform in CommunicationMenu!, using \"{pstatus.transform.parent.name}\"");
			return true;
		}

		var bgObjClone = Instantiate(bgTrans.gameObject);
		bgObjClone.name = $"{Plugin.GUID}_MenuBackground";
		_menuBackground = bgObjClone.GetComponent<SpriteRenderer>();
		_menuBackground.sortingOrder = -20;
		var bgRt = bgObjClone.GetComponent<RectTransform>();
		bgRt.SetParent(_menuObj.transform);
		bgRt.anchoredPosition3D = new Vector3(0f, -40f, 0f);

		return true;
	}

	private TextMeshPro CreateTMPFrom(TextMeshPro from, string name) {
		TextMeshPro tmp = Instantiate(from);
		tmp.name = name;
		tmp.color = new(1.0f, 1.0f, 1.0f, 0.8f);
		tmp.enableAutoSizing = false;
		tmp.enableWordWrapping = false;
		tmp.overflowMode = TextOverflowModes.Overflow;
		tmp.margin = Vector4.zero;
		tmp.fontSize = 26;
		tmp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		tmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		return tmp;
	}

	private void Update() {
		if ((!SpectateCam.Instance?.Active ?? true) ||
		    !SpectateCam.Instance.TargetReady) {
			SetUIActive(false);

			return;
		}

		if (!CheckOrCreateTMP()) return; // ensure TMP is valid // TODO: performance?

		ProcessInput();
		UpdatePlayerText();
		SetUIActive(true);
	}

	private void UpdatePlayerText() {
		string playerName = SpectateCam.Instance?.Target?.Agent.InteractionName ?? "<#FFDE21>Unknown</color>";
		string spectateText = $"<#{_specTargetTextColor}>Spectating\n<size=36>{playerName}</size></color>";
		UpdateText(_specTargetTmp, spectateText);
	}

	public void UpdateMenu() => UpdateMenu(SpectateCam.Instance?.Freecam ?? false);

	public void UpdateMenu(bool freecam) {
		_uiStatePrev = _uiState;
		_freecamPrev = freecam;

		ClearMenu();
		UpdateMenuTitle(freecam);

		switch (_uiState) {
			case eSpectateUIState.ShowMenu:
				AddMenuItem(eSpectateMenuItem.HideMenu);
				AddMenuItem(eSpectateMenuItem.ToggleSpectate);
				AddMenuItem(eSpectateMenuItem.ToggleFreecam);
				AddMenuItem(eSpectateMenuItem.SwitchPlayer);
				AddMenuItem(eSpectateMenuItem.SelectPlayer);
				AddMenuItem(eSpectateMenuItem.AdjustDistance);
				AddMenuItem(eSpectateMenuItem.AdjustOrbitCenterHeight);
				if (!freecam) AddMenuItem(eSpectateMenuItem.AdjustFollowPitch);
				break;
			case eSpectateUIState.HideMenu:
				AddMenuItem(eSpectateMenuItem.ShowMenu);
				break;
		}

		FlushMenu();
	}

	void ProcessInput() {
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS)) return;

		if (Input.GetKeyDown(KeyCode.Backslash)) {
			if (_uiState == eSpectateUIState.ShowMenu) {
				_uiState = eSpectateUIState.HideMenu;
			} else {
				_uiState = eSpectateUIState.ShowMenu;
			}

			UpdateMenu();
		}
	}

	void ClearMenu() {
		_menuTitleStr = "";
		_menuOptionsStr = "<allcaps>";
		_menuKeybindsStr = "<color=orange>";
	}

	void AddMenuItem(eSpectateMenuItem item) {
		if (!_menuItems.ContainsKey(item)) return;
		var (itemText, keybindText) = _menuItems[item];
		_menuOptionsStr += $"{itemText}\n";
		_menuKeybindsStr += $" <size=20>[ {keybindText} ]</size>\n";
	}

	void UpdateMenuTitle(bool freecam) {
		string freeTxt = freecam ? "<#18935EFF>FREE-LOOK</color>" : "<#FFFFFF60>FREE-LOOK</color>";
		string followTxt = !freecam ? "<#18935EFF>FOLLOW</color>" : "<#FFFFFF60>FOLLOW</color>";
		_menuTitleStr = $"SPECTATE\n<size=30>{freeTxt} / {followTxt}</size>";
	}

	void FlushMenu() {
		_menuOptionsStr += "</allcaps>";
		_menuKeybindsStr += "</color>";
		UpdateText(_menuTitleTmp, _menuTitleStr);
		UpdateText(_menuOptionsTmp, _menuOptionsStr);
		UpdateText(_menuKeybindsTmp, _menuKeybindsStr);
	}

	void SetUIActive(bool active) {
		if (_wasUIActive == active) return;
		Util.SetObjActiveIfChanged(_specTargetTmp, active);
		Util.SetObjActiveIfChanged(_menuObj, active);
		_wasUIActive = active;
	}

	void UpdateText(TextMeshPro? tmp, string newText) {
		if (tmp == null) return;
		if (tmp.text == newText) return;
		if (newText != tmp.text) {
			tmp.text = newText;
			ForceTMPUpdate(tmp);
		}
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
