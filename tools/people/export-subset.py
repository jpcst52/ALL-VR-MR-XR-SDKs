"""Copies a small, shrunk subset of the Microsoft Rocketbox library (MIT) from the hub project into a stand-alone Unity project,
so the project shows realistic people without the 1.1 GB library. Textures are resized to 1024 px (the same cap the hub imports at);
specular maps are dropped (the Standard shader does not use them here). Only the clips named below are copied.
    python tools/people/export-subset.py 03-warehouse-forklift Professions/Construction_Male_02 Professions/Construction_Male_01
The result is committed under <project>/Assets/Resources/Rocketbox (Resources so the scripts can load it at runtime) (LICENSE.md is copied beside it). Credit: Gonzalez-Franco et al. 2020,
doi 10.3389/frvir.2020.561558."""
import shutil, sys
from pathlib import Path
from PIL import Image

repo = Path(__file__).resolve().parents[2]
hub = repo / "vive-steamvr" / "hub" / "Assets" / "Rocketbox"
project, avatars = sys.argv[1], sys.argv[2:]
dest = repo / "vive-steamvr" / project / "Assets" / "Resources" / "Rocketbox"

# (folder, file) pairs: the idle and walk clips for the male rig
clips = [("all_animations_max_motextr_static", "m_idle_neutral_01.max.fbx"), ("all_animations_max_motextr_xy", "m_walk_neutral.max.fbx")]

total = 0
def put(src, dst):
    global total
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(src, dst)
    total += dst.stat().st_size

for a in avatars:
    name = a.split("/")[-1]
    put(hub / "Avatars" / a / "Export" / f"{name}.fbx", dest / "Avatars" / a / "Export" / f"{name}.fbx")
    for tga in sorted((hub / "Avatars" / a / "Textures").glob("*.tga")):
        if "specular" in tga.name:
            continue
        out = dest / "Avatars" / a / "Textures" / (tga.stem + ".png")
        out.parent.mkdir(parents=True, exist_ok=True)
        with Image.open(tga) as im:
            if im.width > 1024:
                im = im.resize((1024, round(im.height * 1024 / im.width)), Image.LANCZOS)
            im.save(out, optimize=True)
        total += out.stat().st_size
for folder, f in clips:
    put(hub / "Animations" / folder / f, dest / "Animations" / folder / f)
put(hub / "LICENSE.md", dest / "LICENSE.md")
print(f"{dest.relative_to(repo)}: {total / 1e6:.1f} MB")
