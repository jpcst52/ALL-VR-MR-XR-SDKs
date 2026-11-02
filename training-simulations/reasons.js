// "Why this matters" content for the Practice Lab, kept in one file so the owner and partners can review it.
//
// Background: in a Penn State randomized study of a battery-storage procedure (Wang & Messner, 2026, ITcon 31),
// VR beat video on procedural and spatial scores, but neither taught WHY the steps mattered. This file adds that.
//
// Two optional fields, applied to the scenarios in scenarios.js (they can also be written inline in a scenario):
//   hotspot.why        one short, plain-language sentence shown in a "Why this matters" panel after the learner
//                      completes that action. Only on correct (required) actions.
//   scenario.rationale one multiple-choice reasoning question asked after the last task:
//                      { question, options: [{ text, best, feedback }] } with exactly 3 options and one best answer.
//                      Its result is reported separately as "Reasoning" and is never added to the procedure score.
//
// Content rules: general safety and quality awareness only. Not a procedure, permit, or certification. No standard or
// regulation numbers and no citations. Each reason is one physically correct, generic sentence. Content needs
// owner or partner review before it is used with learners.
//
// Shape: reasons[scenarioId] = { steps: [ { hotspotId: why, ... } per task, in task order ], rationale }.
// The Circulation Builder (biology) is a draft for faculty review and has no entry here on purpose.

