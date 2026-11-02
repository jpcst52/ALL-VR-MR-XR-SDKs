// "Why this matters" reasons and the separate reasoning (rationale) question. See reasons.js.
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { scenarios } from "../scenarios.js";
import { reasons } from "../reasons.js";

const byId = (id) => scenarios.find((scenario) => scenario.id === id);
const appSource = readFileSync(new URL("../app.js", import.meta.url), "utf8");
const pageSource = readFileSync(new URL("../index.html", import.meta.url), "utf8");
// No invented standards, regulation numbers, or citations in learner-facing reasons.
const noStandards = /\b(OSHA|ASME|API|NFPA|ANSI|CFR|ISO|IEC|EN|BS|CSA|NEC)\b|\b\d{2,}\.\d+|§|et al\.|\(\d{4}\)/;

test("every reasons entry points at a real scenario and real correct actions", () => {
  for (const [id, entry] of Object.entries(reasons)) {
    const scenario = byId(id);
    assert.ok(scenario, `${id} is a scenario`);
    assert.ok(entry.steps.length <= scenario.steps.length, `${id} has no extra task entries`);
    entry.steps.forEach((map, index) => {
      for (const hotspotId of Object.keys(map)) {
        const hotspot = scenario.steps[index].hotspots.find((item) => item.id === hotspotId);
        assert.ok(hotspot, `${id} task ${index + 1} has hotspot ${hotspotId}`);
        assert.equal(hotspot.correct, true, `${id} task ${index + 1} ${hotspotId}: a reason belongs on a required action`);
        assert.equal(hotspot.why, map[hotspotId], `${id} task ${index + 1} ${hotspotId}: the reason was applied`);
      }
    });
  }
});

test("every non-draft scenario has a reason in every task and one reasoning question", () => {
  for (const scenario of scenarios.filter((item) => !item.draft)) {
    scenario.steps.forEach((step, index) => {
      assert.ok(step.hotspots.some((hotspot) => hotspot.why), `${scenario.id} task ${index + 1} has at least one reason`);
    });
    assert.ok(scenario.rationale, `${scenario.id} has a reasoning question`);
  }
});

test("hydrostatic test and refinery scenarios explain every required action", () => {
  for (const id of ["heat-exchanger-hydrotest", "refinery-battery-area", "control-room-unit", "pipe-spool-welding"]) {
    for (const [index, step] of byId(id).steps.entries()) {
      for (const hotspot of step.hotspots.filter((item) => item.correct)) {
        assert.ok(hotspot.why, `${id} task ${index + 1} ${hotspot.id} has a reason`);
      }
    }
  }
  const hydro = byId("heat-exchanger-hydrotest").rationale;
  assert.match(hydro.question, /vent/i, "the hydrotest question is about venting");
  assert.match(hydro.options.find((option) => option.best).text, /air/i, "best answer: air gets in as the water drains");
});

test("the draft Circulation Builder has no reasons or reasoning question", () => {
  const bio = byId("circulation-builder");
  assert.equal(bio.draft, true);
  assert.equal(bio.rationale, undefined);
  assert.ok(bio.steps.every((step) => step.hotspots.every((hotspot) => !hotspot.why)));
});

test("reasons are short, single plain sentences that fit a VR panel", () => {
  for (const scenario of scenarios) {
    for (const [index, step] of scenario.steps.entries()) {
      for (const hotspot of step.hotspots.filter((item) => item.why)) {
        const where = `${scenario.id} task ${index + 1} ${hotspot.id}`;
        assert.ok(hotspot.why.length <= 110, `${where} reason is at most 110 characters (${hotspot.why.length})`);
        assert.match(hotspot.why, /^[A-Z].*\.$/, `${where} reason is a sentence`);
        assert.equal(hotspot.why.slice(0, -1).match(/[.!?](\s|$)/), null, `${where} reason is one sentence`);
        assert.doesNotMatch(hotspot.why, noStandards, `${where} cites no standard or number`);
      }
    }
  }
});

test("each reasoning question has three options, one best answer, and feedback for each", () => {
  for (const scenario of scenarios.filter((item) => item.rationale)) {
    const { question, options } = scenario.rationale;
    assert.ok(question.length <= 110 && question.endsWith("?"), `${scenario.id} question is a short question`);
    assert.equal(options.length, 3, `${scenario.id} has three options`);
    assert.equal(options.filter((option) => option.best === true).length, 1, `${scenario.id} has exactly one best answer`);
    assert.equal(new Set(options.map((option) => option.text)).size, 3, `${scenario.id} options differ`);
    for (const option of options) {
      assert.ok(option.text.length <= 64, `${scenario.id} option fits a VR card: ${option.text}`);
      assert.ok(option.feedback && option.feedback.length <= 130, `${scenario.id} option has short feedback: ${option.text}`);
      assert.equal(typeof option.best, "boolean");
      for (const text of [question, option.text, option.feedback]) assert.doesNotMatch(text, noStandards, `${scenario.id} cites no standard: ${text}`);
    }
  }
  // The best answer is not always in the same position.
  const positions = new Set(scenarios.filter((item) => item.rationale).map((item) => item.rationale.options.findIndex((option) => option.best)));
  assert.equal(positions.size, 3);
});

test("the page shows reasons in a styled panel and reports reasoning separately", () => {
  assert.match(pageSource, /id="why-panel"/);
  assert.match(pageSource, /Why this matters/);
  assert.match(pageSource, /id="reasoning-result"/);
  assert.match(appSource, /"Why this matters"/, "the in-world VR panel has the heading");
  assert.match(appSource, /Escape/, "one-key dismiss on desktop");
  // Reasoning is recorded on its own and never added to the procedure score or called pass/fail.
  const choose = appSource.slice(appSource.indexOf("function chooseRationale"), appSource.indexOf("function renderQuestion"));
  assert.ok(choose.length > 100, "chooseRationale exists");
  assert.doesNotMatch(choose, /totalScore/, "the reasoning answer does not change the procedure score");
  assert.doesNotMatch(appSource + pageSource, /\b(PASS|FAIL)ED\b|certif(ied|ication) (passed|result)/i);
});
