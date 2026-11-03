// Participant avatars: a stylised CC0 character (KayKit) wearing a hard hat in the participant's color, with a brand name tag.
// Headset and phone participants send their head and hand positions: the character stands under the head and floating
// gloves follow the controllers (no IK). Desktop participants stand at the edge of the board and point at their cursor.
// If a model cannot load, or the room already has MAX_MODEL_AVATARS of them, a primitive avatar is used instead.
import * as THREE from "three";
import { PITT } from "../../brand-kit/panels.js";
import { cloneSkinned, loadTemplate } from "./world.js";
import {
  MAX_MODEL_AVATARS, angleDelta, avatarScaleForHead, chooseAnimation, loadWithFallback, lookFor, smoothing,
  hashString,
} from "./avatar-logic.mjs";

const STATION_RADIUS = 3.95; // desktop avatars stand just outside the board pad
const FLOOR_Y = -0.09; // the room floor sits just below the board pad
const WALK_SPEED = 0.12; // m/s above which the walk clip plays
const hatMaterials = new Map();
const hatGeometry = (() => {
  const dome = new THREE.SphereGeometry(1, 20, 10, 0, Math.PI * 2, 0, Math.PI / 2);
  dome.scale(1, 0.85, 1);
  const brim = new THREE.CylinderGeometry(1.18, 1.18, 0.06, 20);
  brim.translate(0, 0.01, 0.06);
  return { dome, brim };
})();
const gloveGeometry = new THREE.SphereGeometry(0.11, 14, 10);
const cuffGeometry = new THREE.CylinderGeometry(0.085, 0.1, 0.1, 12);
const markerGeometry = new THREE.RingGeometry(0.1, 0.16, 28);
const bodyGeometry = new THREE.CylinderGeometry(0.2, 0.26, 0.8, 12);
const headGeometry = new THREE.SphereGeometry(0.17, 16, 12);
let modelAvatars = 0;

function hatMaterial(hex) {
  if (!hatMaterials.has(hex)) hatMaterials.set(hex, new THREE.MeshStandardMaterial({ color: hex, roughness: 0.35, metalness: 0.1 }));
  return hatMaterials.get(hex);
}

function buildHat(hex, headRadius) {
  const hat = new THREE.Group();
  const material = hatMaterial(hex);
  for (const geometry of [hatGeometry.dome, hatGeometry.brim]) {
    const mesh = new THREE.Mesh(geometry, material);
    mesh.castShadow = true;
    hat.add(mesh);
  }
  hat.scale.setScalar(headRadius);
  return hat;
}

function primitiveBody(look) {
  const body = new THREE.Group();
  const cloth = new THREE.MeshStandardMaterial({ color: look.hat.hex, roughness: 0.7 });
  const torso = new THREE.Mesh(bodyGeometry, cloth);
  torso.position.y = 0.95;
  const head = new THREE.Mesh(headGeometry, new THREE.MeshStandardMaterial({ color: "#e8c3a0", roughness: 0.8 }));
  head.position.y = 1.55;
  const legs = new THREE.Mesh(new THREE.CylinderGeometry(0.17, 0.17, 0.55, 10), new THREE.MeshStandardMaterial({ color: PITT.navy, roughness: 0.8 }));
  legs.position.y = 0.28;
  body.add(torso, head, legs);
  body.userData.own = [cloth, head.material, legs.material, legs.geometry];
  body.userData.headY = 1.65;
  return body;
}

