import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { scenarios, categories } from "../scenarios.js";

// Checks for the DRAFT "Circulation Builder" life-science scenario (biology-scenarios.js, biology-stations.js).
const builder = scenarios.find((scenario) => scenario.id === "circulation-builder");
const stationSource = readFileSync(new URL("../biology-stations.js", import.meta.url), "utf8");
const allText = (scenario) => [
  scenario.title, scenario.setting, scenario.description, ...(scenario.briefing ?? []),
  ...scenario.steps.flatMap((step) => [step.prompt, step.principle, ...step.hotspots.flatMap((hotspot) => [hotspot.label, hotspot.say, hotspot.early ?? ""])]),
].join(" ");

// The order in which required actions become available, resolving `after` one action at a time.
function completionOrder(step) {
  const required = step.hotspots.filter((hotspot) => hotspot.correct);
  const done = [];
  while (done.length < required.length) {
    const ready = required.filter((hotspot) => !done.includes(hotspot.id) && (hotspot.after ?? []).every((id) => done.includes(id)));
    assert.ok(ready.length, "a required action is always available");
    done.push(ready[0].id);
  }
  return done;
}

test("Circulation Builder is registered as a draft life-science scenario", () => {
  assert.ok(builder, "circulation-builder is in the catalog");
  assert.equal(builder.category, "biology");
  assert.equal(builder.object, "circulation");
  assert.equal(builder.draft, true);
  assert.equal(builder.steps.length, 4);
  assert.match(builder.setting, /draft for faculty review/i);
  const category = categories.find((item) => item.id === "biology");
  assert.ok(category, "biology category exists");
  assert.match(category.heading, /DRAFT FOR FACULTY REVIEW/);
  assert.match(category.note, /not clinical or medical guidance/i);
  assert.ok(builder.briefing.some((line) => /not clinical or medical guidance/i.test(line)), "briefing says it is not clinical guidance");
});

test("the station builder is wired into the app and exposes every revealed part", () => {
  const app = readFileSync(new URL("../app.js", import.meta.url), "utf8");
  assert.match(app, /import \{ createCirculationStation \} from "\.\/biology-stations\.js"/);
  assert.match(app, /circulation: createCirculationStation/);
  const names = new Set([...stationSource.matchAll(/reveal\.(\w+)\s*=/g)].map((match) => match[1]));
  const objectKeys = (name) => [...stationSource.match(new RegExp(`const ${name} = \\{([\\s\\S]*?)\\n  \\};`))[1].matchAll(/^\s{4}(\w+):/gm)].map((match) => match[1]);
  for (const key of objectKeys("chambers")) names.add(key);
  for (const key of objectKeys("paths")) names.add(key).add(`${key}Grey`);
  for (const [index, step] of builder.steps.entries()) {
    for (const hotspot of step.hotspots) {
      for (const part of [hotspot.reveal, hotspot.hide].filter(Boolean)) assert.ok(names.has(part), `task ${index + 1} ${hotspot.id} uses a part the model has: ${part}`);
    }
  }
  assert.deepEqual(builder.steps.map((step) => step.onStart ?? ""), ["", "t2", "t3", "t4"]);
});

test("the pulmonary and systemic circuits must be connected in blood-flow order", () => {
  assert.deepEqual(completionOrder(builder.steps[1]), ["venae", "tricuspid", "pa", "lungs"]);
  assert.deepEqual(completionOrder(builder.steps[2]), ["pv", "mitral", "aorta", "body"]);
  for (const step of builder.steps.slice(1, 3)) {
    for (const hotspot of step.hotspots.filter((item) => item.correct && item.after)) {
      assert.ok(hotspot.early, `${hotspot.id} explains what to do first when chosen too early`);
    }
  }
});

test("chambers, vessels, and colours match introductory biology", () => {
  const [chambers, , , colours] = builder.steps;
  assert.deepEqual(chambers.hotspots.filter((hotspot) => hotspot.correct).map((hotspot) => hotspot.id).sort(), ["la", "lv", "ra", "rv"]);
  // Front view: the heart's right side is on the viewer's left (negative x).
  const at = Object.fromEntries(chambers.hotspots.map((hotspot) => [hotspot.id, hotspot.at]));
  assert.ok(at.ra[0] < 0 && at.rv[0] < 0 && at.la[0] > 0 && at.lv[0] > 0, "right chambers on the viewer's left");
  assert.ok(at.ra[1] > at.rv[1] && at.la[1] > at.lv[1], "atria above ventricles");
  const say = Object.fromEntries(colours.hotspots.map((hotspot) => [hotspot.id, `${hotspot.label} ${hotspot.say}`]));
  assert.match(say.aortaRed, /oxygen-rich/);
  assert.match(say.pvRed, /oxygen-rich/);
  assert.match(say.paBlue, /oxygen-poor/);
  assert.match(say.venaeBlue, /oxygen-poor/);
  assert.equal(colours.hotspots.find((hotspot) => hotspot.id === "rule").correct, false, "'every artery is red' is a misconception");
  assert.match(stationSource, /const RED = "#d8384a"/);
  assert.match(stationSource, /const BLUE = "#3f6fd8"/);
  assert.match(allText(builder), /real blood is (always red|dark red)/i, "says the blue colour is only a diagram convention");
});

test("every task has a key misconception and labels fit", () => {
  for (const [index, step] of builder.steps.entries()) {
    assert.ok(step.hotspots.some((hotspot) => !hotspot.correct && hotspot.critical), `task ${index + 1} has a key misconception`);
    assert.ok(step.hotspots.length >= 4 && step.hotspots.length <= 5, `task ${index + 1} has 4 or 5 hotspots`);
    for (const hotspot of step.hotspots) assert.ok(hotspot.label.length <= 30, `label fits: ${hotspot.label}`);
  }
});

test("draft content names no partner companies and cites no sources", () => {
  const text = allText(builder) + " " + stationSource;
  for (const name of ["Zip" + "po", "Alle" + "gheny", "K" + "OA", "Spe" + "er", "Or" + "in", "Ja" + "mes"]) assert.ok(!text.includes(name), `does not name ${name}`);
  assert.doesNotMatch(text, /et al\.|\(\d{4}\)|doi\.org|https?:\/\//i, "no citations or links");
});
