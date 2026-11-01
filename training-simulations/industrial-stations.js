import * as THREE from "three";

// Small diorama models for the industrial scenarios. Each group stays under ~1.85 units tall and may set
// userData.tick(seconds) for light animation (blinking alarms, swaying platform, weld flicker).

export const box = (w, h, d) => new THREE.BoxGeometry(w, h, d);
export const cyl = (top, bottom, height, segments = 16) => new THREE.CylinderGeometry(top, bottom, height, segments);
export const ball = (radius) => new THREE.SphereGeometry(radius, 16, 12);

export function part(group, geometry, color, [x, y, z], { rot, basic, opacity, metal = 0.2, rough = 0.6, emissive } = {}) {
  const material = basic
    ? new THREE.MeshBasicMaterial({ color })
    : new THREE.MeshStandardMaterial({ color, metalness: metal, roughness: rough, ...(emissive ? { emissive, emissiveIntensity: 0.9 } : {}) });
  if (opacity !== undefined) {
    material.transparent = true;
    material.opacity = opacity;
  }
  const mesh = new THREE.Mesh(geometry, material);
  mesh.position.set(x, y, z);
  if (rot) mesh.rotation.set(...rot);
  group.add(mesh);
  return mesh;
}

// A line between two points, drawn as a thin cylinder.
function link(group, from, to, color, radius = 0.012) {
  const a = new THREE.Vector3(...from);
  const b = new THREE.Vector3(...to);
  const mesh = part(group, cyl(radius, radius, a.distanceTo(b), 8), color, [0, 0, 0], { basic: true });
  mesh.position.copy(a).lerp(b, 0.5);
  mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), b.clone().sub(a).normalize());
  return mesh;
}

// Parts that tasks can show or hide (userData.reveal[name]); they start hidden.
export function shownPart(group, name, object) {
  (group.userData.reveal ??= {})[name] = object;
  group.add(object);
  return object;
}

export function hiddenPart(group, name, object) {
  object.visible = false;
  (group.userData.reveal ??= {})[name] = object;
  group.add(object);
  return object;
}

export function addPerson(group, [x, y, z], vest = "#ff9d59") {
  part(group, cyl(0.09, 0.1, 0.4), "#3b5a78", [x, y + 0.2, z]);
  part(group, box(0.24, 0.22, 0.17), vest, [x, y + 0.5, z], { rough: 0.7 });
  part(group, ball(0.085), "#e0b99a", [x, y + 0.74, z]);
  part(group, new THREE.SphereGeometry(0.095, 16, 8, 0, Math.PI * 2, 0, Math.PI / 2), "#ffd23f", [x, y + 0.77, z]);
}

