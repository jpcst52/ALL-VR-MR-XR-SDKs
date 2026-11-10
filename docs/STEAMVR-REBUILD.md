# Rebuilding the Vive examples on Valve's SteamVR plugin

Status as of 2026-11-08. General safety-awareness practice only. The checks so far were run in SteamVR's simulated headset, which has no controllers (see the table at the end).

## Why

The first Unity builds used a custom OpenXR rig with thumbstick teleport. On the Vive wands that did not work (the exact failure was never captured). The owner decided to rebuild all five Vive examples (hub and projects 01 to 04) on Valve's own SteamVR Unity plugin and its Interaction System (Player, Teleporting with TeleportArea, hands, Throwable, Interactable). Development is currently done away from the Vive, so only simulators are available.

## Architecture

All in the shared kit `vive-steamvr/packages/com.pittbradford.xrtraining`.

| Piece | File | What it does |
|---|---|---|
| `XRBackends` | `Runtime/XRBackend.cs` | Two static hooks, `Rig` (`IRigBackend.CreateRig`) and `Grab` (`IGrabBackend.Setup`), plus `IHandInput` (trigger, grip, stick, haptics). The kit's `XRRig`, `Hand` and `Grabbable` call them. With no backend, or if the backend returns null, the kit builds its own Input System rig as before. |
| `SteamVRBackend` | `Runtime/SteamVR/SteamVRBackend.cs` (own asmdef, compiled only with the `PB_STEAMVR` define) | Adopts the Player, Teleporting and `PB Teleport Area` that are saved in the scene (`FindFirstObjectByType<Player>`); only if the scene has none does it instantiate `Resources/PBSteamVR/Player` and `Teleporting`. Then puts the kit's `Hand` on Valve's hands, adds one TeleportArea over the training area, and wraps each `Grabbable` with Valve's `Throwable` (a small `PBThrowable` subclass that reports pick-up and release back to the scenario). Registers itself before the first scene loads. Steps aside when no headset is present. |
| `DeskMode` | inside `SteamVRBackend.cs` | Keyboard and mouse fallback. Switches on by itself 3 s after start if no controller is tracked; off again once one is. WASD walk, mouse look, left click = trigger, E = grip, Esc frees the mouse. Also lifts the origin by 1.6 m if the head reports height 0, and sets the window to 756 x 850. |
| Setup script | `scripts/setup-steamvr.ps1` + `vive-steamvr/steamvr-setup/SteamVRProjectSetup.cs` | Moves a project onto the plugin (details below). |
| Simulator script | `scripts/steamvr-simulator.ps1` | Turns SteamVR's null driver on or off and runs a build with a screenshot. Passes `-pbHeadOffset 1.6` to the build, because the simulated headset reports height 0. See [STEAMVR-SIMULATOR.md](STEAMVR-SIMULATOR.md). |

### Objects saved in the scene (owner requirement, 2026-11-08)

