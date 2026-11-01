# Project status and task list

Target: advisory-board demonstration at Pitt-Bradford on **November 13, 2026**. Update this file whenever a task changes state. Status values: **Done**, **In progress**, **Blocked**, **Not started**.

Last updated: November 1, 2026.

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
| Fieldwork XR Practice Lab: 12 hands-on scenarios | 3 business, 3 research, 6 industrial (height, control room, refinery battery limits, skyscraper window cleaning, welding, electrical isolation). Desktop-tested; VR only in an emulator |
| Multiplayer Circuit Lab (VR) and Site Safety Layout (AR) | Relay with theme-separated rooms; 8 automated tests; desktop-tested |
| VR emulator capture tool (`tools/vr-capture`) | Emulated Quest 3 via Meta IWER; not a physical headset |
| Offline vendoring (`vendor/`) | Three.js, Babylon.js, A-Frame, fonts; all 7 browser pages pass `offline-check.mjs` |
| Three PowerPoint decks rebuilt (`tools/decks`) | Need updating again for the Vive/Quest work |
| Overall README with screenshot gallery | Needs updating as new projects land |
| ARCore Gradle wrapper and build fix | APK builds; commit 2f5c37f |
| Research bibliography (`docs/research`) | 11 sources, each checked against a bibliographic record; BibTeX included |
| Branding collected | `branding/` (logo, guidelines PDF, shield EPS/PNG); kept out of git because the repo is public |

## In progress

| Task | State |
|---|---|
| Vive / SteamVR Unity projects (`vive-steamvr/`) | Shared kit package written (rig with desktop fallback, grabbing, text panels, screenshot runner, OpenXR setup). Project 1 (Hydrostatic Test Bay) code written but **never compiled or run**. First Unity run failed: batch mode reports "No valid Unity Editor license ... com.unity.editor.headless". Plan: run the editor with a window (signed-in Hub license) instead of `-batchmode` |

## Not started

| Task | Notes |
|---|---|
| Vive project 2: Refinery battery-limits rounds (live weather API) | Online API is acceptable here because the Vive PCs have internet |
| Vive project 3: Warehouse / forklift safety | Build from primitives so it works from a clean clone; Asset Store packs are local-only |
| Vive project 4: KOA resistor selection bench | Based on KOA Speer's product range (resistors, anti-sulfur, derating, ESD) |
| Quest projects | Options: Unity Android builds of the same projects; a Quest-specific web multiplayer "Hydrotest Builder"; MR Site Safety Layout. Needs a decision after the Vive projects compile |
| Pitt-Bradford launcher UI | One home page with tiles per example and a recommended demo path; one-command start for the lab PC |
| Realism pass on "hero" scenes | Refinery battery limits, electrical lockout, pipe-spool welding, hydrostatic test, control room: CC0 PBR textures/HDRIs, shadows, glTF models, quality switch, attribution file |
| New Practice Lab scenarios | Lighter lot sampling, heat-exchanger hydrostatic test, refinery pipe-spool welding, KOA resistor selection |
| Circulation Builder for Orin James's anatomy courses | Waiting on his description of what he is building |
| Multi-user training in the style of WaterWorks | Approach to decide; WaterWorks does not work on the Vive (owner-confirmed), so do not build on it |
| Self-signed HTTPS start script for the lab PC | `npm start` style, as in the owner's WaterWorks project |
| Advanced README and board-facing deck | After the projects exist |
| Demo-day kit | Start script, QR codes, rehearsal checklist, backup recordings |
| Update decks and README gallery for new projects | After each milestone |

## Blocked on the project owner

| Item | Needed |
|---|---|
| Vive WebXR check | Try `http://localhost:8000/training-simulations/` in Chrome/Edge with SteamVR running and report what happens (and set SteamVR as the OpenXR runtime). Tells us whether web examples can run on the Vive |
| Orin James's project | A few lines on what he is building for his students |
| Zippo process | A line on how lighters are tested, or a page that loads; the scenario stays generic until then |
| Hardware testing time | A slot on the real Vive Pro 2 and Quest before Nov 13 |
| Naming companies on slides | Confirm comfort naming Zippo, Allegheny Bradford, and KOA Speer, or keep it "inspired by" |
| Publishing | Decide whether to push commits; decide whether branding/logo may appear in the public repo |

## Known limitations and honest caveats

- Nothing has been run on a physical Vive or Quest. VR screenshots come from an emulated Quest 3 (Meta IWER) and are labelled as such.
- ARCore does not start a session on the Android emulator image used; build is verified, runtime is not.
- The Hydrostatic Test C# code has not been compiled.
- Scenario content is general safety-awareness practice, not a procedure; companies' real processes have not been verified.
- GitHub Models was retired on July 30, 2026 (per GitHub's docs); there is no Copilot-backed AI coach. The AI coach was cut from scope.

## Decisions log

| Decision | Reason |
|---|---|
| Hosting for Quests: HTTPS served from the lab PC with a self-signed certificate (as in the owner's WaterWorks project), not tunnels or domains | Simplest, works offline |
| Drop the AI coach and local-model setup | Owner wants simple training and has no time for local AI setup; GitHub Models retired |
| Vive gets native Unity/OpenXR projects (several, separate) | WaterWorks (WebXR) does not work on the Vive; multiple projects as fallbacks |
| Keep `branding/` and `assets-private/` out of git | Public repo; university branding and Asset Store licenses |
| Web demos use CC0 assets only; Asset Store assets only in local Unity builds | License terms |
