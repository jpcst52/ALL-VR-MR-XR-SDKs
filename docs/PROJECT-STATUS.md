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
| Hub scenes: Courtyard lobby with portals to each scenario | Local-only scenes (gitignored) that use the asset packs; committed "lite" projects stay primitive-only |
| Realism pass on Hydrostatic Test Bay using the packs | Real wall/floor/steel materials, office furniture for a supervisor corner, lighting, post-processing, audio; legible task board; smaller floor text |
| Vive project 02: Refinery battery-limits rounds (live weather API) | Online API is acceptable on the Vive PCs |
| Vive project 03: Warehouse / forklift safety | Uses the warehouse, construction, and container packs in the hub; lite version from primitives |
| Vive project 04: KOA resistor selection bench | Based on KOA Speer's product range |
| README and screenshots for each Vive project | Screenshots via the windowed Unity capture runner after the editor is closed |
| Quest projects | Options: Unity Android builds of the lite projects; Quest web multiplayer "Hydrotest Builder"; MR Site Safety Layout. Asset-pack scenes are too heavy for a Quest without optimisation |
| Pitt-Bradford launcher UI | One home page with tiles per example and a recommended demo path; one-command start for the lab PC |
| Realism pass on web "hero" scenes | CC0 PBR textures/HDRIs, glTF models, quality switch, attribution file |
| New Practice Lab scenarios | Lighter lot sampling, heat-exchanger hydrostatic test, refinery pipe-spool welding, KOA resistor selection |
| Circulation Builder for Orin James | Waiting on his description |
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
