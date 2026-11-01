import assert from "node:assert/strict";
import { test } from "node:test";
import { scenarios } from "../scenarios.js";

test("the catalog contains three business and three research simulations", () => {
  assert.equal(scenarios.length, 6);
  assert.equal(scenarios.filter((scenario) => scenario.category === "business").length, 3);
  assert.equal(scenarios.filter((scenario) => scenario.category === "research").length, 3);
  assert.equal(new Set(scenarios.map((scenario) => scenario.id)).size, 6);
});

test("each simulation is a short three-decision activity with complete choices", () => {
  for (const scenario of scenarios) {
    assert.ok(scenario.title, `${scenario.id} has a title`);
    assert.ok(scenario.description, `${scenario.id} has a description`);
    assert.ok(scenario.duration, `${scenario.id} has a duration`);
    assert.equal(scenario.steps.length, 3, `${scenario.id} has three decisions`);
    for (const [index, step] of scenario.steps.entries()) {
      assert.ok(step.prompt, `${scenario.id} decision ${index + 1} has a prompt`);
      assert.ok(step.principle, `${scenario.id} decision ${index + 1} has a principle`);
      assert.equal(step.choices.length, 3, `${scenario.id} decision ${index + 1} has three choices`);
      assert.equal(step.choices.filter((choice) => choice.score === 2).length, 1);
      for (const choice of step.choices) {
        assert.ok(choice.text, `${scenario.id} decision ${index + 1} has choice text`);
        assert.ok(choice.feedback, `${scenario.id} decision ${index + 1} has feedback`);
        assert.ok([0, 1, 2].includes(choice.score));
      }
    }
  }
});

test("research exercises reinforce consent, accurate records, and safe protocol use", () => {
  const researchText = scenarios
    .filter((scenario) => scenario.category === "research")
    .flatMap((scenario) => scenario.steps.flatMap((step) => [
      step.prompt,
      step.principle,
      ...step.choices.map((choice) => choice.text),
    ]))
    .join(" ")
    .toLowerCase();
  for (const concept of ["voluntary", "consent", "protocol", "guess", "record"]) {
    assert.ok(researchText.includes(concept), `research catalog covers ${concept}`);
  }
});
