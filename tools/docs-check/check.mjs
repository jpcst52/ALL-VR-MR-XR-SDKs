// Documentation check (no network). Run: npm run docs-check   (from the repository root)
//  1. every relative Markdown link and image in every Markdown file (tracked or new) resolves to a file, and #anchors
//     exist (heading slugs use the same rule as launcher/doc.js)
//  2. every url / screenshot / guide in launcher/examples.json exists
//  3. docs/README.md links every .md under docs/ (the launcher listing is checked as a warning)
//  4. the "N pass" counts quoted in docs/TESTING.md and docs/HOW-TO-RUN.md match a fresh run of the unit suites
//     (skip the run with --no-run)
import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve, posix } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const problems = [];
const warnings = [];
const files = execFileSync("git", ["ls-files", "-z", "--cached", "--others", "--exclude-standard"], { cwd: repo, encoding: "utf8" }).split("\0").filter(Boolean);
const mds = files.filter((f) => /\.md$/i.test(f) && !/(^|\/)LICENSE\.md$/i.test(f) && existsSync(join(repo, f)));
const exists = (p) => existsSync(join(repo, p));

// same rule as launcher/doc.js (including the -1, -2 suffix for repeated headings)
const slug = (s) => s.toLowerCase().replace(/<[^>]+>/g, "").replace(/[^\w\- ]+/g, "").trim().replace(/ /g, "-");
const anchorCache = new Map();
function anchors(file) {
  if (anchorCache.has(file)) return anchorCache.get(file);
  const set = new Set(), used = {};
  let fence = false;
  for (const line of readFileSync(join(repo, file), "utf8").split(/\r?\n/)) {
    if (/^```/.test(line)) { fence = !fence; continue; }
    if (fence) continue;
    const m = /^(#{1,6})\s+(.*?)\s*#*$/.exec(line);
    if (!m) continue;
    let id = slug(m[2]);
    if (used[id]) id += "-" + used[id]++; else used[id] = 1;
    set.add(id);
  }
  anchorCache.set(file, set);
  return set;
}

let linkCount = 0;
for (const f of mds) {
  let fence = false;
  readFileSync(join(repo, f), "utf8").split(/\r?\n/).forEach((raw, i) => {
    if (/^```/.test(raw)) { fence = !fence; return; }
    if (fence) return;
    const line = raw.replace(/`[^`]*`/g, "");
    for (const m of line.matchAll(/!?\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g)) {
      let href = m[1];
      if (/^[a-z][a-z0-9+.-]*:/i.test(href)) continue; // external or mailto
      linkCount++;
      let hash = "";
      const h = href.indexOf("#");
      if (h >= 0) { hash = href.slice(h + 1); href = href.slice(0, h); }
      let target = f;
      if (href) {
        target = posix.normalize(posix.join(posix.dirname(f), decodeURIComponent(href)));
        if (!exists(target)) { problems.push(`${f}:${i + 1}: broken link ${m[1]} (no ${target})`); continue; }
      }
      if (hash && /\.md$/i.test(target) && statSync(join(repo, target)).isFile()) {
        if (!anchors(target).has(decodeURIComponent(hash).toLowerCase())) problems.push(`${f}:${i + 1}: missing anchor #${hash} in ${target}`);
      }
    }
  });
}

// launcher data
const data = JSON.parse(readFileSync(join(repo, "launcher", "examples.json"), "utf8"));
const refs = [];
for (const e of data.examples) {
  refs.push([`example ${e.id} url`, e.url, e.webServed === false && !/\.[a-z0-9]+$/i.test(e.url)]);
  if (e.screenshot) refs.push([`example ${e.id} screenshot`, e.screenshot]);
}
for (const g of data.guides) for (const t of g.links) refs.push([`guide ${t.id}`, t.url]);
for (const [what, p, optional] of refs) {
  const clean = p.split("#")[0].split("?")[0];
  if (!exists(clean) && !optional) problems.push(`launcher/examples.json: ${what} -> ${p} does not exist`);
  else if (p.includes("#") && /\.md$/i.test(clean) && exists(clean) && !anchors(clean).has(p.split("#")[1])) problems.push(`launcher/examples.json: ${what} -> missing anchor in ${p}`);
}
for (const s of data.demoPath) if (s.ref && !data.examples.some((e) => e.id === s.ref)) problems.push(`launcher/examples.json: demo step ${s.step} refers to unknown example ${s.ref}`);

// docs index completeness
const index = readFileSync(join(repo, "docs", "README.md"), "utf8");
const docsMds = mds.filter((f) => /^docs\/.*\.md$/i.test(f) && f !== "docs/README.md");
for (const f of docsMds) {
  const rel = relative("docs", f).replace(/\\/g, "/");
  if (!index.includes("](" + rel + ")") && !index.includes("](" + rel + "#")) problems.push(`docs/README.md does not link ${f}`);
}
const launcherUrls = new Set(refs.map((r) => r[1].split("#")[0]));
for (const f of docsMds) if (!launcherUrls.has(f)) warnings.push(`launcher does not list ${f}`);

// quoted test counts versus a fresh run
const quotedIn = [["docs/TESTING.md", readFileSync(join(repo, "docs", "TESTING.md"), "utf8")], ["docs/HOW-TO-RUN.md", readFileSync(join(repo, "docs", "HOW-TO-RUN.md"), "utf8")]];
const suites = [["Practice Lab", "training-simulations", ["--test", "test/*.test.mjs"]], ["Brand kit", "brand-kit", ["--test", "test/*.test.mjs"]], ["Multiplayer relay", "multi-player", ["--test", "test/*.test.mjs"]]];
let suiteCount = 0;
if (!process.argv.includes("--no-run")) {
  for (const [name, dir, args] of suites) {
    let out = "";
    try { out = execFileSync("node", args, { cwd: join(repo, dir), encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] }); }
    catch (e) { out = String(e.stdout || ""); problems.push(`${name}: the suite fails`); }
    const pass = /# pass (\d+)/.exec(out)?.[1];
    if (!pass) { problems.push(`${name}: could not read the pass count`); continue; }
    suiteCount++;
    for (const [file, text] of quotedIn) {
      const row = text.split("\n").find((l) => l.startsWith("|") && l.includes(name));
      const q = row && /\|\s*(\d+) pass\s*\|?\s*$/.exec(row)?.[1];
      if (q && q !== pass) problems.push(`${file}: quotes ${q} passing for ${name}, the suite now has ${pass}`);
    }
  }
}

console.log(`docs-check: ${mds.length} Markdown files, ${linkCount} relative links/images, ${refs.length} launcher references, ${docsMds.length} docs guides indexed, ${suiteCount} suites run for counts`);
warnings.forEach((w) => console.log("  warning: " + w));
if (problems.length) { console.log(`FAIL (${problems.length})`); problems.forEach((p) => console.log("  " + p)); process.exit(1); }
console.log("ok: all links, anchors, launcher paths and the docs index resolve");
