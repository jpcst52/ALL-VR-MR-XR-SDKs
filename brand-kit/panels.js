// In-world UI kit for the Three.js examples: every piece of text in a 3D scene is drawn on a backed panel
// (rounded, bordered, with an accent bar) in the bundled Pitt fonts, never as bare floating text.
// Used by training-simulations/ and multi-player/. Offline: fonts from ../vendor/fonts, no network.
import * as THREE from "three";
import { loadLogo, deptName, deptShort } from "./brand.js";
import { fitText } from "./layout.js";

export const PITT = { royal: "#003594", gold: "#FFB81C", navy: "#00205B", sky: "#DBEEFF" };
const SANS = "Roboto, 'Segoe UI', system-ui, sans-serif";
const SERIF = "Merriweather, Georgia, serif";
const DISPLAY = "'League Gothic', Impact, 'Arial Narrow', sans-serif";
const PANEL_TOP = "#0d2a66";
const PANEL_BOTTOM = "#061a45";
const EDGE = "rgba(219, 238, 255, 0.42)";

// Load the fonts through the FontFace API so canvas drawing can use them (CSS @font-face alone loads lazily).
const fontDir = new URL("../vendor/fonts/", import.meta.url);
export const fontsReady = typeof FontFace === "undefined" ? Promise.resolve() : Promise.all([
  ["Roboto", "500", "roboto-latin-500-normal"],
  ["Roboto", "700", "roboto-latin-700-normal"],
  ["Merriweather", "700", "merriweather-latin-700-normal"],
  ["League Gothic", "400", "league-gothic-latin-400-normal"],
].map(([family, weight, file]) => {
  const face = new FontFace(family, `url(${new URL(`${file}.woff2`, fontDir).href})`, { weight });
  document.fonts.add(face);
  return face.load().catch(() => null);
})).then(() => undefined);

let logo = null;
export const logoReady = loadLogo().then((image) => (logo = image));
// Redraws a texture once fonts and the optional logo are ready (the first draw may use fallback fonts).
export const brandAssetsReady = Promise.all([fontsReady, logoReady]);

function canvas2d(width, height) {
  const canvas = document.createElement("canvas");
  canvas.width = Math.round(width);
  canvas.height = Math.round(height);
  return { canvas, context: canvas.getContext("2d") };
}

function measureWith(context, weight, family = SANS) {
  return (text, size) => {
    context.font = `${weight} ${size}px ${family}`;
    return context.measureText(text).width;
  };
}

function texture(canvas) {
  const map = new THREE.CanvasTexture(canvas);
  map.colorSpace = THREE.SRGBColorSpace;
  map.anisotropy = 4;
  return map;
}

// A backed panel: navy gradient, light edge, rounded corners, and an accent bar on the top or left.
export function drawPanel(context, width, height, { accent = PITT.gold, radius, bar = "top", fill } = {}) {
  const r = radius ?? Math.min(width, height) * 0.14;
  const inset = Math.max(2, Math.round(Math.min(width, height) * 0.012));
  context.clearRect(0, 0, width, height);
  context.save();
  context.beginPath();
  context.roundRect(inset, inset, width - inset * 2, height - inset * 2, r);
  if (fill) {
    context.fillStyle = fill;
  } else {
    const gradient = context.createLinearGradient(0, 0, 0, height);
    gradient.addColorStop(0, PANEL_TOP);
    gradient.addColorStop(1, PANEL_BOTTOM);
    context.fillStyle = gradient;
  }
  context.fill();
  context.clip();
  context.fillStyle = accent;
  const thick = Math.max(6, Math.round(Math.min(width, height) * 0.045));
  if (bar === "top") context.fillRect(0, 0, width, thick);
  else if (bar === "left") context.fillRect(0, 0, thick, height);
  context.restore();
  context.beginPath();
  context.roundRect(inset, inset, width - inset * 2, height - inset * 2, r);
  context.lineWidth = inset * 1.6;
  context.strokeStyle = EDGE;
  context.stroke();
  return { inset, thick, radius: r };
}

