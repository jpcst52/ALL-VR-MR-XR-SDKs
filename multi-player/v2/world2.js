// Lab v2 rooms: a lit workshop (Circuit Lab) and a safety bay (Site Safety), plus the board surface and the environment map.
// Everything is generated in code (canvas textures, merged geometry); the CC0 glTF props already in assets/models-mp are an
// optional extra on the desktop. In a headset the room is drawn too, but with a smaller set of objects and no shadow map.
import * as THREE from "three";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { PITT, brandPlate, fontsReady } from "../../brand-kit/panels.js";
import { loadTemplate, primitiveProp } from "../shared/world.js";
import { loadWithFallback } from "../shared/avatar-logic.mjs";
import { woodSet, concreteSet, wallSet, corrugatedSet, pegboardSet, brushedSet, matSet, bayFloorSet, labelTexture, refreshLabels, glowTexture, makeCanvas, rng } from "./tex.js";
import { MAT, SPRITE_GLOW } from "./parts2.js";

const shared = (m) => { m.userData.shared = true; return m; };
const std = (opts) => shared(new THREE.MeshStandardMaterial(opts));

// ------------------------------------------------------------------ geometry helpers
// Vertex-coloured batches (same classes as the parts), plus UV-scaled boxes and planes for tiled textures.
const m4 = new THREE.Matrix4(); const q = new THREE.Quaternion(); const e = new THREE.Euler(); const v3 = new THREE.Vector3(); const s3 = new THREE.Vector3(); const col = new THREE.Color();
class Merge {
  constructor() { this.groups = new Map(); }
  add(key, geometry, { pos = [0, 0, 0], rot = [0, 0, 0], scale = [1, 1, 1], color = null, uv = null } = {}) {
    if (geometry.index) geometry = geometry.toNonIndexed();
    m4.compose(v3.set(...pos), q.setFromEuler(e.set(...rot)), s3.set(...scale));
    geometry.applyMatrix4(m4);
    if (uv) { const a = geometry.attributes.uv; for (let i = 0; i < a.count; i += 1) a.setXY(i, a.getX(i) * uv[0], a.getY(i) * uv[1]); }
    if (color) {
      col.set(color);
      const n = geometry.attributes.position.count; const c = new Float32Array(n * 3);
      for (let i = 0; i < n; i += 1) { c[i * 3] = col.r; c[i * 3 + 1] = col.g; c[i * 3 + 2] = col.b; }
      geometry.setAttribute("color", new THREE.BufferAttribute(c, 3));
    }
    if (!this.groups.has(key)) this.groups.set(key, []);
    this.groups.get(key).push(geometry);
    return this;
  }
  box(key, w, h, d, pos, opts = {}) { return this.add(key, new THREE.BoxGeometry(w, h, d), { ...opts, pos }); }
  cyl(key, rt, rb, h, pos, opts = {}, seg = 16) { return this.add(key, new THREE.CylinderGeometry(rt, rb, h, seg), { ...opts, pos }); }
  build(materials, group, { shadow = true } = {}) {
    for (const [key, list] of this.groups) {
      const mesh = new THREE.Mesh(mergeGeometries(list), materials[key]);
      mesh.name = `room-${key}`; mesh.castShadow = shadow; mesh.receiveShadow = true;
      group.add(mesh);
      list.forEach((g) => g.dispose());
    }
  }
}

// ------------------------------------------------------------------ environment map
// A small "room" drawn once into a PMREM: a bright window, two warm ceiling strips, cool walls and a warm floor. It is only the
// reflection and ambient light source; the visible room is separate.
export function makeEnvironment(renderer, scene, themeId, mode = "vr") {
  const env = new THREE.Scene();
  const unlit = (r, g, b) => { const m = new THREE.MeshBasicMaterial({ side: THREE.BackSide }); m.color.setRGB(r, g, b); return m; };
  const shell = new THREE.Mesh(new THREE.BoxGeometry(24, 12, 24), unlit(themeId === "safety" ? 0.16 : 0.2, themeId === "safety" ? 0.2 : 0.27, 0.34));
  shell.position.y = 5; env.add(shell);
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(24, 24), unlit(0.28, 0.2, 0.14)); floor.rotation.x = -Math.PI / 2; floor.position.y = -0.9; env.add(floor);
  const panel = (w, h, pos, rot, r, g, b) => { const mesh = new THREE.Mesh(new THREE.PlaneGeometry(w, h), unlit(r, g, b)); mesh.material.side = THREE.DoubleSide; mesh.position.set(...pos); mesh.rotation.set(...rot); env.add(mesh); };
  panel(9, 5, [-11.6, 4.5, -2], [0, Math.PI / 2, 0], 3.4, 4.2, 5.4); // window: cool daylight, bright
  panel(10, 1.2, [-3, 10.8, -3], [Math.PI / 2, 0, 0], 4.6, 4.2, 3.6); // warm ceiling strips
  panel(10, 1.2, [4, 10.8, 3], [Math.PI / 2, 0, 0], 4.2, 3.9, 3.4);
  panel(6, 3, [6, 4, -11.6], [0, 0, 0], 1.6, 1.5, 1.2); // dim warm light on the back wall
  const pmrem = new THREE.PMREMGenerator(renderer);
  const texture = pmrem.fromScene(env, 0.03).texture;
  pmrem.dispose();
  env.traverse((o) => { if (o.isMesh) { o.geometry.dispose(); o.material.dispose(); } });
  scene.environment = texture;
  scene.environmentIntensity = mode === "ar" ? 0.7 : 0.8;
  return texture;
}

