using System.Runtime.CompilerServices;
using Player;
using Spectate.Config;
using TMPro;
using UnityEngine;

namespace Spectate.UI;

public class SpectateUI : MonoBehaviour {
	public static SpectateUI? Instance { get; private set; }

	// === UI Core States ===
	/// <summary>
	/// Whether the UI was active in the last update
	/// </summary>
	private bool _wasUIActive = false;

	/// <summary>
	/// Whether the UI needs to be refreshed
	/// </summary>
	private bool _isUIDirty = true;

	/// <summary>
	/// The current state/context of the UI
	/// </summary>
	private eSpectateUIState _uiState = eSpectateUIState.FPNotDowned;

	/// <summary>
	/// The current state/context of the UI
	/// </summary>
	public eSpectateUIState UIState => _uiState;

	/// <summary>
	/// The previous state/context of the UI
	/// </summary>
	private eSpectateUIState _uiStatePrev = eSpectateUIState.FPNotDowned;

	/// <summary>
	/// The UI state that was last rendered
	/// </summary>
	private eSpectateUIState _uiStateRendered = eSpectateUIState.ShowMenu;

	/// <summary>
	/// List of root UI GameObjects that can be rendered.
	/// Parents of roots are always external.
	/// </summary>
	private readonly List<GameObject> _uiRoots = new();

	/// <summary>
	/// The state of individual UI components to be rendered on next render.
	/// </summary>
	private readonly Dictionary<eSpectateUIComp, bool> _uiCompState = new();

	// === UI Layout Constants ===
	/// <summary>
	/// The horizontal offset of the center of the menu
	/// from the left edge of the screen
	/// </summary>
	private const float MenuCenterOffsetX = 300f;

	/// <summary>
	/// The space between the left edge of the menu and the edge of the screen
	/// </summary>
	private const float MenuLeftPadding = 50f;

	/// <summary>
	/// The width of menu elements
	/// </summary>
	private const float MenuElementWidth = MenuCenterOffsetX - MenuLeftPadding;

	/// <summary>
	/// Spacing between left and right halves of the menu
	/// </summary>
	private const float MenuCenterDivideSpacing = 5f;

	/// <summary>
	/// Maximum number of menu items
	/// </summary>
	private const int MaxMenuItems = 10;

	/// <summary>
	/// The vertical spacing between menu items (centers)
	/// </summary>
	private const float MenuItemSpacing = 27.0f;

	/// <summary>
	/// The height of a menu option item (left)
	/// </summary>
	private const float MenuOptionHeight = 22.0f;

	/// <summary>
	/// The height of a menu keybind item (right)
	/// </summary>
	private const float MenuKeybindHeight = 16.0f;

	/// <summary>
	/// The offset (localPosition) of the spectate target object align
	/// </summary>
	private static readonly Vector3 HeadBeaconAlignOffset = new(0f, 0.3f, 0f);

	// === UI Style Constants ===
	private const string SpecTargetTextColor = "FFFFFF";
	private const string StateHighlightColor = "04B065";

	// === UI Elements ===
	private GameObject? _navMarkerTrackTarget;
	private NavMarker? _navMarker;
	private TextMeshPro? _specTargetTmp;

	// --- Menu Elements ---
	private GameObject? _menuObj;
	private TextMeshPro? _menuTitleTmp;
	private TextMeshPro? _menuViewModeTmp;
	private GameObject? _menuListObj;
	private List<ValueTuple<TextMeshPro?, TextMeshPro?>> _menuItemsTmp = new(MaxMenuItems); // (option, keybind)
	private SpriteRenderer? _menuBackground;

	// === UI Render Data ===
	private string _specTargetStr = "";
	private readonly string _menuTitleStr = "SPECTATE";
	private string _menuViewModeStr = "";
	private readonly List<ValueTuple<string, string>> _menuItemsStr = new(); // (option, keybind)

