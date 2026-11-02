# Refinery Rounds (Vive Pro 2 / SteamVR, Unity)

Outdoor rounds at the battery limits of a refinery unit. The scenario is inspired by the "Outdoor rounds at the battery limits" scenario in the web lab (`training-simulations/industrial-scenarios.js`, id `refinery-battery-area`) and uses the same four tasks, rebuilt as a walk-around 3D scene with real wind and drifting gas.

**Status (November 1, 2026): compiles in Unity 6000.3.20f1 in this project. The scripted screenshot run (thirteen staged views, below) completes with no errors from the scenario or kit scripts, including the new "why this matters" panels and the reasoning question. After the signage and "why" updates the scenario has not been played through by hand, and it has not been tested on a Vive Pro 2.** (The hub keeps its own copy of this script; it is refreshed separately.)

![Start view: pre-entry station, conditions board, gate with the university plate, task board (desktop editor render, not a headset)](screenshots/07-start-view.png)

This is general safety-awareness practice. It is not a site procedure, a permit, or a certification, and it does not replace training by your employer. Real sites have their own rules for gas monitors, radio calls, barricades, and emergency response.

## Learning goals

- Prepare before entering a unit: know the wind, know your exit (muster point), bump-test and carry a personal gas monitor, tell the control room you are entering.
- React to a gas alarm: move out of the gas, upwind or crosswind, never toward the source; warn others; report.
- Handle a small hydrocarbon weep: stay clear, keep ignition sources (phone) away, barricade the area, report it, never tighten or wrap the leak yourself.
- Respond to a site emergency alarm: walk the safe route to the muster point, do not return for belongings, be counted.

## The scene

An outdoor process-unit edge built from primitives and the shared kit's procedural materials (no imported art): ground and pad, fenced area with a gate, pipe rack, two vessels (V-101 tower, V-102 drum), a flare stack with a flickering flame, a fixed gas detector post, a control-room cabin, a muster point (sign, check-in button, flag), a wind sock, and a pre-entry station with a bump-test unit and the handheld gas monitor.

**Live wind.** The wind sock and a conditions board use `Weather.Fetch` (Open-Meteo, Bradford PA). If there is no network the kit's `Weather.Fallback` is used and the board says "simulated (no network)". The sock points downwind and droops in light wind. The flare flame bends downwind.

**Gas.** Each release is drawn as a stream of translucent puffs that drift with the wind, grow, and fade. The monitor, the fixed detector, and the rules all read the same puff field, so staying "upwind" or "crosswind" really keeps you out of the gas. The reading is an illustrative % LEL (alarm at 10, high alarm at 20), not a calibrated value. Real hydrocarbon vapor is usually invisible; the cloud is drawn only for training.

**Muster point.** It is placed on the upwind side of the unit, so its position depends on the live wind (it moves when the live reading arrives). Real sites have designated muster points and alternates.

