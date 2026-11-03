# Testing guide

Two kinds of testing: **automated suites** anyone can re-run in a minute, and **hands-on checks** that need a person at a keyboard, or in a headset. Record hands-on results in the Result column (Pass, Fail or Skipped, with a note) and send the file back so the READMEs and decks can be corrected honestly.

Status on November 2, 2026: every automated suite below passes on the development PC. **No hands-on check has been done by a person yet, except the project 01 desktop play the owner mentioned and a look at Refinery Rounds**; the Unity projects were checked by scripted editor runs and screenshots, and nothing has run on a Vive Pro 2 or a Quest. General safety-awareness practice only; none of the scenarios is a procedure or certification.

## 1. Automated suites (run from the repository root)

| Suite | Command | Expected today |
|---|---|---|
| Practice Lab (catalog, scenarios, biology draft, why-this-matters, thumbnails) | `cd training-simulations`, then `node --test test/*.test.mjs` | 27 pass |
| Brand kit (brand bar, text fitting, sky logic, logo rules) | `cd brand-kit`, then `npm test` | 16 pass |
| Multiplayer relay | `cd multi-player`, then `npm test` | 31 pass |
| Web UI layout, desktop and phone width, logo blocked and allowed | start the relay (see HOW-TO-RUN section 1), then `node tools/vr-capture/ui-check.mjs` (set `UI_BASE` if the relay is not on port 8132) | 48 pass |
| Offline check (no network, no external requests) | `node tools/vr-capture/offline-check.mjs` (`STATIC_BASE` and `RELAY_BASE` select the server) | all pages ok |
| Launcher check (tiles, guide links, in-menu viewer, search box and keyboard) | `node tools/vr-capture/launcher-check.mjs` (set `LAUNCHER_URL`, for example `http://localhost:8132/`, if the server is not on port 8101) | pass |
| Documentation (every link, anchor, launcher path, docs index, and the pass counts above against a fresh run) | `npm run docs-check` (add `--no-run` to skip running the suites) | ok |
| Unity compile | open each project; the Console shows no red errors | no errors |

Record a result for the dashboard with `node tools/status/record-results.cjs "<suite>" <passed> <failed> <skipped>`.

## 2. Hands-on desktop checks for the Unity projects (no headset)

These checks need a Windows PC, a keyboard and a mouse. No headset is used. Nothing in this section has been done by a person yet (see the status paragraph at the top), so every Result cell starts empty. General safety-awareness practice only; none of the scenarios is a procedure or certification.

### 2.1 Set up once

1. Install Unity Hub and the editor **6000.3.20f1** (built-in render pipeline). Add the Windows Build Support module only if you also want the optional player build.
2. Use a **full clone of this repository**. Do not copy a single project folder out of it: each project loads the shared kit from `vive-steamvr\packages\com.pittbradford.xrtraining` through a relative `file:` path in its `Packages\manifest.json`.
3. In Unity Hub choose **Add > Add project from disk** and select the project folder from the table below. The first open imports assets and compiles scripts; it was not timed, so allow several minutes.
4. If Unity offers **Safe Mode**, a script failed to compile. Copy the first red message from the dialog or the Console and send it back.
5. In the **Project** window open the scene (double-click the file from the table). The scenes and OpenXR settings are committed, so **Pitt-Bradford > Set up project** should not be needed. That has not been tried on a fresh clone; if the scene is missing or the Hierarchy holds only a "Scenario" object, run **Pitt-Bradford > Set up project** once and note that you had to.
6. Close SteamVR if it is running. What happens with SteamVR open and no headset connected has not been checked.
7. Open **Window > General > Console** and click **Clear**. Press **Play** (top centre), then **click once inside the Game view** so it receives the keyboard and mouse. Press Play again to stop.

| Project | Folder | Scene to open |
|---|---|---|
| 01 Hydrostatic Test Bay | `vive-steamvr\01-hydrostatic-test` | `Assets\Scenes\HydrostaticTest.unity` |
| 02 Refinery Rounds | `vive-steamvr\02-refinery-rounds` | `Assets\Scenes\RefineryRounds.unity` |
| 03 Warehouse and Forklift | `vive-steamvr\03-warehouse-forklift` | `Assets\Scenes\WarehouseForklift.unity` |
| 04 Resistor Bench | `vive-steamvr\04-resistor-bench` | `Assets\Scenes\ResistorBench.unity` |