// ------------------------------------------------------------------ the board surface
export function buildBoardSurface(themeId) {
  const group = new THREE.Group();
  group.name = "board-surface";
  const edgeWood = woodSet({ size: 256, planks: 6, seed: 14, light: "#a9763c", dark: "#5e3a1a", repeat: [6, 1] });
  const edgeMat = std({ map: edgeWood.map, normalMap: edgeWood.normalMap, roughnessMap: edgeWood.roughnessMap, roughness: 1, metalness: 0 });
  const side = new THREE.Mesh(new THREE.CylinderGeometry(3.5, 3.5, 0.17, 96, 1, true), edgeMat);
  side.position.y = -0.005; side.castShadow = true; side.receiveShadow = true;
  group.add(side);
  const surface = themeId === "safety" ? bayFloorSet({ size: 1024 }) : matSet({ size: 1024 });
  const topMat = std({ map: surface.map, normalMap: surface.normalMap, roughnessMap: surface.roughnessMap ?? null, roughness: themeId === "safety" ? 1 : 0.8, metalness: 0, normalScale: new THREE.Vector2(0.3, 0.3) });
  const top = new THREE.Mesh(new THREE.CircleGeometry(3.5, 96), topMat);
  top.rotation.x = -Math.PI / 2; top.position.y = 0.081; top.receiveShadow = true;
  group.add(top);
  const brushed = brushedSet({ size: 256, repeat: [24, 1] });
  const rimMat = std({ map: brushed.map, normalMap: brushed.normalMap, roughnessMap: brushed.roughnessMap, roughness: 1, metalness: 1, color: "#e8eef5" });
  const rim = new THREE.Mesh(new THREE.TorusGeometry(3.5, 0.045, 10, 120), rimMat);
  rim.rotation.x = Math.PI / 2; rim.position.y = 0.083; rim.castShadow = true;
  group.add(rim);
  const goldRim = new THREE.Mesh(new THREE.TorusGeometry(3.5, 0.016, 6, 120), new THREE.MeshStandardMaterial({ color: PITT.gold, metalness: 0.9, roughness: 0.25 }));
  goldRim.userData.shared = true; goldRim.material.userData.shared = true;
  goldRim.rotation.x = Math.PI / 2; goldRim.position.y = 0.1;
  group.add(goldRim);
  // brand plaque on a short stand at the back edge
  const plaque = brandPlate([1.7, 0.36]);
  plaque.position.set(0, 0.46, -3.05);
  group.add(plaque);
  const post = new THREE.Mesh(new THREE.BoxGeometry(1.74, 0.05, 0.08), std({ color: "#0b1f4d", roughness: 0.45, metalness: 0.3 }));
  post.position.set(0, 0.2, -3.07);
  group.add(post);
  for (const x of [-0.8, 0.8]) {
    const leg = new THREE.Mesh(new THREE.CylinderGeometry(0.018, 0.018, 0.3, 8), MAT.chrome.clone());
    leg.material = std({ color: "#dfe6ee", metalness: 1, roughness: 0.2 });
    leg.position.set(x, 0.23, -3.07);
    group.add(leg);
  }
  return group;
}

// ------------------------------------------------------------------ window and light shafts
function skyTexture() {
  const canvas = makeCanvas(512, 320); const g = canvas.getContext("2d");
  const sky = g.createLinearGradient(0, 0, 0, 320);
  sky.addColorStop(0, "#6fb0f5"); sky.addColorStop(0.6, "#bfe0ff"); sky.addColorStop(1, "#f5fbff");
  g.fillStyle = sky; g.fillRect(0, 0, 512, 320);
  const random = rng(8);
  g.fillStyle = "rgba(255,255,255,0.75)";
  for (let i = 0; i < 9; i += 1) { const x = random() * 512; const y = 30 + random() * 130; for (let j = 0; j < 6; j += 1) { g.beginPath(); g.ellipse(x + j * 18, y + (random() - 0.5) * 8, 30 + random() * 22, 10 + random() * 7, 0, 0, Math.PI * 2); g.fill(); } }
  g.fillStyle = "#9fb7a0"; g.beginPath(); g.moveTo(0, 270); for (let x = 0; x <= 512; x += 32) g.lineTo(x, 250 + Math.sin(x * 0.03) * 12 + random() * 12); g.lineTo(512, 320); g.lineTo(0, 320); g.fill();
  g.fillStyle = "#6f8c76"; g.beginPath(); g.moveTo(0, 300); for (let x = 0; x <= 512; x += 24) g.lineTo(x, 285 + random() * 14); g.lineTo(512, 320); g.lineTo(0, 320); g.fill();
  const texture = new THREE.CanvasTexture(canvas); texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}
