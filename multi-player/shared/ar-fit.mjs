// Field-of-view maths for small see-through displays such as the HoloLens 2.
// Pure functions, no browser needed, so they run in the unit tests.
//
// Why this exists: a see-through headset shows a much smaller window of the world than a VR headset. The HoloLens 2
// is usually quoted at about 43 x 29 degrees (52 diagonal); a Quest is roughly 100+ degrees wide. Content that fills a
// Quest view at 1:1 scale can fill only a small part of the HoloLens view. These helpers measure the real field of
// view from the XR projection matrices and size a model so it fills a chosen share of it.

/** Half-angle tangents from a WebGL projection matrix (column-major, as in XRView.projectionMatrix). */
export function tangentsFromProjection(m) {
  // m[0] = 2n/(r-l), m[5] = 2n/(t-b), m[8] = (r+l)/(r-l), m[9] = (t+b)/(t-b)
  const left = (m[8] - 1) / m[0];
  const right = (m[8] + 1) / m[0];
  const bottom = (m[9] - 1) / m[5];
  const top = (m[9] + 1) / m[5];
  return { left, right, bottom, top };
}

const deg = (rad) => (rad * 180) / Math.PI;

/** Horizontal and vertical field of view in degrees for one eye. */
export function fovFromProjection(m) {
  const t = tangentsFromProjection(m);
  return {
    horizontal: deg(Math.atan(t.right) - Math.atan(t.left)),
    vertical: deg(Math.atan(t.top) - Math.atan(t.bottom)),
  };
}

/**
 * Scale to apply to a model so its largest on-screen extent fills `fill` (0 to 1) of the narrower field of view at
 * `distance` metres. `size` is the model's current width and height in metres (depth is ignored: the nearest face
 * sets the apparent size, so pass the larger of width and depth as `width` for a deep model).
 */
export function fitScale({ width, height, distance, horizontalFov, verticalFov, fill = 0.7 }) {
  if (!(width > 0) || !(height > 0) || !(distance > 0)) throw new Error("width, height and distance must be positive");
  const availW = 2 * distance * Math.tan((horizontalFov * Math.PI) / 360) * fill;
  const availH = 2 * distance * Math.tan((verticalFov * Math.PI) / 360) * fill;
  return Math.min(availW / width, availH / height);
}

/** Share of the field of view (0 to 1, the larger of the two directions) that a model of this size fills. */
export function viewShare({ width, height, distance, horizontalFov, verticalFov }) {
  const w = width / (2 * distance * Math.tan((horizontalFov * Math.PI) / 360));
  const h = height / (2 * distance * Math.tan((verticalFov * Math.PI) / 360));
  return Math.max(w, h);
}

/** One line per view for an on-screen or console diagnostic: what the headset really reports. */
export function describeViews(views, baseLayer) {
  const lines = [];
  views.forEach((view, i) => {
    const f = fovFromProjection(view.projectionMatrix);
    const vp = baseLayer?.getViewport?.(view);
    lines.push(`view ${i} (${view.eye}): fov ${f.horizontal.toFixed(0)} x ${f.vertical.toFixed(0)} deg` + (vp ? `, viewport ${vp.width} x ${vp.height}` : ""));
  });
  if (baseLayer) lines.push(`framebuffer ${baseLayer.framebufferWidth} x ${baseLayer.framebufferHeight}`);
  return lines;
}
