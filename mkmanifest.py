#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

CONF_NAME = "manifest.conf"
OUT_NAME = "manifest.json"

VERSION_RE = re.compile(r'\bstring\s+VERSION\s*=\s*"([^"]+)"', re.IGNORECASE)


def eprint(*args, **kwargs):
    print(*args, file=sys.stderr, **kwargs)


def parse_conf(conf_text: str) -> dict[str, str]:
    """
      - ignores blank lines
      - ignores comments starting with '#' or ';'
      - keeps the last value if a key repeats
    """
    out: dict[str, str] = {}
    for raw in conf_text.splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or line.startswith(";"):
            continue
        if "=" not in line:
            eprint(f"Warning: skipping malformed line in {CONF_NAME!r}: {raw!r}")
            continue
        k, v = line.split("=", 1)
        out[k.strip()] = v.strip()
    return out


def split_deps(dep_str: str | None) -> list[str]:
    if not dep_str:
        return []
    items = []
    seen = set()
    for part in dep_str.split(","):
        d = part.strip()
        if d and d not in seen:
            items.append(d)
            seen.add(d)
    return items


def extract_version_from_cs(cs_text: str) -> str:
    m = VERSION_RE.search(cs_text)
    if not m:
        raise ValueError('Could not find a line like:  string VERSION = "1.2.3";')
    return m.group(1)


def main() -> int:
    cwd = Path.cwd()
    conf_path = cwd / CONF_NAME
    if not conf_path.is_file():
        eprint(f"Error: {CONF_NAME} not found in {cwd}")
        return 1

    try:
        conf_text = conf_path.read_text(encoding="utf-8")
    except Exception as ex:
        eprint(f"Error reading {CONF_NAME}: {ex}")
        return 1

    conf = parse_conf(conf_text)

    name = conf.get("name")
    url = conf.get("url")
    desc = conf.get("desc")
    plugfile = conf.get("plugfile")
    deps = split_deps(conf.get("dep"))

    missing = [k for k, v in [("name", name), ("url", url), ("desc", desc), ("plugfile", plugfile)] if not v]
    if missing:
        eprint(f"Error: missing required key(s) in {CONF_NAME}: {', '.join(missing)}")
        return 1

    plug_path = (cwd / plugfile).resolve()
    if not plug_path.is_file():
        eprint(f"Error: plugfile path not found: {plug_path}")
        return 1

    try:
        cs_text = plug_path.read_text(encoding="utf-8")
    except Exception as ex:
        eprint(f"Error reading plugfile {plug_path}: {ex}")
        return 1

    try:
        version = extract_version_from_cs(cs_text)
    except Exception as ex:
        eprint(f"Error: {ex}")
        return 1

    manifest = {
        "name": name,
        "version_number": version,
        "website_url": url,
        "description": desc,
        "dependencies": deps,
    }

    out_path = cwd / OUT_NAME
    try:
        out_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    except Exception as ex:
        eprint(f"Error writing {OUT_NAME}: {ex}")
        return 1

    print(f"Wrote {OUT_NAME} with version {version}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
