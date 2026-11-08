// Which way does current flow through each live part? Pure (no Three.js) so tests can run it.
// The circuit model in shared/circuit-eval.mjs says which parts are on a loop with the battery; this adds a direction for each,
// so v2 can animate dots along the parts. Conventional current leaves the battery's + terminal (its +x end) and returns to the - end.
// Direction +1 means "from the part's -x end to its +x end", -1 the opposite, 0 means no drawn flow (not live, or no clear direction).
import { terminalPoints, CONTACT_DISTANCE } from "../shared/circuit-eval.mjs";

export function flowDirections(items, evaluation) {
  const result = new Array(items.length).fill(0);
  if (evaluation?.state !== "closed") return result;
  const battery = items.findIndex((item) => item.part === "battery");
  if (battery < 0) return result;

  const points = items.flatMap(terminalPoints);
  const parent = points.map((_, index) => index);
  const find = (i) => { while (parent[i] !== i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; };
  for (let a = 0; a < points.length; a += 1) {
    for (let b = a + 1; b < points.length; b += 1) {
      if ((a >> 1) === (b >> 1)) continue;
      if (Math.hypot(points[a][0] - points[b][0], points[a][1] - points[b][1]) <= CONTACT_DISTANCE) parent[find(a)] = find(b);
    }
  }
  const live = [...evaluation.live].filter((index) => index !== battery);
  const start = find(battery * 2 + 1);
  const depth = new Map([[start, 0]]);
  const queue = [start];
  while (queue.length) {
    const node = queue.shift();
    for (const index of live) {
      const a = find(index * 2); const b = find(index * 2 + 1);
      const other = a === node ? b : b === node ? a : null;
      if (other !== null && !depth.has(other)) { depth.set(other, depth.get(node) + 1); queue.push(other); }
    }
  }
  result[battery] = 1; // inside the cell the current goes from - to +
  for (const index of live) {
    const da = depth.get(find(index * 2)); const db = depth.get(find(index * 2 + 1));
    if (da === undefined || db === undefined || da === db) continue;
    result[index] = da < db ? 1 : -1;
  }
  return result;
}

// Where dot number `k` of `count` is along a part at time `t`: a position u in [-reach, reach] along the part's axis.
// `dir` is from flowDirections; `speed` is in part-lengths per second.
export function dotOffset(k, count, t, dir, reach, speed = 0.45) {
  const phase = (((k / count) + t * speed * dir) % 1 + 1) % 1;
  return (phase * 2 - 1) * reach;
}
