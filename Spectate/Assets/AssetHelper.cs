using UnityEngine;

namespace Spectate.Assets;

public static class AssetHelper {
	public static Sprite CreateSpriteFromTexture2D(Texture2D texture, float unitSize = 1.0f) {
		return Sprite.Create(
			texture,
			new Rect(0f, 0f, texture.width, texture.height),
			new Vector2(0.5f, 0.5f),
			pixelsPerUnit: texture.width / unitSize
		);
	}
}
