# Testing guide

Two kinds of testing: **automated suites** anyone can re-run in a minute, and **hands-on checks** that need a person at a keyboard, or in a headset. Record hands-on results in the Result column (Pass, Fail or Skipped, with a note) and send the file back so the READMEs and decks can be corrected honestly.

Status on November 2, 2026: every automated suite below passes on the development PC. **No hands-on check has been done by a person yet, except the project 01 desktop play the owner mentioned and a look at Refinery Rounds**; the Unity projects were checked by scripted editor runs and screenshots, and nothing has run on a Vive Pro 2 or a Quest. General safety-awareness practice only; none of the scenarios is a procedure or certification.

## 1. Automated suites (run from the repository root)

| Suite | Command | Expected today |
|---|---|---|
| Practice Lab (catalog, scenarios, biology draft, why-this-matters, thumbnails) | `cd training-simulations`, then `node --test test/*.test.mjs` | 20 pass |
| Brand kit (brand bar, text fitting, sky logic, logo rules) | `cd brand-kit`, then `npm test` | 16 pass |
| Multiplayer relay | `cd multi-player`, then `npm test` | 18 pass |
| Web UI layout, desktop and phone width, logo blocked and allowed | start the relay (see HOW-TO-RUN section 1), then `node tools/vr-capture/ui-check.mjs` (set `UI_BASE` if the relay is not on port 8132) | 48 pass |
| Offline check (no network, no external requests) | `node tools/vr-capture/offline-check.mjs` (`STATIC_BASE` and `RELAY_BASE` select the server) | all pages ok |
| Launcher check | `node tools/vr-capture/launcher-check.mjs` (set `LAUNCHER_URL`, for example `http://localhost:8132/`, if the server is not on port 8101) | pass |
| Unity compile | open each project; the Console shows no red errors | no errors |

Record a result for the dashboard with `node tools/status/record-results.cjs "<suite>" <passed> <failed> <skipped>`.

## 2. Hands-on desktop checks for the Unity projects (no headset)

Open each project in Unity Hub with editor **6000.3.20f1**, open the scene, press Play with no headset connected. Desktop controls: W A S D move, hold the **right mouse button** and move to look, left click presses, hold E to grab.

| Project | Folder | Scene |
|---|---|---|
| 01 Hydrostatic Test Bay | `vive-steamvr\01-hydrostatic-test` | `Assets\Scenes\HydrostaticTest.unity` |
| 02 Refinery Rounds | `vive-steamvr\02-refinery-rounds` | `Assets\Scenes\RefineryRounds.unity` |
| 03 Warehouse and Forklift | `vive-steamvr\03-warehouse-forklift` | `Assets\Scenes\WarehouseForklift.unity` |
| 04 Resistor Bench | `vive-steamvr\04-resistor-bench` | `Assets\Scenes\ResistorBench.unity` |

| # | Project | Check | Pass looks like | Result |
|---|---|---|---|---|
| D1 | all four | Opens without red Console errors (a Safe Mode prompt means a compile error: send me the message) | Scene opens, Play starts | |
| D2 | all four | **Mouse-look**: hold the right mouse button and move | View turns smoothly and does not snap back (the capture run once showed the camera rotation being reset; normal play is expected to be fine) | |
| D3 | all four | Movement with W A S D | Moves relative to where you look; cannot walk out of the area | |
| D4 | all four | Click a button or valve, hold E to grab | The object responds; held objects follow and drop on release | |
| D5 | all four | University and CIST plate visible near the start; name tags face you | Plate readable; tags turn toward you; no text showing through walls | |
| D6 | 01 | Play the whole test: blind flanges, vent and fill, raise to 150 psi, hold from behind the line, drain, vent | After each step a "Why this matters" panel appears; at the end one question; the task board shows a separate "Reasoning" line | |
| D7 | 02 | Play from the gate to the muster point; make one deliberate mistake | Rounds complete; mistake is flagged; "Why this matters" panels and the final question appear; no error spam in the Console | |
| D8 | 02 | The wind board and sock | Board says live weather, or "simulated (no network)" without internet | |
| D9 | 03 | Finish the crossing step, then wait about 8 seconds | A co-worker walks past the forklift; the scene freezes after a few seconds and asks three questions; you answer by clicking | |
| D10 | 03 | After the questions, stop, watch, wait | A prompt tells you to stop, watch and wait; the forklift holds until the co-worker is clear; the final summary shows an "Awareness check" section apart from the task score | |
| D11 | 03 | Original flow with the hazard switched off (select the scenario object, untick `CoworkerHazard`) | The scenario plays as before with no door, co-worker or questions | |
| D12 | 04 | ESD strap, read the color code, measure the unknown part, pick a resistor, test it on the LED board | Strap snaps on; meter reads; right part lights the LED; a wrong part burns it out with a clear message | |
| D13 | 04 | Bin labels, wall cards and answer panel | All readable from the standing position; nothing blocks the parts | |
| D14 | all | Anything that feels wrong: text too small, objects in the way, panels you cannot click | Note what and where; screenshots help | |

### Baked scenes (all four projects and the hub)

| # | Check | Pass looks like | Result |
|---|---|---|---|
| B1 | Open a project scene (02, 03, 04, 01) and look at the Hierarchy before pressing Play | Hundreds of GameObjects are listed (for example 393 in 02, 628 in 03, 399 in 04, 223 in 01), not just a "Scenario" object | |
| B2 | Press Play | The scene looks and behaves as it did before baking; no new red errors in the Console | |
| B3 | Stop; select a baked object (for example the table), move it, save, press Play | The object is at its new position at Play and there is no duplicate | |
| B4 | **Pitt-Bradford > Unbake scene**, press Play, then **Bake scene (play mode)** again | Unbake leaves only the Scenario object and the scene still plays; re-bake restores the objects with no duplicates | |

## 3. Hands-on checks for the hub lobby

Open `vive-steamvr\hub`, run **Pitt-Bradford > Build hub: lobby and all scenarios**, open the `Lobby` scene and press Play.

| # | Check | Pass looks like | Result |
|---|---|---|---|
| L1 | Walk onto each of the four pads, or press 1 to 4 | The matching scenario loads | |
| L2 | In a scenario, hold both grips for 1.5 s or press Backspace | Back in the lobby | |
| L3 | Thumbnails on the portals; the season chip on the welcome board | Four pictures; chip names the season | |
| L4 | Wait about 30 seconds | Sky changes season with a short pixelate and scan-line glitch, then clears | |
| L5 | The university plate and CIST line | Logo and "CIST \| Computer Information Systems & Technology" readable | |
| L6 | Hydrostatic Test Bay scene: press P | Post-processing toggles off and on | |

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
