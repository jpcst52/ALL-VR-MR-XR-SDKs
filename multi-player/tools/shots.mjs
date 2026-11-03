// Screenshots and measurements for the upgraded multiplayer scenes (rooms with props, avatars, name tags).
//   node multi-player/tools/shots.mjs [out folder]      (relay + static server must be running; BASE defaults to http://localhost:8162/)
//   ONLY=circuit-lab|site-safety|test-bay|quest|fallback  runs one part
// Needs Playwright and IWER from tools/vr-capture (npm install there first). The other participants are scripted WebSocket
// clients (a desktop guest walking about, a "headset" guest sending head and hand poses), so only one browser page renders.
// The headset view is an EMULATED Meta Quest 3 (IWER), not a real headset. Prints triangle and draw-call counts per scene.
import { copyFileSync, mkdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import WebSocket from "ws";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const capture = join(repo, "tools", "vr-capture", "node_modules");
const { chromium } = await import(pathToFileURL(join(capture, "playwright", "index.mjs")).href);
const iwerSource = readFileSync(join(capture, "iwer", "build", "iwer.min.js"), "utf8");
const base = process.env.BASE ?? "http://localhost:8162/";
const out = process.argv[2] ?? join(repo, "multi-player", "screenshots");
mkdirSync(out, { recursive: true });

const browser = await chromium.launch({
  channel: "chrome",
  args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"],
});
const problems = [];

async function newPage({ width = 1280, height = 720, quest = false, block = false } = {}) {
  const page = await (await browser.newContext({ viewport: { width, height } })).newPage();
  page.setDefaultTimeout(120000);
  page.on("pageerror", (error) => problems.push(`pageerror: ${error.message}`));
  page.on("console", (message) => {
    const url = message.location().url;
    if (message.type() === "error" && !/favicon|branding\/logo/.test(url) && !(block && /models-mp/.test(url))) {
      problems.push(`console: ${message.text()} ${url}`);
    }
  });
  // The university logo is git-ignored; keep it out of committed screenshots.
  await page.route("**/branding/**", (route) => route.fulfill({ status: 404, body: "" }));
  if (block) await page.route("**/assets/models-mp/**", (route) => route.fulfill({ status: 404, body: "" }));
  if (quest) {
    await page.addInitScript(iwerSource);
    await page.addInitScript(() => {
      const device = new window.IWER.XRDevice(window.IWER.metaQuest3);
      device.installRuntime({ forceInstall: true });
      window.__device = device;
    });
  }
  return page;
}

async function joinRoom(page, path, name, room, query = "") {
  await page.goto(`${base}${path}${query}`);
  await page.fill("#name", name);
  await page.fill("#room", room);
  await page.click("#join");
  await page.waitForSelector("#workspace:not([hidden])");
}

// A scripted participant (no browser): joins over the relay's WebSocket and sends cursors or poses.
function scriptedPeer(theme, room, name, mode) {
  const socket = new WebSocket(`${base.replace("http", "ws")}ws`);
  const ready = new Promise((resolve) => socket.on("open", () => {
    socket.send(JSON.stringify({ type: "join", room, theme, name, mode }));
    resolve();
  }));
  const send = (message) => socket.readyState === WebSocket.OPEN && socket.send(JSON.stringify(message));
  return { ready, send, close: () => socket.close() };
}

// Walk a cursor from one board position to another so the avatar is seen walking, then standing still.
async function walk(peer, from, to, steps = 24) {
  for (let step = 0; step <= steps; step += 1) {
    const t = step / steps;
    peer.send({ type: "cursor", position: [from[0] + (to[0] - from[0]) * t, from[1] + (to[1] - from[1]) * t] });
    await new Promise((resolve) => setTimeout(resolve, 45));
  }
}

const sendPose = (peer, head, target, hands) => peer.send({
  type: "pose", head, yaw: Math.atan2(target[0] - head[0], target[2] - head[2]), hands,
});

async function measure(page) {
  return page.evaluate(() => {
    const { renderer, environment, peers } = window.__mp;
    const info = renderer.info;
    return {
      triangles: info.render.triangles,
      drawCalls: info.render.calls,
      geometries: info.memory.geometries,
      textures: info.memory.textures,
      shadows: renderer.shadowMap.enabled,
      envProps: environment.stats,
      avatarsWithModel: [...peers.values()].filter((peer) => peer.avatar.usesModel).length,
      peers: peers.size,
    };
  });
}

// Waits until the room props and every avatar model finished loading.
async function settle(page, expectPeers) {
  await page.waitForFunction((count) => {
    const { environment, peers } = window.__mp;
    const done = environment.stats.loaded + environment.stats.fallbacks === environment.stats.requested;
    return done && peers.size >= count && [...peers.values()].every((peer) => peer.avatar.state.ready);
  }, expectPeers, { timeout: 90000 });
  await page.waitForTimeout(1500);
}

const themes = [
  ["multi-player/vr/", "circuits", "circuit-lab"],
  ["multi-player/ar/", "safety", "site-safety"],
  ["multi-player/hydrotest/", "hydrotest", "test-bay"],
];
const report = {};
const stamp = Date.now() % 100000;
const only = process.env.ONLY;
for (const [path, theme, key] of themes) {
  if (only && only !== key) continue;
  const room = `shots-${stamp}-${key}`.slice(0, 24);
  const host = await newPage({ width: 1440, height: 810 });
  await joinRoom(host, path, "Builder", room, "?shadows=1&props=1");
  if (key === "test-bay") {
    await settle(host, 0);
    await host.screenshot({ path: join(out, "hydrotest-empty.png") });
  }
  const dana = scriptedPeer(theme, room, "Dana", "web");
  const marcus = scriptedPeer(theme, room, "Marcus", "vr");
  const priya = scriptedPeer(theme, room, "Priya", "web");
  await Promise.all([dana.ready, marcus.ready, priya.ready]);
  await host.click("#load-demo");
  await settle(host, 3);
  // Marcus is "in a headset": a head and two hands. Dana and Priya are desktop users who walk to a spot on the board edge.
  sendPose(marcus, [3.4, 1.62, -0.4], [0, 0, 0], [[2.9, 1.2, -0.7], [3.0, 1.3, 0.0]]);
  priya.send({ type: "cursor", position: [-1.6, -1.2] });
  await walk(dana, [2, 3], [0.9, -1.7]);
  await host.waitForTimeout(3500);
  await host.screenshot({ path: join(out, `${key}-avatars.png`) });
  // The names the README and the deck already use.
  const legacy = { "circuit-lab": "multiplayer-vr.png", "site-safety": "multiplayer-ar.png", "test-bay": "hydrotest-rig.png" }[key];
  copyFileSync(join(out, `${key}-avatars.png`), join(out, legacy));
  if (key === "test-bay") {
    const second = await newPage({ width: 1280, height: 800 });
    await joinRoom(second, path, "Blair", room, "?shadows=1&props=1");
    await settle(second, 4);
    await second.screenshot({ path: join(out, "hydrotest-rig-client2.png") });
    await second.context().close();
  }
  dana.close(); marcus.close(); priya.close();
  await host.context().close();
  // Budget measurement as a headset sees it: no shadow pass.
  const lean = await newPage({ width: 1280, height: 720 });
  await joinRoom(lean, path, "Builder", room, "?shadows=0&props=1");
  const a = scriptedPeer(theme, room, "A", "web");
  const b = scriptedPeer(theme, room, "B", "vr");
  await Promise.all([a.ready, b.ready]);
  await lean.click("#load-demo");
  await settle(lean, 2);
  await lean.waitForTimeout(1000);
  report[key] = await measure(lean);
  console.log(key, JSON.stringify(report[key]));
  a.close(); b.close();
  await lean.context().close();
}

// An emulated Quest participant: the desktop host sees a character under the headset with floating gloves at the controllers.
if (!only || only === "quest") {
  const room = `shots-${stamp}-quest`.slice(0, 24);
  const host = await newPage({ width: 1440, height: 810 });
  const quest = await newPage({ width: 1280, height: 720, quest: true });
  await joinRoom(host, "multi-player/vr/", "Builder", room, "?shadows=1&props=1");
  await joinRoom(quest, "multi-player/vr/", "Quest Guest", room, "?shadows=0&props=0");
  const dana = scriptedPeer("circuits", room, "Dana", "web");
  const priya = scriptedPeer("circuits", room, "Priya", "web");
  await Promise.all([dana.ready, priya.ready]);
  await host.click("#load-demo");
  await settle(host, 3);
  priya.send({ type: "cursor", position: [-1.6, -1.2] });
  await walk(dana, [2, 3], [0.9, -1.7]);
  await quest.click("#enter-xr");
  await quest.waitForTimeout(3000);
  // Walk the emulated headset to one side of the board (the board is a 0.4 scale model in front of the player).
  await quest.evaluate(() => {
    const d = window.__device;
    d.position.set(0.9, 1.65, -0.5);
    d.quaternion.set(0, 0.2, 0, 0.98);
    d.controllers.left.position.set(0.55, 1.2, -0.85);
    d.controllers.right.position.set(1.05, 1.25, -0.95);
  });
  await quest.waitForTimeout(3000);
  mkdirSync(join(out, "vr"), { recursive: true });
  await host.screenshot({ path: join(out, "vr", "02-quest-peer-seen-by-desktop.png") });
  await quest.screenshot({ path: join(out, "vr", "01-circuit-lab-in-vr.png") });
  console.log("quest peer pose seen by host:", JSON.stringify(await host.evaluate(() => [...window.__mp.peers.values()].map((p) => p.avatar.state.pose))));
  dana.close(); priya.close();
  await host.context().close();
  await quest.context().close();
}

// Fallback: every model request fails (as if the assets were missing). The page must still work with primitives.
if (!only || only === "fallback") {
  const room = `shots-${stamp}-fb`.slice(0, 24);
  const host = await newPage({ width: 1440, height: 810, block: true });
  await joinRoom(host, "multi-player/vr/", "Builder", room, "?shadows=1&props=1");
  const dana = scriptedPeer("circuits", room, "Dana", "web");
  await dana.ready;
  await host.click("#load-demo");
  await settle(host, 1);
  await walk(dana, [2, 3], [0.9, -1.7]);
  await host.waitForTimeout(3000);
  await host.screenshot({ path: join(out, "fallback-no-models.png") });
  const result = await measure(host);
  console.log("fallback", JSON.stringify(result));
  if (result.envProps.fallbacks !== result.envProps.requested || result.avatarsWithModel !== 0) problems.push("fallback did not engage fully");
  dana.close();
  await host.context().close();
}

await browser.close();
console.log(problems.length ? `Problems:\n${problems.join("\n")}` : "No page errors.");
process.exit(problems.length ? 1 : 0);
