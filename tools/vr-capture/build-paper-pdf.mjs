// Rebuilds docs/paper/paper.pdf from docs/paper/paper.html using headless Chrome (print CSS in the HTML sets Letter, 1 in margins).
// Run from tools/vr-capture:  node build-paper-pdf.mjs
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { chromium } from "playwright";
const here = dirname(fileURLToPath(import.meta.url));
const html = join(here, "..", "..", "docs", "paper", "paper.html");
const pdf = join(here, "..", "..", "docs", "paper", "paper.pdf");
const browser = await chromium.launch({ channel: "chrome" });
const page = await browser.newPage();
await page.goto(pathToFileURL(html).href, { waitUntil: "networkidle" });
await page.pdf({ path: pdf, preferCSSPageSize: true, printBackground: true });
await browser.close();
console.log("wrote", pdf);
