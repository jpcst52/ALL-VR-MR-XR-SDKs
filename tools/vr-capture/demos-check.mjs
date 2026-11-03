// Automated check for the four single-SDK web demos (threejs/, a-frame/, babylonjs/, webxr/).
// For each demo, at desktop (1280x800) and phone (390x844) width, with every non-local request blocked, it fails on:
//   - console errors, uncaught page errors, failed requests or HTTP >= 400 (the optional branding/logo.png 404 is allowed)
//   - horizontal page scroll on the phone, missing canvas, missing title/status, buttons smaller than 40 px
// and, from the files the page really requested, it fails if an asset is not credited in CREDITS-web-demos.md,
// if a demo's own assets exceed 8 MB, or if its glTF triangle count (counting every instance) exceeds 100k.
// Serve the repo root first:  python -m http.server 8163   then   STATIC_BASE=http://localhost:8163/ node demos-check.mjs
import { chromium } from "playwright";
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.STATIC_BASE ?? "http://localhost:8163/").replace(/\/?$/, "/");
const credits = readFileSync(join(repo, "CREDITS-web-demos.md"), "utf8");
// instances = how many copies of each model the page places (for the triangle budget)
const demos = {
  threejs: { instances: { brass_vase_03: 1, ceramic_vase_04: 1 } },
  "a-frame": { instances: { brass_diya_lantern: 3 } },
  babylonjs: { instances: { bronze_ray_statue: 1, ceramic_pot: 1 } },
  webxr: { instances: {} },
};
const counts = { pages: 0, passes: 0, failures: 0, assetsChecked: 0, modelsChecked: 0 };
const fail = (where, message) => { counts.failures += 1; console.log(`FAIL ${where}: ${message}`); };
const pass = (where, message) => { counts.passes += 1; console.log(`ok   ${where}: ${message}`); };

function dirSize(dir) {
  if (!existsSync(dir)) return 0;
  return readdirSync(dir, { withFileTypes: true }).reduce((sum, entry) => sum + (entry.isDirectory() ? dirSize(join(dir, entry.name)) : statSync(join(dir, entry.name)).size), 0);
}
function triangles(gltfPath) {
  const gltf = JSON.parse(readFileSync(gltfPath, "utf8"));
  let total = 0;
  for (const mesh of gltf.meshes) for (const primitive of mesh.primitives) {
    total += (primitive.indices !== undefined ? gltf.accessors[primitive.indices].count : gltf.accessors[primitive.attributes.POSITION].count) / 3;
  }
  return Math.round(total);
}

