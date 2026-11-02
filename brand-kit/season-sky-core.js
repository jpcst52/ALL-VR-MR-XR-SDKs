// Seasonal skies: the pure parts (no Three.js, no DOM), so they can be unit-tested in Node.
// See season-sky.js for the sky sphere and README.md for how a page uses it.
//
// Manifest contract (assets/skies/skies.json): an array of
//   { id, label, file4k, file2k, credit, isBradford }   (optional: source URL, license)
// File paths are relative to the manifest (e.g. "spring-2k.jpg") or to the repository root (e.g.
// "assets/skies/spring-2k.jpg"). An object { skies: [...] } is accepted too.

export const SEASONS = ["spring", "summer", "autumn", "winter"];
export const SEASON_LABELS = { spring: "Spring", summer: "Summer", autumn: "Autumn", winter: "Winter" };
export const ROTATE_MS = 30000; // one season every 30 seconds
export const GLITCH_MS = 1100; // pixelate + scan lines + flicker, swap at the peak (about one second)
export const FADE_MS = 1500; // plain cross-fade when the viewer prefers reduced motion

// Meteorological seasons (northern hemisphere): Mar-May spring, Jun-Aug summer, Sep-Nov autumn, Dec-Feb winter.
export function seasonForDate(date = new Date()) {
  const month = date.getMonth(); // 0 = January
  if (month >= 2 && month <= 4) return "spring";
  if (month >= 5 && month <= 7) return "summer";
  if (month >= 8 && month <= 10) return "autumn";
  return "winter";
}

// The season an entry shows, from its id or label ("fall" counts as autumn). null if none is named.
export function seasonOf(entry) {
  const text = `${entry?.id ?? ""} ${entry?.label ?? ""}`.toLowerCase();
  if (/\bfall\b|autumn/.test(text)) return "autumn";
  return SEASONS.find((season) => text.includes(season)) ?? null;
}

// Usable entries in season order (spring, summer, autumn, winter). Entries without an image or a recognisable
// season are dropped, and only the first entry per season is kept. Never throws: bad input gives [].
export function normalizeManifest(json) {
  const list = Array.isArray(json) ? json : Array.isArray(json?.skies) ? json.skies : [];
  const bySeason = new Map();
  for (const entry of list) {
    if (!entry || typeof entry !== "object") continue;
    const season = seasonOf(entry);
    if (!season || bySeason.has(season)) continue;
    if (typeof entry.file2k !== "string" && typeof entry.file4k !== "string") continue;
    bySeason.set(season, {
      id: String(entry.id ?? season),
      season,
      label: SEASON_LABELS[season],
      title: typeof entry.label === "string" && entry.label.trim() ? entry.label.trim() : SEASON_LABELS[season],
      file2k: typeof entry.file2k === "string" ? entry.file2k : null,
      file4k: typeof entry.file4k === "string" ? entry.file4k : null,
      credit: typeof entry.credit === "string" ? entry.credit.trim() : "",
      source: typeof entry.source === "string" && /^https?:\/\//.test(entry.source) ? entry.source : null,
      license: typeof entry.license === "string" ? entry.license : null,
      isBradford: entry.isBradford === true,
    });
  }
  return SEASONS.filter((season) => bySeason.has(season)).map((season) => bySeason.get(season));
}

// Index of the entry for today's season, or the next available season after it, or 0.
export function startIndex(entries, date = new Date()) {
  if (!entries.length) return -1;
  const today = SEASONS.indexOf(seasonForDate(date));
  for (let step = 0; step < SEASONS.length; step += 1) {
    const index = entries.findIndex((entry) => entry.season === SEASONS[(today + step) % SEASONS.length]);
    if (index >= 0) return index;
  }
  return 0;
}

export const nextIndex = (index, count) => (count > 0 ? (index + 1) % count : -1);

