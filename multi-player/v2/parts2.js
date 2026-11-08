// Lab v2 part models. Each part is a few merged meshes (one per material class, colours in vertex colours) so a part costs
// 3 to 6 draw calls, not 15. Materials are shared module-wide, so the shader programs are compiled once.
// Conventions kept from v1 (the shared logic relies on them): part origin on the board, terminals at x = +-TERMINAL[part],
// the battery's + end at +x, the switch lever and motor fan found through group.userData.
import * as THREE from "three";
import { RoundedBoxGeometry } from "three/addons/geometries/RoundedBoxGeometry.js";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { PITT, fontsReady } from "../../brand-kit/panels.js";
import { TERMINAL } from "../shared/circuit-eval.mjs";
import { labelTexture, refreshLabels, glowTexture, hazardStripeTexture, blobShadowTexture, coneTexture, makeCanvas } from "./tex.js";

const Y0 = 0.15; // height of the lead axis above the board
const labelTextures = [];
fontsReady.then(() => refreshLabels(labelTextures));
const shared = (m) => { m.userData.shared = true; return m; };

// ------------------------------------------------------------------ shared materials
export const MAT = {
  gloss: shared(new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.32, metalness: 0.0 })),
  matte: shared(new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.78, metalness: 0.0 })),
  metal: shared(new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.34, metalness: 1.0 })),
  chrome: shared(new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.14, metalness: 1.0 })),
  stripes: shared(new THREE.MeshStandardMaterial({ map: hazardStripeTexture(), roughness: 0.22, metalness: 0.15, emissive: "#3a2d00", emissiveMap: null, envMapIntensity: 1.6 })),
  stripesRed: shared(new THREE.MeshStandardMaterial({ map: hazardStripeTexture("#e8ecf2", "#c8261f"), roughness: 0.22, metalness: 0.15, envMapIntensity: 1.6 })),
};
MAT.stripes.map.repeat.set(2, 1);
MAT.stripesRed.map.repeat.set(2, 1);

export const SPRITE_GLOW = glowTexture();
export const BLOB = blobShadowTexture();
const blobMaterial = shared(new THREE.MeshBasicMaterial({ map: BLOB, transparent: true, depthWrite: false, opacity: 0.9, polygonOffset: true, polygonOffsetFactor: -2 }));

// ------------------------------------------------------------------ geometry batching
const tmpM = new THREE.Matrix4(); const tmpQ = new THREE.Quaternion(); const tmpE = new THREE.Euler(); const tmpV = new THREE.Vector3(); const tmpS = new THREE.Vector3();
const tmpC = new THREE.Color();
class Batch {
  constructor() { this.list = { gloss: [], matte: [], metal: [], chrome: [] }; }
  add(kind, geometry, color, pos = [0, 0, 0], rot = [0, 0, 0], scale = [1, 1, 1]) {
    if (geometry.index) geometry = geometry.toNonIndexed(); // RoundedBoxGeometry is not indexed, the rest are: merge needs one kind
    tmpM.compose(tmpV.set(...pos), tmpQ.setFromEuler(tmpE.set(...rot)), tmpS.set(...scale));
    geometry.applyMatrix4(tmpM);
    tmpC.set(color);
    const count = geometry.attributes.position.count;
    const colors = new Float32Array(count * 3);
    for (let i = 0; i < count; i += 1) { colors[i * 3] = tmpC.r; colors[i * 3 + 1] = tmpC.g; colors[i * 3 + 2] = tmpC.b; }
    geometry.setAttribute("color", new THREE.BufferAttribute(colors, 3));
    this.list[kind].push(geometry);
    return this;
  }
  // A cylinder along x between x0 and x1 (radius rA at x0, rB at x1) at height y, depth z.
  cylX(kind, color, rA, rB, x0, x1, y = Y0, z = 0, seg = 14) {
    return this.add(kind, new THREE.CylinderGeometry(rB, rA, Math.abs(x1 - x0), seg, 1), color, [(x0 + x1) / 2, y, z], [0, 0, -Math.PI / 2]);
  }
  cylY(kind, color, rTop, rBottom, y0, y1, x = 0, z = 0, seg = 14) {
    return this.add(kind, new THREE.CylinderGeometry(rTop, rBottom, y1 - y0, seg, 1), color, [x, (y0 + y1) / 2, z]);
  }
  sphere(kind, color, r, pos, scale = [1, 1, 1], seg = 14) { return this.add(kind, new THREE.SphereGeometry(r, seg, Math.max(6, seg >> 1)), color, pos, [0, 0, 0], scale); }
  box(kind, color, w, h, d, pos, rot, radius = 0) {
    return this.add(kind, radius ? new RoundedBoxGeometry(w, h, d, 2, radius) : new THREE.BoxGeometry(w, h, d), color, pos, rot);
  }
  meshes(group) {
    for (const [kind, geometries] of Object.entries(this.list)) {
      if (!geometries.length) continue;
      const mesh = new THREE.Mesh(mergeGeometries(geometries), MAT[kind]);
      mesh.name = `part-${kind}`;
      group.add(mesh);
      geometries.forEach((g) => g.dispose());
    }
    return group;
  }
}

