// Checks the built site/ as GitHub Pages will serve it: under the sub-path /xr-training-lab/, with every
// non-localhost request blocked. Starts its own server on 8151 and stops it at the end.
//   node scripts/build-webxr.mjs && node tools/pages/site-check.mjs
// Checks: every web page in launcher/examples.json loads with no console error, failed request or HTTP error; every launcher
// tile, screenshot and guide link resolves; the Markdown viewer opens a guide; qr.html draws; 404.html is served for a bad
// path; and, with the host pretended to be jpcst52.github.io, the relay pages and the launcher say plainly there is no relay.
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { startSubpathServer } from "./serve-subpath.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const { chromium } = await import(pathToFileURL(join(repo, "tools", "vr-capture", "node_modules", "playwright", "index.mjs")).href);
if (!existsSync(join(repo, "site", "index.html"))) { console.error("site/ is missing: run node scripts/build-webxr.mjs first"); process.exit(2); }

const PORT = 8151, PREFIX = "/xr-training-lab/";
const base = `http://localhost:${PORT}${PREFIX}`;
const PAGES_ORIGIN = "https://jpcst52.github.io";
const data = JSON.parse(readFileSync(join(repo, "site", "launcher", "examples.json"), "utf8"));
const server = await startSubpathServer({ port: PORT, prefix: PREFIX });
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
let failures = 0;
const optional = (u) => /favicon|\/branding\/|\/healthz$/.test(u);

async function open(url, { asPages = false, viewport = { width: 1280, height: 720 } } = {}) {
  const context = await browser.newContext({ viewport, reducedMotion: "reduce" });
  const page = await context.newPage();
  const problems = [];
  await page.route("**/*", async (route) => {
    const u = new URL(route.request().url());
    if (u.hostname === "localhost" || u.hostname === "127.0.0.1") return route.continue();
    if (asPages && u.origin === PAGES_ORIGIN) { // pretend the local server is github.io (same sub-path)
      const r = await fetch(`http://localhost:${PORT}${u.pathname}${u.search}`);
      return route.fulfill({ status: r.status, headers: { "content-type": r.headers.get("content-type") ?? "application/octet-stream" }, body: Buffer.from(await r.arrayBuffer()) });
    }
    problems.push(`blocked external request: ${u.href}`);
    return route.abort();
  });
  page.on("pageerror", (e) => problems.push(`page error: ${e.message}`));
  page.on("console", (m) => { if (m.type() === "error" && !/status of 404/.test(m.text())) problems.push(`console error: ${m.text()}`); });
  page.on("requestfailed", (r) => { if (!optional(r.url()) && !/ERR_ABORTED/.test(r.failure()?.errorText ?? "")) problems.push(`failed: ${r.url()}`); });
  page.on("response", (r) => { if (r.status() >= 400 && !optional(r.url())) problems.push(`HTTP ${r.status()}: ${r.url()}`); });
  await page.goto(url, { waitUntil: "load" });
  return { page, context, problems };
}
function report(label, problems, extra = "") {
  console.log(`${problems.length ? "FAIL" : "ok  "} ${label}${extra ? "  " + extra : ""}${problems.length ? "\n   " + [...new Set(problems)].join("\n   ") : ""}`);
  if (problems.length) failures++;
}
const fetchOk = async (u) => { const r = await fetch(u); return r.status === 200; };

