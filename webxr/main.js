// Color Field: the WebXR Device API used directly (navigator.xr, XRSession, XRWebGLLayer, XRFrame poses) with a small
// hand-written WebGL2 renderer. No engine, no library, no external assets. One renderer draws both the desktop preview
// (mouse/touch orbit camera) and the immersive session (the headset's own view and projection matrices).
const canvas = document.querySelector("#scene");
const enterButton = document.querySelector("#enter");
const paletteButton = document.querySelector("#palette");
const statusEl = document.querySelector("#status");
const speedControl = document.querySelector("#speed");
const speedValue = document.querySelector("#speed-value");
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

// ---------------------------------------------------------------------------------------------- tiny matrix kit
const m4 = {
  identity: () => new Float32Array([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]),
  mul(a, b) {
    const out = new Float32Array(16);
    for (let c = 0; c < 4; c += 1) for (let r = 0; r < 4; r += 1) {
      out[c * 4 + r] = a[r] * b[c * 4] + a[4 + r] * b[c * 4 + 1] + a[8 + r] * b[c * 4 + 2] + a[12 + r] * b[c * 4 + 3];
    }
    return out;
  },
  translate: (x, y, z) => new Float32Array([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, x, y, z, 1]),
  scale: (x, y, z) => new Float32Array([x, 0, 0, 0, 0, y, 0, 0, 0, 0, z, 0, 0, 0, 0, 1]),
  rotX(a) { const c = Math.cos(a), s = Math.sin(a); return new Float32Array([1, 0, 0, 0, 0, c, s, 0, 0, -s, c, 0, 0, 0, 0, 1]); },
  rotY(a) { const c = Math.cos(a), s = Math.sin(a); return new Float32Array([c, 0, -s, 0, 0, 1, 0, 0, s, 0, c, 0, 0, 0, 0, 1]); },
  rotZ(a) { const c = Math.cos(a), s = Math.sin(a); return new Float32Array([c, s, 0, 0, -s, c, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]); },
  perspective(fovY, aspect, near, far) {
    const f = 1 / Math.tan(fovY / 2), nf = 1 / (near - far);
    return new Float32Array([f / aspect, 0, 0, 0, 0, f, 0, 0, 0, 0, (far + near) * nf, -1, 0, 0, 2 * far * near * nf, 0]);
  },
  lookAt(eye, target, up = [0, 1, 0]) {
    const z = norm(sub(eye, target)), x = norm(cross(up, z)), y = cross(z, x);
    return new Float32Array([x[0], y[0], z[0], 0, x[1], y[1], z[1], 0, x[2], y[2], z[2], 0, -dot(x, eye), -dot(y, eye), -dot(z, eye), 1]);
  },
  compose(...ms) { return ms.reduce((acc, m) => m4.mul(acc, m)); },
};
function sub(a, b) { return [a[0] - b[0], a[1] - b[1], a[2] - b[2]]; }
function cross(a, b) { return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]; }
function dot(a, b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
function norm(a) { const l = Math.hypot(...a) || 1; return [a[0] / l, a[1] / l, a[2] / l]; }
function hsl(h, s, l) {
  h = ((h % 360) + 360) % 360 / 360;
  const f = (n) => { const k = (n + h * 12) % 12; return l - s * Math.min(l, 1 - l) * Math.max(-1, Math.min(k - 3, 9 - k, 1)); };
  return [f(0), f(8), f(4)];
}

// ---------------------------------------------------------------------------------------------- GL setup
const gl = canvas.getContext("webgl2", { antialias: true, alpha: false, xrCompatible: true });
if (!gl) {
  statusEl.textContent = "This browser could not create a WebGL2 context, so the scene cannot be drawn.";
  enterButton.textContent = "WebGL2 unavailable";
  throw new Error("WebGL2 is not available");
}

function program(vertex, fragment) {
  const build = (type, source) => {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader));
    return shader;
  };
  const p = gl.createProgram();
  gl.attachShader(p, build(gl.VERTEX_SHADER, vertex));
  gl.attachShader(p, build(gl.FRAGMENT_SHADER, fragment));
  gl.linkProgram(p);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(p));
  const cache = new Map();
  p.u = (name) => { if (!cache.has(name)) cache.set(name, gl.getUniformLocation(p, name)); return cache.get(name); };
  return p;
}

