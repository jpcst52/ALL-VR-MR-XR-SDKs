# A-Frame Orbit Garden

A single-file A-Frame scene with animated lantern shapes, a walkable ground, and standard A-Frame VR entry controls.

## Prerequisites

- A modern browser with WebGL.
- For headset VR, a compatible WebXR browser and VR runtime/device; support varies by browser and platform.
- Network access to load the pinned A-Frame CDN script.
- A local static server. Secure contexts are required for WebXR; `localhost` qualifies, while access over a network requires HTTPS.

## Launch

From this folder, run `python -m http.server 8001` and open <http://localhost:8001>. Use the VR headset icon supplied by A-Frame to enter VR when supported.

## Limitations

- A-Frame's VR button depends on browser/device WebXR support and an available runtime. Without that, the scene remains viewable as a desktop WebGL scene.
- WebXR device compatibility varies. `localhost` is secure for local development; remote/headset access needs HTTPS and a certificate trusted by the device.
- A-Frame 1.6.0 is loaded from its public CDN, so an internet connection is needed.

## Upstream

- [A-Frame documentation](https://aframe.io/docs/1.6.0/introduction/)
- [A-Frame repository](https://github.com/aframevr/aframe)
- [WebXR Device API](https://developer.mozilla.org/docs/Web/API/WebXR_Device_API)