export function createHeightStation() {
  const group = new THREE.Group();
  const deckY = 0.7;
  for (const x of [-0.65, 0.65]) {
    for (const z of [-0.35, 0.35]) part(group, cyl(0.035, 0.035, 1.75, 10), "#aeb9c4", [x, 0.875, z], { metal: 0.5 });
  }
  part(group, box(1.5, 0.05, 0.84), "#8b6f4b", [0, deckY, 0], { rough: 0.9 });
  for (const y of [0.98, 1.2]) {
    for (const z of [-0.35, 0.35]) part(group, box(1.3, 0.03, 0.03), "#d8edf3", [0, y, z], { metal: 0.4 });
    for (const x of [-0.65, 0.65]) part(group, box(0.03, 0.03, 0.7), "#d8edf3", [x, y, 0], { metal: 0.4 });
  }
  for (const x of [-0.65, 0.65]) part(group, box(0.02, 0.9, 0.025), "#aeb9c4", [x, 0.45, 0], { rot: [0, 0, 0.0], metal: 0.4 });
  part(group, box(1.4, 0.05, 0.05), "#ffd23f", [0, 1.75, 0], { rough: 0.5 });
  for (const x of [-0.7, 0.7]) part(group, box(0.05, 0.4, 0.05), "#ffd23f", [x, 1.58, 0]);
  addPerson(group, [-0.2, deckY + 0.03, 0.05]);
  part(group, box(0.26, 0.04, 0.2), "#ff9d59", [-0.2, deckY + 0.55, 0.05], { basic: true });
  link(group, [-0.2, deckY + 0.75, 0.05], [-0.2, 1.75, 0], "#ff9d59", 0.014);
  const ladder = new THREE.Group();
  for (const x of [-0.12, 0.12]) part(ladder, box(0.03, 1.1, 0.03), "#c5d4de", [x, 0, 0], { metal: 0.5 });
  for (let rung = 0; rung < 6; rung += 1) part(ladder, box(0.24, 0.025, 0.025), "#c5d4de", [0, -0.46 + rung * 0.18, 0], { metal: 0.5 });
  ladder.position.set(1.0, 0.57, 0.1);
  ladder.rotation.z = -0.22;
  group.add(ladder);
  part(group, box(0.1, 0.18, 0.02), "#3ee08f", [-0.65, 0.55, 0.4], { basic: true }); // scaffold tag
  part(group, box(0.22, 0.14, 0.14), "#8b5a2b", [0.4, 0.8, 0.25], { rough: 0.9 }); // tool bag
  const zone = new THREE.Group();
  part(zone, box(0.7, 0.025, 0.03), "#ffd23f", [0.25, 0.3, 0.62], { basic: true });
  for (const x of [-0.1, 0.6]) part(zone, new THREE.ConeGeometry(0.07, 0.3, 4), "#ff9d59", [x, 0.15, 0.62]);
  hiddenPart(group, "zone", zone);
  let windy = false;
  group.userData.worker = [-0.2, 1.3, 0.05];
  group.userData.setState = (state) => { windy = state === "wind"; };
  group.userData.tick = (time) => {
    group.rotation.z = windy ? Math.sin(time * 3.2) * 0.022 : 0;
  };
  return group;
}

export function createControlRoomStation() {
  const group = new THREE.Group();
  part(group, box(1.7, 0.07, 0.62), "#3a5265", [0, 0.8, 0], { rough: 0.5 });
  for (const x of [-0.75, 0.75]) part(group, box(0.1, 0.8, 0.5), "#2c3e50", [x, 0.4, 0]);
  const screens = ["#3ee08f", "#ffb36b", "#ff5f5f"];
  const alarmScreens = [];
  screens.forEach((color, index) => {
    const x = (index - 1) * 0.52;
    const yaw = (1 - index) * 0.2;
    part(group, cyl(0.025, 0.04, 0.2, 8), "#92a9ba", [x, 0.94, -0.12]);
    part(group, box(0.46, 0.3, 0.03), "#1a2533", [x, 1.17, -0.15], { rot: [0, yaw, 0] });
    const screen = part(group, box(0.4, 0.24, 0.015), color, [x, 1.17, -0.13], { rot: [0, yaw, 0], basic: true });
    if (index === 2) alarmScreens.push(screen);
  });
  part(group, box(1.5, 0.2, 0.05), "#2a3a4a", [0, 1.62, -0.28]);
  const lights = [];
  for (let index = 0; index < 6; index += 1) {
    const lit = index === 4 || index === 5;
    const light = part(group, box(0.16, 0.1, 0.02), lit ? "#ff5f5f" : "#3ee08f", [-0.58 + index * 0.23, 1.62, -0.25], { basic: true });
    if (lit) lights.push(light);
  }
  part(group, box(0.44, 0.06, 0.4), "#33566a", [0, 0.5, 0.62]);
  part(group, box(0.44, 0.42, 0.05), "#33566a", [0, 0.72, 0.8]);
  part(group, cyl(0.03, 0.03, 0.34, 8), "#92a9ba", [0, 0.3, 0.62]);
  const beacon = part(group, ball(0.07), "#ff5f5f", [0.7, 1.67, -0.28], { basic: true });
  group.userData.tick = (time) => {
    const on = Math.sin(time * 6) > 0;
    beacon.visible = on;
    for (const light of lights) light.visible = on;
    for (const screen of alarmScreens) screen.material.color.set(on ? "#ff5f5f" : "#8a3a3a");
  };
  return group;
}

