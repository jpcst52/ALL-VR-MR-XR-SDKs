import test from "node:test";
import assert from "node:assert/strict";
import { fovFromProjection, fitScale, viewShare } from "../shared/ar-fit.mjs";

// Builds a WebGL projection matrix (column-major) for a symmetric field of view, as an XRView would report it.
function projection(hDeg, vDeg, near = 0.1, far = 100) {
  const m = new Array(16).fill(0);
  m[0] = 1 / Math.tan((hDeg * Math.PI) / 360);
  m[5] = 1 / Math.tan((vDeg * Math.PI) / 360);
  m[10] = -(far + near) / (far - near);
  m[11] = -1;
  m[14] = (-2 * far * near) / (far - near);
  return m;
}

test("fovFromProjection recovers the field of view of a symmetric projection", () => {
  const f = fovFromProjection(projection(43, 29));
  assert.ok(Math.abs(f.horizontal - 43) < 0.01);
  assert.ok(Math.abs(f.vertical - 29) < 0.01);
});

test("fovFromProjection handles an off-centre projection", () => {
  // shift the window right: m[8] != 0
  const m = projection(50, 40);
  m[8] = 0.2;
  const f = fovFromProjection(m);
  assert.ok(f.horizontal > 0 && f.horizontal < 180);
  assert.ok(Math.abs(f.vertical - 40) < 0.01);
});

test("a 3 m wide layout at 1.5 m fills far more than a HoloLens-sized view", () => {
  const share = viewShare({ width: 3, height: 1, distance: 1.5, horizontalFov: 43, verticalFov: 29 });
  assert.ok(share > 2, `share ${share}`); // wider than twice the view
});

test("fitScale makes a model fill the requested share of the narrower direction", () => {
  const args = { width: 3, height: 1, distance: 1.5, horizontalFov: 43, verticalFov: 29 };
  const s = fitScale({ ...args, fill: 0.7 });
  const share = viewShare({ ...args, width: args.width * s, height: args.height * s });
  assert.ok(Math.abs(share - 0.7) < 1e-9, `share ${share}`);
});

test("fitScale scales up a model that is smaller than the view", () => {
  const s = fitScale({ width: 0.2, height: 0.2, distance: 1.5, horizontalFov: 43, verticalFov: 29, fill: 0.7 });
  assert.ok(s > 1);
});

test("fitScale rejects non-positive sizes", () => {
  assert.throws(() => fitScale({ width: 0, height: 1, distance: 1, horizontalFov: 40, verticalFov: 30 }));
});
