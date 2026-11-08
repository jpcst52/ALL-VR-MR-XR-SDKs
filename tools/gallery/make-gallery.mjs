// Builds docs/gallery.html: every current screenshot in the repo on one page, grouped by project, newest state of each file.
//   node tools/gallery/make-gallery.mjs
// Images are linked by relative path, so open the page from the repo (docs/gallery.html). Unity pictures are editor desktop renders,
// the VR pictures are an EMULATED Quest 3; nothing here comes from a real headset.
import { readdirSync, statSync, writeFileSync, existsSync } from "node:fs";
import { join, dirname, relative } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const sections = [
  { title: "Unity hub scenes (editor renders, round 7 after, softer light)", note: "Lobby, Hydro, Refinery, Warehouse, Resistor in the hub project.", dir: "docs/images", filter: /^hub-r7-after-/ },
  { title: "Unity 01 Hydrostatic Test Bay", note: "Standalone Vive project.", dir: "vive-steamvr/01-hydrostatic-test/screenshots" },
  { title: "Unity 02 Refinery Rounds", note: "Standalone Vive project (re-baked Nov 6).", dir: "vive-steamvr/02-refinery-rounds/screenshots" },
  { title: "Unity 03 Warehouse Forklift", note: "Standalone Vive project.", dir: "vive-steamvr/03-warehouse-forklift/screenshots" },
  { title: "Unity 04 Resistor Bench", note: "Standalone Vive project.", dir: "vive-steamvr/04-resistor-bench/screenshots" },
  { title: "Circuit Lab and Safety Lab v2 (web, Quest)", note: "Desktop Chrome and an emulated Quest 3.", dir: "multi-player/screenshots", filter: /^v2-.*\.png$/ },
  { title: "Circuit Lab and Safety Lab v1 (web, Quest)", note: "The original labs, kept.", dir: "multi-player/screenshots", filter: /^(?!v2-).*\.png$/ },
  { title: "Fieldwork XR Practice Lab (web)", note: "Three.js, 11 hands-on scenarios.", dir: "training-simulations/screenshots" },
];

const esc = (s) => s.replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
let body = "";
let total = 0;
for (const section of sections) {
  const dir = join(repo, section.dir);
  if (!existsSync(dir)) continue;
  const files = readdirSync(dir).filter((f) => /\.png$/i.test(f) && (!section.filter || section.filter.test(f))).sort();
  if (!files.length) continue;
  total += files.length;
  body += `<section><h2>${esc(section.title)} <small>${files.length} images</small></h2><p>${esc(section.note)}</p><div class="grid">`;
  for (const f of files) {
    const full = join(dir, f);
    const rel = relative(join(repo, "docs"), full).split("\\").join("/");
    const when = statSync(full).mtime.toISOString().slice(0, 16).replace("T", " ");
    body += `<figure><a href="${esc(rel)}"><img loading="lazy" src="${esc(rel)}" alt="${esc(f)}"></a><figcaption>${esc(f)}<br><span>${when}</span></figcaption></figure>`;
  }
  body += "</div></section>";
}

const html = `<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>XR Training Gallery</title>
<style>
:root{--bg:#00205B;--card:#0b2f73;--ink:#fff;--mut:#b9d4f2;--gold:#FFB81C}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.45 Roboto,Segoe UI,system-ui,sans-serif}
header{padding:24px 16px 8px;max-width:1500px;margin:0 auto}
h1{margin:0;font:400 44px "League Gothic",Impact,sans-serif;letter-spacing:.02em}
header p{color:var(--mut);margin:.3em 0}
section{max-width:1500px;margin:0 auto;padding:8px 16px 24px}
h2{color:var(--gold);margin:24px 0 0;font-size:22px} h2 small{color:var(--mut);font-weight:400;font-size:14px;margin-left:8px}
section>p{margin:2px 0 12px;color:var(--mut)}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:12px}
figure{margin:0;background:var(--card);border-radius:8px;overflow:hidden}
img{display:block;width:100%;height:auto;aspect-ratio:16/9;object-fit:cover;background:#061a45}
figcaption{padding:6px 10px;font-size:13px;color:var(--mut)} figcaption span{opacity:.7}
</style></head><body>
<header><h1>XR TRAINING GALLERY</h1>
<p>${total} images as of ${new Date().toISOString().slice(0, 16).replace("T", " ")} UTC. Unity pictures are editor desktop renders; VR pictures are an emulated Quest 3. Nothing is from a real headset.</p></header>
${body}</body></html>`;
let out = "gallery.html";
try { writeFileSync(join(repo, "docs", out), html); } catch (error) {
  if (error.code !== "EBUSY") throw error;
  out = "gallery-latest.html"; // the page is open somewhere and locked
  writeFileSync(join(repo, "docs", out), html);
}
console.log(`docs/${out} written, ${total} images`);
