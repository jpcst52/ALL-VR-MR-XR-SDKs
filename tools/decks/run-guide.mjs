import { demoDaySlide, launcherSlide, verifiedSlide, viveProjectsSlide, viveSetupSlide } from "./newslides.mjs";
import { applyDeckTheme, bullets, callout, card, chip, code, contentSlide, entries, img, label, newDeck, section, shot, steps, text } from "./lib.mjs";

const WEB = [
  {
    eyebrow: "BROWSER EXAMPLE", title: "WebXR | Color Field", sub: "Browser API first: request an immersive session directly.", folder: "webxr", shot: "screenshots/webxr-color-field.png",
    illustrates: "Session support checks, the user-gesture session request, and a fallback preview when immersive VR is unavailable.",
    tryThis: "Change the animation speed. On a compatible headset, click Enter VR to begin the session.", note: "No 3D geometry in XR: this sample focuses on session setup and lifecycle.",
  },
  {
    eyebrow: "BROWSER EXAMPLE", title: "A-Frame | Orbit Garden", sub: "Declarative scene composition with gaze interaction.", folder: "a-frame", shot: "screenshots/a-frame-orbit-garden.png",
    illustrates: "HTML-like scene entities, built-in animation, camera controls, and cursor-driven selection.",
    tryThis: "Look at a lantern to select it; the selected lantern grows and the status label names it.", note: "VR entry is available when the browser and device support WebXR.",
  },
  {
    eyebrow: "BROWSER EXAMPLE", title: "Three.js | Prism Bloom", sub: "Custom scene construction and WebXR renderer entry.", folder: "threejs", shot: "screenshots/threejs-prism-bloom.png",
    illustrates: "Three.js geometry, materials, lighting, orbit controls, animation, and its VR entry button.",
    tryThis: "Drag to orbit, cycle the prism glow, and inspect the three moving satellites.", note: "A desktop browser can show the scene even if immersive VR is unavailable.",
  },
  {
    eyebrow: "BROWSER EXAMPLE", title: "Babylon.js | Luminous Totem", sub: "A full 3D engine with a default XR experience flow.", folder: "babylonjs", shot: "screenshots/babylonjs-luminous-totem.png",
    illustrates: "Babylon.js scene setup, orbit camera, lights, emissive materials, animated beacons, and XR UI.",
    tryThis: "Orbit the totem and use Change totem hue to cycle its emissive palette.", note: "XR initialization fails gracefully on unsupported desktop contexts.",
  },
];

