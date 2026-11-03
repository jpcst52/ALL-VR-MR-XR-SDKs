# Hub project (Unity, built-in render pipeline)

The local Unity project where the training scenarios are dressed with higher-quality Asset Store art, and where the lobby with portals to each scenario lives. It uses the shared kit in `../packages/com.pittbradford.xrtraining`.

**Status:** work in progress. The Hydrostatic Test Bay scene builds and plays in the Unity editor in desktop mode. It has not been tested on a Vive Pro 2. The lobby scene builds and plays in the editor (desktop mode); the portals have not been tried with a headset.

![Hydrostatic Test Bay in the hub, Unity editor Game view (desktop mode, not a headset)](../../docs/images/hub-play.png)

## Third-party assets stay local

The hub imports Asset Store packs (IGBlocks warehouse, Sci-Fi Warehouse Kit, Modern Office, Shipping Container, Warehouse, and five Courtyard prop folders). Their licenses do not allow redistributing the files, so the `.gitignore` commits only our own code and settings (`Assets/Editor`, `Assets/Scripts`, `Assets/Settings`, `Assets/XR`). Scenes that reference the packs are saved under `Assets/Scenes/Local/`, which is also ignored. A fresh clone therefore has no art; the scenarios still run from primitives in the other projects.

## Build the Hydrostatic Test Bay scene

1. Open `vive-steamvr/hub` in Unity Hub with Unity **6000.3.20f1**, with the packs imported.
2. Run **Pitt-Bradford > Build hub scene: Hydrostatic Test Bay (local assets)**. `Assets/Editor/HubDressing.cs` builds the warehouse shell (a floor, four wall strips and a roof), plant props, a supervisor's corner and Courtyard props around the shared `HydroTest` scenario, then saves `Assets/Scenes/Local/HydrostaticTest_Hub.unity`. A missing pack is skipped with a log line.
3. Press Play. Controls are the same as in [../01-hydrostatic-test/README.md](../01-hydrostatic-test/README.md).

## Lobby

![Hub lobby: open roof terrace under the autumn sky, portals with scenario thumbnails. Unity editor render, desktop, not a headset](../../docs/images/hub-sky-lobby.png)

Run **Pitt-Bradford > Build hub: lobby and all scenarios**. It saves five scenes under `Assets/Scenes/Local/` and registers them in the build settings (Lobby first): `Lobby`, `HydrostaticTest_Hub` (dressed with the packs), and plain `RefineryRounds_Hub`, `WarehouseForklift_Hub`, `ResistorBench_Hub`.

- **Portals:** step onto a portal's pad and stay about 1.4 s (progress shows on the pad), or press 1-4 on the desktop. `Assets/Scripts/Lobby/LobbyHub.cs` builds the hall from primitives, so the lobby itself needs no packs; the builder only adds a few office props.
- **Back to the lobby:** in any scenario hold both grips for 1.5 s, or press Backspace (`ReturnToLobby.cs`). A hint shows for the first 8 seconds.
- **Scenario scripts:** `Assets/Scripts/Scenarios/` holds copies of the scripts from projects 02-04. If you change those projects, copy the scripts here again.

Checked in the editor (play mode): the lobby renders, and standing on pad 2 loaded `RefineryRounds_Hub`. Not checked: the Backspace and two-grip return, the other three portals, any headset.

## Scenario thumbnails and seasonal skies

![Portal thumbnails, Unity editor render, desktop, not a headset](../../docs/images/hub-sky-thumbnails.png)

