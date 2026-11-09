# Developer setup: everything needed to work on this repository

Written for a developer setting up a new Windows PC. Versions are the ones on the lab PC on 2026-11-08. Nothing below was tested on another machine, so if a step differs on yours, fix this page.

What you need depends on what you work on:

| You want to | Install |
|---|---|
| Run or edit the web labs (Three.js, Circuit Lab, Safety Lab, Fieldwork Practice Lab) | Git, Node.js 22 |
| Retake screenshots of the web labs (emulated Quest 3) | the above, plus Python 3 with Pillow, plus the `tools/vr-capture` packages |
| Open or build the Unity projects for the Vive | Unity Hub, Unity 6000.3.20f1 with Windows Build Support, SteamVR |
| Try a Quest app without a headset (Meta XR Simulator) | the Meta XR Simulator (section 5) |

## 1. Core tools

| Tool | Version used | Why |
|---|---|---|
| Git | 2.54 | clone and commit |
| Node.js | 22.x (22.22 here) | the relay server, site build, capture tools |
| Python | 3.14 (any recent 3) | gallery and image scripts; `pip install pillow` |
| PowerShell | 5.1 or 7 | build and check scripts in `scripts\` |

```
git clone https://github.com/jpcst52/xr-training-lab.git
cd xr-training-lab
npm install
```

## 2. Run the web labs (no headset)

Double-click `scripts\start-demo.cmd` (or `scripts\lab-test.cmd` on test day). It installs the server packages once, starts the HTTPS relay on port 8443 (or the next free port) and opens the launcher. See [HOW-TO-RUN.md](HOW-TO-RUN.md) and [LAB-TEST-TODAY.md](LAB-TEST-TODAY.md). Browser examples run offline: libraries are in `vendor/`, no CDN.

## 3. Screenshots and checks (emulated Quest 3)

```
cd tools\vr-capture
npm install
npx playwright install chromium
```

`tools/vr-capture` uses Playwright and IWER (an emulated Quest 3 runtime in the browser). Details: [../tools/vr-capture/README.md](../tools/vr-capture/README.md). Emulated pictures are labelled "emulated Quest 3", never presented as a real headset.

Useful checks from the repository root:

```
node tools/vr-capture/offline-check.mjs      browser examples load with no network
node scripts/check-image-sizes.mjs           no picture over 3 MB (see section 7)
node scripts/build-webxr.mjs                 builds the GitHub Pages site into site/
node tools/pages/site-check.mjs              checks the built site under its sub-path
```

## 4. Unity (Vive, SteamVR)

1. Install **Unity Hub**, then **Unity 6000.3.20f1** with the *Windows Build Support (Mono)* module. (Android Build Support is optional.)
2. Install **Steam** and **SteamVR**. In SteamVR: Settings > Developer > *Set SteamVR as OpenXR runtime*. The projects use OpenXR, so this must be set or controllers will not work.
3. Open a project from `vive-steamvr\` in Unity Hub: `01-hydrostatic-test`, `02-refinery-rounds`, `03-warehouse-forklift`, `04-resistor-bench`. They share the package `vive-steamvr\packages\com.pittbradford.xrtraining`.
4. Only one editor can open a project. A killed editor leaves `Temp\__Backupscenes` and the next open asks to "Recover Scene Backups"; delete that project's `Temp` folder to clear it.
5. After editing the shared kit, compile without opening a window, using a project that is not open: `powershell -File scripts\unity-compile-check.ps1 -Project 02-refinery-rounds`. One compile error in the kit puts every project into Safe Mode.
6. Build players with `scripts\build-all-vive.cmd` (all five, 45 minutes on the first PC and a few minutes on a faster one; on a new PC run `scripts\check-build-pc.cmd` first) or `scripts\build-vive.cmd` (the four without the hub); outputs go to `builds\vive\`, git-ignored. Editor-only APIs in Runtime code pass the compile check but break a player build, so build before handing over. See [BUILDS.md](BUILDS.md).

**The hub** (`vive-steamvr\hub`, lobby and all four scenarios) uses licensed Asset Store packs and the full Rocketbox library, none of which are in the repository. It builds only on a PC that has those files (the lab PC; see [BUILD-THE-HUB.md](BUILD-THE-HUB.md) to build elsewhere): `scripts\build-all-vive.cmd`. Never publish that build.

Rocketbox people (MIT): the standalone project 03 ships a small subset. To add more, run `python tools\people\export-subset.py <project> <Avatars/Folder/Name> ...` (reads from the hub's local library).

## 5. Meta XR Simulator (optional, for Quest development)

The simulator lets a Quest (OpenXR) app run on the PC without a headset. It is **not the focus of this repository**: the Unity projects target SteamVR, and the Quest is covered by the web labs. It is installed on the lab PC and documented here so it can be reproduced.

Installed here: **Meta XR Simulator v207.1** at `C:\Program Files\MetaXRSimulator\v207.1`, installed by the `metavr` command line tool.

Install:

```
npm install -g metavr                  Meta VR CLI (1.10.0 here); needs Node.js
metavr doctor                          lists what is detected and where
metavr tools list                      shows installable tools and versions
metavr tools install xrsim             installs the Meta XR Simulator (about 416 MB)
```

Other tools from `metavr tools list` that are **not** installed here and not needed for this repository: Android Studio and SDK, RenderDoc (Meta fork), Haptics Studio, Spatial Editor, SpatialSim emulator. For an Android/Quest build you also need JDK 17 (Eclipse Temurin 17 is installed) and Unity's Android module.

Using it:

- `metavr xrsim runtime` shows or changes which OpenXR runtime is active; `metavr xrsim app` starts and stops the simulator window; `metavr xrsim device`, `env` and `input` configure the simulated headset, room and controllers. Run `metavr xrsim --help` for the options.
- **The simulator is an OpenXR runtime, so it competes with SteamVR.** To test with the Vive, set the runtime back to SteamVR (SteamVR > Settings > Developer). If controllers stop working in a Unity build, check this first.
- Unity: with the simulator as the active OpenXR runtime, press Play in the editor with the OpenXR loader enabled. Not tested for these projects.
- **Verification status:** the simulator was installed and detected by `metavr doctor`. No project in this repository has been verified running in it. Quest screenshots in this repository come from IWER in a browser, not from the Meta XR Simulator.

## 6. Quest and Vive hardware

- Quest: same Wi-Fi as the PC, open the printed `https://<PC address>:8443/` in Quest Browser, accept the certificate warning once.
- Vive Pro 2 (wireless) on SteamVR: run the `.exe` from `builds\vive\<project>\`.
- Neither has been tested on real hardware yet at the time of writing; see [LAB-TEST-TODAY.md](LAB-TEST-TODAY.md) and [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md).

## 7. Repository rules that affect developers

- **Pictures must be under 3 MB.** `scripts\build-webxr.mjs` drops any image over 3 MB from the browser-example guides and the root README (700 KB for other guides), and the Pages link check then fails the deployment. This happened with `docs/images/launcher.png` (3.3 MB). Run `node scripts/check-image-sizes.mjs` before committing screenshots; it also runs in the Pages workflow and fails the build first with the file names. To shrink a PNG: `python -c "from PIL import Image; f='x.png'; Image.open(f).convert('RGB').quantize(256,dither=Image.Dither.NONE).save(f,optimize=True)"`. If a larger limit is ever wanted, change `LIMIT` in that script and the caps in `build-webxr.mjs` together.
- Do not commit `branding/` or `assets-private/` (ignored). The university logo files named in CLAUDE.md are the exception.
- Web demos use CC0 assets only; licensed packs stay local.
- Update `docs\Task-Status.md` when a task starts, finishes or is blocked (`npm run status` shows the dashboard).
- No Word documents: write HTML pages or Markdown.
