import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
// Lab v2: a fork of shared/app.js (the room protocol, controls, toolbox, glove hands and avatars behave the same) with a new look:
// PBR parts, a lit workshop or safety bay, an environment map, glow sprites and a four-light pool. See v2/README section in multi-player/README.md.
import { themes2 as themes, attachFx } from "./themes2.js";
import { PITT, tagSprite } from "../../brand-kit/panels.js";
import { buildRoom2, buildBoardSurface } from "./world2.js";
import { createFx } from "./fx.js";
import { createAvatar } from "../shared/avatars.js";
import { normalizePose, spreadAngles } from "../shared/avatar-logic.mjs";
import { createToolbox } from "../shared/toolbox.js";
import { XRHandModelFactory } from "three/addons/webxr/XRHandModelFactory.js";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";

const mode = document.body.dataset.mode === "ar" ? "ar" : "vr";
const params = new URLSearchParams(location.search);
const themeId = themes[params.get("theme")] ? params.get("theme") : document.body.dataset.theme;
const theme = themes[themeId] ?? themes.circuits;
const nameInput = document.querySelector("#name");
const roomInput = document.querySelector("#room");
const joinButton = document.querySelector("#join");
const enterXrButton = document.querySelector("#enter-xr");
const status = document.querySelector("#status");
const connection = document.querySelector(".connection");
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const workspace = document.querySelector("#workspace");
const roster = document.querySelector("#roster");
const deleteButton = document.querySelector("#delete-selected");
const alignmentStatus = document.querySelector("#alignment");
const summary = document.querySelector("#summary");
const partPalette = document.querySelector("#parts");
const toggleButton = document.querySelector("#toggle-selected");
const rotateButton = document.querySelector("#rotate-selected");
const demoButton = document.querySelector("#load-demo");
const xrHint = document.querySelector("#xr-hint");
const roomPattern = /^[a-z0-9][a-z0-9-]{0,23}$/i;

if (params.has("room")) roomInput.value = params.get("room");
// xrBlocked: this browser cannot start the page's immersive session, so the Enter button stays off and the desktop view is used.
let xrBlocked = !("xr" in navigator);
if (xrBlocked) {
  enterXrButton.disabled = true;
  enterXrButton.textContent = "WebXR unavailable";
  xrHint.hidden = false;
  xrHint.textContent = "WebXR is unavailable in this browser. You can still build together in desktop mode.";
} else if (mode === "ar") {
  // Desktop browsers have navigator.xr but no immersive-ar: say so up front instead of failing when the button is pressed.
  navigator.xr.isSessionSupported("immersive-ar").catch(() => false).then((supported) => {
    if (supported) return;
    xrBlocked = true;
    enterXrButton.disabled = true;
    enterXrButton.textContent = "AR is not available on this device";
    xrHint.hidden = false;
    xrHint.textContent = "This browser cannot start an AR session. You can still build with everyone in desktop mode.";
  });
}
// AR bench size. The board is 7 units across, far too big for a table, so an AR page can offer a size list (#ar-scale, or ?scale=).
// Everyone in the same physical room must pick the same size, because part positions are shared in board units.
const arScaleSelect = document.querySelector("#ar-scale");
if (arScaleSelect && params.has("scale")) {
  const wanted = [...arScaleSelect.options].find((option) => Number(option.value) === Number(params.get("scale")));
  if (wanted) arScaleSelect.value = wanted.value;
}
function arScale() {
  if (mode !== "ar") return 1;
  const value = Number(arScaleSelect ? arScaleSelect.value : params.get("scale"));
  return value >= 0.05 && value <= 1 ? value : 1;
}

// Join flow furniture, added here so the three pages share it: numbered steps, a note when the lite view is on, and a
// compact "you are in room X" bar with a Leave button that replaces the name and room fields once connected.
document.body.classList.add("v2");
// Switch between the two labs, and a link to the same lab in v1 (rooms are shared: v1 and v2 pages in one room see the same parts).
{
  const nav = document.querySelector(".version-nav");
  if (nav) {
    nav.querySelectorAll("a[data-theme]").forEach((link) => {
      const target = new URL(location.href);
      target.searchParams.set("theme", link.dataset.theme);
      link.href = target.search || "?";
      if (link.dataset.theme === themeId) link.setAttribute("aria-current", "page");
    });
    const old = nav.querySelector("a[data-v1]");
    if (old) old.href = themeId === "safety" ? "../vr/?theme=safety" : "../vr/";
  }
}
const heading = document.querySelector(".heading");
const steps = document.createElement("ol");
steps.className = "steps";
steps.innerHTML = "<li>Pick a name and a room, then press <b>Join room</b>.</li><li>Ask others to open this page and join the same room (rooms are per example).</li><li>Add parts with the buttons, then drag them on the board. One person holds a part at a time.</li>";
heading.after(steps);
const roomBar = document.createElement("div");
roomBar.className = "room-bar";
const roomLine = document.createElement("span");
roomLine.id = "room-line";
const leaveButton = document.createElement("button");
leaveButton.type = "button";
leaveButton.id = "leave";
leaveButton.textContent = "Leave room";
roomBar.append(roomLine, leaveButton);
connection.insertBefore(roomBar, status);
leaveButton.addEventListener("click", () => leaveRoom());

const scene = new THREE.Scene();
// Deep Pitt navy behind the workshop and the bay. The AR page's desktop preview is a bright room, so it gets a pale backdrop;
// that backdrop is taken away while an AR session runs (the camera view shows instead) and put back when it ends.
const sceneColor = mode === "ar" ? "#dfe9f2" : themeId === "safety" ? "#0a1426" : "#071433";
const desktopBackground = new THREE.Color(sceneColor);
scene.background = desktopBackground;
scene.fog = new THREE.Fog(sceneColor, 18, 46);
const camera = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.1, 80);
camera.position.set(2.3, 2.4, 3.5);
camera.lookAt(0, 0.35, 0);

