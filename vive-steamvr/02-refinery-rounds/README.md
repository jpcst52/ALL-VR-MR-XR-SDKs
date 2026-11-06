# Refinery Rounds (Unity, desktop and Vive Pro 2 / SteamVR)

Outdoor rounds at the battery limits of a refinery unit: check the wind, bump-test a gas monitor, enter the unit, then react to a gas alarm, a flange weep and a site emergency alarm. The scenario is inspired by the "Outdoor rounds at the battery limits" scenario in the web lab (`training-simulations/industrial-scenarios.js`, id `refinery-battery-area`) and uses the same four tasks, rebuilt as a walk-around 3D scene with real wind and drifting gas.

![Start view: pre-entry station, conditions board (partly out of frame at left), gate with the university plate, task board (desktop editor render, not a headset)](screenshots/07-start-view.png)

This is general safety-awareness practice. It is not a site procedure, a permit or a certification, and it does not replace training by your employer. Real sites have their own rules for gas monitors, radio calls, barricades and emergency response.

## What is verified and what is not

| Item | Status (as of November 3, 2026) |
|---|---|
| Compiles in Unity 6000.3.20f1 | Yes, in scripted editor runs (November 1 and 2). |
| Scripted capture run (twenty staged views, the "why this matters" panels, the reasoning question, scoring) | Yes, in the desktop editor (round 3: November 3, 2026, `-pbOfflineWeather`, 0 compile errors, scene re-baked and the baked scene played through the whole scripted run). This is emulated, not a headset. |
| Sound (round 3) | Wired to the kit's `Sfx` and compiled and run in the scripted capture (no errors logged). **Nobody has listened to it yet**: levels, balance and the siren are unheard. |
| Frame rate / draw calls | **Not measured.** Round 3 more than doubled the object count (see below); detail objects are marked static for batching but this has not been profiled. |
| Played through by a person | The owner has looked at it. It has not been played from the gate to the muster point by hand since the signage, "why this matters" and baking changes. |
| Headset (Vive Pro 2, Quest or any other) | **Not tested.** |
| Fresh clone opens and plays without running Set up project | Not tested. |
| Live weather | The request to Open-Meteo is coded; with no network the scene uses simulated wind and says so. Not checked on a conference network. |

The hub keeps its own copy of this script (`../hub/Assets/Scripts/Scenarios/`); it is refreshed separately. The hand-test table is [../../docs/TESTING.md](../../docs/TESTING.md) section 2 (checks D1 to D5, D7, D8, B1 to B4), with exact steps and the expected text.

## How to open and run

Requirements: Unity **6000.3.20f1** (built-in render pipeline), Windows, and a **full clone** of this repository (the project loads the shared kit from `../packages/com.pittbradford.xrtraining` by a relative `file:` path). OpenXR 1.19.0 and the Input System come from `Packages/manifest.json`.

**Desktop (no headset).**

1. Unity Hub > **Add > Add project from disk** > `vive-steamvr/02-refinery-rounds`. The first open imports and compiles (not timed; allow several minutes). A Safe Mode prompt means a script error: send the first red message.
2. Open `Assets/Scenes/RefineryRounds.unity`. The scene and the OpenXR settings are committed. If the scene is missing or only holds a "Scenario" object, run **Pitt-Bradford > Set up project** once (it keeps a baked scene; restart the editor if it asks about the input handling change).
3. Close SteamVR, press **Play**, then click once in the Game view.

**Headset (untested).** Set SteamVR as the active OpenXR runtime (SteamVR > Settings > Developer > Set SteamVR as OpenXR runtime), start SteamVR, then press Play.

**Optional player build.** **Pitt-Bradford > Build Windows player** writes `Builds/RefineryRounds/` (not committed; not tried by a person).

## Controls

| Action | Vive wands (untested) | Desktop |
|---|---|---|
| Move | Teleport: push either thumbstick (or the trackpad) forward to show an arc, let go to jump there (green = valid, red = refused) | Hold T, let go to teleport; W A S D also walks |
| Look / turn | Head; thumbstick or trackpad left or right turns 45 degrees (either hand) | Hold the right mouse button and move the mouse |
| Press (buttons, radio, flange, answers) | Point + trigger | Aim with the centre of the view, left click (range about 4 m) |
| Grab (monitor, cones) | Grip near the object | Aim at it and hold **E** (range about 3 m); release E to drop |
| Answer the reasoning question | Point + trigger | Left click, or press 1, 2 or 3 |

