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
| A8 | Web professional-look pass (launcher, Practice Lab, multiplayer) | ✅ | brand-kit/: Pitt-Bradford + CIST bar (logo if local, wordmark otherwise), backed in-world panels and tags; ui-check 40/40 desktop + emulated phone; README shots re-shot (logo blocked); no headset or real phone |

## B. Vive / SteamVR (Unity)
| # | Task | Status | Notes |
|---|---|---|---|
| B1 | Shared Unity kit (rig, grabbing, text, screenshots) | ✅ | compiles; works in built-in and URP |
| B2 | Project 01: Hydrostatic Test Bay | ✅ | runs in editor, desktop mode; no headset yet |
| B3 | Hub project and asset packs | ✅ | 5 of 6 packs in hub; built-in pipeline |
| B4 | Courtyard project compile errors | ✅ | fixed (missing ugui package) |
| B5 | Choose and copy Courtyard pieces into the hub | ✅ | generator, fuel cans, work lights copied to hub (233 MB, git-ignored) and placed in the test bay |
| B6 | Hub lobby scene with portals to each scenario | ✅ | lobby + 4 portals; pad 2 verified loading a scenario in the editor (desktop); return-to-lobby keys and other portals not exercised; no headset; scenes git-ignored, see hub README |
| B7 | Realism pass on Hydrostatic Test Bay | 🔄 👤 | done in hub: ambient + room tone + pump hum, tone-mapping/bloom/vignette (key P toggles), work lights, broken StandingLight shader fixed. Still: hall is 39 x 37 m and dark/noisy; headset frame rate untested |
| B8 | Project 02: Refinery battery-limits rounds (live weather) | 🔄 | compiles and plays in the editor, 0 console errors; not played through; no headset |
| B9 | Project 03: Warehouse and forklift safety | 🔄 | compiles, plays in the editor, 0 console errors; two mirrored signs fixed (SAFETY CONES sign not checked); no headset |
| B10 | Project 04: KOA resistor selection bench | 🔄 | compiles and plays in the editor, 0 console errors; not played through; no headset |
| B11 | README and screenshots for each Vive project | ✅ | READMEs plus editor renders for 01 (6 shots) and 02-04 and the hub; desktop only, no headset |
| B13 | Professional-look pass (panels, tags, logo, no bare floating text) | 🔄 👤 | rounded panels, name tags, depth-correct text and the logo + CIST plate are in all four projects and every hub scene (project 01 got its own plate on Nov 2); launcher image and README screenshots re-shot after the bake. Still: your eyes-on review in the editor and headset (text size, glare) |
| B12 | Test on the real Vive Pro 2 | ⏳ 👤 | needs lab time before Nov 13 |

## C. Quest and multi-user
| # | Task | Status | Notes |
|---|---|---|---|
| C1 | Try the web Practice Lab on the Vive (Chrome/Edge + SteamVR) | 🔄 👤 | page looks good on desktop; confirm Enter VR works on the Vive |
| C2 | Quest projects | 🔄 | decision Nov 1: web only (Practice Lab in Quest Browser, web multiplayer); Unity Android builds are future work; needs a Quest test |
| C3 | Multi-user training in the style of WaterWorks | ✅ | decision Nov 1: web multiplayer is the demo; Unity multi-user is future work |
| C4 | Self-signed HTTPS start script for the lab PC | ✅ | scripts/start-demo.cmd, QR page; 18 relay tests pass; not run on hardware |
| C5 | Test on a real Quest | ⏳ 👤 | needs lab time |

## D. Launcher, realism, and partner content
| # | Task | Status | Notes |
|---|---|---|---|
| D1 | Pitt-Bradford launcher UI | ✅ | index.html + launcher/; launcher-check passes on :8101; not tried on the relay or a Quest |
| D2 | Web realism pass on hero scenes | ✅ | shared scene lighting, shadows, tone mapping, procedural textures; 5/5 tests; 3 of 16 screenshots re-shot; Quest frame rate untested |
| D3 | Circulation Builder for Orin James | ✅ ❓ | draft built (Practice Lab scenario 17, labelled "Draft for faculty review"); 11/11 lab tests pass; desktop play-through only, no VR emulator or headset run; needs his review |
| D4 | Zippo lighter-test details | ✅ | decision Nov 1: scenario stays generic, no Zippo process details |
| D5 | Permission to name the partner companies on slides | ✅ | decision Nov 1: no names; slides say "inspired by regional industry" |

