using Spectate.Config;
using UnityEngine;

namespace Spectate.UI;

public struct MenuKeybindEntry {
	private string _keyName;

	private bool _isAssociated;
	private SpectateInputAction _associatedAction;

	public MenuKeybindEntry(string keyName) {
		_isAssociated = false;
		_associatedAction = SpectateInputAction.None;
		_keyName = keyName;
	}

	public MenuKeybindEntry(SpectateInputAction action) {
		_isAssociated = true;
		_associatedAction = action;
		_keyName = "";
	}

	public override string ToString() {
		if (_isAssociated) {
			KeyCode key = ConfigMgr.GetKeybind(_associatedAction);
			return CellSettingGlobals.GetLocalizedKeyCode(key);
		} else {
			return _keyName;
		}
	}
}
