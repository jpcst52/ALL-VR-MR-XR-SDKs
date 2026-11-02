import * as THREE from "three";
import { PITT, tagSprite, drawSignLines, fontsReady } from "../../brand-kit/panels.js";
import { evaluateHydro, summaryText, PORTS, NOZZLES } from "./hydro-eval.mjs";

// A theme describes one shared-building example: its parts, how to draw them, and how to
// summarize/animate the shared scene. The relay only knows part names and toggle state.

const metal = (color, o = {}) => new THREE.MeshStandardMaterial({ color, metalness: 0.55, roughness: 0.32, ...o });
const matte = (color, o = {}) => new THREE.MeshStandardMaterial({ color, roughness: 0.7, metalness: 0.08, ...o });
const mesh = (geometry, material, [x, y, z] = [0, 0, 0], rot) => {
  const item = new THREE.Mesh(geometry, material);
  item.position.set(x, y, z);
  if (rot) item.rotation.set(...rot);
  return item;
};
const cylinder = (rt, rb, h, seg = 20) => new THREE.CylinderGeometry(rt, rb, h, seg);
const along = Math.PI / 2; // lay a cylinder along the local x axis

// Part labels go through the shared brand kit. A transparent background means text printed on a physical sign
// (EXIT, MUSTER); otherwise the label is a backed pill (WATER, PUMP, polarity marks).
function textSprite(text, color = "#f2f6fc", background = "#00205bdd", width = 0.7) {
  if (/00$/.test(background)) {
    const canvas = document.createElement("canvas");
    canvas.width = 256;
    canvas.height = 96;
    const draw = () => {
      canvas.getContext("2d").clearRect(0, 0, 256, 96);
      drawSignLines(canvas.getContext("2d"), [text], { fg: color, maxSize: 60 });
    };
    draw();
    const map = new THREE.CanvasTexture(canvas);
    fontsReady.then(() => { draw(); map.needsUpdate = true; });
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map, transparent: true }));
    sprite.scale.set(width, width * 0.375, 1);
    return sprite;
  }
  const accent = /^#(f2f6fc|ffffff|dbeeff)$/i.test(color) ? PITT.gold : color;
  return tagSprite(text, { accent, status: null, worldHeight: Math.max(0.12, width * 0.44), depthTest: true });
}

// ---------------------------------------------------------------- circuits

const TERMINAL = { battery: 0.4, switch: 0.4, bulb: 0.4, resistor: 0.4, motor: 0.4, wire: 0.6 };
const CONTACT_DISTANCE = 0.32;

function terminalPoints(item) {
  const reach = TERMINAL[item.part];
  const [x, , z] = item.position;
  const angle = item.rotation[1] ?? 0;
  const dx = Math.cos(angle) * reach;
  const dz = -Math.sin(angle) * reach;
  return [[x - dx, z - dz], [x + dx, z + dz]];
}

class UnionFind {
  constructor(size) { this.parent = Array.from({ length: size }, (_, index) => index); }
  find(item) {
    while (this.parent[item] !== item) {
      this.parent[item] = this.parent[this.parent[item]];
      item = this.parent[item];
    }
    return item;
  }
  union(a, b) { this.parent[this.find(a)] = this.find(b); }
}

// A deliberately simplified model: terminals that touch are joined, and a part carries current when it
// lies on a loop with the battery (a "2-core" after pruning dead ends). Series/parallel resistance is
// approximated by the number of resistors in the loop.
export function evaluateCircuit(items) {
  const touching = new Array(items.length * 2).fill(false);
  const points = items.flatMap(terminalPoints);
  const nodes = new UnionFind(points.length);
  for (let a = 0; a < points.length; a += 1) {
    for (let b = a + 1; b < points.length; b += 1) {
      if ((a >> 1) === (b >> 1)) continue;
      if (Math.hypot(points[a][0] - points[b][0], points[a][1] - points[b][1]) <= CONTACT_DISTANCE) {
        nodes.union(a, b);
        touching[a] = true;
        touching[b] = true;
      }
    }
  }
  const result = {
    state: items.some((item) => item.part === "battery") ? "open" : "no-battery",
    live: new Set(),
    brightness: 1,
    touching,
  };
  const battery = items.findIndex((item) => item.part === "battery");
  if (battery < 0) return result;
  if (nodes.find(battery * 2) === nodes.find(battery * 2 + 1)) {
    result.state = "short";
    return result;
  }
  const edges = [];
  items.forEach((item, index) => {
    if (item.part === "switch" && !item.on) return;
    edges.push({ index, a: nodes.find(index * 2), b: nodes.find(index * 2 + 1) });
  });
  let active = edges.filter((edge) => edge.a !== edge.b);
  for (let changed = true; changed;) {
    const degree = new Map();
    for (const edge of active) {
      degree.set(edge.a, (degree.get(edge.a) ?? 0) + 1);
      degree.set(edge.b, (degree.get(edge.b) ?? 0) + 1);
    }
    const kept = active.filter((edge) => degree.get(edge.a) > 1 && degree.get(edge.b) > 1);
    changed = kept.length !== active.length;
    active = kept;
  }
  if (!active.some((edge) => edge.index === battery)) return result;
  result.state = "closed";
  for (const edge of active) result.live.add(edge.index);
  const resistors = [...result.live].filter((index) => items[index].part === "resistor").length;
  result.brightness = 1 / (1 + resistors * 0.9);
  return result;
}

