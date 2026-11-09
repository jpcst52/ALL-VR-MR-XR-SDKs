# Building the Vive / SteamVR players for the VR Stage PC

`scripts\build-vive.cmd` (or `scripts\build-vive.ps1`) makes Windows 64-bit standalone players of the Unity training projects so the `.exe` folders can be copied to the VR Stage PC (HTC Vive Pro 2 with SteamVR).

**Honest status.** Run for real on 2026-11-08 on the author's PC: all four projects build (148 to 204 MB each, about 10 minutes each). The first run failed because `SceneLook.cs` used `Light.shadowRadius`, an editor-only API that the editor accepts but a player build rejects; that is fixed. The Warehouse build was started once without a window and ran 25 s with no crash. **No build has been run on a headset or on the VR Stage PC.** Lesson: the editor compile check (`scripts/unity-compile-check.ps1`) does not catch editor-only APIs in Runtime code, so build one project after kit edits.

## Run it

1. Close every Unity editor that has one of the projects open (the script refuses to build an open project; it checks for a Unity process whose command line names the project and for `Temp\UnityLockfile`).
2. **All five at once:** double-click `scripts\build-all-vive.cmd` (the four projects plus the hub, about 45 minutes on the first PC and a few minutes on a faster one; it opens `builds\vive` when done, ready to copy to the VR Stage PC). Or double-click `scripts\build-vive.cmd` for the four projects without the hub, or run from the repository root:

```
powershell -ExecutionPolicy Bypass -File scripts\build-vive.ps1 -DryRun        # print the commands only
powershell -ExecutionPolicy Bypass -File scripts\build-vive.ps1                # the four projects
powershell -ExecutionPolicy Bypass -File scripts\build-vive.ps1 -Projects 04-resistor-bench
powershell -ExecutionPolicy Bypass -File scripts\build-vive.ps1 -IncludeHub    # plus the hub (lab PC only)
```

`npm run build:vive` runs the same script. Options: `-UnityExe <path>` (or set `UNITY_EXE`; default `C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe`), `-Projects <names>`, `-NoZip`, `-Batchmode`, `-DryRun` (alias `-WhatIf`).

The projects are built one at a time. For each, Unity is started with `-projectPath <project> -executeMethod ProjectSetup.BuildWindows -quit -logFile builds\logs\<project>.log`. `ProjectSetup.BuildWindows` already exists in projects 01 to 04 (it is the menu item Pitt-Bradford > Build Windows player and calls the kit's `Configurator.BuildWindows`, which builds the scenes enabled in Build Settings for Windows 64-bit). The repository's other Unity command-line runs (captures, bakes) are documented without `-batchmode` because they need graphics, so the script does not pass it either; `-Batchmode` is there to try, untested. Each Unity start takes minutes (script compile, shaders, player build): expect a long first run.

**The hub needs files that are not in git.** Its scenes (`Assets\Scenes\Local`), baked data and the licensed Asset Store packs and Rocketbox people are git-ignored, so on a fresh checkout the script reports the hub as SKIPPED (listing what is missing) and still builds the other four. To build the hub on another PC follow [BUILD-THE-HUB.md](BUILD-THE-HUB.md) (pack the files on the lab PC with `scripts\pack-hub-assets.cmd`, unpack them with `scripts\unpack-hub-assets.cmd`). Projects 01 to 04 build from a plain checkout. The combined zip `XR-Training-Vive-<date>.zip` holds projects 01 to 04 only; the hub has its own `hub.zip` and must stay off GitHub and shared drives.

After each build the script checks the log for `error CS`, requires the line `[build] Succeeded` and the `.exe`, then copies the build folder. A failed project is reported and the next one still runs; the exit code is 1 if any failed.

**SteamVR plugin migration (in progress, 2026-11-08).** Projects are being moved from the custom OpenXR rig to Valve's SteamVR plugin. A migrated project needs `powershell -File scripts\setup-steamvr.ps1 -Projects <folder>` before `build-vive` (it is not run automatically yet; Unity editors closed). Only project 01 is migrated so far, and only tested in SteamVR's simulated headset; 02 to 04 and the hub may still be the old build, and no migrated build has been tried on a real Vive. See [STEAMVR-REBUILD.md](STEAMVR-REBUILD.md). The OpenXR-runtime step below applies to the old builds.

## What you get

```
builds\                          (git-ignored)
  logs\<project>.log             Unity log per build
  vive\
    01-hydrostatic-test\         copy this WHOLE folder
      HydrostaticTest.exe
      HydrostaticTest_Data\
      UnityPlayer.dll ... 
      README-VR-STAGE.txt
    02-refinery-rounds\   (RefineryRounds.exe)
    03-warehouse-forklift\ (WarehouseForklift.exe)
    04-resistor-bench\    (ResistorBench.exe)
    <project>.zip                one zip per project
    XR-Training-Vive-<date>.zip  all of them in one zip
```

The `.exe` keeps the name the project gives it (not the folder name) because Unity finds its `<name>_Data` folder by that name; do not rename it. The `.exe` does not run without the `_Data` folder and the DLLs next to it. Zips are made with `Compress-Archive` (fine for these sizes; it is slow for very large builds).

## Copy to the VR Stage PC and run

1. Copy a project folder (or unzip the combined zip) to the VR Stage PC, for example `C:\XRTraining\`.
2. Once per PC: install Steam and SteamVR, set up the Vive Pro 2, and in SteamVR choose Settings, Developer, Set SteamVR as OpenXR runtime. The whole path from build to headset is on one page: [VR-STAGE-QUICKSTART.md](VR-STAGE-QUICKSTART.md). `README-VR-STAGE.txt` in each folder repeats this.
3. Start SteamVR, wake the headset, double-click the `.exe`. Windows SmartScreen may warn about the unsigned file (More info, Run anyway).
4. No headset: the app runs on the desktop with keyboard and mouse (keys are in each project's README).

## The hub

`-IncludeHub` builds `vive-steamvr\hub` (lobby and the four scenarios) into `builds\vive\hub\XRTrainingHub.exe`, using `HubBuild.BuildWindows` in `vive-steamvr\hub\Assets\Editor\HubBuild.cs`. It uses local Asset Store packs and Rocketbox people: copy that build to the lab PC only and never publish it. First built 2026-11-08 (about 1 GB, 11 minutes, `-IncludeHub -Projects hub` builds only the hub); it was built but never started, so lobby navigation in the player is unverified. The hub is usually open in the owner's editor, which blocks the build. The combined zip holds projects 01 to 04 only; the hub has its own `hub.zip`.

## If it fails

| Symptom | Cause |
|---|---|
| "already open in a Unity editor" | Close the editor for that project |
| "Temp\UnityLockfile exists" | Unity did not shut down cleanly; close Unity and delete that project's `Temp` folder |
| "no '[build] Succeeded' line" | Read the end of `builds\logs\<project>.log` |
| Compile errors | Fix them in the editor first; the script prints the first one |
| `Unity.exe not found` | Install Unity 6000.3.20f1 or pass `-UnityExe` |

Projects also still document the manual route: Pitt-Bradford > Build Windows player inside the editor writes `Builds\<Name>\` in the project folder.

Related: [GITHUB-PAGES.md](GITHUB-PAGES.md) (browser examples), [HOW-TO-RUN.md](HOW-TO-RUN.md), [demo-day/VIVE-SETUP.md](demo-day/VIVE-SETUP.md).
