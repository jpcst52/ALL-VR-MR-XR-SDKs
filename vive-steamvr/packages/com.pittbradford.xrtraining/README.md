# Pitt-Bradford XR Training Kit (`com.pittbradford.xrtraining`)

Shared Unity package used by the Vive / SteamVR example projects (01-04 and the hub) through a `file:` reference in each project's `Packages/manifest.json`. It provides the OpenXR rig with a desktop fallback (`XRRig`, `Hand`, `Pressable`, `Grabbable`, `SnapZone`), procedural materials, textures, shapes and text (`P` in `Procedural.cs`), the sound library (`Sfx`, `Synth`), lighting and particle effects (`Fx`), signage (`Ui` in `Signage.cs`), the task board, the "why this matters" panels (`WhyCue`), live weather, seasonal skies, the screenshot runner (`CaptureDriver`, `CaptureRunner`; it needs graphics, so it runs without `-batchmode`) and project setup (`Configurator`).

Editing a file here changes every project that uses the kit, including the hub. After a change, open at least one other project headlessly (`Unity.exe -batchmode -quit -projectPath <project> -logFile <log>`) and check the log for `error CS`.

## Where it is used and what has been tested

| Project | Uses |
|---|---|
| [01 Hydrostatic Test Bay](../../01-hydrostatic-test/README.md) | Rig, hands, snap zones, signage, task board, `WhyCue`, weather; the scenario itself (`Runtime/Scenarios/HydroTest.cs`) lives in this package. |
| [02 Refinery Rounds](../../02-refinery-rounds/README.md) | Rig, hands, grabbing, signage, task board, `WhyCue`, weather. |
| [03 Warehouse and Forklift](../../03-warehouse-forklift/README.md) | Rig, hands, snap zones, signage, task board. No `WhyCue`. |
| [04 Resistor Bench](../../04-resistor-bench/README.md) | Rig, hands, snap zones, signage, task board. No `WhyCue`. |
| [Hub](../../hub/README.md) | All of the above, the seasonal sky and the lobby. |

Tested: the package compiles in Unity 6000.3.20f1 inside each project (scripted editor runs, November 1 and 2, 2026) and the desktop fallback ran in the scripted capture runs. Not tested: any headset. The controller bindings (`<XRController>{LeftHand/RightHand}` trigger, grip and primary 2D axis) are written for the OpenXR Vive, Index and Touch profiles that **Pitt-Bradford > Set up project** enables; they have not been run on a Vive Pro 2 or a Quest. The package declares Unity 6000.0 and Input System 1.8.1 as minimums; the projects are pinned to 6000.3.20f1 and Input System 1.20.1 and only that combination has been run.

## Audio and effects (round 3)

Written November 3, 2026. API with short examples: [../../../docs/agents/KIT-AUDIO-FX.md](../../../docs/agents/KIT-AUDIO-FX.md). Honest status: the library compiles, every catalogue name generates and passes a numeric check (no NaN, peak below 1, not silent; menu **Pitt-Bradford > Sfx self-test**, or `-executeMethod PittBradford.XR.EditorTools.SfxSelfTest.RunAndExit [-pbWavDir <dir>]` which also writes WAV files), and project 01 runs with it in the desktop editor. **Nobody has listened to any of it on speakers or in a headset**, so levels, taste and the recorded-sample choices are unchecked. Spatial rolloff and the pitch of the pump hum are untested by ear.

### How it works

- `Sfx.Play(name, position, volume, pitchJitter)`, `Sfx.Play2D`, `Sfx.Loop(name, parent, volume, minDist, maxDist)`, `Sfx.Footstep(position, Surface)`, `Sfx.Enabled`, `Sfx.Preload()`.
- Each name has several variations (never the same twice in a row) and a little pitch jitter. One-shots use a pool of 24 AudioSources. Nothing plays while a scene bake is running or outside play mode.
- If a recorded sample exists for a name in `Runtime/Resources/Audio/` it is used (and levelled to the catalogue peak); otherwise the sound is synthesized in `Synth.cs` (filtered and band-passed noise, FM and inharmonic partials, pitch glides, envelopes, a small comb-filter room, random variation). Import rules for samples (mono, short clips decompress on load, loops compressed in memory) are in `Editor/AudioImport.cs`.
- Built in: `Grabbable` (grab, release), `SnapZone` (snap), `TaskBoard` (ui_confirm on a completed step, ui_error or error_buzz on a mistake), `XRRig` (footsteps; `FloorSurface`, `Footsteps`).
- `P.Tone` and `P.Noise` still work but are the old bare sounds; use `Sfx`.

