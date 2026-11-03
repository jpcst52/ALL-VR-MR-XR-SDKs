// Round 2 QA of the Practice Lab: for each scenario take a start shot, one after a correct action, and one after a wrong
// action, and report page errors, console errors, per-frame triangles, and JS heap. Desktop Chrome, software GL (SwiftShader).
// Usage: QA_BASE=http://localhost:8171/ node tools/vr-capture/lab-qa.mjs <outDir> [id ...]
import { chromium } from "playwright";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { mkdirSync } from "node:fs";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.QA_BASE ?? "http://localhost:8171/").replace(/\/?$/, "/");
const out = process.argv[2];
const only = process.argv.slice(3);
mkdirSync(out, { recursive: true });
const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const context = await browser.newContext({ viewport: { width: Number(process.env.QA_W ?? 1280), height: Number(process.env.QA_H ?? 720) }, deviceScaleFactor: 1, reducedMotion: "reduce" });
const page = await context.newPage();
await page.route("**/*", (route) => {
  const url = new URL(route.request().url());
  if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
  if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
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
  const heap = await page.evaluate(() => Math.round((performance.memory?.usedJSHeapSize ?? 0) / 1e6));
  await page.screenshot({ path: join(out, `${s.id}-1start.png`) });
  const step = s.steps[0];
  const good = step.hotspots.find((h) => h.correct && !(h.after ?? []).length);
  const wrong = step.hotspots.find((h) => !h.correct);
  await page.evaluate(() => { document.querySelector("#task-actions-wrap").open = true; });
  if (good) { await page.click(`#task-actions [data-hotspot="${good.id}"]`); await page.waitForTimeout(900); await page.screenshot({ path: join(out, `${s.id}-2good.png`) }); }
  if (wrong) { await page.click(`#task-actions [data-hotspot="${wrong.id}"]`); await page.waitForTimeout(900); await page.screenshot({ path: join(out, `${s.id}-3wrong.png`) }); }
  console.log(s.id.padEnd(34), `tris=${stats.triangles} calls=${stats.calls} heapMB=${heap}`, stats.missing.length ? `MISSING ${stats.missing}` : "");
  await page.click("#back-to-catalog").catch(async () => { await page.click("text=All scenarios"); });
  await page.waitForTimeout(400);
}
await browser.close();
