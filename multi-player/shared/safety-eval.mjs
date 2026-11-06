// The site-safety checklist, with no Three.js so tests can run it. Items are { part, x, z, on }.

export const SAFETY_CHECKS = [
  ["exit", "exit sign"],
  ["extinguisher", "extinguisher"],
  ["firstaid", "first-aid kit"],
  ["muster", "muster point"],
];
export const BARRIER_REACH = 1.1; // a barrier this close (metres on the board) guards a hazard beacon

export function safetyStatus(items) {
  const present = (part) => items.some((item) => item.part === part);
  const barriers = items.filter((item) => item.part === "barrier");
  const beacons = items.filter((item) => item.part === "beacon");
  return {
    checks: SAFETY_CHECKS.map(([part, label]) => ({ part, label, ok: present(part) })),
    beacons: beacons.length,
    unguarded: beacons.filter((item) => !barriers.some((barrier) => Math.hypot(barrier.x - item.x, barrier.z - item.z) < BARRIER_REACH)).length,
    active: beacons.filter((item) => item.on).length,
  };
}
