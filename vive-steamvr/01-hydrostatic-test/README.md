# Hydrostatic Test Bay (Vive Pro 2 / SteamVR, Unity)

Hands-on practice for a hydrostatic (150 psi) test of a shell-and-tube heat exchanger, in VR on a Vive Pro 2 or at the desktop with mouse and keyboard. The scenario is inspired by the web example in `training-simulations/` and the multi-user rig in `multi-player/hydrotest/`.

**Status:** runs in the Unity editor in desktop mode (the owner played it). It has **not** been tested on a Vive Pro 2 or any headset. Screenshots from the Unity editor are in the hub project (see [../hub/README.md](../hub/README.md)); a screenshot set for this project has not been generated yet.

This is general safety-awareness practice, not a site procedure, a test permit, or a certification. It does not replace training by your employer.

## What you do

1. Fit both blind flanges on the top nozzles.
2. Open the VENT first, then the FILL valve.
3. Close VENT when the vessel is full, then close FILL.
4. Start the pump and raise to 150 psi (no more).
5. Hold 15 seconds from behind the yellow line.
6. Stop the pump, open DRAIN, then open VENT.

A task board shows the steps, mistakes, and critical misses (for example, pressurizing with the vent closed or standing over the vessel during the hold).

## Controls

| Action | Vive wands | Desktop |
|---|---|---|
| Move | Left trackpad/stick | W A S D |
| Look / turn | Head; right stick snap-turn | Hold right mouse and move |
| Press (valves, pump buttons) | Point + trigger | Crosshair + left click |
| Grab (blind flanges) | Grip near the object | Look at the object and press E |

## Setup

1. Open `vive-steamvr/01-hydrostatic-test` in Unity Hub with Unity **6000.3.20f1**. The project uses the shared kit in `../packages/com.pittbradford.xrtraining` by a `file:` path.
2. Run the menu **Pitt-Bradford > Set up project**. It configures OpenXR and the Input System and builds `Assets/Scenes/HydrostaticTest.unity`.
3. For a headset: set SteamVR as the active OpenXR runtime, start SteamVR, then press Play.
4. Optional: **Pitt-Bradford > Build Windows player** builds to `Builds/HydrostaticTest/`.

## Files

- `Assets/Editor/ProjectSetup.cs`: the setup and build menu items.
- `../packages/com.pittbradford.xrtraining/Runtime/Scenarios/HydroTest.cs`: the scenario itself. It lives in the shared kit so the hub project can reuse it.

## What is simulated and what is not

Simulated: valve and vent states, fill level, pump and pressure rise, the pressure hold, and the rules that score order and safety. Not simulated: real fluid and pressure physics, relief devices, permits, or site procedure.

## Research basis

See [../../docs/research/README.md](../../docs/research/README.md).
