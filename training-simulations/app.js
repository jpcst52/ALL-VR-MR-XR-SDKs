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

const canvas = document.querySelector("#scene");
const profile = document.querySelector("#profile");
const nameInput = document.querySelector("#learner-name");
const welcome = document.querySelector("#welcome");
const catalog = document.querySelector("#catalog");
const simulation = document.querySelector("#simulation");
const progress = document.querySelector("#progress");
const categoryList = document.querySelector("#category-list");
const briefing = document.querySelector("#briefing");
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

const scene = new THREE.Scene();
scene.background = new THREE.Color("#0c131d");
scene.fog = new THREE.Fog("#0c131d", 8, 24);
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
    xrNote.textContent = "WebXR VR is unavailable in this browser. You can still complete all sixteen simulations in desktop mode.";
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
  scene.environmentIntensity = 0.4;
  pmrem.dispose();
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
    node.castShadow = true;
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
  new THREE.MeshStandardMaterial({ color: "#1a2d40", roughness: 0.82, metalness: 0.08, map: noiseTexture(256, 215, 70, { repeat: 6, streaks: true }) }),
);
floor.receiveShadow = true;
floor.rotation.x = -Math.PI / 2;
floor.position.y = -0.02;
world.add(floor);

const platform = new THREE.Mesh(
  new THREE.CylinderGeometry(2.4, 2.55, 0.14, 64),
  new THREE.MeshStandardMaterial({ color: "#2a4a63", roughness: 0.5, metalness: 0.3, map: plateTexture(10) }),
);
platform.receiveShadow = true;
platform.castShadow = true;
platform.position.y = 0.12;
world.add(platform);

const platformRing = new THREE.Mesh(
  new THREE.TorusGeometry(2.08, 0.022, 8, 100),
  new THREE.MeshBasicMaterial({ color: "#73e7d4", toneMapped: false }),
);
platformRing.rotation.x = Math.PI / 2;
platformRing.position.y = 0.205;
world.add(platformRing);

const station = new THREE.Group();
world.add(station);
let questionPanel = null;
let scenarioModel = null;

function roundedRect(context, x, y, width, height, radius) {
  context.beginPath();
  context.roundRect(x, y, width, height, radius);
}

