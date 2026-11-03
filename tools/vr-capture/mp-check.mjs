// Multiplayer WebXR check: several REAL browser contexts join the same room over the real WebSocket relay and use the pages.
//   node tools/vr-capture/mp-check.mjs        (MP_BASE selects a running relay; by default one is started here on port 8179)
// Per example (Circuit Lab, Site Safety Layout, Hydrostatic Test Rig): two desktop contexts and one phone-sized touch context join,
// load the demo, drag a part and see it move on the other screens, add a part by arming it (a plain click on empty board adds nothing),
// move a part with the keyboard, and hold a part against a second grabber. Then a headset participant (EMULATED Quest 3, IWER) enters
// VR and the others see it labelled VR while the desktop contexts stay unlabelled. Also: touch targets and text sizes, the relay
// going away and coming back (auto reconnect), a full room, and a relay that is not running. Desktop Chrome and an emulator only.
import { pathToFileURL } from "node:url";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const { chromium } = await import(pathToFileURL(join(here, "node_modules", "playwright", "index.mjs")).href);
const iwer = readFileSync(join(here, "node_modules", "iwer", "build", "iwer.min.js"), "utf8");
const WebSocket = (await import(pathToFileURL(join(repo, "multi-player", "node_modules", "ws", "wrapper.mjs")).href)).default;
const { createRelay } = await import(pathToFileURL(join(repo, "multi-player", "server.mjs")).href);

let relay = null;
let base = process.env.MP_BASE;
const OWN_PORT = Number(process.env.MP_PORT ?? 8179);
if (!base) {
  relay = createRelay();
  await relay.listen({ host: "127.0.0.1", port: OWN_PORT });
  base = `http://127.0.0.1:${OWN_PORT}/`;
}
const url = new URL(base);

let passed = 0;
const failures = [];
function check(name, ok, detail = "") {
  if (ok) passed += 1;
  else { failures.push(`${name} ${detail}`); console.log(`FAIL ${name} ${detail}`); }
}

const browser = await chromium.launch({
  channel: "chrome",
  args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"],
});
const pageErrors = [];
async function page({ width = 1280, height = 760, touch = false, quest = false } = {}) {
  const context = await browser.newContext({ viewport: { width, height }, hasTouch: touch, isMobile: touch });
  const p = await context.newPage();
  p.setDefaultTimeout(60000);
  p.on("pageerror", (error) => pageErrors.push(error.message));
  p.on("console", (message) => {
    if (message.type() === "error" && !/favicon|logo\.png|Failed to load resource|:59999/.test(message.text() + message.location().url)) pageErrors.push(message.text());
  });
  await p.route("**/branding/**", (route) => route.fulfill({ status: 404, body: "" }));
  if (quest) {
    await p.addInitScript(iwer);
    await p.addInitScript(() => {
      const device = new window.IWER.XRDevice(window.IWER.metaQuest3);
      device.installRuntime({ forceInstall: true });
      window.__device = device;
    });
  }
  return p;
}
async function joinAs(p, path, name, room, query = "?lite=1") {
  await p.goto(`${base}${path}${query}`);
  await p.fill("#name", name);
  await p.fill("#room", room);
  await p.click("#join");
  await p.waitForSelector("#workspace:not([hidden])");
}
const positions = (p) => p.evaluate(() => Object.fromEntries([...window.__mp.objects].map(([id, o]) => [id, o.data.position.map((n) => Math.round(n * 100) / 100)])));
const screenOf = (p, id) => p.evaluate((objectId) => {
  const o = window.__mp.objects.get(objectId);
  const v = o.group.getWorldPosition(o.group.position.clone());
  v.y += 0.12;
  v.project(window.__mp.camera);
  const r = window.__mp.renderer.domElement.getBoundingClientRect();
  return [((v.x + 1) / 2) * r.width + r.left, ((1 - v.y) / 2) * r.height + r.top];
}, id);
const settle = (ms = 700) => new Promise((resolve) => setTimeout(resolve, ms));

const examples = [
  ["multi-player/vr/", "Circuit Lab", "cl"],
  ["multi-player/ar/", "Site Safety Layout", "ss"],
  ["multi-player/hydrotest/", "Hydrostatic Test Rig", "ht"],
];
const only = process.env.ONLY;
const run = Date.now() % 100000;

