// Checks the launcher home page: loads it with all non-localhost network blocked, fails on console
// errors, and verifies every url and screenshot in launcher/examples.json answers HTTP 200.
// Usage: serve the repo root first (python -m http.server 8101), then
//   node tools/vr-capture/launcher-check.mjs            (LAUNCHER_URL overrides the base)
//   node tools/vr-capture/launcher-check.mjs --shots    (also writes docs/images/launcher*.png)
import { chromium } from "playwright";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.LAUNCHER_URL ?? "http://localhost:8101/").replace(/\/?$/, "/");
const shots = process.argv.includes("--shots");
const data = JSON.parse(readFileSync(join(repo, "launcher", "examples.json"), "utf8"));

const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
let failed = 0;
const problems = [];

async function load(viewport) {
  const context = await browser.newContext({ viewport, reducedMotion: "reduce" });
  const page = await context.newPage();
  await page.route("**/*", (route) => {
    const host = new URL(route.request().url()).hostname;
    if (host === "localhost" || host === "127.0.0.1") return route.continue();
    problems.push(`blocked external request: ${route.request().url()}`);
    return route.abort();
  });
  page.on("pageerror", (e) => problems.push(`page error: ${e.message}`));
  page.on("console", (m) => {
    // branding/logo.png is intentionally uncommitted; the page falls back to a text wordmark without it
    if (m.type() === "error" && !/branding\/logo\.png|logo\.png/.test(m.text()) && !/status of 404/.test(m.text())) problems.push(`console error: ${m.text()}`);
  });
  page.on("response", (r) => { if (r.status() >= 400 && !/favicon|branding\/logo\.png/.test(r.url())) problems.push(`HTTP ${r.status()}: ${r.url()}`); });
  await page.goto(base, { waitUntil: "networkidle" });
  await page.waitForSelector(".tile");
  return { context, page };
}

// 1. page load at desktop and phone widths
for (const vp of [{ width: 1600, height: 1000 }, { width: 420, height: 900 }]) {
  const { context, page } = await load(vp);
  const tiles = await page.locator(".tile").count();
  const fonts = await page.evaluate(async () => { await document.fonts.ready; return ["League Gothic", "Merriweather", "Roboto"].map((f) => document.fonts.check(`16px "${f}"`)); });
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1);
  if (tiles !== data.examples.length) problems.push(`${vp.width}px: expected ${data.examples.length} tiles, found ${tiles}`);
  if (fonts.includes(false)) problems.push(`${vp.width}px: a brand font failed to load`);
  if (overflow) problems.push(`${vp.width}px: horizontal overflow`);
  console.log(`page ${vp.width}x${vp.height}: tiles=${tiles} fonts=${fonts.join("/")} overflow=${overflow}`);
  if (shots) {
    const name = vp.width > 800 ? "launcher.png" : "launcher-narrow.png";
    await page.screenshot({ path: join(repo, "docs", "images", name), fullPage: true });
    console.log(`wrote docs/images/${name}`);
  }
  await context.close();
}

// 2. every url and screenshot in the data file
const rows = [];
const targets = [
  ...data.examples.map((e) => ({ id: e.id, kind: "url", path: e.url, skip: e.webServed === false })),
  ...data.examples.filter((e) => e.screenshot).map((e) => ({ id: e.id, kind: "image", path: e.screenshot, skip: false })),
  ...data.tools.map((t) => ({ id: t.id, kind: "tool", path: t.url, skip: t.webServed === false })),
  ...data.demoPath.filter((s) => s.ref).map((s) => ({ id: `step ${s.step}`, kind: "step ref", path: data.examples.find((e) => e.id === s.ref)?.url ?? null, skip: data.examples.find((e) => e.id === s.ref)?.webServed === false })),
];
for (const t of targets) {
  if (t.skip) { rows.push({ id: t.id, kind: t.kind, path: t.path, result: "skipped (not web-served)" }); continue; }
  if (!t.path) { rows.push({ id: t.id, kind: t.kind, path: String(t.path), result: "MISSING" }); problems.push(`${t.id}: unknown reference`); continue; }
  const res = await fetch(new URL(t.path, base));
  rows.push({ id: t.id, kind: t.kind, path: t.path, result: String(res.status) });
  if (res.status !== 200) problems.push(`${t.id}: ${t.path} returned ${res.status}`);
}
console.table(rows);

await browser.close();
if (problems.length) { console.log("FAIL"); problems.forEach((p) => console.log("  " + p)); failed = 1; } else console.log("ok: launcher loads offline with no console errors, all web-served paths return 200");
process.exit(failed);