	private readonly Dictionary<eSpectateMenuItem, ValueTuple<string, MenuKeybindEntry>> _menuItems = new() {
		[eSpectateMenuItem.ShowMenu] = ("Show Menu", new(SpectateInputAction.ToggleMenu)),
		[eSpectateMenuItem.HideMenu] = ("Hide Menu", new(SpectateInputAction.ToggleMenu)),
		[eSpectateMenuItem.EnterSpectate] = ("Enter Spectate", new(SpectateInputAction.ToggleSpectate)),
		[eSpectateMenuItem.ExitSpectate] = ("Exit Spectate", new(SpectateInputAction.ToggleSpectate)),
		[eSpectateMenuItem.ToggleFreecam] = ("Toggle Free-Look", new(SpectateInputAction.ToggleFreecam)),
		[eSpectateMenuItem.EnableFreecamAutoTransition] =
			("Enable Auto-Follow", new(SpectateInputAction.ToggleAutoFollow)),
		[eSpectateMenuItem.DisableFreecamAutoTransition] =
			("Disable Auto-Follow", new(SpectateInputAction.ToggleAutoFollow)),
		[eSpectateMenuItem.SwitchPlayer] = ("Switch Player", new("LMB / RMB")),
		[eSpectateMenuItem.SelectPlayer] = ("Select Player", new("1 - 8")),
		[eSpectateMenuItem.AdjustDistance] = ("Camera Distance", new("Scroll")),
		[eSpectateMenuItem.AdjustOrbitCenterHeight] = ("Camera Vertical Offset", new("Ctrl + Scroll")),
		[eSpectateMenuItem.AdjustFollowPitch] = ("Camera Pitch", new("Shift + Scroll")),
	};

