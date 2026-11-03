// Enters an EMULATED immersive-vr session (Meta's IWER, configured as a Quest 3) in each of the four single-SDK
// demos, presses the emulated controller trigger, and reports console errors. This proves the code path runs; it
// is NOT a headset test. Serve the repo root first (python -m http.server 8163), then:
//   STATIC_BASE=http://localhost:8163/ node demos-xr-check.mjs   [shots]   (shots = also save screenshots)
import { chromium } from "playwright";
import { readFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.STATIC_BASE ?? "http://localhost:8163/").replace(/\/?$/, "/");
const iwer = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");
const saveShots = process.argv.includes("shots");
const demos = [
  { folder: "threejs", enter: "#VRButton", changed: () => document.querySelector("#status").textContent.startsWith("Prism glow") },
  { folder: "a-frame", enter: ".a-enter-vr-button", changed: () => true },
  { folder: "babylonjs", enter: ".babylonVRicon", changed: () => true },
  { folder: "webxr", enter: "#enter", changed: () => document.querySelector("#status").textContent.startsWith("Palette") },
];
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
let failed = 0;
for (const demo of demos) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await context.newPage();
  const problems = [];
  await page.route("**/*", (route) => {
    const url = new URL(route.request().url());
    if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
    if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
    return route.continue();
  });
  page.on("pageerror", (error) => problems.push(`pageerror ${error.message}`));
  const notes = new Set();
  // Known: Babylon.js 7.42's default XR experience asks controllers.babylonjs.com / snippet.babylonjs.com for controller
  // and touch-point models when a session starts. Offline that fails harmlessly (no controller models); we report it, not fail.
  const served = new Set();
  page.on("response", (response) => { if (response.status() < 400) served.add(response.url()); });
  page.on("requestfailed", async (request) => {
    const url = request.url();
    if (/babylonjs\.com/.test(url)) { notes.add(new URL(url).hostname); return; }
    // Chrome sometimes cancels one of two overlapping identical requests; that is fine if the file was served.
    await new Promise((resolve) => setTimeout(resolve, 500));
    if (request.failure()?.errorText === "net::ERR_ABORTED" && served.has(url)) return;
    problems.push(`blocked/failed request ${url} (${request.failure()?.errorText})`);
  });
  page.on("console", (message) => { if (message.type() === "error" && !/404|ERR_FAILED/.test(message.text())) problems.push(`console ${message.text()}`); });
  await page.addInitScript(iwer);
  await page.addInitScript(() => {
    const device = new window.IWER.XRDevice(window.IWER.metaQuest3);
    device.installRuntime({ forceInstall: true });
    window.__device = device;
  });
  await page.goto(`${base}${demo.folder}/`);
  await page.waitForTimeout(4000);
  await page.locator(demo.enter).first().click();
  await page.waitForTimeout(2500);
  const inSession = await page.evaluate(() => Boolean(window.__device?.activeSession ?? window.__device?.sessions?.length ?? false));
  // Point the right controller forward and pull the trigger.
  await page.evaluate(() => { const c = window.__device.controllers.right; c.position.set(0.2, 1.3, -0.2); });
  await page.evaluate(() => window.__device.controllers.right.updateButtonValue("trigger", 1));
  await page.waitForTimeout(250);
  await page.evaluate(() => window.__device.controllers.right.updateButtonValue("trigger", 0));
  await page.waitForTimeout(1200);
  const changed = await page.evaluate(demo.changed);
  if (saveShots) {
    const out = join(repo, demo.folder, "screenshots", `${demo.folder}-emulated-quest3.png`);
    mkdirSync(dirname(out), { recursive: true });
    await page.screenshot({ path: out });
  }
  const ok = inSession && changed && problems.length === 0;
  console.log(`${ok ? "ok  " : "FAIL"} ${demo.folder}: emulated session=${inSession} trigger-effect=${changed}${problems.length ? "\n   " + problems.join("\n   ") : ""}`);
  failed += ok ? 0 : 1;
  await context.close();
}
await browser.close();
process.exit(failed ? 1 : 0);