const NATIVE = [
  {
    eyebrow: "UNITY / OPEN STANDARD", title: "OpenXR | Instance Lifecycle", color: "accent1",
    illustrates: "Unity's OpenXR provider feature callback when the runtime creates an OpenXR instance.",
    requires: "Unity 2022.3+; XR Plug-in Management; OpenXR Plugin 1.14.x-compatible version; active PC OpenXR runtime.",
    steps: ["Install XR Plug-in Management and OpenXR Plugin.", "Copy OpenXRInstanceLifecycleFeature.cs into Assets/Scripts/.", "Enable OpenXR for Standalone and enable \"Instance Lifecycle Example\" in OpenXR Features.", "Run with the intended runtime active; inspect the Unity Console or player log."],
    check: "The log reports the OpenXR instance callback.", note: "No scene object is needed. The sample does not implement input or rendering.",
  },
  {
    eyebrow: "UNITY / RUNTIME + INPUT", title: "SteamVR | Boolean Action Toggle", color: "accent2",
    illustrates: "A SteamVR Boolean action transition toggles a renderer's material color.",
    requires: "Unity version supported by Valve's plugin; SteamVR Unity Plugin; configured headset/controller action binding.",
    steps: ["Install Valve's SteamVR Unity Plugin and run its setup.", "Create a Boolean toggle action in the SteamVR Input window; generate/update actions.", "Add SteamVRActionColorToggle.cs to a rendered object and assign the action and Renderer.", "Bind the action to a controller button; run the scene with SteamVR active and press it."],
    check: "The target changes color once each time the action goes down.", note: "Bindings and action assets are intentionally configured by the developer.",
  },
  {
    eyebrow: "UNITY / QUEST DEVICE SDK", title: "Meta XR | Quest Camera Preview", color: "accent3",
    illustrates: "Meta's passthrough-camera texture displayed in a Unity UI RawImage after streaming begins.",
    requires: "Unity 6000.0.38f1+; MRUK v81+; Meta XR Core SDK setup; Quest 3 / 3S on Horizon OS v74+.",
    steps: ["Enable passthrough and declare HEADSET_CAMERA permission in the Android project settings.", "Add PassthroughCameraAccess to a scene object and create a Canvas with RawImage + Text.", "Copy QuestPassthroughCameraPreview.cs, assign its camera, preview image, and optional status label.", "Deploy to Quest, grant camera permission, and check the streaming status."],
    check: "The RawImage displays the Quest camera texture and status reports streaming.", note: "Requires physical Quest hardware; the XR Simulator does not support this camera API.",
  },
  {
    eyebrow: "UNITY / INTERACTION TOOLKIT", title: "Unity XRI | Grab Feedback", color: "accent4",
    illustrates: "XRI selection events drive visual feedback when an interactable is grabbed and released.",
    requires: "Unity 2022.3+; XRI 3.0.x; Input System; XR Plug-in Management and a compatible provider.",
    steps: ["Install XRI, Input System, and configure an XR provider.", "Add XR Origin (VR) and XR Interaction Manager; configure controller or hand interactors.", "Add Collider, Rigidbody, and XR Grab Interactable to a cube; attach GrabColorFeedback.cs.", "Run with a headset/controller or the XR Device Simulator and grab the cube."],
    check: "The object turns green on select and returns to its original color on release.", note: "The script subscribes to selection events; it does not create the rig or input actions.",
  },
  {
    eyebrow: "ANDROID / AUGMENTED REALITY", title: "ARCore | Plane Placement", color: "accent3",
    illustrates: "Horizontal plane finding and a camera hit-test used to place or move a 12 cm cube.",
    requires: "Android Studio; Android SDK Platform 35; JDK 17; ARCore-supported device (Android 7.0+).",
    steps: ["Open arcore-android/ in Android Studio and allow Gradle sync.", "Connect a supported device with USB debugging; install Google Play Services for AR if prompted.", "Run the app and accept camera permission.", "Move the phone to find a floor plane, then tap to place or relocate the cube."],
    check: "A cyan marker appears at the tapped plane hit-test pose.", note: "An emulator is not a substitute for camera-based plane detection.",
  },
  {
    eyebrow: "APPLE / IMMERSIVE SPACE", title: "Spatial Atlas | visionOS", color: "accent2",
    illustrates: "SwiftUI opens a RealityKit immersive space containing a world-anchored landmark constellation.",
    requires: "macOS; Xcode 16+; visionOS 2 SDK; simulator or Apple Vision Pro destination.",
    steps: ["Open apple-spatial/SpatialAtlas.xcodeproj on a Mac.", "Select the SpatialAtlas scheme and a visionOS simulator or headset destination.", "Build and run; choose Enter the immersive atlas in the app window.", "For a physical headset, configure a development team under Signing & Capabilities."],
    check: "Landmarks and connecting bars appear in the immersive space.", note: "This project cannot be built on Windows; the atlas is not a room-mapping demo.",
  },
];

