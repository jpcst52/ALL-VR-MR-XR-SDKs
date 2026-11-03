// Makes the scenario-card thumbnails for the Practice Lab from the README screenshots.
//
//   node tools/vr-capture/thumbs.mjs            (from the repository root; needs `npm install` in tools/vr-capture)
//
// Input:  training-simulations/screenshots/03-scenario-<n>-<slug>.png (1280 x 1100 desktop captures, made with the
//         git-ignored logo blocked by readme-shots.mjs).
// Output: training-simulations/thumbs/<scenario-id>.jpg, 320 x 180 (16:9) JPEG, plus thumbs/index.json (the ids that
//         have a thumbnail). The catalog looks thumbnails up by scenario id and shows a plain card if one is missing.
//
// The crop takes the 3D work area only: left of the page panel (x < 596) and below the in-scene scenario sign
// (y > 328), so neither the panel's brand bar nor the sign's brand band (wordmark, or the logo on a lab PC) is in a
// thumbnail. Uses Playwright's Chrome (already a dependency here) to scale and encode; no image library needed.
import { chromium } from "playwright";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const shots = join(repo, "training-simulations", "screenshots");
const out = join(repo, "training-simulations", "thumbs");
mkdirSync(out, { recursive: true });
const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);

export const size = { width: 320, height: 180 };
const defaultCrop = { x: 0, y: 420, width: 596, height: 335 }; // 16:9, the work area on the left of the panel
const crops = {}; // per-scenario overrides, e.g. { "circulation-builder": { x: 0, y: 330, width: 596, height: 335 } }
const slug = (title) => title.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/-$/, "");

const browser = await chromium.launch({ channel: "chrome" });
const page = await browser.newPage();
const made = [];
for (const [index, scenario] of scenarios.entries()) {
  const source = join(shots, `03-scenario-${index + 1}-${slug(scenario.title)}.png`);
  if (!existsSync(source)) {
    console.log(`skip ${scenario.id}: no screenshot (${source})`);
    continue;
  }
  const crop = crops[scenario.id] ?? defaultCrop;
  const dataUrl = `data:image/png;base64,${readFileSync(source).toString("base64")}`;
  const jpeg = await page.evaluate(async ({ dataUrl, crop, size }) => {
    const image = new Image();
    image.src = dataUrl;
    await image.decode();
    // Two-step downscale for a cleaner result than one large step.
    const mid = document.createElement("canvas");
    mid.width = size.width * 2;
    mid.height = size.height * 2;
    const m = mid.getContext("2d");
    m.imageSmoothingQuality = "high";
    m.drawImage(image, crop.x, crop.y, crop.width, crop.height, 0, 0, mid.width, mid.height);
    const canvas = document.createElement("canvas");
    canvas.width = size.width;
    canvas.height = size.height;
    const c = canvas.getContext("2d");
    c.imageSmoothingQuality = "high";
    c.drawImage(mid, 0, 0, size.width, size.height);
    return canvas.toDataURL("image/jpeg", 0.8).split(",")[1];
  }, { dataUrl, crop, size });
  const file = join(out, `${scenario.id}.jpg`);
  const bytes = Buffer.from(jpeg, "base64");
  writeFileSync(file, bytes);
  made.push(scenario.id);
  console.log(`wrote thumbs/${scenario.id}.jpg (${(bytes.length / 1024).toFixed(1)} KB)`);
}
await browser.close();
writeFileSync(join(out, "index.json"), `${JSON.stringify({ size: [size.width, size.height], ids: made }, null, 2)}\n`);
console.log(`${made.length} of ${scenarios.length} thumbnails written to training-simulations/thumbs/`);
