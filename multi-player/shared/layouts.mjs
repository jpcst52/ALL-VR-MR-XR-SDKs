// Where the environment props go for each example. Pure data (no Three.js) so tests can check every model id.
// Units are metres. x/z are floor positions in the room, `y` lifts a prop onto a bench, `rot` is yaw in degrees.
// The shared board is a disc of radius 3.4 around (0, 0); keep props outside r = 3.9 so the pad stays clear.

const bench = (x, z, rot = 0) => ({ bench: true, x, z, rot });

export const layouts = {
  circuits: {
    title: "Electronics lab",
    floor: "tiles",
    wall: "plaster",
    walls: { back: -7.5, left: -7.5, right: 7.5 },
    benches: [bench(-3.6, -6.5), bench(0, -6.5), bench(3.6, -6.5), bench(-6.6, -2.4, 90)],
    props: [
      { model: "retro_multimeter", x: -3.9, z: -6.5, y: 0.9, rot: 20, scale: 1.3 },
      { model: "classic_laptop", x: -3.1, z: -6.55, y: 0.9, rot: 5, scale: 1 },
      { model: "metal_toolbox", x: 0.4, z: -6.55, y: 0.9, rot: -10, scale: 1.2 },
      { model: "bench_vice_01", x: 3.1, z: -6.5, y: 0.9, rot: 0, scale: 1.4 },
      { model: "retro_multimeter", x: -6.6, z: -2.0, y: 0.9, rot: 80, scale: 1.3 },
      { model: "metal_stool_01", x: -2.0, z: -5.2, rot: 30, scale: 1.1 },
      { model: "metal_stool_01", x: 2.2, z: -5.0, rot: -40, scale: 1.1 },
      { model: "tool_cart", x: 6.0, z: -3.6, rot: -90, scale: 1.1 },
      { model: "metal_tool_chest", x: 6.3, z: -1.2, rot: -90, scale: 1.4 },
      { model: "korean_fire_extinguisher_01", x: 6.9, z: 0.9, rot: -90, scale: 1.2 },
      { model: "fire_alarm", x: 6.95, z: 0.9, y: 1.5, rot: -90, scale: 2 },
      { model: "hanging_industrial_lamp", x: -2.2, z: -4.2, y: 2.6, rot: 0, scale: 1 },
      { model: "hanging_industrial_lamp", x: 2.2, z: -4.2, y: 2.6, rot: 0, scale: 1 },
      { model: "WetFloorSign_01", x: 4.7, z: 3.9, rot: 35, scale: 1 },
    ],
  },
  safety: {
    title: "Site yard",
    floor: "asphalt",
    wall: "corrugated",
    walls: { back: -8, left: -8, right: 8 },
    benches: [],
    props: [
      { model: "barrel_03", x: -5.6, z: -6.2, rot: 0, scale: 1 },
      { model: "barrel_03", x: -4.9, z: -6.5, rot: 40, scale: 1 },
      { model: "Barrel_01", x: -4.5, z: -5.7, rot: 90, scale: 1 },
      { model: "propane_tank", x: -6.4, z: -5.3, rot: 0, scale: 1.3 },
      { model: "propane_tank", x: -6.9, z: -4.7, rot: 70, scale: 1.3 },
      { model: "wooden_crate_01", x: 5.2, z: -6.4, rot: 10, scale: 1.4 },
      { model: "wooden_crate_01", x: 5.3, z: -6.4, y: 0.57, rot: -20, scale: 1.2 },
      { model: "wooden_crate_01", x: 6.6, z: -5.7, rot: 70, scale: 1.4 },
      { model: "industrial_pastic_container", x: 4.2, z: -6.7, rot: 0, scale: 1.4 },
      { model: "portable_generator", x: 6.6, z: -2.6, rot: -90, scale: 1.3 },
      { model: "tool_cart", x: -6.5, z: -1.0, rot: 90, scale: 1.1 },
      { model: "korean_fire_extinguisher_01", x: -7.3, z: 2.0, rot: 90, scale: 1.3 },
      { model: "fire_alarm", x: -7.9, z: 2.0, y: 1.6, rot: 90, scale: 2 },
      { model: "security_light", x: 0, z: -7.6, y: 3.2, rot: 0, scale: 1.5 },
      { model: "security_light", x: -4, z: -7.6, y: 3.2, rot: 0, scale: 1.5 },
      { model: "portable_searchlight", x: 3.0, z: -6.9, rot: 15, scale: 2.2 },
      { model: "WetFloorSign_01", x: 4.4, z: 3.8, rot: 20, scale: 1 },
      { model: "WetFloorSign_01", x: -4.6, z: 3.5, rot: -30, scale: 1 },
    ],
  },
  hydrotest: {
    title: "Test bay",
    floor: "concrete",
    wall: "concrete",
    walls: { back: -8, left: -8, right: 8 },
    benches: [bench(-5.4, -6.9)],
    props: [
      { model: "modular_industrial_pipes_01", x: -1.4, z: -7.55, rot: 0, scale: 1.2 },
      { model: "modular_industrial_pipes_01", x: 1.6, z: -7.55, rot: 0, scale: 1.2 },
      { model: "metal_tool_chest", x: -5.8, z: -6.9, y: 0.9, rot: 10, scale: 1.4 },
      { model: "metal_toolbox", x: -4.6, z: -6.9, y: 0.9, rot: -15, scale: 1.2 },
      { model: "tool_cart", x: -6.8, z: -3.2, rot: 90, scale: 1.1 },
      { model: "portable_generator", x: 6.7, z: -0.4, rot: -90, scale: 1.3 },
      { model: "Barrel_01", x: -6.6, z: 0.8, rot: 0, scale: 1 },
      { model: "barrel_03", x: -6.1, z: 1.4, rot: 70, scale: 1 },
      { model: "korean_fire_extinguisher_01", x: 7.4, z: 2.4, rot: -90, scale: 1.3 },
      { model: "fire_alarm", x: 7.9, z: 2.4, y: 1.6, rot: -90, scale: 2 },
      { model: "hanging_industrial_lamp", x: -2.5, z: -4.6, y: 2.7, rot: 0, scale: 1 },
      { model: "hanging_industrial_lamp", x: 2.5, z: -4.6, y: 2.7, rot: 0, scale: 1 },
      { model: "WetFloorSign_01", x: 4.8, z: 3.9, rot: 35, scale: 1 },
    ],
  },
};

export function modelIdsInLayouts() {
  return [...new Set(Object.values(layouts).flatMap((layout) => layout.props.map((prop) => prop.model)))];
}

// Which primitive stand-in a model gets when it cannot be loaded (world.js draws it).
export function fallbackKind(id) {
  if (/barrel/i.test(id)) return "barrel";
  if (/propane/i.test(id)) return "tank";
  if (/extinguisher|fire_alarm/i.test(id)) return "extinguisher";
  if (/crate|container|chest/i.test(id)) return "crate";
  if (/cart|generator/i.test(id)) return "cart";
  if (/tool|vice|multimeter|laptop|stool|sign/i.test(id)) return "tool";
  if (/pipe/i.test(id)) return "pipe";
  if (/lamp|light/i.test(id)) return "lamp";
  return "generic";
}
