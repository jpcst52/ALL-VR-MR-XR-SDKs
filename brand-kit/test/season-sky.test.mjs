// Seasonal skies: the pure parts (season by date, manifest handling, fallbacks, transition curve, clock).
// Run with `node --test` in brand-kit/.
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import {
  SEASONS, seasonForDate, seasonOf, normalizeManifest, startIndex, nextIndex, pickFile, resolveSkyUrl,
  skyCaption, creditLines, transitionState, SkyClock, ROTATE_MS, GLITCH_MS,
} from "../season-sky-core.js";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const sample = SEASONS.map((season) => ({ id: season, label: season[0].toUpperCase() + season.slice(1), file2k: `${season}-2k.jpg`, file4k: `${season}-4k.jpg`, credit: `${season} by someone, CC0`, isBradford: false }));

test("season by date uses meteorological seasons", () => {
  const at = (month) => seasonForDate(new Date(2026, month, 15));
  assert.deepEqual([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11].map(at), [
    "winter", "winter", "spring", "spring", "spring", "summer", "summer", "summer", "autumn", "autumn", "autumn", "winter",
  ]);
  assert.equal(seasonForDate(new Date(2026, 10, 13)), "autumn", "the Nov 13 demo is in autumn");
});

test("season names are read from id or label; fall counts as autumn", () => {
  assert.equal(seasonOf({ id: "fall" }), "autumn");
  assert.equal(seasonOf({ id: "x", label: "Late Autumn" }), "autumn");
  assert.equal(seasonOf({ id: "winter-2k" }), "winter");
  assert.equal(seasonOf({ id: "beach" }), null);
});

test("the manifest is ordered by season, bad entries are dropped, and bad input never throws", () => {
  const shuffled = [sample[3], { id: "beach", file2k: "b.jpg" }, sample[1], { id: "spring" }, sample[0], sample[2], { ...sample[0], id: "spring-again", label: "Spring" }];
  const entries = normalizeManifest(shuffled);
  assert.deepEqual(entries.map((e) => e.season), ["spring", "summer", "autumn", "winter"]);
  assert.equal(entries[0].file2k, "spring-2k.jpg", "the first usable spring entry wins (an entry without files is skipped)");
  for (const bad of [null, undefined, 42, "x", {}, { skies: "no" }, [null, 3, "x"]]) assert.deepEqual(normalizeManifest(bad), []);
  assert.equal(normalizeManifest({ skies: sample }).length, 4, "an object with a skies array is accepted");
});

test("start on today's season, or the next available one", () => {
  const entries = normalizeManifest(sample);
  assert.equal(entries[startIndex(entries, new Date(2026, 10, 1))].season, "autumn");
  assert.equal(entries[startIndex(entries, new Date(2026, 0, 1))].season, "winter");
  const noAutumn = normalizeManifest(sample.filter((e) => e.id !== "autumn"));
  assert.equal(noAutumn[startIndex(noAutumn, new Date(2026, 10, 1))].season, "winter");
  assert.equal(startIndex([], new Date()), -1);
  assert.equal(nextIndex(3, 4), 0);
  assert.equal(nextIndex(0, 0), -1);
});

test("2k by default, 4k only on request, and whichever exists as a fallback", () => {
  const [entry] = normalizeManifest(sample);
  assert.equal(pickFile(entry), "spring-2k.jpg");
  assert.equal(pickFile(entry, { prefer4k: true }), "spring-4k.jpg");
  const only4k = normalizeManifest([{ id: "winter", file4k: "w4.jpg" }])[0];
  assert.equal(pickFile(only4k), "w4.jpg");
});

test("image paths resolve against the manifest folder or the repository root", () => {
  const manifest = "http://localhost:8000/assets/skies/skies.json";
  assert.equal(resolveSkyUrl("spring-2k.jpg", manifest), "http://localhost:8000/assets/skies/spring-2k.jpg");
  assert.equal(resolveSkyUrl("assets/skies/spring-2k.jpg", manifest), "http://localhost:8000/assets/skies/spring-2k.jpg");
  assert.equal(resolveSkyUrl("data:image/png;base64,AA", manifest), "data:image/png;base64,AA");
});

test("the page never claims Bradford photos unless the manifest says isBradford", () => {
  const entries = normalizeManifest(sample);
  assert.match(skyCaption(entries), /regional style/i);
  assert.doesNotMatch(skyCaption(entries), /photographed in Bradford/i);
  for (const line of creditLines(entries)) assert.doesNotMatch(line, /Bradford/);
  const local = normalizeManifest(sample.map((e) => ({ ...e, isBradford: true })));
  assert.match(skyCaption(local), /Bradford, PA/);
  const mixed = normalizeManifest(sample.map((e, i) => ({ ...e, isBradford: i === 0 })));
  assert.match(skyCaption(mixed), /some from Bradford/);
  assert.equal(normalizeManifest([{ ...sample[0], isBradford: "yes" }])[0].isBradford, false, "only a real true counts");
  assert.equal(skyCaption([]), "");
});

