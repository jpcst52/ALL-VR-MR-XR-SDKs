// Professional-look checks for the web pages (launcher, Practice Lab, multiplayer), at desktop and phone widths,
// with the git-ignored logo both blocked (as in a fresh clone) and allowed (as on the lab PC).
//
// Checks: no external requests, no page errors or console errors, no broken <img>, the Pitt-Bradford + CIST brand
// bar is present and settled (logo or wordmark, never a broken image), the in-world brand plaque is drawn, no
// horizontal overflow, brand fonts loaded, the panel stays inside the viewport, and buttons are at least 36 px tall.
// Practice Lab pages also check that the seasonal sky loaded, and the catalog that every card has its thumbnail.
//
// Usage: serve the repository root from the relay (it serves the site and the WebSocket on one port), e.g.
//   cd multi-player && set PORT=8132 && node server.mjs        (or any static server + relay on the same origin)
//   node tools/vr-capture/ui-check.mjs                         UI_BASE overrides http://localhost:8132/
//   node tools/vr-capture/ui-check.mjs --shots <dir>           also writes screenshots to <dir>
//   node tools/vr-capture/ui-check.mjs --only lab              run only pages whose name contains "lab"
// Screenshots taken with the logo blocked are named *.png; with the logo allowed, *-logo.png.
import { chromium } from "playwright";
import { existsSync, mkdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.UI_BASE ?? "http://localhost:8132/").replace(/\/?$/, "/");
const argValue = (flag) => { const i = process.argv.indexOf(flag); return i > 0 ? process.argv[i + 1] : null; };
const shotsDir = argValue("--shots") ? resolve(argValue("--shots")) : null;
const only = argValue("--only");
if (shotsDir) mkdirSync(shotsDir, { recursive: true });
const logoOnDisk = existsSync(join(repo, "branding", "logo.png"));
const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);

const viewports = { desktop: { width: 1440, height: 900 }, phone: { width: 390, height: 844 } };
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const results = [];
let room = 0;

// Order in which a step's required hotspots can be done (resolving `after`).
function requiredOrder(step) {
  const required = step.hotspots.filter((h) => h.correct);
  const done = [];
  while (done.length < required.length) {
    const next = required.find((h) => !done.includes(h.id) && (h.after ?? []).every((id) => done.includes(id)));
    done.push(next.id);
  }
  return done;
}

async function labStart(page, id) {
  await page.goto(`${base}training-simulations/`);
  await page.waitForTimeout(1500);
  await page.fill("#learner-name", "Alex");
  await page.click("#profile button[type=submit]");
  if (id) {
    await page.click(`[data-scenario="${id}"]`);
    await page.waitForTimeout(800);
  }
}

async function labDoStep(page, step) {
  await page.evaluate(() => { document.querySelector("#task-actions-wrap").open = true; });
  for (const id of requiredOrder(step)) await page.click(`#task-actions [data-hotspot="${id}"]`);
  await page.evaluate(() => { document.querySelector("#task-actions-wrap").open = false; document.querySelector("#panel").scrollTop = 0; });
}

async function joinRoom(page, path) {
  await page.goto(`${base}${path}`);
  await page.waitForTimeout(800);
  await page.fill("#name", "Alex");
  await page.fill("#room", `ui-check-${Date.now() % 100000}-${room++}`);
  await page.click("#join");
  await page.waitForSelector("#workspace:not([hidden])");
  await page.click("#load-demo");
  await page.waitForTimeout(1200);
}