const HEAD = "#version 300 es\nprecision highp float;\n";
const meshVS = HEAD + `in vec3 aPos; in vec3 aNormal; uniform mat4 uModel, uViewProj; out vec3 vPos; out vec3 vN;
void main(){ vec4 w = uModel * vec4(aPos, 1.0); vPos = w.xyz; vN = mat3(uModel) * aNormal; gl_Position = uViewProj * w; }`;
const litFS = HEAD + `in vec3 vPos; in vec3 vN; uniform vec3 uCam, uColor, uEmissive, uRim; uniform float uAlpha; out vec4 o;
void main(){
  vec3 n = normalize(vN); vec3 v = normalize(uCam - vPos); if (!gl_FrontFacing) n = -n;
  vec3 l = normalize(vec3(-0.4, 0.8, 0.5));
  float d = max(dot(n, l), 0.0), hemi = 0.5 + 0.5 * n.y;
  float spec = pow(max(dot(n, normalize(l + v)), 0.0), 80.0), fres = pow(1.0 - max(dot(n, v), 0.0), 3.0);
  vec3 c = uColor * (0.22 + 0.5 * hemi + 0.75 * d) + uEmissive + uRim * fres + vec3(spec) * 0.9;
  o = vec4(pow(c, vec3(1.0 / 2.2)), uAlpha);
}`;
const skyFS = HEAD + `in vec3 vPos; in vec3 vN; uniform vec3 uCam, uA, uB; uniform float uTime; out vec4 o;
void main(){
  vec3 d = normalize(vPos - uCam); float h = clamp(d.y * 0.5 + 0.5, 0.0, 1.0);
  vec3 top = vec3(0.015, 0.025, 0.08) + uB * 0.16, hor = vec3(0.02, 0.03, 0.07) + uA * 0.36;
  vec3 c = mix(hor, top, smoothstep(0.48, 0.95, h));
  float band = 0.0;
  for (int i = 0; i < 3; i++) { float fi = float(i);
    band += smoothstep(0.65, 1.0, sin(d.x * (3.0 + fi) + d.z * 2.3 + uTime * (0.15 + 0.06 * fi) + fi * 2.0) * 0.5 + 0.5) * (0.34 - 0.08 * fi); }
  band *= smoothstep(0.5, 0.6, h) * (1.0 - smoothstep(0.64, 0.92, h));
  c += mix(uA, uB, d.x * 0.5 + 0.5) * band;
  vec3 q = floor(d * 170.0); float r = fract(sin(dot(q, vec3(12.9898, 78.233, 37.719))) * 43758.5453);
  c += step(0.9972, r) * smoothstep(0.58, 0.8, h) * (0.6 + 0.4 * sin(uTime * 2.0 + r * 50.0));
  c = mix(c, vec3(0.008, 0.014, 0.04), 1.0 - smoothstep(0.38, 0.5, h));
  o = vec4(pow(c, vec3(1.0 / 2.2)), 1.0);
}`;
const floorFS = HEAD + `in vec3 vPos; in vec3 vN; uniform vec3 uA, uB, uCenter; uniform float uTime; out vec4 o;
void main(){
  vec2 p = vPos.xz - uCenter.xz; float r = length(p);
  vec2 g = abs(fract(p - 0.5) - 0.5) / fwidth(p);
  float line = 1.0 - min(min(g.x, g.y), 1.0);
  float ripple = smoothstep(0.82, 1.0, sin(r * 5.0 - uTime * 1.6) * 0.5 + 0.5) * exp(-r * 0.35);
  float fade = 1.0 - smoothstep(2.0, 6.4, r);
  vec3 col = mix(uA, uB, clamp(r / 6.0, 0.0, 1.0));
  vec3 c = col * (0.16 + 0.8 * line + 1.1 * ripple) + col * 0.35 * exp(-r * 0.9);
  o = vec4(pow(c, vec3(1.0 / 2.2)), (0.18 + 0.5 * line + 0.5 * ripple + 0.35 * exp(-r)) * fade);
}`;
const pointVS = HEAD + `in vec4 aSeed; uniform mat4 uViewProj, uModel; uniform float uTime, uScale; out float vTw;
void main(){
  float a = aSeed.x + uTime * 0.1 * aSeed.z, rad = aSeed.y;
  float y = mod(aSeed.w * 4.0 + uTime * (0.12 + 0.1 * aSeed.z), 4.2);
  vec3 p = vec3(cos(a) * rad, 0.1 + y, sin(a) * rad);
  vec4 clip = uViewProj * uModel * vec4(p, 1.0); gl_Position = clip;
  gl_PointSize = uScale * (0.05 + 0.05 * aSeed.z) / max(clip.w, 0.1);
  vTw = 0.5 + 0.5 * sin(uTime * 2.0 + aSeed.w * 30.0);
}`;
const pointFS = HEAD + `in float vTw; uniform vec3 uA; out vec4 o;
void main(){ float d = length(gl_PointCoord - 0.5) * 2.0; float a = smoothstep(1.0, 0.0, d); o = vec4(mix(uA, vec3(1.0), 0.6) * a * (0.5 + 0.5 * vTw), a * 0.9); }`;

