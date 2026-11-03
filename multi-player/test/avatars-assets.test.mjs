import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import {
  CHARACTERS, HAT_COLORS, MAX_MODEL_AVATARS, SCENE_TRIANGLE_BUDGET, angleDelta, avatarScaleForHead, chooseAnimation,
  hashString, loadWithFallback, lookFor, normalizePose, smoothing,
} from "../shared/avatar-logic.mjs";
import { fallbackKind, layouts, modelIdsInLayouts } from "../shared/layouts.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const mp = join(here, "..");
const models = join(mp, "..", "assets", "models-mp");
const manifest = JSON.parse(readFileSync(join(mp, "shared", "models.json"), "utf8"));
const credits = readFileSync(join(mp, "CREDITS.md"), "utf8");

function glbJson(path) {
  const buffer = readFileSync(path);
  assert.equal(buffer.readUInt32LE(0), 0x46546c67, `${path} is a GLB`);
  return JSON.parse(buffer.subarray(20, 20 + buffer.readUInt32LE(12)).toString("utf8"));
}

function triangles(path) {
  const json = glbJson(path);
  let total = 0;
  for (const mesh of json.meshes ?? []) {
    for (const primitive of mesh.primitives) {
      total += (json.accessors[primitive.indices]?.count ?? json.accessors[primitive.attributes.POSITION].count) / 3;
    }
  }
  return total;
}

test("every model in the manifest exists, is a GLB, and has a credits entry with a CC0 license", () => {
  const entries = [...Object.entries(manifest.props), ...Object.entries(manifest.characters)];
  assert.ok(entries.length >= 20);
  for (const [id, entry] of entries) {
    const path = join(models, entry.file);
    assert.ok(existsSync(path), `${id}: ${entry.file} is missing`);
    glbJson(path);
    assert.ok(credits.includes(entry.file), `${entry.file} has no row in CREDITS.md`);
    assert.ok(entry.author && entry.source, `${id} lacks author or source`);
    assert.ok(credits.includes(entry.source), `${entry.file} source URL is not in CREDITS.md`);
    const pack = manifest.packs[entry.pack];
    assert.match(pack.license, /CC0/);
    assert.match(pack.licenseUrl, /publicdomain\/zero/);
  }
  assert.match(credits, /CC0/);
});

test("no model file is shipped without a manifest entry", () => {
  const listed = new Set([...Object.values(manifest.props), ...Object.values(manifest.characters)].map((entry) => entry.file));
  for (const folder of ["props", "characters"]) {
    for (const file of readdirSync(join(models, folder))) assert.ok(listed.has(`${folder}/${file}`), `${folder}/${file} is not in models.json`);
  }
});

test("the README links the credits file", () => {
  assert.match(readFileSync(join(mp, "README.md"), "utf8"), /\(\.\/CREDITS\.md\)/);
});

test("every prop used by a layout is in the manifest, and the characters used by avatars are too", () => {
  for (const id of modelIdsInLayouts()) assert.ok(manifest.props[id], `layout uses unknown prop ${id}`);
  for (const id of CHARACTERS) assert.ok(manifest.characters[id], `avatars use unknown character ${id}`);
});

test("performance budget: models stay small and a full scene stays under the triangle budget", () => {
  const propTris = Object.fromEntries(Object.entries(manifest.props).map(([id, entry]) => [id, triangles(join(models, entry.file))]));
  for (const [id, count] of Object.entries(propTris)) assert.ok(count <= 8000, `${id} has ${count} triangles`);
  const charTris = Object.values(manifest.characters).map((entry) => triangles(join(models, entry.file)));
  for (const count of charTris) assert.ok(count <= 6000, `character has ${count} triangles`);
  for (const entry of [...Object.values(manifest.props), ...Object.values(manifest.characters)]) {
    assert.ok(statSync(join(models, entry.file)).size <= 700 * 1024, `${entry.file} is over 700 KB`);
  }
  const worstCharacter = Math.max(...charTris);
  for (const [themeId, layout] of Object.entries(layouts)) {
    const env = layout.props.reduce((sum, prop) => sum + propTris[prop.model], 0) + 4000; // plus generated room and benches
    const parts = 15000; // allowance for roughly 40 shared parts built from primitives
    const people = MAX_MODEL_AVATARS * worstCharacter;
    assert.ok(env + parts + people < SCENE_TRIANGLE_BUDGET, `${themeId}: ${env + parts + people} triangles over budget`);
  }
});

