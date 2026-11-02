# Warehouse and forklift pedestrian safety (Vive Pro 2, SteamVR)

A Unity project for the HTC Vive Pro 2 on SteamVR (OpenXR), with a keyboard-and-mouse desktop fallback. It uses the shared kit in [`../packages/com.pittbradford.xrtraining`](../packages/com.pittbradford.xrtraining) and builds everything from primitives, so it needs no imported art.

**Status (November 1, 2026): compiles in Unity 6000.3.20f1 in this project. The scripted screenshot run (13 views, below) completes with no errors from the scenario scripts, and its scripted self-test of the co-worker event and awareness check passes. The scenario has not been played through by hand since these changes, and it has not been tested on a Vive Pro 2. The hub has its own copy of `WarehouseForklift.cs`, which does not have the co-worker event yet.**

![Start view: PPE table and sign, task board, university plate, PPE header sign (desktop editor render, not a headset)](screenshots/07-start-view.png)

## Scenario and learning goals

You arrive at the entry of a warehouse where a forklift drives a loop around the racking. You practice general pedestrian awareness:

- put on PPE before you enter the floor,
- use a convex mirror at a blind corner before you step out,
- make eye contact with the driver, wait for the forklift to stop, and cross only at the striped crossing,
- stay in the green pedestrian walkways,
- find and mark hazards (a spill and a damaged rack upright) with cones and report them by radio.

This is general safety-awareness practice. It is not a certification, a training record, or a site procedure. Real sites have their own rules, signage, and supervisors.

## Steps on the task board

1. **PPE.** Put on the hard hat, hi-vis vest, and safety glasses from the PPE station before you cross the yellow entry line.
2. **Mirror.** From the entry walkway, look at the convex mirror on the cargo stack for 1.5 s. It shows the forklift lane that the stack hides.
3. **Cross.** Look at the driver, press SIGNAL on the post (the driver waves and the forklift stops), wait until it has stopped, then cross at the striped crossing.
4. **Report.** In the interior, find the spill and the bent rack upright. Place a cone near each, then press RADIO at the supervisor station.

Mistakes and critical misses are counted on the board like the Hydrostatic Test Bay. A **critical miss** (near miss) happens when you step into a forklift lane while the forklift is within about 6 m and not stopped for you. It triggers a red screen flash with text, a horn, haptics, and the forklift brakes hard. Other mistakes include entering the floor without full PPE, walking outside the green walkways, entering a lane away from the crossing, crossing without checking the mirror or without eye contact and a full stop, and radioing before both hazards are coned.

The steps may be done in any order, but the order above is the safe one.

## Co-worker hazard and awareness check (optional, on by default)

In this scenario you are a pedestrian, not the forklift driver, so the event is built around what a pedestrian should notice and do.

1. **The event.** About 8 s after you have crossed into the racking area, the forklift is sent to put a pallet away on the west-wall racking. It stops in the marked **LOADING ZONE** on the west lane, turns to the rack, raises the forks, drives in, sets the pallet on the shelf (in code; the screenshots do not show this step clearly), backs out, lowers, and turns back into the lane. While it works, a co-worker (hi-vis vest, white hard hat, clipboard) comes out of the **STAFF ENTRANCE** door, crosses the north lane, and walks close past the working forklift to the cone station. The driver sounds the horn when the co-worker comes within 4.5 m and holds ("Holding: person behind me") until they are more than 3.2 m away. Nothing warns you before the co-worker appears.
2. **The awareness check.** 4.5 s after the co-worker appears, the scene freezes: physics, the forklift and the co-worker stop (`Time.timeScale = 0`), audio pauses, you cannot walk (you can still look around and point), and a dark shell around your head blanks the view behind a panel. Three questions are asked one at a time, answered by pointing and pulling the trigger (desktop: aim and click):
   - *Where was the co-worker in the hi-vis vest?* In front of you / to your left / to your right / behind you, relative to the way your head was facing at the freeze.
   - *Which way were they walking, as seen from you?* Toward you / away from you / across from your left to your right / across from your right to your left.
   - *How far away was the forklift?* Less than 6 m / 6 to 12 m / more than 12 m.

   Answers are scored automatically against the true positions at the moment of the freeze. Near a boundary both neighbouring answers are accepted (15 degrees for direction; any walking-direction component of 0.4 or more; 1 m for distance), and the summary says when an answer was accepted as "close to the line". No feedback is given during the check. The scene then resumes.
