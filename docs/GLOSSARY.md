# Glossary and who this is for

Plain-language meanings of the terms used in this repository, written for the advisory board, employers and faculty. Nothing here assumes you have used a headset.

General safety-awareness practice only: none of the scenarios is a procedure, permit or certification. Nothing in this repository has been verified on a real Vive Pro 2 or Meta Quest yet; the guides say what was actually run (desktop browsers, the Unity editor, an emulated Quest 3).

## Who is this for?

| If you are... | Start with | What you will get |
|---|---|---|
| An employer or advisory-board member | The main menu, then [demo-day/TALK-TRACK.md](demo-day/TALK-TRACK.md) | A short demo path: the browser practice lab, a shared multi-user bench and the Unity training scenes, with the limits stated |
| A faculty member (industrial, energy, biology) | [research/README.md](research/README.md) and the scenario list in [../training-simulations/README.md](../training-simulations/README.md) | What each scenario teaches, what is a draft for review, and study ideas |
| A student or instructor who will run it | [HOW-TO-RUN.md](HOW-TO-RUN.md) | The three-step quick start and the full run guide |
| A developer extending it | [agents/AGENT-RULES.md](agents/AGENT-RULES.md), [README.md](README.md) and [../SDK-list.md](../SDK-list.md) | Repository conventions, the documentation index and the SDK samples |
| Setting up a headset | [demo-day/VIVE-SETUP.md](demo-day/VIVE-SETUP.md) or [demo-day/QUEST-SETUP.md](demo-day/QUEST-SETUP.md) | Steps for the first hardware session (still to be run) |

## Terms

**XR (extended reality).** The umbrella word for virtual, mixed and augmented reality together.

**VR (virtual reality).** A headset replaces what you see with a computer-made place. Used here for training you could not do safely or cheaply in real life.

**AR (augmented reality).** Computer graphics are added on top of the real world, usually seen through a phone or tablet camera.

**MR (mixed reality).** A headset shows the real room and virtual objects together, so virtual things can sit on a real table. Meta Quest headsets do this with passthrough cameras.

**WebXR.** A browser standard that lets a web page show VR or AR. Our web demos use it, so a headset only needs its browser: no app install. Where a browser or headset does not support it, the pages fall back to a normal desktop view.

**OpenXR.** An open industry standard that lets one program talk to many different headsets. The Unity projects use it, so the same project can target different headsets.

**SteamVR.** Valve's software that runs PC virtual reality and connects the PC to the HTC Vive Pro 2. It acts as the "OpenXR runtime" (the program that actually drives the headset) on the Vive PC.

**Meta Quest.** A family of standalone headsets from Meta. The web demos are opened in the Quest Browser, over the same Wi-Fi as the demo PC. Untested on a physical Quest so far.

**HTC Vive Pro 2.** A high-resolution PC-connected headset (used here wirelessly on a gaming desktop). Untested so far; [demo-day/HARDWARE-TEST-SESSION.md](demo-day/HARDWARE-TEST-SESSION.md) is the sheet for the first run.

**Unity.** The game engine used for the Vive training projects. They also run on a desktop with mouse and keyboard, which is how they have been checked.

**Relay.** A small program on the demo PC that passes messages between people in the same room, so several users see the same shared bench or layout. It needs no internet at demo time.

**Practice Lab.** The browser example of 17 short hands-on safety-awareness scenarios, in [../training-simulations/README.md](../training-simulations/README.md). Wrong actions show their consequence; one scenario is a labelled draft for faculty review.

**Hub (lobby).** A Unity scene where you walk onto a pad, called a portal, to load one of the four training projects. It uses commercial Asset Store packs that are kept on the development PC, not in the repository.

**Bake and adopt.** The Unity scenes used to be built entirely by code each time they started. "Bake" saves what the code built into the scene file so it can be seen and edited in the editor; "adopt" means that at start the code reuses those saved objects instead of building duplicates, so edits survive. See [../vive-steamvr/packages/com.pittbradford.xrtraining/README.md](../vive-steamvr/packages/com.pittbradford.xrtraining/README.md).

**Rocketbox people.** The animated human characters (Microsoft Rocketbox library) placed in the hub's warehouse and refinery scenes, including the co-worker who walks past in the awareness check. They are licensed local assets and are not committed to the repository.

**Season sky.** The sky behind the web pages changes through the four seasons about every 30 seconds with a short pixelate and scan-line transition. The images and credits are in [../assets/skies/CREDITS.md](../assets/skies/CREDITS.md); the brand kit describes the code in [../brand-kit/README.md](../brand-kit/README.md).

**Emulated Quest 3.** A desktop browser pretending to be a Quest 3, used to take VR-style screenshots. Pictures labelled this way are not photos from a real headset.

**Awareness check.** In the Warehouse scenario, a moment where the scene pauses and asks three short questions about what you just noticed. It is scored separately from the task score.
