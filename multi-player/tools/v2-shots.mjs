// Screenshots and render-cost numbers for Lab v2 (multi-player/v2). Starts its own relay; needs Playwright and IWER from tools/vr-capture.
//   node multi-player/tools/v2-shots.mjs [out folder]       ONLY=circuits|safety|quest|cost runs one part; FULL=0 forces lite mode
// Desktop shots use ?lite=0 (shadows and glTF props on) on a software renderer (SwiftShader), so they are slow but show the full look.
// The headset shots are an EMULATED Meta Quest 3 (IWER) in desktop Chrome, never a real headset. Frame rate on SwiftShader says nothing
// about a Quest: only triangle and draw-call counts (renderer.info) are meaningful here.
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join as pathJoin } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import WebSocket from "ws";

const here = dirname(fileURLToPath(import.meta.url));
const repo = pathJoin(here, "..", "..");
const capture = pathJoin(repo, "tools", "vr-capture", "node_modules");
const { chromium } = await import(pathToFileURL(pathJoin(capture, "playwright", "index.mjs")).href);
const iwerSource = readFileSync(pathJoin(capture, "iwer", "build", "iwer.min.js"), "utf8");
const { createRelay } = await import(pathToFileURL(pathJoin(repo, "multi-player", "server.mjs")).href);
const out = process.argv[2] ?? pathJoin(repo, "multi-player", "screenshots");
mkdirSync(out, { recursive: true });
const only = process.env.ONLY ?? "";
const wants = (name) => !only || only.split(",").includes(name);

const relay = createRelay();
const { port } = await relay.listen({ host: "127.0.0.1", port: 8184, autoPort: true });
const base = `http://127.0.0.1:${port}/`;
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const problems = [];
const costs = {};

async function newPage({ width = 1440, height = 810, quest = false } = {}) {
  const page = await (await browser.newContext({ viewport: { width, height } })).newPage();
  page.setDefaultTimeout(180000);
  page.on("pageerror", (error) => problems.push(`pageerror: ${error.message}`));
  page.on("console", (message) => { if (message.type() === "error" && !/favicon|logo\.png|Failed to load resource/.test(message.text())) problems.push(`console: ${message.text()}`); });
  await page.route("**/branding/**", (route) => route.fulfill({ status: 404, body: "" })); // the logo is git-ignored; keep it out of committed shots
  if (quest) {
    await page.addInitScript(iwerSource);
    await page.addInitScript(() => { const device = new window.IWER.XRDevice(window.IWER.metaQuest3); device.installRuntime({ forceInstall: true }); window.__device = device; });
  }
  return page;
}
async function join(page, theme, room, query = "lite=0", path = "multi-player/v2/") {
  await page.goto(`${base}${path}?theme=${theme}&${query}`);
  await page.fill("#name", "Builder");
  await page.fill("#room", room);
  await page.click("#join");
  await page.waitForSelector("#workspace:not([hidden])");
}
function peer(theme, room, name, mode) {
  const socket = new WebSocket(`${base.replace("http", "ws")}ws`);
  const ready = new Promise((resolve) => socket.on("open", () => { socket.send(JSON.stringify({ type: "join", room, theme, name, mode })); resolve(); }));
  const send = (m) => socket.readyState === WebSocket.OPEN && socket.send(JSON.stringify(m));
  return { ready, send, close: () => socket.close() };
}
const stats = (page) => page.evaluate(() => {
  const { renderer, scene } = window.__mp;
  renderer.info.autoReset = false; renderer.info.reset();
  renderer.render(scene, window.__mp.camera);
  const info = renderer.info.render;
  const result = { calls: info.calls, triangles: info.triangles, textures: renderer.info.memory.textures, geometries: renderer.info.memory.geometries, programs: renderer.info.programs?.length ?? 0 };
  renderer.info.autoReset = true;
  return result;
});
const settle = (page, ms = 2500) => page.waitForTimeout(ms);
async function loadExample(page, label) {
  await page.evaluate((text) => [...document.querySelectorAll("button")].find((b) => b.textContent.includes(text)).click(), label);
  await settle(page, 1500);
}
async function waitProps(page) {
  await page.evaluate(() => window.__mp.environment.ready);
  // every glTF prop must sit near where it was placed (one model once drew 5 m away from its origin)
  const far = await page.evaluate(async () => {
    const THREE = await import("/vendor/three/three.module.js");
    const out = [];
    window.__mp.environment.group.getObjectByName("props").children.forEach((child) => {
      const box = new THREE.Box3().setFromObject(child); const centre = box.getCenter(new THREE.Vector3()); const at = child.getWorldPosition(new THREE.Vector3());
      const d = Math.hypot(centre.x - at.x, centre.z - at.z);
      if (d > 1.5) out.push(`${child.children[0]?.name ?? "prop"} is ${d.toFixed(1)} m from its placement`);
    });
    return out;
  });
  far.forEach((message) => problems.push(`prop: ${message}`));
}
const setView = (page, pos, target) => page.evaluate(({ pos, target }) => { const { camera } = window.__mp; camera.clearViewOffset(); camera.position.set(...pos); camera.lookAt(...target); camera.updateProjectionMatrix(); }, { pos, target });

