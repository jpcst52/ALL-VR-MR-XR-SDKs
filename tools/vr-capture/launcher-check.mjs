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
  const guideLinks = await page.locator(".tools .group a").count();
  if (guideLinks !== data.guides.flatMap((g) => g.links).length) problems.push(`${vp.width}px: expected ${data.guides.flatMap((g) => g.links).length} guide links, found ${guideLinks}`);
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
  ...data.guides.flatMap((g) => g.links).map((t) => ({ id: t.id, kind: "guide", path: t.url, skip: t.webServed === false })),
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

// 3. every Markdown guide opens in the in-menu viewer: heading shown, no console errors, no unrendered Markdown, links resolve
{
  const { context, page } = await load({ width: 1200, height: 900 });
  const guides = [...new Set([...data.guides.flatMap((g) => g.links), ...data.examples].map((x) => x.url).filter((u) => /\.md$/i.test(u)))];
  const viewRows = [];
  for (const g of guides) {
    const before = problems.length;
    await page.goto(new URL("launcher/doc.html?f=" + encodeURIComponent(g), base).href, { waitUntil: "domcontentloaded" });
    await page.waitForSelector("#doc h1, #doc h2", { timeout: 10000 }).catch(() => {});
    const info = await page.evaluate(() => {
      const d = document.getElementById("doc");
      const bad = [...d.querySelectorAll("a[href]")].map((a) => a.getAttribute("href")).filter((h) => /^(\.\.?\/|[\w.-]+\.md)/.test(h) && !h.startsWith("doc.html"));
      return { h1: !!d.querySelector("h1"), raw: (() => { const c = d.cloneNode(true); c.querySelectorAll("pre, code").forEach((n) => n.remove()); return /^#{1,6} |^```|\*\*\w/m.test(c.innerText); })(), bad: bad.slice(0, 3), overflow: document.documentElement.scrollWidth > innerWidth + 1 };
    });
    if (!info.h1) problems.push(`viewer: ${g} shows no heading`);
    if (info.raw) problems.push(`viewer: ${g} has unrendered Markdown`);
    if (info.bad.length) problems.push(`viewer: ${g} has unrewritten links ${info.bad.join(", ")}`);
    if (info.overflow) problems.push(`viewer: ${g} overflows horizontally`);
    viewRows.push({ guide: g, ok: problems.length === before });
  }
  console.table(viewRows);
  await context.close();
}

// 4. search, keyboard and legend
{
  const { context, page } = await load({ width: 1200, height: 900 });
  const total = data.examples.length;
  const vis = () => page.locator(".tile:not([hidden])").count();
  const guidesVis = () => page.locator("#tools li:not([hidden])").count();
  if (await page.locator(".legend li").count() !== 3) problems.push("search: status legend should list 3 kinds");
  if (await page.locator("#q").isDisabled()) problems.push("search: box is disabled after load");
  await page.keyboard.press("/");
  if (!(await page.evaluate(() => document.activeElement && document.activeElement.id === "q"))) problems.push("search: '/' does not focus the box");
  await page.keyboard.type("forklift");
  const hits = await vis();
  const expected = data.examples.filter((e) => [e.title, e.summary, e.note ?? "", e.status, e.badges.join(" "), e.id].join(" ").toLowerCase().includes("forklift")).length;
  if (hits !== expected || hits === 0 || hits >= total) problems.push(`search: 'forklift' shows ${hits} tiles, expected ${expected} of ${total}`);
  if (!/match/.test(await page.locator("#count").innerText())) problems.push("search: the result count is not announced");
  if (!(await page.locator("#path").isHidden())) problems.push("search: demo path should hide while filtering");
  await page.keyboard.press("ArrowDown");
  if (!(await page.evaluate(() => !!document.activeElement.closest(".tile")))) problems.push("search: ArrowDown does not move into the results");
  await page.keyboard.press("Escape");
  await page.focus("#q");
  await page.keyboard.type("zzzznothing");
  if ((await vis()) !== 0 || (await guidesVis()) !== 0 || !/Nothing matches/.test(await page.locator("#count").innerText())) problems.push("search: a miss should show no tiles and say so");
  await page.keyboard.press("Escape");
  if ((await vis()) !== total || (await page.inputValue("#q")) !== "") problems.push("search: Escape does not restore all tiles");
  await page.fill("#q", "glossary");
  if ((await guidesVis()) < 1) problems.push("search: 'glossary' finds no guide");
  console.log(`search: forklift=${hits}/${total} tiles, glossary guides=${await guidesVis()}`);
  if (shots) {
    await page.fill("#q", "quest");
    await page.screenshot({ path: join(repo, "docs", "images", "launcher-search.png"), fullPage: false });
    console.log("wrote docs/images/launcher-search.png");
  }
  await context.close();
}

await browser.close();
if (problems.length) { console.log("FAIL"); problems.forEach((p) => console.log("  " + p)); failed = 1; } else console.log("ok: launcher loads offline with no console errors, all web-served paths return 200");
process.exit(failed);