test("avatar looks are deterministic, varied, and use known characters and hat colors", () => {
  assert.deepEqual(lookFor("abc"), lookFor("abc"));
  const seen = new Set();
  for (let index = 0; index < 60; index += 1) {
    const look = lookFor(`peer-${index}`);
    assert.ok(CHARACTERS.includes(look.character));
    assert.ok(HAT_COLORS.includes(look.hat));
    seen.add(`${look.character}/${look.hat.name}`);
  }
  assert.ok(seen.size >= 16, "looks should be varied");
  assert.equal(hashString("x"), hashString("x"));
});

test("animation choice, smoothing and turning helpers", () => {
  assert.equal(chooseAnimation({ moving: true }), "Walking_A");
  assert.equal(chooseAnimation({}), "Unarmed_Idle");
  assert.equal(chooseAnimation({ holding: true, moving: true }), "Interact");
  assert.equal(smoothing(0), 0);
  assert.ok(smoothing(1 / 60) > 0 && smoothing(1 / 60) < 1);
  assert.ok(Math.abs(angleDelta(3, -3)) < Math.PI);
  assert.ok(Math.abs(angleDelta(0, Math.PI * 2)) < 1e-9);
  assert.equal(avatarScaleForHead(100), 1.5);
  assert.equal(avatarScaleForHead(0.1), 0.5);
  assert.equal(avatarScaleForHead(NaN), 1);
});

test("poses are normalized and bad ones rejected", () => {
  assert.deepEqual(
    normalizePose({ head: [0, 1.6, 0], yaw: 0.5, hands: [null, [1, 1, 1]] }),
    { head: [0, 1.6, 0], yaw: 0.5, hands: [null, [1, 1, 1]] },
  );
  assert.equal(normalizePose(null), null);
  assert.equal(normalizePose({ head: [0, 1.6, 0], yaw: 0, hands: [null] }), null);
  assert.equal(normalizePose({ head: [0, 99, 0], yaw: 0, hands: [null, null] }), null);
});

test("fallback: a failing model load produces the fallback instead of throwing", async () => {
  const failed = await loadWithFallback(async () => { throw new Error("404 model missing"); }, () => "primitive");
  assert.equal(failed.usedFallback, true);
  assert.equal(failed.value, "primitive");
  assert.match(failed.error.message, /404/);
  const rejected = await loadWithFallback(() => Promise.reject(new Error("network")), async () => "box");
  assert.equal(rejected.value, "box");
  const fine = await loadWithFallback(async () => "model", () => "primitive");
  assert.deepEqual([fine.usedFallback, fine.value], [false, "model"]);
});

test("fallback: every layout prop maps to a primitive stand-in kind", () => {
  const known = new Set(["barrel", "tank", "extinguisher", "crate", "cart", "tool", "pipe", "lamp", "generic"]);
  for (const id of modelIdsInLayouts()) assert.ok(known.has(fallbackKind(id)), id);
  assert.equal(fallbackKind("Barrel_01"), "barrel");
  assert.equal(fallbackKind("korean_fire_extinguisher_01"), "extinguisher");
});

test("layouts keep floor props out of the board pad", () => {
  for (const [themeId, layout] of Object.entries(layouts)) {
    for (const prop of layout.props) {
      if ((prop.y ?? 0) > 1.2) continue; // wall and ceiling mounted items
      assert.ok(Math.hypot(prop.x, prop.z) > 3.9, `${themeId}/${prop.model} is on the board pad`);
    }
  }
});
