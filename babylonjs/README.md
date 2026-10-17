# Babylon.js Luminous Totem

A small Babylon.js scene with a floating emissive polyhedron, animated halo, desktop orbit camera, and Babylon's default WebXR experience UI.

## Prerequisites

- A current WebGL-capable browser.
- A compatible WebXR browser, VR runtime, and device for immersive VR; availability varies.
- Network access for Babylon.js 7.42.0 from the official Babylon.js CDN.
- A local static server. WebXR is available only in secure contexts; `localhost` qualifies for local work, while other devices need HTTPS.

## Launch

From this folder, run `python -m http.server 8003` and open <http://localhost:8003>. Orbit with pointer/touch on desktop; use the Babylon.js VR button when available.

## Limitations

- Immersive VR support depends on browser, operating system, runtime, and headset compatibility. The desktop scene remains usable when XR initialization is unavailable.
- WebXR requires a secure context. `localhost` is trusted for local development; serving to a headset over a network requires HTTPS with a trusted certificate.
- The demo uses the Babylon.js CDN and therefore needs an internet connection.

## Upstream

- [Babylon.js documentation](https://doc.babylonjs.com/)
- [Babylon.js WebXR documentation](https://doc.babylonjs.com/features/featuresDeepDive/webXR/)
- [Babylon.js repository](https://github.com/BabylonJS/Babylon.js)
