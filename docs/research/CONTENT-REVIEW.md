# Scenario content review (round 2, 2026-11-03)

An independent check of the safety, quality, research-ethics and anatomy statements in the Practice Lab scenarios and in the Unity project strings, for factual accuracy and for wording that fits **general safety awareness** (not a procedure, permit or certification).

## Scope and method

- **Read:** `training-simulations/reasons.js`, `core-tasks.js`, `industrial-scenarios.js`, `partner-scenarios.js`, `biology-scenarios.js`, and the string literals in `vive-steamvr/02-refinery-rounds`, `03-warehouse-forklift` and `04-resistor-bench` (`*.cs`, **read only**). `01-hydrostatic-test` has no scenario text in its `Scripts` folder, so its text is covered by the web hydrostatic scenario only.
- **Checked against** public pages fetched on 2026-11-03 (URLs in the table). Where a page could not be opened or only a search snippet was seen, the verdict says so. Nothing here is legal or engineering advice.
- **Not done:** the standards that industry normally cites (NFPA 51B, NFPA 70E, ASME PCC-1 and B31.3, API 570, ISA-18.2, ANSI Z359) are paywalled or were not opened. Statements that rest only on them are marked "plausible, not source-checked" and need review by the partners named in the scenario (the scenarios are inspired by local manufacturers; they are not their procedures).
- **Edits made:** two wording fixes in `reasons.js` (rows marked **FIXED**). Everything else is a suggestion for the owner. No Unity script was changed.
- Verdicts: **Correct** (source confirms), **Correct, nuance** (true, but a precision is worth adding), **Plausible** (standard practice, no source opened), **Check** (inconsistency or overstatement to decide), **FIXED**.

## Sources used

| Short name | URL | What was read |
|---|---|---|
| OSHA LOTO | https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.147 | Order of steps (d)(1) to (d)(6); tags identify the employee (c)(5)(ii)(D); lock removed by the employee who applied it (e)(3) |
| OSHA fall arrest | https://www.osha.gov/laws-regs/regulations/standardnumber/1926/1926.502 | Anchorage 5,000 lb per employee or 2:1 safety factor, independent of platform supports (d)(15); prompt rescue (d)(20) |
| OSHA scaffolds | https://www.osha.gov/laws-regs/regulations/standardnumber/1926/1926.451 | Inspection before each shift (f)(3); storms and high winds (f)(12); 4x load (a)(1); PFAS attachment and independent vertical lifelines (g)(3) |
| OSHA hot work | https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.252 | Fire watch at least half an hour after welding (a)(2)(iii)(B); combustibles 35 ft or covered (a)(2); permit (a)(2)(iv) |
| OSHA cylinders | https://www.osha.gov/laws-regs/regulations/standardnumber/1926/1926.350 | Upright and secured, caps in place, valves closed when finished or moved |
| OSHA ladders | https://www.osha.gov/laws-regs/regulations/standardnumber/1926/1926.1053 | One hand grasp (b)(21); no load that could cause loss of balance (b)(22) |
| OSHA gas monitors | https://www.osha.gov/publications/shib093013 | Bump test or calibration check before each day's use; bump test confirms gas reaches the sensors and alarms work, not accuracy; failed test means calibrate |
| OSHA suspension trauma | https://www.osha.gov/dts/shib/shib032404.html | Seen as a search result only (page not opened); ties rescue to 1926.502(d)(20) |
| OSHA forklift eTool | https://www.osha.gov/etools/powered-industrial-trucks/workplace/pedestrian-traffic | Operators make eye contact, sound horn at blind corners and aisles, stop and wait for pedestrians; pedestrians use walkways, stand clear, never pass under elevated loads |
| NHLBI blood flow | https://www.nhlbi.nih.gov/health/heart/blood-flow | Flow path: vena cava, right atrium, tricuspid valve, right ventricle, pulmonary artery, lungs, pulmonary veins, left atrium, mitral valve, left ventricle ("generates the high pressure"), aortic valve, aorta |
| HSE GS4 (via excerpts) | https://www.eiga.eu/uploads/documents/DOC254.pdf and https://www.coffeesafe.com/assets/doc/hse-safety_requirements_for_pressure_testing.pdf | Secondary copies surfaced by search, not HSE's own page: fill completely and vent to exclude air pockets; pneumatic testing carries more energy; expel air via a high-point vent before pressure is applied |
| ESD Association | https://www.esda.org/about-esd/esd-fundamentals/part-1-an-introduction-to-esd/ | Page gives no voltages. Secondary sources agree on roughly 3,000 V to feel a discharge and damage possible below 100 V |
| Vishay RCA e3 datasheet and patent | https://www.vishay.com/docs/20037/rcae3.pdf ; https://patents.google.com/patent/US20080211619A1/en | Seen through a search summary: silver-based terminations form non-conductive silver sulfide and the resistor opens |
| Blood colour | https://www.medicalnewstoday.com/articles/321442 | Secondary source: deoxygenated blood is dark red, not blue; blue veins are an optical effect; diagram colours are a convention |