const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
for (const [folder, config] of Object.entries(demos)) {
  const requested = new Set();
  for (const [label, viewport] of [["desktop", { width: 1280, height: 800 }], ["phone", { width: 390, height: 844 }]]) {
    const where = `${folder} ${label}`;
    counts.pages += 1;
    const context = await browser.newContext({ viewport, hasTouch: label === "phone" });
    const page = await context.newPage();
    const problems = [];
    await page.route("**/*", (route) => {
      const url = new URL(route.request().url());
      if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") { problems.push(`external request blocked: ${url.href}`); return route.abort(); }
      if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
      return route.continue();
    });
    page.on("pageerror", (error) => problems.push(`page error: ${error.message}`));
    page.on("console", (message) => { if (message.type() === "error" && !/status of 404/.test(message.text())) problems.push(`console error: ${message.text()}`); });
    const served = new Set();
    page.on("response", (response) => { if (response.status() < 400) served.add(response.url()); });
    page.on("requestfailed", async (request) => {
      const url = request.url();
      // Chrome occasionally cancels one of two overlapping identical requests; that is fine if the file was served.
      await new Promise((resolve) => setTimeout(resolve, 500));
      if (request.failure()?.errorText === "net::ERR_ABORTED" && served.has(url)) return;
      if (!problems.some((p) => p.includes(url))) problems.push(`request failed: ${url} (${request.failure()?.errorText})`);
    });
    // The demos log a console warning when they fall back to a primitive because a model or sky failed to load.
    page.on("console", (message) => { if (message.type() === "warning" && /did not load|did not start|unavailable; using/i.test(message.text())) problems.push(`fallback used: ${message.text()}`); });
    page.on("response", (response) => {
      const url = new URL(response.url());
      if (response.status() >= 400 && !url.pathname.startsWith("/branding/")) problems.push(`HTTP ${response.status()}: ${url.pathname}`);
      if (response.status() < 400) requested.add(decodeURIComponent(url.pathname));
    });
    await page.goto(`${base}${folder}/`);
    await page.waitForTimeout(5000);
    const info = await page.evaluate(() => ({
      title: document.title,
      canvases: document.querySelectorAll("canvas").length,
      h1: document.querySelector("h1")?.textContent.trim(),
      status: document.querySelector("#status")?.textContent.trim(),
      overflowX: document.documentElement.scrollWidth - window.innerWidth,
      smallButtons: [...document.querySelectorAll(".demo-panel button")].filter((b) => b.getBoundingClientRect().height < 40).map((b) => b.textContent.trim()),
      brandBar: Boolean(document.querySelector(".pitt-brand")),
    }));
    if (problems.length) problems.forEach((p) => fail(where, p));
    if (info.canvases < 1) fail(where, "no canvas");
    if (!info.h1 || !info.title) fail(where, "missing title or heading");
    if (!info.status || /^Loading|^Checking/.test(info.status) && folder !== "webxr") fail(where, `status never became ready: "${info.status}"`);
    if (info.overflowX > 0) fail(where, `horizontal scroll of ${info.overflowX}px`);
    if (info.smallButtons.length) fail(where, `buttons under 40 px tall: ${info.smallButtons.join(", ")}`);
    if (!info.brandBar) fail(where, "brand bar missing");
    if (!problems.length && info.canvases >= 1 && info.h1 && info.overflowX <= 0 && !info.smallButtons.length && info.brandBar) pass(where, `canvases=${info.canvases}, no console/page/request errors, no horizontal scroll`);
    await context.close();
  }

  // Every asset this demo requested must exist on disk and be credited.
  const assets = [...requested].filter((path) => /\/(assets|vendor\/three\/addons\/environments)\//.test(path) && /\.(gltf|glb|bin|jpg|jpeg|png|hdr|webp)$/.test(path));
  const credited = new Set();
  for (const path of assets) {
    counts.assetsChecked += 1;
    const file = join(repo, path);
    if (!existsSync(file)) { fail(folder, `asset missing on disk: ${path}`); continue; }
    const parts = path.split("/").filter(Boolean); // e.g. threejs/assets/brass_vase_03/x.bin or assets/skies/summer-2k.jpg
    const name = parts[2] ?? parts[1]; // model folder name, or the sky file name (summer-2k.jpg)
    if (!credited.has(name)) {
      credited.add(name);
      if (!credits.includes(name)) fail(folder, `asset "${name}" (${path}) is not credited in CREDITS-web-demos.md`);
    }
  }
  const ownAssets = dirSize(join(repo, folder, "assets")) + dirSize(join(repo, folder, "lib")) + assets.filter((p) => p.startsWith("/assets/skies/")).reduce((sum, p) => sum + statSync(join(repo, p)).size, 0);
  if (ownAssets > 8 * 1024 * 1024) fail(folder, `assets total ${(ownAssets / 1048576).toFixed(1)} MB (> 8 MB)`);
  let tris = 0;
  for (const [model, n] of Object.entries(config.instances)) {
    const gltf = join(repo, folder, "assets", model, `${model}_1k.gltf`);
    if (!existsSync(gltf)) { fail(folder, `model missing: ${relative(repo, gltf)}`); continue; }
    counts.modelsChecked += 1;
    tris += triangles(gltf) * n;
  }
  if (tris > 100000) fail(folder, `${tris} triangles in glTF models (> 100k)`);
  else pass(folder, `${assets.length} asset files requested and credited, own assets ${(ownAssets / 1048576).toFixed(1)} MB, glTF triangles ${tris}`);
}
await browser.close();
console.log(`\n${counts.pages} page loads, ${counts.assetsChecked} asset files checked, ${counts.modelsChecked} models counted, ${counts.passes} passes, ${counts.failures} failures`);
process.exit(counts.failures ? 1 : 0);
