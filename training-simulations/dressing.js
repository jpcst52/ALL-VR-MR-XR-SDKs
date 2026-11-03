import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { clone as cloneSkinned } from "three/addons/utils/SkeletonUtils.js";

// Realism pass: CC0 glTF props and people (see ../assets/models/CREDITS.md) placed into the primitive dioramas.
// Everything here is optional. The stations are built from primitives first and work on their own; this module
// only adds to them once a model has loaded. If a file is missing or fails to parse, loadModel() resolves to null,
// the primitive stays (or the doll stays), and a single console warning is logged. Nothing here throws.

export const MODEL_BASE = new URL("../assets/models/", import.meta.url).href;

// People: Quaternius "Ultimate Animated Character Pack" (CC0). Props: Poly Haven (CC0).
// Clip names are matched by suffix ("Idle" matches "HumanArmature|Man_Idle"), never by exact string.
export const PEOPLE = {
  worker: { file: "people/Worker.glb", idle: "Idle", walk: "Walk" },
  suit: { file: "people/Man_in_Suit.glb", idle: "Idle", walk: "Walk" },
  man: { file: "people/Man_Long.glb", idle: "Idle", walk: "Walk" },
  woman: { file: "people/Woman.glb", idle: "Idle", walk: "Walk" },
  womanTank: { file: "people/Woman_Tank.glb", idle: "Idle", walk: "Walk" },
};

// Which character stands in for a doll, chosen by the doll's vest colour and a running index (so neighbours differ).
const ROLE_BY_VEST = {
  "#ff9d59": ["worker"], // supervisor, manager, operator in hi-vis
  "#ff7a59": ["worker"], // fire watch
  "#7ab6ff": ["woman", "man", "womanTank"], // visitor, participant, nearby people, coworker
  "#73e7d4": ["womanTank", "man"], // customer
};

const loader = new GLTFLoader();
const cache = new Map();
const failed = new Set();
let people = 0;

// Resolves to { scene, animations } or null. Never rejects. Cached, so each file is fetched and parsed once.
export function loadModel(file) {
  if (!cache.has(file)) {
    cache.set(
      file,
      loader
        .loadAsync(new URL(file, MODEL_BASE).href)
        .catch((error) => {
          failed.add(file);
          console.warn(`Model ${file} could not be loaded; the plain primitive stays. (${error?.message ?? error})`);
          return null;
        }),
    );
  }
  return cache.get(file);
}

export function failedModels() {
  return [...failed];
}

// Natural size of a loaded scene (metres), measured once on the un-posed original.
function sizeOf(scene) {
  if (!scene.userData.size) {
    scene.updateMatrixWorld(true);
    const box = new THREE.Box3().setFromObject(scene);
    scene.userData.size = box.getSize(new THREE.Vector3());
    scene.userData.min = box.min.clone();
    scene.userData.center = box.getCenter(new THREE.Vector3());
  }
  return scene.userData.size;
}

function markShared(root) {
  root.traverse((node) => {
    node.userData.sharedAsset = true; // clearObject() must not dispose geometry/materials shared through the cache
    if (node.isMesh && node.material) {
      for (const material of Array.isArray(node.material) ? node.material : [node.material]) {
        for (const key of ["map", "normalMap", "roughnessMap", "metalnessMap", "aoMap", "emissiveMap"]) {
          if (material[key]) material[key].anisotropy = 4;
        }
      }
    }
  });
}

// A prop standing on y = 0 of the station, centred on x/z, scaled so it is `h` metres tall (or `w` wide).
export function placeProp(station, loaded, { pos = [0, 0, 0], h, w, fit, rot = 0 }) {
  const model = loaded.scene.clone(true);
  const size = sizeOf(loaded.scene);
  const { min, center } = loaded.scene.userData;
  const holder = new THREE.Group();
  model.position.set(-center.x, -min.y, -center.z);
  holder.add(model);
  if (fit) {
    // Fit an exact box [w, h, d] (cardboard cartons replacing the placeholder crates).
    holder.scale.set(fit[0] / size.x, fit[1] / size.y, fit[2] / size.z);
  } else {
    holder.scale.setScalar(h ? h / size.y : w / size.x);
  }
  holder.position.set(...pos);
  holder.rotation.y = rot;
  markShared(holder);
  station.add(holder);
  return holder;
}

