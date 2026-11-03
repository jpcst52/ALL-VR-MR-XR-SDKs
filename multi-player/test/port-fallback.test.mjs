import test from "node:test";
import assert from "node:assert/strict";
import { createServer } from "node:net";
import { createRelay } from "../server.mjs";

// The demo must not fail just because its usual port is taken: with autoPort it moves to the next free port, and it
// answers /healthz so the launcher and the start/stop scripts can tell it is this demo.

function freePortBase() {
  return new Promise((resolve) => {
    const probe = createServer().listen(0, "127.0.0.1", () => {
      const { port } = probe.address();
      probe.close(() => resolve(port));
    });
  });
}

test("autoPort moves to the next port when the first is busy, and /healthz identifies the relay", async () => {
  const base = await freePortBase();
  const first = createRelay();
  const second = createRelay();
  try {
    const a = await first.listen({ port: base, host: "127.0.0.1", autoPort: true });
    assert.equal(a.port, base);
    assert.equal(first.portMoved, undefined);

    const b = await second.listen({ port: base, host: "127.0.0.1", autoPort: true });
    assert.ok(b.port > base && b.port <= base + 49, `moved to ${b.port}`);
    assert.deepEqual(second.portMoved, { from: base, to: b.port });

    const health = await (await fetch(`http://127.0.0.1:${b.port}/healthz`)).json();
    assert.equal(health.relay, true);
  } finally {
    await first.close();
    await second.close();
  }
});

test("without autoPort a busy port is an error that says what to do", async () => {
  const base = await freePortBase();
  const first = createRelay();
  const second = createRelay();
  try {
    await first.listen({ port: base, host: "127.0.0.1" });
    await assert.rejects(second.listen({ port: base, host: "127.0.0.1" }), /already in use/);
  } finally {
    await first.close();
    await second.close().catch(() => {});
  }
});
