import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";

const mode = document.body.dataset.mode === "ar" ? "ar" : "vr";
const params = new URLSearchParams(location.search);
const nameInput = document.querySelector("#name");
const roomInput = document.querySelector("#room");
const joinButton = document.querySelector("#join");
const enterXrButton = document.querySelector("#enter-xr");
const status = document.querySelector("#status");
const workspace = document.querySelector("#workspace");
const roster = document.querySelector("#roster");
const deleteButton = document.querySelector("#delete-selected");
const alignmentStatus = document.querySelector("#alignment");
const xrHint = document.querySelector("#xr-hint");
const roomPattern = /^[a-z0-9][a-z0-9-]{0,23}$/i;

if (params.has("room")) roomInput.value = params.get("room");
if (!("xr" in navigator)) {
  enterXrButton.disabled = true;
  enterXrButton.textContent = "WebXR unavailable";
  xrHint.hidden = false;
  xrHint.textContent = "WebXR is unavailable in this browser. You can still build together in desktop mode.";
}

const scene = new THREE.Scene();
scene.background = mode === "ar" ? null : new THREE.Color("#101723");
scene.fog = new THREE.Fog("#101723", 10, 28);
const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 80);
camera.position.set(4.4, 4.1, 6.8);
camera.lookAt(0, 0.5, 0);

const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: mode === "ar" });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.xr.enabled = true;
if (mode === "ar") renderer.setClearColor(0x000000, 0);
renderer.domElement.className = "scene";
renderer.domElement.setAttribute("aria-label", "Shared XR waterworks scene");
document.body.appendChild(renderer.domElement);

const orbit = new OrbitControls(camera, renderer.domElement);
orbit.enableDamping = true;
orbit.target.set(0, 0.55, 0);

scene.add(new THREE.HemisphereLight(0xc9e7ff, 0x37484e, 2.3));
const keyLight = new THREE.DirectionalLight(0xffedcf, 3.2);
keyLight.position.set(-4, 7, 5);
scene.add(keyLight);

const board = new THREE.Group();
scene.add(board);

const workSurface = new THREE.Mesh(
  new THREE.CylinderGeometry(3.4, 3.4, 0.12, 72),
  new THREE.MeshStandardMaterial({ color: "#243949", roughness: 0.82 }),
);
workSurface.position.y = -0.08;
board.add(workSurface);

const grid = new THREE.GridHelper(6.7, 28, "#73e7d4", "#446074");
grid.position.y = 0.0;
board.add(grid);

const boardGuide = new THREE.Mesh(
  new THREE.TorusGeometry(2.8, 0.018, 6, 96),
  new THREE.MeshBasicMaterial({ color: "#73e7d4", transparent: true, opacity: 0.68 }),
);
boardGuide.rotation.x = Math.PI / 2;
boardGuide.position.y = 0.015;
board.add(boardGuide);

const objects = new Map();
const peers = new Map();
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
const up = new THREE.Vector3(0, 1, 0);
const cursorFloor = new THREE.Plane(up, -0.08);
const tempVector = new THREE.Vector3();
const tempVector2 = new THREE.Vector3();
const partNames = {
  tank: "Storage tank",
  valve: "Gate valve",
  pipe: "Pipe",
  pump: "Centrifugal pump",
};
const partColors = {
  tank: 0x91b5ff,
  valve: 0xffb36b,
  pipe: 0x73e7d4,
  pump: 0xb8a5ff,
};

let socket = null;
let selfId = null;
let selectedPart = "valve";
let selectedObjectId = null;
let drag = null;
let pointerStart = null;
let activeXrController = null;
let activeSession = null;
let hitTestSource = null;
let viewerSpace = null;
let localSpace = null;
let hitWorldPoint = null;
let arOrigin = null;
let arDirection = null;
let arCalibrationStep = 0;
let lastCursorSent = 0;
let objectSerial = 0;

const reticle = new THREE.Mesh(
  new THREE.RingGeometry(0.09, 0.13, 32),
  new THREE.MeshBasicMaterial({ color: "#73e7d4", side: THREE.DoubleSide }),
);
reticle.rotation.x = -Math.PI / 2;
reticle.visible = false;
scene.add(reticle);

