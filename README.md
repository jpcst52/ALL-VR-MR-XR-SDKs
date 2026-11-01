# VR, MR, and AR SDK Examples

Small, separate examples that highlight a distinctive capability of each technology in [SDK-list.md](SDK-list.md). Each example uses the named SDK or framework directly; a browser demo is not a substitute for a native SDK integration.

## Examples

| Folder | Technology | Example |
|---|---|---|
| `openxr/` | OpenXR in Unity | OpenXR instance lifecycle callback |
| `meta-xr/` | Meta XR SDK | Quest passthrough camera preview |
| `steamvr/` | SteamVR Unity plugin | Action-driven color toggle |
| `webxr/` | WebXR Device API | Immersive color field with desktop fallback |
| `a-frame/` | A-Frame | Animated Orbit Garden |
| `threejs/` | Three.js | Prism Bloom with VR entry |
| `babylonjs/` | Babylon.js | Luminous Totem with XR entry |
| `arcore-android/` | Google ARCore | Plane detection and cube placement |
| `apple-spatial/` | ARKit / visionOS | SwiftUI spatial atlas |
| `unity-xri/` | Unity XR Interaction Toolkit | Grab selection color feedback |
| `multi-player/vr/` | Three.js + WebXR | Multiplayer collaborative VR waterworks |
| `multi-player/ar/` | Three.js + WebXR | Multiplayer shared AR layout with hit-test alignment |

Each folder has its own README with setup instructions, prerequisites, and platform limitations. Unity examples are Unity-ready source folders rather than full Unity projects: copy the scripts into a compatible Unity project and follow that example's setup instructions.

## Getting started

For the browser demos, start a static server from the repository root with `python -m http.server 8000`, then open one of these pages:

- `http://localhost:8000/a-frame/`
- `http://localhost:8000/threejs/`
- `http://localhost:8000/babylonjs/`
- `http://localhost:8000/webxr/`

Use `Ctrl+C` to stop the server. Network access is needed for the pinned CDN libraries; immersive XR additionally requires a compatible device/browser and a secure context.

## Multiplayer examples

The two multiplayer WebXR examples share a small WebSocket relay. In a terminal, run:

```powershell
cd multi-player
npm install
npm test
npm start
```

Open `http://localhost:8443/multi-player/vr/` and `http://localhost:8443/multi-player/ar/`; clients who join the same room see and edit shared parts. Network-connected headsets/phones require HTTPS with a trusted certificate. See the [multiplayer setup guide](multi-player/README.md) for detailed run steps, AR alignment, relay behavior, and limitations.

The [run guide slide deck](Run-the-XR-Examples.pptx) walks through the launch steps and learning goal for the original ten examples. The [multiplayer deck](multi-player/Multiplayer-WebXR-Examples.pptx) covers the shared VR and AR experiences. Browse the [multiplayer screenshots](multi-player/screenshots/README.md) for desktop captures and the AR fallback preview; the [original screenshots](screenshots/) cover the other interactive browser scenes.

## Browser examples

The four browser examples include desktop interactions so their scenes can be explored without a headset. WebGL and CDN access are needed to see the rendered scenes. Screenshots of the browser fallbacks are collected in [screenshots/](screenshots/).

| Example | Desktop interaction |
|---|---|
| A-Frame | Gaze-select a lantern to enlarge it and show its name. |
| Babylon.js | Cycle the totem's emissive color and watch orbiting beacons. |
| Three.js | Cycle the prism glow and explore orbiting satellites. |
| WebXR | Adjust the color-field animation speed in the preview and immersive session. |
| Multiplayer VR | Join a room, see remote participants, and place or grab shared waterworks parts. |
| Multiplayer AR | Calibrate a shared board origin using hit testing, then edit the shared layout. |

The multiplayer examples require the separate Node.js relay described in [`multi-player/`](multi-player/); they do not work as static pages alone.

## Platform notes

- WebXR immersive sessions require a compatible browser/device and a secure context; `localhost` is suitable for local development.
- Unity and mobile examples may require the relevant SDK packages, a supported runtime, and compatible hardware.
- The Apple example requires macOS and Xcode to build or test. It cannot be built from Windows.
- Do not commit private signing keys, access tokens, or app credentials. Check third-party SDK terms before publishing.

## Repository status

This repository is initialized locally. To publish it, create an empty GitHub repository and add it as a remote before pushing.