// The brand band: logo if present (git-ignored file), otherwise a text wordmark; CIST on the right.
function drawBrandBand(context, x, y, width, height) {
  context.save();
  context.beginPath();
  context.rect(x, y, width, height);
  context.clip();
  context.fillStyle = PITT.royal;
  context.fillRect(x, y, width, height);
  const pad = height * 0.16;
  let left = x + pad * 1.4;
  if (logo) {
    const h = height - pad * 1.2;
    const w = (logo.naturalWidth / logo.naturalHeight) * h;
    context.drawImage(logo, left, y + (height - h) / 2, w, h);
    left += w;
  } else {
    // Text wordmark, scaled so it never runs into the CIST mark (at most ~48% of the band).
    const room = (width - pad * 2.8) * 0.48;
    const text = "University of Pittsburgh";
    context.font = `700 100px ${SERIF}`;
    const size = Math.min(height * 0.34, (100 * room) / context.measureText(text).width);
    context.fillStyle = "#ffffff";
    context.textBaseline = "middle";
    context.textAlign = "left";
    context.font = `700 ${Math.round(size)}px ${SERIF}`;
    context.fillText(text, left, y + height * 0.36);
    const w1 = context.measureText(text).width;
    context.font = `700 ${Math.round(size * 0.86)}px ${SERIF}`;
    context.fillStyle = PITT.gold;
    context.letterSpacing = `${Math.round(size * 0.12)}px`;
    context.fillText("BRADFORD", left, y + height * 0.72);
    context.letterSpacing = "0px";
    left += w1;
  }
  // divider between the university mark and the department
  context.fillStyle = "rgba(255, 255, 255, 0.55)";
  context.fillRect(left + pad * 0.9, y + height * 0.2, Math.max(2, height * 0.02), height * 0.6);
  left += pad * 1.8;
  const right = x + width - pad * 1.4;
  context.textAlign = "right";
  context.textBaseline = "middle";
  context.fillStyle = PITT.gold;
  context.font = `400 ${Math.round(height * 0.62)}px ${DISPLAY}`;
  context.fillText(deptShort, right, y + height * 0.5);
  const cistWidth = context.measureText(deptShort).width;
  const spare = right - cistWidth - left - pad * 1.2;
  if (spare > height * 2.2) {
    context.fillStyle = PITT.sky;
    context.font = `500 ${Math.round(height * 0.22)}px ${SANS}`;
    context.textAlign = "left";
    context.fillText(deptName, left, y + height * 0.52, spare);
  }
  context.restore();
  context.fillStyle = PITT.gold;
  context.fillRect(x, y + height - Math.max(3, height * 0.05), width, Math.max(3, height * 0.05));
}

// Kicker + wrapped body text on a backed panel. Text shrinks to fit and never draws outside the border.
// Options: brand (adds the Pitt-Bradford/CIST band at the top), align ("left" | "center").
export function drawTextPanel(context, width, height, { kicker = "", body = "", accent = PITT.gold, size = 40, minSize = 18, brand = false, align = "left", bar = "top", bodyWeight = 500 } = {}) {
  const { thick } = drawPanel(context, width, height, { accent, bar: brand ? "none" : bar });
  const pad = Math.round(Math.min(width, height) * 0.09);
  let top = bar === "top" && !brand ? thick + pad * 0.7 : pad * 0.85;
  if (brand) {
    const band = Math.round(Math.max(56, height * 0.22));
    context.save();
    context.beginPath();
    const inset = Math.max(2, Math.round(Math.min(width, height) * 0.012));
    context.roundRect(inset, inset, width - inset * 2, height - inset * 2, Math.min(width, height) * 0.14);
    context.clip();
    drawBrandBand(context, 0, 0, width, band);
    context.restore();
    top = band + pad * 0.6;
  }
  const left = bar === "left" ? thick + pad * 0.8 : pad;
  const maxWidth = width - left - pad;
  const x = align === "center" ? left + maxWidth / 2 : left;
  context.textAlign = align;
  context.textBaseline = "top";
  if (kicker) {
    const kickerSize = Math.round(Math.max(minSize * 0.8, size * 0.56));
    context.font = `700 ${kickerSize}px ${SANS}`;
    context.letterSpacing = `${Math.round(kickerSize * 0.12)}px`;
    context.fillStyle = accent;
    context.fillText(String(kicker).toUpperCase(), x, top, maxWidth);
    context.letterSpacing = "0px";
    top += kickerSize * 1.55;
  }
  const fit = fitText(body, { measure: measureWith(context, bodyWeight), maxWidth, maxHeight: height - top - pad * 0.8, size, minSize });
  context.font = `${bodyWeight} ${fit.size}px ${SANS}`;
  context.fillStyle = "#ffffff";
  fit.lines.forEach((line, index) => context.fillText(line, x, top + index * fit.size * 1.24, maxWidth));
  return fit;
}

