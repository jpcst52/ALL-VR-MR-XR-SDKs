import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { VRButton } from "three/addons/webxr/VRButton.js";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";
import { scenarios, categories } from "./scenarios.js";
import {
  createHeightStation, createControlRoomStation, createBatteryStation,
  createFacadeStation, createWeldingStation, createElectricalStation,
} from "./industrial-stations.js";
import { decorateCoreStation } from "./core-stations.js";
import { createLightersStation, createHydrotestStation, createSpoolStation, createResistorBench } from "./partner-stations.js";
import { createCirculationStation } from "./biology-stations.js";
import { PITT, textPanelTexture, thumbCardTexture, tagSprite, brandPlate, brandAssetsReady } from "../brand-kit/panels.js";
import { createRoom, ROOM_BY_STATION, GROUND_BY_STATION, DEFAULT_GROUND } from "./rooms.js";
import { dressStation, clearDressing, tickDressing, failedModels } from "./dressing.js";
import { createSeasonSky } from "../brand-kit/season-sky.js";

const canvas = document.querySelector("#scene");
const profile = document.querySelector("#profile");
const nameInput = document.querySelector("#learner-name");
const welcome = document.querySelector("#welcome");
const catalog = document.querySelector("#catalog");
const simulation = document.querySelector("#simulation");
const progress = document.querySelector("#progress");
const categoryList = document.querySelector("#category-list");
const briefing = document.querySelector("#briefing");
const draftNote = document.querySelector("#draft-note");
const review = document.querySelector("#review");
const reviewList = document.querySelector("#review-list");
const taskHint = document.querySelector("#task-hint");
const taskLog = document.querySelector("#task-log");
const taskActionsWrap = document.querySelector("#task-actions-wrap");
const taskActions = document.querySelector("#task-actions");
const simulationCategory = document.querySelector("#simulation-category");
const simulationTitle = document.querySelector("#simulation-title");
const simulationBrief = document.querySelector("#simulation-brief");
const stepCount = document.querySelector("#step-count");
const decisionCard = document.querySelector("#decision-card");
const decisionKicker = document.querySelector("#decision-kicker");
const decisionPrompt = document.querySelector("#decision-prompt");
const choices = document.querySelector("#choices");
const feedback = document.querySelector("#feedback");
const feedbackLabel = document.querySelector("#feedback-label");
const feedbackCopy = document.querySelector("#feedback-copy");
const principle = document.querySelector("#principle");
const completion = document.querySelector("#completion");
const completionTitle = document.querySelector("#completion-title");
const completionCopy = document.querySelector("#completion-copy");
const xrNote = document.querySelector("#xr-note");
const continueButton = document.querySelector("#continue");
const retryButton = document.querySelector("#retry");
const finishButton = document.querySelector("#finish");
const backButton = document.querySelector("#back-to-catalog");
const whyPanel = document.querySelector("#why-panel");
const whyText = document.querySelector("#why-text");
const whyDismiss = document.querySelector("#why-dismiss");
const rationaleReview = document.querySelector("#rationale-review");
const reasoningResult = document.querySelector("#reasoning-result");
const reasoningCopy = document.querySelector("#reasoning-copy");
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
const choiceTargets = [];
const catalogTargets = [];
const taskTargets = [];
let task = null;
const completed = new Map();
let activeScenario = null;
let currentStep = 0;
let totalScore = 0;
let answered = false;
let learnerName = "";
let criticalMisses = 0;
let missedPrinciples = [];
// Reasoning question (scenario.rationale), asked after the last task. Its result is kept apart from totalScore.
let rationaleMode = false;
let reasoning = null;

const scene = new THREE.Scene();
const sceneColor = "#071433"; // deep Pitt navy
scene.background = new THREE.Color(sceneColor);
scene.fog = new THREE.Fog(sceneColor, 8, 24);
const camera = new THREE.PerspectiveCamera(54, window.innerWidth / window.innerHeight, 0.1, 80);
camera.position.set(0, 2.4, 8.5);
camera.lookAt(0, 1.3, 0);

const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: false });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
// Realism pass: sRGB output, filmic tone mapping, one soft 1024 shadow map (cheap enough for a Quest browser; untested on a headset).
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.05;
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.xr.enabled = true;
document.body.appendChild(VRButton.createButton(renderer, { optionalFeatures: ["local-floor"] }));
const xrButton = document.querySelector("body > button");
xrButton?.classList.add("xr-button");
if (xrButton) xrButton.disabled = true;
const immersiveVrSupportCheck = "xr" in navigator
  ? navigator.xr.isSessionSupported("immersive-vr")
  : Promise.resolve(false);
const immersiveVrSupport = immersiveVrSupportCheck.catch((error) => {
  console.error("Could not check WebXR VR support.", error);
  xrNote.textContent = "Could not check WebXR support. The desktop simulations are still available.";
  return false;
});
immersiveVrSupport.then((supported) => {
  if (!supported) {
    xrNote.textContent = `WebXR VR is unavailable in this browser. You can still complete all ${scenarios.length} simulations in desktop mode.`;
  } else if (learnerName && xrButton) {
    xrButton.disabled = false;
  }
});

const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;
controls.target.set(0, 1.2, 0);
scene.add(new THREE.HemisphereLight(0xc8e8ff, 0x253241, 2.0));
const keyLight = new THREE.DirectionalLight(0xffdfb3, 3.2);
keyLight.position.set(-4, 7, 5);
keyLight.castShadow = true;
keyLight.shadow.mapSize.set(1024, 1024);
keyLight.shadow.camera.left = -5;
keyLight.shadow.camera.right = 5;
keyLight.shadow.camera.top = 5;
keyLight.shadow.camera.bottom = -5;
keyLight.shadow.camera.near = 2;
keyLight.shadow.camera.far = 20;
keyLight.shadow.bias = -0.0004;
keyLight.shadow.normalBias = 0.02;
keyLight.shadow.radius = 3;
scene.add(keyLight);
scene.add(new THREE.DirectionalLight(0x7fb4ff, 0.7).translateX(5).translateY(3).translateZ(-4));

// Soft image-based light from a built-in room, generated once on the GPU. Gives metal something to reflect.
{
  const pmrem = new THREE.PMREMGenerator(renderer);
  scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
  scene.environmentIntensity = 0.65;
  pmrem.dispose();
}

// ---------------------------------------------------------------- seasonal skies
// One sky per season (assets/skies/skies.json), rotating every 30 s with a pixelate + scan-line transition
// (a cross-fade with prefers-reduced-motion). Shown behind the scenario menu and behind the outdoor scenarios;
// indoor scenarios keep the plain navy room. If the manifest or an image is missing, the navy background stays.
const reducedMotion = matchMedia("(prefers-reduced-motion: reduce)").matches;
let skyWanted = true;
const outdoorStations = new Set(["height", "battery", "facade", "field"]);
const seasonChip = document.querySelector("#season-chip");
const skyCredits = document.querySelector("#sky-credits");
const sky = createSeasonSky({
  manifestUrl: new URL("../assets/skies/skies.json", import.meta.url).href,
  prefer4k: new URLSearchParams(location.search).get("sky") === "4k", // optional, desktop only: ?sky=4k
  reducedMotion,
  yaw: 0.25, // turn the most open part of each panorama to face the menu
  onChange: () => updateSeasonUi(),
  onReady: () => renderSkyCredits(),
});
scene.add(sky.mesh);
sky.start();
window.__seasonSky = sky; // for the capture scripts (screenshots of each season and of a transition)

let seasonTag = null;
function updateSeasonUi() {
  if (sky.label) {
    seasonChip.textContent = `${sky.label} sky`;
    seasonChip.title = sky.caption;
    seasonChip.hidden = false;
  }
  if (seasonTag) {
    seasonTag.parent?.remove(seasonTag);
    seasonTag.material.map.dispose();
    seasonTag.material.dispose();
    seasonTag = null;
  }
  if (!sky.label) return;
  seasonTag = tagSprite(`${sky.label} sky`, { accent: PITT.sky, status: null, worldHeight: 0.17, depthTest: true });
  seasonTag.position.set(1.62, 2.92, -1.36); // beside the Pitt-Bradford + CIST plaque over the menu
  seasonTag.visible = !activeScenario;
  station.add(seasonTag);
}