function terminalStub(x, hot) {
  return mesh(cylinder(0.045, 0.045, 0.1, 12), metal(hot ? "#ffd479" : "#c9a14a"), [x, 0.2, 0], [0, 0, along]);
}

const circuitParts = {
  battery(group) {
    const body = mesh(cylinder(0.17, 0.17, 0.62), matte("#2b3a4d"), [0, 0.2, 0], [0, 0, along]);
    group.add(body);
    group.add(mesh(cylinder(0.172, 0.172, 0.22), matte("#ffb36b"), [0.17, 0.2, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.06, 0.06, 0.08), metal("#d8edf3"), [0.34, 0.2, 0], [0, 0, along]));
    const plus = textSprite("+", "#ffb36b", "#101723cc", 0.24);
    plus.position.set(0.17, 0.5, 0);
    const minus = textSprite("−", "#9bb0c8", "#101723cc", 0.24);
    minus.position.set(-0.2, 0.5, 0);
    group.add(plus, minus);
  },
  switch(group) {
    group.add(mesh(new THREE.BoxGeometry(0.62, 0.08, 0.3), matte("#3a4d63"), [0, 0.04, 0]));
    group.add(mesh(cylinder(0.05, 0.05, 0.12, 12), metal("#d8edf3"), [-0.24, 0.12, 0]));
    group.add(mesh(cylinder(0.05, 0.05, 0.12, 12), metal("#d8edf3"), [0.24, 0.12, 0]));
    const lever = new THREE.Group();
    lever.position.set(-0.24, 0.16, 0);
    lever.add(mesh(new THREE.BoxGeometry(0.5, 0.035, 0.07), metal("#e8eef5"), [0.25, 0, 0]));
    lever.add(mesh(new THREE.SphereGeometry(0.06, 12, 8), matte("#ff7a59"), [0.5, 0.02, 0]));
    group.add(lever);
    group.userData.lever = lever;
  },
  bulb(group) {
    group.add(mesh(cylinder(0.2, 0.22, 0.12, 24), metal("#8c9aa8"), [0, 0.06, 0]));
    const glass = new THREE.MeshStandardMaterial({
      color: "#fff6d0", emissive: "#ffd479", emissiveIntensity: 0, transparent: true, opacity: 0.82, roughness: 0.12,
    });
    group.add(mesh(new THREE.SphereGeometry(0.22, 24, 18), glass, [0, 0.33, 0]));
    group.add(mesh(cylinder(0.01, 0.01, 0.16, 6), metal("#ffcf6b"), [0, 0.28, 0]));
    group.add(mesh(cylinder(0.04, 0.04, 0.1, 12), metal("#c9a14a"), [-0.4, 0.06, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.04, 0.04, 0.1, 12), metal("#c9a14a"), [0.4, 0.06, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.02, 0.02, 0.6, 8), metal("#c9a14a"), [0, 0.05, 0], [0, 0, along]));
    const light = new THREE.PointLight("#ffd479", 0, 2.6);
    light.position.set(0, 0.45, 0);
    group.add(light);
    group.userData.glass = glass;
    group.userData.light = light;
  },
  resistor(group) {
    group.add(mesh(cylinder(0.025, 0.025, 0.8, 8), metal("#b9c3cc"), [0, 0.2, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.12, 0.12, 0.42, 20), matte("#d9c29a"), [0, 0.2, 0], [0, 0, along]));
    for (const [x, color] of [[-0.12, "#c0392b"], [-0.04, "#8e44ad"], [0.04, "#e67e22"], [0.13, "#d4af37"]]) {
      group.add(mesh(cylinder(0.124, 0.124, 0.04, 20), matte(color), [x, 0.2, 0], [0, 0, along]));
    }
  },
  motor(group) {
    group.add(mesh(cylinder(0.18, 0.18, 0.5, 24), metal("#6f8fb0"), [-0.08, 0.22, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.025, 0.025, 0.7, 8), metal("#b9c3cc"), [0, 0.22, 0], [0, 0, along]));
    const fan = new THREE.Group();
    fan.position.set(0.32, 0.22, 0);
    for (let blade = 0; blade < 3; blade += 1) {
      const item = mesh(new THREE.BoxGeometry(0.02, 0.28, 0.08), matte("#73e7d4"), [0, 0.14, 0]);
      const pivot = new THREE.Group();
      pivot.rotation.x = (blade * Math.PI * 2) / 3;
      pivot.add(item);
      fan.add(pivot);
    }
    group.add(fan);
    group.userData.fan = fan;
  },
  wire(group) {
    group.add(mesh(cylinder(0.035, 0.035, 1.1, 10), matte("#ff7a59", { roughness: 0.5 }), [0, 0.2, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.05, 0.05, 0.08, 10), metal("#d8edf3"), [-0.58, 0.2, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.05, 0.05, 0.08, 10), metal("#d8edf3"), [0.58, 0.2, 0], [0, 0, along]));
  },
};

