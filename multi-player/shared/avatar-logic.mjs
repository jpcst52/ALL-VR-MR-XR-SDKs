// Pure logic for the participant avatars (no Three.js, so it is unit-tested in Node).
// Everyone derives the same look for a participant from the participant id, so no extra protocol field is needed.

export const CHARACTERS = ["knight", "barbarian", "mage", "rogue"];

// Hard-hat colors follow common site practice (white, yellow, orange, blue, green, red, plus two brand colors).
export const HAT_COLORS = [
  { name: "white", hex: "#f4f6f8" },
  { name: "yellow", hex: "#ffc933" },
  { name: "orange", hex: "#ff7a1a" },
  { name: "blue", hex: "#2f6fe0" },
  { name: "green", hex: "#2fb36b" },
  { name: "red", hex: "#e0343a" },
  { name: "royal", hex: "#003594" },
  { name: "gold", hex: "#ffb81c" },
];

// More model avatars than this and new arrivals get the cheap primitive avatar (keeps the scene under the triangle budget).
export const MAX_MODEL_AVATARS = 8;
// Triangle budget for one whole scene on a Quest-class device.
export const SCENE_TRIANGLE_BUDGET = 120000;

export function hashString(text) {
  let hash = 2166136261;
  for (let index = 0; index < String(text).length; index += 1) {
    hash ^= String(text).charCodeAt(index);
    hash = Math.imul(hash, 16777619) >>> 0;
  }
  return hash >>> 0;
}

export function lookFor(id) {
  const hash = hashString(id);
  return {
    character: CHARACTERS[hash % CHARACTERS.length],
    hat: HAT_COLORS[(hash >>> 4) % HAT_COLORS.length],
  };
}

// Which clip to play: walking while the avatar is moving, a reaching clip while it holds a part, otherwise idle.
export function chooseAnimation({ moving = false, holding = false } = {}) {
  if (holding) return "Interact";
  return moving ? "Walking_A" : "Unarmed_Idle";
}

// Frame-rate independent smoothing factor (0..1) for following a network position.
export function smoothing(dt, rate = 10) {
  return 1 - Math.exp(-Math.max(0, dt) * rate);
}

// Shortest signed angle from a to b, so avatars turn the short way round.
export function angleDelta(from, to) {
  return ((((to - from) % (Math.PI * 2)) + Math.PI * 3) % (Math.PI * 2)) - Math.PI;
}

const finite = (value) => Number.isFinite(value);
const vector3 = (value, limit) => Array.isArray(value) && value.length === 3
  && value.every((item) => finite(item) && Math.abs(item) <= limit);

// Returns a clean pose object, or null when the input is not a usable pose. Shared by the relay and the client.
export function normalizePose(raw, limit = 20) {
  if (!raw || !vector3(raw.head, limit) || !finite(raw.yaw) || Math.abs(raw.yaw) > Math.PI * 4) return null;
  if (!Array.isArray(raw.hands) || raw.hands.length !== 2) return null;
  const hands = raw.hands.map((hand) => (hand === null ? null : vector3(hand, limit) ? hand : undefined));
  if (hands.includes(undefined)) return null;
  return { head: raw.head, yaw: raw.yaw, hands };
}

// Scale an avatar so its eyes meet the reported head height (the VR board is a 0.4 scale model, so heads can sit high).
export function avatarScaleForHead(headY) {
  if (!finite(headY)) return 1;
  return Math.min(1.5, Math.max(0.5, headY / 1.62));
}

// Run a loader; if it throws or rejects, build the fallback instead. Never throws.
export async function loadWithFallback(load, fallback) {
  try {
    return { value: await load(), usedFallback: false };
  } catch (error) {
    return { value: await fallback(error), usedFallback: true, error };
  }
}

// Desktop participants stand on a ring around the board at the angle of their cursor. Two people pointing at the same spot would
// stand inside each other, so push neighbours apart until each pair is at least minGap radians apart (order is kept).
export function spreadAngles(angles, minGap = 0.32, iterations = 40) {
  const count = angles.length;
  if (count < 2 || count * minGap >= Math.PI * 2) return [...angles];
  const tau = Math.PI * 2;
  const order = angles.map((angle, index) => ({ index, angle: ((angle % tau) + tau) % tau })).sort((a, b) => a.angle - b.angle);
  for (let pass = 0; pass < iterations; pass += 1) {
    let moved = false;
    for (let i = 0; i < count; i += 1) {
      const a = order[i];
      const b = order[(i + 1) % count];
      const gap = (i + 1 === count ? b.angle + tau : b.angle) - a.angle;
      if (gap < minGap - 1e-6) {
        const push = (minGap - gap) / 2;
        a.angle -= push;
        b.angle += push;
        moved = true;
      }
    }
    if (!moved) break;
  }
  const result = new Array(count);
  for (const { index, angle } of order) result[index] = angle;
  return result;
}
