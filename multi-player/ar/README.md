# Multiplayer AR — Waterworks Workshop

A browser-based, collaborative WebXR augmented-reality workshop. AR participants place the shared virtual workbench relative to real surfaces, then add or move shared tank, valve, pipe, and pump parts. Desktop browser participants can join the same room and see or edit the same layout.

This is a WebXR/WebSocket teaching example, not a native ARCore or ARKit app.

## Run it

From `multi-player/`, run:

```powershell
npm install
npm test
npm start
```

Open `http://localhost:8443/multi-player/ar/`, enter a name, and join the same room as collaborators. To let a WebXR-capable mobile device join over the network, configure HTTPS and a certificate trusted by the device; follow the [parent server instructions](../README.md#connect-a-headset-or-another-computer).

## Calibrate a shared physical board

WebXR hit testing places the board against real surfaces, but individual devices do not automatically share one world origin. For participants in the same physical location:

1. Set two visible markers on a horizontal floor or tabletop: a shared origin point and a second point that defines the board's +Z direction.
2. Each AR participant enters the same room and selects **Align origin & enter AR**.
3. Point at and tap the same physical origin marker, then point at and tap the second marker.
4. When alignment completes, the shared virtual board is mapped to that device's local tracking space.
5. Add or manipulate parts. Other AR clients that performed the same alignment should see the board in the same physical area; VR/desktop clients see the same layout in the virtual work surface.

Use clearly visible markers and keep devices at the same site. If participants tap different points or reverse the +Z direction, objects still synchronize but may be in different physical positions or orientations.

## What it demonstrates

- A real `immersive-ar` session request with WebXR hit testing and a visible placement reticle.
- A two-point local calibration that maps server-shared board coordinates to each device's own AR space.
- The same server room, part IDs, presence, and object ownership rules used by the VR sibling example.
- Cross-device shared transforms and valve state with a desktop editing fallback.

## Limitations

WebXR `immersive-ar` and hit testing require a compatible mobile browser, device, and HTTPS secure context (except `localhost`). The local coordinate calibration is not a persistent cloud anchor or platform-shared spatial anchor; it needs the same two physical reference points at each session. There is no automatic room scan, collision detection, occlusion mesh, saved layout, account system, or production service authentication.

See the [parent README](../README.md) for the server, room model, test coverage, secure hosting, and project references.

See the [AR screenshot](../screenshots/multiplayer-ar.png) for an annotated desktop fallback preview, and the [combined slide deck](../Multiplayer-WebXR-Examples.pptx) for the shared setup and AR calibration walkthrough. The screenshot is not an immersive camera-passthrough capture.