const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: mode === "ar" });
renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.xr.enabled = true;
// Filmic tone mapping and a soft shadow map (the shadow is switched off inside headsets to save fill rate).
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.0;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
// Lite mode (no shadows, no glTF props: just the room and the avatars) is the default on software renderers, which are far too
// slow for the full scene. ?lite=0|1 forces it; ?shadows=0|1 and ?props=0|1 override the two parts. Headsets never draw shadows.
const gl = renderer.getContext();
const gpuName = String(gl.getParameter(gl.getExtension("WEBGL_debug_renderer_info")?.UNMASKED_RENDERER_WEBGL ?? gl.RENDERER));
const liteMode = params.has("lite") ? params.get("lite") === "1" : /swiftshader|llvmpipe|software/i.test(gpuName);
if (liteMode && !params.has("lite")) {
  const note = document.createElement("p");
  note.className = "lite-note";
  const fullUrl = new URL(location.href);
  fullUrl.searchParams.set("lite", "0");
  note.innerHTML = `Lite view: no graphics card was detected, so shadows and room props are off. <a href="${fullUrl.search}">Show the full scene</a> (slow without a GPU).`;
  steps.after(note);
}
const flag = (name, fallback) => (params.has(name) ? params.get(name) === "1" : fallback);
const shadowsWanted = flag("shadows", !liteMode);
const propsWanted = flag("props", !liteMode);
renderer.shadowMap.enabled = shadowsWanted;
if (mode === "ar") renderer.setClearColor(0x000000, 0);
renderer.domElement.className = "scene";
renderer.domElement.setAttribute("aria-label", theme.sceneLabel);
renderer.domElement.setAttribute("role", "application");
renderer.domElement.tabIndex = 0;
renderer.domElement.setAttribute("aria-describedby", "controls");
document.body.appendChild(renderer.domElement);

const orbit = new OrbitControls(camera, renderer.domElement);
orbit.enableDamping = !reducedMotion;
orbit.target.set(0, 0.35, 0);

// The room around the board: generated floor, walls, window, pegboard or bay props, an environment map, the key light and
// (on a desktop with a GPU) optional CC0 glTF props. VR keeps the room; AR hides it.
const environment = buildRoom2({ themeId, mode, scene, renderer, withProps: propsWanted, shadows: shadowsWanted });
const keyLight = environment.key;

const board = new THREE.Group();
scene.add(board);

const surface = buildBoardSurface(themeId, mode);
board.add(surface);

const boardGuide = new THREE.Mesh(
  new THREE.TorusGeometry(2.8, 0.012, 6, 96),
  new THREE.MeshBasicMaterial({ color: PITT.gold, transparent: true, opacity: 0.55 }),
);
boardGuide.rotation.x = Math.PI / 2;
boardGuide.position.y = 0.088;
boardGuide.visible = false; // the printed ring on the mat / floor is the guide in v2; kept for the capture scripts
board.add(boardGuide);

const objects = new Map();
const peers = new Map();
const raycaster = new THREE.Raycaster();
const pointer = new THREE.Vector2();
const up = new THREE.Vector3(0, 1, 0);
const cursorFloor = new THREE.Plane(up, -0.08);
const fx = createFx({ scene, board });
attachFx(fx);
const tempVector = new THREE.Vector3();
const tempVector2 = new THREE.Vector3();
const partName = (part) => theme.parts.find((item) => item.id === part)?.name ?? part;
const pendingToggles = [];
const selectionRing = new THREE.Mesh(
  new THREE.TorusGeometry(0.5, 0.016, 8, 56),
  new THREE.MeshBasicMaterial({ color: PITT.gold, toneMapped: false }),
);
selectionRing.rotation.x = Math.PI / 2;
selectionRing.visible = false;
board.add(selectionRing);

let socket = null;
let selfId = null;
let selectedPart = theme.parts[0].id;
let armedPart = null; // a part type waiting to be dropped by clicking the board (desktop); the headset always places on select
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
let lastMoveSent = 0;
let objectSerial = 0;

const reticle = new THREE.Mesh(
  new THREE.RingGeometry(0.09, 0.13, 32),
  new THREE.MeshBasicMaterial({ color: "#73e7d4", side: THREE.DoubleSide }),
);
reticle.rotation.x = -Math.PI / 2;
reticle.visible = false;
scene.add(reticle);

// Name tags use the shared brand kit: a backed pill with a coloured chip, never bare floating text.
function makeLabel(text, color) {
  return tagSprite(text, { accent: color, status: "idle", worldHeight: 0.27, depthTest: true, maxChars: 28 });
}

function makePeer(peer) {
  const avatar = createAvatar({ id: peer.id, name: peer.name, mode: peer.mode, makeLabel });
  board.add(avatar.root);
  return avatar;
}

// Contact-shadow blobs stand in for the shadow map when it is off (inside a headset, in lite mode).
let shadowMapOn = shadowsWanted;
function syncBlob(group) { if (group.userData.blob) group.userData.blob.visible = !shadowMapOn; }
function setShadowMode(on) {
  shadowMapOn = on;
  environment.setShadows(on);
  for (const object of objects.values()) syncBlob(object.group);
}

function makePart(part) {
  const group = theme.build(part);
  group.userData.part = part;
  group.traverse((child) => {
    if (child.isMesh) {
      child.userData.sharedObject = group;
      child.castShadow = !child.name.startsWith("blob") && !child.material.transparent;
      child.receiveShadow = true;
    }
  });
  syncBlob(group);
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
  const group = makePart(data.part);
  group.userData.sharedId = data.id;
  const object = { id: data.id, group, data, heldBy: data.heldBy ?? null };
  setObjectPosition(object, data.position, data.rotation);
  theme.applyState(object);
  objects.set(data.id, object);
  board.add(group);
}

