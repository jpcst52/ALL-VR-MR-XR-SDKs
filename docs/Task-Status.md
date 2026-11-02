# Task status: Pitt-Bradford XR Training (updated November 1, 2026, evening)

Feeds the live dashboard (`npm run status`, then open the address it prints; the port is chosen automatically). Narrative status, decisions, and caveats are in [PROJECT-STATUS.md](PROJECT-STATUS.md). Demo date: **November 13, 2026**.

Legend: ✅ done · 🔄 in progress · ⏳ waiting on a prerequisite · 👤 needs the owner (Jeremy) · ❓ needs partners or faculty

## A. Web examples and tools
| # | Task | Status | Notes |
|---|---|---|---|
| A1 | Ten SDK/framework examples with READMEs | ✅ | browser ones have screenshots |
| A2 | Practice Lab: 12 hands-on scenarios | ✅ | desktop-tested; VR only emulated |
| A3 | Multiplayer Circuit Lab and Site Safety Layout | ✅ | relay tests pass |
| A4 | VR emulator capture tool | ✅ | emulated Quest 3, not a real headset |
| A5 | Offline vendoring of web libraries | ✅ | 7 pages pass with network blocked |
| A6 | ARCore Gradle wrapper and build | ✅ | APK builds; session fails on the emulator image |
| A7 | New Practice Lab scenarios (lighter sampling, hydrostatic test, pipe-spool welding, resistor selection) | ✅ | 16 scenarios, 5/5 tests pass; desktop play-through only, VR captures not refreshed |

## B. Vive / SteamVR (Unity)
| # | Task | Status | Notes |
|---|---|---|---|
| B1 | Shared Unity kit (rig, grabbing, text, screenshots) | ✅ | compiles; works in built-in and URP |
| B2 | Project 01: Hydrostatic Test Bay | ✅ | runs in editor, desktop mode; no headset yet |
| B3 | Hub project and asset packs | ✅ | 5 of 6 packs in hub; built-in pipeline |
| B4 | Courtyard project compile errors | ✅ | fixed (missing ugui package) |
| B5 | Choose and copy Courtyard pieces into the hub | ✅ | generator, fuel cans, work lights copied to hub (233 MB, git-ignored) and placed in the test bay |
| B6 | Hub lobby scene with portals to each scenario | ⏳ | after B5 |
| B7 | Realism pass on Hydrostatic Test Bay | 🔄 👤 | fixed: wall strip was inside the play area; four walls now ring the floor and equipment renders (docs/images/hub-play.png). Still to do: lighting, audio, post-processing |
| B8 | Project 02: Refinery battery-limits rounds (live weather) | 🔄 | compiles and plays in the editor, 0 console errors; not played through; no headset |
| B9 | Project 03: Warehouse and forklift safety | 🔄 | compiles, plays in the editor, 0 console errors; two mirrored signs fixed (SAFETY CONES sign not checked); no headset |
| B10 | Project 04: KOA resistor selection bench | 🔄 | compiles and plays in the editor, 0 console errors; not played through; no headset |
| B11 | README and screenshots for each Vive project | 🔄 | READMEs and a desktop editor screenshot for 02-04 and the hub; 01 has no screenshot yet |
| B12 | Test on the real Vive Pro 2 | ⏳ 👤 | needs lab time before Nov 13 |

## C. Quest and multi-user
| # | Task | Status | Notes |
|---|---|---|---|
| C1 | Try the web Practice Lab on the Vive (Chrome/Edge + SteamVR) | 🔄 👤 | page looks good on desktop; confirm Enter VR works on the Vive |
| C2 | Quest projects (Unity Android builds, web multiplayer, MR layout) | ⏳ | choose after Vive projects compile |
| C3 | Multi-user training in the style of WaterWorks | 🔄 | web hydrotest multiplayer theme done (6 tests, desktop only); Unity multi-user not started |
| C4 | Self-signed HTTPS start script for the lab PC | ✅ | scripts/start-demo.cmd, QR page; 18 relay tests pass; not run on hardware |
| C5 | Test on a real Quest | ⏳ 👤 | needs lab time |

## D. Launcher, realism, and partner content
| # | Task | Status | Notes |
|---|---|---|---|
| D1 | Pitt-Bradford launcher UI | ✅ | index.html + launcher/; launcher-check passes on :8101; not tried on the relay or a Quest |
| D2 | Web realism pass on hero scenes | ✅ | shared scene lighting, shadows, tone mapping, procedural textures; 5/5 tests; 3 of 16 screenshots re-shot; Quest frame rate untested |
| D3 | Circulation Builder for Orin James | ❓ | needs his project description |
| D4 | Zippo lighter-test details | ❓ | scenario stays generic until then |
| D5 | Permission to name the partner companies on slides | ❓ | else "inspired by" |

## E. Documentation, decks, and research
| # | Task | Status | Notes |
|---|---|---|---|
| E1 | Research bibliography | ✅ | 36 entries + STUDY-DESIGNS.md; "to confirm" items (NASA-TLX, SUS, a few abstracts) need full-text reads before any paper |
| E2 | Project status docs, CLAUDE.md, memory notes | ✅ | |
| E3 | Status dashboard | ✅ | generalised; automatic port so several projects can run it |
| E4 | Overall README gallery | ✅ | refresh at each milestone |
| E5 | Update the three decks for Vive, Quest, and hub work | ✅ | decks rebuilt (Pitt palette, 56 slides), validated, 38 viewed; hardware claims labelled unverified |
| E6 | Advanced README and board-facing deck | ⏳ | |

## F. Demo day (November 13)
| # | Task | Status | Notes |
|---|---|---|---|
| F1 | Demo-day kit (start script, QR codes, checklist, backups) | 🔄 | docs/demo-day written; hardware steps need a rehearsal |
| F2 | Rehearsal | ⏳ 👤 | |
| F3 | Decide whether to push to GitHub | 👤 | local commits not pushed |
