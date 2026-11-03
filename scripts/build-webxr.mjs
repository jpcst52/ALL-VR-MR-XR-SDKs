// Assembles the static WebXR site into site/ (git-ignored) for GitHub Pages. No dependencies.
//   node scripts/build-webxr.mjs                 build site/
//   node scripts/build-webxr.mjs --with-decks    also copy the four .pptx decks (about 90 MB; off by default)
//   node scripts/build-webxr.mjs --out <dir>     write somewhere else
//
// What goes in: the launcher and every browser example, vendor/ and assets/, the multi-player PAGES (not the relay
// server, tests or node_modules), the Markdown guides the launcher opens, and the images those guides and tiles use.
// What stays out: vive-steamvr (Unity source), tools, branding, assets-private, tests, node_modules, .git.
// Everything is served from a sub-path (https://<user>.github.io/<repo>/), so all links must be relative.
import { cpSync, existsSync, mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
import { dirname, extname, join, posix, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const args = process.argv.slice(2);
const withDecks = args.includes("--with-decks");
const outArg = args.indexOf("--out");
const site = resolve(outArg >= 0 ? args[outArg + 1] : join(repo, "site"));
const PAGES_URL = "https://jpcst52.github.io/ALL-VR-MR-XR-SDKs/";
const IMAGE_CAP = 700_000; // bytes; larger images referenced from guides are skipped and listed
// Guides of the browser examples keep their pictures up to 3 MB; every other guide (the Unity project notes, docs) up to 700 KB.
const capFor = (md, target) => (/^(README\.md|training-simulations\/|a-frame\/|threejs\/|babylonjs\/|webxr\/|multi-player\/)/.test(md) ? 3_000_000 : IMAGE_CAP);
const IMG = /\.(png|jpe?g|gif|svg|webp)$/i;

const NEVER = /(^|\/)(node_modules|\.git|\.certs|branding|assets-private|Library|Temp|test|tests|tools|vive-steamvr)(\/|$)/;
const SECRET = /(^|\/)(node_modules|\.git|\.certs|branding|assets-private|Library|Temp)(\/|$)/;
// Guides (Markdown) and their pictures may come from vive-steamvr/ and tools/ even though those folders' code is left out.
const docOk = (r) => !SECRET.test(r) && /\.(md|png|jpe?g|gif|svg|webp)$/i.test(r) && !/(^|\/)(Library|Packages|ProjectSettings|Assets)(\/|$)/.test(r.replace(/^vive-steamvr\/[^/]+\//, "")) ;
const skipName = /^(package(-lock)?\.json|server\.mjs|tls\.mjs|\.DS_Store|Thumbs\.db)$/;

const rel = (p) => relative(repo, p).split(sep).join("/");
const copied = new Set(); // repo-relative files in the site

function copyFile(r) {
  const from = join(repo, r), to = join(site, r);
  if (!existsSync(from) || !statSync(from).isFile()) return false;
  if (copied.has(r)) return true;
  mkdirSync(dirname(to), { recursive: true });
  cpSync(from, to);
  copied.add(r);
  return true;
}

// Copy a directory tree. `skip(relPath, isDir)` returns true to leave an entry out.
function copyTree(dir, skip = () => false) {
  const base = join(repo, dir);
  (function walk(d) {
    for (const e of readdirSync(d, { withFileTypes: true })) {
      const full = join(d, e.name), r = rel(full);
      if (NEVER.test(r) || skipName.test(e.name) || skip(r, e.isDirectory())) continue;
      if (e.isDirectory()) walk(full); else copyFile(r);
    }
  })(base);
}

rmSync(site, { recursive: true, force: true });
mkdirSync(site, { recursive: true });

// 1. the pages
copyFile("index.html");
copyFile("pages.html"); // flat list of every page and the other Pages sites
copyFile("branding/logo.png"); // the one branding file the owner approved for publishing (2026-11-02); the rest of branding/ stays out
for (const dir of ["launcher", "brand-kit", "vendor", "assets", "a-frame", "threejs", "babylonjs", "webxr", "demo-ui"]) {
  if (existsSync(join(repo, dir))) copyTree(dir, (r, isDir) => isDir && /\/screenshots$/.test(r)); // screenshots are added when referenced
}
copyTree("training-simulations", (r, isDir) => isDir && /\/screenshots$/.test(r));
// multi-player: pages and shared code only
copyTree("multi-player", (r, isDir) => {
  if (isDir) return /^multi-player\/(tools|test|screenshots)$/.test(r);
  return /^multi-player\/[^/]+\.(mjs|json)$/.test(r) || /\.pptx$/.test(r) || /\/(shots|prepare-.*)\.mjs$/.test(r);
});
for (const f of ["README.md", "SDK-list.md", "CREDITS.md", "CREDITS-web-demos.md"]) copyFile(f);
// the in-site QR page (generated below) and the docs
copyTree("docs", (r, isDir) => isDir ? /^docs\/images$/.test(r) : !/\.md$/i.test(r)); // docs: markdown only; images are added when referenced
copyFile("docs/images/pages-qr.png");

// 2. the launcher's own files: every url and screenshot, and (in Markdown) every guide
const data = JSON.parse(readFileSync(join(repo, "launcher", "examples.json"), "utf8"));
const wanted = [];
for (const e of data.examples) { wanted.push(e.url); if (e.screenshot) wanted.push(e.screenshot); }
for (const g of data.guides) for (const l of g.links) wanted.push(l.url);
const missingFromRepo = [];
let droppedDecks = [];
for (const u of wanted) {
  const p = u.replace(/[?#].*$/, "");
  if (/\.pptx$/i.test(p) && !withDecks) { droppedDecks.push(p); continue; }
  const full = join(repo, p);
  if (!existsSync(full)) { missingFromRepo.push(p); continue; }
  if (statSync(full).isDirectory()) continue; // pages are already copied
  const r = rel(full);
  if (!(docOk(r) || !NEVER.test(r))) { missingFromRepo.push(p + " (excluded folder)"); continue; }
  copyFile(r);
}
if (withDecks) for (const d of ["Project-Overview.pptx", "Run-the-XR-Examples.pptx", "Repository-Guide.pptx", "multi-player/Multiplayer-WebXR-Examples.pptx"]) copyFile(d);

// 3. follow relative links in every Markdown file in the site: more guides (text) and the images they show
const skippedBig = [], brokenLinks = [];
const queue = [...copied].filter((f) => /\.md$/i.test(f));
const seenMd = new Set(queue);
for (let i = 0; i < queue.length; i++) {
  const md = queue[i];
  let fence = false;
  for (const raw of readFileSync(join(repo, md), "utf8").split(/\r?\n/)) {
    if (/^```/.test(raw)) { fence = !fence; continue; }
    if (fence) continue;
    for (const m of raw.replace(/`[^`]*`/g, "").matchAll(/!?\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g)) {
      let href = m[1];
      if (/^[a-z][a-z0-9+.-]*:/i.test(href) || href.startsWith("#")) continue;
      href = decodeURIComponent(href.replace(/[?#].*$/, ""));
      const target = posix.normalize(posix.join(posix.dirname(md), href));
      if (target.startsWith("..") || !(docOk(target) || !NEVER.test(target))) continue;
      const full = join(repo, target);
      if (!existsSync(full) || !statSync(full).isFile()) { brokenLinks.push(`${md} -> ${target}`); continue; }
      if (/\.md$/i.test(target)) { if (!seenMd.has(target)) { seenMd.add(target); queue.push(target); copyFile(target); } }
      else if (IMG.test(target)) { if (statSync(full).size > capFor(md, target)) skippedBig.push(`${target} (${(statSync(full).size / 1e6).toFixed(1)} MB)`); else copyFile(target); }
    }
  }
}
// screenshots named in the launcher or used by tiles are already copied; the example pages' own screenshots too
for (const e of data.examples) if (e.screenshot) copyFile(e.screenshot);

// 4. files that exist only in the site
writeFileSync(join(site, ".nojekyll"), "");
writeFileSync(join(site, "404.html"), `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Page not found</title>
<style>body{font-family:Roboto,system-ui,sans-serif;background:#DBEEFF;color:#00205B;margin:0}main{max-width:40rem;margin:12vh auto;padding:0 16px}
h1{color:#003594}a{color:#003594}.bar{border-top:4px solid #FFB81C;padding-top:12px}</style></head>
<body><main class="bar"><h1>That page is not here</h1>
<p>This is the static copy of the XR training examples. Some items in the repository (the Unity projects, the multiplayer relay, the decks) are not part of it.</p>
<p><a id="home" href="./">Go to the main menu</a></p></main>
<script>
// Works from the repository sub-path (/<repo>/...) and from a root host: go to the first path segment on github.io, else the root.
(function(){var h=location.hostname.slice(-10)==="github.io";var seg=location.pathname.split("/")[1];
document.getElementById("home").href=(h&&seg)?"/"+seg+"/":"/";})();
</script></body></html>
`);
writeFileSync(join(site, "qr.html"), `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>QR code for the XR examples</title>
<style>:root{--royal:#003594;--gold:#FFB81C;--blue:#00205B;--sky:#DBEEFF}
body{margin:0;font-family:Roboto,system-ui,sans-serif;background:var(--sky);color:var(--blue)}
header{background:var(--royal);color:#fff;padding:16px;border-bottom:4px solid var(--gold)}h1{margin:0;font-size:1.5rem}
main{max-width:34rem;margin:0 auto;padding:16px}.card{background:#fff;border-radius:8px;padding:16px;box-shadow:0 1px 3px rgba(0,0,0,.2)}
.card svg{width:100%;height:auto;display:block}code{word-break:break-all}label{display:block;margin:12px 0 4px;font-weight:700}
input{width:100%;box-sizing:border-box;padding:8px;font:inherit;border:1px solid var(--blue);border-radius:4px}a{color:var(--royal)}</style></head>
<body><header><h1>Scan to open the XR examples</h1></header>
<main><div class="card" id="qr" role="img" aria-label="QR code for the address below"></div>
<p><code id="addr"></code></p>
<label for="u">Address to encode</label><input id="u" type="url" spellcheck="false">
<p>Scan with the headset's or phone's camera, or type the address in its browser. Open the <a href="./">main menu</a>. The code is drawn on this page with a bundled library, so it also works offline. Not tested on any headset.</p></main>
<script src="vendor/qrcode/qrcode.js"></script>
<script>
(function(){var def=${JSON.stringify(PAGES_URL)};
var here=location.protocol.slice(0,4)==="http"?new URL("./",location.href).href:def;
var u=document.getElementById("u"),box=document.getElementById("qr"),a=document.getElementById("addr");
u.value=location.hostname.slice(-10)==="github.io"?here:def;
function draw(){var q=qrcode(0,"M");q.addData(u.value||def);q.make();box.innerHTML=q.createSvgTag({cellSize:6,margin:4,scalable:true});a.textContent=u.value||def;}
u.addEventListener("input",draw);draw();})();
</script></body></html>
`);

// 5. when decks are left out, the site copy of the launcher list drops those links (the Pages guide says why)
if (!withDecks && droppedDecks.length) {
  const p = join(site, "launcher", "examples.json");
  const d = JSON.parse(readFileSync(p, "utf8"));
  for (const g of d.guides) g.links = g.links.filter((l) => !/\.pptx$/i.test(l.url));
  d.guides = d.guides.filter((g) => g.links.length);
  writeFileSync(p, JSON.stringify(d, null, 2));
}

// 6. report
const all = [];
(function walk(d) { for (const e of readdirSync(d, { withFileTypes: true })) { const f = join(d, e.name); if (e.isDirectory()) walk(f); else all.push([f, statSync(f).size]); } })(site);
const total = all.reduce((s, [, n]) => s + n, 0);
all.sort((a, b) => b[1] - a[1]);
console.log(`site: ${site}`);
console.log(`files: ${all.length}  total: ${(total / 1e6).toFixed(1)} MB (GitHub Pages limit 1 GB; aim under 150 MB)`);
console.log("largest files:");
for (const [f, n] of all.slice(0, 8)) console.log(`  ${(n / 1e6).toFixed(2).padStart(6)} MB  ${rel(f).replace(rel(site) + "/", "")}`);
if (droppedDecks.length) console.log(`decks left out (use --with-decks): ${droppedDecks.join(", ")}`);
if (skippedBig.length) console.log(`images over ${IMAGE_CAP / 1e6} MB left out: ${skippedBig.join("; ")}`);
if (missingFromRepo.length) console.log(`launcher entries not in the site: ${missingFromRepo.join(", ")}`);
if (brokenLinks.length) console.log(`links in guides that do not resolve in the repo (docs-check covers these): ${brokenLinks.length}`);
if (total > 150e6) console.log("WARNING: over 150 MB");
process.exit(missingFromRepo.length ? 1 : 0);
