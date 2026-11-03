// Re-shoots the four single-SDK web demo screenshots (desktop 1440x900, plus a 390x844 phone view) and reports
// console errors. Serve the repo root first:  python -m http.server 8163   then   STATIC_BASE=http://localhost:8163/ node demos-shots.mjs
// Only one demo: node demos-shots.mjs threejs. The git-ignored/optional logo is blocked so shots show the text wordmark.
import { chromium } from "playwright";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.STATIC_BASE ?? "http://localhost:8163/").replace(/\/?$/, "/");
const demos = {
  threejs: { name: "threejs-prism-bloom.png", settle: 3500, act: async (page) => { await page.keyboard.press("c"); } },
  "a-frame": { name: "a-frame-orbit-garden.png", settle: 4500, act: async (page) => { await page.evaluate(() => window.__demo?.select?.(1)); } },
  babylonjs: { name: "babylonjs-luminous-totem.png", settle: 4500, act: async (page) => { await page.keyboard.press("c"); } },
  webxr: { name: "webxr-color-field.png", settle: 3000, act: async (page) => { await page.keyboard.press("c"); } },
};
const only = process.argv[2];
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
let errors = 0;
for (const [folder, demo] of Object.entries(demos)) {
  if (only && only !== folder) continue;
  for (const [label, viewport] of [["desktop", { width: 1440, height: 900 }], ["phone", { width: 390, height: 844 }]]) {
    const context = await browser.newContext({ viewport, deviceScaleFactor: 1, reducedMotion: "no-preference", hasTouch: label === "phone" });
    const page = await context.newPage();
    const problems = [];
    await page.route("**/*", (route) => {
      const url = new URL(route.request().url());
      if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
      if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
      return route.continue();
    });
    page.on("pageerror", (error) => problems.push(`pageerror ${error.message}`));
    page.on("console", (message) => { if (message.type() === "error" && !/branding\/logo|404/.test(message.text())) problems.push(`console ${message.text()}`); });
    await page.goto(`${base}${folder}/`);
    await page.waitForTimeout(demo.settle);
    await demo.act(page);
    await page.waitForTimeout(900);
    const out = label === "desktop" ? join(repo, "screenshots", demo.name) : join(repo, "screenshots", "phone", demo.name);
    mkdirSync(dirname(out), { recursive: true });
    await page.screenshot({ path: out });
    if (label === "desktop") {
      mkdirSync(join(repo, folder, "screenshots"), { recursive: true });
      await page.screenshot({ path: join(repo, folder, "screenshots", demo.name) });
    }
    console.log(`${problems.length ? "ERR " : "ok  "} ${folder} ${label} -> ${out}${problems.length ? "\n   " + problems.join("\n   ") : ""}`);
    errors += problems.length;
    await context.close();
  }
}
await browser.close();
process.exit(errors ? 1 : 0);
