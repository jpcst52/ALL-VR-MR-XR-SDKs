# Project status and task list

Target: advisory-board demonstration at Pitt-Bradford on **November 13, 2026**. Update this file whenever a task changes state. Status values: **Done**, **In progress**, **Blocked**, **Not started**.

Last updated: November 1, 2026 (evening).

## Demo hardware and environment (confirmed by the project owner)

| Item | Detail |
|---|---|
| Headsets | Meta Quest headsets (standalone, via Quest Browser), and an HTC Vive Pro 2 with the wireless adapter on SteamVR |
| Desktops | Dell Alienware Aurora ACT1250: Core Ultra 7 265F, RTX 5070 Ti 16 GB, 64 GB RAM, Windows 11 Home |
| Network | The VR lab has fast internet; the multiplayer relay can also run on the local wireless network |
| Offline | Browser demos must run with no internet (see `vendor/`); online features are allowed only in the Unity/Vive examples |
| Dev PC | RTX 4060 Laptop (8 GB). Unity 6000.3.20f1 and 2022.3.62f3 installed; Android SDK and an emulator available |

## Done

| Task | Notes |
|---|---|
| Ten SDK/framework examples with READMEs | OpenXR, Meta XR, SteamVR (Unity scripts), WebXR, A-Frame, Three.js, Babylon.js, ARCore, Apple spatial, Unity XRI |
| Browser example screenshots in each README | Four browser demos; Unity/Android/Apple cannot be screenshotted here |
| Fieldwork XR Practice Lab: 12 hands-on scenarios | 3 business, 3 research, 6 industrial. Desktop-tested; VR only in an emulator |
| Multiplayer Circuit Lab (VR) and Site Safety Layout (AR) | Relay with theme-separated rooms; 8 automated tests; desktop-tested |
| VR emulator capture tool (`tools/vr-capture`) | Emulated Quest 3 via Meta IWER; not a physical headset |
| Offline vendoring (`vendor/`) | Three.js, Babylon.js, A-Frame, fonts; all 7 browser pages pass `offline-check.mjs` |
| Three PowerPoint decks rebuilt (`tools/decks`) | Need updating again for the Vive/Quest work |
| Overall README with screenshot gallery | Needs updating as new projects land |
| ARCore Gradle wrapper and build fix | APK builds; does not run on the emulator image used |
| Research bibliography (`docs/research`) | 11 sources, each checked against a bibliographic record; BibTeX included |
| Branding collected | `branding/` kept out of git (public repo) |
| Shared Unity kit `com.pittbradford.xrtraining` | Compiles cleanly under Unity 6000.3.20f1; materials work in both URP and the built-in pipeline |
| Vive project 01: Hydrostatic Test Bay | Compiles and **runs in the editor in desktop mode** (owner screenshot, Nov 1). Not yet tried in a headset |
| Hub project created (`vive-steamvr/hub`) | Whitelist `.gitignore`: only our code is committed, never asset-pack files |
| Asset packs imported into the hub (5 of 6) | Big Warehouse Pack (IGBlocks), Sci-Fi Warehouse Kit, ShippingContainer, Modular Warehouse, Modern Office |
| Courtyard imported as its own project | `C:\UnityProjects\Courtyard\The Courtyard` (a "complete project" cannot be imported into another project without overwriting its settings) |
| Render-pipeline check of the packs | All 242 hub-pack materials, and the Courtyard, use the built-in pipeline (Standard shader). Decision: the hub uses the built-in pipeline, not URP |
| Web professional-look pass (Nov 1) | Shared `brand-kit/`: Pitt-Bradford + CIST brand bar on the launcher, Practice Lab and the three multiplayer pages (logo from the git-ignored `branding/logo.png` if present, text wordmark otherwise, never a broken image); every in-world label is a backed panel or tag in the bundled fonts; brand plaque in each 3D scene; phone layout is a bottom sheet. `tools/vr-capture/ui-check.mjs` (desktop + emulated phone, logo blocked and allowed) and `brand-kit/test`. README screenshots re-shot with the logo blocked. Not tried on a real phone or headset |

## In progress