3. **Stop, watch, wait.** After the check a panel in front of you says: *STOP where you are. WATCH them. WAIT until they are clear.* The same three steps are on the "FORKLIFT LOADING?" stand beside the walkway. The scenario records whether you held your position (moved less than 0.8 m and did not enter a lane) and looked at the co-worker (head direction within 15 degrees, line of sight, at least 0.5 s) until the co-worker and forklift were clear. This does not add mistakes to the task board, so the original scoring is unchanged.
4. **Summary.** When all four board steps are done, a **Scenario summary** panel appears in front of you: the board result and counts, then a separate **Awareness check** section (score out of 3, each question with your answer and the simulation's answer) and the stop-watch-wait result. It is labelled general safety-awareness practice, not a test, a certification, or a training record.

Inspector settings on the `Scenario` object (`WarehouseForklift` component): **Coworker Hazard** (off = the original scenario, with no door, loading zone, stand or co-worker), **Awareness Check** (off = the co-worker event without the freeze), **Event Delay** (8 s) and **Check Delay** (4.5 s).

Why: Choi, Ahn and Seo (2020) used SAGAT queries (the simulation pauses and the person answers questions about the surroundings) in a VR forklift study and found that operators were likely to have reduced awareness of nearby workers while loading or unloading. Our learner is a pedestrian rather than the operator, so this adapts the idea rather than reproducing it. The co-worker appears without warning; Kim et al. (2021) reported that a virtual accident triggered by inattention reduced risk habituation for about a week, but this scenario has no accident and has not been studied. See [../../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md](../../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md).

Limits: one scripted co-worker on a fixed route, once per run; directions are judged from head direction, not eye tracking; a real SAGAT study would pause at random times and ask about more elements.

## Signage and look

Nothing is plain text floating in the air. Signs are rounded, shadowed panels with a metal back (the shared kit's `Ui.Panel`): the yellow PPE sign on the entry gantry, RECEIVING AND STORAGE on the north wall, WATCH FOR FORKLIFTS on the west wall (above the racking, where the old text was hidden behind it), the dock sign, the BLIND CORNER sign on the cargo stack, the SAFETY CONES plate on its post, and the PPE, signal and radio stands. The task board hangs in a fixed place, so it also has a solid back. The forklift driver's message is a white speech-bubble panel above the cab that turns to face you. The University of Pittsburgh at Bradford / CIST plate (`Ui.BrandPlate`) hangs from the gantry beside the task board, in view from the start; it shows a text wordmark, and the logo appears only in local builds that have the git-ignored logo file. Painted floor words and the text on buttons are unchanged.

The BLIND CORNER text used to face into the cargo stack, so from the walkway it read mirrored; it now reads correctly from the walkway (see screenshot 08). The SAFETY CONES text was already the right way round and now sits on a plate (screenshot 09). Ambient light now comes from separate ceiling, wall and floor colours instead of one flat colour.

## Controls

| Action | Vive wands (SteamVR) | Desktop |
|---|---|---|
| Move | Left trackpad or stick | W A S D |
| Turn | Right stick snap turn, or turn your body | Hold right mouse and move |
| Press a button | Point with the controller, trigger | Aim the crosshair, left click |
| Grab | Grip, near the object | E (aim at the object, within about 3 m) |
| Wear PPE | Bring the item to head or chest and release | Hold the item, then press WEAR at the PPE sign |

Cones are grabbed from the tray and dropped near each hazard (within about 2 m, resting on the floor).

## What is simulated and what is not

Simulated: a forklift on a fixed loop (about 2.2 m/s) that slows and stops when you are in its path, honks near the crossing, and holds still after you signal it; a driver who waves; a live convex-mirror view of the blocked lane; PPE snap zones on your head and chest; cone placement; a supervisor radio reply.

Not simulated, or simplified:

- no real forklift physics, load handling, or reversing; only one forklift and one fixed route
- eye contact is approximated by a head-direction check with a short gaze time and a line-of-sight test; it does not track eyes
- you cannot be struck or hurt; the near miss is a scored event, and you can walk through racking and walls (the play area is only clamped to the building)
- worn PPE cannot be taken off again; PPE fit, inspection, and site-specific requirements are not modeled
- no audio from a real horn recording (it is a generated tone), no site procedures, signage standards, or regulations
- not tested on a Vive Pro 2 or any other hardware

## Research

Background references and study ideas for XR safety training: [../../docs/research/README.md](../../docs/research/README.md).

## Setup

1. Install Unity Hub and add **Unity 6000.3.20f1** (the built-in render pipeline is used; no extra assets are needed).
2. In Unity Hub choose **Add project from disk** and select this folder (`vive-steamvr/03-warehouse-forklift`). Packages are resolved from `Packages/manifest.json`, including the shared kit at `../packages/com.pittbradford.xrtraining`.
3. When the editor opens, run the menu **Pitt-Bradford > Set up project**. It configures OpenXR (Vive, Index and Touch profiles), the Input System, and shader variants, then creates `Assets/Scenes/WarehouseForklift.unity`. If it asks for an editor restart (input handling), restart and run it once more.
4. Make **SteamVR** the active OpenXR runtime (SteamVR > Settings > Developer > Set SteamVR as OpenXR runtime), start SteamVR, and put on the headset.
5. Open `Assets/Scenes/WarehouseForklift.unity` and press **Play**. With no headset connected the project runs in desktop mode.
6. To make a Windows build use **Pitt-Bradford > Build Windows player**. The output goes to `Builds/WarehouseForklift/` (not committed).

Screenshots: `Unity.exe -projectPath <this folder> -executeMethod PittBradford.XR.EditorTools.CaptureRunner.Run -pbCapture <folder> -logFile <log>` (no `-batchmode`; it needs graphics). The editor enters play mode, the scenario's `Shots` coroutine stages and photographs views 01-09, then runs the co-worker self-test (below) for views 10-13, and the editor exits. A built player also accepts `-pbCapture <folder>`.

### Scripted self-test of the co-worker event

The capture run ends with a scripted self-test (`CoworkerShots` in `WarehouseForklift.Awareness.cs`). It puts the learner, with PPE on, in the interior walkway facing the loading zone, marks board steps 2 and 3 done, and lets the event play live: the forklift drives to the loading zone, the co-worker appears, and the scene freezes. It checks that nothing moves during the freeze, presses the real answer buttons (questions 1 and 3 with the simulation's answer, question 2 with a wrong one, so the expected score is 2 of 3), checks that the scene resumes, keeps the learner still and looking at the co-worker, and then checks that the event finishes, the forklift drives off, the board's mistake counts are unchanged, and the summary panel appears. Results are written to the log as `[selftest]` and `[awareness]` lines. Run of November 1, 2026 (excerpt):

```
[awareness] check starts. Truth: co-worker 50 deg right of where you faced, 5.7 m away; walking toward/away component 0.71 (toward +), across component -0.71 (left-to-right +); forklift 6.3 m
[selftest] paused: timeScale = 0, forklift + co-worker moved 0.0000 m in 0.6 s of real time
[selftest] awareness check score 2 of 3; expected 2 (Q2 deliberately answered wrong)
[selftest] forklift holding for the co-worker = True (phase BackOut), co-worker at (-7.97, 0.00, 7.15)
[awareness] behaviour: held position = True (moved 0.00 m, entered lane = False), looked at co-worker = True (gaze 9.8 s)
[selftest] forklift moved off after loading = True; mistakes 0 (was 0), critical 0 (was 0)
[selftest] PASS
```

The self-test turns off the head's `TrackedPoseDriver` for its run: with XR stopped by the capture driver, that component reset the camera rotation every frame and undid the scripted head direction. This affects only the capture run. Whether the same happens in normal desktop play with no headset has not been checked.

## Baked scenes

`Assets/Scenes/WarehouseForklift.unity` is **baked**: the warehouse that `Start()` builds (shell, lights, floor markings, racking and cartons, cargo block, dock, signs and stands, PPE items, cones, mirror, hazards, the forklift in its start pose, and the co-worker event's staff door, loading-zone paint and stand) is saved in the scene as about 630 ordinary GameObjects, so you can see, select and edit them in the Hierarchy and Scene view. Generated materials and textures are in `Assets/Baked/WarehouseForklift/` (commit it with the scene). At Play the same `Start()` runs and the kit helpers adopt the saved objects instead of making new ones. How baking and adoption work, the editing rules (do not rename, re-parent or reorder baked objects; untick instead of deleting) and the menus **Pitt-Bradford > Bake scene (play mode)** / **Unbake scene** are described in the kit README: [../packages/com.pittbradford.xrtraining/README.md](../packages/com.pittbradford.xrtraining/README.md#baked-scenes-bake-and-adopt). Re-bake after changing the build code. **Set up project** does not replace a baked scene.

Specific to this project:

- **Still made at runtime (never baked):** the XR rig, the player body and PPE zones, the task board, the near-miss flash, the corner-mirror camera and its render texture (the mirror face gets its material at every Play), the forklift's speech bubbles, the co-worker, and the awareness-check blanking shell, question panels, prompts and summary.
- **Moved by the script while it runs, so position edits do not last:** the forklift (drives its route; it is baked in its start pose), its lift and cargo, the driver's arm, the PPE items and cones once grabbed.
- **Rack cartons:** all cartons are baked. When the co-worker hazard is on, `Start()` removes the cartons in the west-wall bay where the forklift puts its pallet, at every Play (also on baked cartons). The bake skips that removal and the runtime mesh merging (static batching), so the scene file keeps the plain objects.
- **The `Coworker Hazard` toggle** (inspector, on the `WarehouseForklift` component) needs no re-bake. The scene was baked with it on. Turn it off and, at Play, the baked staff door, loading-zone paint and stand are hidden (the objects `Co-worker event`, `Loading zone markings`, `Loading zone sign` are set inactive), no co-worker is made and the cartons stay on the shelf, as in the unbaked scenario. If a scene is baked with the toggle off, those three objects are simply not in the scene and are built at Play when it is turned back on (re-bake only if you want to see them in the editor).

Checked on November 2, 2026 (desktop editor, emulated, no headset): the bake saved 627 objects (play mode had 690; the rest are the runtime-only objects above); the scene file is about 1.7 MB and opens in under 0.05 s. The 13 capture shots were rendered with the scene unbaked and baked (`-pbOfflineWeather`) and compared: at most 0.35% of pixels differ by more than 8 of 255 levels in any shot, at text glyph and sign panel edges, the hard hat's resting position (about 1 px), and the animated beacon, driver's arm and live co-worker shots, which also differ between two baked runs (up to 0.39%). The scripted self-test passed on the baked scene (awareness score 2 of 3 as expected, `[selftest] PASS`). At Play the baked and unbaked scenes hold the same objects (694, same names and counts, no duplicates); a table moved in the saved scene stayed at its new position at Play; with `Coworker Hazard` off the event scenery was hidden, no co-worker was made and all 52 cartons stayed.

## Screenshots

Desktop editor renders made by the capture run above (November 1, 2026). They are not headset captures. Views 01-09 are staged by the script (the forklift is placed and frozen, cones are dropped by the script). Views 10-13 are taken during the scripted self-test while the event plays live, with a script standing in for the learner. None of them is a recorded play-through by a person.

| | |
|---|---|
| ![Warehouse overview](screenshots/01-warehouse-overview.png) | ![PPE station](screenshots/02-ppe-station.png) |
| 01 Overview | 02 PPE station |
| ![From the crossing: forklift stopped, driver bubble](screenshots/03-crossing-forklift-stopped.png) | ![Corner mirror showing the hidden lane](screenshots/04-corner-mirror.png) |
| 03 Crossing, forklift stopped | 04 Corner mirror |
| ![Spill and bent upright marked with cones](screenshots/05-hazard-markers.png) | ![Near-miss flash](screenshots/06-near-miss.png) |
| 05 Hazard markers | 06 Near miss |
| ![Start view](screenshots/07-start-view.png) | ![Blind corner sign, readable from the walkway](screenshots/08-blind-corner-sign.png) |
| 07 Start view | 08 Blind corner sign |
| ![Safety cones sign and tray](screenshots/09-cone-station.png) | ![Co-worker comes out of the staff door while the forklift starts its put-away](screenshots/10-coworker-appears.png) |
| 09 Cone station (loading-zone stand behind) | 10 Co-worker appears; forklift putting a pallet away |
| ![Awareness check: scene blanked, first question](screenshots/11-awareness-check.png) | ![Driver holding while the co-worker passes behind the forklift](screenshots/12-coworker-behind-forklift.png) |
| 11 Awareness check, question 1 (learner's view) | 12 Driver holding for the co-worker |
| ![End-of-scenario summary with the awareness check result](screenshots/13-summary.png) | |
| 13 Summary with the awareness check (scripted answers, 2 of 3) | |

## Files

- `Assets/Scripts/WarehouseForklift.cs` - the scenario, rules, and screenshot script
- `Assets/Scripts/WarehouseForklift.Awareness.cs` - the optional co-worker event, awareness check, summary panel, and self-test
- `Assets/Scripts/CoWorker.cs` - the walking co-worker built from primitives
- `Assets/Scripts/Forklift.cs` - the forklift, driver, and its behavior (including the put-away at the racking and holding for the co-worker)
- `Assets/Scripts/WfBillboard.cs` - keeps a label or the driver's speech bubble facing the player
- `Assets/Editor/ProjectSetup.cs` - the Pitt-Bradford menu items
- `Assets/Scenes`, `Assets/XR`, `Assets/Resources`, `ProjectSettings` - created by **Set up project**
- `Assets/Baked/WarehouseForklift/` - materials and textures written by **Bake scene**; the baked scene refers to them (see "Baked scenes")
- `screenshots/` - the 13 desktop editor renders above
