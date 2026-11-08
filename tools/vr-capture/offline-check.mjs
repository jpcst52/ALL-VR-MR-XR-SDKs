// Loads each example with all non-local network access blocked and reports errors.
import { chromium } from "playwright";
// STATIC_BASE / RELAY_BASE override the default servers (e.g. both http://localhost:8132/ when the relay serves the site).
const staticBase = (process.env.STATIC_BASE ?? "http://localhost:8000/").replace(/\/?$/, "/");
const relayBase = (process.env.RELAY_BASE ?? "http://localhost:8443/").replace(/\/?$/, "/");
const pages = [
  ...["a-frame/", "threejs/", "babylonjs/", "webxr/", "training-simulations/", ""].map((path) => staticBase + path),
  ...["multi-player/vr/", "multi-player/ar/", "multi-player/hydrotest/", "multi-player/v2/", "multi-player/v2/?theme=safety"].map((path) => relayBase + path),
];
// branding/logo.png is git-ignored and optional: pages fall back to a text wordmark when it is missing.
const optional = (url) => url.includes("favicon") || url.includes("/branding/");
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
let failed = 0;
for (const url of pages) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
  const page = await context.newPage();
  const problems = [];
  await page.route("**/*", (route) => {
    const host = new URL(route.request().url()).hostname;
    if (host === "localhost" || host === "127.0.0.1") return route.continue();
    problems.push(`blocked external request: ${route.request().url()}`);
    return route.abort();
  });
  page.on("pageerror", (error) => problems.push(`page error: ${error.message}`));
  page.on("requestfailed", (request) => { if (!optional(request.url())) problems.push(`failed: ${request.url()}`); });
  page.on("response", (response) => { if (response.status() >= 400 && !optional(response.url())) problems.push(`HTTP ${response.status()}: ${response.url()}`); });
  await page.goto(url);
  await page.waitForTimeout(3500);
  const canvases = await page.locator("canvas").count();
  console.log(`${problems.length ? "FAIL" : "ok  "} ${url}  canvases=${canvases}${problems.length ? "\n   " + problems.join("\n   ") : ""}`);
  failed += problems.length ? 1 : 0;
  await context.close();
}
await browser.close();
process.exit(failed ? 1 : 0);