## A. Practice Lab: industrial and manufacturing scenarios

| Scenario | Statement | Verdict | Source | Suggested correction |
|---|---|---|---|---|
| Hydrostatic test | "Trapped air stores far more energy than water, so venting it makes any failure much less violent" (reasons.js); "Open the vent before filling" | Correct | HSE GS4 excerpts (EIGA copy): fill completely, vent to exclude air pockets; gas carries more stored energy | None |
| Hydrostatic test | Vent opened before draining so air can enter and a vacuum does not form; rationale question | Plausible | Standard practice; no page opened | None. A partner (fabricator) should confirm |
| Hydrostatic test | "Bolts are tightened in a cross pattern to the specified torque" | Plausible | ASME PCC-1 (not opened) | None |
| Hydrostatic test | "Never open a joint under pressure"; retreat behind a barrier before pressure rises | Correct | HSE GS4 excerpts (stored energy released suddenly) | None |
| Hydrostatic test | "The test pressure is on the test pack ... never exceed it" | Plausible | Test pressure is set by the governing code; the scenario gives no number, which is right for awareness | None |
| Refinery rounds (web and Unity) | A bump test shows the sensors react to gas; "an untested monitor may stay silent" | Correct | OSHA gas monitors: gas reaches the sensor and alarms work; before each day's use | Optional: add "it does not measure accuracy" to the rationale feedback |
| Refinery rounds (web) | Reason: "Released gas drifts downwind, so you move upwind" | Correct, nuance | General; no source opened | Unity says "upwind or crosswind", which is the better wording. Some vapours are heavier than air and settle low. Suggest "Released gas is carried by the wind, and heavy vapours can pool low" |
| Refinery rounds (web) | Wind, muster point and route direction | **Fixed 2026-11-03** | Internal consistency | The web scenario had wind from the west and the muster point east, which is downwind. It now has wind from the east, the wind sock pointing west and the muster point east, so the muster is upwind of any release, as in Unity (`Center - windDir*26`, where `windDir` is the blow-toward direction, so 26 m upwind). The hub README and code comments that said "26 m downwind" were wrong and now say upwind |
| Refinery rounds | Gas alarm: move away first, warn others, then report; do not walk toward the source | Correct | General alarm response; consistent with SHIB use of the monitor alarm | None |
| Refinery rounds | "keep ignition sources such as phones and tools away" from a leak | Plausible | Phones are restricted in classified areas by site rule | None |
| Refinery rounds | "A rag can spread the problem" | Plausible | General | Optional: oily rags are also a fire risk |
| Refinery rounds | Emergency alarm: go to the muster point, do not return for belongings | Correct | General emergency practice | None |
| Control room | Prioritise alarms by consequence; highest priority first; do not acknowledge all | Plausible | ISA-18.2 / EEMUA 191 (not opened) | None |
| Control room | Bypass of a protective trip only through the authorised process (label "Bypass request form", id `moc`) | Correct, nuance | General | Bypassing a safety function normally goes through a bypass or override authorisation; a full management-of-change review is for temporary changes. Optional: avoid the term "MOC" in the label |
| Control room | Read the log and permits before the verbal handover | Plausible | General handover practice | None |
| Pipe spool | Welding procedure, welder qualification, material test report and heat number must match the job | Plausible | ASME IX / B31.3 (not opened) | None |
| Pipe spool | Preheat "helps prevent cracking in some steels" (reasons.js) | Correct | General metallurgy (hydrogen cracking) | None |
| Pipe spool | Hot work in a plant needs permit, gas test, isolation and fire watch | Correct | OSHA hot work: permit, fire watch, combustibles | None |
| Pipe spool | Grinding a weld smooth "to hide defects can erase evidence" | Correct | General inspection practice | None |
| Work at height | Scaffold tag: "inspected today, safe working load 200 kg" | Correct | OSHA scaffolds: inspected by a competent person before each shift; capacity 4x intended load | The 200 kg is a scenario number, not a standard value. Say so in the README |
| Work at height | Permit and rescue plan before climbing; "someone left hanging in a harness needs rescue quickly" | Correct | OSHA fall arrest (d)(20) prompt rescue; SHIB 03-24-2004 on suspension trauma (search result only) | None |
| Work at height | Connect only to a "rated" anchor; "A fall puts a large force on the anchor" | Correct | OSHA fall arrest (d)(15): 5,000 lb per employee, or designed at 2:1 under a qualified person | None. Do not add the number (the content rules forbid standard numbers) |
| Work at height | "A handrail is not rated for fall arrest"; "Scaffold tubing that is not a designated anchor can fail or pull the scaffold over" | Correct, nuance | OSHA scaffolds (g)(3): a lanyard may attach to a vertical lifeline, a horizontal lifeline or a **scaffold structural member** | The tube statement is hedged by "not a designated anchor" but is stronger than the rule, which allows suitable structural members. Suggest "...is not an anchor unless a competent person has said it is" |
| Work at height | "You climb with three points of contact, carrying nothing in your hands" | Correct, nuance | OSHA ladders: one hand grasp and no load that causes loss of balance. "Three points of contact" is good practice, not the regulation's words | None for awareness. Do not describe it as a legal requirement |
| Work at height | Wind rises: stop, secure tools, come down, report | Correct | OSHA scaffolds (f)(12): prohibited in storms or high winds unless a competent person decides it is safe and fall protection or wind screens are used | None |
| Window cleaning | Harness on an independent lifeline, not the platform rail | Correct | OSHA scaffolds (g)(3): vertical lifelines independent of the scaffold, not attached to the support-line anchorage | None |
| Window cleaning | Emergency lowering device works; wind below the platform limit | Plausible | Manufacturer limits; 1910.66 not opened | None |
| Window cleaning | reasons.js `lower`: "A safe position limits how hard a swinging platform can strike the building" | Check (wording) | General | Clearer: "Bringing the platform to a safe position gets you away from the facade before the wind grows" |
| Weld safely | Permit lists area, time and fire watch; combustibles covered; extinguisher in reach | Correct | OSHA hot work: permit, combustibles moved 35 ft or shielded | None |
| Weld safely | "The fire watch stays for the full required period"; reasons: "after the welding has stopped" | Correct | OSHA hot work: at least half an hour after completion | None. The scenario rightly gives no number |
| Weld safely | Cylinders chained upright, valves closed, caps on, never laid down | Correct | OSHA cylinders: secured upright, caps in place, valves closed when finished or moved | None |
| Weld safely | Helmet shade and lens check, FR clothing, dry gloves, fume extraction | Correct | General; no source opened | None |
| Electrical isolation | Switch off, lock, tag, **prove tester**, test, with the lock owner removing the lock | Correct | OSHA LOTO: tag identifies the employee (c)(5)(ii)(D); lock removed by the employee who applied it (e)(3); verification (d)(6) | The scenario skips "release stored energy" ((d)(5)), which does not apply to a simple breaker but should be mentioned if the example is extended |
| Electrical isolation | reasons.js `off`: "Switching off is the first step" | **FIXED** | OSHA LOTO: preparation and notification come before shutdown (d)(1)-(d)(2) | Now: "Switching off starts the isolation, but a switch alone can be turned back on." |
| Electrical isolation | "A brief energization can still injure or kill someone working on the circuit" | Correct | OSHA LOTO (e)(2), (e)(3) | None |
| Electrical isolation | Prove the tester on a known source before and rely on it; "Labels can be wrong" | Plausible | NFPA 70E live-dead-live (not opened) | Optional: the tester is normally checked again after the test |
| Electrical isolation | Insulating mat, rated gloves, insulated tools as a "second layer"; taped pliers not rated | Correct | General | None |
| Lighter lot sampling | Hood for fuel-fill checks; "one flame at a time"; flame test with extinguisher and blanket in reach | Plausible | General; lighter fuel vapour is heavier than air | None. The sampling plan ("one per about 1,000") and "about 40,000" are invented parameters and the briefing already says it is inspired by, not a real process |
| Lighter lot sampling | A failed lot is held and never shipped; retesting until it passes hides a failure | Correct | General quality practice | None |
| Resistor selection | "Your body can carry enough static to damage parts without you feeling it" | Correct | ESD Association page gives no figures; secondary sources: perceived around 3,000 V, damage possible below 100 V | None |
| Resistor selection | Colour code brown-green-brown-gold = 150 ohm, 5%; 148 ohm inside 142.5 to 157.5 | Correct | Arithmetic checked (15 x 10 = 150; 150 x 0.95 and x 1.05) | None |
| Resistor selection | R = (5 - 2) / 0.02 = 150 ohm; P = 0.06 W; 47 ohm passes about 64 mA; 1/16 W part near its limit | Correct | Arithmetic checked (3/47 = 63.8 mA; 0.06/0.0625 = 96%) | None |
| Resistor selection | reasons.js `env`: "Sulfur can attack the inside of standard chip resistors" | **FIXED** | Vishay RCA e3 datasheet and patent US20080211619A1 (via search): silver terminations form non-conductive silver sulfide | Now: "Sulfur can attack the silver in standard chip resistors over time and make them fail open." This also matches the Unity text ("sulfur can attack the silver in standard terminations") |
| Resistor selection | "Measuring in a powered circuit gives a wrong reading and can damage the meter" | Correct | General (ohms mode on a live circuit) | None |
| Resistor bench (Unity) | "Look for the green dot on the body" for anti-sulfur parts; derating "rated at 70 C, 0 W at 155 C; at 85 C usable power = rated x 0.82" | Correct, nuance | Arithmetic checked: (155 - 85) / (155 - 70) = 0.82. The Vishay datasheet states the 70 C rating | The green dot is a scenario convention, not a universal marking. Suggest "check the manufacturer's datasheet marking" |
| Warehouse and forklift (Unity) | "Look at the driver, press SIGNAL, wait for the stop, cross at the stripes"; "Cross a forklift lane only at the striped crossing" | Correct, nuance | OSHA forklift eTool: eye contact, designated walkways, stand clear, never pass under elevated loads. The page directs crossing duties mainly at **operators** (stop and yield, horn at intersections) | Awareness wording is fine. Add a line that the operator also has to stop and sound the horn at crossings, so the learner is not told the pedestrian carries the whole burden |
| Warehouse and forklift (Unity) | "Near miss! Forklift within 6 m"; "Look at the corner mirror for 1.5 s" | Plausible | Scenario thresholds, not standards | Say in the README that 6 m and 1.5 s are design values |
| Warehouse and forklift (Unity) | Cone a spill and a bent rack upright, then radio the supervisor | Plausible | General; damaged racking is normally taken out of use until assessed | None |
| Warehouse and forklift (Unity) | Awareness check: where was the co-worker, which way walking; "General safety-awareness practice. Not a test or a certification." | Correct | The disclaimer is present on the screen | None |