function renderSkyCredits() {
  const list = skyCredits.querySelector("ul");
  skyCredits.querySelector(".sky-caption").textContent = `${sky.caption}. Images rotate every 30 seconds.`;
  list.replaceChildren(...sky.entries.map((entry) => {
    const item = document.createElement("li");
    const name = document.createElement("strong");
    name.textContent = `${entry.label}${entry.isBradford ? " (Bradford, PA)" : ""}: `;
    item.append(name, entry.credit || "No credit given in the manifest.");
    if (entry.source) {
      const link = document.createElement("a");
      link.href = entry.source;
      link.rel = "noopener";
      link.target = "_blank";
      link.textContent = "source";
      item.append(" (", link, ")");
    }
    return item;
  }));
  skyCredits.hidden = false;
}

// ---------------------------------------------------------------- scenario thumbnails
// thumbs/<scenario id>.jpg, made by tools/vr-capture/thumbs.mjs. thumbs/index.json lists the ids that have one, so
// a missing thumbnail is never requested; an <img> is only inserted after it has loaded (no broken image).
const thumbImages = new Map(); // id -> HTMLImageElement (loaded)
const thumbsReady = fetch(new URL("./thumbs/index.json", import.meta.url))
  .then((response) => (response.ok ? response.json() : { ids: [] }))
  .catch(() => ({ ids: [] }))
  .then(({ ids = [] }) => Promise.all(ids.filter((id) => scenarios.some((s) => s.id === id)).map((id) => new Promise((resolve) => {
    const image = new Image();
    image.decoding = "async";
    image.onload = () => { thumbImages.set(id, image); resolve(); };
    image.onerror = () => resolve();
    image.src = new URL(`./thumbs/${encodeURIComponent(id)}.jpg`, import.meta.url).href;
  }))));

function thumbFor(scenario) {
  const frame = document.createElement("span");
  frame.className = "scenario-thumb";
  frame.setAttribute("aria-hidden", "true");
  const image = thumbImages.get(scenario.id);
  if (image) {
    const copy = image.cloneNode();
    copy.alt = "";
    frame.append(copy);
  } else {
    frame.classList.add("is-empty");
  }
  return frame;
}

