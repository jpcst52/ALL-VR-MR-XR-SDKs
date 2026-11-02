// Slides shared by more than one deck: Unity projects, launcher, demo day, research, dashboard, hydrotest.
// Every slide states what was verified. Screenshots are checked for existence before use.
import { existsSync } from "node:fs";
import { bullets, callout, card, contentSlide, entries, img, label, shot, steps, text } from "./lib.mjs";

async function maybeShot(slide, C, relative, opts) {
  const file = img(relative);
  if (!existsSync(file)) {
    console.warn(`  skipped missing screenshot: ${relative}`);
    return;
  }
  await shot(slide, C, file, opts);
}

export async function viveProjectsSlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "Four Unity training projects for Vive Pro 2 / SteamVR" });
  entries(slide, C, [
    ["01  Hydrostatic Test Bay", "Fit blind flanges, vent and fill, raise to 150 psi, hold from behind the line, drain."],
    ["02  Refinery Rounds", "Gas alarm, flange weep, muster point. Live weather sets the wind (Open-Meteo)."],
    ["03  Warehouse and forklift", "PPE, convex mirror, eye contact, striped crossing, near-miss feedback."],
    ["04  Resistor bench", "ESD strap, color code, Ohm's law, E24 value, derating. Generic, inspired by component makers."],
  ], { x: 0.6, y: 1.75, w: 4.9, h: 3.9, nameColor: C.accent1, nameSize: 15, descSize: 12.5, gap: 10, name: "Unity projects" });
  callout(slide, C, "Verified: compiles and plays in the Unity 6000.3.20f1 editor, desktop mode. Not tested on a Vive Pro 2 or any headset.", { x: 0.6, y: 5.75, w: 4.9, h: 1.0, color: C.accent3, name: "Unity honesty" });
  const cap = "Unity editor Game view (desktop, not a headset)";
  await maybeShot(slide, C, "docs/images/hub-play.png", { x: 5.75, y: 1.75, w: 3.45, h: 2.4, caption: "01 in the hub project. " + "Desktop editor", name: "Hub screenshot" });
  await maybeShot(slide, C, "vive-steamvr/02-refinery-rounds/screenshots/01-desktop-editor-view.png", { x: 9.3, y: 1.75, w: 3.45, h: 2.4, caption: "02 Refinery Rounds. Desktop editor", name: "Refinery screenshot" });
  await maybeShot(slide, C, "vive-steamvr/03-warehouse-forklift/screenshots/01-desktop-editor-view.png", { x: 5.75, y: 4.3, w: 3.45, h: 2.4, caption: "03 Warehouse. Desktop editor", name: "Warehouse screenshot" });
  await maybeShot(slide, C, "vive-steamvr/04-resistor-bench/screenshots/01-desktop-editor-view.png", { x: 9.3, y: 4.3, w: 3.45, h: 2.4, caption: "04 Resistor bench. Desktop editor", name: "Resistor screenshot" });
  slide.addNotes(`${cap}. Projects 02 to 04 were not played through to the end. General safety-awareness practice, not a procedure or certification.`);
}

export async function viveSetupSlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "Open a project, set up, press Play" });
  steps(slide, C, [
    "Open vive-steamvr/0N-... in Unity Hub with Unity 6000.3.20f1. The shared kit loads from a file: path.",
    "Run the menu Pitt-Bradford > Set up project. It configures OpenXR and the Input System and builds the scene.",
    "For a headset: set SteamVR as the OpenXR runtime, start SteamVR, press Play. With no headset it runs in desktop mode.",
    "Optional: Pitt-Bradford > Build Windows player.",
  ], { x: 0.6, y: 1.85, w: 6.2, gap: 0.98, size: 14 });
  card(slide, C, { x: 7.1, y: 1.75, w: 5.65, h: 2.55, name: "Controls card" });
  label(slide, C, "CONTROLS: WANDS  /  DESKTOP", { x: 7.35, y: 1.95, w: 5.2, color: C.accent1, name: "Controls label" });
  bullets(slide, C, ["Move: left stick  /  W A S D", "Look: head, snap-turn  /  hold right mouse", "Press: point and trigger  /  crosshair and click", "Grab: grip  /  look at it and press E"], { x: 7.35, y: 2.35, w: 5.2, h: 1.9, size: 13, gap: 4, name: "Controls" });
  card(slide, C, { x: 7.1, y: 4.45, w: 5.65, h: 2.3, name: "Hub card" });
  label(slide, C, "HUB PROJECT (WORK IN PROGRESS)", { x: 7.35, y: 4.65, w: 5.2, color: C.accent2, name: "Hub label" });
  text(slide, "Dresses the Hydrostatic Test Bay with Asset Store art that stays local and is not committed. The lobby with portals is not started; lighting and audio are untuned. Desktop editor screenshots only.", { x: 7.35, y: 5.0, w: 5.2, h: 1.65, fontSize: 13, color: C.background1, objectName: "Hub text" });
}