### Catalogue

**Recorded CC0 samples** (OpenGameArt, author rubberduck, CC0; details in `Runtime/Resources/Audio/CREDITS.md`; chosen by file name, not by ear): `door_open`, `door_close`, `switch`, `water_drip`, `clink_metal`, `pallet_drop`, `snap`.

**Synthesized** (everything else): `ui_confirm ui_error ui_select button valve_turn grab release thud_box cone_place tape spray water_pour pump_hum gas_hiss alarm_siren alarm_siren_rise alarm_siren_fall gas_monitor_beep monitor_alarm radio_click radio_static forklift_horn forklift_engine_loop forklift_beep_reverse hydraulic_whine mirror_snap led_on spark_pop smoke_puff esd_click meter_beep wind_loop hvac_loop plant_drone_loop flare_roar_loop ding error_buzz pressure_tone gauge_tick footstep_concrete footstep_metal footstep_tile footstep_gravel footstep_wood`.

Loops (use `Sfx.Loop`): `water_pour pump_hum alarm_siren monitor_alarm radio_static forklift_engine_loop forklift_beep_reverse wind_loop hvac_loop plant_drone_loop flare_roar_loop pressure_tone`.

### How to add a sound

1. **A recording:** put a CC0 or public-domain `.ogg` or `.wav` in `Runtime/Resources/Audio/` named `<name>.ogg` (or `<name>_1.ogg`, `<name>_2.ogg` ... for variations), and add its source URL, author and licence to `CREDITS.md`. It overrides synthesis for that name.
2. **A synthesized sound:** add a generator to `Synth.cs` (a method taking a `System.Random` and returning a float array at `Synth.R` samples per second), then register it in the `Sfx` static constructor with `Reg("name", Synth.MyGenerator, loop: false, rate: 32000, peak: 0.6f, variants: 3)`. Run the Sfx self-test.
3. Add the name to the lists above and to `docs/agents/KIT-AUDIO-FX.md`.

### Effects (`Fx`)

`Fx.ApplyMood(MoodPreset, keyLight)` (IndoorBright, WarehouseDim, OutdoorOvercast, LabNeutral: ambient trilight, fog, pixel-light cap), `Fx.Practical` (point light for a visible fixture; beyond `Fx.PixelLightBudget` they are vertex lights), `Fx.LightShaft` (additive cone), `Fx.Dust`, `Fx.Steam`, `Fx.Smoke`, `Fx.Sparks`, `Fx.Drip` (with a drip sound), `Fx.ReflectionProbe` (realtime, one refresh at start), and `Fx.ParticleMat` / `Fx.SoftTexture`. No heat-shimmer helper (it needs a grab pass). All are made through the bake helpers, so a bake saves them and a baked scene adopts them.

### Procedural textures (`P`)

`P.ConcreteFloor`, `P.PaintedWall`, `P.Rusty`, `P.DiamondPlate`, `P.Grating`, `P.Wood`, `P.Cardboard`, `P.Rubber`, `P.Glass` (colour textures with structure: flecks, stains, joints, roller marks, rust, grain). There are no normal maps: the baker saves textures as plain colour maps.

## Desktop fallback (when no headset is active)

`XRRig.Headset` is true when an XR device is active (`XRSettings.isDeviceActive`); otherwise the rig uses the desktop controls. These are what the scenario READMEs and [../../../docs/TESTING.md](../../../docs/TESTING.md) describe:

| Action | Input | Source |
|---|---|---|
| Move | W A S D, 2 m/s, relative to the view direction, clamped to the scenario's play area | `XRRig.cs` |
| Look | Hold the right mouse button and move the mouse (pitch limited to 80 degrees) | `XRRig.cs` |
| Press | Left click; the pointer is the centre of the view, range 4 m | `Hand.cs` |
| Grab | Hold E; the object under the centre of the view within 3.2 m; release E to drop | `Hand.cs` |
| Answer a `WhyCue` question | Click, or press 1, 2 or 3 | `WhyCue.cs` |

In a headset: left stick or trackpad moves, the right stick snaps 30 degrees, the trigger presses and the grip grabs.

## Branding

`Ui.BrandPlate` (`Signage.cs`) shows the University of Pittsburgh at Bradford shield logo and the CIST line. The approved logo is shipped in this package at `Runtime/Resources/Branding/pitt-bradford.png` (published with the owner's approval on November 2, 2026). Without that file the plate shows a text wordmark. The brand guidelines PDF, other logo variants and licensed assets are not part of the repository.

## Baked scenes (bake and adopt)

The scenarios build their worlds in code at `Start()`. A scene can be **baked** so all of those objects are saved in the `.unity` file and can be seen and edited in the editor, while the same `Start()` code keeps working.

- **Menu:** **Pitt-Bradford > Bake scene (play mode)** bakes the open scene (or the first scene in Build Settings); **Pitt-Bradford > Unbake scene** removes the baked objects. Headless: `-executeMethod PittBradford.XR.EditorTools.SceneBaker.BakeAndExit` or `...UnbakeAndExit` (without `-batchmode`). Code: `Editor/SceneBaker.cs`.
- **How the bake works:** it enters play mode with time frozen, waits until nothing has been built for a few frames, then saves every object a kit helper made (minus runtime-only ones), converts runtime materials, textures and meshes into assets under `Assets/Baked/<Scene>/` (one asset per unique content), carries the objects out of play mode through a temporary prefab, unpacks them into the scene as plain GameObjects in their original places, copies the lighting `Start()` set (ambient, fog, skybox), adds a `BakedWorld` component to the scenario object and saves. Re-baking replaces the previous baked objects and deletes baked assets nothing uses any more.
- **How adoption works:** in a scene with a `BakedWorld`, the helpers `P.Node`, `P.Prim`, `P.Box`, `P.Cyl`, `P.Ball`, `P.Text`, `Ui.Panel`, `Ui.Tag` and `Ui.BrandPlate` return the saved object instead of creating one. Matching is by **parent + name + call order** (the n-th call for a given parent and name gets the n-th child of that name that was in the scene file). An adopted object is returned untouched, so scene edits are kept; text gets the runtime OS font and text material back (they cannot be saved; baked text uses Unity's built-in font so it is visible in the editor), and `P.Speaker` puts the generated clip back on the saved AudioSource. Scenes without a `BakedWorld` behave exactly as before.
- **What is baked:** objects created by the helpers (or adopted) that sit at the scene root or directly under an object from the scene file, and everything under them that a helper made or that was in the scene file. **Not baked:** anything made with plain `new GameObject` (it stays code-built, so it is never duplicated), anything under a code-built parent, objects with `[DontBake]` (`DontBake` component), and objects carrying `XRRig`, `Hand`, `TaskBoard`, `WhyCue`, `CaptureDriver`, `Camera` or `AudioListener`. Objects that do not exist yet when the bake runs (spawned during play) are naturally left out.
- **Do not mark baked objects static.** Static batching combines meshes when the scene loads, so a later re-bake would save the combined mesh instead of each object's own and the objects would disappear. (Dynamic batching still merges small meshes that share a material.)
- **Adopted objects keep what was saved**: after changing a material, texture or position in code, Unbake and then Bake again.
- **Do not rename, re-parent or reorder baked objects** the script looks up; it would create a new copy beside them. MonoBehaviours that should be saved must live in a file of the same name (that is why `Pressable`, `Grabbable`, `SnapZone` and `TagFacing` have their own files).

### Converting a scenario (recipe)

1. Replace `new GameObject(name)` + `SetParent(parent, false)` with `P.Node(name, parent)` (root-level objects: `P.Node(name)`), and `AddComponent<T>()` on those objects with `P.Ensure<T>(go)`. Keep plain `new GameObject` only for things that must stay runtime-only (anchors, spawned items), or add `DontBake`.
2. Wrap set-up that should not overwrite scene edits in `if (P.IsNew(go)) { ... }`: positions, rotations, scales and component settings applied after creation (for example `head.transform.localScale = ...`, light colour and range, rigidbody mass, collider size, a LineRenderer's material).
3. Event handlers (`Pressed +=`, `Grabbed +=`), list/field assignments and runtime state (`SetActive(false)`, `enabled = false`, `positionCount = 0`) stay unconditional: they must run in both cases.
4. If the script changes a material every frame (blinking lamps), take the renderer's own copy, `renderer.material`, instead of holding the material passed to the helper; a baked renderer shares a saved asset.
5. Do not rename objects after a helper creates them (`t.name = ...`); pass the name to the helper instead (`P.Text(..., name: "Label")`).
6. Run **Bake scene**, read the `[bake]` lines in the console (objects baked, anything "left to runtime"), then check that Play looks and behaves the same: run the project's capture with the scene baked and unbaked and compare the images (add `-pbOfflineWeather` so the wind is the same in both runs).
7. Commit the scene together with `Assets/Baked/<Scene>/`.

## Locomotion, hands and stereo (November 5, 2026)

- **Teleport and snap turn (`Teleporter.cs`, created by `XRRig.Create`).** Push either thumbstick forward (or touch the upper part of a Vive trackpad) to show a green arc and ring; let go to teleport after a very short black blink. Left or right on either stick turns `XRRig.SnapTurnDegrees` (45) about the head. A target is refused (red arc and ring, error tick) unless the surface is nearly level, within about 0.4 m above or 0.6 m below the current floor, inside `XRRig.Area`, not a loose rigid body, and clear of colliders from knee to head. A scenario can add a rule with `rig.Teleport.Filter`, or switch it off with `rig.TeleportEnabled = false` (or `LocomotionEnabled = false`). Smooth walking on the left stick is now off by default (`rig.SmoothLocomotion = true` brings it back; the right stick then still snap-turns). Desktop: hold T and release; WASD and mouse look are unchanged. A head-locked hint shows for about 18 s (not during `-pbCapture`).
- **Trackpad assumption.** The code reads `Primary2DAxis` (position) and `Primary2DAxisClick`. On a Vive wand through OpenXR that is the trackpad: touching the upper part counts as "forward" and lifting the finger fires the teleport; a click is also accepted while aiming. It was not tried on a Vive wand.
- **Hands.** `Hand` builds the controller model from code (handle, ring, tip) and keeps it switched on even with no controller tracked; on the desktop both hands are shown (the left one rests at the lower left with no input). `Hand.VisualOk` reports a missing or disabled model.
- **Hand check.** `Unity.exe -projectPath <project> -executeMethod PittBradford.XR.EditorTools.CaptureRunner.Run -pbHandCheck <folder> [-pbScene <part of scene name>] -logFile <log>` (no `-batchmode`) loads the scene, checks both hands (exist, on, renderers and materials, inside the view), tests teleport rules, snap turn and writes `hands-<scene>.png` and `teleport-<scene>.png`. Exit code 3 means a check failed. Desktop renders only.
- **Stereo shaders.** Every custom shader (`PBText`, `PBColor`, `SeasonSky`) carries the stereo-instancing macros; the headsets use Single Pass Instanced. `PBText` lacked them, which draws text in one eye only. Small `Resources/PBTextKeep.mat` and `PBFlatColorKeep.mat` keep both shaders in every build (`Shader.Find` alone drops them). Do not add a custom shader without the macros, and prefer the built-in ones (`Sprites/Default`, `Unlit/*`, legacy particles), which have them.
