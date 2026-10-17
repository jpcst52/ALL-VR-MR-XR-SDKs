# WebXR Color Field

A small, framework-free demo using the browser's WebXR Device API directly. It shows an animated fallback preview and, when available, opens an immersive VR session that clears each XR view with a changing color.

## Prerequisites

- A current browser that implements WebXR immersive VR (for example, a compatible Chromium-based browser).
- A WebXR-capable VR device/runtime for headset use. Browser and device support varies.
- A local static web server. WebXR requires a secure context: `http://localhost` is treated as trustworthy for local development; access from another device generally requires HTTPS.

## Launch

From this folder, run `python -m http.server 8000`, then open <http://localhost:8000> in the browser. Click **Enter VR** from the page to request the session; session requests must be triggered by a user gesture.

## Limitations

- Immersive VR support differs by browser, operating system, runtime, and headset. Unsupported browsers still show the preview.
- WebXR is restricted to secure contexts. `localhost` works for local development; serving to a headset over a network requires HTTPS with a certificate trusted by that device.
- This intentionally minimal sample renders only a changing clear color, not a 3D scene or controller input.

## Upstream

- [WebXR Device API specification](https://immersive-web.github.io/webxr/)
- [MDN: WebXR Device API](https://developer.mozilla.org/docs/Web/API/WebXR_Device_API)