function removeSharedObject(id) {
  const object = objects.get(id);
  if (!object) return;
  board.remove(object.group);
  object.group.traverse((child) => {
    if (!child.isMesh && !child.isSprite) return;
    if (child.isMesh && !child.geometry.userData.shared) child.geometry.dispose();
    const materials = Array.isArray(child.material) ? child.material : [child.material];
    for (const item of materials) if (!item.userData.shared) item.dispose(); // v2 shares most materials between parts
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
  rotateButton.disabled = !id || !isConnected();
  const part = theme.parts.find((item) => item.id === objects.get(id)?.data.part);
  toggleButton.disabled = !part?.toggles || !isConnected();
  selectionRing.visible = Boolean(objects.get(id));
}

function isConnected() {
  return socket?.readyState === WebSocket.OPEN && selfId !== null;
}

function send(message) {
  if (!isConnected()) {
    setStatus("Join a room before changing the shared scene.", "warn");
    return false;
  }
  socket.send(JSON.stringify(message));
  return true;
}

function updateRoster() {
  const tag = (value) => (value === "vr" || value === "ar" ? ` (${value.toUpperCase()})` : "");
  const participants = [...peers.values()].map((peer) => `${peer.name}${tag(peer.mode)}`);
  participants.unshift(`${nameInput.value.trim() || "You"} (you)${tag(renderer.xr.isPresenting ? mode : "web")}`);
  roster.textContent = `${participants.length} in room: ${participants.join(" · ")}`;
}

function updatePeer(peer) {
  let avatar = peers.get(peer.id)?.avatar;
  if (!avatar) {
    avatar = makePeer(peer);
    peers.set(peer.id, { ...peer, avatar, group: avatar.root });
  } else {
    peers.set(peer.id, { ...peers.get(peer.id), ...peer, avatar, group: avatar.root });
  }
  // A cursor of exactly [0, 0] is the relay's default for "not seen yet". Poses are optional and only come from XR clients.
  if (Array.isArray(peer.cursor) && (peer.cursor[0] !== 0 || peer.cursor[1] !== 0)) avatar.setCursor(peer.cursor);
  const pose = normalizePose(peer.pose);
  if (pose) avatar.setPose(pose);
  updateRoster();
}

function removePeer(id) {
  const peer = peers.get(id);
  if (!peer) return;
  peer.avatar.dispose();
  peers.delete(id);
  for (const object of objects.values()) {
    if (object.heldBy === id) object.heldBy = null;
  }
  updateRoster();
}

function setStatus(message, state = "info") {
  status.textContent = message;
  status.dataset.state = state;
}
function updateStatus(message) { setStatus(message); }

function receive(message) {
  switch (message.type) {
    case "welcome":
      selfId = message.selfId;
      nameInput.disabled = true;
      roomInput.disabled = true;
      joinButton.disabled = true;
      enterXrButton.disabled = xrBlocked;
      workspace.hidden = false;
      connection.classList.add("joined");
      roomLine.textContent = `Room "${message.room}"`;
      for (const object of message.objects) addSharedObject(object);
      for (const peer of message.peers) updatePeer(peer);
      updateRoster();
      if (renderer.xr.isPresenting) send({ type: "mode", mode });
      if (reconnect.attempts > 0) {
        const lost = reconnect.objectCount > 0 && message.objects.length === 0;
        updateStatus(`Reconnected to room "${message.room}".${lost ? " The room was empty, so earlier parts are gone." : ""}`);
      } else {
        updateStatus(message.objects.length ? `Joined room "${message.room}". ${message.objects.length} parts are already on the board.` : `Joined room "${message.room}". The board is empty: add a part to start.`);
      }
      reconnect.attempts = 0;
      break;
    case "peer-join":
      updatePeer(message.peer);
      updateStatus(`${message.peer.name} joined the room.`);
      break;
    case "peer-cursor": {
      const peer = peers.get(message.id);
      if (peer) {
        peer.cursor = message.position;
        peer.avatar.setCursor(message.position);
      }
      break;
    }
    case "peer-pose": {
      const peer = peers.get(message.id);
      const pose = normalizePose(message);
      if (peer && pose) peer.avatar.setPose(pose);
      break;
    }
    case "peer-mode": {
      const peer = peers.get(message.id);
      if (peer) {
        peer.mode = message.mode;
        peer.avatar.setMode(message.mode);
        updateRoster();
      }
      break;
    }
    case "peer-leave":
      removePeer(message.id);
      updateStatus("A participant left the room.");
      break;
    case "object-added":
      addSharedObject(message.object);
      updateStatus(`${partName(message.object.part)} added to the shared board.`);
      if (message.object.createdBy === selfId) {
        const wanted = pendingToggles.findIndex((item) => item === message.object.part);
        if (wanted >= 0) {
          pendingToggles.splice(wanted, 1);
          send({ type: "toggle", id: message.object.id, on: true });
        }
      }
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
    case "toggle-state": {
      const object = objects.get(message.id);
      if (!object) break;
      object.data.on = message.on;
      theme.applyState(object);
      updateStatus(`${partName(object.data.part)} turned ${message.on ? "on" : "off"} for everyone.`);
      break;
    }
    case "object-rotated": {
      const object = objects.get(message.id);
      if (object) setObjectPosition(object, object.data.position, message.rotation);
      break;
    }
    case "room-limit":
      updateStatus(message.message);
      break;
    default:
      break;
  }
}

// Close codes where the relay gave a reason (room full, bad name, wrong theme): show the reason, do not retry.
const FINAL_CLOSE_CODES = new Set([1000, 1003, 1007, 1008, 1013]);
const MAX_RECONNECTS = 5;
const reconnect = { attempts: 0, timer: null, room: null, objectCount: 0 };
let leaving = false;

function friendlyClose(event) {
  if (event.code === 1013) return `${event.reason || "The room is full."} Try another room name.`;
  return event.reason || "Disconnected from the room. Join again to continue.";
}

// Where the relay is. Normally the page and the relay are the same server. On GitHub Pages (static files only) there is no relay;
// ?relay=wss://host:8443 points the page at one. Browsers refuse ws:// from an https page and refuse a wss:// relay whose
// certificate they do not trust, so the lab PC's own https address (which serves this page and the relay) is the simple route.
const onPages = /github\.io$/i.test(location.hostname);
const relayParam = params.get("relay");
const relayUrl = /^wss?:\/\/[^/\s]+$/i.test(relayParam ?? "") ? `${relayParam}/ws` : `${location.protocol === "https:" ? "wss:" : "ws:"}//${location.host}/ws`;
if (onPages && !relayParam) {
  const note = document.createElement("p");
  note.className = "lite-note";
  note.textContent = "This copy is on GitHub Pages, which serves static files only, so there is no shared-room relay here and Join room cannot connect. Open this page from the relay on the lab PC (its https address, port 8443), or add ?relay=wss://host:8443 if you run a relay with a trusted certificate.";
  steps.after(note);
}

function joinRoom(retry = false) {
  if (socket && socket.readyState !== WebSocket.CLOSED) return;
  clearTimeout(reconnect.timer);
  const room = retry ? reconnect.room : roomInput.value.trim().toLowerCase();
  if (!roomPattern.test(room)) {
    setStatus("Room names must be 1-24 letters, numbers, or hyphens.", "error");
    roomInput.focus();
    return;
  }
  if (!retry) reconnect.attempts = 0;
  reconnect.room = room;
  leaving = false;
  const ws = new WebSocket(relayUrl);
  socket = ws;
  joinButton.disabled = true;
  setStatus(retry ? `Reconnecting to room "${room}" (try ${reconnect.attempts} of ${MAX_RECONNECTS})...` : "Connecting to the shared room...");
  ws.addEventListener("open", () => {
    // Everyone joins as a browser page; a headset or phone AR session announces itself with a "mode" message once it starts.
    ws.send(JSON.stringify({ type: "join", room, theme: theme.id, name: nameInput.value.trim() || "Guest", mode: "web" }));
  });
  ws.addEventListener("message", (event) => {
    try {
      receive(JSON.parse(event.data));
    } catch (error) {
      console.error("Could not read a shared-room update.", error);
      updateStatus("Received an invalid room update. Check the relay log.");
    }
  });
  // "error" always precedes "close" with no detail of its own, so the close handler explains what happened.
  ws.addEventListener("close", (event) => {
    if (socket !== ws) return;
    const wasConnected = selfId !== null;
    if (wasConnected) reconnect.objectCount = objects.size;
    selfId = null;
    nameInput.disabled = false;
    roomInput.disabled = false;
    joinButton.disabled = false;
    workspace.hidden = true;
    connection.classList.remove("joined");
    for (const id of [...objects.keys()]) removeSharedObject(id);
    for (const id of [...peers.keys()]) removePeer(id);
    selectObject(null);
    drag = null;
    if (leaving) { setStatus("You left the room. Join again to continue."); return; }
    if (FINAL_CLOSE_CODES.has(event.code)) { reconnect.attempts = 0; setStatus(friendlyClose(event), "error"); return; }
    if (wasConnected || reconnect.attempts > 0) {
      if (reconnect.attempts < MAX_RECONNECTS) {
        reconnect.attempts += 1;
        const seconds = Math.min(8, 2 ** (reconnect.attempts - 1));
        setStatus(`Connection lost. Trying again in ${seconds} s (try ${reconnect.attempts} of ${MAX_RECONNECTS}). Press Join room to try now.`, "warn");
        reconnect.timer = setTimeout(() => joinRoom(true), seconds * 1000);
        return;
      }
      reconnect.attempts = 0;
      setStatus("Could not reconnect. The relay may have stopped: start it with npm start in the multi-player folder, then press Join room.", "error");
      return;
    }
    setStatus(`Could not reach the room relay at ${location.host}. Start it with npm start in the multi-player folder and check the address, then press Join room.`, "error");
  });
}

function leaveRoom() {
  leaving = true;
  clearTimeout(reconnect.timer);
  if (socket && socket.readyState !== WebSocket.CLOSED) { releaseGrab(); socket.close(1000, "Leaving the room."); }
}

function spawnPart(part, position = [0, 0.08, 0]) {
  selectedPart = part;
  document.querySelectorAll("[data-part]").forEach((button) => {
    button.setAttribute("aria-pressed", String(button.dataset.part === part));
  });
  const index = objectSerial++;
  const offset = index % 4;
  const spread = Math.floor(index / 4);
  const gap = theme.spawnSpacing;
  const spawnPosition = position[0] === 0 && position[2] === 0
    ? [position[0] + (offset - 1.5) * gap, position[1], position[2] + (spread - 1) * gap * 0.8]
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

// accept(object) lets the caller skip parts it cannot use (the Y and B buttons look past a barrier for the beacon behind it).
function findObjectHit(accept = null) {
  // setFromXRController leaves the raycaster without a camera, and raycasting against a label sprite throws without one.
  raycaster.camera = renderer.xr.isPresenting ? renderer.xr.getCamera() : camera;
  const hits = raycaster.intersectObjects(meshesForRaycast(), true);
  for (const hit of hits) {
    const group = hit.object.userData.sharedObject ?? hit.object.parent;
    const object = group?.userData.sharedId ? objects.get(group.userData.sharedId) : null;
    if (object && (!accept || accept(object))) return { object, point: hit.point };
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
  const now = performance.now();
  if (now - lastMoveSent >= 33) { // the release message carries the final position
    lastMoveSent = now;
    send({ type: "move", id: object.id, position, rotation });
  }
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
  const tool = toolbox.pick(raycaster);
  if (tool) {
    event.preventDefault();
    event.stopPropagation();
    toolbox.press(tool);
    return;
  }
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
      else if (isConnected() && armedPart) spawnAt(armedPart, point);
      else selectObject(null);
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

for (const item of theme.parts) {
  const button = document.createElement("button");
  button.type = "button";
  button.dataset.part = item.id;
  button.textContent = `+ ${item.label}`;
  button.addEventListener("click", () => {
    selectedPart = item.id;
    armedPart = item.id;
    partPalette.querySelectorAll("button").forEach((other) => other.setAttribute("aria-pressed", String(other === button)));
    if (spawnPart(selectedPart)) setStatus(`Added ${partName(item.id)}. Click the board to add another, or press Esc to stop adding.`);
  });
  partPalette.append(button);
}

function toggleSelected() {
  const object = objects.get(selectedObjectId);
  if (theme.parts.find((item) => item.id === object?.data.part)?.toggles) {
    send({ type: "toggle", id: object.id, on: !object.data.on });
  }
}

function rotateSelected() {
  const object = objects.get(selectedObjectId);
  if (!object) return;
  const rotation = [object.group.rotation.x, object.group.rotation.y + Math.PI / 4, object.group.rotation.z];
  send({ type: "rotate", id: object.id, rotation });
}

// Removes every part (parts another person is holding stay until they let go).
function clearScene() {
  if (!isConnected()) return false;
  pendingToggles.length = 0;
  for (const id of [...objects.keys()]) send({ type: "delete", id });
  return true;
}

function resetScene() {
  if (!clearScene()) { setStatus("Join a room before changing the shared scene.", "warn"); return; }
  objectSerial = 0;
  setStatus("Scene cleared for everyone. Add parts or load an example.");
}

// Replaces the scene with a ready-made one. The relay handles messages in order, so the clear finishes before the new parts arrive.
function loadExample(index) {
  const example = theme.examples?.[index];
  if (!example) return;
  if (!isConnected()) { setStatus("Join a room before changing the shared scene.", "warn"); return; }
  clearScene();
  objectSerial = 0;
  for (const item of example.items) {
    if (item.on) pendingToggles.push(item.part);
    send({ type: "place", part: item.part, position: item.position, rotation: item.rotation });
  }
  setStatus(`Loaded the example "${example.label}": ${example.blurb}`);
}

function loadDemo() { loadExample(0); }

// Sets the selected part's state, or the part under a controller's ray: Y opens/turns off, B closes/turns on.
function setPartState(on, controller = null) {
  let object = objects.get(selectedObjectId);
  if (controller) {
    raycaster.setFromXRController(controller);
    const toggles = (item) => theme.parts.find((part) => part.id === item.data.part)?.toggles;
    const hit = findObjectHit(toggles) ?? findObjectHit();
    if (hit) { object = hit.object; selectObject(object.id); }
  }
  const labels = theme.xrLabels;
  if (!object) { setStatus("Point at a part, or select one, then press the button again.", "warn"); return false; }
  const part = theme.parts.find((item) => item.id === object.data.part);
  if (!part?.toggles) { setStatus(`${partName(object.data.part)} has no ${labels.offWord}/${labels.onWord} state. ${theme.toggleLabel} works on a part that does.`, "warn"); return false; }
  return send({ type: "toggle", id: object.id, on });
}

function toolboxState() {
  const object = objects.get(selectedObjectId);
  const part = theme.parts.find((item) => item.id === object?.data.part);
  const words = theme.xrLabels;
  return {
    connected: isConnected(),
    hasSelection: Boolean(object),
    canToggle: Boolean(part?.toggles),
    selectedLabel: !isConnected() ? "Join a room on the page first" : object ? `Selected: ${partName(object.data.part)}${part?.toggles ? ` (${object.data.on ? words.onWord : words.offWord})` : ""}` : "Nothing selected: grab a part to select it",
    summary: summary.textContent,
  };
}

const toolbox = createToolbox({
  theme,
  getState: toolboxState,
  actions: {
    add: (part) => { selectedPart = part; spawnPart(part); },
    example: (index) => loadExample(index),
    rotate: () => rotateSelected(),
    remove: () => { if (selectedObjectId) send({ type: "delete", id: selectedObjectId }); },
    setState: (on) => setPartState(on),
    reset: () => resetScene(),
  },
});
toolbox.group.visible = false;
scene.add(toolbox.group);
let toolboxPlacement = false; // true until the toolbox has been put in front of the player in this session

// Puts the toolbox at the player's left, a little ahead, at chest height, turned to face them. Squeezing both grips does it again.
const placeHead = new THREE.Vector3();
const placeAhead = new THREE.Vector3();
function placeToolbox() {
  const xrCamera = renderer.xr.getCamera();
  xrCamera.getWorldPosition(placeHead);
  xrCamera.getWorldDirection(placeAhead);
  placeAhead.y = 0;
  if (placeAhead.lengthSq() < 0.01) placeAhead.set(0, 0, -1);
  placeAhead.normalize();
  const reach = mode === "ar" ? 0.65 : 0.75;
  const side = mode === "ar" ? 0.55 : 0.75;
  toolbox.group.position.set(placeHead.x + placeAhead.x * reach + placeAhead.z * side, placeHead.y - 0.2, placeHead.z + placeAhead.z * reach - placeAhead.x * side);
  toolbox.group.rotation.set(0, Math.atan2(placeHead.x - toolbox.group.position.x, placeHead.z - toolbox.group.position.z), 0);
  toolboxPlacement = false;
}

// Desktop preview of the toolbox (?toolbox=1) so it can be checked without a headset.
if (params.get("toolbox") === "1") {
  toolbox.group.visible = true;
  toolbox.group.position.set(-2.9, 1.5, 1.6);
  toolbox.group.rotation.y = 0.9;
}

toggleButton.addEventListener("click", toggleSelected);
rotateButton.addEventListener("click", rotateSelected);
demoButton.addEventListener("click", loadDemo);
demoButton.textContent = theme.examples?.length ? `Load: ${theme.examples[0].label}` : theme.demoLabel;
// The other ready-made scenes and a reset button, under the demo button on the page.
{
  const row = document.createElement("div");
  row.className = "examples";
  (theme.examples ?? []).slice(1).forEach((example, index) => {
    const button = document.createElement("button");
    button.type = "button";
    button.dataset.example = String(index + 1);
    button.title = example.blurb;
    button.textContent = `Load: ${example.label}`;
    button.addEventListener("click", () => loadExample(index + 1));
    row.append(button);
  });
  const reset = document.createElement("button");
  reset.type = "button";
  reset.id = "reset-scene";
  reset.className = "danger";
  reset.textContent = "Reset scene";
  reset.addEventListener("click", () => { if (!objects.size || window.confirm("Remove every part for everyone in this room?")) resetScene(); });
  row.append(reset);
  demoButton.after(row);
}
toggleButton.textContent = theme.toggleLabel;
// A page that writes its own heading and controls text (the AR Circuit Lab) sets data-own-text on <body>.
if (document.body.dataset.ownText === undefined) {
  document.title = `${document.title.split("—")[0]}— ${theme.title}`;
  document.querySelector("h1").textContent = theme.title;
  document.querySelector(".heading > p:last-child").textContent = theme.pitch;
  document.querySelector("#controls").textContent = theme.controls;
}

joinButton.addEventListener("click", () => joinRoom(false));
roomInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter") joinRoom(false);
});
nameInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter") joinRoom(false);
});

deleteButton.addEventListener("click", () => {
  if (selectedObjectId) send({ type: "delete", id: selectedObjectId });
});

document.addEventListener("keydown", (event) => {
  if (event.target instanceof HTMLInputElement) return;
  if (event.key.toLowerCase() === "t") toggleSelected();
  if (event.key.toLowerCase() === "r") rotateSelected();
  if (event.key === "Delete" && selectedObjectId) send({ type: "delete", id: selectedObjectId });
  if (event.key === "Escape") {
    armedPart = null;
    partPalette.querySelectorAll("button").forEach((other) => other.setAttribute("aria-pressed", "false"));
    selectObject(null);
  }
  keyboardMove(event);
});

// Keyboard control of the board: [ and ] choose the previous or next part, arrow keys nudge the chosen part (Shift: bigger step).
function keyboardMove(event) {
  if (!(event.target === document.body || event.target === renderer.domElement)) return;
  if (!isConnected() || event.ctrlKey || event.metaKey || event.altKey) return;
  const ids = [...objects.keys()];
  if (event.key === "[" || event.key === "]") {
    if (!ids.length) return;
    const index = ids.indexOf(selectedObjectId);
    const next = event.key === "]" ? (index + 1) % ids.length : (index <= 0 ? ids.length - 1 : index - 1);
    selectObject(ids[next]);
    const chosen = objects.get(ids[next]);
    setStatus(`Selected ${partName(chosen.data.part)} (${next + 1} of ${ids.length}). Arrow keys move it, R rotates it.`);
    event.preventDefault();
    return;
  }
  const delta = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] }[event.key];
  const object = objects.get(selectedObjectId);
  if (!delta || !object) return;
  event.preventDefault();
  if (object.heldBy && object.heldBy !== selfId) { setStatus("That part is being held by another participant.", "warn"); return; }
  const step = event.shiftKey ? 0.4 : 0.1;
  const limit = 3.1;
  const position = [object.group.position.x + delta[0] * step, 0.08, object.group.position.z + delta[1] * step];
  const radius = Math.hypot(position[0], position[2]);
  if (radius > limit) { position[0] *= limit / radius; position[2] *= limit / radius; }
  const rotation = [object.group.rotation.x, object.group.rotation.y, object.group.rotation.z];
  send({ type: "grab", id: object.id });
  setObjectPosition(object, position, rotation);
  send({ type: "move", id: object.id, position, rotation });
  send({ type: "release", id: object.id, position, rotation });
}

