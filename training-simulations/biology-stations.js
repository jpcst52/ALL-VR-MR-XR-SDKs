import * as THREE from "three";
import { ball, box, cyl, part } from "./industrial-stations.js";
import { plateTexture } from "../brand-kit/panels.js";

// Work-area model for the DRAFT "Circulation Builder" life-science scenario (see biology-scenarios.js).
// A simplified textbook schematic, not an anatomical model: lungs on top, the heart in the middle, the body's
// capillaries at the bottom, seen from the front (so the heart's right side is on the viewer's left).
// Same contract as industrial-stations.js / partner-stations.js: stays under ~1.85 units tall, exposes
// userData.reveal parts for hotspots, userData.setState(state) for "" / "t2" / "t3" / "t4", and userData.tick(seconds).

const RED = "#d8384a"; // diagram convention: oxygen-rich
const BLUE = "#3f6fd8"; // diagram convention: oxygen-poor (real blood is dark red, not blue)
const GREY = "#8a96a3"; // not yet coloured (task 4)

// A small flat text plate: a rounded, bordered panel with an accent bar (shared brand kit, ../brand-kit/panels.js).
function plate(group, lines, [x, y, z], [w, h], { border = "#7fc4ff" } = {}) {
  const map = plateTexture(lines, { width: 512, height: Math.round((512 * h) / w), accent: border });
  const mesh = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ map, transparent: true, toneMapped: false }));
  mesh.position.set(x, y, z);
  mesh.userData.isTrainingUi = true;
  group.add(mesh);
  return mesh;
}

// A blood vessel along a smooth curve, with a few moving "blood" dots that animate in tick().
function vessel(points, color, radius = 0.035, dots = 0) {
  const group = new THREE.Group();
  const curve = new THREE.CatmullRomCurve3(points.map((point) => new THREE.Vector3(...point)));
  const tube = new THREE.Mesh(
    new THREE.TubeGeometry(curve, 48, radius, 10, false),
    new THREE.MeshStandardMaterial({ color, roughness: 0.45, metalness: 0.05 }),
  );
  group.add(tube);
  group.userData.curve = curve;
  group.userData.dots = [];
  for (let index = 0; index < dots; index += 1) {
    const dot = new THREE.Mesh(ball(radius * 0.9), new THREE.MeshBasicMaterial({ color: new THREE.Color(color).lerp(new THREE.Color("#ffffff"), 0.45) }));
    dot.userData.offset = index / dots;
    group.add(dot);
    group.userData.dots.push(dot);
  }
  return group;
}

function chamber(group, [x, y, z], [sx, sy, sz], color) {
  const mesh = part(group, new THREE.SphereGeometry(1, 24, 16), color, [x, y, z], { rough: 0.5 });
  mesh.scale.set(sx, sy, sz);
  mesh.userData.baseScale = [sx, sy, sz];
  return mesh;
}

function valve(group, [x, y, z]) {
  const ring = new THREE.Group();
  ring.position.set(x, y, z);
  part(ring, new THREE.TorusGeometry(0.06, 0.012, 8, 20), "#f2f6fc", [0, 0, 0], { rot: [Math.PI / 2, 0, 0], rough: 0.4 });
  for (const angle of [0, 2.1, 4.2]) part(ring, box(0.05, 0.006, 0.03), "#ffd23f", [Math.cos(angle) * 0.025, 0, Math.sin(angle) * 0.025], { rot: [0, -angle, 0.25], basic: true });
  group.add(ring);
  return ring;
}