for (const [path, title, key] of examples) {
  if (only && only !== key) continue;
  const room = `mpc-${key}-${run}`;
  const A = await page();
  const B = await page();
  const C = await page({ width: 390, height: 844, touch: true });
  await joinAs(A, path, "Alice", room);
  await joinAs(B, path, "Bob", room);
  await joinAs(C, path, "Cy", room);
  await A.waitForFunction(() => window.__mp.peers.size === 2);

  // Join flow: compact room bar replaces the form; the lite note and steps show before joining.
  check(`${key} room bar`, (await A.textContent("#room-line")).includes(room));
  check(`${key} form hidden once joined`, !(await A.isVisible("#join")));
  check(`${key} roster says who is a desktop browser`, !/\(VR\)|\(AR\)/.test(await A.textContent("#roster")) && (await A.textContent("#roster")).startsWith("3 in room"));

  await A.click("#load-demo");
  await A.waitForFunction(() => window.__mp.objects.size > 4);
  await settle();
  const pa = await positions(A);
  check(`${key} demo synced to all`, JSON.stringify(pa) === JSON.stringify(await positions(B)) && JSON.stringify(pa) === JSON.stringify(await positions(C)));

  // B drags a part; A and C must see it at the same place; A cannot grab it while B holds it.
  const ids = Object.keys(pa);
  let dragged = null;
  for (const id of ids) {
    const [x, y] = await screenOf(B, id);
    const hit = await B.evaluate(([px, py]) => document.elementFromPoint(px, py)?.tagName, [x, y]);
    if (hit === "CANVAS") { dragged = { id, x, y }; break; }
  }
  check(`${key} found a part to drag`, Boolean(dragged));
  if (dragged) {
    const { id, x, y } = dragged;
    await B.mouse.move(x, y);
    await B.mouse.down();
    await B.mouse.move(x + 70, y + 24, { steps: 8 });
    await settle(500);
    const [ax, ay] = await screenOf(A, id);
    await A.mouse.move(ax, ay);
    await A.mouse.down();
    await A.mouse.up();
    await settle(300);
    const heldMessage = await A.textContent("#status");
    const aHeld = await A.evaluate((i) => window.__mp.objects.get(i).heldBy !== null, id);
    await B.mouse.up();
    await settle(700);
    const pb = await positions(B);
    check(`${key} drag moved the part`, JSON.stringify(pb[id]) !== JSON.stringify(pa[id]), `${pa[id]} -> ${pb[id]}`);
    check(`${key} drag seen on other screens`, JSON.stringify((await positions(A))[id]) === JSON.stringify(pb[id]) && JSON.stringify((await positions(C))[id]) === JSON.stringify(pb[id]));
    check(`${key} second grabber is refused while held`, aHeld, heldMessage);
    check(`${key} released after drag`, await A.evaluate((i) => window.__mp.objects.get(i).heldBy === null, id));
  }

  // A plain click on the empty board adds nothing; arming a part and clicking adds one; Escape disarms.
  const before = (await positions(A)) && Object.keys(await positions(A)).length;
  const spot = await A.evaluate(() => {
    const r = window.__mp.renderer.domElement.getBoundingClientRect();
    return [r.left + r.width * 0.68, r.top + r.height * 0.78];
  });
  await A.mouse.click(spot[0], spot[1]);
  await settle(400);
  check(`${key} click on empty board does not add a part`, Object.keys(await positions(A)).length === before);
  await A.click("#parts button >> nth=0");
  await A.waitForFunction((n) => window.__mp.objects.size === n + 1, before);
  await A.mouse.click(spot[0], spot[1]);
  await A.waitForFunction((n) => window.__mp.objects.size === n + 2, before);
  await settle(400);
  check(`${key} armed click adds a part everyone sees`, Object.keys(await positions(B)).length === before + 2);
  await A.keyboard.press("Escape");
  await A.mouse.click(spot[0] - 60, spot[1] + 10);
  await settle(400);
  check(`${key} Escape stops adding`, Object.keys(await positions(A)).length === before + 2);

  // Keyboard: ] selects a part, arrow keys move it, B sees it.
  await A.focus("canvas.scene");
  await A.keyboard.press("]");
  const selected = await A.evaluate(() => window.__mp.objects.size);
  const statusAfter = await A.textContent("#status");
  check(`${key} ] selects a part and says so`, /Selected .* of \d+/.test(statusAfter), statusAfter);
  const pBefore = await positions(B);
  await A.keyboard.press("ArrowRight");
  await A.keyboard.press("ArrowRight");
  await settle(600);
  const pAfter = await positions(B);
  const moved = Object.keys(pAfter).filter((id) => JSON.stringify(pAfter[id]) !== JSON.stringify(pBefore[id]));
  check(`${key} arrow keys move the selected part for everyone`, moved.length === 1 && Math.abs(pAfter[moved[0]][0] - pBefore[moved[0]][0] - 0.2) < 0.02, JSON.stringify(moved));
  check(`${key} keyboard move left it released`, selected > 0 && (await A.evaluate(() => [...window.__mp.objects.values()].every((o) => o.heldBy === null))));

  // Touch targets, text size and names on the panel (desktop and phone width).
  for (const [who, p] of [["desktop", A], ["phone", C]]) {
    const audit = await p.evaluate(() => {
      const small = [];
      for (const el of document.querySelectorAll("#panel button, #panel a, #panel input")) {
        const r = el.getBoundingClientRect();
        if (r.width && (r.height < 43.5 || r.width < 43.5)) small.push(`${el.id || el.textContent.trim().slice(0, 16)} ${Math.round(r.width)}x${Math.round(r.height)}`);
      }
      const tiny = [...document.querySelectorAll("#panel p, #panel label, #panel button, #panel a, #panel li, #panel span")]
        .filter((el) => !el.closest("[data-pitt-brand]") && el.getBoundingClientRect().width && parseFloat(getComputedStyle(el).fontSize) < 12)
        .map((el) => `${el.textContent.trim().slice(0, 16)} ${getComputedStyle(el).fontSize}`);
      const unnamed = [...document.querySelectorAll("button, input, canvas")].filter((el) => !(el.getAttribute("aria-label") || el.textContent.trim() || el.labels?.length)).map((el) => el.tagName);
      return { small, tiny, unnamed };
    });
    check(`${key} ${who} controls are at least 44 px`, audit.small.length === 0, audit.small.join("; "));
    check(`${key} ${who} no text under 12 px`, audit.tiny.length === 0, audit.tiny.join("; "));
    check(`${key} ${who} every control has a name`, audit.unnamed.length === 0, audit.unnamed.join(","));
  }

  // Desktop participants pointing at the same spot must not stand inside each other.
  const sameSpot = await B.evaluate(() => [window.innerWidth * 0.6, window.innerHeight * 0.5]);
  for (const p of [A, B]) { await p.mouse.move(sameSpot[0], sameSpot[1]); await p.mouse.move(sameSpot[0] + 2, sameSpot[1] + 1); }
  await settle(2200);
  const gap = await C.evaluate(() => {
    const list = [...window.__mp.peers.values()].map((peer) => peer.avatar.state);
    let min = Infinity;
    for (let i = 0; i < list.length; i += 1) for (let j = i + 1; j < list.length; j += 1) min = Math.min(min, Math.hypot(list[i].x - list[j].x, list[i].z - list[j].z));
    return min;
  });
  check(`${key} desktop avatars keep apart`, gap > 0.9, `closest ${gap.toFixed(2)} m`);

  // Headset participant (emulated Quest 3): everyone else sees it labelled VR; desktop contexts stay plain.
  if (key === "cl") {
    const Q = await page({ quest: true });
    await joinAs(Q, path, "Quest", room, "?lite=1");
    await Q.click("#enter-xr");
    await A.waitForFunction(() => [...window.__mp.peers.values()].some((peer) => peer.mode === "vr"), null, { timeout: 15000 }).catch(() => {});
    const roster = await A.textContent("#roster");
    check(`${key} emulated headset shows as VR in the roster`, /Quest \(VR\)/.test(roster) && !/Bob \(VR\)|Cy \(VR\)/.test(roster), roster);
    await Q.evaluate(() => window.__device?.controllers?.right && (window.__device.position.set(0.8, 1.6, -0.3)));
    const gotPose = await A.waitForFunction(() => [...window.__mp.peers.values()].some((peer) => peer.avatar.hasPose), null, { timeout: 20000 }).then(() => true, () => false);
    check(`${key} headset pose reaches desktop avatar`, gotPose);
    await Q.evaluate(() => window.__mp.renderer.xr.getSession()?.end());
    await settle(1200);
    check(`${key} leaving VR returns the label to a plain name`, !/Quest \(VR\)/.test(await A.textContent("#roster")), await A.textContent("#roster"));
    await Q.context().close();
  }

  // Leave room.
  await C.click("#leave");
  await C.waitForSelector("#workspace", { state: "hidden" });
  check(`${key} Leave room returns to the form`, (await C.isVisible("#join")) && /left the room/.test(await C.textContent("#status")));
  await A.waitForFunction(() => window.__mp.peers.size === 1);
  await A.context().close(); await B.context().close(); await C.context().close();
}