try {
  // 1. every web page in the launcher (urls that are not Markdown or decks)
  const pageUrls = [...new Set(data.examples.map((e) => e.url).filter((u) => !/\.(md|pptx)$/i.test(u)))];
  for (const u of [...pageUrls, "qr.html", "pages.html"]) {
    const { page, context, problems } = await open(base + u);
    await page.waitForTimeout(u.startsWith("multi-player") || u.startsWith("training") ? 3500 : 2000);
    const canvases = await page.locator("canvas").count();
    report(base + u, problems, `canvases=${canvases}`);
    await context.close();
  }

  // 2. launcher: tiles, screenshots, guide links
  {
    const { page, context, problems } = await open(base);
    await page.waitForSelector(".tile");
    const tiles = await page.locator(".tile").count();
    // sections marked "subpage" are on launcher/section.html, linked from the bottom of the home page
    const sub = data.sections.filter((s) => s.subpage).map((s) => s.id);
    const want = data.examples.filter((e) => !sub.includes(e.section)).length;
    if (tiles !== want) problems.push(`expected ${want} tiles, found ${tiles}`);
    const hrefs = await page.$$eval(".tile, #tools .group a, #more a", (as) => as.map((a) => a.href));
    for (const id of sub) {
      await page.goto(`${base}launcher/section.html?s=${id}`, { waitUntil: "load" });
      await page.waitForSelector(".tile");
      hrefs.push(...await page.$$eval(".tile", (as) => as.map((a) => a.href)));
    }
    await page.goto(base, { waitUntil: "load" });
    await page.waitForSelector(".tile");
    const imgs = await page.$$eval(".tile img", (is) => is.map((i) => i.src));
    for (const h of [...new Set([...hrefs, ...imgs])]) if (!(await fetchOk(h))) problems.push(`does not resolve: ${h}`);
    // the Markdown files the viewer fetches
    for (const h of hrefs) {
      const f = new URL(h).searchParams.get("f");
      if (f && !(await fetchOk(base + f))) problems.push(`guide file missing: ${f}`);
    }
    if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1)) problems.push("horizontal overflow");
    report("launcher tiles and guides", problems, `tiles=${tiles} links=${hrefs.length}`);
    await context.close();
  }

  // 3. Markdown viewer opens a guide (and one with an image)
  for (const f of ["docs/GITHUB-PAGES.md", "docs/README.md", "README.md"]) {
    const { page, context, problems } = await open(`${base}launcher/doc.html?f=${encodeURIComponent(f)}`);
    await page.waitForSelector("h1", { timeout: 8000 }).catch(() => problems.push("no heading rendered"));
    const broken = await page.$$eval("img", (is) => is.filter((i) => i.complete && i.naturalWidth === 0).map((i) => i.src));
    for (const b of broken) problems.push(`broken image: ${b}`);
    const h1 = await page.locator("h1").first().textContent().catch(() => "");
    report(`viewer ${f}`, problems, `h1="${(h1 ?? "").trim().slice(0, 50)}"`);
    await context.close();
  }

  // 4. 404.html is served for a bad path, with status 404
  { const r = await fetch(base + "no-such-page.html"); const t = await r.text(); const ok = r.status === 404 && /not here/.test(t); report("404.html for a bad path", ok ? [] : [`status ${r.status}`]); }
  // an absolute /path outside the prefix must not work (it would on a root host but not on Pages)
  { const r = await fetch(`http://localhost:${PORT}/launcher/launcher.js`); report("absolute path outside the sub-path is 404 (as on Pages)", r.status === 404 ? [] : [`status ${r.status}`]); }

  // 5. as github.io: the launcher and the relay pages say there is no relay
  {
    const { page, context, problems } = await open(`${PAGES_ORIGIN}${PREFIX}`, { asPages: true });
    await page.waitForSelector(".tile");
    const note = await page.locator("#relay-note").textContent();
    if (!/GitHub Pages/.test(note) || await page.locator("#relay-note").isHidden()) problems.push("launcher shows no GitHub Pages relay note");
    report("launcher as github.io", problems);
    await context.close();
  }
  for (const u of ["multi-player/vr/", "multi-player/ar/", "multi-player/hydrotest/"]) {
    const { page, context, problems } = await open(`${PAGES_ORIGIN}${PREFIX}${u}`, { asPages: true });
    await page.waitForTimeout(2500);
    const txt = await page.locator("body").innerText();
    if (!/no shared-room relay here/.test(txt)) problems.push("no 'no relay' note on the page");
    report(`${u} as github.io`, problems);
    await context.close();
  }
} finally {
  await browser.close();
  server.close();
}
console.log(failures ? `\n${failures} check(s) FAILED` : "\nsite check passed");
process.exit(failures ? 1 : 0);
