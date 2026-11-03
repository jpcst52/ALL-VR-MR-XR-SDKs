// The room around the shared board: lighting, a generated floor and walls, and CC0 glTF props with primitive fallbacks.
// Everything loads from this repository (assets/models-mp, vendor/three); nothing is fetched from the network.
import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { clone as cloneSkinned } from "three/addons/utils/SkeletonUtils.js";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";
import { PITT } from "../../brand-kit/panels.js";
import { fallbackKind, layouts } from "./layouts.mjs";
import { loadWithFallback } from "./avatar-logic.mjs";

const manifestUrl = new URL("./models.json", import.meta.url);
const modelBase = new URL("../../assets/models-mp/", import.meta.url);
const loader = new GLTFLoader();
const templates = new Map();
let manifestPromise = null;

export function loadManifest() {
  manifestPromise ??= fetch(manifestUrl).then((response) => {
    if (!response.ok) throw new Error(`models.json: ${response.status}`);
    return response.json();
  });
  return manifestPromise;
}

// Resolves to a cached glTF template, or rejects. Callers go through loadWithFallback.
export function loadTemplate(kind, id) {
  const key = `${kind}:${id}`;
  if (!templates.has(key)) {
    templates.set(key, loadManifest().then((manifest) => {
      const entry = manifest[kind]?.[id];
      if (!entry) throw new Error(`Unknown ${kind} model "${id}"`);
      return loader.loadAsync(new URL(entry.file, modelBase).href).then((gltf) => ({ gltf, entry }));
    }));
    templates.get(key).catch(() => templates.delete(key)); // let a later attempt retry
  }
  return templates.get(key);
}

export { cloneSkinned };

// ---------------------------------------------------------------- generated surfaces

function noisyCanvas(size, base, speckle, count, alpha = 0.06) {
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const g = canvas.getContext("2d");
  g.fillStyle = base;
  g.fillRect(0, 0, size, size);
  for (let index = 0; index < count; index += 1) {
    g.fillStyle = `rgba(${speckle},${Math.random() * alpha})`;
    const w = 1 + Math.random() * 3;
    g.fillRect(Math.random() * size, Math.random() * size, w, w);
  }
  return { canvas, g };
}

function toTexture(canvas, repeatX, repeatY) {
  const map = new THREE.CanvasTexture(canvas);
  map.colorSpace = THREE.SRGBColorSpace;
  map.wrapS = map.wrapT = THREE.RepeatWrapping;
  map.repeat.set(repeatX, repeatY);
  map.anisotropy = 4;
  return map;
}

function floorTexture(kind, span) {
  if (kind === "tiles") {
    const { canvas, g } = noisyCanvas(512, "#c9d2dc", "40,52,70", 5000);
    g.strokeStyle = "#7f8ca0";
    g.lineWidth = 3;
    for (const p of [0, 256]) { g.strokeRect(p + 1.5, 1.5, 256, 256); g.strokeRect(p + 1.5, 257.5, 256, 254); }
    g.fillStyle = "rgba(0,53,148,0.08)";
    g.fillRect(256, 0, 256, 256);
    g.fillRect(0, 256, 256, 256);
    return toTexture(canvas, span / 2, span / 2);
  }
  if (kind === "asphalt") {
    const { canvas } = noisyCanvas(512, "#3c4046", "190,195,205", 9000, 0.22);
    return toTexture(canvas, span / 3, span / 3);
  }
  const { canvas, g } = noisyCanvas(512, "#8f949b", "40,44,50", 7000, 0.14);
  g.strokeStyle = "rgba(40,44,50,0.55)";
  g.lineWidth = 3;
  g.strokeRect(1.5, 1.5, 509, 509);
  return toTexture(canvas, span / 3, span / 3);
}

