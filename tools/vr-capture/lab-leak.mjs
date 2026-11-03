// Visits all 17 scenarios in turn, several times, and prints the GPU geometry/texture counts and JS heap after each round.
// Flat numbers after round 1 mean nothing leaks when a scenario is left (a leak of bone textures was found and fixed in round 2).
// Usage: LEAK_BASE=http://localhost:8171/ node tools/vr-capture/lab-leak.mjs [rounds]
import { chromium } from "playwright";
const ids = ["customer-welcome","workplace-spill","delivery-check","informed-consent","sample-custody","field-observation","work-at-height","control-room-unit","refinery-battery-area","skyscraper-window-cleaning","welding-hot-work","electrical-work","lighter-lot-sampling","heat-exchanger-hydrotest","pipe-spool-welding","resistor-selection","circulation-builder"];
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const page = await (await browser.newContext({ viewport: { width: 640, height: 400 } })).newPage();
await page.route("**/*", (r) => new URL(r.request().url()).hostname === "localhost" ? r.continue() : r.abort());
page.on("pageerror", (e) => console.log("PAGE ERROR", e.message));
page.on("crash", () => console.log("PAGE CRASHED"));
await page.goto(`${(process.env.LEAK_BASE ?? "http://localhost:8171/").replace(/\/?$/, "/")}training-simulations/`);
await page.waitForTimeout(1200);
await page.fill("#learner-name", "A"); await page.click("#profile button[type=submit]");
for (let round = 1; round <= Number(process.argv[2] ?? 3); round++) {
  for (const id of ids) {
    await page.click(`[data-scenario="${id}"]`);
    await page.waitForTimeout(1200);
    await page.click("#back-to-catalog");
    await page.waitForTimeout(200);
  }
  await page.waitForTimeout(1500);
  const s = await page.evaluate(() => ({ ...globalThis.__labStats().memory, heap: Math.round(performance.memory.usedJSHeapSize / 1e6) }));
  console.log("round", round, JSON.stringify(s));
}
await browser.close();