function makeLabel(text, color) {
  const canvas = document.createElement("canvas");
  canvas.width = 512;
  canvas.height = 128;
  const context = canvas.getContext("2d");
  context.fillStyle = "#101723dd";
  context.beginPath();
  context.roundRect(2, 2, 508, 124, 26);
  context.fill();
  context.fillStyle = color;
  context.font = "bold 42px system-ui, sans-serif";
  context.textAlign = "center";
  context.textBaseline = "middle";
  context.fillText(text.slice(0, 18), 256, 65);
  const texture = new THREE.CanvasTexture(canvas);
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: texture, transparent: true }));
  sprite.scale.set(0.85, 0.22, 1);
  return sprite;
}

function makePeer(peer) {
  const group = new THREE.Group();
  const color = peer.mode === "ar" ? "#ffb36b" : peer.mode === "vr" ? "#73e7d4" : "#b8a5ff";
  const head = new THREE.Mesh(
    new THREE.SphereGeometry(0.12, 18, 14),
    new THREE.MeshStandardMaterial({ color, emissive: color, emissiveIntensity: 0.18 }),
  );
  head.position.y = 1.65;
  group.add(head);
  const torso = new THREE.Mesh(
    new THREE.CylinderGeometry(0.08, 0.11, 0.42, 12),
    new THREE.MeshStandardMaterial({ color, transparent: true, opacity: 0.75 }),
  );
  torso.position.y = 1.36;
  group.add(torso);
  const label = makeLabel(`${peer.name} · ${peer.mode.toUpperCase()}`, color);
  label.position.y = 1.92;
  group.add(label);
  board.add(group);
  return group;
}

function makePart(part, valveOpen = false) {
  const group = new THREE.Group();
  const base = partColors[part];
  const material = new THREE.MeshStandardMaterial({
    color: base,
    roughness: 0.34,
    metalness: 0.24,
  });
  const trim = new THREE.MeshStandardMaterial({
    color: "#d8edf3",
    metalness: 0.72,
    roughness: 0.24,
  });
  const dark = new THREE.MeshStandardMaterial({
    color: valveOpen ? "#73e7d4" : "#a74345",
    emissive: valveOpen ? "#174a3f" : "#431619",
    roughness: 0.4,
  });

  if (part === "tank") {
    const body = new THREE.Mesh(new THREE.CylinderGeometry(0.42, 0.42, 0.8, 24), material);
    body.position.y = 0.48;
    group.add(body);
    const cap = new THREE.Mesh(new THREE.CylinderGeometry(0.45, 0.45, 0.08, 24), trim);
    cap.position.y = 0.9;
    group.add(cap);
  } else if (part === "valve") {
    const body = new THREE.Mesh(new THREE.CylinderGeometry(0.29, 0.29, 0.36, 16), trim);
    body.position.y = 0.24;
    body.rotation.z = Math.PI / 2;
    group.add(body);
    const hub = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.1, 0.36, 12), material);
    hub.position.y = 0.48;
    group.add(hub);
    const wheel = new THREE.Mesh(new THREE.TorusGeometry(0.24, 0.035, 8, 24), dark);
    wheel.position.y = 0.68;
    group.add(wheel);
    for (let index = 0; index < 4; index += 1) {
      const spoke = new THREE.Mesh(new THREE.BoxGeometry(0.035, 0.46, 0.035), dark);
      spoke.position.y = 0.68;
      spoke.rotation.z = (Math.PI / 4) + index * (Math.PI / 2);
      group.add(spoke);
    }
    group.userData.wheel = wheel;
  } else if (part === "pipe") {
    const tube = new THREE.Mesh(new THREE.CylinderGeometry(0.115, 0.115, 1.45, 18), material);
    tube.position.y = 0.2;
    tube.rotation.z = Math.PI / 2;
    group.add(tube);
    for (const x of [-0.62, 0.62]) {
      const flange = new THREE.Mesh(new THREE.CylinderGeometry(0.2, 0.2, 0.07, 20), trim);
      flange.position.set(x, 0.2, 0);
      flange.rotation.z = Math.PI / 2;
      group.add(flange);
    }
  } else if (part === "pump") {
    const body = new THREE.Mesh(new THREE.SphereGeometry(0.42, 24, 18), material);
    body.scale.set(1.05, 0.74, 0.9);
    body.position.y = 0.43;
    group.add(body);
    const motor = new THREE.Mesh(new THREE.CylinderGeometry(0.21, 0.21, 0.48, 16), trim);
    motor.position.set(-0.4, 0.52, 0);
    motor.rotation.z = Math.PI / 2;
    group.add(motor);
    const outlet = new THREE.Mesh(new THREE.CylinderGeometry(0.11, 0.14, 0.3, 14), trim);
    outlet.position.set(0.13, 0.78, 0);
    group.add(outlet);
  }

  group.userData.part = part;
  group.traverse((child) => {
    if (child.isMesh) {
      child.castShadow = true;
      child.receiveShadow = true;
      child.userData.sharedObject = group;
    }
  });
  return group;
}