function shaftTexture() {
  const canvas = makeCanvas(64, 128); const g = canvas.getContext("2d");
  const img = g.createImageData(64, 128);
  for (let y = 0; y < 128; y += 1) for (let x = 0; x < 64; x += 1) {
    const edge = Math.sin((x / 63) * Math.PI) ** 1.5; const fade = (1 - y / 127) ** 1.2;
    const o = (y * 64 + x) * 4; img.data[o] = 255; img.data[o + 1] = 244; img.data[o + 2] = 214; img.data[o + 3] = edge * fade * 255;
  }
  g.putImageData(img, 0, 0);
  const texture = new THREE.CanvasTexture(canvas); texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}
function shaftGeometry(zCentre, width, top, bottom) {
  // a slanted quad from the window (top) to the floor (bottom); uv.v = 1 at the window end
  const geometry = new THREE.BufferGeometry();
  const h = width / 2;
  const p = [top.x, top.y, zCentre - h, top.x, top.y, zCentre + h, bottom.x, bottom.y, zCentre + h * 1.8, bottom.x, bottom.y, zCentre - h * 1.8];
  geometry.setAttribute("position", new THREE.Float32BufferAttribute(p, 3));
  geometry.setAttribute("uv", new THREE.Float32BufferAttribute([0, 0, 1, 0, 1, 1, 0, 1], 2));
  geometry.setIndex([0, 1, 2, 0, 2, 3]);
  return geometry;
}

// ------------------------------------------------------------------ the rooms
function textSign(w, h, draw) { return labelTexture(w, h, draw); }

