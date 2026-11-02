// Usage: node status-shot.mjs <out.png> [light|dark]   (reads the dashboard address from tools/status/.dashboard-url, or DASH_URL)
import fs from "node:fs";
import { chromium } from "playwright";
const browser = await chromium.launch({ channel: "chrome" });
const page = await browser.newPage({ viewport: { width: 1400, height: 1500 }, colorScheme: process.argv[3] === "dark" ? "dark" : "light" });
await page.goto((process.env.DASH_URL || fs.readFileSync(new URL("../status/.dashboard-url", import.meta.url), "utf8").trim()) + "/");
await page.waitForTimeout(2500);
await page.screenshot({ path: process.argv[2], fullPage: false });
await browser.close();