function wallTexture(kind, length) {
  const w = 512;
  const h = 1024; // 4 m wide x 4.2 m tall per repeat; a Pitt Royal band and gold line are painted in
  const canvas = document.createElement("canvas");
  canvas.width = w;
  canvas.height = h;
  const g = canvas.getContext("2d");
  const upper = kind === "corrugated" ? "#b8c0c9" : kind === "concrete" ? "#9a9fa6" : "#e6ebf0";
  g.fillStyle = upper;
  g.fillRect(0, 0, w, h);
  if (kind === "corrugated") {
    for (let x = 0; x < w; x += 16) {
      const grad = g.createLinearGradient(x, 0, x + 16, 0);
      grad.addColorStop(0, "rgba(0,0,0,0.20)");
      grad.addColorStop(0.5, "rgba(255,255,255,0.12)");
      grad.addColorStop(1, "rgba(0,0,0,0.20)");
      g.fillStyle = grad;
      g.fillRect(x, 0, 16, h);
    }
  }
  for (let index = 0; index < 5000; index += 1) {
    g.fillStyle = `rgba(30,40,60,${Math.random() * 0.05})`;
    g.fillRect(Math.random() * w, Math.random() * h, 1 + Math.random() * 3, 1 + Math.random() * 3);
  }
  const bottomOf = (metres) => h - (metres / 4.2) * h;
  g.fillStyle = PITT.navy; // dado (lower wall)
  g.fillRect(0, bottomOf(0.9), w, (0.9 / 4.2) * h);
  g.fillStyle = PITT.royal; // brand band
  g.fillRect(0, bottomOf(1.5), w, (0.4 / 4.2) * h);
  g.fillStyle = PITT.gold;
  g.fillRect(0, bottomOf(1.56), w, (0.06 / 4.2) * h);
  g.fillStyle = "rgba(0,0,0,0.18)"; // panel seams
  g.fillRect(0, 0, 3, h);
  return toTexture(canvas, length / 4, 1);
}

// ---------------------------------------------------------------- primitive fallbacks

// Rough size (w, h, d) and color for the primitive stand-in used when a model cannot be loaded.
const FALLBACK = {
  barrel: { size: [0.58, 0.9, 0.58], color: "#3e6fa8", round: true },
  tank: { size: [0.34, 0.55, 0.34], color: "#c9ced4", round: true },
  extinguisher: { size: [0.18, 0.55, 0.18], color: "#d12a2a", round: true },
  crate: { size: [0.8, 0.4, 0.4], color: "#a9824f" },
  cart: { size: [1.2, 0.9, 0.7], color: "#c33a2f" },
  tool: { size: [0.35, 0.3, 0.25], color: "#d9a21b" },
  pipe: { size: [1.4, 1.9, 0.3], color: "#7d8793" },
  lamp: { size: [0.5, 0.5, 0.5], color: "#d8dde3", round: true },
  generic: { size: [0.5, 0.5, 0.5], color: "#8b95a3" },
};

export function primitiveProp(id) {
  const spec = FALLBACK[fallbackKind(id)];
  const [w, h, d] = spec.size;
  const geometry = spec.round ? new THREE.CylinderGeometry(w / 2, w / 2, h, 16) : new THREE.BoxGeometry(w, h, d);
  const mesh = new THREE.Mesh(geometry, new THREE.MeshStandardMaterial({ color: spec.color, roughness: 0.6, metalness: 0.2 }));
  mesh.position.y = h / 2;
  const group = new THREE.Group();
  group.add(mesh);
  group.userData.fallback = true;
  return group;
}

// ---------------------------------------------------------------- the room

const benchMaterials = {
  top: new THREE.MeshStandardMaterial({ color: "#d9dee5", roughness: 0.45, metalness: 0.05 }),
  edge: new THREE.MeshStandardMaterial({ color: PITT.navy, roughness: 0.5 }),
  steel: new THREE.MeshStandardMaterial({ color: "#8a94a1", roughness: 0.35, metalness: 0.8 }),
};

