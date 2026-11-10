// Scenario-Walkthrough.pptx: step by step through the Unity hub (lobby) and each of its four training scenes,
// with numbered markers on the screenshots that show where each control is.
import sharp from "sharp";
import { applyDeckTheme, bullets, callout, card, chip, contentSlide, entries, img, label, newDeck, numberDot, section, shot, text } from "./lib.mjs";

const IM = "docs/images/";
const HY = "vive-steamvr/01-hydrostatic-test/screenshots/";
const RF = "vive-steamvr/02-refinery-rounds/screenshots/";
const WH = "vive-steamvr/03-warehouse-forklift/screenshots/";
const RS = "vive-steamvr/04-resistor-bench/screenshots/";

// A framed screenshot with numbered markers. marks: [px, py, n] in the picture's own pixels.
async function figure(slide, C, file, { x, y, w, h, caption, marks = [], name = "Screenshot", color }) {
  const buffer = await sharp(file).resize({ width: 1600, withoutEnlargement: true }).png({ compressionLevel: 9 }).toBuffer();
  const src = await sharp(file).metadata();
  const meta = await sharp(buffer).metadata();
  const captionH = caption ? 0.34 : 0;
  slide.addShape("roundRect", { x, y, w, h, rectRadius: 0.12, fill: { color: C.text2 }, line: { color: C.background2, width: 0.75, transparency: 75 }, objectName: `${name} frame` });
  const boxW = w - 0.2, boxH = h - 0.2 - captionH;
  const scale = Math.min(boxW / meta.width, boxH / meta.height);
  const iw = meta.width * scale, ih = meta.height * scale;
  const ix = x + (w - iw) / 2, iy = y + 0.1 + (boxH - ih) / 2;
  slide.addImage({ data: `image/png;base64,${buffer.toString("base64")}`, x: ix, y: iy, w: iw, h: ih, altText: caption ?? name, objectName: name });
  for (const [px, py, n, c] of marks) {
    const cx = ix + (px / src.width) * iw, cy = iy + (py / src.height) * ih;
    const ring = 0.46;
    slide.addShape("ellipse", { x: cx - ring / 2, y: cy - ring / 2, w: ring, h: ring, fill: { color: C.text1, transparency: 100 }, line: { color: c ?? color ?? C.accent1, width: 3 }, objectName: `Marker ring ${n}` });
    numberDot(slide, C, n, { x: cx + 0.12, y: cy - 0.56, color: c ?? color ?? C.accent1, name: `Marker ${n}` });
  }
  if (caption) text(slide, caption, { x: x + 0.15, y: y + h - captionH - 0.06, w: w - 0.3, h: captionH, fontSize: 10.5, color: C.background2, align: "center", valign: "middle", objectName: `${name} caption` });
}

// Numbered legend: items are [n, head, body]. Returns nothing; sized to fit the panel.
function legend(slide, C, items, { x, y, w, gap, color, headSize = 15, bodySize = 13 }) {
  items.forEach(([n, head, body], i) => {
    const top = y + i * gap;
    numberDot(slide, C, n, { x, y: top, color });
    text(slide, [
      { text: head, options: { bold: true, fontSize: headSize, color: C.background1, breakLine: true } },
      { text: body, options: { fontSize: bodySize, color: C.background2 } },
    ], { x: x + 0.6, y: top - 0.03, w: w - 0.6, h: gap - 0.08, objectName: `Legend ${n}` });
  });
}

// The standard step slide: annotated picture on the left, numbered actions on the right, a note at the bottom.
async function stepSlide(pres, C, o) {
  const slide = contentSlide(pres, { section: o.section, eyebrow: o.eyebrow, title: o.title, notes: o.notes });
  await figure(slide, C, img(o.file), { x: 0.6, y: 1.6, w: 7.55, h: 4.65, caption: o.caption, marks: o.marks, color: o.color, name: "Step screenshot" });
  card(slide, C, { x: 8.4, y: 1.6, w: 4.35, h: 4.65, name: "Steps card" });
  label(slide, C, o.panel ?? "DO THIS", { x: 8.7, y: 1.78, w: 3.9, color: o.color, name: "Steps label" });
  legend(slide, C, o.items, { x: 8.7, y: 2.2, w: 3.85, gap: o.gap ?? (3.95 / o.items.length), color: o.color, headSize: o.headSize, bodySize: o.bodySize });
  if (o.note) callout(slide, C, o.note, { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: o.noteColor ?? o.color, name: "Step note" });
  return slide;
}

