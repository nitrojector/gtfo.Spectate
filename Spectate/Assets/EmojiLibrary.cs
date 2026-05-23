using Clonesoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Spectate.Assets;

public static class EmojiLibrary {
	// -------------------------------------------------------------------------
	// Paths
	// -------------------------------------------------------------------------

	private const string BundlePath = "assets/emojis";
	private const string ManifestPath = "Assets/Emojis/manifest.json"; // asset path inside bundle
	private const string PagePathFmt = "Assets/Emojis/atlas_{0:000}"; // {0} = page index

	// -------------------------------------------------------------------------
	// State
	// -------------------------------------------------------------------------

	private static EmojiAtlasManifest? _manifest;
	private static readonly Dictionary<int, Texture2D> _textures = new();
	private static readonly Dictionary<int, EmojiAtlasPage> _pages = new();
	private static readonly Dictionary<string, EmojiEntry> _byName = new();
	private static readonly Dictionary<int, EmojiEntry> _byId = new();

	public static bool IsLoaded { get; private set; }

	// -------------------------------------------------------------------------
	// Loading
	// -------------------------------------------------------------------------

	/// <summary>
	/// Loads the manifest and all atlas pages + textures from the bundle.
	/// Call once at startup (e.g. from your Plugin.Load or a scene init).
	/// </summary>
	public static void Load() {
		if (IsLoaded) return;

		// Manifest
		TextAsset? manifestAsset = AssetBundleLoader.LoadAsset<TextAsset>(BundlePath, ManifestPath);
		if (manifestAsset == null) {
			Logger.Error("[EmojiLibrary] Failed to load manifest.");
			return;
		}

		_manifest = JsonConvert.DeserializeObject<EmojiAtlasManifest>(manifestAsset.text);
		if (_manifest == null) {
			Logger.Error("[EmojiLibrary] Failed to deserialize manifest.");
			return;
		}

		// Pages
		for (int i = 0; i < _manifest.NumPages; i++) {
			string jsonPath = string.Format(PagePathFmt, i) + ".json";
			string texturePath = string.Format(PagePathFmt, i) + ".png";

			TextAsset? pageAsset = AssetBundleLoader.LoadAsset<TextAsset>(BundlePath, jsonPath);
			Texture2D? texture = AssetBundleLoader.LoadAsset<Texture2D>(BundlePath, texturePath);

			if (pageAsset == null || texture == null) {
				Logger.Error($"[EmojiLibrary] Failed to load page {i}.");
				continue;
			}

			EmojiAtlasPage? page = JsonConvert.DeserializeObject<EmojiAtlasPage>(pageAsset.text);
			if (page == null) continue;

			_textures[i] = texture;
			_pages[i] = page;

			foreach (EmojiEntry entry in page.Emojis) {
				_byName[entry.Name] = entry;
				_byId[entry.Id] = entry;
			}
		}

		IsLoaded = true;
		Logger.Debug($"[EmojiLibrary] Loaded {_byId.Count} emojis across {_manifest.NumPages} atlas page(s).");
	}

	public static void Unload() {
		AssetBundleLoader.Unload(BundlePath);
		_textures.Clear();
		_pages.Clear();
		_byName.Clear();
		_byId.Clear();
		_manifest = null;
		IsLoaded = false;
	}

	// -------------------------------------------------------------------------
	// Lookup
	// -------------------------------------------------------------------------

	public static EmojiEntry? GetByName(string name)
		=> _byName.TryGetValue(name, out EmojiEntry? e) ? e : null;

	public static EmojiEntry? GetById(int id)
		=> _byId.TryGetValue(id, out EmojiEntry? e) ? e : null;

	/// <summary>Returns the atlas texture for the page that contains this entry.</summary>
	public static Texture2D? GetTexture(EmojiEntry entry)
		=> _textures.TryGetValue(entry.Page, out Texture2D? t) ? t : null;

	// -------------------------------------------------------------------------
	// Sprite
	// -------------------------------------------------------------------------

	/// <summary>
	/// Creates a <see cref="Sprite"/> cropped to this emoji's UV region.
	/// The sprite can be assigned to an Image component directly.
	/// </summary>
	public static Sprite? CreateSprite(EmojiEntry entry) {
		if (!_textures.TryGetValue(entry.Page, out Texture2D? tex)) return null;

		Rect pixelRect = UVToPixelRect(entry.UV, tex.width, tex.height);
		return Sprite.Create(tex, pixelRect, new Vector2(0.5f, 0.5f));
	}

	public static Sprite? CreateSprite(string name) {
		EmojiEntry? entry = GetByName(name);
		return entry != null ? CreateSprite(entry) : null;
	}

	public static Sprite? CreateSprite(int id) {
		EmojiEntry? entry = GetById(id);
		return entry != null ? CreateSprite(entry) : null;
	}

	// -------------------------------------------------------------------------
	// RawImage
	// -------------------------------------------------------------------------

	/// <summary>
	/// Applies this emoji to an existing <see cref="RawImage"/>, setting its
	/// texture and <see cref="RawImage.uvRect"/> so only the correct cell shows.
	/// </summary>
	public static bool ApplyToRawImage(EmojiEntry entry, RawImage target) {
		if (!_textures.TryGetValue(entry.Page, out Texture2D? tex)) return false;

		target.texture = tex;
		target.uvRect = entry.UV.ToRect();
		return true;
	}

	public static bool ApplyToRawImage(string name, RawImage target) {
		EmojiEntry? entry = GetByName(name);
		return entry != null && ApplyToRawImage(entry, target);
	}

	public static bool ApplyToRawImage(int id, RawImage target) {
		EmojiEntry? entry = GetById(id);
		return entry != null && ApplyToRawImage(entry, target);
	}

	/// <summary>
	/// Instantiates a new <see cref="GameObject"/> with a <see cref="RawImage"/>
	/// already configured for this emoji, parented to <paramref name="parent"/>.
	/// </summary>
	public static RawImage? CreateRawImage(EmojiEntry entry, Transform parent, Vector2 size = default) {
		if (!_textures.TryGetValue(entry.Page, out Texture2D? tex)) return null;

		if (size == default) size = new Vector2(64f, 64f);

		GameObject go = new GameObject(entry.Name);
		go.transform.SetParent(parent, false);

		RectTransform rt = go.AddComponent<RectTransform>();
		rt.sizeDelta = size;

		RawImage img = go.AddComponent<RawImage>();
		img.texture = tex;
		img.uvRect = entry.UV.ToRect();

		return img;
	}

	public static RawImage? CreateRawImage(string name, Transform parent, Vector2 size = default) {
		EmojiEntry? entry = GetByName(name);
		return entry != null ? CreateRawImage(entry, parent, size) : null;
	}

	public static RawImage? CreateRawImage(int id, Transform parent, Vector2 size = default) {
		EmojiEntry? entry = GetById(id);
		return entry != null ? CreateRawImage(entry, parent, size) : null;
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Converts normalised UV coords (top-left origin, as written by the packer)
	/// to a pixel <see cref="Rect"/> in Unity's bottom-left convention.
	/// </summary>
	private static Rect UVToPixelRect(EmojiUV uv, int texWidth, int texHeight) {
		float x = uv.U0 * texWidth;
		float y = (1f - uv.V1) * texHeight; // flip V, use V1 as new bottom
		float w = (uv.U1 - uv.U0) * texWidth;
		float h = (uv.V1 - uv.V0) * texHeight;
		return new Rect(x, y, w, h);
	}
}