// All benches are merged into one mesh per material (3 draw calls however many benches the layout has).
function buildBenches(items) {
  const parts = { top: [], edge: [], steel: [] };
  const matrix = new THREE.Matrix4();
  const add = (kind, geometry, x, y, z, item) => {
    geometry.translate(x, y, z);
    matrix.makeRotationY(THREE.MathUtils.degToRad(item.rot)).setPosition(item.x, 0, item.z);
    geometry.applyMatrix4(matrix);
    parts[kind].push(geometry);
  };
  for (const item of items) {
    add("top", new THREE.BoxGeometry(2.8, 0.05, 0.95), 0, 0.875, 0, item);
    add("edge", new THREE.BoxGeometry(2.8, 0.06, 0.03), 0, 0.84, 0.46, item);
    for (const sx of [-1.3, 1.3]) for (const sz of [-0.42, 0.42]) add("steel", new THREE.BoxGeometry(0.06, 0.85, 0.06), sx, 0.425, sz, item);
    add("steel", new THREE.BoxGeometry(2.6, 0.04, 0.8), 0, 0.25, 0, item);
  }
  const group = new THREE.Group();
  for (const [kind, geometries] of Object.entries(parts)) {
    if (!geometries.length) continue;
    const mesh = new THREE.Mesh(mergeGeometries(geometries), benchMaterials[kind]);
    mesh.castShadow = mesh.receiveShadow = true;
    group.add(mesh);
  }
  return group;
}

/**
 * Builds the room for a theme. The generated room appears immediately; props stream in and report through `onProgress`.
 * Returns { group, ready, stats, applyXr(inXr), setShadows(on) }.
 */
export function buildEnvironment({ themeId, mode, scene, renderer, shadows = true, withProps = true }) {
  const layout = layouts[themeId] ?? layouts.circuits;
  const group = new THREE.Group();
  group.name = `environment-${themeId}`;
  group.position.y = -0.09; // the shared board pad sits just above the floor
  scene.add(group);

  // Image-based light: small, generated in code, gives metal and plastic something to reflect.
  const pmrem = new THREE.PMREMGenerator(renderer);
  scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;
  scene.environmentIntensity = mode === "ar" ? 0.7 : 0.55;
  pmrem.dispose();

  const span = 40;
  const floor = new THREE.Mesh(
    new THREE.PlaneGeometry(span, span),
    new THREE.MeshStandardMaterial({ map: floorTexture(layout.floor, span), roughness: layout.floor === "tiles" ? 0.35 : 0.85, metalness: 0 }),
  );
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  group.add(floor);

  const { back, left, right } = layout.walls;
  const wallDefs = [
    { length: right - left, pos: [(left + right) / 2, 2.1, back], rotY: 0 },
    { length: 17, pos: [left, 2.1, back + 8.5], rotY: Math.PI / 2 },
    { length: 17, pos: [right, 2.1, back + 8.5], rotY: -Math.PI / 2 },
  ];
  for (const def of wallDefs) {
    const wall = new THREE.Mesh(
      new THREE.PlaneGeometry(def.length, 4.2),
      new THREE.MeshStandardMaterial({ map: wallTexture(layout.wall, def.length), roughness: 0.9 }),
    );
    wall.position.set(...def.pos);
    wall.rotation.y = def.rotY;
    wall.receiveShadow = true;
    group.add(wall);
  }

  if (layout.benches.length) group.add(buildBenches(layout.benches));

  const propList = withProps ? layout.props : [];
  const stats = { requested: propList.length, loaded: 0, fallbacks: 0 };
  const loads = propList.map(async (prop) => {
    const result = await loadWithFallback(
      async () => {
        const { gltf } = await loadTemplate("props", prop.model);
        const copy = gltf.scene.clone(true); // geometry and materials stay shared between copies
        copy.traverse((child) => { if (child.isMesh) { child.castShadow = true; child.receiveShadow = true; } });
        return copy;
      },
      () => primitiveProp(prop.model),
    );
    if (result.usedFallback) stats.fallbacks += 1; else stats.loaded += 1;
    const object = result.value;
    object.scale.setScalar(prop.scale ?? 1);
    object.position.set(prop.x, prop.y ?? 0, prop.z);
    object.rotation.y = THREE.MathUtils.degToRad(prop.rot ?? 0);
    object.userData.propId = prop.model;
    group.add(object);
  });
  const ready = Promise.all(loads).then(() => stats);

  return {
    group,
    stats,
    ready,
    layout,
    // The room is hidden in AR (the real room shows instead) and in VR (the board becomes a table-top model).
    applyXr(inXr) { group.visible = !inXr; },
    setShadows(on) { scene.traverse((child) => { if (child.isDirectionalLight) child.castShadow = on; }); },
  };
}