export function textPanelTexture(options = {}) {
  const { width = 1024, height = 384 } = options;
  const { canvas, context } = canvas2d(width, height);
  drawTextPanel(context, width, height, options);
  const map = texture(canvas);
  map.userData = { redraw: () => { drawTextPanel(context, width, height, options); map.needsUpdate = true; } };
  return map;
}

// A menu card with a 16:9 picture on the left and a kicker + title on the right (the scenario catalog in VR).
// `image` is an loaded <img>/<canvas> or null: without it the picture area shows a tinted placeholder tile, so a
// missing thumbnail never looks broken. The title is large and shrinks to fit; the card never draws outside itself.
export function drawThumbCard(context, width, height, { kicker = "", title = "", accent = PITT.gold, image = null, size = 60, minSize = 34 } = {}) {
  const { thick } = drawPanel(context, width, height, { accent, bar: "left", radius: height * 0.12 });
  const pad = Math.round(height * 0.075);
  // The picture takes about 42% of the card width (16:9), vertically centred; the text gets the rest.
  const picW = Math.round(Math.min(width * 0.42, ((height - pad * 2) * 16) / 9));
  const picH = Math.round((picW * 9) / 16);
  const picX = thick + pad * 0.8;
  const picY = Math.round((height - picH) / 2);
  const r = picH * 0.1;
  context.save();
  context.beginPath();
  context.roundRect(picX, picY, picW, picH, r);
  context.clip();
  if (image) {
    // Cover: fill the 16:9 box, cropping the image if its aspect differs.
    const iw = image.naturalWidth ?? image.width;
    const ih = image.naturalHeight ?? image.height;
    const scale = Math.max(picW / iw, picH / ih);
    const dw = iw * scale;
    const dh = ih * scale;
    context.drawImage(image, picX + (picW - dw) / 2, picY + (picH - dh) / 2, dw, dh);
  } else {
    const gradient = context.createLinearGradient(picX, picY, picX + picW, picY + picH);
    gradient.addColorStop(0, "#0b2a6b");
    gradient.addColorStop(1, PITT.navy);
    context.fillStyle = gradient;
    context.fillRect(picX, picY, picW, picH);
    context.globalAlpha = 0.35;
    context.fillStyle = accent;
    context.beginPath();
    context.arc(picX + picW / 2, picY + picH / 2, picH * 0.22, 0, Math.PI * 2);
    context.fill();
    context.globalAlpha = 1;
  }
  context.restore();
  context.beginPath();
  context.roundRect(picX, picY, picW, picH, r);
  context.lineWidth = Math.max(2, height * 0.008);
  context.strokeStyle = "rgba(219, 238, 255, 0.35)";
  context.stroke();

  const left = picX + picW + pad;
  const maxWidth = width - left - pad;
  let top = pad * 1.05;
  context.textAlign = "left";
  context.textBaseline = "top";
  if (kicker) {
    const kickerSize = Math.round(Math.max(minSize * 0.8, size * 0.58));
    context.font = `700 ${kickerSize}px ${SANS}`;
    context.letterSpacing = `${Math.round(kickerSize * 0.08)}px`;
    context.fillStyle = accent;
    context.fillText(String(kicker).toUpperCase(), left, top, maxWidth);
    context.letterSpacing = "0px";
    top += kickerSize * 1.5;
  }
  const fit = fitText(title, { measure: measureWith(context, 700), maxWidth, maxHeight: height - top - pad, size, minSize });
  context.font = `700 ${fit.size}px ${SANS}`;
  context.fillStyle = "#ffffff";
  fit.lines.forEach((line, index) => context.fillText(line, left, top + index * fit.size * 1.18, maxWidth));
  return fit;
}

