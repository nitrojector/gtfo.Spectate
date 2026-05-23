using Clonesoft.Json;
using UnityEngine;

namespace Spectate.Assets;

public sealed class EmojiUV {
	[JsonProperty("u0")] public float U0;
	[JsonProperty("v0")] public float V0;
	[JsonProperty("u1")] public float U1;
	[JsonProperty("v1")] public float V1;

	public Rect ToRect() => new(U0, 1f - V1, U1 - U0, V1 - V0);
}

public sealed class EmojiEntry {
	[JsonProperty("id")] public int Id;
	[JsonProperty("page")] public int Page;
	[JsonProperty("name")] public string Name = "";
	[JsonProperty("uv")] public EmojiUV UV = new();
}

public sealed class EmojiAtlasPage {
	[JsonProperty("page")] public int Page;
	[JsonProperty("atlas_size")] public int AtlasSize;
	[JsonProperty("cell_size")] public int CellSize;
	[JsonProperty("glyph_size")] public int GlyphSize;
	[JsonProperty("padding")] public int Padding;
	[JsonProperty("grid_width")] public int GridWidth;
	[JsonProperty("emojis")] public List<EmojiEntry> Emojis = new();
}

public sealed class EmojiAtlasManifest {
	[JsonProperty("num_pages")] public int NumPages;
	[JsonProperty("atlas_size")] public int AtlasSize;
	[JsonProperty("cell_size")] public int CellSize;
	[JsonProperty("glyph_size")] public int GlyphSize;
	[JsonProperty("padding")] public int Padding;
	[JsonProperty("grid_width")] public int GridWidth;
	[JsonProperty("total_emojis")] public int TotalEmojis;
	[JsonProperty("pages")] public List<string> Pages = new();
}