function buildCircuitRoom(room, lod) {
  const group = room;
  const floorWood = woodSet({ size: 512, planks: 14, seed: 4, light: "#b58a58", dark: "#6e4a2a", repeat: [14, 14] });
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(42, 42), std({ map: floorWood.map, normalMap: floorWood.normalMap, roughnessMap: floorWood.roughnessMap, roughness: 1, metalness: 0, normalScale: new THREE.Vector2(0.8, 0.8) }));
  floor.rotation.x = -Math.PI / 2; floor.receiveShadow = true; floor.name = "room-floor";
  group.add(floor);

  // brand roundel on the floor: navy disc with a gold ring, under the board platform
  const roundel = makeCanvas(512); const g = roundel.getContext("2d");
  const grad = g.createRadialGradient(256, 256, 40, 256, 256, 256); grad.addColorStop(0, "#14346e"); grad.addColorStop(1, "#071a3f");
  g.fillStyle = grad; g.beginPath(); g.arc(256, 256, 250, 0, Math.PI * 2); g.fill();
  g.strokeStyle = PITT.gold; g.lineWidth = 8; g.beginPath(); g.arc(256, 256, 236, 0, Math.PI * 2); g.stroke();
  g.strokeStyle = "rgba(219,238,255,0.35)"; g.lineWidth = 3; g.beginPath(); g.arc(256, 256, 218, 0, Math.PI * 2); g.stroke();
  const roundelTex = new THREE.CanvasTexture(roundel); roundelTex.colorSpace = THREE.SRGBColorSpace; roundelTex.anisotropy = 4;
  const roundelMesh = new THREE.Mesh(new THREE.CircleGeometry(5.3, 64), shared(new THREE.MeshStandardMaterial({ map: roundelTex, roughness: 0.55, transparent: true, polygonOffset: true, polygonOffsetFactor: -1 })));
  roundelMesh.rotation.x = -Math.PI / 2; roundelMesh.position.y = 0.004; roundelMesh.receiveShadow = true;
  group.add(roundelMesh);

  // walls
  const walls = wallSet({ size: 512, seed: 9, repeat: [1, 1] });
  const wallMat = std({ map: walls.map, normalMap: walls.normalMap, roughnessMap: walls.roughnessMap, roughness: 1, metalness: 0, normalScale: new THREE.Vector2(0.5, 0.5) });
  const wallMerge = new Merge();
  wallMerge.add("wall", new THREE.PlaneGeometry(15, 4.2), { pos: [0, 2.1, -7.5], uv: [15 / 4, 1] });
  wallMerge.add("wall", new THREE.PlaneGeometry(15, 4.2), { pos: [-7.5, 2.1, 0], rot: [0, Math.PI / 2, 0], uv: [15 / 4, 1] });
  wallMerge.add("wall", new THREE.PlaneGeometry(15, 4.2), { pos: [7.5, 2.1, 0], rot: [0, -Math.PI / 2, 0], uv: [15 / 4, 1] });
  wallMerge.build({ wall: wallMat }, group, { shadow: false });

  // pegboard on the back wall, with tools hanging on it
  const peg = pegboardSet({ size: 512, holes: 16 });
  peg.map.repeat.set(7, 2); peg.normalMap.repeat.set(7, 2); peg.roughnessMap.repeat.set(7, 2);
  const pegMesh = new THREE.Mesh(new THREE.PlaneGeometry(7, 1.6), std({ map: peg.map, normalMap: peg.normalMap, roughnessMap: peg.roughnessMap, roughness: 1, metalness: 0 }));
  pegMesh.position.set(0, 2.55, -7.47); pegMesh.receiveShadow = true;
  group.add(pegMesh);

  const woodBench = woodSet({ size: 512, planks: 14, seed: 11, light: "#d8b27a", dark: "#9b6a38", repeat: [1, 1] });
  const benchWoodMat = std({ map: woodBench.map, normalMap: woodBench.normalMap, roughnessMap: woodBench.roughnessMap, roughness: 1, metalness: 0 });
  const merged = new Merge();
  const sc = (w, d) => [w / 3, d / 3];
  const benchAt = (x, z, w = 2.8, rot = 0) => {
    const o = { rot: [0, rot, 0] };
    const cs = Math.cos(rot); const sn = Math.sin(rot);
    const place = (lx, y, lz) => [x + lx * cs + lz * sn, y, z - lx * sn + lz * cs];
    merged.add("wood", new THREE.BoxGeometry(w, 0.06, 0.95), { pos: place(0, 0.9, 0), rot: [0, rot, 0], uv: sc(w, 0.95) });
    merged.add("wood", new THREE.BoxGeometry(w - 0.1, 0.03, 0.8), { pos: place(0, 0.28, 0), rot: [0, rot, 0], uv: sc(w, 0.8) });
    for (const sx of [-w / 2 + 0.08, w / 2 - 0.08]) for (const sz of [-0.4, 0.4]) merged.add("metal", new THREE.BoxGeometry(0.06, 0.88, 0.06), { pos: place(sx, 0.44, sz), rot: [0, rot, 0], color: "#516070" });
    merged.add("metal", new THREE.BoxGeometry(w - 0.1, 0.05, 0.05), { pos: place(0, 0.8, 0.4), rot: [0, rot, 0], color: "#516070" });
    return o;
  };
  const benches = [[-3.7, -6.9], [0, -6.9], [3.7, -6.9]];
  benches.forEach(([x, z]) => benchAt(x, z));
  benchAt(-6.9, -2.6, 2.8, Math.PI / 2);

  // things on the benches: parts bins, a soldering station, spools, a lamp
  const binColours = ["#c8261f", "#1a9f61", "#2f6fb5", "#f2c230", "#e8821e", "#7b2fa0"];
  binColours.forEach((c, i) => {
    merged.add("gloss", new THREE.BoxGeometry(0.34, 0.16, 0.24), { pos: [-4.9 + i * 0.37, 1.01, -7.1], color: "#dfe6ee" });
    merged.add("gloss", new THREE.BoxGeometry(0.3, 0.05, 0.2), { pos: [-4.9 + i * 0.37, 1.1, -7.1], color: c });
  });
  merged.add("metal", new THREE.CylinderGeometry(0.1, 0.12, 0.04, 16), { pos: [-0.9, 0.95, -6.9], color: "#8e99a8" });
  merged.add("matte", new THREE.BoxGeometry(0.34, 0.1, 0.24), { pos: [-0.9, 0.98, -7.0], color: "#1a2230" });
  merged.add("metal", new THREE.CylinderGeometry(0.01, 0.01, 0.3, 6), { pos: [-0.8, 1.1, -6.9], rot: [0, 0, 0.9], color: "#cfd6de" });
  const spoolColours = ["#e5532d", "#2f6fb5", "#f2c230", "#1a9f61"];
  spoolColours.forEach((c, i) => {
    const px = -1.9 + i * 0.5;
    merged.add("gloss", new THREE.CylinderGeometry(0.1, 0.1, 0.12, 20), { pos: [px + 3.9, 0.99, -6.7], color: c, rot: [Math.PI / 2, 0, 0] });
    merged.add("metal", new THREE.CylinderGeometry(0.13, 0.13, 0.015, 20), { pos: [px + 3.9, 0.99, -6.64], color: "#cfd6de", rot: [Math.PI / 2, 0, 0] });
    merged.add("metal", new THREE.CylinderGeometry(0.13, 0.13, 0.015, 20), { pos: [px + 3.9, 0.99, -6.76], color: "#cfd6de", rot: [Math.PI / 2, 0, 0] });
  });
  // tools on the pegboard
  const tool = (x, y, kind, color) => {
    if (kind === "driver") { merged.add("gloss", new THREE.CylinderGeometry(0.022, 0.025, 0.14, 10), { pos: [x, y + 0.05, -7.42], color }); merged.add("chrome", new THREE.CylinderGeometry(0.006, 0.006, 0.18, 6), { pos: [x, y - 0.1, -7.42], color: "#cfd6de" }); }
    if (kind === "pliers") { merged.add("matte", new THREE.BoxGeometry(0.04, 0.2, 0.025), { pos: [x - 0.02, y, -7.42], rot: [0, 0, 0.12], color }); merged.add("matte", new THREE.BoxGeometry(0.04, 0.2, 0.025), { pos: [x + 0.02, y, -7.42], rot: [0, 0, -0.12], color }); merged.add("chrome", new THREE.BoxGeometry(0.07, 0.12, 0.02), { pos: [x, y + 0.14, -7.42], color: "#cfd6de" }); }
    if (kind === "hammer") { merged.add("matte", new THREE.CylinderGeometry(0.015, 0.018, 0.34, 8), { pos: [x, y, -7.42], color: "#8a5a2b" }); merged.add("chrome", new THREE.BoxGeometry(0.16, 0.06, 0.05), { pos: [x, y + 0.2, -7.41], color: "#9aa5b2" }); }
    if (kind === "spool") { merged.add("gloss", new THREE.CylinderGeometry(0.1, 0.1, 0.06, 20), { pos: [x, y, -7.42], rot: [Math.PI / 2, 0, 0], color }); }
  };
  [[-2.7, 2.7, "driver", "#d9402c"], [-2.45, 2.7, "driver", "#f2c230"], [-2.2, 2.7, "driver", "#2f6fb5"], [-1.6, 2.65, "pliers", "#c8261f"], [-0.9, 2.65, "pliers", "#f2c230"],
    [0.1, 2.55, "hammer", "#c8261f"], [1.1, 2.7, "spool", "#e5532d"], [1.4, 2.7, "spool", "#2f6fb5"], [2.0, 2.65, "pliers", "#1a9f61"], [2.6, 2.7, "driver", "#d9402c"]].forEach(([x, y, k, c]) => tool(x, y, k, c));
  // a shelf under the pegboard with labelled boxes
  merged.add("wood", new THREE.BoxGeometry(6.4, 0.05, 0.28), { pos: [0, 1.62, -7.36], uv: sc(6.4, 0.28) });
  // side wall shelving (right wall)
  for (const y of [0.55, 1.15, 1.75, 2.35]) merged.add("wood", new THREE.BoxGeometry(0.4, 0.05, 2.6), { pos: [7.28, y, -1.5], uv: sc(0.4, 2.6) });
  for (const z of [-2.8, -0.2]) merged.add("metal", new THREE.BoxGeometry(0.4, 2.4, 0.05), { pos: [7.28, 1.35, z], color: "#516070" });
  const random = rng(5);
  for (const y of [0.6, 1.2, 1.8, 2.4]) for (let i = 0; i < 4; i += 1) {
    const h = 0.18 + random() * 0.14; const c = ["#2f6fb5", "#dfe6ee", "#f2c230", "#1a9f61", "#c8261f"][Math.floor(random() * 5)];
    merged.add("gloss", new THREE.BoxGeometry(0.3, h, 0.4), { pos: [7.28, y + h / 2 + 0.025, -2.55 + i * 0.62], color: c });
  }
  // window frame on the left wall
  const wz = -1.8;
  for (const [w, h, d, y, z] of [[0.12, 0.12, 4.3, 3.2, wz], [0.12, 0.12, 4.3, 0.95, wz], [0.12, 2.35, 0.12, 2.1, wz - 2.1], [0.12, 2.35, 0.12, 2.1, wz + 2.1], [0.1, 2.3, 0.07, 2.1, wz]]) merged.add("gloss", new THREE.BoxGeometry(w, h, d), { pos: [-7.46, y, z], color: "#eef2f6" });
  merged.add("gloss", new THREE.BoxGeometry(0.1, 0.07, 4.2), { pos: [-7.46, 2.1, wz], color: "#eef2f6" });
  merged.add("wood", new THREE.BoxGeometry(0.4, 0.05, 4.5), { pos: [-7.3, 0.92, wz], uv: sc(0.4, 4.5) });
  // pendant lamps
  const lampX = [-3.7, 0, 3.7];
  if (!lod) {
    for (const x of lampX) {
      merged.add("metal", new THREE.CylinderGeometry(0.01, 0.01, 1.2, 6), { pos: [x, 3.6, -5.6], color: "#14181f" });
      merged.add("metal", new THREE.ConeGeometry(0.3, 0.26, 24, 1, true), { pos: [x, 2.95, -5.6], color: "#1a2230", scale: [1, 1, 1] });
    }
  }
  merged.build({ wood: benchWoodMat, metal: MAT.metal, gloss: MAT.gloss, matte: MAT.matte, chrome: MAT.chrome }, group);

  // window panes (sky) and light shafts
  const windowMesh = new THREE.Mesh(new THREE.PlaneGeometry(4.1, 2.25), new THREE.MeshBasicMaterial({ map: skyTexture(), toneMapped: false }));
  windowMesh.rotation.y = Math.PI / 2; windowMesh.position.set(-7.49, 2.1, wz);
  group.add(windowMesh);
  group.userData.windowGlow = addGlow(group, [-7.2, 2.1, wz], "#cfe6ff", 6.5, 0.55);
  const shaft = shaftTexture();
  const shaftMat = new THREE.MeshBasicMaterial({ map: shaft, transparent: true, opacity: 0.1, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide, toneMapped: false, fog: false });
  const shafts = new THREE.Group(); shafts.name = "light-shafts";
  [-1.4, 0.3].forEach((dz) => { const mesh = new THREE.Mesh(shaftGeometry(wz + dz, 1.1, new THREE.Vector3(-7.4, 3.0, 0), new THREE.Vector3(-1.4, 0.03, 0)), shaftMat); mesh.renderOrder = 2; shafts.add(mesh); });
  group.add(shafts);
  group.userData.shafts = shafts;
  // pendant lamp shells with emissive underside and glow
  if (!lod) for (const x of lampX) {
    const glow = new THREE.Mesh(new THREE.CircleGeometry(0.24, 20), new THREE.MeshBasicMaterial({ color: "#fff1d0", toneMapped: false }));
    glow.rotation.x = Math.PI / 2; glow.position.set(x, 2.84, -5.6);
    group.add(glow);
    addGlow(group, [x, 2.75, -5.6], "#ffcf80", 1.8, 0.5);
  }
  return group;
}