function buildCircuitPart(part) {
  const group = new THREE.Group();
  circuitParts[part](group);
  const reach = TERMINAL[part];
  group.userData.terminalMarks = [-reach, reach].map((x) => {
    const mark = mesh(new THREE.SphereGeometry(0.055, 10, 8), new THREE.MeshBasicMaterial({ color: "#8a97a8" }), [x, part === "bulb" ? 0.06 : 0.2, 0]);
    group.add(mark);
    return mark;
  });
  return group;
}

const circuitState = { evaluation: null };

export const circuits = {
  id: "circuits",
  title: "Circuit Lab",
  pitch: "Snap batteries, switches, bulbs, resistors, wires, and motors together. Everyone sees the same live circuit.",
  sceneLabel: "Shared electrical circuit board",
  controls: "Click a part to add it. Drag to move; rotate with R. Touch the gold ends of parts so they connect, close the switch (T), and light the bulb.",
  spawnSpacing: 1.15,
  toggleLabel: "Toggle switch",
  parts: [
    { id: "battery", label: "Battery", name: "Battery" },
    { id: "switch", label: "Switch", name: "Switch", toggles: true },
    { id: "bulb", label: "Bulb", name: "Bulb" },
    { id: "resistor", label: "Resistor", name: "Resistor" },
    { id: "motor", label: "Motor", name: "Motor" },
    { id: "wire", label: "Wire", name: "Wire" },
  ],
  demoLabel: "Load demo circuit",
  demo: [
    { part: "battery", position: [-0.8, 0.08, -0.6], rotation: [0, 0, 0] },
    { part: "switch", position: [0, 0.08, -0.6], rotation: [0, 0, 0], on: true },
    { part: "bulb", position: [0.8, 0.08, -0.6], rotation: [0, 0, 0] },
    { part: "wire", position: [1.2, 0.08, 0], rotation: [0, Math.PI / 2, 0] },
    { part: "wire", position: [0.6, 0.08, 0.6], rotation: [0, 0, 0] },
    { part: "wire", position: [-0.6, 0.08, 0.6], rotation: [0, 0, 0] },
    { part: "wire", position: [-1.2, 0.08, 0], rotation: [0, Math.PI / 2, 0] },
  ],
  build: buildCircuitPart,
  applyState(object) {
    const lever = object.group.userData.lever;
    if (lever) lever.rotation.z = object.data.on ? 0 : 0.62;
  },
  update(objects, time) {
    const items = [...objects.values()].map((object) => ({
      part: object.data.part,
      position: object.group.position.toArray(),
      rotation: [object.group.rotation.x, object.group.rotation.y, object.group.rotation.z],
      on: object.data.on,
    }));
    const list = [...objects.values()];
    const evaluation = evaluateCircuit(items);
    circuitState.evaluation = evaluation;
    list.forEach((object, index) => {
      const data = object.group.userData;
      const live = evaluation.live.has(index);
      data.terminalMarks.forEach((mark, side) => {
        mark.material.color.set(evaluation.touching[index * 2 + side] ? "#4be3a0" : "#8a97a8");
      });
      if (data.glass) {
        const level = live ? evaluation.brightness : 0;
        data.glass.emissiveIntensity = level * 2.2;
        data.light.intensity = level * 4;
      }
      if (data.fan) data.fan.userData.speed = live ? 14 * evaluation.brightness : 0;
    });
    this.tick(objects, time);
    return this.summary(objects);
  },
  tick(objects, time) {
    const dt = Math.min(0.05, time - (this.lastTime ?? time));
    this.lastTime = time;
    for (const object of objects.values()) {
      const fan = object.group.userData.fan;
      if (fan) fan.rotation.x += (fan.userData.speed ?? 0) * dt;
    }
  },
  summary(objects) {
    const evaluation = circuitState.evaluation;
    if (!objects.size) return "Add a battery, then build a loop.";
    if (!evaluation || evaluation.state === "no-battery") return "No battery yet: a circuit needs a power source.";
    if (evaluation.state === "short") return "SHORT CIRCUIT: the battery terminals are joined with no load. Break the connection.";
    if (evaluation.state === "open") return "Open circuit: no complete loop through the battery. Connect the gold ends and close any switch.";
    const list = [...objects.values()];
    const lit = [...evaluation.live].filter((index) => list[index].data.part === "bulb").length;
    const spinning = [...evaluation.live].filter((index) => list[index].data.part === "motor").length;
    const parts = [];
    if (lit) parts.push(`${lit} bulb${lit === 1 ? "" : "s"} lit`);
    if (spinning) parts.push(`${spinning} motor${spinning === 1 ? "" : "s"} running`);
    return `Closed circuit${parts.length ? `: ${parts.join(", ")}` : " (add a bulb or motor as a load)"}.`;
  },
};

