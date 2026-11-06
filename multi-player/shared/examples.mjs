// Ready-made scenes for the Circuit Lab and the Site Safety Layout. Pure data (no Three.js) so tests can check that every
// circuit really closes and every safety layout really passes its checklist. Positions are board metres; the board is a disc
// of radius 3.4. `on` means a switch is closed (circuit) or a beacon is flashing (safety).

const p = (part, x, z, rotation = 0, on) => ({ part, position: [x, 0.08, z], rotation: [0, rotation, 0], ...(on === undefined ? {} : { on }) });
const HALF = Math.PI / 2;

// A rectangular loop: three parts in series along the back (z = -0.6), a wire down each side, and the front (z = 0.6).
const sides = [p("wire", 1.2, 0, HALF), p("wire", -1.2, 0, HALF)];

export const circuitExamples = [
  {
    id: "simple",
    label: "Simple loop",
    blurb: "Battery, switch and bulb in one loop.",
    items: [
      p("battery", -0.8, -0.6), p("switch", 0, -0.6, 0, true), p("bulb", 0.8, -0.6),
      ...sides, p("wire", 0.6, 0.6), p("wire", -0.6, 0.6),
    ],
  },
  {
    id: "series",
    label: "Two bulbs in series",
    blurb: "Both bulbs share one loop, with the switch on the front edge.",
    items: [
      p("battery", -0.8, -0.6), p("bulb", 0, -0.6), p("bulb", 0.8, -0.6),
      ...sides, p("wire", 0.6, 0.6), p("switch", -0.6, 0.6, 0, true),
    ],
  },
  {
    id: "dimmer",
    label: "Dimmed bulb",
    blurb: "A resistor in the loop makes the bulb dimmer.",
    items: [
      p("battery", -0.8, -0.6), p("resistor", 0, -0.6), p("bulb", 0.8, -0.6),
      ...sides, p("wire", 0.6, 0.6), p("switch", -0.6, 0.6, 0, true),
    ],
  },
  {
    id: "motor",
    label: "Motor and bulb",
    blurb: "A motor runs with a bulb as the 'power on' light.",
    items: [
      p("battery", -0.8, -0.6), p("switch", 0, -0.6, 0, true), p("motor", 0.8, -0.6),
      ...sides, p("wire", 0.6, 0.6), p("bulb", -0.6, 0.6),
    ],
  },
];

export const safetyExamples = [
  {
    id: "sample",
    label: "Sample layout",
    blurb: "Exit, extinguisher, first aid, muster point and one guarded hazard.",
    items: [
      p("exit", -2.0, -1.6, 0.4), p("extinguisher", -1.0, -1.6), p("firstaid", 0, -1.6), p("muster", 2.0, -1.2, -0.4),
      p("beacon", 0.6, 0.6, 0, true), p("barrier", 0.6, 1.2),
    ],
  },
  {
    id: "welding",
    label: "Welding bay",
    blurb: "Two exits and two extinguishers, with the hot-work hazard fenced on two sides.",
    items: [
      p("exit", -2.2, -1.4, 0.4), p("exit", 2.2, -1.4, -0.4), p("extinguisher", -1.0, -1.8), p("extinguisher", 1.0, -1.8),
      p("firstaid", 0, -1.9), p("muster", 2.4, 0.9, -0.4),
      p("beacon", -0.8, 0.5, 0, true), p("barrier", -0.8, 1.1), p("barrier", 0, 0.5, HALF),
    ],
  },
  {
    id: "chemical",
    label: "Chemical store",
    blurb: "Two active hazards, each fenced in, with first aid and an extinguisher at each side.",
    items: [
      p("exit", -2.4, -1.5, 0.5), p("extinguisher", -2.0, 1.4), p("extinguisher", 2.0, 1.4), p("firstaid", 0, -1.8),
      p("muster", 2.4, -1.4, -0.4),
      p("beacon", -1.2, 0.2, 0, true), p("barrier", -1.2, 0.9), p("barrier", -1.2, -0.5),
      p("beacon", 1.2, 0.2, 0, true), p("barrier", 1.2, 0.9), p("barrier", 1.2, -0.5),
    ],
  },
  {
    id: "lane",
    label: "Loading lane",
    blurb: "A vehicle lane marked with barriers and a beacon, with exits and first aid either side.",
    items: [
      p("barrier", -0.7, -0.6, HALF), p("barrier", -0.7, 0.8, HALF), p("barrier", 0.7, -0.6, HALF), p("barrier", 0.7, 0.8, HALF),
      p("beacon", 0, -1.0, 0, true),
      p("exit", -2.4, -1.2, 0.4), p("exit", 2.4, 1.4, -0.4), p("extinguisher", -2.2, 0.3), p("extinguisher", 2.2, -1.2),
      p("firstaid", 2.2, 0.3), p("muster", 0, 2.3),
    ],
  },
];