function addGlow(group, pos, color, size, opacity) {
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: SPRITE_GLOW, color, transparent: true, opacity, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false, fog: false }));
  sprite.position.set(...pos); sprite.scale.setScalar(size); sprite.raycast = () => {};
  group.add(sprite);
  return sprite;
}

function buildSafetyRoom(room, lod) {
  const group = room;
  const concrete = concreteSet({ size: 512, seed: 23, base: "#7a8088", repeat: [14, 14], joints: 1, polish: 0.42 });
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(42, 42), std({ map: concrete.map, normalMap: concrete.normalMap, roughnessMap: concrete.roughnessMap, roughness: 1, metalness: 0 }));
  floor.rotation.x = -Math.PI / 2; floor.receiveShadow = true; floor.name = "room-floor";
  group.add(floor);
  // painted walkway lines and a hazard-stripe boundary on the floor
  const lines = makeCanvas(1024); const g = lines.getContext("2d");
  g.strokeStyle = "#f2c230"; g.lineWidth = 14; g.strokeRect(70, 70, 884, 884);
  g.setLineDash([46, 30]); g.strokeStyle = "rgba(255,255,255,0.85)"; g.lineWidth = 7; g.strokeRect(120, 120, 784, 784);
  const lineTex = new THREE.CanvasTexture(lines); lineTex.colorSpace = THREE.SRGBColorSpace; lineTex.anisotropy = 8;
  const lineMesh = new THREE.Mesh(new THREE.PlaneGeometry(13, 13), shared(new THREE.MeshStandardMaterial({ map: lineTex, transparent: true, roughness: 0.6, polygonOffset: true, polygonOffsetFactor: -1 })));
  lineMesh.rotation.x = -Math.PI / 2; lineMesh.position.y = 0.004; lineMesh.receiveShadow = true;
  group.add(lineMesh);

  const corr = corrugatedSet({ size: 512, seed: 12, base: "#a3afbd", repeat: [1, 1] });
  const wallMat = std({ map: corr.map, normalMap: corr.normalMap, roughnessMap: corr.roughnessMap, roughness: 1, metalness: 0.35, normalScale: new THREE.Vector2(1, 1) });
  const wallMerge = new Merge();
  wallMerge.add("wall", new THREE.PlaneGeometry(16, 4.4), { pos: [0, 2.2, -8], uv: [16 / 2, 4.4 / 2] });
  wallMerge.add("wall", new THREE.PlaneGeometry(16, 4.4), { pos: [-8, 2.2, 0], rot: [0, Math.PI / 2, 0], uv: [16 / 2, 4.4 / 2] });
  wallMerge.add("wall", new THREE.PlaneGeometry(16, 4.4), { pos: [8, 2.2, 0], rot: [0, -Math.PI / 2, 0], uv: [16 / 2, 4.4 / 2] });
  wallMerge.build({ wall: wallMat }, group, { shadow: false });

  // roller door and signs on the back wall
  const door = corrugatedSet({ size: 512, seed: 30, base: "#b4bcc6", repeat: [1, 1] });
  door.map.repeat.set(6, 3); door.normalMap.repeat.set(6, 3); door.roughnessMap.repeat.set(6, 3);
  const doorMesh = new THREE.Mesh(new THREE.PlaneGeometry(4.2, 3.4), std({ map: door.map, normalMap: door.normalMap, roughnessMap: door.roughnessMap, roughness: 1, metalness: 0.5 }));
  doorMesh.position.set(0, 1.7, -7.96);
  group.add(doorMesh);
  const merged = new Merge();
  const stripe = (x0, y0, w, h) => merged.add("stripes", new THREE.PlaneGeometry(w, h), { pos: [x0, y0, -7.95], uv: [w / 1.0, 1] });
  stripe(-2.3, 1.7, 0.25, 3.5); stripe(2.3, 1.7, 0.25, 3.5); stripe(0, 3.52, 4.8, 0.25);
  merged.box("metal", 4.8, 0.2, 0.18, [0, 3.75, -7.9], { color: "#59636f" });
  // bollards (yellow with a black band) around the lanes, drums, pallets and crates in the corners
  const bollard = (x, z) => { merged.cyl("gloss", 0.09, 0.09, 0.95, [x, 0.475, z], { color: "#f2c230" }, 14); merged.cyl("gloss", 0.092, 0.092, 0.14, [x, 0.55, z], { color: "#14181f" }, 14); merged.cyl("metal", 0.12, 0.12, 0.03, [x, 0.015, z], { color: "#59636f" }, 14); };
  [[-5.6, 3.9], [-5.6, 1.6], [-5.6, -0.6], [5.6, 3.9], [5.6, 1.6], [5.6, -0.6], [-3.2, 5.6], [3.2, 5.6]].forEach(([x, z]) => bollard(x, z));
  const drum = (x, z, c) => { merged.cyl("gloss", 0.29, 0.29, 0.88, [x, 0.44, z], { color: c }, 20); for (const y of [0.22, 0.66]) merged.cyl("metal", 0.3, 0.3, 0.035, [x, y, z], { color: "#9aa5b2" }, 20); merged.cyl("metal", 0.27, 0.27, 0.02, [x, 0.885, z], { color: "#9aa5b2" }, 20); };
  [[-6.3, -6.6, "#2f6fb5"], [-5.6, -6.9, "#c8261f"], [-5.9, -6.0, "#2f6fb5"], [-6.9, -5.8, "#1a9f61"]].forEach(([x, z, c]) => drum(x, z, c));
  const pallet = (x, z, h, rot = 0) => { merged.box("matte", 1.2, 0.13, 1.0, [x, 0.065, z], { color: "#a87a42", rot: [0, rot, 0] }); merged.box("gloss", 1.1, h, 0.9, [x, 0.13 + h / 2, z], { color: "#c79a62", rot: [0, rot, 0] }); merged.box("matte", 1.12, 0.03, 0.04, [x, 0.13 + h * 0.5, z + 0.45], { color: "#3a2a18", rot: [0, rot, 0] }); };
  pallet(5.0, -6.6, 0.8, 0.08); pallet(6.5, -5.9, 0.55, -0.2); merged.box("gloss", 0.8, 0.5, 0.8, [5.1, 1.18, -6.55], { color: "#d9b27a", rot: [0, 0.3, 0] });
  // right wall: fire hose cabinet
  merged.box("gloss", 0.28, 1.0, 0.8, [7.8, 1.4, 0.2], { color: "#c8261f" });
  merged.box("matte", 0.02, 0.8, 0.6, [7.66, 1.4, 0.2], { color: "#1a2230" });
  merged.cyl("chrome", 0.26, 0.26, 0.06, [7.62, 1.4, 0.2], { color: "#e6edf3", rot: [0, 0, Math.PI / 2] }, 22);
  // hazard-stripe base strip along the back wall
  merged.build({ stripes: MAT.stripes, metal: MAT.metal, gloss: MAT.gloss, matte: MAT.matte }, group);
  const signs = [];
  const sign = (draw, w, h, pos, rotY = 0) => {
    const tex = labelTexture(512, Math.round(512 * h / w), draw);
    const mesh = new THREE.Mesh(new THREE.PlaneGeometry(w, h), shared(new THREE.MeshStandardMaterial({ map: tex, roughness: 0.4 })));
    mesh.position.set(...pos); mesh.rotation.y = rotY;
    group.add(mesh); signs.push(tex);
  };
  sign((c, w, h) => { c.fillStyle = "#003594"; c.fillRect(0, 0, w, h); c.strokeStyle = "#fff"; c.lineWidth = 6; c.strokeRect(8, 8, w - 16, h - 16); c.fillStyle = "#fff"; c.textAlign = "center"; c.textBaseline = "middle"; c.font = "90px 'League Gothic', Impact, sans-serif"; c.fillText("PPE REQUIRED", w / 2, h * 0.28); c.font = "500 25px Roboto, sans-serif"; c.fillStyle = "#FFB81C"; c.fillText("HARD HAT · EYE PROTECTION · GLOVES", w / 2, h * 0.7); }, 1.8, 0.9, [-4.3, 2.4, -7.96]);
  sign((c, w, h) => { c.fillStyle = "#f2c230"; c.fillRect(0, 0, w, h); c.strokeStyle = "#14181f"; c.lineWidth = 10; c.strokeRect(10, 10, w - 20, h - 20); c.fillStyle = "#14181f"; c.textAlign = "center"; c.textBaseline = "middle"; c.font = "92px 'League Gothic', Impact, sans-serif"; c.fillText("CAUTION", w / 2, h * 0.3); c.font = "500 25px Roboto, sans-serif"; c.fillText("FORKLIFT AND PEDESTRIAN TRAFFIC", w / 2, h * 0.7); }, 1.8, 0.9, [4.3, 2.4, -7.96]);
  sign((c, w, h) => { c.fillStyle = "#0b7f4b"; c.fillRect(0, 0, w, h); c.fillStyle = "#fff"; c.textAlign = "center"; c.textBaseline = "middle"; c.font = "86px 'League Gothic', Impact, sans-serif"; c.fillText("EMERGENCY EXIT", w / 2, h * 0.5); }, 1.6, 0.6, [-7.96, 2.6, -0.8], Math.PI / 2);
  group.userData.signs = signs;
  fontsReady.then(() => refreshLabels(signs)); // the signs were drawn before the brand fonts finished loading
  // security lights with glow
  if (!lod) for (const x of [-4, 4]) {
    const fixture = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.1, 0.22), MAT.metal.clone());
    fixture.material = std({ color: "#59636f", metalness: 1, roughness: 0.4 });
    fixture.position.set(x, 3.7, -7.8);
    group.add(fixture);
    const glow = new THREE.Mesh(new THREE.PlaneGeometry(0.44, 0.16), new THREE.MeshBasicMaterial({ color: "#fff6dd", toneMapped: false }));
    glow.rotation.x = Math.PI / 2 + 0.5; glow.position.set(x, 3.64, -7.75);
    group.add(glow);
    addGlow(group, [x, 3.55, -7.6], "#ffe8b8", 2.2, 0.5);
  }
  return group;
}

