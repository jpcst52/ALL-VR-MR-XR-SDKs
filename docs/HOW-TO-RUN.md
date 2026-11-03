# How to run everything

One page for running every part of this repository. Pick the row that matches what you want to do, then follow that section. Status as of November 1, 2026: **all of this has been run on a Windows development PC, in the Unity editor, in desktop browsers and in an emulated Quest 3. Nothing has been run on a real Vive Pro 2 or Quest yet**; use [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md) for that first run.

## Quick start for demo day

Three steps on the demo PC (Windows, Node.js 22 or newer installed, `npm install` already done once while online):

1. Double-click `scripts\start-demo.cmd`. It starts one HTTPS server on port 8443 and opens the main menu at `https://localhost:8443/`. (Add `/dashboard` to also open the status dashboard.)
2. On a Quest or phone on the same Wi-Fi, open one of the `https://<PC address>:8443/` addresses the window prints and accept the certificate warning once. For the Vive, start SteamVR, then open the same page in desktop Chrome or Edge.
3. Everyone who shares an activity types the same room name. Stop with Ctrl+C in the server window.

If the web demo does not start, open [demo-day/BACKUP-PLAN.md](demo-day/BACKUP-PLAN.md). Verified: the script and server were run on the development PC and in an emulated Quest 3; the headset steps have not been tried on real hardware.