// -------- desktop shots
for (const lab of [
  { id: "circuits", name: "circuit-lab", example: "Motor and bulb", second: "Simple loop" },
  { id: "safety", name: "safety-lab", example: "Welding bay", second: "Loading lane" },
]) {
  if (!wants(lab.id) && !wants("desktop")) continue;
  const room = `v2-${lab.id}-${Date.now() % 100000}`;
  const page = await newPage();
  await join(page, lab.id, room, process.env.FULL === "0" ? "lite=1" : "lite=0");
  const guests = [peer(lab.id, room, "Priya", "web"), peer(lab.id, room, "Marcus", "web")];
  await Promise.all(guests.map((g) => g.ready));
  guests[0].send({ type: "cursor", position: [-2.6, 1.9] });
  guests[1].send({ type: "cursor", position: [2.7, 1.2] });
  await waitProps(page);
  await loadExample(page, lab.example);
  await page.evaluate(() => { document.querySelector("#panel").style.display = "none"; });
  await settle(page, 4000);
  const wide = lab.id === "circuits" ? [[2.4, 2.4, 3.6], [0, 0.3, 0]] : [[2.8, 2.9, 4.6], [0, 0.2, 0]];
  await setView(page, ...wide);
  await settle(page, 2500);
  await page.screenshot({ path: pathJoin(out, `v2-${lab.name}-desktop.png`) });
  costs[`${lab.id} desktop (full, shadows on)`] = await stats(page);
  if (lab.id === "circuits") {
    await setView(page, [1.4, 1.1, 1.8], [0.2, 0.15, -0.5]);
    await settle(page, 2500);
    await page.screenshot({ path: pathJoin(out, "v2-circuit-lab-closeup.png") });
    await setView(page, [-1.2, 0.6, 1.0], [0.8, 0.2, -0.6]);
    await settle(page, 2000);
    await page.screenshot({ path: pathJoin(out, "v2-circuit-lab-bulb.png") });
  } else {
    await setView(page, [1.6, 1.3, 3.2], [0, 0.3, 0.2]);
    await settle(page, 2500);
    await page.screenshot({ path: pathJoin(out, "v2-safety-lab-closeup.png") });
  }
  guests.forEach((g) => g.close());
  await page.context().close();
}

// -------- AR Circuit Lab, desktop preview (multi-player/ar-circuit). NOT an AR session: desktop Chrome cannot start one.
if (wants("ar") || wants("desktop")) {
  const room = `v2-ar-${Date.now() % 100000}`;
  const page = await newPage();
  await join(page, "circuits", room, process.env.FULL === "0" ? "lite=1" : "lite=0", "multi-player/ar-circuit/");
  const guests = [peer("circuits", room, "Priya", "web"), peer("circuits", room, "Marcus", "web")];
  await Promise.all(guests.map((g) => g.ready));
  guests[0].send({ type: "cursor", position: [-2.4, -1.6] });
  guests[1].send({ type: "cursor", position: [2.7, -1.5] });
  await waitProps(page);
  await loadExample(page, "Simple loop");
  await page.evaluate(() => { document.querySelector("#panel").style.display = "none"; document.querySelector("#xr-hint").hidden = true; });
  await settle(page, 4000);
  await setView(page, [3.4, 2.7, 4.8], [-0.6, 0.5, -1.2]);
  await settle(page, 2500);
  await page.screenshot({ path: pathJoin(out, "ar-circuit-desktop.png") });
  costs["ar-circuit desktop preview (full, shadows on)"] = await stats(page);
  guests.forEach((g) => g.close());
  await page.context().close();
}

// -------- emulated Quest 3
if (wants("quest")) {
  for (const lab of [{ id: "circuits", name: "circuit-lab", example: "Simple loop" }, { id: "safety", name: "safety-lab", example: "Sample layout" }]) {
    const room = `v2q-${lab.id}-${Date.now() % 100000}`;
    const page = await newPage({ width: 1280, height: 760, quest: true });
    await join(page, lab.id, room, "lite=1");
    await page.click("#enter-xr");
    await page.waitForFunction(() => window.__mp.renderer.xr.isPresenting, null, { timeout: 20000 });
    await page.waitForTimeout(1500);
    // put the headset at standing height looking at the table-top model
    await page.evaluate(() => { window.__device.position.set(0.5, 1.55, 0.4); window.__device.quaternion.set(-0.18, 0.12, 0, 0.976); });
    // load the example through the toolbox (the page buttons are not visible in VR, but the same code path)
    await page.evaluate((label) => { const { toolbox } = window.__mp; const b = toolbox.buttons.find((x) => x.label === label); toolbox.press(b); }, lab.example);
    await page.waitForTimeout(3500);
    await page.screenshot({ path: pathJoin(out, `v2-${lab.name}-quest.png`) });
    costs[`${lab.id} emulated Quest 3 (in session, no shadow map)`] = await stats(page);
    await page.context().close();
  }
}

await browser.close();
await relay.close();
writeFileSync(pathJoin(out, "v2-render-cost.json"), JSON.stringify(costs, null, 2));
console.log(JSON.stringify(costs, null, 2));
if (problems.length) { console.log("PROBLEMS:\n" + problems.slice(0, 12).join("\n")); process.exitCode = 1; }