// ------------------------------------------------------------------ props (optional glTF extras, desktop only)
const PROPS = {
  circuits: [
    { model: "retro_multimeter", x: -3.9, z: -6.85, y: 0.93, rot: 20, scale: 1.3 },
    { model: "classic_laptop", x: -3.2, z: -6.95, y: 0.93, rot: 5, scale: 1 },
    { model: "metal_toolbox", x: 0.9, z: -6.95, y: 0.93, rot: -10, scale: 1.2 },
    // bench_vice_01 is left out: its mesh is built about 5 m off its origin, so it shows up in the middle of the board (it does in v1 too)
    { model: "metal_stool_01", x: -1.9, z: -5.8, rot: 30, scale: 1.1 },
    { model: "metal_stool_01", x: 2.1, z: -5.6, rot: -40, scale: 1.1 },
    { model: "tool_cart", x: 6.2, z: 2.6, rot: -90, scale: 1.1 },
    { model: "korean_fire_extinguisher_01", x: 7.1, z: 4.6, rot: -90, scale: 1.2 },
    { model: "WetFloorSign_01", x: 4.9, z: 3.9, rot: 35, scale: 1 },
  ],
  safety: [
    { model: "barrel_03", x: -3.6, z: -6.6, rot: 40, scale: 1 },
    { model: "propane_tank", x: -3.0, z: -6.7, rot: 0, scale: 1.3 },
    { model: "propane_tank", x: -2.5, z: -6.6, rot: 70, scale: 1.3 },
    { model: "wooden_crate_01", x: 3.1, z: -6.7, rot: 10, scale: 1.4 },
    { model: "industrial_pastic_container", x: 2.0, z: -6.9, rot: 0, scale: 1.4 },
    { model: "portable_generator", x: 6.8, z: -3.2, rot: -90, scale: 1.3 },
    { model: "tool_cart", x: -6.8, z: 0.4, rot: 90, scale: 1.1 },
    { model: "korean_fire_extinguisher_01", x: -7.4, z: 2.6, rot: 90, scale: 1.3 },
    { model: "portable_searchlight", x: 6.6, z: 2.4, rot: -150, scale: 2.2 },
    { model: "WetFloorSign_01", x: 4.8, z: 4.0, rot: 20, scale: 1 },
    { model: "WetFloorSign_01", x: -4.8, z: 3.6, rot: -30, scale: 1 },
  ],
};