export function thumbCardTexture(options = {}) {
  const { width = 820, height = 400 } = options;
  const { canvas, context } = canvas2d(width, height);
  drawThumbCard(context, width, height, options);
  return texture(canvas);
}

// A plane mesh with a text panel. Redraws itself once fonts/logo are ready.
export function textPanelMesh(options = {}, [w, h] = [1, 0.4]) {
  const map = textPanelTexture(options);
  const mesh = new THREE.Mesh(
    new THREE.PlaneGeometry(w, h),
    new THREE.MeshBasicMaterial({ map, transparent: true, toneMapped: false, side: THREE.DoubleSide }),
  );
  mesh.userData.isTrainingUi = true;
  brandAssetsReady.then(() => map.userData.redraw());
  return mesh;
}

// Small status glyph drawn with paths (no font dependency): check, cross, or dot.
function drawGlyph(context, kind, cx, cy, r) {
  context.save();
  context.strokeStyle = PITT.navy;
  context.fillStyle = PITT.navy;
  context.lineWidth = r * 0.32;
  context.lineCap = "round";
  context.lineJoin = "round";
  context.beginPath();
  if (kind === "done") {
    context.moveTo(cx - r * 0.45, cy + r * 0.02);
    context.lineTo(cx - r * 0.1, cy + r * 0.38);
    context.lineTo(cx + r * 0.5, cy - r * 0.35);
    context.stroke();
  } else if (kind === "wrong") {
    context.moveTo(cx - r * 0.38, cy - r * 0.38);
    context.lineTo(cx + r * 0.38, cy + r * 0.38);
    context.moveTo(cx + r * 0.38, cy - r * 0.38);
    context.lineTo(cx - r * 0.38, cy + r * 0.38);
    context.stroke();
  } else {
    context.arc(cx, cy, r * 0.26, 0, Math.PI * 2);
    context.fill();
  }
  context.restore();
}

// A name tag / hotspot label: a pill that hugs its text, with a coloured status chip on the left.
// status: "idle" | "done" | "wrong" | null (no chip). Returns a Sprite whose height in metres is `worldHeight`.
export function tagSprite(text, { accent = PITT.gold, status = "idle", worldHeight = 0.24, depthTest = false, maxChars = 40 } = {}) {
  const height = 128;
  const fontSize = 50;
  const probe = canvas2d(8, 8).context;
  const label = String(text ?? "").slice(0, maxChars);
  const measure = measureWith(probe, 700);
  const chip = status ? height * 0.56 : 0;
  const padX = 34;
  const textWidth = Math.min(measure(label, fontSize), 900);
  const width = Math.ceil(padX * 2 + textWidth + (chip ? chip + 22 : 0) + 8);
  const { canvas, context } = canvas2d(width, height);
  const draw = () => {
    drawPanel(context, width, height, { accent, radius: height * 0.3, bar: "left" });
    context.lineWidth = 5;
    context.strokeStyle = accent;
    context.beginPath();
    context.roundRect(4, 4, width - 8, height - 8, height * 0.3);
    context.stroke();
    let x = padX;
    if (chip) {
      context.fillStyle = accent;
      context.beginPath();
      context.arc(x + chip / 2, height / 2, chip / 2, 0, Math.PI * 2);
      context.fill();
      drawGlyph(context, status, x + chip / 2, height / 2, chip / 2);
      x += chip + 22;
    }
    context.fillStyle = "#ffffff";
    context.textAlign = "left";
    context.textBaseline = "middle";
    context.font = `700 ${fontSize}px ${SANS}`;
    context.fillText(label, x, height / 2 + 3, width - x - padX + 4);
  };
  draw();
  const map = texture(canvas);
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map, transparent: true, depthTest, toneMapped: false }));
  sprite.scale.set((worldHeight * width) / height, worldHeight, 1);
  sprite.userData.isTrainingUi = true;
  sprite.userData.worldWidth = (worldHeight * width) / height;
  fontsReady.then(() => { draw(); map.needsUpdate = true; });
  return sprite;
}