const animations = []; // { mixer } for the station currently shown

export function clearDressing() {
  animations.length = 0;
}

export function tickDressing(deltaSeconds) {
  for (const entry of animations) entry.mixer.update(deltaSeconds);
}

function findClip(clips, suffix) {
  return clips.find((clip) => clip.name.toLowerCase().endsWith(suffix.toLowerCase()));
}

// Put a character where a doll was. `anim` is "idle" or "walk"; `scale` shrinks people that stand on a raised deck
// in a miniature (scaffold, window cradle) so they still read against the primitives around them.
export async function placePerson(parent, roleKey, { pos, rot = 0, scale = 1, anim = "idle", speed = 1 }) {
  const spec = PEOPLE[roleKey];
  const loaded = await loadModel(spec.file);
  if (!loaded) return null;
  const root = cloneSkinned(loaded.scene);
  // Characters are authored about 1.7 m tall in metres already; normalise anyway so a re-export cannot change the size.
  const size = sizeOf(loaded.scene);
  const holder = new THREE.Group();
  holder.add(root);
  holder.scale.setScalar((1.72 / (size.y > 0.5 ? size.y : 1.72)) * scale);
  holder.position.set(...pos);
  holder.rotation.y = rot;
  markShared(holder);
  holder.traverse((node) => { if (node.isSkinnedMesh) node.frustumCulled = false; });
  const clip = findClip(loaded.animations, spec[anim]) ?? findClip(loaded.animations, "Idle");
  if (clip) {
    const mixer = new THREE.AnimationMixer(root);
    const action = mixer.clipAction(clip);
    action.timeScale = speed;
    action.play();
    mixer.update(Math.random() * clip.duration); // de-synchronise neighbours
    animations.push({ mixer });
  }
  parent.add(holder);
  return holder;
}

// Props per work area. Positions are station-local metres (y = 0 is the platform), kept out of the hotspot ring.
// `rot` turns a prop about y so its front faces the camera/work. `h` is the height in metres.
export const STATION_PROPS = {
  delivery: [
    { file: "props/hand_truck.glb", pos: [1.55, 0, -0.35], h: 1.15, rot: -0.5 },
    { file: "props/plastic_crate_01.glb", pos: [-1.0, 0, 0.75], h: 0.3, rot: 0.3 },
    { file: "props/steel_frame_shelves_01.glb", pos: [-1.8, 0, -0.35], h: 1.9, rot: 0 },
    { file: "props/wooden_crate_01.glb", pos: [1.0, 0, -0.55], h: 0.4, rot: 0.2 },
  ],
  safety: [
    { file: "props/WetFloorSign_01.glb", pos: [-0.35, 0, 0.9], h: 0.63, rot: 0.2 },
    { file: "props/plastic_broom.glb", pos: [0.95, 0, -0.45], h: 1.2, rot: 0 },
    { file: "props/plastic_jerrycan.glb", pos: [0.75, 0, 0.7], h: 0.34, rot: 0.5 },
    { file: "props/korean_fire_extinguisher_01.glb", pos: [1.7, 0, -0.3], h: 0.5 },
    { file: "props/medical_box.glb", pos: [-1.75, 0, -0.35], w: 0.45, rot: 0 },
  ],
  welding: [
    { file: "props/portable_welding_cart.glb", pos: [0.45, 0, -0.8], h: 1.0, rot: 0 },
    { file: "props/metal_toolbox.glb", pos: [0.0, 0, 0.6], h: 0.29, rot: 0.3 },
    { file: "props/worn_metal_rack.glb", pos: [1.85, 0, -0.55], h: 1.5, rot: -0.4 },
    { file: "props/propane_tank.glb", pos: [1.65, 0, 0.05], h: 0.55 },
  ],
  electrical: [
    { file: "props/power_box_01.glb", pos: [-1.45, 0, -0.3], h: 0.5, rot: 0.2 },
    { file: "props/utility_box_01.glb", pos: [1.6, 0, -0.45], h: 1.1, rot: -0.3 },
    { file: "props/metal_tool_chest.glb", pos: [-0.95, 0, 0.55], h: 0.65, rot: 0.2 },
  ],
  hydrotest: [
    { file: "props/Barrel_01.glb", pos: [-1.7, 0, 0.35], h: 0.88 },
    { file: "props/portable_generator.glb", pos: [1.5, 0, -0.5], h: 0.56, rot: -0.4 },
    { file: "props/tool_cart.glb", pos: [-1.55, 0, -0.55], h: 0.96, rot: 0.3 },
  ],
  counter: [
    { file: "props/CashRegister_01.glb", pos: [-0.45, 1.02, 0.05], h: 0.3, rot: 0.15 },
    { file: "props/steel_frame_shelves_01.glb", pos: [-1.95, 0, -0.5], h: 1.9 },
    { file: "props/plastic_crate_01.glb", pos: [1.3, 0, -0.1], h: 0.3, rot: -0.3 },
  ],
};

