import { avatarsSlide, demoDaySlide, hydrotestSlide } from "./newslides.mjs";
import { applyDeckTheme, bullets, callout, card, chip, code, contentSlide, entries, img, label, newDeck, numberDot, section, shot, steps, text } from "./lib.mjs";

export async function buildMultiplayer(file) {
  const { pres, C } = newDeck({ title: "Multiplayer WebXR examples", footer: "ALL-VR-MR-XR-SDKs  |  Multiplayer WebXR" });

  // 1 title
  section(pres, "Introduction");
  {
    const slide = pres.addSlide({ masterName: "TITLE", sectionTitle: "Introduction" });
    slide.addText("MULTIPLAYER WEBXR  /  VR + AR", { placeholder: "eyebrow" });
    slide.addText("Multiplayer\nWebXR examples", { placeholder: "title" });
    text(slide, "Three WebXR themes share one WebSocket relay: Circuit Lab (VR), Site Safety Layout (AR), and a Hydrostatic Test Rig. Other participants appear as characters in hard hats.", { x: 0.7, y: 3.8, w: 6.2, h: 1.2, fontSize: 18, color: C.background2, objectName: "Subtitle" });
    chip(slide, C, "Circuit Lab · VR", { x: 0.7, y: 5.4, w: 2.2, name: "Chip VR" });
    chip(slide, C, "Site Safety Layout · AR", { x: 3.05, y: 5.4, w: 2.8, color: C.accent3, name: "Chip AR" });
    chip(slide, C, "Desktop fallback", { x: 6.0, y: 5.4, w: 1.9, color: C.accent2, name: "Chip desktop" });
    await shot(slide, C, img("multi-player/screenshots/vr/01-circuit-lab-in-vr.png"), { x: 8.1, y: 0.6, w: 4.65, h: 3.2, caption: "Circuit Lab in an emulated Quest 3 session", name: "Title VR screenshot" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-ar.png"), { x: 8.1, y: 3.95, w: 4.65, h: 3.2, caption: "Site Safety Layout, desktop fallback", name: "Title AR screenshot" });
  }

  // 2 what is shared
  section(pres, "What is shared");
  {
    const slide = contentSlide(pres, { section: "What is shared", eyebrow: "01 / THE IDEA", title: "A shared room, not just shared discovery" });
    const cards = [
      ["Rooms", "Join a named room. A small WebSocket relay keeps the latest snapshot so late joiners see what is already there.", C.accent1],
      ["Presence", "Names, connection counts, and remote participant positions and modes are visible to everyone.", C.accent2],
      ["Ownership", "Only one person can hold a part. Competing grabs are denied and a part is released if its owner leaves.", C.accent3],
      ["Shared state", "Placement, transforms, rotation, switch and beacon state, and removal are broadcast to every client.", C.accent4],
    ];
    cards.forEach(([head, body, color], i) => {
      const x = 0.6 + (i % 2) * 6.15;
      const y = 1.8 + Math.floor(i / 2) * 2.2;
      card(slide, C, { x, y, w: 6.0, h: 2.05, name: `Idea card ${i + 1}` });
      numberDot(slide, C, i + 1, { x: x + 0.3, y: y + 0.28, color });
      text(slide, head, { x: x + 0.95, y: y + 0.25, w: 4.8, h: 0.5, fontSize: 20, bold: true, color: C.background1, objectName: `Idea title ${i + 1}` });
      text(slide, body, { x: x + 0.3, y: y + 0.9, w: 5.4, h: 1.05, fontSize: 14, color: C.background2, objectName: `Idea text ${i + 1}` });
    });
    callout(slide, C, "Themes: each example has its own part catalog and its rooms are namespaced by theme. A room called lab in Circuit Lab is separate from lab in Site Safety Layout.", { x: 0.6, y: 6.2, w: 12.15, h: 0.65, name: "Theme note" });
  }

  // 3 run it
  section(pres, "Run it");
  {
    const slide = contentSlide(pres, { section: "Run it", eyebrow: "02 / RUN IT", title: "Start the relay and join the same room" });
    card(slide, C, { x: 0.6, y: 1.75, w: 6.0, h: 4.9, name: "Run card" });
    label(slide, C, "1  START THE SERVER  (multi-player/)", { x: 0.9, y: 1.95, w: 5.4, name: "Run label" });
    code(slide, C, "npm install\nnpm test\nnpm start", { x: 0.9, y: 2.35, w: 5.4, h: 1.2, size: 16, name: "Run commands" });
    label(slide, C, "2  OPEN THE EXAMPLES", { x: 0.9, y: 3.85, w: 5.4, color: C.accent4, name: "Open label" });
    code(slide, C, "http://localhost:8443/multi-player/vr/\nhttp://localhost:8443/multi-player/ar/", { x: 0.9, y: 4.25, w: 5.4, h: 0.95, size: 12, name: "Open commands" });
    text(slide, "Use the same room name on each client. Add ?theme=circuits or ?theme=safety to put either page in either theme.", { x: 0.9, y: 5.4, w: 5.4, h: 1.1, fontSize: 13, color: C.background2, objectName: "Run note" });
    card(slide, C, { x: 6.85, y: 1.75, w: 5.9, h: 4.9, name: "Network card" });
    label(slide, C, "HEADSET OR PHONE ON THE LAN", { x: 7.15, y: 1.95, w: 5.3, color: C.accent3, name: "Network label" });
    bullets(slide, C, ["WebXR needs HTTPS off localhost; create a trusted certificate with mkcert.", "Set TLS_KEY and TLS_CERT, then open https://<host>:8443/multi-player/...", "Immersive VR needs a headset browser; AR needs a phone with WebXR hit testing.", "The relay has no accounts and keeps rooms in memory only. Do not expose it publicly."], { x: 7.15, y: 2.4, w: 5.3, h: 4.0, size: 14, gap: 10, name: "Network points" });
  }

  // 4 Circuit Lab
  section(pres, "Circuit Lab (VR)");
  {
    const slide = contentSlide(pres, { section: "Circuit Lab (VR)", eyebrow: "03 / CIRCUIT LAB", title: "Snap parts together and power a live circuit" });
    steps(slide, C, [
      "Add a battery, switch, bulb, resistor, motor, or wire from the palette, or press Load demo circuit.",
      "Drag parts so their gold terminal ends touch (they turn green). R rotates the selected part.",
      "Select the switch and press T. A closed loop lights the bulb and spins the motor for everyone.",
      "A resistor dims the bulb; a wire straight across the battery shows a short-circuit warning.",
    ], { x: 0.6, y: 1.85, w: 5.4, gap: 1.05, size: 14 });
    callout(slide, C, "CHECK: \"Closed circuit: 1 bulb lit\" appears on every client.", { x: 0.6, y: 6.2, w: 5.4, h: 0.6, color: C.accent4, name: "Circuit check" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-vr.png"), { x: 6.3, y: 1.75, w: 6.45, h: 4.95, caption: "Two participants in room \"showcase\", demo circuit with the switch closed", name: "Circuit Lab screenshot" });
  }

  // 5 circuit model
  {
    const slide = contentSlide(pres, { section: "Circuit Lab (VR)", eyebrow: "04 / HOW THE CIRCUIT WORKS", title: "Every client computes the same result from shared state" });
    const four = [
      ["Terminals", "Each part has two terminals. Terminals within about 0.3 board units are joined.", C.accent1],
      ["A loop", "Open switches are removed. Dead-end branches are pruned. The battery must sit on a closed loop.", C.accent2],
      ["Live parts", "Bulbs and motors on that loop are live. Each resistor on the loop reduces brightness and speed.", C.accent3],
      ["Short circuit", "If the battery's terminals touch with no load, the model reports a short circuit instead.", C.accent6],
    ];
    four.forEach(([head, body, color], i) => {
      const x = 0.6 + i * 3.08;
      card(slide, C, { x, y: 1.8, w: 2.95, h: 3.2, name: `Model card ${i + 1}` });
      numberDot(slide, C, i + 1, { x: x + 0.25, y: 2.05, color });
      text(slide, head, { x: x + 0.25, y: 2.65, w: 2.5, h: 0.5, fontSize: 19, bold: true, color: C.background1, objectName: `Model title ${i + 1}` });
      text(slide, body, { x: x + 0.25, y: 3.2, w: 2.5, h: 1.7, fontSize: 13.5, color: C.background2, objectName: `Model text ${i + 1}` });
    });
    callout(slide, C, "A simplified teaching model: it counts resistors rather than solving a full electrical network. The relay only shares part positions and switch state.", { x: 0.6, y: 5.3, w: 12.15, h: 0.8, color: C.accent3, name: "Model note" });
    text(slide, "Rotation, toggle, and delete are shared messages; nothing about the circuit result is stored on the server.", { x: 0.6, y: 6.3, w: 12.15, h: 0.4, fontSize: 13, color: C.background2, objectName: "Model footnote" });
  }

  // 6 Site Safety Layout
  section(pres, "Site Safety Layout (AR)");
  {
    const slide = contentSlide(pres, { section: "Site Safety Layout (AR)", eyebrow: "05 / SITE SAFETY LAYOUT", title: "Plan a work-area layout and watch the checklist" });
    steps(slide, C, [
      "Place exit signs, extinguishers, first-aid kits, a muster point, hazard beacons, and barriers, or load the sample layout.",
      "Select a beacon and press T to mark an active hazard; it flashes amber for everyone.",
      "The checklist shows which essentials are missing and warns about any beacon with no barrier within about a meter.",
      "Fix the warnings together; another participant's grab is denied while you hold a part.",
    ], { x: 0.6, y: 1.85, w: 5.4, gap: 1.05, size: 14 });
    callout(slide, C, "A teaching aid, not a code-compliant safety plan.", { x: 0.6, y: 6.2, w: 5.4, h: 0.6, color: C.accent3, name: "Safety note" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-ar.png"), { x: 6.3, y: 1.75, w: 6.45, h: 4.95, caption: "Sample layout, flashing beacon behind a barrier, checklist passing", name: "Safety Layout screenshot" });
  }

  // 7 AR calibration
  {
    const slide = contentSlide(pres, { section: "Site Safety Layout (AR)", eyebrow: "06 / AR ALIGNMENT", title: "Two devices, one board: calibrate to the same two points" });
    card(slide, C, { x: 0.6, y: 1.75, w: 6.0, h: 4.95, name: "Calibration card" });
    label(slide, C, "CALIBRATE EACH DEVICE", { x: 0.9, y: 1.95, w: 5.4, name: "Calibration label" });
    steps(slide, C, ["Use the same visible physical origin marker.", "Tap a second point that defines the +Z direction.", "The app maps the shared virtual board to that device's local tracking space.", "Build: edits stay in sync even if alignment differs, but the board will not line up physically."], { x: 0.9, y: 2.45, w: 5.4, gap: 1.0, size: 14 });
    card(slide, C, { x: 6.85, y: 1.75, w: 5.9, h: 4.95, name: "Limits card" });
    label(slide, C, "WHAT THIS DOES NOT DO", { x: 7.15, y: 1.95, w: 5.3, color: C.accent3, name: "Limits label" });
    bullets(slide, C, ["No cloud anchors or saved world maps.", "No automatic room scan, occlusion, or collision detection.", "WebXR gives each device its own tracking space; sharing one is up to the app.", "Immersive AR needs HTTPS (except localhost) and a phone with hit testing."], { x: 7.15, y: 2.45, w: 5.3, h: 4.0, size: 14, gap: 10, name: "Limits points" });
  }

  // 8 relay protocol
  section(pres, "Under the hood");
  {
    const slide = contentSlide(pres, { section: "Under the hood", eyebrow: "07 / THE RELAY", title: "A small JSON protocol over one WebSocket" });
    const rows = [
      ["join", "Enter a room (name, mode, theme). The server replies with a snapshot."],
      ["place · delete", "Add or remove a part. Part names are validated against the theme's catalog."],
      ["grab · move · release", "Single-owner dragging. Moves are accepted only from the holder."],
      ["rotate", "Turn a part unless someone else is holding it."],
      ["toggle", "Switch or hazard beacon state; accepted only on toggleable parts."],
      ["cursor", "Presence: each participant's position on the board."],
    ];
    rows.forEach(([name, detail], i) => {
      const y = 1.75 + i * 0.78;
      card(slide, C, { x: 0.6, y, w: 12.15, h: 0.68, name: `Message row ${i + 1}` });
      text(slide, name, { x: 0.9, y, w: 3.4, h: 0.68, fontSize: 15, bold: true, fontFace: "Courier New", color: C.accent1, valign: "middle", objectName: `Message name ${i + 1}` });
      text(slide, detail, { x: 4.4, y, w: 8.1, h: 0.68, fontSize: 14, color: C.background1, valign: "middle", objectName: `Message detail ${i + 1}` });
    });
    text(slide, "Limits protect the relay: 24 clients per room, 64 rooms, 250 parts per room, 16 KB messages, validated coordinates.", { x: 0.6, y: 6.5, w: 12.15, h: 0.4, fontSize: 13, color: C.background2, objectName: "Protocol note" });
  }

  section(pres, "Hydrostatic Test Rig");
  await hydrotestSlide(pres, C, { section: "Hydrostatic Test Rig", eyebrow: "HYDROSTATIC TEST RIG" });
  await avatarsSlide(pres, C, { section: "Hydrostatic Test Rig", eyebrow: "ROOMS AND PEOPLE" });
  await demoDaySlide(pres, C, { section: "Hydrostatic Test Rig", eyebrow: "DEMO DAY / HTTPS START" });

  // 9 VR + tests
  section(pres, "Verify");
  {
    const slide = contentSlide(pres, { section: "Verify", eyebrow: "08 / TESTS AND VR CHECKS", title: "What is tested, and what still needs a headset" });
    card(slide, C, { x: 0.6, y: 1.75, w: 5.6, h: 4.95, name: "Tests card" });
    label(slide, C, "AUTOMATED TESTS (npm test, 34 pass)", { x: 0.85, y: 1.95, w: 5.1, color: C.accent5, name: "Tests label" });
    bullets(slide, C, ["Mixed VR and AR clients share a room", "Snapshots for late joiners; rooms stay isolated", "Theme separation and part catalogs", "Single-owner grabs; disconnect cleanup", "Toggle and rotate rules", "Invalid input and path traversal are rejected"], { x: 0.85, y: 2.4, w: 5.1, h: 3.0, size: 13.5, gap: 6, name: "Tests points" });
    text(slide, "Headset sessions and AR camera hit testing still need physical hardware.", { x: 0.85, y: 5.65, w: 5.1, h: 0.9, fontSize: 13, color: C.accent3, objectName: "Hardware note" });
    await shot(slide, C, img("multi-player/screenshots/vr/01-circuit-lab-in-vr.png"), { x: 6.45, y: 1.75, w: 6.3, h: 4.0, caption: "Emulated Quest 3 (Meta IWER) with a scripted session", name: "Emulated VR screenshot" });
    text(slide, "In VR the board becomes a table-height model in front of you instead of a floor you stand inside. Captured with tools/vr-capture.", { x: 6.45, y: 5.9, w: 6.3, h: 0.8, fontSize: 13, color: C.background2, objectName: "VR note" });
  }

  // 10 limitations
  {
    const slide = contentSlide(pres, { section: "Verify", eyebrow: "09 / SCOPE", title: "A teaching sample, not a production networking stack" });
    const items = [
      ["No accounts or moderation", "Anyone who can reach the server can join a room. Use non-sensitive names."],
      ["In-memory state", "Rooms and parts reset when everyone leaves or the server restarts."],
      ["No prediction or voice", "No lag compensation, reconnect-with-state, or voice chat."],
      ["Local deployment", "No hosting automation. Use HTTPS with a trusted certificate for devices."],
    ];
    items.forEach(([head, body], i) => {
      const x = 0.6 + (i % 2) * 6.15;
      const y = 1.8 + Math.floor(i / 2) * 2.0;
      card(slide, C, { x, y, w: 6.0, h: 1.85, name: `Scope card ${i + 1}` });
      text(slide, head, { x: x + 0.3, y: y + 0.25, w: 5.4, h: 0.45, fontSize: 18, bold: true, color: C.background1, objectName: `Scope title ${i + 1}` });
      text(slide, body, { x: x + 0.3, y: y + 0.85, w: 5.4, h: 0.9, fontSize: 14, color: C.background2, objectName: `Scope text ${i + 1}` });
    });
    callout(slide, C, "Repository: multi-player/README.md  ·  vr/README.md  ·  ar/README.md  ·  screenshots/README.md", { x: 0.6, y: 5.95, w: 12.15, name: "Docs note" });
  }

  await pres.writeFile({ fileName: file });
  await applyDeckTheme(file);
}
