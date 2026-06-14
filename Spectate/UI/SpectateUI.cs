using System.Runtime.CompilerServices;
using CellMenu;
using Player;
using PlayerSync.Sync.Ammo;
using Spectate.Config;
using Spectate.Interop;
using Spectate.Localization;
using Spectate.Patches;
using TMPro;
using UnityEngine;

namespace Spectate.UI;

[RegisterIl2Cpp]
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
	/// The last PlayerAgent target that the UI was rendered for.
	/// </summary>
	private PlayerAgent? _lastRenderedTarget = null;

	/// <summary>
	/// List of root UI GameObjects that can be rendered.
	/// Parents of roots are always external.
	/// </summary>
	private readonly List<GameObject> _uiRoots = new();

	/// <summary>
	/// The state of individual UI components to be rendered on next render.
	/// </summary>
	private readonly Dictionary<eSpectateUIComp, bool> _uiCompState = new();

	/// <summary>
	/// Whether there is a pending request to revert PUI to local player.
	/// Sometimes we want to do so but player is null at the moment.
	/// </summary>
	private bool _wantToRevertPUIStatus = false;

	/// <summary>
	/// Whether we are currently updating PUI_LocalPlayerStatus
	/// </summary>
	public bool InPlayerStatusUpdate { get; private set; } = false;

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

	/// <summary>
	/// Offset of our spectate PUI_Inventory to show header
	/// </summary>
	private const float SpectateInvOffsetY = -20.0f;

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

	private readonly List<ValueTuple<TextMeshPro?, TextMeshPro?>>
		_menuItemsTmp = new(MaxMenuItems); // (option, keybind)

	private SpriteRenderer? _menuBackground;

	// === PUI_Inventory ===
	/// <summary>
	/// NOTE: this is outside of the <see cref="_uiRoots"> system
	/// </summary>
	private PUI_Inventory? _spectateInv;

	public PUI_Inventory? SpectateInventory => _spectateInv;

	// === UI Render Data ===
	private string MenuTitleStr => $"<allcaps>{Loc.T("mod.name")}</allcaps>";
	private string _specTargetStr = "";
	private string _menuViewModeStr = "";

	private readonly List<ValueTuple<string, string>> _menuItemsToRender = new(); // (option, keybind)

	private readonly Dictionary<eSpectateMenuItem, ValueTuple<string, MenuKeybindEntry>> _menuItems = new() {
		[eSpectateMenuItem.ShowMenu] = ("menu.label.showMenu", new(SpectateInputAction.ToggleMenu)),
		[eSpectateMenuItem.HideMenu] = ("menu.label.hideMenu", new(SpectateInputAction.ToggleMenu)),
		[eSpectateMenuItem.EnterSpectate] = ("menu.label.enterSpectate", new(SpectateInputAction.ToggleSpectate)),
		[eSpectateMenuItem.ExitSpectate] = ("menu.label.exitSpectate", new(SpectateInputAction.ToggleSpectate)),
		[eSpectateMenuItem.ToggleFreecam] = ("menu.label.toggleFreecam", new(SpectateInputAction.ToggleFreecam)),
		[eSpectateMenuItem.EnableFreecamAutoTransition] =
			("menu.label.enableAutoFollow", new(SpectateInputAction.ToggleAutoFollow)),
		[eSpectateMenuItem.DisableFreecamAutoTransition] =
			("menu.label.disableAutoFollow", new(SpectateInputAction.ToggleAutoFollow)),
		[eSpectateMenuItem.SwitchPlayer] = ("menu.label.switchPlayer", new("menu.key.switchPlayer")),
		[eSpectateMenuItem.SelectPlayer] = ("menu.label.selectPlayer", new("menu.key.selectPlayer")),
		[eSpectateMenuItem.AdjustDistance] = ("menu.label.distance", new("menu.key.distance")),
		[eSpectateMenuItem.AdjustOrbitCenterHeight] = ("menu.label.orbitCenterHeight", new("menu.key.orbitCenterHeight")),
		[eSpectateMenuItem.AdjustFollowPitch] = ("menu.label.followPitch", new("menu.key.followPitch")),
	};

	/// <summary>
	/// IL2CPP ctor
	/// </summary>
	public SpectateUI(IntPtr ptr) : base(ptr) {
	}

	private void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this);

		Events.OnSessionEnd += Unload;
		Events.OnCheckpointReload += Unload;
	}

	/// <summary>
	/// Unloads the spectate UI so there are no leftover elements.
	/// </summary>
	public void Unload() {
		SetSpectateInventoryActive(false);
		HideNavMarker();
		_uiState = eSpectateUIState.FPNotDowned;
		_uiStatePrev = eSpectateUIState.FPNotDowned;
		_uiStateRendered = eSpectateUIState.ShowMenu;
		_lastRenderedTarget = null;
		_wantToRevertPUIStatus = false;
#if DEBUG
		Logger.Debug("SpectateUI: Unload");
#endif
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
		_navMarker.SetSignInfo($"<#FFF><allcaps>{Loc.T("ui.navMarkerYou")}</allcaps></color>");
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
		if (_wantToRevertPUIStatus) {
			var self = SpectateCam.Instance?.Self;
			if (self != null) {
				UpdatePlayerStatusUI(self);
				_wantToRevertPUIStatus = false;
			}
		}

		var cam = SpectateCam.Instance;
		if (!(SpectateCam.Instance?.CanSpectate ?? false)) {
			SetUIActive(false);
			return;
		}

		if (!CheckOrCreateTMP()) return; // ensure TMP is valid

		ProcessInput();

		bool camActive = SpectateCam.Instance?.Active ?? false;
		if (camActive) {
			UpdatePlayerStatusUI(SpectateCam.Instance!.Target);
			UpdatePlayerInventoryUI(SpectateCam.Instance.Target);
			_lastRenderedTarget = SpectateCam.Instance.Target?.Agent;
		}

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

		SetSpectateInventoryActive(true);
		GuiManager.PlayerLayer.m_playerStatus.gameObject.SetActive(true); // for compat with EOSExt EMP ig...
		if (ConfigMgr.ShowLocalPlayerNavMarker)
			ShowNavMarker();
	}

	public void UpdateForDetach() {
		bool isDowned = SpectateCam.Instance?.Self?.IsDowned ?? false;
		SetUIState(isDowned ? eSpectateUIState.FPDowned : eSpectateUIState.FPNotDowned);
		_wantToRevertPUIStatus = true;
		SetSpectateInventoryActive(false);
		HideNavMarker();
#if DEBUG
		Logger.Debug("SpectateUI: UpdateForDetach");
#endif
	}

	private void ProcessInput() {
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
	/// Replicates existing elements for use
	/// </summary>
	public void ReplicateUI() {
		if (!CheckOrCreateTMP()) {
			Logger.Error("SpectateUI: Failed to create TMP elements for spectate UI!");
		}

		if (!CheckOrCreatePUI_Inventory()) {
			Logger.Error("SpectateUI: Failed to create PUI_Inventory elements for spectate UI!");
		}
	}

	private bool CheckOrCreatePUI_Inventory() {
		var pInv = GuiManager.PlayerLayer?.Inventory;
		if (pInv == null) {
			Logger.Warn("SpectateUI: Failed to find PUI_Inventory for spectate UI replication!");
			return false;
		}

		if (_spectateInv != null) return true;

		_spectateInv = Instantiate(pInv, pInv.transform.parent, false);
		var rt = _spectateInv.RectTrans;
		for (int i = 0; i < rt.childCount; i++) {
			Destroy(rt.GetChild(i).gameObject);
		}

		_spectateInv.Setup(GuiManager.PlayerLayer);
		_spectateInv.name = $"{Plugin.GUID}_SpectateInventory";

		_spectateInv.m_headerRoot = Instantiate(pInv.m_headerRoot, _spectateInv.RectTrans, false);
		_spectateInv.m_headerTxt = _spectateInv.m_headerRoot.GetComponentInChildren<TextMeshPro>();

		var newPos = _spectateInv.RectTrans.localPosition;
		newPos.y += SpectateInvOffsetY;
		_spectateInv.RectTrans.localPosition = newPos;
		_spectateInv.gameObject.SetActive(false);
		return true;
	}

	/// <summary>
	/// Checks if necessary TMP elements exist, and creates them if not.
	/// If partially exist, removes all and recreates.
	/// </summary>
	/// <returns>true if TMP already exists or created successfully</returns>
	private bool CheckOrCreateTMP() {
		if (AllTMPExist()) return true;

		RemoveAllUIRoots();

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		if (pstatus == null) return false;

		Transform? mvmtRootTrans = GuiManager.PlayerLayer?.CustomComponentRoot;
		if (mvmtRootTrans == null) return false;

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

	/// <summary>
	/// Adjust state of discrete inventory UI.
	/// </summary>
	/// <param name="active">whether spectate inventory ui should be active</param>
	public void SetSpectateInventoryActive(bool active) {
		_spectateInv?.SetVisible(active);
		if (active) {
			GuiManager.PlayerLayer.Inventory.SetVisible(false);
		} else {
			GuiManager.PlayerLayer.UpdateGUIElementsVisibility(eFocusState.FPS);
		}
	}

	// === UI Update Methods (implementation specific) ===
	// These methods should (only) be called in UpdateUI or its helpers.
	public void UpdatePlayerStatusUI(AgentTarget? target) {
		if (target == null) {
			Logger.Error("SpectateUI: Could not update player status text: no target!");
			return;
		}

		InPlayerStatusUpdate = true;

		PUI_LocalPlayerStatus? pstatus = GuiManager.PlayerLayer?.m_playerStatus;
		pstatus?.UpdateHealth(target.Health);
		pstatus?.UpdateInfection(target.Infection, target.Agent.InfectionTargetHealthRel);
		pstatus?.m_boosterIconActiveDisplay.UpdateBoosterIconsActiveState(target.SAgent);

		StaminaPatch.RevertStaminaBpmDisplay(pstatus);

		if (_lastRenderedTarget?.Pointer != target.Agent.Pointer) {
			pstatus?.ResetDamageAnimation();
		}

		InPlayerStatusUpdate = false;
	}

	public void UpdatePlayerInventoryUI(AgentTarget? target) {
		if (target == null) {
			Logger.Error("SpectateUI: Could not update player inventory text: no target!");
			return;
		}

		if (_spectateInv == null) {
			if (!CheckOrCreatePUI_Inventory()) {
				Logger.Error("SpectateUI: Could not update player inventory text: failed to create PUI_Inventory!");
				return;
			}

			_spectateInv?.gameObject.SetActive(true);
		}

		if (target.Agent.Owner == null) {
			Logger.Warn("SpectateUI: UpdatePlayerInventoryUI: SNet Owner is null");
			return;
		}

		var tBackpack = target.Backpack;
		var tAmmoStorage = target.AmmoStorage;
		if (tBackpack == null || tAmmoStorage == null) {
			Logger.Warn("SpectateUI: Target backpack/ammostorage is null, cannot update inventory UI!");
			return;
		}

		var activeSlot = target.ActiveItemSlot ?? InventorySlot.None;

		bool reserveHasClip = AmmoSync.ReserveIncludesClip(target.SAgent);

		_spectateInv!.SetHeader(target.Agent.InteractionName, Color.white);

		foreach (var slot in _spectateInv.m_slotGUIOrder) {
			var guiSlot = _spectateInv.m_inventorySlots[slot];
			if (!tBackpack.TryGetBackpackItem(slot, out var item)) {
				guiSlot.SetState(ePUI_InventortyItemState.Empty);
				continue;
			}

			var ammo = tAmmoStorage.GetInventorySlotAmmo(slot);
			var db = item.Instance.ItemDataBlock;

			string archeName = item.Instance.ArchetypeName;
			string modelName = item.Instance.PublicName;

			int clip = (int)tAmmoStorage.GetClipAmmoFromSlot(slot);
			int inPack = ammo.BulletsInPack;
			float inPackRel = (inPack + clip) * ammo.BulletsToRelConv;

			if (reserveHasClip) {
				inPack -= clip;
			}

			guiSlot.SetArchetypeName(archeName);
			guiSlot.SetDetailedName(modelName);
			guiSlot.SetState(slot == activeSlot ? ePUI_InventortyItemState.Selected : ePUI_InventortyItemState.Slim);
			guiSlot.SetAll(clip, inPack, inPackRel);
			if (db != null) {
				guiSlot.SetAllShowFlags(
					target.HasClipData && db.GUIShowAmmoClip,
					db.GUIShowAmmoPack,
					db.GUIShowAmmoTotalRel,
					db.GUIShowAmmoInfinite
				);
			}

			_spectateInv.SetSlotAmmo(slot, clip, inPack, inPackRel);
		}

		// TODO: this method sucks ass. please come up with sth else...
		if (CM_PageMap.TryGetInventoryWithSlotIndex(target.Agent.Owner.PlayerSlotIndex(), out var inv)) {
			_spectateInv.SetFlashLightIcon(inv.m_iconDisplay.FlashLightIcon.Enabled.gameObject.activeSelf);
		}

		_spectateInv.UpdateSlotPositions();
	}

	private void AddMenuItem(eSpectateMenuItem item, bool enableUI = true) {
		if (_menuItemsToRender.Count >= MaxMenuItems) {
			Logger.Warn($"SpectateUI: AddMenuItem ({item.ToString()}) but menu is full!");
			return;
		}

		if (_menuItems.TryGetValue(item, out var val)) {
			var (label, key) = val;
			_menuItemsToRender.Add((Loc.T(label), key.ToString()));
		} else {
			Logger.Warn("SpectateUI: Tried to add unknown menu item!");
		}

		if (enableUI) {
			EnableUI(eSpectateUIComp.Menu);
		}
	}

	private void UpdateViewMode(bool freecam, bool enableUI = true) {
		string freeTxt = $"<allcaps>{Loc.T("menu.cam.freeLook")}</allcaps>";
		string followTxt = $"<allcaps>{Loc.T("menu.cam.follow")}</allcaps>";
		string freeExpr = freecam ? $"<#{StateHighlightColor}FF>{freeTxt}</color>" : $"<#FFFFFF60>{freeTxt}</color>";
		string followExpr = !freecam ? $"<#{StateHighlightColor}FF>{followTxt}</color>" : $"<#FFFFFF60>{followTxt}</color>";
		_menuViewModeStr = $"{freeExpr} / {followExpr}";
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
		_menuItemsToRender.Clear();

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
	/// Registers a UI root GameObject for rendering management. Disabling the root after.
	/// Roots should not have a parent who is also managed by this manager.
	/// </summary>
	/// <param name="root"></param>
	private void RegisterUIRoot(GameObject root) {
		if (!_uiRoots.Contains(root)) {
			_uiRoots.Add(root);
			root.gameObject.SetActive(false);
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
						UpdateText(_menuTitleTmp, MenuTitleStr);
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
						int itemsToRenderCount = _menuItemsToRender.Count;
						for (int i = 0; i < MaxMenuItems; i++) {
							var (optionTmp, keybindTmp) = _menuItemsTmp[i];

							if (i < itemsToRenderCount) {
								var (optionStr, keybindStr) = _menuItemsToRender[i];
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
