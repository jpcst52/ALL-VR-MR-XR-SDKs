# VR Stage PC: from this repository to a headset, start to finish

One page that ties the other guides together. Nothing here has been tried on the VR Stage PC or in a headset yet: treat the first run as a test and note what fails. General safety-awareness practice only; none of the scenarios is a procedure or certification.

## What runs where

| PC | Needs | Does |
|---|---|---|
| **Build PC** (the author's PC, which has the hub's licensed files) | Unity 6000.3.20f1 with Windows Build Support (Mono), this repository | Makes the five Windows players in `builds\vive\` |
| **VR Stage PC** | Steam and SteamVR, the Vive Pro 2 set up, a current GPU driver. **No Unity, no repository.** | Runs the players with the headset |
| Any PC or a Quest | A browser (the web labs, via the demo server) | See [LAB-TEST-TODAY.md](LAB-TEST-TODAY.md) |

The five players are `01-hydrostatic-test`, `02-refinery-rounds`, `03-warehouse-forklift`, `04-resistor-bench`, and **`hub`**, a lobby with portals that loads the other four, so the hub alone reaches every example. Projects 01 to 04 are small (about 150 to 200 MB each, no licensed art). The hub is about 1 GB and contains licensed Asset Store art and people.

## The usual arrangement: a remote Build PC, a lab PC, and the Stage PC

If the PC that holds the hub's licensed files is remote and you work at a lab PC, there are two ways to get working builds onto the Stage PC.

**Way 1: ship the finished builds (no Unity needed at the lab).** Copy `builds\vive` from the Build PC to the lab PC (about 1.9 GB: four projects of 150 to 200 MB each and the 1 GB hub), then on to the Stage PC (step 2). Any change you ask for is rebuilt on the Build PC and copied again.

**Way 2: build at the lab PC.** Then the lab PC needs all of this, in this order:

1. **Unity 6000.3.20f1** installed through Unity Hub with *Windows Build Support (Mono)*, and Unity licensed or signed in on that PC.
2. **The repository:** `git clone https://github.com/jpcst52/xr-training-lab.git` (or `git pull`). This gives projects 01 to 04 everything they need.
3. **The hub's licensed files** (only if you want the hub). They are not in git. On the Build PC run `scripts\pack-hub-assets.cmd` (writes `builds\hub-assets-<date>.zip`; 2,084 MB on 2026-11-08, about 3 GB unpacked). Get that file to the lab PC by whatever private route you have: remote-desktop file copy, a USB drive, or a private network share. It holds licensed Asset Store files, so move it only between your own machines and keep it out of GitHub, email and shared folders; check the packs' license terms if unsure.
4. **Close Unity, then unpack before opening the hub in Unity:** drag the zip onto `scripts\unpack-hub-assets.cmd`.
5. **Run `scripts\check-build-pc.cmd`.** It checks Unity and its Windows module, the repository, that no editor is open, free disk space (about 11 GB), the hub's files, and the Unity package server, and says what to fix. It changes nothing.
6. **Run `scripts\build-all-vive.cmd`.** The first Unity run on a new PC downloads packages (OpenXR, Input System) and imports everything into a new cache, so the first build is much slower than the author's 10 minutes per project (not measured on another PC).
7. Copy the folders from `builds\vive` to the Stage PC (step 2).

If Way 2 hits trouble, Way 1 is the fallback: the builds on the Build PC are already made.

## Step 1: get the builds (pick one)

