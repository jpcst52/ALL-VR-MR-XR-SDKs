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

`TRAINING_URL` and `CIRCUITS_URL` environment variables override the default addresses. `CAPTURE_ONLY=why` captures only the "Why this matters" panel and the reasoning question in the hydrostatic test (`training-simulations/screenshots/vr/08` to `10`).

## What this is and is not

- It is a faithful check of layout, scale, readability, and controller interaction logic in a headset-style view with a Quest-3-like field of view.
- It is **not** a recording from a real headset. There is no stereo rendering, lens distortion, controller 3D model, passthrough, comfort evaluation, or performance measurement. The controller appears only as its pointer ray, which is how the examples draw it.
- Browsers differ from the Quest Browser. Final verification should still be done on hardware.
- Only immersive VR is captured. Immersive AR (hit testing against a real room) is not emulated here, so the AR example's screenshots remain desktop fallbacks.

## How the scripted controller aims

`capture.mjs` computes a quaternion that rotates the controller's forward axis (`-Z`) onto the direction of a target point, then sets the emulated controller's position and orientation. Target positions are derived from the same hotspot coordinates used by the lab, transformed by the offset and scale the page applies to its content in VR (`vrWorldOffset` and `vrWorldScale` in `training-simulations/app.js`). If you change those values, update `vrPoints()` in `capture.mjs` to match.

## Look-and-feel checks and README screenshots (desktop and phone)

Two more scripts in this folder use the same Playwright setup. Both need the site and the relay on one origin; the simplest is the relay itself, which serves the repository root:

```powershell
# multi-player/ (plain HTTP on a free port, so it does not clash with a demo relay on 8443)
$env:PORT = 8132; node -e "import('./server.mjs').then(m => m.createRelay().listen({ port: 8132, host: '127.0.0.1' }))"
```

- `node ui-check.mjs` checks the launcher, the Practice Lab (welcome, catalog, a task, feedback, a result, the Circulation Builder) and the three multiplayer pages at 1440 x 900 and 390 x 844. For each it fails on: external requests, page or console errors, a broken `<img>`, a missing or unsettled Pitt-Bradford + CIST brand bar, a missing in-world brand plaque, horizontal overflow, brand fonts not loaded, the panel leaving the viewport, or buttons under 36 px. Each page is loaded with `branding/` blocked (as in a fresh clone) and, if `branding/logo.png` exists, allowed. `--shots <dir>` saves screenshots; `--only <name>` limits the pages. `UI_BASE` overrides `http://localhost:8132/`.
- `node readme-shots.mjs [lab|mp|why]` re-shoots (`why` only takes the four "Why this matters" and reasoning-question shots, 09 to 12) the desktop screenshots in `training-simulations/screenshots/` and `multi-player/screenshots/`. `SHOTS_BASE` overrides the address.
- `node thumbs.mjs` makes the Practice Lab card thumbnails (`training-simulations/thumbs/<scenario id>.jpg`, 320 x 180, plus `index.json`) by cropping the work area out of the `03-scenario-*` README screenshots (left of the page panel, below the in-scene sign, so no brand band or logo). No server needed. Re-run it after `readme-shots.mjs lab`.
- `node sky-shots.mjs` re-shoots the desktop thumbnail and seasonal-sky screenshots in `training-simulations/screenshots/skies/` (menu, one frame per season, the transition early and at its peak, the reduced-motion cross-fade, an outdoor and an indoor scenario, phone width). It holds the rotation still through `window.__seasonSky` so frames are repeatable.
- `CAPTURE_ONLY=menu node capture.mjs` takes only the Practice Lab menu in the emulated Quest 3 (`01-catalog.png`, `02-pointing-at-a-scenario.png`, `11-menu-sky-transition.png`).
- `ui-check.mjs` also fails a Practice Lab page if the seasonal sky did not load (`<html data-season-sky="on">`), and the catalog if any card is missing its thumbnail.

All the capture scripts (`capture.mjs`, `ui-check.mjs` screenshots meant for READMEs, `readme-shots.mjs`, `sky-shots.mjs`) block the git-ignored `branding/` folder by default, so committed screenshots show the text wordmark, never the university logo. Set `CAPTURE_LOGO=1` for `capture.mjs` to include it in a local-only capture.

`offline-check.mjs` takes `STATIC_BASE` and `RELAY_BASE` (defaults `http://localhost:8000/` and `http://localhost:8443/`), also checks the launcher and the Hydrostatic Test Rig, and allows the optional `branding/logo.png` 404.

`mp-check.mjs` opens two desktop and one phone-sized browser context in the same room of the real relay for each multiplayer example (Circuit Lab, Site Safety Layout, Hydrostatic Test Rig) and checks sync, drag ownership, armed placing, keyboard moves, control sizes, avatar spacing, an emulated Quest participant, Leave room, a missing relay, a full room and an automatic reconnect after a relay restart. It starts its own relay on `MP_PORT` (default 8179), or uses `MP_BASE`; `ONLY=cl|ss|ht` runs one example. `demos-sdk-check.mjs` (`STATIC_BASE`) probes the four single-SDK demos with an emulated Quest 3: requested session features, eye height, session end, resize and WebGL context loss. Both use desktop Chrome and an emulator, not a headset.


`v2-check.mjs` is the Lab v2 version of `toolbox-check.mjs`: the same emulated-Quest-3 checks against `multi-player/v2/` (toolbox, examples, Y / B / X, reset, gloves) plus v2-only ones (environment map and PBR materials, frames drawn, a constant light count, current dots follow the switch, terminal rings, bulb and beacon lights, a render-cost budget). Run `node tools/vr-capture/v2-check.mjs` (56 checks). Not a headset test. Screenshots and cost numbers for v2 come from `node multi-player/tools/v2-shots.mjs`.
