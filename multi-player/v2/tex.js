// Procedural textures for Lab v2: nothing is downloaded. Every surface is baked once on a canvas (colour, roughness and a height map
// that is turned into a tangent-space normal map), so MeshStandardMaterial gets real PBR inputs without any image files.
import * as THREE from "three";

const lerp = (a, b, t) => a + (b - a) * t;
const clamp01 = (v) => Math.min(1, Math.max(0, v));
const mix = (c1, c2, t) => [lerp(c1[0], c2[0], t), lerp(c1[1], c2[1], t), lerp(c1[2], c2[2], t)];
export const rgb = (hex) => { const n = parseInt(hex.replace("#", ""), 16); return [(n >> 16) & 255, (n >> 8) & 255, n & 255]; };

// Seeded random numbers (so every client bakes the same boards).
export function rng(seed = 1) {
  let s = seed >>> 0 || 1;
  return () => { s = (s + 0x6d2b79f5) >>> 0; let t = s; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
}

// Periodic value noise: lattice of px x py cells that wraps, so textures tile without a seam.
export function wrapNoise(px, py, seed = 1) {
  const random = rng(seed);
  const grid = new Float32Array(px * py);
  for (let i = 0; i < grid.length; i += 1) grid[i] = random();
  const at = (x, y) => grid[(((y % py) + py) % py) * px + (((x % px) + px) % px)];
  return (x, y) => {
    const xi = Math.floor(x); const yi = Math.floor(y);
    const u = x - xi; const v = y - yi;
    const su = u * u * (3 - 2 * u); const sv = v * v * (3 - 2 * v);
    return lerp(lerp(at(xi, yi), at(xi + 1, yi), su), lerp(at(xi, yi + 1), at(xi + 1, yi + 1), su), sv);
  };
}
export const fbm = (noise, x, y, octaves = 3) => {
  let total = 0; let amp = 0.5; let norm = 0;
  for (let o = 0; o < octaves; o += 1) { total += noise(x * 2 ** o, y * 2 ** o) * amp; norm += amp; amp *= 0.5; }
  return total / norm;
};

export function makeCanvas(w, h = w) {
  const canvas = document.createElement("canvas");
  canvas.width = w; canvas.height = h;
  return canvas;
}

function toTexture(canvas, { srgb = false, repeat = [1, 1], aniso = 4 } = {}) {
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = srgb ? THREE.SRGBColorSpace : THREE.NoColorSpace;
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  texture.repeat.set(repeat[0], repeat[1]);
  texture.anisotropy = aniso;
  return texture;
}

// Height values (0..1 floats, size x size) to a normal map canvas. Wraps at the edges.
export function heightToNormal(height, size, strength = 2) {
  const canvas = makeCanvas(size);
  const g = canvas.getContext("2d");
  const out = g.createImageData(size, size);
  const at = (x, y) => height[(((y % size) + size) % size) * size + (((x % size) + size) % size)];
  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      const dx = (at(x + 1, y) - at(x - 1, y)) * strength;
      const dy = (at(x, y + 1) - at(x, y - 1)) * strength;
      const len = Math.hypot(dx, dy, 1);
      const o = (y * size + x) * 4;
      out.data[o] = (-dx / len * 0.5 + 0.5) * 255;
      out.data[o + 1] = (dy / len * 0.5 + 0.5) * 255; // canvas y runs down, texture v runs up: +dy is the OpenGL green axis
      out.data[o + 2] = (1 / len * 0.5 + 0.5) * 255;
      out.data[o + 3] = 255;
    }
  }
  g.putImageData(out, 0, 0);
  return canvas;
}

