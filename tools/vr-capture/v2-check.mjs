// Lab v2 version of toolbox-check.mjs: the same in-headset checks (toolbox, Y / B / X, examples, reset, glove hands) against
// multi-player/v2/, plus v2-only checks: v2 page marker, frames really render, the fixed light pool, current-flow dots follow the
// switch, terminal rings, bulb glow, render cost under a budget. EMULATED Quest 3 (Meta IWER) in desktop Chrome: it proves the code
// path, not how it feels or performs on a real Quest.
//   node tools/vr-capture/v2-check.mjs
import { pathToFileURL, fileURLToPath } from "node:url";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const { chromium } = await import(pathToFileURL(join(here, "node_modules", "playwright", "index.mjs")).href);
const iwer = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");
const { createRelay } = await import(pathToFileURL(join(repo, "multi-player", "server.mjs")).href);

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
  { key: "circuits", path: "multi-player/v2/?theme=circuits", toggler: "switch", open: "Open switch", close: "Close switch", examples: ["Simple loop", "Two bulbs in series", "Dimmed bulb", "Motor and bulb"] },
  { key: "safety", path: "multi-player/v2/?theme=safety", toggler: "beacon", open: "Beacon off", close: "Beacon on", examples: ["Sample layout", "Welding bay", "Chemical store", "Loading lane"] },
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

  // v2 only: page marker, rendering, fixed light pool.
  const base2 = await page.evaluate(() => ({ version: window.__mp.version, lights: window.__mp.fx.lights.length, hasEnv: Boolean(window.__mp.scene.environment), pbr: (() => { let n = 0; window.__mp.scene.traverse((o) => { if (o.isMesh && o.material?.isMeshStandardMaterial) n += 1; }); return n; })() }));
  check(`${label} is the v2 page`, base2.version === 2, JSON.stringify(base2));
  check(`${label} has an environment map and PBR materials`, base2.hasEnv && base2.pbr > 20, JSON.stringify(base2));
  const lightsIn = () => page.evaluate(() => { let n = 0; window.__mp.scene.traverse((o) => { if (o.isLight && !o.isAmbientLight) n += 1; }); return n; });
  const lightsStart = await lightsIn();
  const frame = await page.evaluate(async () => {
    const { renderer, scene } = window.__mp;
    renderer.info.autoReset = false; renderer.info.reset();
    await new Promise((resolve) => setTimeout(resolve, 600));
    const calls = renderer.info.render.calls; const frames = renderer.info.render.frame;
    renderer.info.autoReset = true;
    return { calls, frames };
  });
  check(`${label} frames are being drawn in the session`, frame.calls > 20, JSON.stringify(frame));

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

  // v2 effects follow the example: dots flow only in a closed circuit, the light count never changes, the cost stays inside the budget.
  const fxNow = () => page.evaluate(() => ({ dots: window.__mp.fx.stats.dots, rings: window.__mp.fx.stats.rings, lit: window.__mp.fx.stats.lit, glow: window.__mp.fx.lights.some((l) => l.intensity > 0) }));
  const cost = await page.evaluate(() => { const { renderer, scene, camera } = window.__mp; renderer.info.autoReset = false; renderer.info.reset(); renderer.render(scene, camera); const r = renderer.info.render; const out = { calls: r.calls, triangles: r.triangles }; renderer.info.autoReset = true; return out; });
  console.log(`  ${label} render cost in the session (both eyes): ${cost.calls} draw calls, ${cost.triangles} triangles`);
  check(`${label} render cost is within budget (both eyes <= 400 calls, <= 150k triangles)`, cost.calls <= 400 && cost.triangles <= 150000, JSON.stringify(cost));
  await clickTool(page, theme.examples[0]);
  await page.waitForTimeout(700);
  const fxClosed = await fxNow();
  if (theme.key === "circuits") {
    check(`${label} terminal rings drawn and current dots flow when the circuit is closed`, fxClosed.rings >= 6 && fxClosed.dots > 10, JSON.stringify(fxClosed));
    check(`${label} a lit bulb gets a real light from the pool`, fxClosed.lit >= 1 && fxClosed.glow, JSON.stringify(fxClosed));
  } else {
    check(`${label} an active beacon gets a real light from the pool`, fxClosed.lit >= 1, JSON.stringify(fxClosed));
  }
  check(`${label} the light count does not change when parts are added or removed`, (await lightsIn()) === lightsStart, `${lightsStart} vs ${await lightsIn()}`);

  // Y opens, B closes the part under the controller ray.
  await clickTool(page, theme.examples[0]);
  await page.waitForTimeout(400);
  const toggler = await partPoint(page, theme.toggler);
  await aim(page, "left", toggler);
  await press(page, "left", "y-button");
  check(`${label} Y opens / turns off the ${theme.toggler}`, (await stateOf(page, theme.toggler)) === false, String(await stateOf(page, theme.toggler)));
  const mid = await page.evaluate(() => window.__mp.toolboxState().summary);
  if (theme.key === "circuits") {
    check(`${label} the circuit goes open with the switch open`, /Open circuit/.test(mid), mid);
    await page.waitForTimeout(500);
    const fxOpen = await fxNow();
    check(`${label} current dots stop when the switch is open`, fxOpen.dots === 0, JSON.stringify(fxOpen));
  } else {
    await page.waitForTimeout(1200);
    const fxOff = await fxNow();
    check(`${label} the light pool goes dark when the beacon is turned off`, fxOff.lit === 0, JSON.stringify(fxOff));
  }
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
console.log(failures.length ? `\n${failures.length} FAILED, ${passed} passed` : `\nv2 check passed (${passed} checks, EMULATED Quest 3)`);
process.exit(failures.length ? 1 : 0);
