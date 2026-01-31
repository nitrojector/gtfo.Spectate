using Spectate.Config;
using TMPro;
using UnityEngine;

namespace Spectate;

public class SpectateUI : MonoBehaviour {
	public static SpectateUI? Instance { get; private set; }

	private bool _wasUIActive = false;
	private eSpectateUIState _uiState = eSpectateUIState.HideMenu;
	private eSpectateUIState _uiStatePrev = eSpectateUIState.ShowMenu;

	private bool _freecamPrev = ConfigMgr.DefaultFreecamView;

	private const float MenuCenterOffsetX = 300f;
	private const float MenuLeftPadding = 50f;
	private const float MenuElementWidth = MenuCenterOffsetX - MenuLeftPadding;
	private const float MenuRightKeybindSpace = 5f;

	private readonly string _specTargetTextColor = "FFFFFF";
	private readonly string _stateHighlightColor = "04B065";

	private TextMeshPro? _specTargetTmp;

	private GameObject? _menuObj;
	private TextMeshPro? _menuTitleTmp;
	private TextMeshPro? _menuViewModeTmp;

	private const int MaxMenuItems = 10;
	private const float MenuItemSpacing = 27.0f;
	private const float MenuOptionHeight = 22.0f;
	private const float MenuKeybindHeight = 16.0f;

	private List<ValueTuple<TextMeshPro?, TextMeshPro?>> _menuItemsTmp = new(MaxMenuItems); // (option, keybind)
	private SpriteRenderer? _menuBackground;

	private const string _menuTitleStr = "SPECTATE";
	private string _menuViewModeStr = "";
	private List<ValueTuple<string, string>> _menuItemsStr = new(); // (option, keybind)

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
			Destroy(gameObject);
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
		_specTargetTmp.rectTransform.SetParent(pstatus.transform.parent, false); // set parent to MovementRoot
		_specTargetTmp.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchoredPosition3D = new Vector3(0f, -200f, 0f);
		_specTargetTmp.rectTransform.sizeDelta = new Vector2(300f, 100f);
		_specTargetTmp.alignment = TextAlignmentOptions.Center;
		_specTargetTmp.gameObject.SetActive(false);

		if (_menuObj != null) Destroy(_menuObj);

		_menuObj = new GameObject($"{Plugin.GUID}_SpectateMenu");
		RectTransform menuRt = _menuObj.AddComponent<RectTransform>();
		menuRt.SetParent(pstatus.transform.parent, false); // set parent to MovementRoot
		menuRt.localScale = Vector3.one;
		menuRt.anchorMin = new Vector2(0.0f, 0.5f);
		menuRt.anchorMax = new Vector2(0.0f, 0.5f);
		menuRt.pivot = new Vector2(0.5f, 0.5f);
		menuRt.anchoredPosition3D = new Vector3(MenuCenterOffsetX, 0f, 0f);

