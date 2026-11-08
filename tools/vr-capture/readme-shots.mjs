// Re-shoots the desktop README screenshots of the Practice Lab and the multiplayer examples.
// The git-ignored logo (branding/) is always blocked here, so committed screenshots show the text wordmark only.
//
// Usage: serve the repository root from the relay on one origin, then
//   node tools/vr-capture/readme-shots.mjs          (SHOTS_BASE overrides http://localhost:8132/)
//   node tools/vr-capture/readme-shots.mjs lab      (only the Practice Lab)   |   ... mp   (only multiplayer)
//   node tools/vr-capture/readme-shots.mjs why      (only the "Why this matters" and reasoning-question shots, 09-12)
import { chromium } from "playwright";
import { join, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, "..", "..");
const base = (process.env.SHOTS_BASE ?? "http://localhost:8132/").replace(/\/?$/, "/");
const which = process.argv[2] ?? "all";
const labOut = join(repo, "training-simulations", "screenshots");
const mpOut = join(repo, "multi-player", "screenshots");
const { scenarios } = await import(pathToFileURL(join(repo, "training-simulations", "scenarios.js")).href);
const byId = (id) => scenarios.find((s) => s.id === id);

const browser = await chromium.launch({ channel: "chrome", args: ["--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist"] });

async function newPage(viewport) {
  const context = await browser.newContext({ viewport, deviceScaleFactor: 1, reducedMotion: "reduce" });
  const page = await context.newPage();
  await page.route("**/*", (route) => {
    const url = new URL(route.request().url());
    if (url.hostname !== "localhost" && url.hostname !== "127.0.0.1") return route.abort();
    if (url.pathname.startsWith("/branding/")) return route.fulfill({ status: 404, body: "" });
    return route.continue();
  });
  page.on("pageerror", (e) => console.log("PAGE ERROR", e.message));
  return page;
}

function order(step) {
  const required = step.hotspots.filter((h) => h.correct);
  const done = [];
  while (done.length < required.length) done.push(required.find((h) => !done.includes(h.id) && (h.after ?? []).every((id) => done.includes(id))).id);
  return done;
}

// ------------------------------------------------------------------------------------------- Practice Lab
async function lab(fn) {
  const page = await newPage({ width: 1280, height: 1100 });
  await page.goto(`${base}training-simulations/`);
  await page.waitForTimeout(1500);
  const api = {
    page,
    shot: async (name) => { await page.waitForTimeout(1600); await page.screenshot({ path: join(labOut, name) }); console.log("wrote", name); },
    name: async () => { await page.fill("#learner-name", "Alex"); await page.click("#profile button[type=submit]"); },
    start: async (id) => { await page.click(`[data-scenario="${id}"]`); await page.waitForTimeout(500); },
    hot: async (id) => {
      await page.evaluate(() => { document.querySelector("#task-actions-wrap").open = true; });
      await page.click(`#task-actions [data-hotspot="${id}"]`);
      await page.evaluate(() => { document.querySelector("#task-actions-wrap").open = false; });
    },
    top: () => page.evaluate(() => { document.querySelector("#panel").scrollTop = 0; }),
  };
  api.finishStep = async (step, extra = []) => { for (const id of extra) await api.hot(id); for (const id of order(step)) await api.hot(id); };
  api.toTask = async (scenario, index) => { for (const step of scenario.steps.slice(0, index)) { await api.finishStep(step); await page.click("#continue"); } };
  await fn(api);
  await page.context().close();
}