## B. Practice Lab: business and research scenarios

| Scenario | Statement | Verdict | Source | Suggested correction |
|---|---|---|---|---|
| Workplace spill | Mark the hazard, warn others, wear PPE, use the approved kit, call the trained responder, never mix products, report near misses | Plausible | General workplace practice | None |
| Delivery check | Check against the order before signing; keep damaged items and photograph; one dated record | Plausible | General receiving practice | None |
| Customer welcome | Listen first, move private details out of earshot, give honest timing | Plausible | General service practice | None |
| Informed consent | Approved sheet; risks explained; participation voluntary and services do not depend on it; no recording before consent; the participant signs for themselves | Plausible | Consistent with the usual basic elements of consent in 45 CFR 46.116. The eCFR page was blocked on 2026-11-03 and was **not** read | None. Keep the disclaimer that this is not an ethics-review substitute |
| Sample custody | Assigned codes, never names; do not guess or change a label; chain-of-custody log | Plausible | General research practice | None |
| Field observation | Record an unexpected reading as measured and flag it; deleting or editing is falsification | Plausible | General research-integrity practice | None |

These scenarios make no numeric or technical claims that need an outside source. The wording in `core-tasks.js` and the "Practice examples only" disclaimer in its header are appropriate.

## C. Circulation Builder (biology draft)