- **Thumbnails.** Each portal shows a 320x180 picture of its scenario on a rounded white frame inside the doorway. The pictures are downscaled from the editor-render screenshots of projects 01-04 (`01-test-bay.png`, `07-start-view.png` twice, `01-bench-overview.png`; none contains the logo image) by `tools/prepare_lobby_images.py` (Python 3 with Pillow: `python vive-steamvr/hub/tools/prepare_lobby_images.py thumbnails`). They live in `Assets/Scripts/Lobby/Resources/Thumbnails/`, which is inside the committed `Assets/Scripts` folder; the `.meta` files set "Non-power-of-2: None" so Unity keeps them at 320x180. A missing picture is skipped and the portal looks as before.
- **Open lobby.** The closed hall is now an open roof terrace: a waist-high parapet with a gold rail, four corner columns and an open ring beam, no ceiling. The welcome board and the university plate stand on posts.
- **Seasonal sky.** `SeasonSky` (kit, `Runtime/SeasonSky.cs` and `Runtime/SeasonSky.shader`, shader name `PittBradford/SeasonSky`) shows one 360 photo per season and starts on today's season (northern hemisphere). Every 30 s it moves to the next season with a glitch of about 1.1 s: the sky breaks into mosaic blocks that grow, gets monitor scan lines and a little red/blue colour split, swaps to the next photo at the peak, then sharpens again. The mosaic is computed from the view direction, not from screen pixels, so both eyes in a headset see the same blocks; only the faint scan lines are screen-space. A chip on top of the welcome board names the season and says "Sky: stand-in photo" (or "Sky: Bradford, PA" if the manifest marks a picture as taken there).
- **The pictures are stand-ins.** The four photos are CC0 images from Poly Haven chosen for a north-eastern look (open field, hillside, autumn woods, snowy field). **They are not photos of Bradford, PA.** Credits are in the kit's `Runtime/Resources/Skies/skies.json` and in `assets/skies/CREDITS.md`. To use your own 360 photos, see [../../docs/HOW-TO-RUN.md](../../docs/HOW-TO-RUN.md) section 3.
- **Refinery Rounds** (the only outdoor scenario) also uses `SeasonSky`, held on today's season with no rotation or glitch during training. Its own sun and trilight ambient are unchanged; the far fog is pulled halfway towards the photo's horizon colour. The other scenarios are indoors and keep their skies.
- **Public API:** `Next()` starts a transition now, `Show("winter")` jumps to a season, `CurrentLabel` / `CurrentId` / `Current`, `SeasonChanged` event, `Paused`, `Interval` (0 = no rotation), `HoldTransition(phase)` freezes the glitch for screenshots. If the shader or the images are missing it logs a warning and the scene keeps its flat or procedural sky. `Configurator.KeepShaderVariants` now also keeps `PittBradford/SeasonSky` in builds, and the hub builder references the shader from the scene.

| | |
|---|---|
| ![Spring](../../docs/images/hub-sky-spring.png) | ![Summer](../../docs/images/hub-sky-summer.png) |
| ![Autumn](../../docs/images/hub-sky-autumn.png) | ![Winter](../../docs/images/hub-sky-winter.png) |
| ![Mid-transition, peak: mosaic blocks, scan lines](../../docs/images/hub-sky-transition.png) | ![Refinery Rounds under the autumn photo sky](../../docs/images/hub-sky-refinery.png) |

All six are Unity editor renders, desktop, not a headset (the transition frame is frozen at its peak with `HoldTransition(0.5f)`; the university logo was swapped for the text wordmark before rendering because screenshots are committed).

Checked in the editor (play mode, Nov 1): scripts and the shader compile without errors; the menu build saves all five scenes; the lobby shows all four seasons, the chip updates, the portals show their thumbnails; with the interval shortened to 3 s the sky rotated through all four seasons with the transition; `RefineryRounds_Hub` shows the autumn sky. Not checked: any headset (stereo rendering of the sky and of the glitch is unverified), frame rate, a Windows player build.

## Look and feel (realism and signage)

![Dressed Hydrostatic Test Bay with panels, name tags and the university plate, Unity editor render, desktop mode, not a headset](../../docs/images/hub-hydro-1-bay.png)

- Every hub scene has a University of Pittsburgh at Bradford and CIST plate (`Ui.BrandPlate`). The logo file lives in `Assets/Resources/Branding/pitt-bradford.png`, which is git-ignored (university branding is not committed); without it the plate shows a text wordmark. The "CIST | Computer Information Systems & Technology" wording was confirmed by the owner on November 2, 2026.
- `Assets/Scripts/Lobby/HubPolish.cs` adds room tone, a pump hum, tone-mapping, bloom and a vignette; press **P** on the desktop to switch post-processing off. Frame rate on a headset has not been measured.
- The kit's signage helpers (`Signage.cs`) give rounded panels, name tags with leader lines, and a depth-correct text shader (`PBText.shader`).

## Baked scenes

Every hub scene (`Lobby`, `HydrostaticTest_Hub`, `RefineryRounds_Hub`, `WarehouseForklift_Hub`, `ResistorBench_Hub`) is **baked**: all its GameObjects are saved in the scene, visible in the Hierarchy and editable, instead of being generated at Start (see [../packages/com.pittbradford.xrtraining/README.md](../packages/com.pittbradford.xrtraining/README.md) for how bake and adopt work). Object counts in the saved scene files: Lobby 125, Hydrostatic Test Bay 239, Refinery Rounds 398, Warehouse and Forklift 632, Resistor Bench 403. Edits you make to baked objects persist at Play.

- **Build, then bake.** **Build hub: lobby and all scenarios** rebuilds the scenes from scratch and so removes any bake. After it, open each scene in turn and run **Pitt-Bradford > Bake scene (play mode)** (about 15 seconds each). **Unbake scene** returns a scene to generating at Start.
- The scenes and their baked assets (`Assets/Baked/`) are local only, like the other hub scenes, because the hub's `.gitignore` commits only our code.
- Still created at runtime on purpose: the VR rig, task boards, why-panels, the co-worker and awareness check, resistors and LEDs, gas puffs, room tone and pump hum.
- Not verified: headset, a built player, hand play of the baked hub scenes (checked by loading each through the portals in the editor, with a screenshot of each).

