#!/usr/bin/env python3
"""
pack_emojis.py — Pack renamed emoji SVGs into texture atlas maps.

Parameters:
  --width    Number of emojis per row/column (default: 8)
  --size     Pixel size of each emoji cell, excluding padding (default: 120)
  --padding  Per-cell padding in px; glyph gets half on each side (default: 8)
  --svgdir   Directory of renamed SVGs (default: ./emojis)
  --out      Output directory (default: ./atlas)

Atlas size:
  atlas_px = width * (cell_px + padding)

  With the defaults (width=8, size=120, padding=8):
    atlas_px = 8 * (120 + 8) = 1024

Glyph size:
  glyph_px = cell_px - padding   (half_pad clearance on each side within cell)

  With the defaults:
    glyph_px = 120 - 8 = 112
"""

import argparse
import json
import math
import os
import sys

# ---------------------------------------------------------------------------
# Dependency check
# ---------------------------------------------------------------------------
try:
    import cairosvg
    from PIL import Image
    import io
except ImportError:
    sys.exit(
        "Missing dependencies. Install with:\n"
        "  pip install cairosvg Pillow"
    )

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def rasterize_svg(svg_path: str, glyph_px: int) -> Image.Image:
    """Render an SVG to a PIL RGBA Image at exactly glyph_px x glyph_px."""
    png_bytes = cairosvg.svg2png(
        url=svg_path,
        output_width=glyph_px,
        output_height=glyph_px,
    )
    return Image.open(io.BytesIO(png_bytes)).convert("RGBA")


def pack(svg_dir: str, out_dir: str, width: int, cell_px: int, inner_padding: int):
    """
    Pack all SVGs in svg_dir (sorted alphabetically) into square atlas pages.

    Cell layout
    -----------
    Each cell is (cell_px + inner_padding) pixels wide/tall in the atlas.
    The glyph occupies a (cell_px - inner_padding) area, with half_pad
    (= inner_padding // 2) pixels of transparent clearance on every side:

        |<-------- cell_px + inner_padding -------->|
        |  half_pad  |<----- glyph_px ----->|  half_pad  |
        |            | (cell_px - padding)  |            |

    Atlas dimensions
    ----------------
        atlas_px = width * (cell_px + inner_padding)

    UV coordinates
    --------------
    Normalised [0, 1], top-left origin (matches the atlas PNG).
    The companion JSON stores u0/v0 (top-left of glyph) and u1/v1
    (bottom-right of glyph) for each emoji.
    """
    svgs = sorted(
        f for f in os.listdir(svg_dir) if f.lower().endswith(".svg")
    )
    if not svgs:
        sys.exit(f"No SVG files found in {svg_dir!r}")

    os.makedirs(out_dir, exist_ok=True)

    stride   = cell_px + inner_padding          # pixels between cell origins
    half_pad = inner_padding // 2
    glyph_px = cell_px - inner_padding          # half_pad clearance on each side
    atlas_px = width * stride

    if glyph_px <= 0:
        sys.exit(f"padding ({inner_padding}) must be smaller than cell size ({cell_px})")

    emojis_per_page = width * width
    num_pages       = math.ceil(len(svgs) / emojis_per_page)
    global_id       = 0

    for page_idx in range(num_pages):
        atlas     = Image.new("RGBA", (atlas_px, atlas_px), (0, 0, 0, 0))
        page_svgs = svgs[page_idx * emojis_per_page : (page_idx + 1) * emojis_per_page]
        entries   = []

        for local_idx, filename in enumerate(page_svgs):
            name  = os.path.splitext(filename)[0]
            col   = local_idx % width
            row   = local_idx // width

            # Top-left corner of this cell, then nudge inward by half_pad
            cell_x  = col * stride
            cell_y  = row * stride
            paste_x = cell_x + half_pad
            paste_y = cell_y + half_pad

            svg_path = os.path.join(svg_dir, filename)
            try:
                glyph = rasterize_svg(svg_path, glyph_px)
            except Exception as exc:
                print(f"[WARN] Could not render {filename}: {exc}")
                global_id += 1
                continue

            atlas.paste(glyph, (paste_x, paste_y), glyph)

            # UV — normalised, top-left origin
            u0 = paste_x / atlas_px
            v0 = paste_y / atlas_px
            u1 = (paste_x + glyph_px) / atlas_px
            v1 = (paste_y + glyph_px) / atlas_px

            entries.append({
                "id":   global_id,
                "name": name,
                "uv": {
                    "u0": round(u0, 6),
                    "v0": round(v0, 6),
                    "u1": round(u1, 6),
                    "v1": round(v1, 6),
                },
            })
            print(f"[OK] id={global_id:04d}  {name}  ({col},{row}) on page {page_idx}")
            global_id += 1

        # Save atlas PNG
        atlas_name = f"atlas_{page_idx:03d}"
        atlas_path = os.path.join(out_dir, f"{atlas_name}.png")
        atlas.save(atlas_path, "PNG")
        print(f"\n[PAGE {page_idx}] Saved {atlas_path} ({atlas_px}x{atlas_px} px)\n")

        # Save companion JSON
        json_path = os.path.join(out_dir, f"{atlas_name}.json")
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump({
                "page":       page_idx,
                "atlas_size": atlas_px,
                "cell_size":  cell_px,
                "glyph_size": glyph_px,
                "padding":    inner_padding,
                "grid_width": width,
                "emojis":     entries,
            }, f, indent=2, ensure_ascii=False)
        print(f"[PAGE {page_idx}] Saved {json_path}\n")

    print(f"Done — {global_id} emojis packed into {num_pages} atlas page(s).")


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Pack emoji SVGs into texture atlases.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "Atlas size formula:  atlas_px = width * (size + padding)\n"
            "Glyph size formula:  glyph_px = size - padding\n\n"
            "Example -- 1024x1024 atlas:\n"
            "  pack_emojis.py --width 8 --size 120 --padding 8\n"
            "  => 8 * (120 + 8) = 1024,  glyph = 112 px"
        ),
    )
    parser.add_argument("--width",   type=int, default=8,
                        help="Emojis per row/col (default: 8)")
    parser.add_argument("--size",    type=int, default=120,
                        help="Cell size in px, excluding padding (default: 120)")
    parser.add_argument("--padding", type=int, default=8,
                        help="Per-cell padding in px; glyph gets half on each side (default: 8)")
    parser.add_argument("--svgdir",  type=str,
                        default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "emojis"),
                        help="Directory of renamed SVGs (default: ./emojis)")
    parser.add_argument("--out",     type=str,
                        default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "atlas"),
                        help="Output directory (default: ./atlas)")
    args = parser.parse_args()

    atlas_px = args.width * (args.size + args.padding)
    glyph_px = args.size - args.padding
    print(f"Config : width={args.width}, cell={args.size}px, padding={args.padding}px")
    print(f"Atlas  : {atlas_px}x{atlas_px} px")
    print(f"Glyph  : {glyph_px}x{glyph_px} px")
    print(f"SVG dir: {args.svgdir}")
    print(f"Out dir: {args.out}\n")

    pack(
        svg_dir=args.svgdir,
        out_dir=args.out,
        width=args.width,
        cell_px=args.size,
        inner_padding=args.padding,
    )


if __name__ == "__main__":
    main()