// Bakes colour, roughness and height from one function: fn(u, v, x, y) -> { c: [r,g,b], h: 0..1, r: 0..1 }.
export function bake(size, fn, { normalStrength = 2, repeat = [1, 1], aniso = 4 } = {}) {
  const colour = makeCanvas(size); const rough = makeCanvas(size);
  const cg = colour.getContext("2d"); const rg = rough.getContext("2d");
  const cd = cg.createImageData(size, size); const rd = rg.createImageData(size, size);
  const height = new Float32Array(size * size);
  for (let y = 0; y < size; y += 1) {
    for (let x = 0; x < size; x += 1) {
      const s = fn(x / size, y / size, x, y);
      const o = (y * size + x) * 4;
      cd.data[o] = s.c[0]; cd.data[o + 1] = s.c[1]; cd.data[o + 2] = s.c[2]; cd.data[o + 3] = 255;
      const r = clamp01(s.r) * 255;
      rd.data[o] = rd.data[o + 1] = rd.data[o + 2] = r; rd.data[o + 3] = 255;
      height[y * size + x] = s.h;
    }
  }
  cg.putImageData(cd, 0, 0); rg.putImageData(rd, 0, 0);
  return {
    map: toTexture(colour, { srgb: true, repeat, aniso }),
    roughnessMap: toTexture(rough, { repeat, aniso }),
    normalMap: toTexture(heightToNormal(height, size, normalStrength), { repeat, aniso }),
    canvas: colour,
  };
}

// ------------------------------------------------------------------ surfaces

// Planks with grain, knots in the noise, darker seams and a normal map that makes the seams and grain catch light.
export function woodSet({ size = 512, planks = 6, seed = 3, light = "#c58d52", dark = "#7d4a22", repeat = [1, 1], tint = null } = {}) {
  const grain = wrapNoise(2, 64, seed); const ring = wrapNoise(4, 8, seed + 7); const fine = wrapNoise(8, 128, seed + 3);
  const random = rng(seed);
  const plankTone = Array.from({ length: planks }, () => random());
  const plankShift = Array.from({ length: planks }, () => random());
  const lc = rgb(light); const dc = rgb(dark);
  const tint3 = tint ? rgb(tint) : null;
  return bake(size, (u, v) => {
    const p = Math.min(planks - 1, Math.floor(v * planks));
    const pv = v * planks - p; // 0..1 across the plank
    const seam = Math.min(pv, 1 - pv) < 0.018;
    const jointAt = (plankShift[p] * 0.8 + 0.1);
    const joint = Math.abs(u - jointAt) < 0.004 || Math.abs(u - jointAt + 1) < 0.004;
    const along = u + plankShift[p];
    const g = fbm(grain, (along * 2) % 2, (pv + p) * 6, 3);
    const rings = 0.5 + 0.5 * Math.sin((ring(along * 4, pv * 3 + p) * 8 + pv * 14) * Math.PI);
    const f = fine((along * 8) % 8, (pv + p) * 20);
    let t = clamp01(0.35 * g + 0.4 * rings + 0.25 * f);
    t = clamp01(t * 0.7 + plankTone[p] * 0.45 - 0.1);
    let c = mix(dc, lc, t);
    if (tint3) c = mix(c, tint3, 0.25);
    let h = 0.55 + (f - 0.5) * 0.25 + (rings - 0.5) * 0.12;
    if (seam || joint) { c = mix(c, [20, 12, 6], 0.75); h = 0.05; }
    return { c, h, r: 0.58 + (1 - t) * 0.2 + (seam ? 0.3 : 0) };
  }, { normalStrength: 3.2, repeat });
}

export function concreteSet({ size = 512, seed = 5, base = "#8d9299", repeat = [1, 1], joints = 2, polish = 0.45 } = {}) {
  const n1 = wrapNoise(8, 8, seed); const n2 = wrapNoise(32, 32, seed + 1); const n3 = wrapNoise(4, 4, seed + 2);
  const bc = rgb(base);
  return bake(size, (u, v) => {
    const a = fbm(n1, u * 8, v * 8, 3); const b = n2(u * 32, v * 32); const c = n3(u * 4, v * 4);
    let t = 0.55 + (a - 0.5) * 0.2 + (b - 0.5) * 0.14 + (c - 0.5) * 0.12;
    const lineU = Math.abs(((u * joints) % 1) - 0); const lineV = Math.abs(((v * joints) % 1) - 0);
    const joint = Math.min(lineU, 1 - lineU, lineV, 1 - lineV) < 0.004;
    let col = [bc[0] * t * 1.5, bc[1] * t * 1.5, bc[2] * t * 1.5];
    if (joint) col = [col[0] * 0.45, col[1] * 0.45, col[2] * 0.45];
    const speck = b > 0.93 ? 1.25 : 1;
    return { c: col.map((x) => Math.min(255, x * speck)), h: joint ? 0.1 : 0.6 + (b - 0.5) * 0.3, r: clamp01(polish + (1 - a) * 0.3 + (joint ? 0.35 : 0)) };
  }, { normalStrength: 1.4, repeat });
}

