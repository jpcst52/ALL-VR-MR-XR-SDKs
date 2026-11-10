# HoloLens and other see-through displays: why content looks small

Written 2026-11-03 after WaterWorks (the course example in `jcallinan/CIST1423-Fall2026-Examples`, `Week-08-WebXR-WaterWorks`) filled only about half of the HoloLens view. The causes below are ranked guesses from the code and the display's published properties; the diagnostic in section 1 tells you which one is real.

## What I read in WaterWorks

It is a Meta Immersive Web SDK project (Three.js, ECS). `scene.js` makes a 70 degree camera (ignored once a session starts: the headset supplies the projection), a pixel ratio of `devicePixelRatio`, and hides the floor and grid when the session is not opaque. The workspace is built at 1 unit = 1 metre for a Quest-style room. I did not find any scaling or field-of-view handling for small displays.

## Likely causes, in the order I would check them

1. **The display really is small.** The HoloLens 2 is usually quoted at about 43 x 29 degrees (52 diagonal). A Quest is more than twice as wide. A workspace that fills a Quest view at arm's length fills well under 60 % of a HoloLens view at the same distance. This is the most likely cause and is not a bug.
2. **Dark colours disappear.** The HoloLens is an additive display: black and very dark pixels are transparent. WaterWorks draws on dark navy (the floor is `0x1d2733`) and dark metal parts; those parts look missing, so the model seems smaller than it is. Use bright, saturated, high-contrast colours, avoid thin dark outlines, and add a light backing panel behind text.
3. **Content is too far or too wide.** Anything wider than about 0.9 m at 1.5 m does not fit horizontally. Place the workspace 1.2 to 1.5 m ahead at chest height and make the main interactive area no wider than about 0.8 m.
4. **Near clipping.** Reaching in closer than the near plane removes the front of parts. Keep the camera near plane at 0.05 m or more and avoid rendering closer than about 0.5 m, which is also where HoloLens comfort guidance puts the limit.
5. **Frame rate or resolution scaling.** The HoloLens 2 has a mobile GPU. If the page asks for 72 Hz (`updateTargetFrameRate(72)` in `scene.js`) the browser will reject it (the code logs and continues). A heavy scene can still run well below 60 Hz and judder; reduce `framebufferScaleFactor` or the part count.

## 1. Measure it first (two minutes in the headset)

`multi-player/shared/ar-fit.mjs` can print what the headset reports. Call it once per session start:

```js
import { describeViews } from "./ar-fit.mjs";
renderer.xr.addEventListener("sessionstart", () => {
  const session = renderer.xr.getSession();
  session.requestAnimationFrame((_, frame) => {
    const pose = frame.getViewerPose(renderer.xr.getReferenceSpace());
    console.log(describeViews(pose.views, session.renderState.baseLayer).join("\n"), session.environmentBlendMode);
  });
});
```

It prints each eye's real field of view (for example `fov 43 x 29 deg`), the viewport and framebuffer sizes, and the blend mode (`additive` on a HoloLens). If the field of view is about 43 x 29, cause 1 applies and the fix is to size the content for it.

## 2. Fit the content to the display

Once the field of view is known, size the workspace so it fills a chosen share (0.7 works well) at a chosen distance:

```js
import { fitScale } from "./ar-fit.mjs";
const fov = fovFromProjection(pose.views[0].projectionMatrix);   // from ar-fit.mjs
const scale = fitScale({ width: 1.6, height: 0.9, distance: 1.3, horizontalFov: fov.horizontal, verticalFov: fov.vertical, fill: 0.7 });
workspaceRoot.scale.setScalar(Math.min(scale, 1));
workspaceRoot.position.set(0, 1.25, -1.3);   // 1.3 m ahead of the start pose
```

Offer **bigger / smaller** and **recenter** buttons as well, because hands and voice are the only input on a HoloLens and a person can fix a bad placement faster than a formula can.

## 3. Other things to design for

- No controllers: use hand rays and pinch (the Hand Tracking and Pointer features of the SDK). Test pinch targets at 3 cm or larger.
- Keep text at 1.5 cm or more per line at 1.3 m, on a bright panel.
- Skip shadows, post-processing and large transparent surfaces; they cost frames and look wrong on an additive display.
- Do not require the `hit-test` feature: request it as optional and fall back to a fixed placement.

## What the repository has

- `multi-player/shared/ar-fit.mjs`: the field-of-view and fit maths, unit-tested (`multi-player/test/ar-fit.test.mjs`, 6 tests, run in `npm test` there).
- The calibrated Site Safety Layout AR example (`multi-player/ar/`) is full-scale on purpose (it is aligned to real markers), so it is **not** auto-scaled. It will look small or cropped on a HoloLens for the reasons above; a table-top mode would be a separate piece of work.
