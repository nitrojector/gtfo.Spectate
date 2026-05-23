#!/usr/bin/env python3
import json
import os
import shutil

base_dir   = os.path.dirname(os.path.abspath(__file__))
json_path  = os.path.join(base_dir, "emojis.json")
input_dir  = os.path.join(base_dir, "emojis-hash")
output_dir = os.path.join(base_dir, "emojis")

os.makedirs(output_dir, exist_ok=True)

with open(json_path, "r", encoding="utf-8") as f:
    emojis = json.load(f)

renamed = 0
skipped = 0

for name, data in emojis.items():
    src        = data.get("src", "")
    hash_filename = os.path.basename(src)
    src_path   = os.path.join(input_dir, hash_filename)
    dst_path   = os.path.join(output_dir, f"{name}.svg")

    if not os.path.exists(src_path):
        print(f"[SKIP] Missing: {hash_filename} (for '{name}')")
        skipped += 1
        continue

    if os.path.exists(dst_path):
        print(f"[SKIP] Already exists: {name}.svg")
        skipped += 1
        continue

    shutil.copy2(src_path, dst_path)
    print(f"[OK]   {hash_filename} -> {name}.svg")
    renamed += 1

print(f"\nDone: {renamed} renamed, {skipped} skipped.")
