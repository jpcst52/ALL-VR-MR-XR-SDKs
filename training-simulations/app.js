import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { VRButton } from "three/addons/webxr/VRButton.js";
import { scenarios } from "./scenarios.js";

const canvas = document.querySelector("#scene");
const profile = document.querySelector("#profile");
const nameInput = document.querySelector("#learner-name");
const welcome = document.querySelector("#welcome");
const catalog = document.querySelector("#catalog");
const simulation = document.querySelector("#simulation");
const progress = document.querySelector("#progress");
const businessList = document.querySelector("#business-list");
const researchList = document.querySelector("#research-list");
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
const completed = new Map();
let activeScenario = null;
let currentStep = 0;
let totalScore = 0;
let answered = false;
let learnerName = "";

const scene = new THREE.Scene();
scene.background = new THREE.Color("#0c131d");
scene.fog = new THREE.Fog("#0c131d", 8, 24);
const camera = new THREE.PerspectiveCamera(54, window.innerWidth / window.innerHeight, 0.1, 80);
camera.position.set(0, 2.4, 8.5);
camera.lookAt(0, 1.3, 0);

const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, alpha: false });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
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
    xrNote.textContent = "WebXR VR is unavailable in this browser. You can still complete all six simulations in desktop mode.";
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
scene.add(keyLight);

const floor = new THREE.Mesh(
  new THREE.CircleGeometry(7, 64),
  new THREE.MeshStandardMaterial({ color: "#152433", roughness: 0.88, metalness: 0.08 }),
);
floor.rotation.x = -Math.PI / 2;
floor.position.y = -0.02;
scene.add(floor);

const platform = new THREE.Mesh(
  new THREE.CylinderGeometry(2.4, 2.55, 0.14, 64),
  new THREE.MeshStandardMaterial({ color: "#20374a", roughness: 0.58, metalness: 0.14 }),
);
platform.position.y = 0.12;
scene.add(platform);

const platformRing = new THREE.Mesh(
  new THREE.TorusGeometry(2.08, 0.022, 8, 100),
  new THREE.MeshBasicMaterial({ color: "#73e7d4" }),
);
platformRing.rotation.x = Math.PI / 2;
platformRing.position.y = 0.205;
scene.add(platformRing);

const station = new THREE.Group();
scene.add(station);
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
};

function clearObject(group) {
  if (!group) return;
  group.traverse((child) => {
    if (!child.isMesh) return;
    child.geometry.dispose();
    if (Array.isArray(child.material)) child.material.forEach((material) => material.dispose());
    else {
      child.material.map?.dispose();
      child.material.dispose();
    }
  });
  scene.remove(group);
}

function setTrainingScene(scenario, stepIndex = 0) {
  clearObject(scenarioModel);
  scenarioModel = stationBuilders[scenario?.object]?.() ?? createCounter();
  scenarioModel.position.set(0, 0.2, -0.28);
  scene.add(scenarioModel);

  clearObject(questionPanel);
  questionPanel = null;
  const signColor = scenario?.category === "research" ? "#73e7d4" : "#ffb36b";
  if (scenario) {
    questionPanel = makeSign(
      [`${scenario.category} practice`, scenario.steps[stepIndex]?.prompt ?? scenario.description],
      [0, 2.35, -1.0],
      [3.1, 0.8],
      signColor,
      384,
    );
    scene.add(questionPanel);
  }
}

function renderCatalog() {
  businessList.replaceChildren();
  researchList.replaceChildren();
  const learner = learnerName || "Learner";
  for (const scenario of scenarios) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `scenario-card${completed.has(scenario.id) ? " completed" : ""}`;
    button.dataset.scenario = scenario.id;
    const title = document.createElement("span");
    title.className = "scenario-title";
    title.textContent = scenario.title;
    const description = document.createElement("span");
    description.className = "scenario-description";
    description.textContent = scenario.description;
    const meta = document.createElement("span");
    meta.className = "scenario-meta";
    const result = completed.get(scenario.id);
    meta.textContent = result
      ? `Completed by ${learner} · ${result.score}/${result.maxScore}`
      : `${scenario.setting} · ${scenario.duration}`;
    button.append(title, description, meta);
    button.addEventListener("click", () => startScenario(scenario));
    (scenario.category === "business" ? businessList : researchList).append(button);
  }
  progress.textContent = `${completed.size} / ${scenarios.length} complete`;
}