// Procedural canvas textures (no downloads, no licensing): concrete speckle and diamond plate.
function noiseTexture(size, base, spread, { repeat = 4, streaks = false } = {}) {
  const art = document.createElement("canvas");
  art.width = art.height = size;
  const ctx = art.getContext("2d");
  const image = ctx.createImageData(size, size);
  let seed = 1337;
  const rand = () => ((seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296);
  for (let i = 0; i < size * size; i++) {
    const v = Math.max(0, Math.min(255, base + (rand() - 0.5) * spread));
    image.data.set([v, v, v, 255], i * 4);
  }
  ctx.putImageData(image, 0, 0);
  if (streaks) {
    ctx.globalAlpha = 0.07;
    for (let i = 0; i < 40; i++) {
      ctx.fillStyle = rand() > 0.5 ? "#fff" : "#000";
      ctx.fillRect(0, rand() * size, size, 1 + rand() * 3);
    }
  }
  const texture = new THREE.CanvasTexture(art);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  texture.repeat.set(repeat, repeat);
  texture.anisotropy = 4;
  return texture;
}

function plateTexture(repeat) {
  const size = 128;
  const art = document.createElement("canvas");
  art.width = art.height = size;
  const ctx = art.getContext("2d");
  ctx.fillStyle = "#d9d9d9";
  ctx.fillRect(0, 0, size, size);
  ctx.fillStyle = "#f4f4f4";
  for (let y = 0; y < 4; y++) {
    for (let x = 0; x < 4; x++) {
      const cx = x * 32 + (y % 2 ? 24 : 8);
      const cy = y * 32 + 16;
      ctx.beginPath();
      ctx.ellipse(cx, cy, 9, 3.5, (y % 2 ? 1 : -1) * 0.7, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  const texture = new THREE.CanvasTexture(art);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  texture.repeat.set(repeat, repeat);
  texture.anisotropy = 4;
  return texture;
}

// Shared shadow setup: every lit mesh casts and receives; unlit UI signs, hotspots, and glass are skipped.
function enableShadows(root) {
  root.traverse((node) => {
    if (!node.isMesh || node.userData.isTrainingUi) return;
    const material = Array.isArray(node.material) ? node.material[0] : node.material;
    if (!material?.isMeshStandardMaterial || material.transparent) return;
    node.castShadow = !node.userData.noShadowCast; // room walls and floors only receive
    node.receiveShadow = true;
  });
}

// All training content lives in `world`. In VR the player starts at the origin, so the world is moved in front of them.
const world = new THREE.Group();
scene.add(world);
const vrWorldOffset = new THREE.Vector3(0, 0, -2.4);
const vrWorldScale = 1.25;

const floor = new THREE.Mesh(
  new THREE.CircleGeometry(7, 64),
  new THREE.MeshStandardMaterial({ color: "#1b2c4f", roughness: 0.82, metalness: 0.08, map: noiseTexture(256, 215, 70, { repeat: 6, streaks: true }) }),
);
floor.receiveShadow = true;
floor.rotation.x = -Math.PI / 2;
floor.position.y = -0.02;
world.add(floor);

const platform = new THREE.Mesh(
  new THREE.CylinderGeometry(2.4, 2.55, 0.14, 64),
  new THREE.MeshStandardMaterial({ color: "#2c4370", roughness: 0.5, metalness: 0.3, map: plateTexture(10) }),
);
platform.receiveShadow = true;
platform.castShadow = true;
platform.position.y = 0.12;
world.add(platform);

const platformRing = new THREE.Mesh(
  new THREE.TorusGeometry(2.08, 0.022, 8, 100),
  new THREE.MeshBasicMaterial({ color: PITT.gold, toneMapped: false }),
);
platformRing.rotation.x = Math.PI / 2;
platformRing.position.y = 0.205;
world.add(platformRing);

const station = new THREE.Group();
world.add(station);
let questionPanel = null;
let scenarioModel = null;

// All in-world text goes through the shared brand kit (../brand-kit/panels.js): backed, rounded, bordered panels
// in the bundled Pitt fonts. lines[0] is the kicker; the rest is the body, which shrinks to fit the panel.
function textTexture(lines, { accent = PITT.gold, width = 1024, height = 512, size = 40, brand = false } = {}) {
  return textPanelTexture({ width, height, size, minSize: Math.round(size * 0.55), accent, brand, kicker: lines[0], body: lines.slice(1).join(" ") });
}

// Canvas width that matches a plane's aspect ratio, so panel text is never stretched.
const canvasWidthFor = (height, [w, h]) => Math.round((height * w) / h);

function makeSign(lines, position, scale, accent = PITT.gold, height = 512) {
  const material = new THREE.MeshBasicMaterial({
    map: textTexture(lines, { accent, height, width: canvasWidthFor(height, scale), size: Math.round(height * 0.13), brand: true }),
    transparent: true,
    toneMapped: false,
    side: THREE.DoubleSide,
  });
  const mesh = new THREE.Mesh(new THREE.PlaneGeometry(...scale), material);
  mesh.position.set(...position);
  mesh.userData.isTrainingUi = true;
  return mesh;
}

function createCounter() {
  const group = new THREE.Group();
  const desk = new THREE.Mesh(
    new THREE.BoxGeometry(1.7, 0.16, 0.72),
    new THREE.MeshStandardMaterial({ color: "#33596a", roughness: 0.6 }),
  );
  desk.position.set(0, 0.94, 0.05);
  group.add(desk);
  for (const x of [-0.66, 0.66]) {
    const leg = new THREE.Mesh(
      new THREE.BoxGeometry(0.1, 0.86, 0.1),
      new THREE.MeshStandardMaterial({ color: "#92a9ba", metalness: 0.32, roughness: 0.38 }),
    );
    leg.position.set(x, 0.48, 0.05);
    group.add(leg);
  }
  const bell = new THREE.Mesh(
    new THREE.CylinderGeometry(0.11, 0.13, 0.08, 24),
    new THREE.MeshStandardMaterial({ color: "#ffb36b", metalness: 0.52, roughness: 0.28 }),
  );
  bell.position.set(0.48, 1.08, -0.06);
  group.add(bell);
  return group;
}

function createSafetyStation() {
  const group = new THREE.Group();
  const coneMaterial = new THREE.MeshStandardMaterial({ color: "#ff9d59", roughness: 0.48 });
  for (const x of [-0.62, 0.62]) {
    const cone = new THREE.Mesh(new THREE.ConeGeometry(0.18, 0.55, 4), coneMaterial);
    cone.position.set(x, 0.5, 0.25);
    group.add(cone);
    const base = new THREE.Mesh(
      new THREE.BoxGeometry(0.43, 0.07, 0.34),
      new THREE.MeshStandardMaterial({ color: "#ffb36b" }),
    );
    base.position.set(x, 0.2, 0.25);
    group.add(base);
  }
  const hazard = new THREE.Mesh(
    new THREE.CircleGeometry(0.42, 40),
    new THREE.MeshStandardMaterial({ color: "#73e7d4", transparent: true, opacity: 0.62, roughness: 0.4 }),
  );
  hazard.rotation.x = -Math.PI / 2;
  hazard.position.set(0, 0.21, 0.35);
  group.add(hazard);
  const rail = new THREE.Mesh(
    new THREE.BoxGeometry(1.9, 0.08, 0.08),
    new THREE.MeshStandardMaterial({ color: "#d8edf3", metalness: 0.42 }),
  );
  rail.position.set(0, 0.72, 0.35);
  group.add(rail);
  return group;
}

function createDeliveryStation() {
  const group = new THREE.Group();
  const colors = ["#7188ba", "#9d86c8", "#668f87"];
  for (let index = 0; index < 3; index += 1) {
    const crate = new THREE.Mesh(
      new THREE.BoxGeometry(0.58, 0.46, 0.48),
      new THREE.MeshStandardMaterial({ color: colors[index], roughness: 0.78 }),
    );
    crate.position.set((index - 1) * 0.62, 0.44 + (index === 1 ? 0.35 : 0), 0.18);
    crate.userData.swap = { file: "props/cardboard_box_01.glb", size: [0.58, 0.46, 0.48], rot: index === 1 ? 0.25 : 0 };
    group.add(crate);
    const label = new THREE.Mesh(
      new THREE.BoxGeometry(0.24, 0.13, 0.025),
      new THREE.MeshBasicMaterial({ color: index === 1 ? "#ffb36b" : "#d8edf3" }),
    );
    label.position.set((index - 1) * 0.62, crate.position.y, -0.07);
    group.add(label);
  }
  const clipboard = new THREE.Mesh(
    new THREE.BoxGeometry(0.48, 0.62, 0.045),
    new THREE.MeshStandardMaterial({ color: "#d6e1ea", roughness: 0.84 }),
  );
  clipboard.position.set(1.22, 0.82, 0.08);
  clipboard.rotation.z = -0.18;
  group.add(clipboard);
  return group;
}

function createConsentStation() {
  const group = new THREE.Group();
  const table = new THREE.Mesh(
    new THREE.CylinderGeometry(0.92, 0.94, 0.13, 48),
    new THREE.MeshStandardMaterial({ color: "#315365", roughness: 0.62 }),
  );
  table.position.y = 0.86;
  group.add(table);
  const stem = new THREE.Mesh(
    new THREE.CylinderGeometry(0.12, 0.2, 0.8, 20),
    new THREE.MeshStandardMaterial({ color: "#91aabd", metalness: 0.3 }),
  );
  stem.position.y = 0.42;
  group.add(stem);
  const form = new THREE.Mesh(
    new THREE.BoxGeometry(0.4, 0.05, 0.56),
    new THREE.MeshStandardMaterial({ color: "#e5edf4", roughness: 0.9 }),
  );
  form.position.set(-0.2, 0.98, 0.16);
  form.rotation.y = -0.18;
  group.add(form);
  const pen = new THREE.Mesh(
    new THREE.CylinderGeometry(0.025, 0.025, 0.4, 10),
    new THREE.MeshStandardMaterial({ color: "#73e7d4", metalness: 0.28 }),
  );
  pen.position.set(0.36, 1.0, 0.12);
  pen.rotation.z = Math.PI / 2.8;
  group.add(pen);
  return group;
}

function createSampleStation() {
  const group = new THREE.Group();
  const tray = new THREE.Mesh(
    new THREE.BoxGeometry(1.55, 0.12, 0.72),
    new THREE.MeshStandardMaterial({ color: "#42536a", roughness: 0.74 }),
  );
  tray.position.y = 0.66;
  group.add(tray);
  for (let index = 0; index < 3; index += 1) {
    const vial = new THREE.Mesh(
      new THREE.CylinderGeometry(0.12, 0.11, 0.48, 20),
      new THREE.MeshPhysicalMaterial({
        color: ["#84cfba", "#90aef2", "#dbab78"][index],
        transparent: true,
        opacity: 0.74,
        roughness: 0.22,
        transmission: 0.12,
      }),
    );
    vial.position.set((index - 1) * 0.43, 0.99, 0.1);
    group.add(vial);
    const cap = new THREE.Mesh(
      new THREE.CylinderGeometry(0.13, 0.13, 0.1, 18),
      new THREE.MeshStandardMaterial({ color: "#d9e3eb", metalness: 0.28 }),
    );
    cap.position.set((index - 1) * 0.43, 1.28, 0.1);
    group.add(cap);
    const tag = new THREE.Mesh(
      new THREE.BoxGeometry(0.18, 0.1, 0.02),
      new THREE.MeshBasicMaterial({ color: index === 1 ? "#ffb36b" : "#73e7d4" }),
    );
    tag.position.set((index - 1) * 0.43, 1.0, -0.02);
    group.add(tag);
  }
  return group;
}

function createFieldStation() {
  const group = new THREE.Group();
  const caseBody = new THREE.Mesh(
    new THREE.BoxGeometry(0.82, 0.28, 0.58),
    new THREE.MeshStandardMaterial({ color: "#4b6478", roughness: 0.48, metalness: 0.12 }),
  );
  caseBody.position.set(-0.2, 0.6, 0.1);
  group.add(caseBody);
  const screen = new THREE.Mesh(
    new THREE.BoxGeometry(0.4, 0.19, 0.025),
    new THREE.MeshBasicMaterial({ color: "#73e7d4" }),
  );
  screen.position.set(-0.2, 0.62, -0.205);
  group.add(screen);
  const pole = new THREE.Mesh(
    new THREE.CylinderGeometry(0.035, 0.045, 1.2, 12),
    new THREE.MeshStandardMaterial({ color: "#c5d4de", metalness: 0.48 }),
  );
  pole.position.set(0.72, 0.68, 0.2);
  group.add(pole);
  const sensor = new THREE.Mesh(
    new THREE.SphereGeometry(0.16, 20, 14),
    new THREE.MeshStandardMaterial({ color: "#ffb36b", metalness: 0.3 }),
  );
  sensor.position.set(0.72, 1.32, 0.2);
  group.add(sensor);
  const marker = new THREE.Mesh(
    new THREE.RingGeometry(0.2, 0.27, 24),
    new THREE.MeshBasicMaterial({ color: "#73e7d4", side: THREE.DoubleSide }),
  );
  marker.rotation.x = -Math.PI / 2;
  marker.position.set(0.72, 0.22, 0.2);
  group.add(marker);
  return group;
}

const stationBuilders = {
  counter: createCounter,
  safety: createSafetyStation,
  delivery: createDeliveryStation,
  consent: createConsentStation,
  sample: createSampleStation,
  field: createFieldStation,
  height: createHeightStation,
  control: createControlRoomStation,
  battery: createBatteryStation,
  facade: createFacadeStation,
  welding: createWeldingStation,
  electrical: createElectricalStation,
  lighters: createLightersStation,
  hydrotest: createHydrotestStation,
  spool: createSpoolStation,
  bench: createResistorBench,
  circulation: createCirculationStation,
};

function clearObject(group) {
  if (!group) return;
  group.traverse((child) => {
    if (child.isSprite) {
      child.material.map?.dispose();
      child.material.dispose();
      return;
    }
    if (child.isLine) {
      child.geometry.dispose();
      child.material.dispose();
      return;
    }
    // Every skinned character owns a bone texture (made on its first draw) that is not shared through the model cache.
    // Without this each visit to a scenario with people leaked a few GPU textures (found in round 2 with renderer.info.memory).
    if (child.isSkinnedMesh) child.skeleton?.dispose();
    if (!child.isMesh || child.userData.sharedAsset) return; // glTF models are shared through the cache (dressing.js)
    child.geometry.dispose();
    if (Array.isArray(child.material)) child.material.forEach((material) => material.dispose());
    else {
      child.material.map?.dispose();
      child.material.dispose();
    }
  });
  group.parent?.remove(group);
}

function setTrainingScene(scenario, stepIndex = 0) {
  skyWanted = !scenario || outdoorStations.has(scenario.object); // the menu, or an outdoor work area
  clearObject(scenarioModel);
  scenarioModel = stationBuilders[scenario?.object]?.() ?? createCounter();
  decorateCoreStation(scenarioModel, scenario?.object);
  // A procedural room (walls, floor, fittings) behind indoor work areas; a ground colour for outdoor ones.
  const room = scenario ? createRoom(ROOM_BY_STATION[scenario.object]) : null;
  if (room) {
    room.traverse((node) => { node.userData.noShadowCast = true; });
    scenarioModel.add(room);
  }
  const ground = GROUND_BY_STATION[scenario?.object];
  floor.material.color.set(ground?.color ?? DEFAULT_GROUND);
  floor.material.roughness = ground?.rough ?? 0.82;
  scenarioModel.position.set(0, 0.2, -0.28);
  enableShadows(scenarioModel);
  world.add(scenarioModel);
  // Optional realism pass: real props and people load asynchronously; the primitives above are the fallback.
  const dressed = scenarioModel;
  clearDressing();
  dressStation(dressed, scenario?.object).then(() => { if (dressed === scenarioModel) enableShadows(dressed); });
  updateSign(scenario, stepIndex);
}

function updateSign(scenario, stepIndex = 0, text) {
  clearObject(questionPanel);
  questionPanel = null;
  if (!scenario) return;
  questionPanel = makeSign(
    [`${scenario.category} practice${scenario.draft ? " · draft for faculty review" : ""}`, text ?? scenario.steps[stepIndex]?.prompt ?? scenario.description],
    [0, 2.8, -1.0],
    [3.1, 0.92],
    categoryOf(scenario)?.accent ?? PITT.gold,
    460,
  );
  world.add(questionPanel);
}

// ---------------------------------------------------------------- "Why this matters"
// After a completed action that has a `why`, the reason appears briefly: a DOM panel on desktop (Esc or the button
// dismisses it) and, in VR, a backed in-world panel just below the scenario sign. Either one hides itself after a while.
const whyShowMs = 9000;
let whyTimer = 0;
let whyMesh = null;

function hideWhy() {
  clearTimeout(whyTimer);
  whyPanel.hidden = true;
  clearObject(whyMesh);
  whyMesh = null;
}

function showWhy(text) {
  hideWhy();
  whyText.textContent = text;
  whyPanel.hidden = false;
  const size = [2.7, 0.56];
  const height = 300;
  whyMesh = new THREE.Mesh(
    new THREE.PlaneGeometry(...size),
    new THREE.MeshBasicMaterial({
      map: textTexture(["Why this matters", text], { accent: PITT.gold, height, width: canvasWidthFor(height, size), size: 62 }),
      transparent: true,
      toneMapped: false,
      depthTest: false,
      side: THREE.DoubleSide,
    }),
  );
  whyMesh.renderOrder = 14; // above the hotspot labels it may touch
  whyMesh.position.set(0, 2.14, -0.92);
  whyMesh.userData.isTrainingUi = true;
  whyMesh.visible = renderer.xr.isPresenting;
  world.add(whyMesh);
  whyTimer = setTimeout(hideWhy, whyShowMs);
}

whyDismiss.addEventListener("click", hideWhy);
document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && !whyPanel.hidden) hideWhy();
});

function categoryOf(scenario) {
  return categories.find((category) => category.id === scenario?.category);
}

function renderCatalog() {
  categoryList.replaceChildren();
  const learner = learnerName || "Learner";
  for (const category of categories) {
    const section = document.createElement("div");
    section.className = "category";
    const heading = document.createElement("h3");
    const mark = document.createElement("span");
    mark.className = `category-mark ${category.id}-mark`;
    heading.append(mark, category.label);
    const note = document.createElement("p");
    note.className = "category-note";
    note.textContent = category.note;
    const list = document.createElement("div");
    list.className = "scenario-list";
    for (const scenario of scenarios.filter((item) => item.category === category.id)) {
      const button = document.createElement("button");
      button.type = "button";
      const result = completed.get(scenario.id);
      button.className = `scenario-card${result ? " completed" : ""}${result?.critical ? " critical-flag" : ""}`;
      button.dataset.scenario = scenario.id;
      const title = document.createElement("span");
      title.className = "scenario-title";
      title.textContent = scenario.title;
      if (scenario.draft) {
        const badge = document.createElement("span");
        badge.className = "draft-badge";
        badge.textContent = "Draft for faculty review";
        title.append(" ", badge);
      }
      const description = document.createElement("span");
      description.className = "scenario-description";
      description.textContent = scenario.description;
      const meta = document.createElement("span");
      meta.className = "scenario-meta";
      meta.textContent = result
        ? `Completed by ${learner} · tasks ${result.score}/${result.maxScore}${result.reasoning ? ` · reasoning: ${result.reasoning.bestAnswer ? "best answer" : "review"}` : ""}`
        : `${scenario.setting} · ${scenario.duration}`;
      button.style.setProperty("--accent", category.accent);
      button.append(thumbFor(scenario), title, description, meta);
      button.addEventListener("click", () => startScenario(scenario));
      list.append(button);
    }
    section.append(heading, note, list);
    categoryList.append(section);
  }
  progress.textContent = `${completed.size} / ${scenarios.length} complete`;
}

function showCatalog() {
  activeScenario = null;
  rationaleMode = false;
  hideWhy();
  document.querySelector("#panel").scrollTop = 0;
  task = null;
  taskTargets.length = 0;
  setCameraMode("overview");
  simulation.hidden = true;
  catalog.hidden = false;
  renderCatalog();
  moveFocus(catalog.querySelector("h2"));
  setTrainingScene(null);
  updateChoiceCards();
}

// ---------------------------------------------------------------- hands-on tasks
// A task is a set of hotspots placed on the 3D work area. The learner carries the task out by selecting them
// (mouse click, or a VR controller trigger). Wrong actions show their consequence; some actions change the scene.

const overviewView = { position: [0, 2.4, 8.5], target: [0, 1.2, 0] };
const taskView = { position: [0.1, 1.5, 6.0], target: [0, 0.95, -0.2] };
let cameraMode = "overview";
let flashUntil = 0;

function setCameraMode(mode) {
  if (cameraMode === mode) return;
  cameraMode = mode;
  const view = mode === "task" ? taskView : overviewView;
  camera.position.set(...view.position);
  controls.target.set(...view.target);
  controls.update();
}

const hotspotColors = { idle: "#FFB81C", done: "#4be3a0", wrong: "#ff6b6b" };

function labelSprite(text, color, status) {
  const sprite = tagSprite(text, { accent: color, status, worldHeight: 0.2 });
  sprite.renderOrder = 12;
  return sprite;
}

function setHotspotStatus(state, status) {
  state.status = status;
  const color = hotspotColors[status];
  state.core.material.color.set(color);
  state.halo.material.color.set(color);
  state.halo.material.opacity = status === "idle" ? 0.3 : 0.15;
  state.group.remove(state.label);
  state.label.material.map.dispose();
  state.label.material.dispose();
  state.label = labelSprite(state.def.label, color, status);
  state.label.position.set(...(state.labelOffset ?? [0, 0.24, 0]));
  state.group.add(state.label);
  if (state.button) {
    const hadFocus = document.activeElement === state.button;
    state.button.disabled = status !== "idle";
    // The state is also spoken: a done or wrong action is not only a colour change.
    state.button.textContent = status === "idle" ? state.def.label : `${state.def.label} (${status === "done" ? "done" : "wrong"})`;
    if (hadFocus && status !== "idle") {
      // A disabled button drops keyboard focus; hand it to the next action that is still available.
      const buttons = [...taskActions.querySelectorAll("button")];
      (buttons.slice(buttons.indexOf(state.button) + 1).find((b) => !b.disabled) ?? buttons.find((b) => !b.disabled))?.focus();
    }
  }
}

function createHotspot(def) {
  const group = new THREE.Group();
  group.position.set(...def.at);
  const core = new THREE.Mesh(
    new THREE.SphereGeometry(0.07, 16, 12),
    new THREE.MeshBasicMaterial({ color: hotspotColors.idle, depthTest: false }),
  );
  const halo = new THREE.Mesh(
    new THREE.SphereGeometry(0.15, 16, 12),
    new THREE.MeshBasicMaterial({ color: hotspotColors.idle, transparent: true, opacity: 0.3, depthTest: false }),
  );
  core.renderOrder = 10;
  halo.renderOrder = 9;
  const state = { def, group, core, halo, label: new THREE.Group(), status: "idle", button: null };
  state.label.material = { map: { dispose() {} }, dispose() {} };
  halo.userData.hotspot = state;
  core.userData.hotspot = state;
  group.add(core, halo);
  setHotspotStatus(state, "idle");
  return state;
}

// Highest label centre in station coordinates: keeps hotspot labels clear of the scenario sign above the station.
const labelCeiling = 1.78;
const labelFloor = 0.18;

// Spread labels to the sides and stack them so neighbouring hotspots stay readable; leader lines tie each label back.
function layoutLabels(states) {
  const placed = [];
  for (const state of [...states].sort((a, b) => b.def.at[1] - a.def.at[1])) {
    const [x, y] = state.def.at;
    const width = state.label.userData.worldWidth ?? 1.1;
    const preferred = Math.max(x > 0.55 || (x < -0.25) ? -0.7 : 0, -1.3 - x); // keep labels on screen at the left edge
    const free = (cx, cy) => !placed.some((p) => Math.abs(p.x - cx) < (p.w + width) / 2 + 0.06 && Math.abs(p.y - cy) < 0.25);
    // Labels stay below the scenario sign (local y <= labelCeiling): stack upward first, then downward, then try the other side.
    const offsets = [0.25, 0.53, 0.81, 1.09, -0.25, -0.53, -0.81, -1.09, 1.37, 1.65];
    let dx = preferred;
    let dy = null;
    for (const side of [preferred, preferred === 0 ? 0.75 : -preferred * 0.6, 0]) {
      dy = offsets.find((o) => y + o <= labelCeiling && y + o >= labelFloor && free(x + side, y + o)) ?? null;
      if (dy !== null) { dx = side; break; }
    }
    if (dy === null) dy = Math.min(0.25, labelCeiling - y); // crowded: overlap rather than cover the sign
    placed.push({ x: x + dx, y: y + dy, w: width });
    state.labelOffset = [dx, dy, 0];
    state.label.position.set(dx, dy, 0);
    const leader = new THREE.Line(
      new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0), new THREE.Vector3(dx, dy - 0.13, 0)]),
      new THREE.LineBasicMaterial({ color: hotspotColors.idle, transparent: true, opacity: 0.6, depthTest: false }),
    );
    leader.renderOrder = 8;
    state.group.add(leader);
  }
}