const programs = {
  lit: program(meshVS, litFS),
  sky: program(meshVS, skyFS),
  floor: program(meshVS, floorFS),
  points: program(pointVS, pointFS),
};

// ---------------------------------------------------------------------------------------------- geometry
function makeMesh(positions, normals) {
  const vao = gl.createVertexArray();
  gl.bindVertexArray(vao);
  for (const [location, data] of [[0, positions], [1, normals]]) {
    const buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(data), gl.STATIC_DRAW);
    gl.enableVertexAttribArray(location);
    gl.vertexAttribPointer(location, 3, gl.FLOAT, false, 0, 0);
  }
  gl.bindVertexArray(null);
  return { vao, count: positions.length / 3 };
}
function icosphere(level, flat) {
  const t = (1 + Math.sqrt(5)) / 2;
  let verts = [[-1, t, 0], [1, t, 0], [-1, -t, 0], [1, -t, 0], [0, -1, t], [0, 1, t], [0, -1, -t], [0, 1, -t], [t, 0, -1], [t, 0, 1], [-t, 0, -1], [-t, 0, 1]].map(norm);
  let faces = [[0, 11, 5], [0, 5, 1], [0, 1, 7], [0, 7, 10], [0, 10, 11], [1, 5, 9], [5, 11, 4], [11, 10, 2], [10, 7, 6], [7, 1, 8], [3, 9, 4], [3, 4, 2], [3, 2, 6], [3, 6, 8], [3, 8, 9], [4, 9, 5], [2, 4, 11], [6, 2, 10], [8, 6, 7], [9, 8, 1]];
  for (let i = 0; i < level; i += 1) {
    const cache = new Map();
    const mid = (a, b) => {
      const key = a < b ? `${a}_${b}` : `${b}_${a}`;
      if (!cache.has(key)) { verts.push(norm([(verts[a][0] + verts[b][0]) / 2, (verts[a][1] + verts[b][1]) / 2, (verts[a][2] + verts[b][2]) / 2])); cache.set(key, verts.length - 1); }
      return cache.get(key);
    };
    faces = faces.flatMap(([a, b, c]) => { const ab = mid(a, b), bc = mid(b, c), ca = mid(c, a); return [[a, ab, ca], [b, bc, ab], [c, ca, bc], [ab, bc, ca]]; });
  }
  const positions = [], normals = [];
  for (const [a, b, c] of faces) {
    const n = norm(cross(sub(verts[b], verts[a]), sub(verts[c], verts[a])));
    for (const i of [a, b, c]) { positions.push(...verts[i]); normals.push(...(flat ? n : verts[i])); }
  }
  return makeMesh(positions, normals);
}
function torus(R, r, seg, tube) {
  const positions = [], normals = [];
  const at = (i, j) => {
    const u = (i / seg) * Math.PI * 2, v = (j / tube) * Math.PI * 2;
    const nx = Math.cos(v) * Math.cos(u), ny = Math.sin(v), nz = Math.cos(v) * Math.sin(u);
    return { p: [(R + r * Math.cos(v)) * Math.cos(u), r * ny, (R + r * Math.cos(v)) * Math.sin(u)], n: [nx, ny, nz] };
  };
  for (let i = 0; i < seg; i += 1) for (let j = 0; j < tube; j += 1) {
    const a = at(i, j), b = at(i + 1, j), c = at(i + 1, j + 1), d = at(i, j + 1);
    for (const q of [a, b, c, a, c, d]) { positions.push(...q.p); normals.push(...q.n); }
  }
  return makeMesh(positions, normals);
}
function disc(radius, seg) {
  const positions = [], normals = [];
  for (let i = 0; i < seg; i += 1) {
    const a = (i / seg) * Math.PI * 2, b = ((i + 1) / seg) * Math.PI * 2;
    positions.push(0, 0, 0, Math.cos(b) * radius, 0, Math.sin(b) * radius, Math.cos(a) * radius, 0, Math.sin(a) * radius);
    normals.push(0, 1, 0, 0, 1, 0, 0, 1, 0);
  }
  return makeMesh(positions, normals);
}
const meshes = { gem: icosphere(1, true), orb: icosphere(2, false), sky: icosphere(2, false), ring: torus(1, 0.014, 160, 8), floor: disc(7, 96) };

