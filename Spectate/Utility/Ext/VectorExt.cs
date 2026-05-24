using UnityEngine;

namespace Spectate.Utility.Ext;

public static class VectorExt {
	public static Vector2 ToVector2(this float val) {
		return new Vector2(val, val);
	}

	public static Vector3 ToVector3(this float val) {
		return new Vector3(val, val, val);
	}
}