function addLine(parent, from, to, color) {
  const start = new THREE.Vector3(...from);
  const end = new THREE.Vector3(...to);
  const line = new THREE.Mesh(
    new THREE.CylinderGeometry(0.014, 0.014, start.distanceTo(end), 8),
    new THREE.MeshBasicMaterial({ color }),
  );
  line.position.copy(start).lerp(end, 0.5);
  line.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), end.clone().sub(start).normalize());
  parent.add(line);
}

function logTask(text, kind) {
  const entry = document.createElement("li");
  entry.className = kind;
  entry.textContent = text;
  taskLog.append(entry);
  taskLog.scrollTop = taskLog.scrollHeight;
}

function renderTask(step) {
  stepCount.textContent = `Task ${currentStep + 1} of ${activeScenario.steps.length}`;
  decisionKicker.textContent = `${learnerName.toUpperCase()} · TASK ${currentStep + 1}`;
  decisionPrompt.textContent = step.prompt;
  choices.hidden = true;
  taskHint.hidden = false;
  taskLog.hidden = false;
  taskLog.replaceChildren();
  taskActionsWrap.hidden = false;
  taskActionsWrap.open = false;
  taskActions.replaceChildren();
  taskTargets.length = 0;
  setTrainingScene(activeScenario, currentStep);
  scenarioModel.userData.setState?.(step.onStart ?? "");
  setCameraMode("task");
  const states = step.hotspots.map(createHotspot);
  layoutLabels(states);
  for (const state of states) {
    enableShadows(state.group);
    scenarioModel.add(state.group);
    taskTargets.push(state.halo);
    const button = document.createElement("button");
    button.type = "button";
    button.className = "choice";
    button.dataset.hotspot = state.def.id;
    button.textContent = state.def.label;
    button.addEventListener("click", () => performHotspot(state));
    state.button = button;
    taskActions.append(button);
  }
  task = {
    step,
    states,
    done: new Set(),
    required: step.hotspots.filter((item) => item.correct).map((item) => item.id),
    mistakes: 0,
    critical: false,
    finished: false,
  };
  updateChoiceCards();
  moveFocus(currentStep === 0 ? simulationTitle : decisionPrompt);
}

