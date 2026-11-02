import * as THREE from "three";
import { addPerson, ball, box, cyl, hiddenPart, part } from "./industrial-stations.js";
import { drawSignLines, fontsReady } from "../brand-kit/panels.js";

// Work-area models for the manufacturing-quality scenarios (see partner-scenarios.js). They follow
// industrial-stations.js: each group stays under ~1.85 units tall, exposes userData.reveal parts for hotspots,
// userData.setState(state) for the per-task starting situation ("" or "t2", "t3", "t4"), and userData.tick(seconds).

// A thin rod between two points.
function rod(group, from, to, color, radius = 0.012) {
  const a = new THREE.Vector3(...from);
  const b = new THREE.Vector3(...to);
  const mesh = part(group, cyl(radius, radius, a.distanceTo(b), 8), color, [0, 0, 0], { basic: true });
  mesh.position.copy(a).lerp(b, 0.5);
  mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), b.clone().sub(a).normalize());
  return mesh;
}

// A flat board with text drawn on a canvas that can be rewritten (disposed with the group by the app).
function textBoard(group, [w, h], [x, y, z], { rot = [0, 0, 0], bg = "#f2f6fc", fg = "#16212e", frame = "#3a4856" } = {}) {
  const frameMesh = part(group, box(w + 0.04, h + 0.04, 0.025), frame, [x, y, z - 0.015], { rot });
  const canvas = document.createElement("canvas");
  canvas.width = 512;
  canvas.height = Math.round(512 * h / w);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  const face = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ map: texture }));
  face.position.set(x, y, z);
  face.rotation.set(...rot);
  group.add(face);
  const write = (lines, colors = {}) => {
    const context = canvas.getContext("2d");
    context.fillStyle = colors.bg ?? bg;
    context.fillRect(0, 0, canvas.width, canvas.height);
    drawSignLines(context, lines, { fg: colors.fg ?? fg });
    texture.needsUpdate = true;
    last = [lines, colors];
  };
  let last = null;
  fontsReady.then(() => last && write(...last));
  write([""]);
  return { write, face, frame: frameMesh };
}

function group3(parent, position = [0, 0, 0]) {
  const child = new THREE.Group();
  child.position.set(...position);
  parent.add(child);
  return child;
}