export function createBatteryStation() {
  const group = new THREE.Group();
  const vessels = [[-0.9, 0.34, 1.3], [-0.2, 0.28, 1.0]];
  for (const [x, radius, height] of vessels) {
    part(group, cyl(radius, radius, height, 28), "#9aa8b4", [x, height / 2, -0.1], { metal: 0.5, rough: 0.4 });
    part(group, new THREE.SphereGeometry(radius, 24, 10, 0, Math.PI * 2, 0, Math.PI / 2), "#9aa8b4", [x, height, -0.1], { metal: 0.5, rough: 0.4 });
    part(group, cyl(radius + 0.01, radius + 0.01, 0.04, 28), "#f2c94c", [x, height * 0.55, -0.1], { basic: true });
  }
  for (const y of [0.45, 0.58, 0.71]) part(group, cyl(0.035, 0.035, 1.4, 10), "#c5d4de", [0.4, y, -0.5], { rot: [0, 0, Math.PI / 2], metal: 0.5 });
  for (const x of [-0.3, 0.4, 1.1]) part(group, box(0.05, 0.8, 0.05), "#657b8c", [x, 0.4, -0.5]);
  part(group, cyl(0.04, 0.05, 1.75, 10), "#8a99a8", [1.2, 0.875, -0.2], { metal: 0.5 });
  const flame = part(group, new THREE.ConeGeometry(0.07, 0.22, 10), "#ff9d59", [1.2, 1.85, -0.2], { basic: true });
  part(group, cyl(0.02, 0.02, 0.9, 8), "#c5d4de", [-1.4, 0.45, 0.5], { metal: 0.5 });
  const sock = part(group, new THREE.ConeGeometry(0.07, 0.3, 10, 1, true), "#ff9d59", [-1.28, 0.88, 0.5], { rot: [0, 0, -Math.PI / 2 + 0.1] });
  sock.material.side = THREE.DoubleSide;
  part(group, cyl(0.025, 0.025, 0.55, 8), "#92a9ba", [0.55, 0.28, 0.45]);
  const monitor = part(group, box(0.14, 0.12, 0.08), "#f2c94c", [0.55, 0.6, 0.45]);
  part(group, new THREE.TorusGeometry(0.31, 0.015, 8, 28), "#c5d4de", [-0.2, 0.35, -0.1], { rot: [Math.PI / 2, 0, 0], metal: 0.5 });
  const leak = part(new THREE.Group(), new THREE.CircleGeometry(0.28, 20), "#2a1a12", [0, 0, 0], { rot: [-Math.PI / 2, 0, 0], opacity: 0.9, basic: true });
  leak.position.set(-0.2, 0.012, 0.42);
  leak.parent.add(leak);
  hiddenPart(group, "leak", leak.parent);
  part(group, box(0.02, 0.45, 0.02), "#92a9ba", [1.05, 0.22, 0.55]);
  part(group, box(0.36, 0.22, 0.03), "#3ee08f", [1.05, 0.52, 0.55], { basic: true }); // muster sign
  const barricade = new THREE.Group();
  for (const [x, z, w, d] of [[-0.2, 0.75, 0.9, 0.03], [-0.2, 0.1, 0.9, 0.03], [-0.65, 0.42, 0.03, 0.65], [0.25, 0.42, 0.03, 0.65]]) {
    part(barricade, box(w, 0.05, d), "#ffd23f", [x, 0.3, z], { basic: true });
  }
  for (const [x, z] of [[-0.65, 0.1], [0.25, 0.1], [-0.65, 0.75], [0.25, 0.75]]) part(barricade, cyl(0.02, 0.02, 0.32, 6), "#8c9aa8", [x, 0.16, z]);
  hiddenPart(group, "barricade", barricade);
  let alarmed = false;
  group.userData.setState = (state) => { alarmed = state === "alarm"; };
  group.userData.tick = (time) => {
    monitor.material.color.set(alarmed && Math.sin(time * 9) > 0 ? "#ff3b3b" : "#f2c94c");
    flame.scale.set(1, 0.85 + Math.sin(time * 9) * 0.18, 1);
    sock.rotation.z = -Math.PI / 2 + 0.1 + Math.sin(time * 1.6) * 0.12;
  };
  return group;
}