function setObjectPosition(object, position, rotation) {
  object.group.position.fromArray(position);
  object.group.rotation.set(...rotation);
  object.data.position = position;
  object.data.rotation = rotation;
}

function addSharedObject(data) {
  if (objects.has(data.id)) return;
  const group = makePart(data.part, data.valveOpen);
  group.userData.sharedId = data.id;
  group.userData.wheel?.rotation.set(0, 0, data.valveOpen ? Math.PI / 5 : 0);
  const object = { id: data.id, group, data, heldBy: data.heldBy ?? null };
  setObjectPosition(object, data.position, data.rotation);
  objects.set(data.id, object);
  board.add(group);
}

function removeSharedObject(id) {
  const object = objects.get(id);
  if (!object) return;
  board.remove(object.group);
  object.group.traverse((child) => {
    if (!child.isMesh) return;
    child.geometry.dispose();
    if (Array.isArray(child.material)) child.material.forEach((item) => item.dispose());
    else child.material.dispose();
  });
  objects.delete(id);
  if (selectedObjectId === id) selectObject(null);
  if (drag?.id === id) drag = null;
}

function replaceSharedObject(data) {
  if (objects.has(data.id)) removeSharedObject(data.id);
  addSharedObject(data);
}

function selectObject(id) {
  selectedObjectId = id;
  deleteButton.disabled = !id || !isConnected();
}

function isConnected() {
  return socket?.readyState === WebSocket.OPEN && selfId !== null;
}

function send(message) {
  if (!isConnected()) {
    status.textContent = "Join a room before changing the shared scene.";
    return false;
  }
  socket.send(JSON.stringify(message));
  return true;
}

function updateRoster() {
  const participants = [...peers.values()]
    .map((peer) => `${peer.name} (${peer.mode.toUpperCase()})`);
  participants.unshift(`${nameInput.value.trim() || "You"} (${mode.toUpperCase()})`);
  roster.textContent = `${participants.length} in room: ${participants.join(" · ")}`;
}

function updatePeer(peer) {
  let group = peers.get(peer.id)?.group;
  if (!group) {
    group = makePeer(peer);
    peers.set(peer.id, { ...peer, group });
  } else {
    peers.set(peer.id, { ...peers.get(peer.id), ...peer, group });
  }
  if (Array.isArray(peer.cursor)) group.position.set(peer.cursor[0], 0, peer.cursor[1]);
  updateRoster();
}

function removePeer(id) {
  const peer = peers.get(id);
  if (!peer) return;
  board.remove(peer.group);
  peer.group.traverse((child) => {
    if (child.isSprite) {
      child.material.map?.dispose();
      child.material.dispose();
    } else if (child.isMesh) {
      child.geometry.dispose();
      child.material.dispose();
    }
  });
  peers.delete(id);
  for (const object of objects.values()) {
    if (object.heldBy === id) object.heldBy = null;
  }
  updateRoster();
}

function updateStatus(message) {
  status.textContent = message;
}