**A. The builds already exist on the Build PC.** Folders in `C:\GitHub\xr-training-lab\builds\vive\`: `01-...`, `02-...`, `03-...`, `04-...`, `hub`, each with its `.exe`. As of 2026-11-08 the hub folder includes the lobby music and sky fix. Skip to step 2.

**B. Rebuild all five on the Build PC** (after pulling new changes). Close every Unity editor, then double-click **`scripts\build-all-vive.cmd`**. About 45 minutes on the first PC (the hub is about 11), a few minutes on a faster lab PC. It opens `builds\vive` when it finishes. A project that fails is reported and the others still build; logs are in `builds\logs\`. Details: [BUILDS.md](BUILDS.md).

**C. Build on a different PC** (a fresh checkout). Projects 01 to 04 build from the checkout alone with `scripts\build-vive.cmd`. The **hub needs the licensed files that are not in git**: on the PC that has them double-click `scripts\pack-hub-assets.cmd` (makes `builds\hub-assets-<date>.zip`, about 2 GB zipped, 3 GB of files), carry it over by USB, then on the new PC drag it onto `scripts\unpack-hub-assets.cmd` and run `scripts\build-all-vive.cmd`. Full explanation, the list of packs and a from-scratch route: [BUILD-THE-HUB.md](BUILD-THE-HUB.md). Never upload that zip or the hub build.

## Step 2: copy to the VR Stage PC

Copy whole folders from `builds\vive\` (USB drive or local network) to the Stage PC, for example `C:\XRTraining\`. Each `.exe` needs the files beside it (`*_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`, `D3D12`), so copy the entire folder.

- Projects 01 to 04: `XR-Training-Vive-<date>.zip` in the same folder holds all four if one file is easier.
- Hub: copy the `hub` folder. It holds licensed files: **USB or local network only**; do not put it in a cloud drive, email or GitHub.

## Step 3: set up the Stage PC (once)

1. Install Steam, then SteamVR (Steam Library, Tools). Set up the Vive Pro 2 and the room in SteamVR; the status window should show the headset and both controllers green.
2. In SteamVR make it the OpenXR runtime: Settings, Developer, **Set SteamVR as OpenXR runtime** (the menu name may differ in other SteamVR versions). If another OpenXR runtime is active (for example the Meta XR Simulator) the headset is not used and the app falls back to the desktop view.
3. First launch of an unsigned `.exe`: Windows SmartScreen may warn; choose More info, then Run anyway.

## Step 4: run and check

1. Start SteamVR and wake the headset.
2. Double-click the `.exe` (start with `hub\XRTrainingHub.exe`).
3. Quit with Alt+F4 on the desktop window.

What to look at, per build, is in [LAB-TEST-TODAY.md](LAB-TEST-TODAY.md) section 3, and the longer Pass/Fail sheet is [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md). For the hub in particular:

- It opens in the lobby: four portals with thumbnails, a sky photo, music starting after a few seconds (press M on the desktop window to mute).
- **Teleport:** push a thumbstick or press the trackpad forward, aim the arc, release. Right or left on a stick turns 45 degrees. (This describes the old OpenXR builds. Builds migrated to Valve's SteamVR plugin, so far only project 01, use Valve's teleport: hold the trackpad, aim at the floor area, release. Neither is verified on a real Vive; see [STEAMVR-REBUILD.md](STEAMVR-REBUILD.md).)
- **Portals:** stand on a pad for about 1.4 seconds; the scenario loads. Hold both grips for 1.5 seconds (Backspace on the desktop) to go back to the lobby.
- Please note: controller feel (the Vive trackpad is the least certain part), comfort, frame rate (the SteamVR frame-time overlay is enough), whether text is readable and reads in both eyes, the seated forklift driver pose in 03, and how the sound and lighting look and sound.

No headset connected? The players still run on the desktop (WASD, mouse, E or left click), which is a quick way to see that a copy is complete.

## If something fails

| Symptom | Likely cause and fix |
|---|---|
| Build script says the hub's scenes or art are missing | A fresh checkout: do step 1 option C |
| `build-all-vive.cmd` says a project is open in Unity | Close that editor (the script refuses to build an open project); if none is open, delete that project's `Temp` folder |
| A build fails with `error CS` | Open the log in `builds\logs\<project>.log` and send it to the developer |
| Black screen or no controller input on the Stage PC | SteamVR not running first, or SteamVR is not the OpenXR runtime (step 3) |
| `.exe` closes at once | The `_Data` folder or `UnityPlayer.dll` was not copied; check `%USERPROFILE%\AppData\LocalLow\University of Pittsburgh at Bradford\<product>\Player.log` |
| Sky looks flat in the hub | Note it; the sky shader fix was only checked in a windowed desktop run |

Related: [BUILDS.md](BUILDS.md) · [BUILD-THE-HUB.md](BUILD-THE-HUB.md) · [LAB-TEST-TODAY.md](LAB-TEST-TODAY.md) · [DEVELOPER-SETUP.md](DEVELOPER-SETUP.md)