test("the glitch transition grows, peaks at the swap, and clears; reduced motion is a plain cross-fade", () => {
  const at = (p) => transitionState(p);
  assert.deepEqual(at(0), { mix: 0, pixel: 0, scan: 0, chroma: 0 });
  assert.ok(at(0.25).pixel > 0 && at(0.25).pixel < at(0.5).pixel, "blocks grow");
  assert.ok(at(0.5).scan > 0.5 && at(0.5).chroma > 0, "scan lines and colour split at the peak");
  assert.equal(at(0.4).mix, 0, "still the old image before the peak");
  assert.equal(at(0.6).mix, 1, "the new image after the peak");
  assert.ok(at(1).pixel < 1e-9 && at(1).scan < 1e-9, "clears at the end");
  assert.equal(at(1).mix, 1);
  for (const p of [0, 0.3, 0.5, 0.8, 1]) {
    const calm = transitionState(p, { reducedMotion: true });
    assert.equal(calm.pixel + calm.scan + calm.chroma, 0, "no mosaic, scan lines, or flicker with reduced motion");
  }
  assert.ok(transitionState(0.5, { reducedMotion: true }).mix > 0.4 && transitionState(0.5, { reducedMotion: true }).mix < 0.6);
  assert.deepEqual(transitionState(-1), at(0), "progress is clamped");
});

test("the clock rotates every 30 seconds, swaps at the half-way point, and does nothing with one image", () => {
  assert.equal(ROTATE_MS, 30000);
  assert.ok(GLITCH_MS >= 800 && GLITCH_MS <= 1500, "the glitch lasts about a second");
  const clock = new SkyClock({ count: 4, index: 2 });
  assert.equal(clock.update(29999).transition, null);
  const started = clock.update(1);
  assert.deepEqual(started.transition && [started.transition.from, started.transition.to], [2, 3]);
  let swapped = 0;
  let finished = null;
  for (let t = 0; t < GLITCH_MS + 50; t += 16) {
    const state = clock.update(16);
    if (state.swapped) swapped += 1;
    if (state.finished) finished = state;
  }
  assert.equal(swapped, 1, "one swap per transition");
  assert.equal(finished.index, 3);
  clock.update(0);
  for (let i = 0; i < 4; i += 1) { clock.trigger(); for (let t = 0; t < GLITCH_MS + 50; t += 50) clock.update(50); }
  assert.equal(clock.index, 3, "four more seasons brings it back round");
  const single = new SkyClock({ count: 1 });
  assert.equal(single.trigger(), false);
  assert.equal(single.update(1e6).transition, null);
  const paused = new SkyClock({ count: 4 });
  for (let i = 0; i < 1000; i += 1) paused.update(0);
  assert.equal(paused.elapsed, 0, "no time passes while paused (dt = 0)");
});

test("the real manifest, if present, follows the contract and its files exist", { skip: !existsSync(join(repo, "assets", "skies", "skies.json")) }, () => {
  const json = JSON.parse(readFileSync(join(repo, "assets", "skies", "skies.json"), "utf8"));
  assert.ok(Array.isArray(json));
  for (const entry of json) for (const key of ["id", "label", "file4k", "file2k", "credit", "isBradford"]) assert.ok(key in entry, `${entry.id} has ${key}`);
  const entries = normalizeManifest(json);
  assert.equal(entries.length, 4, "one sky per season");
  for (const entry of entries) {
    assert.ok(entry.credit.length > 10, `${entry.season} has a credit`);
    assert.ok(existsSync(join(repo, "assets", "skies", entry.file2k)), `${entry.file2k} exists`);
  }
});

test("the sky module is wired the way the page relies on", () => {
  const source = readFileSync(join(repo, "brand-kit", "season-sky.js"), "utf8");
  assert.match(source, /visibilitychange/, "pauses while the tab is hidden");
  assert.match(source, /ShaderMaterial/);
  assert.doesNotMatch(source, /EffectComposer|gl_FragCoord/, "no screen-space post-processing (stereo-safe)");
  assert.doesNotMatch(source, /https?:\/\//, "no network URLs");
  const app = readFileSync(join(repo, "training-simulations", "app.js"), "utf8");
  assert.match(app, /createSeasonSky\(/);
  assert.match(app, /prefers-reduced-motion/);
});