function performHotspot(state) {
  if (!task || task.finished || state.status !== "idle") return;
  const def = state.def;
  const missing = (def.after ?? []).filter((id) => !task.done.has(id));
  if (def.correct && missing.length === 0) {
    task.done.add(def.id);
    setHotspotStatus(state, "done");
    logTask(`✓ ${def.label}: ${def.say}`, "good");
    const parts = scenarioModel.userData.reveal ?? {};
    if (def.reveal && parts[def.reveal]) parts[def.reveal].visible = true;
    if (def.hide && parts[def.hide]) parts[def.hide].visible = false;
    if (def.tether && scenarioModel.userData.worker) addLine(scenarioModel, scenarioModel.userData.worker, def.at, "#ff9d59");
    updateSign(activeScenario, currentStep, def.say);
    if (def.why) showWhy(def.why);
    if (task.required.every((id) => task.done.has(id))) finishTask();
    return;
  }
  task.mistakes += 1;
  const message = def.correct ? (def.early ?? "Not yet. Finish the earlier steps first.") : def.say;
  if (def.critical) {
    task.critical = true;
    flashUntil = performance.now() + 350;
  }
  if (!def.correct) setHotspotStatus(state, "wrong");
  logTask(`${def.critical ? "✗ CRITICAL" : "✗"} ${def.label}: ${message}`, def.critical ? "critical" : "bad");
  updateSign(activeScenario, currentStep, message);
}

