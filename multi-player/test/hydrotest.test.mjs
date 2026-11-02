import assert from "node:assert/strict";
import { after, before, test } from "node:test";
import WebSocket from "ws";
import { createRelay } from "../server.mjs";
import { evaluateHydro } from "../shared/hydro-eval.mjs";

const at = (part, x, z, extra = {}) => ({ part, position: [x, 0.08, z], rotation: [0, 0, 0], on: false, ...extra });

// Same layout as the demo rig in themes.js.
function rig(overrides = {}) {
  const items = [
    at("tank", -2.35, 0.3),
    at("pump", -1.6, 0.3),
    at("pipe", -0.8, 0.3),
    at("valve", -0.1, 0.3, { on: true }),
    at("exchanger", 1.1, 0.3),
    at("blind", 0.6, 0.3),
    at("blind", 1.6, 0.3),
    at("vent", 1.1, 0.85, { on: true }),
    at("gauge", -0.6, 0.7),
  ];
  return items.filter((item, index) => !(overrides.drop ?? []).includes(index));
}

test("a complete rig is ready to fill", () => {
  const result = evaluateHydro(rig());
  assert.equal(result.status, "Ready to fill");
  assert.equal(result.ready, true);
  assert.ok(result.checks.every((check) => check.ok));
});

test("a missing blind flange is flagged", () => {
  const result = evaluateHydro(rig({ drop: [6] }));
  assert.match(result.status, /^Blind a nozzle/);
  assert.equal(result.ready, false);
  assert.equal(result.checks.find((check) => check.id === "blinds").ok, false);
});

test("a closed vent traps air", () => {
  const items = rig();
  items[7].on = false;
  const result = evaluateHydro(items);
  assert.equal(result.status, "Vent is closed: air will be trapped");
  assert.equal(result.ready, false);
});

test("no fill path without a connection, or with a closed valve", () => {
  const cut = evaluateHydro(rig({ drop: [2] }));
  assert.equal(cut.status, "Connect the tank to the exchanger with pipe");
  assert.equal(cut.checks.find((check) => check.id === "fill").ok, false);

  const items = rig();
  items[3].on = false;
  const closed = evaluateHydro(items);
  assert.equal(closed.status, "Open the fill valve");
  assert.equal(closed.ready, false);
});

test("an empty board asks for the exchanger", () => {
  assert.equal(evaluateHydro([]).status, "Place the heat exchanger");
});

// ------------------------------------------------------------ relay

let relay;
let baseUrl;

before(async () => {
  relay = createRelay();
  const address = await relay.listen({ host: "127.0.0.1", port: 0 });
  baseUrl = `ws://127.0.0.1:${address.port}`;
});

after(async () => {
  await relay.close();
});

function connect(room, theme) {
  return new Promise((resolve, reject) => {
    const socket = new WebSocket(`${baseUrl}/ws`);
    const messages = [];
    socket.on("message", (raw) => {
      const message = JSON.parse(raw.toString());
      messages.push(message);
      if (message.type === "welcome") resolve({ socket, messages });
    });
    socket.on("error", reject);
    socket.on("open", () => socket.send(JSON.stringify({ type: "join", room, name: "T", mode: "web", theme })));
  });
}

const pause = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const place = (client, part) => client.socket.send(JSON.stringify({ type: "place", part, position: [0, 0.08, 0], rotation: [0, 0, 0] }));

test("a hydrotest room accepts its parts, toggles valves, and rejects circuit parts", async () => {
  const client = await connect("hydro-room", "hydrotest");
  try {
    place(client, "exchanger");
    place(client, "battery");
    place(client, "vent");
    await pause(150);
    const added = client.messages.filter((message) => message.type === "object-added");
    assert.deepEqual(added.map((message) => message.object.part), ["exchanger", "vent"]);

    client.socket.send(JSON.stringify({ type: "toggle", id: added[1].object.id, on: true }));
    client.socket.send(JSON.stringify({ type: "toggle", id: added[0].object.id, on: true }));
    await pause(150);
    const toggles = client.messages.filter((message) => message.type === "toggle-state");
    assert.equal(toggles.length, 1);
    assert.equal(toggles[0].on, true);
  } finally {
    client.socket.close();
  }
});
