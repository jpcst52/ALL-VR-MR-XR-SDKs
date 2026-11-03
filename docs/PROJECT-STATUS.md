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
| Vive project 04: KOA resistor selection bench | Based on KOA Speer's product range |
| README and screenshots for each Vive project | Screenshots via the windowed Unity capture runner after the editor is closed |
| Quest projects | Options: Unity Android builds of the lite projects; Quest web multiplayer "Hydrotest Builder"; MR Site Safety Layout. Asset-pack scenes are too heavy for a Quest without optimisation |
| Pitt-Bradford launcher UI | One home page with tiles per example and a recommended demo path; one-command start for the lab PC |
| Realism pass on web "hero" scenes | CC0 PBR textures/HDRIs, glTF models, quality switch, attribution file |
| New Practice Lab scenarios | Lighter lot sampling, heat-exchanger hydrostatic test, refinery pipe-spool welding, KOA resistor selection |
| Circulation Builder for Orin James | Draft built Nov 1 as Practice Lab scenario 17 (`biology-scenarios.js`, `biology-stations.js`, new "Life science (draft for faculty review)" group): place the four chambers, connect the pulmonary and systemic circuits in order, colour vessels oxygen-rich/poor. General biology only, no citations, labelled "Draft for faculty review" and "not clinical or medical guidance". 11/11 lab tests; desktop play-through only (no VR emulator or headset run). Needs his review of content and level |
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
| Orin James's project | A few lines on what he is building for his students |
| Zippo process | A line on how lighters are tested, or a page that loads |
| Hardware testing time | A slot on the real Vive Pro 2 and Quest before Nov 13 |
| Naming companies on slides | Comfort naming Zippo, Allegheny Bradford, and KOA Speer, or keep it "inspired by" |
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
| Zippo lighter test | Stays generic; no process details |
| Orin James's Circulation Builder | Build a draft from public general-biology knowledge, flagged for his review |
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
