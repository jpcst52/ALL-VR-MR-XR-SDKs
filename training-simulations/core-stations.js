import * as THREE from "three";
import { addPerson, ball, box, cyl, hiddenPart, part, shownPart } from "./industrial-stations.js";

// Extra props and tasks-revealed parts for the business and research work areas built in app.js.
// Coordinates match the hotspot positions in core-tasks.js.

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

function decorateSafety(group) {
  const tape = new THREE.Group();
  for (let index = 0; index < 8; index += 1) {
    part(tape, box(0.16, 0.05, 0.02), index % 2 ? "#1c232c" : "#ffd23f", [-0.56 + index * 0.16, 0.62, 0.25], { basic: true });
  }
  hiddenPart(group, "tape", tape);
  part(group, box(0.4, 1.0, 0.3), "#3b5a78", [-1.3, 0.5, -0.3], { rough: 0.7 }); // PPE locker
  part(group, box(0.36, 0.3, 0.26), "#d33a2c", [1.3, 0.15, -0.2], { rough: 0.6 }); // spill kit
  addPerson(group, [1.2, 0.0, 0.3], "#ff9d59"); // supervisor
  sheet(group, "#f2f6fc", [-1.2, 0.9, 0.2], [0.3, 0.38, 0.02]); // incident log board
  part(group, cyl(0.05, 0.05, 0.9, 8), "#8c9aa8", [-1.4, 0.45, 0.6]); // post for the people nearby
  addPerson(group, [-1.2, 0.0, 0.6], "#7ab6ff"); // people nearby
}

function decorateDelivery(group) {
  const quarantine = new THREE.Group();
  part(quarantine, box(0.7, 0.02, 0.6), "#ffd23f", [-1.3, 0.02, 0.45], { basic: true, opacity: 0.8 });
  hiddenPart(group, "quarantine", quarantine);
  addPerson(group, [-1.4, 0.0, 0.5], "#ff9d59"); // manager
  part(group, box(0.22, 0.14, 0.16), "#1b2530", [0.9, 0.2, 0.4], { metal: 0.3 }); // camera
  part(group, box(0.9, 0.9, 0.04), "#8c9aa8", [-1.5, 0.45, 0.0], { metal: 0.4 }).userData.hideWhenDressed = "props/steel_frame_shelves_01.glb"; // shelving (the shelf unit replaces it)
}

function decorateConsent(group) {
  addPerson(group, [-1.3, 0.0, 0.3], "#7ab6ff"); // participant
  sheet(group, "#9fd3ff", [0.25, 0.99, 0.1], [0.34, 0.02, 0.42]); // consent form
  part(group, box(0.18, 0.06, 0.12), "#1b2530", [0.0, 1.0, 0.5], { metal: 0.4 }); // recorder
  part(group, box(0.5, 0.35, 0.04), "#33566a", [1.35, 0.95, -0.3]); // services desk sign
  const signed = new THREE.Group();
  part(signed, box(0.26, 0.012, 0.03), "#1a2a6b", [0.25, 1.012, 0.2], { basic: true });
  hiddenPart(group, "signed", signed);
}

function decorateSample(group) {
  sheet(group, "#f2f6fc", [-1.2, 0.9, 0.2], [0.34, 0.44, 0.02]); // protocol board
  part(group, box(0.4, 0.04, 0.3), "#6f5438", [1.2, 0.85, 0.2], { rough: 0.9 }); // log book desk
  sheet(group, "#d8edf3", [1.2, 0.89, 0.2], [0.3, 0.02, 0.22]);
  addPerson(group, [1.4, 0.0, 0.6], "#7ab6ff"); // team member
  part(group, box(0.4, 0.04, 0.3), "#3a4a5a", [-0.8, 0.5, 0.5]); // shared table
  part(group, box(0.34, 0.2, 0.24), "#d8edf3", [0.2, 0.2, 0.7], { rough: 0.8 }); // unapproved cooler
  part(group, cyl(0.2, 0.2, 0.2, 18), "#b9c3cc", [0.0, 1.3, 0.1], { opacity: 0.5 }); // sealed bag
}

function decorateField(group) {
  sheet(group, "#f2f6fc", [-0.9, 1.0, 0.2], [0.3, 0.38, 0.02]); // protocol card / notebook
  addPerson(group, [-1.4, 0.0, 0.4], "#7ab6ff"); // visitor
  part(group, cyl(0.03, 0.03, 0.7, 8), "#8c9aa8", [-0.9, 0.35, 0.2]); // stand
}

const decorators = {
  counter: decorateCounter,
  safety: decorateSafety,
  delivery: decorateDelivery,
  consent: decorateConsent,
  sample: decorateSample,
  field: decorateField,
};

export function decorateCoreStation(group, key) {
  decorators[key]?.(group);
}