function finishTask() {
  task.finished = true;
  const points = task.critical ? 0 : task.mistakes === 0 ? 2 : task.mistakes === 1 ? 1 : 0;
  totalScore += points;
  if (points < 2) missedPrinciples.push(task.step.principle);
  if (task.critical) criticalMisses += 1;
  for (const state of task.states) {
    if (state.status === "idle") {
      state.group.visible = false;
      if (state.button) state.button.disabled = true;
    }
  }
  answered = true;
  feedback.classList.toggle("critical", task.critical);
  const learningOnly = activeScenario.category === "biology";
  feedbackLabel.textContent = task.critical ? (learningOnly ? "KEY MISCONCEPTION" : activeScenario.category === "industrial" || activeScenario.category === "manufacturing" ? "CRITICAL SAFETY MISS" : "CRITICAL MISS") : points === 2 ? "STRONG PRACTICE" : "PAUSE AND REFLECT";
  feedbackCopy.textContent = task.critical
    ? learningOnly
      ? `Task complete, but with a key misconception (${task.mistakes} mistake${task.mistakes === 1 ? "" : "s"} in total). Review the principle below before the next task.`
      : `Task complete, but with a critical mistake (${task.mistakes} mistake${task.mistakes === 1 ? "" : "s"} in total). In real practice that could seriously harm someone or invalidate the work.`
    : task.mistakes === 0
      ? "Task complete with no mistakes."
      : `Task complete with ${task.mistakes} mistake${task.mistakes === 1 ? "" : "s"}.`;
  principle.textContent = `Remember: ${task.step.principle}`;
  continueButton.textContent = currentStep < activeScenario.steps.length - 1 ? "Next task" : activeScenario.rationale ? "Reasoning question" : "See your result";
  feedback.hidden = false;
  updateSign(activeScenario, currentStep, task.step.principle);
  updateChoiceCards();
  if (taskActions.contains(document.activeElement) || document.activeElement === document.body) continueButton.focus({ preventScroll: true });
}

// ---------------------------------------------------------------- reasoning question
// One multiple-choice question about WHY a step matters, after the last task. Three options, one best answer, and
// feedback for every option. The answer is recorded as a separate "Reasoning" result, never added to the task score.
function renderRationale() {
  const { question, options } = activeScenario.rationale;
  rationaleMode = true;
  answered = false;
  task = null;
  taskTargets.length = 0;
  hideWhy();
  document.querySelector("#panel").scrollTop = 0;
  feedback.hidden = true;
  rationaleReview.hidden = true;
  choices.hidden = false;
  taskHint.hidden = true;
  taskLog.hidden = true;
  taskActionsWrap.hidden = true;
  stepCount.textContent = "Reasoning question";
  decisionKicker.textContent = `${learnerName.toUpperCase()} · REASONING CHECK · NOT PART OF THE TASK SCORE`;
  decisionPrompt.textContent = question;
  choices.replaceChildren(...options.map((option, index) => {
    const button = document.createElement("button");
    button.className = "choice";
    button.type = "button";
    button.dataset.choice = String(index);
    button.textContent = option.text;
    button.addEventListener("click", () => chooseRationale(index));
    return button;
  }));
  setCameraMode("overview");
  setTrainingScene(activeScenario, currentStep);
  scenarioModel.visible = false; // a clear view of the three answer cards in VR
  updateSign(activeScenario, currentStep, question);
  updateChoiceCards();
}

function chooseRationale(index) {
  if (!activeScenario?.rationale || answered) return;
  const { options } = activeScenario.rationale;
  const option = options[index];
  if (!option) return;
  const best = options.find((item) => item.best);
  answered = true;
  reasoning = { chosen: index, best: option.best };
  for (const button of choices.querySelectorAll("button")) {
    button.disabled = true;
    button.classList.toggle("chosen", button.dataset.choice === String(index));
  }
  feedback.classList.remove("critical");
  feedback.classList.add("reasoning");
  feedbackLabel.textContent = option.best ? "REASONING · BEST ANSWER" : "REASONING · NOT THE BEST ANSWER";
  feedbackCopy.textContent = option.best ? option.feedback : `${option.feedback} The best answer: ${best.text}.`;
  principle.textContent = "Each option explained (this question is reported on its own, not added to your task score):";
  rationaleReview.replaceChildren(...options.map((item, i) => {
    const entry = document.createElement("li");
    entry.className = `${item.best ? "best" : ""}${i === index ? " chosen" : ""}`.trim();
    const head = document.createElement("strong");
    head.textContent = `${item.best ? "Best answer" : "Not the best"}${i === index ? " (your choice)" : ""}: ${item.text}`;
    entry.append(head, item.feedback);
    return entry;
  }));
  rationaleReview.hidden = false;
  continueButton.textContent = "See your result";
  feedback.hidden = false;
  feedback.scrollIntoView?.({ block: "nearest" });
  updateSign(activeScenario, currentStep, option.best ? option.feedback : `Best answer: ${best.text}. ${option.feedback}`);
  updateChoiceCards();
}

// Keyboard and screen-reader users: when the panel content changes, move focus to its new heading so they are not left on a removed control.
function moveFocus(element) {
  if (!element) return;
  element.setAttribute("tabindex", "-1");
  element.focus({ preventScroll: true });
}

function renderQuestion() {
  const step = activeScenario.steps[currentStep];
  hideWhy();
  rationaleReview.hidden = true;
  feedback.classList.remove("reasoning");
  if (step.hotspots) {
    renderTask(step);
    return;
  }
  task = null;
  taskTargets.length = 0;
  choices.hidden = false;
  taskHint.hidden = true;
  taskLog.hidden = true;
  taskActionsWrap.hidden = true;
  setCameraMode("overview");
  stepCount.textContent = `Decision ${currentStep + 1} of ${activeScenario.steps.length}`;
  decisionKicker.textContent = `${learnerName.toUpperCase()} · DECISION ${currentStep + 1}`;
  decisionPrompt.textContent = step.prompt;
  choices.replaceChildren();
  step.choices.forEach((choice, index) => {
    const button = document.createElement("button");
    button.className = "choice";
    button.type = "button";
    button.dataset.choice = String(index);
    button.textContent = choice.text;
    button.addEventListener("click", () => chooseAnswer(index));
    choices.append(button);
  });
  setTrainingScene(activeScenario, currentStep);
  updateChoiceCards();
  moveFocus(currentStep === 0 ? simulationTitle : decisionPrompt);
}

function startScenario(scenario) {
  activeScenario = scenario;
  document.querySelector("#panel").scrollTop = 0;
  currentStep = 0;
  totalScore = 0;
  answered = false;
  catalog.hidden = true;
  simulation.hidden = false;
  simulationCategory.textContent = categoryOf(scenario).heading;
  briefing.replaceChildren(...(scenario.briefing ?? []).map((item) => {
    const entry = document.createElement("li");
    entry.textContent = item;
    return entry;
  }));
  briefing.hidden = !scenario.briefing?.length;
  draftNote.hidden = !scenario.draft;
  criticalMisses = 0;
  missedPrinciples = [];
  rationaleMode = false;
  reasoning = null;
  reasoningResult.hidden = true;
  rationaleReview.hidden = true;
  hideWhy();
  review.hidden = true;
  simulationTitle.textContent = scenario.title;
  simulationBrief.textContent = scenario.description;
  task = null;
  decisionCard.hidden = false;
  feedback.hidden = true;
  completion.hidden = true;
  renderQuestion();
}

function chooseAnswer(index) {
  if (rationaleMode) {
    chooseRationale(index);
    return;
  }
  if (!activeScenario || answered) return;
  const choice = activeScenario.steps[currentStep]?.choices[index];
  if (!choice) return;
  answered = true;
  totalScore += choice.score;
  for (const button of choices.querySelectorAll("button")) button.disabled = true;
  const principleText = activeScenario.steps[currentStep].principle;
  if (choice.score < 2) missedPrinciples.push(principleText);
  if (choice.critical) criticalMisses += 1;
  feedback.classList.toggle("critical", Boolean(choice.critical));
  feedbackLabel.textContent = choice.critical ? "CRITICAL SAFETY MISS" : choice.score === 2 ? "STRONG PRACTICE" : "PAUSE AND REFLECT";
  feedbackCopy.textContent = choice.feedback;
  principle.textContent = `Remember: ${activeScenario.steps[currentStep].principle}`;
  continueButton.textContent = currentStep === activeScenario.steps.length - 1 ? "See your result" : "Next decision";
  feedback.hidden = false;
  setTrainingScene(activeScenario, currentStep);
  updateChoiceCards();
}