| I want to... | Go to |
|---|---|
| Find any guide, status page or deck | The **main menu** (the launcher that opens with the demo) or [README.md](README.md), the documentation index |
| Run the demo for people (launcher, Practice Lab, multiplayer) | [1. Run the demo](#1-run-the-demo-web-launcher-practice-lab-multiplayer) |
| Try the Unity training projects at a desk or on the Vive | [2. Unity projects](#2-unity-projects-01-to-04) |
| Walk through the lobby with portals | [3. The hub lobby](#3-the-hub-lobby-local-only) |
| Use a headset (Vive Pro 2 or Quest) | [4. Headsets](#4-headsets) |
| Check that nothing broke | [5. Tests](#5-tests-and-checks) and the full [TESTING.md](TESTING.md) checklist |
| Make screenshots, rebuild decks, see project status | [6. Screenshots, decks, dashboard](#6-screenshots-decks-and-the-status-dashboard) |
| Something is not working | [7. Troubleshooting](#7-troubleshooting) |

General safety-awareness practice only: none of the scenarios is a procedure, permit or certification.

## 0. What you need

| Part | Needs |
|---|---|
| Web demo | Windows PC with Node.js 22 (or newer), Chrome or Edge. Internet is needed **once** to run `npm install` in `multi-player/`; after that everything works offline. |
| Unity projects | Unity Hub with editor **6000.3.20f1**. Projects open from `vive-steamvr/`. |
| Hub lobby | The same editor plus the Asset Store packs you own (they are not in the repository). |
| Vive Pro 2 | SteamVR running and set as the OpenXR runtime ([demo-day/VIVE-SETUP.md](demo-day/VIVE-SETUP.md)). |
| Quest | Quest Browser on the same Wi-Fi as the PC ([demo-day/QUEST-SETUP.md](demo-day/QUEST-SETUP.md)). |

The libraries the web pages use are bundled in `vendor/` and `brand-kit/`, so the web side needs no internet at demo time.

## 1. Run the demo (web: launcher, Practice Lab, multiplayer)

**Normal way (one command).** Double-click `scripts\start-demo.cmd` (or run `powershell -File scripts\start-demo.ps1`). It checks Node, starts one HTTPS server on port 8443 that serves the launcher, the Practice Lab, Circuit Lab, Site Safety Layout and the multiplayer relay, creates a self-signed certificate the first time, prints every address for every network adapter and opens the launcher on the PC. Add `/dashboard` to also open the status dashboard.

- On the PC: the launcher opens by itself. Choose a tile.
- On a Quest or phone: open `https://<PC address>:8443/` (or scan the QR page at `https://localhost:8443/multi-player/qr.html`) and accept the certificate warning once.
- Multi-user activities: everyone types the **same room name**. Circuit Lab and Site Safety Layout rooms are separate.
- Stop with Ctrl+C in the window.

**Only the single-user pages, no relay.** From the repository root: `python -m http.server 8000`, then open `http://localhost:8000/` (launcher) or `http://localhost:8000/training-simulations/`. Multiplayer pages need the relay (previous option).

**Practice Lab controls.** Pick a scenario card, read the briefing, then do the tasks by selecting hotspots in the 3D work area (mouse click on desktop; controller ray and trigger in VR). Wrong actions show a consequence; the result screen lists mistakes. Scenario 17, the Circulation Builder, is a **draft for faculty review** (general biology, not clinical guidance).

Files: [../scripts/start-demo.ps1](../scripts/start-demo.ps1), [demo-day/README.md](demo-day/README.md), [../training-simulations/README.md](../training-simulations/README.md), [../multi-player/README.md](../multi-player/README.md).

## 2. Unity projects (01 to 04)

All four are in `vive-steamvr/` and share the package `vive-steamvr/packages/com.pittbradford.xrtraining`.

| Project | Folder | What it is |
|---|---|---|
| 01 | `01-hydrostatic-test` | Hydrostatic Test Bay: blind flanges, vent and fill, 150 psi, hold from behind the line, drain |
| 02 | `02-refinery-rounds` | Battery-limits rounds with live wind (needs internet for weather; otherwise simulated) |
| 03 | `03-warehouse-forklift` | PPE, mirror, striped crossing, forklift near-miss |
| 04 | `04-resistor-bench` | ESD strap, color code, Ohm's law, resistor choice |

**First time on a fresh clone** (each project):
1. In Unity Hub choose Add, select the project folder, open it with 6000.3.20f1 (the first import takes several minutes).
2. Menu **Pitt-Bradford > Set up project**. This configures OpenXR and the Input System and creates the scene under `Assets/Scenes/`. Restart the editor if it asks.
3. Open the scene and press **Play**.

**Controls**

| Action | Vive or Quest controllers | Desktop (no headset) |
|---|---|---|
| Move | Left stick or trackpad | W A S D |
| Look and turn | Head; right stick snap-turns 30 degrees | Hold right mouse and move |
| Press a button or valve | Point and pull the trigger | Crosshair and left click |
| Grab | Grip near the object | Look at it and press E (hold) |

If no headset is active the project runs in desktop mode automatically.

**Windows build.** **Pitt-Bradford > Build Windows player** writes to `Builds/<name>/` inside the project (git-ignored).

**Screenshots from the editor** (how the README images were made): close the project, then run `Unity.exe -projectPath <project> -executeMethod PittBradford.XR.EditorTools.CaptureRunner.Run -pbCapture <folder> -logFile <log>` (do not add `-batchmode`; it needs graphics). The scenario's `Shots` coroutine photographs and the editor exits.

Each project's own README has its learning goals, scene notes and known limits.

## 3. The hub lobby (local only)

The hub project `vive-steamvr/hub` dresses the scenarios with Asset Store art and adds a lobby with four portals. Third-party art cannot be redistributed, so **a fresh clone has no hub scenes**: you build them locally.

1. Open `vive-steamvr/hub` in Unity 6000.3.20f1 with the packs imported (the list is in [../vive-steamvr/hub/README.md](../vive-steamvr/hub/README.md)). Without a pack, its pieces are skipped and the scenes still build.
2. Menu **Pitt-Bradford > Set up hub project** (once).
3. Menu **Pitt-Bradford > Build hub: lobby and all scenarios**. This saves five scenes under `Assets/Scenes/Local/` and puts `Lobby` first in the build settings. Then open each scene in turn and run **Pitt-Bradford > Bake scene (play mode)** so its GameObjects are saved in the scene (about 15 seconds each); rebuilding removes the bakes. Projects 01 to 04 are already baked and committed.
4. Open `Lobby` and press Play.

**In the lobby:** step onto a portal's pad and wait about 1.4 seconds, or press 1 to 4 on the keyboard. **In a scenario:** hold both grips for 1.5 seconds or press Backspace to return. In the Hydrostatic Test Bay scene, **P** toggles post-processing.

**Logo.** The owner approved publishing the university logo on November 2, 2026. The web pages use `branding/logo.png` and the Unity plates use `vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/Resources/Branding/pitt-bradford.png`; both are tracked. The rest of `branding/` (brand guidelines PDF and other variants) is git-ignored. Delete the logo file to get the text wordmark.

**Seasonal skies and thumbnails.** The lobby is an open roof terrace under a 360 photo sky that changes season every 30 seconds with a short pixelate and scan-line glitch; each portal shows a thumbnail of its scenario; a chip on the welcome board names the season. Refinery Rounds uses the photo of today's season without rotating. The four photos are CC0 stand-ins from Poly Haven, **not photos of Bradford** (credits: `assets/skies/CREDITS.md`). Screenshots and details: [../vive-steamvr/hub/README.md](../vive-steamvr/hub/README.md).

**Replace the skies with your own 360 photos.** Take one equirectangular panorama per season (2:1, for example with a 360 camera or a phone panorama app that exports equirectangular). Put the files in `assets/skies/` as `spring-2k.jpg`, `summer-2k.jpg`, `autumn-2k.jpg`, `winter-2k.jpg`, edit `assets/skies/skies.json` (set `credit`, and `"isBradford": true` only for pictures really taken in Bradford), and run `python vive-steamvr/hub/tools/prepare_lobby_images.py skies` (Python 3 with Pillow). It writes 2048x1024 JPGs named by season plus the manifest into the kit at `vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/Resources/Skies/`, which the hub and the other Unity projects share. Do not put a second `Resources/Skies/` folder in a project: Unity would then pick one of the two same-named files unpredictably.

Then return to Unity (it re-imports the files) and press Play; no scene rebuild is needed. Keep the horizon level and the files under about 1 MB each. A missing season is skipped; with no pictures at all the lobby falls back to its flat blue background. If the pictures are of Bradford, the chip says "Sky: Bradford, PA"; otherwise it says "Sky: stand-in photo".

**Copies of scenario scripts.** The hub keeps copies of the scripts from projects 02 to 04 in `Assets/Scripts/Scenarios/`. After changing those projects, copy the scripts across again and rebuild the scenes.

## 4. Headsets

**Vive Pro 2 with SteamVR.** Start SteamVR first and make sure it is the active OpenXR runtime, then open the Unity project and press Play with the headset on. Full steps: [demo-day/VIVE-SETUP.md](demo-day/VIVE-SETUP.md). The web pages also work in desktop Chrome or Edge through SteamVR (use the launcher's Enter VR button); this is a rehearsal check, not yet confirmed.

**Meta Quest.** Quest Browser opens the demo server address from section 1. Unity Android builds for Quest are future work. Steps: [demo-day/QUEST-SETUP.md](demo-day/QUEST-SETUP.md).

**Without a headset.** Everything has a desktop mode. The VR screenshots in this repository come from an emulated Quest 3 and are labelled that way.

**First real-headset session.** Follow [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md): a pass or fail sheet with fallbacks.

## 5. Tests and checks

Run from the repository root unless noted. Each prints pass and fail counts; the status dashboard shows the recorded results.

| What | Command | Expected today |
|---|---|---|
| Practice Lab scenario data | `cd training-simulations`, then `node --test test/*.test.mjs` | 36 pass |
| Multiplayer relay | `cd multi-player`, then `npm test` | 40 pass |
| Brand kit | `cd brand-kit`, then `npm test` | 16 pass |
| Offline check (no network) | `node tools/vr-capture/offline-check.mjs` | all pages ok |
| Launcher check | `node tools/vr-capture/launcher-check.mjs` | pass |
| Web UI layout check (desktop and phone width) | `node tools/vr-capture/ui-check.mjs` | 48 pass |
| Unity compile | open the project; the Console must show no red errors | no errors |

Some checks start their own server on a free port; stop a leftover server by its process ID if a port is busy (see section 7). To record a result for the dashboard: `node tools/status/record-results.cjs "<suite>" <passed> <failed> <skipped>`.

## 6. Screenshots, decks and the status dashboard

**Status dashboard.** `npm run status` from the repository root, then open the address it prints. It reads [Task-Status.md](Task-Status.md). Guide: [status-dashboard-guide.md](status-dashboard-guide.md).

**Browser screenshots.** `node tools/vr-capture/readme-shots.mjs` (desktop) and `node tools/vr-capture/capture.mjs` (emulated Quest 3). They block the `branding/` folder, so committed images show only the text wordmark.

**Decks.** `cd tools/decks`, `npm install`, set `PPTX_SKILL_DIR` to the folder that contains `scripts/apply_theme.js`, then `node build.mjs` (all decks) or `node build.mjs overview`. Details: [../tools/decks/README.md](../tools/decks/README.md).

## 7. Troubleshooting

| Symptom | Try |
|---|---|
| Port 8000, 8443 or 8101 is busy | A leftover server is running. Find it with `netstat -ano` and stop that process ID; do not reuse the port. |
| Quest says the connection is not private | Expected with the self-signed certificate: Advanced, then Proceed. Once per headset. |
| Unity opens in Safe Mode | A script has a compile error. Read the red Console lines, fix them, restart the editor. |
| Magenta (pink) objects in the hub | A pack material uses a shader that does not compile; rebuild with **Build hub: lobby and all scenarios**, which switches those to Standard. |
| No logo, text wordmark instead | Normal when the git-ignored logo file is absent. |
| Practice Lab shows "WebXR VR is unavailable" | The browser has no headset session. Use desktop mode or open it in Quest Browser or SteamVR. |
| Weather board says "simulated (no network)" | No internet; the wind is a fixed fallback. Fine for practice. |

More: [demo-day/TROUBLESHOOTING.md](demo-day/TROUBLESHOOTING.md) and [demo-day/BACKUP-PLAN.md](demo-day/BACKUP-PLAN.md).

## 7a. Features worth trying (and how to switch them)

| Feature | Where | How to see it | Switches |
|---|---|---|---|
| **Why this matters** reasons and a rationale question | Practice Lab (16 of 17 scenarios), Hydrostatic Test Bay, Refinery Rounds | Complete a step: a one-sentence reason appears. After the last step, answer the question. The result screen or task board shows a separate "Reasoning" line. | Web: Esc or "Got it" closes the panel; the text is in `training-simulations/reasons.js`. Unity: answer by pointing and trigger, mouse click, or keys 1, 2, 3 |
| **Pedestrian awareness check** | Warehouse and Forklift (project 03) | Finish the crossing step and wait about 8 seconds. A co-worker walks past the forklift; the scene freezes and asks three questions about where they were. Then stop, watch, wait. | Inspector on the scenario: `CoworkerHazard`, `AwarenessCheck`, `EventDelay`, `CheckDelay`. The hub's copy is refreshed from project 03 |
| **Scenario thumbnails** | Practice Lab menu and the hub lobby portals | Open the menu or lobby. | Web thumbnails are made by the thumbnail script described in `training-simulations/README.md`; Unity thumbnails by `vive-steamvr/hub/tools/prepare_lobby_images.py` (needs Python with Pillow) |
| **Seasonal sky** | Practice Lab menu, hub lobby, hub Refinery Rounds | The sky changes season every 30 seconds with a pixelate and scan-line transition; a small chip names the season. | Replace the images with your own 360 photos: [assets/skies/README.md](../assets/skies/README.md). Reduced-motion browsers get a plain fade |

The sky images are public-domain stand-ins in a regional style; **they are not photos of Bradford**, and the credits are in [assets/skies/CREDITS.md](../assets/skies/CREDITS.md).

## 8. What is verified

Run on a development PC: the web demo and tests (desktop browsers, emulated Quest 3), the Unity projects in the editor in desktop mode, the hub lobby in the editor. **Not run:** any real Vive Pro 2 or Quest, a built Windows player of the hub, phones (only Chrome emulation). Scenario 17 has not been reviewed by a biologist. The reasons and questions have not been reviewed by a subject-matter expert. The new features have been exercised by scripted runs and screenshots, not played by hand end to end.

## Open the WebXR projects at once (VS Code)

`XR-Training.code-workspace` in the repository root opens each browser (WebXR) project as its own folder: the Practice Lab, multiplayer, the A-Frame, Three.js, Babylon.js and raw WebXR demos, the brand kit and the launcher, plus the repository root for docs, scripts and tools. In VS Code use **File > Open Workspace from File...** and pick it. The Unity projects and native SDK samples are not in it: open the Unity projects from Unity Hub (**Add > Add project from disk**, Unity 6000.3.20f1): `vive-steamvr/01-hydrostatic-test`, `02-refinery-rounds`, `03-warehouse-forklift`, `04-resistor-bench` and `hub` (the hub needs the local Asset Store packs and Rocketbox). The native samples (`unity-xri`, `openxr`, `steamvr`, `meta-xr`) hold only an `Assets` folder and notes, not full Unity projects.