const pages = [
  { name: "launcher", three: false, run: async (p) => { await p.goto(base); await p.waitForSelector(".tile"); } },
  { name: "lab-welcome", three: true, run: async (p) => { await p.goto(`${base}training-simulations/`); } },
  { name: "lab-catalog", three: true, run: (p) => labStart(p, null) },
  { name: "lab-task", three: true, run: (p) => labStart(p, "heat-exchanger-hydrotest") },
  {
    name: "lab-feedback", three: true, run: async (p) => {
      await labStart(p, "work-at-height");
      const scenario = scenarios.find((s) => s.id === "work-at-height");
      await labDoStep(p, scenario.steps[0]);
    },
  },
  {
    // "Why this matters" panel after the first correct action of the hydrostatic test.
    name: "lab-why", three: true, run: async (p) => {
      await labStart(p, "heat-exchanger-hydrotest");
      await p.evaluate(() => { document.querySelector("#task-actions-wrap").open = true; });
      await p.click('#task-actions [data-hotspot="pack"]');
      await p.evaluate(() => { document.querySelector("#task-actions-wrap").open = false; });
      await p.waitForSelector("#why-panel:not([hidden])");
      const text = await p.textContent("#why-panel");
      if (!/Why this matters/i.test(text)) throw new Error("why panel has no heading");
      await p.keyboard.press("Escape");
      await p.waitForSelector("#why-panel", { state: "hidden" });
      await p.evaluate(() => { document.querySelector("#task-actions-wrap").open = true; });
      await p.click('#task-actions [data-hotspot="blindA"]');
      await p.evaluate(() => { document.querySelector("#task-actions-wrap").open = false; });
      await p.waitForSelector("#why-panel:not([hidden])");
    },
  },
  {
    // The reasoning question after the last task, answered with the best option.
    name: "lab-rationale", three: true, run: async (p) => {
      await labStart(p, "heat-exchanger-hydrotest");
      const scenario = scenarios.find((s) => s.id === "heat-exchanger-hydrotest");
      for (const step of scenario.steps) { await labDoStep(p, step); await p.click("#continue"); await p.waitForTimeout(300); }
      const best = scenario.rationale.options.findIndex((o) => o.best);
      await p.click(`#choices [data-choice="${best}"]`);
      await p.waitForSelector("#rationale-review:not([hidden])");
    },
  },
  {
    name: "lab-complete", three: true, run: async (p) => {
      await labStart(p, "electrical-work");
      const scenario = scenarios.find((s) => s.id === "electrical-work");
      for (const step of scenario.steps) { await labDoStep(p, step); await p.click("#continue"); await p.waitForTimeout(300); }
      if (scenario.rationale) { await p.click('#choices [data-choice="0"]'); await p.click("#continue"); await p.waitForTimeout(300); }
      await p.waitForSelector("#reasoning-result:not([hidden])");
    },
  },
  { name: "lab-circulation", three: true, run: (p) => labStart(p, "circulation-builder") },
  { name: "mp-circuit-lab", three: true, run: (p) => joinRoom(p, "multi-player/vr/") },
  { name: "mp-site-safety", three: true, run: (p) => joinRoom(p, "multi-player/ar/") },
  { name: "mp-hydrotest", three: true, run: (p) => joinRoom(p, "multi-player/hydrotest/") },
].filter((p) => !only || p.name.includes(only));

