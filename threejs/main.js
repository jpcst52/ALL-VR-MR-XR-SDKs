import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { VRButton } from "three/addons/webxr/VRButton.js";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { textPanelMesh, brandPlate } from "../brand-kit/panels.js";

const SKY_URL = "../assets/skies/summer-2k.jpg"; // Poly Haven "Spaichingen Hill", CC0 (see CREDITS-web-demos.md)
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const statusEl = document.querySelector("#status");

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 0.95;
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.xr.enabled = true;
document.body.appendChild(renderer.domElement);
document.body.appendChild(VRButton.createButton(renderer)); // placed top right by demo-ui/hud.css

const scene = new THREE.Scene();
scene.background = new THREE.Color("#12254a"); // shown only if the sky photo fails to load
const camera = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 0.05, 200);
const HOME = new THREE.Vector3(0, 2.1, window.innerWidth < window.innerHeight ? 9 : 5.6); // farther back on a portrait phone
camera.position.copy(HOME);

// Everything that belongs to the exhibit sits in `world`, so it can be moved in front of a standing VR player.
const world = new THREE.Group();
scene.add(world);

// --- Sky and image-based lighting -------------------------------------------------------------------------
const pmrem = new THREE.PMREMGenerator(renderer);
new THREE.TextureLoader().load(SKY_URL, (texture) => {
  texture.mapping = THREE.EquirectangularReflectionMapping;
  texture.colorSpace = THREE.SRGBColorSpace;
  scene.background = texture;
  scene.backgroundBlurriness = 0.02;
  scene.environment = pmrem.fromEquirectangular(texture).texture;
  scene.environmentIntensity = 1.1;
}, undefined, () => {
  // Fallback: a neutral room environment so the glass and brass still reflect something.
  import("three/addons/environments/RoomEnvironment.js").then(({ RoomEnvironment }) => {
    scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
  });
  statusEl.textContent = "The sky photo did not load; using a plain studio environment.";
});

// --- Lights ------------------------------------------------------------------------------------------------
scene.add(new THREE.HemisphereLight(0xdbeeff, 0x1b2a4a, 0.5));
const sun = new THREE.DirectionalLight(0xfff0d0, 2.6);
sun.position.set(-4, 7, 3.5);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
sun.shadow.camera.left = -4; sun.shadow.camera.right = 4; sun.shadow.camera.top = 4; sun.shadow.camera.bottom = -4;
sun.shadow.camera.near = 1; sun.shadow.camera.far = 20;
sun.shadow.radius = 5;
sun.shadow.bias = -0.0004; sun.shadow.normalBias = 0.02;
world.add(sun);

// --- Platform: a brass-rimmed disc with a soft radial pool of light --------------------------------------
function glowTexture(inner, outer) {
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = 256;
  const context = canvas.getContext("2d");
  const gradient = context.createRadialGradient(128, 128, 0, 128, 128, 128);
  gradient.addColorStop(0, inner);
  gradient.addColorStop(1, outer);
  context.fillStyle = gradient;
  context.fillRect(0, 0, 256, 256);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}
const platform = new THREE.Mesh(
  new THREE.CylinderGeometry(3.1, 3.25, 0.18, 96),
  new THREE.MeshStandardMaterial({ map: glowTexture("#2a4f9a", "#0a1d4a"), roughness: 0.55, metalness: 0.15 }),
);
platform.position.y = -0.09;
platform.receiveShadow = true;
world.add(platform);
const rim = new THREE.Mesh(
  new THREE.TorusGeometry(3.2, 0.045, 12, 128),
  new THREE.MeshStandardMaterial({ color: "#ffb81c", metalness: 1, roughness: 0.25 }),
);
rim.rotation.x = Math.PI / 2;
rim.position.y = 0.0;
world.add(rim);

// --- The prism: transmissive, iridescent glass around an emissive core -----------------------------------
const palette = [
  { name: "Violet", color: "#b8a5ff" },
  { name: "Teal", color: "#4fe0c8" },
  { name: "Amber", color: "#ffb36b" },
  { name: "Sky blue", color: "#7fb2ff" },
];
let paletteIndex = 0;
const prism = new THREE.Group();
prism.position.y = 1.35;
const glass = new THREE.Mesh(
  new THREE.IcosahedronGeometry(1.05, 1),
  new THREE.MeshPhysicalMaterial({
    color: palette[0].color, roughness: 0.06, metalness: 0, transmission: 0.92, thickness: 1.4, ior: 1.45,
    iridescence: 1, iridescenceIOR: 1.6, clearcoat: 1, flatShading: true, attenuationColor: new THREE.Color(palette[0].color), attenuationDistance: 1.6,
  }),
);
glass.castShadow = true;
const core = new THREE.Mesh(
  new THREE.IcosahedronGeometry(0.42, 2),
  new THREE.MeshStandardMaterial({ color: "#ffffff", emissive: palette[0].color, emissiveIntensity: 2.2, roughness: 0.4 }),
);
const coreLight = new THREE.PointLight(palette[0].color, 6, 7, 2);
prism.add(glass, core, coreLight);
world.add(prism);

