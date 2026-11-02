import assert from "node:assert/strict";
import { test } from "node:test";
import { scenarios, categories } from "../scenarios.js";

const count = (id) => scenarios.filter((scenario) => scenario.category === id).length;

test("the catalog has business, research, industrial, and manufacturing simulations", () => {
  assert.equal(scenarios.length, 16);
  assert.equal(categories.length, 4);
  assert.equal(count("business"), 3);
  assert.equal(count("research"), 3);
  assert.equal(count("industrial"), 6);
  assert.equal(count("manufacturing"), 4);
  assert.equal(new Set(scenarios.map((scenario) => scenario.id)).size, 16);
  for (const scenario of scenarios) assert.ok(categories.some((category) => category.id === scenario.category));
});

test("industrial scenarios cover the requested trades", () => {
  const ids = scenarios.filter((scenario) => scenario.category === "industrial").map((scenario) => scenario.id);
  for (const id of ["work-at-height", "control-room-unit", "refinery-battery-area", "skyscraper-window-cleaning", "welding-hot-work", "electrical-work"]) {
    assert.ok(ids.includes(id), `${id} is present`);
  }
});

test("manufacturing quality scenarios cover the requested local-manufacturer exercises", () => {
  const ids = scenarios.filter((scenario) => scenario.category === "manufacturing").map((scenario) => scenario.id);
  for (const id of ["lighter-lot-sampling", "heat-exchanger-hydrotest", "pipe-spool-welding", "resistor-selection"]) {
    assert.ok(ids.includes(id), `${id} is present`);
  }
  const heading = categories.find((category) => category.id === "manufacturing");
  assert.equal(heading.accent, "#66B2E3");
  assert.equal(heading.heading, "MANUFACTURING QUALITY PRACTICE");
  for (const scenario of scenarios.filter((item) => item.category === "manufacturing")) {
    assert.ok(/inspired by/i.test(scenario.setting), `${scenario.id} says it is inspired by, not a real process`);
  }
});

test("every scenario is a set of hands-on tasks that can be completed", () => {
  for (const scenario of scenarios) {
    assert.ok(scenario.title && scenario.description && scenario.duration, `${scenario.id} has its details`);
    const expected = scenario.category === "industrial" || scenario.category === "manufacturing" ? 4 : 3;
    assert.equal(scenario.steps.length, expected, `${scenario.id} has ${expected} tasks`);
    if (scenario.category === "industrial" || scenario.category === "manufacturing") assert.ok(scenario.briefing?.length >= 3, `${scenario.id} has a briefing`);
    for (const [index, step] of scenario.steps.entries()) {
      const where = `${scenario.id} task ${index + 1}`;
      assert.ok(step.prompt && step.principle, `${where} has a prompt and principle`);
      const ids = step.hotspots.map((hotspot) => hotspot.id);
      assert.equal(new Set(ids).size, ids.length, `${where} has unique hotspot ids`);
      if (scenario.category === "manufacturing") assert.ok(step.hotspots.length >= 4 && step.hotspots.length <= 5, `${where} has 4 or 5 hotspots`);
      const required = step.hotspots.filter((hotspot) => hotspot.correct);
      assert.ok(required.length >= 2, `${where} needs at least two required actions`);
      assert.ok(step.hotspots.some((hotspot) => !hotspot.correct || hotspot.early), `${where} has a way to go wrong`);
      if (scenario.category === "industrial" || scenario.category === "manufacturing") assert.ok(step.hotspots.some((hotspot) => hotspot.critical), `${where} has a critical mistake`);
      for (const hotspot of step.hotspots) {
        assert.ok(hotspot.label.length <= 30, `${where} label fits: ${hotspot.label}`);
        assert.equal(hotspot.at.length, 3, `${where} hotspot ${hotspot.id} has a position`);
        assert.ok(hotspot.say, `${where} hotspot ${hotspot.id} has a result`);
        for (const needed of hotspot.after ?? []) {
          assert.ok(required.some((other) => other.id === needed), `${where} hotspot ${hotspot.id} depends on a required action (${needed})`);
        }
        if (!hotspot.correct) assert.equal(hotspot.after, undefined, `${where} wrong action ${hotspot.id} has no prerequisites`);
      }
      const done = new Set();
      for (let pass = 0; pass < required.length; pass += 1) {
        for (const hotspot of required) {
          if ((hotspot.after ?? []).every((needed) => done.has(needed))) done.add(hotspot.id);
        }
      }
      assert.equal(done.size, required.length, `${where} can be completed`);
    }
  }
});

test("research exercises reinforce consent, accurate records, and safe protocol use", () => {
  const researchText = scenarios
    .filter((scenario) => scenario.category === "research")
    .flatMap((scenario) => scenario.steps.flatMap((step) => [
      step.prompt,
      step.principle,
      ...step.hotspots.flatMap((hotspot) => [hotspot.label, hotspot.say]),
    ]))
    .join(" ")
    .toLowerCase();
  for (const concept of ["voluntary", "consent", "protocol", "guess", "record"]) {
    assert.ok(researchText.includes(concept), `research catalog covers ${concept}`);
  }
});
