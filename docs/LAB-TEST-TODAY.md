# Lab test day: what to start and what to check

Nothing here has been tried on a real Vive Pro 2 or Quest yet. Every screenshot so far is a desktop editor render or an emulated Quest 3. Today is the first real run, so please note what fails, not just what works. The longer checklist with Pass / Fail columns is [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md); this page is the short version for the things changed most recently.

## 1. Start everything (one double-click)

Double-click **`scripts\lab-test.cmd`**. It opens the folder with the Unity builds, starts the demo server (HTTPS, port 8443 or the next free one) and opens the launcher in your browser. Leave its window open; closing it stops the server. If Node.js is missing it says so. First run only: it runs `npm install` once (needs internet).

The window prints the addresses to use, like `https://192.168.x.x:8443/`. If another program holds the port, it picks the next free one and says so; use the printed number everywhere. Problems: `scripts\demo-doctor.cmd`, or [demo-day/TROUBLESHOOTING.md](demo-day/TROUBLESHOOTING.md).

## 2. Quest (web pages, server needed)

Quest on the same Wi-Fi as this PC. In Quest Browser open the printed address plus the path below. The first visit shows a certificate warning: Advanced, then Proceed. Enter a name and a shared room name, then **Enter VR**.

| Page | Path | Check |
|---|---|---|
| Circuit Lab v2 (new look) | `/multi-player/v2/` | joins, parts drop and snap, bulb lights, a second headset or a laptop in the same room sees it |
| Site Safety Lab v2 | `/multi-player/v2/?theme=safety` | same, with the checklist |
| Circuit Lab v1 (kept) | `/multi-player/vr/` | toolbox panel, **Y** opens and **B** closes the part under the ray, 4 examples, Reset needs two presses |
| Site Safety Layout v1 | `/multi-player/vr/?theme=safety` | same |
| Fieldwork Practice Lab | `/training-simulations/` | pick a scenario, point and trigger, result screen with reasoning |

Things to look at: frame rate and comfort in v2 (it has PBR textures, glow and four dynamic lights, built for a Quest budget but **not measured on a Quest**), whether text is readable, whether the hands look right, whether the two labs behave the same with two people in the room. There is no v2 AR page; the AR page is v1.

## 3. Vive Pro 2 with SteamVR (Unity builds, no server)

1. Start SteamVR and check the headset is tracked. SteamVR must be the OpenXR runtime (SteamVR > Settings > Developer > Set SteamVR as OpenXR runtime). You did this already on this PC.
2. Open `builds\vive\<project>\` (the folder that `lab-test.cmd` opened) and run the `.exe` inside it. Keep the whole folder together; the `.exe` needs the `_Data` folder beside it. If a project folder is missing, its build failed: see `builds\logs\<project>.log` and section 5.

| Build | What to check |
|---|---|
| 01 Hydrostatic Test Bay | trigger on valves, grip a blind flange, pump to test pressure, relief valve; the "why this matters" question answered with the trigger |
| 02 Refinery Rounds | walk the rounds, gas alarm, muster to the upwind point; text readable |
| 03 Warehouse and Forklift | **new:** the co-worker and the forklift driver are realistic people. Does the driver look right in the cab? Does the co-worker walk past? Mirror, signs, the pause-and-ask check |
| 04 Resistor Bench | pick up a part, use the meter, the burnt-LED mistake, the question |
| **Hub** (`builds\vive\hub\XRTrainingHub.exe`) | **the one to start with if you want to reach everything:** it opens in the lobby, and from there you walk to the four scenarios (Hydrostatic, Refinery, Warehouse, Resistor Bench). Check that each scenario loads from the lobby and that you can get back. About 1 GB, uses licensed art: lab PC only, never upload it. Built 2026-11-08; only built, never started or run in a headset |

For every build: left thumbstick moves (or teleports on forward), right thumbstick snap-turns 45 degrees, both hands visible, and the Vive wands work (the **trackpad** feel is the least certain part; the controller bindings are enabled for Vive, Index and Touch but were never tried). Note anything that is too bright, too dark or too harsh (the lighting was just softened), anything that sounds wrong (the sound was rebuilt but only measured, never heard in a scene), and frame rate (the SteamVR frame-time overlay is enough).

## 4. What changed recently and has not been seen in a headset

- Softer lighting in all five Unity scenes (hub: lobby, hydro, refinery, warehouse, resistor), a new `SceneLook` pass.
- All Unity sound effects and room beds rebuilt. Please listen, then tell me which sounds are bad. The clip-by-clip review page is `docs\audio-review\index.html` (works offline; open it in a browser).
- Realistic people in project 03 only. Projects 01, 02 and 04 have no people.
- Teleport, snap turn and visible hands (checked with a desktop self-test, never on a headset).
- Circuit and Safety Lab v2, and the in-headset toolbox in v1 (tested only in an emulated Quest 3).

## 5. If something fails

- A build will not start or shows a black screen: run it again with SteamVR already running; look in `%USERPROFILE%\AppData\LocalLow\University of Pittsburgh at Bradford\<product>\Player.log`.
- No controller input in a build: the OpenXR runtime is probably not SteamVR. Check SteamVR > Settings > Developer.
- A web page will not load on the Quest: wrong address or port, or the Quest is on a different network. Open the printed address on the PC first.
- Open the Unity project in the editor instead of the build (`vive-steamvr\0x-...`, Unity 6000.3.20f1), press Play with the headset on. Close any other Unity first; stray backup scene prompts ("Recovering Scene Backups") are caused by a killed editor, and deleting that project's `Temp\__Backupscenes` folder clears them.
- Screenshot gallery to compare against what you see: `docs\gallery-standalone.html` (one file; Unity pictures are editor renders and the VR pictures are an emulated Quest 3, not a headset).

## 6. Rebuilding the Unity builds

`scripts\build-vive.cmd` (close every Unity editor first). Details and options: [BUILDS.md](BUILDS.md). The hub (all five scenes, uses Asset Store art that cannot be redistributed) is built with `scripts\build-vive.cmd -IncludeHub` (add `-Projects hub` for the hub alone, about 11 minutes) only on the lab PC and its build must never be published. Setting up another PC: [DEVELOPER-SETUP.md](DEVELOPER-SETUP.md).