export async function launcherSlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "One launcher page for every example" });
  await maybeShot(slide, C, "docs/images/launcher.png", { x: 0.6, y: 1.75, w: 7.0, h: 4.95, caption: "Launcher, wide layout (top of page)", name: "Launcher screenshot", crop: { left: 0, top: 0, width: 1600, height: 1060 } });
  await maybeShot(slide, C, "docs/images/launcher-narrow.png", { x: 7.85, y: 1.75, w: 2.2, h: 4.95, caption: "Narrow (phone)", name: "Launcher narrow screenshot", crop: { left: 0, top: 0, width: 420, height: 1000 } });
  bullets(slide, C, [
    "Tiles for browser demos, multiplayer, Vive/SteamVR, and the Practice Lab, built from launcher/examples.json.",
    "Open it from the relay (port 8443) so the multiplayer links reach it.",
    "Runs offline: libraries are vendored.",
    "Desktop and phone layouts checked in a browser; not opened in a headset.",
  ], { x: 10.2, y: 1.8, w: 2.6, h: 4.9, size: 12.5, gap: 8, name: "Launcher points" });
}

export async function demoDaySlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "Demo day: one command starts everything over HTTPS" });
  card(slide, C, { x: 0.6, y: 1.75, w: 6.0, h: 4.95, name: "Start card" });
  label(slide, C, "DOUBLE-CLICK  scripts\\start-demo.cmd", { x: 0.85, y: 1.95, w: 5.5, color: C.accent1, name: "Start label" });
  steps(slide, C, [
    "Checks Node.js; first run does npm install (needs internet once).",
    "Starts one server on port 8443 over HTTPS: launcher, multiplayer pages, Practice Lab, and the relay.",
    "Makes a self-signed certificate on first run (cached, never committed).",
    "Prints every URL and opens the launcher on the PC.",
  ], { x: 0.85, y: 2.5, w: 5.5, gap: 0.98, size: 13 });
  card(slide, C, { x: 6.85, y: 1.75, w: 5.9, h: 2.6, name: "Who card" });
  label(slide, C, "WHO OPENS WHAT", { x: 7.1, y: 1.95, w: 5.4, color: C.accent2, name: "Who label" });
  bullets(slide, C, ["Quest Browser: https://<lab-PC>:8443/ (accept the certificate warning once)", "QR codes: /multi-player/qr.html on the PC", "Vive Pro 2 PC: desktop browser or the Unity project", "Everyone in an activity uses the same room name"], { x: 7.1, y: 2.35, w: 5.4, h: 1.95, size: 12.5, gap: 4, name: "Who list" });
  text(slide, "Kit: CHECKLIST, QUEST-SETUP, VIVE-SETUP, BACKUP-PLAN, TROUBLESHOOTING, and a 15-minute TALK-TRACK in docs/demo-day/.", { x: 6.85, y: 4.5, w: 5.9, h: 0.75, fontSize: 13, color: C.background2, objectName: "Kit list" });
  callout(slide, C, "Run on a development PC (Node 22, Windows 11). No Quest or Vive used in any test: every hardware step is verify at rehearsal.", { x: 6.85, y: 5.4, w: 5.9, h: 1.3, color: C.accent3, name: "Demo honesty" });
}

export async function researchSlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "Research basis: 36 references, checked against records" });
  const cols = [
    ["WHAT THE REVIEWS SUGGEST", C.accent1, [
      "Kaplan et al. (2021): XR training performs about as well as common methods overall, with a likely edge where danger or cost limits conventional training.",
      "Makransky et al. (2019): in one study of 52 students, immersive VR raised presence but lowered learning scores.",
      "Scorgie et al. (2024) and Stefan et al. (2023): retention and behavior-level outcomes are rarely measured.",
    ]],
    ["THREE DRAFT STUDY DESIGNS", C.accent2, [
      "A: hands-on 3D tasks versus choose-a-response.",
      "B: low versus high visual realism, same scenario.",
      "C: desktop, Vive, and Quest comparison.",
      "Hypotheses, not results. No sample sizes proposed. Needs IRB review before any data collection.",
    ]],
  ];
  cols.forEach(([head, color, items], i) => {
    const x = 0.6 + i * 6.15;
    card(slide, C, { x, y: 1.75, w: 6.0, h: 4.0, name: `Research card ${i + 1}` });
    label(slide, C, head, { x: x + 0.25, y: 1.95, w: 5.5, color, name: `Research label ${i + 1}` });
    bullets(slide, C, items, { x: x + 0.25, y: 2.35, w: 5.5, h: 3.3, size: 13, gap: 8, name: `Research points ${i + 1}` });
  });
  callout(slide, C, "A working bibliography, not a literature review. Records were checked (mostly Crossref); several entries are flagged to confirm. Read the full text before citing.", { x: 0.6, y: 5.95, w: 12.15, h: 0.8, color: C.accent3, name: "Research honesty" });
}

