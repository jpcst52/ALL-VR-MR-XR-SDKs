import * as THREE from "three";
import { drawSignLines } from "../brand-kit/panels.js";

// Procedural "dollhouse" rooms for the indoor work areas: a floor, a back wall and two side walls with textures drawn
// on a canvas (no files, no licences), plus a few wall fittings. Without a room, the indoor stations float on a navy
// void. The walls face inward only, so they vanish when the camera is orbited behind them.
// Coordinates are station-local metres: the platform top is y = 0, the room floor is at ROOM_FLOOR_Y (world y = 0).

export const ROOM_FLOOR_Y = -0.2;
const BACK_Z = -3.0;
const SIDE_X = 5.6;
const WALL_HEIGHT = 9;

// Which station gets which room. Outdoor stations (height, battery, facade) and the schematic circulation
// station are not listed: they keep the sky or the plain stage.
export const ROOM_BY_STATION = {
  control: "control",
  welding: "workshop",
  electrical: "workshop",
  lighters: "lab",
  hydrotest: "workshop",
  spool: "workshop",
  bench: "lab",
};

export const GROUND_BY_STATION = {
  height: { color: "#7b7d78", rough: 0.95 }, // gravel yard
  battery: { color: "#6d7075", rough: 0.95 }, // process-unit slab
  facade: { color: "#8b8e92", rough: 0.9 }, // paved plaza
};
export const DEFAULT_GROUND = "#1b2c4f";

function rng(seed) {
  let s = seed;
  return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296);
}

function canvas(w, h) {
  const art = document.createElement("canvas");
  art.width = w;
  art.height = h;
  return art;
}

function speckle(ctx, w, h, amount, seed = 7) {
  const rand = rng(seed);
  const image = ctx.getImageData(0, 0, w, h);
  for (let i = 0; i < w * h; i += 1) {
    const v = (rand() - 0.5) * amount;
    image.data[i * 4] = Math.max(0, Math.min(255, image.data[i * 4] + v));
    image.data[i * 4 + 1] = Math.max(0, Math.min(255, image.data[i * 4 + 1] + v));
    image.data[i * 4 + 2] = Math.max(0, Math.min(255, image.data[i * 4 + 2] + v));
  }
  ctx.putImageData(image, 0, 0);
}

function toTexture(art, repeatX, repeatY) {
  const texture = new THREE.CanvasTexture(art);
  texture.colorSpace = THREE.SRGBColorSpace;
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  texture.repeat.set(repeatX, repeatY);
  texture.anisotropy = 4;
  return texture;
}

// Wall profile: the canvas is one strip of wall, 1.5 m wide and the full wall height, drawn from the floor (bottom) up.
function wallArt(kind) {
  const w = 256;
  const h = 1440; // 9 m: 160 px per metre
  const art = canvas(w, h);
  const ctx = art.getContext("2d");
  const y = (metres) => h - metres * 160;
  const fillBand = (from, to, color) => { ctx.fillStyle = color; ctx.fillRect(0, y(to), w, y(from) - y(to)); };
  if (kind === "workshop" || kind === "warehouse") {
    fillBand(0, 1.15, "#4b5663"); // painted lower panel
    fillBand(1.15, 1.27, "#e8b82a"); // yellow safety stripe
    fillBand(1.27, WALL_HEIGHT, "#a9b3bd"); // corrugated sheet above
    for (let x = 0; x < w; x += 16) {
      const grad = ctx.createLinearGradient(x, 0, x + 16, 0);
      grad.addColorStop(0, "#00000030");
      grad.addColorStop(0.5, "#ffffff22");
      grad.addColorStop(1, "#00000030");
      ctx.fillStyle = grad;
      ctx.fillRect(x, 0, 16, y(1.27));
    }
    ctx.fillStyle = "#00000040"; // sheet joint
    ctx.fillRect(0, y(3.1), w, 2);
    ctx.fillRect(0, y(5.0), w, 2);
    ctx.fillStyle = "#00000030";
    ctx.fillRect(0, 0, 3, h);
  } else if (kind === "lab") {
    fillBand(0, 1.8, "#dfe6eb"); // white tile
    ctx.strokeStyle = "#9aa6b0";
    ctx.lineWidth = 2;
    for (let metre = 0; metre <= 1.8; metre += 0.15) {
      ctx.beginPath();
      ctx.moveTo(0, y(metre));
      ctx.lineTo(w, y(metre));
      ctx.stroke();
    }
    for (let x = 0; x <= w; x += 24) {
      ctx.beginPath();
      ctx.moveTo(x, y(0));
      ctx.lineTo(x, y(1.8));
      ctx.stroke();
    }
    fillBand(1.8, 1.86, "#5f7f96"); // trim
    fillBand(1.86, WALL_HEIGHT, "#b9c8cf"); // pale painted wall
  } else if (kind === "control") {
    fillBand(0, 1.0, "#2f3d4f"); // dark wainscot
    fillBand(1.0, 1.06, "#8da2b8"); // chair rail
    fillBand(1.06, WALL_HEIGHT, "#8795a8"); // acoustic-panel grey-blue
    ctx.strokeStyle = "#00000028";
    ctx.lineWidth = 2;
    for (let metre = 1.06; metre < WALL_HEIGHT; metre += 0.6) {
      ctx.beginPath();
      ctx.moveTo(0, y(metre));
      ctx.lineTo(w, y(metre));
      ctx.stroke();
    }
    ctx.strokeRect(1, 0, w - 2, h);
  } else {
    // office and shop: painted wall with a wainscot and chair rail
    const wain = kind === "shop" ? "#7a5a3c" : "#516a8a";
    const paint = kind === "shop" ? "#e3d5bd" : "#d8dee6";
    fillBand(0, 1.0, wain);
    fillBand(1.0, 1.07, "#f2f4f7");
    fillBand(1.07, WALL_HEIGHT, paint);
    ctx.strokeStyle = "#00000020";
    ctx.lineWidth = 2;
    for (let x = 64; x < w; x += 64) {
      ctx.beginPath();
      ctx.moveTo(x, y(0));
      ctx.lineTo(x, y(1.0));
      ctx.stroke(); // wainscot panels
    }
  }
  speckle(ctx, w, h, 14, 11);
  // darken toward the top so a tall wall does not glare behind the in-world sign
  const fade = ctx.createLinearGradient(0, 0, 0, y(3));
  fade.addColorStop(0, "#00000090");
  fade.addColorStop(1, "#00000000");
  ctx.fillStyle = fade;
  ctx.fillRect(0, 0, w, y(3));
  return art;
}