const POINTS = 420;
const pointVao = gl.createVertexArray();
gl.bindVertexArray(pointVao);
{
  const seeds = new Float32Array(POINTS * 4);
  for (let i = 0; i < POINTS; i += 1) seeds.set([Math.random() * 6.283, 0.7 + Math.random() * 2.8, 0.3 + Math.random(), Math.random()], i * 4);
  const buffer = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
  gl.bufferData(gl.ARRAY_BUFFER, seeds, gl.STATIC_DRAW);
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 4, gl.FLOAT, false, 0, 0);
}
gl.bindVertexArray(null);
for (const p of [programs.lit, programs.sky, programs.floor]) { gl.bindAttribLocation(p, 0, "aPos"); gl.bindAttribLocation(p, 1, "aNormal"); gl.linkProgram(p); }
gl.bindAttribLocation(programs.points, 0, "aSeed"); gl.linkProgram(programs.points);

// ---------------------------------------------------------------------------------------------- scene state
const palettes = [{ name: "Aurora", hue: 0 }, { name: "Sunset", hue: 120 }, { name: "Meadow", hue: 190 }, { name: "Ocean", hue: 260 }];
let paletteIndex = 0;
const startTime = performance.now();
let simTime = 0, lastNow = performance.now();
function setPalette(index) {
  paletteIndex = (index + palettes.length) % palettes.length;
  statusEl.textContent = `Palette: ${palettes[paletteIndex].name} (${paletteIndex + 1} of ${palettes.length}).`;
}
function colors() {
  const drift = simTime * 14 + palettes[paletteIndex].hue;
  return { a: hsl(190 + drift, 0.85, 0.5), b: hsl(285 + drift, 0.8, 0.45), c: hsl(35 + drift, 0.95, 0.6) };
}

function setCommon(p, viewProj, model, cam) {
  gl.uniformMatrix4fv(p.u("uViewProj"), false, viewProj);
  gl.uniformMatrix4fv(p.u("uModel"), false, model);
  if (cam) gl.uniform3fv(p.u("uCam"), cam);
}
function drawMesh(mesh, mode = gl.TRIANGLES) { gl.bindVertexArray(mesh.vao); gl.drawArrays(mode, 0, mesh.count); }
function lit(viewProj, cam, mesh, model, { color, emissive = [0, 0, 0], rim = [0, 0, 0], alpha = 1 }) {
  const p = programs.lit;
  gl.useProgram(p);
  setCommon(p, viewProj, model, cam);
  gl.uniform3fv(p.u("uColor"), color); gl.uniform3fv(p.u("uEmissive"), emissive); gl.uniform3fv(p.u("uRim"), rim); gl.uniform1f(p.u("uAlpha"), alpha);
  drawMesh(mesh);
}

