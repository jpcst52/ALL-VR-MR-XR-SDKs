// The circuit model, with no Three.js so tests can run it. Terminals that touch are joined, and a part carries current when it
// lies on a loop with the battery.

export const TERMINAL = { battery: 0.4, switch: 0.4, bulb: 0.4, resistor: 0.4, motor: 0.4, wire: 0.6 };
export const CONTACT_DISTANCE = 0.32;

export function terminalPoints(item) {
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