		_menuTitleTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_MenuTitle");
		_menuTitleTmp.rectTransform.SetParent(_menuObj.transform, false);
		_menuTitleTmp.rectTransform.pivot = new Vector2(1.0f, 0.0f);
		_menuTitleTmp.rectTransform.anchoredPosition3D = new Vector3(0f, 55f, 0f);
		_menuTitleTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, 45.0f);
		_menuTitleTmp.alignment = TextAlignmentOptions.Right;

		_menuViewModeTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_ViewMode");
		_menuViewModeTmp.rectTransform.SetParent(_menuObj.transform, false);
		_menuViewModeTmp.rectTransform.pivot = new Vector2(1.0f, 0.0f);
		_menuViewModeTmp.rectTransform.anchoredPosition3D = new Vector3(0f, 13f, 0f);
		_menuViewModeTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, 40.0f);
		_menuViewModeTmp.alignment = TextAlignmentOptions.Right;

		for (int i = 0; i < MaxMenuItems; i++) {
			var optionTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_MenuOption_{i}");
			var keybindTmp = CreateTMPFrom(pstatus.m_healthText, $"{Plugin.GUID}_MenuKeybind_{i}");

			optionTmp.rectTransform.SetParent(_menuObj.transform, false);
			optionTmp.rectTransform.pivot = new Vector2(1.0f, 0.5f);
			optionTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, MenuOptionHeight);
			optionTmp.rectTransform.anchoredPosition3D =
				new Vector3(0f, -((i + 0.5f) * MenuItemSpacing), 0f);
			optionTmp.alignment = TextAlignmentOptions.Right;

			keybindTmp.rectTransform.SetParent(_menuObj.transform, false);
			keybindTmp.rectTransform.pivot = new Vector2(0.0f, 0.5f);
			keybindTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, MenuKeybindHeight);
			keybindTmp.rectTransform.anchoredPosition3D =
				new Vector3(MenuRightKeybindSpace, -((i + 0.5f) * MenuItemSpacing), 0f);
			keybindTmp.alignment = TextAlignmentOptions.Left;

			_menuItemsTmp.Add(new ValueTuple<TextMeshPro?, TextMeshPro?>(optionTmp, keybindTmp));
		}

		UpdateMenu();

		var bgTrans = pstatus.transform.parent.Find("PUI_CommunicationMenu(Clone)/Root/Backround");
		if (bgTrans == null) {
			Logger.Warn(
				$"SpectateUI: Could not find background transform in CommunicationMenu!, " +
				$"using \"{pstatus.transform.parent.name}\"");
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
		tmp.enableAutoSizing = true;
		tmp.autoSizeTextContainer = false;
		tmp.enableWordWrapping = false;
		tmp.overflowMode = TextOverflowModes.Ellipsis;
		tmp.margin = Vector4.zero;
		tmp.fontSize = 26;
		tmp.fontSizeMin = 0;
		tmp.fontSizeMax = 69;
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

		if (!CheckOrCreateTMP()) return; // ensure TMP is valid

		ProcessInput();
		UpdatePlayerStatus(SpectateCam.Instance.Target);
		UpdatePlayerText();
		SetUIActive(true);
	}

	public void UpdatePlayerStatus(SpectateTarget? target) {
		if (target == null || target.Agent == null) {
			Logger.Error("SpectateUI: Could not update player status text: no target!");
			return;
		}

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		pstatus?.UpdateHealth(target.Health);
		pstatus?.UpdateInfection(target.Infection, 0.0f);
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
		_menuViewModeStr = "";
		_menuItemsStr.Clear();
	}

	void AddMenuItem(eSpectateMenuItem item) {
		if (_menuItems.TryGetValue(item, out var val)) {
			_menuItemsStr.Add(val);
		} else {
			Logger.Warn("SpectateUI: Tried to add unknown menu item!");
		}
	}

	void UpdateMenuTitle(bool freecam) {
		string freeTxt = freecam ? $"<#{_stateHighlightColor}FF>FREE-LOOK</color>" : "<#FFFFFF60>FREE-LOOK</color>";
		string followTxt = !freecam ? $"<#{_stateHighlightColor}FF>FOLLOW</color>" : "<#FFFFFF60>FOLLOW</color>";
		_menuViewModeStr = $"{freeTxt} / {followTxt}";
	}

	void FlushMenu() {
		UpdateText(_menuTitleTmp, _menuTitleStr);
		UpdateText(_menuViewModeTmp, _menuViewModeStr);
		int menuItemCount = _menuItemsStr.Count;
		for (int i = 0; i < MaxMenuItems; i++) {
			var (optionTmp, keybindTmp) = _menuItemsTmp[i];

			if (i < menuItemCount) {
				var (optionStr, keybindStr) = _menuItemsStr[i];
				UpdateText(optionTmp, $"<allcaps>{optionStr}</allcaps>");
				UpdateText(keybindTmp, $"<color=orange>[{keybindStr}]</color>");
				Util.SetObjActiveIfChanged(optionTmp, true);
				Util.SetObjActiveIfChanged(keybindTmp, true);
			} else {
				Util.SetObjActiveIfChanged(optionTmp, false);
				Util.SetObjActiveIfChanged(keybindTmp, false);
			}
		}
	}

	void SetUIActive(bool active) {
		if (_wasUIActive == active) return;
		// Util.SetObjActiveIfChanged(_specTargetTmp, active);
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
		// TODO: implement warning UI
	}
}
