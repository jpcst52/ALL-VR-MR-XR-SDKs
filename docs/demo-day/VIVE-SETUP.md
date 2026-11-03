# Vive Pro 2 setup (wireless adapter, SteamVR)

**Verify at rehearsal.** The agents who wrote this had no Vive. Steps below are drawn from the vendor pages cited; some vendor pages are old, so compare against the current page before relying on them.

## 1. Headset and wireless adapter

HTC's pages for the Vive Pro series describe attaching the wireless adapter, pairing it with the link box (for the Vive Pro 2 pairing is done in the VIVE Console app), powering it from the approved power bank, and opening SteamVR to confirm base stations and controllers are ready.

- Setting up the wireless adapter with the Vive Pro series: https://www.vive.com/us/support/wireless-adapter/category_howto/vive-pro-wireless-setup-overview.html
- Attaching the adapter to Vive Pro / Pro Eye / Pro 2: https://www.vive.com/us/support/wireless-adapter/category_howto/attaching-wireless-adapter-to-vive-pro.html
- Vive Pro 2 setup: https://www.vive.com/us/setup/vivepro2/

Follow those pages for the PCIe card, cables and pairing. Do not rely on this file for the hardware procedure.

## 2. Make SteamVR the active OpenXR runtime

The Unity projects use OpenXR. VIVE's support page gives these steps: open SteamVR settings, under Advanced Settings click Show, open Developer, check that "Current OpenXR Runtime" is SteamVR, and if not click "SET STEAMVR AS OPENXR RUNTIME", then run the OpenXR title again. Source: https://www.vive.com/us/support/vs/category_howto/trouble-with-openxr-titles.html (that page's context is VIVE Streaming; HTC's developer overview says the same setting applies for Vive Pro PC VR: https://developer.vive.com/resources/openxr/openxr-pcvr/overview/). Menu names can differ between SteamVR versions: verify at rehearsal.

Only one OpenXR runtime is active at a time. If another runtime (for example a Quest PC link app) was set active, set SteamVR again before the demo.

## 3. Unity projects

The projects are in `vive-steamvr/`: `hub/` (a lobby with four portals and five scenes) and `01-hydrostatic-test/`, `02-refinery-rounds/`, `03-warehouse-forklift/`, `04-resistor-bench/`. The hub's props and people are local-only; see [README.md](README.md). They use the shared package `vive-steamvr/packages/com.pittbradford.xrtraining`. `hub` is set to Unity `6000.3.20f1` (see `hub/ProjectSettings/ProjectVersion.txt`); install that editor version in Unity Hub.

1. Start SteamVR first (headset awake and tracked), then open the project in Unity Hub.
2. First open imports assets and can take several minutes. Do this the day before.
3. Check Edit > Project Settings > XR Plug-in Management has OpenXR enabled for the Windows platform. Unity's documentation: https://docs.unity3d.com/6000.2/Documentation/Manual/com.unity.xr.openxr.html
4. Press Play. Verify at rehearsal that the headset view and controllers work in the Editor and in a built player. The projects compile and run in the editor in desktop mode and passed scripted self-tests there; nothing has been run on a headset.

## 4. Web version on the Vive PC

Open `https://localhost:8443/training-simulations/` (the Practice Lab) or `https://localhost:8443/multi-player/vr/` in desktop Chrome or Edge (accept the certificate warning once). Whether the browser enters WebXR through SteamVR on this PC is a rehearsal check: verify at rehearsal. The desktop 2D view works without a headset.
