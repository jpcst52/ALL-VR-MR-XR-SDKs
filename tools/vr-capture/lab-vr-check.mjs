// Checks the Practice Lab's in-headset navigation in an EMULATED Quest 3 (Meta IWER), not a real headset:
//   - a "Main menu" card is visible at the scenario list and pointing at it leaves the lab for the main menu
//   - inside a scenario a "Back" card returns to the scenario list, and the "Main menu" card is still there
//   - the 3D cards are drawn over the scene (the continue box used to be hidden behind props and people)
//   - desktop (no headset) does not show the in-world exit cards, because the page has its own buttons
//   node tools/vr-capture/lab-vr-check.mjs      (starts its own static server; set LAB_URL to use another)
import { readFileSync } from "node:fs";
import { createServer } from "node:http";
import { dirname, join, extname } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const iwer = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");

const types = { ".html": "text/html", ".js": "text/javascript", ".mjs": "text/javascript", ".css": "text/css", ".json": "application/json", ".png": "image/png", ".jpg": "image/jpeg", ".glb": "model/gltf-binary", ".woff2": "font/woff2", ".ttf": "font/ttf", ".md": "text/markdown" };
let server = null, base = process.env.LAB_URL;
if (!base) {
  server = createServer((req, res) => {
    try {
      const path = decodeURIComponent(new URL(req.url, "http://x").pathname);
      let file = join(repo, path.endsWith("/") ? path + "index.html" : path);
      if (!file.startsWith(repo)) throw new Error("outside");
      const body = readFileSync(file);
      res.writeHead(200, { "content-type": types[extname(file)] ?? "application/octet-stream" }); res.end(body);
    } catch { res.writeHead(404); res.end(); }
  });
  await new Promise((r) => server.listen(0, "127.0.0.1", r));
  base = `http://127.0.0.1:${server.address().port}/training-simulations/`;
}

let passed = 0; const failures = [];
const check = (name, ok, detail = "") => { if (ok) passed += 1; else { failures.push(`${name} ${detail}`); console.log(`FAIL ${name} ${detail}`); } };

const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const errors = [];
const context = await browser.newContext({ viewport: { width: 1280, height: 760 } });
const page = await context.newPage();
page.setDefaultTimeout(60000);
page.on("pageerror", (e) => errors.push(e.message));
page.on("console", (m) => { if (m.type() === "error" && !/favicon|logo\.png|Failed to load resource/.test(m.text())) errors.push(m.text()); });
await page.route("**/branding/**", (r) => r.fulfill({ status: 404, body: "" }));
await page.addInitScript(iwer);
await page.addInitScript(() => { const d = new window.IWER.XRDevice(window.IWER.metaQuest3); d.installRuntime({ forceInstall: true }); window.__device = d; });
await page.goto(base);
await page.fill("#learner-name", "Alex");
await page.click("#profile button[type=submit]");
await page.waitForFunction(() => globalThis.__labUi && !document.querySelector("#catalog").hidden);

// desktop: no in-world exit cards
let ui = await page.evaluate(() => ({ menu: __labUi.cards.menu.visible, back: __labUi.cards.back.visible }));
check("desktop shows no in-world Main menu card", ui.menu === false);

// enter VR (the VRButton) in the emulator
await page.click("button:has-text('ENTER VR')");
await page.waitForFunction(() => __labUi.renderer.xr.isPresenting, null, { timeout: 15000 });
await page.waitForTimeout(800);
ui = await page.evaluate(() => ({ menu: __labUi.cards.menu.visible, back: __labUi.cards.back.visible }));
check("VR scenario list shows the Main menu card", ui.menu === true);
check("VR scenario list has no Back card", ui.back === false);

async function aimAndSelect(cardName) {
  const pos = await page.evaluate((n) => __labUi.worldOf(__labUi.cards[n]), cardName);
  await page.evaluate((target) => {
    const c = window.__device.controllers.right;
    const from = [0.25, 1.2, -0.2];
    const d = target.map((v, i) => v - from[i]);
    const len = Math.hypot(...d); const [fx, fy, fz] = d.map((v) => v / len);
    const axis = [fy, -fx, 0]; const al = Math.hypot(...axis) || 1;
    const angle = Math.acos(Math.min(1, Math.max(-1, -fz))); const s = Math.sin(angle / 2);
    c.position.set(...from); c.quaternion.set((axis[0] / al) * s, (axis[1] / al) * s, 0, Math.cos(angle / 2));
  }, pos);
  await page.waitForTimeout(300);
  await page.evaluate(() => window.__device.controllers.right.updateButtonValue("trigger", 1));
  await page.waitForTimeout(200);
  await page.evaluate(() => window.__device.controllers.right.updateButtonValue("trigger", 0));
  await page.waitForTimeout(500);
}

// start a scenario (the one with the largest number of props), then the Back and Main menu cards
for (const index of [0]) {
  await page.evaluate((i) => __labUi.start(i), index);
  await page.waitForTimeout(1200);
  ui = await page.evaluate(() => ({ back: __labUi.cards.back.visible, menu: __labUi.cards.menu.visible, state: __labUi.state(), top: [__labUi.isDrawnOnTop(__labUi.cards.advance), __labUi.isDrawnOnTop(__labUi.cards.menu), __labUi.isDrawnOnTop(__labUi.cards.back)] }));
  check("in a scenario the Back card is visible", ui.back === true);
  check("in a scenario the Main menu card is still visible", ui.menu === true);
  check("the continue, Main menu and Back cards are drawn over the scene", ui.top.every(Boolean), JSON.stringify(ui.top));
  await aimAndSelect("back");
  const after = await page.evaluate(() => __labUi.state());
  check("pointing at Back returns to the scenario list", after.scenario === null, JSON.stringify(after));
}

// Main menu leaves the lab for the main menu page (one level up)
const nav = page.waitForURL((u) => new URL(u).pathname === new URL("../", base).pathname, { timeout: 15000 }).then(() => true).catch(() => false);
await aimAndSelect("menu");
check("pointing at Main menu leaves the lab for the main menu page", await nav, page.url());

check("no page or console errors", errors.length === 0, errors.slice(0, 3).join(" | "));
await browser.close(); server?.close();
if (failures.length) { console.log(`FAILED: ${failures.length}`); process.exit(1); }
console.log(`lab VR navigation check passed (${passed} checks, EMULATED Quest 3)`);