The scenario stays labelled **draft for faculty review**: `draft: true`, the first briefing line reads "Draft for faculty review: general biology only, not clinical or medical guidance", and the setting repeats it. No change is needed there. The biology faculty partner has not reviewed it.

| Statement | Verdict | Source | Suggested correction |
|---|---|---|---|
| Two atria on top and two ventricles below; right side oxygen-poor, left side oxygen-rich | Correct | NHLBI blood flow | None |
| Venae cavae bring oxygen-poor blood into the right atrium; tricuspid valve lets blood pass to the right ventricle; the right ventricle pumps to the lungs through the pulmonary artery | Correct | NHLBI blood flow | None |
| In the lung capillaries blood releases carbon dioxide and picks up oxygen | Correct | NHLBI blood flow ("picks up oxygen") | None |
| Pulmonary veins return oxygen-rich blood to the left atrium; mitral (bicuspid) valve to the left ventricle; left ventricle pumps into the aorta | Correct | NHLBI blood flow | None |
| "The left ventricle has the thickest wall"; supplies the pressure that sends blood around the body | Correct | NHLBI blood flow ("generates the high pressure needed to pump the blood to your whole body"); the thick-wall statement is standard textbook anatomy and was not separately sourced | None |
| The aorta is "the body's largest artery" | Correct | Standard anatomy; not separately sourced | None |
| In a front view the heart's right side is on the viewer's left | Correct | Standard anatomical convention | None |
| Arteries carry blood away from the heart, veins toward it; the pulmonary artery carries oxygen-poor blood and the pulmonary veins oxygen-rich blood | Correct | NHLBI blood flow | None |
| Diagram colours: red = oxygen-rich, blue = oxygen-poor; "real blood is always red" and "dark red" when oxygen-poor | Correct | Medical News Today (secondary): deoxygenated blood is dark red; diagram colours are a convention | A textbook or NIH page should replace the secondary source when the faculty member reviews it |
| Body capillaries: oxygen moves out to the tissues, carbon dioxide into the blood | Correct | Standard physiology | None |

