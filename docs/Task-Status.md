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
| A9 | Practice Lab round 2: rooms and props for the plain scenarios, texture-leak fix, keyboard and reduced-motion fixes, panel/camera layout fix | 🔄 | agent r2Lab; desktop Chrome (software GL) only, nothing on a headset |

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
| B10 | Project 04: resistor selection bench | 🔄 | compiles and plays in the editor, 0 console errors; not played through; no headset |
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
| D2 | Web realism pass on hero scenes | ✅ | shared scene lighting, shadows, tone mapping, procedural textures; 5/5 tests; 3 of 16 screenshots re-shot; Quest frame rate untested. Nov 3: CC0 glTF props (Poly Haven) + animated people (Quaternius) added offline with primitive fallback, credits in assets/models/CREDITS.md, 27 lab tests; desktop only |
| D3 | Circulation Builder for biology faculty | ✅ ❓ | draft built (Practice Lab scenario 17, labelled "Draft for faculty review"); 11/11 lab tests pass; desktop play-through only, no VR emulator or headset run; needs faculty review |
| D4 | Lighter-test details (kept generic) | ✅ | decision Nov 1: scenario stays generic, no process details |
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
| F6 | Docs check, glossary, menu search | ✅ | `npm run docs-check`; docs/GLOSSARY.md; launcher search box, keyboard navigation and status legend |
| G1 | Real-looking people (Microsoft Rocketbox avatars, MIT) in the hub scenes | ✅ | All five hub scenes have animated Rocketbox people: Warehouse 8 (+ the walking co-worker, scripted self-test PASS), Refinery 8, Hydro 6, Lobby 6, Resistor 4. People play their activity in Play mode (no T-pose; animator times advance). Unity editor desktop only: no headset, no hand play |
| G2 | Dress the hub scenes with many more pack props (refinery, warehouse, resistor bench, lobby, hydro) | ✅ | All five scenes dressed from code (HubSceneDressing.cs) and re-baked; editor renders in docs/images/hub-dressed-*.png. Edit-mode triangle counts (baked scene, before static batching): Lobby 125 k, Hydro 149 k, Resistor 77 k, Warehouse 125 k, Refinery 189 k (the earlier 1.4-1.5 M figures were most likely read in Play mode, where static batching merges meshes, and should be disregarded). No frame rate measured on a Vive Pro 2; nothing run on a headset |
| G3 | Hub round 2: critical review, richer lobby, perf pass on the five hub scenes | ✅ 👤 | Lobby rebuilt (tiles, gallery walls, reception, lounge, CIST corner, 10 people), fixes in Warehouse/Refinery/Hydro/Resistor, portal blurbs corrected; SetPass (editor Play mode, median) 352 / 515 / 551 / 1301 / 787 for Lobby / Hydro / Refinery / Warehouse / Resistor, all under 2,000. Editor desktop only: no headset, no frame rate. Owner: look at it in the editor |
| G4 | Owner decisions applied (Nov 3): muster direction, names scrubbed, credits, why panels in 03 and 04, scenario wording | ✅ 👤 | Muster aligned to **upwind** (see CONTENT-REVIEW A and E); names removed from status docs and CLAUDE.md untracked; CREDITS.md added; WHY THIS MATTERS panels in projects 03 and 04 (kit unchanged); project 03 manifest gained the animation module (fixes a compile error); 04 compiles headless, 03 needs a re-run; not hand-played |
| H1 | Detail and realism pass over every project: more objects and refined UI, textures and models, lighting and effects, better sound | 🔄 👤 | Unity 01–04, the kit and the hub done Nov 3 (compiled, baked, desktop shots; 7 CC0 sounds, rest synthesized; **sound unheard**). **Render cost roughly tripled (hub warehouse 4,295 draw calls, editor counters): optimisation pass needed before the Vive**; then Practice Lab and web demos |
| H2 | HoloLens: content looks small | 🔄 👤 | docs/HOLOLENS-AR-NOTES.md (causes ranked, measuring snippet) and tested FOV and fit maths (`multi-player/shared/ar-fit.mjs`); not tested on a HoloLens; needs the measurement from the headset |
| I1 | Unity VR fixes: text in one eye only, teleport, hands visible in every scene | 🔄 👤 | Text: PBText shader lacked stereo-instancing macros (Single Pass Instanced) and is fixed; new stereo-aware PBColor shader replaces Hidden/Internal-Colored in the warehouse awareness shell. Teleport (thumbstick forward, release) and 45 degree snap turn added to the kit rig (all four projects and the hub); desktop T key. Hands: model rebuilt, left hand shown on desktop, `-pbHandCheck` self-test. `-pbHandCheck` PASSED in 02, 03, 04 and all five hub scenes (editor desktop fallback; screenshots made); compiled with no errors; **nothing tried on a headset** |
| I2 | Web labs in VR/AR: toolbox (parts, examples, reset), Y/B open-close, X/A rotate, 3 more examples per lab, glove hands | ✅ 👤 | Circuit Lab and Safety Lab (and Hydro rig): `toolbox-check.mjs` 41 checks pass in an EMULATED Quest 3; unit tests for the examples; fixed a crash when aiming a controller at a labelled part. Not tried on a real Quest; Y=open, B=close is my reading of the request |
| J1 | Sound effects pass 2: better, less crude sounds across the Unity scenes | ✅ 👤 | Nov 5: all 51 old sounds re-synthesized (impacts = transient + modal body + thump; pink noise; softer alarms and beeps; seamless loops), 11 new (5 scene beds, 6 distant events), per-scene reverb zone and ambience start by themselves (`SceneAudio`), loudness targets with a limiter, `SfxReport` measures every clip. Compiled and run headless in project 04 only (report, self-test PASS, `SceneAudioCheck` PASS). **Unheard: owner must listen** on speakers and in a headset; the numbers show levels, harshness and loop seams, not taste. No new recordings (none could be verified) |
| J2 | Visual quality pass 2 on the five Vive scenes (lobby, hydro, refinery, warehouse, resistor), within the render budget | 🔄 👤 | Nov 5: kit adds normal maps for every textured material, 512 px grain, daylight windows, contact shadows, 4x MSAA + forced anisotropic; warehouse lighter, ambient up. Hub scenes rebuilt, re-baked, re-shot; projects 01, 03, 04 re-baked and re-shot; hand check passes (hub x5, 01, 03, 04). Editor set-pass/draw counters within +-6 / +-56 of before. Project 02 re-baked and re-shot Nov 6 (setPass 358, 1,003 draw calls, editor counters). Owner must look in the headset (MSAA, anisotropy and texture sharpness are GPU effects the counters do not show). Details: docs/PROJECT-STATUS.md |
| J3 | Baked lightmap spike on hub Warehouse | ✅ | Nov 6: feasible (bake 15-47 s, 4 MB) but the aisle got 15-25% darker, not lighter; not rolled out; scene restored. Fully baked lights cut editor set-pass 589 to 347 (perf option only). Details: docs/PROJECT-STATUS.md |
| J4 | Unity safe-mode prompts fixed; compile check added; focus Unity on SteamVR + Vive, Quest on the web labs | ✅ | Cause: an agent's XrSimCheck.cs (compile errors) in the shared kit; deleted. New `scripts/unity-compile-check.ps1` (windowless, about 10 s, tested with a planted error). OpenXR runtime now SteamVR. Vive controller profile is enabled in 01 and the hub; trackpad bindings read correctly on paper only. Not tested on a Vive |
| J5 | Unity visual overhaul (round 7): visible jump on the five Vive scenes and projects 01-04, no new packages | ✅ 👤 | Nov 6: kit `SceneLook` (runtime by scene name, nothing baked): real interior key light with soft shadows (was off), ceiling stops casting, glow halos, stronger light shafts, daylight floor patches, wall-foot gradients, floor patina and sheen, gentle fog, warm sun. Softened after owner said too harsh. Editor set-pass/draw: Warehouse hub 635/1351 to 1052/2415, others +0 to +300; hand check passes x9. Desktop renders only, owner must look in the headset. Details: docs/PROJECT-STATUS.md |
| J7 | Sound effects pass 3 ("better sound effects"): more natural, gentle, consistent, with a listenable review | 🔄 👤 | Nov 6: convolution reverb, bubble-model water, FM-bell UI, rebuilt beds and loops, CC0 concrete and gravel footsteps, one-shots peak -6 dBFS and loops at LUFS-equivalent levels, custom spatial rolloff, background generation. Compiled; `SfxReport` (208 clips, 0 flags) and `SfxSelfTest` (384 clips) PASS headless; **unheard: owner must open docs/audio-review/index.html and listen, then name what is wrong**. Not run in play mode (SceneAudioCheck could not be re-run: a hung Unity process on project 04, PID 56128, needs closing by the owner); no Vive |
| J8 | Realistic people in the stand-alone Vive projects | ✅ | Only project 03 has people: co-worker and forklift driver are now Rocketbox avatars (shrunk, 18 MB, committed); projects 01, 02, 04 have no figures. Compiles, 21 desktop renders retaken; driver pose is bent by code (no sitting clip), tuned by eye; not hand-played, no headset |
| J9 | Windows player builds of projects 01 to 04 for Vive testing; lab test guide and start script | ✅ | `scripts\build-vive.cmd` built all four (builds\vive, git-ignored); fixed an editor-only API in SceneLook.cs that broke player builds; 03 started once windowless, 25 s, no crash; `scripts\lab-test.cmd` starts the server; guide docs/LAB-TEST-TODAY.md. No headset yet |
| J10 | Hub Windows player build (lobby plus 4 scenarios) | ✅ | `HubBuild.cs` added; `build-vive.cmd -IncludeHub` built `builds\vive\hub` (1 GB, lab PC only). Built only: not started, lobby navigation unverified |
| J11 | 3 MB picture limit: launcher screenshots shrunk, `scripts/check-image-sizes.mjs`, Pages workflow step | ✅ | launcher.png 3.3 to 0.8 MB, launcher-narrow.png 3.0 to 0.8 MB; the Pages deploy failed because the root README picture exceeded the 3 MB cap. Not pushed yet |
| J12 | Developer setup guide incl. Meta XR Simulator | ✅ | docs/DEVELOPER-SETUP.md; simulator install documented from the lab PC, no project verified in it |
| J13 | Hub: lobby music (original synth loop) and season-sky shader kept in the player | ✅ | `HubMusic.cs` (124 BPM, 31 s loop, M mutes on desktop) and `HubKeepShaders` in `HubBuild.cs`; hub player runs, loads the lobby, music generates, no exceptions (windowed, no headset). Not heard, not tried in a headset |
| J14 | Review of branch Update-OpenXR-Settings-VR-Hand-Models | ✅ | Web toolbox/hands superseded by main (41 checks pass); Unity settings are editor-generated. Ported the AR grip-drop (solo alignment, emulator-tested); branch deleted; old files removed (gallery-latest.html, proposed-BuildScript) |
| J15 | One-click build of all five Unity players for the VR Stage PC | ✅ | `scripts\build-all-vive.cmd`; hub reported SKIPPED with a list when its git-ignored scenes/art are missing; combined zip excludes the hub. Dry run and the missing-asset check tested; the full 45-minute run not repeated |
| J16 | Guide and scripts to build the hub on another PC | ✅ | docs/BUILD-THE-HUB.md (what is missing from git, pack sources verified against the Asset Store cache, two routes); `scripts\pack-hub-assets.cmd` / `unpack-hub-assets.cmd` (tested on a fake hub; dry run on the real one: 4,971 files, 2,993 MB). Not tried on a second PC |
