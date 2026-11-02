// Pure evaluation of the shared hydrostatic-test rig (no THREE import, so it is unit-testable in Node).
// Teaching model for safety awareness only. It is not a test procedure, a code check, or a certification.
//
// An item is { part, position: [x, y, z], rotation: [rx, ry, rz], on }. Only x/z and the y rotation matter.
// Parts connect by proximity (like the Circuit Lab terminals): ports that touch are joined.

export const CONTACT = 0.3; // port-to-port distance that counts as connected
export const CAP_DISTANCE = 0.28; // blind flange centre to nozzle centre
export const GAUGE_REACH = 0.5; // gauge centre to the nearest line port
export const VENT_REACH = [0.8, 0.65]; // vent must sit within this box (local x, z) around the exchanger

// Local [x, z, height] of each connection port, in the part's own frame (x runs along the part).
export const PORTS = {
  tank: [[0.35, 0, 0.25]],
  pump: [[-0.4, 0, 0.25], [0.4, 0, 0.25]],
  valve: [[-0.3, 0, 0.25], [0.3, 0, 0.25]],
  pipe: [[-0.4, 0, 0.25], [0.4, 0, 0.25]],
  exchanger: [[-0.9, 0, 0.3], [0.9, 0, 0.3]],
};

// The two top nozzles of the exchanger (local x, z, height of the flange face).
export const NOZZLES = [[-0.5, 0, 0.7], [0.5, 0, 0.7]];

const LINE_PARTS = new Set(["tank", "pump", "valve", "pipe", "exchanger"]);

export function toWorld(item, [lx, lz]) {
  const angle = item.rotation?.[1] ?? 0;
  const c = Math.cos(angle);
  const s = Math.sin(angle);
  return [item.position[0] + lx * c + lz * s, item.position[2] - lx * s + lz * c];
}

const dist = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);

export function evaluateHydro(items) {
  const ports = items.map((item) => (PORTS[item.part] ?? []).map((port) => toWorld(item, port)));
  const touching = items.map((item) => (PORTS[item.part] ?? []).map(() => false));
  const adjacent = items.map(() => new Set());
  for (let a = 0; a < items.length; a += 1) {
    for (let b = a + 1; b < items.length; b += 1) {
      ports[a].forEach((pa, ia) => ports[b].forEach((pb, ib) => {
        if (dist(pa, pb) > CONTACT) return;
        touching[a][ia] = true;
        touching[b][ib] = true;
        adjacent[a].add(b);
        adjacent[b].add(a);
      }));
    }
  }

  const find = (part) => items.map((item, index) => (item.part === part ? index : -1)).filter((index) => index >= 0);
  const exchangerIndex = find("exchanger")[0] ?? -1;
  const exchanger = exchangerIndex >= 0 ? items[exchangerIndex] : null;

  // Blind flanges over each nozzle.
  const nozzleCapped = [false, false];
  if (exchanger) {
    const flanges = find("blind").map((index) => [items[index].position[0], items[index].position[2]]);
    NOZZLES.forEach((nozzle, side) => {
      const spot = toWorld(exchanger, nozzle);
      nozzleCapped[side] = flanges.some((flange) => dist(flange, spot) <= CAP_DISTANCE);
    });
  }
  const capped = nozzleCapped.filter(Boolean).length;

  // Vent valve near the exchanger, and whether it is open.
  let ventNear = false;
  let ventOpen = false;
  if (exchanger) {
    const angle = exchanger.rotation?.[1] ?? 0;
    const c = Math.cos(angle);
    const s = Math.sin(angle);
    for (const index of find("vent")) {
      const dx = items[index].position[0] - exchanger.position[0];
      const dz = items[index].position[2] - exchanger.position[2];
      const lx = dx * c - dz * s;
      const lz = dx * s + dz * c;
      if (Math.abs(lx) <= VENT_REACH[0] && Math.abs(lz) <= VENT_REACH[1]) {
        ventNear = true;
        if (items[index].on) ventOpen = true;
      }
    }
  }

  // Fill path: tank -> (pipe / pump / valve) -> exchanger. Closed valves block flow.
  const tanks = find("tank");
  const reach = (openOnly) => {
    const seen = new Set();
    const queue = tanks.map((index) => [index, 0]);
    let hit = null;
    while (queue.length) {
      const [index, throughValve] = queue.shift();
      const key = index * 2 + throughValve;
      if (seen.has(key)) continue;
      seen.add(key);
      if (index === exchangerIndex) hit = hit === null || throughValve ? throughValve : hit;
      for (const next of adjacent[index]) {
        const part = items[next].part;
        if (!LINE_PARTS.has(part) || (part === "tank" && !tanks.includes(next))) continue;
        if (part === "valve" && openOnly && !items[next].on) continue;
        queue.push([next, throughValve || (part === "valve" ? 1 : 0)]);
      }
    }
    return hit; // null = not reached, 0 = reached with no valve, 1 = reached through a valve
  };
  const flowing = exchanger ? reach(true) : null;
  const joined = exchanger ? reach(false) : null;
  const fillOk = flowing === 1;

  // The pump must touch a line part; the gauge sits near any line port.
  const pumpOk = find("pump").some((index) => adjacent[index].size > 0);
  const gaugeOk = find("gauge").some((index) => {
    const spot = [items[index].position[0], items[index].position[2]];
    return ports.some((list, other) => LINE_PARTS.has(items[other].part) && list.some((port) => dist(port, spot) <= GAUGE_REACH));
  });

  const checks = [
    { id: "exchanger", label: "Heat exchanger placed", ok: Boolean(exchanger) },
    { id: "blinds", label: "Both nozzles capped with blind flanges", ok: capped === 2 },
    { id: "vent", label: "Vent valve at the top, open", ok: ventNear && ventOpen },
    { id: "fill", label: "Fill path from tank, valve open", ok: fillOk },
    { id: "pump", label: "Pump connected to the line", ok: pumpOk },
    { id: "gauge", label: "Gauge near the line", ok: gaugeOk },
  ];

  let status;
  if (!exchanger) status = "Place the heat exchanger";
  else if (capped === 0) status = "Blind a nozzle: both top nozzles are open";
  else if (capped === 1) status = "Blind a nozzle: one top nozzle is still open";
  else if (!ventNear) status = "Add a vent valve near the top";
  else if (!ventOpen) status = "Vent is closed: air will be trapped";
  else if (!tanks.length) status = "Add a tank to fill from";
  else if (joined === null) status = "Connect the tank to the exchanger with pipe";
  else if (flowing === null) status = "Open the fill valve";
  else if (!fillOk) status = "Add a valve to the fill line";
  else if (!pumpOk) status = "Connect a pump to the line";
  else if (!gaugeOk) status = "Add a gauge near the line";
  else status = "Ready to fill";

  return { checks, status, ready: checks.every((check) => check.ok), touching, nozzleCapped, exchangerIndex };
}

export function summaryText(result) {
  return [result.status, ...result.checks.map((check) => `${check.ok ? "✓" : "✗"} ${check.label}`)].join("\n");
}