// ---------------------------------------------------------------------------------------------------------------
// 1. Lighter lot sampling: pallet of cartons, test bench with ventilated hood, fire controls, quarantine cage.
export function createLightersStation() {
  const group = new THREE.Group();
  // Pallet of cartons (a few shades so the lot looks like many boxes).
  part(group, box(0.95, 0.1, 0.72), "#8b6f4b", [-0.95, 0.05, 0], { rough: 0.9 });
  const shades = ["#b08a5a", "#c49a66", "#a47c4e"];
  for (let layer = 0; layer < 3; layer += 1) {
    for (const x of [-1.15, -0.75]) {
      for (const z of [-0.17, 0.17]) part(group, box(0.38, 0.28, 0.3), shades[(layer + (x > -1 ? 1 : 0) + (z > 0 ? 2 : 0)) % 3], [x, 0.24 + layer * 0.3, z], { rough: 0.95 });
    }
  }
  // An open carton at the front: the "easy" one.
  part(group, box(0.4, 0.3, 0.3), "#a47c4e", [-1.05, 0.25, 0.52], { rough: 0.95 });
  for (let index = 0; index < 5; index += 1) part(group, box(0.05, 0.1, 0.03), ["#e04848", "#3fa0d8", "#f2c94c", "#3ee08f", "#d8edf3"][index], [-1.2 + index * 0.075, 0.45, 0.52]);
  // Clipboard on a stand.
  part(group, cyl(0.02, 0.02, 1.15, 8), "#657b8c", [-0.55, 0.575, 0.45], { metal: 0.5 });
  part(group, box(0.32, 0.42, 0.02), "#f2f6fc", [-0.55, 1.25, 0.45], { rot: [-0.2, 0, 0], rough: 0.9 });
  for (let line = 0; line < 4; line += 1) part(group, box(0.22, 0.012, 0.005), "#8a99a8", [-0.55, 1.36 - line * 0.07, 0.465], { rot: [-0.2, 0, 0], basic: true });
  // Bench.
  part(group, box(1.7, 0.06, 0.8), "#586979", [0.4, 0.8, 0.1], { metal: 0.5 });
  for (const x of [-0.4, 1.2]) for (const z of [-0.25, 0.45]) part(group, box(0.06, 0.78, 0.06), "#3a4856", [x, 0.39, z]);
  // Hot plate (an ignition source).
  part(group, box(0.3, 0.06, 0.25), "#2b3440", [-0.1, 0.86, 0.3]);
  const coil = part(group, new THREE.TorusGeometry(0.08, 0.015, 8, 20), "#ff7a3a", [-0.1, 0.9, 0.3], { rot: [Math.PI / 2, 0, 0], basic: true });
  // Sample bag and lot-ID tag.
  const sample = group3(group, [0.2, 0.84, 0.4]);
  part(sample, box(0.13, 0.02, 0.17), "#d8edf3", [0, 0, 0], { opacity: 0.6, rough: 0.3 });
  part(sample, box(0.04, 0.025, 0.1), "#e04848", [0, 0.015, 0]);
  group.userData.reveal = { sample };
  sample.visible = false;
  const tag = group3(group, [0.3, 0.86, 0.4]);
  part(tag, box(0.07, 0.01, 0.1), "#ffffff", [0, 0, 0], { basic: true });
  part(tag, box(0.05, 0.012, 0.015), "#1b2530", [0, 0.003, -0.02], { basic: true });
  tag.visible = false;
  group.userData.reveal.tag = tag;
  // Ventilated hood.
  part(group, box(0.82, 0.8, 0.03), "#9aa8b4", [0.55, 1.23, -0.33], { metal: 0.5 });
  for (const x of [0.14, 0.96]) part(group, box(0.03, 0.8, 0.5), "#9aa8b4", [x, 1.23, -0.1], { metal: 0.5 });
  part(group, box(0.86, 0.04, 0.54), "#9aa8b4", [0.55, 1.65, -0.1], { metal: 0.5 });
  part(group, box(0.8, 0.4, 0.02), "#8fd0ee", [0.55, 1.45, 0.16], { opacity: 0.25, rough: 0.1 });
  part(group, box(0.8, 0.03, 0.03), "#d8edf3", [0.55, 1.24, 0.16], { metal: 0.5 });
  part(group, cyl(0.07, 0.07, 0.2, 14), "#7a8794", [0.55, 1.77, -0.2], { metal: 0.6 });
  const fan = part(group, box(0.18, 0.015, 0.04), "#c5d4de", [0.55, 1.88, -0.2], { metal: 0.6 });
  part(group, box(0.5, 0.012, 0.3), "#d8edf3", [0.55, 0.84, -0.05], { metal: 0.5 }); // hood tray
  // Airflow indicator on a post.
  part(group, cyl(0.02, 0.02, 0.85, 8), "#657b8c", [1.05, 1.22, -0.2], { metal: 0.5 });
  part(group, box(0.2, 0.14, 0.04), "#1a2533", [1.05, 1.58, -0.2]);
  const airflow = part(group, ball(0.03), "#3ee08f", [1.05, 1.58, -0.17], { basic: true });
  // Lighter in the hood, flame, and a tray of spares.
  const hoodSample = group3(group, [0.55, 0.88, 0.0]);
  part(hoodSample, box(0.05, 0.1, 0.03), "#e04848", [0, 0.05, 0]);
  hoodSample.visible = false;
  group.userData.reveal.hoodSample = hoodSample;
  const flame = group3(group, [0.55, 0.99, 0.0]);
  part(flame, new THREE.ConeGeometry(0.035, 0.12, 10), "#ffb347", [0, 0.06, 0], { basic: true });
  part(flame, new THREE.ConeGeometry(0.018, 0.07, 10), "#fff2c8", [0, 0.04, 0], { basic: true });
  flame.visible = false;
  group.userData.reveal.flame = flame;
  const spares = group3(group, [0.05, 0.84, 0.15]);
  for (let index = 0; index < 5; index += 1) part(spares, box(0.04, 0.09, 0.025), ["#e04848", "#3fa0d8", "#f2c94c", "#3ee08f", "#d8edf3"][index], [-0.12 + index * 0.06, 0.05, 0]);
  spares.visible = false;
  // Fire controls.
  part(group, cyl(0.07, 0.07, 0.5, 14), "#d94646", [1.12, 0.25, 0.4], { rough: 0.4 });
  part(group, cyl(0.02, 0.02, 0.1, 8), "#1b2530", [1.12, 0.55, 0.4]);
  part(group, cyl(0.02, 0.02, 1.3, 8), "#657b8c", [1.12, 0.65, 0.0], { metal: 0.5 });
  part(group, box(0.24, 0.3, 0.1), "#d94646", [1.12, 1.1, 0.0]);
  part(group, box(0.16, 0.06, 0.02), "#fff2c8", [1.12, 1.12, 0.055], { basic: true });
  // Quarantine cage (shown after the lot is held).
  const cage = group3(group, [1.0, 0.35, -0.45]);
  part(cage, box(0.55, 0.62, 0.4), "#ffd23f", [0, 0, 0], { opacity: 0.28, basic: true });
  part(cage, box(0.24, 0.2, 0.22), "#b08a5a", [-0.1, -0.2, 0], { rough: 0.95 });
  part(cage, box(0.24, 0.2, 0.22), "#c49a66", [0.12, -0.2, 0.02], { rough: 0.95 });
  part(cage, box(0.18, 0.12, 0.02), "#e04848", [0, 0.05, 0.21], { basic: true });
  cage.visible = false;
  group.userData.reveal.quarantine = cage;
  // Wall phone.
  part(group, cyl(0.02, 0.02, 1.3, 8), "#657b8c", [-0.1, 0.65, -0.45], { metal: 0.5 });
  part(group, box(0.14, 0.2, 0.06), "#1a2533", [-0.1, 1.35, -0.45]);
  part(group, box(0.08, 0.1, 0.01), "#3fa0d8", [-0.1, 1.37, -0.415], { basic: true });
  // Fail lamp.
  const fail = part(group, ball(0.06), "#ff3b3b", [0.55, 1.73, -0.1], { basic: true });
  fail.visible = false;
  let state = "";
  group.userData.setState = (next) => {
    state = next;
    hoodSample.visible = next === "t3" || next === "t4";
    spares.visible = next === "t3";
    flame.visible = false;
    fail.visible = next === "t4";
  };
  group.userData.tick = (time) => {
    fan.rotation.y = time * 9;
    airflow.material.color.set(Math.sin(time * 3) > -0.6 ? "#3ee08f" : "#1f6b49");
    coil.material.color.set(Math.sin(time * 2) > -0.2 ? "#ff7a3a" : "#d9552a");
    flame.scale.set(1, 0.85 + Math.sin(time * 11) * 0.18, 1);
    fail.visible = state === "t4" && Math.sin(time * 6) > -0.2;
  };
  return group;
}

