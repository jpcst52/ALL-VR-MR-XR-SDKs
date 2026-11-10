// Shared theme, layouts, and drawing helpers for the project's decks. The look follows the original decks:
// Pitt Medium Blue background, Royal rounded cards, and gold / light blue / orange accents.
import { createRequire } from "node:module";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import pptxgen from "pptxgenjs";
import sharp from "sharp";

const here = dirname(fileURLToPath(import.meta.url));
export const repo = join(here, "..", "..");
export const img = (relative) => join(repo, relative);

export const THEME = {
  name: "XR Examples",
  headFontFace: "Calibri",
  bodyFontFace: "Calibri",
  colors: {
    // University of Pittsburgh palette: Medium Blue background, Royal cards, New Sky muted text, Gold lead accent.
    dk1: "00205B", lt1: "FFFFFF", dk2: "003594", lt2: "DBEEFF",
    accent1: "FFB81C", accent2: "8DB9F2", accent3: "F58A4B", accent4: "DBEEFF", accent5: "6CCBA5", accent6: "E8636B",
    hlink: "FFB81C", folHlink: "8DB9F2",
  },
};

export async function applyDeckTheme(file) {
  // apply_theme.js locates pptxgenjs/jszip relative to itself, so point Node at this folder's modules.
  process.env.NODE_PATH = join(here, "node_modules");
  createRequire(import.meta.url)("node:module").Module._initPaths();
  const skill = process.env.PPTX_SKILL_DIR;
  if (!skill) throw new Error("Set PPTX_SKILL_DIR to the pptx skill folder (it contains scripts/apply_theme.js).");
  const { applyTheme } = createRequire(import.meta.url)(join(skill, "scripts", "apply_theme.js"));
  await applyTheme(file, THEME);
}

export function newDeck({ title, footer }) {
  const pres = new pptxgen();
  pres.layout = "LAYOUT_WIDE"; // 13.33 x 7.5 in
  pres.title = title;
  pres.author = "xr-training-lab";
  pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
  const C = pres.SchemeColor;
  pres.defineSlideMaster({
    title: "TITLE",
    background: { color: C.text1 },
    objects: [
      { placeholder: { options: { name: "eyebrow", type: "body", x: 0.7, y: 0.9, w: 6.2, h: 0.35, fontSize: 13, bold: true, color: C.accent1, margin: 0, align: "left" }, text: "" } },
      { placeholder: { options: { name: "title", type: "title", x: 0.7, y: 1.4, w: 6.4, h: 2.2, fontSize: 54, bold: true, color: C.background1, margin: 0, valign: "top", align: "left" }, text: "" } },
    ],
  });
  pres.defineSlideMaster({
    title: "CONTENT",
    background: { color: C.text1 },
    objects: [
      { text: { text: footer, options: { x: 0.6, y: 7.03, w: 8, h: 0.3, fontSize: 10, color: C.background2, margin: 0 } } },
      { placeholder: { options: { name: "eyebrow", type: "body", x: 0.6, y: 0.32, w: 9, h: 0.3, fontSize: 12, bold: true, color: C.accent1, margin: 0, align: "left" }, text: "" } },
      { placeholder: { options: { name: "title", type: "title", x: 0.6, y: 0.64, w: 12.1, h: 0.75, fontSize: 30, bold: true, color: C.background1, margin: 0, valign: "top", align: "left" }, text: "" } },
    ],
    slideNumber: { x: 12.1, y: 7.03, w: 0.65, h: 0.3, color: C.background2, fontSize: 10, align: "right" },
  });
  return { pres, C };
}

// ------------------------------------------------------------------ drawing helpers

export function section(pres, name) {
  pres.addSection({ title: name });
}

export function contentSlide(pres, { section: sectionTitle, eyebrow, title, notes }) {
  const slide = pres.addSlide({ masterName: "CONTENT", sectionTitle });
  slide.addText(eyebrow, { placeholder: "eyebrow" });
  slide.addText(title, { placeholder: "title" });
  if (notes) slide.addNotes(notes);
  return slide;
}

export function card(slide, C, { x, y, w, h, name = "Card", fill = C.text2 }) {
  slide.addShape("roundRect", {
    x, y, w, h, rectRadius: 0.14, fill: { color: fill }, line: { color: C.background2, width: 0.75, transparency: 78 }, objectName: name,
  });
}

export function text(slide, str, o) {
  slide.addText(str, { isTextBox: true, margin: 0, valign: "top", fontFace: "Calibri", ...o });
}

export function label(slide, C, str, { x, y, w, color, name = "Label" }) {
  text(slide, str, { x, y, w, h: 0.28, fontSize: 11, bold: true, color: color ?? C.accent1, charSpacing: 1, objectName: name });
}

