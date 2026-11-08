# Documentation index

Every guide in this repository, and what to read it for. The same list is in the **main menu** (the launcher): start the demo with `scripts\start-demo.cmd` and it opens at `https://localhost:8443/`. The menu also opens these guides in the browser, offline.

General safety-awareness practice only: none of the scenarios is a procedure, permit or certification. Nothing here has been verified on a real Vive Pro 2 or Quest yet; each guide says what was actually run.

## I want to...

| I want to... | Read |
|---|---|
| Run the demo, a Unity project, the hub, or a headset | [HOW-TO-RUN.md](HOW-TO-RUN.md) |
| Set up a new PC (Node, Unity, SteamVR, Meta XR Simulator, checks) | [DEVELOPER-SETUP.md](DEVELOPER-SETUP.md) |
| Test day: what to start and what to check | [LAB-TEST-TODAY.md](LAB-TEST-TODAY.md) |
| Listen to and review the Unity sound effects | [audio-review/README.md](audio-review/README.md) |
| Check that nothing is broken, or play-test by hand | [TESTING.md](TESTING.md) |
| See what is done and what is left | [Task-Status.md](Task-Status.md) (task list) and [PROJECT-STATUS.md](PROJECT-STATUS.md) (decisions and caveats) |
| Run the live status dashboard | [status-dashboard-guide.md](status-dashboard-guide.md) |
| Prepare for demo day (Nov 13, 2026) | [demo-day/README.md](demo-day/README.md) |
| Test on the real headsets | [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md) |
| Understand the research and study ideas | [research/README.md](research/README.md) |
| Learn the terms (XR, WebXR, OpenXR, bake and adopt) and who each guide is for | [GLOSSARY.md](GLOSSARY.md) |
| Pick up the work in a new session | [agents/AGENT-RULES.md](agents/AGENT-RULES.md); the Unity sound and effects API is in [agents/KIT-AUDIO-FX.md](agents/KIT-AUDIO-FX.md) |

## Running and testing

| Guide | What it covers |
|---|---|
| [../CREDITS.md](../CREDITS.md) | Third-party credits: fonts, Open-Meteo weather, Rocketbox people, Asset Store packs, models |
| [GLOSSARY.md](GLOSSARY.md) | Plain-language terms and a who-is-this-for table, for the advisory board and faculty |
| [HOW-TO-RUN.md](HOW-TO-RUN.md) | One page for everything: a three-step quick start for demo day, the demo server, Unity projects (desktop and headset), the hub lobby, tests, screenshots, decks, troubleshooting |
| [HOLOLENS-AR-NOTES.md](HOLOLENS-AR-NOTES.md) | Why content looks small on a HoloLens (small additive display), how to measure it and fit content to it; untested on a HoloLens |
| [GITHUB-PAGES.md](GITHUB-PAGES.md) | The static WebXR site on GitHub Pages: address, QR code, what works (and what needs the relay), opening it on Quest, HoloLens 2, Vive XR and phones, updating and checking it; nothing tested on a headset |
| [BUILDS.md](BUILDS.md) | `scripts\build-vive.cmd`: builds the four Unity projects (and optionally the hub) as Windows players for the VR Stage PC; script not run for real yet |
| [TESTING.md](TESTING.md) | The automated suites with expected counts, and hands-on check tables (desktop Unity D1-D14, baked scenes B1-B4, lobby L1-L6, web W1-W6) |
| [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md) | Pass/fail sheet for the first lab run on the Vive Pro 2 and Quest |

## Demo day

[demo-day/README.md](demo-day/README.md) is the entry point. Then: [CHECKLIST](demo-day/CHECKLIST.md), [TALK-TRACK](demo-day/TALK-TRACK.md), [VIVE-SETUP](demo-day/VIVE-SETUP.md), [QUEST-SETUP](demo-day/QUEST-SETUP.md), [BACKUP-PLAN](demo-day/BACKUP-PLAN.md), [TROUBLESHOOTING](demo-day/TROUBLESHOOTING.md).

## Status