const halo = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowTexture("rgba(255,255,255,.85)", "rgba(255,255,255,0)"), color: palette[0].color, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0.55 }));
halo.scale.setScalar(4.2);
halo.position.y = 1.35;
world.add(halo);

const ring = new THREE.Mesh(
  new THREE.TorusGeometry(2.1, 0.022, 10, 160),
  new THREE.MeshStandardMaterial({ color: "#dbeeff", emissive: "#dbeeff", emissiveIntensity: 0.9, roughness: 0.3 }),
);
ring.rotation.x = Math.PI / 2.4;
ring.position.y = 1.35;
world.add(ring);

const satelliteColors = [0xffa66b, 0x76e7d4, 0x8db7ff];
const satellites = satelliteColors.map((color, index) => {
  const group = new THREE.Group();
  const orb = new THREE.Mesh(new THREE.SphereGeometry(0.13, 28, 20), new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 1.6, roughness: 0.3 }));
  const glow = new THREE.Sprite(new THREE.SpriteMaterial({ map: glowTexture("rgba(255,255,255,.9)", "rgba(255,255,255,0)"), color, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0.8 }));
  glow.scale.setScalar(0.8);
  const light = new THREE.PointLight(color, 1.6, 4, 2);
  group.add(orb, glow, light);
  group.userData.phase = (index / 3) * Math.PI * 2;
  world.add(group);
  return group;
});

// --- Floating sparkles ------------------------------------------------------------------------------------
const sparkleCount = 220;
const sparkleGeometry = new THREE.BufferGeometry();
const sparklePositions = new Float32Array(sparkleCount * 3);
const sparkleSeeds = new Float32Array(sparkleCount);
const sparkleBaseY = new Float32Array(sparkleCount);
for (let i = 0; i < sparkleCount; i += 1) {
  const radius = 0.8 + Math.random() * 2.6, angle = Math.random() * Math.PI * 2;
  sparklePositions.set([Math.cos(angle) * radius, 0.1 + Math.random() * 3.2, Math.sin(angle) * radius], i * 3);
  sparkleSeeds[i] = Math.random() * 10;
  sparkleBaseY[i] = sparklePositions[i * 3 + 1];
}
sparkleGeometry.setAttribute("position", new THREE.BufferAttribute(sparklePositions, 3));
const sparkles = new THREE.Points(sparkleGeometry, new THREE.PointsMaterial({ map: glowTexture("rgba(255,255,255,1)", "rgba(255,255,255,0)"), size: 0.07, color: "#dbeeff", transparent: true, opacity: 0.8, blending: THREE.AdditiveBlending, depthWrite: false }));
world.add(sparkles);

// --- CC0 glTF props (fall back to simple primitives if a model fails to load) -----------------------------
function primitiveVase(color) {
  const mesh = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.16, 0.5, 24), new THREE.MeshStandardMaterial({ color, roughness: 0.4, metalness: 0.6 }));
  mesh.position.y = 0.25;
  const group = new THREE.Group();
  group.add(mesh);
  return group;
}
function placeProp(url, { x, z, scale, rotation = 0 }, fallbackColor) {
  const holder = new THREE.Group();
  holder.position.set(x, 0, z);
  holder.rotation.y = rotation;
  holder.scale.setScalar(scale);
  world.add(holder);
  new GLTFLoader().load(url, (gltf) => {
    gltf.scene.traverse((node) => { if (node.isMesh) { node.castShadow = true; node.receiveShadow = true; } });
    holder.add(gltf.scene);
  }, undefined, () => {
    console.warn(`Model ${url} did not load; using a primitive instead.`);
    const fallback = primitiveVase(fallbackColor);
    fallback.scale.setScalar(1 / scale);
    holder.add(fallback);
  });
}
placeProp("./assets/brass_vase_03/brass_vase_03_1k.gltf", { x: -2.2, z: 0.9, scale: 3.4, rotation: 0.6 }, "#c9a14a");
placeProp("./assets/ceramic_vase_04/ceramic_vase_04_1k.gltf", { x: 2.25, z: 0.55, scale: 3.4, rotation: -0.5 }, "#d9e4f5");

