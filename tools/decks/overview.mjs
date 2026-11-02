import { dashboardSlide, demoDaySlide, forkliftDesignSlide, launcherSlide, researchFeaturesSlide, researchSlide, verifiedSlide, viveProjectsSlide, viveSetupSlide } from "./newslides.mjs";
import { applyDeckTheme, bullets, callout, card, chip, contentSlide, entries, img, label, newDeck, numberDot, section, shot, steps, text } from "./lib.mjs";

export async function buildOverview(file) {
  const { pres, C } = newDeck({ title: "VR, MR, and AR SDK examples: project overview", footer: "ALL-VR-MR-XR-SDKs  |  Project overview" });

  // 1 ---------------------------------------------------------------- title
  section(pres, "Introduction");
  {
    const slide = pres.addSlide({ masterName: "TITLE", sectionTitle: "Introduction" });
    slide.addText("PROJECT OVERVIEW  /  2026", { placeholder: "eyebrow" });
    slide.addText("VR · MR · AR\nSDK examples", { placeholder: "title" });
    text(slide, "A practical tour of immersive technology through small, focused examples: ten SDK and framework demos, three multiplayer WebXR themes, four Unity training projects, and a hands-on training lab.", { x: 0.7, y: 3.8, w: 6.2, h: 1.3, fontSize: 18, color: C.background2, objectName: "Subtitle" });
    chip(slide, C, "13 examples", { x: 0.7, y: 5.55, w: 1.7, name: "Chip examples" });
    chip(slide, C, "16 hands-on scenarios", { x: 2.55, y: 5.55, w: 2.5, color: C.accent3, name: "Chip scenarios" });
    chip(slide, C, "VR emulator captures", { x: 5.2, y: 5.55, w: 2.3, color: C.accent2, name: "Chip VR" });
    await shot(slide, C, img("training-simulations/screenshots/vr/03-hands-on-task.png"), { x: 7.9, y: 0.6, w: 4.85, h: 3.2, caption: "Practice Lab task in an emulated Quest 3 session", name: "Title VR task" });
    await shot(slide, C, img("multi-player/screenshots/vr/01-circuit-lab-in-vr.png"), { x: 7.9, y: 3.95, w: 4.85, h: 3.2, caption: "Multiplayer Circuit Lab in an emulated VR session", name: "Title VR circuit" });
  }

  // 2 ---------------------------------------------------------------- what it is
  section(pres, "The project");
  {
    const slide = contentSlide(pres, { section: "The project", eyebrow: "01 / PROJECT IN A SENTENCE", title: "A learning gallery, not a single XR product" });
    text(slide, "One repository. Separate examples. Each demonstrates a distinctive capability.", { x: 0.6, y: 1.8, w: 5.4, h: 1.5, fontSize: 24, bold: true, color: C.background1, objectName: "Statement" });
    text(slide, "The aim is to make the XR landscape tangible: compare standards, engines, and device SDKs by seeing what each is built to do, then learn by building and by practicing with the training lab.", { x: 0.6, y: 3.45, w: 5.4, h: 1.5, fontSize: 15, color: C.background2, objectName: "Aim" });
    label(slide, C, "DESIGN PRINCIPLE", { x: 0.6, y: 5.3, w: 4, color: C.accent4 });
    text(slide, "A small, credible slice beats a broad claim.", { x: 0.6, y: 5.62, w: 5.4, h: 0.9, fontSize: 18, bold: true, color: C.background1, objectName: "Principle" });
    const rows = [
      ["Learn by doing", "Small, focused interactions instead of broad product demos.", C.accent1],
      ["Use the real stack", "Each sample uses its named SDK or framework directly.", C.accent2],
      ["Keep boundaries clear", "Web demos are not presented as native vendor SDK integrations.", C.accent3],
      ["Show it running", "Browser examples include screenshots; VR views come from an emulated headset and are labelled as such.", C.accent4],
    ];
    rows.forEach(([head, body, color], i) => {
      const y = 1.75 + i * 1.28;
      card(slide, C, { x: 6.5, y, w: 6.25, h: 1.12, name: `Principle card ${i + 1}` });
      numberDot(slide, C, i + 1, { x: 6.75, y: y + 0.35, color });
      entries(slide, C, [[head, body]], { x: 7.4, y: y + 0.16, w: 5.2, h: 0.85, nameSize: 15, descSize: 12.5, gap: 0, name: `Principle ${i + 1}` });
    });
  }

  // 3 ---------------------------------------------------------------- portfolio
  {
    const slide = contentSlide(pres, { section: "The project", eyebrow: "02 / THE PORTFOLIO", title: "Thirteen examples across five pathways" });
    const columns = [
      ["RUNTIMES & INPUT", C.accent1, [["OpenXR · Unity", "Instance lifecycle callback"], ["Meta XR · Quest", "Passthrough camera preview"], ["SteamVR · Unity", "Action-driven color toggle"]]],
      ["WEB 3D & IMMERSION", C.accent2, [["WebXR", "Immersive color field"], ["A-Frame", "Animated Orbit Garden"], ["Three.js", "Prism Bloom + VR entry"], ["Babylon.js", "Luminous Totem + XR"]]],
      ["MOBILE & SPATIAL", C.accent3, [["Google ARCore", "Plane detection + cube placement"], ["Apple · visionOS", "SwiftUI spatial atlas"]]],
      ["ENGINE TOOLING", C.accent4, [["Unity XRI", "Grab selection color feedback"]]],
      ["MULTIPLAYER & TRAINING", C.accent5, [["Circuit Lab", "Shared VR circuit builder"], ["Site Safety Layout", "Shared AR safety plan"], ["Practice Lab", "16 hands-on scenarios"]]],
    ];
    columns.forEach(([head, color, items], i) => {
      const x = 0.6 + i * 2.45;
      card(slide, C, { x, y: 1.75, w: 2.3, h: 4.45, name: `Pathway card ${i + 1}` });
      label(slide, C, head, { x: x + 0.2, y: 1.95, w: 2, color, name: `Pathway ${i + 1}` });
      entries(slide, C, items, { x: x + 0.2, y: 2.4, w: 1.95, h: 3.7, nameSize: 14, descSize: 12, gap: 10, name: `Pathway ${i + 1} examples` });
    });
    text(slide, "These are starter-scale demonstrations, not a feature-complete comparison or a unified runtime.", { x: 0.6, y: 6.4, w: 12, h: 0.35, fontSize: 12, color: C.background2, objectName: "Portfolio note" });
  }

  // 4 ---------------------------------------------------------------- web lane
  section(pres, "Browser XR");
  {
    const slide = contentSlide(pres, { section: "Browser XR", eyebrow: "03 / BROWSER-BASED XR", title: "The web lane: one API, four ways to build" });
    card(slide, C, { x: 0.6, y: 1.75, w: 4.1, h: 4.4, name: "Web lane card" });
    label(slide, C, "WEBXR DEVICE API", { x: 0.85, y: 1.95, w: 3.5, name: "WebXR label" });
    text(slide, "The browser interface for requesting immersive sessions. Support varies by browser, device, and runtime, and it needs a secure context (localhost or HTTPS).", { x: 0.85, y: 2.3, w: 3.6, h: 1.5, fontSize: 13, color: C.background1, objectName: "WebXR text" });
    entries(slide, C, [["A-Frame", "Declarative, HTML-like scenes."], ["Three.js", "Custom 3D scenes and rendering."], ["Babylon.js", "Full web 3D engine and XR UI."]], { x: 0.85, y: 3.9, w: 3.6, h: 2.1, nameColor: C.accent2, nameSize: 14, descSize: 12, gap: 6, name: "Web builders" });
    const shots = [
      ["screenshots/webxr-color-field.png", "WebXR · color field"], ["screenshots/a-frame-orbit-garden.png", "A-Frame · Orbit Garden"],
      ["screenshots/threejs-prism-bloom.png", "Three.js · Prism Bloom"], ["screenshots/babylonjs-luminous-totem.png", "Babylon.js · Luminous Totem"],
    ];
    for (const [i, [file, caption]] of shots.entries()) {
      await shot(slide, C, img(file), { x: 4.95 + (i % 2) * 4.0, y: 1.75 + Math.floor(i / 2) * 2.3, w: 3.85, h: 2.15, caption, name: `Web screenshot ${i + 1}` });
    }
    text(slide, "Desktop captures: the immersive session needs a supported headset or an emulator.", { x: 4.95, y: 6.4, w: 7.8, h: 0.35, fontSize: 12, color: C.background2, objectName: "Web caption" });
  }

  // 5 ---------------------------------------------------------------- native + engine
  section(pres, "Native and engine SDKs");
  {
    const slide = contentSlide(pres, { section: "Native and engine SDKs", eyebrow: "04 / DEVICE SDKS + UNITY", title: "Native and engine examples show distinct capabilities" });
    const rows = [
      ["OpenXR", "STANDARD", "Provider lifecycle callback", "Compatible runtime + Unity OpenXR package", C.accent1],
      ["Meta XR", "DEVICE SDK", "Quest camera stream in a UI preview", "Quest 3 / 3S + camera permission", C.accent2],
      ["SteamVR", "RUNTIME + INPUT", "Boolean action toggles object color", "SteamVR plugin + configured binding", C.accent3],
      ["Unity XRI", "INTERACTION TOOLKIT", "Grabbing an object changes its color", "XRI packages + interaction setup", C.accent4],
    ];
    rows.forEach(([name, tag, what, needs, color], i) => {
      const y = 1.75 + i * 1.12;
      card(slide, C, { x: 0.6, y, w: 12.15, h: 0.98, name: `Native row ${i + 1}` });
      text(slide, name, { x: 0.9, y, w: 2.2, h: 0.98, fontSize: 18, bold: true, color, valign: "middle", objectName: `Native name ${i + 1}` });
      text(slide, tag, { x: 3.1, y, w: 2.2, h: 0.98, fontSize: 11, bold: true, color: C.background2, valign: "middle", objectName: `Native tag ${i + 1}` });
      text(slide, what, { x: 5.3, y, w: 3.6, h: 0.98, fontSize: 14, color: C.background1, valign: "middle", objectName: `Native capability ${i + 1}` });
      text(slide, needs, { x: 9.0, y, w: 3.55, h: 0.98, fontSize: 12, color: C.background2, valign: "middle", objectName: `Native needs ${i + 1}` });
    });
    callout(slide, C, "OpenXR is a cross-vendor API standard; Meta XR adds Quest-specific features; XRI supplies interaction components.", { x: 0.6, y: 6.25, w: 12.15, name: "Native note" });
  }

  // 6 ---------------------------------------------------------------- mobile + spatial
  {
    const slide = contentSlide(pres, { section: "Native and engine SDKs", eyebrow: "05 / MOBILE + SPATIAL", title: "Mobile AR and spatial computing need different toolchains" });
    const two = [
      ["Google ARCore", "ANDROID  /  AUGMENTED REALITY", "Scan a surface. Tap to place a cyan cube.", ["Native Android app", "Plane detection + camera hit-test", "ARCore-supported phone or tablet", "Android Studio, SDK 35, JDK 17"], C.accent3, 0.6],
      ["Apple Spatial Atlas", "VISIONOS  /  IMMERSIVE SPACE", "Enter a world-anchored atlas of landmarks.", ["SwiftUI + RealityKit geometry", "Xcode 16+ and visionOS 2 SDK", "Build and test on macOS", "Windows cannot build it"], C.accent2, 6.8],
    ];
    two.forEach(([name, tag, line, points, color, x]) => {
      card(slide, C, { x, y: 1.75, w: 5.95, h: 4.5, name: `${name} card` });
      text(slide, name, { x: x + 0.3, y: 1.95, w: 5.3, h: 0.5, fontSize: 24, bold: true, color, objectName: `${name} title` });
      label(slide, C, tag, { x: x + 0.3, y: 2.5, w: 5.3, color: C.background2, name: `${name} tag` });
      text(slide, line, { x: x + 0.3, y: 2.95, w: 5.3, h: 0.9, fontSize: 18, bold: true, color: C.background1, objectName: `${name} line` });
      bullets(slide, C, points, { x: x + 0.3, y: 4.0, w: 5.3, h: 2.0, size: 14, name: `${name} points` });
    });
  }

  // 7 ---------------------------------------------------------------- multiplayer
  section(pres, "Multiplayer and training");
  {
    const slide = contentSlide(pres, { section: "Multiplayer and training", eyebrow: "06 / MULTIPLAYER WEBXR", title: "Three themes, one relay: build together in VR and AR" });
    bullets(slide, C, [
      "One small WebSocket relay shares parts, presence, and ownership. Rooms are separated by theme.",
      "Circuit Lab (VR): snap batteries, switches, bulbs, resistors, motors, and wires together. A closed loop lights the bulb and spins the motor.",
      "Site Safety Layout (AR): place exits, extinguishers, first aid, a muster point, hazard beacons, and barriers; a live checklist flags gaps.",
      "Only one person can hold a part; the server rejects competing grabs and frees parts when an owner leaves.",
      "Hydrostatic Test Rig (new): build a heat-exchanger test rig together. AR uses a two-point calibration, not a shared cloud anchor.",
    ], { x: 0.6, y: 1.8, w: 5.3, h: 4.9, size: 13, gap: 7, name: "Multiplayer points" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-vr.png"), { x: 6.2, y: 1.75, w: 6.55, h: 2.55, caption: "Circuit Lab · two participants, closed loop, bulb lit", name: "Circuit Lab screenshot" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-ar.png"), { x: 6.2, y: 4.4, w: 6.55, h: 2.55, caption: "Site Safety Layout · sample layout with a passing checklist", name: "Safety Layout screenshot" });
  }

  // 8 ---------------------------------------------------------------- practice lab
  {
    const slide = contentSlide(pres, { section: "Multiplayer and training", eyebrow: "07 / FIELDWORK XR PRACTICE LAB", title: "Sixteen scenarios, every one hands-on" });
    await shot(slide, C, img("training-simulations/screenshots/02-catalog.png"), { x: 0.6, y: 1.75, w: 3.9, h: 4.9, caption: "Scenario catalog", name: "Catalog screenshot" });
    await shot(slide, C, img("training-simulations/screenshots/03-scenario-7-work-at-height.png"), { x: 4.7, y: 1.75, w: 3.9, h: 4.9, caption: "Work at height · hands-on task", name: "Task screenshot", crop: { left: 0, top: 130, width: 620, height: 780 } });
    entries(slide, C, [
      ["3 business", "Welcome a walk-in, make a safe work area, check a delivery."],
      ["3 research", "Informed consent, sample custody, field observation."],
      ["6 industrial", "Work at height, control room, battery limits, window cleaning, welding, electrical isolation."],
      ["4 manufacturing quality", "Lighter-lot sampling, hydrostatic test, pipe-spool welding, resistor selection. Inspired by local manufacturers."],
    ], { x: 8.9, y: 1.8, w: 3.85, h: 3.4, nameSize: 15, descSize: 12, gap: 6, name: "Lab groups" });
    callout(slide, C, "You do each task by selecting hotspots in a 3D work area. Wrong actions show consequences.", { x: 8.9, y: 5.4, w: 3.85, h: 1.3, color: C.accent4, name: "Lab callout" });
  }

  section(pres, "Unity, demo day, and research");
  await viveProjectsSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "UNITY / VIVE PRO 2 + STEAMVR" });
  await viveSetupSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "UNITY / SETUP AND CONTROLS" });
  await launcherSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "PITT-BRADFORD LAUNCHER" });
  await demoDaySlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "DEMO DAY / NOV 13, 2026" });
  await researchSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "RESEARCH" });
  await forkliftDesignSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "DESIGN CHOICE / FORKLIFT" });
  await researchFeaturesSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "FEATURES FROM THE RESEARCH" });
  await dashboardSlide(pres, C, { section: "Unity, demo day, and research", eyebrow: "PROJECT TOOLING" });

  // 9 ---------------------------------------------------------------- VR emulator
  {
    const slide = contentSlide(pres, { section: "Multiplayer and training", eyebrow: "08 / SEE IT IN VR", title: "How it looks in a headset: emulated Quest 3 captures" });
    const vr = [
      ["training-simulations/screenshots/vr/01-catalog.png", "Scenario cards in VR"],
      ["training-simulations/screenshots/vr/04-selecting-hotspots.png", "Controller ray on a hotspot"],
      ["multi-player/screenshots/vr/01-circuit-lab-in-vr.png", "Circuit Lab table-height board"],
    ];
    for (const [i, [file, caption]] of vr.entries()) {
      await shot(slide, C, img(file), { x: 0.6 + i * 4.1, y: 1.75, w: 3.95, h: 2.9, caption, name: `VR screenshot ${i + 1}` });
    }
    card(slide, C, { x: 0.6, y: 4.85, w: 12.15, h: 1.85, name: "VR notes card" });
    bullets(slide, C, [
      "Made with Meta's IWER emulator in Chrome: a real immersive-vr session and a scripted controller. The tool is in tools/vr-capture.",
      "Good for layout, scale, readability, and interaction logic. Not a physical headset: no stereo, lens, controller model, or performance data.",
      "Only VR is emulated; AR screenshots remain desktop fallbacks. Verify on hardware before release.",
    ], { x: 0.9, y: 5.0, w: 11.6, h: 1.6, size: 14, gap: 6, name: "VR notes" });
  }

  // 10 --------------------------------------------------------------- getting started
  section(pres, "Getting started");
  {
    const slide = contentSlide(pres, { section: "Getting started", eyebrow: "09 / GETTING STARTED", title: "Start with the capability, then check the environment" });
    const four = [
      ["Choose", "Pick a target experience: browser, multiplayer, Unity headset, Android AR, or visionOS.", C.accent1],
      ["Check", "Read that folder's prerequisites, SDK versions, hardware, and account needs.", C.accent2],
      ["Set up", "Use the platform-native toolchain; Unity folders are source to copy into a compatible project.", C.accent3],
      ["Run", "Use the folder's launch instructions and verify the capability on supported hardware.", C.accent4],
    ];
    four.forEach(([head, body, color], i) => {
      const x = 0.6 + i * 3.08;
      card(slide, C, { x, y: 1.8, w: 2.95, h: 3.5, name: `Step card ${i + 1}` });
      numberDot(slide, C, i + 1, { x: x + 0.25, y: 2.05, color });
      text(slide, head, { x: x + 0.25, y: 2.7, w: 2.5, h: 0.5, fontSize: 20, bold: true, color: C.background1, objectName: `Step title ${i + 1}` });
      text(slide, body, { x: x + 0.25, y: 3.25, w: 2.5, h: 1.9, fontSize: 13.5, color: C.background2, objectName: `Step text ${i + 1}` });
    });
    callout(slide, C, "Before committing to a platform: verify current device support, vendor terms, and distribution requirements.", { x: 0.6, y: 5.65, w: 12.15, name: "Start note" });
  }

  await verifiedSlide(pres, C, { section: "Getting started", eyebrow: "VERIFIED STATUS" });

  // 11 --------------------------------------------------------------- reality check
  {
    const slide = contentSlide(pres, { section: "Getting started", eyebrow: "10 / WHAT TO KEEP IN MIND", title: "Reality check: compatibility, access, and project scope" });
    const four = [
      ["DEVICE + BROWSER", "XR features depend on compatible hardware, runtimes, OS, and browser versions.", C.accent1],
      ["SECURE CONTEXT", "WebXR immersive sessions need HTTPS or localhost. Headsets on a LAN need a trusted certificate.", C.accent2],
      ["BUILD ENVIRONMENT", "Android needs Android tooling and a supported device; Apple builds require macOS + Xcode.", C.accent3],
      ["ACCOUNT + TERMS", "Meta developer setup, Unity licensing, Steam publishing, or Apple distribution can add requirements.", C.accent4],
    ];
    four.forEach(([head, body, color], i) => {
      const x = 0.6 + (i % 2) * 6.15;
      const y = 1.8 + Math.floor(i / 2) * 1.85;
      card(slide, C, { x, y, w: 6.0, h: 1.7, name: `Reality card ${i + 1}` });
      label(slide, C, head, { x: x + 0.3, y: y + 0.22, w: 5, color, name: `Reality label ${i + 1}` });
      text(slide, body, { x: x + 0.3, y: y + 0.62, w: 5.4, h: 0.95, fontSize: 14, color: C.background1, objectName: `Reality text ${i + 1}` });
    });
    callout(slide, C, "The Practice Lab is a non-certified learning prototype. It does not replace site procedures, permits, or professional training.", { x: 0.6, y: 5.65, w: 12.15, color: C.accent3, name: "Scope note" });
  }

  // 12 --------------------------------------------------------------- takeaway
  {
    const slide = contentSlide(pres, { section: "Getting started", eyebrow: "THE TAKEAWAY", title: "A map of XR, best explored one capability at a time" });
    const three = [["Compare", "See how standards, engines, and device SDKs differ.", C.accent1], ["Prototype", "Run the smallest example that proves the interaction.", C.accent2], ["Expand", "Add platform-specific depth only when the use case needs it.", C.accent3]];
    three.forEach(([head, body, color], i) => {
      const x = 0.6 + i * 4.1;
      card(slide, C, { x, y: 1.95, w: 3.95, h: 2.6, name: `Takeaway card ${i + 1}` });
      numberDot(slide, C, i + 1, { x: x + 0.3, y: 2.2, color });
      text(slide, head, { x: x + 0.3, y: 2.85, w: 3.4, h: 0.5, fontSize: 22, bold: true, color: C.background1, objectName: `Takeaway title ${i + 1}` });
      text(slide, body, { x: x + 0.3, y: 3.4, w: 3.4, h: 1.0, fontSize: 14, color: C.background2, objectName: `Takeaway text ${i + 1}` });
    });
    callout(slide, C, "Explore: README.md  ·  SDK-list.md  ·  each example folder's README.md  ·  tools/vr-capture", { x: 0.6, y: 5.0, w: 12.15, name: "Explore note" });
  }

  await pres.writeFile({ fileName: file });
  await applyDeckTheme(file);
}
