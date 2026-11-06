import assert from "node:assert/strict";
import { test } from "node:test";
import { circuitExamples, safetyExamples } from "../shared/examples.mjs";
import { evaluateCircuit } from "../shared/circuit-eval.mjs";
import { safetyStatus } from "../shared/safety-eval.mjs";

// The ready-made scenes must be finished: every circuit closes and powers its load, every safety layout passes the checklist.

const asCircuit = (items) => items.map((item) => ({ part: item.part, position: item.position, rotation: item.rotation, on: item.on }));
const asSafety = (items) => items.map((item) => ({ part: item.part, x: item.position[0], z: item.position[2], on: item.on }));
const spaced = (items) => items.every((a, i) => items.every((b, j) => j <= i || Math.hypot(a.position[0] - b.position[0], a.position[2] - b.position[2]) >= 0.3));

test("there are four circuit examples and four safety examples", () => {
  assert.equal(circuitExamples.length, 4);
  assert.equal(safetyExamples.length, 4);
});

for (const example of circuitExamples) {
  test(`circuit example "${example.label}" is a closed, working circuit`, () => {
    const result = evaluateCircuit(asCircuit(example.items));
    assert.equal(result.state, "closed");
    const live = [...result.live].map((index) => example.items[index].part);
    assert.ok(live.includes("bulb") || live.includes("motor"), "a bulb or motor is powered");
    assert.equal(result.live.size, example.items.length, "every part is in the loop");
    assert.ok(example.items.every((item) => Math.hypot(item.position[0], item.position[2]) <= 3.1), "on the board");
    assert.ok(spaced(example.items), "no two parts on top of each other");
  });

  test(`circuit example "${example.label}" goes dark when its switch is opened`, () => {
    const items = asCircuit(example.items);
    const index = items.findIndex((item) => item.part === "switch");
    if (index < 0) return;
    items[index].on = false;
    assert.equal(evaluateCircuit(items).state, "open");
  });
}

for (const example of safetyExamples) {
  test(`safety example "${example.label}" passes the checklist with every hazard guarded`, () => {
    const status = safetyStatus(asSafety(example.items));
    assert.ok(status.checks.every((check) => check.ok), "exit, extinguisher, first aid and muster present");
    assert.ok(status.beacons >= 1, "has a hazard");
    assert.equal(status.unguarded, 0);
    assert.ok(status.active >= 1, "a hazard is active");
    assert.ok(example.items.every((item) => Math.hypot(item.position[0], item.position[2]) <= 3.1), "on the board");
    assert.ok(spaced(example.items), "no two parts on top of each other");
  });
}

test("the dimmed example is dimmer than the simple loop", () => {
  const simple = evaluateCircuit(asCircuit(circuitExamples[0].items));
  const dimmed = evaluateCircuit(asCircuit(circuitExamples.find((item) => item.id === "dimmer").items));
  assert.ok(dimmed.brightness < simple.brightness);
});