// ---------------------------------------------------------------------------------------------------------------
// 2. Hydrostatic test of a heat exchanger: vessel on saddles, nozzles with blind flanges, vent, gauge, pump, barrier.
export function createHydrotestStation() {
  const group = new THREE.Group();
  const shellY = 0.67;
  for (const x of [-0.5, 0.5]) part(group, box(0.18, 0.4, 0.5), "#3a4856", [x, 0.2, 0]);
  const shell = part(group, cyl(0.3, 0.3, 1.5, 28), "#9aa8b4", [0, shellY, 0], { rot: [0, 0, Math.PI / 2], metal: 0.5, rough: 0.4, opacity: 0.5 });
  shell.material.depthWrite = false;
  for (const x of [-0.75, 0.75]) part(group, new THREE.SphereGeometry(0.3, 20, 10, 0, Math.PI * 2, 0, Math.PI / 2), "#9aa8b4", [x, shellY, 0], { rot: [0, 0, x < 0 ? Math.PI / 2 : -Math.PI / 2], metal: 0.5, rough: 0.4, opacity: 0.5 });
  for (const x of [-0.5, 0.5]) {
    part(group, cyl(0.07, 0.07, 0.2, 14), "#9aa8b4", [x, 1.02, 0], { metal: 0.5 });
    part(group, cyl(0.11, 0.11, 0.03, 18), "#c5d4de", [x, 1.13, 0], { metal: 0.6 });
  }
  for (const ring of [-0.2, 0.2]) part(group, new THREE.TorusGeometry(0.305, 0.012, 8, 28), "#c5d4de", [ring, shellY, 0], { rot: [0, Math.PI / 2, 0], metal: 0.5 });
  const water = part(group, cyl(0.27, 0.27, 1.46, 24), "#3f9ad8", [0, shellY - 0.01, 0], { rot: [0, 0, Math.PI / 2], opacity: 0.7, basic: true });
  water.visible = false;
  (group.userData.reveal ??= {}).water = water;
  // Blind flanges (fitted by the learner).
  for (const [name, x] of [["blindA", -0.5], ["blindB", 0.5]]) {
    const blind = group3(group, [x, 1.17, 0]);
    part(blind, cyl(0.12, 0.12, 0.03, 18), "#ff9d59", [0, 0, 0], { metal: 0.5 });
    for (let bolt = 0; bolt < 6; bolt += 1) part(blind, cyl(0.012, 0.012, 0.04, 6), "#3a4856", [Math.cos(bolt * 1.047) * 0.09, 0.02, Math.sin(bolt * 1.047) * 0.09]);
    blind.visible = false;
    group.userData.reveal[name] = blind;
  }
  // High-point vent: valve, pipe and clear tube end.
  part(group, cyl(0.03, 0.03, 0.4, 10), "#c5d4de", [0.2, 1.12, 0], { metal: 0.6 });
  part(group, box(0.1, 0.1, 0.1), "#7a8794", [0.2, 1.08, 0], { metal: 0.6 });
  const ventClosed = group3(group, [0.2, 1.08, 0]);
  part(ventClosed, box(0.2, 0.025, 0.025), "#ff5f5f", [0, 0.09, 0]);
  const ventOpen = group3(group, [0.2, 1.08, 0]);
  part(ventOpen, box(0.025, 0.2, 0.025), "#3ee08f", [0, 0.1, 0]);
  ventOpen.visible = false;
  group.userData.reveal.ventClosed = ventClosed;
  group.userData.reveal.ventOpen = ventOpen;
  rod(group, [0.2, 1.3, 0], [0.55, 1.3, 0], "#c5d4de", 0.025);
  part(group, cyl(0.045, 0.045, 0.16, 10), "#8fd0ee", [0.55, 1.38, 0], { opacity: 0.5, rough: 0.1 });
  // Pressure gauge.
  part(group, cyl(0.02, 0.02, 0.3, 8), "#c5d4de", [-0.25, 1.02, 0.2], { metal: 0.6 });
  part(group, cyl(0.1, 0.1, 0.05, 24), "#f2f6fc", [-0.25, 1.25, 0.25], { rot: [Math.PI / 2, 0, 0], rough: 0.3 });
  part(group, cyl(0.1, 0.1, 0.02, 24), "#1a2533", [-0.25, 1.25, 0.225], { rot: [Math.PI / 2, 0, 0] });
  const needle = new THREE.Group();
  needle.position.set(-0.25, 1.25, 0.285);
  part(needle, box(0.012, 0.085, 0.005), "#d94646", [0, 0.04, 0], { basic: true });
  group.add(needle);
  // Pump with control cabinet and hose.
  part(group, box(0.4, 0.24, 0.3), "#2d6f8f", [-1.1, 0.12, 0.3]);
  part(group, box(0.3, 0.9, 0.2), "#7f8d9a", [-1.1, 0.7, 0.3], { metal: 0.4 });
  part(group, cyl(0.045, 0.045, 0.03, 16), "#3ee08f", [-1.1, 0.6, 0.41], { rot: [Math.PI / 2, 0, 0], basic: true });
  part(group, cyl(0.07, 0.07, 0.03, 16), "#d8edf3", [-1.1, 1.05, 0.41], { rot: [Math.PI / 2, 0, 0] });
  part(group, box(0.012, 0.07, 0.01), "#d94646", [-1.1, 1.07, 0.43], { basic: true });
  rod(group, [-1.0, 0.2, 0.3], [-0.78, 0.5, 0.05], "#2d6f8f", 0.02);
  // Unlabelled hose on a hook.
  part(group, cyl(0.02, 0.02, 1.0, 8), "#657b8c", [-1.35, 0.5, 0.45], { metal: 0.5 });
  part(group, new THREE.TorusGeometry(0.1, 0.025, 8, 20), "#6f8a4a", [-1.35, 0.9, 0.45], { rot: [0, Math.PI / 2, 0] });
  // Drain valve at the low point.
  part(group, cyl(0.025, 0.025, 0.12, 8), "#c5d4de", [0.6, 0.31, 0], { metal: 0.6 });
  rod(group, [0.6, 0.26, 0], [0.6, 0.26, 0.45], "#c5d4de", 0.03);
  part(group, new THREE.TorusGeometry(0.06, 0.012, 8, 16), "#ff5f5f", [0.6, 0.3, 0.45], { rot: [Math.PI / 2, 0, 0] });
  // Test pack board.
  part(group, cyl(0.02, 0.02, 1.4, 8), "#657b8c", [-0.95, 0.8, -0.4], { metal: 0.5 });
  const board = textBoard(group, [0.55, 0.32], [-0.95, 1.6, -0.4], {});
  board.write(["TEST PACK", "Test pressure: see pack", "Gauge cal: in date"], {});
  // Barrier and operator (shown when the learner retreats).
  const barrier = group3(group);
  for (const y of [0.45, 0.85]) part(barrier, box(0.8, 0.04, 0.03), "#ffd23f", [0.85, y, 0.6], { basic: true });
  for (const x of [0.45, 1.25]) part(barrier, box(0.04, 0.9, 0.04), "#3a4856", [x, 0.45, 0.6]);
  barrier.visible = false;
  group.userData.reveal.barrier = barrier;
  addPerson(barrier, [1.0, 0, 0.85]); // the operator stands behind the barrier
  let target = 0;
  let shown = 0;
  group.userData.setState = (state) => {
    for (const name of ["blindA", "blindB"]) group.userData.reveal[name].visible = state !== "";
    water.visible = state === "t3" || state === "t4";
    ventOpen.visible = state === "t3";
    ventClosed.visible = state !== "t3";
    barrier.visible = state === "t4";
    target = state === "t3" ? 0.1 : state === "t4" ? 0.65 : 0;
    shown = target;
  };
  group.userData.tick = (time) => {
    needle.rotation.z = -(shown * 4.2 - 2.1) + Math.sin(time * 5) * 0.012;
  };
  return group;
}