// Painted plaster wall tile (4 m wide, 4.2 m tall per repeat) with a Pitt navy dado, royal band and gold line.
export function wallSet({ size = 512, seed = 9, plaster = "#f0ece4", dado = "#00205B", band = true, repeat = [1, 1] } = {}) {
  const n1 = wrapNoise(16, 16, seed); const n2 = wrapNoise(64, 64, seed + 4);
  const pc = rgb(plaster); const dc = rgb(dado); const rc = rgb("#003594"); const gc = rgb("#FFB81C");
  const bottomV = (metres) => 1 - metres / 4.2;
  return bake(size, (u, v) => {
    const a = fbm(n1, u * 16, v * 16, 3); const b = n2(u * 64, v * 64);
    const t = 0.9 + (a - 0.5) * 0.07 + (b - 0.5) * 0.04;
    let c = [pc[0] * t, pc[1] * t, pc[2] * t];
    let r = 0.88;
    if (band) {
      if (v > bottomV(0.95)) { c = [dc[0] * (0.9 + a * 0.2), dc[1] * (0.9 + a * 0.2), dc[2] * (0.9 + a * 0.2)]; r = 0.55; }
      else if (v > bottomV(1.2) && v < bottomV(1.14)) { c = gc; r = 0.4; }
      else if (v > bottomV(1.6) && v <= bottomV(1.2)) { c = rc; r = 0.5; }
    }
    const seam = u < 0.004 && v < 0.99;
    return { c: seam ? [c[0] * 0.8, c[1] * 0.8, c[2] * 0.8] : c, h: 0.5 + (b - 0.5) * 0.3 + (a - 0.5) * 0.4, r };
  }, { normalStrength: 1.6, repeat });
}

// Corrugated painted metal sheet (safety bay walls).
export function corrugatedSet({ size = 512, seed = 12, base = "#7d8793", repeat = [1, 1] } = {}) {
  const n1 = wrapNoise(16, 16, seed); const n2 = wrapNoise(4, 64, seed + 1);
  const bc = rgb(base);
  return bake(size, (u, v) => {
    const wave = 0.5 + 0.5 * Math.sin(u * Math.PI * 2 * 16);
    const wear = fbm(n2, u * 4, v * 64, 3); const a = n1(u * 16, v * 16);
    const t = 0.7 + wave * 0.3 + (a - 0.5) * 0.15;
    const rust = wear > 0.72 ? (wear - 0.72) * 3 : 0;
    const c = mix([bc[0] * t, bc[1] * t, bc[2] * t], [150, 90, 55], clamp01(rust));
    return { c, h: wave * 0.9 + (a - 0.5) * 0.05, r: 0.45 + rust * 0.4 + (1 - wave) * 0.15 };
  }, { normalStrength: 5, repeat });
}

// Perforated pegboard with a warm board colour.
export function pegboardSet({ size = 512, holes = 16, repeat = [1, 1] } = {}) {
  const n1 = wrapNoise(8, 8, 21);
  const board = rgb("#b99a6b"); const hole = rgb("#2b2013");
  return bake(size, (u, v) => {
    const cx = ((u * holes) % 1) - 0.5; const cy = ((v * holes) % 1) - 0.5;
    const d = Math.hypot(cx, cy);
    const t = 0.85 + (n1(u * 8, v * 8) - 0.5) * 0.3;
    const inHole = d < 0.17;
    const rim = d < 0.24;
    const c = inHole ? hole : [board[0] * t * (rim ? 0.9 : 1), board[1] * t * (rim ? 0.9 : 1), board[2] * t * (rim ? 0.9 : 1)];
    return { c, h: inHole ? 0 : rim ? 0.6 : 0.8, r: inHole ? 1 : 0.8 };
  }, { normalStrength: 4, repeat });
}

// Brushed metal: long streaks in the roughness and normal maps, colour from `base`.
export function brushedSet({ size = 256, seed = 31, base = "#aab4c0", repeat = [1, 1] } = {}) {
  const streak = wrapNoise(2, 128, seed); const bc = rgb(base);
  return bake(size, (u, v) => {
    const s = fbm(streak, u * 2, v * 128, 2);
    const t = 0.82 + s * 0.3;
    return { c: [bc[0] * t, bc[1] * t, bc[2] * t], h: s, r: 0.28 + s * 0.22 };
  }, { normalStrength: 1.2, repeat });
}

