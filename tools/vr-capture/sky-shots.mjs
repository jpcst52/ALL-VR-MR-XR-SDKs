// Screenshots of the Practice Lab menu with scenario thumbnails and the seasonal skies (desktop browser).
//
//   node tools/vr-capture/sky-shots.mjs            SHOTS_BASE overrides http://localhost:8132/
//
// Writes training-simulations/screenshots/skies/:
//   menu-thumbnails.png            the catalog with thumbnails (page cards and the in-world cards behind)
//   season-<spring|summer|autumn|winter>.png   one frame per season
//   transition-mid.png             the glitch transition held at its peak (mosaic, scan lines, colour split)
//   transition-early.png           the transition a quarter of the way in
//   reduced-motion-crossfade.png   the plain cross-fade used with prefers-reduced-motion, half way
//   menu-phone.png                 the catalog at phone width (390 x 844, emulated, not a real phone)
//   scenario-outdoor.png / scenario-indoor.png   sky behind an outdoor work area; an indoor one keeps the navy room
// The git-ignored logo (branding/) is blocked, as in every committed screenshot.
import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const out = join(here, "..", "..", "training-simulations", "screenshots", "skies");
mkdirSync(out, { recursive: true });
const base = (process.env.SHOTS_BASE ?? "http://localhost:8132/").replace(/\/?$/, "/");
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const errors = [];

async function open({ viewport = { width: 1440, height: 900 }, reducedMotion = "no-preference" } = {}) {
  const context = await browser.newContext({ viewport, deviceScaleFactor: 1, reducedMotion });
  const page = await context.newPage();
  await page.route("**/*", (route) => {
    const url = new URL(route.request().url());
    if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
    if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
    return route.continue();
  });
  page.on("pageerror", (e) => errors.push(e.message));
  await page.goto(`${base}training-simulations/`);
  await page.waitForFunction(() => document.documentElement.dataset.seasonSky, null, { timeout: 20000 });
  await page.fill("#learner-name", "Alex");
  await page.click("#profile button[type=submit]");
  await page.waitForTimeout(1500);
  // Hold the rotation so each frame is deterministic.
  await page.evaluate(() => window.__seasonSky.stop());
  return page;
}

const shot = async (page, name) => { await page.waitForTimeout(500); await page.screenshot({ path: join(out, name) }); console.log("wrote", name); };

{
  const page = await open();
  console.log("sky:", await page.evaluate(() => document.documentElement.dataset.seasonSky), "season:", await page.evaluate(() => window.__seasonSky.label));
  await shot(page, "menu-thumbnails.png");
  for (const season of ["spring", "summer", "autumn", "winter"]) {
    const ok = await page.evaluate((s) => window.__seasonSky.show(s), season);
    if (!ok) { console.log(`no ${season} sky`); continue; }
    await shot(page, `season-${season}.png`);
  }
  await page.evaluate(() => window.__seasonSky.show("autumn"));
  await page.evaluate(() => window.__seasonSky.preview(0.25));
  await shot(page, "transition-early.png");
  await page.evaluate(() => window.__seasonSky.preview(0.5));
  await shot(page, "transition-mid.png");
  await page.context().close();
}
// An outdoor scenario shows the sky behind its work area; an indoor one keeps the navy room.
for (const [id, name] of [["refinery-battery-area", "scenario-outdoor.png"], ["electrical-work", "scenario-indoor.png"]]) {
  const page = await open();
  await page.evaluate(() => window.__seasonSky.show("autumn"));
  await page.click(`[data-scenario="${id}"]`);
  await page.waitForTimeout(1200);
  await shot(page, name);
  await page.context().close();
}
{
  const page = await open({ reducedMotion: "reduce" });
  await page.evaluate(() => window.__seasonSky.show("autumn"));
  await page.evaluate(() => window.__seasonSky.preview(0.5));
  await shot(page, "reduced-motion-crossfade.png");
  await page.context().close();
}
{
  const page = await open({ viewport: { width: 390, height: 844 } });
  await shot(page, "menu-phone.png");
  await page.context().close();
}
await browser.close();
if (errors.length) { console.log("PAGE ERRORS:\n  " + errors.join("\n  ")); process.exit(1); }
console.log("Done, no page errors.");
