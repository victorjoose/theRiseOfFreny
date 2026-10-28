#!/usr/bin/env python3
"""Checks the .meta files tracked in Assets/.

Fails when:
- an asset or folder is committed without its .meta (Unity would create a new
  GUID on each machine and break references);
- a .meta is committed without its asset (leftover of a delete/rename);
- two .meta files share the same GUID (usually a copied file).

Run from the repo root: python3 .ci/check_meta.py
"""
import re
import subprocess
import sys

files = subprocess.run(["git", "ls-files", "-z", "Assets"], capture_output=True, check=True).stdout.decode().split("\0")
files = [f for f in files if f]
tracked = set(files)

# Folders that hold tracked files (folders need a .meta too).
dirs = set()
for f in files:
    parts = f.split("/")[:-1]
    for i in range(2, len(parts) + 1):
        dirs.add("/".join(parts[:i]))


def hidden(path):
    # Unity ignores names starting with "." or ending in "~" and never makes .meta for them.
    return any(p.startswith(".") or p.endswith("~") for p in path.split("/")[1:])


errors = []
for path in sorted((tracked | dirs)):
    if path.endswith(".meta") or hidden(path):
        continue
    if path + ".meta" not in tracked:
        errors.append(f"sem .meta: {path}")

for meta in sorted(f for f in tracked if f.endswith(".meta")):
    asset = meta[:-5]
    if asset not in tracked and asset not in dirs and not hidden(asset):
        errors.append(f".meta sem arquivo: {meta}")

guids = {}
for meta in (f for f in tracked if f.endswith(".meta")):
    with open(meta, encoding="utf-8", errors="replace") as fh:
        m = re.search(r"^guid: ([0-9a-f]{32})", fh.read(), re.M)
    if m:
        guids.setdefault(m.group(1), []).append(meta)
for guid, metas in sorted(guids.items()):
    if len(metas) > 1:
        errors.append(f"GUID repetido {guid}: " + ", ".join(sorted(metas)))

if errors:
    print("\n".join(errors))
    print(f"\n{len(errors)} problema(s) com .meta. Veja CONTRIBUTING.md.")
    sys.exit(1)
print(f"OK: {len(tracked)} arquivos em Assets/, .meta em ordem.")
