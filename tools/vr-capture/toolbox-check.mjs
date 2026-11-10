// Checks the in-headset toolbox, the Y and B buttons, the examples and the reset button in the Circuit Lab and the Site Safety Layout,
// in desktop Chrome with an EMULATED Quest 3 (Meta IWER). Not a headset test: the emulator proves the code path (a controller ray
// pressing a floating panel, face buttons changing a shared part), not how it feels or reads in a real Quest.
//   node tools/vr-capture/toolbox-check.mjs            (TOOLBOX_SHOTS=1 also writes multi-player/screenshots/vr-toolbox-*.png)
import { pathToFileURL, fileURLToPath } from "node:url";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const { chromium } = await import(pathToFileURL(join(here, "node_modules", "playwright", "index.mjs")).href);
const iwer = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");
const { createRelay } = await import(pathToFileURL(join(repo, "multi-player", "server.mjs")).href);
const shots = process.env.TOOLBOX_SHOTS === "1";

const relay = createRelay();
const { port } = await relay.listen({ host: "127.0.0.1", port: 8183, autoPort: true });
const base = `http://127.0.0.1:${port}/`;
let passed = 0;
const failures = [];
const check = (name, ok, detail = "") => { if (ok) passed += 1; else { failures.push(`${name} ${detail}`); console.log(`FAIL ${name} ${detail}`); } };

const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const errors = [];

async function openQuest(path, room) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 760 } });
  const page = await context.newPage();
  page.setDefaultTimeout(60000);
  page.on("pageerror", (error) => errors.push(error.message));
  page.on("console", (message) => { if (message.type() === "error" && !/favicon|logo\.png|Failed to load resource/.test(message.text())) errors.push(message.text()); });
  await page.route("**/branding/**", (route) => route.fulfill({ status: 404, body: "" }));
  await page.addInitScript(iwer);
  await page.addInitScript(() => {
    const device = new window.IWER.XRDevice(window.IWER.metaQuest3);
    device.installRuntime({ forceInstall: true });
    window.__device = device;
  });
  await page.goto(`${base}${path}${path.includes("?") ? "&" : "?"}lite=1`);
  await page.fill("#name", "Quest");
  await page.fill("#room", room);
  await page.click("#join");
  await page.waitForSelector("#workspace:not([hidden])");
  await page.click("#enter-xr");
  await page.waitForFunction(() => window.__mp.renderer.xr.isPresenting, null, { timeout: 15000 });
  await page.waitForTimeout(900);
  return page;
}

// Point a controller at a world point (from where the controller is held) and let a frame pass.
async function aim(page, hand, target) {
  await page.evaluate(({ hand, target }) => {
    const controller = window.__device.controllers[hand];
    const from = hand === "left" ? [-0.32, 1.1, -0.35] : [0.32, 1.1, -0.35];
    const d = target.map((v, i) => v - from[i]);
    const len = Math.hypot(...d);
    const [fx, fy, fz] = d.map((v) => v / len);
    const axis = [fy, -fx, 0];
    const axisLength = Math.hypot(...axis) || 1;
    const angle = Math.acos(Math.min(1, Math.max(-1, -fz)));
    const s = Math.sin(angle / 2);
    controller.position.set(...from);
    controller.quaternion.set((axis[0] / axisLength) * s, (axis[1] / axisLength) * s, 0, Math.cos(angle / 2));
  }, { hand, target });
  await page.waitForTimeout(300);
}
const press = async (page, hand, name, ms = 180) => {
  await page.evaluate(({ hand, name }) => window.__device.controllers[hand].updateButtonValue(name, 1), { hand, name });
  await page.waitForTimeout(ms);
  await page.evaluate(({ hand, name }) => window.__device.controllers[hand].updateButtonValue(name, 0), { hand, name });
  await page.waitForTimeout(250);
};
const toolboxPoint = (page, label) => page.evaluate((text) => {
  const { toolbox } = window.__mp;
  toolbox.group.updateMatrixWorld(true);
  return toolbox.group.localToWorld(toolbox.buttonCentre(text)).toArray();
}, label);
const count = (page) => page.evaluate(() => window.__mp.objects.size);
const clickTool = async (page, label, times = 1) => {
  await aim(page, "right", await toolboxPoint(page, label));
  for (let i = 0; i < times; i += 1) await press(page, "right", "trigger", 120);
};
const partPoint = (page, partId) => page.evaluate((id) => {
  const object = [...window.__mp.objects.values()].find((item) => item.data.part === id);
  if (!object) return null;
  const point = object.group.getWorldPosition(new window.__mp.camera.position.constructor());
  point.y += 0.015;
  return point.toArray();
}, partId);
const stateOf = (page, partId) => page.evaluate((id) => [...window.__mp.objects.values()].find((item) => item.data.part === id)?.data.on, partId);

