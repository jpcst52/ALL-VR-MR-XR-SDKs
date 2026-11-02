// Scenario-card thumbnails (thumbs/, made by tools/vr-capture/thumbs.mjs) and how the catalog uses them.
import assert from "node:assert/strict";
import { existsSync, readFileSync, statSync } from "node:fs";
import { test } from "node:test";
import { scenarios } from "../scenarios.js";

const here = new URL("../", import.meta.url);
const appSource = readFileSync(new URL("app.js", here), "utf8");

// Width and height from a baseline/progressive JPEG's SOF marker.
function jpegSize(bytes) {
  assert.equal(bytes.readUInt16BE(0), 0xffd8, "starts with a JPEG marker");
  let offset = 2;
  while (offset < bytes.length) {
    const marker = bytes.readUInt16BE(offset);
    const length = bytes.readUInt16BE(offset + 2);
    if (marker >= 0xffc0 && marker <= 0xffc3) return { height: bytes.readUInt16BE(offset + 5), width: bytes.readUInt16BE(offset + 7) };
    offset += 2 + length;
  }
  throw new Error("no SOF marker");
}

test("every scenario has a small 16:9 thumbnail keyed by its id, and the index lists them", () => {
  const index = JSON.parse(readFileSync(new URL("thumbs/index.json", here), "utf8"));
  for (const scenario of scenarios) {
    const file = new URL(`thumbs/${scenario.id}.jpg`, here);
    assert.ok(existsSync(file), `${scenario.id}.jpg exists`);
    assert.ok(index.ids.includes(scenario.id), `${scenario.id} is in thumbs/index.json`);
    const { width, height } = jpegSize(readFileSync(file));
    assert.deepEqual([width, height], [320, 180], `${scenario.id} is 320 x 180`);
    assert.ok(statSync(file).size < 40 * 1024, `${scenario.id}.jpg is small (${statSync(file).size} bytes)`);
  }
  for (const id of index.ids) assert.ok(scenarios.some((s) => s.id === id), `index entry ${id} is a scenario`);
});

test("the catalog never shows a broken thumbnail", () => {
  assert.match(appSource, /thumbs\/index\.json/, "only ids listed in the index are requested");
  assert.match(appSource, /image\.onerror = \(\) => resolve\(\)/, "a failed load resolves quietly");
  assert.match(appSource, /is-empty/, "a missing thumbnail shows a placeholder tile");
  assert.match(appSource, /image: thumbImages\.get\(scenario\.id\) \?\? null/, "the in-world cards fall back to a placeholder too");
});