function floorArt(kind) {
  const size = 256; // one 2 m tile
  const art = canvas(size, size);
  const ctx = art.getContext("2d");
  if (kind === "lab") {
    ctx.fillStyle = "#a9b3bc";
    ctx.fillRect(0, 0, size, size);
    ctx.fillStyle = "#98a3ad";
    for (let ty = 0; ty < 4; ty += 1) for (let tx = 0; tx < 4; tx += 1) if ((tx + ty) % 2 === 0) ctx.fillRect(tx * 64, ty * 64, 64, 64);
    speckle(ctx, size, size, 18, 3);
  } else if (kind === "office" || kind === "control") {
    ctx.fillStyle = kind === "control" ? "#3b4656" : "#6e7a8d";
    ctx.fillRect(0, 0, size, size);
    speckle(ctx, size, size, 38, 5);
    ctx.strokeStyle = "#00000030";
    ctx.lineWidth = 2;
    ctx.strokeRect(0, 0, size, size);
    ctx.strokeRect(0, 0, size / 2, size / 2);
    ctx.strokeRect(size / 2, size / 2, size / 2, size / 2);
  } else if (kind === "shop") {
    ctx.fillStyle = "#b9a889";
    ctx.fillRect(0, 0, size, size);
    ctx.strokeStyle = "#7a6a4f";
    ctx.lineWidth = 2;
    for (let x = 0; x < size; x += 32) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, size);
      ctx.stroke();
    }
    speckle(ctx, size, size, 20, 9);
  } else {
    // sealed concrete with expansion joints
    ctx.fillStyle = "#8c9094";
    ctx.fillRect(0, 0, size, size);
    speckle(ctx, size, size, 44, 4);
    ctx.fillStyle = "#00000028";
    ctx.fillRect(0, 0, size, 3);
    ctx.fillRect(0, 0, 3, size);
    const rand = rng(17);
    ctx.strokeStyle = "#00000022";
    ctx.lineWidth = 1;
    for (let i = 0; i < 6; i += 1) {
      ctx.beginPath();
      ctx.moveTo(rand() * size, rand() * size);
      ctx.lineTo(rand() * size, rand() * size);
      ctx.stroke();
    }
  }
  return art;
}

function signArt(text, bg, fg, w = 256, h = 96) {
  const art = canvas(w, h);
  const ctx = art.getContext("2d");
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, w, h);
  ctx.strokeStyle = fg;
  ctx.lineWidth = 6;
  ctx.strokeRect(5, 5, w - 10, h - 10);
  drawSignLines(ctx, [text], { fg, maxSize: Math.round(h * 0.5) }); // brand-kit sign text: brand sans, scaled to fit
  return art;
}