// ------------------------------------------------------------------ safety

function stripedBar(length) {
  const group = new THREE.Group();
  const stripes = Math.round(length / 0.2);
  for (let index = 0; index < stripes; index += 1) {
    group.add(mesh(new THREE.BoxGeometry(length / stripes, 0.14, 0.05), matte(index % 2 ? "#1c232c" : "#f2c94c"), [-length / 2 + (index + 0.5) * (length / stripes), 0.5, 0]));
  }
  for (const x of [-length / 2 + 0.06, length / 2 - 0.06]) {
    group.add(mesh(new THREE.BoxGeometry(0.06, 0.52, 0.06), metal("#8c9aa8"), [x, 0.26, 0]));
    group.add(mesh(new THREE.BoxGeometry(0.06, 0.04, 0.3), metal("#8c9aa8"), [x, 0.02, 0]));
  }
  return group;
}

const safetyBuilders = {
  exit(group) {
    group.add(mesh(cylinder(0.025, 0.025, 0.95, 8), metal("#8c9aa8"), [0, 0.475, 0]));
    group.add(mesh(cylinder(0.12, 0.14, 0.04, 18), metal("#3a4a5a"), [0, 0.02, 0]));
    group.add(mesh(new THREE.BoxGeometry(0.52, 0.24, 0.05), new THREE.MeshStandardMaterial({ color: "#0e7a4a", emissive: "#14a863", emissiveIntensity: 0.5 }), [0, 0.95, 0]));
    const label = textSprite("EXIT", "#ffffff", "#0e7a4a00", 0.5);
    label.position.set(0, 0.95, 0.04);
    group.add(label);
  },
  extinguisher(group) {
    group.add(mesh(cylinder(0.11, 0.11, 0.42, 20), matte("#d33a2c", { roughness: 0.4 }), [0, 0.26, 0]));
    group.add(mesh(new THREE.SphereGeometry(0.11, 18, 10, 0, Math.PI * 2, 0, Math.PI / 2), matte("#d33a2c", { roughness: 0.4 }), [0, 0.47, 0]));
    group.add(mesh(cylinder(0.035, 0.035, 0.1, 10), metal("#1b2530"), [0, 0.6, 0]));
    group.add(mesh(new THREE.BoxGeometry(0.22, 0.03, 0.04), metal("#1b2530"), [0.04, 0.66, 0]));
    group.add(mesh(cylinder(0.01, 0.01, 0.3, 6), matte("#1b2530"), [0.12, 0.38, 0], [0, 0, 0.5]));
    group.add(mesh(cylinder(0.13, 0.13, 0.04, 18), matte("#3a4a5a"), [0, 0.02, 0]));
  },
  firstaid(group) {
    group.add(mesh(new THREE.BoxGeometry(0.42, 0.3, 0.16), matte("#f4f7fa"), [0, 0.17, 0]));
    group.add(mesh(new THREE.BoxGeometry(0.22, 0.06, 0.02), matte("#1fa564"), [0, 0.17, 0.09]));
    group.add(mesh(new THREE.BoxGeometry(0.06, 0.22, 0.02), matte("#1fa564"), [0, 0.17, 0.09]));
    group.add(mesh(new THREE.BoxGeometry(0.16, 0.04, 0.05), matte("#cbd5df"), [0, 0.34, 0]));
  },
  barrier(group) {
    group.add(stripedBar(1.2));
  },
  beacon(group) {
    group.add(mesh(cylinder(0.2, 0.24, 0.1, 6), matte("#2b3440"), [0, 0.05, 0]));
    group.add(mesh(cylinder(0.12, 0.2, 0.24, 6), matte("#f2c94c"), [0, 0.2, 0]));
    const dome = new THREE.MeshStandardMaterial({ color: "#ffb36b", emissive: "#ff8a2a", emissiveIntensity: 0.1, transparent: true, opacity: 0.9 });
    group.add(mesh(new THREE.SphereGeometry(0.15, 18, 12, 0, Math.PI * 2, 0, Math.PI / 2), dome, [0, 0.32, 0]));
    const light = new THREE.PointLight("#ff8a2a", 0, 2.2);
    light.position.set(0, 0.5, 0);
    group.add(light);
    group.userData.dome = dome;
    group.userData.light = light;
  },
  muster(group) {
    group.add(mesh(cylinder(0.03, 0.03, 1.2, 8), metal("#8c9aa8"), [0, 0.6, 0]));
    group.add(mesh(cylinder(0.14, 0.16, 0.04, 18), metal("#3a4a5a"), [0, 0.02, 0]));
    group.add(mesh(new THREE.BoxGeometry(0.56, 0.34, 0.03), new THREE.MeshStandardMaterial({ color: "#1fa564", emissive: "#0d5a37", emissiveIntensity: 0.4 }), [0.3, 1.0, 0]));
    const label = textSprite("MUSTER", "#ffffff", "#1fa56400", 0.54);
    label.position.set(0.3, 1.0, 0.03);
    group.add(label);
  },
};

