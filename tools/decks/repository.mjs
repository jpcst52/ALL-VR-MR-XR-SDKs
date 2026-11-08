// Repository guide deck: one tour of everything in the repository, folder by folder.
// Reuses the shared slides in newslides.mjs and adds the repository map, per-area slides, tests, tools and docs.
// Every slide states what was verified. Screenshots are checked for existence before use.
import { existsSync } from "node:fs";
import {
  avatarsSlide, dashboardSlide, demoDaySlide, hubSlide, launcherSlide, nextStepsSlide, realismSlide, researchSlide,
  systemSlide, verifiedSlide, viveProjectsSlide, viveSetupSlide,
} from "./newslides.mjs";
import { applyDeckTheme, bullets, callout, card, chip, contentSlide, entries, img, label, newDeck, section, shot, text } from "./lib.mjs";

async function maybeShot(slide, C, relative, opts) {
  if (!existsSync(img(relative))) {
    console.warn(`  skipped missing screenshot: ${relative}`);
    return;
  }
  await shot(slide, C, img(relative), opts);
}

export async function buildRepository(file) {
  const { pres, C } = newDeck({ title: "XR training repository guide", footer: "ALL-VR-MR-XR-SDKs  |  Repository guide" });

  // 1 ---- title
  section(pres, "Introduction");
  {
    const slide = pres.addSlide({ masterName: "TITLE", sectionTitle: "Introduction" });
    slide.addText("REPOSITORY GUIDE  /  2026", { placeholder: "eyebrow" });
    slide.addText("XR training\nrepository", { placeholder: "title" });
    text(slide, "A tour of every folder: browser demos, multiplayer WebXR, a 17-scenario Practice Lab, four Unity projects and a lobby hub, native SDK samples, tools, tests, research and documentation.", { x: 0.7, y: 3.8, w: 6.2, h: 1.3, fontSize: 16, color: C.background2, objectName: "Title description" });
    chip(slide, C, "University of Pittsburgh at Bradford", { x: 0.7, y: 5.55, w: 3.6, name: "Chip owner" });
    chip(slide, C, "Demo: Nov 13, 2026", { x: 4.45, y: 5.55, w: 2.3, color: C.accent3, name: "Chip demo" });
    await maybeShot(slide, C, "docs/images/hub-r2-lobby-3-gallery-and-portal.png", { x: 7.9, y: 0.6, w: 4.85, h: 2.75, caption: "Hub lobby (desktop editor render, local assets)", name: "Title hub" });
    await maybeShot(slide, C, "training-simulations/screenshots/vr/03-hands-on-task.png", { x: 7.9, y: 3.5, w: 4.85, h: 3.2, caption: "Practice Lab task in an emulated Quest 3 session", name: "Title lab" });
  }

  // 2 ---- what it is
  section(pres, "The repository");
  await systemSlide(pres, C, { section: "The repository", eyebrow: "01 / THE SYSTEM" });

  // 3 ---- repository map
  {
    const slide = contentSlide(pres, { section: "The repository", eyebrow: "02 / REPOSITORY MAP", title: "What is in each folder" });
    const left = [
      ["training-simulations/", "Fieldwork XR Practice Lab: 17 short scenarios in the browser, optional WebXR view."],
      ["multi-player/", "WebSocket relay and the shared Circuit Lab, Site Safety Layout and hydrostatic test."],
      ["a-frame/  threejs/  babylonjs/  webxr/", "One small browser demo per library, each with its own README and screenshot."],
      ["brand-kit/  vendor/", "Web brand kit (bar, fonts, panels, sky) and offline copies of the libraries."],
      ["launcher/  index.html", "The main menu that opens every example and guide, offline."],
    ];
    const right = [
      ["vive-steamvr/", "Unity projects 01 to 04, the shared kit package, and the hub lobby project."],
      ["unity-xri/  openxr/  steamvr/  meta-xr/", "Native SDK samples, one per toolchain (source and notes)."],
      ["arcore-android/  apple-spatial/", "Mobile AR and visionOS samples; they need their own toolchains."],
      ["tools/  scripts/", "Capture, deck builder, status dashboard, docs check, demo launch scripts."],
      ["docs/  assets/", "Guides, research, demo-day packet, credits, models, skies and images."],
    ];
    entries(slide, C, left, { x: 0.6, y: 1.75, w: 6.0, h: 4.6, nameColor: C.accent1, nameSize: 14, descSize: 12.5, gap: 12, name: "Map left" });
    entries(slide, C, right, { x: 6.85, y: 1.75, w: 5.9, h: 4.6, nameColor: C.accent2, nameSize: 14, descSize: 12.5, gap: 12, name: "Map right" });
    callout(slide, C, "Every folder has its own README that says what was run and what was not. The index is docs/README.md.", { x: 0.6, y: 6.4, w: 12.15, h: 0.5, color: C.accent3, name: "Map note" });
  }

  // 4 ---- launcher
  await launcherSlide(pres, C, { section: "The repository", eyebrow: "03 / THE MAIN MENU" });

  // 5 ---- practice lab
  section(pres, "Browser");
  {
    const slide = contentSlide(pres, { section: "Browser", eyebrow: "04 / FIELDWORK XR PRACTICE LAB", title: "Seventeen short scenarios in the browser" });
    await maybeShot(slide, C, "training-simulations/screenshots/02-catalog.png", { x: 0.6, y: 1.75, w: 3.7, h: 4.9, caption: "Scenario catalogue", name: "Lab catalogue" });
    await maybeShot(slide, C, "training-simulations/screenshots/03-scenario-1-work-at-height.png", { x: 4.45, y: 1.75, w: 3.7, h: 4.9, caption: "Work at height, hands-on task", name: "Lab scenario" });
    bullets(slide, C, [
      "Five groups: everyday business, community and field research, high-risk industrial, manufacturing quality, and a draft life-science scenario.",
      "Click glowing points in a 3D scene to do each step. Every option has feedback; critical mistakes are flagged.",
      "'Why this matters' panels and a reasoning question are scored apart from the procedure.",
      "Optional WebXR view with a controller; checked in an emulated Quest 3, not on a headset.",
      "Props and people are CC0 models; the scene runs offline.",
    ], { x: 8.4, y: 1.8, w: 4.4, h: 4.9, size: 13, gap: 8, name: "Lab points" });
    slide.addNotes("Desktop and emulated Quest 3 captures. Scenarios are general safety awareness, not procedures. The manufacturing scenarios have not been reviewed by an industry expert; the Circulation Builder is a draft for faculty review.");
  }

  // 6 ---- multiplayer
  {
    const slide = contentSlide(pres, { section: "Browser", eyebrow: "05 / MULTIPLAYER WEBXR", title: "Shared rooms through a small relay" });
    bullets(slide, C, [
      "A Node WebSocket relay serves the pages and keeps each room's shared state (HTTPS on the lab PC, port 8443).",
      "Circuit Lab (VR): snap batteries, switches, bulbs and motors together with others in one room.",
      "Site Safety Layout (AR): place exits, extinguishers, first aid, muster point and barriers; a live checklist flags gaps.",
      "Hydrostatic test: a shared multi-user version of the Unity bay.",
      "Phones, desktop browsers and headsets join the same room; people see each other's head and hands.",
    ], { x: 0.6, y: 1.8, w: 5.5, h: 4.7, size: 14, gap: 10, name: "Multiplayer points" });
    await maybeShot(slide, C, "multi-player/screenshots/multiplayer-vr.png", { x: 6.3, y: 1.75, w: 6.45, h: 2.5, caption: "Circuit Lab, desktop view", name: "Multiplayer VR" });
    await maybeShot(slide, C, "multi-player/screenshots/multiplayer-ar.png", { x: 6.3, y: 4.35, w: 6.45, h: 2.5, caption: "Site Safety Layout with the sample layout", name: "Multiplayer AR" });
    slide.addNotes("Verified with 2 to 3 browser contexts on one PC (81 end-to-end checks) and 40 relay unit tests. A real two-device session has not been run.");
  }

  // 7 ---- single-SDK demos
  {
    const slide = contentSlide(pres, { section: "Browser", eyebrow: "06 / THE SINGLE-LIBRARY DEMOS", title: "Four libraries, one idea each" });
    const items = [
      ["a-frame/screenshots/a-frame-orbit-garden.png", "A-Frame: gaze-selectable lantern shapes in declarative HTML"],
      ["threejs/screenshots/threejs-prism-bloom.png", "Three.js: animated prism with orbiting lights and a VR entry button"],
      ["babylonjs/screenshots/babylonjs-luminous-totem.png", "Babylon.js: glowing totem with built-in beacon effects"],
      ["webxr/screenshots/webxr-color-field.png", "Raw WebXR Device API: a colour-changing immersive scene"],
    ];
    for (let i = 0; i < items.length; i++) {
      const [file, caption] = items[i];
      await maybeShot(slide, C, file, { x: 0.6 + (i % 2) * 6.15, y: 1.7 + Math.floor(i / 2) * 2.4, w: 6.0, h: 2.25, caption, name: `Demo ${i + 1}` });
    }
    text(slide, "Each has a desktop fallback, runs offline from vendor/, and a README with what was tested. The ENTER VR button works only with a supported headset or an emulator.", { x: 0.6, y: 6.55, w: 12.15, h: 0.4, fontSize: 12, color: C.background2, objectName: "Demos note" });
  }

  // 8 ---- brand kit and offline
  {
    const slide = contentSlide(pres, { section: "Browser", eyebrow: "07 / BRAND KIT AND OFFLINE", title: "One look, no internet needed" });
    entries(slide, C, [
      ["Brand kit", "The brand bar, fonts, panels and sky shader for every page. League Gothic, Merriweather and Roboto are bundled; University of Pittsburgh palette."],
      ["Offline by design", "Three.js, Babylon.js, A-Frame and a QR library live in vendor/. An automated check loads every page with the network blocked."],
      ["Accessibility", "Keyboard control for the 3D tasks, 44 px targets, reduced-motion support, a list view of the same actions."],
      ["Checks", "36 Practice Lab tests, 16 brand-kit tests, 48 layout checks, and a test that no scene code draws bare text."],
    ], { x: 0.6, y: 1.8, w: 7.0, h: 4.6, nameColor: C.accent1, nameSize: 16, descSize: 13, gap: 14, name: "Brand points" });
    await maybeShot(slide, C, "training-simulations/screenshots/01-welcome.png", { x: 7.9, y: 1.75, w: 4.85, h: 4.9, caption: "Practice Lab welcome page", name: "Brand screenshot" });
  }

  // 9 ---- Unity: setup, projects, kit
  section(pres, "Unity");
  await viveProjectsSlide(pres, C, { section: "Unity", eyebrow: "08 / UNITY PROJECTS" });
  {
    const slide = contentSlide(pres, { section: "Unity", eyebrow: "09 / THE SHARED KIT", title: "One package behind all four projects" });
    entries(slide, C, [
      ["Signage and UI", "Rounded panels, brand plate, task board, 'why this matters' cues and a rationale question."],
      ["Bake and adopt", "Scenes are built in code, then saved as real GameObjects you can edit in the Hierarchy."],
      ["Procedural materials", "Concrete, steel, wood, paint, rubber and glass made in code, so no art files are needed."],
      ["Audio and effects", "Being added in round 3: a sound library (CC0 recordings plus synthesis) and cheap particle and lighting helpers."],
      ["Rig and input", "A desktop and OpenXR rig: wands or mouse and keyboard, same scenes."],
    ], { x: 0.6, y: 1.8, w: 6.6, h: 4.7, nameColor: C.accent1, nameSize: 15, descSize: 12.5, gap: 12, name: "Kit points" });
    callout(slide, C, "Projects 01 to 04 contain no Asset Store art, so they can be committed. Compiles in the Unity 6000.3.20f1 editor; nothing here has run on a headset.", { x: 7.5, y: 1.9, w: 5.25, h: 1.4, color: C.accent3, name: "Kit note" });
    await maybeShot(slide, C, "vive-steamvr/01-hydrostatic-test/screenshots/01-test-bay.png", { x: 7.5, y: 3.45, w: 5.25, h: 3.2, caption: "01 Hydrostatic Test Bay (desktop editor render)", name: "Kit screenshot" });
  }
  await viveSetupSlide(pres, C, { section: "Unity", eyebrow: "10 / OPEN A PROJECT" });
  await hubSlide(pres, C, { section: "Unity", eyebrow: "11 / THE HUB LOBBY" });
  await avatarsSlide(pres, C, { section: "Unity", eyebrow: "12 / PEOPLE IN THE SCENES" });
  await realismSlide(pres, C, { section: "Unity", eyebrow: "13 / REALISM AND ASSET RULES" });

  // credits
  {
    const slide = contentSlide(pres, { section: "Unity", eyebrow: "13b / CREDITS AND LICENCES", title: "Third-party material and its licences" });
    entries(slide, C, [
      ["Fonts", "League Gothic and Merriweather (SIL OFL 1.1), Roboto (Apache 2.0), bundled."],
      ["3D models, textures, skies", "CC0: Poly Haven, Quaternius, KayKit. Each file is listed with its source."],
      ["People in the hub scenes", "Microsoft Rocketbox avatars, MIT licence; imported locally, not committed."],
      ["Unity Asset Store packs", "Used only in the local hub project. Their licences forbid redistribution, so they are never committed."],
      ["Live weather", "Weather data by Open-Meteo.com, CC BY 4.0."],
      ["Sounds (being added, round 3)", "CC0 recordings plus our own synthesis; every file will be credited. Not finished yet."],
    ], { x: 0.6, y: 1.8, w: 7.6, h: 4.7, nameColor: C.accent1, nameSize: 15, descSize: 13, gap: 12, name: "Credits points" });
    callout(slide, C, "Everything is listed in CREDITS.md. Only CC0 or public-domain files are added to the repository.", { x: 8.5, y: 1.9, w: 4.25, h: 1.5, color: C.accent3, name: "Credits note" });
    callout(slide, C, "The University of Pittsburgh at Bradford logo is published with approval; the brand guidelines are not.", { x: 8.5, y: 3.6, w: 4.25, h: 1.5, color: C.accent2, name: "Brand note" });
  }

  // 10 ---- native samples
  section(pres, "Native SDK samples");
  {
    const slide = contentSlide(pres, { section: "Native SDK samples", eyebrow: "14 / NATIVE SDK SAMPLES", title: "Six samples, each from its own toolchain" });
    entries(slide, C, [
      ["unity-xri/", "Unity XR Interaction Toolkit: grab an object, change its colour."],
      ["openxr/", "Unity OpenXR: a small sample that reacts to the runtime."],
      ["steamvr/", "SteamVR plugin: a boolean action toggles a material."],
    ], { x: 0.6, y: 1.8, w: 5.9, h: 3.4, nameColor: C.accent1, nameSize: 15, descSize: 13, gap: 12, name: "Native left" });
    entries(slide, C, [
      ["meta-xr/", "Meta XR: passthrough camera stream in a Unity UI image."],
      ["arcore-android/", "Google ARCore: detect a floor plane and place a cube on tap."],
      ["apple-spatial/", "Apple Vision Pro: SwiftUI and RealityKit anchored content."],
    ], { x: 6.85, y: 1.8, w: 5.9, h: 3.4, nameColor: C.accent2, nameSize: 15, descSize: 13, gap: 12, name: "Native right" });
    callout(slide, C, "Source and setup notes only. None was built or run here: each needs its own toolchain (Android Studio, Xcode, or a Meta or SteamVR setup). The READMEs say so.", { x: 0.6, y: 5.5, w: 12.15, h: 0.9, color: C.accent3, name: "Native note" });
  }

  // 11 ---- tools, tests, verification
  section(pres, "Tools and tests");
  {
    const slide = contentSlide(pres, { section: "Tools and tests", eyebrow: "15 / TOOLS", title: "Tools that keep the repository honest" });
    entries(slide, C, [
      ["tools/vr-capture", "Playwright captures, including an emulated Quest 3 session; UI, offline, launcher and multiplayer checks."],
      ["tools/decks", "Rebuilds these PowerPoint decks from the READMEs and current screenshots."],
      ["tools/status", "A local live dashboard: task list, agents, test results, servers, uncommitted work."],
      ["tools/docs-check", "Fails on a broken link, a guide missing from the index, or a quoted test count that is out of date."],
    ], { x: 0.6, y: 1.8, w: 6.2, h: 4.6, nameColor: C.accent1, nameSize: 15, descSize: 13, gap: 14, name: "Tools points" });
    await maybeShot(slide, C, "docs/images/status-dashboard.png", { x: 7.1, y: 1.75, w: 5.65, h: 4.2, caption: "Status dashboard (npm run status)", name: "Dashboard screenshot" });
    slide.addNotes("The dashboard reads docs/Task-Status.md. Screenshots are desktop captures.");
  }
  {
    const slide = contentSlide(pres, { section: "Tools and tests", eyebrow: "16 / AUTOMATED CHECKS", title: "What the automated checks cover" });
    const rows = [
      ["Practice Lab unit tests", "36 pass"], ["Brand kit unit tests", "16 pass"], ["Multiplayer relay unit tests", "40 pass"],
      ["Web layout checks", "48 of 48"], ["Multiplayer end to end (2 to 3 browser contexts)", "81 pass"], ["Demo SDK correctness", "26 pass"],
      ["Demos check, offline check, launcher check, docs-check", "all pass"], ["Emulated Quest 3 demos", "4 of 4"],
    ];
    rows.forEach(([name, result], i) => {
      const y = 1.8 + i * 0.52;
      card(slide, C, { x: 0.6, y, w: 7.4, h: 0.44, name: `Check row ${i + 1}` });
      text(slide, name, { x: 0.8, y, w: 5.2, h: 0.44, fontSize: 13, color: C.background1, valign: "middle", objectName: `Check name ${i + 1}` });
      text(slide, result, { x: 6.0, y, w: 1.9, h: 0.44, fontSize: 13, bold: true, color: C.accent3, align: "right", valign: "middle", objectName: `Check result ${i + 1}` });
    });
    callout(slide, C, "All on a development PC: desktop Chrome, an emulated Quest 3, and the Unity editor. None on a headset.", { x: 8.3, y: 1.8, w: 4.45, h: 1.3, color: C.accent3, name: "Checks note" });
    text(slide, "Counts are checked by docs-check, so a slide or README that quotes an old number fails the build.", { x: 8.3, y: 3.3, w: 4.45, h: 1.2, fontSize: 13, color: C.background2, objectName: "Checks detail" });
  }
  await verifiedSlide(pres, C, { section: "Tools and tests", eyebrow: "17 / VERIFIED AND NOT", title: "What is verified, and what is not" });

  // 12 ---- research and docs
  section(pres, "Research and documentation");
  await researchSlide(pres, C, { section: "Research and documentation", eyebrow: "18 / RESEARCH" });
  {
    const slide = contentSlide(pres, { section: "Research and documentation", eyebrow: "19 / DOCUMENTATION", title: "Where to read what" });
    entries(slide, C, [
      ["docs/README.md", "The documentation index: every guide, by purpose."],
      ["HOW-TO-RUN, TESTING", "Run the demo, a project, the hub or a headset; the suites and hands-on check tables."],
      ["demo-day/", "Checklist, talk track, Vive and Quest setup, backup plan, troubleshooting."],
      ["research/", "Bibliography (re-checked against Crossref), study designs, content review."],
      ["GLOSSARY, CREDITS", "Plain terms for the advisory board; every third-party credit."],
      ["HOLOLENS-AR-NOTES", "Why content can look small on a HoloLens and how to fit it (untested there)."],
    ], { x: 0.6, y: 1.8, w: 12.15, h: 4.7, nameColor: C.accent1, nameSize: 15, descSize: 13, gap: 12, name: "Docs points" });
  }
  await dashboardSlide(pres, C, { section: "Research and documentation", eyebrow: "20 / STATUS DASHBOARD" });

  // 13 ---- demo day and next
  section(pres, "Demo day");
  await demoDaySlide(pres, C, { section: "Demo day", eyebrow: "21 / DEMO DAY, NOV 13" });
  {
    const slide = contentSlide(pres, { section: "Demo day", eyebrow: "22 / AR ON SMALL DISPLAYS", title: "HoloLens: why content looks small" });
    bullets(slide, C, [
      "The HoloLens 2 shows about 43 x 29 degrees, less than half a Quest's width.",
      "It is additive: dark colours are transparent, so dark scenes seem to shrink.",
      "Fit content to the real field of view, 1.3 m ahead, 0.8 m wide, bright colours.",
      "A two-minute headset measurement tells us which cause is real.",
    ], { x: 0.6, y: 1.8, w: 7.0, h: 3.6, size: 15, gap: 12, name: "HoloLens points" });
    callout(slide, C, "Tested: the field-of-view and fit maths in unit tests only. Not tested on a HoloLens.", { x: 0.6, y: 5.6, w: 7.0, h: 0.9, color: C.accent3, name: "HoloLens note" });
    card(slide, C, { x: 7.9, y: 1.8, w: 4.85, h: 4.7, name: "HoloLens card" });
    label(slide, C, "CAUSES, MOST LIKELY FIRST", { x: 8.15, y: 2.0, w: 4.3, color: C.accent1, name: "HoloLens causes" });
    bullets(slide, C, ["The display is small (not a bug)", "Dark colours turn transparent", "Content too wide or too far away", "Near clipping at arm length", "Frame rate on a mobile GPU"], { x: 8.15, y: 2.4, w: 4.3, h: 2.6, size: 13, gap: 8, name: "HoloLens cause list" });
    text(slide, "Read: docs/HOLOLENS-AR-NOTES.md. Code: multi-player/shared/ar-fit.mjs", { x: 8.15, y: 5.3, w: 4.3, h: 0.9, fontSize: 12, color: C.background2, objectName: "HoloLens path" });
  }
  await nextStepsSlide(pres, C, { section: "Demo day", eyebrow: "23 / NEXT STEPS" });

  await pres.writeFile({ fileName: file });
  await applyDeckTheme(file);
}