| Task | State |
|---|---|
| Choose and copy Courtyard pieces into the hub | The Courtyard compiles (the 37 errors were a missing `com.unity.ugui` package, now fixed). Scanning its prefabs to select lobby architecture |
| Hub reconfigure for the built-in pipeline | `HubSetup.cs` and `manifest.json` updated (URP removed; UI and post-processing added). Owner must run **Pitt-Bradford > Set up hub project** once so the pipeline setting is cleared |
| Choose and copy Courtyard pieces into the hub | Needs the Courtyard compiling; then inspect its prefabs and select architecture for the lobby |
| Vive projects (`vive-steamvr/`) | Project 01 done in code; projects 02-04 not started; hub scenes not started |

## Not started

| Task | Notes |
|---|---|
| Hub scenes: lobby with portals to each scenario | Built (B6): `LobbyHub` primitive hall, 4 portals, `ReturnToLobby`, scenes in git-ignored `Assets/Scenes/Local`. Hall uses its own walls because the IGBlocks shell (39 x 37 m) is far too large. Desktop editor only |
| Realism pass on Hydrostatic Test Bay using the packs | Real wall/floor/steel materials, office furniture for a supervisor corner, lighting, post-processing, audio; legible task board; smaller floor text |
| Vive project 02: Refinery battery-limits rounds (live weather API) | Online API is acceptable on the Vive PCs |
| Vive project 03: Warehouse / forklift safety | Uses the warehouse, construction, and container packs in the hub; lite version from primitives |
| Vive project 04: resistor selection bench | Generic education, inspired by passive-component makers |
| README and screenshots for each Vive project | Screenshots via the windowed Unity capture runner after the editor is closed |
| Quest projects | Options: Unity Android builds of the lite projects; Quest web multiplayer "Hydrotest Builder"; MR Site Safety Layout. Asset-pack scenes are too heavy for a Quest without optimisation |
| Pitt-Bradford launcher UI | One home page with tiles per example and a recommended demo path; one-command start for the lab PC |
| Realism pass on web "hero" scenes | CC0 PBR textures/HDRIs, glTF models, quality switch, attribution file |
| New Practice Lab scenarios | Lighter lot sampling, heat-exchanger hydrostatic test, refinery pipe-spool welding, resistor selection |
| Circulation Builder for biology faculty | Draft built Nov 1 as Practice Lab scenario 17 (`biology-scenarios.js`, `biology-stations.js`, new "Life science (draft for faculty review)" group): place the four chambers, connect the pulmonary and systemic circuits in order, colour vessels oxygen-rich/poor. General biology only, no citations, labelled "Draft for faculty review" and "not clinical or medical guidance". 11/11 lab tests; desktop play-through only (no VR emulator or headset run). Needs faculty review of content and level |
| Multi-user training in the style of WaterWorks | Approach to decide; WaterWorks does not work on the Vive (owner-confirmed) |
| Self-signed HTTPS start script for the lab PC | As in the owner's WaterWorks project |
| Advanced README and board-facing deck | After the projects exist |
| Demo-day kit | Start script, QR codes, rehearsal checklist, backup recordings |
| Update decks, README gallery, and research notes | After each milestone |

## Blocked on the project owner

| Item | Needed |
|---|---|
| Courtyard errors | Confirm in the Courtyard Unity window that the 37 errors cleared after the `ugui` fix |
| Hub setup | Run **Pitt-Bradford > Set up hub project** in the hub editor (after it recompiles), then restart if asked |
| Vive WebXR check | Try `http://localhost:8000/training-simulations/` in Chrome/Edge with SteamVR running and report what happens |
| Biology faculty project | A few lines on what the faculty partner is building for students |
| Lighter-test process | A line on how lighters are tested, or a page that loads |
| Hardware testing time | A slot on the real Vive Pro 2 and Quest before Nov 13 |
| Naming companies on slides | Comfort naming local employers on slides, or keep it "inspired by" |
| Publishing | Whether to push commits; whether the logo may appear in the public repo |

## Known limitations and honest caveats