function receive(message) {
  switch (message.type) {
    case "welcome":
      selfId = message.selfId;
      nameInput.disabled = true;
      roomInput.disabled = true;
      joinButton.disabled = true;
      enterXrButton.disabled = !("xr" in navigator);
      workspace.hidden = false;
      for (const object of message.objects) addSharedObject(object);
      for (const peer of message.peers) updatePeer(peer);
      updateRoster();
      updateStatus(`Connected to room "${message.room}". Add a part to start building.`);
      break;
    case "peer-join":
      updatePeer(message.peer);
      updateStatus(`${message.peer.name} joined the room.`);
      break;
    case "peer-cursor": {
      const peer = peers.get(message.id);
      if (peer) {
        peer.cursor = message.position;
        peer.group.position.set(message.position[0], 0, message.position[1]);
      }
      break;
    }
    case "peer-leave":
      removePeer(message.id);
      updateStatus("A participant left the room.");
      break;
    case "object-added":
      addSharedObject(message.object);
      updateStatus(`${partNames[message.object.part]} added to the shared work surface.`);
      break;
    case "object-grabbed": {
      const object = objects.get(message.id);
      if (object) object.heldBy = message.holder;
      break;
    }
    case "object-moved": {
      const object = objects.get(message.id);
      if (object && object.heldBy !== selfId) {
        setObjectPosition(object, message.position, message.rotation);
      }
      break;
    }
    case "object-released": {
      const object = objects.get(message.id);
      if (object) {
        setObjectPosition(object, message.position, message.rotation);
        object.heldBy = null;
      }
      if (drag?.id === message.id) drag = null;
      break;
    }
    case "grab-denied": {
      const object = objects.get(message.id);
      if (object) object.heldBy = message.holder;
      if (drag?.id === message.id) drag = null;
      updateStatus("That part is being held by another participant.");
      break;
    }
    case "object-removed":
      removeSharedObject(message.id);
      updateStatus("Part removed from the room.");
      break;
    case "valve-state": {
      const object = objects.get(message.id);
      const wasSelected = selectedObjectId === message.id;
      if (object) replaceSharedObject({ ...object.data, valveOpen: message.open });
      if (wasSelected) selectObject(message.id);
      updateStatus(`Valve ${message.open ? "opened" : "closed"} for everyone.`);
      break;
    }
    case "room-limit":
      updateStatus(message.message);
      break;
    default:
      break;
  }
}

function joinRoom() {
  if (socket && socket.readyState !== WebSocket.CLOSED) return;
  const room = roomInput.value.trim().toLowerCase();
  if (!roomPattern.test(room)) {
    updateStatus("Room names must be 1-24 letters, numbers, or hyphens.");
    roomInput.focus();
    return;
  }
  socket = new WebSocket(`${location.protocol === "https:" ? "wss:" : "ws:"}//${location.host}/ws`);
  joinButton.disabled = true;
  updateStatus("Connecting to the shared room...");
  socket.addEventListener("open", () => {
    socket.send(JSON.stringify({
      type: "join",
      room,
      name: nameInput.value.trim() || "Guest",
      mode,
    }));
  });
  socket.addEventListener("message", (event) => {
    try {
      receive(JSON.parse(event.data));
    } catch (error) {
      console.error("Could not read a shared-room update.", error);
      updateStatus("Received an invalid room update. Check the relay log.");
    }
  });
  socket.addEventListener("error", () => {
    updateStatus("Could not connect to the room relay. Check the server and secure connection.");
  });
  socket.addEventListener("close", (event) => {
    selfId = null;
    nameInput.disabled = false;
    roomInput.disabled = false;
    joinButton.disabled = false;
    workspace.hidden = true;
    for (const id of [...objects.keys()]) removeSharedObject(id);
    for (const id of [...peers.keys()]) removePeer(id);
    updateStatus(event.reason || "Disconnected from the room. Rejoin to continue.");
  });
}

function spawnPart(part, position = [0, 0.08, 0]) {
  selectedPart = part;
  document.querySelectorAll("[data-part]").forEach((button) => {
    button.setAttribute("aria-pressed", String(button.dataset.part === part));
  });
  const index = objectSerial++;
  const offset = index % 4;
  const spread = Math.floor(index / 4);
  const spawnPosition = position[0] === 0 && position[2] === 0
    ? [position[0] + (offset - 1.5) * 0.65, position[1], position[2] + spread * 0.65]
    : position;
  return send({
    type: "place",
    part,
    position: spawnPosition,
    rotation: [0, 0, 0],
  });
}

function spawnAt(part, worldPoint) {
  const local = board.worldToLocal(worldPoint.clone());
  local.y = 0.08;
  spawnPart(part, [local.x, local.y, local.z]);
}

function meshesForRaycast() {
  return [...objects.values()].flatMap((object) => object.group.children);
}

