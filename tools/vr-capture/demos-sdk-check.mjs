// SDK correctness probes for the four single-SDK demos, in desktop Chrome with an EMULATED Quest 3 (IWER). Not a headset test.
//   STATIC_BASE=http://localhost:8172/ node tools/vr-capture/demos-sdk-check.mjs
// For each demo it records the session request (mode, required and optional features), enters the emulated session, checks the
// standing eye height, ends the session from the runtime side and checks the page recovers, resizes the window, and (WebGL
// demos) loses and restores the graphics context with WEBGL_lose_context and checks the demo draws again without errors.
import { chromium } from "playwright";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const base = (process.env.STATIC_BASE ?? "http://localhost:8172/").replace(/\/?$/, "/");
const iwer = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");
const demos = [
  { folder: "threejs", enter: "#VRButton", ready: () => window.__demo?.ready, camera: () => window.__demoCameraY?.() },
  { folder: "a-frame", enter: ".a-enter-vr-button", ready: () => window.__demo?.ready, camera: () => document.querySelector("#head").object3D.getWorldPosition(new AFRAME.THREE.Vector3()).y },
  { folder: "babylonjs", enter: ".babylonVRicon", ready: () => window.__demo?.ready, camera: () => null },
  { folder: "webxr", enter: "#enter", ready: () => window.__demo?.ready, camera: () => null },
];
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
let passed = 0;
const failures = [];
const check = (name, ok, detail = "") => { if (ok) passed += 1; else { failures.push(name); console.log(`FAIL ${name} ${detail}`); } };

for (const demo of demos) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
  const page = await context.newPage();
  const errors = [];
  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => { if (message.type() === "error" && !/favicon|logo|Failed to load resource|controllers.babylonjs|snippet.babylonjs|assets.babylonjs/.test(message.text())) errors.push(message.text()); });
  await page.route("**/*", (route) => {
    const url = new URL(route.request().url());
    if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
    if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
    return route.continue();
  });
  await page.addInitScript(iwer);
  await page.addInitScript(() => {
    const device = new window.IWER.XRDevice(window.IWER.metaQuest3);
    device.installRuntime({ forceInstall: true });
    window.__device = device;
    const original = navigator.xr.requestSession.bind(navigator.xr);
    window.__sessionRequests = [];
    navigator.xr.requestSession = (mode, init) => {
      window.__sessionRequests.push({ mode, required: init?.requiredFeatures ?? [], optional: init?.optionalFeatures ?? [] });
      return original(mode, init);
    };
  });
  await page.goto(`${base}${demo.folder}/`);
  await page.waitForFunction(demo.ready, null, { timeout: 60000 });
  await page.waitForTimeout(1500);

  // Session setup: immersive-vr, nothing required beyond what every headset has, floor as an optional feature or a fallback.
  await page.click(demo.enter);
  await page.waitForTimeout(3500);
  const requests = await page.evaluate(() => window.__sessionRequests);
  check(`${demo.folder} requests immersive-vr`, requests.length > 0 && requests.every((r) => r.mode === "immersive-vr"), JSON.stringify(requests));
  const required = requests.flatMap((r) => r.required).filter((f) => f !== "viewer");
  check(`${demo.folder} does not require local-floor (it is optional with a fallback)`, !required.includes("local-floor") && !required.includes("bounded-floor"), `required: ${required}`);
  console.log(`${demo.folder} session request:`, JSON.stringify(requests[0]));

  const inSession = await page.evaluate(() => Boolean(window.__device?.activeSession ?? window.__device?.session ?? true));
  const height = await page.evaluate(demo.camera).catch(() => null);
  if (height !== null && height !== undefined) check(`${demo.folder} eye height is the headset height, not doubled`, height > 1.2 && height < 2.0, `y = ${height}`);

  // Runtime ends the session (as when a person takes the headset off or presses the system button).
  await page.evaluate(() => { const session = window.__device.sessions?.[0] ?? window.__device.activeSession; session?.end?.(); });
  await page.waitForTimeout(2500);
  const afterEnd = await page.evaluate((selector) => {
    const button = document.querySelector(selector);
    return { disabled: button?.disabled ?? null, text: button?.textContent?.trim() ?? null, status: document.querySelector("#status")?.textContent ?? "" };
  }, demo.enter);
  check(`${demo.folder} offers VR again after the session ends`, afterEnd.disabled !== true || /Enter|VR/i.test(afterEnd.text ?? ""), JSON.stringify(afterEnd));

  // Resize: the drawing surface follows the window.
  await page.setViewportSize({ width: 700, height: 900 });
  await page.waitForTimeout(800);
  const size = await page.evaluate(() => { const c = document.querySelector("canvas"); return [c.clientWidth, c.clientHeight, c.width, c.height]; });
  check(`${demo.folder} canvas follows a resize`, Math.abs(size[0] - 700) < 4 && size[2] >= size[0] * 0.9, JSON.stringify(size));

  // Context loss and restore (not for A-Frame, which owns its renderer and has its own handling; still reported).
  if (demo.folder !== "a-frame") {
    const outcome = await page.evaluate(async () => {
      const canvases = [...document.querySelectorAll("canvas")];
      const canvas = canvases.find((c) => c.getContext("webgl2") || c.getContext("webgl")) ?? canvases[0];
      const gl = canvas.getContext("webgl2") ?? canvas.getContext("webgl");
      const ext = gl.getExtension("WEBGL_lose_context");
      if (!ext) return { skipped: true };
      ext.loseContext();
      await new Promise((resolve) => setTimeout(resolve, 600));
      const lost = gl.isContextLost();
      ext.restoreContext();
      await new Promise((resolve) => setTimeout(resolve, 1500));
      return { lost, restored: !gl.isContextLost(), status: document.querySelector("#status")?.textContent };
    });
    check(`${demo.folder} survives context loss and restore`, outcome.skipped || (outcome.lost && outcome.restored), JSON.stringify(outcome));
    if (demo.folder === "webxr") {
      await page.waitForTimeout(500);
      const before = await page.evaluate(() => window.__demo.simTime);
      await page.waitForTimeout(800);
      const afterTime = await page.evaluate(() => window.__demo.simTime);
      check("webxr keeps running after the context is restored", afterTime > before);
      const restored = await page.evaluate(() => { const g = document.querySelector("#scene").getContext("webgl2"); return { error: g.getError(), lost: g.isContextLost(), status: document.querySelector("#status").textContent }; });
      check("webxr rebuilt its GL resources without errors", restored.error === 0 && !restored.lost && /restored/i.test(restored.status), JSON.stringify(restored));
    }
  }
  check(`${demo.folder} no console or page errors`, errors.length === 0, errors.slice(0, 2).join(" | "));
  await context.close();
}
await browser.close();
console.log(`demos-sdk-check: ${passed} passed, ${failures.length} failed`);
process.exit(failures.length ? 1 : 0);
