import * as THREE from "three";
import { addPerson, ball, box, cyl, hiddenPart, part, shownPart } from "./industrial-stations.js";

// The idle counter shown behind the scenario menu before a scenario is chosen (the customer, a few props). It belongs to no scenario.

function sheet(group, color, [x, y, z], size = [0.34, 0.02, 0.24], rot) {
  return part(group, box(...size), color, [x, y, z], { rough: 0.9, rot });
}

function decorateCounter(group) {
  const customer = new THREE.Group();
  addPerson(customer, [-1.1, 0.0, 0.35], "#73e7d4");
  shownPart(group, "customer", customer);
  const atDoor = new THREE.Group();
  addPerson(atDoor, [1.25, 0.0, 0.35], "#73e7d4");
  hiddenPart(group, "customerAtDoor", atDoor);
  part(group, box(0.5, 1.2, 0.06), "#6f5438", [1.4, 0.6, 0.0], { rough: 0.8 }); // quiet-room door
  part(group, ball(0.03), "#d8edf3", [1.2, 0.6, 0.04], { metal: 0.6 });
  sheet(group, "#f2f6fc", [0.0, 1.03, 0.12]); // notepad / identity form
  sheet(group, "#ffd23f", [0.3, 1.03, 0.15], [0.3, 0.02, 0.22]); // options sheet
  part(group, box(0.5, 0.35, 0.03), "#ff9d59", [-0.9, 1.35, -0.35], { basic: true }); // offer poster
  part(group, box(0.1, 0.02, 0.18), "#1b2530", [0.7, 1.03, 0.05]); // phone
}

const decorators = {
  counter: decorateCounter,
};

export function decorateCoreStation(group, key) {
  decorators[key]?.(group);
}