function findObjectHit() {
  const hits = raycaster.intersectObjects(meshesForRaycast(), true);
  for (const hit of hits) {
    const group = hit.object.userData.sharedObject ?? hit.object.parent;
    if (group?.userData.sharedId) return { object: objects.get(group.userData.sharedId), point: hit.point };
  }
  return null;
}

function setCursor(event, sendPresence = true) {
  const bounds = renderer.domElement.getBoundingClientRect();
  pointer.set(
    ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
    -((event.clientY - bounds.top) / bounds.height) * 2 + 1,
  );
  raycaster.setFromCamera(pointer, camera);
  const point = raycaster.ray.intersectPlane(getBoardPlane(), tempVector);
  if (!point) return null;
  if (sendPresence && Date.now() - lastCursorSent >= 55 && isConnected()) {
    const local = board.worldToLocal(point.clone());
    send({ type: "cursor", position: [local.x, local.z] });
    lastCursorSent = Date.now();
  }
  return point.clone();
}

function getBoardPlane() {
  const normal = up.clone().applyQuaternion(board.quaternion);
  const point = board.localToWorld(new THREE.Vector3(0, 0.08, 0));
  return new THREE.Plane().setFromNormalAndCoplanarPoint(normal, point);
}

function beginGrab(object, worldPoint) {
  if (!object || !send({ type: "grab", id: object.id })) return;
  object.heldBy = selfId;
  selectObject(object.id);
  const localPoint = board.worldToLocal(worldPoint.clone());
  drag = {
    id: object.id,
    offset: object.group.position.clone().sub(localPoint),
    owner: "self",
  };
}

function moveGrab(worldPoint) {
  if (!drag || !isConnected()) return;
  const object = objects.get(drag.id);
  if (!object) return;
  const local = board.worldToLocal(worldPoint.clone()).add(drag.offset);
  local.y = 0.08;
  const position = [local.x, local.y, local.z];
  const rotation = [object.group.rotation.x, object.group.rotation.y, object.group.rotation.z];
  setObjectPosition(object, position, rotation);
  send({ type: "move", id: object.id, position, rotation });
}

function releaseGrab() {
  if (!drag) return;
  const object = objects.get(drag.id);
  if (object && object.heldBy === selfId) {
    send({
      type: "release",
      id: object.id,
      position: object.group.position.toArray(),
      rotation: [object.group.rotation.x, object.group.rotation.y, object.group.rotation.z],
    });
  }
  drag = null;
}

function onPointerDown(event) {
  if (event.button !== 0 || renderer.xr.isPresenting) return;
  const bounds = renderer.domElement.getBoundingClientRect();
  pointer.set(
    ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
    -((event.clientY - bounds.top) / bounds.height) * 2 + 1,
  );
  raycaster.setFromCamera(pointer, camera);
  const hit = findObjectHit();
  if (hit) {
    event.preventDefault();
    event.stopPropagation();
    orbit.enabled = false;
    renderer.domElement.setPointerCapture(event.pointerId);
    pointerStart = { x: event.clientX, y: event.clientY, id: event.pointerId };
    beginGrab(hit.object, hit.point);
    return;
  }
  pointerStart = { x: event.clientX, y: event.clientY, id: event.pointerId };
}

function onPointerMove(event) {
  const point = setCursor(event);
  if (drag?.owner === "self" && point) moveGrab(point);
}

function onPointerUp(event) {
  if (!pointerStart || pointerStart.id !== event.pointerId) return;
  const distance = Math.hypot(event.clientX - pointerStart.x, event.clientY - pointerStart.y);
  if (drag?.owner === "self") releaseGrab();
  else if (distance < 6) {
    const point = setCursor(event, false);
    if (point) {
      const bounds = renderer.domElement.getBoundingClientRect();
      pointer.set(
        ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
        -((event.clientY - bounds.top) / bounds.height) * 2 + 1,
      );
      raycaster.setFromCamera(pointer, camera);
      const hit = findObjectHit();
      if (hit) selectObject(hit.object.id);
      else if (isConnected()) spawnAt(selectedPart, point);
    }
  }
  pointerStart = null;
  orbit.enabled = true;
  if (renderer.domElement.hasPointerCapture(event.pointerId)) {
    renderer.domElement.releasePointerCapture(event.pointerId);
  }
}