if (!only || only === "cl") {
  // Relay not running: a clear message, no endless retries.
  const dead = await page();
  await dead.goto(`${base}multi-player/vr/?lite=1`);
  // The page is served, but its WebSocket is pointed at a closed port, as if the relay were not running.
  await dead.evaluate(() => {
    const Real = window.WebSocket;
    window.WebSocket = class extends Real { constructor(u) { super(u.replace(/:\d+/, ":59999")); } };
  });
  await dead.fill("#room", "nobody");
  await dead.click("#join");
  await dead.waitForFunction(() => /Could not reach the room relay/.test(document.querySelector("#status").textContent), null, { timeout: 15000 }).catch(() => {});
  const deadStatus = await dead.textContent("#status");
  check("relay down: message says what to do", /Could not reach the room relay/.test(deadStatus) && /npm start/.test(deadStatus), deadStatus);
  check("relay down: shown as an error and Join is usable again", (await dead.getAttribute("#status", "data-state")) === "error" && (await dead.isEnabled("#join")));
  await dead.context().close();

  // Room full (24 scripted participants), then the 25th joins from the page.
  const sockets = [];
  const wsUrl = `ws://${url.host}/ws`;
  for (let i = 0; i < 24; i += 1) {
    const socket = new WebSocket(wsUrl);
    await new Promise((resolve) => socket.on("open", () => { socket.send(JSON.stringify({ type: "join", room: `full-${run}`, theme: "circuits", name: `P${i}`, mode: "web" })); resolve(); }));
    sockets.push(socket);
  }
  await settle(500);
  const late = await page();
  await late.goto(`${base}multi-player/vr/?lite=1`);
  await late.fill("#room", `full-${run}`);
  await late.click("#join");
  await late.waitForFunction(() => /full/i.test(document.querySelector("#status").textContent), null, { timeout: 10000 }).catch(() => {});
  const fullStatus = await late.textContent("#status");
  check("room full: says so and suggests another name", /full/i.test(fullStatus) && /another room name/i.test(fullStatus), fullStatus);
  check("room full: no retry loop and Join is enabled", await late.isEnabled("#join"));
  await late.context().close();
  sockets.forEach((socket) => socket.close());

  // Relay restart: the page reconnects by itself (only when this script owns the relay).
  if (relay) {
    const R = await page();
    await joinAs(R, "multi-player/vr/", "Rita", `back-${run}`);
    await R.click("#load-demo");
    await R.waitForFunction(() => window.__mp.objects.size > 4);
    await relay.close();
    await R.waitForFunction(() => /Connection lost/.test(document.querySelector("#status").textContent), null, { timeout: 10000 }).catch(() => {});
    const lostStatus = await R.textContent("#status");
    check("relay stops: page says the connection was lost and that it will retry", /Connection lost/.test(lostStatus) && /try \d of 5/.test(lostStatus), lostStatus);
    check("relay stops: warning style", (await R.getAttribute("#status", "data-state")) === "warn");
    relay = createRelay();
    await relay.listen({ host: "127.0.0.1", port: OWN_PORT });
    await R.waitForSelector("#workspace:not([hidden])", { timeout: 20000 }).catch(() => {});
    const backStatus = await R.textContent("#status");
    check("relay returns: page reconnects without a click", /Reconnected/.test(backStatus), backStatus);
    check("relay returns: says earlier parts are gone (the relay keeps rooms in memory)", /earlier parts are gone/.test(backStatus), backStatus);
    await R.context().close();
  }
}

check("no page errors", pageErrors.length === 0, pageErrors.slice(0, 3).join(" | "));
await browser.close();
if (relay) await relay.close();
console.log(`mp-check: ${passed} passed, ${failures.length} failed`);
process.exit(failures.length ? 1 : 0);