export function createAvatar({ id, name, mode, makeLabel }) {
  const look = lookFor(id);
  const root = new THREE.Group();
  root.name = `avatar-${id}`;
  const stand = new THREE.Group(); // positioned at floor level under the head, turned to face the right way
  root.add(stand);

  const accent = look.hat.hex;
  const label = makeLabel(`${name} · ${mode.toUpperCase()}`, accent);
  root.add(label);

  const gloves = [0, 1].map(() => {
    const glove = new THREE.Group();
    const material = new THREE.MeshStandardMaterial({ color: accent, roughness: 0.5 });
    const cuff = new THREE.Mesh(cuffGeometry, new THREE.MeshStandardMaterial({ color: "#f4f6f8", roughness: 0.6 }));
    cuff.position.set(0, 0, 0.1);
    cuff.rotation.x = Math.PI / 2;
    glove.add(new THREE.Mesh(gloveGeometry, material), cuff);
    glove.visible = false;
    glove.userData.own = [material, cuff.material];
    root.add(glove);
    return glove;
  });

  const marker = new THREE.Mesh(markerGeometry, new THREE.MeshBasicMaterial({ color: accent, side: THREE.DoubleSide, transparent: true, opacity: 0.9 }));
  marker.rotation.x = -Math.PI / 2;
  marker.visible = false;
  root.add(marker);
  const beam = new THREE.Line(
    new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3(0, 0, 1)]),
    new THREE.LineBasicMaterial({ color: accent, transparent: true, opacity: 0.55 }),
  );
  beam.visible = false;
  beam.frustumCulled = false;
  root.add(beam);

  const state = {
    id, look, root,
    x: 0, z: 0, yaw: 0, scale: 1, targetX: 0, targetZ: 0, targetYaw: 0, targetScale: 1,
    pose: null, cursor: null, holding: false, moving: false,
    mixer: null, actions: {}, currentClip: null, arms: [], headY: 1.7, usesModel: false, placed: false,
    disposed: false,
  };
  // Until a cursor or pose arrives, spread newcomers along the board edge by id so they do not overlap.
  // They start on the back arc of the board (z < 0) so a newcomer never blocks the default camera view.
  const angle = Math.PI + 0.35 + ((hashString(id) % 1000) / 1000) * (Math.PI - 0.7);
  state.targetX = state.x = Math.cos(angle) * STATION_RADIUS;
  state.targetZ = state.z = Math.sin(angle) * STATION_RADIUS;

  function setBody(body, headY) {
    if (state.disposed) { return; }
    stand.clear();
    stand.add(body);
    state.headY = headY;
    label.position.y = headY * 1.12 + 0.18;
  }

  function attachModel(gltf) {
    const body = cloneSkinned(gltf.scene);
    body.traverse((child) => {
      if (child.isMesh) child.castShadow = true;
      if (child.isMesh && /Arm(Left|Right)/.test(child.name)) state.arms.push(child);
      if (child.isSkinnedMesh) child.frustumCulled = false;
    });
    const holder = new THREE.Group();
    holder.add(body);
    holder.updateMatrixWorld(true);
    const raw = new THREE.Box3().setFromObject(holder);
    const fit = 1.78 / ((raw.max.y - raw.min.y) || 2); // normalise every character to roughly adult height
    body.scale.setScalar(fit);
    holder.updateMatrixWorld(true);
    const bounds = new THREE.Box3().setFromObject(holder);
    let headMesh = null;
    let headBone = null;
    body.traverse((child) => {
      if (child.isMesh && /_Head/.test(child.name)) headMesh = child;
      if (child.isBone && /^head$/i.test(child.name)) headBone = child;
    });
    const headBox = headMesh ? new THREE.Box3().setFromObject(headMesh) : bounds;
    const headWidth = (headBox.max.x - headBox.min.x) || 0.5;
    // Hard hat on top of the head, attached to the head bone so it follows the animation.
    const hat = buildHat(look.hat.hex, headWidth * 0.5);
    hat.position.set((headBox.min.x + headBox.max.x) / 2, headBox.max.y - headWidth * 0.44, (headBox.min.z + headBox.max.z) / 2);
    holder.add(hat);
    holder.updateMatrixWorld(true);
    if (headBone) headBone.attach(hat);
    const mixer = new THREE.AnimationMixer(body);
    for (const clip of gltf.animations) state.actions[clip.name] = mixer.clipAction(clip);
    state.mixer = mixer;
    state.usesModel = true;
    setBody(holder, bounds.max.y - bounds.min.y);
    play("Unarmed_Idle");
  }

  function attachPrimitive() {
    const body = primitiveBody(look);
    const hat = buildHat(look.hat.hex, 0.2);
    hat.position.y = 1.62;
    body.add(hat);
    state.primitiveOwn = body.userData.own;
    setBody(body, 1.7);
  }

  function play(clip) {
    const next = state.actions[clip];
    if (!next || state.currentClip === clip) return;
    const previous = state.actions[state.currentClip];
    next.reset().play();
    if (previous) previous.crossFadeTo(next, 0.25, false);
    state.currentClip = clip;
  }

  // Decide model or primitive; only the first MAX_MODEL_AVATARS participants get the heavier character model.
  const wantsModel = modelAvatars < MAX_MODEL_AVATARS;
  if (wantsModel) modelAvatars += 1;
  state.countedModel = wantsModel;
  if (wantsModel) {
    loadWithFallback(
      async () => (await loadTemplate("characters", look.character)).gltf,
      () => null,
    ).then(({ value }) => {
      if (state.disposed) return;
      if (value) attachModel(value); else attachPrimitive();
      state.ready = true;
    });
  } else {
    attachPrimitive();
    state.ready = true;
  }

  const api = {
    id,
    root,
    state,
    get usesModel() { return state.usesModel; },
    // The newest network data for this participant. Any of the fields may be missing.
    setCursor(position) {
      state.cursor = position;
      if (state.pose) return;
      const [cx, cz] = position;
      const length = Math.hypot(cx, cz) || 1;
      state.targetX = (cx / length) * STATION_RADIUS;
      state.targetZ = (cz / length) * STATION_RADIUS;
    },
    setPose(pose) {
      state.pose = pose;
      state.targetX = pose.head[0];
      state.targetZ = pose.head[2];
      state.targetYaw = pose.yaw;
      state.targetScale = avatarScaleForHead(pose.head[1]);
    },
    setHolding(holding) { state.holding = holding; },
    update(dt) {
      if (state.disposed) return;
      const k = smoothing(dt);
      const dx = state.targetX - state.x;
      const dz = state.targetZ - state.z;
      const speed = Math.hypot(dx, dz) / Math.max(dt, 1e-3) * k;
      state.x += dx * k;
      state.z += dz * k;
      state.moving = speed > WALK_SPEED;
      let yawTarget;
      if (state.pose) yawTarget = state.targetYaw;
      else if (state.moving) yawTarget = Math.atan2(dx, dz);
      else yawTarget = Math.atan2(-state.x, -state.z); // face the board
      state.yaw += angleDelta(state.yaw, yawTarget) * smoothing(dt, 8);
      state.scale += (state.targetScale - state.scale) * k;
      stand.position.set(state.x, state.pose ? 0 : FLOOR_Y, state.z);
      stand.rotation.y = state.yaw;
      stand.scale.setScalar(state.scale);
      label.position.x = state.x;
      label.position.z = state.z;
      label.position.y = state.headY * state.scale * 1.05 + 0.3;

      const hasHands = Boolean(state.pose?.hands?.some(Boolean));
      state.arms.forEach((arm) => { arm.visible = !hasHands; });
      gloves.forEach((glove, index) => {
        const hand = state.pose?.hands?.[index];
        glove.visible = Boolean(hand);
        if (hand) {
          glove.position.set(hand[0], hand[1], hand[2]);
          glove.scale.setScalar(state.scale);
        }
      });

      const showPointer = !state.pose && Array.isArray(state.cursor);
      marker.visible = beam.visible = showPointer;
      if (showPointer) {
        marker.position.set(state.cursor[0], 0.04, state.cursor[1]);
        const from = new THREE.Vector3(state.x, 1.25 * state.scale, state.z);
        const to = new THREE.Vector3(state.cursor[0], 0.05, state.cursor[1]);
        beam.geometry.setFromPoints([from, to]);
      }

      if (state.mixer) {
        play(chooseAnimation({ moving: state.moving, holding: state.holding }));
        state.mixer.update(dt);
      } else if (state.usesModel === false && state.ready) {
        stand.position.y = FLOOR_Y + (state.moving ? Math.abs(Math.sin(performance.now() / 120)) * 0.05 : 0);
      }
    },
    dispose() {
      state.disposed = true;
      if (state.countedModel) modelAvatars = Math.max(0, modelAvatars - 1);
      state.mixer?.stopAllAction();
      root.removeFromParent();
      label.material.map?.dispose();
      label.material.dispose();
      marker.material.dispose();
      beam.geometry.dispose();
      beam.material.dispose();
      gloves.forEach((glove) => { glove.userData.own.forEach((item) => item.dispose()); });
      state.primitiveOwn?.forEach((item) => item.dispose?.());
    },
  };
  return api;
}

export const avatarBudget = { MAX_MODEL_AVATARS };
