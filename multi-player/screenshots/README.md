# Multiplayer WebXR screenshots

These captures show the running examples in desktop browser fallback mode, re-shot on November 1, 2026 after the professional-look pass (`node tools/vr-capture/readme-shots.mjs mp`; the git-ignored logo is blocked, so the brand bar and the board plaque show the text wordmark). They document the browser experience and the shared Circuit Lab and Site Safety Layout scenes; neither image is a headset recording.

| Example | Capture | Notes |
|---|---|---|
| Multiplayer VR | [multiplayer-vr.png](./multiplayer-vr.png) | Circuit Lab with two participants and a closed loop (battery, switch, bulb, wires) with the bulb lit. |
| Multiplayer AR | [multiplayer-ar.png](./multiplayer-ar.png) | Site Safety Layout with two participants, the sample layout, and a passing checklist. Physical camera passthrough is not shown. |

For actual immersive sessions, use a supported WebXR device and a trusted HTTPS origin. The AR example additionally requires both participants to calibrate the same physical origin and +Z direction; see the [AR guide](../ar/README.md#calibrate-a-shared-physical-board).

![Multiplayer VR](./multiplayer-vr.png)

![Multiplayer AR](./multiplayer-ar.png)