// Draws the whole scene for one eye / camera. `origin` is where the exhibit stands in the viewer's space.
function drawScene({ viewProj, cam, origin, width, height, controllers = [] }) {
  const t = simTime, { a, b, c } = colors();
  const [ox, oy, oz] = origin;
  gl.enable(gl.DEPTH_TEST);
  gl.depthFunc(gl.LEQUAL);
  gl.enable(gl.CULL_FACE);

  // sky: a big sphere centred on the viewer, drawn first without writing depth
  gl.disable(gl.DEPTH_TEST); gl.disable(gl.CULL_FACE); gl.disable(gl.BLEND);
  gl.useProgram(programs.sky);
  setCommon(programs.sky, viewProj, m4.compose(m4.translate(...cam), m4.scale(60, 60, 60)), cam);
  gl.uniform3fv(programs.sky.u("uA"), a); gl.uniform3fv(programs.sky.u("uB"), b); gl.uniform1f(programs.sky.u("uTime"), t);
  drawMesh(meshes.sky);
  gl.enable(gl.DEPTH_TEST); gl.enable(gl.CULL_FACE);

  // glowing floor (alpha blended)
  gl.enable(gl.BLEND); gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA); gl.disable(gl.CULL_FACE); gl.depthMask(false);
  gl.useProgram(programs.floor);
  setCommon(programs.floor, viewProj, m4.translate(ox, oy, oz), cam);
  gl.uniform3fv(programs.floor.u("uA"), a); gl.uniform3fv(programs.floor.u("uB"), b); gl.uniform3f(programs.floor.u("uCenter"), ox, oy, oz); gl.uniform1f(programs.floor.u("uTime"), t);
  drawMesh(meshes.floor);
  gl.depthMask(true); gl.enable(gl.CULL_FACE); gl.disable(gl.BLEND);

  // gem with an inner core
  const bob = Math.sin(t * 1.1) * 0.08, gemY = oy + 1.4 + bob;
  lit(viewProj, cam, meshes.gem, m4.compose(m4.translate(ox, gemY, oz), m4.rotY(t * 0.5), m4.rotX(t * 0.25), m4.scale(0.8, 1.05, 0.8)), { color: [a[0] * 0.5 + 0.08, a[1] * 0.5 + 0.08, a[2] * 0.5 + 0.08], rim: c, emissive: [a[0] * 0.12, a[1] * 0.12, a[2] * 0.12] });
  lit(viewProj, cam, meshes.orb, m4.compose(m4.translate(ox, gemY, oz), m4.scale(0.28, 0.28, 0.28)), { color: [0, 0, 0], emissive: [1.5 * b[0] + 0.4, 1.5 * b[1] + 0.4, 1.5 * b[2] + 0.4] });

  // three orbiting rings in the palette colours
  [[a, 1.5, 0.0, 0.35], [b, 1.9, 1.05, -0.28], [c, 2.3, 2.1, 0.2]].forEach(([col, radius, tilt, speed], i) => {
    lit(viewProj, cam, meshes.ring, m4.compose(m4.translate(ox, gemY, oz), m4.rotY(t * speed + i), m4.rotX(0.5 + tilt * 0.4), m4.rotZ(tilt * 0.5), m4.scale(radius, radius, radius)), { color: [0, 0, 0], emissive: col.map((v) => v * 1.6 + 0.1) });
    const ang = t * (0.6 + i * 0.25) + i * 2.1;
    const orbit = m4.compose(m4.translate(ox, gemY, oz), m4.rotY(t * speed + i), m4.rotX(0.5 + tilt * 0.4), m4.rotZ(tilt * 0.5), m4.translate(Math.cos(ang) * radius, 0, Math.sin(ang) * radius), m4.scale(0.07, 0.07, 0.07));
    lit(viewProj, cam, meshes.orb, orbit, { color: [0, 0, 0], emissive: col.map((v) => v * 2 + 0.3) });
  });

  // rising sparkles (additive)
  gl.enable(gl.BLEND); gl.blendFunc(gl.SRC_ALPHA, gl.ONE); gl.depthMask(false);
  gl.useProgram(programs.points);
  gl.uniformMatrix4fv(programs.points.u("uViewProj"), false, viewProj);
  gl.uniformMatrix4fv(programs.points.u("uModel"), false, m4.translate(ox, oy, oz));
  gl.uniform3fv(programs.points.u("uA"), a); gl.uniform1f(programs.points.u("uTime"), t); gl.uniform1f(programs.points.u("uScale"), height * 0.9);
  gl.bindVertexArray(pointVao);
  gl.drawArrays(gl.POINTS, 0, POINTS);
  gl.depthMask(true); gl.disable(gl.BLEND);

  // VR controllers: a marker at each grip and a thin ray along the target-ray direction
  for (const { grip, ray } of controllers) {
    if (grip) lit(viewProj, cam, meshes.orb, m4.mul(grip, m4.scale(0.025, 0.025, 0.025)), { color: [0.1, 0.1, 0.1], emissive: [1, 0.72, 0.11] });
    if (ray) lit(viewProj, cam, meshes.orb, m4.compose(ray, m4.translate(0, 0, -0.6), m4.scale(0.003, 0.003, 0.6)), { color: [0, 0, 0], emissive: [1, 0.72, 0.11] });
  }
}

