import assert from "node:assert/strict";
import { after, before, test } from "node:test";
import WebSocket from "ws";
import { createRelay } from "../server.mjs";
import { spreadAngles } from "../shared/avatar-logic.mjs";

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

function connect(room, { name = "Guest", mode = "web" } = {}) {
  return new Promise((resolve, reject) => {
    const socket = new WebSocket(`${baseUrl}/ws`);
    const messages = [];
    socket.on("message", (raw) => {
      const message = JSON.parse(raw.toString());
      messages.push(message);
      if (message.type === "welcome") resolve({ socket, messages, selfId: message.selfId });
    });
    socket.on("error", reject);
    socket.on("open", () => socket.send(JSON.stringify({ type: "join", room, name, mode })));
  });
}

const send = (client, message) => client.socket.send(JSON.stringify(message));
const pause = (ms = 80) => new Promise((resolve) => setTimeout(resolve, ms));

test("a client can report that it entered or left an XR session, and late joiners see the current mode", async () => {
  const a = await connect("mode-room", { name: "A" });
  const b = await connect("mode-room", { name: "B" });
  send(a, { type: "mode", mode: "vr" });
  await pause();
  const change = b.messages.find((message) => message.type === "peer-mode");
  assert.deepEqual({ id: change.id, mode: change.mode }, { id: a.selfId, mode: "vr" });
  const late = await connect("mode-room", { name: "Late" });
  assert.equal(late.messages.find((message) => message.type === "welcome").peers.find((peer) => peer.id === a.selfId).mode, "vr");
  // Leaving the session drops the stale pose so nobody keeps a ghost headset.
  send(a, { type: "pose", head: [0, 1.6, 1], yaw: 0, hands: [null, null] });
  await pause(120);
  send(a, { type: "mode", mode: "web" });
  await pause();
  const after = await connect("mode-room", { name: "After" });
  const profile = after.messages.find((message) => message.type === "welcome").peers.find((peer) => peer.id === a.selfId);
  assert.equal(profile.mode, "web");
  assert.equal(profile.pose, undefined);
  for (const client of [a, b, late, after]) client.socket.close();
});

test("an unknown mode, or the mode the client already has, is ignored", async () => {
  const a = await connect("mode-room-2", { name: "A" });
  const b = await connect("mode-room-2", { name: "B" });
  for (const mode of ["admin", "", 7, null, "web"]) send(a, { type: "mode", mode });
  await pause(150);
  assert.equal(b.messages.filter((message) => message.type === "peer-mode").length, 0);
  a.socket.close();
  b.socket.close();
});

test("spreadAngles keeps people at least the minimum gap apart and leaves spread-out people alone", () => {
  const gap = (list) => {
    const sorted = list.map((angle) => ((angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2)).sort((x, y) => x - y);
    let min = Infinity;
    sorted.forEach((angle, index) => { min = Math.min(min, (index + 1 === sorted.length ? sorted[0] + Math.PI * 2 : sorted[index + 1]) - angle); });
    return min;
  };
  const crowded = spreadAngles([1, 1, 1.01, 1.02], 0.34);
  assert.ok(gap(crowded) >= 0.34 - 1e-3, `gap ${gap(crowded)}`);
  assert.equal(crowded.length, 4);
  // Order around the ring is kept (the first person is still the first one counter-clockwise).
  assert.ok(crowded[0] <= crowded[2] + 1e-9);
  assert.deepEqual(spreadAngles([0, 2, 4], 0.34).map((value) => Math.round(value * 100) / 100), [0, 2, 4]);
  assert.deepEqual(spreadAngles([3]), [3]);
  // Across the 0 / 2 pi seam.
  const seam = spreadAngles([6.2, 0.02, 0.05], 0.34);
  assert.ok(gap(seam) >= 0.34 - 1e-3);
  // Too many people for the ring: give up spreading rather than loop.
  assert.equal(spreadAngles(new Array(40).fill(1), 0.34).length, 40);
});
