// Loads each example with all non-local network access blocked and reports errors.
import { chromium } from "playwright";
const pages = [
  "http://localhost:8000/a-frame/", "http://localhost:8000/threejs/", "http://localhost:8000/babylonjs/", "http://localhost:8000/webxr/",
  "http://localhost:8000/training-simulations/", "http://localhost:8443/multi-player/vr/", "http://localhost:8443/multi-player/ar/",
];
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
  page.on("requestfailed", (request) => { if (!request.url().includes("favicon")) problems.push(`failed: ${request.url()}`); });
  page.on("response", (response) => { if (response.status() >= 400 && !response.url().includes("favicon")) problems.push(`HTTP ${response.status()}: ${response.url()}`); });
  await page.goto(url);
  await page.waitForTimeout(3500);
  const canvases = await page.locator("canvas").count();
  console.log(`${problems.length ? "FAIL" : "ok  "} ${url}  canvases=${canvases}${problems.length ? "\n   " + problems.join("\n   ") : ""}`);
  failed += problems.length ? 1 : 0;
  await context.close();
}
await browser.close();
process.exit(failed ? 1 : 0);
