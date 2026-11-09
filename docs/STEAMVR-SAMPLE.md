# Valve's SteamVR plugin sample: the first thing to check on the Vive

The Unity scenes in this repository did not work on the Vive when they were first tried, so the Vive work is being rebuilt on **Valve's own SteamVR Unity plugin** and its **Interaction System** (hands, grabbing, throwing, teleporting, drives and buttons), instead of the custom OpenXR rig the kit used before. Before anything of ours is built on it, this page makes **Valve's own sample scene** (`Interactions_Example`) into a Windows build, so the Vive can answer one question: *does Valve's plugin work on this PC, this Unity and this headset?* If it does, everything we rebuild on it has a working foundation. If it does not, the problem is the environment, not our scenes, and we fix that first.

General safety-awareness practice only; this page is about the Vive and the plugin, not about scenario content. **Nothing here has been tried on a headset yet.**

**Progress (2026-11-08, later):** the rebuild has started without waiting for the Vive result. Project 01 is migrated to the plugin and runs in the simulated headset; 02 to 04 and the hub follow. The sample remains the quickest way to learn whether Valve's plugin works on the Vive. See [STEAMVR-REBUILD.md](STEAMVR-REBUILD.md) for what is built and what is unverified.

## What was found (2026-11-08)

- **Use plugin release 2.8.5b (a beta).** The stable 2.8.0 shows no picture in Unity 6 (the "next up" window never goes away); Valve's fix is in 2.8.5b. Unity 6 starts VR on Direct3D 12 by default, and the plugin's tracker records that the Built-in pipeline does not work with single-pass stereo there. The sample build is therefore **Direct3D 11 only** and uses multi-pass stereo (the Built-in default). (Source: ValveSoftware/steamvr_unity_plugin issues 1129 and 1123 and the 2.8.5b release.)
- **The plugin does not compile as shipped on Unity 6000.3.** `PhysicMaterial` was renamed `PhysicsMaterial` and Unity 6.3 now treats the old name as an error (4 errors in `HandCollider.cs` and `Arrow.cs`). The setup script fixes those two files.
- The earlier custom-kit builds already used Direct3D 11 (read from the player log), so Direct3D 12 does not explain why they failed. They also start and draw correctly in SteamVR's simulated headset ([STEAMVR-SIMULATOR.md](STEAMVR-SIMULATOR.md)), so the cause is most likely the controllers (input and interaction), which only a real Vive can test. See the questions in "What I need from the Vive test".

## Make the sample build (about 10 minutes, once)

On the PC that has Unity 6000.3.20f1 with Windows Build Support (Mono), with **every Unity editor closed** and internet available (it downloads the plugin, 79 MB, from Valve's GitHub):

1. `git pull` the repository.
2. Double-click **`scripts\setup-steamvr-sample.cmd`**.
3. It creates the Unity project `vive-steamvr\steamvr-sample` (git-ignored), imports the plugin, applies the two-file fix, generates Steam Input actions from Valve's example files, assigns the OpenVR loader and Direct3D 11, and builds. The result is **`builds\vive\steamvr-sample\SteamVRSample.exe`** (about 200 MB). Logs are in `builds\logs\steamvr-sample-*.log`.
4. Copy the whole `builds\vive\steamvr-sample` folder to the VR Stage PC.

## Run it on the Vive

1. Start SteamVR and make sure the headset and both controllers show green.
2. Run `SteamVRSample.exe`.
3. The scene is Valve's `Interactions_Example`: a room with things to pick up and throw, a longbow, buttons, a wheel and a lever (drives), teleport areas, and animated hands. Valve's controls: point and press the trackpad (or thumbstick) forward to teleport, and use the trigger or grip to pick things up.

## What I need from the Vive test

Please tell me, for the sample (not for our scenes):

1. Does an image appear in the headset, and do the head and both controllers track?
2. Do you see hands (or controllers), can you teleport, can you pick up and throw an object, turn the wheel, press a button?
3. If something fails: what exactly happens (black screen, "next up" dashboard, no controllers, controllers present but nothing grabs), and what does `%USERPROFILE%\AppData\LocalLow\University of Pittsburgh at Bradford\SteamVR Sample\Player.log` say near the end?
4. For the old builds (the four projects and the hub): what exactly did not work? This decides what the rebuild has to fix.

## Next

If the sample works, the four scenarios and the lobby are rebuilt on the same foundation, one at a time, each tested on the Vive before the next. The plugin is not committed to this repository (it is 100 MB and Valve distributes it; it is BSD-3-Clause licensed); the setup script downloads it. See [BUILDS.md](BUILDS.md) for the older projects and [VR-STAGE-QUICKSTART.md](VR-STAGE-QUICKSTART.md) for getting builds onto the Stage PC.