export async function dashboardSlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "A local status dashboard keeps the project honest" });
  await maybeShot(slide, C, "docs/images/status-dashboard.png", { x: 0.6, y: 1.75, w: 5.6, h: 4.95, caption: "Status dashboard, light theme", name: "Dashboard screenshot" });
  entries(slide, C, [
    ["Progress and what needs you", "Rings per section of docs/Task-Status.md; tasks waiting on the owner, partners, or faculty."],
    ["Agents and test health", "Which helpers are running and the last recorded result of each test suite."],
    ["Servers and repos", "Whether the demo servers answer, uncommitted files, recent commits."],
  ], { x: 6.5, y: 1.8, w: 6.25, h: 3.2, nameColor: C.accent1, nameSize: 15, descSize: 13, gap: 10, name: "Dashboard panels" });
  callout(slide, C, "Start: npm run status. Node.js only, bound to 127.0.0.1, no cloud, no database.", { x: 6.5, y: 5.25, w: 6.25, h: 0.8, color: C.accent2, name: "Dashboard start" });
}

export async function hydrotestSlide(pres, C, { section, eyebrow }) {
  const slide = contentSlide(pres, { section, eyebrow, title: "Third theme: build a hydrostatic test rig together" });
  const shots = [
    ["multi-player/screenshots/hydrotest-empty.png", "Empty room"],
    ["multi-player/screenshots/hydrotest-rig.png", "Completed rig"],
    ["multi-player/screenshots/hydrotest-rig-client2.png", "Second client"],
  ];
  for (const [i, [file, caption]] of shots.entries()) {
    await maybeShot(slide, C, file, { x: 0.6 + i * 4.1, y: 1.75, w: 3.95, h: 2.75, caption: caption + " (desktop browser)", name: `Hydrotest screenshot ${i + 1}` });
  }
  card(slide, C, { x: 0.6, y: 4.7, w: 12.15, h: 2.05, name: "Hydrotest card" });
  bullets(slide, C, [
    "Page: /multi-player/hydrotest/. Tank, pump, valve, vent, gauge, exchanger, blind flanges, pipe; touch port dots to connect.",
    "Same one-holder-per-part rule and separate rooms as the other themes. The web counterpart of the Unity Hydrostatic Test Bay.",
    "Desktop and emulator only; not tried on a real headset. General safety awareness, inspired by heat-exchanger makers, generic parts.",
  ], { x: 0.9, y: 4.85, w: 11.6, h: 1.8, size: 13, gap: 6, name: "Hydrotest points" });
}

export async function verifiedSlide(pres, C, { section, eyebrow, title = "What is verified, and what is not" }) {
  const slide = contentSlide(pres, { section, eyebrow, title });
  card(slide, C, { x: 0.6, y: 1.75, w: 6.0, h: 4.1, name: "Verified card" });
  label(slide, C, "RUN AND CHECKED", { x: 0.85, y: 1.95, w: 5.4, color: C.accent5, name: "Verified label" });
  bullets(slide, C, [
    "Browser demos and the Practice Lab (16 scenarios) on a desktop browser; automated scenario tests pass",
    "Multiplayer relay with automated tests; two-client rooms",
    "Unity projects 01 to 04 compile and play in the editor, desktop mode",
    "Emulated Quest 3 captures (Meta IWER in Chrome)",
    "HTTPS start script, certificates, and QR page on a development PC",
  ], { x: 0.85, y: 2.35, w: 5.5, h: 3.4, size: 13, gap: 6, name: "Verified list" });
  card(slide, C, { x: 6.75, y: 1.75, w: 6.0, h: 4.1, name: "Unverified card" });
  label(slide, C, "NOT VERIFIED", { x: 7.0, y: 1.95, w: 5.4, color: C.accent3, name: "Unverified label" });
  bullets(slide, C, [
    "Anything on a real Quest or Vive Pro 2, including controllers and comfort",
    "Unity projects 02 to 04 played to the end",
    "Practice Lab VR captures for the four newest scenarios",
    "Learning outcomes: no study has been run",
    "Safety content against any real site procedure",
  ], { x: 7.0, y: 2.35, w: 5.5, h: 3.4, size: 13, gap: 6, name: "Unverified list" });
  callout(slide, C, "Scenarios are general safety awareness, not a procedure or certification. Company names appear only as inspired by.", { x: 0.6, y: 6.05, w: 12.15, h: 0.7, color: C.accent4, name: "Scope callout" });
}