// A scene opener: which portal, what the job is, the task board in short.
async function sceneOpener(pres, C, o) {
  section(pres, o.section);
  const slide = contentSlide(pres, { section: o.section, eyebrow: o.eyebrow, title: o.title, notes: o.notes });
  await shot(slide, C, img(o.file), { x: 0.6, y: 1.6, w: 6.6, h: 4.65, caption: o.caption, name: "Scene screenshot" });
  card(slide, C, { x: 7.45, y: 1.6, w: 5.3, h: 4.65, name: "Scene card" });
  chip(slide, C, o.portal, { x: 7.75, y: 1.82, w: 2.3, color: o.color, name: "Portal chip" });
  text(slide, o.goal, { x: 10.2, y: 1.82, w: 2.35, h: 0.42, fontSize: 12, color: C.background2, valign: "middle", objectName: "Scene time" });
  label(slide, C, "THE TASK BOARD (DO THEM IN THIS ORDER)", { x: 7.75, y: 2.45, w: 4.8, color: o.color, name: "Board label" });
  o.board.forEach((line, i) => {
    numberDot(slide, C, i + 1, { x: 7.75, y: 2.85 + i * o.boardGap, color: o.color, name: `Board step ${i + 1}` });
    text(slide, line, { x: 8.35, y: 2.83 + i * o.boardGap, w: 4.2, h: o.boardGap - 0.05, fontSize: 13.5, color: C.background1, valign: "top", objectName: `Board line ${i + 1}` });
  });
  callout(slide, C, o.note, { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: o.color, name: "Scene note" });
}