renderer.domElement.addEventListener("pointerdown", onPointerDown, true);
renderer.domElement.addEventListener("pointermove", onPointerMove);
renderer.domElement.addEventListener("pointerup", onPointerUp);
renderer.domElement.addEventListener("pointercancel", () => {
  releaseGrab();
  pointerStart = null;
  orbit.enabled = true;
});

document.querySelectorAll("[data-part]").forEach((button) => {
  button.addEventListener("click", () => {
    selectedPart = button.dataset.part;
    document.querySelectorAll("[data-part]").forEach((item) => {
      item.setAttribute("aria-pressed", String(item === button));
    });
    spawnPart(selectedPart);
  });
});

joinButton.addEventListener("click", joinRoom);
roomInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter") joinRoom();
});
nameInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter") joinRoom();
});

deleteButton.addEventListener("click", () => {
  if (selectedObjectId) send({ type: "delete", id: selectedObjectId });
});

document.addEventListener("keydown", (event) => {
  if (event.target instanceof HTMLInputElement) return;
  if (event.key.toLowerCase() === "v") {
    const object = objects.get(selectedObjectId);
    if (object?.data.part === "valve") send({ type: "valve", id: object.id, open: !object.data.valveOpen });
  }
  if (event.key === "Delete" && selectedObjectId) send({ type: "delete", id: selectedObjectId });
  if (event.key === "Escape") selectObject(null);
});

for (let index = 0; index < 2; index += 1) {
  const controller = renderer.xr.getController(index);
  const beam = new THREE.Line(
    new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0), new THREE.Vector3(0, 0, -1)]),
    new THREE.LineBasicMaterial({ color: index === 0 ? "#73e7d4" : "#ffb36b", transparent: true, opacity: 0.7 }),
  );
  controller.add(beam);
  scene.add(controller);
  controller.addEventListener("selectstart", () => onXrSelectStart(controller));
  controller.addEventListener("selectend", () => {
    if (activeXrController === controller) releaseGrab();
    activeXrController = null;
  });
  controller.addEventListener("connected", (event) => {
    controller.userData.targetRayMode = event.data?.targetRayMode;
  });
}

function onXrSelectStart(controller) {
  if (mode === "ar" && arCalibrationStep < 2) {
    if (hitWorldPoint) calibrateArOrigin(hitWorldPoint.clone());
    return;
  }
  raycaster.setFromXRController(controller);
  const hit = findObjectHit();
  if (hit) {
    beginGrab(hit.object, hit.point);
    activeXrController = controller;
    return;
  }
  if (mode === "ar" && hitWorldPoint) {
    spawnAt(selectedPart, hitWorldPoint);
    return;
  }
  const point = raycaster.ray.intersectPlane(getBoardPlane(), tempVector);
  if (point && point.distanceTo(camera.getWorldPosition(tempVector2)) < 10) {
    spawnAt(selectedPart, point);
  }
}

function updateControllerGrab() {
  if (!activeXrController || !drag) return;
  if (mode === "ar" && hitWorldPoint) {
    moveGrab(hitWorldPoint);
    return;
  }
  raycaster.setFromXRController(activeXrController);
  const point = raycaster.ray.intersectPlane(getBoardPlane(), tempVector);
  if (point) moveGrab(point);
}

function resetArAlignment() {
  arCalibrationStep = 0;
  arOrigin = null;
  arDirection = null;
  board.position.set(0, 0, 0);
  board.quaternion.identity();
  reticle.visible = false;
  alignmentStatus.textContent = "Tap the shared origin marker, then a second marker along +Z.";
}

function calibrateArOrigin(point) {
  if (arCalibrationStep === 0) {
    arOrigin = point;
    arCalibrationStep = 1;
    alignmentStatus.textContent = "Origin set. Aim at the shared +Z direction marker and tap again.";
    xrHint.textContent = "Origin set. Tap your shared +Z direction marker.";
    return;
  }
  const direction = point.clone().sub(arOrigin);
  direction.y = 0;
  if (direction.length() < 0.25) {
    alignmentStatus.textContent = "Choose a +Z direction point at least 25 cm from the origin.";
    return;
  }
  direction.normalize();
  const xAxis = new THREE.Vector3().crossVectors(up, direction).normalize();
  const basis = new THREE.Matrix4().makeBasis(xAxis, up, direction);
  board.position.copy(arOrigin);
  board.quaternion.setFromRotationMatrix(basis);
  arDirection = direction;
  arCalibrationStep = 2;
  alignmentStatus.textContent = "Aligned. Shared board coordinates now map to this local AR space.";
  xrHint.textContent = "Board aligned. Tap a shared part to grab it, or tap the work surface to add one.";
  reticle.material.color.set("#73e7d4");
}

