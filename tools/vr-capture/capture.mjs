// Captures the browser XR examples inside an emulated Meta Quest 3 using Meta's Immersive Web Emulation
// Runtime (IWER). The page really enters an immersive-vr session; a scripted controller points at objects
// and presses the trigger. Output: PNGs in ../../training-simulations/screenshots/vr and
// ../../multi-player/screenshots/vr.
//
// Prerequisites: Google Chrome, `npm install` in this folder, and the example servers running:
//   python -m http.server 8000        (repository root)
//   node server.mjs                   (multi-player/, port 8443)
import { mkdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { chromium } from "playwright";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const trainingOut = join(repo, "training-simulations", "screenshots", "vr");
const multiplayerOut = join(repo, "multi-player", "screenshots", "vr");
mkdirSync(trainingOut, { recursive: true });
mkdirSync(multiplayerOut, { recursive: true });

const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);
const iwerSource = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");
const trainingUrl = process.env.TRAINING_URL ?? "http://localhost:8000/training-simulations/";
const circuitsUrl = process.env.CIRCUITS_URL ?? "http://localhost:8443/multi-player/vr/";

const browser = await chromium.launch({
  channel: "chrome",
  args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"],
});

async function newVrPage(width = 1600, height = 900) {
  const page = await (await browser.newContext({ viewport: { width, height } })).newPage();
  page.on("pageerror", (error) => console.log("PAGE ERROR", error.message));
  // The logo in branding/ is git-ignored; keep it out of committed screenshots unless CAPTURE_LOGO=1.
  if (process.env.CAPTURE_LOGO !== "1") await page.route("**/branding/**", (route) => route.fulfill({ status: 404, body: "" }));
  await page.addInitScript(iwerSource);
  await page.addInitScript(() => {
    const device = new window.IWER.XRDevice(window.IWER.metaQuest3);
    device.installRuntime({ forceInstall: true });
    window.__device = device;
  });
  return page;
}

// World-space targets. The training page moves its content in front of the player in VR (offset and scale
// must match vrWorldOffset / vrWorldScale in training-simulations/app.js).
function vrPoints(offset = [0, 0, -2.4], scale = 1.25) {
  const toWorld = (point) => point.map((value, index) => value * scale + offset[index]);
  const model = [0, 0.2, -0.28];
  return {
    hotspotPoint: (hotspot) => toWorld(hotspot.at.map((value, index) => value + model[index])),
    // Must match the catalog card grid in training-simulations/app.js (4 x 4, extra cards in a fifth column).
    cardPoint: (index) => toWorld(index < 16
      ? [((index % 4) - 1.5) * 0.98, 2.3 - Math.floor(index / 4) * 0.48, -1.36]
      : [2.5 * 0.98, 2.3 - (index - 16) * 0.48, -1.36]),
    advancePoint: toWorld([0, 1.15, -1.32]),
  };
}

// Point the right controller at a world-space target; optionally press the trigger.
async function aim(page, target, { trigger = false, from = [0.32, 1.1, -0.35] } = {}) {
  await page.evaluate(({ target, from }) => {
    const controller = window.__device.controllers.right;
    const direction = target.map((value, index) => value - from[index]);
    const length = Math.hypot(...direction);
    const [fx, fy, fz] = direction.map((value) => value / length);
    // Rotate the controller's forward axis (0, 0, -1) onto the direction to the target.
    const dot = -fz;
    const axis = [fy, -fx, 0];
    const axisLength = Math.hypot(...axis) || 1;
    const angle = Math.acos(Math.min(1, Math.max(-1, dot)));
    const sin = Math.sin(angle / 2);
    controller.position.set(...from);
    controller.quaternion.set((axis[0] / axisLength) * sin, (axis[1] / axisLength) * sin, 0, Math.cos(angle / 2));
  }, { target, from });
  await page.waitForTimeout(350);
  if (trigger) {
    await page.evaluate(() => window.__device.controllers.right.updateButtonValue("trigger", 1));
    await page.waitForTimeout(160);
    await page.evaluate(() => window.__device.controllers.right.updateButtonValue("trigger", 0));
    await page.waitForTimeout(450);
  }
}

