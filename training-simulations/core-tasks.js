// Hands-on tasks for the business and research scenarios (same hotspot format as industrial-scenarios.js).
// Practice examples only: not a substitute for workplace procedures, ethics review, or an approved study protocol.
const ok = (id, label, at, say, extra = {}) => ({ id, label, at, correct: true, say, ...extra });
const bad = (id, label, at, say, extra = {}) => ({ id, label, at, correct: false, say, ...extra });
const task = (prompt, principle, hotspots, extra = {}) => ({ prompt, principle, hotspots, ...extra });

export const coreTasks = {
  "customer-welcome": [
    task("A customer arrives while the counter is busy. Welcome them and find out what they need.",
      "Listen before recommending; acknowledge people even when you are busy.", [
        ok("greet", "Customer", [-1.1, 1.0, 0.35], "You greet them, ask what they need, and give them your attention."),
        ok("listen", "Notepad", [0.0, 1.25, 0.12], "You note their request in their own words.", { after: ["greet"], early: "Greet the customer before you start taking notes." }),
        bad("pitch", "Offer poster", [-0.9, 1.5, -0.35], "A pitch can miss the customer's need. Begin by listening, then offer relevant options."),
        bad("screen", "Phone", [0.7, 1.25, 0.05], "Ignoring someone makes the service feel disrespectful. Give a short acknowledgement at least."),
      ]),
    task("The customer starts saying personal account details where others can hear. Protect their privacy.",
      "Keep private information private and follow the organization's identity-check process.", [
        ok("move", "Quiet room", [1.4, 1.2, 0.2], "You invite them to a quieter place.", { hide: "customer", reveal: "customerAtDoor" }),
        ok("confirm", "Identity check form", [0.0, 1.25, 0.12], "You confirm only the minimum details the process needs.", { after: ["move"], early: "Move to a private spot before discussing their account." }),
        bad("loud", "Read details aloud", [-0.5, 1.4, 0.3], "Repeating private details in public exposes information. Verify discreetly.", { critical: true }),
        bad("line", "Next in line", [-1.5, 1.0, -0.5], "Other customers should not be asked to verify somebody's private information.", { critical: true }),
      ]),
    task("You have two solutions, but one is not available today. Agree the next step.",
      "Be accurate, explain options, and confirm the next step.", [
        ok("options", "Options sheet", [0.3, 1.25, 0.15], "You explain both options and the honest timing for each."),
        ok("agree", "Customer", [-1.1, 1.0, 0.35], "You agree on the next step the customer prefers.", { after: ["options"], early: "Explain the options first so they can choose." }),
        bad("promise", "Promise it today", [0.7, 1.4, 0.0], "A promise you cannot keep damages trust. Say what you know and verify the rest."),
        bad("choose", "Choose for them", [0.0, 0.5, 0.45], "A recommendation can help, but explain the trade-offs and leave the customer in control."),
      ]),
  ],
  "workplace-spill": [
    task("You notice an unknown liquid on a busy walkway. Make the area safe.",
      "Stop access to the hazard before approaching or cleaning it.", [
        ok("cones", "Warning cones", [0.62, 0.8, 0.25], "You place cones and tape so people can see the hazard.", { reveal: "tape" }),
        ok("people", "People nearby", [-1.2, 1.1, 0.6], "You warn nearby people and keep them clear."),
        bad("walk", "Walk through it", [0.0, 0.5, 0.45], "Testing an unknown spill can expose you to a hazard. Avoid contact.", { critical: true }),
        bad("leave", "Leave it", [1.4, 0.9, 0.7], "An unreported hazard can injure someone. Notify the responsible person promptly.", { critical: true }),
      ]),
    task("The substance is unidentified and your team has a spill-response procedure. Respond properly.",
      "Do not improvise around unknown substances; use the trained response.", [
        ok("ppe", "PPE locker", [-1.3, 1.0, -0.3], "You put on the required gloves and eye protection."),
        ok("kit", "Approved spill kit", [1.3, 0.8, -0.2], "You collect the approved spill kit.", { after: ["ppe"], early: "Put on protective equipment before you handle anything." }),
        ok("responder", "Trained responder", [0.0, 1.3, -0.5], "You call the trained responder as the procedure requires.", { after: ["ppe"] }),
        bad("mix", "Mix cleaning products", [0.9, 0.6, 0.6], "Mixing chemicals can create new hazards. Use only approved products.", { critical: true }),
        bad("bare", "Wipe with bare hands", [0.0, 0.5, 0.35], "Appearance does not establish safety. Follow the procedure and wear PPE.", { critical: true }),
      ]),
    task("The area is safe again. Complete the response.",
      "Report hazards and near misses through the approved workplace channel.", [
        ok("report", "Supervisor", [1.2, 1.2, 0.3], "You tell the supervisor what you found and what you did."),
        ok("log", "Incident log", [-1.2, 1.2, 0.2], "You record the hazard and response as required.", { after: ["report"] }),
        bad("silent", "Say nothing", [0.0, 1.5, 0.0], "Hazards and near misses are useful safety information even when nobody is hurt."),
        bad("post", "Post photo publicly", [0.6, 1.4, 0.5], "Use the designated reporting channel and avoid unnecessary personal information or public blame."),
      ]),
  ],
  "delivery-check": [
    task("A delivery arrives during a busy period. Check it before you accept it.",
      "Verify the shipment against its order before recording it as complete.", [
        ok("po", "Purchase order", [1.22, 1.25, 0.08], "The order lists three cartons, reference PO-1187."),
        ok("labels", "Carton labels", [-0.62, 1.0, 0.0], "The labels match the supplier and order reference."),
        ok("count", "Count the items", [0.0, 1.5, 0.18], "You count what is actually in the cartons.", { after: ["po", "labels"], early: "Check the order and the labels before you count." }),
        ok("sign", "Delivery receipt", [1.5, 0.7, 0.5], "You sign for what you have actually checked.", { after: ["count"], early: "Do not sign until you have verified the delivery." }),
        bad("shelve", "Shelves", [-1.5, 1.0, 0.0], "Putting everything away unchecked removes the evidence trail."),
      ]),
    task("One carton is visibly crushed and the count is short by one item. Handle it.",
      "Preserve evidence of discrepancies and record facts, not guesses.", [
        ok("separate", "Crushed carton", [0.0, 1.3, 0.18], "You set the damaged carton aside, untouched.", { reveal: "quarantine" }),
        ok("photo", "Camera", [0.9, 1.2, 0.4], "You photograph the damage and the packaging."),
        ok("note", "Receiving note", [1.22, 1.25, 0.08], "You note the shortage and damage as facts.", { after: ["separate", "photo"] }),
        ok("notify", "Manager", [-1.4, 1.2, 0.5], "You notify the responsible person.", { after: ["note"] }),
        bad("hide", "Hide the carton", [-0.4, 0.4, -0.3], "Hiding it makes the count look right but destroys the evidence.", { critical: true }),
        bad("bin", "Bin", [1.4, 0.4, 0.7], "Discarding the damaged item before recording what arrived removes proof.", { critical: true }),
      ]),
    task("The supplier asks for proof of the issue. Send a useful record.",
      "Keep a timely, factual receiving record and follow the escalation path.", [
        ok("ref", "Order reference", [1.22, 1.25, 0.08], "You include the order reference."),
        ok("counts", "Expected and actual count", [0.0, 1.5, 0.18], "You state the expected and actual counts."),
        ok("condition", "Damage photo", [0.9, 1.2, 0.4], "You attach the dated photo of the condition."),
        ok("send", "Email to supplier", [-1.4, 1.2, 0.5], "You send one clear, dated record.", { after: ["ref", "counts", "condition"], early: "Gather the reference, counts, and condition before you send it." }),
        bad("memory", "Wait and recall", [0.0, 0.4, 0.6], "A memory sent days later is hard to verify."),
        bad("blame", "Blame the driver", [0.5, 1.7, 0.2], "Blame without describing the package is not evidence."),
      ]),
  ],
  "informed-consent": [
    task("You invite a community member to a short interview. Explain the study to them.",
      "Use the approved study information and explain it in understandable language.", [
        ok("sheet", "Approved information sheet", [-0.2, 1.2, 0.16], "You use the approved information sheet."),
        ok("explain", "Participant", [-1.3, 1.1, 0.3], "You explain the purpose, activities, time, foreseeable risks, and how information will be used.", { after: ["sheet"], early: "Start from the approved information sheet." }),
        ok("questions", "Invite questions", [0.3, 1.45, 0.2], "You ask whether they have any questions.", { after: ["explain"] }),
        bad("title", "Project title only", [1.1, 1.2, -0.2], "A title alone is not enough for informed consent."),
        bad("benefit", "Promise a benefit", [0.6, 1.2, 0.5], "You cannot promise a personal benefit. Describe only what the study offers.", { critical: true }),
      ]),
    task("They ask whether saying no could affect services they receive. Respond.",
      "A person may decline without penalty; do not use authority or services to pressure them.", [
        ok("voluntary", "Participant", [-1.3, 1.1, 0.3], "You explain that participation is voluntary and they can decline at any time."),
        ok("services", "Services desk sign", [1.35, 1.2, -0.3], "You confirm their services do not depend on joining.", { after: ["voluntary"] }),
        bad("pressure", "Encourage them to join", [0.6, 1.3, 0.4], "Using your position to pressure someone invalidates voluntary consent.", { critical: true }),
        bad("skip", "Skip the question", [0.0, 1.6, 0.0], "A direct question about consequences deserves a direct answer."),
      ]),
    task("They agree to hear more but have not completed the approved consent process. Proceed carefully.",
      "Follow the approved protocol and ethics review; pause if anything is unclear.", [
        ok("answer", "Participant", [-1.3, 1.1, 0.3], "You answer their questions."),
        ok("consent", "Consent form", [0.25, 1.2, 0.1], "They read and sign the approved consent form themselves.", { after: ["answer"], reveal: "signed", early: "Answer their questions before asking for a signature." }),
        ok("record", "Recorder", [0.0, 1.2, 0.5], "Only now do you start recording.", { after: ["consent"], critical: true, early: "You started recording before consent was complete. Do not collect study data first." }),
        bad("fill", "Fill in the form for them", [0.7, 1.4, 0.0], "The participant must complete their own consent. Do not fill it in for them.", { critical: true }),
      ]),
  ],
  "sample-custody": [
    task("Two samples are ready for labeling. Label them using the safest routine.",
      "Use the approved identifier and protect participant information.", [
        ok("protocol", "Study protocol", [-1.2, 1.2, 0.2], "The protocol assigns codes A-101 and A-102."),
        ok("labelA", "Vial A", [-0.43, 1.5, 0.1], "You label the first vial and verify its assigned code.", { after: ["protocol"], early: "Read the protocol for the assigned codes first." }),
        ok("labelB", "Vial B", [0.0, 1.5, 0.1], "You label the second vial and verify its code.", { after: ["labelA"] }),
        bad("memory", "Label from memory", [0.5, 1.5, 0.4], "Labeling from memory invites mix-ups. Use the assigned code and check it."),
        bad("name", "Write participant's name", [0.9, 1.4, 0.1], "Use the approved identifier. Names on containers expose participant information.", { critical: true }),
      ]),
    task("A label does not match the collection log. Handle the mismatch.",
      "Never resolve identity uncertainty by guessing or silently changing records.", [
        ok("secure", "Vial C", [0.43, 1.5, 0.1], "You pause and keep the item secure."),
        ok("document", "Collection log", [1.2, 1.2, 0.2], "You document the mismatch exactly as you found it.", { after: ["secure"] }),
        ok("ask", "Supervisor", [1.4, 1.3, 0.6], "You ask the supervisor how to proceed.", { after: ["document"] }),
        bad("change", "Change the label", [0.2, 1.2, 0.5], "Guessing which code is right and changing it corrupts the record.", { critical: true }),
        bad("discard", "Discard the log entry", [1.0, 0.8, 0.0], "Removing the entry hides the problem.", { critical: true }),
      ]),
    task("You hand the sealed sample to the next authorized team member. Record the handoff.",
      "Follow the approved handling, storage, and handoff procedure.", [
        ok("seal", "Sealed bag", [0.0, 1.3, 0.1], "You confirm the sample is sealed and undamaged."),
        ok("log", "Custody log", [1.2, 1.2, 0.2], "You record the transfer, time, condition, and recipient.", { after: ["seal"] }),
        ok("hand", "Team member", [1.4, 1.3, 0.6], "You hand it to the authorized recipient.", { after: ["log"], early: "Record the transfer first." }),
        bad("table", "Shared table", [-0.9, 0.8, 0.5], "A sample left without a handoff note can go missing or be mixed up."),
        bad("cooler", "Unapproved cooler", [0.2, 0.4, 0.7], "Unapproved storage can compromise the sample. Follow the approved method.", { critical: true }),
      ]),
  ],
  "field-observation": [
    task("Your team begins a repeated measurement route. Set up for comparable data.",
      "Follow the protocol consistently and record instrument or method issues.", [
        ok("check", "Instrument", [-0.2, 1.1, 0.1], "The instrument check passes."),
        ok("steps", "Protocol card", [-0.9, 1.3, 0.2], "You follow the same approved steps at every site."),
        ok("measure", "Sensor", [0.72, 1.7, 0.2], "You take the reading using the same method as last time.", { after: ["check", "steps"], early: "Check the instrument and read the steps first." }),
        bad("change", "Change the method", [0.3, 1.1, 0.5], "Changing the method per site makes readings incomparable."),
        bad("skip", "Skip the instrument check", [0.0, 0.5, 0.55], "Instruments drift. Check it each time and note any issue."),
      ]),
    task("One reading is much higher than the others. Record it.",
      "Record what happened; do not edit results to fit a hypothesis.", [
        ok("keep", "Field notebook", [-0.9, 1.3, 0.2], "You record the reading as measured, with units and context."),
        ok("flag", "Flag for review", [0.0, 1.5, -0.2], "You flag the reading for review by the team.", { after: ["keep"] }),
        bad("delete", "Delete the reading", [0.5, 1.2, 0.5], "Deleting an unexpected result hides real data.", { critical: true }),
        bad("edit", "Edit the value", [0.9, 0.9, 0.1], "Changing a record to fit the expected pattern is falsification.", { critical: true }),
      ]),
    task("A visitor asks to be included in your field notes with their name and photo.",
      "Minimize personal data and follow the approved consent, privacy, and publication plan.", [
        ok("plan", "Privacy plan", [-0.9, 1.3, 0.2], "You check the approved privacy and consent plan."),
        ok("explain", "Visitor", [-1.4, 1.1, 0.4], "You explain what the protocol allows and do not collect extra identifying details.", { after: ["plan"], early: "Check the privacy plan before you answer." }),
        bad("add", "Add name and photo", [0.2, 1.3, 0.5], "A casual offer is not consent under the approved protocol.", { critical: true }),
        bad("post", "Post photo publicly", [0.9, 1.3, 0.5], "Do not publish identifying photos without an approved plan.", { critical: true }),
      ]),
  ],
};