const safetyChecks = [
  ["exit", "exit sign"],
  ["extinguisher", "extinguisher"],
  ["firstaid", "first-aid kit"],
  ["muster", "muster point"],
];

export const safety = {
  id: "safety",
  title: "Site Safety Layout",
  pitch: "Lay out a shared work-area safety plan: exits, extinguishers, first aid, muster point, and barriers around hazards.",
  sceneLabel: "Shared site safety layout board",
  controls: "Click a part to add it. Drag to move; rotate with R. Put a barrier next to each hazard beacon and toggle a beacon (T) to mark an active hazard.",
  spawnSpacing: 0.85,
  toggleLabel: "Toggle hazard beacon",
  parts: [
    { id: "exit", label: "Exit", name: "Exit sign" },
    { id: "extinguisher", label: "Extinguisher", name: "Fire extinguisher" },
    { id: "firstaid", label: "First aid", name: "First-aid kit" },
    { id: "barrier", label: "Barrier", name: "Barrier" },
    { id: "beacon", label: "Beacon", name: "Hazard beacon", toggles: true },
    { id: "muster", label: "Muster", name: "Muster point" },
  ],
  demoLabel: "Load sample layout",
  demo: [
    { part: "exit", position: [-2.0, 0.08, -1.6], rotation: [0, 0.4, 0] },
    { part: "extinguisher", position: [-1.0, 0.08, -1.6], rotation: [0, 0, 0] },
    { part: "firstaid", position: [0.0, 0.08, -1.6], rotation: [0, 0, 0] },
    { part: "muster", position: [2.0, 0.08, -1.2], rotation: [0, -0.4, 0] },
    { part: "beacon", position: [0.6, 0.08, 0.6], rotation: [0, 0, 0], on: true },
    { part: "barrier", position: [0.6, 0.08, 1.2], rotation: [0, 0, 0] },
  ],
  build(part) {
    const group = new THREE.Group();
    safetyBuilders[part](group);
    return group;
  },
  applyState(object) {
    const { dome, light } = object.group.userData;
    if (dome) object.group.userData.active = Boolean(object.data.on);
    if (light && !object.data.on) light.intensity = 0;
  },
  update(objects, time) {
    for (const object of objects.values()) {
      const { dome, light, active } = object.group.userData;
      if (!dome) continue;
      const pulse = active ? 0.5 + 0.5 * Math.sin(time * 6) : 0;
      dome.emissiveIntensity = 0.1 + pulse * 1.6;
      light.intensity = pulse * 3;
    }
    return this.summary(objects);
  },
  summary(objects) {
    if (!objects.size) return "Add parts, or load the sample layout.";
    const items = [...objects.values()].map((object) => ({ part: object.data.part, x: object.group.position.x, z: object.group.position.z, on: object.data.on }));
    const present = (part) => items.some((item) => item.part === part);
    const checks = safetyChecks.map(([part, label]) => `${present(part) ? "✓" : "✗"} ${label}`);
    const barriers = items.filter((item) => item.part === "barrier");
    const unguarded = items.filter((item) => item.part === "beacon"
      && !barriers.some((barrier) => Math.hypot(barrier.x - item.x, barrier.z - item.z) < 1.1)).length;
    const active = items.filter((item) => item.part === "beacon" && item.on).length;
    const hazard = unguarded ? ` ${unguarded} hazard beacon${unguarded === 1 ? "" : "s"} without a barrier.` : items.some((item) => item.part === "beacon") ? " Every hazard beacon has a barrier." : "";
    return `Checklist: ${checks.join(" · ")}.${hazard}${active ? ` ${active} active hazard${active === 1 ? "" : "s"}.` : ""}`;
  },
};