**Desktop controls (all four projects).**

| Action | Keys | Notes |
|---|---|---|
| Move | W A S D | Relative to where you look, about 2 m/s. You cannot leave the play area. |
| Look | Hold the **right mouse button** and move the mouse | The view only turns while the button is held. |
| Press a button, valve or answer | **Left click** | The pointer is the centre of the view: no dot is drawn. A pointer wand sits at the lower right and its line ends on what you aim at. Range about 4 m. |
| Grab | Aim at the object and **hold E** | Range about 3 m. The object follows the wand while E is held and drops when you release E. |
| Answer a "Why this matters" question (projects 01 and 02) | Click an option, or press 1, 2 or 3 | |

### 2.2 The checks

Use one project per row unless the Project column says "all four". "Board" means the task board in the scene.

| # | Project | Do this | Pass looks like | Result |
|---|---|---|---|---|
| D1 | all four | Open the project, open the scene, press Play. | No Safe Mode prompt. The Console shows no red lines (a warning is fine). The scene shows a room or yard, a task board and a University of Pittsburgh at Bradford plate. | |
| D2 | all four | In Play, hold the right mouse button and move the mouse left, right, up and down for 10 seconds. | The view turns smoothly and stays where you leave it. It does not snap back to the start direction. (A scripted capture run once showed the camera being reset by a pose driver. Normal play is expected to be fine but has not been checked by a person.) | |
| D3 | all four | Walk with W A S D in all four directions, then keep walking into a wall or fence. | You move in the direction you are looking. You stop at the edge of the area and do not walk through walls. | |
| D4 | all four | 01: click the **VENT** wheel (top of the shell). 02: look down and click **PHONE OFF** on the chest radio. 03: hold **E** on the hard hat at the PPE table, keep E held and click **WEAR**. 04: click the **4.7 k ohm** button on the answer panel. | 01: the wheel turns and its lamp goes from red to green. 02: the button text changes to "PHONE OFF: OK" and the board says "Phone off: no ignition source on you.". 03: the hard hat disappears from the table and the PPE panel shows "HARD HAT ON". 04: the board says "Bands give 4.7 k ohm, 5%. Now confirm with the meter: both probes on the two leads.". | |
| D5 | all four | From the start position, look for the plate, then walk close to an equipment name tag. | The plate reads "University of Pittsburgh Bradford" with the line "CIST \| Computer Information Systems & Technology". Name tags turn to face you as you move. No text shows through walls. (01: back wall. 02: top of the gate. 03: next to the board. 04: wall above the three cards.) | |
| D6 | 01 | Play the whole test (section 2.3). | Each of the six board lines gets a green "OK" and a "WHY THIS MATTERS" panel for about 7 seconds. At the end one three-option question appears. After you answer, every option shows a line of feedback, the best option turns green, and the board adds a separate "Reasoning:" line. | |
| D7 | 02 | Play from the gate to the muster point (section 2.4). In task 3 click the small tan **RAG** pad on the ground near flange F-12 as your deliberate mistake. | The board prints "A rag spreads the problem. Report the leak." and the footer reads "Total mistakes: 1   Critical: 0". All four tasks complete, a "WHY THIS MATTERS" panel follows each step, the question appears after CHECK IN, and the Console does not fill with red lines. | |
| D8 | 02, 01 | Read the conditions board by the gate (02) or on the right wall (01, "BAY CONDITIONS"). Do the check once with internet and once with the network off if you can. | The "Source:" line says "Open-Meteo, Bradford PA (live)" with a network, or "simulated (no network)" without one (02 may show "(live fetch pending)" for a few seconds first). | |
| D9 | 03 | Do steps 1 to 3 of section 2.5 (PPE, mirror, signal and cross), then keep walking north and wait. | After you leave the crossing, the forklift is sent to the west wall 8 seconds later. It stops in the marked LOADING ZONE, a co-worker in a hi-vis vest comes out of the STAFF ENTRANCE door, and 4.5 seconds after that the scene freezes. A dark shell blanks the view behind a panel headed "AWARENESS CHECK, Question 1 of 3". The time from crossing to freeze was not measured (the forklift drives there from wherever it is). If nothing happens within 2 minutes, record that. The freeze can come while you are still placing cones; that is expected. | |
| D10 | 03 | Answer the three questions by clicking, then do not move: stand still and keep the co-worker in view. | The scene resumes. A panel says "STOP where you are. WATCH them. WAIT until they are clear." The forklift bubble reads "Holding: person behind me" while the co-worker is close. After the event the panel reads "Stopped, watched the co-worker, and waited until clear." Finish the four board steps: the "SCENARIO SUMMARY" shows the board result and a separate "AWARENESS CHECK" section scored out of 3. | |
| D11 | 03 | Stop. In the Hierarchy select the object **Scenario**. In the Inspector untick **Coworker Hazard** on the Warehouse Forklift component. Press Play and repeat section 2.5. | No co-worker, no questions, no stand called "FORKLIFT LOADING?" and no summary panel. The four board steps behave as in section 2.5 and the board shows its own "STRONG PRACTICE" or "Complete with N mistakes" line. Tick the box again afterwards. | |
| D12 | 04 | Play the whole check (section 2.6). | Strap snaps on. The meter reads about 4.7 k. A suitable 150 ohm part passes CHECK PART. The anti-sulfur 150 ohm part in R1 lights the LED. A 47 ohm part burns the spare LED out with a puff of smoke and a clear message. | |
| D13 | 04 | Stand at the start and read the bin labels, the wall cards and the answer panel. Try to click each answer button. | Everything is readable from standing height. Nothing blocks the parts or the buttons. | |
| D14 | all | Anything that feels wrong: text too small, objects in the way, panels you cannot click, frame rate drops. | Write down what and where. Screenshots help (Game view, then Win+Shift+S). | |
| D15 | 01 | Run the test twice more. Run A: press the green **PRESSURIZE** button before fitting the blind flanges. Run B: finish steps 1 to 4, then stand in front of the yellow line during the hold. | Run A: board message "The nozzles are open. Fit both blind flanges before pressurizing." counted as critical. Run B: "Stay behind the yellow line while the vessel is under pressure." counted as critical, then "Step back behind the yellow line. The hold restarts." | |
| D16 | 03 | Play the four board steps (section 2.5) without the co-worker event if it is in the way (see D11). | Each step turns to "OK": PPE, mirror, cross, cones and radio. After the radio press the supervisor panel reads "SUPERVISOR: Copy. Spill and damaged upright reported. Crew on the way." | |
| D17 | 03 | With PPE on, step onto the striped crossing without pressing SIGNAL while the forklift is within about 6 m and moving. | A red flash with "NEAR MISS Forklift within 6 m", a horn, the forklift brakes, and the board counts a critical miss ("Near miss! Forklift within 6 m. Wait at the edge and signal."). This depends on the forklift's position on its loop, so you may need to wait at the edge until it is close. | |