| Guide | What it covers |
|---|---|
| [Task-Status.md](Task-Status.md) | The task list the dashboard reads (done, in progress, waiting, needs the owner) |
| [PROJECT-STATUS.md](PROJECT-STATUS.md) | Narrative status: decisions, caveats, what was verified and what was not |
| [status-dashboard-guide.md](status-dashboard-guide.md) | How the local dashboard works (`npm run status`) |
| [COMPLIANCE-SWEEP.md](COMPLIANCE-SWEEP.md) | Sweep for partner names, slogan titles, third-party credits and private files, with the findings left for the owner |

## Research

[research/README.md](research/README.md) is the index. [VERIFICATION.md](research/VERIFICATION.md) records the 2026-11-03 re-check of every citation, and [CONTENT-REVIEW.md](research/CONTENT-REVIEW.md) reviews the scenario safety and anatomy statements against public sources. [UNIVERSITY-VR-AR-LANDSCAPE.md](research/UNIVERSITY-VR-AR-LANDSCAPE.md) surveys other universities' VR and AR work; [STUDY-DESIGNS.md](research/STUDY-DESIGNS.md) holds the study designs and the presence and cognitive-load instruments; [references.bib](research/references.bib) holds the verified sources.

## Per-project notes

| Folder | Notes |
|---|---|
| [../training-simulations/](../training-simulations/README.md) | Fieldwork XR Practice Lab (web, 17 scenarios) |
| [../multi-player/](../multi-player/README.md) | Relay, Circuit Lab, Site Safety Layout, hydrostatic test (multi-user web) |
| [../vive-steamvr/hub/](../vive-steamvr/hub/README.md) | Hub lobby with portals (local Asset Store packs) |
| [../vive-steamvr/01-hydrostatic-test/](../vive-steamvr/01-hydrostatic-test/README.md) | Unity project 01 |
| [../vive-steamvr/02-refinery-rounds/](../vive-steamvr/02-refinery-rounds/README.md) | Unity project 02 |
| [../vive-steamvr/03-warehouse-forklift/](../vive-steamvr/03-warehouse-forklift/README.md) | Unity project 03 |
| [../vive-steamvr/04-resistor-bench/](../vive-steamvr/04-resistor-bench/README.md) | Unity project 04 |
| [../vive-steamvr/packages/com.pittbradford.xrtraining/](../vive-steamvr/packages/com.pittbradford.xrtraining/README.md) | Shared Unity kit (signage, bake and adopt, scenarios) |
| [../brand-kit/](../brand-kit/README.md) | Web brand kit: brand bar, fonts, sky shader, logo rules |
| [../SDK-list.md](../SDK-list.md) | The SDK list the single examples follow |

Single-SDK examples (each folder has its own README): [a-frame](../a-frame/README.md), [threejs](../threejs/README.md), [babylonjs](../babylonjs/README.md), [webxr](../webxr/README.md), [unity-xri](../unity-xri/README.md), [openxr](../openxr/README.md), [steamvr](../steamvr/README.md), [meta-xr](../meta-xr/README.md), [arcore-android](../arcore-android/README.md), [apple-spatial](../apple-spatial/README.md).

## Tools

| Tool | Guide |
|---|---|
| Emulated-VR screenshots, UI and offline checks | [../tools/vr-capture/README.md](../tools/vr-capture/README.md) |
| PowerPoint deck builder | [../tools/decks/README.md](../tools/decks/README.md) |
| Status dashboard | [../tools/status/README.md](../tools/status/README.md) |
| Offline libraries | [../vendor/README.md](../vendor/README.md) |
| Sky image credits | [../assets/skies/CREDITS.md](../assets/skies/CREDITS.md) |

## Slide decks

[Repository-Guide.pptx](../Repository-Guide.pptx), [Project-Overview.pptx](../Project-Overview.pptx), [Run-the-XR-Examples.pptx](../Run-the-XR-Examples.pptx), [Multiplayer-WebXR-Examples.pptx](../multi-player/Multiplayer-WebXR-Examples.pptx). Rebuild with `tools/decks` (see its README).