export async function buildRunGuide(file) {
  const { pres, C } = newDeck({ title: "Run the XR examples", footer: "ALL-VR-MR-XR-SDKs  |  Run guide" });

  // 1 title
  section(pres, "Start");
  {
    const slide = pres.addSlide({ masterName: "TITLE", sectionTitle: "Start" });
    slide.addText("RUN GUIDE  /  13 EXAMPLES", { placeholder: "eyebrow" });
    slide.addText("Run the XR\nexamples", { placeholder: "title" });
    text(slide, "A practical guide to launching each project and understanding the capability it demonstrates.", { x: 0.7, y: 3.8, w: 6.2, h: 1.2, fontSize: 18, color: C.background2, objectName: "Subtitle" });
    chip(slide, C, "4 browser demos", { x: 0.7, y: 5.4, w: 1.9, name: "Chip browser" });
    chip(slide, C, "Practice Lab", { x: 2.75, y: 5.4, w: 1.6, color: C.accent4, name: "Chip lab" });
    chip(slide, C, "Multiplayer", { x: 4.5, y: 5.4, w: 1.6, color: C.accent5, name: "Chip multiplayer" });
    chip(slide, C, "Unity", { x: 6.25, y: 5.4, w: 1.3, color: C.accent2, name: "Chip Unity" });
    chip(slide, C, "Android + visionOS", { x: 0.7, y: 6.0, w: 2.3, color: C.accent3, name: "Chip native" });
    text(slide, "Separate samples. Real SDKs. Hardware where required.", { x: 3.2, y: 6.0, w: 4.4, h: 0.42, fontSize: 13, bold: true, color: C.background1, valign: "middle", objectName: "Tagline" });
    await shot(slide, C, img("screenshots/threejs-prism-bloom.png"), { x: 7.9, y: 0.6, w: 4.85, h: 3.2, caption: "Three.js · Prism Bloom", name: "Title web screenshot" });
    await shot(slide, C, img("training-simulations/screenshots/vr/04-selecting-hotspots.png"), { x: 7.9, y: 3.95, w: 4.85, h: 3.2, caption: "Practice Lab in an emulated Quest 3 session", name: "Title VR screenshot" });
  }

  // 2 choose a route
  section(pres, "Choose and set up");
  {
    const slide = contentSlide(pres, { section: "Choose and set up", eyebrow: "01 / CHOOSE A ROUTE", title: "Start with the platform you can run today" });
    const cards = [
      ["BROWSER", "Fastest start", ["Python static server", "Current browser with WebGL", "Libraries are vendored: runs offline"], C.accent1],
      ["MULTIPLAYER", "Shared rooms", ["Node.js 20+ and npm", "Run the relay: npm start", "Two tabs, a phone, or a headset"], C.accent5],
      ["UNITY", "Headset examples", ["Unity Editor + package versions", "Copy scripts into a Unity project", "Compatible runtime or device"], C.accent2],
      ["ANDROID", "ARCore placement", ["Android Studio, SDK 35, JDK 17", "ARCore-supported device", "Camera permission"], C.accent3],
      ["VISIONOS", "Spatial Atlas", ["macOS + Xcode 16+", "visionOS 2 SDK", "Simulator or Apple Vision Pro"], C.accent4],
    ];
    cards.forEach(([tag, head, points, color], i) => {
      const x = 0.6 + i * 2.45;
      card(slide, C, { x, y: 1.75, w: 2.3, h: 4.2, name: `Route card ${i + 1}` });
      label(slide, C, tag, { x: x + 0.2, y: 1.95, w: 2, color, name: `Route tag ${i + 1}` });
      text(slide, head, { x: x + 0.2, y: 2.3, w: 1.95, h: 0.8, fontSize: 18, bold: true, color: C.background1, objectName: `Route title ${i + 1}` });
      bullets(slide, C, points, { x: x + 0.2, y: 3.2, w: 1.95, h: 2.6, size: 13, gap: 6, name: `Route points ${i + 1}` });
    });
    text(slide, "Tip: the four Vive/SteamVR folders are full Unity projects; the older Unity folders are scripts to copy in.", { x: 0.6, y: 6.2, w: 12, h: 0.4, fontSize: 13, color: C.accent4, objectName: "Route tip" });
  }

  // 3 static server
  {
    const slide = contentSlide(pres, { section: "Choose and set up", eyebrow: "02 / BROWSER QUICK START", title: "One local server runs all the browser examples" });
    text(slide, "Run the command from the repository root so each example folder has its own URL path.", { x: 0.6, y: 1.5, w: 12, h: 0.4, fontSize: 14, color: C.background2, objectName: "Server intro" });
    card(slide, C, { x: 0.6, y: 2.05, w: 5.9, h: 3.7, name: "Server card" });
    label(slide, C, "1  START THE SERVER", { x: 0.9, y: 2.25, w: 5, name: "Server label" });
    code(slide, C, "python -m http.server 8000", { x: 0.9, y: 2.65, w: 5.3, h: 0.8, size: 18, name: "Server command" });
    label(slide, C, "2  OPEN A DEMO URL", { x: 0.9, y: 3.7, w: 5, color: C.accent4, name: "Open label" });
    text(slide, "localhost is a secure context for WebXR development. Use Ctrl+C in the terminal to stop the server.", { x: 0.9, y: 4.05, w: 5.3, h: 1.4, fontSize: 14, color: C.background1, objectName: "Server note" });
    card(slide, C, { x: 6.75, y: 2.05, w: 6.0, h: 3.7, name: "URL card" });
    label(slide, C, "DEMO URLS", { x: 7.05, y: 2.25, w: 5, color: C.accent3, name: "URL label" });
    const urls = ["http://localhost:8000/webxr/", "http://localhost:8000/a-frame/", "http://localhost:8000/threejs/", "http://localhost:8000/babylonjs/", "http://localhost:8000/training-simulations/"];
    urls.forEach((url, i) => text(slide, url, { x: 7.05, y: 2.7 + i * 0.5, w: 5.5, h: 0.4, fontSize: 15, fontFace: "Courier New", color: C.accent1, objectName: `URL ${i + 1}` }));
    text(slide, "WebGL is needed; libraries are vendored so no internet is required. A desktop fallback is not the same as a headset session.", { x: 0.6, y: 6.0, w: 12.1, h: 0.6, fontSize: 13, color: C.background2, objectName: "Server caveat" });
  }

  // 4-7 web examples
  section(pres, "Browser examples");
  for (const [i, ex] of WEB.entries()) {
    const slide = contentSlide(pres, { section: "Browser examples", eyebrow: ex.eyebrow, title: ex.title });
    text(slide, ex.sub, { x: 0.6, y: 1.4, w: 12, h: 0.35, fontSize: 14, color: C.background2, objectName: "Subtitle" });
    card(slide, C, { x: 0.6, y: 1.95, w: 5.6, h: 4.75, name: "Instructions card" });
    label(slide, C, "WHAT IT ILLUSTRATES", { x: 0.9, y: 2.15, w: 4.5, name: "Illustrates label" });
    text(slide, ex.illustrates, { x: 0.9, y: 2.5, w: 5.0, h: 1.0, fontSize: 15, bold: true, color: C.background1, objectName: "Illustrates" });
    label(slide, C, "RUN FROM THE REPOSITORY ROOT", { x: 0.9, y: 3.6, w: 4.5, color: C.accent2, name: "Run label" });
    code(slide, C, `python -m http.server 8000\nhttp://localhost:8000/${ex.folder}/`, { x: 0.9, y: 3.95, w: 5.0, h: 0.95, size: 13, name: "Run command" });
    label(slide, C, "TRY THIS", { x: 0.9, y: 5.05, w: 4.5, color: C.accent3, name: "Try label" });
    text(slide, ex.tryThis, { x: 0.9, y: 5.38, w: 5.0, h: 0.75, fontSize: 13.5, color: C.background1, objectName: "Try this" });
    text(slide, ex.note, { x: 0.9, y: 6.2, w: 5.0, h: 0.4, fontSize: 11, color: C.background2, objectName: "Example note" });
    await shot(slide, C, img(ex.shot), { x: 6.45, y: 1.95, w: 6.3, h: 4.75, caption: "Desktop capture; immersive VR needs a supported headset or emulator", name: `${ex.folder} screenshot` });
  }

  // 8-9 Practice lab
  section(pres, "Practice Lab");
  {
    const slide = contentSlide(pres, { section: "Practice Lab", eyebrow: "BROWSER / TRAINING", title: "Fieldwork XR Practice Lab | 16 hands-on scenarios" });
    text(slide, "Business, research, industrial, and manufacturing-quality runs. Enter a name, pick a scenario, and carry out each task in the 3D scene.", { x: 0.6, y: 1.4, w: 12, h: 0.35, fontSize: 14, color: C.background2, objectName: "Subtitle" });
    card(slide, C, { x: 0.6, y: 1.95, w: 4.6, h: 4.75, name: "Lab instructions card" });
    label(slide, C, "RUN FROM THE REPOSITORY ROOT", { x: 0.85, y: 2.15, w: 4, color: C.accent2, name: "Lab run label" });
    code(slide, C, "python -m http.server 8000\nhttp://localhost:8000/\n  training-simulations/", { x: 0.85, y: 2.5, w: 4.1, h: 1.1, size: 12, name: "Lab command" });
    label(slide, C, "TRY THIS", { x: 0.85, y: 3.85, w: 4, color: C.accent3, name: "Lab try label" });
    bullets(slide, C, ["Enter a name and open Work at height.", "Click the glowing hotspots to do each step.", "Try a wrong action to see its consequence.", "Use the Keyboard / list controls if you prefer buttons."], { x: 0.85, y: 4.2, w: 4.1, h: 2.4, size: 13, gap: 5, name: "Lab try" });
    await shot(slide, C, img("training-simulations/screenshots/02-catalog.png"), { x: 5.4, y: 1.95, w: 3.7, h: 4.75, caption: "Scenario catalog", name: "Lab catalog" });
    await shot(slide, C, img("training-simulations/screenshots/03-scenario-7-work-at-height.png"), { x: 9.25, y: 1.95, w: 3.5, h: 4.75, caption: "Work at height", name: "Lab task", crop: { left: 0, top: 130, width: 620, height: 780 } });
  }
  {
    const slide = contentSlide(pres, { section: "Practice Lab", eyebrow: "HOW A TASK WORKS", title: "Do the task yourself: hotspots, order, consequences" });
    steps(slide, C, [
      "Each task puts glowing hotspots on the 3D work area. Click one (or point a VR controller and pull the trigger) to carry out that step.",
      "Some steps have prerequisites. You cannot lock out a breaker you have not switched off, or strike an arc before the permit and fire watch are ready.",
      "Wrong actions turn red and explain the consequence. Critical ones flash the scene and are flagged as critical safety misses.",
      "The scene responds: lanyards connect, locks and tags appear, barricades and a fire watch show up, the platform sways in wind.",
      "A task scores 2 with no mistakes, 1 with one, and 0 with more or any critical mistake. The result lists the principles to review.",
    ], { x: 0.6, y: 1.8, w: 6.3, gap: 0.98, size: 14 });
    await shot(slide, C, img("training-simulations/screenshots/06-critical-miss.png"), { x: 7.2, y: 1.75, w: 5.55, h: 4.95, caption: "A critical mistake in the electrical scenario", name: "Critical miss screenshot", crop: { left: 0, top: 0, width: 1280, height: 1100 } });
  }

  // 10-12 multiplayer
  section(pres, "Multiplayer");
  {
    const slide = contentSlide(pres, { section: "Multiplayer", eyebrow: "MULTIPLAYER / NODE.JS RELAY", title: "Start the relay, then join the same room" });
    card(slide, C, { x: 0.6, y: 1.75, w: 6.0, h: 4.9, name: "Relay card" });
    label(slide, C, "1  START THE SERVER  (multi-player/)", { x: 0.9, y: 1.95, w: 5.4, name: "Relay label" });
    code(slide, C, "npm install\nnpm test\nnpm start", { x: 0.9, y: 2.35, w: 5.4, h: 1.2, size: 16, name: "Relay commands" });
    label(slide, C, "2  OPEN THE PAGES", { x: 0.9, y: 3.85, w: 5.4, color: C.accent4, name: "Pages label" });
    code(slide, C, "http://localhost:8443/multi-player/vr/\nhttp://localhost:8443/multi-player/ar/", { x: 0.9, y: 4.25, w: 5.4, h: 0.95, size: 12, name: "Pages commands" });
    text(slide, "Join the same room name. A room belongs to one theme (circuits or safety), so use ?theme=circuits or ?theme=safety to mix pages.", { x: 0.9, y: 5.4, w: 5.4, h: 1.1, fontSize: 13, color: C.background2, objectName: "Relay note" });
    card(slide, C, { x: 6.85, y: 1.75, w: 5.9, h: 4.9, name: "Headset card" });
    label(slide, C, "HEADSET OR PHONE ON THE LAN", { x: 7.15, y: 1.95, w: 5.3, color: C.accent3, name: "Headset label" });
    bullets(slide, C, ["WebXR needs HTTPS off localhost. Create a trusted development certificate with mkcert.", "Set TLS_KEY and TLS_CERT, then npm start and open https://<host>:8443/...", "The relay is an unauthenticated teaching sample. Do not expose it to the internet.", "Immersive VR needs a headset browser; AR needs a phone with WebXR hit testing."], { x: 7.15, y: 2.4, w: 5.3, h: 4.0, size: 14, gap: 10, name: "Headset points" });
  }
  {
    const slide = contentSlide(pres, { section: "Multiplayer", eyebrow: "MULTIPLAYER / VR", title: "Circuit Lab | build a live circuit together" });
    steps(slide, C, [
      "Join a room and press Load demo circuit, or add parts from the palette.",
      "Drag parts so their gold terminal ends touch; touching ends turn green. R rotates the selected part.",
      "Select the switch and press T to close it. A closed loop lights the bulb and spins the motor.",
      "Add a resistor to dim it, or short the battery with a wire to see the warning.",
    ], { x: 0.6, y: 1.9, w: 5.4, gap: 1.05, size: 14 });
    callout(slide, C, "CHECK: the status line reads \"Closed circuit: 1 bulb lit\" for everyone in the room.", { x: 0.6, y: 6.15, w: 5.4, h: 0.6, color: C.accent4, name: "Circuit check" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-vr.png"), { x: 6.3, y: 1.75, w: 6.45, h: 2.4, caption: "Desktop fallback with two participants", name: "Circuit screenshot" });
    await shot(slide, C, img("multi-player/screenshots/vr/01-circuit-lab-in-vr.png"), { x: 6.3, y: 4.3, w: 6.45, h: 2.4, caption: "Emulated Quest 3 session: the board becomes a table-height model", name: "Circuit VR screenshot" });
  }
  {
    const slide = contentSlide(pres, { section: "Multiplayer", eyebrow: "MULTIPLAYER / AR", title: "Site Safety Layout | plan a shared work area" });
    steps(slide, C, [
      "Join a room, press Load sample layout, or place exits, extinguishers, first aid, a muster point, beacons, and barriers.",
      "Select a hazard beacon and press T to mark an active hazard. It flashes amber for everyone.",
      "Watch the checklist: it lists missing items and warns about beacons with no barrier nearby.",
      "In AR, align each device to the same two physical markers (origin, then +Z) before building.",
    ], { x: 0.6, y: 1.9, w: 5.4, gap: 1.05, size: 14 });
    callout(slide, C, "Calibration maps a shared virtual board to each device. It is not a cloud anchor.", { x: 0.6, y: 6.15, w: 5.4, h: 0.6, color: C.accent3, name: "AR note" });
    await shot(slide, C, img("multi-player/screenshots/multiplayer-ar.png"), { x: 6.3, y: 1.75, w: 6.45, h: 4.95, caption: "Desktop fallback of the AR page; physical AR needs a phone", name: "Safety screenshot" });
  }

  // 13-18 native examples
  section(pres, "Unity, Android, and visionOS");
  for (const ex of NATIVE) {
    const color = C[ex.color];
    const slide = contentSlide(pres, { section: "Unity, Android, and visionOS", eyebrow: ex.eyebrow, title: ex.title });
    text(slide, "These are native SDK or Unity source examples; follow the folder README for version-specific setup.", { x: 0.6, y: 1.4, w: 12, h: 0.35, fontSize: 14, color: C.background2, objectName: "Subtitle" });
    card(slide, C, { x: 0.6, y: 1.95, w: 4.4, h: 4.75, name: "Illustrates card" });
    label(slide, C, "WHAT IT ILLUSTRATES", { x: 0.85, y: 2.15, w: 3.9, color, name: "Illustrates label" });
    text(slide, ex.illustrates, { x: 0.85, y: 2.5, w: 3.9, h: 1.5, fontSize: 15, bold: true, color: C.background1, objectName: "Illustrates" });
    label(slide, C, "REQUIRES", { x: 0.85, y: 4.1, w: 3.9, color: C.accent2, name: "Requires label" });
    text(slide, ex.requires, { x: 0.85, y: 4.45, w: 3.9, h: 1.4, fontSize: 12.5, color: C.background2, objectName: "Requires" });
    text(slide, ex.note, { x: 0.85, y: 5.9, w: 3.9, h: 0.7, fontSize: 11, color: C.background2, objectName: "Example note" });
    card(slide, C, { x: 5.25, y: 1.95, w: 7.5, h: 4.75, name: "Setup card" });
    label(slide, C, "SETUP & RUN", { x: 5.55, y: 2.15, w: 6.8, color: C.accent1, name: "Setup label" });
    steps(slide, C, ex.steps, { x: 5.55, y: 2.6, w: 6.9, gap: 0.82, size: 13.5, color: C.accent1 });
    callout(slide, C, `CHECK: ${ex.check}`, { x: 5.55, y: 5.95, w: 6.9, h: 0.58, color: C.accent4, name: "Check" });
  }

  section(pres, "Vive, launcher, and demo day");
  await viveProjectsSlide(pres, C, { section: "Vive, launcher, and demo day", eyebrow: "UNITY / VIVE PRO 2 + STEAMVR" });
  await viveSetupSlide(pres, C, { section: "Vive, launcher, and demo day", eyebrow: "UNITY / RUN A PROJECT" });
  await launcherSlide(pres, C, { section: "Vive, launcher, and demo day", eyebrow: "LAUNCHER" });
  await demoDaySlide(pres, C, { section: "Vive, launcher, and demo day", eyebrow: "DEMO DAY / ONE-COMMAND START" });

  // 19 VR without a headset
  section(pres, "Verify");
  {
    const slide = contentSlide(pres, { section: "Verify", eyebrow: "SEE IT IN VR WITHOUT A HEADSET", title: "Capture the VR view with an emulated Quest 3" });
    card(slide, C, { x: 0.6, y: 1.75, w: 5.2, h: 4.95, name: "Capture card" });
    label(slide, C, "FROM tools/vr-capture/", { x: 0.85, y: 1.95, w: 4.7, color: C.accent2, name: "Capture label" });
    code(slide, C, "npm install\nnpm run capture", { x: 0.85, y: 2.35, w: 4.7, h: 1.0, size: 16, name: "Capture commands" });
    bullets(slide, C, ["Start the static server (port 8000) and the multiplayer relay first.", "Meta's IWER emulates a Quest 3 inside Chrome; a script points a controller at objects and pulls the trigger.", "It checks layout, scale, readability, and interaction. It is not a physical headset."], { x: 0.85, y: 3.55, w: 4.7, h: 3.0, size: 13.5, gap: 8, name: "Capture points" });
    await shot(slide, C, img("training-simulations/screenshots/vr/05-task-complete.png"), { x: 6.05, y: 1.75, w: 6.7, h: 2.4, caption: "Task complete in VR", name: "VR task complete" });
    await shot(slide, C, img("training-simulations/screenshots/vr/07-electrical-lockout.png"), { x: 6.05, y: 4.3, w: 6.7, h: 2.4, caption: "Lock and tag applied in the electrical scenario", name: "VR electrical" });
  }

  await verifiedSlide(pres, C, { section: "Verify", eyebrow: "VERIFIED STATUS" });

  // 20 validate
  {
    const slide = contentSlide(pres, { section: "Verify", eyebrow: "VALIDATE & CONTINUE", title: "Know what was tested and what needs a device" });
    text(slide, "Use the captured screenshots as references; always verify immersive behavior on the target runtime.", { x: 0.6, y: 1.4, w: 12, h: 0.35, fontSize: 14, color: C.background2, objectName: "Subtitle" });
    card(slide, C, { x: 0.6, y: 1.95, w: 6.0, h: 4.0, name: "Tested card" });
    label(slide, C, "TESTED ON DESKTOP / EMULATOR", { x: 0.9, y: 2.15, w: 5.4, color: C.accent5, name: "Tested label" });
    bullets(slide, C, ["Four browser demos served and rendered; controls exercised", "Practice Lab: 16 scenarios on desktop (VR captures not refreshed for the 4 newest); scenario tests pass", "Multiplayer relay: automated tests (npm test); two-client rooms", "VR sessions in an emulated Quest 3 with a scripted controller"], { x: 0.9, y: 2.55, w: 5.4, h: 3.2, size: 13.5, gap: 8, name: "Tested points" });
    card(slide, C, { x: 6.75, y: 1.95, w: 6.0, h: 4.0, name: "Needs device card" });
    label(slide, C, "NEEDS HARDWARE / A TOOLCHAIN", { x: 7.05, y: 2.15, w: 5.4, color: C.accent3, name: "Device label" });
    bullets(slide, C, ["Headset VR/AR sessions need supported devices and runtimes", "Real controller input in the Practice Lab and Circuit Lab", "Quest camera needs permission and physical hardware", "ARCore needs a supported Android camera device", "visionOS builds need macOS + Xcode; Unity examples need package setup"], { x: 7.05, y: 2.55, w: 5.4, h: 3.2, size: 13.5, gap: 8, name: "Device points" });
    callout(slide, C, "Read each folder README for exact versions and limits.  Repository overview: README.md  ·  Technology catalog: SDK-list.md", { x: 0.6, y: 6.15, w: 12.15, name: "Next note" });
  }

  await pres.writeFile({ fileName: file });
  await applyDeckTheme(file);
}