// ---------------------------------------------------------------------------------------------------------------
// 3. Pipe spool welding: two pipes on stands with a joint, paperwork table, hot-work gear and a live process line.
export function createSpoolStation() {
  const group = new THREE.Group();
  const pipeY = 0.72;
  for (const x of [-0.3, 0.32]) {
    part(group, box(0.18, 0.58, 0.3), "#3a4856", [x, 0.29, 0]);
    part(group, box(0.22, 0.03, 0.34), "#7a8794", [x, 0.585, 0], { metal: 0.5 });
  }
  part(group, cyl(0.13, 0.13, 0.55, 22), "#8fa0ae", [-0.295, pipeY, 0], { rot: [0, 0, Math.PI / 2], metal: 0.6, rough: 0.35 });
  part(group, cyl(0.13, 0.13, 0.55, 22), "#8fa0ae", [0.295, pipeY, 0], { rot: [0, 0, Math.PI / 2], metal: 0.6, rough: 0.35 });
  part(group, cyl(0.1, 0.1, 0.02, 18), "#1a2533", [0, pipeY, 0], { rot: [0, 0, Math.PI / 2] }); // visible root gap
  part(group, box(0.14, 0.06, 0.01), "#ffd23f", [-0.25, 0.78, 0.135], { basic: true }); // heat-number stencil
  part(group, box(0.1, 0.012, 0.012), "#1b2530", [-0.25, 0.78, 0.142], { basic: true });
  // Tacks and weld bead.
  const tacks = group3(group);
  for (const angle of [0.2, 2.3, 4.4]) part(tacks, new THREE.SphereGeometry(0.03, 8, 6), "#4a525c", [0, pipeY + Math.cos(angle) * 0.135, Math.sin(angle) * 0.135], { metal: 0.5 });
  tacks.visible = false;
  (group.userData.reveal ??= {}).tacks = tacks;
  const bead = part(group, new THREE.TorusGeometry(0.135, 0.022, 8, 28), "#6b7480", [0, pipeY, 0], { rot: [0, Math.PI / 2, 0], metal: 0.7, rough: 0.4 });
  bead.visible = false;
  const mark = part(group, box(0.12, 0.05, 0.01), "#ffd23f", [0.36, 0.78, 0.135], { basic: true });
  mark.visible = false;
  group.userData.reveal.mark = mark;
  // Torch (preheat) and grinder (shown only when relevant).
  const torch = group3(group, [0.02, 1.1, -0.05]);
  part(torch, cyl(0.025, 0.02, 0.3, 10), "#d94646", [0, 0.15, 0], { rot: [0, 0, 0.5] });
  const preheatFlame = part(torch, new THREE.ConeGeometry(0.03, 0.12, 8), "#ff9d59", [-0.09, -0.02, 0], { basic: true, rot: [0, 0, 0.5 + Math.PI] });
  const grinder = group3(group, [0.02, 1.1, -0.05]);
  part(grinder, cyl(0.04, 0.04, 0.22, 10), "#2d6f8f", [0, 0.1, 0], { rot: [0, 0, 1.2] });
  part(grinder, cyl(0.08, 0.08, 0.01, 18), "#8a8a8a", [-0.12, 0.06, 0], { rot: [0, 0, 0] });
  grinder.visible = false;
  // Sledge hammer leaning on the stand.
  part(group, cyl(0.015, 0.015, 0.5, 8), "#8b6f4b", [0.55, 0.25, 0.3], { rot: [0, 0, 0.25] });
  part(group, box(0.14, 0.08, 0.08), "#3a4856", [0.6, 0.5, 0.3], { metal: 0.6 });
  // Welding machine and electrode holder.
  part(group, box(0.4, 0.45, 0.3), "#2d6f8f", [0.8, 0.225, -0.35]);
  part(group, box(0.2, 0.06, 0.02), "#fff2c8", [0.8, 0.38, -0.19], { basic: true });
  part(group, cyl(0.02, 0.02, 0.28, 8), "#2b3440", [0.5, 0.95, 0.1], { rot: [0, 0, 1.0] });
  rod(group, [0.7, 0.4, -0.2], [0.55, 0.9, 0.08], "#1b2530", 0.012);
  const arc = part(group, ball(0.03), "#ffd9a0", [0.42, 0.88, 0.1], { basic: true });
  arc.visible = false;
  // Paperwork table (left): binders, qualification card, report, NDE request, weld record.
  part(group, box(0.95, 0.06, 0.55), "#6f5438", [-1.0, 0.8, 0.35], { rough: 0.8 });
  for (const x of [-1.38, -0.62]) for (const z of [0.15, 0.55]) part(group, box(0.05, 0.78, 0.05), "#3a4856", [x, 0.39, z]);
  part(group, box(0.22, 0.04, 0.3), "#3a6a9a", [-1.25, 0.86, 0.35], { rough: 0.9 });
  part(group, box(0.22, 0.01, 0.15), "#f2f6fc", [-0.95, 0.84, 0.35], { rough: 0.9 });
  part(group, box(0.22, 0.012, 0.3), "#ffd23f", [-0.65, 0.84, 0.35], { rough: 0.9, basic: true });
  // Unmarked offcut on the floor.
  part(group, cyl(0.07, 0.07, 0.4, 14), "#8a8a8a", [-1.2, 0.07, 0.65], { rot: [0, 0, Math.PI / 2], metal: 0.5 });
  // Live process line and valve (behind), tank.
  part(group, cyl(0.22, 0.22, 1.5, 20), "#9aa8b4", [-1.3, 0.75, -0.5], { metal: 0.5, rough: 0.4 });
  part(group, cyl(0.045, 0.045, 1.0, 12), "#c5d4de", [-0.8, 0.9, -0.5], { rot: [0, 0, Math.PI / 2], metal: 0.5 });
  part(group, box(0.12, 0.14, 0.12), "#7a8794", [-0.95, 0.9, -0.5], { metal: 0.5 });
  part(group, new THREE.TorusGeometry(0.06, 0.012, 8, 16), "#ff5f5f", [-0.95, 1.03, -0.5]);
  const isoTag = part(group, box(0.1, 0.14, 0.01), "#ffd23f", [-0.95, 0.78, -0.42], { basic: true });
  isoTag.visible = false;
  group.userData.reveal.isoTag = isoTag;
  // Permit board.
  part(group, cyl(0.02, 0.02, 1.45, 8), "#657b8c", [1.15, 0.725, -0.1], { metal: 0.5 });
  const permit = textBoard(group, [0.42, 0.3], [1.15, 1.5, -0.1], { bg: "#fff2c8" });
  permit.write(["HOT WORK", "PERMIT", "Valid today"], { fg: "#7a2b00" });
  // Gas tester on a post.
  part(group, cyl(0.02, 0.02, 0.8, 8), "#657b8c", [-0.2, 0.4, 0.6], { metal: 0.5 });
  part(group, box(0.14, 0.2, 0.08), "#f2c94c", [-0.2, 0.86, 0.6]);
  const gasLight = part(group, ball(0.02), "#3ee08f", [-0.2, 0.9, 0.65], { basic: true });
  // Extinguisher and fire watch.
  part(group, cyl(0.06, 0.06, 0.4, 14), "#d94646", [1.1, 0.2, 0.25], { rough: 0.4 });
  const watch = group3(group);
  addPerson(watch, [0.9, 0.0, 0.0], "#ff7a59");
  watch.visible = false;
  group.userData.reveal.watch = watch;
  group.userData.setState = (state) => {
    tacks.visible = state === "t3" || state === "t4";
    bead.visible = state === "t4";
    torch.visible = state === "t2";
    grinder.visible = state === "t4";
    mark.visible = false;
    isoTag.visible = false;
    watch.visible = false;
    arc.visible = false;
  };
  group.userData.tick = (time) => {
    preheatFlame.scale.set(1, 0.85 + Math.sin(time * 14) * 0.2, 1);
    gasLight.material.color.set(Math.sin(time * 4) > -0.3 ? "#3ee08f" : "#1f6b49");
  };
  return group;
}

