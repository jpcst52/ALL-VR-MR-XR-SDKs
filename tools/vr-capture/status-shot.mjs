import { chromium } from "playwright";
const browser = await chromium.launch({ channel: "chrome" });
const page = await browser.newPage({ viewport: { width: 1400, height: 1500 } });
await page.goto("http://localhost:5210/");
await page.waitForTimeout(2500);
await page.screenshot({ path: process.argv[2], fullPage: false });
await browser.close();
