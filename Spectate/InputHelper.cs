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
}