// Rubber work mat for the Circuit Lab bench: dark green-navy, a printed grid, tick marks and a corner legend.
export function matSet({ size = 1024, grid = 28 } = {}) {
  const colour = makeCanvas(size); const g = colour.getContext("2d");
  const grad = g.createRadialGradient(size / 2, size / 2, size * 0.1, size / 2, size / 2, size * 0.72);
  grad.addColorStop(0, "#143a63"); grad.addColorStop(1, "#0a1f3d");
  g.fillStyle = grad; g.fillRect(0, 0, size, size);
  const random = rng(77);
  for (let i = 0; i < 9000; i += 1) { g.fillStyle = `rgba(255,255,255,${random() * 0.035})`; g.fillRect(random() * size, random() * size, 1 + random() * 2, 1 + random() * 2); }
  const cell = size / grid;
  g.strokeStyle = "rgba(160,205,255,0.16)"; g.lineWidth = 1.5;
  for (let i = 0; i <= grid; i += 1) { g.beginPath(); g.moveTo(i * cell, 0); g.lineTo(i * cell, size); g.moveTo(0, i * cell); g.lineTo(size, i * cell); g.stroke(); }
  g.strokeStyle = "rgba(160,205,255,0.34)"; g.lineWidth = 2.5;
  for (let i = 0; i <= grid; i += 4) { g.beginPath(); g.moveTo(i * cell, 0); g.lineTo(i * cell, size); g.moveTo(0, i * cell); g.lineTo(size, i * cell); g.stroke(); }
  g.strokeStyle = "rgba(255,184,28,0.55)"; g.lineWidth = 5; g.beginPath(); g.arc(size / 2, size / 2, size * 0.412, 0, Math.PI * 2); g.stroke();
  g.setLineDash([14, 14]); g.lineWidth = 2; g.strokeStyle = "rgba(255,184,28,0.35)"; g.beginPath(); g.arc(size / 2, size / 2, size * 0.385, 0, Math.PI * 2); g.stroke(); g.setLineDash([]);
  const map = new THREE.CanvasTexture(colour); map.colorSpace = THREE.SRGBColorSpace; map.anisotropy = 8;
  const heightData = new Float32Array(256 * 256);
  const n = wrapNoise(64, 64, 4);
  for (let y = 0; y < 256; y += 1) for (let x = 0; x < 256; x += 1) heightData[y * 256 + x] = n(x / 4, y / 4) * 0.6 + 0.2;
  const normalMap = toTexture(heightToNormal(heightData, 256, 0.45), { repeat: [4, 4] });
  return { map, normalMap, canvas: colour };
}

// Painted safety-bay floor deck: concrete with yellow border line, hazard-striped edge, a muster circle and "KEEP CLEAR" lane text.
export function bayFloorSet({ size = 1024 } = {}) {
  const base = concreteSet({ size: 512, seed: 41, base: "#a3a9b1", polish: 0.4 });
  const colour = makeCanvas(size); const g = colour.getContext("2d");
  g.drawImage(base.canvas, 0, 0, size, size);
  g.fillStyle = "rgba(10,14,22,0.12)"; g.fillRect(0, 0, size, size);
  const c = size / 2;
  g.lineWidth = size * 0.012; g.strokeStyle = "#f2c230"; g.beginPath(); g.arc(c, c, size * 0.43, 0, Math.PI * 2); g.stroke();
  // hazard-stripe ring
  g.save(); g.beginPath(); g.arc(c, c, size * 0.5, 0, Math.PI * 2); g.arc(c, c, size * 0.46, 0, Math.PI * 2, true); g.clip();
  g.fillStyle = "#1b1f26"; g.fillRect(0, 0, size, size);
  g.fillStyle = "#f2c230";
  for (let i = -size; i < size * 2; i += 44) { g.beginPath(); g.moveTo(i, 0); g.lineTo(i + 22, 0); g.lineTo(i + 22 - size, size); g.lineTo(i - size, size); g.closePath(); g.fill(); }
  g.restore();
  // walkway edge lines (green) and arrow decals
  g.strokeStyle = "rgba(31,165,100,0.75)"; g.lineWidth = size * 0.008; g.setLineDash([size * 0.04, size * 0.025]);
  g.beginPath(); g.arc(c, c, size * 0.37, 0, Math.PI * 2); g.stroke(); g.setLineDash([]);
  g.fillStyle = "rgba(242,194,48,0.9)";
  for (const [x, y, a] of [[c, c + size * 0.31, 0], [c - size * 0.31, c, Math.PI / 2], [c + size * 0.31, c, -Math.PI / 2], [c, c - size * 0.31, Math.PI]]) {
    g.save(); g.translate(x, y); g.rotate(a); g.beginPath(); g.moveTo(0, -size * 0.022); g.lineTo(size * 0.02, size * 0.012); g.lineTo(-size * 0.02, size * 0.012); g.closePath(); g.fill(); g.restore();
  }
  g.fillStyle = "rgba(255,255,255,0.7)"; g.font = `${size * 0.035}px 'League Gothic', Impact, sans-serif`; g.textAlign = "center";
  g.fillText("KEEP CLEAR", c, c + size * 0.43 - size * 0.013 + size * 0.0);
  const map = new THREE.CanvasTexture(colour); map.colorSpace = THREE.SRGBColorSpace; map.anisotropy = 8;
  map.userData = { redraw: () => { map.needsUpdate = true; } };
  return { map, normalMap: base.normalMap, roughnessMap: base.roughnessMap, canvas: colour };
}