### 2.3 Project 01, Hydrostatic Test Bay: the full run

You start south of the heat exchanger, facing it. The task board is on the left, the "BAY CONDITIONS" board on the right wall, the blind flanges lie on a table at the left (tag "BLIND FLANGES"), the VENT wheel is on top of the shell, FILL is low on the right end, DRAIN is low on the left, and the pump buttons are on the right-hand pump stand. The yellow line on the floor is labelled "STAY BEHIND THIS LINE DURING THE PRESSURE HOLD".

| Step | Do this | Expect on the board |
|---|---|---|
| 1 | Walk to the table, hold **E** on a blind flange, carry it to a top nozzle (a yellow disc marks the spot) and release E. Repeat for the second flange. | "Blind flanges fitted." |
| 2 | Click **VENT**, then click **FILL**. Watch the LEVEL sight glass fill (about 8 seconds). | "Vent and fill open. Watch the sight glass." |
| 3 | When the glass is full, click **VENT** (close), then **FILL** (close). | "Full and sealed." |
| 4 | Click the green **PRESSURIZE** button. The gauge and the PSI panel rise (about 14 seconds). | "At test pressure: 150 psi." |
| 5 | Stand behind the yellow line (south of it) for 15 seconds. The footer counts "Holding: n / 15 s". | "Held 15 s. No leaks seen at the gauge." |
| 6 | Click the red **STOP** button, then **DRAIN**. When the gauge is below 20 psi (about 5 seconds), click **VENT**. (Opening VENT earlier prints "Release pressure through the drain first; open the vent last.") | "Depressurized and vented." Then the summary ("STRONG PRACTICE: no mistakes." if you made none) and the question. |

