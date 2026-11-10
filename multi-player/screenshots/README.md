# Multiplayer WebXR screenshots

These captures show the running examples in desktop browser fallback mode, re-shot on November 1, 2026 after the professional-look pass (`node tools/vr-capture/readme-shots.mjs mp`; the git-ignored logo is blocked, so the brand bar and the board plaque show the text wordmark). They document the browser experience and the shared Circuit Lab and Site Safety Layout scenes; neither image is a headset recording.

| Example | Capture | Notes |
|---|---|---|
| Multiplayer VR | [multiplayer-vr.png](./multiplayer-vr.png) | Circuit Lab with two participants and a closed loop (battery, switch, bulb, wires) with the bulb lit. |
| Multiplayer AR | [multiplayer-ar.png](./multiplayer-ar.png) | Site Safety Layout with two participants, the sample layout, and a passing checklist. Physical camera passthrough is not shown. |

For actual immersive sessions, use a supported WebXR device and a trusted HTTPS origin. The AR example additionally requires both participants to calibrate the same physical origin and +Z direction; see the [AR guide](../ar/README.md#calibrate-a-shared-physical-board).

![Multiplayer VR](./multiplayer-vr.png)

![Multiplayer AR](./multiplayer-ar.png)

## November 3, 2026: rooms, props and people

Re-shot after the realism upgrade (see [Rooms, props and people](../README.md#rooms-props-and-people) and [CREDITS.md](../CREDITS.md)). Produced by `node multi-player/tools/shots.mjs` (full-detail page: shadows and glTF props on) in desktop Chrome on a software renderer. The other participants are scripted relay clients, so their positions are staged: one walks to its cursor, one stands still, one sends a head and hand pose the way a headset does. **Nothing here is a real headset or a real two-device session.** The Quest captures are an **emulated Meta Quest 3** (IWER).

| Capture | What it shows |
|---|---|
| [circuit-lab-avatars.png](./circuit-lab-avatars.png) | Circuit Lab in the lab room with three other participants in hard hats (also copied to multiplayer-vr.png). |
| [site-safety-avatars.png](./site-safety-avatars.png) | Site Safety Layout in the site yard (also copied to multiplayer-ar.png). Desktop fallback of the AR page, no camera passthrough. |
| [test-bay-avatars.png](./test-bay-avatars.png) | Hydrostatic Test Rig in the test bay, completed rig (also copied to hydrotest-rig.png). |
| [fallback-no-models.png](./fallback-no-models.png) | Every model request blocked: props become boxes and cylinders, the person becomes a primitive figure, the page still works. |
| [vr/01-circuit-lab-in-vr.png](./vr/01-circuit-lab-in-vr.png) | Emulated Quest 3 inside the VR session: table-top board on a floor stage, other participants as small characters. |
| [vr/02-quest-peer-seen-by-desktop.png](./vr/02-quest-peer-seen-by-desktop.png) | What a desktop user sees of the emulated Quest participant: character under the head, gloves at the controllers. |

![Site Safety Layout](./site-safety-avatars.png)

![Test bay](./test-bay-avatars.png)

## November 9, 2026: AR Circuit Lab

[ar-circuit-desktop.png](./ar-circuit-desktop.png) is the [AR Circuit Lab](../ar-circuit/) page in its desktop preview, written by `node multi-player/tools/v2-shots.mjs`: a bright stand-in room with a pale bench and teal lines, the sample circuit loaded and the switch closed; the other participants are scripted relay clients. **It is not an AR session**; the AR path has not been run on any device or emulator.

## November 6, 2026: Lab v2 (new look)

Produced by `node multi-player/tools/v2-shots.mjs` (desktop Chrome, software renderer, full detail: shadows and glTF props on). The other participants in the desktop shots are scripted relay clients; the Quest shots are an **emulated Meta Quest 3** (IWER) in lite mode, never a real headset. Render-cost numbers are in [v2-render-cost.json](./v2-render-cost.json) and the [multi-player README](../README.md#lab-v2-circuit-lab-and-safety-lab-with-a-new-look-v1-is-unchanged).

| Capture | What it shows |
|---|---|
| [v2-circuit-lab-desktop.png](./v2-circuit-lab-desktop.png) | Circuit Lab v2: workshop with window light and a green cutting-mat bench, a closed "Motor and bulb" circuit with current dots, the lit bulb and terminal rings. |
| [v2-circuit-lab-closeup.png](./v2-circuit-lab-closeup.png) | Close-up of the battery, switch, motor and sagging wire. |
| [v2-circuit-lab-bulb.png](./v2-circuit-lab-bulb.png) | The lit bulb (glass, filament, halo glow) next to the battery and switch. |
| [v2-circuit-lab-quest.png](./v2-circuit-lab-quest.png) | Emulated Quest 3 inside the session: toolbox, glove hands, the room around the table-top board. |
| [v2-safety-lab-desktop.png](./v2-safety-lab-desktop.png) | Safety Lab v2: bay with painted deck, Welding bay layout, a pulsing beacon with light beams. |
| [v2-safety-lab-closeup.png](./v2-safety-lab-closeup.png) | Close-up of the striped barriers, beacon, extinguishers, exit and muster signs. |
| [v2-safety-lab-quest.png](./v2-safety-lab-quest.png) | Emulated Quest 3 inside the session, Safety Lab. |

![Circuit Lab v2](./v2-circuit-lab-desktop.png)

![Safety Lab v2](./v2-safety-lab-desktop.png)
