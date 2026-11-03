import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const models = join(here, "..", "..", "assets", "models");
const source = readFileSync(join(here, "..", "dressing.js"), "utf8");
const credits = readFileSync(join(models, "CREDITS.md"), "utf8");

const files = (sub) => readdirSync(join(models, sub)).filter((f) => f.endsWith(".glb")).map((f) => `${sub}/${f}`);
const referenced = [...new Set([...source.matchAll(/"((?:props|people)\/[A-Za-z0-9_]+\.glb)"/g)].map((m) => m[1]))];
const app = readFileSync(join(here, "..", "app.js"), "utf8") + readFileSync(join(here, "..", "core-stations.js"), "utf8");
const taggedInStations = [...new Set([...app.matchAll(/"((?:props|people)\/[A-Za-z0-9_]+\.glb)"/g)].map((m) => m[1]))];

test("every model file the code references exists and is a binary glTF", () => {
  assert.ok(referenced.length > 15, "expected the people and prop tables to be found");
  for (const file of [...referenced, ...taggedInStations]) {
    const path = join(models, file);
    assert.ok(existsSync(path), `${file} is referenced but missing`);
    assert.equal(readFileSync(path).subarray(0, 4).toString("latin1"), "glTF", `${file} is not a GLB`);
  }
});

test("every model file on disk has a credits entry with a source link and CC0 license", () => {
  for (const file of [...files("props"), ...files("people")]) {
    const row = credits.split("\n").find((line) => line.startsWith(`| ${file} |`));
    assert.ok(row, `${file} has no row in assets/models/CREDITS.md`);
    assert.match(row, /https:\/\/(polyhaven\.com\/a|poly\.pizza\/m)\//, `${file}: source link`);
    assert.match(row, /CC0 1\.0/, `${file}: license`);
  }
});

test("credits do not list files that are not shipped", () => {
  for (const [, file] of credits.matchAll(/^\| ((?:props|people)\/\S+\.glb) \|/gm)) assert.ok(existsSync(join(models, file)), `${file} listed but missing`);
});

test("people files carry Idle and Walk clips", () => {
  for (const file of files("people")) {
    const buffer = readFileSync(join(models, file));
    const json = JSON.parse(buffer.subarray(20, 20 + buffer.readUInt32LE(12)).toString("utf8"));
    const names = (json.animations ?? []).map((a) => a.name.toLowerCase());
    assert.ok(names.some((n) => n.endsWith("idle")), `${file} has no Idle clip`);
    assert.ok(names.some((n) => n.endsWith("walk")), `${file} has no Walk clip`);
  }
});

test("added model weight stays under the 25 MB budget", () => {
  const total = [...files("props"), ...files("people")].reduce((sum, file) => sum + statSync(join(models, file)).size, 0);
  assert.ok(total < 25 * 1024 * 1024, `models total ${(total / 1048576).toFixed(1)} MB`);
});

test("loader failure falls back to the primitive (a missing file resolves to null and is recorded)", async () => {
  // dressing.js imports three and the GLTFLoader through the page import map, so it is exercised in the browser by
  // tools/vr-capture/ui-check.mjs. Here we check the contract in the source: loadModel never rejects.
  assert.match(source, /\.catch\(\(error\) => \{[\s\S]*return null;/);
  assert.match(source, /if \(!loaded\) return/);
  assert.match(source, /if \(placed\) doll\.visible = false/);
});

test("the page footer links the model credits", () => {
  const html = readFileSync(join(here, "..", "index.html"), "utf8");
  assert.match(html, /assets\/models\/CREDITS\.md/);
});
