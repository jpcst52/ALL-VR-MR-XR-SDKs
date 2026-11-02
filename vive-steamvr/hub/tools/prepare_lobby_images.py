"""Prepare the lobby images for the Unity projects (run from anywhere; needs Python 3 with Pillow).

1. Portal thumbnails: downscales the editor-render screenshots of projects 01-04 to 320x180 JPG in
   vive-steamvr/hub/Assets/Scripts/Lobby/Resources/Thumbnails/ (loaded by LobbyHub with Resources.Load).
2. Season skies: copies the 2k equirectangular images listed in assets/skies/skies.json into the kit's
   Runtime/Resources/Skies/<id>.jpg (re-encoded to keep each file modest) and writes Resources/Skies/skies.json
   for SeasonSky.cs. Skipped with a message if the manifest is not there yet.

    python vive-steamvr/hub/tools/prepare_lobby_images.py

To use your own 360 photos instead, see docs/HOW-TO-RUN.md section 3.
"""
import json
import os
import shutil
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
VIVE = os.path.join(ROOT, "vive-steamvr")
THUMBS_OUT = os.path.join(VIVE, "hub", "Assets", "Scripts", "Lobby", "Resources", "Thumbnails")
SKIES_IN = os.path.join(ROOT, "assets", "skies")
SKIES_OUT = os.path.join(VIVE, "packages", "com.pittbradford.xrtraining", "Runtime", "Resources", "Skies")

THUMBS = {
    # Resources name -> source screenshot (editor renders, no university logo image in them)
    "hydrostatic-test": "01-hydrostatic-test/screenshots/01-test-bay.png",
    "refinery-rounds": "02-refinery-rounds/screenshots/07-start-view.png",
    "warehouse-forklift": "03-warehouse-forklift/screenshots/07-start-view.png",
    "resistor-bench": "04-resistor-bench/screenshots/01-bench-overview.png",
}


def thumbnails():
    os.makedirs(THUMBS_OUT, exist_ok=True)
    for name, rel in THUMBS.items():
        src = os.path.join(VIVE, rel)
        if not os.path.exists(src):
            print("missing screenshot, skipped:", rel)
            continue
        im = Image.open(src).convert("RGB")
        # centre-crop to 16:9, then downscale
        w, h = im.size
        tw, th = (w, int(w * 9 / 16)) if w * 9 / 16 <= h else (int(h * 16 / 9), h)
        left, top = (w - tw) // 2, (h - th) // 2
        im = im.crop((left, top, left + tw, top + th)).resize((320, 180), Image.LANCZOS)
        out = os.path.join(THUMBS_OUT, name + ".jpg")
        im.save(out, "JPEG", quality=86, optimize=True)
        print("thumbnail", out, os.path.getsize(out), "bytes")


def skies():
    manifest = os.path.join(SKIES_IN, "skies.json")
    if not os.path.exists(manifest):
        print("assets/skies/skies.json not found yet; skies skipped")
        return
    with open(manifest, encoding="utf-8") as f:
        items = json.load(f)
    os.makedirs(SKIES_OUT, exist_ok=True)
    out_items = []
    for it in items:
        src = os.path.join(SKIES_IN, it.get("file2k") or it.get("file4k"))
        if not os.path.exists(src):
            print("missing sky image, skipped:", src)
            continue
        im = Image.open(src)
        out = os.path.join(SKIES_OUT, it["id"] + ".jpg")
        if im.format == "JPEG" and im.size == (2048, 1024) and os.path.getsize(src) <= 900_000:
            shutil.copyfile(src, out)  # already the right size: copy as is (no second JPEG pass)
        else:
            im = im.convert("RGB").resize((2048, 1024), Image.LANCZOS)
            im.save(out, "JPEG", quality=84, optimize=True, progressive=False)
        print("sky", out, os.path.getsize(out), "bytes")
        out_items.append({"id": it["id"], "label": it.get("label", it["id"].title()),
                          "credit": it.get("credit", ""), "isBradford": bool(it.get("isBradford", False))})
    with open(os.path.join(SKIES_OUT, "skies.json"), "w", encoding="utf-8") as f:
        json.dump(out_items, f, indent=2)
        f.write("\n")
    print("wrote", len(out_items), "seasons to", SKIES_OUT)


if __name__ == "__main__":
    which = sys.argv[1:] or ["thumbnails", "skies"]
    if "thumbnails" in which:
        thumbnails()
    if "skies" in which:
        skies()
