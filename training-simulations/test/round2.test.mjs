// Round 2 checks: rooms and dressing for the plain scenarios, texture-leak fix, reduced motion, keyboard focus.
// rooms.js and dressing.js import three through the page import map, so they are checked from their source here and
// exercised in the browser by tools/vr-capture/lab-qa.mjs and ui-check.mjs.
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { test } from "node:test";
import { scenarios } from "../scenarios.js";

const here = new URL("../", import.meta.url);
const read = (name) => readFileSync(new URL(name, here), "utf8");
const app = read("app.js");
const rooms = read("rooms.js");
const dressing = read("dressing.js");
const models = new URL("../../assets/models/", import.meta.url);

const objectKeys = [...new Set(scenarios.map((s) => s.object))];
const block = (source, name) => source.match(new RegExp(`${name} = \\{([\\s\\S]*?)\\n\\};`))[1];

test("every indoor station has a known room kind and every room kind is built", () => {
  const map = Object.fromEntries([...block(rooms, "export const ROOM_BY_STATION").matchAll(/(\w+): "(\w+)"/g)].map((m) => [m[1], m[2]]));
  const kinds = new Set([...rooms.match(/const KINDS = new Set\(\[([^\]]*)\]/)[1].matchAll(/"(\w+)"/g)].map((m) => m[1]));
  for (const [station, kind] of Object.entries(map)) {
    assert.ok(objectKeys.includes(station), `${station} is not a scenario object`);
    assert.ok(kinds.has(kind), `${kind} is not a room kind`);
  }
  // Outdoor stations keep the sky; the schematic circulation station keeps the plain stage. Everything else has a room.
  const outdoor = new Set(["height", "battery", "facade", "circulation"]);
  for (const key of objectKeys) assert.equal(key in map, !outdoor.has(key), `${key}: room expected only for indoor stations`);
  for (const key of outdoor) if (key !== "circulation") assert.match(block(rooms, "export const GROUND_BY_STATION"), new RegExp(`\\b${key}:`), `${key} has a ground colour`);
});

test("rooms are disposed with the station and never cast shadows over it", () => {
  assert.match(app, /createRoom\(ROOM_BY_STATION\[scenario\.object\]\)/);
  assert.match(app, /room\.traverse\(\(node\) => \{ node\.userData\.noShadowCast = true; \}\)/);
  assert.match(app, /node\.castShadow = !node\.userData\.noShadowCast/);
  assert.match(rooms, /clearObject\(\) disposes them/);
});

test("skinned characters free their bone texture when a scenario is left (texture leak found in round 2)", () => {
  assert.match(app, /if \(child\.isSkinnedMesh\) child\.skeleton\?\.dispose\(\);/);
  // ...and that line runs before the sharedAsset early return, which skips everything else on cached models
  assert.ok(app.indexOf("child.skeleton?.dispose()") < app.indexOf("child.userData.sharedAsset) return"));
});

test("a model that finishes loading after the learner left is not added to a scene that is gone", () => {
  assert.match(dressing, /export function clearDressing\(\) \{[\s\S]*generation \+= 1;/);
  assert.match(dressing, /if \(!loaded \|\| !isCurrent\(\)\) return null/);
  assert.match(dressing, /station\.parent && isCurrent\(\)/);
});

test("every prop file added in round 2 is used, small, and under the triangle and size budgets", () => {
  const files = [...new Set([...dressing.matchAll(/"((?:props|people)\/[A-Za-z0-9_]+\.glb)"/g)].map((m) => m[1]))];
  for (const file of files) assert.ok(existsSync(new URL(file, models)), `${file} exists`);
});

test("ground-level props keep clear of hotspots they could hide, and stay on the platform or the room floor", () => {
  const table = block(dressing, "export const STATION_PROPS");
  const stations = [...table.matchAll(/^  (\w+): \[([\s\S]*?)^  \],/gm)];
  assert.ok(stations.length >= 10, "found the station prop tables");
  for (const [, key, body] of stations) {
    const hotspots = scenarios.filter((s) => s.object === key).flatMap((s) => s.steps.flatMap((step) => step.hotspots ?? []));
    for (const [, x, y, z, size] of body.matchAll(/pos: \[(-?[\d.]+), ([-\w.]+), (-?[\d.]+)\][^}]*?\b([hw]): ([\d.]+)/g).map((m) => [m[0], m[1], m[2], m[3], m[5]])) {
      const px = Number(x);
      const pz = Number(z);
      const onFloor = y.includes("ROOM_FLOOR_Y");
      if (!onFloor) assert.ok(Math.hypot(px, pz) <= 2.45, `${key}: prop at (${px}, ${pz}) is off the platform`);
      if (y !== "0" && !onFloor) continue; // sits on a table or bench, placed by eye against the primitive
      for (const hotspot of hotspots) {
        if (hotspot.at[1] > Number(size) + 0.15) continue; // the hotspot floats above this prop
        assert.ok(Math.hypot(hotspot.at[0] - px, hotspot.at[2] - pz) >= 0.4, `${key}: prop at (${px}, ${pz}) covers hotspot ${hotspot.id}`);
      }
    }
  }
});

test("reduced motion: no pulsing halos, no flash, no walking people, no spinning ring", () => {
  assert.match(app, /state\.status === "idle" && !reducedMotion \?/);
  assert.match(app, /const flashing = !reducedMotion && performance\.now\(\) < flashUntil/);
  assert.match(app, /if \(!reducedMotion\) tickDressing\(delta\)/);
  assert.match(app, /if \(!reducedMotion\) platformRing\.rotation\.z/);
});

test("keyboard and screen reader: focus follows the panel, actions say their state, no nested live regions", () => {
  assert.match(app, /function moveFocus\(element\)/);
  for (const target of ["simulationTitle : decisionPrompt", "completionTitle", 'catalog.querySelector\\("h2"\\)']) assert.match(app, new RegExp(`moveFocus\\(.*${target}`));
  assert.match(app, /\(done\)|"done" : "wrong"/);
  assert.match(app, /A disabled button drops keyboard focus/);
  const html = read("index.html");
  assert.doesNotMatch(html, /<section id="simulation"[^>]*aria-live/);
  assert.match(html, /Using a keyboard or screen reader\?/);
  assert.match(html, /<ol id="task-log"[^>]*aria-live="polite"/);
});

test("the camera follows the real panel width so the work area is never hidden behind it", () => {
  assert.match(app, /document\.querySelector\("\.panel"\)/);
  assert.match(app, /camera\.setViewOffset\(width, height, covered \/ 2 - 12, 0, width, height\)/);
  assert.match(read("style.css"), /@media \(min-width: 701px\) and \(max-width: 1100px\) \{\s*\.panel \{ width: clamp\(/);
});