export const reasons = {
  // ------------------------------------------------------------------ manufacturing: hydrostatic test
  "heat-exchanger-hydrotest": {
    steps: [
      {
        pack: "The test pressure is the limit for the whole test, and a gauge out of calibration can read low.",
        blindA: "Every opening must be sealed, or water escapes and the vessel cannot hold pressure.",
        blindB: "A missed opening sprays water out as soon as the pressure rises.",
        torque: "Even, cross-pattern tightening seats the gasket evenly so the joint does not leak or blow out.",
      },
      {
        openvent: "Trapped air stores far more energy than water, so venting it makes any failure much less violent.",
        pump: "A slow fill lets the air rise and leave through the vent instead of forming trapped pockets.",
        full: "Steady water with no bubbles at the vent is the sign that the air is gone.",
      },
      {
        closevent: "The vessel has to be sealed before pressure can build.",
        retreat: "A part that fails under pressure fails suddenly, so distance and a barrier protect you.",
        raise: "Small steps let you stop at the first leak and avoid overshooting the test pressure.",
        hold: "A falling gauge or a wet joint during the hold is the evidence of a leak.",
      },
      {
        stop: "Stopping the pump removes the source of pressure before anything is opened.",
        zero: "A zero reading confirms no stored pressure is left before anyone opens anything.",
        vent: "Letting air in as water leaves stops a vacuum forming inside the vessel.",
        drain: "Draining to the approved point keeps test water out of work areas.",
      },
    ],
    rationale: {
      question: "At the end of the test, why is the high-point vent opened BEFORE the drain?",
      options: [
        { text: "To lower the test pressure a little more", best: false, feedback: "The gauge already reads zero by then. The vent is opened for air, not for pressure." },
        { text: "So air can get in as the water drains out", best: true, feedback: "Yes. Without air coming in, a vacuum forms. It stalls the draining and can damage a vessel not built for vacuum." },
        { text: "To check the vessel for leaks one more time", best: false, feedback: "The leak check was the hold at test pressure. An open vent tells you nothing about leaks." },
      ],
    },
  },

  // ------------------------------------------------------------------ industrial: refinery battery limits
  "refinery-battery-area": {
    steps: [
      {
        wind: "Released gas drifts downwind, so the wind tells you which way is safer to move.",
        muster: "In an alarm there is no time to look for the route, so you learn it first.",
        monitor: "A bump test shows the sensors react to gas; an untested monitor may stay silent.",
        radio: "The control room then knows someone is in the unit and where to look if needed.",
      },
      {
        move: "Some gases cannot be seen or smelled, so getting away from the source comes first.",
        warn: "Others may not have a monitor alarm yet, and your warning gets them moving too.",
        report: "The control room can start the response, stop sources, and keep others out.",
      },
      {
        clear: "Hydrocarbon vapour can ignite from a spark, so ignition sources stay away.",
        barricade: "A barrier stops people walking into the vapour or slipping on the leak.",
        report: "Only an authorized crew can isolate and repair the leak safely.",
      },
      {
        route: "A known route upwind keeps you out of the release on the way out.",
        count: "The headcount tells responders who is safe, so no one searches for you in danger.",
      },
    ],
    rationale: {
      question: "Why check the wind sock before you start outdoor rounds?",
      options: [
        { text: "To record the weather in the shift log", best: false, feedback: "Logging weather can help, but the check is for your own escape direction." },
        { text: "Because strong wind stops all outdoor work", best: false, feedback: "Some tasks have wind limits, but rounds go on. The point is knowing where gas would go." },
        { text: "To know which way a gas release would drift", best: true, feedback: "Yes. Gas drifts downwind, so you move upwind and know your safe direction before an alarm." },
      ],
    },
  },

  // ------------------------------------------------------------------ industrial: refinery control room
  "control-room-unit": {
    steps: [
      {
        log: "The log shows what changed last shift, so an alarm does not take you by surprise.",
        permits: "Knowing where people are working tells you who a process change could affect.",
        talk: "Questions can only be answered while the outgoing operator is still there.",
      },
      {
        p1: "The highest-priority alarm is the one that can hurt people or equipment soonest.",
        p2: "Once the most serious alarm is handled, the next one gets your full attention.",
        p3: "Logging advisories keeps small problems from being forgotten.",
      },
      {
        hold: "A process change during a possible release can raise pressure or flow at the leak.",
        radio: "An operator who stays in contact can be guided away and can report changes.",
        procedure: "A written emergency procedure prevents missed steps under stress.",
      },
      {
        decline: "A trip acts at the moment things go wrong; bypassing it removes that safeguard.",
        moc: "A formal review lets people with authority weigh the risk and add other protection.",
        record: "The next shift needs to know, so a bypass is never left in place by accident.",
      },
    ],
    rationale: {
      question: "At handover, why read the log and open permits BEFORE talking with the outgoing operator?",
      options: [
        { text: "So you know what to ask while they are still there", best: true, feedback: "Yes. Reading first turns the talk into real questions, asked while the person who knows the answers is present." },
        { text: "Because the log replaces the conversation", best: false, feedback: "The talk still matters. Some things never make it into the log." },
        { text: "So the outgoing operator can leave sooner", best: false, feedback: "Speed is not the goal. A complete handover is." },
      ],
    },
  },

  // ------------------------------------------------------------------ manufacturing: refinery pipe spool
  "pipe-spool-welding": {
    steps: [
      {
        wps: "A welding procedure is proven to make a sound weld only for what it covers.",
        qual: "The qualification shows this welder has proven they can make this kind of weld.",
        mtr: "The report shows the pipe is the grade the design calls for.",
        heat: "The matching number ties this exact pipe to its report; without it the grade is unknown.",
      },
      {
        clean: "Oil, paint, and scale burn into the weld and leave pores and weak spots.",
        fit: "A wrong gap or poor alignment leaves unfused metal at the root, where cracks can start.",
        preheat: "Preheat slows cooling, which helps prevent cracking in some steels.",
        tack: "Tacks hold the fit-up so the joint does not shift as it heats.",
      },
      {
        permit: "The permit means someone checked that this place and time are safe for sparks.",
        isolate: "A nearby live line could leak flammable material into the hot work area.",
        gas: "Sparks can ignite vapour you cannot see, so the air is tested first.",
        watch: "Sparks can smoulder unseen, and a fire watch catches a fire early.",
      },
      {
        visual: "Surface defects should be found before any further testing.",
        mark: "The mark links the joint to its welder if a problem turns up later.",
        nde: "Testing finds flaws inside the weld that the eye cannot see.",
        record: "A complete record lets anyone trace the joint later for repairs or investigations.",
      },
    ],
    rationale: {
      question: "Why check the heat number stamped on the pipe against the material test report?",
      options: [
        { text: "It tells the welder how hot to preheat", best: false, feedback: "A heat number is a batch ID from the mill, not a temperature." },
        { text: "It proves this pipe is the material on the report", best: true, feedback: "Yes. The wrong material in pressure piping can fail in service, so identity is checked before welding." },
        { text: "It is only needed for the paperwork at the end", best: false, feedback: "It is checked before welding, because once the pipe is welded in, the wrong material is costly to find." },
      ],
    },
  },

  // ------------------------------------------------------------------ industrial: other scenarios
  "work-at-height": {
    steps: [
      {
        tag: "The tag shows a competent person inspected the scaffold and the load it can carry.",
        deck: "Missing guardrails or a damaged board can cause a fall before you even start work.",
        plan: "Someone left hanging in a harness needs rescue quickly, so the plan comes first.",
        climb: "Three points of contact keep you on the ladder if a hand or foot slips.",
      },
      {
        harness: "A damaged harness may fail at the moment it has to stop a fall.",
        anchor: "A fall puts a large force on the anchor, and only a rated anchor is made to hold it.",
      },
      {
        bag: "Even a small tool dropped from height can seriously injure someone below.",
        zone: "A barricade keeps people out of the area where dropped objects can land.",
      },
      {
        stop: "Moving on a swaying platform raises the chance of a slip.",
        secure: "Wind can blow loose tools off the platform onto people below.",
        report: "The supervisor can stop other work at height and decide when it is safe again.",
      },
    ],
    rationale: {
      question: "Why must the rescue plan be in place BEFORE you climb?",
      options: [
        { text: "A person hanging in a harness needs rescue fast", best: true, feedback: "Yes. Hanging still in a harness can become dangerous in a short time, so rescue is planned ahead." },
        { text: "It is only needed if there are no guardrails", best: false, feedback: "Guardrails make a fall less likely. The rescue plan covers the case where one happens anyway." },
        { text: "It is the supervisor's job, not yours", best: false, feedback: "You are the one who relies on it if you fall, so you confirm it exists." },
      ],
    },
  },

  "skyscraper-window-cleaning": {
    steps: [
      {
        ropes: "The ropes and roof anchors carry the whole platform and you.",
        controls: "If power fails, you need a working way to bring the platform down.",
        forecast: "Wind high on a building can be much stronger than at street level.",
      },
      {
        harness: "A harness only protects you if it is checked and worn correctly.",
        lifeline: "An independent line still holds you if the platform or its ropes fail.",
      },
      {
        stop: "Working on a swinging platform risks hitting the glass or losing your grip.",
        lower: "A safe position limits how hard a swinging platform can strike the building.",
        report: "Reporting lets the supervisor stop other crews before they go up.",
      },
      {
        pause: "Stopping your tools removes the drop risk while someone is near the zone.",
        watcher: "A ground watcher can stop people before they walk under the work.",
      },
    ],
    rationale: {
      question: "Why must your lifeline be independent of the platform?",
      options: [
        { text: "It lets you reach farther along the window", best: false, feedback: "Reach is not the purpose. The line exists to catch you if the platform fails." },
        { text: "It is only needed in windy weather", best: false, feedback: "Platforms and ropes can fail in calm weather too, so the line is always used." },
        { text: "If the platform or its ropes fail, it still holds you", best: true, feedback: "Yes. A line tied to the platform would fall with it." },
      ],
    },
  },

  "welding-hot-work": {
    steps: [
      {
        permit: "The permit means the area and time were checked as safe for hot work.",
        clear: "Sparks can travel well beyond the weld and ignite nearby materials.",
        watch: "A fire watch can spot and put out a small fire before it spreads.",
        ext: "Seconds matter with a small fire, so the extinguisher must be within reach.",
      },
      {
        helmet: "Arc light can burn the eyes, and the right shade filters it.",
        clothes: "Spatter can set ordinary clothing alight; flame-resistant clothing resists it.",
        fume: "Welding fume carries metal particles that are harmful to breathe.",
      },
      {
        chain: "A falling cylinder can break its valve and release gas under pressure.",
        caps: "Closed valves and caps protect against leaks and valve damage.",
        clear: "Sparks and heat near a cylinder can damage it or ignite leaking gas.",
      },
      {
        mark: "Hot metal can look exactly like cold metal.",
        watch: "Fires can smoulder and break out some time after the work stops.",
        check: "Sparks can land in gaps and smoulder out of sight.",
      },
    ],
    rationale: {
      question: "Why does the fire watch stay AFTER the welding has stopped?",
      options: [
        { text: "To help put the tools away", best: false, feedback: "Tidying up is not the job. The fire watch is there for fire." },
        { text: "Sparks can smoulder unseen and flare up later", best: true, feedback: "Yes. A spark in a gap can smoulder before it flares, so someone keeps watching." },
        { text: "To guard the welding machine", best: false, feedback: "The machine is switched off. The risk is a fire that starts after the work ends." },
      ],
    },
  },

  "electrical-work": {
    steps: [
      {
        drawing: "The drawing shows which breaker actually feeds the part you will touch.",
        plan: "Agreeing the plan catches mistakes before anyone is exposed.",
      },
      {
        off: "Switching off is the first step, but a switch alone can be turned back on.",
        lock: "Your own lock stops anyone switching the circuit back on while you work.",
        tag: "The tag tells others who is working and why the breaker is locked.",
        prove: "A broken tester also reads zero, so you prove it works first.",
        test: "Only a test proves the circuit is dead; switches and labels can be wrong.",
      },
      {
        refuse: "Even a brief switch-on can energize the circuit while someone is working on it.",
      },
      {
        mat: "The mat adds insulation between you and the floor.",
        gloves: "Protection is a backup in case the isolation is wrong, not a replacement.",
        tools: "Rated insulated tools are made and tested to protect against contact.",
      },
    ],
    rationale: {
      question: "Why check the tester on a known live source BEFORE testing the circuit?",
      options: [
        { text: "To warm up the tester's battery", best: false, feedback: "Battery warm-up is not the point. The point is knowing the tester works." },
        { text: "A broken tester also shows no voltage", best: true, feedback: "Yes. A dead tester and a dead circuit look the same, so you prove the tester first." },
        { text: "It is only needed for brand-new testers", best: false, feedback: "Any tester can fail, new or old, so it is proved every time." },
      ],
    },
  },

  // ------------------------------------------------------------------ manufacturing: other scenarios
  "lighter-lot-sampling": {
    steps: [
      {
        plan: "The plan sets how many units are needed for the result to mean something.",
        random: "Defects can cluster in one part of a run, so a random pick gives every part a chance.",
        label: "Without the lot ID, the result cannot be tied back to the lighters it describes.",
      },
      {
        hood: "Fuel vapour can collect in still air, and the hood draws it away from you.",
        airflow: "A hood with its fan off gives no protection.",
        place: "Working well inside the hood keeps vapour from escaping toward you.",
      },
      {
        ext: "If a test lighter flares, a fire needs a response within seconds.",
        one: "One flame at a time keeps any fire small and easy to control.",
        out: "The metal near the flame stays hot after it goes out.",
      },
      {
        record: "An accurate record is the only way others can trust the decision on the lot.",
        hold: "Quarantine stops a failed lot from being shipped by mistake.",
        notify: "The quality lead can decide what happens next and check related lots.",
      },
    ],
    rationale: {
      question: "Why take the sample from a random carton instead of the nearest one?",
      options: [
        { text: "So every part of the lot has a fair chance of being checked", best: true, feedback: "Yes. Problems can sit in one part of a run, and a convenient box can miss them." },
        { text: "Random cartons are easier to reach", best: false, feedback: "Often they are harder to reach. Fairness, not convenience, is the point." },
        { text: "The nearest carton is always the best quality", best: false, feedback: "There is no reason to think so. The point is that it may not represent the lot." },
      ],
    },
  },

  "resistor-selection": {
    steps: [
      {
        strap: "Your body can carry enough static to damage parts without you feeling it.",
        ground: "The strap only drains static when its cord is connected to ground.",
      },
      {
        bands: "The bands tell you what the part is supposed to be.",
        meter: "Checking the leads on a short shows the meter reads near zero before you trust it.",
        probe: "Out of circuit, no other part can change the reading.",
      },
      {
        calc: "The value sets the LED current, and the power tells you how hot the part will run.",
        rating: "A part run close to its power limit runs hot and can fail early.",
      },
      {
        env: "Sulfur can attack the inside of standard chip resistors over time.",
        derate: "Parts can safely handle less power when their surroundings are hot.",
      },
    ],
    rationale: {
      question: "Why measure the loose resistor OUT of the circuit?",
      options: [
        { text: "Soldering changes a resistor's value", best: false, feedback: "Soldering does not normally change it. The problem is other paths in the circuit." },
        { text: "Other parts connected to it can change the reading", best: true, feedback: "Yes. Current through parallel parts makes the meter read a different value." },
        { text: "The meter only works on loose parts", best: false, feedback: "Meters work in circuits too, but the reading may not be the resistor alone." },
      ],
    },
  },

  // ------------------------------------------------------------------ business
  "customer-welcome": {
    steps: [
      {
        greet: "People who feel heard are more likely to explain what they really need.",
        listen: "Their own words help you avoid solving the wrong problem.",
      },
      {
        move: "Private details said aloud can be overheard and misused.",
        confirm: "Asking only for what the process needs limits what could be exposed.",
      },
      {
        options: "Honest timing lets the customer plan, and a broken promise costs trust.",
        agree: "A next step the customer chose is one they are likely to follow.",
      },
    ],
    rationale: {
      question: "Why confirm only the minimum identity details the process needs?",
      options: [
        { text: "To finish the conversation faster", best: false, feedback: "It may be quicker, but the reason is protecting the customer's information." },
        { text: "Less shared information means less can be exposed", best: true, feedback: "Yes. Every extra detail shared is one more that could be overheard or misused." },
        { text: "Customers dislike being asked questions", best: false, feedback: "Some questions are needed. The aim is to share no more than necessary." },
      ],
    },
  },

  "workplace-spill": {
    steps: [
      {
        cones: "Marking the hazard stops others walking into it while you respond.",
        people: "People nearby are the most likely to step in it or breathe it.",
      },
      {
        ppe: "An unknown liquid could harm your skin or eyes.",
        kit: "Approved kit materials are chosen to be safe with the substances on site.",
        responder: "Trained responders know how to identify and handle unknown substances.",
      },
      {
        report: "A report lets someone fix the cause so it does not happen again.",
        log: "Records show patterns over time that a single event cannot.",
      },
    ],
    rationale: {
      question: "Why report a spill even after it is cleaned up and nobody was hurt?",
      options: [
        { text: "So the cause can be found and the next one prevented", best: true, feedback: "Yes. Near misses show where the next accident could come from." },
        { text: "So someone can be blamed", best: false, feedback: "Reporting is for learning and prevention, not blame." },
        { text: "Only spills that injure someone need a report", best: false, feedback: "Near misses are useful precisely because nobody was hurt yet." },
      ],
    },
  },

  "delivery-check": {
    steps: [
      {
        po: "The order is the reference for what should have arrived.",
        count: "Counting what is actually there is the only way to find a shortage.",
        sign: "Your signature says the delivery is correct, so problems are hard to claim later.",
      },
      {
        separate: "Leaving the damaged carton as found keeps the evidence intact.",
        photo: "A dated photo shows the condition at the time it arrived.",
      },
      {
        send: "One complete, dated record is easier to check than several partial ones.",
      },
    ],
    rationale: {
      question: "Why check the delivery BEFORE you sign the receipt?",
      options: [
        { text: "The driver cannot leave until you do", best: false, feedback: "That may be true, but it is not why you check first." },
        { text: "It is a formality that does not matter", best: false, feedback: "It matters: the receipt is the record of what arrived." },
        { text: "Your signature confirms what arrived", best: true, feedback: "Yes. Once you sign for a full delivery, a shortage or damage is much harder to claim." },
      ],
    },
  },

  // ------------------------------------------------------------------ research
  "informed-consent": {
    steps: [
      {
        sheet: "The approved sheet is the version the ethics review checked.",
        explain: "People can only make a real choice if they understand what joining involves.",
        questions: "Questions show what was unclear and give the person time to decide.",
      },
      {
        voluntary: "Consent only counts if the person truly feels free to say no.",
        services: "Tying services to joining would pressure people to agree.",
      },
      {
        consent: "The person signs for themselves to show it is their own decision.",
        record: "No study data should be collected before consent is complete.",
      },
    ],
    rationale: {
      question: "Why make clear that their services do not depend on joining the study?",
      options: [
        { text: "If people fear losing services, saying yes is not free", best: true, feedback: "Yes. Consent given under pressure is not voluntary consent." },
        { text: "Because the study has no budget for services", best: false, feedback: "Budget is not the issue. The issue is pressure on the person's choice." },
        { text: "To keep the conversation short", best: false, feedback: "Length is not the point. A free choice is." },
      ],
    },
  },

  "sample-custody": {
    steps: [
      {
        protocol: "Assigned codes keep sample identity consistent across the whole study.",
        labelA: "Checking each label as you go catches a mix-up when it happens.",
        labelB: "Each vial is checked on its own, because one correct label says nothing about the next.",
      },
      {
        secure: "Pausing keeps a possibly mixed-up sample from being used or changed.",
        document: "Writing it down as found preserves the facts for whoever resolves it.",
      },
      {
        seal: "A broken seal could mean the sample was opened or contaminated.",
        log: "The custody log shows who had the sample at every point.",
      },
    ],
    rationale: {
      question: "Why label samples with assigned codes instead of participants' names?",
      options: [
        { text: "Names do not fit on the label", best: false, feedback: "Space is not the reason. Privacy is." },
        { text: "Codes are faster to write", best: false, feedback: "Speed is not the reason. Codes protect who the sample came from." },
        { text: "Codes keep participants private if a container is seen", best: true, feedback: "Yes. Anyone can read a container, so it must not identify the person." },
      ],
    },
  },

  "field-observation": {
    steps: [
      {
        check: "An instrument that has drifted gives readings that look real but are wrong.",
        steps: "The same steps at every site make readings comparable.",
      },
      {
        keep: "An unexpected reading can be a real finding, so it is kept as measured.",
        flag: "Flagging asks others to look without changing the data.",
      },
      {
        plan: "The approved plan says what personal data may be collected.",
      },
    ],
    rationale: {
      question: "Why record an unusually high reading exactly as measured?",
      options: [
        { text: "So the average comes out higher", best: false, feedback: "The goal is accurate data, not a particular average." },
        { text: "It may be real, and changing it makes the data untrustworthy", best: true, feedback: "Yes. Flag it for review, but never edit or delete it." },
        { text: "Deleting data takes too long", best: false, feedback: "Time is not the reason. Deleting or editing results is falsification." },
      ],
    },
  },
};

// Copies the reasons onto the scenario objects: hotspot.why and scenario.rationale. Inline values win.
export function applyReasons(scenarios) {
  for (const scenario of scenarios) {
    const entry = reasons[scenario.id];
    if (!entry) continue;
    entry.steps?.forEach((map, index) => {
      const step = scenario.steps[index];
      if (!step) return;
      for (const [id, why] of Object.entries(map)) {
        const hotspot = step.hotspots.find((item) => item.id === id && item.correct);
        if (hotspot && !hotspot.why) hotspot.why = why;
      }
    });
    if (entry.rationale && !scenario.rationale) scenario.rationale = entry.rationale;
  }
  return scenarios;
}