export function createFacadeStation() {
  const group = new THREE.Group();
  const columns = 8;
  const rows = 4;
  for (let col = 0; col < columns; col += 1) {
    for (let row = 0; row < rows; row += 1) {
      part(group, box(0.29, 0.36, 0.02), "#5d9ac4", [-1.12 + col * 0.32, 0.25 + row * 0.4, -0.5], { opacity: 0.72, metal: 0.6, rough: 0.12 });
    }
  }
  for (let col = 0; col <= columns; col += 1) part(group, box(0.025, 1.65, 0.04), "#cfd9e2", [-1.28 + col * 0.32, 0.9, -0.5], { metal: 0.5 });
  part(group, box(2.7, 0.08, 0.1), "#cfd9e2", [0, 1.78, -0.5], { metal: 0.5 });
  part(group, box(0.12, 0.08, 0.7), "#ffd23f", [0, 1.78, -0.2]); // roof outrigger
  const cradle = new THREE.Group();
  cradle.position.set(0, 0.72, 0.15);
  part(cradle, box(1.0, 0.06, 0.36), "#b8c3cd", [0, 0, 0], { metal: 0.5 });
  for (const z of [-0.17, 0.17]) part(cradle, box(1.0, 0.03, 0.025), "#ff9d59", [0, 0.28, z]);
  for (const x of [-0.5, 0.5]) part(cradle, box(0.03, 0.3, 0.36), "#ff9d59", [x, 0.15, 0]);
  part(cradle, cyl(0.07, 0.06, 0.12, 12), "#4aa0d0", [0.32, 0.09, 0.05]);
  addPerson(cradle, [-0.15, 0.03, 0.02], "#ff9d59");
  link(cradle, [-0.5, 0.3, 0], [-0.5, 1.06, -0.2], "#f2f6fc", 0.012);
  link(cradle, [0.5, 0.3, 0], [0.5, 1.06, -0.2], "#f2f6fc", 0.012);
  link(cradle, [-0.15, 0.78, 0.02], [0.05, 1.06, -0.2], "#ff9d59", 0.014);
  group.add(cradle);
  const zone = new THREE.Group();
  part(zone, box(0.9, 0.025, 0.03), "#ffd23f", [0.1, 0.3, 0.8], { basic: true });
  for (const x of [-0.3, 0.5]) part(zone, new THREE.ConeGeometry(0.07, 0.3, 4), "#ff9d59", [x, 0.15, 0.8]);
  hiddenPart(group, "zone", zone);
  const walker = new THREE.Group();
  addPerson(walker, [0, 0, 0], "#7ab6ff");
  walker.position.set(-1.3, 0, 0.95);
  hiddenPart(group, "walker", walker);
  let amplitude = 0.06;
  group.userData.worker = [-0.15, 1.1, 0.17];
  group.userData.setState = (state) => { amplitude = state === "wind" ? 0.16 : 0.06; };
  group.userData.tick = (time) => {
    cradle.position.x = Math.sin(time * (amplitude > 0.1 ? 2.2 : 0.8)) * amplitude;
    cradle.rotation.z = Math.sin(time * (amplitude > 0.1 ? 2.2 : 0.8)) * amplitude * 0.5;
    if (walker.visible) walker.position.x = -1.3 + ((time * 0.18) % 1.5);
  };
  return group;
}