const plate = brandPlate([1.5, 0.32]);
plate.position.set(0, 0.34, 2.75);
plate.rotation.x = -0.6;
world.add(plate);

// VR-only help panel (the DOM controls are not visible inside a headset).
const vrHelp = textPanelMesh({ kicker: "Prism Bloom", body: "Press the trigger to change the glow. Walk around the platform.", accent: "#ffb81c", size: 44 }, [1.5, 0.5]);
vrHelp.position.set(0, 0.55, 2.2);
vrHelp.rotation.x = -0.45;
vrHelp.visible = false;
world.add(vrHelp);

// --- Interaction --------------------------------------------------------------------------------------------
function setGlow(index) {
  paletteIndex = (index + palette.length) % palette.length;
  const { name, color } = palette[paletteIndex];
  glass.material.color.set(color);
  glass.material.attenuationColor.set(color);
  core.material.emissive.set(color);
  coreLight.color.set(color);
  halo.material.color.set(color);
  statusEl.textContent = `Prism glow: ${name} (${paletteIndex + 1} of ${palette.length}).`;
}
const controls = new OrbitControls(camera, renderer.domElement);
controls.target.set(0, 1.25, 0);
controls.enableDamping = true;
controls.minDistance = 2.5;
controls.maxDistance = 11;
controls.maxPolarAngle = Math.PI * 0.49;
controls.autoRotate = !reducedMotion;
controls.autoRotateSpeed = 0.6;
controls.listenToKeyEvents(window);
controls.update();

let paused = reducedMotion;
const motionButton = document.querySelector("#toggle-motion");
function setPaused(value) {
  paused = value;
  controls.autoRotate = !paused;
  motionButton.textContent = paused ? "Resume motion" : "Pause motion";
  motionButton.setAttribute("aria-pressed", String(paused));
}
setPaused(paused);
function resetView() {
  camera.position.copy(HOME);
  controls.target.set(0, 1.25, 0);
  controls.update();
  statusEl.textContent = "View reset.";
}
document.querySelector("#change-look").addEventListener("click", () => setGlow(paletteIndex + 1));
motionButton.addEventListener("click", () => setPaused(!paused));
document.querySelector("#reset-view").addEventListener("click", resetView);
window.addEventListener("keydown", (event) => {
  if (event.target instanceof HTMLElement && event.target.closest("button, input, summary")) return;
  if (event.key === "c" || event.key === "C") setGlow(paletteIndex + 1);
  else if (event.key === " ") { event.preventDefault(); setPaused(!paused); }
  else if (event.key === "r" || event.key === "R") resetView();
});

// WebXR: stand the exhibit two metres in front of the player and let the trigger change the glow.
renderer.xr.addEventListener("sessionstart", () => {
  world.position.set(0, 0, -2.6);
  vrHelp.visible = true;
  statusEl.textContent = "VR session started.";
});
renderer.xr.addEventListener("sessionend", () => {
  world.position.set(0, 0, 0);
  vrHelp.visible = false;
});
for (let i = 0; i < 2; i += 1) {
  const controller = renderer.xr.getController(i);
  controller.addEventListener("selectstart", () => setGlow(paletteIndex + 1));
  const line = new THREE.Line(new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3(0, 0, -1.2)]), new THREE.LineBasicMaterial({ color: 0xffb81c }));
  controller.add(line);
  scene.add(controller);
}

const clock = new THREE.Clock();
let motionTime = 0;
renderer.setAnimationLoop(() => {
  const delta = Math.min(clock.getDelta(), 0.1);
  if (!paused) motionTime += delta;
  const t = motionTime;
  glass.rotation.set(t * 0.28, t * 0.45, t * 0.18);
  core.rotation.y = -t * 0.9;
  prism.position.y = 1.35 + Math.sin(t * 1.1) * 0.08;
  halo.position.y = prism.position.y;
  ring.rotation.z = t * 0.2;
  satellites.forEach((satellite) => {
    const angle = t * 0.35 + satellite.userData.phase;
    satellite.position.set(Math.cos(angle) * 1.65, 1.35 + Math.sin(angle * 1.7) * 0.42, Math.sin(angle) * 1.65);
  });
  const attribute = sparkleGeometry.attributes.position;
  for (let i = 0; i < sparkleCount; i += 1) {
    attribute.setY(i, 0.1 + ((sparkleBaseY[i] + t * (0.1 + sparkleSeeds[i] * 0.01)) % 3.3));
  }
  attribute.needsUpdate = true;
  controls.update();
  renderer.render(scene, camera);
});

window.addEventListener("resize", () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

statusEl.textContent = "Prism Bloom is ready. Drag to orbit, press C to change the glow.";
window.__demo = { ready: true, setGlow };
