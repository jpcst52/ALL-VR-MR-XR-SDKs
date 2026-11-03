// Screenshots each dressed Practice Lab scenario and prints the per-frame triangle / draw-call count (shadow pass included).
// Usage: python -m http.server 8161 (from the repo root), then  SHOTS_BASE=http://localhost:8161/ node tools/vr-capture/realism-shots.mjs [outDir] [id ...]
// Rendered by SwiftShader (software GL) in Chrome: these are desktop-view captures, not a headset.
import { chromium } from "playwright";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { mkdirSync } from "node:fs";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.SHOTS_BASE ?? "http://localhost:8161/").replace(/\/?$/, "/");
const out = (process.argv[2] || null) ?? join(repo, "training-simulations", "screenshots", "realism-after");
const only = process.argv.slice(3);
mkdirSync(out, { recursive: true });
const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, deviceScaleFactor: 1, reducedMotion: "reduce" });
const page = await context.newPage();
await page.route("**/*", (route) => {
  const url = new URL(route.request().url());
  if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
  if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
  if (process.env.BLOCK_MODELS && url.pathname.includes("/assets/models/")) return route.fulfill({ status: 404, body: "" }); // fallback check
  return route.continue();
});
page.on("pageerror", (e) => console.log("PAGE ERROR", e.message));
page.on("console", (m) => { if (m.type() === "warning" || m.type() === "error") console.log("console", m.type(), m.text().slice(0, 160)); });
await page.goto(`${base}training-simulations/`);
await page.waitForTimeout(1500);
await page.fill("#learner-name", "Alex");
await page.click("#profile button[type=submit]");
for (const s of scenarios) {
  if (only.length && !only.includes(s.id)) continue;
  await page.click(`[data-scenario="${s.id}"]`);
  await page.waitForTimeout(3500);
  const stats = await page.evaluate(() => globalThis.__labStats());
  await page.screenshot({ path: join(out, `${s.id}.png`) });
  console.log(s.id.padEnd(34), `tris=${stats.triangles} calls=${stats.calls}`, stats.missing.length ? `MISSING ${stats.missing}` : "");
  await page.click("#back-to-catalog").catch(async () => { await page.click("text=All scenarios"); });
  await page.waitForTimeout(400);
}
await browser.close();