function finishScenario() {
  const maximum = activeScenario.steps.length * 2;
  rationaleMode = false;
  hideWhy();
  // The procedure score and the reasoning result are stored side by side and never combined.
  completed.set(activeScenario.id, {
    score: totalScore,
    maxScore: maximum,
    critical: criticalMisses > 0,
    reasoning: reasoning ? { bestAnswer: reasoning.best, chosen: reasoning.chosen } : null,
  });
  if (activeScenario.rationale && reasoning) {
    const { options } = activeScenario.rationale;
    const best = options.find((item) => item.best);
    reasoningCopy.textContent = reasoning.best
      ? `You chose the best answer: "${best.text}". This is recorded separately from your task score.`
      : `You chose "${options[reasoning.chosen].text}". The best answer was "${best.text}". This is recorded separately from your task score, as a learning check.`;
    reasoningResult.hidden = false;
  } else {
    reasoningResult.hidden = true;
  }
  decisionCard.hidden = true;
  feedback.hidden = true;
  feedback.classList.remove("reasoning");
  stepCount.textContent = "Complete";
  completion.hidden = false;
  completionTitle.textContent = `Nice work, ${learnerName}.`;
  const criticalNote = !criticalMisses
    ? ""
    : activeScenario.category === "biology"
      ? ` ${criticalMisses} task${criticalMisses === 1 ? " included a key misconception" : "s included key misconceptions"}; the principles below explain ${criticalMisses === 1 ? "it" : "them"}.`
      : ` ${criticalMisses} decision${criticalMisses === 1 ? " was a critical miss" : "s were critical misses"}; in real practice that could seriously harm someone or invalidate the work.`;
  completionCopy.textContent = `You scored ${totalScore} of ${maximum} task points across ${activeScenario.steps.length} tasks.${criticalNote} Review the principles, then try another scenario or replay this one.`;
  reviewList.replaceChildren(...missedPrinciples.map((item) => {
    const entry = document.createElement("li");
    entry.textContent = item;
    return entry;
  }));
  review.hidden = !missedPrinciples.length;
  moveFocus(completionTitle);
  renderCatalog();
  setTrainingScene(activeScenario);
  updateChoiceCards();
}

profile.addEventListener("submit", (event) => {
  event.preventDefault();
  learnerName = nameInput.value.trim().slice(0, 32);
  if (!learnerName) {
    welcome.textContent = "Enter your name to begin.";
    nameInput.focus();
    return;
  }
  nameInput.value = learnerName;
  profile.querySelector("button[type='submit']").disabled = true;
  nameInput.disabled = true;
  immersiveVrSupport.then((supported) => {
    if (supported && xrButton) xrButton.disabled = false;
  });
  welcome.textContent = `Welcome, ${learnerName}. Your name and progress stay in this browser session.`;
  document.querySelector("#panel").classList.add("is-started");
  catalog.hidden = false;
  renderCatalog();
});

function advanceStep() {
  if (!activeScenario || !answered) return;
  if (currentStep === activeScenario.steps.length - 1) {
    if (activeScenario.rationale && !rationaleMode) renderRationale();
    else finishScenario();
    return;
  }
  currentStep += 1;
  answered = false;
  feedback.hidden = true;
  renderQuestion();
}

continueButton.addEventListener("click", advanceStep);

retryButton.addEventListener("click", () => {
  if (activeScenario) startScenario(activeScenario);
});
finishButton.addEventListener("click", showCatalog);
backButton.addEventListener("click", showCatalog);

function createChoiceCard(index) {
  const material = new THREE.MeshBasicMaterial({
    map: textTexture([`Choice ${index + 1}`, "Start a practice scenario to load your decisions."], {
      accent: index === 1 ? PITT.gold : "#7fc4ff",
      width: 680,
      height: 384,
      size: 34,
    }),
    transparent: true,
    side: THREE.DoubleSide,
  });
  const card = new THREE.Mesh(new THREE.PlaneGeometry(1.15, 0.65), material);
  card.userData.choiceIndex = index;
  card.userData.isTrainingUi = true;
  return card;
}

function replaceCardTexture(card, lines, accent = PITT.gold, size = 34, height = 384) {
  card.material.map?.dispose();
  const { width: w, height: h } = card.geometry.parameters;
  card.material.map = textTexture(lines, {
    accent,
    width: canvasWidthFor(height, [w, h]),
    height,
    size,
  });
  card.material.needsUpdate = true;
}

for (let index = 0; index < 3; index += 1) {
  const card = createChoiceCard(index);
  card.position.set((index - 1) * 1.28, 1.15, -1.32);
  station.add(card);
  choiceTargets.push(card);
}

const advanceTarget = createChoiceCard(1);
advanceTarget.geometry.dispose();
advanceTarget.geometry = new THREE.PlaneGeometry(1.7, 0.78);
advanceTarget.position.set(0, 1.15, -1.32);
advanceTarget.visible = false;
station.add(advanceTarget);

for (let index = 0; index < scenarios.length; index += 1) {
  const scenario = scenarios[index];
  const card = createChoiceCard(index);
  card.geometry.dispose();
  card.geometry = new THREE.PlaneGeometry(0.9, 0.44);
  // The first 16 cards keep their 4 x 4 grid; any extra cards stack in a fifth column on the right.
  if (index < 16) card.position.set((index % 4 - 1.5) * 0.98, 2.3 - Math.floor(index / 4) * 0.48, -1.36);
  else card.position.set(2.5 * 0.98, 2.3 - (index - 16) * 0.48, -1.36);
  card.visible = false;
  card.userData.scenarioIndex = index;
  station.add(card);
  catalogTargets.push(card);
}

// Pitt-Bradford + CIST plaque above the scenario cards (the question sign carries the brand band in a scenario).
const catalogBrand = brandPlate([2.2, 0.4]);
catalogBrand.position.set(0, 2.92, -1.36);
station.add(catalogBrand);

function updateChoiceCards() {
  // In VR the scenario cards need a clear view, so the idle demo counter is hidden until a scenario starts.
  if (scenarioModel && !activeScenario) scenarioModel.visible = !renderer.xr.isPresenting;
  const step = activeScenario?.steps[currentStep];
  const rationale = rationaleMode ? activeScenario?.rationale : null;
  if (rationale && scenarioModel) scenarioModel.visible = false;
  choiceTargets.forEach((card, index) => {
    if (rationale) {
      // Reasoning question: the cards hold the options; once answered they move up and explain each option,
      // leaving room for the continue card below.
      const option = rationale.options[index];
      card.visible = true;
      card.position.y = answered ? 1.98 : 1.15;
      replaceCardTexture(card, answered
        ? [`${option.best ? "Best answer" : "Not the best"}${reasoning?.chosen === index ? " · your choice" : ""}`, `${option.text}: ${option.feedback}`]
        : [`Reasoning · option ${index + 1}`, option.text],
      answered ? (option.best ? "#4be3a0" : "#7fc4ff") : index === 1 ? PITT.gold : "#7fc4ff",
      answered ? 52 : 72); // larger than the default 34 px so the options read in a headset
      return;
    }
    const choice = step?.choices?.[index];
    card.visible = Boolean(activeScenario && !answered && !task);
    card.position.y = 1.15;
    replaceCardTexture(card, [
      `${learnerName || "Learner"} · CHOICE ${index + 1}`,
      choice?.text ?? "Start a practice scenario to load your decisions.",
    ], index === 1 ? PITT.gold : "#7fc4ff");
  });
  advanceTarget.visible = Boolean(activeScenario && answered && (!task || renderer.xr.isPresenting));
  if (advanceTarget.visible) {
    const last = activeScenario.steps.length - 1;
    const text = !completion.hidden ? "Return to scenario selection"
      : rationaleMode ? "See your result"
        : currentStep === last && activeScenario.rationale ? "Continue to the reasoning question"
          : currentStep === last ? "See your result" : "Continue to the next task";
    replaceCardTexture(advanceTarget, ["PRACTICE", text], PITT.gold, 40);
  }
  catalogTargets.forEach((card, index) => {
    const scenario = scenarios[index];
    card.visible = !activeScenario;
    // Thumbnail on the left (16:9, rounded), category/minutes and the title on the right.
    card.material.map?.dispose();
    const height = 400;
    card.material.map = thumbCardTexture({
      width: canvasWidthFor(height, [card.geometry.parameters.width, card.geometry.parameters.height]),
      height,
      kicker: `${scenario.category} · ${scenario.draft ? "draft" : scenario.duration}`,
      title: scenario.title,
      accent: categoryOf(scenario).accent,
      image: thumbImages.get(scenario.id) ?? null,
      size: 64,
      minSize: 40,
    });
    card.material.needsUpdate = true;
  });
  catalogBrand.visible = !activeScenario;
  if (seasonTag) seasonTag.visible = !activeScenario;
}