- Nothing has been run on a physical Vive or Quest. VR screenshots come from an emulated Quest 3 (Meta IWER) and are labelled as such.
- ARCore does not start a session on the Android emulator image used; build is verified, runtime is not.
- The Hydrostatic Test Bay compiles and runs in the editor in desktop mode, but has not been run in a headset.
- Scenario content is general safety-awareness practice, not a procedure; companies' real processes have not been verified.
- GitHub Models was retired on July 30, 2026 (per GitHub's docs); there is no Copilot-backed AI coach. The AI coach was cut from scope.

## Decisions log

| Decision | Reason |
|---|---|
| Hosting for Quests: HTTPS served from the lab PC with a self-signed certificate (as in the owner's WaterWorks project), not tunnels or domains | Simplest, works offline |
| Drop the AI coach and local-model setup | Owner wants simple training and has no time for local AI setup; GitHub Models retired |
| Vive gets native Unity/OpenXR projects (several, separate) | WaterWorks (WebXR) does not work on the Vive; multiple projects as fallbacks |
| Keep `branding/` and `assets-private/` out of git | Public repo; university branding and Asset Store licenses |
| Hub project uses the built-in render pipeline, not URP | All imported packs use Standard-shader materials; URP would render them pink |
| Import the Courtyard as its own project | Unity warns that a complete project overwrites project settings |
| Web demos use CC0 assets only; Asset Store assets only in local Unity builds | License terms |

## Decisions of November 1, 2026 (owner)

| Question | Decision |
|---|---|
| Name partner companies on slides | No; say "inspired by regional industry" |
| Lighter test | Stays generic; no process details |
| Circulation Builder for biology faculty | Build a draft from public general-biology knowledge, flagged for faculty review |
| Unity multi-user and Unity Android builds | Future work; the demo uses the web multiplayer and the web Practice Lab in Quest Browser |
| Lab time | Owner has Vive Pro 2 and Quest access this week |

## Decisions of November 2, 2026 (owner)

| Question | Decision |
|---|---|
| CIST wording on the plates | Confirmed: "CIST | Computer Information Systems & Technology" |
| Reasons and rationale questions (`training-simulations/reasons.js` and the Unity scripts) | Owner reviewed and said they look fine. Not reviewed by an outside subject-matter expert |
| Citations added to `references.bib` | Owner reviewed and said they look fine. Still read the full texts before citing any number in a paper |
| University logo in the public repo | OK to go with: the old launcher image containing the logo in local commit 4915dd1 may stay. The `branding/` folder and the hub's `Assets/Resources/Branding/` are still git-ignored (no logo files are committed); say so if you want the logo file added so a fresh clone shows it |
| Commit the work | Yes, committed locally on November 2, 2026; not pushed |

## November 3, 2026: hub Warehouse and Refinery dressed, with people

- `Assets/Editor/HubSceneDressing.cs` (hub) dresses `WarehouseForklift_Hub` and `RefineryRounds_Hub` with Asset Store props and Rocketbox people from code, so rebuilds reproduce it. Menu: **Build hub: warehouse and refinery only**.
- The warehouse co-worker is a Rocketbox avatar through `CoWorker.ModelFactory` (`PeopleLibrary`). Verified in the editor: walks in the Walk state, freezes at the awareness pause, scripted self-test PASS (`ShotRunner`). Not verified: headset, frame rate, hand play.
- Found and fixed: `HubDressing.Spawn` reset the prefab root scale, so the 100x Courtyard props (generator, floodlights) came out about 100 m wide. It now keeps the prefab scale (also affects the Hydro scene's props, which should be re-checked).
- Open: the Sci-Fi shipping containers render near black; the IG container was used in the warehouse. Triangle count is about 1.4-1.5 M per scene; optimise before relying on a Vive Pro 2 frame rate. Screenshots are editor desktop renders.

## Documentation audit (November 3, 2026)

- `npm run docs-check` (tools/docs-check/check.mjs) checks every relative Markdown link, image and anchor, every launcher path, that docs/README.md links every guide under docs/, and that the pass counts quoted in TESTING.md and HOW-TO-RUN.md equal a fresh run of the three unit suites (20, 16, 18). First run found one broken anchor (HOW-TO-RUN) and stale counts (brand kit 5 vs 16); both fixed.
- New: docs/GLOSSARY.md (terms and who-is-this-for), "Quick start for demo day" at the top of HOW-TO-RUN.md, and "Run it" / "What was verified" sections in the six native SDK sample READMEs. None of those samples was compiled or run on hardware in this pass.
- Main menu: search box (client-side, works offline), keyboard navigation (`/`, arrows, Esc), a status legend; launcher-check.mjs now tests them and refreshed docs/images/launcher*.png (plus launcher-search.png).

## November 3, 2026 (later): hub Hydro, Lobby and Resistor Bench dressed; all five scenes re-baked

- `HubSceneDressing.cs` now also builds `DressHydro` (a pack-built room around the bay: walls, door, drop ceiling, blue and gold wainscot, ducts, sprinklers, hanging lights, pipe runs, tool bench area with stocked shelving, supervisor corner, extinguishers, receiving stack; 6 people behind the stay-behind line), `DressLobby` (reception desk with the receptionist, seating and coffee lounge with a projector screen, waiting row, plants; 6 people) and `DressResistor` (the open room closed with painted walls, stocked shelving, student table with computers, projector wall, wall maps; 4 people). `Build hub: lobby and all scenarios` reproduces everything; all five scenes were then baked again.
- Found and fixed: after the scale change in `HubDressing.Spawn`, the Hydro scene's `WareMedium_Roof` ended up at floor level (its pivot is not at the wall top) and hid every person to the waist. The IG hall walls and roof are no longer used there (the bay has its own room); the floor stays.
- Triangle counts: the figures reported earlier (about 1.4-1.5 M for Warehouse and Refinery) were most likely read in Play mode. With static batching a MeshFilter's shared mesh is the combined mesh, so the sum counts it many times. Counted in edit mode on the baked scenes: Lobby 125 k, Hydro 149 k, Resistor 77 k, Warehouse 125 k, Refinery 189 k triangles. Frame rate on a Vive Pro 2 is still unmeasured.
- Checked in the Unity editor (desktop): all four pads load their scenes (rig stood on each pad); the scripted self-test (ShotRunner) ran the Hydro flow to all six steps OK and no mistakes, the Resistor flow and the Refinery flow to their ends, and the Warehouse flow including the awareness pause (score 2 of 3 as designed); people animate. Not checked: any headset, frame rate on a Vive Pro 2, hand play.
- Open: the Rocketbox "Documents" animation is a bent-over reading pose (right at a desk, odd when standing), so it is used only at desks; the Sci-Fi crates and pallets render dark.

## November 3, 2026 (round 2): hub critical review, richer lobby, render budget

- All five hub scenes were re-inspected from the start view and from walking height; details and before/after pictures (`docs/images/hub-r2-*.png`) are in `vive-steamvr/hub/README.md` ("Round 2 review").
- Lobby rebuilt (`HubLobbyDressing.cs`): tiled floor and medallion, reception, lounge, two scenario gallery walls with framed renders, CIST corner, "How it works" board, banners, plants, ten Rocketbox people. Two portal blurbs corrected after a reviewer found them inaccurate (Hydro is a heat exchanger; the Warehouse learner is a pedestrian).
- Found and fixed: overlapping containers in the Warehouse (furniture inside one), near-black Sci-Fi containers and crates (tinted copies of the materials), barrels inside a boiler, a hard hat in a classroom, a 25-lamp grid in the Hydro room, work-light spots above the Resistor ceiling.
- Render budget (editor Play mode counters, not a frame rate): SetPass calls fell from 251 / 1066 / 756 / 3196 / 1736 to 352 / 515 / 551 / 1301 / 787 (Lobby / Hydro / Refinery / Warehouse / Resistor, median, start view). Main cause was the pixel-light count (now 1, shadows 25 m with 2 cascades, set in `HubPolish`); the Warehouse mirror camera is throttled in the hub copy only. No headset and no Vive Pro 2 frame rate were measured.
- Checked: all four portals load; Warehouse scripted self-test PASS; other flows ran to the end; people animate. Not checked: headset, hand play.
- Decision for the owner: the hub copy of `WarehouseForklift.cs` now differs from project 03 (mirror camera drawn every third frame within 12 m). Copy the change to project 03 if you want them in step.


## November 5, 2026: Unity VR fixes (text in one eye, teleport, hands)

- **Text in one eye:** the kit shader `PBText` (all TextMesh text) had no stereo-instancing macros while the OpenXR settings use Single Pass Instanced, so it drew in one eye only. Fixed. The only other custom shaders are `SeasonSky` (already correct) and the new `PBColor` (`PittBradford/FlatColor`, stereo aware), which replaces `Hidden/Internal-Colored` (stereo support unknown) for the warehouse awareness dark shell. Everything else uses built-in shaders (Sprites/Default, Unlit, legacy particles, Standard, Skybox/Procedural), which support instancing. The mirror camera and capture cameras render to textures with `stereoTargetEye = None`, which is fine. Resources materials `PBTextKeep` and `PBFlatColorKeep` keep the shaders in builds.
- **Teleport:** kit `Teleporter.cs`. Push either stick (or the upper trackpad) forward, release to teleport (short blink); left or right turns 45 degrees. Smooth walking is off by default (it shared the left stick). Desktop: hold T. Invalid targets (steep, table-high, loose body, no standing room, outside `Area`) show a red arc. Assumption: Vive trackpad reads as `Primary2DAxis`, upper touch = forward, lift fires.
- **Hands:** the controller model is rebuilt, kept on when untracked, and on the desktop both hands show (the left was hidden). `-pbHandCheck <folder>` (see the kit README) checks both hands and the teleport rules and writes screenshots.
- **Verified:** compiles and `-pbHandCheck` passed in the editor desktop fallback for projects 02, 03, 04 (and the hub scenes, see Task-Status I1). **Not verified:** any headset, the stereo shader in a real Single Pass Instanced run, Vive wand/trackpad input, teleport feel. Owner should try it on the Vive.

### Web labs in headsets (Nov 5)

Circuit Lab, Site Safety Layout and the Hydro rig gained an in-headset toolbox (parts, four examples each for the first two, reset with confirm), Y (open/off) and B (close/on) on the part under the ray, X/A rotate, and glove hands on the controllers. Also fixed: aiming a controller at a part with a label sprite threw an error. Verified in an emulated Quest 3 only; see multi-player/README.md.

## November 5, 2026: sound effects pass 2 (task J1) - unheard, measured only

- **Why:** the owner said the Unity sounds were still crude. A measurement of the old catalogue (121 clips, project 04, headless) found: four sounds almost all hiss above 6 kHz (esd_click 82 percent of energy above 6 kHz, spark_pop 79, spray 70, gas_hiss 61), a siren loop with a seam, alarms and beeps 10 to 20 dB louder than clicks and footsteps (every clip peak-normalised to about -3 to -6 dBFS), bare sine beeps, white-noise beds and a 150 Hz saw buzz for errors.
- **What changed (all in the kit, `vive-steamvr/packages/com.pittbradford.xrtraining`):** `Synth.cs` rewritten (impacts = noise transient + modal body with inharmonic partials per material + thump; pink noise; stick-slip valve squeak; bubble chirps for water; softer sirens, beeps and horns with soft attacks and fewer high harmonics; one interface sound family in one key; heel-and-toe footsteps per surface; loops that close exactly), `Sfx.cs` (loudness target per name, soft limiter under 0.85, high-pass, gentle per-name low-pass, fades, priorities, per-name reverb share, distance low-pass on loops, volume jitter, recordings get the same treatment), new `SceneAudio.cs`, `SceneAudioDirector.cs`, `SceneAudioBoot.cs`, `SfxFar.cs`, new editor tools `SfxReport.cs` and `SceneAudioCheck.cs`. 11 new names (5 scene beds, 6 distant events). No scenario script was edited: the scene audio starts by itself by scene name. Old names and calls all still work.
- **Measured (same tool, `SfxReport`):** clips with energy above 6 kHz over 35 percent: 4 names before, 0 after (esd_click 1.7 percent, spark_pop 4.0, spray 6.5, gas_hiss 5.3); loop seams 1 before, 0 after; clipping 0 before and after (peak at most -1.5 dBFS after, all levelled to a per-kind target, 172 of 173 clips within 1.5 dB of it, the last held 2 dB under by the limiter); start or end clicks 0. Loudness by design now steps from beds (about -30 dBFS active RMS) through machines, interaction, to alarms (about -14); before it was the reverse for alarms and beeps (-10 to -14 whole-clip RMS against -22 to -29 for clicks) and the whole set was peak-normalised near -4 dBFS. Overall the one-shots are now about 3 dB quieter on average (active RMS mean -18.8 against -15.5, because the alarms and hiss were deliberately brought down); raise `Trim` in `Sfx.cs` if the headset feels quiet.
- **Checked in the editor, project 04 only:** the kit compiles; `SfxReport` and `SfxSelfTest` (372 clips) pass; `SceneAudioCheck` passes 18 checks in play mode (auto-start by scene name, reverb preset, bed source playing, pooled voice settings, loop low-pass, silence during a bake flag). Projects 01 to 03 and the hub were not run by me (another agent was using them); they share the package, so they will compile it the same way, but that was not tested.
- **Not verified:** **nobody has listened**, on speakers or in a headset. Levels, taste, reverb preset choice (Room, Hallway, Stoneroom, Auditorium, Plain) and the timing of distant events are reasoned, not heard. A real headset also applies its own volume and spatializer. No new recordings were added: the one attempt (Kenney) was refused by the site and no licence could be verified, so nothing was used (see the kit `CREDITS.md`).
- **For the owner:** listen in a headset to each scene, then tell us which sounds are still wrong by name (`Sfx.Names`); the WAVs for every variation can be regenerated with `SfxReport` (see `docs/agents/KIT-AUDIO-FX.md`).

## November 5, 2026: visual quality pass 2 (task J2) - desktop renders only, no headset

- **Why:** the owner wanted a clear visual gain on the five Vive scenes without raising the render budget (built-in pipeline, no post-processing package, no URP).
- **Found (looked at every scene first):** Warehouse dark and flat (near-black lanes, grey ceiling, little light pooling, flat blue dock door); every textured surface was flat colour with no relief (no normal maps anywhere); procedural textures were 256 px; windows in Hydro were flat blue; nothing stood on the floor (no contact shadows); no anisotropic filtering or MSAA set by the kit; Resistor floor and ambient very dark; Refinery ground ended at 65 m inside the fog.
- **Changed (kit `Runtime/Procedural.cs`, new `VrQuality.cs`, `Editor/ContactShadows.cs`):** (1) `P.Mat` now derives a tangent-space normal map from the fine detail of any generated texture (brightness minus a local average, so stains stay flat and grain, blocks, planks, flutes get relief); picture-like textures (signs, decals, stripes) are skipped; metals use a weaker bump; the baker saves them as real normal-map textures. (2) Noise textures are 512 px with a fine-grain octave. (3) `P.Daylight()`: a lit emissive sky / tree-line / yard picture for the Warehouse dock door and the Hydro windows. (4) `ContactShadows`: ONE merged mesh and ONE alpha-blend material per scene (about 50 footprints in the Warehouse) is added at the end of every bake. (5) `VrQuality`: 4x MSAA, anisotropic filtering forced on (16x limit), 2 shadow cascades, soft shadows, shadow distance at most 40 m, LOD bias 1.5. (6) Per scene: Warehouse ambient up about 30 percent, lighter floor with more gloss (picks up the probe), lighter lane asphalt texture, stronger light-pool decals, daylight dock; Resistor ambient and ESD tile lighter; Hydro hub ambient up; Refinery ground 300 m (was 130 m). (7) New `PerfProbe` moved into the kit with a headless `-pbPerf <file>` mode, `-pbTimeout` watchdog for capture and perf runs, hub `HubBakeCli` / `HubLobbyBuilder.BuildOneCli` for headless rebuild and bake.
- **Editor CPU counters (Play mode, median, start view, Ultra, same `PerfProbe`; NOT a Vive frame rate), before / after, set-pass calls and draw calls:** Lobby 354 / 436 to 308 / 356 (the lobby was rebuilt from its builder; it also lost its realtime key-light shadows to the budget pass); Hydro hub 344 / 398 to 348 / 400; Refinery hub 546 / 1,085 to 540 / 1,093; Warehouse hub 583 / 1,282 to 589 / 1,284; Resistor hub 568 / 812 to 570 / 813; project 01 437 / 507 to 442 / 509; project 03 406 / 1,018 to 408 / 1,020; project 04 no before value (not measured before the change; its hub twin, same scripts, moved +2 / +1), after 627 / 928; project 02 not re-run. Contact shadows add 1 draw call per scene; normal maps, MSAA, anisotropy cost nothing in these counters.
- **Checked:** the kit compiles; the five hub scenes and projects 01, 03, 04 were re-baked and re-shot; `-pbHandCheck` passes for the five hub scenes, 01, 03 and 04; no exceptions from our code in the bake logs.
- **Tried and not done:** baked lightmaps (the pack meshes have no lightmap UVs and SceneBaker's saved meshes are unreadable to the lightmapper; too risky for the time and the hub scenes are rebuilt from code, so a bake would have to be redone each time); a world-space (triplanar) surface shader, which would fix texture stretch on large boxes (walls use per-face UVs, so 28 m walls stretch a texture) but costs 3 samples per map in pixel shaders at 2448 x 2448 per eye; a larger Refinery ground did not remove the visible ground edge in the aerial overview (it is farther away and less obvious from the player's height).
- **Not verified:** nothing was run in a headset; GPU cost of MSAA 4x, anisotropy 16x, normal maps and shadow settings at 2448 x 2448 per eye on the RTX 5070 Ti is unmeasured; the textures' look at VR distance is unjudged; sharper text was not changed (signs already use a 160 px dynamic font; MSAA should help edges).
- **Resolved Nov 6:** project 02 (Refinery standalone) was re-baked and re-shot after the hung Unity (PID 11140) and a stale queued job were ended and the stale scene backup in its Temp folder removed. Editor counters: set-pass 358, 1,003 draw calls (not a Vive frame rate). Before/after pictures: `docs/images/hub-r5-before-*.png` and `hub-r5-after-*.png`.


## November 6, 2026: baked lighting spike on the hub Warehouse (task J3) - editor desktop renders only, no headset

- **Question:** can lightmaps make the dark, flat Warehouse aisle better? **Answer: feasible, but it did not help; not rolled out.** The other four hub scenes were not touched.
- **Feasibility (verified):** the Warehouse has 2,122 mesh renderers, 1,973 usable as static. Unity primitives and the baker's saved mesh copies already carry a second UV set (17 meshes, no unwrap needed); 14 pack FBX files (SciFi Warehouse Kit, IGBlocks, Modern Office) were switched to `generateSecondaryUV` by the editor script (local, git-ignored assets). Nothing needed `Unwrapping` at bake time. The earlier "saved meshes are unreadable to the lightmapper" worry did not occur. The new `vive-steamvr/hub/Assets/Editor/HubLightmap.cs` (`BakeCli`, `ClearCli`) sets static + ContributeGI, emissive materials to baked, lights to Mixed (or Baked with `-pbLmBaked`), a 200-probe light-probe grid, a LightingSettings asset (Progressive GPU, 12 texels/m, 1024 max, non-directional, 3 bounces, AO) and runs `Lightmapping.BakeAsync` with a timeout.
- **Cost:** bake 47 s (mixed lights) and 15 s (baked lights) on the RTX 5070 Ti with the GPU lightmapper (another Unity was running on project 02 at the time). Lightmap memory 4.0 MB (one 1024 and two 512 maps, compressed).
- **Look (screenshots viewed; mean luminance of the 1920x1080 frame, sampled):** overview 72.6 before, 62.1 mixed, 60.4 baked; aisle view 65.8 before, 49.2 mixed, 48.8 baked; start view 69.1 / 57.8 / 57.8. So the aisle got DARKER by 15 to 25 percent, not lighter. Reason: the baked environment light is occluded by the closed roof and racking (the realtime flat ambient the scene uses now reaches everything), and the point lights are a weak fill. I did not find it less flat: there is a little more shadow under the racking, and the floor is greyer. A stronger indirect scale (2.0 tried) did not fix it; the remaining option is to re-tune ambient and light intensities for the bake, which is the same work as tuning them for realtime.
- **Editor CPU counters (Play mode, median, start view, same PerfProbe; NOT a Vive frame rate), set-pass / draw calls:** before 589 / 1,284; lights Mixed 668 / 1,428 (worse: lightmapped objects batch less); lights fully Baked 347 / 1,203 (better, because the per-pixel light passes disappear; the moving forklift and people then get light only from the probes).
- **Recommendation:** do not roll lightmaps out for looks. A fully baked-lights variant is worth a separate, deliberate look only as a performance option for Quest-class builds; it needs the ambient and light intensities re-tuned and a headset check. The scenes are rebuilt from code, so any bake would have to be redone after every re-bake.
- **State left:** the Warehouse scene and its Baked folder were restored from a backup taken before the spike (verified identical to the pre-spike files); the pack FBX importers keep `generateSecondaryUV` on (harmless, local). Not verified: any headset view, Vive frame rate, lightmap seams at VR distance. Pictures: `docs/images/hub-r6-before-warehouse-*.png`, `hub-r6-after-mixed-warehouse-*.png`, `hub-r6-after-baked-warehouse-*.png` (overview, aisle, start view; editor desktop renders).