The chest radio (ENTERING, WARN, REPORT, PHONE OFF and the live monitor readout) follows you at chest height: on the desktop look down to see it. The task board follows your view and is replaced for each of the four tasks. No crosshair dot is drawn on the desktop: the pointer is the centre of the view.

## Task list and why

Four tasks, thirteen steps. When you complete a step, a panel headed **WHY THIS MATTERS** shows one plain-language sentence for about 7 seconds, a little below and left of your line of sight (the task board floats to the right), and follows your view gently.

| Task | Step (as on the board) | What completes it | Why this matters (shown in the scene) |
|---|---|---|---|
| 1 Prepare to enter | Look at the wind sock and note the wind direction | Look at the sock for about 1.2 seconds | Gas drifts with the wind. Knowing the wind tells you which way to move if there is a release. |
| | Look at the muster point flag: know your exit | Look at the flag for about 1.2 seconds | In an alarm there is no time to search. Knowing your muster point first saves seconds. |
| | Bump-test your gas monitor, then carry it | Hold the monitor at the station (within about 2.6 m on desktop) and press BUMP TEST (2.5 seconds); release it to clip it to your chest | A bump test shows the sensors and alarm respond to gas. A monitor can switch on fine and still miss gas. |
| | Radio the control room: ENTERING | Press ENTERING after the first three steps | The control room tracks who is inside, so everyone can be accounted for in an emergency. |
| | Enter the unit through the gate | Walk through the gate | The gate is the controlled way in, where entry is checked and others expect you to be. |
| 2 Gas alarm | Move upwind or crosswind, out of the gas | Walk to gauge G-1 (gold marker, stand within about 2 m for a second). A release starts 4 m upwind of you and drifts toward you. Get at least 10 m from it, in clean air | Moving upwind or crosswind gets you out of the cloud fastest. Downwind keeps you in it. |
| | Warn nearby workers on the radio (WARN) | Press WARN after reaching clean air | Nearby workers may not know about the gas yet. A warning keeps them from walking into it. |
| | Report to the control room (REPORT) | Press REPORT after WARN | The control room can isolate the release and start the response, but only once it knows. |
| 3 Flange weep | Stay 3.5 m clear and switch your phone off | Press PHONE OFF and stand 3.5 m or more from flange F-12 | Vapor near a leak can reach a flammable mix. Distance and no phone keep ignition sources away. |
| | Barricade: place 3 cones around the leak | Four cones stand at the barricade station: carry three and drop them 2 to 7 m from the flange, at least 1.5 m apart and spread around it (the tape strings itself between them) | Cones and tape stop others from walking into the hazard without knowing it is there. |
| | Report the leak on the radio (REPORT) | Press REPORT after the barricade | Leaks are repaired by authorized crews once the line is made safe. Your report starts that. |
| 4 Site alarm | Walk the safe route to the muster point | A siren sounds and a large release starts. Walk to within 3.5 m of the green muster flag, avoiding the gas | A route upwind of the release keeps you out of the gas on the way to safety. |
| | Check in at the muster point for headcount | Press CHECK IN on the muster sign | The headcount tells responders who is safe, so no one goes into the gas searching for you. |

**The reasoning question.** After the check-in one multiple-choice question appears (three options, one best answer): *Your gas monitor switched on and read 0. Why bump-test it before you enter the unit?* Every option then shows a line of feedback and the best answer (the third) turns green. The question is about the first task, so it checks whether the reason stuck, not whether you just read it. The result is kept apart from the procedure score: the final board shows the usual summary and, on its own line, "Reasoning: best answer ..." or "Reasoning: review ...". It is practice feedback, not pass or fail. A Penn State study (Wang & Messner, 2026; see [../../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md](../../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md)) found that VR beat video on procedure and spatial scores, but neither conveyed why the steps mattered; this is the reason for the panels. The reasons are written for this example, are not taken from a standard or a site procedure, and have not been reviewed by a partner.

**Mistakes counted** (shown as "Total mistakes ... Critical ..." in the board footer, across all four tasks):

- Critical: entering the unit before the checks are done; entering without the gas monitor; walking toward the release source (within 2.2 m, more than 1.5 seconds after the alarm); clicking the flange to tighten it ("Never tighten a leaking joint yourself."); a phone on while within 2.4 m of the leak; walking to the orange tool bag during the site alarm ("Never go back for belongings during an alarm.").
- Mistake: entering across the fence instead of the gate; taking more than 30 seconds to get clear of the gas; clicking the rag ("A rag spreads the problem. Report the leak."); standing within 2.4 m of the leak; walking through the gas cloud to the muster point (about two seconds of exposure); waiting more than 45 seconds before moving to the muster point.