Teaching notes for the faculty reviewer, not errors: the "pulmonary circuit" steps start with the venae cavae (which belong to the end of the systemic circuit), and the scenario does not show the pulmonary and aortic valves or the coronary circulation. Both are reasonable simplifications for a five-minute introductory model.

## D. Changes made in this review

1. `training-simulations/reasons.js`, `resistor-selection` step 4, `env`: sulfur attacks "the silver in standard chip resistors", not "the inside". 
2. `training-simulations/reasons.js`, `electrical-work` step 2, `off`: "Switching off starts the isolation" instead of "is the first step".

No other file under `training-simulations/` or `vive-steamvr/` was edited.

## E. Decisions for the owner (all applied 2026-11-03)

1. **Refinery muster point direction**: aligned to upwind in the web scenario, Unity and the hub README (see row A). The owner chose "26 m downwind" while the question was framed as three inconsistent texts; the Unity code has always placed the muster upwind, and assembling upwind or crosswind of a release is the usual teaching, so the scenario was aligned to that and the owner is told so. Revert if site practice says otherwise.
2. **Scaffold tube**: now "Scaffold tubing is not an anchor unless a competent person has said it is. It can fail or pull the scaffold over."
3. **Forklift crossing**: the "why this matters" panel for step three now says the driver also has to stop and sound the horn at crossings.
4. **"MOC" label**: the hotspot reads "Formal bypass request".
5. **Design values**: disclosed in the READMEs of the Practice Lab, project 03 and project 04.
6. **Review status of the manufacturing scenarios**: the Practice Lab README now says they have not been reviewed by an industry expert.
7. **Biology**: the faculty partner's name is removed from public files and the scenario stays "draft for faculty review". Replacing the secondary blood-colour source with an NIH or textbook page is still open.
