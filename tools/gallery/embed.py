"""Turns docs/gallery.html into one self-contained file (docs/gallery-standalone.html): every image is downscaled to
1280 px wide, saved as JPEG and inlined as base64, so the page works when emailed or copied without the repo.
    node tools/gallery/make-gallery.mjs
    python tools/gallery/embed.py [docs/gallery.html]
Unity pictures are editor desktop renders; VR pictures are an EMULATED Quest 3. Nothing is from a real headset."""
import base64, io, re, sys
from pathlib import Path
from PIL import Image

repo = Path(__file__).resolve().parents[2]
src = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else repo / "docs" / "gallery.html"
html = src.read_text(encoding="utf-8")

def inline(match):
    rel = match.group(1)
    path = (src.parent / rel).resolve()
    with Image.open(path) as im:
        im = im.convert("RGB")
        if im.width > 1280:
            im = im.resize((1280, round(im.height * 1280 / im.width)), Image.LANCZOS)
        buf = io.BytesIO()
        im.save(buf, "JPEG", quality=80, optimize=True)
    return 'src="data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode() + '"'

# the full-size link around each picture is dropped: the page has to stand alone
html = re.sub(r'<a href="[^"]*">(<img [^>]*>)</a>', r"\1", html)
html = re.sub(r'src="([^"]+\.png)"', inline, html)
html = html.replace('loading="lazy" ', "")
out = src.with_name("gallery-standalone.html")
out.write_text(html, encoding="utf-8")
print(f"{out.relative_to(repo)} written, {out.stat().st_size / 1e6:.1f} MB")
