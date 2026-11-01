// Hands-on safety-awareness tasks. The learner carries out each task by selecting hotspots in the 3D work area.
// This is general awareness practice only; real work follows site permits, procedures, and trained authorities.
//
// Hotspot fields:
//   id, label, at: [x, y, z] in the work-area model's space, say: what happens when it is selected
//   correct: true  -> a required action (all must be completed to finish the task)
//   correct: false -> a wrong action (counts as a mistake)
//   after: [ids]   -> prerequisites; selecting too early is a mistake (message in `early`)
//   critical: true -> a mistake here is a critical safety miss
//   reveal / hide: names of model parts to show or hide; tether: draw a line from the worker to the hotspot
const ok = (id, label, at, say, extra = {}) => ({ id, label, at, correct: true, say, ...extra });
const bad = (id, label, at, say, extra = {}) => ({ id, label, at, correct: false, say, ...extra });
const task = (prompt, principle, hotspots, extra = {}) => ({ prompt, principle, hotspots, ...extra });

export const industrialScenarios = [
  {
    id: "work-at-height",
    category: "industrial",
    title: "Work at height",
    duration: "5 min",
    setting: "A scaffold platform above a plant walkway",
    description: "Check the scaffold, connect to a rated anchor, control dropped tools, and come down when the wind rises.",
    object: "height",
    briefing: ["Work-at-height permit or plan in place", "Harness inspected and sized", "Rescue plan known before you climb"],
    steps: [
      task("Get ready to climb to the platform and replace a light fixture. Do what you must before climbing, then climb.",
        "Verify the platform, permit, and rescue plan before leaving the ground.", [
          ok("tag", "Scaffold tag", [-0.65, 0.55, 0.45], "Green tag: inspected today, safe working load 200 kg."),
          ok("deck", "Guardrails and deck", [0.65, 1.1, 0.4], "Guardrails and toe boards are secure and the deck boards are undamaged."),
          ok("plan", "Permit and rescue plan", [1.35, 0.6, 0.6], "The permit is valid today. Rescue plan: standby person and rescue kit at the base."),
          ok("climb", "Ladder", [1.0, 0.6, 0.1], "You climb with three points of contact, carrying nothing in your hands.", {
            after: ["tag", "deck", "plan"], critical: true,
            early: "You started climbing before the scaffold, permit, and rescue plan were checked. A hidden defect or missing rescue plan can turn a fall into a fatality.",
          }),
        ]),
      task("You are on the platform near an open edge. Protect yourself from a fall.",
        "Connect only to a rated anchor and inspect fall-arrest equipment before each use.", [
          ok("harness", "Harness", [-0.2, 1.35, 0.25], "Webbing, stitching, buckles, and shock absorber are undamaged and in date."),
          bad("rail", "Handrail", [0.3, 1.2, 0.38], "A handrail is not rated for fall arrest. It can bend or detach under a fall load.", { critical: true }),
          bad("tube", "Scaffold tube", [-0.65, 1.4, -0.38], "Scaffold tubing that is not a designated anchor can fail or pull the scaffold over.", { critical: true }),
          ok("anchor", "Overhead anchor beam", [0.0, 1.75, 0.0], "You connect to the rated overhead anchor. Your lanyard now stops a fall.", {
            after: ["harness"], tether: true, early: "Inspect the harness before you rely on it.",
          }),
        ]),
      task("You need your tools on the platform. Keep people below safe from dropped objects.",
        "Keep three points of contact, tether tools, and guard the zone below.", [
          ok("bag", "Tool bag", [0.4, 0.9, 0.25], "The tools are tied to the platform with a lanyard so they cannot fall."),
          ok("zone", "Area below", [0.25, 0.15, 0.62], "A barricade is placed and a ground watcher keeps people out of the drop zone.", { reveal: "zone" }),
          bad("toss", "Coworker below", [0.9, 0.3, 0.45], "A tool thrown to a coworker can be missed and fall on someone.", { critical: true }),
        ]),
      task("Wind picks up and the platform starts to sway. Respond in the right order.",
        "Stop for changing conditions, secure tools, come down, and report.", [
          ok("stop", "Stop work", [-0.2, 1.05, 0.3], "You stop work and hold on to the guardrail."),
          ok("secure", "Tool bag", [0.4, 0.9, 0.25], "Tools are secured so they cannot be blown off.", { after: ["stop"] }),
          ok("descend", "Ladder", [1.0, 0.6, 0.1], "You climb down with three points of contact.", { after: ["secure"], early: "Secure your tools before you leave the platform." }),
          ok("report", "Supervisor", [1.4, 0.95, 0.3], "The supervisor is told. Work resumes only when conditions are safe.", { after: ["descend"] }),
          bad("keep", "Keep working", [0.0, 1.0, 0.25], "Rushing in rising wind raises the risk of a slip or fall.", { critical: true }),
        ], { onStart: "wind" }),
    ],
  },
  {
    id: "control-room-unit",
    category: "industrial",
    title: "Run a unit from the control room",
    duration: "5 min",
    setting: "A refinery process-unit control room",
    description: "Take a handover, rank an alarm flood, react to a field report, and refuse an unauthorized bypass.",
    object: "control",
    briefing: ["Know your alarm priorities", "Use the formal handover and permit log", "Never bypass a trip without authorization"],
    steps: [
      task("You are taking over the unit at shift change. Complete the handover before the outgoing operator leaves.",
        "A complete, two-way handover keeps shift knowledge from being lost.", [
          ok("log", "Shift log", [-0.7, 1.0, 0.1], "The log notes a pump vibration alarm and a compressor trip test scheduled this shift."),
          ok("permits", "Open permits", [0.7, 1.0, 0.1], "Open permit: hot work near the east side of the unit until 18:00."),
          ok("talk", "Outgoing operator", [0.0, 1.05, 0.5], "You confirm alarms, abnormal conditions, and field work, and the operator signs off.", { after: ["log", "permits"], critical: true, early: "You started the verbal handover without reading the log and permits. Important details can be missed." }),
          bad("leave", "Let operator leave", [0.5, 0.6, 0.85], "The operator leaves with your questions unanswered. Information from the last shift is hardest to rebuild.", { critical: true }),
        ]),
      task("A process upset triggers several alarms at once. Handle them by priority.",
        "Prioritize alarms by consequence and use the response procedure.", [
          ok("p1", "High-high pressure (P1)", [0.52, 1.35, -0.1], "You handle the Priority 1 alarm first and follow its response procedure."),
          bad("ackall", "Acknowledge all", [0.0, 1.62, -0.2], "Acknowledging without understanding hides a developing hazard.", { critical: true }),
          bad("horn", "Silence horn", [0.0, 0.95, 0.15], "Silencing the horn removes the audible cue but does not address the cause."),
          ok("p2", "Low flow (P2)", [0.0, 1.35, -0.1], "You address the Priority 2 alarm using its procedure.", { after: ["p1"], early: "Handle the highest-consequence alarm first." }),
          ok("p3", "Pump status (P3)", [-0.52, 1.35, -0.1], "The advisory is noted and logged.", { after: ["p2"], early: "Handle the higher-priority alarms first." }),
        ]),
      task("A field operator radios a hydrocarbon smell near a pump while you were about to change a setpoint.",
        "Treat a possible release as an emergency and keep communication open.", [
          ok("hold", "Setpoint change", [0.0, 1.0, 0.0], "You hold the change. Process moves during a possible release can make things worse."),
          ok("radio", "Radio", [0.45, 0.95, 0.3], "You keep the channel open and tell the operator to move upwind of the pump."),
          ok("procedure", "Leak procedure", [-0.5, 1.62, -0.2], "You start the emergency leak procedure and call the response team.", { after: ["hold", "radio"], early: "First hold the change and keep the operator in contact." }),
          bad("finish", "Finish the change", [-0.52, 1.0, 0.0], "A process change during a possible release may worsen conditions.", { critical: true }),
        ]),
      task("Production is under pressure and a coworker suggests bypassing a protective trip for a few minutes.",
        "Safeguards are bypassed only through the authorized process.", [
          ok("decline", "Coworker", [0.0, 1.05, 0.5], "You decline. Protective trips are only bypassed through the approved process."),
          bad("bypass", "Trip bypass switch", [-0.52, 1.5, -0.1], "An unauthorized bypass leaves the unit unprotected and can be forgotten.", { critical: true }),
          ok("moc", "Bypass request form", [0.7, 1.0, 0.1], "You open the formal request so authorized people can assess the risk and set compensating measures.", { after: ["decline"] }),
          ok("record", "Shift log", [-0.7, 1.0, 0.1], "The request and your decision are recorded for the next shift.", { after: ["moc"] }),
        ]),
    ],
  },
  {
    id: "refinery-battery-area",
    category: "industrial",
    title: "Outdoor rounds at the battery limits",
    duration: "5 min",
    setting: "Outdoor process areas at the edge of a refinery unit",
    description: "Prepare to enter, respond to a gas alarm and a leak, and reach the muster point.",
    object: "battery",
    briefing: ["Know the wind direction and muster point", "Carry a bump-tested personal gas monitor", "Do not touch leaking equipment"],
    steps: [
      task("You are about to start outdoor rounds. Prepare before entering the unit.",
        "Know the wind, your exit, and your PPE before entering the unit.", [
          ok("wind", "Wind sock", [-1.35, 1.0, 0.5], "Wind is from the west. Stay upwind of equipment when you can."),
          ok("muster", "Muster point sign", [1.05, 0.75, 0.55], "The muster point is east of the unit and the route is clear."),
          ok("monitor", "Gas monitor", [0.55, 0.75, 0.45], "The personal monitor passes the bump test."),
          ok("radio", "Radio", [0.1, 0.5, 0.85], "The control room logs that you are entering.", { after: ["wind", "muster", "monitor"] }),
          bad("enter", "Enter now", [-0.2, 0.15, 0.9], "You entered without checking wind, escape route, or monitor. A hidden hazard can disable you quickly.", { critical: true }),
        ]),
      task("Your gas monitor alarms during rounds. React.",
        "A gas alarm means move to safety first, then report.", [
          ok("move", "Upwind route", [-1.35, 0.5, 0.75], "You move away from the source toward the upwind side."),
          ok("warn", "Nearby workers", [-0.2, 0.5, 0.75], "You alert nearby workers by radio and hand signal.", { after: ["move"] }),
          ok("report", "Radio", [0.5, 0.9, 0.75], "The control room starts the gas-release response.", { after: ["warn"] }),
          bad("search", "Look for the source", [-0.55, 0.5, 0.0], "Walking toward the source risks exposure. Trained responders investigate.", { critical: true }),
          bad("reading", "Finish gauge reading", [0.35, 0.5, -0.5], "Delaying the report delays everyone's protection."),
        ], { onStart: "alarm" }),
      task("You find a small hydrocarbon weep at a flange.",
        "Do not repair a leak you are not authorized to fix; report and restrict access.", [
          ok("clear", "Stay back", [-0.5, 0.4, 0.7], "You stay clear and keep ignition sources such as phones and tools away."),
          ok("barricade", "Area around leak", [0.0, 0.15, 0.55], "You barricade the area so others do not walk through it.", { after: ["clear"], reveal: "barricade" }),
          ok("report", "Radio", [0.5, 0.9, 0.75], "You report the leak for authorized repair.", { after: ["barricade"] }),
          bad("tighten", "Flange bolts", [-0.1, 0.35, 0.3], "Working on a leaking joint without authorization can make it worse.", { critical: true }),
          bad("rag", "Wrap with rag", [0.05, 0.25, 0.3], "A rag can spread the problem. Report the leak instead."),
        ], { onStart: "leak" }),
      task("The site emergency alarm sounds. Your tool bag is still near the unit.",
        "In an emergency, go to the muster point by the safe route and be counted.", [
          ok("route", "Muster point", [1.05, 0.75, 0.55], "You walk the safe route, upwind of the release."),
          ok("count", "Headcount", [1.35, 0.35, 0.65], "You report for the headcount and wait for instructions.", { after: ["route"] }),
          bad("bag", "Tool bag", [-0.45, 0.15, 0.4], "Returning for belongings puts you at risk and delays the headcount.", { critical: true }),
          bad("wait", "Wait here", [0.0, 0.5, 0.2], "Waiting leaves you unaccounted for."),
        ], { onStart: "alarm" }),
    ],
  },
  {
    id: "skyscraper-window-cleaning",
    category: "industrial",
    title: "Clean windows on a skyscraper",
    duration: "5 min",
    setting: "A suspended platform on a high-rise facade",
    description: "Inspect the rig, use an independent lifeline, react to wind, and protect people below.",
    object: "facade",
    briefing: ["Inspect the platform and anchorages", "Use an independent lifeline", "Know the wind limit and exclusion zone"],
    steps: [
      task("Before you ride the suspended platform, inspect it and the conditions, then board.",
        "Inspect the rig and conditions yourself before every ride.", [
          ok("ropes", "Ropes and roof anchors", [0.5, 1.4, -0.1], "Ropes are undamaged and the roof anchorages are secure."),
          ok("controls", "Controls and lowering", [0.3, 0.95, 0.3], "Controls respond and the emergency lowering device works."),
          ok("forecast", "Wind forecast", [-1.0, 1.8, -0.4], "Forecast gusts are below the platform limit for the next hour."),
          ok("board", "Board platform", [-0.35, 0.95, 0.35], "You board the platform and close the gate.", {
            after: ["ropes", "controls", "forecast"], critical: true,
            early: "You boarded before inspecting the ropes, controls, and forecast. Do not trust that someone else checked.",
          }),
        ]),
      task("You are on the platform. Protect yourself from a fall.",
        "Use a harness on an independent lifeline whenever you are on a suspended platform.", [
          ok("harness", "Harness", [-0.15, 1.05, 0.2], "You put on the harness and check the buckles and lanyard."),
          bad("platform", "Platform rail", [-0.5, 1.0, 0.18], "If the platform fails you would fall with it. The lifeline must be independent.", { critical: true }),
          bad("none", "Work without harness", [0.3, 1.0, 0.3], "Rails do not replace fall protection on a suspended platform.", { critical: true }),
          ok("lifeline", "Independent roof lifeline", [0.05, 1.6, -0.2], "You connect to a separate lifeline anchored to the roof.", { after: ["harness"], tether: true, early: "Put on and check your harness first." }),
        ]),
      task("Gusts rise and the platform begins to swing against the glass.",
        "Stop and secure the platform when wind exceeds limits or movement becomes unsafe.", [
          ok("stop", "Stop work", [-0.15, 1.05, 0.2], "You stop work immediately."),
          ok("secure", "Cleaning tools", [0.32, 0.9, 0.2], "Tools are tethered and stowed.", { after: ["stop"] }),
          ok("lower", "Controls", [0.3, 0.95, 0.3], "You bring the platform to a safe position per the wind procedure.", { after: ["secure"] }),
          ok("report", "Radio", [0.8, 0.95, 0.3], "You report the conditions to the supervisor.", { after: ["lower"] }),
          bad("keep", "Keep cleaning", [-0.7, 1.3, -0.35], "Continuing in rising wind risks collision, entanglement, or a fall.", { critical: true }),
          bad("slow", "Slow down", [0.7, 1.3, -0.35], "Reducing speed does not control a swinging platform."),
        ], { onStart: "wind" }),
      task("A pedestrian walks toward the barricaded zone below while you hold a squeegee.",
        "Protect people below with an exclusion zone, a ground watcher, and tethered tools.", [
          ok("pause", "Pause work", [-0.15, 1.05, 0.2], "You stop and hold your tools still."),
          ok("watcher", "Ground watcher", [0.1, 0.25, 0.7], "The ground watcher redirects the pedestrian and keeps the zone clear.", { after: ["pause"], reveal: "zone" }),
          ok("resume", "Resume cleaning", [0.5, 0.95, 0.2], "When the area is clear, you resume with tools tethered.", { after: ["watcher"], early: "Wait until the area below is clear." }),
          bad("ignore", "Keep working", [-0.7, 1.3, -0.35], "A barrier does not help if someone enters it. Pause and communicate.", { critical: true }),
          bad("shout", "Shout and continue", [0.7, 1.3, -0.35], "A shout helps, but you should stop the drop risk and involve the ground watcher."),
        ], { onStart: "walker" }),
    ],
  },
  {
    id: "welding-hot-work",
    category: "industrial",
    title: "Weld safely",
    duration: "5 min",
    setting: "A fabrication bay or maintenance workshop",
    description: "Prepare the hot work area, protect yourself, secure gas cylinders, and keep a fire watch afterward.",
    object: "welding",
    briefing: ["Hot work permit and fire watch", "Correct helmet, gloves, and clothing", "Secure cylinders upright"],
    steps: [
      task("You must weld near stored materials. Prepare the area, then strike the arc.",
        "No permit, no controlled area, no hot work.", [
          ok("permit", "Hot work permit", [-0.55, 1.3, 0.55], "The permit is signed and lists the area, time, and fire watch."),
          ok("clear", "Stored materials", [-0.95, 0.55, -0.3], "You cover the combustibles with a fire blanket.", { reveal: "blanket" }),
          ok("watch", "Fire watch", [0.75, 1.1, 0.55], "A trained fire watch is assigned with an extinguisher.", { reveal: "watch" }),
          ok("ext", "Extinguisher", [1.25, 0.55, 0.3], "The extinguisher is in date and within reach."),
          ok("arc", "Weld joint", [-0.12, 1.05, 0.0], "You strike the arc inside the controlled area.", {
            after: ["permit", "clear", "watch", "ext"], critical: true,
            early: "You struck the arc before the permit, covered materials, fire watch, and extinguisher were in place. Sparks travel.",
          }),
        ]),
      task("Protect your eyes, skin, and lungs before welding.",
        "Use the right PPE and ventilate to control fumes.", [
          ok("helmet", "Welding helmet", [0.55, 1.3, 0.55], "You choose the correct shade and check the lens."),
          ok("clothes", "FR clothing and gloves", [-0.3, 1.3, 0.6], "You wear flame-resistant clothing and dry welding gloves."),
          ok("fume", "Fume extraction", [0.3, 1.45, -0.3], "You position the extraction arm at the weld."),
          ok("weld", "Weld joint", [-0.12, 1.05, 0.0], "You weld with your helmet down and fumes extracted.", { after: ["helmet", "clothes", "fume"], critical: true, early: "You started before protecting your eyes, skin, and lungs." }),
          bad("flip", "Lift helmet to look", [0.0, 1.2, 0.3], "Arc light can injure unprotected eyes. Wait until the arc stops.", { critical: true }),
        ]),
      task("Gas cylinders are in the bay. Handle them safely.",
        "Secure cylinders upright, capped, and away from ignition sources.", [
          ok("chain", "Cylinders", [1.05, 0.7, -0.08], "You chain both cylinders upright to the wall.", { reveal: "chain" }),
          ok("caps", "Cylinder valves", [1.05, 1.1, -0.2], "You close the valves and fit the caps when not in use."),
          ok("clear", "Spark area", [0.6, 0.6, -0.2], "You move cylinders clear of the spark path."),
          bad("lay", "Lay cylinder down", [1.05, 0.1, 0.1], "A cylinder can roll or be knocked and a valve can be damaged.", { critical: true }),
          bad("lean", "Lean on wall", [0.8, 0.3, -0.35], "Leaning can still fall. Chain or strap it."),
        ]),
      task("You have finished the last weld.",
        "Stay for the post-work fire watch before closing the permit.", [
          ok("off", "Welding machine", [0.1, 1.05, 0.0], "You switch off the machine and close the torch."),
          ok("mark", "Hot metal", [-0.25, 0.95, 0.0], "You mark the hot metal so no one touches it."),
          ok("watch", "Fire watch", [0.75, 1.1, 0.55], "The fire watch stays for the full required period.", { after: ["off", "mark"] }),
          ok("check", "Nearby areas", [-0.95, 0.55, -0.3], "You check around for smoldering material before closing the permit.", { after: ["watch"] }),
          bad("leave", "Leave now", [1.2, 0.5, 0.6], "Fires can start after the work stops. Leaving at once can leave a hidden fire unnoticed.", { critical: true }),
        ]),
    ],
  },
  {
    id: "electrical-work",
    category: "industrial",
    title: "Electrical isolation and work",
    duration: "5 min",
    setting: "A distribution panel in a service room",
    description: "Plan the isolation, lock out and test, defend your lock, and use the right protection.",
    object: "electrical",
    briefing: ["Only authorized, qualified staff", "Lock, tag, and test before touching", "Your lock, your key"],
    steps: [
      task("You must replace a component in the panel. Plan before touching anything.",
        "Assume live until isolated, locked out, and proven dead.", [
          ok("drawing", "Circuit drawing", [-0.9, 1.1, 0.3], "You match the drawing to the panel and identify the correct breaker."),
          ok("plan", "Supervisor", [0.9, 1.0, 0.4], "You agree the isolation plan and the work scope."),
          bad("label", "Panel label", [0.2, 1.45, 0.25], "Labels can be wrong or out of date. Prove it dead with a test.", { critical: true }),
          ok("start", "Begin isolation", [0.0, 0.4, 0.8], "You begin the isolation steps.", { after: ["drawing", "plan"] }),
        ]),
      task("Isolate the circuit. Do each step in the right order.",
        "Lock, tag, and test before you touch any conductor.", [
          ok("off", "Breaker", [-0.2, 1.05, 0.2], "You switch off the correct breaker.", { hide: "breakerOn", reveal: "breakerOff" }),
          ok("lock", "Lockout hasp", [-0.2, 0.9, 0.25], "You apply your personal lock.", { after: ["off"], reveal: "lock", early: "Switch the breaker off first, then lock it." }),
          ok("tag", "Danger tag", [-0.1, 0.78, 0.28], "You attach your danger tag with your name and date.", { after: ["lock"], reveal: "tag" }),
          ok("prove", "Tester on known source", [0.85, 0.35, 0.5], "The tester reads correctly on a known live source, so it is working.", { after: ["tag"] }),
          ok("test", "Test for no voltage", [-0.2, 1.25, 0.25], "The tester reads no voltage. The circuit is proven dead.", { after: ["prove"], reveal: "leads", critical: true, early: "Prove your tester on a known source before relying on a no-voltage reading." }),
          bad("assume", "Start work now", [0.0, 0.4, 0.8], "A breaker position does not prove the circuit is dead.", { critical: true }),
        ]),
      task("A coworker walks up and wants to switch the breaker back on briefly.",
        "Your lock stays until your work is complete; only you remove it.", [
          ok("refuse", "Coworker", [0.7, 1.0, 0.7], "You tell them to stay clear. Only you remove your lock, when you are finished."),
          ok("check", "Your lock", [-0.2, 0.9, 0.25], "Your lock and tag are still in place.", { after: ["refuse"] }),
          bad("allow", "Allow a brief switch-on", [0.2, 1.0, 0.3], "A brief energization can still injure or kill someone working on the circuit.", { critical: true }),
          bad("tag2", "Add another tag", [0.0, 0.7, 0.3], "More tags do not stop someone switching it on. Keep the lock."),
        ], { onStart: "coworker" }),
      task("Before you work in the panel, use the right protection.",
        "Use rated tools and PPE as a second layer, not a replacement for isolation.", [
          ok("mat", "Insulating mat", [0.0, 0.15, 0.55], "You stand on the insulating mat."),
          ok("gloves", "Gloves and eye protection", [0.6, 1.35, 0.7], "You wear rated gloves and eye protection."),
          ok("tools", "Insulated tools", [0.9, 0.45, 0.55], "You pick the insulated tools rated for the task."),
          bad("tape", "Taped pliers", [0.3, 0.25, 0.8], "Improvised insulation is not rated. Use insulated tools.", { critical: true }),
          ok("work", "Open panel", [0.0, 0.8, 0.2], "You carry out the work.", { after: ["mat", "gloves", "tools"] }),
        ]),
    ],
  },
];
