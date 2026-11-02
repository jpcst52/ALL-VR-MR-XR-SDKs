// Static checks for the professional-look pass: run with `node --test brand-kit/test`.
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { readFileSync, readdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { fitText, wrapWords } from "../layout.js";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const read = (path) => readFileSync(join(repo, path), "utf8");
const pages = ["index.html", "training-simulations/index.html", "multi-player/vr/index.html", "multi-player/ar/index.html", "multi-player/hydrotest/index.html"];
const measure = (text, size) => text.length * size * 0.5; // a monospace-like stand-in for canvas measureText

test("every page carries the Pitt-Bradford + CIST brand bar and loads the shared kit locally", () => {
  for (const page of pages) {
    const html = read(page);
    assert.match(html, /data-pitt-brand/, `${page} has a brand bar`);
    assert.match(html, /brand-kit\/brand\.css"/, `${page} loads brand.css`);
    assert.match(html, /brand-kit\/brand\.js"/, `${page} loads brand.js`);
    assert.doesNotMatch(html, /<(script|link)[^>]+(src|href)="https?:\/\//i, `${page} loads nothing from a CDN`);
    assert.doesNotMatch(html, /<img[^>]+branding\//, `${page} has no hard-coded logo <img> that could show as broken`);
  }
  const kit = read("brand-kit/brand.js");
  assert.match(kit, /CIST/);
  assert.match(kit, /Computer Information Systems & Technology/);
  assert.match(kit, /onerror = \(\) => resolve\(null\)/, "a missing logo falls back to the wordmark");
});

test("only the approved logo file is tracked: the rest of branding/ stays git-ignored, brand-kit holds no copied images", () => {
  const tracked = execFileSync("git", ["ls-files", "branding", "brand-kit"], { cwd: repo, encoding: "utf8" }).trim().split(/\r?\n/).filter(Boolean);
  const branding = tracked.filter((f) => f.startsWith("branding/"));
  assert.deepEqual(branding, ["branding/logo.png"], "only branding/logo.png (approved 2026-11-02) is tracked under branding/");
  const ignored = execFileSync("git", ["check-ignore", "branding/University_of_Pittsburgh_Brand_Guidelines.pdf", "branding/Shield_Bradford/Horizontal/PNG/x.png"], { cwd: repo, encoding: "utf8" });
  assert.match(ignored, /Brand_Guidelines\.pdf/, "the brand guidelines PDF stays ignored");
  assert.match(ignored, /Shield_Bradford/, "other logo variants stay ignored");
  assert.equal(tracked.filter((f) => f.startsWith("brand-kit/") && /\.(png|jpe?g|svg|eps)$/i.test(f)).length, 0, "brand-kit holds no copied logo files");
  for (const file of readdirSync(join(repo, "brand-kit"))) assert.doesNotMatch(file, /\.(png|jpe?g|svg|eps)$/i, `brand-kit/${file} is not an image`);
});

test("in-world text goes through the panel kit (no bare canvas text in scene code)", () => {
  const sources = [
    ...readdirSync(join(repo, "training-simulations")).filter((f) => f.endsWith(".js")).map((f) => `training-simulations/${f}`),
    ...readdirSync(join(repo, "multi-player", "shared")).filter((f) => f.endsWith(".js")).map((f) => `multi-player/shared/${f}`),
  ];
  for (const file of sources) {
    assert.doesNotMatch(read(file), /\.fillText\(/, `${file} draws text itself; use brand-kit/panels.js`);
    assert.doesNotMatch(read(file), /Arial|system-ui, sans-serif/, `${file} uses a non-brand font in the scene`);
  }
  assert.match(read("training-simulations/app.js"), /brandPlate\(/, "the Practice Lab scene shows the brand plaque");
  assert.match(read("training-simulations/app.js"), /brand: true/, "the scenario sign carries the brand band");
  assert.match(read("multi-player/shared/app.js"), /brandPlate\(/, "the multiplayer board shows the brand plaque");
});

test("panel text wraps, shrinks to fit, and is clipped with an ellipsis rather than overflowing", () => {
  assert.deepEqual(wrapWords("aa bb cc", measure, 10, 25), ["aa bb", "cc"]);
  const short = fitText("Lock out the breaker", { measure, maxWidth: 400, maxHeight: 100, size: 40, minSize: 20 });
  assert.equal(short.size, 40);
  assert.equal(short.truncated, false);
  const long = "word ".repeat(60).trim();
  const fit = fitText(long, { measure, maxWidth: 300, maxHeight: 120, size: 40, minSize: 18 });
  assert.ok(fit.size < 40 && fit.size >= 18, `shrank to ${fit.size}`);
  assert.ok(fit.lines.length * fit.size * 1.24 <= 120 + 0.001, "fits the height");
  for (const line of fit.lines) assert.ok(measure(line, fit.size) <= 300, `line fits the width: ${line}`);
  assert.ok(fit.truncated && fit.lines.at(-1).endsWith("…"), "overflow ends with an ellipsis");
});

test("scenario panels use the Pitt palette and bundled fonts", () => {
  const css = read("brand-kit/brand.css");
  for (const colour of ["#003594", "#ffb81c", "#00205b", "#dbeeff"]) assert.match(css, new RegExp(colour, "i"));
  for (const font of ["league-gothic", "merriweather", "roboto"]) assert.match(css, new RegExp(`\\.\\./vendor/fonts/${font}`));
  for (const sheet of ["training-simulations/style.css", "multi-player/shared/style.css"]) {
    const styles = read(sheet);
    assert.match(styles, /--gold: #ffb81c/i, `${sheet} uses Pitt gold`);
    assert.match(styles, /--royal: #003594/i, `${sheet} uses Pitt royal`);
    assert.doesNotMatch(styles, /--aqua|#214a43/i, `${sheet} has dropped the old generic teal theme`);
  }
});
