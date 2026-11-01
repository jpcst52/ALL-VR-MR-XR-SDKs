# VR capture tool

Takes screenshots of the browser XR examples running inside an **emulated Meta Quest 3**, so you can see (and show) roughly how they look in a headset without owning one.

It uses Meta's [Immersive Web Emulation Runtime (IWER)](https://github.com/meta-quest/immersive-web-emulation-runtime) injected into headless Chrome. The pages really call `navigator.xr.requestSession("immersive-vr")` and render through their normal WebXR code path; a script then moves an emulated right controller, points it at scene objects, and presses the trigger so the examples' `select` handlers run exactly as they would from a real controller.

## Run it

Prerequisites: Node.js 20+, Google Chrome, and the example servers running.

```powershell
# repository root
python -m http.server 8000

# multi-player/ (separate terminal)
npm install
npm start

# tools/vr-capture/ (separate terminal)
npm install
npm run capture
```

Captures are written to:

- `training-simulations/screenshots/vr/` — the scenario catalog in VR, pointing at a card, a hands-on task, selecting hotspots, task completion, a wrong action, and the electrical lockout.
- `multi-player/screenshots/vr/` — Circuit Lab in VR with a second participant.

`TRAINING_URL` and `CIRCUITS_URL` environment variables override the default addresses.

## What this is and is not

- It is a faithful check of layout, scale, readability, and controller interaction logic in a headset-style view with a Quest-3-like field of view.
- It is **not** a recording from a real headset. There is no stereo rendering, lens distortion, controller 3D model, passthrough, comfort evaluation, or performance measurement. The controller appears only as its pointer ray, which is how the examples draw it.
- Browsers differ from the Quest Browser. Final verification should still be done on hardware.
- Only immersive VR is captured. Immersive AR (hit testing against a real room) is not emulated here, so the AR example's screenshots remain desktop fallbacks.

## How the scripted controller aims

`capture.mjs` computes a quaternion that rotates the controller's forward axis (`-Z`) onto the direction of a target point, then sets the emulated controller's position and orientation. Target positions are derived from the same hotspot coordinates used by the lab, transformed by the offset and scale the page applies to its content in VR (`vrWorldOffset` and `vrWorldScale` in `training-simulations/app.js`). If you change those values, update `vrPoints()` in `capture.mjs` to match.
