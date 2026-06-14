using Spectate.Config;
using Spectate.Localization;
using UnityEngine;

namespace Spectate.UI;

public struct MenuKeybindEntry {
	public bool IsAssociated => _isAssociated;

	private string _keyLocaleKey;

	private bool _isAssociated;
	private SpectateInputAction _associatedAction;

	public MenuKeybindEntry(string keyLocaleKey) {
		_isAssociated = false;
		_associatedAction = SpectateInputAction.None;
		_keyLocaleKey = keyLocaleKey;
	}

	public MenuKeybindEntry(SpectateInputAction action) {
		_isAssociated = true;
		_associatedAction = action;
		_keyLocaleKey = "";
	}

	public override string ToString() {
		if (_isAssociated) {
			KeyCode key = ConfigMgr.GetKeybind(_associatedAction);
			return CellSettingGlobals.GetLocalizedKeyCode(key);
		} else {
			return Loc.T(_keyLocaleKey);
		}
	}
}