// Brass terminal posts at x = +-reach, standing on the board, with a collar and a rounded cap. The status ring is drawn by fx.js.
function posts(b, reach, y = Y0) {
  for (const x of [-reach, reach]) {
    b.cylY("metal", "#d6a948", 0.058, 0.066, 0, 0.024, x, 0, 14);
    b.cylY("metal", "#d6a948", 0.03, 0.034, 0.02, y, x, 0, 12);
    b.cylY("chrome", "#e6edf3", 0.05, 0.05, y - 0.05, y - 0.03, x, 0, 6);
    b.sphere("metal", "#e0b653", 0.042, [x, y, 0], [1, 0.8, 1], 12);
  }
}

function blob(group, w, d) {
  const mesh = new THREE.Mesh(new THREE.PlaneGeometry(w, d), blobMaterial);
  mesh.rotation.x = -Math.PI / 2;
  mesh.position.y = 0.004;
  mesh.name = "blob-shadow";
  mesh.raycast = () => {}; // never picked
  mesh.renderOrder = -1;
  group.add(mesh);
  group.userData.blob = mesh;
}

// ------------------------------------------------------------------ circuit parts
const batteryLabel = labelTexture(512, 256, (g, w, h) => {
  g.fillStyle = "#003594"; g.fillRect(0, 0, w, h);
  const bands = g.createLinearGradient(0, 0, 0, h);
  bands.addColorStop(0, "rgba(255,255,255,0.14)"); bands.addColorStop(0.5, "rgba(255,255,255,0)"); bands.addColorStop(1, "rgba(0,0,0,0.25)");
  // The cylinder is rotated so that u is round the can and v runs along it; text is drawn sideways to read along the can.
  g.fillStyle = PITT.gold; g.fillRect(0, h * 0.06, w, h * 0.05); g.fillRect(0, h * 0.89, w, h * 0.05);
  g.save(); g.translate(w * 0.75, h * 0.5); g.rotate(-Math.PI / 2);
  g.fillStyle = "#ffffff"; g.textAlign = "center"; g.textBaseline = "middle";
  g.font = "700 68px 'League Gothic', Impact, sans-serif"; g.fillText("6 V CELL", 0, -4);
  g.font = "500 28px Roboto, sans-serif"; g.fillStyle = PITT.gold; g.fillText("PITT-BRADFORD CIST", 0, 56);
  g.restore();
  g.save(); g.translate(w * 0.75, h * 0.5); g.rotate(-Math.PI / 2);
  g.fillStyle = PITT.gold; g.font = "700 76px Roboto, sans-serif"; g.fillText("+", 108, -4); g.fillText("−", -118, -4);
  g.restore();
});
labelTextures.push(batteryLabel);
const batteryMaterial = shared(new THREE.MeshStandardMaterial({ map: batteryLabel, roughness: 0.38, metalness: 0.55, envMapIntensity: 1.2 }));

const SIGNS = {};
function sign(key, w, h, draw, extra = {}) {
  const texture = labelTexture(w, h, draw);
  labelTextures.push(texture);
  const material = shared(new THREE.MeshStandardMaterial({ map: texture, roughness: 0.4, metalness: 0, ...extra }));
  if (extra.emissive) material.emissiveMap = texture;
  SIGNS[key] = material;
  return material;
}
const plane = (material, w, h, pos, rotY = 0) => {
  const mesh = new THREE.Mesh(new THREE.PlaneGeometry(w, h), material);
  mesh.position.set(...pos); mesh.rotation.y = rotY;
  return mesh;
};