export function createWeldingStation() {
  const group = new THREE.Group();
  part(group, box(1.5, 0.08, 0.7), "#586979", [0, 0.8, 0], { metal: 0.6 });
  for (const x of [-0.65, 0.65]) for (const z of [-0.25, 0.25]) part(group, box(0.06, 0.78, 0.06), "#3a4856", [x, 0.4, z]);
  part(group, box(0.5, 0.04, 0.3), "#7a8794", [-0.2, 0.86, 0], { metal: 0.7 });
  part(group, box(0.5, 0.04, 0.3), "#7a8794", [-0.2, 0.9, 0.02], { metal: 0.7, rot: [0, 0.1, 0] });
  part(group, box(0.04, 1.2, 0.9), "#d9772b", [-1.0, 0.65, 0.0], { opacity: 0.42, basic: true }); // welding curtain
  part(group, box(0.06, 1.3, 0.06), "#3a4856", [-1.0, 0.65, -0.46]);
  part(group, box(0.06, 1.3, 0.06), "#3a4856", [-1.0, 0.65, 0.46]);
  part(group, cyl(0.025, 0.02, 0.5, 10), "#2b3440", [0.1, 1.05, 0.02], { rot: [0, 0, 1.1] });
  const tip = part(group, ball(0.04), "#ffd9a0", [-0.12, 0.93, 0.0], { basic: true });
  const glow = new THREE.PointLight("#ffb347", 4, 2.2);
  glow.position.set(-0.12, 0.96, 0.02);
  group.add(glow);
  const sparks = [];
  for (let index = 0; index < 8; index += 1) sparks.push(part(group, ball(0.012), "#ffd23f", [-0.12, 0.95, 0.0], { basic: true }));
  for (const [x, color] of [[0.95, "#6f8fa8"], [1.15, "#8a8a8a"]]) {
    part(group, cyl(0.09, 0.09, 0.9, 18), color, [x, 0.45, -0.2], { metal: 0.6, rough: 0.35 });
    part(group, ball(0.09), color, [x, 0.9, -0.2], { metal: 0.6 });
    part(group, box(0.04, 0.05, 0.04), "#d8edf3", [x, 1.0, -0.2], { metal: 0.6 });
  }
  hiddenPart(group, "chain", part(new THREE.Group(), box(0.5, 0.025, 0.025), "#c5d4de", [1.05, 0.7, -0.08], { metal: 0.6 }).parent);
  part(group, box(0.45, 0.25, 0.35), "#b08a5a", [-0.95, 0.125, -0.3], { rough: 0.95 }); // stored cardboard
  part(group, box(0.35, 0.22, 0.3), "#c49a66", [-0.9, 0.36, -0.28], { rough: 0.95 });
  hiddenPart(group, "blanket", part(new THREE.Group(), box(0.58, 0.5, 0.48), "#6f7f6a", [-0.93, 0.26, -0.3], { rough: 1, opacity: 0.92 }).parent);
  const watcher = new THREE.Group();
  addPerson(watcher, [0.8, 0.0, 0.55], "#ff7a59");
  hiddenPart(group, "watch", watcher);
  part(group, cyl(0.06, 0.06, 0.38, 14), "#d94646", [1.25, 0.2, 0.3], { rough: 0.4 }); // extinguisher
  part(group, cyl(0.02, 0.02, 0.1, 8), "#1b2530", [1.25, 0.44, 0.3]);
  group.userData.tick = (time) => {
    const flicker = Math.sin(time * 40) * Math.sin(time * 17);
    glow.intensity = flicker > 0.15 ? 4.5 : 1.2;
    tip.material.color.set(flicker > 0.15 ? "#fff2c8" : "#ffb347");
    sparks.forEach((spark, index) => {
      const phase = (time * 1.6 + index / sparks.length) % 1;
      const angle = index * 2.4;
      spark.position.set(-0.12 + Math.cos(angle) * phase * 0.28, 0.95 + Math.sin(phase * 3.1) * 0.22 - phase * 0.1, Math.sin(angle) * phase * 0.28);
      spark.visible = phase < 0.85;
    });
  };
  return group;
}