// ---------------------------------------------------------------------------------------------- desktop preview
const orbit = { yaw: 0.35, pitch: 0.18, dist: 7.4, target: [0, 1.35, 0], idle: 0 };
const home = { ...orbit };
function resize() {
  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  const w = Math.max(1, Math.round(canvas.clientWidth * dpr)), h = Math.max(1, Math.round(canvas.clientHeight * dpr));
  if (canvas.width !== w || canvas.height !== h) { canvas.width = w; canvas.height = h; }
}
function drawPreview() {
  resize();
  gl.bindFramebuffer(gl.FRAMEBUFFER, null);
  gl.viewport(0, 0, canvas.width, canvas.height);
  gl.clearColor(0, 0, 0, 1);
  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
  const portrait = canvas.width < canvas.height;
  const dist = orbit.dist * (portrait ? 1.7 : 1);
  const eye = [orbit.target[0] + dist * Math.sin(orbit.yaw) * Math.cos(orbit.pitch), orbit.target[1] + dist * Math.sin(orbit.pitch), orbit.target[2] + dist * Math.cos(orbit.yaw) * Math.cos(orbit.pitch)];
  const view = m4.lookAt(eye, orbit.target);
  const proj = m4.perspective(0.9, canvas.width / canvas.height, 0.05, 200);
  drawScene({ viewProj: m4.mul(proj, view), cam: eye, origin: [0, 0, 0], width: canvas.width, height: canvas.height });
}

let dragging = new Map();
canvas.addEventListener("pointerdown", (event) => { canvas.setPointerCapture(event.pointerId); dragging.set(event.pointerId, { x: event.clientX, y: event.clientY }); canvas.focus({ preventScroll: true }); });
canvas.addEventListener("pointermove", (event) => {
  const last = dragging.get(event.pointerId);
  if (!last) return;
  if (dragging.size === 2) {
    const [p, q] = [...dragging.values()];
    const before = Math.hypot(p.x - q.x, p.y - q.y);
    dragging.set(event.pointerId, { x: event.clientX, y: event.clientY });
    const [p2, q2] = [...dragging.values()];
    orbit.dist = Math.min(14, Math.max(2.5, orbit.dist * (before / (Math.hypot(p2.x - q2.x, p2.y - q2.y) || before))));
    return;
  }
  orbit.yaw -= (event.clientX - last.x) * 0.006;
  orbit.pitch = Math.min(1.2, Math.max(-0.05, orbit.pitch + (event.clientY - last.y) * 0.006));
  dragging.set(event.pointerId, { x: event.clientX, y: event.clientY });
  orbit.idle = 0;
});
for (const type of ["pointerup", "pointercancel"]) canvas.addEventListener(type, (event) => dragging.delete(event.pointerId));
canvas.addEventListener("wheel", (event) => { event.preventDefault(); orbit.dist = Math.min(14, Math.max(2.5, orbit.dist * (1 + Math.sign(event.deltaY) * 0.08))); }, { passive: false });
window.addEventListener("keydown", (event) => {
  if (event.target instanceof HTMLElement && event.target.closest("button, input, summary")) return;
  const key = event.key;
  if (key === "ArrowLeft") orbit.yaw -= 0.08; else if (key === "ArrowRight") orbit.yaw += 0.08;
  else if (key === "ArrowUp") orbit.pitch = Math.min(1.2, orbit.pitch + 0.05); else if (key === "ArrowDown") orbit.pitch = Math.max(-0.05, orbit.pitch - 0.05);
  else if (key === "c" || key === "C") setPalette(paletteIndex + 1);
  else if (key === "+" || key === "=") setSpeed(Number(speedControl.value) + 0.2);
  else if (key === "-" || key === "_") setSpeed(Number(speedControl.value) - 0.2);
  else if (key === "r" || key === "R") Object.assign(orbit, home);
});
function setSpeed(value) {
  speedControl.value = String(Math.min(2.2, Math.max(0.4, value)));
  speedValue.textContent = `${Number(speedControl.value).toFixed(1)}x`;
}
speedControl.addEventListener("input", () => setSpeed(Number(speedControl.value)));
paletteButton.addEventListener("click", () => setPalette(paletteIndex + 1));
if (reducedMotion) setSpeed(0.4);

// ---------------------------------------------------------------------------------------------- WebXR session
let activeSession = null;
let referenceSpace = null;
let originY = 0;
const controllers = new Map();

