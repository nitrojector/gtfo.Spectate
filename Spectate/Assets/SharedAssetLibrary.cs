using UnityEngine;

namespace Spectate.Assets;

public static class SharedAssetLibrary {
	private const string AssetBundlePath = "spectate";
	public static Texture2D SpectateIconTexture { get; private set; } = null!;
	public static Sprite SpectateIconSprite { get; private set; } = null!;

	public static void Load() {

		{
			var tex = AssetBundleLoader.LoadAsset<Texture2D>(AssetBundlePath, "Assets/Spectate/eye_icon.png");
			if (tex == null) {
				Logger.Fatal("Spectate icon texture is required but failed to load.");
				return;
			}

			SpectateIconTexture = tex;
			SpectateIconSprite = AssetHelper.CreateSpriteFromTexture2D(SpectateIconTexture);
		}
	}
}
