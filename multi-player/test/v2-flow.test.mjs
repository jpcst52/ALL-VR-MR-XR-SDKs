import test from "node:test";
import assert from "node:assert/strict";
import { evaluateCircuit } from "../shared/circuit-eval.mjs";
import { circuitExamples } from "../shared/examples.mjs";
import { flowDirections, dotOffset } from "../v2/flow.mjs";

const itemsOf = (example) => example.items.map((item) => ({ part: item.part, position: item.position, rotation: item.rotation, on: item.on }));
const run = (items) => ({ items, evaluation: evaluateCircuit(items) });
const simple = circuitExamples.find((example) => example.id === "simple");

test("flow: in the simple loop the current leaves the battery + end and goes round in one direction", () => {
  const { items, evaluation } = run(itemsOf(simple));
  const flow = flowDirections(items, evaluation);
  const at = (part, x, z) => items.findIndex((item) => item.part === part && Math.abs(item.position[0] - x) < 0.01 && Math.abs(item.position[2] - z) < 0.01);
  assert.equal(flow[at("battery", -0.8, -0.6)], 1, "inside the cell: - to +");
  assert.equal(flow[at("switch", 0, -0.6)], 1, "switch is next to the + end");
  assert.equal(flow[at("bulb", 0.8, -0.6)], 1);
  assert.equal(flow[at("wire", 1.2, 0)], -1, "right wire runs back toward the front");
  assert.equal(flow[at("wire", 0.6, 0.6)], -1);
  assert.equal(flow[at("wire", -0.6, 0.6)], -1);
  assert.equal(flow[at("wire", -1.2, 0)], 1, "left wire returns to the - end of the battery");
});

test("flow: every live part of every circuit example has a direction", () => {
  for (const example of circuitExamples) {
    const { items, evaluation } = run(itemsOf(example));
    const flow = flowDirections(items, evaluation);
    assert.equal(evaluation.state, "closed", example.id);
    for (const index of evaluation.live) assert.notEqual(flow[index], 0, `${example.id} part ${index} (${items[index].part})`);
  }
});

test("flow: nothing flows with the switch open or without a battery", () => {
  const open = itemsOf(simple).map((item) => (item.part === "switch" ? { ...item, on: false } : item));
  assert.deepEqual(flowDirections(open, evaluateCircuit(open)), new Array(open.length).fill(0));
  const noBattery = itemsOf(simple).filter((item) => item.part !== "battery");
  assert.deepEqual(flowDirections(noBattery, evaluateCircuit(noBattery)), new Array(noBattery.length).fill(0));
});

test("flow: dot positions stay on the part, move with the flow direction and repeat", () => {
  const reach = 0.4;
  for (let k = 0; k < 5; k += 1) {
    for (const t of [0, 0.37, 1.9, 12.5]) {
      const u = dotOffset(k, 5, t, 1, reach);
      assert.ok(u >= -reach - 1e-9 && u <= reach + 1e-9, `u ${u}`);
    }
  }
  const before = dotOffset(0, 4, 0, 1, reach); const after = dotOffset(0, 4, 0.1, 1, reach);
  assert.ok(after > before, "dir +1 moves toward +x");
  const back = dotOffset(1, 4, 0.1, -1, reach); const start = dotOffset(1, 4, 0, -1, reach);
  assert.ok(back < start, "dir -1 moves toward -x");
  assert.ok(Math.abs(dotOffset(2, 4, 0, 1, reach) - dotOffset(2, 4, 1 / 0.45, 1, reach)) < 1e-9, "one lap takes 1/speed seconds");
});