// ---------------------------------------------------------------- hydrotest

const steel = "#9fb0c2";

function hydroHandle(group, y) {
  const material = matte("#d33a2c", { roughness: 0.45 });
  const handle = new THREE.Group();
  handle.position.set(0, y, 0);
  handle.add(mesh(new THREE.BoxGeometry(0.32, 0.03, 0.06), material));
  handle.add(mesh(cylinder(0.04, 0.04, 0.05, 12), metal("#d8edf3"), [0, 0.02, 0]));
  group.add(handle);
  group.userData.handle = handle;
  group.userData.handleMaterial = material;
}

const hydroParts = {
  tank(group) {
    group.add(mesh(cylinder(0.34, 0.34, 0.7, 28), metal("#6f8fb0"), [0, 0.4, 0]));
    group.add(mesh(new THREE.SphereGeometry(0.34, 24, 10, 0, Math.PI * 2, 0, Math.PI / 2), metal("#6f8fb0"), [0, 0.75, 0]));
    group.add(mesh(cylinder(0.38, 0.38, 0.05, 28), matte("#3a4a5a"), [0, 0.025, 0]));
    group.add(mesh(cylinder(0.05, 0.05, 0.1, 12), metal(steel), [0.4, 0.25, 0], [0, 0, along]));
    const label = textSprite("WATER", "#dbeeff", "#00205bdd", 0.5);
    label.position.set(0, 1.15, 0);
    group.add(label);
  },
  pump(group) {
    group.add(mesh(new THREE.BoxGeometry(0.62, 0.05, 0.34), matte("#3a4a5a"), [0, 0.025, 0]));
    group.add(mesh(cylinder(0.14, 0.14, 0.34, 20), matte("#2f6fb5", { roughness: 0.4 }), [-0.14, 0.24, 0], [0, 0, along]));
    group.add(mesh(cylinder(0.17, 0.17, 0.16, 24), metal("#c9d3de"), [0.14, 0.24, 0], [along, 0, 0]));
    group.add(mesh(cylinder(0.05, 0.05, 0.8, 12), metal(steel), [0, 0.25, 0], [0, 0, along]));
    const label = textSprite("PUMP", "#ffffff", "#00205bdd", 0.4);
    label.position.set(0, 0.62, 0);
    group.add(label);
  },
  valve(group) {
    group.add(mesh(cylinder(0.05, 0.05, 0.6, 12), metal(steel), [0, 0.25, 0], [0, 0, along]));
    group.add(mesh(new THREE.SphereGeometry(0.13, 16, 12), metal("#c9a14a"), [0, 0.25, 0]));
    group.add(mesh(cylinder(0.025, 0.025, 0.2, 8), metal("#d8edf3"), [0, 0.42, 0]));
    hydroHandle(group, 0.52);
  },
  vent(group) {
    group.add(mesh(cylinder(0.12, 0.14, 0.04, 18), matte("#3a4a5a"), [0, 0.02, 0]));
    group.add(mesh(cylinder(0.03, 0.03, 0.7, 10), metal(steel), [0, 0.37, 0]));
    group.add(mesh(new THREE.SphereGeometry(0.09, 14, 10), metal("#c9a14a"), [0, 0.72, 0]));
    hydroHandle(group, 0.84);
    const label = textSprite("VENT", "#dbeeff", "#00205bdd", 0.34);
    label.position.set(0, 1.05, 0);
    group.add(label);
  },
  gauge(group) {
    group.add(mesh(cylinder(0.12, 0.14, 0.04, 18), matte("#3a4a5a"), [0, 0.02, 0]));
    group.add(mesh(cylinder(0.025, 0.025, 0.55, 8), metal(steel), [0, 0.3, 0]));
    group.add(mesh(cylinder(0.17, 0.17, 0.06, 28), metal("#8c9aa8"), [0, 0.68, 0], [along, 0, 0]));
    group.add(mesh(cylinder(0.145, 0.145, 0.065, 28), matte("#f4f7fa"), [0, 0.68, 0], [along, 0, 0]));
    const needle = new THREE.Group();
    needle.position.set(0, 0.68, 0.04);
    needle.add(mesh(new THREE.BoxGeometry(0.02, 0.12, 0.01), matte("#c0392b"), [0, 0.06, 0]));
    group.add(needle);
    group.userData.needle = needle;
  },
  exchanger(group) {
    const shell = metal("#aebdcb");
    group.add(mesh(cylinder(0.26, 0.26, 1.5, 32), shell, [0, 0.3, 0], [0, 0, along]));
    for (const x of [-0.8, 0.8]) {
      group.add(mesh(cylinder(0.3, 0.3, 0.1, 32), metal("#00205b"), [x, 0.3, 0], [0, 0, along]));
      group.add(mesh(cylinder(0.06, 0.06, 0.12, 12), metal(steel), [x * 1.1, 0.3, 0], [0, 0, along]));
    }
    for (const x of [-0.45, 0.45]) group.add(mesh(new THREE.BoxGeometry(0.14, 0.2, 0.4), matte("#3a4a5a"), [x, 0.1, 0]));
    for (const [x] of NOZZLES) {
      group.add(mesh(cylinder(0.09, 0.09, 0.2, 14), metal(steel), [x, 0.57, 0]));
      group.add(mesh(cylinder(0.13, 0.13, 0.03, 20), metal("#c9d3de"), [x, 0.675, 0]));
    }
    const label = textSprite("EXCH.", "#ffffff", "#00205bdd", 0.45);
    label.position.set(0, 0.95, -0.4);
    group.add(label);
    group.userData.shell = shell;
  },
  blind(group) {
    group.add(mesh(cylinder(0.12, 0.14, 0.04, 18), matte("#3a4a5a"), [0, 0.02, 0]));
    group.add(mesh(cylinder(0.02, 0.02, 0.68, 8), metal("#8c9aa8", { transparent: true, opacity: 0.55 }), [0, 0.38, 0]));
    group.add(mesh(cylinder(0.15, 0.15, 0.05, 24), metal("#ffb81c"), [0, 0.72, 0]));
    for (let bolt = 0; bolt < 6; bolt += 1) {
      const angle = (bolt * Math.PI) / 3;
      group.add(mesh(cylinder(0.018, 0.018, 0.03, 6), metal("#6b5a1a"), [Math.cos(angle) * 0.105, 0.75, Math.sin(angle) * 0.105]));
    }
  },
  pipe(group) {
    group.add(mesh(cylinder(0.06, 0.06, 0.8, 14), metal(steel), [0, 0.25, 0], [0, 0, along]));
    for (const x of [-0.4, 0.4]) group.add(mesh(cylinder(0.1, 0.1, 0.04, 16), metal("#c9d3de"), [x, 0.25, 0], [0, 0, along]));
  },
};