export async function buildWalkthrough(file) {
  const { pres, C } = newDeck({ title: "Training scene walkthrough", footer: "xr-training-lab  |  Unity hub: step-by-step walkthrough" });
  const HYC = C.accent1, RFC = C.accent3, WHC = C.accent5, RSC = C.accent6;

  // 1 title
  section(pres, "Introduction");
  {
    const slide = pres.addSlide({ masterName: "TITLE", sectionTitle: "Introduction" });
    slide.addText("UNITY HUB  /  STEP-BY-STEP WALKTHROUGH", { placeholder: "eyebrow" });
    slide.addText("How to run\nevery training\nscene", { placeholder: "title" });
    text(slide, "From the lobby to the last step of each scenario: which portal to take, what to do in which order, and where each valve, button and station is.", { x: 0.7, y: 4.35, w: 6.2, h: 1.2, fontSize: 17, color: C.background2, objectName: "Subtitle" });
    chip(slide, C, "1 Hydrostatic", { x: 0.7, y: 5.8, w: 1.5, color: HYC, name: "Chip hydro" });
    chip(slide, C, "2 Refinery", { x: 2.3, y: 5.8, w: 1.4, color: RFC, name: "Chip refinery" });
    chip(slide, C, "3 Warehouse", { x: 3.8, y: 5.8, w: 1.5, color: WHC, name: "Chip warehouse" });
    chip(slide, C, "4 Resistor", { x: 5.4, y: 5.8, w: 1.4, color: RSC, name: "Chip resistor" });
    await shot(slide, C, img(IM + "hub-r7-after-lobby.png"), { x: 7.4, y: 0.9, w: 5.35, h: 3.3, caption: "The lobby: four portals, one per scene", name: "Title lobby" });
    await shot(slide, C, img(IM + "hub-r7-after-hydro-bay.png"), { x: 7.4, y: 4.3, w: 5.35, h: 3.0, caption: "Hydrostatic Test Bay, as loaded from portal 1", name: "Title hydro" });
  }

  // 2 controls
  {
    const slide = contentSlide(pres, { section: "Introduction", eyebrow: "CONTROLS", title: "Four actions cover every scene" });
    const rows = [
      ["Move", "Push a thumbstick or trackpad forward, aim the arc, let go (green = allowed)", "Hold T and let go to teleport, or walk with W A S D"],
      ["Look and turn", "Turn your head; flick the stick left or right to turn 45 degrees", "Hold the right mouse button and move the mouse"],
      ["Press", "Point at a valve, button or answer and pull the trigger", "Aim the centre of the view at it and left-click (about 4 m)"],
      ["Grab", "Squeeze the grip near the object; let go to drop it", "Aim at it and hold E (about 3 m); release E to drop"],
    ];
    card(slide, C, { x: 0.6, y: 1.6, w: 12.15, h: 4.1, name: "Controls card" });
    label(slide, C, "ACTION", { x: 0.9, y: 1.8, w: 2.2, name: "Head action" });
    label(slide, C, "VIVE / STEAMVR CONTROLLERS", { x: 3.2, y: 1.8, w: 4.6, color: C.accent2, name: "Head vr" });
    label(slide, C, "DESKTOP (KEYBOARD AND MOUSE)", { x: 8.0, y: 1.8, w: 4.5, color: C.accent5, name: "Head desk" });
    rows.forEach(([a, vr, desk], i) => {
      const yy = 2.25 + i * 0.85;
      if (i > 0) slide.addShape("line", { x: 0.9, y: yy - 0.1, w: 11.55, h: 0, line: { color: C.background2, width: 0.5, transparency: 70 }, objectName: `Row rule ${i}` });
      text(slide, a, { x: 0.9, y: yy, w: 2.2, h: 0.7, fontSize: 16, bold: true, color: C.background1, valign: "middle", objectName: `Action ${i + 1}` });
      text(slide, vr, { x: 3.2, y: yy, w: 4.6, h: 0.7, fontSize: 13.5, color: C.background1, valign: "middle", objectName: `VR ${i + 1}` });
      text(slide, desk, { x: 8.0, y: yy, w: 4.5, h: 0.7, fontSize: 13.5, color: C.background1, valign: "middle", objectName: `Desk ${i + 1}` });
    });
    callout(slide, C, "On the desktop there is no crosshair: the centre of the view is the pointer. Click once in the Game view first, and Esc frees the mouse.", { x: 0.6, y: 5.95, w: 12.15, h: 0.75, color: C.accent2, name: "Desk tip" });
  }

  // 3 start the hub and choose a portal
  section(pres, "The hub");
  await stepSlide(pres, C, {
    section: "The hub", eyebrow: "THE HUB  /  GETTING TO A SCENE", title: "Start in the lobby and pick a portal", color: C.accent1,
    file: IM + "hub-r7-after-lobby.png", caption: "Lobby start view: you stand on the medallion facing the four portals",
    marks: [[615, 440, 1, HYC], [845, 440, 2, RFC], [1075, 440, 3, WHC], [1305, 440, 4, RSC], [960, 900, 5]],
    items: [
      ["1", "Hydrostatic Test Bay", "Pressure-test a heat exchanger"],
      ["2", "Refinery Rounds", "Walk-round with gas alarms and live wind"],
      ["3", "Warehouse & Forklift", "Cross a forklift floor safely"],
      ["4", "Resistor Bench", "Choose the right resistor for an LED"],
      ["5", "You start here", "Follow the gold line to the portals"],
    ],
    panel: "THE FOUR PORTALS (LEFT TO RIGHT)",
    note: "To enter: stand on the portal's floor pad for about 1.4 s, or point at the doorway and pull the trigger. Desktop: press 1, 2, 3 or 4.",
    notes: "Open vive-steamvr/hub in Unity 6000.3.20f1, open the Lobby scene and press Play (or run the built player). The pad shows 'Starting n%' while you stand on it.",
  });

  // 4 coming back
  {
    const slide = contentSlide(pres, { section: "The hub", eyebrow: "THE HUB  /  COMING BACK", title: "Three ways back to the lobby from any scene" });
    await shot(slide, C, img(IM + "hub-lobby-3-pad.png"), { x: 0.6, y: 1.6, w: 5.6, h: 3.6, caption: "A portal pad: stand on it until it reaches 100%", name: "Pad screenshot" });
    const ways = [
      ["Lobby panel", "A small \"Lobby\" panel floats above your left controller. Point at it with the right hand and pull the trigger. Desktop: aim at it and click."],
      ["Both grips", "Hold both grip buttons together for 1.5 seconds."],
      ["Backspace", "On the desktop, press Backspace."],
    ];
    ways.forEach(([head, body], i) => {
      const yy = 1.6 + i * 1.25;
      card(slide, C, { x: 6.45, y: yy, w: 6.3, h: 1.1, name: `Way card ${i + 1}` });
      numberDot(slide, C, i + 1, { x: 6.7, y: yy + 0.33, color: C.accent1, name: `Way ${i + 1}` });
      text(slide, [
        { text: head, options: { bold: true, fontSize: 16, color: C.background1, breakLine: true } },
        { text: body, options: { fontSize: 13, color: C.background2 } },
      ], { x: 7.3, y: yy + 0.12, w: 5.3, h: 0.9, valign: "middle", objectName: `Way text ${i + 1}` });
    });
    card(slide, C, { x: 0.6, y: 5.4, w: 5.6, h: 0.85, name: "Hint card" });
    text(slide, "A hint with these options shows for the first 10 seconds of every scene.", { x: 0.85, y: 5.4, w: 5.1, h: 0.85, fontSize: 13, color: C.background1, valign: "middle", objectName: "Hint text" });
    callout(slide, C, "Every scene has a floating task board. It ticks off each step, counts mistakes, and ends with a summary and a short \"why\" question.", { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: C.accent1, name: "Board note" });
  }

  // ---------------------------------------------------------------- 1 Hydrostatic Test Bay
  await sceneOpener(pres, C, {
    section: "1 Hydrostatic Test Bay", eyebrow: "SCENE 1  /  HYDROSTATIC TEST BAY", title: "Pressure-test heat exchanger HX-101 to 150 psi", color: HYC,
    file: IM + "hub-r7-after-hydro-bay.png", caption: "What you see when the scene loads (hub version)", portal: "Portal 1", goal: "About 3 to 5 minutes",
    board: [
      "Fit both blind flanges on the top nozzles",
      "Open the VENT first, then the FILL valve",
      "Close VENT when full, then close FILL",
      "Start the pump; raise to 150 psi (no more)",
      "Hold 15 s from behind the yellow line",
      "Stop pump, open DRAIN, then open VENT",
    ], boardGap: 0.55,
    note: "Valves show a red lamp when closed and a green lamp when open. Click a valve once to toggle it.",
  });
  await stepSlide(pres, C, {
    section: "1 Hydrostatic Test Bay", eyebrow: "SCENE 1  /  WHERE THINGS ARE", title: "Map of the test bay", color: HYC,
    file: IM + "hub-r7-after-hydro-bay.png", caption: "Start view in the hub. The flange table is behind the task board.",
    marks: [[760, 790, 1], [938, 440, 2], [1070, 498, 3], [1255, 532, 4], [1110, 705, 5]],
    items: [
      ["1", "Blind-flange table", "Two flanges wait here (walk round the board)"],
      ["2", "VENT valve", "On top of the vessel, under the VENT tag"],
      ["3", "Pressure readout", "Big psi number and a bar; 150 is the target"],
      ["4", "Pump panel", "Green PRESSURIZE and red STOP buttons"],
      ["5", "Yellow line", "Stand behind it (your side) during the hold"],
    ],
    panel: "KEY LOCATIONS",
    note: "FILL and DRAIN are the low valves on the pipe under the vessel: FILL on the right (pump side), DRAIN on the left.",
  });
  await stepSlide(pres, C, {
    section: "1 Hydrostatic Test Bay", eyebrow: "SCENE 1  /  STEP 1", title: "Fit both blind flanges on the top nozzles", color: HYC,
    file: HY + "02-blind-flanges.png", caption: "The flange table beside the vessel; yellow discs mark the nozzles",
    marks: [[920, 735, 1], [1090, 630, 1], [780, 105, 2], [1005, 140, 2]],
    items: [
      ["1", "Pick up a flange", "Grip it (desktop: aim and hold E)"],
      ["2", "Drop it on a nozzle", "Carry it to a glowing yellow disc on top of the vessel and let go: it snaps on"],
      ["3", "Repeat for the second", "The step ticks when both are fitted"],
    ],
    note: "Critical miss: pressing PRESSURIZE with an open nozzle. The flanges keep the water and pressure inside the vessel.",
  });
  await stepSlide(pres, C, {
    section: "1 Hydrostatic Test Bay", eyebrow: "SCENE 1  /  STEPS 2 AND 3", title: "Open the VENT first, then FILL; close both when full", color: HYC,
    file: HY + "03-filling-and-venting.png", caption: "Filling: VENT and FILL both open (green lamps), DRAIN closed (red)",
    marks: [[565, 128, 1], [1180, 773, 2], [1515, 205, 3], [400, 668, 4, C.accent6]],
    items: [
      ["1", "VENT (top) first", "Click it: the lamp turns green. Air can now escape"],
      ["2", "Then FILL (low right)", "Water flows in; the level rises for about 8 s"],
      ["3", "Wait for LEVEL full", "Then close VENT, then close FILL"],
      ["4", "Leave DRAIN shut", "Low left valve; it is for the last step"],
    ],
    note: "Why vent first: air trapped in the shell squeezes and stores energy under pressure. Opening FILL before VENT counts as a mistake.",
  });
  await stepSlide(pres, C, {
    section: "1 Hydrostatic Test Bay", eyebrow: "SCENE 1  /  STEPS 4 AND 5", title: "Pump to 150 psi, then hold from behind the yellow line", color: HYC,
    file: HY + "05-hold-from-behind-the-line.png", caption: "At 150 psi, standing behind the line; the pump panel is to the right",
    marks: [[1443, 580, 1], [1105, 497, 2], [1000, 855, 3]],
    items: [
      ["1", "Press PRESSURIZE", "Green button on the pump panel"],
      ["2", "Watch it climb to 150", "About 14 s; it holds there by itself"],
      ["3", "Step back behind the line", "Stay on your side for 15 s; the board counts \"Holding n / 15 s\""],
      ["4", "Leave STOP for now", "Red button, used in step 6"],
    ],
    note: "Critical miss: standing in front of the line during the hold (the count restarts). Pressing PRESSURIZE with VENT open or before full is a mistake.",
  });
  await stepSlide(pres, C, {
    section: "1 Hydrostatic Test Bay", eyebrow: "SCENE 1  /  STEP 6 AND THE QUESTION", title: "Stop the pump, open DRAIN, then open VENT", color: HYC,
    file: HY + "04-test-pressure.png", caption: "Close-up of the vessel: VENT on top, DRAIN low on the left",
    marks: [[870, 318, 3], [580, 950, 2]],
    items: [
      ["1", "Press STOP", "Red button on the pump panel"],
      ["2", "Open DRAIN", "Pressure falls to zero as water leaves"],
      ["3", "Open VENT last", "Air comes back in so the shell drains without a vacuum"],
      ["4", "Answer the question", "Point and trigger, or press 1, 2 or 3"],
    ],
    note: "Opening VENT above 20 psi, or DRAIN before the hold is done, is a mistake. Finish: \"STRONG PRACTICE: no mistakes.\"",
  });

  // ---------------------------------------------------------------- 2 Refinery Rounds
  await sceneOpener(pres, C, {
    section: "2 Refinery Rounds", eyebrow: "SCENE 2  /  REFINERY ROUNDS", title: "Outdoor rounds: enter safely, then handle three alarms", color: RFC,
    file: IM + "hub-r7-after-refinery-start.png", caption: "Start view in the hub: pre-entry station, gate and wind sock", portal: "Portal 2", goal: "About 6 to 8 minutes",
    board: [
      "Prepare to enter: wind, muster point, bump test, radio, gate",
      "Gas alarm: move upwind or crosswind, WARN, REPORT",
      "Flange weep: phone off, stay clear, 3 cones, REPORT",
      "Site alarm: safe route to the muster point, CHECK IN",
    ], boardGap: 0.75,
    note: "Your chest radio (ENTERING, WARN, REPORT, PHONE OFF, monitor reading) follows you. Look down to see it.",
  });
  await stepSlide(pres, C, {
    section: "2 Refinery Rounds", eyebrow: "SCENE 2  /  TASK 1 OF 4", title: "Prepare to enter the unit", color: RFC,
    file: IM + "hub-r7-after-refinery-start.png", caption: "Start view: everything for task 1 is in front of you",
    marks: [[1790, 170, 1], [610, 650, 3], [960, 455, 5], [1520, 470, 6, C.accent2]],
    items: [
      ["1", "Look at the wind sock", "About 1 s: it points downwind"],
      ["2", "Look at the muster flag", "Green flag on the upwind side; turn to find it"],
      ["3", "Bump-test the monitor", "Hold the monitor at the pre-entry station, press BUMP TEST, then let go (it clips on)"],
      ["4", "Radio ENTERING", "Chest radio, after steps 1 to 3"],
      ["5", "Walk through the gate", "Not over the fence"],
    ],
    gap: 0.79, bodySize: 12.5,
    note: "Critical miss: entering before the checks, or without the gas monitor. Board (6) lists the steps and ticks them.",
  });
  {
    const slide = contentSlide(pres, { section: "2 Refinery Rounds", eyebrow: "SCENE 2  /  TASKS 2 AND 3", title: "Gas alarm, then a leaking flange" });
    await figure(slide, C, img(RF + "03-gas-alarm-cloud.png"), { x: 0.6, y: 1.6, w: 6.0, h: 3.4, caption: "Task 2: gas drifts with the wind; gauge G-1 is the gold marker", marks: [[1100, 660, 1], [1400, 300, 2, C.accent6]], color: RFC, name: "Gas screenshot" });
    await figure(slide, C, img(RF + "04-weep-and-barricade.png"), { x: 6.75, y: 1.6, w: 6.0, h: 3.4, caption: "Task 3: flange F-12 with three cones around it", marks: [[920, 390, 1], [790, 740, 2], [1250, 640, 2]], color: RFC, name: "Weep screenshot" });
    entries(slide, C, [
      ["Task 2  Gas alarm", "Walk to gauge G-1 (1). Gas (2) starts upwind and drifts at you: move upwind or crosswind at least 10 m into clean air. Then press WARN, then REPORT on the radio."],
    ], { x: 0.75, y: 5.1, w: 5.75, h: 1.25, nameColor: RFC, nameSize: 15, descSize: 13, name: "Task 2 text" });
    entries(slide, C, [
      ["Task 3  Flange weep", "Press PHONE OFF and stay 3.5 m from F-12 (1). Carry 3 cones from the barricade station and drop them around the leak (2), 2 to 7 m away and spread out. Then REPORT."],
    ], { x: 6.9, y: 5.1, w: 5.75, h: 1.25, nameColor: RFC, nameSize: 15, descSize: 13, name: "Task 3 text" });
    callout(slide, C, "Critical misses: walking toward the release, clicking the flange to tighten it, or a phone on near the leak.", { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: RFC, name: "Refinery note" });
  }
  await stepSlide(pres, C, {
    section: "2 Refinery Rounds", eyebrow: "SCENE 2  /  TASK 4 OF 4", title: "Site alarm: walk upwind to the muster point and check in", color: RFC,
    file: RF + "05-muster-point.png", caption: "The muster point: flag, sign and CHECK IN button",
    marks: [[850, 245, 1], [956, 604, 2]],
    items: [
      ["1", "Walk to the green flag", "A siren sounds and a large release starts. Go round the gas, not through it; get within 3.5 m"],
      ["2", "Press CHECK IN", "On the muster sign: the headcount"],
      ["3", "Answer the question", "Why bump-test a monitor that reads 0?"],
    ],
    note: "Critical miss: going back to the orange tool bag during the alarm. Waiting more than 45 s before moving is a mistake.",
  });

  // ---------------------------------------------------------------- 3 Warehouse & Forklift
  await sceneOpener(pres, C, {
    section: "3 Warehouse & Forklift", eyebrow: "SCENE 3  /  WAREHOUSE & FORKLIFT", title: "Cross a working forklift floor as a pedestrian", color: WHC,
    file: IM + "hub-r7-after-warehouse-overview.png", caption: "The warehouse floor in the hub: racking, lanes, crossing and dock", portal: "Portal 3", goal: "About 4 to 6 minutes",
    board: [
      "Put on hard hat, vest and glasses before entering the floor",
      "Look at the corner mirror for 1.5 s before stepping out",
      "Signal the driver, wait for the stop, cross at the stripes",
      "Cone the spill and the bent rack upright, then radio",
    ], boardGap: 0.75,
    note: "Stay on the green walkways. The forklift drives a loop the whole time; an awareness check pauses the world after the crossing.",
  });
  await stepSlide(pres, C, {
    section: "3 Warehouse & Forklift", eyebrow: "SCENE 3  /  STEP 1", title: "Put on all three PPE items before the yellow line", color: WHC,
    file: WH + "07-start-view.png", caption: "Start view: PPE table and station, with the entry line beyond",
    marks: [[385, 860, 1], [600, 800, 1], [1374, 684, 2], [1740, 260, 3, C.accent1]],
    items: [
      ["1", "Pick up an item", "Hard hat, hi-vis vest, safety glasses on the table"],
      ["2", "Wear it", "VR: bring it to your head or chest and let go. Desktop: keep E held and click WEAR"],
      ["3", "Then cross the line", "Only when all three show on the PPE station"],
    ],
    note: "Entering the floor without full PPE is a mistake: PPE only protects if it is on before you reach the hazard.",
  });
  {
    const slide = contentSlide(pres, { section: "3 Warehouse & Forklift", eyebrow: "SCENE 3  /  STEPS 2 AND 3", title: "Check the mirror, then signal and cross at the stripes" });
    await figure(slide, C, img(WH + "04-corner-mirror.png"), { x: 0.6, y: 1.6, w: 6.0, h: 3.4, caption: "Step 2: the convex mirror on the cargo stack shows the hidden lane", marks: [[990, 560, 1]], color: WHC, name: "Mirror screenshot" });
    await figure(slide, C, img(WH + "03-crossing-forklift-stopped.png"), { x: 6.75, y: 1.6, w: 6.0, h: 3.4, caption: "Step 3: the driver has seen you and is holding", marks: [[910, 266, 2], [1000, 1000, 3]], color: WHC, name: "Crossing screenshot" });
    entries(slide, C, [
      ["Step 2  Corner mirror", "From the entry walkway, keep the mirror (1) near the centre of your view for about 1.5 s. Looking away resets the count."],
    ], { x: 0.75, y: 5.1, w: 5.75, h: 1.25, nameColor: WHC, nameSize: 15, descSize: 13, name: "Step 2 text" });
    entries(slide, C, [
      ["Step 3  Signal and cross", "Look at the driver, press SIGNAL on the \"SIGNAL THE DRIVER\" post. Wait until the forklift stops (2), then cross at the striped crossing (3) to the north side."],
    ], { x: 6.9, y: 5.1, w: 5.75, h: 1.25, nameColor: WHC, nameSize: 15, descSize: 13, name: "Step 3 text" });
    callout(slide, C, "Critical miss (near miss): stepping into a lane with the forklift within 6 m and not stopped for you.", { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: WHC, name: "Warehouse note" });
  }
  await stepSlide(pres, C, {
    section: "3 Warehouse & Forklift", eyebrow: "SCENE 3  /  STEP 4", title: "Cone the spill and the bent upright, then radio", color: WHC,
    file: WH + "09-cone-station.png", caption: "The cone station by the loading zone",
    marks: [[1100, 740, 1]],
    items: [
      ["1", "Take cones here", "Grip a cone from the tray (desktop: hold E)"],
      ["2", "Cone the spill", "Drop one within 2 m of the dark spill on the floor"],
      ["3", "Cone the bent upright", "Drop one within 2 m of the damaged rack leg"],
      ["4", "Press RADIO", "At the SUPERVISOR RADIO station, after both cones"],
    ],
    note: "Radioing before both hazards are coned is a mistake. Marking warns others; the report gets it fixed.",
  });
  {
    const slide = contentSlide(pres, { section: "3 Warehouse & Forklift", eyebrow: "SCENE 3  /  AWARENESS CHECK", title: "Watch the co-worker: the scene pauses and asks" });
    await shot(slide, C, img(WH + "11-awareness-check.png"), { x: 0.6, y: 1.6, w: 6.6, h: 4.65, caption: "The world freezes and three questions appear, one at a time", name: "Check screenshot" });
    card(slide, C, { x: 7.45, y: 1.6, w: 5.3, h: 4.65, name: "Check card" });
    label(slide, C, "WHAT HAPPENS", { x: 7.75, y: 1.8, w: 4.8, color: WHC, name: "Check label" });
    bullets(slide, C, [
      "About 8 s after you cross, the forklift goes to load the west rack and a co-worker walks out of the STAFF ENTRANCE past it.",
      "4.5 s later the scene freezes. Answer from memory: where were they, which way were they walking, how far was the forklift?",
      "Then: STOP where you are, WATCH them, WAIT until they are clear.",
      "The summary shows the board result and, separately, the awareness score out of 3.",
    ], { x: 7.75, y: 2.2, w: 4.75, h: 3.9, size: 14, gap: 10, name: "Check points" });
    callout(slide, C, "Tip: keep looking around while you work. Nothing warns you before the co-worker appears.", { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: WHC, name: "Check note" });
  }

  // ---------------------------------------------------------------- 4 Resistor Bench
  await sceneOpener(pres, C, {
    section: "4 Resistor Bench", eyebrow: "SCENE 4  /  RESISTOR BENCH", title: "Pick and fit the right resistor for an LED", color: RSC,
    file: IM + "hub-r7-after-resistor-room.png", caption: "The bench in the hub: wall cards above, bench tools below", portal: "Portal 4", goal: "About 5 to 8 minutes",
    board: [
      "Clip on the ESD strap before touching parts",
      "Read the bands of the unknown part, measure it, answer",
      "Work out the LED resistor; test a part in the tray",
      "Sulfur-rich air: fit an anti-sulfur part, switch on",
    ], boardGap: 0.75,
    note: "The wall cards hold everything you need: the colour code, the LED circuit, the datasheet and the environment.",
  });
  await stepSlide(pres, C, {
    section: "4 Resistor Bench", eyebrow: "SCENE 4  /  WHERE THINGS ARE", title: "Map of the bench", color: RSC,
    file: RS + "01-bench-overview.png", caption: "Bench overview (standalone project; the hub bench is the same)",
    marks: [[395, 645, 1], [620, 435, 2], [900, 480, 3], [1070, 525, 4], [1256, 490, 5], [1430, 610, 6], [1530, 525, 7]],
    items: [
      ["1", "Wrist strap and ground hook", "Step 1"],
      ["2", "Parts drawer", "Six labelled bins; green dot = anti-sulfur"],
      ["3", "\"What is the value?\"", "Four answer buttons"],
      ["4", "Selection tray and CHECK PART", ""],
      ["5", "Multimeter and probes", ""],
      ["6", "LED test board", "R1 and LED1 sockets"],
      ["7", "5 V bench supply", "Its switch powers the board"],
    ],
    panel: "KEY LOCATIONS", gap: 0.56, headSize: 14, bodySize: 12,
    note: "The unknown part lies on the white card at the front edge of the bench.",
  });
  await stepSlide(pres, C, {
    section: "4 Resistor Bench", eyebrow: "SCENE 4  /  STEPS 1 AND 2", title: "Ground yourself, then read and measure the unknown part", color: RSC,
    file: RS + "04-measuring.png", caption: "Both probes on the leads: the meter confirms 4.7 k ohm",
    marks: [[680, 300, 2], [360, 720, 3], [1460, 440, 4]],
    items: [
      ["1", "Clip on the ESD strap", "Grab the strap, let go near the ground hook. Touching a part first is a mistake"],
      ["2", "Answer the value", "Yellow-violet-red-gold = 4.7 k ohm, 5%"],
      ["3", "Probe the part", "One probe on each lead (desktop: let go near a lead and it clips on)"],
      ["4", "Read the meter", "The step ticks when answer and reading agree"],
    ],
    bodySize: 12.5,
    note: "Always confirm with a meter: the colour code gives the nominal value, the meter shows what the part really is.",
  });
  await stepSlide(pres, C, {
    section: "4 Resistor Bench", eyebrow: "SCENE 4  /  STEPS 3 AND 4", title: "Choose 150 ohm, test it, fit an anti-sulfur part, switch on", color: RSC,
    file: RS + "05-led-lit.png", caption: "Done: anti-sulfur 150 ohm in R1, LED lit at about 20 mA",
    marks: [[720, 650, 2], [1060, 590, 3], [1370, 470, 4]],
    items: [
      ["1", "Work it out", "(5 V - 2 V) / 20 mA = 150 ohm, 1/8 W. Put a part in the selection tray and press CHECK PART"],
      ["2", "Fit an anti-sulfur part in R1", "150 ohm 1% 1/8 W, the bin with the green label"],
      ["3", "Put an LED in LED1", "A spare LED is on the bench"],
      ["4", "Switch on the 5 V supply", "The LED lights; then answer the question"],
    ],
    bodySize: 12.5,
    note: "Critical miss: powering with the 47 ohm part or no resistor burns the LED out. 1/16 W is too weak; 1/2 W is too big for the pads.",
  });

  // ---------------------------------------------------------------- wrap-up
  section(pres, "Summary");
  {
    const slide = contentSlide(pres, { section: "Summary", eyebrow: "QUICK REFERENCE", title: "The one thing to get right in each scene" });
    const cols = [
      ["1  Hydrostatic", HYC, "VENT before FILL, both flanges on, and stand behind the yellow line during the 15 s hold.", HY + "03-filling-and-venting.png"],
      ["2  Refinery", RFC, "Bump-test the monitor before the gate, and always move upwind or crosswind of the gas.", RF + "02-wind-sock-and-board.png"],
      ["3  Warehouse", WHC, "PPE first, mirror next, and cross only once the driver has stopped for you.", WH + "03-crossing-forklift-stopped.png"],
      ["4  Resistor", RSC, "Strap on first, measure what you read, and fit the anti-sulfur 150 ohm part.", RS + "05-led-lit.png"],
    ];
    for (const [i, [head, color, body, pic]] of cols.entries()) {
      const x = 0.6 + i * 3.08;
      card(slide, C, { x, y: 1.6, w: 2.9, h: 4.6, name: `Ref card ${i + 1}` });
      await shot(slide, C, img(pic), { x: x + 0.1, y: 1.7, w: 2.7, h: 1.65, name: `Ref picture ${i + 1}` });
      text(slide, head, { x: x + 0.25, y: 3.5, w: 2.45, h: 0.4, fontSize: 17, bold: true, color, objectName: `Ref head ${i + 1}` });
      text(slide, body, { x: x + 0.25, y: 3.95, w: 2.45, h: 2.1, fontSize: 14, color: C.background1, objectName: `Ref body ${i + 1}` });
    }
    callout(slide, C, "General safety-awareness practice, not a procedure, permit or certification. Return to the lobby any time: Lobby panel, both grips, or Backspace.", { x: 0.6, y: 6.38, w: 12.15, h: 0.55, color: C.accent1, name: "Scope note" });
  }

  await pres.writeFile({ fileName: file });
  await applyDeckTheme(file);
}