function flatPanel(art, w, h, { basic = false } = {}) {
  const texture = new THREE.CanvasTexture(art);
  texture.colorSpace = THREE.SRGBColorSpace;
  const material = basic ? new THREE.MeshBasicMaterial({ map: texture }) : new THREE.MeshStandardMaterial({ map: texture, roughness: 0.7 });
  return new THREE.Mesh(new THREE.PlaneGeometry(w, h), material);
}

function window3(group, x, y, z, rotY, w = 1.6, h = 1.2) {
  const frame = new THREE.Mesh(new THREE.BoxGeometry(w + 0.16, h + 0.16, 0.08), new THREE.MeshStandardMaterial({ color: "#e9edf2", roughness: 0.6 }));
  const art = canvas(64, 128);
  const ctx = art.getContext("2d");
  const grad = ctx.createLinearGradient(0, 0, 0, 128);
  grad.addColorStop(0, "#9fd0ff");
  grad.addColorStop(1, "#e6f3ff");
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, 64, 128);
  const glass = flatPanel(art, w, h, { basic: true });
  glass.position.z = 0.045;
  const barV = new THREE.Mesh(new THREE.BoxGeometry(0.05, h, 0.04), frame.material);
  barV.position.z = 0.06;
  const barH = new THREE.Mesh(new THREE.BoxGeometry(w, 0.05, 0.04), frame.material);
  barH.position.z = 0.06;
  const holder = new THREE.Group();
  holder.add(frame, glass, barV, barH);
  holder.position.set(x, y, z);
  holder.rotation.y = rotY;
  group.add(holder);
}

function clock(group, x, y, z, rotY = 0) {
  const art = canvas(128, 128);
  const ctx = art.getContext("2d");
  ctx.fillStyle = "#f5f6f8";
  ctx.beginPath();
  ctx.arc(64, 64, 60, 0, Math.PI * 2);
  ctx.fill();
  ctx.strokeStyle = "#1b2430";
  ctx.lineWidth = 6;
  ctx.stroke();
  ctx.lineWidth = 3;
  for (let i = 0; i < 12; i += 1) {
    const a = (i / 12) * Math.PI * 2;
    ctx.beginPath();
    ctx.moveTo(64 + Math.sin(a) * 50, 64 - Math.cos(a) * 50);
    ctx.lineTo(64 + Math.sin(a) * 56, 64 - Math.cos(a) * 56);
    ctx.stroke();
  }
  ctx.lineWidth = 5;
  ctx.beginPath(); ctx.moveTo(64, 64); ctx.lineTo(64 + 24, 64 - 12); ctx.stroke();
  ctx.lineWidth = 3;
  ctx.beginPath(); ctx.moveTo(64, 64); ctx.lineTo(64 - 8, 64 - 40); ctx.stroke();
  const face = flatPanel(art, 0.42, 0.42);
  face.position.set(x, y, z);
  face.rotation.y = rotY;
  group.add(face);
}

function noticeBoard(group, x, y, z, rotY = 0) {
  const board = new THREE.Mesh(new THREE.BoxGeometry(1.3, 0.9, 0.05), new THREE.MeshStandardMaterial({ color: "#a47a52", roughness: 0.9 }));
  const holder = new THREE.Group();
  holder.add(board);
  const colors = ["#f6f1d8", "#d9ecff", "#ffe3a8", "#e6f5e1"];
  colors.forEach((color, index) => {
    const note = new THREE.Mesh(new THREE.PlaneGeometry(0.26, 0.34), new THREE.MeshStandardMaterial({ color, roughness: 0.8 }));
    note.position.set(-0.45 + index * 0.3, index % 2 ? 0.08 : -0.06, 0.03);
    note.rotation.z = (index - 1.5) * 0.06;
    holder.add(note);
  });
  holder.position.set(x, y, z);
  holder.rotation.y = rotY;
  group.add(holder);
}

function cagedLamp(group, x, y, z) {
  const body = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.12, 0.2, 12), new THREE.MeshBasicMaterial({ color: "#fff3cf" }));
  const cage = new THREE.Mesh(new THREE.CylinderGeometry(0.15, 0.17, 0.28, 8, 1, true), new THREE.MeshStandardMaterial({ color: "#2c333b", wireframe: true }));
  const arm = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.05, 0.3), new THREE.MeshStandardMaterial({ color: "#2c333b" }));
  arm.position.set(0, 0.14, -0.15);
  const holder = new THREE.Group();
  holder.add(body, cage, arm);
  holder.position.set(x, y, z);
  group.add(holder);
}

