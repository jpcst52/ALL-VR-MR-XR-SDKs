// Life science practice. DRAFT FOR FACULTY REVIEW.
//
// "Circulation Builder" was drafted before the biology faculty partner described his own project, so it is built
// only from widely taught introductory (general) biology: the four heart chambers, the pulmonary and systemic
// circuits, arteries and veins, and the textbook red/blue diagram convention for oxygen-rich and oxygen-poor blood.
// It is educational general biology, not clinical or medical guidance, and it cites no sources.
// Expect the content, wording, and level to change once the faculty member reviews it.
//
// Same data format as industrial-scenarios.js (hotspots with ok()/bad(), `after`, `critical`, `reveal`/`hide`).
// In this category `critical` marks a key misconception rather than a safety miss. The work-area model and its
// onStart states ("t2", "t3", "t4") are in biology-stations.js.
const ok = (id, label, at, say, extra = {}) => ({ id, label, at, correct: true, say, ...extra });
const bad = (id, label, at, say, extra = {}) => ({ id, label, at, correct: false, say, ...extra });
const task = (prompt, principle, hotspots, extra = {}) => ({ prompt, principle, hotspots, ...extra });

export const biologyScenarios = [
  {
    id: "circulation-builder",
    category: "biology",
    draft: true,
    title: "Circulation Builder",
    duration: "5 min",
    setting: "A model of the human heart, lungs, and body (draft for faculty review)",
    description: "Place the four heart chambers, connect the pulmonary and systemic circuits in order, then colour each vessel oxygen-rich or oxygen-poor.",
    object: "circulation",
    briefing: [
      "Draft for faculty review: general biology only, not clinical or medical guidance",
      "Front view: the heart's right side is on YOUR left, as in most textbook diagrams",
      "Diagram colours: red = oxygen-rich, blue = oxygen-poor (real blood is always red)",
    ],
    steps: [
      task("The heart model is empty. Place the four chambers. You are looking at the front of the body, so the heart's right side is on your left.",
        "The heart has two atria on top and two ventricles below; the right side handles oxygen-poor blood and the left side oxygen-rich blood.", [
          ok("ra", "Right atrium", [-0.24, 1.18, 0.22], "The right atrium sits upper left in this front view. It receives oxygen-poor blood from the body.", { reveal: "ra" }),
          ok("rv", "Right ventricle", [-0.2, 0.84, 0.24], "The right ventricle sits below the right atrium. It pumps blood to the lungs.", { reveal: "rv" }),
          ok("la", "Left atrium", [0.24, 1.18, 0.22], "The left atrium sits upper right in this front view. It receives oxygen-rich blood from the lungs.", { reveal: "la" }),
          ok("lv", "Left ventricle", [0.2, 0.8, 0.24], "The left ventricle has the thickest wall: it pumps blood out to the whole body.", { reveal: "lv" }),
          bad("mirror", "Left side on your left", [-1.0, 1.45, 0.2], "In a front view the body faces you, so its left side is on your right. Diagrams are drawn this way.", { critical: true }),
        ]),
      task("Oxygen-poor blood is returning from the body. Connect the pulmonary circuit in the order the blood travels.",
        "Pulmonary circuit: body veins, right atrium, right ventricle, pulmonary artery, lungs. Arteries carry blood away from the heart.", [
          ok("venae", "Venae cavae to right atrium", [-0.75, 0.72, 0.18], "The superior and inferior venae cavae bring oxygen-poor blood from the body into the right atrium.", { reveal: "venae" }),
          ok("tricuspid", "Tricuspid valve", [-0.24, 1.0, 0.3], "The tricuspid valve opens so blood flows from the right atrium down into the right ventricle, and closes to stop backflow.", { after: ["venae"], reveal: "tricuspid", early: "Blood has not reached the right atrium yet. Start where it enters the heart." }),
          ok("pa", "Pulmonary artery to lungs", [-0.32, 1.42, 0.18], "The right ventricle pumps blood through the pulmonary artery to the lungs. It is an artery because it leads away from the heart.", { after: ["tricuspid"], reveal: "pa", early: "The pulmonary artery is fed by the right ventricle, so the valve between the right atrium and ventricle comes first." }),
          ok("lungs", "Lung capillaries", [-0.55, 1.68, 0.1], "In the lung capillaries the blood releases carbon dioxide and picks up oxygen.", { after: ["pa"], reveal: "exchange", early: "Blood reaches the lung capillaries through the pulmonary artery. Connect that first." }),
          bad("rvAorta", "Right ventricle to aorta", [0.62, 1.38, 0.15], "The aorta leaves the LEFT ventricle. The right ventricle sends blood only to the lungs, through the pulmonary artery.", { critical: true }),
        ], { onStart: "t2" }),
      task("Oxygen-rich blood is leaving the lungs. Connect the systemic circuit in the order the blood travels.",
        "Systemic circuit: pulmonary veins, left atrium, left ventricle, aorta, body capillaries. Veins carry blood back toward the heart.", [
          ok("pv", "Pulmonary veins to left atrium", [0.48, 1.42, 0.18], "The pulmonary veins bring oxygen-rich blood from the lungs into the left atrium. They are veins because they lead toward the heart.", { reveal: "pv" }),
          ok("mitral", "Mitral valve", [0.24, 1.0, 0.3], "The mitral (bicuspid) valve lets blood pass from the left atrium into the left ventricle and stops it flowing back.", { after: ["pv"], reveal: "mitral", early: "Blood has not reached the left atrium yet. Connect the vessels from the lungs first." }),
          ok("aorta", "Aorta from left ventricle", [0.78, 1.08, 0.18], "The left ventricle pumps blood into the aorta, the body's largest artery.", { after: ["mitral"], reveal: "aorta", early: "The aorta is fed by the left ventricle, so the valve between the left atrium and ventricle comes first." }),
          ok("body", "Body capillaries", [0.05, 0.36, 0.34], "In the body's capillaries oxygen moves out to the tissues and carbon dioxide moves into the blood. The blood then returns through veins.", { after: ["aorta"], reveal: "tissue", early: "Blood reaches the body's capillaries through the aorta and its branches. Connect the aorta first." }),
          bad("shortcut", "Lungs straight to the body", [1.08, 1.62, 0.12], "Blood from the lungs returns to the left side of the heart first. The left ventricle supplies the pressure that sends it around the body.", { critical: true }),
        ], { onStart: "t3" }),
      task("The whole loop is connected but the vessels are uncoloured. Colour each one using the diagram convention: red for oxygen-rich, blue for oxygen-poor.",
        "Name vessels by direction (arteries leave the heart, veins return to it), not by colour: the pulmonary artery carries oxygen-poor blood and the pulmonary veins carry oxygen-rich blood.", [
          ok("aortaRed", "Aorta: oxygen-rich (red)", [0.82, 0.86, 0.18], "Correct. The aorta carries oxygen-rich blood from the left ventricle to the body.", { reveal: "aortaRed", hide: "aortaGrey" }),
          ok("venaeBlue", "Venae cavae: oxygen-poor", [-0.78, 0.52, 0.18], "Correct. The venae cavae return oxygen-poor blood from the body (blue in diagrams; the real blood is dark red).", { reveal: "venaeBlue", hide: "venaeGrey" }),
          ok("paBlue", "Pulmonary artery: O2-poor", [-0.32, 1.42, 0.18], "Correct. The pulmonary artery is the artery that carries oxygen-poor blood, from the right ventricle to the lungs.", { reveal: "paBlue", hide: "paGrey" }),
          ok("pvRed", "Pulmonary veins: O2-rich", [0.48, 1.42, 0.18], "Correct. The pulmonary veins are the veins that carry oxygen-rich blood, from the lungs to the left atrium.", { reveal: "pvRed", hide: "pvGrey" }),
          bad("rule", "Rule: every artery is red", [-1.08, 1.62, 0.2], "That rule fails for the pulmonary artery, which carries oxygen-poor blood. Arteries are defined by direction: away from the heart.", { critical: true }),
        ], { onStart: "t4" }),
    ],
  },
];