function textTexture(lines, { accent = "#73e7d4", width = 1024, height = 512, size = 40 } = {}) {
  const drawing = document.createElement("canvas");
  drawing.width = width;
  drawing.height = height;
  const context = drawing.getContext("2d");
  context.fillStyle = "#101a28";
  roundedRect(context, 2, 2, width - 4, height - 4, 34);
  context.fill();
  context.strokeStyle = accent;
  context.lineWidth = 6;
  context.stroke();
  context.textBaseline = "top";
  context.fillStyle = accent;
  context.font = `700 ${Math.round(size * 0.62)}px system-ui, sans-serif`;
  context.fillText(lines[0].slice(0, 35).toUpperCase(), 44, 34, width - 88);
  context.fillStyle = "#f2f6fc";
  context.font = `600 ${size}px system-ui, sans-serif`;
  const words = lines.slice(1).join(" ").split(/\s+/);
  const maxWidth = width - 90;
  let line = "";
  let y = 110;
  for (const word of words) {
    const nextLine = line ? `${line} ${word}` : word;
    if (context.measureText(nextLine).width > maxWidth && line) {
      context.fillText(line, 44, y, maxWidth);
      line = word;
      y += size * 1.26;
      if (y > height - size * 1.2) break;
    } else {
      line = nextLine;
    }
  }
  if (line && y <= height - size * 0.4) context.fillText(line, 44, y, maxWidth);
  const texture = new THREE.CanvasTexture(drawing);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

function makeSign(lines, position, scale, accent = "#73e7d4", height = 512) {
  const material = new THREE.MeshBasicMaterial({
    map: textTexture(lines, { accent, height }),
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
    if (!child.isMesh) return;
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
  clearObject(scenarioModel);
  scenarioModel = stationBuilders[scenario?.object]?.() ?? createCounter();
  decorateCoreStation(scenarioModel, scenario?.object);
  scenarioModel.position.set(0, 0.2, -0.28);
  enableShadows(scenarioModel);
  world.add(scenarioModel);
  updateSign(scenario, stepIndex);
}

function updateSign(scenario, stepIndex = 0, text) {
  clearObject(questionPanel);
  questionPanel = null;
  if (!scenario) return;
  questionPanel = makeSign(
    [`${scenario.category} practice`, text ?? scenario.steps[stepIndex]?.prompt ?? scenario.description],
    [0, 2.6, -1.0],
    [3.1, 0.8],
    categoryOf(scenario)?.accent ?? "#ffb36b",
    384,
  );
  world.add(questionPanel);
}

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
      const description = document.createElement("span");
      description.className = "scenario-description";
      description.textContent = scenario.description;
      const meta = document.createElement("span");
      meta.className = "scenario-meta";
      meta.textContent = result
        ? `Completed by ${learner} · ${result.score}/${result.maxScore}`
        : `${scenario.setting} · ${scenario.duration}`;
      button.append(title, description, meta);
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
  task = null;
  taskTargets.length = 0;
  setCameraMode("overview");
  simulation.hidden = true;
  catalog.hidden = false;
  renderCatalog();
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

const hotspotColors = { idle: "#73e7d4", done: "#4be3a0", wrong: "#ff6b6b" };

function labelSprite(text, color) {
  const drawing = document.createElement("canvas");
  drawing.width = 512;
  drawing.height = 112;
  const context = drawing.getContext("2d");
  context.fillStyle = "#0e1824ee";
  roundedRect(context, 3, 3, 506, 106, 22);
  context.fill();
  context.strokeStyle = color;
  context.lineWidth = 5;
  context.stroke();
  context.fillStyle = "#f2f6fc";
  context.font = "700 44px system-ui, sans-serif";
  context.textAlign = "center";
  context.textBaseline = "middle";
  context.fillText(text, 256, 58, 480);
  const texture = new THREE.CanvasTexture(drawing);
  texture.colorSpace = THREE.SRGBColorSpace;
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true, depthTest: false }));
  sprite.scale.set(1.2, 0.262, 1);
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
  const mark = status === "done" ? "✓ " : status === "wrong" ? "✗ " : "";
  state.label = labelSprite(`${mark}${state.def.label}`, color);
  state.label.position.set(...(state.labelOffset ?? [0, 0.24, 0]));
  state.group.add(state.label);
  if (state.button) state.button.disabled = status !== "idle";
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

// Spread labels to the sides and stack them so neighbouring hotspots stay readable; leader lines tie each label back.
function layoutLabels(states) {
  const placed = [];
  for (const state of [...states].sort((a, b) => b.def.at[1] - a.def.at[1])) {
    const [x, y] = state.def.at;
    const dx = Math.max(x > 0.55 || (x < -0.25) ? -0.7 : 0, -1.3 - x); // keep labels on screen at the left edge
    let dy = 0.25;
    for (let guard = 0; guard < 14 && placed.some((p) => Math.abs(p.x - (x + dx)) < 1.15 && Math.abs(p.y - (y + dy)) < 0.27); guard += 1) dy += 0.28;
    placed.push({ x: x + dx, y: y + dy });
    state.labelOffset = [dx, dy, 0];
    state.label.position.set(dx, dy, 0);
    const leader = new THREE.Line(
      new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0), new THREE.Vector3(dx, dy - 0.13, 0)]),
      new THREE.LineBasicMaterial({ color: "#73e7d4", transparent: true, opacity: 0.55, depthTest: false }),
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
  feedbackLabel.textContent = task.critical ? (activeScenario.category === "industrial" || activeScenario.category === "manufacturing" ? "CRITICAL SAFETY MISS" : "CRITICAL MISS") : points === 2 ? "STRONG PRACTICE" : "PAUSE AND REFLECT";
  feedbackCopy.textContent = task.critical
    ? `Task complete, but with a critical mistake (${task.mistakes} mistake${task.mistakes === 1 ? "" : "s"} in total). In real practice that could seriously harm someone or invalidate the work.`
    : task.mistakes === 0
      ? "Task complete with no mistakes."
      : `Task complete with ${task.mistakes} mistake${task.mistakes === 1 ? "" : "s"}.`;
  principle.textContent = `Remember: ${task.step.principle}`;
  continueButton.textContent = currentStep === activeScenario.steps.length - 1 ? "See your result" : "Next task";
  feedback.hidden = false;
  updateSign(activeScenario, currentStep, task.step.principle);
  updateChoiceCards();
}

function renderQuestion() {
  const step = activeScenario.steps[currentStep];
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
}

