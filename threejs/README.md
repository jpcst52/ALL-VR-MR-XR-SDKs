# Three.js Prism Bloom

A compact Three.js scene with a faceted animated prism, a luminous orbit ring, desktop orbit controls, and Three.js's WebXR VR entry button.

## Prerequisites

- A modern browser with WebGL and JavaScript modules.
- For immersive VR, a compatible WebXR browser, VR runtime, and device; support varies.
- Network access to load the pinned Three.js 0.169.0 CDN modules.
- A local static server. WebXR requires a secure context: `localhost` works locally; other devices need HTTPS.

## Launch

From this folder, run `python -m http.server 8002` and open <http://localhost:8002>. Drag to orbit on desktop. Select **ENTER VR** when it appears to request an immersive session.

## Limitations

- The VR button and immersive session require browser, OS, runtime, and headset WebXR compatibility.
- WebXR requires a secure context. `localhost` qualifies for local development; network access from a headset requires HTTPS with a trusted certificate.
- CDN modules require an internet connection; no bundler or local dependency installation is included.

## Upstream

- [Three.js documentation](https://threejs.org/docs/)
- [Three.js WebXR guide](https://threejs.org/manual/#en/webxr-basics)
- [Three.js repository](https://github.com/mrdoob/three.js)