function buildHydroPart(part) {
  const group = new THREE.Group();
  hydroParts[part](group);
  const mark = (x, y, z) => {
    const item = mesh(new THREE.SphereGeometry(0.05, 10, 8), new THREE.MeshBasicMaterial({ color: "#8a97a8" }), [x, y, z]);
    group.add(item);
    return item;
  };
  group.userData.portMarks = (PORTS[part] ?? []).map(([x, z, y]) => mark(x, y, z));
  if (part === "exchanger") group.userData.nozzleMarks = NOZZLES.map(([x, z, y]) => mark(x, y + 0.08, z));
  return group;
}

export const hydrotest = {
  id: "hydrotest",
  title: "Hydrostatic Test Rig",
  pitch: "Together, assemble a water-filled test rig for a shell-and-tube heat exchanger: cap the nozzles, vent the air, and plumb a fill line. Safety awareness practice, not a test procedure.",
  sceneLabel: "Shared hydrostatic test rig board",
  controls: "Click a part to add it. Drag to move; rotate with R. Touch the grey port dots so parts connect, put a gold blind flange over each top nozzle, set the vent and valve open (T), then watch the checklist.",
  spawnSpacing: 0.95,
  toggleLabel: "Open / close valve",
  parts: [
    { id: "tank", label: "Tank", name: "Water tank" },
    { id: "pump", label: "Pump", name: "Pump" },
    { id: "valve", label: "Valve", name: "Valve", toggles: true },
    { id: "vent", label: "Vent valve", name: "Vent valve", toggles: true },
    { id: "gauge", label: "Gauge", name: "Pressure gauge" },
    { id: "exchanger", label: "Exchanger", name: "Heat exchanger" },
    { id: "blind", label: "Blind flange", name: "Blind flange" },
    { id: "pipe", label: "Pipe", name: "Pipe" },
  ],
  demoLabel: "Load demo rig",
  demo: [
    { part: "tank", position: [-2.35, 0.08, 0.3], rotation: [0, 0, 0] },
    { part: "pump", position: [-1.6, 0.08, 0.3], rotation: [0, 0, 0] },
    { part: "pipe", position: [-0.8, 0.08, 0.3], rotation: [0, 0, 0] },
    { part: "valve", position: [-0.1, 0.08, 0.3], rotation: [0, 0, 0], on: true },
    { part: "exchanger", position: [1.1, 0.08, 0.3], rotation: [0, 0, 0] },
    { part: "blind", position: [0.6, 0.08, 0.3], rotation: [0, 0, 0] },
    { part: "blind", position: [1.6, 0.08, 0.3], rotation: [0, 0, 0] },
    { part: "vent", position: [1.1, 0.08, 0.85], rotation: [0, 0, 0], on: true },
    { part: "gauge", position: [-0.6, 0.08, 0.7], rotation: [0, 0, 0] },
  ],
  build: buildHydroPart,
  applyState(object) {
    const { handle, handleMaterial } = object.group.userData;
    if (!handle) return;
    handle.rotation.y = object.data.on ? 0 : Math.PI / 2;
    handleMaterial.color.set(object.data.on ? "#1fa564" : "#d33a2c");
  },
  update(objects, time) {
    const list = [...objects.values()];
    if (!list.length) return "Place the heat exchanger, or load the demo rig.";
    const items = list.map((object) => ({
      part: object.data.part,
      position: object.group.position.toArray(),
      rotation: [object.group.rotation.x, object.group.rotation.y, object.group.rotation.z],
      on: object.data.on,
    }));
    const result = evaluateHydro(items);
    list.forEach((object, index) => {
      const data = object.group.userData;
      data.portMarks?.forEach((mark, side) => mark.material.color.set(result.touching[index][side] ? "#4be3a0" : "#8a97a8"));
      if (index === result.exchangerIndex) {
        data.nozzleMarks.forEach((mark, side) => mark.material.color.set(result.nozzleCapped[side] ? "#4be3a0" : "#ffb81c"));
        data.shell.emissive.set(result.ready ? "#1d6a8c" : "#000000");
      }
      if (data.needle) data.needle.rotation.z = result.ready ? -0.2 + 0.03 * Math.sin(time * 3) : 1.2;
    });
    return summaryText(result);
  },
};

export const themes = { circuits, safety, hydrotest };