export function bullets(slide, C, items, { x, y, w, h, size = 14, color, gap = 6, name = "Bullets" }) {
  const runs = items.map((item, index) => ({
    text: item,
    options: { bullet: { indent: 16 }, breakLine: index < items.length - 1, paraSpaceAfter: gap },
  }));
  text(slide, runs, { x, y, w, h, fontSize: size, color: color ?? C.background1, objectName: name });
}

export function chip(slide, C, str, { x, y, w, color, name = "Chip" }) {
  slide.addShape("roundRect", { x, y, w, h: 0.42, rectRadius: 0.21, fill: { color: C.text2 }, line: { color: color ?? C.accent1, width: 1 }, objectName: `${name} frame` });
  text(slide, str, { x, y, w, h: 0.42, fontSize: 12, bold: true, color: color ?? C.accent1, align: "center", valign: "middle", objectName: name });
}

export function numberDot(slide, C, n, { x, y, color, name = "Step number" }) {
  slide.addShape("ellipse", { x, y, w: 0.42, h: 0.42, fill: { color: color ?? C.accent1 }, line: { color: color ?? C.accent1, width: 0 }, objectName: `${name} circle` });
  text(slide, String(n), { x, y, w: 0.42, h: 0.42, fontSize: 14, bold: true, color: C.text1, align: "center", valign: "middle", objectName: name });
}

// Numbered steps down a column. Returns the y after the last step.
export function steps(slide, C, items, { x, y, w, gap = 0.78, color, size = 14 }) {
  items.forEach((item, index) => {
    numberDot(slide, C, index + 1, { x, y: y + index * gap, color });
    text(slide, item, { x: x + 0.62, y: y + index * gap - 0.02, w: w - 0.62, h: gap - 0.06, fontSize: size, color: C.background1, valign: "middle", objectName: `Step ${index + 1}` });
  });
  return y + items.length * gap;
}

export function code(slide, C, str, { x, y, w, h, size = 14, name = "Code" }) {
  slide.addShape("roundRect", { x, y, w, h, rectRadius: 0.1, fill: { color: C.text1 }, line: { color: C.background2, width: 0.75, transparency: 70 }, objectName: `${name} frame` });
  text(slide, str, { x: x + 0.2, y: y + 0.12, w: w - 0.4, h: h - 0.24, fontSize: size, fontFace: "Courier New", color: C.accent1, valign: "middle", objectName: name });
}

export function callout(slide, C, str, { x, y, w, h = 0.6, color, name = "Callout" }) {
  slide.addShape("roundRect", { x, y, w, h, rectRadius: 0.1, fill: { color: C.text2 }, line: { color: color ?? C.accent1, width: 1, transparency: 40 }, objectName: `${name} frame` });
  text(slide, str, { x: x + 0.25, y, w: w - 0.5, h, fontSize: 13, bold: true, color: C.background1, valign: "middle", objectName: name });
}

// A framed screenshot: the picture is contained inside the frame and a caption sits underneath it.
export async function shot(slide, C, file, { x, y, w, h, caption, name = "Screenshot", crop }) {
  let source = sharp(file);
  if (crop) source = source.extract(crop);
  const buffer = await source.resize({ width: 1600, withoutEnlargement: true }).png({ compressionLevel: 9 }).toBuffer();
  const meta = await sharp(buffer).metadata();
  const captionH = caption ? 0.34 : 0;
  slide.addShape("roundRect", { x, y, w, h, rectRadius: 0.12, fill: { color: C.text2 }, line: { color: C.background2, width: 0.75, transparency: 75 }, objectName: `${name} frame` });
  const boxW = w - 0.2;
  const boxH = h - 0.2 - captionH;
  const scale = Math.min(boxW / meta.width, boxH / meta.height);
  const iw = meta.width * scale;
  const ih = meta.height * scale;
  slide.addImage({
    data: `image/png;base64,${buffer.toString("base64")}`, x: x + (w - iw) / 2, y: y + 0.1 + (boxH - ih) / 2, w: iw, h: ih,
    altText: caption ?? name, objectName: name,
  });
  if (caption) {
    text(slide, caption, { x: x + 0.15, y: y + h - captionH - 0.06, w: w - 0.3, h: captionH, fontSize: 10.5, color: C.background2, align: "center", valign: "middle", objectName: `${name} caption` });
  }
}

// A list of "name + description" entries inside one text box.
export function entries(slide, C, items, { x, y, w, h, nameColor, nameSize = 14, descSize = 12, gap = 8, name = "Entries" }) {
  const runs = [];
  items.forEach(([title, detail], index) => {
    const last = index === items.length - 1;
    runs.push({ text: title, options: { bold: true, fontSize: nameSize, color: nameColor ?? C.background1, breakLine: true } });
    runs.push({ text: detail, options: { fontSize: descSize, color: C.background2, breakLine: !last, paraSpaceAfter: gap } });
  });
  text(slide, runs, { x, y, w, h, objectName: name });
}
