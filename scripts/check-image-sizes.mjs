// Fails if any picture in the repo is over the GitHub Pages limit used by scripts/build-webxr.mjs (3 MB for the browser
// examples and the root README), and warns above 2.5 MB. Run before committing screenshots:  node scripts/check-image-sizes.mjs
// To shrink a PNG: python -c "from PIL import Image; f='x.png'; Image.open(f).convert('RGB').quantize(256,dither=Image.Dither.NONE).save(f,optimize=True)"
import { readdirSync, statSync } from "node:fs";
import { join, relative, resolve } from "node:path";

const repo = resolve(new URL("..", import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));
const LIMIT = 3_000_000, WARN = 2_500_000;
const SKIP = new Set(["node_modules", ".git", "builds", "site", "Library", "Temp", "Logs", "branding", "assets-private", ".certs", "vive-steamvr"]);
const IMG = /\.(png|jpe?g|gif|webp)$/i;
const bad = [], warn = [];

(function walk(d) {
  for (const e of readdirSync(d, { withFileTypes: true })) {
    if (e.isDirectory()) { if (!SKIP.has(e.name)) walk(join(d, e.name)); continue; }
    if (!IMG.test(e.name)) continue;
    const f = join(d, e.name), n = statSync(f).size;
    if (n > LIMIT) bad.push([f, n]); else if (n > WARN) warn.push([f, n]);
  }
})(repo);

const show = (l) => l.map(([f, n]) => `  ${(n / 1e6).toFixed(2)} MB  ${relative(repo, f).replace(/\\/g, "/")}`).join("\n");
if (warn.length) console.log(`Close to the 3 MB limit:\n${show(warn)}`);
if (bad.length) { console.error(`Over the 3 MB limit (the Pages build drops these and the link check fails):\n${show(bad)}`); process.exit(1); }
console.log("All images are under 3 MB.");
