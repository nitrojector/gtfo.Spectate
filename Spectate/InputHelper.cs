using UnityEngine;

namespace Spectate;

public class InputHelper {
	public static int GetAlphaNumKeyDown() {
		int key = -1;
		for (int i = 0; i < 10; i++) {
			if (Input.GetKeyDown(KeyCode.Alpha0 + i)) {
				key = i;
				break;
			}
		}

		return key;
	}

	public static bool OnlyModifies(KeyCode mod, KeyCode altMod = KeyCode.None, KeyCode nonMod = KeyCode.None) {
		bool targetPressed = Input.GetKey(mod) || Input.GetKey(altMod) || Input.GetKeyDown(nonMod);

		bool otherModPressed = false;

		// Shift
		if (mod != KeyCode.LeftShift && mod != KeyCode.RightShift &&
		    altMod != KeyCode.LeftShift && altMod != KeyCode.RightShift) {
			otherModPressed |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		}

		// Control
		if (mod != KeyCode.LeftControl && mod != KeyCode.RightControl &&
		    altMod != KeyCode.LeftControl && altMod != KeyCode.RightControl) {
			otherModPressed |= Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		}

		// Alt
		if (mod != KeyCode.LeftAlt && mod != KeyCode.RightAlt &&
		    altMod != KeyCode.LeftAlt && altMod != KeyCode.RightAlt) {
			otherModPressed |= Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
		}

		return targetPressed && !otherModPressed;
	}
}