const KINDS = new Set(["shop", "warehouse", "office", "lab", "control", "workshop"]);

// Returns a Group to add to the station. Geometry and materials are owned by the group: clearObject() disposes them.
export function createRoom(kind) {
  if (!KINDS.has(kind)) return null;
  const group = new THREE.Group();
  group.name = `room-${kind}`;
  const width = SIDE_X * 2;

  const wallTexture = toTexture(wallArt(kind), width / 1.5, 1);
  const wallMaterial = new THREE.MeshStandardMaterial({ map: wallTexture, roughness: 0.92, metalness: 0 });
  const back = new THREE.Mesh(new THREE.PlaneGeometry(width, WALL_HEIGHT), wallMaterial);
  back.position.set(0, ROOM_FLOOR_Y + WALL_HEIGHT / 2, BACK_Z);
  group.add(back);

  const sideLength = 10;
  const sideTexture = toTexture(wallArt(kind), sideLength / 1.5, 1);
  const sideMaterial = new THREE.MeshStandardMaterial({ map: sideTexture, roughness: 0.92, metalness: 0 });
  for (const side of [-1, 1]) {
    const wall = new THREE.Mesh(new THREE.PlaneGeometry(sideLength, WALL_HEIGHT), sideMaterial);
    wall.position.set(side * SIDE_X, ROOM_FLOOR_Y + WALL_HEIGHT / 2, BACK_Z + sideLength / 2);
    wall.rotation.y = -side * Math.PI / 2; // faces the room
    group.add(wall);
  }

  const floorTexture = toTexture(floorArt(kind), 14 / 2, 10.5 / 2);
  const floor = new THREE.Mesh(
    new THREE.PlaneGeometry(14, 10.5),
    new THREE.MeshStandardMaterial({ map: floorTexture, roughness: kind === "lab" ? 0.6 : 0.85, metalness: 0 }),
  );
  floor.rotation.x = -Math.PI / 2;
  floor.position.set(0, ROOM_FLOOR_Y + 0.005, BACK_Z + 5.25);
  group.add(floor);

  // Skirting at the foot of the walls.
  const skirt = new THREE.Mesh(new THREE.BoxGeometry(width, 0.12, 0.04), new THREE.MeshStandardMaterial({ color: kind === "lab" ? "#5f7f96" : "#2b3440", roughness: 0.8 }));
  skirt.position.set(0, ROOM_FLOOR_Y + 0.06, BACK_Z + 0.02);
  group.add(skirt);

  // Wall fittings, kept clear of the station (which spans about x -2.2..2.2) and of the in-world sign (y > 2.6).
  const wallZ = BACK_Z + 0.02;
  if (kind === "workshop" || kind === "warehouse") {
    const exit = flatPanel(signArt("EXIT", "#1f8f4e", "#f3fff7"), 0.7, 0.26, { basic: true });
    exit.position.set(-3.6, 2.4, wallZ);
    group.add(exit);
    cagedLamp(group, -2.9, 2.2, wallZ + 0.2);
    cagedLamp(group, 2.9, 2.2, wallZ + 0.2);
    cagedLamp(group, 4.6, 2.2, wallZ + 0.2);
    noticeBoard(group, 3.7, 1.55, wallZ + 0.03);
  } else if (kind === "lab") {
    window3(group, -3.2, 1.95, wallZ + 0.03, 0, 1.5, 1.0);
    clock(group, 3.4, 2.1, wallZ + 0.02);
    noticeBoard(group, 4.2, 1.1, wallZ + 0.03);
    const exit = flatPanel(signArt("EXIT", "#1f8f4e", "#f3fff7"), 0.7, 0.26, { basic: true });
    exit.position.set(-4.7, 2.4, wallZ);
    group.add(exit);
  } else if (kind === "control") {
    window3(group, -3.4, 1.9, wallZ + 0.03, 0, 1.8, 0.9);
    clock(group, 3.4, 2.2, wallZ + 0.02);
    noticeBoard(group, -4.7, 1.45, wallZ + 0.03);
  } else {
    // office and shop
    window3(group, -3.3, 1.8, wallZ + 0.03, 0, 1.7, 1.2);
    clock(group, 3.3, 2.2, wallZ + 0.02);
    noticeBoard(group, 4.3, 1.5, wallZ + 0.03);
    if (kind === "shop") {
      const open = flatPanel(signArt("OPEN", "#c4372c", "#fff6e8"), 0.6, 0.22, { basic: true });
      open.position.set(-1.8, 2.3, wallZ);
      group.add(open);
    }
  }
  return group;
}