## Dressing and people (Warehouse and Forklift, Refinery Rounds)

Both scenes are dressed from code in `Assets/Editor/HubSceneDressing.cs` (`DressWarehouse`, `DressRefinery`), called from `HubLobbyBuilder.BuildPlain`, so a rebuild reproduces everything, people included. Props are placed by their rendered bounds (the packs' pivots differ), their colliders are switched off so nothing blocks the rig, hands or forklift, and they are marked batching-static.

- **Warehouse and Forklift:** loaded racking along the north wall, pallet staging row, parked IG forklifts (east and west strips), loaded pallets and a hand truck by the dock, a shipping container with crates, extinguisher stations, an office corner (Modern Office: desk, chair, computer, cabinet, water cooler, plant, world map), duct runs and hanging lights overhead, security cameras, barrels, ladders. Eight people: two pickers at the racking, two talking by the east wall, a supervisor with documents, one crouching at a pallet, one at the office desk, one by the entry. The walking co-worker of the awareness check is now a Rocketbox avatar (`Assets/Scripts/People/PeopleLibrary.cs` sets `CoWorker.ModelFactory`; the scenario's `Speed` parameter drives Idle/Walk).
- **Refinery Rounds:** two boilers, flue pipes, ladders, a stacked container yard, ducts, barrels, a generator with fuel cans and floodlights near the flare, pallets and crates, cabin details. Props stay within about 22 m of the unit centre, so the muster point (26 m downwind) is clear. Eight people: a guard by the gate, operators at the tower and the drum, a pair at the control-room cabin, one at the generator, and a pair at the muster point (`FollowTarget.cs` keeps them with the sign when the wind moves it).
- **Rebuild:** **Pitt-Bradford > Build hub: warehouse and refinery only** rebuilds just these two (the other scenes keep their bake); then open each and run **Bake scene (play mode)**. **Build hub: lobby and all scenarios** resets all five scenes, so bake all five afterwards.
- **Courtyard props** import at 100x and rely on a 0.01 root scale; `HubDressing.Spawn` now keeps the prefab's own root scale (this also affects the Hydrostatic Test Bay's generator and lights).
- **Checked (Unity editor, desktop):** rebuilt, baked, played; the people animate (no T-pose); the Rocketbox co-worker walks the route in the Walk state, freezes with the awareness pause (movement 0.0000 m while paused) and the scripted self-test run (`Assets/Scripts/Dev/ShotRunner.cs`, same flow as `-pbCapture`) ended PASS with a 2 of 3 awareness score as designed. **Not checked:** any headset, frame rate on a Vive Pro 2, hand play. Triangle counts in the editor (static meshes plus skinned people): about 1.4 M for the warehouse and 1.5 M for the refinery, which is heavy for a headset and has not been optimised (LODs, lighter props).
- Screenshots (Unity editor desktop render, not a headset): `docs/images/hub-dressed-warehouse-*.png`, `docs/images/hub-dressed-refinery-*.png`.

## Files

- `Assets/Editor/HubSetup.cs`: project configuration (built-in pipeline, OpenXR).
- `Assets/Editor/HubDressing.cs`: the Hydrostatic Test Bay scene builder and shared helpers.
- `Assets/Editor/HubLobbyBuilder.cs`: builds the lobby and all scenario scenes.
- `Assets/Scripts/Lobby/`: `LobbyHub` and `ReturnToLobby`; `Resources/Thumbnails/` holds the portal pictures.
- `tools/prepare_lobby_images.py`: makes the thumbnails and copies the season skies into the kit.
- `Packages/manifest.json`: packages, including the kit by `file:` path.

## Known limits

- The shell is about 39 m by 37 m, much larger than the test area, so the equipment sits in the middle of a big empty hall. Lighting, audio and post-processing have not been tuned yet.
- Desktop editor screenshots only; nothing here has been run on a headset.

## People (Microsoft Rocketbox, local only)

The scenes can include realistic, animated people from the **Microsoft Rocketbox Avatar Library** (MIT license, https://github.com/microsoft/Microsoft-Rocketbox; cite Gonzalez-Franco et al. 2020, DOI 10.3389/frvir.2020.561558). The files (about 1.1 GB: 11 avatars plus walk, idle, talk and work animations) live in `Assets/Rocketbox`, which is git-ignored like the Asset Store packs, so nothing third-party is committed. To get them: sparse-checkout `Assets/Avatars/...` and `Assets/Animations/...` from that repository into `Assets/Rocketbox` (the avatars used are listed in `Assets/Editor/PeopleSetup.cs`). `Assets/Editor/Rocketbox/RocketboxImport.cs` (our own script) sets the import options, and **Pitt-Bradford > People > Set up Rocketbox controllers** builds the animation controllers. The MIT license text is kept at `Assets/Rocketbox/LICENSE.md`.
