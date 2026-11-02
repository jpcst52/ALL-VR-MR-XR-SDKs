# Task status: Pitt-Bradford XR Training (updated November 1, 2026, evening)

Feeds the live dashboard (`npm run status`, http://localhost:5210). Narrative status, decisions, and caveats are in [PROJECT-STATUS.md](PROJECT-STATUS.md). Demo date: **November 13, 2026**.

Legend: ✅ done · 🔄 in progress · ⏳ waiting on a prerequisite · 👤 needs the owner (Jeremy) · ❓ needs partners or faculty

## A. Web examples and tools
| # | Task | Status | Notes |
|---|---|---|---|
| A1 | Ten SDK/framework examples with READMEs | ✅ | browser ones have screenshots |
| A2 | Practice Lab: 12 hands-on scenarios | ✅ | desktop-tested; VR only emulated |
| A3 | Multiplayer Circuit Lab and Site Safety Layout | ✅ | 8 relay tests pass |
| A4 | VR emulator capture tool | ✅ | emulated Quest 3, not a real headset |
| A5 | Offline vendoring of web libraries | ✅ | 7 pages pass with network blocked |
| A6 | ARCore Gradle wrapper and build | ✅ | APK builds; session fails on the emulator image |
| A7 | New Practice Lab scenarios (lighter sampling, hydrostatic test, pipe-spool welding, resistor selection) | ⏳ | after partner details |

## B. Vive / SteamVR (Unity)
| # | Task | Status | Notes |
|---|---|---|---|
| B1 | Shared Unity kit (rig, grabbing, text, screenshots) | ✅ | compiles; works in built-in and URP |
| B2 | Project 01: Hydrostatic Test Bay | ✅ | runs in editor, desktop mode; no headset yet |
| B3 | Hub project and asset packs | ✅ | 5 of 6 packs in hub; built-in pipeline |
| B4 | Courtyard project compile errors | ✅ | fixed (missing ugui package) |
| B5 | Choose and copy Courtyard pieces into the hub | 🔄 | scanning its prefabs |
| B6 | Hub lobby scene with portals to each scenario | ⏳ | after B5 |
| B7 | Realism pass on Hydrostatic Test Bay | ⏳ | pack materials, furniture, lighting, audio |
| B8 | Project 02: Refinery battery-limits rounds (live weather) | ⏳ | |
| B9 | Project 03: Warehouse and forklift safety | ⏳ | |
| B10 | Project 04: KOA resistor selection bench | ⏳ | |
| B11 | README and screenshots for each Vive project | ⏳ | after each project |
| B12 | Test on the real Vive Pro 2 | ⏳ 👤 | needs lab time before Nov 13 |

## C. Quest and multi-user
| # | Task | Status | Notes |
|---|---|---|---|
| C1 | Try the web Practice Lab on the Vive (Chrome/Edge + SteamVR) | 🔄 👤 | page looks good on desktop; confirm Enter VR works on the Vive |
| C2 | Quest projects (Unity Android builds, web multiplayer, MR layout) | ⏳ | choose after Vive projects compile |
| C3 | Multi-user training in the style of WaterWorks | ⏳ | WaterWorks does not work on the Vive |
| C4 | Self-signed HTTPS start script for the lab PC | ⏳ | |
| C5 | Test on a real Quest | ⏳ 👤 | needs lab time |

## D. Launcher, realism, and partner content
| # | Task | Status | Notes |
|---|---|---|---|
| D1 | Pitt-Bradford launcher UI | ⏳ | tiles per example, demo path |
| D2 | Web realism pass on hero scenes | ⏳ | CC0 textures and models |
| D3 | Circulation Builder for Orin James | ❓ | needs his project description |
| D4 | Zippo lighter-test details | ❓ | scenario stays generic until then |
| D5 | Permission to name the partner companies on slides | ❓ | else "inspired by" |

## E. Documentation, decks, and research
| # | Task | Status | Notes |
|---|---|---|---|
| E1 | Research bibliography | ✅ | 11 verified sources |
| E2 | Project status docs, CLAUDE.md, memory notes | ✅ | |
| E3 | Status dashboard | ✅ | copied from the EVE project, generalised |
| E4 | Overall README gallery | ✅ | refresh at each milestone |
| E5 | Update the three decks for Vive, Quest, and hub work | ⏳ | |
| E6 | Advanced README and board-facing deck | ⏳ | |

## F. Demo day (November 13)
| # | Task | Status | Notes |
|---|---|---|---|
| F1 | Demo-day kit (start script, QR codes, checklist, backups) | ⏳ | |
| F2 | Rehearsal | ⏳ 👤 | |
| F3 | Decide whether to push to GitHub | 👤 | three local commits not pushed |