const handFactory = new XRHandModelFactory();
const gloveMaterial = new THREE.MeshStandardMaterial({ color: "#1448a8", roughness: 0.42, metalness: 0.0, envMapIntensity: 1.3 });
const cuffMaterial = new THREE.MeshStandardMaterial({ color: PITT.gold, roughness: 0.28, metalness: 0.85 });
// side is -1 for the left hand and 1 for the right. The grip space has -Z pointing out along the fingers.
function makeGloveHand(side) {
  // v2 merges the seven glove pieces into two meshes (glove leather, gold cuff): fewer draw calls for every frame in the headset.
  const hand = new THREE.Group();
  hand.name = "glove-hand";
  const leather = []; const gold = [];
  const piece = (list, geometry, position, rotation = [0, 0, 0]) => {
    const mesh = new THREE.Mesh(geometry);
    mesh.position.set(...position); mesh.rotation.set(...rotation); mesh.updateMatrix();
    list.push(geometry.applyMatrix4(mesh.matrix));
  };
  piece(leather, new THREE.BoxGeometry(0.085, 0.03, 0.09), [0, 0, 0.01]);
  piece(gold, new THREE.CylinderGeometry(0.036, 0.04, 0.04, 16), [0, 0, 0.075], [Math.PI / 2, 0, 0]);
  [-0.03, -0.01, 0.01, 0.03].forEach((x, finger) => {
    const length = [0.05, 0.062, 0.058, 0.046][finger];
    piece(leather, new THREE.CapsuleGeometry(0.0085, length, 4, 8), [x, -0.006, -0.04 - length / 2], [Math.PI / 2 + 0.35, 0, 0]); // fingers curl slightly toward the palm
  });
  // thumb on the inner side of the hand (toward the other hand when the palm faces down)
  piece(leather, new THREE.CapsuleGeometry(0.01, 0.04, 4, 8), [-side * 0.052, 0.002, -0.015], [Math.PI / 2 - 0.2, 0, -side * 0.9]);
  hand.add(new THREE.Mesh(mergeGeometries(leather), gloveMaterial), new THREE.Mesh(mergeGeometries(gold), cuffMaterial));
  hand.position.set(0, 0, -0.03);
  // A hand holding a controller is on its side: thumb up, palm toward the other hand. The glove is modelled palm-down, so roll it.
  hand.rotation.z = -side * Math.PI * 0.45;
  return hand;
}
// Which glove a grip shows. The glove is a rigid shape, so it is only for a held controller in VR:
// - bare tracked hands get the jointed hand model instead (it bends with the real fingers), not a glove on top of it;
// - in AR the real hand and controller are in view, so no glove is drawn over them;
// - controller 0 is not always the left hand, so the side comes from the input source when it connects.
function attachGloves(grip, index) {
  const gloves = { left: makeGloveHand(-1), right: makeGloveHand(1) };
  grip.add(gloves.left, gloves.right);
  const show = (source) => {
    const side = source?.handedness === "left" || source?.handedness === "right" ? source.handedness : index === 0 ? "left" : "right";
    const wanted = mode !== "ar" && !source?.hand;
    gloves.left.visible = wanted && side === "left";
    gloves.right.visible = wanted && side === "right";
  };
  show(null);
  grip.addEventListener("connected", (event) => show(event.data));
}

