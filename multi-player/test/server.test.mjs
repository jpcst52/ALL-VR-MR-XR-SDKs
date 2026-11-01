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

function connect(room = "test-room", { name = "Guest", mode = "web" } = {}) {
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
    socket.on("close", (code, reason) => {
      if (code !== 1000 && messages.length === 0) {
        reject(new Error(`WebSocket closed (${code}): ${reason.toString()}`));
      }
    });
  });
}

function waitFor(client, type, predicate = () => true) {
  const alreadyReceived = client.messages.find((message) => message.type === type && predicate(message));
  if (alreadyReceived) return Promise.resolve(alreadyReceived);
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

function send(client, message) {
  client.socket.send(JSON.stringify(message));
}

function close(client) {
  return new Promise((resolve) => {
    if (client.socket.readyState === WebSocket.CLOSED) {
      resolve();
      return;
    }
    client.socket.once("close", resolve);
    client.socket.close(1000, "Test complete");
  });
}

test("VR and AR clients in the same room receive a shared object", async () => {
  const vr = await connect("mixed-room", { name: "VR Builder", mode: "vr" });
  const ar = await connect("mixed-room", { name: "AR Builder", mode: "ar" });
  try {
    const peer = await waitFor(vr, "peer-join");
    assert.equal(peer.peer.name, "AR Builder");
    assert.equal(peer.peer.mode, "ar");

    send(vr, { type: "place", part: "pump", position: [1, 0.2, -1], rotation: [0, 0, 0] });
    const added = await waitFor(ar, "object-added");
    assert.equal(added.object.part, "pump");
    assert.deepEqual(added.object.position, [1, 0.2, -1]);
  } finally {
    await close(vr);
    await close(ar);
  }
});

test("late joiners receive room objects and rooms remain isolated", async () => {
  const host = await connect("snapshot-room");
  const otherRoom = await connect("separate-room");
  try {
    send(host, { type: "place", part: "tank", position: [0, 0, 0], rotation: [0, 0, 0] });
    await waitFor(host, "object-added");
    const lateJoiner = await connect("snapshot-room", { name: "Late Builder" });
    try {
      const welcome = lateJoiner.messages[0];
      assert.equal(welcome.type, "welcome");
      assert.equal(welcome.objects.length, 1);
      assert.equal(welcome.objects[0].part, "tank");
      assert.equal(welcome.peers.length, 1);
    } finally {
      await close(lateJoiner);
    }
    assert.equal(otherRoom.messages.some((message) => message.type === "object-added"), false);
  } finally {
    await close(host);
    await close(otherRoom);
  }
});

test("only the holder can move a shared object", async () => {
  const owner = await connect("ownership-room");
  const other = await connect("ownership-room");
  try {
    send(owner, { type: "place", part: "valve", position: [0, 0, 0], rotation: [0, 0, 0] });
    const { object } = await waitFor(other, "object-added");

    send(owner, { type: "grab", id: object.id });
    await waitFor(other, "object-grabbed");
    send(other, { type: "grab", id: object.id });
    const denied = await waitFor(other, "grab-denied");
    assert.equal(denied.id, object.id);

    send(other, {
      type: "move", id: object.id, position: [3, 0.2, 1], rotation: [0, 0, 0],
    });
    send(owner, {
      type: "move", id: object.id, position: [1, 0.2, 0], rotation: [0, 0.5, 0],
    });
    const moved = await waitFor(other, "object-moved");
    assert.deepEqual(moved.position, [1, 0.2, 0]);

    send(owner, { type: "release", id: object.id });
    await waitFor(other, "object-released");
  } finally {
    await close(owner);
    await close(other);
  }
});

test("disconnect releases held objects and invalid room names are rejected", async () => {
  const owner = await connect("disconnect-room");
  const observer = await connect("disconnect-room");
  try {
    send(owner, { type: "place", part: "pipe", position: [0, 0, 0], rotation: [0, 0, 0] });
    const { object } = await waitFor(observer, "object-added");
    send(owner, { type: "grab", id: object.id });
    await waitFor(observer, "object-grabbed");
    await close(owner);
    await waitFor(observer, "object-released");
    await waitFor(observer, "peer-leave");
    assert.equal(relay.rooms.get("disconnect-room").objects.get(object.id).heldBy, null);
    assert.equal(WebSocket.CLOSED, 3);
    const invalid = new WebSocket(`${baseUrl}/ws`);
    invalid.on("open", () => {
      invalid.send(JSON.stringify({ type: "join", room: "../invalid" }));
    });
    const code = await new Promise((resolve) => invalid.once("close", resolve));
    assert.equal(code, 1008);
  } finally {
    if (observer.socket.readyState === WebSocket.OPEN) await close(observer);
  }
});

test("the relay serves both browser examples and refuses path traversal", async () => {
  const httpUrl = baseUrl.replace("ws:", "http:");
  const [vr, ar] = await Promise.all([
    fetch(`${httpUrl}/multi-player/vr/`),
    fetch(`${httpUrl}/multi-player/ar/`),
  ]);
  assert.equal(vr.status, 200);
  assert.match(await vr.text(), /MULTIPLAYER WEBXR \/ VR/);
  assert.equal(ar.status, 200);
  assert.match(await ar.text(), /MULTIPLAYER WEBXR \/ AR/);
  const traversal = await fetch(`${httpUrl}/multi-player/%2e%2e%2f%2e%2e%2fWindows%2fwin.ini`);
  assert.equal(traversal.status, 403);
});

test("invalid part types and unbounded transforms are ignored", async () => {
  const host = await connect("validation-room");
  const observer = await connect("validation-room");
  try {
    send(host, { type: "place", part: "secrets", position: [0, 0, 0], rotation: [0, 0, 0] });
    send(host, { type: "place", part: "pump", position: [999, 0, 0], rotation: [0, 0, 0] });
    await new Promise((resolve) => setTimeout(resolve, 80));
    assert.equal(host.messages.some((message) => message.type === "object-added"), false);
    assert.equal(observer.messages.some((message) => message.type === "object-added"), false);
  } finally {
    await close(host);
    await close(observer);
  }
});