async function check(def, vpName, logo) {
  const viewport = viewports[vpName];
  const context = await browser.newContext({ viewport, reducedMotion: "reduce", deviceScaleFactor: 1 });
  const page = await context.newPage();
  const problems = [];
  await page.route("**/*", (route) => {
    const url = new URL(route.request().url());
    if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") {
      problems.push(`external request: ${url.href}`);
      return route.abort();
    }
    if (logo === "blocked" && url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "not here" });
    return route.continue();
  });
  page.on("pageerror", (e) => problems.push(`page error: ${e.message}`));
  page.on("console", (m) => {
    if (m.type() !== "error") return;
    if (logo === "blocked" && /404/.test(m.text())) return; // the expected missing-logo request
    problems.push(`console error: ${m.text()}`);
  });
  page.on("response", (r) => {
    if (r.status() >= 400 && !/favicon/.test(r.url()) && !(logo === "blocked" && r.url().includes("/branding/"))) problems.push(`HTTP ${r.status()}: ${r.url()}`);
  });
  try {
    await def.run(page);
    await page.waitForTimeout(def.three ? 2200 : 900);
  } catch (error) {
    problems.push(`script: ${error.message.split("\n")[0]}`);
  }
  const facts = await page.evaluate(async () => {
    await document.fonts.ready;
    const brand = document.querySelector("[data-pitt-brand]");
    const box = brand?.getBoundingClientRect();
    const panel = document.querySelector(".panel");
    const pr = panel?.getBoundingClientRect();
    const visible = (el) => { const r = el.getBoundingClientRect(); const s = getComputedStyle(el); return r.width > 0 && r.height > 0 && s.visibility !== "hidden" && s.display !== "none"; };
    return {
      brand: Boolean(brand && box.width > 0 && box.height > 0),
      brandText: brand?.textContent ?? "",
      brandLogo: brand?.dataset.logo ?? "missing",
      logoImgs: document.querySelectorAll(".pitt-brand__logo").length,
      broken: [...document.images].filter((img) => visible(img) && img.complete && img.naturalWidth === 0).map((img) => img.src),
      overflow: document.documentElement.scrollWidth > innerWidth + 1,
      fonts: ["Roboto", "League Gothic"].every((f) => document.fonts.check(`16px "${f}"`)),
      brand3d: document.documentElement.dataset.brand3d ?? null,
      seasonSky: document.documentElement.dataset.seasonSky ?? null,
      thumbs: [...document.querySelectorAll(".scenario-thumb img")].filter((img) => img.complete && img.naturalWidth > 0).length,
      emptyThumbs: document.querySelectorAll(".scenario-thumb.is-empty").length,
      panelOut: pr ? (pr.left < -1 || pr.right > innerWidth + 1) : false,
      smallButtons: [...document.querySelectorAll("button")].filter((b) => visible(b) && b.getBoundingClientRect().height < 36 && !b.closest("details:not([open])")).map((b) => `${b.textContent.trim().slice(0, 24)} (${Math.round(b.getBoundingClientRect().height)}px)`),
    };
  });
  if (!facts.brand) problems.push("brand bar missing or not visible");
  if (!/CIST/.test(facts.brandText)) problems.push("brand bar does not name CIST");
  if (facts.brandLogo === "pending" || facts.brandLogo === "missing") problems.push(`brand logo state: ${facts.brandLogo}`);
  if (logo === "blocked" && (facts.brandLogo !== "off" || facts.logoImgs)) problems.push("logo blocked but an <img> was shown");
  if (logo === "allowed" && logoOnDisk && facts.brandLogo !== "on") problems.push("logo file present but not shown");
  if (facts.broken.length) problems.push(`broken images: ${facts.broken.join(", ")}`);
  if (facts.overflow) problems.push("horizontal overflow");
  if (!facts.fonts) problems.push("brand fonts not loaded");
  if (facts.panelOut) problems.push("panel extends outside the viewport");
  if (facts.smallButtons.length) problems.push(`buttons under 36 px: ${facts.smallButtons.join("; ")}`);
  if (def.three) {
    const want = logo === "blocked" ? ["wordmark"] : logoOnDisk ? ["logo"] : ["wordmark"];
    if (!want.includes(facts.brand3d)) problems.push(`in-world brand plaque: ${facts.brand3d} (expected ${want.join("/")})`);
  }
  // Practice Lab: the seasonal sky loaded (assets/skies/skies.json and its images), and on the catalog every
  // scenario card shows its thumbnail (none fell back to the placeholder tile).
  if (def.name.startsWith("lab-") && facts.seasonSky !== "on") problems.push(`seasonal sky: ${facts.seasonSky ?? "not set"} (expected on)`);
  if (def.name === "lab-catalog" && (facts.thumbs !== scenarios.length || facts.emptyThumbs)) problems.push(`thumbnails: ${facts.thumbs} of ${scenarios.length} loaded, ${facts.emptyThumbs} placeholders`);
  if (shotsDir) {
    await page.screenshot({ path: join(shotsDir, `${def.name}-${vpName}${logo === "allowed" ? "-logo" : ""}.png`), fullPage: def.name === "launcher" });
  }
  await context.close();
  results.push({ page: def.name, viewport: vpName, logo, brand: facts.brandLogo, plaque: facts.brand3d ?? "-", result: problems.length ? "FAIL" : "ok" });
  if (problems.length) console.log(`FAIL ${def.name} ${vpName} logo=${logo}\n  ${problems.join("\n  ")}`);
  return problems.length === 0;
}

let failed = 0;
const logoModes = logoOnDisk ? ["blocked", "allowed"] : ["blocked"];
for (const def of pages) {
  for (const vpName of Object.keys(viewports)) {
    for (const logo of logoModes) if (!(await check(def, vpName, logo))) failed += 1;
  }
}
await browser.close();
console.table(results);
console.log(`${results.length - failed} passed, ${failed} failed${logoOnDisk ? "" : " (branding/logo.png not on this machine: logo-allowed runs skipped)"}`);
process.exit(failed ? 1 : 0);