The question is "Why must the shell be completely full of water, with no air left inside, before you raise the pressure?" The best answer is the second option ("Trapped air stores far more energy under pressure, so a failure would be much more violent."). With the best answer the board line reads "Reasoning: best answer (why no air may be left in the shell). Scored apart from the procedure."; otherwise "Reasoning: review why no air may be left in the shell. Scored apart from the procedure."

### 2.4 Project 02, Refinery Rounds: the full run

You start outside the fence, south of the gate (a yellow sign "UNIT ENTRY: GAS MONITOR REQUIRED BEYOND THIS GATE"). The pre-entry table with the gas monitor and the green BUMP TEST unit is to the left of the gate, the wind sock is to the right. The chest radio (ENTERING, WARN, REPORT, PHONE OFF) hangs below your view: look down to see it. The board follows your view and replaces itself for each of the four tasks.

| Task | Do this | Expect |
|---|---|---|
| 1 | Look at the wind sock for about a second. | "Wind from <direction> at N mph. Stay upwind." |
| 1 | Look at the green muster flag (up to 70 m away) for about a second. | "Muster point noted: upwind side of the unit." |
| 1 | Hold **E** on the gas monitor, keep it near the BUMP TEST unit (within about 2.6 m on desktop) and click **BUMP TEST**. Wait 2.5 seconds, then release E (the monitor clips to your chest). | "Bump test passed. Keep the monitor with you." |
| 1 | Click **ENTERING** on the chest radio, then walk through the gate. | "Control room logs your entry." then "Entry logged. Next: walk to gauge G-1." |
| 2 | Walk to gauge G-1 (gold marker) and stand within about 2 m for a second. | "ALARM: your monitor reads gas. Act now." Move away from the drifting cloud (at least 10 m from the release, crosswind is easiest), then click **WARN**, then **REPORT**. |
| 3 | Walk toward flange F-12. | "You smell hydrocarbon: a weep at flange F-12." Click **PHONE OFF**, stay 3.5 m or more clear. Four cones stand on a pallet at the barricade station: carry three and drop them 2 to 7 m from the flange, at least 1.5 m apart and spread around it (tape strings itself). Then click **REPORT**. |
| 4 | A siren sounds. Walk to the green muster flag by a route that avoids the gas (within 3.5 m), then click **CHECK IN** on the muster sign. | "Checked in. Everyone is accounted for." The board shows the summary, then the question "Your gas monitor switched on and read 0. Why bump-test it before you enter the unit?" The best answer is the third option. |

Other mistakes you can try: entering across the fence instead of the gate, entering before the checks are done (critical), clicking the flange itself in task 3 ("Never tighten a leaking joint yourself.", critical), or walking to the orange tool bag in task 4 ("Never go back for belongings during an alarm.", critical).

### 2.5 Project 03, Warehouse and Forklift: the full run

You start at the entry, south of the yellow entry line. The board hangs on the left, the PPE table with a hard hat, a hi-vis vest and safety glasses is in front of you with a PPE STATION panel and a green WEAR button. A forklift drives a loop around the racking.

| Step | Do this | Expect |
|---|---|---|
| 1 | Hold **E** on one PPE item, keep E held and click **WEAR**. Repeat for all three items. | The PPE panel shows ON for each item. Board: "PPE on: hard hat, vest and safety glasses." |
| 2 | Walk onto the entry walkway and look at the round convex mirror on the cargo stack for 1.5 seconds. | The mirror lamp turns green. Board: "Mirror checked: forklift traffic comes from the west." |
| 3 | Look at the driver, then click **SIGNAL** on the "SIGNAL THE DRIVER" post (within 8 seconds of looking). Wait until the forklift has stopped, then walk over the striped crossing and off the north side. | "The driver waved and is stopping. Cross only when it has stopped." then "Forklift stopped and driver aware. Cross now." then "Crossed at the stripes after eye contact and a full stop." |
| 4 | Find the spill (floor) and the bent rack upright. Carry a cone from the tray to within 2 m of each and release E. Then click **RADIO** at the supervisor station. | "Spill marked with a cone.", "Damaged upright marked with a cone.", then "Reported by radio: spill and damaged upright, both coned off." |