	/// <summary>
	/// IL2CPP ctor
	/// </summary>
	public SpectateUI(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
		}
	}

	/// <summary>
	/// Shows a nav marker on the local player to indicate their position while spectating.
	/// </summary>
	public void ShowNavMarker() {
		var localPlayer = PlayerManager.GetLocalPlayerAgent();
		if (localPlayer == null) {
			Logger.Warn("SpectateUI: Cannot show nav marker: no local player!");
			return;
		}

		if (_navMarkerTrackTarget == null) {
			var headTrans = localPlayer.PlayerSyncModel.m_nameNavMarkerAlign;
			_navMarkerTrackTarget = new GameObject($"{Plugin.GUID}_SpectateNavMarkerTrackTarget");
			_navMarkerTrackTarget.transform.SetParent(headTrans, false);
			_navMarkerTrackTarget.transform.localPosition = HeadBeaconAlignOffset;
		}

		if (_navMarker == null) {
			_navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(
				NavMarkerOption.SignTitleDistance,
				_navMarkerTrackTarget, "Spectate LocalPlayer");
		}

		_navMarker.SetTrackingObject(_navMarkerTrackTarget);
		_navMarker.SetVisible(true);
		_navMarker.SetIconScale(0.4f);
		_navMarker.SetTitle(localPlayer.InteractionName);
		_navMarker.SetAlpha(2.0f);
		_navMarker.SetSignInfo("<#FFF>YOU</color>");

		/* Player Title Distance style options
		if (_navMarker == null) {
			_navMarker = GuiManager.NavMarkerLayer.PlaceCustomMarker(
				NavMarkerOption.PlayerTitleDistance,
				localPlayer.gameObject, "Spectate LocalPlayer");
		}

		_navMarker.SetVisible(true);
		_navMarker.SetIconScale(0.4f);
		_navMarker.SetPlayerName(localPlayer.InteractionName);
		_navMarker.SetTitle("");
		_navMarker.SetAlpha(1.0f);
		*/
	}

	/// <summary>
	/// Hides the nav marker on the local player.
	/// Should be called when exiting spectate mode to prevent confusion.
	/// </summary>
	public void HideNavMarker() {
		if (_navMarker != null)
			_navMarker.SetVisible(false);
	}

	/// <summary>
	/// Returns whether all TMP elements exist
	/// </summary>
	/// <returns>true if needed TMPs are non-null</returns>
	private bool AllTMPExist() {
		return _menuObj != null &&
		       _specTargetTmp != null;
	}

	private void Update() {
		bool canSpectate = (SpectateCam.Instance?.Self != null && SpectateCam.Instance.Self.IsDowned)
		                   || ConfigMgr.DevEnables(eDevOpts.AllowSpectatingAnytime);
		if (!canSpectate) {
			SetUIActive(false);
			return;
		}

		if (!CheckOrCreateTMP()) return; // ensure TMP is valid

		ProcessInput();
		if (SpectateCam.Instance?.Active ?? false)
			UpdatePlayerStatusUI(SpectateCam.Instance.Target);
		SetUIActive(true);
		if (_isUIDirty || UIState != _uiStateRendered) {
			RefreshUI();
		}
	}

	public void UpdateForAttach() {
		if (_uiStatePrev == eSpectateUIState.HideMenu || _uiStatePrev == eSpectateUIState.ShowMenu) {
			SetUIState(_uiStatePrev);
		} else {
			SetUIState(eSpectateUIState.HideMenu);
		}
	}

	public void UpdateForDetach() {
		bool isDowned = SpectateCam.Instance?.Self?.IsDowned ?? false;
		SetUIState(isDowned ? eSpectateUIState.FPDowned : eSpectateUIState.FPNotDowned);
		UpdatePlayerStatusUI(SpectateCam.Instance?.Self);
	}

	void ProcessInput() {
		if (!InputMapper.Current.FocusStateFilterPass(eFocusState.FPS)) return;

		if (Input.GetKeyDown(ConfigMgr.GetKeybind(SpectateInputAction.ToggleMenu))) {
			switch (UIState) {
				case eSpectateUIState.ShowMenu:
					SetUIState(eSpectateUIState.HideMenu);
					break;
				case eSpectateUIState.HideMenu:
					SetUIState(eSpectateUIState.ShowMenu);
					break;
			}
		}
	}

	/// <summary>
	/// Checks if necessary TMP elements exist, and creates them if not.
	/// If partially exist, removes all and recreates.
	/// </summary>
	/// <returns>true if TMP already exists or created successfully</returns>
	bool CheckOrCreateTMP() {
		if (AllTMPExist()) return true;

		RemoveAllUIRoots();

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		if (pstatus == null) return false;

		Transform mvmtRootTrans = pstatus.transform.parent;
		TextMeshPro refTmp = pstatus.m_healthText;

		// Spectate Target Text (floating)
		_specTargetTmp = CreateTMPFrom(refTmp, $"{Plugin.GUID}_SpectateTarget");
		RegisterUIRoot(_specTargetTmp.gameObject);
		_specTargetTmp.rectTransform.SetParent(mvmtRootTrans, false); // set parent to MovementRoot
		_specTargetTmp.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
		_specTargetTmp.rectTransform.anchoredPosition3D = new Vector3(0f, -200f, 0f);
		_specTargetTmp.rectTransform.sizeDelta = new Vector2(300f, 100f);
		_specTargetTmp.alignment = TextAlignmentOptions.Center;
		_specTargetTmp.gameObject.SetActive(false);

		// Spectate Menu
		_menuObj = new GameObject($"{Plugin.GUID}_SpectateMenu");
		RegisterUIRoot(_menuObj);
		RectTransform menuRt = _menuObj.AddComponent<RectTransform>();
		menuRt.SetParent(mvmtRootTrans, false); // set parent to MovementRoot
		menuRt.localScale = Vector3.one;
		menuRt.anchorMin = new Vector2(0.0f, 0.5f);
		menuRt.anchorMax = new Vector2(0.0f, 0.5f);
		menuRt.pivot = new Vector2(0.5f, 0.5f);
		menuRt.anchoredPosition3D = new Vector3(MenuCenterOffsetX, 0f, 0f);

		_menuTitleTmp = CreateTMPFrom(refTmp, $"{Plugin.GUID}_MenuTitle");
		_menuTitleTmp.rectTransform.SetParent(_menuObj.transform, false);
		_menuTitleTmp.rectTransform.pivot = new Vector2(1.0f, 0.0f);
		_menuTitleTmp.rectTransform.anchoredPosition3D = new Vector3(0f, 55f, 0f);
		_menuTitleTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, 45.0f);
		_menuTitleTmp.alignment = TextAlignmentOptions.Right;

		_menuViewModeTmp = CreateTMPFrom(refTmp, $"{Plugin.GUID}_ViewMode");
		_menuViewModeTmp.rectTransform.SetParent(_menuObj.transform, false);
		_menuViewModeTmp.rectTransform.pivot = new Vector2(1.0f, 0.0f);
		_menuViewModeTmp.rectTransform.anchoredPosition3D = new Vector3(0f, 13f, 0f);
		_menuViewModeTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, 40.0f);
		_menuViewModeTmp.alignment = TextAlignmentOptions.Right;

		_menuListObj = new GameObject($"{Plugin.GUID}_SpectateMenuList");
		RectTransform menuListRt = _menuListObj.AddComponent<RectTransform>();
		menuListRt.SetParent(menuRt, false); // set parent to MovementRoot
		menuListRt.localScale = Vector3.one;
		menuListRt.anchorMin = new Vector2(0.5f, 0.5f);
		menuListRt.anchorMax = new Vector2(0.5f, 0.5f);
		menuListRt.pivot = new Vector2(0.5f, 0.5f);
		menuListRt.anchoredPosition3D = Vector3.zero;

		for (int i = 0; i < MaxMenuItems; i++) {
			var optionTmp = CreateTMPFrom(refTmp, $"{Plugin.GUID}_MenuOption_{i}");
			var keybindTmp = CreateTMPFrom(refTmp, $"{Plugin.GUID}_MenuKeybind_{i}");

			optionTmp.rectTransform.SetParent(menuListRt, false);
			optionTmp.rectTransform.pivot = new Vector2(1.0f, 0.5f);
			optionTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, MenuOptionHeight);
			optionTmp.rectTransform.anchoredPosition3D =
				new Vector3(0f, -((i + 0.5f) * MenuItemSpacing), 0f);
			optionTmp.alignment = TextAlignmentOptions.Right;

			keybindTmp.rectTransform.SetParent(menuListRt, false);
			keybindTmp.rectTransform.pivot = new Vector2(0.0f, 0.5f);
			keybindTmp.rectTransform.sizeDelta = new Vector2(MenuElementWidth, MenuKeybindHeight);
			keybindTmp.rectTransform.anchoredPosition3D =
				new Vector3(MenuCenterDivideSpacing, -((i + 0.5f) * MenuItemSpacing), 0f);
			keybindTmp.alignment = TextAlignmentOptions.Left;

			_menuItemsTmp.Add(new ValueTuple<TextMeshPro?, TextMeshPro?>(optionTmp, keybindTmp));
		}

		RefreshUI();

		var bgTrans = mvmtRootTrans.Find("PUI_CommunicationMenu(Clone)/Root/Backround");
		if (bgTrans == null) {
			Logger.Warn($"SpectateUI: Can't find bg transform in CommunicationMenu! Using \"{mvmtRootTrans.name}\"");
			return true;
		}

		var bgObjClone = Instantiate(bgTrans.gameObject);
		bgObjClone.name = $"{Plugin.GUID}_MenuBackground";
		_menuBackground = bgObjClone.GetComponent<SpriteRenderer>();
		_menuBackground.sortingOrder = -20;
		var bgRt = bgObjClone.GetComponent<RectTransform>();
		bgRt.SetParent(_menuObj.transform);
		bgRt.anchoredPosition3D = new Vector3(-25f, -55f, 0f);

		return true;
	}

	internal static TextMeshPro CreateTMPFrom(TextMeshPro from, string name) {
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

	// === UI Update Methods (implementation specific) ===
	// These methods should (only) be called in UpdateUI or its helpers.
	public void UpdatePlayerStatusUI(AgentTarget? target) {
		if (target == null || target.Agent == null) {
			Logger.Error("SpectateUI: Could not update player status text: no target!");
			return;
		}

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		pstatus?.UpdateHealth(target.Health);
		pstatus?.UpdateInfection(target.Infection, 0.0f);
	}

	private void AddMenuItem(eSpectateMenuItem item, bool enableUI = true) {
		if (_menuItemsStr.Count >= MaxMenuItems) {
			Logger.Warn($"SpectateUI: AddMenuItem ({item.ToString()}) but menu is full!");
			return;
		}

		if (_menuItems.TryGetValue(item, out var val)) {
			_menuItemsStr.Add((val.Item1, val.Item2.ToString()));
		} else {
			Logger.Warn("SpectateUI: Tried to add unknown menu item!");
		}

		if (enableUI) {
			EnableUI(eSpectateUIComp.Menu);
		}
	}

	private void UpdateViewMode(bool freecam, bool enableUI = true) {
		string freeTxt = freecam ? $"<#{StateHighlightColor}FF>FREE-LOOK</color>" : "<#FFFFFF60>FREE-LOOK</color>";
		string followTxt = !freecam ? $"<#{StateHighlightColor}FF>FOLLOW</color>" : "<#FFFFFF60>FOLLOW</color>";
		_menuViewModeStr = $"{freeTxt} / {followTxt}";
		if (enableUI) {
			EnableUI(eSpectateUIComp.ViewMode);
		}
	}

	private void UpdateSpectateTargetText(bool enableUI = true) {
		string playerName = SpectateCam.Instance?.Target?.Agent.InteractionName ?? "<#FFDE21>Unknown</color>";
		_specTargetStr = $"<#{SpecTargetTextColor}>Spectating\n<size=36>{playerName}</size></color>";
		if (enableUI) {
			EnableUI(eSpectateUIComp.SpectateTarget);
		}
	}

	// === UI Management ===
	/// <summary>
	/// Sets the current UI state only. Does not update the UI.
	/// </summary>
	/// <param name="state"></param>
	public void SetUIState(eSpectateUIState state) {
		_uiStatePrev = _uiState;
		_uiState = state;
	}

	/// <summary>
	/// Mark the UI as dirty, forcing a refresh on next update.
	/// </summary>
	public void MarkUIDirty() {
		_isUIDirty = true;
	}

	/// <summary>
	/// Refresh the UI based on the current state.
	/// Infers freecam state from SpectateCam.
	/// </summary>
	private void RefreshUI() => RefreshUI(SpectateCam.Instance?.Freecam ?? false);

	/// <summary>
	/// Updates the UI based on specified state.
	/// Renders necessary components and data.
	/// </summary>
	/// <param name="freecam">whether freecam is currently active</param>
	private void RefreshUI(bool freecam) {
		_isUIDirty = false;
		_uiStateRendered = _uiState;

		ClearUI();

		switch (_uiState) {
			case eSpectateUIState.ShowMenu:
				EnableUI(eSpectateUIComp.Title);
				UpdateViewMode(freecam);
				AddMenuItem(eSpectateMenuItem.ExitSpectate);
				AddMenuItem(eSpectateMenuItem.HideMenu);
				AddMenuItem(eSpectateMenuItem.ToggleFreecam);

				AddMenuItem(eSpectateMenuItem.SwitchPlayer);
				AddMenuItem(eSpectateMenuItem.SelectPlayer);
				AddMenuItem(eSpectateMenuItem.AdjustDistance);
				AddMenuItem(eSpectateMenuItem.AdjustOrbitCenterHeight);
				if (freecam) {
					if (ConfigMgr.AutoTransitionToFollowView) {
						AddMenuItem(eSpectateMenuItem.DisableFreecamAutoTransition);
					} else {
						AddMenuItem(eSpectateMenuItem.EnableFreecamAutoTransition);
					}
				} else {
					AddMenuItem(eSpectateMenuItem.AdjustFollowPitch);
				}

				break;
			case eSpectateUIState.HideMenu:
				EnableUI(eSpectateUIComp.Title);
				UpdateViewMode(freecam);
				AddMenuItem(eSpectateMenuItem.ExitSpectate);
				AddMenuItem(eSpectateMenuItem.ShowMenu);
				break;
			case eSpectateUIState.FPDowned:
			case eSpectateUIState.FPNotDowned:
				AddMenuItem(eSpectateMenuItem.EnterSpectate);
				break;
		}

		RenderUI();
	}

	/// <summary>
	/// Clears all UI render content caches and
	/// disables all UI components for next render.
	/// </summary>
	private void ClearUI() {
		_menuViewModeStr = "";
		_menuItemsStr.Clear();

		// Clear menu item states (to display)
		foreach (var e in Enum.GetValues<eSpectateUIComp>()) {
			_uiCompState[e] = false;
		}
	}

	/// <summary>
	/// Removes all registered UI roots.
	/// This should destroy all UI elements created by this manager.
	/// </summary>
	private void RemoveAllUIRoots() {
		foreach (var root in _uiRoots) {
			Destroy(root);
		}

		_specTargetTmp = null;
		_menuObj = null;
		_menuTitleTmp = null;
		_menuViewModeTmp = null;
		_menuListObj = null;
		_menuBackground = null;

		_menuItemsTmp.Clear();
		_uiRoots.Clear();
	}

	/// <summary>
	/// Registers a UI root GameObject for rendering management.
	/// Roots should not have a parent who is also managed by this manager.
	/// </summary>
	/// <param name="root"></param>
	private void RegisterUIRoot(GameObject root) {
		if (!_uiRoots.Contains(root)) {
			_uiRoots.Add(root);
		}
	}

	/// <summary>
	/// Enables a specific UI component for rendering on next render.
	/// </summary>
	/// <param name="comp"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnableUI(eSpectateUIComp comp) {
		_uiCompState[comp] = true;
	}

	/// <summary>
	/// Renders the UI based on the current component states and render data.
	/// </summary>
	private void RenderUI() {
		foreach (var (comp, state) in _uiCompState) {
			switch (comp) {
				case eSpectateUIComp.SpectateTarget:
					if (state) {
						UpdateText(_specTargetTmp, _specTargetStr);
					}

					Util.SetTargetActiveIfDiff(_specTargetTmp, state);
					break;

				case eSpectateUIComp.Title:
					if (state) {
						UpdateText(_menuTitleTmp, _menuTitleStr);
					}

					Util.SetTargetActiveIfDiff(_menuTitleTmp, state);
					break;

				case eSpectateUIComp.ViewMode:
					if (state) {
						UpdateText(_menuViewModeTmp, _menuViewModeStr);
					}

					Util.SetTargetActiveIfDiff(_menuViewModeTmp, state);
					break;

				case eSpectateUIComp.Menu:
					if (state) {
						int menuItemCount = _menuItemsStr.Count;
						for (int i = 0; i < MaxMenuItems; i++) {
							var (optionTmp, keybindTmp) = _menuItemsTmp[i];

							if (i < menuItemCount) {
								var (optionStr, keybindStr) = _menuItemsStr[i];
								UpdateText(optionTmp, $"<allcaps>{optionStr}</allcaps>");
								UpdateText(keybindTmp, $"<color=orange>[{keybindStr}]</color>");
								Util.SetTargetActiveIfDiff(optionTmp, true);
								Util.SetTargetActiveIfDiff(keybindTmp, true);
							} else {
								Util.SetTargetActiveIfDiff(optionTmp, false);
								Util.SetTargetActiveIfDiff(keybindTmp, false);
							}
						}
					}

					Util.SetTargetActiveIfDiff(_menuListObj, state);
					break;
			}
		}
	}

	private void SetUIActive(bool active) {
		if (_wasUIActive == active) return;
		foreach (var root in _uiRoots) {
			Util.SetTargetActiveIfDiff(root, active);
		}

		_wasUIActive = active;
	}

	private void UpdateText(TextMeshPro? tmp, string newText) {
		if (tmp == null) return;
		if (tmp.text == newText) return;
		if (newText != tmp.text) {
			tmp.text = newText;
			ForceTMPUpdate(tmp);
		}
	}

	private void ForceTMPUpdate(TextMeshPro? tmp) {
		if (tmp == null) return;
		tmp.SetAllDirty();
		tmp.ForceMeshUpdate();
	}

	public void WarnFreecamNoAdjustPitch() {
		// TODO: implement warning UI
	}
}