if (which === "all" || which === "lab") {
  await lab(async ({ shot }) => shot("01-welcome.png"));
  await lab(async ({ name, shot }) => { await name(); await shot("02-catalog.png"); });

  const taskFor = { "lighter-lot-sampling": 2, "heat-exchanger-hydrotest": 2, "pipe-spool-welding": 2, "resistor-selection": 1 };
  for (const [index, scenario] of scenarios.entries()) {
    const file = `03-scenario-${index + 1}-${scenario.title.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/-$/, "")}.png`;
    await lab(async ({ name, start, toTask, hot, top, shot }) => {
      await name();
      await start(scenario.id);
      await toTask(scenario, taskFor[scenario.id] ?? 0);
      const step = scenario.steps[taskFor[scenario.id] ?? 0];
      if (scenario.id === "work-at-height") for (const id of order(step).slice(0, 2)) await hot(id);
      if (scenario.id === "circulation-builder") for (const id of order(step).slice(0, 3)) await hot(id);
      await top();
      await shot(file);
    });
  }

  await lab(async ({ name, start, finishStep, top, shot }) => {
    await name(); await start("work-at-height"); await finishStep(byId("work-at-height").steps[0]); await top(); await shot("04-feedback.png");
  });
  await lab(async ({ page, name, start, toTask, top, shot }) => {
    const s = byId("work-at-height");
    await name(); await start(s.id); await toTask(s, s.steps.length - 1);
    await (async () => { for (const id of order(s.steps.at(-1))) { await page.evaluate(() => { document.querySelector("#task-actions-wrap").open = true; }); await page.click(`#task-actions [data-hotspot="${id}"]`); } })();
    await page.click("#continue");
    if (s.rationale) { await page.click(`#choices [data-choice="${s.rationale.options.findIndex((o) => o.best)}"]`); await page.click("#continue"); }
    await top(); await shot("05-complete.png");
  });
  await lab(async ({ name, start, finishStep, top, shot }) => {
    const step = byId("work-at-height").steps[0];
    const critical = step.hotspots.find((h) => h.critical && !h.correct);
    await name(); await start("work-at-height"); await finishStep(step, critical ? [critical.id] : []); await top(); await shot("06-critical-miss.png");
  });
  await lab(async ({ page, name, start, finishStep, top, shot }) => {
    const s = byId("electrical-work");
    await name(); await start(s.id);
    for (const [i, step] of s.steps.entries()) {
      const critical = i === 0 ? step.hotspots.find((h) => h.critical && !h.correct) : null;
      await finishStep(step, critical ? [critical.id] : []);
      await page.click("#continue");
    }
    if (s.rationale) { await page.click(`#choices [data-choice="${s.rationale.options.findIndex((o) => !o.best)}"]`); await page.click("#continue"); }
    await top(); await shot("07-industrial-result.png");
  });

  // Circulation Builder (draft for faculty review) README set
  const bio = byId("circulation-builder");
  await lab(async ({ page, name, shot }) => {
    await name();
    await page.evaluate(() => { const panel = document.querySelector("#panel"); const card = document.querySelector('[data-scenario="circulation-builder"]'); panel.scrollTop = card.closest(".category").offsetTop - 90; });
    await shot("08-circulation-catalog.png");
  });
  await lab(async ({ name, start, toTask, hot, top, shot }) => {
    const step = bio.steps[1];
    const early = step.hotspots.find((h) => h.correct && h.after);
    await name(); await start(bio.id); await toTask(bio, 1);
    if (early) await hot(early.id);
    for (const id of order(step).slice(0, 2)) await hot(id);
    await top(); await shot("08-circulation-task2-pulmonary.png");
  });
  await lab(async ({ name, start, toTask, finishStep, top, shot }) => {
    const step = bio.steps[2];
    const wrong = step.hotspots.find((h) => h.critical && !h.correct);
    await name(); await start(bio.id); await toTask(bio, 2); await finishStep(step, wrong ? [wrong.id] : []); await top(); await shot("08-circulation-misconception.png");
  });
  await lab(async ({ name, start, toTask, hot, top, shot }) => {
    await name(); await start(bio.id); await toTask(bio, 3);
    for (const id of order(bio.steps[3]).slice(0, 2)) await hot(id);
    await top(); await shot("08-circulation-task4-colouring.png");
  });
  await lab(async ({ page, name, start, finishStep, top, shot }) => {
    await name(); await start(bio.id);
    for (const [i, step] of bio.steps.entries()) {
      const wrong = i === 2 ? step.hotspots.find((h) => h.critical && !h.correct) : null;
      await finishStep(step, wrong ? [wrong.id] : []);
      await page.click("#continue");
    }
    await top(); await shot("08-circulation-result.png");
  });
}

// "Why this matters" panel and the separate reasoning question (09-11). Also part of `lab`; `why` runs only these.
if (which === "all" || which === "lab" || which === "why") {
  const hydro = byId("heat-exchanger-hydrotest");
  await lab(async ({ name, start, toTask, hot, top, shot }) => {
    await name(); await start(hydro.id); await toTask(hydro, 1);
    for (const id of order(hydro.steps[1]).slice(0, 1)) await hot(id);
    await top(); await shot("09-why-this-matters.png");
  });
  await lab(async ({ name, start, toTask, finishStep, page, top, shot }) => {
    await name(); await start(hydro.id); await toTask(hydro, 3); await finishStep(hydro.steps[3]); await page.click("#continue");
    await top(); await shot("10-reasoning-question.png");
    await page.click(`#choices [data-choice="${hydro.rationale.options.findIndex((o) => !o.best)}"]`);
    await shot("11-reasoning-feedback.png"); // the page scrolls the feedback into view; keep that scroll
    await page.click("#continue");
    await top(); await shot("12-result-with-reasoning.png");
  });
}

// ------------------------------------------------------------------------------------------- multiplayer
async function joinRoom(page, path, name, room) {
  await page.goto(`${base}${path}`);
  await page.waitForTimeout(800);
  await page.fill("#name", name);
  await page.fill("#room", room);
  await page.click("#join");
  await page.waitForSelector("#workspace:not([hidden])");
}

if (which === "all" || which === "mp") {
  const stamp = Date.now() % 100000;
  for (const [path, file, size, guest] of [
    ["multi-player/vr/", "multiplayer-vr.png", { width: 1280, height: 720 }, "Desktop Guest"],
    ["multi-player/ar/", "multiplayer-ar.png", { width: 1280, height: 720 }, "Site Lead"],
  ]) {
    const room = `readme-${stamp}-${file.length}`;
    const a = await newPage(size);
    const b = await newPage(size);
    await joinRoom(a, path, "Builder", room);
    await joinRoom(b, path, guest, room);
    await a.click("#load-demo");
    await a.waitForTimeout(2500);
    await a.screenshot({ path: join(mpOut, file) });
    console.log("wrote", file);
    await a.context().close();
    await b.context().close();
  }
  const room = `readme-hydro-${stamp}`;
  const a = await newPage({ width: 1280, height: 800 });
  await joinRoom(a, "multi-player/hydrotest/", "Alex", room);
  await a.waitForTimeout(1500);
  await a.screenshot({ path: join(mpOut, "hydrotest-empty.png") });
  const b = await newPage({ width: 1280, height: 800 });
  await joinRoom(b, "multi-player/hydrotest/", "Blair", room);
  await a.click("#load-demo");
  await a.waitForTimeout(2500);
  await a.screenshot({ path: join(mpOut, "hydrotest-rig.png") });
  await b.waitForTimeout(500);
  await b.screenshot({ path: join(mpOut, "hydrotest-rig-client2.png") });
  console.log("wrote hydrotest-empty.png, hydrotest-rig.png, hydrotest-rig-client2.png");
  await a.context().close();
  await b.context().close();
}

await browser.close();