The co-worker event starts after step 3 (see D9).

### 2.6 Project 04, Resistor Bench: the full run

You stand in front of the bench. From left to right: GROUND HOOK, PARTS DRAWER (six bins), "WHAT IS THE VALUE?" answer panel, CHECK PART button with the SELECTION TRAY, MULTIMETER, LED TEST BOARD (places R1 and LED1), 5 V BENCH SUPPLY. Yellow discs mark where parts snap. The unknown part lies on the card in front of the drawer.

| Step | Do this | Expect |
|---|---|---|
| 1 | Hold **E** on the wrist strap (label on the mat), move it to the ground hook and release E. | "Strap clipped to the ground hook. You can handle parts now." (Grabbing any part first gives "You touched parts without the ESD strap on. Clip it to the hook first.") |
| 2 | Read the bands of the unknown part. Click **4.7 k ohm**. Hold E on a meter probe, put its tip on one lead of the unknown part and release E (it clips within about 12 cm). Do the same with the other probe on the other lead. | The meter shows about "4.7 k". Board: "Meter reads ... ohm: matches the bands (4.7 k, 5%)." (A wrong answer gives "That is not what the bands say. Yellow-violet = 47, red = x100.") |
| 3 | Take a 150 ohm part from a bin, put it on the yellow disc of the selection tray, click **CHECK PART**. | A pass reads "Part OK: 150 ohm, 5%, 1/8 W. Needs 150 ohm, 0.060 W vs 0.103 W derated." Wrong parts print a reason, for example "1/16 W derates to 0.051 W at 85 C, below the 0.060 W the circuit dissipates." or "1/2 W is oversize: ...". |
| 4 | Put the **anti-sulfur** 150 ohm part (label "150 ohm 1% 1/8 W ANTI-SULFUR", green dot on the body) on R1, an LED on LED1, and click the supply switch (display goes to "5.00 V" and "OUTPUT ON"). | The LED lights. Board: "LED lit at about 20 mA with an anti-sulfur 150 ohm part." (If step 3 is not done yet: "Test the part in the selection tray first (step 3).") |
| Burn-out | Switch the supply off, take the first part out of R1, put the 47 ohm part in R1 (and the spare LED, from the mat, in LED1 if the first LED burned) and switch on. | A puff of smoke, the LED goes dark, and the board reads "... ohm lets 64 mA through, over the LED's 30 mA limit. The LED burned out." (critical). |

### 2.7 Baked scenes (all four projects and the hub)

Baked means the scene's objects are saved in the scene file instead of being built at Play. Counts below are GameObjects in the scene file (the baked objects plus the one "Scenario" object that holds the script).

| # | Check | Pass looks like | Result |
|---|---|---|---|
| B1 | Open a project scene and look at the **Hierarchy** before pressing Play. | Many objects are listed, not only "Scenario": 223 in 01, 393 in 02, 628 in 03, 399 in 04 (the READMEs give the baked count, one less). | |
| B2 | Press Play. | The scene looks and behaves as it did before baking. No new red errors in the Console. | |
| B3 | Stop. Select a baked object (for example the table), move it, save the scene (Ctrl+S) and press Play. | The object is at its new position at Play and there is no second copy. Undo the move afterwards (Ctrl+Z, save again) so you do not commit it. | |
| B4 | Stop. **Pitt-Bradford > Unbake scene**, press Play, then **Pitt-Bradford > Bake scene (play mode)** again. | After Unbake the Hierarchy holds only "Scenario" and the scene still plays. After the re-bake the objects are back with no duplicates. Revert the scene file with `git checkout` afterwards if you do not want the changed file. | |

### 2.8 Results to send back

Copy this block into a message or a file, fill it in and send it back. Leave a row out only if you skipped it, and then write Skipped.