async function enterXr() {
  if (!("xr" in navigator)) return;
  enterXrButton.disabled = true;
  try {
    if (mode === "vr") {
      const supported = await navigator.xr.isSessionSupported("immersive-vr");
      if (!supported) throw new Error("This browser or device cannot start an immersive VR session.");
      renderer.xr.setReferenceSpaceType("local-floor");
      const session = await navigator.xr.requestSession("immersive-vr", {
        optionalFeatures: ["local-floor", "bounded-floor", "hand-tracking"],
      });
      await renderer.xr.setSession(session);
      updateStatus("In VR. Point a controller, then squeeze to grab or add a shared part.");
    } else {
      const supported = await navigator.xr.isSessionSupported("immersive-ar");
      if (!supported) throw new Error("This browser or device cannot start an immersive AR session.");
      resetArAlignment();
      renderer.xr.setReferenceSpaceType("local");
      const session = await navigator.xr.requestSession("immersive-ar", {
        requiredFeatures: ["hit-test"],
        optionalFeatures: ["local-floor", "dom-overlay"],
        domOverlay: { root: document.body },
      });
      await renderer.xr.setSession(session);
      await startArHitTesting(session);
      document.body.classList.add("in-xr");
      xrHint.hidden = false;
      xrHint.textContent = "Tap the shared origin marker. Then tap the agreed +Z direction marker.";
      updateStatus("AR active. Align the same two physical markers used by your collaborators.");
    }
    activeSession = renderer.xr.getSession();
    activeSession?.addEventListener("end", onXrSessionEnd, { once: true });
  } catch (error) {
    console.error(`Could not start immersive ${mode.toUpperCase()}.`, error);
    updateStatus(error instanceof Error ? error.message : "The immersive session could not be started.");
  } finally {
    enterXrButton.disabled = !isConnected();
  }
}

async function startArHitTesting(session) {
  viewerSpace = await session.requestReferenceSpace("viewer");
  localSpace = await session.requestReferenceSpace("local");
  hitTestSource = await session.requestHitTestSource({ space: viewerSpace });
  if (!hitTestSource) throw new Error("This browser could not create a hit-test source.");
}

function updateArHitTest(frame) {
  if (!hitTestSource || !localSpace) return;
  const results = frame.getHitTestResults(hitTestSource);
  if (results.length === 0) {
    reticle.visible = false;
    hitWorldPoint = null;
    return;
  }
  const pose = results[0].getPose(localSpace);
  if (!pose) return;
  reticle.matrix.fromArray(pose.transform.matrix);
  reticle.matrix.decompose(reticle.position, reticle.quaternion, reticle.scale);
  reticle.visible = true;
  hitWorldPoint = reticle.position.clone();
}

function onXrSessionEnd() {
  activeSession = null;
  hitTestSource?.cancel();
  hitTestSource = null;
  viewerSpace = null;
  localSpace = null;
  hitWorldPoint = null;
  activeXrController = null;
  drag = null;
  document.body.classList.remove("in-xr");
  xrHint.hidden = true;
  enterXrButton.disabled = !isConnected();
  if (mode === "ar") resetArAlignment();
  updateStatus(`${mode.toUpperCase()} session ended. The shared room remains connected.`);
}

enterXrButton.addEventListener("click", enterXr);

function onMessageFrame(_time, frame) {
  if (mode === "ar" && renderer.xr.isPresenting && frame) updateArHitTest(frame);
  if (renderer.xr.isPresenting) updateControllerGrab();
  orbit.update();
  renderer.render(scene, camera);
}

renderer.setAnimationLoop(onMessageFrame);

window.addEventListener("resize", () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

window.addEventListener("beforeunload", () => {
  releaseGrab();
  socket?.close(1000, "Leaving the room.");
  renderer.setAnimationLoop(null);
  renderer.dispose();
});

enterXrButton.disabled = !isConnected();
