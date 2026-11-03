// Keyboard-only run of one Practice Lab scenario: no mouse. Checks where keyboard focus goes after each change.
// Usage: A11Y_BASE=http://localhost:8171/ node tools/vr-capture/lab-a11y.mjs [scenario id]
import { chromium } from "playwright";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const base = (process.env.A11Y_BASE ?? "http://localhost:8171/").replace(/\/?$/, "/");
const id = process.argv[2] ?? "electrical-work";
const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);
const scenario = scenarios.find((s) => s.id === id);
const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });
const page = await (await browser.newContext({ viewport: { width: 900, height: 600 }, reducedMotion: "reduce" })).newPage();
await page.route("**/*", (r) => (new URL(r.request().url()).hostname === "localhost" ? r.continue() : r.abort()));
const problems = [];
const where = () => page.evaluate(() => { const a = document.activeElement; return a === document.body ? "BODY" : `${a.tagName.toLowerCase()}#${a.id || ""}${a.dataset?.hotspot ? "[" + a.dataset.hotspot + "]" : ""} "${(a.textContent || "").trim().slice(0, 30)}"`; });
const expect = async (label, test) => { const w = await where(); const ok = test(w); console.log(ok ? "ok  " : "FAIL", label.padEnd(44), w); if (!ok) problems.push(label); };
await page.goto(`${base}training-simulations/`);
await page.waitForTimeout(1200);
await page.focus("#learner-name");
await page.keyboard.type("Alex");
await page.keyboard.press("Enter");
await page.waitForSelector("#catalog:not([hidden])");
await page.focus(`[data-scenario="${id}"]`);
await page.keyboard.press("Enter");
await page.waitForTimeout(500);
await expect("after opening a scenario: focus on its title", (w) => w.startsWith("h2#simulation-title"));
for (const [n, step] of scenario.steps.entries()) {
  // Tab to the disclosure, open it with the keyboard, then press each required action in order.
  await page.focus("#task-actions-wrap summary");
  await page.keyboard.press("Enter");
  const required = [];
  const done = new Set();
  while (done.size < step.hotspots.filter((h) => h.correct).length) {
    const next = step.hotspots.find((h) => h.correct && !done.has(h.id) && (h.after ?? []).every((a) => done.has(a)));
    required.push(next.id);
    done.add(next.id);
  }
  for (const hid of required) {
    await page.focus(`#task-actions [data-hotspot="${hid}"]`);
    await page.keyboard.press("Enter");
    if (hid !== required.at(-1)) await expect(`task ${n + 1}: after ${hid}, focus stays in the actions`, (w) => w !== "BODY");
  }
  await expect(`task ${n + 1} done: focus on Continue`, (w) => w.startsWith("button#continue"));
  await page.keyboard.press("Enter");
  await page.waitForTimeout(300);
  if (n < scenario.steps.length - 1) await expect(`next task ${n + 2}: focus on its prompt`, (w) => w.startsWith("h3#decision-prompt"));
}
if (scenario.rationale) {
  await page.focus('#choices [data-choice="0"]');
  await page.keyboard.press("Enter");
  await page.keyboard.press("Tab");
  await page.focus("#continue");
  await page.keyboard.press("Enter");
}
await page.waitForSelector("#completion:not([hidden])");
await expect("result screen: focus on its heading", (w) => w.startsWith("h3#completion-title"));
await page.focus("#finish");
await page.keyboard.press("Enter");
await expect("back at the catalog: focus on its heading", (w) => w.startsWith("h2"));
const labels = await page.evaluate(() => ({ canvasHidden: document.querySelector("#scene").getAttribute("aria-hidden"), lang: document.documentElement.lang, buttonsWithoutName: [...document.querySelectorAll("button")].filter((b) => !(b.textContent || b.getAttribute("aria-label") || "").trim()).length }));
console.log(labels);
console.log(problems.length ? `${problems.length} problem(s)` : "keyboard run clean");
await browser.close();
process.exit(problems.length ? 1 : 0);
