// Lab v2 themes. They keep everything from v1 that the room protocol and the checks rely on (ids, parts, examples, toolbox labels,
// the circuit and safety rules) and replace how parts are built, how they react to state, and the live effects.
import * as THREE from "three";
import { themes as v1 } from "../shared/themes.js";
import { evaluateCircuit } from "../shared/circuit-eval.mjs";
import { flowDirections, dotOffset } from "./flow.mjs";
import { buildCircuitPart2, buildSafetyPart2, Y0 } from "./parts2.js";

let fx = null;
export function attachFx(instance) { fx = instance; }

const lerp = (a, b, t) => a + (b - a) * t;
const smooth = (current, target, dt, rate = 8) => lerp(current, target, 1 - Math.exp(-dt * rate));
const GREEN = "#2ee6a0"; const GREY = "#5d6b7f";
const hotColor = new THREE.Color();
const cold = new THREE.Color("#5a4a3a"); const warm = new THREE.Color("#ffd9a0"); const white = new THREE.Color("#fffbf0");

// Same sentences as v1's circuit summary (the headset toolbox and the checks read them).
function circuitSummary(objects, evaluation) {
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
}

const wireHeight = (u) => 0.11 * Math.min(1, Math.max(0, (0.6 - Math.abs(u)) / 0.4));

export const circuits2 = {
  ...v1.circuits,
  title: "Circuit Lab",
  build: buildCircuitPart2,
  applyState(object) {
    const lever = object.group.userData.lever;
    if (lever) lever.userData.target = object.data.on ? 0 : 0.62;
  },
  lastTime: null,
  update(objects, time) {
    const dt = Math.min(0.05, Math.max(0, time - (this.lastTime ?? time)));
    this.lastTime = time;
    const list = [...objects.values()];
    const items = list.map((object) => ({
      part: object.data.part,
      position: object.group.position.toArray(),
      rotation: [object.group.rotation.x, object.group.rotation.y, object.group.rotation.z],
      on: object.data.on,
    }));
    const evaluation = evaluateCircuit(items);
    const flow = flowDirections(items, evaluation);
    fx?.begin();
    list.forEach((object, index) => {
      const data = object.group.userData;
      const { x, y, z } = object.group.position;
      const angle = object.group.rotation.y;
      const cos = Math.cos(angle); const sin = Math.sin(angle);
      const live = evaluation.live.has(index);
      const level = live ? evaluation.brightness : 0;
      const reach = data.reach ?? 0.4;
      if (fx) {
        for (const side of [0, 1]) {
          const s = side === 0 ? -1 : 1;
          fx.ring(x + cos * reach * s, y + 0.075, z - sin * reach * s, evaluation.touching[index * 2 + side] ? GREEN : GREY);
        }
        if (flow[index] && live) {
          const count = Math.max(3, Math.round((reach * 2) / 0.17));
          for (let k = 0; k < count; k += 1) {
            const u = dotOffset(k, count, time, flow[index], reach, 0.35 + 0.35 * evaluation.brightness);
            const lift = Y0 + 0.04 + (object.data.part === "wire" ? wireHeight(u) : 0);
            fx.dot(x + cos * u, y + lift, z - sin * u);
          }
        }
      }
      // switch lever eases to its target
      if (data.lever) data.lever.rotation.z = smooth(data.lever.rotation.z, data.lever.userData.target ?? (object.data.on ? 0 : 0.62), dt, 14);
      if (data.glass) {
        data.lit = smooth(data.lit ?? 0, level, dt, 9);
        const lit = data.lit;
        const flicker = lit > 0.05 ? 1 + 0.015 * Math.sin(time * 37 + index) : 1;
        data.glass.emissiveIntensity = lit * 1.5;
        hotColor.copy(cold).lerp(warm, Math.min(1, lit * 2)).lerp(white, Math.max(0, lit * 2 - 1));
        data.filament.color.copy(hotColor).multiplyScalar(1 + lit * 2.5);
        data.halo.material.opacity = Math.min(1, lit * 0.95) * flicker;
        data.halo.scale.setScalar((0.7 + lit * 0.9) * flicker);
        if (fx && lit > 0.02) fx.emitter(x, y + data.emitter.y, z, data.emitter.color, lit * 1.8);
      }
      if (data.fan) {
        data.fanSpeed = smooth(data.fanSpeed ?? 0, live ? 14 * evaluation.brightness : 0, dt, 3);
        data.fan.rotation.x += data.fanSpeed * dt;
        data.blur.material.opacity = Math.min(0.32, data.fanSpeed / 40);
      }
    });
    fx?.end();
    return circuitSummary(objects, evaluation);
  },
};

// Beacon visuals follow the same rule as v1: flashing when on. Ten seconds of speaking is not needed: the summary is v1's.
export const safety2 = {
  ...v1.safety,
  build: buildSafetyPart2,
  applyState(object) {
    object.group.userData.active = Boolean(object.data.on);
  },
  lastTime: null,
  update(objects, time) {
    const dt = Math.min(0.05, Math.max(0, time - (this.lastTime ?? time)));
    this.lastTime = time;
    fx?.begin();
    for (const object of objects.values()) {
      const data = object.group.userData;
      if (!data.dome) continue;
      data.glow = smooth(data.glow ?? 0, data.active ? 1 : 0, dt, 10);
      const pulse = data.active ? 0.5 + 0.5 * Math.sin(time * 6) : 0;
      const g = data.glow;
      data.dome.emissiveIntensity = 0.1 + (pulse * 1.5 + 0.2) * g;
      data.rotor.rotation.y += (data.active ? 5.2 : 0) * dt;
      data.beam.opacity = g * (0.38 + 0.2 * pulse);
      data.halo.material.opacity = g * (0.35 + 0.5 * pulse);
      data.pool.material.opacity = g * (0.25 + 0.3 * pulse);
      if (fx && g > 0.02) fx.emitter(object.group.position.x, object.group.position.y + data.emitter.y, object.group.position.z, data.emitter.color, g * (0.6 + pulse * 1.6));
    }
    fx?.end();
    return this.summary(objects);
  },
};

export const themes2 = { circuits: circuits2, safety: safety2 };