```
Tester:            Date:
PC (CPU, GPU, RAM):
Unity editor version (Help > About):      Commit (git rev-parse --short HEAD):
Projects checked:

| # | Project | Result (Pass / Fail / Skipped) | What you saw (exact message text, and the first red Console line if any) |
|---|---|---|---|
| D1 |  |  |  |
| D2 |  |  |  |
| D3 |  |  |  |
| D4 |  |  |  |
| D5 |  |  |  |
| D6 | 01 |  |  |
| D7 | 02 |  |  |
| D8 | 02 / 01 |  |  |
| D9 | 03 |  |  |
| D10 | 03 |  |  |
| D11 | 03 |  |  |
| D12 | 04 |  |  |
| D13 | 04 |  |  |
| D14 | all |  |  |
| D15 | 01 |  |  |
| D16 | 03 |  |  |
| D17 | 03 |  |  |
| B1-B4 | each project |  |  |

Did you have to run "Pitt-Bradford > Set up project"?  yes / no
Approximate minutes from opening the project to the first Play:
Anything you would change:
```

If you can, attach the Editor log (`%LOCALAPPDATA%\Unity\Editor\Editor.log` on Windows) and a screenshot of any failure.

## 3. Hands-on checks for the hub lobby

The hub (`vive-steamvr\hub`) needs the owner's local Asset Store packs for its dressed Hydrostatic Test Bay: those files are not in the repository, so a clean clone cannot run check L6 or build that portal's scene. The lobby itself is built from primitives. Only the owner's PC is expected to run this section.

1. Open `vive-steamvr\hub` in Unity Hub with editor **6000.3.20f1**.
2. Run **Pitt-Bradford > Build hub: lobby and all scenarios**. It saves five scenes under `Assets\Scenes\Local\` and lists the lobby first in the build settings.
3. Open `Assets\Scenes\Local\Lobby.unity` and press Play. Desktop controls are the same as in section 2.1.

| # | Check | Pass looks like | Result |
|---|---|---|---|
| L1 | Walk onto one of the four pads and stay about 1.4 seconds (progress shows on the pad), or press **1** to **4**. Do it for each pad. | The matching scenario loads: 1 Hydrostatic Test Bay, 2 Refinery Rounds, 3 Warehouse & Forklift, 4 Resistor Bench. | |
| L2 | In a scenario press **Backspace**, or hold both grips for 1.5 seconds in a headset. | You are back in the lobby. (A hint shows for the first 8 seconds of each scenario. Not checked by a person; only a portal load was tried in the editor.) | |
| L3 | Look at the thumbnails on the portals and at the season chip on the welcome board. | Four pictures; the chip names the season. | |
| L4 | Wait about 30 seconds in the lobby. | The sky changes season with a short pixelate and scan-line glitch, then clears. | |
| L5 | Look at the university plate and the CIST line. | The logo and "CIST \| Computer Information Systems & Technology" are readable. | |
| L6 | Open `Assets\Scenes\Local\HydrostaticTest_Hub.unity`, press Play, then press **P**. | Post-processing toggles off and on. | |

Known wording to fix: the portal blurbs in `LobbyHub.cs` say "Pressure-test a pipe spool" (the scenario tests a heat exchanger) and "Spot hazards, drive and stack safely" (you are a pedestrian; there is no driving or stacking). Report anything else that reads wrongly.

For the results, use the same block as in section 2.8 and replace the rows with L1 to L6.

## 4. Hands-on checks for the web Practice Lab (desktop browser)

Start the demo (HOW-TO-RUN section 1), open the launcher, choose Practice Lab.

| # | Check | Pass looks like | Result |
|---|---|---|---|
| W1 | Catalog | 17 cards, each with a thumbnail; the sky behind changes season about every 30 seconds with a glitch; "Autumn sky" style chip | |
| W2 | Play a hydrostatic test or refinery scenario | After a correct action a "Why this matters" panel appears; Esc or "Got it" closes it | |
| W3 | Finish a scenario | One reasoning question; the result screen has a separate "Reasoning" box; the catalog card shows tasks and reasoning | |
| W4 | Scenario 17 Circulation Builder | Labelled draft for faculty review; no reasoning question | |
| W5 | Narrow the window to phone width | Panel becomes a bottom sheet with the scene above it; no horizontal scroll | |
| W6 | Open Image credits in the page footer | Each sky image is credited and none is claimed to be Bradford | |

## 5. First real-headset run

Use [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md). It includes the new features: answering a "Why this matters" question with the trigger, the warehouse awareness check, and the sky glitch in stereo (the effect most likely to need a comfort check).

## 6. Reporting

For each failure note the project or page, the step, what you expected, what happened, and a screenshot if you can. The Unity Console text (red lines) is the most useful detail.