for (let index = 0; index < 2; index += 1) {
  const controller = renderer.xr.getController(index);
  const beam = new THREE.Line(
    new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(0, 0, 0), new THREE.Vector3(0, 0, -1)]),
    new THREE.LineBasicMaterial({ color: index === 0 ? "#7fc4ff" : PITT.gold, transparent: true, opacity: 0.7 }),
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
    controller.userData.source = event.data;
    controller.userData.connected = true;
  });
  controller.addEventListener("disconnected", () => { controller.userData.connected = false; controller.userData.source = null; });
  controller.addEventListener("squeezestart", () => {
    controller.userData.squeezing = true;
    if ([0, 1].every((other) => renderer.xr.getController(other).userData.squeezing)) toolboxPlacement = true;
  });
  controller.addEventListener("squeezeend", () => { controller.userData.squeezing = false; });

  // A visible hand for every controller (a blue glove with a gold cuff), and a tracked hand when the headset sees bare hands.
  // Nothing is fetched: the stock controller models come from a CDN, which the offline demo cannot use.
  const grip = renderer.xr.getControllerGrip(index);
  attachGloves(grip, index);
  scene.add(grip);
  const hand = renderer.xr.getHand(index);
  hand.add(handFactory.createHandModel(hand, "boxes"));
  scene.add(hand);
}