export const circuitBuilders = {
  battery(group) {
    const b = new Batch();
    const body = new THREE.Mesh(new THREE.CylinderGeometry(0.152, 0.152, 0.5, 28, 1), batteryMaterial);
    body.rotation.z = -Math.PI / 2; body.position.set(-0.04, Y0, 0); body.name = "battery-can";
    group.add(body);
    b.cylX("metal", "#d8dde3", 0.157, 0.157, -0.295, -0.285, Y0, 0, 28); // rim at the - end
    b.cylX("metal", "#d8dde3", 0.157, 0.157, 0.205, 0.215, Y0, 0, 28);
    b.cylX("metal", "#d6a948", 0.1, 0.1, 0.215, 0.27, Y0, 0, 20); // + cap
    b.cylX("metal", "#e0b653", 0.052, 0.052, 0.27, 0.32, Y0, 0, 14);
    b.cylX("chrome", "#d9e2ea", 0.12, 0.12, -0.33, -0.295, Y0, 0, 20); // - plate
    for (const x of [-0.12, 0.1]) b.box("matte", "#1a2230", 0.1, 0.05, 0.3, [x, 0.025, 0], [0, 0, 0], 0.015); // cradle
    b.cylX("metal", "#d6a948", 0.022, 0.022, 0.32, 0.4, Y0, 0, 8);
    b.cylX("metal", "#d6a948", 0.022, 0.022, -0.4, -0.33, Y0, 0, 8);
    posts(b, 0.4);
    b.meshes(group);
    blob(group, 1.0, 0.5);
  },
  switch(group) {
    const b = new Batch();
    b.box("gloss", "#0b2a63", 0.96, 0.07, 0.3, [0, 0.035, 0], [0, 0, 0], 0.025); // bakelite base
    b.box("gloss", "#17408f", 0.84, 0.03, 0.2, [0, 0.082, 0], [0, 0, 0], 0.012);
    b.cylX("metal", "#d6a948", 0.02, 0.02, -0.4, -0.2, Y0, 0, 8);
    b.cylX("metal", "#d6a948", 0.02, 0.02, 0.2, 0.4, Y0, 0, 8);
    b.box("metal", "#cfd6de", 0.07, 0.14, 0.04, [0.4, 0.14, 0.05], [0, 0, 0], 0.01); // contact fork
    b.box("metal", "#cfd6de", 0.07, 0.14, 0.04, [0.4, 0.14, -0.05], [0, 0, 0], 0.01);
    posts(b, 0.4);
    b.meshes(group);
    const lever = new THREE.Group();
    lever.position.set(-0.4, Y0 + 0.02, 0);
    const lb = new Batch();
    lb.box("metal", "#e3b95a", 0.84, 0.026, 0.07, [0.42, 0, 0], [0, 0, 0], 0.01);
    lb.cylY("gloss", "#d9402c", 0.035, 0.05, 0.0, 0.16, 0.42, 0, 14); // red insulating handle
    lb.sphere("gloss", "#e25040", 0.052, [0.42, 0.17, 0], [1, 0.8, 1], 12);
    lb.cylY("chrome", "#e6edf3", 0.03, 0.03, -0.02, 0.05, 0, 0, 10);
    lb.meshes(lever);
    group.add(lever);
    group.userData.lever = lever;
    blob(group, 1.1, 0.42);
  },
  bulb(group) {
    const b = new Batch();
    // socket: threaded brass, with a dark insulating collar
    const profile = [];
    for (let i = 0; i <= 5; i += 1) { profile.push(new THREE.Vector2(0.12, 0.01 + i * 0.025), new THREE.Vector2(0.105, 0.0225 + i * 0.025)); }
    profile.unshift(new THREE.Vector2(0.001, 0)); profile.push(new THREE.Vector2(0.001, 0.14));
    b.add("metal", new THREE.LatheGeometry(profile, 20), "#c99b3d", [0, 0, 0]);
    b.cylY("matte", "#1a2230", 0.13, 0.145, 0.0, 0.03, 0, 0, 20);
    b.cylY("gloss", "#101828", 0.115, 0.125, 0.14, 0.17, 0, 0, 20);
    b.cylX("metal", "#d6a948", 0.02, 0.02, -0.4, -0.1, 0.1, 0, 8);
    b.cylX("metal", "#d6a948", 0.02, 0.02, 0.1, 0.4, 0.1, 0, 8);
    b.cylX("metal", "#d6a948", 0.02, 0.02, -0.4, -0.38, Y0, 0, 6);
    for (const x of [-0.04, 0.04]) b.cylY("chrome", "#cfd6de", 0.006, 0.006, 0.17, 0.33, x, 0, 6);
    posts(b, 0.4);
    b.meshes(group);
    // glass: a lathe bulb outline; slightly transparent, strongly reflective
    const glassProfile = [new THREE.Vector2(0.095, 0.17), new THREE.Vector2(0.1, 0.2), new THREE.Vector2(0.14, 0.26), new THREE.Vector2(0.185, 0.34),
      new THREE.Vector2(0.2, 0.42), new THREE.Vector2(0.18, 0.5), new THREE.Vector2(0.12, 0.56), new THREE.Vector2(0.001, 0.58)];
    const glass = new THREE.MeshStandardMaterial({
      color: "#eef7ff", emissive: "#ffb84a", emissiveIntensity: 0, transparent: true, opacity: 0.3, roughness: 0.04, metalness: 0, envMapIntensity: 2.2, depthWrite: false,
    });
    const glassMesh = new THREE.Mesh(new THREE.LatheGeometry(glassProfile, 28), glass);
    glassMesh.name = "bulb-glass";
    group.add(glassMesh);
    // filament coil (colour follows brightness), halo sprite
    const pts = [];
    for (let i = 0; i <= 14; i += 1) pts.push(new THREE.Vector3(-0.04 + (i / 14) * 0.08, 0.33 + (i % 2 ? 0.035 : 0), (i % 3 - 1) * 0.008));
    const filament = new THREE.MeshBasicMaterial({ color: "#5a4a3a", toneMapped: false });
    const coil = new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(pts), 40, 0.006, 5), filament);
    coil.name = "bulb-filament";
    group.add(coil);
    const halo = new THREE.Sprite(new THREE.SpriteMaterial({ map: SPRITE_GLOW, color: "#ffb347", transparent: true, opacity: 0, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
    halo.position.set(0, 0.36, 0); halo.scale.setScalar(0.9);
    halo.raycast = () => {};
    group.add(halo);
    group.userData.glass = glass; group.userData.filament = filament; group.userData.halo = halo;
    group.userData.emitter = { y: 0.38, color: "#ffb25a" };
    blob(group, 1.0, 0.4);
  },
  resistor(group) {
    const b = new Batch();
    b.cylX("metal", "#c3ccd5", 0.02, 0.02, -0.4, 0.4, Y0, 0, 8);
    // ceramic body: barbell lathe, bands as slightly larger rings
    const prof = [new THREE.Vector2(0.001, -0.2), new THREE.Vector2(0.055, -0.19), new THREE.Vector2(0.085, -0.165), new THREE.Vector2(0.07, -0.12), new THREE.Vector2(0.07, 0.12),
      new THREE.Vector2(0.085, 0.165), new THREE.Vector2(0.055, 0.19), new THREE.Vector2(0.001, 0.2)];
    b.add("gloss", new THREE.LatheGeometry(prof, 20), "#dcc8a0", [0, Y0, 0], [0, 0, -Math.PI / 2]);
    for (const [x, c] of [[-0.075, "#b3261e"], [-0.025, "#7b2fa0"], [0.025, "#e8821e"], [0.095, "#d4af37"]]) b.cylX("gloss", c, 0.073, 0.073, x - 0.014, x + 0.014, Y0, 0, 20);
    posts(b, 0.4);
    b.meshes(group);
    blob(group, 0.9, 0.3);
  },
  motor(group) {
    const b = new Batch();
    b.cylX("metal", "#7f9bbd", 0.16, 0.16, -0.32, 0.12, Y0, 0, 28); // can
    for (const x of [-0.24, -0.15, -0.06]) b.cylX("metal", "#a9bdd6", 0.166, 0.166, x - 0.012, x + 0.012, Y0, 0, 28); // ribs
    b.cylX("gloss", "#003594", 0.163, 0.163, 0.04, 0.075, Y0, 0, 28); // brand band
    b.cylX("chrome", "#d9e2ea", 0.17, 0.12, 0.12, 0.16, Y0, 0, 24); // end bell
    b.cylX("chrome", "#e6edf3", 0.024, 0.024, 0.14, 0.34, Y0, 0, 10); // shaft
    b.cylX("metal", "#1d2430", 0.1, 0.1, -0.345, -0.32, Y0, 0, 20);
    b.cylX("metal", "#d6a948", 0.02, 0.02, -0.4, -0.32, Y0, 0, 8);
    for (const x of [-0.2, 0.0]) b.box("matte", "#1a2230", 0.1, 0.05, 0.3, [x, 0.025, 0], [0, 0, 0], 0.015);
    posts(b, 0.4);
    b.meshes(group);
    const fan = new THREE.Group();
    fan.position.set(0.29, Y0, 0);
    const fb = new Batch();
    fb.cylX("chrome", "#e6edf3", 0.04, 0.04, -0.03, 0.03, 0, 0, 14);
    for (let i = 0; i < 3; i += 1) {
      const a = (i * Math.PI * 2) / 3;
      fb.add("gloss", new THREE.BoxGeometry(0.014, 0.17, 0.08), "#46d9c0", [0, Math.cos(a) * 0.1, Math.sin(a) * 0.1], [a, 0, 0.0]);
    }
    fb.meshes(fan);
    const blur = new THREE.Mesh(new THREE.CircleGeometry(0.2, 24), new THREE.MeshBasicMaterial({ color: "#8ff3df", transparent: true, opacity: 0, depthWrite: false, side: THREE.DoubleSide, blending: THREE.AdditiveBlending }));
    blur.rotation.y = Math.PI / 2; blur.raycast = () => {};
    fan.add(blur);
    group.add(fan);
    group.userData.fan = fan; group.userData.blur = blur;
    blob(group, 1.0, 0.45);
  },
  wire(group) {
    const b = new Batch();
    const curve = new THREE.CatmullRomCurve3([new THREE.Vector3(-0.6, Y0, 0), new THREE.Vector3(-0.45, Y0 + 0.07, 0), new THREE.Vector3(-0.2, Y0 + 0.11, 0.0), new THREE.Vector3(0.2, Y0 + 0.11, 0),
      new THREE.Vector3(0.45, Y0 + 0.07, 0), new THREE.Vector3(0.6, Y0, 0)]);
    b.add("gloss", new THREE.TubeGeometry(curve, 36, 0.03, 10), "#e5532d");
    for (const s of [-1, 1]) b.sphere("metal", "#d6a948", 0.04, [s * 0.6, Y0, 0], [1, 1, 1], 10); // brass ends
    posts(b, 0.6);
    b.meshes(group);
    blob(group, 1.3, 0.2);
  },
};

// ------------------------------------------------------------------ safety parts
const exitSign = () => sign("exit", 512, 256, (g, w, h) => {
  g.fillStyle = "#0b7f4b"; g.fillRect(0, 0, w, h);
  g.strokeStyle = "#ffffff"; g.lineWidth = 8; g.strokeRect(10, 10, w - 20, h - 20);
  g.fillStyle = "#ffffff"; g.font = "180px 'League Gothic', Impact, sans-serif"; g.textAlign = "left"; g.textBaseline = "middle";
  g.fillText("EXIT", 40, h / 2 + 8);
  // running figure
  g.save(); g.translate(w - 150, h / 2); g.lineCap = "round"; g.lineJoin = "round"; g.strokeStyle = "#fff"; g.fillStyle = "#fff"; g.lineWidth = 16;
  g.beginPath(); g.arc(10, -62, 15, 0, Math.PI * 2); g.fill();
  g.beginPath(); g.moveTo(5, -38); g.lineTo(-8, 8); g.lineTo(-40, 40); g.moveTo(-8, 8); g.lineTo(22, 30); g.lineTo(12, 66); g.moveTo(5, -38); g.lineTo(-30, -20); g.moveTo(5, -38); g.lineTo(40, -26); g.stroke();
  g.restore();
}, { emissive: new THREE.Color("#ffffff"), emissiveIntensity: 0.75 });

const musterSign = () => sign("muster", 512, 340, (g, w, h) => {
  g.fillStyle = "#0b7f4b"; g.fillRect(0, 0, w, h);
  g.strokeStyle = "#fff"; g.lineWidth = 8; g.strokeRect(10, 10, w - 20, h - 20);
  g.fillStyle = "#fff"; g.textAlign = "center"; g.textBaseline = "middle";
  g.font = "104px 'League Gothic', Impact, sans-serif"; g.fillText("MUSTER POINT", w / 2, h * 0.2);
  for (let i = 0; i < 4; i += 1) {
    const x = w * (0.2 + i * 0.2);
    g.beginPath(); g.arc(x, h * 0.5, 18, 0, Math.PI * 2); g.fill();
    g.beginPath(); g.moveTo(x - 22, h * 0.78); g.quadraticCurveTo(x, h * 0.46, x + 22, h * 0.78); g.fill();
  }
  g.font = "500 36px Roboto, sans-serif"; g.fillText("ASSEMBLE HERE", w / 2, h * 0.9);
}, { emissive: new THREE.Color("#ffffff"), emissiveIntensity: 0.6 });

const firstAidSign = () => sign("firstaid", 512, 340, (g, w, h) => {
  g.fillStyle = "#f7fafc"; g.fillRect(0, 0, w, h);
  g.fillStyle = "#1a9f61"; g.fillRect(w / 2 - 28, h * 0.12, 56, h * 0.58); g.fillRect(w / 2 - 90, h * 0.12 + h * 0.58 / 2 - 28, 180, 56);
  g.fillStyle = "#1a9f61"; g.textAlign = "center"; g.textBaseline = "middle"; g.font = "86px 'League Gothic', Impact, sans-serif"; g.fillText("FIRST AID", w / 2, h * 0.85);
}, { roughness: 0.3 });

const extinguisherSign = () => sign("ext", 256, 256, (g, w, h) => {
  g.fillStyle = "#f7fafc"; g.fillRect(0, 0, w, h);
  g.fillStyle = "#c8261f"; g.fillRect(0, 0, w, h * 0.3); g.fillStyle = "#fff"; g.textAlign = "center"; g.textBaseline = "middle";
  g.font = "80px 'League Gothic', Impact, sans-serif"; g.fillText("FIRE", w / 2, h * 0.16);
  g.fillStyle = "#c8261f"; g.beginPath(); g.moveTo(w / 2, h * 0.38); g.quadraticCurveTo(w * 0.78, h * 0.62, w / 2, h * 0.92); g.quadraticCurveTo(w * 0.22, h * 0.62, w / 2, h * 0.38); g.fill();
  g.fillStyle = "#111"; g.font = "700 28px Roboto, sans-serif"; g.fillText("ABC", w / 2, h * 0.7);
}, { roughness: 0.35 });

function standBase(b, r = 0.14) {
  b.cylY("matte", "#171d28", r, r + 0.02, 0, 0.035, 0, 0, 24);
  b.cylY("chrome", "#dfe6ee", r + 0.004, r + 0.004, 0.03, 0.04, 0, 0, 24);
}

export const safetyBuilders = {
  exit(group) {
    exitSign(); const b = new Batch();
    standBase(b, 0.15);
    b.cylY("chrome", "#dfe6ee", 0.018, 0.022, 0.035, 0.92, 0, 0, 10);
    b.box("gloss", "#e9eef4", 0.6, 0.3, 0.07, [0, 1.0, 0], [0, 0, 0], 0.02);
    b.meshes(group);
    group.add(plane(SIGNS.exit, 0.56, 0.26, [0, 1.0, 0.037]), plane(SIGNS.exit, 0.56, 0.26, [0, 1.0, -0.037], Math.PI));
    const halo = new THREE.Sprite(new THREE.SpriteMaterial({ map: SPRITE_GLOW, color: "#27d37f", transparent: true, opacity: 0.45, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
    halo.position.set(0, 1.0, 0.06); halo.scale.set(1.1, 0.7, 1); halo.raycast = () => {};
    group.add(halo);
    blob(group, 0.5, 0.5);
  },
  extinguisher(group) {
    extinguisherSign(); const b = new Batch();
    standBase(b, 0.14);
    const body = [new THREE.Vector2(0.001, 0.04), new THREE.Vector2(0.085, 0.04), new THREE.Vector2(0.1, 0.07), new THREE.Vector2(0.1, 0.5), new THREE.Vector2(0.088, 0.58), new THREE.Vector2(0.05, 0.63), new THREE.Vector2(0.034, 0.66), new THREE.Vector2(0.001, 0.66)];
    b.add("gloss", new THREE.LatheGeometry(body, 28), "#cf2a22", [0, 0, 0]);
    b.cylY("chrome", "#d5dde6", 0.032, 0.036, 0.64, 0.72, 0, 0, 14);
    b.box("chrome", "#aeb8c4", 0.2, 0.026, 0.045, [0.03, 0.76, 0], [0, 0, 0.0], 0.01); // lever
    b.box("matte", "#14181f", 0.16, 0.022, 0.04, [0.0, 0.7, 0], [0, 0, 0.0], 0.008);
    b.cylY("metal", "#2a3341", 0.03, 0.03, 0.7, 0.8, 0.03, 0, 10);
    b.sphere("gloss", "#14181f", 0.018, [0.13, 0.72, 0], [1, 1, 1], 8);
    const hose = new THREE.CatmullRomCurve3([new THREE.Vector3(0.04, 0.68, 0), new THREE.Vector3(0.17, 0.62, 0.04), new THREE.Vector3(0.18, 0.4, 0.06), new THREE.Vector3(0.13, 0.22, 0.04), new THREE.Vector3(0.1, 0.14, 0.02)]);
    b.add("matte", new THREE.TubeGeometry(hose, 20, 0.014, 6), "#14181f");
    b.cylY("chrome", "#d5dde6", 0.026, 0.026, 0.56, 0.6, -0.085, 0, 10); // gauge
    b.meshes(group);
    const label = plane(SIGNS.ext, 0.15, 0.15, [0, 0.32, 0.101]);
    group.add(label);
    blob(group, 0.5, 0.5);
  },
  firstaid(group) {
    firstAidSign(); const b = new Batch();
    standBase(b, 0.2);
    b.cylY("chrome", "#dfe6ee", 0.016, 0.02, 0.035, 0.24, 0, 0, 10);
    b.box("gloss", "#f3f7fa", 0.5, 0.34, 0.16, [0, 0.43, 0], [0, 0, 0], 0.04);
    b.box("matte", "#16a05f", 0.52, 0.04, 0.17, [0, 0.61, 0], [0, 0, 0], 0.015);
    b.box("chrome", "#cdd5de", 0.14, 0.035, 0.05, [0, 0.665, 0], [0, 0, 0], 0.012);
    for (const x of [-0.15, 0.15]) b.box("chrome", "#cdd5de", 0.04, 0.06, 0.02, [x, 0.4, 0.085], [0, 0, 0], 0.006);
    b.meshes(group);
    group.add(plane(SIGNS.firstaid, 0.34, 0.225, [0, 0.43, 0.082]), plane(SIGNS.firstaid, 0.34, 0.225, [0, 0.43, -0.082], Math.PI));
    blob(group, 0.6, 0.45);
  },
  barrier(group) {
    const b = new Batch();
    for (const x of [-0.54, 0.54]) {
      b.box("metal", "#8e99a8", 0.06, 0.6, 0.06, [x, 0.3, 0], [0, 0, 0], 0.01);
      b.box("metal", "#8e99a8", 0.07, 0.04, 0.38, [x, 0.02, 0], [0, 0, 0], 0.01);
      b.cylY("gloss", "#c8261f", 0.03, 0.03, 0.6, 0.64, x, 0, 8);
    }
    b.meshes(group);
    for (const y of [0.5, 0.31]) {
      const mesh = new THREE.Mesh(new RoundedBoxGeometry(1.14, 0.14, 0.035, 2, 0.01), y > 0.4 ? MAT.stripes : MAT.stripesRed);
      mesh.position.set(0, y, 0);
      group.add(mesh);
    }
    for (const x of [-0.3, 0.3]) {
      const reflector = new THREE.Mesh(new THREE.CylinderGeometry(0.02, 0.02, 0.012, 12), new THREE.MeshStandardMaterial({ color: "#ff3b30", emissive: "#ff2a1a", emissiveIntensity: 0.4, roughness: 0.2 }));
      reflector.rotation.x = Math.PI / 2; reflector.position.set(x, 0.4, 0.024);
      group.add(reflector);
    }
    blob(group, 1.3, 0.45);
  },
  beacon(group) {
    const b = new Batch();
    b.cylY("matte", "#1a2230", 0.19, 0.22, 0, 0.07, 0, 0, 24);
    b.cylY("gloss", "#f2c230", 0.13, 0.19, 0.07, 0.27, 0, 0, 24);
    b.cylY("chrome", "#dfe6ee", 0.135, 0.135, 0.26, 0.285, 0, 0, 24);
    b.meshes(group);
    const dome = new THREE.MeshStandardMaterial({ color: "#ff9a2e", emissive: "#ff7a10", emissiveIntensity: 0.1, transparent: true, opacity: 0.88, roughness: 0.08, metalness: 0.0, envMapIntensity: 1.8 });
    const domeMesh = new THREE.Mesh(new THREE.SphereGeometry(0.13, 22, 12, 0, Math.PI * 2, 0, Math.PI / 2), dome);
    domeMesh.position.y = 0.285; domeMesh.name = "beacon-dome";
    group.add(domeMesh);
    // rotating reflector + light bar inside the dome, and the two light beams
    const rotor = new THREE.Group(); rotor.position.y = 0.37;
    const bar = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.05, 0.02), new THREE.MeshBasicMaterial({ color: "#fff3d0", toneMapped: false }));
    rotor.add(bar);
    const beamMaterial = new THREE.MeshBasicMaterial({ color: "#ff9a2e", map: coneTexture(), transparent: true, opacity: 0.0, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide, toneMapped: false, fog: false });
    for (const s of [1, -1]) {
      const geometry = new THREE.ConeGeometry(0.3, 2.4, 16, 1, true);
      geometry.translate(0, -1.2, 0);
      geometry.rotateZ(s * Math.PI / 2 * -1); // apex at the beacon, opening outward along +-x
      const beam = new THREE.Mesh(geometry, beamMaterial);
      beam.raycast = () => {};
      rotor.add(beam);
    }
    group.add(rotor);
    const halo = new THREE.Sprite(new THREE.SpriteMaterial({ map: SPRITE_GLOW, color: "#ff8a1f", transparent: true, opacity: 0, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
    halo.position.y = 0.37; halo.scale.setScalar(1.0); halo.raycast = () => {};
    group.add(halo);
    const pool = new THREE.Mesh(new THREE.CircleGeometry(0.9, 28), new THREE.MeshBasicMaterial({ map: SPRITE_GLOW, color: "#ff8a1f", transparent: true, opacity: 0, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
    pool.rotation.x = -Math.PI / 2; pool.position.y = 0.006; pool.raycast = () => {};
    group.add(pool);
    group.userData.dome = dome; group.userData.rotor = rotor; group.userData.beam = beamMaterial; group.userData.halo = halo; group.userData.pool = pool;
    group.userData.emitter = { y: 0.4, color: "#ff8a2a" };
    blob(group, 0.6, 0.6);
  },
  muster(group) {
    musterSign(); const b = new Batch();
    standBase(b, 0.17);
    b.cylY("chrome", "#dfe6ee", 0.02, 0.024, 0.035, 1.24, 0, 0, 10);
    b.box("gloss", "#e9eef4", 0.76, 0.52, 0.05, [0.0, 1.0, 0], [0, 0, 0], 0.02);
    b.meshes(group);
    group.add(plane(SIGNS.muster, 0.72, 0.48, [0, 1.0, 0.027]), plane(SIGNS.muster, 0.72, 0.48, [0, 1.0, -0.027], Math.PI));
    const ring = new THREE.Mesh(new THREE.RingGeometry(0.3, 0.38, 36), new THREE.MeshBasicMaterial({ color: "#1fa564", transparent: true, opacity: 0.9, side: THREE.DoubleSide }));
    ring.rotation.x = -Math.PI / 2; ring.position.y = 0.006; ring.raycast = () => {};
    group.add(ring);
    blob(group, 0.7, 0.7);
  },
};

export function buildCircuitPart2(part) {
  const group = new THREE.Group();
  circuitBuilders[part](group);
  group.userData.reach = TERMINAL[part];
  group.userData.leadY = Y0;
  return group;
}
export function buildSafetyPart2(part) {
  const group = new THREE.Group();
  safetyBuilders[part](group);
  return group;
}
export { Y0, blobMaterial, makeCanvas };
