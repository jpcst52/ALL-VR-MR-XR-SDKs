import assert from "node:assert/strict";
import { after, before, test } from "node:test";
import WebSocket from "ws";
import { createRelay } from "../server.mjs";

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

function waitFor(client, type, predicate = () => true) {
  const found = client.messages.find((message) => message.type === type && predicate(message));
  if (found) return Promise.resolve(found);
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error(`Timed out waiting for ${type}.`)), 1500);
    const listener = (raw) => {
      const message = JSON.parse(raw.toString());
      client.messages.push(message);
      if (message.type === type && predicate(message)) {
        clearTimeout(timeout);
        client.socket.off("message", listener);
        resolve(message);
      }
    };
    client.socket.on("message", listener);
  });
}

const send = (client, message) => client.socket.send(JSON.stringify(message));
const close = (client) => new Promise((resolve) => {
  if (client.socket.readyState === WebSocket.CLOSED) { resolve(); return; }
  client.socket.once("close", resolve);
  client.socket.close(1000, "Test complete");
});

test("optional avatar poses are validated, relayed, and given to late joiners", async () => {
  const headset = await connect("pose-room", { name: "Headset", mode: "vr" });
  const desktop = await connect("pose-room", { name: "Desktop", mode: "web" });
  try {
    // A peer that never sent a pose keeps the original four-field profile (old clients are unaffected).
    const joined = await waitFor(headset, "peer-join");
    assert.deepEqual(Object.keys(joined.peer).sort(), ["cursor", "id", "mode", "name"]);

    send(headset, { type: "pose", head: [0.5, 1.6, 2], yaw: 1.2, hands: [[0.3, 1.1, 1.8], null] });
    const pose = await waitFor(desktop, "peer-pose");
    assert.equal(pose.id, headset.selfId);
    assert.deepEqual(pose.head, [0.5, 1.6, 2]);
    assert.equal(pose.yaw, 1.2);
    assert.deepEqual(pose.hands, [[0.3, 1.1, 1.8], null]);

    const late = await connect("pose-room", { name: "Late", mode: "web" });
    try {
      const welcome = late.messages.find((message) => message.type === "welcome");
      assert.deepEqual(welcome.peers.find((peer) => peer.id === headset.selfId).pose.head, [0.5, 1.6, 2]);
      assert.equal(welcome.peers.find((peer) => peer.id === desktop.selfId).pose, undefined);
    } finally {
      await close(late);
    }
  } finally {
    await close(headset);
    await close(desktop);
  }
});

test("malformed or out-of-range poses are ignored and do not break the room", async () => {
  const a = await connect("pose-bad-room", { name: "A", mode: "vr" });
  const b = await connect("pose-bad-room", { name: "B", mode: "web" });
  try {
    await waitFor(a, "peer-join");
    const bad = [
      { type: "pose", head: [0, 1.6], yaw: 0, hands: [null, null] },
      { type: "pose", head: [0, 1.6, 0], yaw: "left", hands: [null, null] },
      { type: "pose", head: [0, 1.6, 0], yaw: 0, hands: [null] },
      { type: "pose", head: [0, 1.6, 0], yaw: 0, hands: [[1, 2], null] },
      { type: "pose", head: [0, 1e9, 0], yaw: 0, hands: [null, null] },
      { type: "pose", head: [0, 1.6, 0], yaw: 99, hands: [null, null] },
    ];
    for (const message of bad) send(a, message);
    await new Promise((resolve) => setTimeout(resolve, 120));
    assert.equal(b.messages.filter((message) => message.type === "peer-pose").length, 0);

    send(a, { type: "pose", head: [0, 1.6, 0], yaw: 0, hands: [null, null] });
    await waitFor(b, "peer-pose");
    send(a, { type: "place", part: "bulb", position: [0, 0.1, 0], rotation: [0, 0, 0] });
    await waitFor(b, "object-added");
  } finally {
    await close(a);
    await close(b);
  }
});