export function createElectricalStation() {
  const group = new THREE.Group();
  part(group, box(0.96, 1.55, 0.36), "#7f8d9a", [0, 0.78, -0.1], { metal: 0.4, rough: 0.5 });
  part(group, box(0.84, 1.42, 0.03), "#1c2631", [0, 0.78, 0.09]);
  const door = new THREE.Group();
  door.position.set(-0.48, 0.78, 0.08);
  door.rotation.y = 1.2;
  const panel = part(door, box(0.96, 1.55, 0.04), "#95a3b0", [0.48, 0, 0], { metal: 0.4 });
  panel.material.side = THREE.DoubleSide;
  group.add(door);
  const leds = [];
  const breakerOn = new THREE.Group();
  const breakerOff = new THREE.Group();
  for (let row = 0; row < 6; row += 1) {
    for (let col = 0; col < 2; col += 1) {
      const target = row === 2 && col === 0 ? null : group;
      const x = -0.2 + col * 0.4;
      const y = 1.4 - row * 0.2;
      if (target) {
        part(group, box(0.18, 0.09, 0.05), "#d8edf3", [x, y, 0.12], { rough: 0.5 });
        part(group, ball(0.016), "#3ee08f", [x + 0.1, y, 0.15], { basic: true });
      } else {
        part(breakerOn, box(0.18, 0.09, 0.05), "#d8edf3", [x, y, 0.12], { rough: 0.5 });
        part(breakerOn, ball(0.016), "#3ee08f", [x + 0.1, y, 0.15], { basic: true });
        part(breakerOff, box(0.18, 0.09, 0.05), "#4a525c", [x, y, 0.12], { rough: 0.5 });
        leds.push(part(breakerOff, ball(0.016), "#ff5f5f", [x + 0.1, y, 0.15], { basic: true }));
      }
    }
  }
  group.add(breakerOn);
  group.userData.reveal = { ...(group.userData.reveal ?? {}), breakerOn };
  hiddenPart(group, "breakerOff", breakerOff);
  const lock = new THREE.Group();
  part(lock, box(0.2, 0.06, 0.06), "#ff5f5f", [-0.2, 0.9, 0.15], { rough: 0.4 }); // lockout hasp
  part(lock, box(0.09, 0.11, 0.05), "#d94646", [-0.2, 0.82, 0.19], { metal: 0.5 }); // padlock
  part(lock, new THREE.TorusGeometry(0.04, 0.012, 8, 16, Math.PI), "#c5d4de", [-0.2, 0.89, 0.19], { metal: 0.6 });
  hiddenPart(group, "lock", lock);
  hiddenPart(group, "tag", part(new THREE.Group(), box(0.1, 0.14, 0.01), "#ffd23f", [-0.1, 0.78, 0.2], { basic: true }).parent);
  const leads = new THREE.Group();
  link(leads, [0.8, 0.2, 0.45], [-0.2, 1.0, 0.18], "#ff5f5f", 0.008);
  link(leads, [0.78, 0.2, 0.45], [-0.12, 1.0, 0.18], "#1b2530", 0.008);
  hiddenPart(group, "leads", leads);
  const coworker = new THREE.Group();
  addPerson(coworker, [0.7, 0.0, 0.8], "#7ab6ff");
  hiddenPart(group, "coworker", coworker);
  part(group, box(1.1, 0.015, 0.7), "#1a1a1a", [0, 0.01, 0.55], { rough: 0.95 }); // insulating mat
  part(group, box(1.1, 0.016, 0.03), "#ffd23f", [0, 0.012, 0.9], { basic: true });
  part(group, box(0.17, 0.27, 0.05), "#f2c94c", [0.8, 0.14, 0.45], { rot: [-0.2, 0, 0], rough: 0.5 }); // tester
  part(group, box(0.12, 0.08, 0.01), "#3ee08f", [0.8, 0.2, 0.47], { basic: true, rot: [-0.2, 0, 0] });
  part(group, cyl(0.012, 0.012, 0.25, 8), "#ff5f5f", [0.95, 0.09, 0.45], { rot: [0, 0, 1.2] });
  group.userData.setState = (state) => { if (state === "coworker") coworker.visible = true; };
  group.userData.tick = (time) => {
    for (const led of leds) led.visible = Math.sin(time * 4) > -0.2;
  };
  return group;
}