renderer.xr.addEventListener("sessionstart", () => {
  world.position.copy(vrWorldOffset);
  world.scale.setScalar(vrWorldScale);
  if (whyMesh) whyMesh.visible = true;
  updateChoiceCards();
});
renderer.xr.addEventListener("sessionend", () => {
  world.position.set(0, 0, 0);
  world.scale.setScalar(1);
  if (whyMesh) whyMesh.visible = false;
  updateChoiceCards();
});

const controllers = [];
for (let index = 0; index < 2; index += 1) {
  const controller = renderer.xr.getController(index);
  const ray = new THREE.Line(
    new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0), new THREE.Vector3(0, 0, -1)]),
    new THREE.LineBasicMaterial({ color: index === 0 ? "#7fc4ff" : PITT.gold }),
  );
  ray.scale.z = 4;
  controller.add(ray);
  scene.add(controller);
  controller.addEventListener("select", () => {
    raycaster.setFromXRController(controller);
    const targets = [
      ...taskTargets,
      ...choiceTargets.filter((card) => card.visible),
      ...catalogTargets.filter((card) => card.visible),
      ...(advanceTarget.visible ? [advanceTarget] : []),
    ];
    const hit = raycaster.intersectObjects(targets, false)[0];
    if (!hit) return;
    if (hit.object.userData.hotspot) {
      performHotspot(hit.object.userData.hotspot);
    } else if (hit.object === advanceTarget) {
      if (completion.hidden) advanceStep();
      else showCatalog();
    } else if (hit.object.userData.scenarioIndex !== undefined) {
      startScenario(scenarios[hit.object.userData.scenarioIndex]);
    } else if (activeScenario && !answered) {
      chooseAnswer(hit.object.userData.choiceIndex);
    }
  });
  controllers.push(controller);
}

let pointerDown = null;
renderer.domElement.addEventListener("pointerdown", (event) => {
  pointerDown = { x: event.clientX, y: event.clientY };
});

function pointerRay(event) {
  const rect = renderer.domElement.getBoundingClientRect();
  pointer.set(
    ((event.clientX - rect.left) / rect.width) * 2 - 1,
    -((event.clientY - rect.top) / rect.height) * 2 + 1,
  );
  raycaster.setFromCamera(pointer, camera);
}

renderer.domElement.addEventListener("click", (event) => {
  if (renderer.xr.isPresenting || !activeScenario) return;
  if (pointerDown && Math.hypot(event.clientX - pointerDown.x, event.clientY - pointerDown.y) > 6) return;
  pointerRay(event);
  if (task) {
    const hit = raycaster.intersectObjects(taskTargets, false)[0];
    if (hit) performHotspot(hit.object.userData.hotspot);
    return;
  }
  if (answered) return;
  const hit = raycaster.intersectObjects(choiceTargets, false)[0];
  if (hit) chooseAnswer(hit.object.userData.choiceIndex);
});

renderer.domElement.addEventListener("pointermove", (event) => {
  if (!task || renderer.xr.isPresenting) {
    canvas.style.cursor = "";
    return;
  }
  pointerRay(event);
  const over = raycaster.intersectObjects(taskTargets, false)[0]?.object.userData.hotspot;
  canvas.style.cursor = over && over.status === "idle" ? "pointer" : "";
});

let lastFrame = 0;
function animate(time) {
  const delta = Math.min(0.1, Math.max(0, (time - lastFrame) / 1000));
  lastFrame = time;
  // prefers-reduced-motion: people stand still, alarms and the swaying platform hold a fixed pose, halos do not pulse.
  if (!reducedMotion) tickDressing(delta);
  scenarioModel?.userData.tick?.(reducedMotion ? 0.5 : time / 1000);
  const flashing = !reducedMotion && performance.now() < flashUntil; // no full-screen flash with reduced motion; the red panel still reports a critical miss
  scene.background.set(flashing ? "#3a1218" : sceneColor);
  // The sky shows behind the menu and outdoor scenarios; it steps aside for the red critical-miss flash.
  sky.setEnabled(skyWanted && !flashing);
  sky.update(time, camera);
  if (task) {
    task.states.forEach((state, index) => {
      state.halo.scale.setScalar(state.status === "idle" && !reducedMotion ? 1 + 0.14 * Math.sin(time / 280 + index) : 1);
    });
  }
  if (!reducedMotion) platformRing.rotation.z += renderer.xr.isPresenting ? 0.0015 : 0.0007;
  controls.update();
  renderer.render(scene, camera);
}

// Read-only hooks for tools/vr-capture/realism-shots.mjs (triangle and draw-call budget, model load failures).
globalThis.__labStats = () => ({ ...renderer.info.render, memory: { ...renderer.info.memory }, missing: failedModels() });
// Triangles of each visible mesh in the current work area, largest first (tools/vr-capture/lab-qa.mjs --tris).
globalThis.__labTriangles = () => {
  const rows = [];
  scenarioModel?.traverse((node) => {
    if (!node.isMesh || !node.visible) return;
    for (let p = node.parent; p; p = p.parent) if (p.visible === false) return;
    const g = node.geometry;
    const tris = (g.index ? g.index.count : g.attributes.position.count) / 3;
    rows.push({ name: (() => { for (let p = node; p; p = p.parent) if (p.userData.dressName) return p.userData.dressName; return "station primitives"; })(), tris: Math.round(tris), shadow: node.castShadow });
  });
  return rows.sort((a, b) => b.tris - a.tris);
};
setTrainingScene(null);
updateChoiceCards();
renderer.setAnimationLoop(animate);
// Thumbnails arrive after the first draw: redraw the catalog (page and in-world cards) once they have loaded.
thumbsReady.then(() => {
  updateChoiceCards();
  if (!catalog.hidden) renderCatalog();
});
// Redraw every panel once the bundled fonts (and the optional local logo) have loaded.
brandAssetsReady.then(() => {
  updateChoiceCards();
  if (activeScenario) updateSign(activeScenario, currentStep);
});

// On wide screens the control panel covers the right side, so shift the scene toward the free area on the left.
// The shift and the field of view follow the real panel width (it narrows on tablets and small laptops), so the
// work area is never hidden behind the panel.
function frameScene() {
  const width = window.innerWidth;
  const height = window.innerHeight;
  camera.aspect = width / height;
  // Portrait phones: widen the view and lift the scene into the space above the bottom-sheet panel.
  camera.fov = width <= 700 && height > width ? 78 : 54;
  if (width > 700) {
    const panelEl = document.querySelector(".panel");
    const covered = Math.min(width * 0.7, (panelEl?.offsetWidth ?? 684) + 24); // panel plus its right margin
    const free = Math.max(width - covered - 24, 240);
    camera.setViewOffset(width, height, covered / 2 - 12, 0, width, height);
    // Show about 8 m of the work area across the free strip (the platform is 5 m wide, and its near edge is closer than the middle) (the camera is about 8.7 m away).
    const wantedHalfAngle = Math.atan(3.9 / (8.7 * Math.max(free / height, 0.3)));
    camera.fov = Math.min(82, Math.max(54, (2 * wantedHalfAngle * 180) / Math.PI));
  } else {
    camera.setViewOffset(width, height, 0, height * 0.24, width, height);
  }
  camera.updateProjectionMatrix();
}
frameScene();

window.addEventListener("resize", () => {
  frameScene();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

document.addEventListener("visibilitychange", () => {
  if (document.visibilityState === "visible") updateChoiceCards();
});