## The scene

An outdoor process-unit edge built from primitives and the shared kit's procedural materials (no imported art): ground and pad, fenced area with a gate, pipe rack, two vessels (V-101 tower, V-102 drum), a flare stack with a flickering flame, a fixed gas detector post, a control-room cabin, a muster point (sign, check-in button, flag), a wind sock, and a pre-entry station with a bump-test unit and the handheld gas monitor.

**Live wind.** The wind sock and a conditions board use `Weather.Fetch` (Open-Meteo, Bradford PA). If there is no network the kit's `Weather.Fallback` is used and the board says "simulated (no network)" (and "(live fetch pending)" for the first seconds). The sock points downwind and droops in light wind. The flare flame bends downwind.

**Gas.** Each release is drawn as a stream of translucent puffs that drift with the wind, grow, and fade. The monitor, the fixed detector and the rules all read the same puff field, so staying upwind or crosswind really keeps you out of the gas. The reading is an illustrative % LEL (alarm at 10, high alarm at 20), not a calibrated value. Real hydrocarbon vapor is usually invisible; the cloud is drawn only for training.

**Muster point.** It is placed on the upwind side of the unit, so its position depends on the wind (it moves when the live reading arrives). Real sites have designated muster points and alternates.

**Signage.** Nothing is plain text floating in the air. Signs are rounded, shadowed panels with a metal back (the kit's `Ui.Panel`): the gate sign, the conditions board, the pre-entry instruction board, the control-room sign, the muster sign and the barricade-station sign. Equipment carries ID plates fixed to it (V-101, V-102, gauge G-1, the gas detector, the wind sock and the flare base). Things that need a label but have nowhere to mount one (flange F-12, the rag, the tool bag, the sample point, the gold objective marker) use name tags with a leader line that turn to face you (`Ui.Tag`). The University of Pittsburgh at Bradford and CIST plate (`Ui.BrandPlate`) stands on the gate lintel, in view from the start. It shows the approved shield logo, which the kit ships in `Runtime/Resources/Branding/pitt-bradford.png` (without that file it shows a text wordmark). Text on the face of a device (monitor screen, detector display, chest radio buttons, CHECK IN button) is unchanged.

**Look.** Daylight ambient comes from sky, horizon and ground colours, and the sky is Unity's built-in procedural skybox when that shader is available (otherwise a flat sky colour). The task board floats a little to the right so it does not cover the gate and its signs.

## Detail, lighting and sound (round 3)

Round 3 (November 3, 2026) added detail, realism, lighting, effects and sound. Everything is built in code (`Assets/Scripts/RefineryDetail.cs`, a `partial` part of `RefineryRounds`), stays pack-free (no Asset Store or private assets) and has no colliders, so it never blocks the player. The muster point is still **upwind** of the unit by design; that logic was not touched.

**Detail (about 540 new objects).** Pipe rack: cross-bracing, pipe shoes, a cable tray with hangers and cable bundles, a clad (insulated) line with bands, two gate valves with red handwheels, a pressure gauge, a drain and elbows on the branch line. Vessels: banding, a bolted manway, a grated platform with a yellow handrail and a gap at the ladder, ladder cage hoops, bollards, a relief valve with a vent line and a rack vent. Ground: yellow pad edge lines, white safe-route and walkway lines, scattered stones, puddles, oil stains, drain grates, a manhole cover and weeds along the fence. Safety: a safety shower and eyewash station, two fire extinguisher stands, four fence signs, drums on a pallet and a spill kit (all signs use the kit's `Ui.Panel`). Cabin: window frames with lit panes and blinds, door handle, kick plate and sign, wall lamp, rooftop AC unit and vent, conduit and a junction box. Flare: guy wires, red and white stack bands, a platform, ladder rails and a blinking aviation light. Muster point: a painted ring, two benches and a first aid box.

**Realism.** New procedural textures made in code: corrugated rust-streaked cladding, open-mesh grating, an irregular oil stain with a soft edge and a rust patch (plus the kit's concrete, steel and gravel textures that were already there). Puddles are opaque and very glossy so they mirror the sky. No normal maps (the baker would lose their import type), so relief comes from geometry and colour only.

**Lighting and effects.** A cooler, more overcast sky tint, a sun with softer shadow strength and a shorter shadow distance, trilight ambient, nearer fog, a pixel-light cap of 4, and a realtime reflection probe captured once at load. Four lighting poles with lit fixtures (two also carry a vertex-mode point light) and a wall lamp at the cabin. The flare flame flickers in colour, size and light; flare smoke and two steam plumes are particle systems pushed by the live wind; a drip falls at the flange during the weep. Heat shimmer and light shafts were not added (outdoors in daylight; shimmer needs a grab pass).

**Sound.** All sounds come from the kit's `Sfx` (see [../../docs/agents/KIT-AUDIO-FX.md](../../docs/agents/KIT-AUDIO-FX.md); synthesized unless the kit lists a CC0 sample in its own credits file). This project adds no audio files. Loops: wind (volume follows the live wind speed), plant drone, flare roar, control-room HVAC. Events: gas monitor beeps (faster at high alarm), rising then wailing site siren and a falling sweep when the scenario ends, gas hiss at each release, button presses, radio clicks, confirm and error sounds on task steps and mistakes, a cone drop sound, a water drip at the weep, and footsteps (concrete on the pad, gravel outside) through the kit rig. The old sine siren and beep were removed.

**Before and after.** Compare `screenshots/01-overview.png` and `07-start-view.png` with the November 1 versions in git history; new views are 14 to 20. All are desktop editor renders, not a headset.

**Still plain.** The ground is a single tiled noise texture; the vessels and rack posts are still smooth primitives with little bevel; the sky has no clouds; there are no stairs; people and vehicles are absent.

## What is simulated and what is not

Simulated: wind from live weather, a simple gas drift and concentration model, a gas monitor with bump test and alarms, a fixed detector, a barricade check (three cones, spread out, around the leak) and the rules above.

Not simulated: real gas physics (density, dilution, terrain, buildings), real instrument behavior and calibration, permits and lockout, PPE, radio protocol, site-specific emergency plans, or anything about actual refinery equipment. No part of this has been checked against a real procedure.

## Baked scene

**What "baked" means.** The world (ground, fence, rack, vessels, signs, lights, cones, the monitor and the round 3 detail; 933 baked objects, 934 GameObjects in the scene file including the "Scenario" object) is still described in `RefineryRounds.cs`, but it has been *baked* into `Assets/Scenes/RefineryRounds.unity`: every object is a normal GameObject in the saved scene, so you can see it in the Hierarchy and Scene view, select it, move it, rescale it or swap its material. The generated materials and textures are saved in `Assets/Baked/RefineryRounds/` (commit this folder together with the scene). The scenario object carries a small **Baked World** component that lists what was baked and when.

**Pressing Play.** `Start()` runs exactly as before. Each kit helper call (`P.Node`, `P.Box`, `P.Cyl`, `P.Ball`, `P.Prim`, `P.Text`, `Ui.Panel`, `Ui.Tag`, `Ui.BrandPlate`) first looks for the saved object and *adopts* it, unchanged, instead of making a new one. The code then re-attaches what a scene file cannot hold: button and grab handlers, the script's fields (sock joints, lamps, cones ...), the generated sounds and the runtime font of the text. Objects are matched by **parent, name and call order**: the third `P.Box(root, "Table leg", ...)` call adopts the third child called "Table leg" under "Refinery unit".

**Edits you make in the scene are kept** (position, rotation, scale, material, light settings, collider size) for objects the script only builds. Some objects are moved by the script while it runs, so editing their position has no lasting effect: the muster point (placed upwind from the live wind), the sock joints and flare flame (animated), the chest radio and the gas monitor once clipped, the release point and the gold marker (moved per task). Scene lighting (ambient, fog, sky) is copied into the scene so the editor view looks right, but `Start()` still sets it, so change lighting in the script.

**Rules while editing.** Do not rename, re-parent or reorder (among same-named siblings) baked objects: the script finds them by name and order, and would build a new copy next to a renamed one. Deleting a baked object brings it back at the next Play (the script recreates what it cannot find); to hide one, untick it (inactive) instead. Your own extra objects are fine anywhere in the scene and survive a re-bake.

**Still created at runtime (on purpose):** the XR rig, hands and cameras; the task board and its anchor; the "why this matters" panels and the reasoning question; the gas puffs; the gold marker's name tag; the capture camera. They are rebuilt each play or change during play, so they are not in the scene.

**Re-bake after changing the script** (for example a new sign in `BuildFence`): open the project, then **Pitt-Bradford > Bake scene (play mode)**. It enters play mode with time frozen, waits until `Start()` has built everything, saves the result into the scene and returns to edit mode (about 15 seconds). Re-baking keeps the edits you made to existing objects and adds the new ones. Headless: `Unity.exe -projectPath <this folder> -executeMethod PittBradford.XR.EditorTools.SceneBaker.BakeAndExit -logFile <log>` (no `-batchmode`).

**Unbake** (go back to building everything at Start): **Pitt-Bradford > Unbake scene** (or `-executeMethod PittBradford.XR.EditorTools.SceneBaker.UnbakeAndExit`). It deletes the baked objects and `Assets/Baked/RefineryRounds/` and restores the scene lighting; the scenario then runs exactly as it did before baking. For a clean bake that drops all scene edits, Unbake and then Bake. **Set up project** no longer replaces a baked scene with an empty one.

**Verified (November 2, 2026, desktop editor only, emulated, not a headset):** the 13 capture shots were rendered with the scene unbaked and baked and compared pixel by pixel. Outside the animated wind sock, flame and flag, which differ between any two runs, the only differences are sub-pixel edges of the sign panels (their textures are now imported PNGs) and text glyph edges (the runtime font atlas is filled in a different order); under 0.3% of pixels differ by more than 8 of 255 levels in every shot. The scripted run completed in both cases (all tasks, scoring and the reasoning question, no script errors). A table moved in the saved scene appeared at its new position at Play. The baked scene file was about 970 KB then; after round 3 it is about 2.8 MB (not timed).

## Files

- `Assets/Scripts/RefineryRounds.cs`: the scenario (`partial class RefineryRounds : MonoBehaviour, IShotScript`), including the step reasons (`StepWhy`) and the rationale question.
- `Assets/Scripts/RefineryDetail.cs`: round 3 detail, textures, effects, wind-driven particles and the `Sfx` sound wiring (other half of the partial class).
- `../packages/com.pittbradford.xrtraining/Runtime/WhyCue.cs`: the shared "why this matters" panel and question used here and in the Hydrostatic Test Bay.
- `Assets/Editor/ProjectSetup.cs`: the setup and build menu items.
- `Assets/Scenes`, `Assets/XR`, `Assets/Resources`, `ProjectSettings`: the baked scene, OpenXR settings and shader keepers (written by **Set up project**, committed).
- `Assets/Baked/RefineryRounds/`: materials and textures written by **Bake scene**; the baked scene refers to them (see "Baked scene").
- `screenshots/`: the twenty desktop editor renders below.
- `Packages/manifest.json`: same dependencies as project 01 (the kit by `file:` path).
- Screenshots are made with `Unity.exe -projectPath <this folder> -executeMethod PittBradford.XR.EditorTools.CaptureRunner.Run -pbCapture <folder> -logFile <log>` (no `-batchmode`; it needs graphics). The editor enters play mode, the scenario's `Shots` coroutine stages and photographs twenty views, and the editor exits. A built player also accepts `-pbCapture <folder>`.

## Screenshots

Desktop editor renders made by the capture run above (November 1, 2026). They are not headset captures. Each view is staged by the script (for example the gas cloud and the barricade are placed, not played, and in 11 and 12 the script finishes the scenario and picks the answer), so they show what the scene looks like, not a recorded play-through. Some views still show cones and tape left over from an earlier staged view (06, 08).

| | |
|---|---|
| ![Overview of the unit from above](screenshots/01-overview.png) | ![Wind sock, conditions board and gate from the walkway](screenshots/02-wind-sock-and-board.png) |
| 01 Overview | 02 Wind sock, conditions board, gate |
| ![Gas alarm: release, drifting cloud, sample-point tag](screenshots/03-gas-alarm-cloud.png) | ![Flange weep with cones and tape](screenshots/04-weep-and-barricade.png) |
| 03 Gas alarm cloud (task 2 board) | 04 Weep and barricade (four cones placed by the script, tape between them; task 3 board) |
| ![Muster point sign with check-in button](screenshots/05-muster-point.png) | ![Critical miss: going back for the tool bag](screenshots/06-critical-miss.png) |
| 05 Muster point | 06 Critical miss (tool bag; tape and a cone from view 04 are at lower right) |
| ![Start view](screenshots/07-start-view.png) | ![Gate sign and university plate close up](screenshots/08-gate-signage.png) |
| 07 Start view | 08 Gate signage (tape from view 04 is visible beyond the gate) |
| ![Equipment ID plates and the flange name tag](screenshots/09-equipment-labels.png) | ![Why this matters panel after the bump test](screenshots/10-why-this-matters.png) |
| 09 Equipment labels | 10 Why this matters (after the bump test) |
| ![The rationale question at the muster point](screenshots/11-rationale-question.png) | ![Feedback on every option, best answer in green](screenshots/12-rationale-feedback.png) |
| 11 Rationale question | 12 Feedback on every option |
| ![Final task board with the separate Reasoning line](screenshots/13-summary-with-reasoning.png) | ![Pipe rack detail](screenshots/14-pipe-rack-detail.png) |
| 13 Summary with the separate Reasoning line (best answer, no mistakes) | 14 Pipe rack: bracing, cable tray, clad line, valves, wet ground (round 3) |
| ![Vessel platform](screenshots/15-vessel-platform-detail.png) | ![Cabin and lighting](screenshots/16-cabin-and-lighting.png) |
| 15 V-101 platform handrail, bands, ladder and a lighting pole | 16 Cabin: lit windows, door, wall lamp, AC unit; lighting pole |
| ![Safety shower](screenshots/17-safety-shower-and-extinguisher.png) | ![Flare smoke and sky](screenshots/18-flare-smoke-and-sky.png) |
| 17 Safety shower and eyewash, extinguisher, guy wire | 18 Flare stack, guy wires, smoke and steam |
| ![Ground detail](screenshots/19-ground-detail.png) | ![Steam and relief valve](screenshots/20-steam-and-relief-valve.png) |
| 19 Ground: walkway lines, puddle, signs, weeds, drums | 20 Steam plume, relief valve and vent line |

All images: desktop editor render, not a headset (round 3 set made November 3, 2026, on the baked scene).

## Render cost

Round 3 detail roughly tripled the render cost; round 4 cut it again with no object removed. Editor CPU counters in Play mode (median, mono editor Game view, measured on the hub copy of this scene because the hub runs the same scripts; **not** a Vive frame rate): set-pass calls 1,290 to **544**, draw calls 1,807 to **1,082**, triangles 448 k to **287 k**.

How: the bake now runs the kit's `RenderBudget` pass (flags and settings only, nothing is merged on disk). Groups that never move are declared with `P.Static(...)` and become batching-static; small, thin, emissive and transparent parts stop casting shadows; at most two non-directional lights stay on with a shorter range (the rest are switched off but stay in the scene), with a little more ambient light to make up; the soft indoor key light no longer casts realtime shadows. Baked materials are colour-quantised and GPU-instanced. The look is close to before but slightly flatter, with less pooled light; re-shoot screenshots were compared by eye and not replaced. GPU time and the frame rate on a headset are **not measured**. Details and the full table: `../hub/README.md`.

## Troubleshooting

- **Safe Mode prompt on open:** a script failed to compile. Send the first red Console message.
- **Keys or mouse do nothing in Play:** click inside the Game view first. If the Console shows OpenXR messages, close SteamVR and press Play again. With no headset the project is expected to fall back to desktop controls; this was not checked with SteamVR running.
- **The view snaps back while you mouse-look:** this is check D2. A scripted capture run once showed the camera being reset by the head pose driver; report it if you see it in normal play.
- **Cyan or pink materials in the first seconds:** the editor is still compiling shader variants. Wait a moment. Report it if it stays.
- **"Reading the wind... one moment." stays on the board:** the live weather request is still pending; the scene falls back to simulated wind when the request fails.
- **The monitor falls instead of clipping to your chest:** it clips only after the bump test has passed. Before that it drops where you release it.
- **Red Console lines or repeated errors during play:** copy the first one.

## Visual pass 2 (2026-11-05)

Normal maps are now generated for every textured material (block joints, concrete grain, planks, cardboard flutes), noise textures are 512 px, windows and the dock door show a daylight picture, a merged contact-shadow mesh (one draw call) grounds standing objects, and the kit asks for 4x MSAA and forced anisotropic filtering (ground is 300 m wide so its edge sits deep in the fog). Editor render counters moved by a few set-pass or draw calls only (see [../hub/README.md](../hub/README.md) and `docs/PROJECT-STATUS.md`); they are CPU counters, not a Vive frame rate. Screenshots in `screenshots/` were re-shot as desktop editor renders (not a headset). **Not re-baked yet** (its Unity process hung and was left running), so the baked scene and screenshots here are still from round 4; the script and kit changes are in place and apply on the next Unbake and Bake.

## Research basis

See [../../docs/research/README.md](../../docs/research/README.md).