// Station-built meshes that a model replaces: tag a mesh with userData.swap = { file, size: [w, h, d] } and it is hidden
// once the model is in place; userData.hideWhenDressed hides a placeholder without replacement.
function swapPlaceholders(station, jobs) {
  const swaps = [];
  station.traverse((node) => {
    if (node.userData.swap) swaps.push(node);
    if (node.userData.hideWhenDressed) {
      // Hide the placeholder only if the prop that replaces it is really there (STATION_PROPS loads the same file).
      jobs.push(loadModel(node.userData.hideWhenDressed).then((loaded) => { if (loaded) node.visible = false; }));
    }
  });
  for (const node of swaps) {
    const { file, size } = node.userData.swap;
    jobs.push(loadModel(file).then((loaded) => {
      if (!loaded) return;
      node.visible = false;
      const p = node.position;
      placeProp(node.parent, loaded, { pos: [p.x, p.y - size[1] / 2, p.z], fit: size, rot: node.userData.swap.rot ?? 0 });
    }));
  }
}

// Replace each primitive doll (addPerson in industrial-stations.js) with a character, and add the station's props.
// Returns a promise that resolves when everything that could load has been placed. Safe to ignore.
export function dressStation(station, key) {
  clearDressing();
  const jobs = [];
  const dolls = [];
  station.traverse((node) => { if (node.userData.person) dolls.push(node); });
  const roleCount = {};
  for (const doll of dolls) {
    const { pos, vest, scale, anim, facing, speed } = doll.userData.person;
    const options = ROLE_BY_VEST[vest] ?? ["man"];
    const role = options[(roleCount[vest] = (roleCount[vest] ?? -1) + 1) % options.length];
    const toward = Math.atan2(-pos[0], 0.3 - pos[2]); // turn toward the work at the middle of the station
    const rot = facing ?? (Math.abs(pos[0]) < 0.01 && Math.abs(pos[2]) < 0.01 ? 0 : toward);
    jobs.push(
      placePerson(doll.parent, role, { pos, rot, scale: scale ?? 1, anim: anim ?? "idle", speed: speed ?? 1 }).then((placed) => {
        if (placed) doll.visible = false;
      }),
    );
  }
  for (const spec of STATION_PROPS[key] ?? []) {
    jobs.push(loadModel(spec.file).then((loaded) => {
      if (loaded && station.parent) placeProp(station, loaded, spec);
    }));
  }
  swapPlaceholders(station, jobs);
  return Promise.allSettled(jobs);
}