function startScenario(scenario) {
  activeScenario = scenario;
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
  criticalMisses = 0;
  missedPrinciples = [];
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
  completed.set(activeScenario.id, { score: totalScore, maxScore: maximum, critical: criticalMisses > 0 });
  decisionCard.hidden = true;
  feedback.hidden = true;
  completion.hidden = false;
  completionTitle.textContent = `Nice work, ${learnerName}.`;
  const criticalNote = criticalMisses
    ? ` ${criticalMisses} decision${criticalMisses === 1 ? " was a critical miss" : "s were critical misses"}; in real practice that could seriously harm someone or invalidate the work.`
    : "";
  completionCopy.textContent = `You scored ${totalScore} of ${maximum} points across ${activeScenario.steps.length} decisions.${criticalNote} Review the principles, then try another scenario or replay this one.`;
  reviewList.replaceChildren(...missedPrinciples.map((item) => {
    const entry = document.createElement("li");
    entry.textContent = item;
    return entry;
  }));
  review.hidden = !missedPrinciples.length;
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
  catalog.hidden = false;
  renderCatalog();
});

function advanceStep() {
  if (!activeScenario || !answered) return;
  if (currentStep === activeScenario.steps.length - 1) {
    finishScenario();
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
      accent: index === 1 ? "#ffb36b" : "#73e7d4",
      width: 768,
      height: 384,
      size: 31,
    }),
    transparent: true,
    side: THREE.DoubleSide,
  });
  const card = new THREE.Mesh(new THREE.PlaneGeometry(1.15, 0.65), material);
  card.userData.choiceIndex = index;
  card.userData.isTrainingUi = true;
  return card;
}

function replaceCardTexture(card, lines, accent = "#73e7d4", size = 30, height = 384) {
  card.material.map?.dispose();
  card.material.map = textTexture(lines, {
    accent,
    width: 768,
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
  card.position.set((index % 4 - 1.5) * 0.98, 2.3 - Math.floor(index / 4) * 0.48, -1.36);
  card.visible = false;
  card.userData.scenarioIndex = index;
  station.add(card);
  catalogTargets.push(card);
}

function updateChoiceCards() {
  // In VR the scenario cards need a clear view, so the idle demo counter is hidden until a scenario starts.
  if (scenarioModel && !activeScenario) scenarioModel.visible = !renderer.xr.isPresenting;
  const step = activeScenario?.steps[currentStep];
  choiceTargets.forEach((card, index) => {
    const choice = step?.choices?.[index];
    card.visible = Boolean(activeScenario && !answered && !task);
    replaceCardTexture(card, [
      `${learnerName || "Learner"} · CHOICE ${index + 1}`,
      choice?.text ?? "Start a practice scenario to load your decisions.",
    ], index === 1 ? "#ffb36b" : "#73e7d4");
  });
  advanceTarget.visible = Boolean(activeScenario && answered && (!task || renderer.xr.isPresenting));
  if (advanceTarget.visible) {
    const text = completion.hidden ? "Continue to the next decision" : "Return to scenario selection";
    replaceCardTexture(advanceTarget, ["PRACTICE", text], "#ffb36b");
  }
  catalogTargets.forEach((card, index) => {
    const scenario = scenarios[index];
    card.visible = !activeScenario;
    replaceCardTexture(card, [
      `${scenario.category} · ${scenario.duration}`,
      scenario.title,
    ], categoryOf(scenario).accent, 52, 444);
  });
}

renderer.xr.addEventListener("sessionstart", () => {
  world.position.copy(vrWorldOffset);
  world.scale.setScalar(vrWorldScale);
  updateChoiceCards();
});
renderer.xr.addEventListener("sessionend", () => {
  world.position.set(0, 0, 0);
  world.scale.setScalar(1);
  updateChoiceCards();
});

const controllers = [];
for (let index = 0; index < 2; index += 1) {
  const controller = renderer.xr.getController(index);
  const ray = new THREE.Line(
    new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0), new THREE.Vector3(0, 0, -1)]),
    new THREE.LineBasicMaterial({ color: index === 0 ? "#73e7d4" : "#ffb36b" }),
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

function animate(time) {
  scenarioModel?.userData.tick?.(time / 1000);
  scene.background.set(performance.now() < flashUntil ? "#3a1218" : "#0c131d");
  if (task) {
    task.states.forEach((state, index) => {
      state.halo.scale.setScalar(state.status === "idle" ? 1 + 0.14 * Math.sin(time / 280 + index) : 1);
    });
  }
  platformRing.rotation.z += renderer.xr.isPresenting ? 0.0015 : 0.0007;
  controls.update();
  renderer.render(scene, camera);
}

setTrainingScene(null);
updateChoiceCards();
renderer.setAnimationLoop(animate);

// On wide screens the control panel covers the right side, so shift the scene toward the left.
function frameScene() {
  camera.aspect = window.innerWidth / window.innerHeight;
  if (window.innerWidth > 900) {
    camera.setViewOffset(window.innerWidth, window.innerHeight, window.innerWidth * 0.23, 0, window.innerWidth, window.innerHeight);
  } else {
    camera.clearViewOffset();
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