function haptic(controller, strength = 0.4, ms = 40) {
  controller.userData.source?.gamepad?.hapticActuators?.[0]?.pulse?.(strength, ms);
}

function onXrSelectStart(controller) {
  if (mode === "ar" && arCalibrationStep < 2) {
    if (hitWorldPoint) calibrateArOrigin(hitWorldPoint.clone());
    return;
  }
  raycaster.setFromXRController(controller);
  const tool = toolbox.pick(raycaster);
  if (tool) { if (toolbox.press(tool)) haptic(controller); return; }
  if (toolbox.group.visible && toolbox.distance(raycaster) !== null) return; // the panel itself: nothing to place or grab behind it
  const hit = findObjectHit();
  if (hit) {
    beginGrab(hit.object, hit.point);
    activeXrController = controller;
    return;
  }
  // AR on a phone: the part goes where the reticle (screen centre) meets the real surface. A tracked controller (headset AR)
  // points at the aligned bench instead, like VR below.
  if (mode === "ar" && hitWorldPoint && controller.userData.targetRayMode !== "tracked-pointer") {
    spawnAt(selectedPart, hitWorldPoint);
    return;
  }
  // Pointing at the empty board adds the last-chosen part there; pointing past the board edge does nothing.
  const point = raycaster.ray.intersectPlane(getBoardPlane(), tempVector);
  if (point && board.worldToLocal(point.clone()).setY(0).length() <= 3.3 && point.distanceTo(camera.getWorldPosition(tempVector2)) < 10) {
    spawnAt(selectedPart, point);
  }
}