// ------------------------------------------------------------------ small decals and sprites

export function glowTexture(stops = [[0, "rgba(255,255,255,1)"], [0.25, "rgba(255,255,255,0.45)"], [1, "rgba(255,255,255,0)"]], size = 128) {
  const canvas = makeCanvas(size); const g = canvas.getContext("2d");
  const grad = g.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  for (const [at, color] of stops) grad.addColorStop(at, color);
  g.fillStyle = grad; g.fillRect(0, 0, size, size);
  const texture = new THREE.CanvasTexture(canvas); texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

// Vertical alpha ramp for light cones: strong at the apex (u=0 at the top, v=1) fading to nothing at the base.
export function coneTexture() {
  const canvas = makeCanvas(8, 64); const g = canvas.getContext("2d");
  const grad = g.createLinearGradient(0, 0, 0, 64);
  grad.addColorStop(0, "rgba(255,255,255,0.95)"); grad.addColorStop(0.35, "rgba(255,255,255,0.35)"); grad.addColorStop(1, "rgba(255,255,255,0)");
  g.fillStyle = grad; g.fillRect(0, 0, 8, 64);
  return new THREE.CanvasTexture(canvas);
}

// A soft dark blob for a contact shadow (cheap stand-in for a shadow map inside a headset).
export function blobShadowTexture() {
  return glowTexture([[0, "rgba(0,0,0,0.55)"], [0.5, "rgba(0,0,0,0.25)"], [1, "rgba(0,0,0,0)"]], 64);
}

export function hazardStripeTexture(a = "#f2c230", b = "#14181f", stripes = 8, size = 256) {
  const canvas = makeCanvas(size, size / 4); const g = canvas.getContext("2d");
  g.fillStyle = b; g.fillRect(0, 0, size, size / 4);
  g.fillStyle = a;
  const step = size / stripes; const h = size / 4;
  for (let i = -stripes; i < stripes * 2; i += 2) { g.beginPath(); g.moveTo(i * step, h); g.lineTo(i * step + step, h); g.lineTo(i * step + step + h, 0); g.lineTo(i * step + h, 0); g.closePath(); g.fill(); }
  const texture = new THREE.CanvasTexture(canvas); texture.colorSpace = THREE.SRGBColorSpace; texture.anisotropy = 4; texture.wrapS = THREE.RepeatWrapping;
  return texture;
}

// A flat canvas label drawn by `draw(g, w, h)`, as a sRGB texture (battery wrap, signs, decals).
export function labelTexture(w, h, draw) {
  const canvas = makeCanvas(w, h);
  draw(canvas.getContext("2d"), w, h);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace; texture.anisotropy = 4;
  texture.userData.canvas = canvas; texture.userData.draw = draw;
  return texture;
}

// Re-draw label textures once the brand fonts have loaded.
export function refreshLabels(textures) {
  for (const t of textures) { const c = t.userData.canvas; if (c) { c.getContext("2d").clearRect(0, 0, c.width, c.height); t.userData.draw(c.getContext("2d"), c.width, c.height); t.needsUpdate = true; } }
}