// CAPTURE_ONLY=why runs only the "Why this matters" / reasoning-question block below.
const only = process.env.CAPTURE_ONLY ?? "";

// CAPTURE_ONLY=menu: only the Practice Lab scenario menu (thumbnail cards, seasonal sky behind): 01-catalog.png,
// 02-pointing-at-a-scenario.png, and 11-menu-sky-transition.png (the season transition held at its peak).
if (only === "menu") {
  const page = await newVrPage();
  const shot = (name) => page.screenshot({ path: join(trainingOut, name) });
  await page.goto(trainingUrl);
  await page.waitForFunction(() => document.documentElement.dataset.seasonSky, null, { timeout: 20000 });
  await page.fill("#learner-name", "Alex");
  await page.click("#profile button[type=submit]");
  await page.waitForTimeout(800);
  await page.locator("body > button").click(); // ENTER VR
  await page.waitForTimeout(2500);
  await page.evaluate(() => window.__seasonSky?.stop());
  await page.waitForTimeout(500);
  const { cardPoint } = vrPoints();
  await shot("01-catalog.png");
  await aim(page, cardPoint(scenarios.findIndex((scenario) => scenario.id === "work-at-height")));
  await shot("02-pointing-at-a-scenario.png");
  await page.evaluate(() => window.__seasonSky?.preview(0.5));
  await page.waitForTimeout(400);
  await shot("11-menu-sky-transition.png");
  await page.close();
}

// ----------------------------------------------------------------- Fieldwork XR Practice Lab
if (!only) {
  const page = await newVrPage();
  const shot = (name) => page.screenshot({ path: join(trainingOut, name) });
  await page.goto(trainingUrl);
  await page.waitForTimeout(3000);
  await page.fill("#learner-name", "Alex");
  await page.click("#profile button[type=submit]");
  await page.waitForTimeout(800);
  await page.locator("body > button").click(); // ENTER VR
  await page.waitForTimeout(2500);

  const { hotspotPoint, cardPoint, advancePoint } = vrPoints();

  await shot("01-catalog.png");
  const heightIndex = scenarios.findIndex((scenario) => scenario.id === "work-at-height");
  await aim(page, cardPoint(heightIndex));
  await shot("02-pointing-at-a-scenario.png");
  await aim(page, cardPoint(heightIndex), { trigger: true });
  await page.waitForTimeout(1200);
  await shot("03-hands-on-task.png");

  const height = scenarios[heightIndex];
  const first = height.steps[0].hotspots;
  for (const id of ["tag", "deck"]) await aim(page, hotspotPoint(first.find((hotspot) => hotspot.id === id)), { trigger: true });
  await aim(page, hotspotPoint(first.find((hotspot) => hotspot.id === "plan")));
  await shot("04-selecting-hotspots.png");
  for (const id of ["plan", "climb"]) await aim(page, hotspotPoint(first.find((hotspot) => hotspot.id === id)), { trigger: true });
  await page.waitForTimeout(500);
  await shot("05-task-complete.png");

  // A wrong action: the platform sways and a critical mistake is flagged.
  await aim(page, advancePoint, { trigger: true });
  await page.waitForTimeout(800);
  const anchorTask = height.steps[1].hotspots;
  await aim(page, hotspotPoint(anchorTask.find((hotspot) => hotspot.id === "rail")), { trigger: true });
  await shot("06-wrong-action.png");
  await page.close();
}