// ------------------------------------------------------------------ public entry
/**
 * Builds the room, the key light and the environment map for a theme. Returns { group, stats, ready, applyXr(inXr), setShadows(on), key }.
 * `lite` skips glTF props (software renderers). `lod` is switched on inside VR: no pendant lamps or other small extras.
 */
export function buildRoom2({ themeId, mode, scene, renderer, withProps = true, shadows = true }) {
  const group = new THREE.Group();
  group.name = `room2-${themeId}`;
  group.position.y = -0.09;
  scene.add(group);
  makeEnvironment(renderer, scene, themeId, mode);
  if (themeId === "safety") buildSafetyRoom(group, false); else buildCircuitRoom(group, false);

  // light from the window (circuits) or the high bay windows (safety)
  const key = new THREE.DirectionalLight(themeId === "safety" ? "#fff4e0" : "#fff0d6", themeId === "safety" ? 3.4 : 3.0);
  key.position.set(themeId === "safety" ? -5.5 : -7, themeId === "safety" ? 8 : 6, themeId === "safety" ? 3 : -1.5);
  key.castShadow = shadows;
  key.shadow.mapSize.set(2048, 2048);
  Object.assign(key.shadow.camera, { left: -7.5, right: 7.5, top: 7.5, bottom: -7.5, near: 1, far: 22 });
  key.shadow.bias = -0.0005; key.shadow.normalBias = 0.03; key.shadow.radius = 4;
  scene.add(key);
  const fill = new THREE.HemisphereLight(0xdbe8ff, themeId === "safety" ? 0x6a707a : 0x7a6a58, 0.7);
  scene.add(fill);

  const list = (PROPS[themeId] ?? []);
  const props = withProps ? list : [];
  const stats = { requested: props.length, loaded: 0, fallbacks: 0 };
  const propGroup = new THREE.Group(); propGroup.name = "props";
  group.add(propGroup);
  const loads = props.map(async (prop) => {
    const result = await loadWithFallback(async () => {
      const { gltf } = await loadTemplate("props", prop.model);
      const copy = gltf.scene.clone(true);
      copy.traverse((child) => { if (child.isMesh) { child.castShadow = true; child.receiveShadow = true; } });
      return copy;
    }, () => primitiveProp(prop.model));
    if (result.usedFallback) stats.fallbacks += 1; else stats.loaded += 1;
    const object = result.value;
    object.scale.setScalar(prop.scale ?? 1);
    object.position.set(prop.x, prop.y ?? 0, prop.z);
    object.rotation.y = THREE.MathUtils.degToRad(prop.rot ?? 0);
    propGroup.add(object);
  });
  const ready = Promise.all(loads).then(() => stats);

  return {
    group, stats, ready, key, layout: { title: themeId === "safety" ? "Safety bay" : "Workshop" },
    // AR: the real room shows instead. VR: the room stays but sits on the real floor (y = 0), without the glTF props or the shadow.
    applyXr(inXr, xrMode = "vr") {
      group.visible = !inXr || xrMode === "vr";
      group.position.y = inXr ? 0 : -0.09;
      propGroup.visible = !inXr;
    },
    setShadows(on) { key.castShadow = on; },
  };
}
export { RoomEnvironment, concreteSet, glowTexture, textSign };