**Signage.** Nothing is plain text floating in the air. Signs are rounded, shadowed panels with a metal back (the shared kit's `Ui.Panel`): the gate sign, the conditions board, the pre-entry instruction board, the control-room sign, the muster sign and the barricade-station sign. Equipment carries ID plates fixed to it (V-101, V-102, gauge G-1, the gas detector, the wind sock and the flare base). Things that need a label but have nowhere to mount one (flange F-12, the rag, the tool bag, the sample point, the gold objective marker) use name tags with a leader line that turn to face you (`Ui.Tag`). The University of Pittsburgh at Bradford / CIST plate (`Ui.BrandPlate`) stands on the gate lintel, in view from the start. It shows a text wordmark; the logo appears only in local builds that have the git-ignored logo file. Text on the face of a device (monitor screen, detector display, chest radio buttons, CHECK IN button) is unchanged.

**Look.** Daylight ambient comes from sky, horizon and ground colours, and the sky is Unity's built-in procedural skybox when that shader is available (otherwise a flat sky colour). The task board floats a little further to the right than before so it does not cover the gate and its signs.

## Steps

1. **Prepare to enter.** Look at the wind sock and at the muster flag (hold your gaze about a second). Grab the gas monitor, hold it at the station and press BUMP TEST. Release it to clip it to your chest. Press ENTERING on the chest radio, then walk through the gate.
2. **Gas alarm.** Walk the round to gauge G-1 (gold marker). A release starts upwind of you and the monitor alarms. Move out of the gas (upwind or crosswind, at least 10 m from the release), press WARN, then REPORT. Walking toward the source is a critical miss.
3. **Flange weep.** Walk toward flange F-12. Stay at least 3.5 m clear and press PHONE OFF. Grab the cones from the barricade station and place three around the leak (tape strings itself between them). Press REPORT. Pressing the flange bolts is a critical miss; the rag is a mistake.
4. **Site emergency alarm.** A siren sounds and a large release starts. Walk to the green muster flag by a route that avoids the gas, then press CHECK IN on the muster sign. Going back for the tool bag is a critical miss.

Mistakes and critical misses are counted across all four tasks and shown on the task board footer.

## Why this matters, and one reasoning question

A Penn State study (Wang & Messner, 2026; see [../../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md](../../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md)) found that VR beat video on procedure and spatial scores, but neither conveyed *why* the steps mattered. This scenario uses the same design as the Hydrostatic Test Bay (the shared kit's `WhyCue`):

- **After each of the 13 steps** a panel headed **WHY THIS MATTERS** shows one plain-language sentence for about seven seconds, a little below and left of your line of sight (the task board floats to the right), and follows your view gently. For example, after the bump test: *"A bump test shows the sensors and alarm respond to gas. A monitor can switch on fine and still miss gas."*
- **After the check-in at the muster point** one multiple-choice question appears (three options, one best answer): *Your gas monitor switched on and read 0. Why bump-test it before you enter the unit?* Point at an option and pull the trigger (VR), or click it or press 1, 2 or 3 (desktop). Every option then shows a line of feedback, and the best answer is marked in green. The question is about the first task, so it checks whether the reason stuck, not whether you just read it.
- **The result is kept apart from the procedure score.** The final task board shows the usual summary and, on its own line, "Reasoning: best answer ..." or "Reasoning: review ...". It is practice feedback, not pass/fail and not a certification.

The reasons are general safety awareness written for this example. They are not taken from a standard or a site procedure and have not been reviewed by a partner yet.

## Controls

| Action | Vive wands | Desktop |
|---|---|---|
| Move | Left trackpad/stick | W A S D |
| Look / turn | Head; right stick snap-turn | Hold right mouse and move |
| Press (buttons, radio, bolts) | Point + trigger | Crosshair + left click |
| Grab (monitor, cones) | Grip near the object | Look at the object and press E (hold to carry, release to drop) |
| Answer the reasoning question | Point + trigger | Crosshair + left click, or keys 1, 2, 3 |

The chest radio (ENTERING, WARN, REPORT, PHONE OFF, and the live monitor readout) follows you at chest height. On desktop, look down to see it. The task board follows your view.

## What is simulated and what is not

Simulated: wind from live weather, a simple gas drift and concentration model, a gas monitor with bump test and alarms, a fixed detector, a barricade check (three cones, spread out, around the leak), and the rules above.

Not simulated: real gas physics (density, dilution, terrain, buildings), real instrument behavior and calibration, permits and lockout, PPE, radio protocol, site-specific emergency plans, or anything about actual refinery equipment. No part of this has been checked against a real procedure.

## Research basis

See [../../docs/research/README.md](../../docs/research/README.md).

## Setup

1. Open `vive-steamvr/02-refinery-rounds` in Unity Hub with Unity **6000.3.20f1** (built-in render pipeline). The project uses the shared kit in `../packages/com.pittbradford.xrtraining`.
2. Run the menu **Pitt-Bradford > Set up project**. It configures OpenXR (Vive, Index, Touch profiles), the Input System, and creates `Assets/Scenes/RefineryRounds.unity`. Restart the editor if it asks for the input handling change.
3. Set **SteamVR as the active OpenXR runtime** (SteamVR > Settings > Developer > Set SteamVR as OpenXR runtime), start SteamVR, then press Play. With no headset, it runs in desktop mode.
4. Optional: **Pitt-Bradford > Build Windows player** builds to `Builds/RefineryRounds/`.
5. Screenshots: `Unity.exe -projectPath <this folder> -executeMethod PittBradford.XR.EditorTools.CaptureRunner.Run -pbCapture <folder> -logFile <log>` (no `-batchmode`; it needs graphics). The editor enters play mode, the scenario's `Shots` coroutine stages and photographs thirteen views, and the editor exits. A built player also accepts `-pbCapture <folder>`.

## Screenshots

Desktop editor renders made by the capture run above (November 1, 2026). They are not headset captures. Each view is staged by the script (for example the gas cloud and the barricade are placed, not played, and in 11 and 12 the script finishes the scenario and picks the answer), so they show what the scene looks like, not a recorded play-through.

| | |
|---|---|
| ![Overview of the unit from above](screenshots/01-overview.png) | ![Wind sock, conditions board and gate from the walkway](screenshots/02-wind-sock-and-board.png) |
| 01 Overview | 02 Wind sock, conditions board, gate |
| ![Gas alarm: release, drifting cloud, sample-point tag](screenshots/03-gas-alarm-cloud.png) | ![Flange weep with three cones and tape](screenshots/04-weep-and-barricade.png) |
| 03 Gas alarm cloud | 04 Weep and barricade |
| ![Muster point sign with check-in button](screenshots/05-muster-point.png) | ![Critical miss: going back for the tool bag](screenshots/06-critical-miss.png) |
| 05 Muster point | 06 Critical miss (tool bag) |
| ![Start view](screenshots/07-start-view.png) | ![Gate sign and university plate close up](screenshots/08-gate-signage.png) |
| 07 Start view | 08 Gate signage |
| ![Equipment ID plates and the flange name tag](screenshots/09-equipment-labels.png) | ![Why this matters panel after the bump test](screenshots/10-why-this-matters.png) |
| 09 Equipment labels | 10 Why this matters (after the bump test) |
| ![The rationale question at the muster point](screenshots/11-rationale-question.png) | ![Feedback on every option, best answer in green](screenshots/12-rationale-feedback.png) |
| 11 Rationale question | 12 Feedback on every option |
| ![Final task board with the separate Reasoning line](screenshots/13-summary-with-reasoning.png) | |
| 13 Summary with the separate Reasoning line | |

## Files

- `Assets/Scripts/RefineryRounds.cs`: the whole scenario (`RefineryRounds : MonoBehaviour, IShotScript`), including the step reasons (`StepWhy`) and the rationale question.
- `../packages/com.pittbradford.xrtraining/Runtime/WhyCue.cs`: the shared "why this matters" panel and question used here and in the Hydrostatic Test Bay.
- `Assets/Editor/ProjectSetup.cs`: the setup and build menu items.
- `Assets/Scenes`, `Assets/XR`, `Assets/Resources`, `ProjectSettings`: created by **Set up project** (OpenXR settings, scene, shader keepers).
- `screenshots/`: the thirteen desktop editor renders above.
- `Packages/manifest.json`: same dependencies as project 01 (the kit by `file:` path).