## E. Documentation, decks, and research
| # | Task | Status | Notes |
|---|---|---|---|
| E1 | Research bibliography | ✅ | 36 entries + STUDY-DESIGNS.md; "to confirm" items (NASA-TLX, SUS, a few abstracts) need full-text reads before any paper |
| E2 | Project status docs, CLAUDE.md, memory notes | ✅ | |
| E3 | Status dashboard | ✅ | generalised; automatic port so several projects can run it |
| E4 | Overall README gallery | ✅ | refresh at each milestone |
| E5 | Update the three decks for Vive, Quest, and hub work | ✅ | decks rebuilt (Pitt palette, 56 slides), validated, 38 viewed; hardware claims labelled unverified |
| E8 | Research-driven features: "why this step" cues + rationale question (web, Hydrostatic Test Bay, Refinery Rounds) | ✅ | web: 16 of 17 scenarios, 18 lab tests and ui-checks pass; Unity 01 and 02: compiled, scripted run only; reasons need review by owner or a partner; not hand-played, no headset |
| E9 | Pedestrian hazard + awareness check in Warehouse and Forklift | ✅ | Unity 03: co-worker walks past the forklift's put-away + 3-question pause-and-ask check (inspector toggles); compiled, scripted self-test PASS (score 2/3 as expected), 13 desktop renders; not hand-played, no headset; hub copy not updated |
| E10 | Controller-only forklift slide and study instruments (presence, cognitive load) | ✅ | slide in the overview deck (rebuild pending), study instruments added to STUDY-DESIGNS.md; from verified abstracts only |
| E13 | Bake the Unity scenes so every GameObject is saved in the scene (not generated at Start) | ✅ | projects 01 (217), 02 (393), 03 (628), 04 (399) and the five hub scenes baked; baked vs unbaked renders differ by at most 2% of pixels (lighting noise) and scripted runs pass; desktop editor only; not hand-played, no headset |
| E12 | Menu upgrade: scene thumbnails on the cards; seasonal skies (4 seasons, every 30 s, pixelate + scan-line glitch transition) in web and Unity | ✅ | web: 17 thumbnails + sky, 20/16/18 tests and 48 UI checks pass; Unity lobby: open terrace, 4 thumbnails, sky; desktop and emulator only, no headset; skies are CC0 regional-style stand-ins, not Bradford photos |
| E11 | Instructions for running everything (docs/HOW-TO-RUN.md) | ✅ | one page covering demo, Unity, hub lobby, headsets, tests, dashboard; will add the new features when the agents finish |
| E7 | Survey of other universities' VR/AR research and how we can build on it | ✅ | docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md; 40 entries added to references.bib (76 total), each checked against Crossref plus an abstract source; no full texts read; spot-check by owner advised before citing; outreach is 👤 |
| E6 | Advanced README and board-facing deck | ⏳ | after the features land; decks need a rebuild |

## F. Demo day (November 13)
| # | Task | Status | Notes |
|---|---|---|---|
| F1 | Demo-day kit (start script, QR codes, checklist, backups) | 🔄 | docs/demo-day written, incl. HARDWARE-TEST-SESSION.md (pass/fail sheet for the lab); hardware steps need a rehearsal |
| F2 | Rehearsal | ⏳ 👤 | owner has lab time this week (answered Nov 1); checklist to prepare |
| F3 | Decide whether to push to GitHub | ✅ | pushed Nov 2, 2026 (to commit 3b81dcc, branch main); later work (baked scenes) will be committed and pushed when the owner asks |
| F4 | Main menu for everything in the repo | ✅ | launcher now lists the browser demos, multiplayer, hub + four Unity projects, six native SDK samples, Quest pages, and 31 guide/deck links; guides open offline in launcher/doc.html; launcher-check covers 20 tiles, 31 links and all 39 guides |
| F5 | Documentation index | ✅ | docs/README.md lists every guide by purpose; linked from the root README, HOW-TO-RUN and the menu |
| G1 | Real-looking people (Microsoft Rocketbox avatars, MIT) in the hub scenes | 🔄 | Done for Warehouse (8 people + Rocketbox walking co-worker; scripted self-test PASS in the editor, freezes with the pause) and Refinery (8 people incl. a pair that follows the muster point). Other scenes (Lobby, Hydro, Resistor) still to do. Editor desktop only; no headset |
| G2 | Dress the hub scenes with many more pack props (refinery, warehouse, resistor bench, lobby, hydro) | 🔄 | Warehouse and Refinery dressed (HubSceneDressing.cs, rebuildable; editor renders in docs/images/hub-dressed-*.png). Resistor bench, lobby, hydro still to do. About 1.4-1.5 M triangles in the editor: not optimised, no frame rate measured |