// A second scenario in VR: electrical isolation, where the lock and tag appear on the breaker.
if (!only) {
  const page = await newVrPage();
  const shot = (name) => page.screenshot({ path: join(trainingOut, name) });
  await page.goto(trainingUrl);
  await page.waitForTimeout(3000);
  await page.fill("#learner-name", "Alex");
  await page.click("#profile button[type=submit]");
  await page.waitForTimeout(800);
  await page.locator("body > button").click();
  await page.waitForTimeout(2500);
  const { hotspotPoint, cardPoint, advancePoint } = vrPoints();
  const electricalIndex = scenarios.findIndex((scenario) => scenario.id === "electrical-work");
  await aim(page, cardPoint(electricalIndex), { trigger: true });
  await page.waitForTimeout(1000);
  const electrical = scenarios[electricalIndex];
  for (const id of ["drawing", "plan", "start"]) {
    await aim(page, hotspotPoint(electrical.steps[0].hotspots.find((hotspot) => hotspot.id === id)), { trigger: true });
  }
  await aim(page, advancePoint, { trigger: true });
  await page.waitForTimeout(800);
  for (const id of ["off", "lock", "tag"]) {
    await aim(page, hotspotPoint(electrical.steps[1].hotspots.find((hotspot) => hotspot.id === id)), { trigger: true });
  }
  await aim(page, hotspotPoint(electrical.steps[1].hotspots.find((hotspot) => hotspot.id === "prove")));
  await shot("07-electrical-lockout.png");
  await page.close();
}

// "Why this matters" in VR: the in-world reason panel under the sign, then the reasoning question on the
// three answer cards, answered with the controller (hydrostatic test).
if (!only || only === "why") {
  const page = await newVrPage();
  const shot = (name) => page.screenshot({ path: join(trainingOut, name) });
  await page.goto(trainingUrl);
  await page.waitForTimeout(3000);
  await page.fill("#learner-name", "Alex");
  await page.click("#profile button[type=submit]");
  await page.waitForTimeout(800);
  await page.locator("body > button").click();
  await page.waitForTimeout(2500);
  const { hotspotPoint, cardPoint, advancePoint } = vrPoints();
  const toWorld = (point) => point.map((value, index) => value * 1.25 + [0, 0, -2.4][index]);
  const hydroIndex = scenarios.findIndex((scenario) => scenario.id === "heat-exchanger-hydrotest");
  const hydro = scenarios[hydroIndex];
  await aim(page, cardPoint(hydroIndex), { trigger: true });
  await page.waitForTimeout(1000);
  const order = (step) => {
    const required = step.hotspots.filter((h) => h.correct);
    const done = [];
    while (done.length < required.length) done.push(required.find((h) => !done.includes(h.id) && (h.after ?? []).every((id) => done.includes(id))).id);
    return done;
  };
  for (const [index, step] of hydro.steps.entries()) {
    for (const id of order(step)) {
      await aim(page, hotspotPoint(step.hotspots.find((hotspot) => hotspot.id === id)), { trigger: true });
      if (index === 1 && id === "openvent") { await page.waitForTimeout(600); await shot("08-why-this-matters.png"); }
    }
    await aim(page, advancePoint, { trigger: true });
    await page.waitForTimeout(800);
  }
  await page.waitForTimeout(600);
  await shot("09-reasoning-question.png");
  const best = hydro.rationale.options.findIndex((option) => option.best);
  await aim(page, toWorld([(best - 1) * 1.28, 1.15, -1.32]), { trigger: true });
  await page.waitForTimeout(800);
  await shot("10-reasoning-feedback.png");
  await page.close();
}

// ------------------------------------------------------------------ Multiplayer Circuit Lab (VR)
if (!only) {
  const vrPage = await newVrPage();
  const guest = await (await browser.newContext({ viewport: { width: 1280, height: 720 } })).newPage();
  for (const [page, name] of [[vrPage, "VR Builder"], [guest, "Desktop Guest"]]) {
    await page.goto(circuitsUrl);
    await page.fill("#name", name);
    await page.fill("#room", "vr-demo");
    await page.click("#join");
    await page.waitForTimeout(1500);
  }
  await vrPage.click("#load-demo");
  await vrPage.waitForTimeout(2500);
  await vrPage.click("#enter-xr");
  await vrPage.waitForTimeout(2500);
  // Look down slightly at the table-height board.
  await vrPage.evaluate(() => window.__device.quaternion.set(-0.14, 0, 0, 0.99));
  await vrPage.waitForTimeout(600);
  await vrPage.screenshot({ path: join(multiplayerOut, "01-circuit-lab-in-vr.png") });
  await vrPage.close();
  await guest.close();
}

await browser.close();
console.log("Done.");