Valve's rig is a real object in each scene file, as in Valve's sample: `XR Rig > Player (SteamVR)` (VRCamera, LeftHand, RightHand, each hand with a `<Hand> render model` child), `Teleporting (SteamVR)` and `PB Teleport Area` (a TeleportArea mesh over the scenario's walking area; the Lobby has one over its floor). Placed by `Editor/SteamVR/SteamVRRigPlacement.cs` (menu Pitt-Bradford > SteamVR: place Player and Teleporting in open scene, or `-executeMethod PittBradford.XR.EditorTools.SteamVRRigPlacement.PlaceInScenesAndExit [-pbScenes "a;b"]`). The scene generators (`Configurator.CreateScene`, `HubDressing.SaveScene`) call it through `SceneRigHook`, so a rebuild keeps it. At Play the backend resizes the area mesh only if the scenario's `rig.Area` differs. Desk mode no longer hides Valve's hand models: with no controller tracked they are carried to a rest pose in front of the camera and put back when a controller appears; the left kit pointing ray (which started at the camera and showed as a large yellow wedge) is off in desk mode and the right one is a very thin line. Scene backups made before the change: scratchpad folder `scene-backup` (outside the repo).

### Panels, text fit and the lobby (2026-11-08, round 4)

- **Boards and cues are scene objects.** `TaskBoard.Create` and `WhyCue.Create` now go through `P.Node` / `Ui.Panel` / `P.Text`, so in a baked scene they adopt the saved objects (`Task Board`, `Why cue > Why this matters`, `Rationale question > Option A/B/C`; Refinery has `Task Board 1` to `4` under `Task board anchor`). Scenarios call `why.Prepare(Question)` so the question panel is saved too (hidden until asked). The code that makes them runs only while `SceneBaker` bakes in the editor (`Bake.Staging` lets the Refinery also build the boards of its other tasks). `TaskBoard` and `WhyCue` are no longer in the baker's runtime-only list.
- **TextFit** (kit `Runtime/TextFit.cs`, saved on each text, limits visible in the Inspector): breaks text at spaces and shrinks it using the real font's glyph widths. Width formula checked against Unity's renderer bounds (ratio 1.000 for Segoe UI and the built-in font). Panel heights in the question panel come from measured line spacing, not 0.6 x font size.
- **Audit**: `Pitt-Bradford > Audit text overflow (open scene)`, or `-executeMethod PittBradford.XR.EditorTools.TextOverflowAudit.RunAndExit [-pbScenes "a;b"] [-pbAuditFree]`; `...FixAndExit` adds a TextFit to any overflowing text and saves. Text must stay inside 4% (max 4 cm) of its panel edge. Counts over the nine saved scenes: 9 overflowing before, 0 after. The "before" scenes did not contain the boards or cues (they were built at Play), so those were not counted before.
- **Lobby glass building** (`hub/Assets/Editor/HubLobbyGlass.cs`): mullions, transoms, gold sills, tinted panes with a frosted band (baked frost strip texture), glazed roof, ground apron, reflection probe. Steel and glass are saved as one combined mesh per material (the individual parts stay in the scene, switched off, as the editable source) because Unity did not batch the 60 separate pieces. Editor counters for the Lobby, 120-frame medians: before set-pass 321, draws 369, batches 344; with separate pieces 384 / 485 / 418; as combined meshes 336 / 392 / 360.
- Fixed on the way: `SteamVRBackend.TeleportAreas` used `GetComponent<T>() ?? AddComponent<T>()`, which never adds the missing component (Unity null), and threw `MissingComponentException` at Play.

Still the kit's own, not Valve's: pressing valves and buttons uses the kit's pointing ray with the trigger. Everything else listed above is Valve's components.

The plugin itself (release 2.8.5b, BSD-3-Clause, about 100 MB per project) is git-ignored. The setup script downloads it and restores it.

## Migrate, build and test one project

Close every Unity editor first. Unity 6000.3.20f1 is required. About 15 minutes per project.

1. `powershell -File scripts\setup-steamvr.ps1 -Projects 02-refinery-rounds`
   - One project per call (a comma list does not work through `-File`). The hub needs `-IncludeHub` and its licensed local files (`Assets\Scenes\Local\Lobby.unity`, `Assets\Rocketbox`; see [BUILD-THE-HUB.md](BUILD-THE-HUB.md)).
   - Steps, each skipped if already done: add the packages the plugin needs to `manifest.json`; copy the plugin files into `Assets\SteamVR` (the script unpacks the `.unitypackage` itself, without Unity); patch `HandCollider.cs` and `Arrow.cs` (`PhysicMaterial` renamed `PhysicsMaterial`, an error in Unity 6.3); copy `SteamVRProjectSetup.cs` into `Assets/Editor`; generate Steam Input actions from Valve's example; assign the OpenVR loader, Direct3D 11 and the `PB_STEAMVR` define; compile.
2. `powershell -File scripts\build-vive.ps1 -Projects 02-refinery-rounds`
   - `build-vive` does not yet run `setup-steamvr` by itself for a project that lacks `Assets\SteamVR` (to do).
3. Test in the simulated headset: `scripts\steamvr-simulator.ps1 -Enable`, then `-Run builds\vive\<project>\<Name>.exe -Shot out.png -Seconds 35`, then `-Disable` (the script edits SteamVR settings for the whole PC). Label screenshots "SteamVR simulated headset", never a real headset.
4. After any kit change run `powershell -File scripts\unity-compile-check.ps1 -Project <a project not open in the editor>` (per CLAUDE.md).

## Tooling gotchas

- Builds and setup need `-accept-apiupdate`, otherwise the API Updater dialog blocks batch mode.
- Do not use `Start-Process -Wait` with Unity: a lingering `UnityCrashHandler64` keeps it waiting. The scripts use `WaitForExit` and kill the crash handler.
- Write `manifest.json` without a BOM.
- On a PC without the plugin files (a fresh clone) Unity cannot import them with `-importPackage`: the committed `manifest.json` names the OpenVR package as a `.tgz` inside `Assets\SteamVR`, and the kit compiles against the plugin (`PB_STEAMVR`), so Unity stops on "Tarball package cannot be found" or on compile errors first. The Hub shows the same error when such a project is opened. `setup-steamvr.ps1` therefore copies the files out of the `.unitypackage` (a gzip tar) with `tar`, `.meta` files included.
- Plugin 2.8.5b is needed (stable 2.8.0 shows no picture in Unity 6); Direct3D 11 only, multi-pass stereo ([STEAMVR-SAMPLE.md](STEAMVR-SAMPLE.md)).
- The simulated headset sits on the floor unless the floor offset is set; the sign mistake once made the scenes look broken ([STEAMVR-SIMULATOR.md](STEAMVR-SIMULATOR.md)).

## What is verified

| Item | Status |
|---|---|
| Project 01 migrated and rebuilt | Done. In the simulated headset the rig, Teleporting and Throwable are created, no exceptions, head at 1.6 m (log lines). |
| Projects 02 to 04, hub | Migration in progress or pending. |
| Kit compiles with the backend | Compile check is required after every kit change; re-run it before relying on this. |
| Keyboard and mouse mode (`DeskMode`) | **Self-tested 2026-11-08** with `-pbDeskSelfTest` (keys and mouse injected through the Input System, so Keyboard.current / Mouse.current are what is read): starts 1 s after start with no controller; W moves the head; mouse delta turns it; the right hand model is in view; the Desk Hint panel is on; the pointer ray hovers a Pressable and a left click presses it; E picks up a Grabbable the ray hits within 3 m (Valve Hand.AttachObject, parented to the Valve hand) and releasing E lets go. PASS in projects 01-04 (player in the simulated headset) and 01, Lobby, hub Hydro (editor Play); the Lobby has no Pressable or Grabbable (SKIP). With a physical keyboard and mouse the editor Game view must be clicked once so it has focus. Run in the editor: Unity.exe -projectPath P -executeMethod PittBradford.XR.EditorTools.DeskSelfTestRunner.RunAndExit -pbScenes Assets/Scenes/X.unity -pbDeskSelfTest (the project must not be open). |
| Stretched picture in the simulator window | **Unresolved.** The 756 x 850 window did not fix it (capture still came out 1920 x 1080 and stretched). Probably how the simulator shows a single-eye picture, not the scene; not confirmed. |

## To do

- Migrate, rebuild and simulator-test 02, 03, 04 and the hub; screenshots.
- Scenario hint text does not mention trackpad teleport. Example: `"Point + trigger = press. Grip = grab. Desktop: click = press, E = grab, WASD, right mouse = look."` (`ResistorBench.cs` line 972, in projects 04 and hub; other scenarios may have similar text). Add the teleport instruction once the trackpad is confirmed on the Vive. Not changed yet because project 04 is being migrated.
- Make `build-vive` run `setup-steamvr` automatically.
- Vive test of the sample first ([STEAMVR-SAMPLE.md](STEAMVR-SAMPLE.md)), then of project 01.

Related: [STEAMVR-SIMULATOR.md](STEAMVR-SIMULATOR.md) · [BUILDS.md](BUILDS.md) · [VR-STAGE-QUICKSTART.md](VR-STAGE-QUICKSTART.md) · [HANDOFF-2026-11-08.md](HANDOFF-2026-11-08.md)

## Desk mode round (2026-11-08)

- Fixed: Valve's pose component rewrote each hand's local pose every frame (zero when untracked), so the rest pose in front of the camera was undone; it is now switched off in desk mode and back on when a controller appears.
- Fixed: a Throwable added in code had null UnityEvents (onPickUp, onDetachFromHand, onHeldUpdate), so every pick-up or release threw a NullReferenceException. This would also have broken a real wand grab. They are created in `SteamVRBackend.Setup`.
- Fixed: Valve's Throwable lets go when its grab action is not held, which is always true at the desk; PBThrowable skips that check in desk mode and DeskMode does the release on E up.
- New: E grabs the Grabbable the pointer ray hits within 3 m (Valve AttachObject), the ray crosses the middle of the view 3 m ahead, desk mode starts after 1 s (was 3 s), the mouse locks at start only if the window has focus, and a small saved panel `Desk Hint` under the camera (placed by SteamVRRigPlacement, switched on by desk mode) lists the keys.
- The scenario footer text in some scenes still says "right mouse = look" (mouse look is automatic); not changed.

## Unify round (2026-11-08, night): what was found and changed

Plain-language write-up: [UNITY-PROJECTS-GUIDE.md](UNITY-PROJECTS-GUIDE.md). Technical points:

- **SteamVR pauses the game (`Time.timeScale = 0`) and gives each Valve hand a dummy `_application_lost_focus` object whenever it takes input focus away** (headset in standby, which happens about 5 s after the app starts when nobody wears it or with the simulated headset; or the dashboard open). Desk mode now sets `SteamVR_Settings.pauseGameWhenDashboardVisible` to false while it is active (restored when a controller is tracked or the scene ends), un-pauses, and sends SteamVR's own `SteamVR_Events.InputFocus.Send(true)` when it sees the dummy so Valve's hands let it go. Without the focus step, un-pausing makes Valve's `Hand.FixedUpdate` throw every physics step. Verified in the simulated headset.
- With **no headset**, `SteamVRBackend.CreateRig` now switches off the saved Valve Player and Teleporting for that run (Valve's 2D fallback camera and "WASD EQ/Arrow Keys" text no longer sit over the kit rig).
- Hub Lobby portals: collider and `Pressable` on each `Gate`, `Hand.RayLength` is now a public field (the Lobby sets 20 m). `HubLobbyPortalSelect` patches an existing Lobby scene; `LobbyHub.BuildPortal` does the same on rebuild.
- `HubPolish.TryAttachPostFx` no longer throws every frame when the camera already has a PostProcessLayer.
- New tools: kit `SceneConsistencyAudit` (`-executeMethod PittBradford.XR.EditorTools.SceneConsistencyAudit.RunAndExit`), `scripts/hub-sync.ps1` (check or copy the scenario scripts shared by projects 02-04 and the hub), and `DeskSelfTest` heartbeat and `-pbHold` (keep the scene open for a screenshot).
- Correction: the OpenVR `StereoRenderingMode` 1 in all five projects is **Single Pass Instanced** (the player log says so), not multi-pass as written earlier on this page and in STEAMVR-SAMPLE.md (that text described the sample build).