const themes = [
  { key: "circuits", path: "multi-player/vr/", toggler: "switch", open: "Open switch", close: "Close switch", examples: ["Simple loop", "Two bulbs in series", "Dimmed bulb", "Motor and bulb"] },
  { key: "safety", path: "multi-player/vr/?theme=safety", toggler: "beacon", open: "Beacon off", close: "Beacon on", examples: ["Sample layout", "Welding bay", "Chemical store", "Loading lane"] },
];

for (const theme of themes) {
  const page = await openQuest(theme.path, `tb-${theme.key}-${Date.now() % 100000}`);
  const label = theme.key;
  const visible = await page.evaluate(() => window.__mp.toolbox.group.visible);
  check(`${label} toolbox is visible in VR`, visible);
  const handsVisible = await page.evaluate(() => {
    let gloves = 0;
    window.__mp.scene.traverse((node) => { if (node.name === "glove-hand" && node.visible) gloves += 1; });
    return gloves;
  });
  check(`${label} both hands have a visible model`, handsVisible === 2, `gloves ${handsVisible}`);

  // Add a part from the toolbox.
  const parts = await page.evaluate(() => window.__mp.toolbox.buttons.filter((b) => b.kind === "part").map((b) => b.label));
  await clickTool(page, parts[0]);
  check(`${label} toolbox button adds a part`, (await count(page)) === 1, `count ${await count(page)}`);
  await clickTool(page, parts[1]);
  check(`${label} toolbox adds a second part`, (await count(page)) === 2);

  // Each example loads and replaces the scene.
  for (let i = 0; i < theme.examples.length; i += 1) {
    await clickTool(page, theme.examples[i]);
    await page.waitForTimeout(500);
    const n = await count(page);
    check(`${label} example "${theme.examples[i]}" loads`, n >= 6, `parts ${n}`);
    const summary = await page.evaluate(() => window.__mp.toolboxState().summary);
    if (theme.key === "circuits") check(`${label} example "${theme.examples[i]}" is working`, /Closed circuit: .*(lit|running)/.test(summary), summary);
    else check(`${label} example "${theme.examples[i]}" passes the checklist`, !/✗/.test(summary) && /Every hazard beacon has a barrier/.test(summary), summary);
  }
  if (shots) await page.screenshot({ path: join(repo, "multi-player", "screenshots", `vr-toolbox-${theme.key}.png`) });

  // Y opens, B closes the part under the controller ray.
  await clickTool(page, theme.examples[0]);
  await page.waitForTimeout(400);
  const toggler = await partPoint(page, theme.toggler);
  await aim(page, "left", toggler);
  await press(page, "left", "y-button");
  check(`${label} Y opens / turns off the ${theme.toggler}`, (await stateOf(page, theme.toggler)) === false, String(await stateOf(page, theme.toggler)));
  const mid = await page.evaluate(() => window.__mp.toolboxState().summary);
  if (theme.key === "circuits") check(`${label} the circuit goes open with the switch open`, /Open circuit/.test(mid), mid);
  await aim(page, "right", toggler);
  await press(page, "right", "b-button");
  check(`${label} B closes / turns on the ${theme.toggler}`, (await stateOf(page, theme.toggler)) === true);
  if (theme.key === "circuits") {
    const end = await page.evaluate(() => window.__mp.toolboxState().summary);
    check(`${label} the circuit closes again`, /Closed circuit/.test(end), end);
  }

  // Selected-part buttons on the panel work too.
  await clickTool(page, theme.open);
  check(`${label} panel "${theme.open}" works on the selected part`, (await stateOf(page, theme.toggler)) === false);
  await clickTool(page, theme.close);
  check(`${label} panel "${theme.close}" works on the selected part`, (await stateOf(page, theme.toggler)) === true);

  // X / A rotate the selected part.
  const yaw = () => page.evaluate((id) => [...window.__mp.objects.values()].find((item) => item.data.part === id).group.rotation.y, theme.toggler);
  const yawBefore = await yaw();
  await press(page, "left", "x-button");
  await page.waitForTimeout(300);
  check(`${label} X rotates the selected part 45 degrees`, Math.abs((await yaw()) - yawBefore - Math.PI / 4) < 0.01, String((await yaw()) - yawBefore));

  // Reset needs a second press.
  await clickTool(page, "Reset scene");
  check(`${label} first Reset press only asks to confirm`, (await count(page)) > 0);
  await clickTool(page, "Reset scene");
  await page.waitForTimeout(500);
  check(`${label} second Reset press clears the scene`, (await count(page)) === 0, `count ${await count(page)}`);
  await page.context().close();
}

await browser.close();
await relay.close();
check("no page errors", errors.length === 0, errors.slice(0, 3).join(" | "));
console.log(failures.length ? `\n${failures.length} FAILED, ${passed} passed` : `\ntoolbox check passed (${passed} checks, EMULATED Quest 3)`);
process.exit(failures.length ? 1 : 0);