// The 2k file on the web; the 4k file only when asked for (desktop) and present. Falls back to whichever exists.
export const pickFile = (entry, { prefer4k = false } = {}) => (prefer4k ? entry.file4k ?? entry.file2k : entry.file2k ?? entry.file4k);

// Resolve an image path from the manifest: "assets/..." is taken from the repository root, anything else from
// the manifest's folder. Absolute and data: URLs are kept.
export function resolveSkyUrl(file, manifestUrl) {
  const base = new URL(manifestUrl);
  if (/^[a-z]+:/i.test(file)) return file;
  if (/^\.?\/?assets\//.test(file)) return new URL(file.replace(/^\.?\//, ""), new URL("../../", base)).href;
  return new URL(file, base).href;
}

// Wording for the page. Never says the images are of Bradford unless the manifest says so (isBradford).
export function skyCaption(entries) {
  if (!entries.length) return "";
  if (entries.every((entry) => entry.isBradford)) return "Seasonal skies photographed in Bradford, PA";
  if (entries.some((entry) => entry.isBradford)) return "Seasonal skies: some from Bradford, PA, the rest regional style";
  return "Seasonal skies, regional style (not photos of Bradford)";
}

export function creditLines(entries) {
  return entries.map((entry) => `${entry.label}${entry.isBradford ? " (Bradford, PA)" : ""}: ${entry.credit || "credit not given in the manifest"}`);
}

const clamp01 = (value) => Math.min(1, Math.max(0, value));
const smooth = (value) => value * value * (3 - 2 * value);

// Shader uniforms at transition progress p (0..1). The glitch peaks at p = 0.5, where the image swaps.
//   mix: 0 shows the old image, 1 the new one     pixel: mosaic cell size (fraction of the image width)
//   scan: scan-line strength                      chroma: red/blue split (fraction of the image width)
export function transitionState(p, { reducedMotion = false, maxPixel = 1 / 56 } = {}) {
  const t = clamp01(p);
  if (reducedMotion) return { mix: smooth(t), pixel: 0, scan: 0, chroma: 0 };
  const glitch = Math.sin(Math.PI * t); // 0 -> 1 at the swap -> 0
  return {
    mix: smooth(clamp01((t - 0.46) / 0.08)), // a hard cut hidden under the biggest blocks
    pixel: maxPixel * Math.pow(glitch, 1.6),
    scan: 0.55 * glitch,
    chroma: 0.004 * glitch,
  };
}

// The rotation clock: call update(dt) every frame with the visible time that passed. It reports when a
// transition is running and when the image swaps. Pausing is the caller's job (pass dt = 0 while hidden).
export class SkyClock {
  constructor({ count = 0, index = 0, interval = ROTATE_MS, duration = GLITCH_MS } = {}) {
    Object.assign(this, { count, index, interval, duration, elapsed: 0, transition: null });
  }

  // Start a transition now (if there is somewhere to go and none is running).
  trigger() {
    if (this.count < 2 || this.transition) return false;
    this.transition = { from: this.index, to: nextIndex(this.index, this.count), time: 0, swapped: false };
    return true;
  }

  // Returns { index, transition: null | { from, to, progress }, swapped: boolean (this frame), finished }.
  update(dt) {
    let swapped = false;
    let finished = false;
    if (this.count >= 2) {
      if (!this.transition) {
        this.elapsed += Math.max(0, dt);
        if (this.elapsed >= this.interval) {
          this.elapsed = 0;
          this.trigger();
        }
      } else {
        this.transition.time += Math.max(0, dt);
        if (!this.transition.swapped && this.transition.time >= this.duration / 2) {
          this.transition.swapped = true;
          swapped = true;
        }
        if (this.transition.time >= this.duration) {
          this.index = this.transition.to;
          this.transition = null;
          this.elapsed = 0;
          finished = true;
        }
      }
    }
    const transition = this.transition && {
      from: this.transition.from,
      to: this.transition.to,
      progress: clamp01(this.transition.time / this.duration),
    };
    return { index: this.index, transition, swapped, finished };
  }
}