// Face buttons, read each frame. On Quest the top face button of each hand is button 5 (Y on the left, B on the right) and the
// lower one is button 4 (X, A). Y opens or turns off, B closes or turns on, X or A rotates the selected part by 45 degrees.
function pollXrButtons() {
  for (let index = 0; index < 2; index += 1) {
    const controller = renderer.xr.getController(index);
    const pad = controller.userData.source?.gamepad;
    if (!pad || controller.userData.targetRayMode === "screen") continue;
    const now = pad.buttons.map((button) => button.pressed);
    const before = controller.userData.buttons ?? [];
    const edge = (number) => now[number] && !before[number];
    const hand = controller.userData.source.handedness;
    if (edge(5) && hand === "left") { if (setPartState(false, controller)) haptic(controller, 0.5, 60); }
    if (edge(5) && hand === "right") { if (setPartState(true, controller)) haptic(controller, 0.5, 60); }
    if (edge(4)) rotateSelected();
    controller.userData.buttons = now;
  }
}

// Highlight the toolbox button under each controller ray, and stop the beam at the panel.
function updateToolboxHover(time) {
  if (!toolbox.group.visible) return;
  if (toolboxPlacement && renderer.xr.isPresenting) placeToolbox();
  let hovered = null;
  for (let index = 0; index < 2; index += 1) {
    const controller = renderer.xr.getController(index);
    if (!controller.userData.connected || controller.userData.targetRayMode === "screen") continue;
    raycaster.setFromXRController(controller);
    const button = toolbox.pick(raycaster);
    const beam = controller.children.find((child) => child.isLine);
    const away = toolbox.distance(raycaster);
    if (beam) beam.scale.z = away !== null ? Math.min(away, 4) : 1;
    if (button) hovered = button;
  }
  toolbox.setHover(hovered);
  toolbox.update(time);
}

function updateControllerGrab() {
  if (!activeXrController || !drag) return;
  if (mode === "ar" && hitWorldPoint && activeXrController.userData.targetRayMode !== "tracked-pointer") {
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
  board.scale.setScalar(1);
  board.visible = true;
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
  board.scale.setScalar(arScale());
  board.visible = true;
  arDirection = direction;
  arCalibrationStep = 2;
  toolbox.group.visible = true;
  toolboxPlacement = true;
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
      updateStatus("In VR. Your toolbox floats at your left: point and press the trigger to add parts or load an example. Trigger grabs a part, Y opens and B closes a switch, X or A rotates.");
    } else {
      const supported = await navigator.xr.isSessionSupported("immersive-ar");
      if (!supported) throw new Error("This browser or device cannot start an immersive AR session.");
      resetArAlignment();
      renderer.xr.setReferenceSpaceType("local");
      const session = await navigator.xr.requestSession("immersive-ar", {
        requiredFeatures: ["hit-test"],
        optionalFeatures: ["local-floor", "dom-overlay", "hand-tracking"],
        domOverlay: { root: document.body },
      });
      await renderer.xr.setSession(session);
      scene.background = null; // the real room shows through
      await startArHitTesting(session);
      board.visible = false; // nothing is drawn in the real room until the two alignment points are set
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
    enterXrButton.disabled = !isConnected() || xrBlocked;
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
  enterXrButton.disabled = !isConnected() || xrBlocked;
  if (mode === "ar") { resetArAlignment(); scene.background = desktopBackground; }
  updateStatus(`${mode.toUpperCase()} session ended. The shared room remains connected.`);
}

enterXrButton.addEventListener("click", enterXr);

// In VR the board becomes a table-height model in front of the player instead of a floor they stand inside.
renderer.xr.addEventListener("sessionstart", () => {
  if (mode !== "vr") return;
  board.position.set(0, 0.85, -1.15);
  board.scale.setScalar(0.4);
});
// Inside a headset VR keeps the room (drawn on the real floor level) without glTF props or the shadow map, and the board
// becomes a table-top model on a steel pedestal. AR hides the room: the real room shows instead.
const vrStage = new THREE.Group();
vrStage.visible = false;
{
  const steel = new THREE.MeshStandardMaterial({ color: "#c3ccd6", roughness: 0.28, metalness: 1 });
  const column = new THREE.Mesh(new THREE.CylinderGeometry(0.09, 0.12, 0.74, 24), steel);
  column.position.set(0, 0.41, -1.15);
  const foot = new THREE.Mesh(new THREE.CylinderGeometry(0.34, 0.38, 0.04, 32), steel);
  foot.position.set(0, 0.02, -1.15);
  const collar = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.12, 0.06, 32), steel);
  collar.position.set(0, 0.78, -1.15);
  vrStage.add(column, foot, collar);
  scene.add(vrStage);
}
renderer.xr.addEventListener("sessionstart", () => { toolbox.group.visible = mode === "vr"; toolboxPlacement = true; send({ type: "mode", mode }); updateRoster(); environment.applyXr(true, mode); setShadowMode(false); vrStage.visible = mode === "vr"; });
renderer.xr.addEventListener("sessionend", () => { toolbox.group.visible = params.get("toolbox") === "1"; send({ type: "mode", mode: "web" }); updateRoster(); environment.applyXr(false, mode); setShadowMode(shadowsWanted); vrStage.visible = false; });
renderer.xr.addEventListener("sessionend", () => {
  if (mode !== "vr") return;
  board.position.set(0, 0, 0);
  board.scale.setScalar(1);
});