function showCatalog() {
  activeScenario = null;
  simulation.hidden = true;
  catalog.hidden = false;
  renderCatalog();
  setTrainingScene(null);
  updateChoiceCards();
}

function renderQuestion() {
  const step = activeScenario.steps[currentStep];
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
  simulationCategory.textContent = scenario.category === "business"
    ? "LOCAL BUSINESS ESSENTIALS"
    : "RESEARCH PRACTICE";
  simulationTitle.textContent = scenario.title;
  simulationBrief.textContent = scenario.description;
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
  feedbackLabel.textContent = choice.score === 2 ? "STRONG PRACTICE" : "PAUSE AND REFLECT";
  feedbackCopy.textContent = choice.feedback;
  principle.textContent = `Remember: ${activeScenario.steps[currentStep].principle}`;
  continueButton.textContent = currentStep === activeScenario.steps.length - 1 ? "See your result" : "Next decision";
  feedback.hidden = false;
  setTrainingScene(activeScenario, currentStep);
  updateChoiceCards();
}

function finishScenario() {
  const maximum = activeScenario.steps.length * 2;
  completed.set(activeScenario.id, { score: totalScore, maxScore: maximum });
  decisionCard.hidden = true;
  feedback.hidden = true;
  completion.hidden = false;
  completionTitle.textContent = `Nice work, ${learnerName}.`;
  completionCopy.textContent = `You scored ${totalScore} of ${maximum} points across ${activeScenario.steps.length} decisions. Review the principles, then try another scenario or replay this one.`;
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

function replaceCardTexture(card, lines, accent = "#73e7d4") {
  card.material.map?.dispose();
  card.material.map = textTexture(lines, {
    accent,
    width: 768,
    height: 384,
    size: 30,
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
  card.geometry = new THREE.PlaneGeometry(1.12, 0.68);
  card.position.set((index % 3 - 1) * 1.23, index < 3 ? 1.72 : 0.88, -1.36);
  card.visible = false;
  card.userData.scenarioIndex = index;
  station.add(card);
  catalogTargets.push(card);
}

function updateChoiceCards() {
  const step = activeScenario?.steps[currentStep];
  choiceTargets.forEach((card, index) => {
    const choice = step?.choices[index];
    card.visible = Boolean(activeScenario && !answered);
    replaceCardTexture(card, [
      `${learnerName || "Learner"} · CHOICE ${index + 1}`,
      choice?.text ?? "Start a practice scenario to load your decisions.",
    ], index === 1 ? "#ffb36b" : "#73e7d4");
  });
  advanceTarget.visible = Boolean(activeScenario && answered);
  if (advanceTarget.visible) {
    const text = completion.hidden ? "Continue to the next decision" : "Return to scenario selection";
    replaceCardTexture(advanceTarget, ["PRACTICE", text], "#ffb36b");
  }
  catalogTargets.forEach((card, index) => {
    const scenario = scenarios[index];
    card.visible = !activeScenario;
    replaceCardTexture(card, [
      `${scenario.category === "business" ? "BUSINESS" : "RESEARCH"} · ${scenario.duration}`,
      scenario.title,
    ], scenario.category === "business" ? "#ffb36b" : "#73e7d4");
  });
}

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
      ...choiceTargets.filter((card) => card.visible),
      ...catalogTargets.filter((card) => card.visible),
      ...(advanceTarget.visible ? [advanceTarget] : []),
    ];
    const hit = raycaster.intersectObjects(targets, false)[0];
    if (!hit) return;
    if (hit.object === advanceTarget) {
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

renderer.domElement.addEventListener("click", (event) => {
  if (renderer.xr.isPresenting || !activeScenario || answered) return;
  const rect = renderer.domElement.getBoundingClientRect();
  pointer.set(
    ((event.clientX - rect.left) / rect.width) * 2 - 1,
    -((event.clientY - rect.top) / rect.height) * 2 + 1,
  );
  raycaster.setFromCamera(pointer, camera);
  const hit = raycaster.intersectObjects(choiceTargets, false)[0];
  if (hit) chooseAnswer(hit.object.userData.choiceIndex);
});

function animate() {
  platformRing.rotation.z += renderer.xr.isPresenting ? 0.0015 : 0.0007;
  controls.update();
  renderer.render(scene, camera);
}

setTrainingScene(null);
updateChoiceCards();
renderer.setAnimationLoop(animate);

window.addEventListener("resize", () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

document.addEventListener("visibilitychange", () => {
  if (document.visibilityState === "visible") updateChoiceCards();
});