function tick() {
  const now = performance.now();
  simTime += ((now - lastNow) / 1000) * Number(speedControl.value);
  lastNow = now;
}

function drawXRFrame(_time, frame) {
  if (!activeSession) return;
  activeSession.requestAnimationFrame(drawXRFrame);
  tick();
  const pose = frame.getViewerPose(referenceSpace);
  if (!pose) return;
  const layer = activeSession.renderState.baseLayer;
  gl.bindFramebuffer(gl.FRAMEBUFFER, layer.framebuffer);
  gl.clearColor(0, 0, 0, 1);
  gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
  const hands = [];
  for (const source of activeSession.inputSources) {
    hands.push({
      grip: source.gripSpace && frame.getPose(source.gripSpace, referenceSpace)?.transform.matrix,
      ray: source.targetRaySpace && frame.getPose(source.targetRaySpace, referenceSpace)?.transform.matrix,
    });
  }
  for (const view of pose.views) {
    const viewport = layer.getViewport(view);
    gl.viewport(viewport.x, viewport.y, viewport.width, viewport.height);
    const p = view.transform.position;
    drawScene({ viewProj: m4.mul(view.projectionMatrix, view.transform.inverse.matrix), cam: [p.x, p.y, p.z], origin: [0, originY, -2.6], width: viewport.height, height: viewport.height, controllers: hands });
  }
}

async function enterVR() {
  enterButton.disabled = true;
  statusEl.textContent = "Starting immersive VR…";
  try {
    const sessionPromise = navigator.xr.requestSession("immersive-vr", { optionalFeatures: ["local-floor"] });
    try {
      await gl.makeXRCompatible();
      activeSession = await sessionPromise;
    } catch (error) {
      sessionPromise.then((session) => session.end()).catch(() => {});
      throw error;
    }
    activeSession.addEventListener("end", () => {
      activeSession = null;
      referenceSpace = null;
      enterButton.disabled = false;
      enterButton.textContent = "Enter VR";
      statusEl.textContent = "VR session ended. You can enter again.";
      lastNow = performance.now();
    }, { once: true });
    activeSession.addEventListener("select", () => setPalette(paletteIndex + 1));
    await activeSession.updateRenderState({ baseLayer: new XRWebGLLayer(activeSession, gl) });
    try {
      referenceSpace = await activeSession.requestReferenceSpace("local-floor");
      originY = 0;
    } catch {
      referenceSpace = await activeSession.requestReferenceSpace("local"); // no floor height known: assume a 1.5 m eye height
      originY = -1.5;
    }
    enterButton.textContent = "In VR";
    statusEl.textContent = "VR session active. Press the trigger to change the palette; use your headset controls to exit.";
    lastNow = performance.now();
    activeSession.requestAnimationFrame(drawXRFrame);
  } catch (error) {
    if (activeSession) { try { await activeSession.end(); } catch { /* Session may already be ending. */ } }
    activeSession = null;
    enterButton.disabled = false;
    enterButton.textContent = "Enter VR";
    statusEl.textContent = `Could not start VR: ${error.message}`;
  }
}
enterButton.addEventListener("click", enterVR);

if (!("xr" in navigator)) {
  enterButton.textContent = "WebXR unavailable";
  statusEl.textContent = "This browser does not expose the WebXR Device API. The desktop preview still works.";
} else {
  navigator.xr.isSessionSupported("immersive-vr").then((supported) => {
    enterButton.disabled = !supported;
    enterButton.textContent = supported ? "Enter VR" : "Immersive VR unavailable";
    statusEl.textContent = supported
      ? "Immersive VR is supported. Select Enter VR to request a session."
      : "This browser or device does not support immersive VR. The desktop preview still works.";
  }).catch((error) => {
    enterButton.textContent = "WebXR support check failed";
    statusEl.textContent = `Could not check WebXR support: ${error.message}`;
  });
}

function loop() {
  requestAnimationFrame(loop);
  if (activeSession) return; // the session's own frame callback draws while immersive
  tick();
  if (!reducedMotion && dragging.size === 0) { orbit.idle += 1; if (orbit.idle > 150) orbit.yaw += 0.0015 * Number(speedControl.value); }
  drawPreview();
}
loop();
window.addEventListener("beforeunload", () => { if (activeSession) activeSession.end(); });
window.__demo = { ready: true, setPalette, get simTime() { return simTime; } };