let lastSummary = "";
let lastFrameTime = 0;
let lastPoseSent = 0;
const poseHead = new THREE.Vector3();
const poseAhead = new THREE.Vector3();
const poseHand = new THREE.Vector3();

// Headset and phone participants share where their head and hands are, in board coordinates, about 15 times a second.
function sendPose(time) {
  if (!renderer.xr.isPresenting || !isConnected() || time - lastPoseSent < 66) return;
  const xrCamera = renderer.xr.getCamera();
  xrCamera.getWorldPosition(poseHead);
  xrCamera.getWorldDirection(poseAhead).add(poseHead);
  const head = board.worldToLocal(poseHead.clone());
  const ahead = board.worldToLocal(poseAhead.clone());
  const hands = [0, 1].map((index) => {
    const controller = renderer.xr.getController(index);
    if (!controller.userData.connected || controller.userData.targetRayMode === "screen") return null;
    controller.getWorldPosition(poseHand);
    return board.worldToLocal(poseHand.clone()).toArray();
  });
  const pose = normalizePose({ head: head.toArray(), yaw: Math.atan2(ahead.x - head.x, ahead.z - head.z), hands });
  if (!pose) return;
  lastPoseSent = time;
  send({ type: "pose", ...pose });
}

function updateAvatars(time) {
  const dt = Math.min(0.1, Math.max(0, (time - lastFrameTime) / 1000));
  lastFrameTime = time;
  const holders = new Set();
  for (const object of objects.values()) if (object.heldBy) holders.add(object.heldBy);
  // Desktop participants who point at the same spot would stand inside each other: spread them along the board edge.
  const standing = [...peers.values()].filter((peer) => !peer.avatar.hasPose);
  if (standing.length > 1) {
    const spread = spreadAngles(standing.map((peer) => peer.avatar.wantedAngle), 0.34);
    standing.forEach((peer, index) => peer.avatar.setStation(spread[index]));
  }
  for (const [id, peer] of peers) {
    // In AR the other AR participants are real people in the same room, so their avatars are not drawn over them.
    if (mode === "ar") peer.avatar.root.visible = !(renderer.xr.isPresenting && peer.mode === "ar");
    peer.avatar.setHolding(holders.has(id));
    peer.avatar.update(dt);
  }
}

function onMessageFrame(time, frame) {
  updateAvatars(time);
  sendPose(time);
  const text = theme.update(objects, time / 1000);
  if (text !== lastSummary) {
    summary.textContent = text;
    lastSummary = text;
  }
  const chosen = objects.get(selectedObjectId);
  if (chosen) {
    selectionRing.position.copy(chosen.group.position).setY(0.1);
    selectionRing.scale.setScalar(reducedMotion ? 1 : 1 + 0.06 * Math.sin(time / 180));
  }

  if (mode === "ar" && renderer.xr.isPresenting && frame) updateArHitTest(frame);
  if (renderer.xr.isPresenting) { pollXrButtons(); updateControllerGrab(); }
  updateToolboxHover(performance.now());
  orbit.update();
  renderer.render(scene, camera);
}

renderer.setAnimationLoop(onMessageFrame);

// On wide screens the control panel covers the left side, so shift the board to the right.
function frameScene() {
  camera.aspect = window.innerWidth / window.innerHeight;
  if (window.innerWidth > 900) {
    camera.setViewOffset(window.innerWidth, window.innerHeight, -window.innerWidth * 0.14, 0, window.innerWidth, window.innerHeight);
  } else if (window.innerWidth <= 520) {
    // Phones: the panel is a bottom sheet, so lift the board into the upper part of the screen.
    camera.setViewOffset(window.innerWidth, window.innerHeight, 0, window.innerHeight * 0.24, window.innerWidth, window.innerHeight);
  } else {
    camera.clearViewOffset();
  }
  camera.updateProjectionMatrix();
}
frameScene();

window.addEventListener("resize", () => {
  frameScene();
  renderer.setSize(window.innerWidth, window.innerHeight);
});

// Read-only handles for the capture and measurement scripts (tools/measure.mjs); nothing in the page depends on it.
window.__mp = { renderer, scene, camera, peers, environment, objects, toolbox, toolboxState, fx, version: 2 };

window.addEventListener("beforeunload", () => {
  releaseGrab();
  socket?.close(1000, "Leaving the room.");
  renderer.setAnimationLoop(null);
  renderer.dispose();
});

enterXrButton.disabled = !isConnected() || xrBlocked;