// ---------------------------------------------------------------------------------------------------------------
// 4. Resistor selection: ESD bench with strap, magnified resistor, meter, component bins, LED circuit, and boards.
export function createResistorBench() {
  const group = new THREE.Group();
  part(group, box(2.7, 0.07, 0.95), "#586979", [0, 0.8, 0.1], { metal: 0.4 });
  for (const x of [-1.25, 1.25]) for (const z of [-0.25, 0.45]) part(group, box(0.07, 0.78, 0.07), "#3a4856", [x, 0.39, z]);
  part(group, box(1.7, 0.012, 0.65), "#2f6f4f", [-0.35, 0.84, 0.18], { rough: 0.95 }); // ESD mat
  // Wrist strap and ground point.
  const strap = part(group, new THREE.TorusGeometry(0.08, 0.025, 8, 20), "#3fa0d8", [-1.0, 0.93, 0.25], { rot: [0, 0.3, 0] });
  part(group, box(0.1, 0.05, 0.04), "#d8edf3", [-1.0, 1.0, 0.25]);
  rod(group, [-1.0, 0.9, 0.25], [-1.25, 0.88, -0.2], "#1b2530", 0.008);
  part(group, box(0.1, 0.05, 0.08), "#f2c94c", [-1.25, 0.88, -0.2]);
  // ESD bag and the loose parts it contains.
  part(group, box(0.25, 0.02, 0.35), "#b9c3cc", [-0.6, 0.86, 0.3], { opacity: 0.7, metal: 0.8, rough: 0.2 });
  const parts = group3(group);
  for (let index = 0; index < 4; index += 1) {
    part(parts, cyl(0.012, 0.012, 0.1, 8), "#d9b78a", [-0.35 + index * 0.1, 0.86, 0.35], { rot: [0, 0, Math.PI / 2] });
  }
  parts.visible = false;
  (group.userData.reveal ??= {}).parts = parts;
  // Multimeter and leads.
  part(group, box(0.22, 0.32, 0.08), "#f2c94c", [0.15, 1.0, 0.35], { rot: [-0.25, 0, 0] });
  part(group, box(0.15, 0.08, 0.01), "#9fe8a4", [0.15, 1.08, 0.395], { rot: [-0.25, 0, 0], basic: true });
  part(group, cyl(0.04, 0.04, 0.02, 14), "#2b3440", [0.15, 0.95, 0.4], { rot: [Math.PI / 2 - 0.25, 0, 0] });
  const leads = group3(group);
  rod(leads, [0.1, 0.86, 0.38], [-0.07, 0.87, 0.02], "#e04848", 0.006);
  rod(leads, [0.2, 0.86, 0.38], [-0.13, 0.87, 0.02], "#1b2530", 0.006);
  leads.visible = false;
  // The loose resistor on the mat (small, ahead of the meter leads).
  const loose = group3(group);
  part(loose, cyl(0.02, 0.02, 0.07, 10), "#d9b78a", [-0.1, 0.87, 0.0], { rot: [0, 0, Math.PI / 2] });
  part(loose, cyl(0.004, 0.004, 0.16, 6), "#c5d4de", [-0.1, 0.87, 0.0], { rot: [0, 0, Math.PI / 2], metal: 0.6 });
  loose.visible = false;
  // Magnified resistor on a stand.
  const big = group3(group);
  part(big, cyl(0.02, 0.02, 0.4, 8), "#657b8c", [-0.55, 1.05, 0], { metal: 0.5 });
  part(big, cyl(0.02, 0.02, 0.4, 8), "#657b8c", [-0.05, 1.05, 0], { metal: 0.5 });
  part(big, cyl(0.011, 0.011, 1.0, 8), "#c5d4de", [-0.3, 1.3, 0], { rot: [0, 0, Math.PI / 2], metal: 0.6 });
  part(big, cyl(0.09, 0.09, 0.5, 20), "#d9b78a", [-0.3, 1.3, 0], { rot: [0, 0, Math.PI / 2], rough: 0.7 });
  for (const [x, color] of [[-0.48, "#7a4a22"], [-0.4, "#2fa84f"], [-0.32, "#7a4a22"], [-0.14, "#d9a520"]]) part(big, cyl(0.0925, 0.0925, 0.04, 20), color, [x, 1.3, 0], { rot: [0, 0, Math.PI / 2] });
  big.visible = false;
  // Breadboard circuit with LED and supply.
  part(group, box(0.5, 0.03, 0.3), "#f2f6fc", [0.8, 0.86, 0.15], { rough: 0.9 });
  const led = part(group, new THREE.SphereGeometry(0.035, 12, 8), "#e04848", [0.95, 0.92, 0.15], { rough: 0.3 });
  part(group, cyl(0.035, 0.035, 0.04, 12), "#e04848", [0.95, 0.9, 0.15]);
  part(group, box(0.22, 0.14, 0.14), "#2b3440", [1.12, 0.94, -0.05]);
  part(group, box(0.1, 0.05, 0.01), "#e04848", [1.12, 0.96, 0.025], { basic: true });
  const installed = group3(group);
  part(installed, cyl(0.013, 0.013, 0.07, 10), "#d9b78a", [0.62, 0.91, 0.15], { rot: [0, 0, Math.PI / 2] });
  part(installed, cyl(0.004, 0.004, 0.14, 6), "#c5d4de", [0.62, 0.9, 0.15], { rot: [0, 0, Math.PI / 2] });
  installed.visible = false;
  group.userData.reveal.installed = installed;
  // Component bins.
  const bins = group3(group);
  const binLabels = [];
  for (const x of [-1.05, -0.45, 0.05]) {
    part(bins, box(0.38, 0.08, 0.2), "#3a6a9a", [x, 0.88, 0.55]);
    part(bins, box(0.34, 0.06, 0.16), "#1a2533", [x, 0.9, 0.55]);
    binLabels.push(textBoard(bins, [0.34, 0.12], [x, 1.0, 0.64], { rot: [-0.45, 0, 0], frame: "#1b2530" }));
  }
  bins.visible = false;
  // Boards on the back wall.
  const boardA = textBoard(group, [0.5, 0.36], [0.1, 1.62, -0.4], {});
  const boardB = textBoard(group, [0.56, 0.4], [0.9, 1.55, -0.3], {});
  // Sulfur-rich materials (rubber gaskets) shown in the last task.
  const sulfur = group3(group);
  for (let index = 0; index < 3; index += 1) part(sulfur, new THREE.TorusGeometry(0.1, 0.03, 8, 20), "#14181e", [1.1, 0.9 + index * 0.05, 0.4], { rot: [Math.PI / 2, 0, 0], rough: 0.9 });
  sulfur.visible = false;
  let state = "";
  group.userData.setState = (next) => {
    state = next;
    big.visible = next === "t2";
    loose.visible = next === "t2";
    leads.visible = next === "t2";
    bins.visible = next === "t3" || next === "t4";
    sulfur.visible = next === "t4";
    parts.visible = next !== "";
    const text = {
      "": [["ESD BENCH", "Strap on first"], ["GROUND CHECK", "Strap to ground", "before any parts"]],
      t2: [["COLOR CODE", "Digits, multiplier,", "then tolerance"], ["METER", "Ohms range", "Out of circuit"]],
      t3: [["CATALOG", "1/4 W = 0.25 W", "1/16 W = 0.0625 W"], ["5 V  20 mA  2 V LED", "R = (5 - 2) / 0.02", "P = I x I x R"]],
      t4: [["DERATING CURVE", "Power drops as", "ambient heat rises"], ["WARNING", "SULFUR-RICH AREA", "Check part type"]],
    }[next] ?? [[""], [""]];
    boardA.write(text[0]);
    boardB.write(text[1], next === "t4" ? { bg: "#ffd23f", fg: "#3a2a00" } : {});
    const labels = next === "t4"
      ? [["150 ohm chip", "standard"], ["150 ohm 1/4 W", "ANTI-SULFUR"], ["150 ohm 1/4 W", "spare"]]
      : [["47 ohm", "1/4 W"], ["150 ohm 5%", "1/4 W"], ["150 ohm 5%", "1/16 W"]];
    labels.forEach((lines, index) => binLabels[index].write(lines));
  };
  group.userData.tick = (time) => {
    const lit = state === "t2" || installed.visible;
    led.material.color.set(lit ? (Math.sin(time * 3) > -0.8 ? "#ff5a5a" : "#ff8f8f") : "#6a2a2a");
    led.material.emissive.set(lit ? "#ff2222" : "#000000");
    strap.rotation.y = 0.3 + Math.sin(time * 1.5) * 0.04;
  };
  return group;
}