export function createCirculationStation() {
  const group = new THREE.Group();
  const reveal = (group.userData.reveal = {});

  // Display plinth and back board.
  part(group, box(2.5, 0.12, 0.9), "#2a3644", [0, 0.06, 0], { rough: 0.8 });
  part(group, box(2.5, 1.7, 0.04), "#18222e", [0, 0.97, -0.42], { rough: 0.9 });
  plate(group, ["CIRCULATION BUILDER", "DRAFT FOR FACULTY REVIEW"], [0, 1.84, -0.39], [0.95, 0.16], { border: "#ffb81c" });

  // Lung capillary bed (top) and body capillary bed (bottom): neutral until the loop reaches them.
  const lungs = new THREE.Group();
  for (const x of [-0.42, 0.42]) {
    const lobe = part(lungs, new THREE.SphereGeometry(1, 24, 16), "#e7a3ad", [x, 1.62, -0.12], { rough: 0.8, opacity: 0.85 });
    lobe.scale.set(0.3, 0.17, 0.16);
  }
  part(lungs, cyl(0.025, 0.025, 0.5, 10), "#c8d0d8", [0, 1.62, -0.12], { rot: [0, 0, Math.PI / 2] }); // airway between the lungs
  part(lungs, cyl(0.03, 0.03, 0.14, 10), "#c8d0d8", [0, 1.71, -0.12]);
  group.add(lungs);
  plate(group, ["LUNGS"], [0, 1.47, 0.0], [0.26, 0.08]);

  part(group, box(1.75, 0.16, 0.42), "#c98c7f", [0, 0.29, 0.05], { rough: 0.85 });
  const capillaries = new THREE.Group();
  for (let index = 0; index < 9; index += 1) part(capillaries, cyl(0.008, 0.008, 0.36, 6), "#8f4f55", [-0.75 + index * 0.19, 0.38, 0.12], { rot: [Math.PI / 2, 0, 0], basic: true });
  group.add(capillaries);
  plate(group, ["BODY TISSUES"], [0, 0.29, 0.265], [0.42, 0.09]);

  // Heart outline (always shown) with the septum down the middle.
  const outline = part(group, new THREE.SphereGeometry(1, 24, 16), "#c9d6e2", [0, 0.99, 0.0], { opacity: 0.16, rough: 0.3 });
  outline.scale.set(0.46, 0.42, 0.24);
  outline.material.depthWrite = false;
  part(group, box(0.025, 0.62, 0.3), "#f2f6fc", [0, 0.98, 0.04], { opacity: 0.6 });
  plate(group, ["HEART (front view)"], [0, 0.5, 0.2], [0.42, 0.08]);

  // Chambers (placed in task 1). Right side oxygen-poor (blue), left side oxygen-rich (red).
  const chambers = {
    ra: chamber(group, [-0.2, 1.17, 0.04], [0.15, 0.13, 0.15], "#5a86e0"),
    rv: chamber(group, [-0.16, 0.86, 0.06], [0.17, 0.19, 0.16], BLUE),
    la: chamber(group, [0.2, 1.17, 0.04], [0.15, 0.13, 0.15], "#e65a69"),
    lv: chamber(group, [0.15, 0.83, 0.06], [0.19, 0.22, 0.17], RED),
  };
  for (const [name, mesh] of Object.entries(chambers)) {
    mesh.visible = false;
    reveal[name] = mesh;
  }

  // Valves between atria and ventricles.
  reveal.tricuspid = valve(group, [-0.2, 1.02, 0.12]);
  reveal.mitral = valve(group, [0.19, 1.02, 0.12]);

  // Vessels: coloured versions (tasks 2-3, revealed when connected; task 4, revealed when coloured) and grey versions (task 4).
  const paths = {
    venae: { color: BLUE, points: [[-0.72, 0.38, 0.12], [-0.8, 0.62, 0.14], [-0.72, 0.95, 0.12], [-0.5, 1.17, 0.08], [-0.3, 1.18, 0.06]] },
    pa: { color: BLUE, points: [[-0.12, 0.98, 0.14], [-0.1, 1.3, 0.16], [-0.32, 1.44, 0.12], [-0.48, 1.56, 0.0]] },
    pv: { color: RED, points: [[0.48, 1.56, -0.04], [0.5, 1.42, 0.06], [0.4, 1.26, 0.06], [0.3, 1.2, 0.04]] },
    aorta: { color: RED, points: [[0.1, 0.98, 0.16], [0.16, 1.3, 0.2], [0.42, 1.4, 0.2], [0.64, 1.36, 0.16], [0.8, 1.12, 0.14], [0.82, 0.7, 0.14], [0.72, 0.38, 0.12]] },
  };
  const coloured = {};
  const greys = {};
  for (const [name, { color, points }] of Object.entries(paths)) {
    const radius = name === "aorta" ? 0.045 : 0.035;
    coloured[name] = vessel(points, color, radius, 4);
    greys[name] = vessel(points, GREY, radius * 0.97, 0);
    coloured[name].visible = false;
    greys[name].visible = false;
    group.add(coloured[name], greys[name]);
    reveal[name] = coloured[name];
    reveal[`${name}Grey`] = greys[name];
  }
  reveal.venaeBlue = coloured.venae;
  reveal.paBlue = coloured.pa;
  reveal.pvRed = coloured.pv;
  reveal.aortaRed = coloured.aorta;

  // Gas exchange in the lungs (oxygen in, carbon dioxide out) and in the body tissues.
  const exchange = new THREE.Group();
  for (let index = 0; index < 10; index += 1) {
    const angle = (index / 10) * Math.PI * 2;
    part(exchange, ball(0.022), index % 2 ? "#f2f6fc" : RED, [-0.42 + Math.cos(angle) * 0.24, 1.62 + Math.sin(angle) * 0.13, 0.08], { basic: true });
  }
  exchange.visible = false;
  group.add(exchange);
  reveal.exchange = exchange;
  plate(exchange, ["O2 in · CO2 out"], [0.42, 1.62, 0.1], [0.34, 0.07], { border: RED });
  const tissue = new THREE.Group();
  part(tissue, box(1.6, 0.03, 0.36), "#ff8fa0", [0, 0.39, 0.06], { opacity: 0.35, basic: true });
  plate(tissue, ["O2 to tissues · CO2 back"], [0, 0.18, 0.27], [0.5, 0.07], { border: RED });
  tissue.visible = false;
  group.add(tissue);
  reveal.tissue = tissue;

  // Distractor props for the wrong actions.
  const mirrorSign = plate(group, ["YOUR LEFT  |  YOUR RIGHT", "(body faces you)"], [-1.0, 1.36, 0.0], [0.42, 0.16], { border: "#ffb81c" });
  const shortcut = vessel([[0.62, 1.64, -0.04], [1.02, 1.6, 0.06], [1.1, 1.0, 0.08], [1.04, 0.4, 0.1], [0.84, 0.34, 0.1]], "#6b7480", 0.025, 0);
  shortcut.children[0].material.transparent = true;
  shortcut.children[0].material.opacity = 0.55;
  group.add(shortcut);
  const ruleCard = plate(group, ["RULE?", "ARTERY = RED", "VEIN = BLUE"], [-1.08, 1.4, 0.0], [0.34, 0.26], { border: "#ff6b6b" });

  const allVessels = Object.keys(paths);
  group.userData.setState = (state) => {
    const stage = { "": 1, t2: 2, t3: 3, t4: 4 }[state] ?? 1;
    for (const mesh of Object.values(chambers)) mesh.visible = stage >= 2;
    reveal.tricuspid.visible = stage >= 3;
    reveal.mitral.visible = stage >= 4;
    for (const name of allVessels) {
      const connected = stage === 3 ? name === "venae" || name === "pa" : false;
      coloured[name].visible = connected;
      greys[name].visible = stage === 4;
    }
    exchange.visible = stage >= 3;
    tissue.visible = stage >= 4;
    mirrorSign.visible = stage === 1;
    shortcut.visible = stage === 3;
    ruleCard.visible = stage === 4;
  };
  group.userData.setState("");

  group.userData.tick = (time) => {
    const beat = 1 + Math.max(0, Math.sin(time * 7)) * 0.05;
    for (const mesh of Object.values(chambers)) {
      const [sx, sy, sz] = mesh.userData.baseScale;
      mesh.scale.set(sx * beat, sy * beat, sz * beat);
    }
    for (const name of allVessels) {
      const { curve, dots } = coloured[name].userData;
      for (const dot of dots) curve.getPointAt((time * 0.25 + dot.userData.offset) % 1, dot.position);
    }
  };
  return group;
}