// Text printed on a physical sign or board (not floating): centred lines in the brand sans, scaled to fit.
export function drawSignLines(context, lines, { fg = "#16212e", maxSize = 54, weight = 700, family = SANS } = {}) {
  const { width, height } = context.canvas;
  const list = lines.filter((line) => line !== undefined && line !== null).map(String);
  if (!list.length) return;
  let size = Math.min(maxSize, (height / (list.length + 0.6)) * 0.8);
  context.font = `${weight} ${Math.round(size)}px ${family}`;
  const widest = Math.max(...list.map((line) => context.measureText(line).width));
  if (widest > width * 0.9) size *= (width * 0.9) / widest;
  context.font = `${weight} ${Math.round(size)}px ${family}`;
  context.fillStyle = fg;
  context.textAlign = "center";
  context.textBaseline = "middle";
  list.forEach((line, index) => context.fillText(line, width / 2, (height / (list.length + 1)) * (index + 1), width * 0.92));
}

// A small labelled plate (rounded, bordered, accent bar) for station parts, e.g. "LUNGS".
export function plateTexture(lines, { width = 512, height = 128, accent = PITT.gold, fg = "#ffffff" } = {}) {
  const { canvas, context } = canvas2d(width, height);
  const draw = () => {
    drawPanel(context, width, height, { accent, bar: "left", radius: Math.min(width, height) * 0.22 });
    context.save();
    context.translate(Math.round(Math.min(width, height) * 0.05), 0);
    drawSignLines(context, lines, { fg, maxSize: 64 });
    context.restore();
  };
  draw();
  const map = texture(canvas);
  fontsReady.then(() => { draw(); map.needsUpdate = true; });
  return map;
}

// Pitt-Bradford + CIST plaque for a scene. Shows the logo when the git-ignored file is present, else a wordmark.
// Sets <html data-brand-3d="logo|wordmark"> so automated checks can see which one was drawn.
export function brandPlate([w, h] = [1.6, 0.34]) {
  const width = 1024;
  const height = Math.round((width * h) / w);
  const { canvas, context } = canvas2d(width, height);
  const draw = () => {
    drawPanel(context, width, height, { accent: PITT.gold, bar: "none", fill: PITT.royal, radius: height * 0.18 });
    context.save();
    context.beginPath();
    context.roundRect(3, 3, width - 6, height - 6, height * 0.18);
    context.clip();
    drawBrandBand(context, 0, 0, width, height);
    context.restore();
    context.lineWidth = 6;
    context.strokeStyle = PITT.gold;
    context.beginPath();
    context.roundRect(3, 3, width - 6, height - 6, height * 0.18);
    context.stroke();
    if (typeof document !== "undefined") document.documentElement.dataset.brand3d = logo ? "logo" : "wordmark";
  };
  draw();
  const map = texture(canvas);
  const mesh = new THREE.Mesh(
    new THREE.PlaneGeometry(w, h),
    new THREE.MeshBasicMaterial({ map, transparent: true, toneMapped: false, side: THREE.DoubleSide }),
  );
  mesh.userData.isTrainingUi = true;
  mesh.name = "pitt-brand-plate";
  brandAssetsReady.then(() => { draw(); map.needsUpdate = true; });
  return mesh;
}
