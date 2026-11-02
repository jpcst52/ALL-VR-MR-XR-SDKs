// Seasonal skies for the Three.js examples: a sky sphere that shows one equirectangular image per season and
// rotates every 30 seconds with a "monitor glitch" transition: the picture breaks into mosaic blocks that grow,
// scan lines and a little red/blue flicker appear for about a second, the next season swaps in at the peak, and
// the blocks shrink away. With prefers-reduced-motion it is a plain cross-fade.
//
// Everything happens in the sky's own fragment shader, in view-direction space (the mosaic quantises the
// equirect uv; the scan lines follow elevation), so it renders the same way in both eyes of a WebXR session.
// There is no screen-space post-processing.
//
// Fallbacks: a missing manifest, a bad entry, or an image that fails to load never throws; the sky simply stays
// hidden (the scene keeps its own background) or skips that season. Rotation pauses while the tab is hidden.
//
//   const sky = createSeasonSky({ manifestUrl: new URL("../assets/skies/skies.json", import.meta.url) });
//   scene.add(sky.mesh); sky.start();            // and each frame: sky.update(time, camera)
//   sky.nextSeason(); sky.label;  // "Autumn"     sky.stop(); sky.setEnabled(false); sky.dispose();
import * as THREE from "three";
import {
  normalizeManifest, startIndex, pickFile, resolveSkyUrl, transitionState, SkyClock,
  ROTATE_MS, GLITCH_MS, FADE_MS, skyCaption, creditLines,
} from "./season-sky-core.js";

const vertexShader = /* glsl */ `
  varying vec3 vDir;
  void main() {
    vDir = position;
    vec4 clip = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
    gl_Position = clip.xyww; // on the far plane: never hides scene content
  }
`;

const fragmentShader = /* glsl */ `
  uniform sampler2D uFrom;
  uniform sampler2D uTo;
  uniform float uMix;
  uniform float uPixel;
  uniform float uScan;
  uniform float uChroma;
  uniform float uTime;
  uniform float uDim;
  uniform float uYaw;
  varying vec3 vDir;
  const float PI = 3.141592653589793;

  float hash(float n) { return fract(sin(n) * 43758.5453); }

  vec3 sampleSky(vec2 uv, float split) {
    vec3 a = vec3(texture2D(uFrom, uv + vec2(split, 0.0)).r, texture2D(uFrom, uv).g, texture2D(uFrom, uv - vec2(split, 0.0)).b);
    vec3 b = vec3(texture2D(uTo, uv + vec2(split, 0.0)).r, texture2D(uTo, uv).g, texture2D(uTo, uv - vec2(split, 0.0)).b);
    return mix(a, b, uMix);
  }

  void main() {
    vec3 d = normalize(vDir);
    vec2 uv = vec2(fract(atan(d.z, d.x) / (2.0 * PI) + 0.5 + uYaw), asin(clamp(d.y, -1.0, 1.0)) / PI + 0.5);
    float elevation = uv.y;
    if (uPixel > 0.0005) {
      // Mosaic: snap the direction to a grid of cells (twice as tall in v, so cells are square at the horizon).
      vec2 cell = vec2(uPixel, uPixel * 2.0);
      uv = (floor(uv / cell) + 0.5) * cell;
      uv.y = clamp(uv.y, 0.0, 1.0);
    }
    float frame = floor(uTime * 24.0);
    float jitter = (hash(frame) - 0.5) * 2.0;
    vec3 colour = sampleSky(uv, uChroma * (0.6 + 0.4 * jitter));
    if (uScan > 0.001) {
      // Scan lines along the elevation (about two per degree), crawling slowly, plus a brightness flicker.
      float line = 0.5 + 0.5 * sin((elevation * 360.0 + uTime * 1.5) * PI);
      colour *= 1.0 - uScan * 0.45 * line;
      colour *= 1.0 + uScan * 0.18 * jitter;
      colour += vec3(0.02, 0.05, 0.09) * uScan; // a faint phosphor glow
    }
    gl_FragColor = vec4(colour * uDim, 1.0);
  }
`;

export function createSeasonSky({
  manifestUrl,
  radius = 50,
  prefer4k = false,
  reducedMotion = typeof matchMedia === "function" && matchMedia("(prefers-reduced-motion: reduce)").matches,
  interval = ROTATE_MS,
  dim = 0.82,
  yaw = 0,
  onChange = () => {},
  onReady = () => {},
} = {}) {
  const placeholder = new THREE.DataTexture(new Uint8Array([7, 20, 51, 255]), 1, 1);
  placeholder.needsUpdate = true;
  const uniforms = {
    uFrom: { value: placeholder },
    uTo: { value: placeholder },
    uMix: { value: 0 },
    uPixel: { value: 0 },
    uScan: { value: 0 },
    uChroma: { value: 0 },
    uTime: { value: 0 },
    uDim: { value: dim },
    uYaw: { value: yaw },
  };
  const material = new THREE.ShaderMaterial({
    uniforms, vertexShader, fragmentShader,
    side: THREE.BackSide, depthWrite: false, depthTest: true, fog: false, toneMapped: false,
  });
  const mesh = new THREE.Mesh(new THREE.SphereGeometry(radius, 48, 24), material);
  mesh.name = "season-sky";
  mesh.frustumCulled = false;
  mesh.renderOrder = -10;
  mesh.visible = false;
  mesh.userData.isTrainingUi = true; // no shadows

  let entries = [];
  const textures = new Map(); // index -> Promise<Texture|null>
  let clock = null;
  let running = false;
  let enabled = true;
  let loaded = false;
  let shown = -1; // index whose label is shown (switches at the swap)
  let last = null;
  let hidden = typeof document !== "undefined" && document.visibilityState === "hidden";
  let pending = false; // a transition is waiting for its next image to load
  const duration = reducedMotion ? FADE_MS : GLITCH_MS;

  const onVisibility = () => { hidden = document.visibilityState === "hidden"; last = null; };
  if (typeof document !== "undefined") document.addEventListener("visibilitychange", onVisibility);

  const loader = new THREE.TextureLoader();
  function loadTexture(index) {
    if (!textures.has(index)) {
      const entry = entries[index];
      const file = pickFile(entry, { prefer4k });
      textures.set(index, new Promise((resolve) => {
        if (!file) return resolve(null);
        loader.load(resolveSkyUrl(file, manifestUrl), (texture) => {
          texture.colorSpace = THREE.NoColorSpace; // sample the sRGB bytes as-is and write them out unchanged
          texture.generateMipmaps = false; // no mip seam at the equirect wrap; the sky is magnified anyway
          texture.minFilter = THREE.LinearFilter;
          texture.wrapS = THREE.RepeatWrapping;
          texture.wrapT = THREE.ClampToEdgeWrapping;
          texture.needsUpdate = true;
          entry.texture = texture;
          resolve(texture);
        }, undefined, () => {
          entry.failed = true;
          resolve(null);
        });
      }));
    }
    return textures.get(index);
  }

  const api = {
    mesh,
    get entries() { return entries; },
    get loaded() { return loaded; },
    get index() { return shown; },
    get season() { return entries[shown]?.season ?? null; },
    get label() { return entries[shown]?.label ?? ""; },
    get caption() { return skyCaption(entries); },
    get credits() { return creditLines(entries); },
    get running() { return running; },
    get transitioning() { return Boolean(clock?.transition); },
    ready: null,
    start() { running = true; last = null; return api; },
    stop() { running = false; return api; },
    setEnabled(value) { enabled = Boolean(value); mesh.visible = enabled && loaded; return api; },
    setDim(value) { uniforms.uDim.value = value; return api; },
    // Begin the transition to the next season now (resets the 30 s timer).
    nextSeason() {
      if (!clock || clock.transition || pending) return false;
      return queueTransition();
    },
    update(time, camera) {
      if (camera) mesh.position.copy(camera.position);
      if (!clock) return;
      const now = time ?? performance.now();
      let dt = last === null ? 0 : now - last;
      last = now;
      if (dt > 1000) dt = 0; // the tab or the headset was paused (no frames for a second): do not jump ahead
      uniforms.uTime.value = now / 1000;
      if (!running || hidden || pending) dt = 0;
      if (!clock.transition && clock.elapsed + dt >= clock.interval) {
        clock.elapsed = 0;
        queueTransition();
        dt = 0;
      }
      const state = clock.update(dt);
      if (state.transition) {
        const t = transitionState(state.transition.progress, { reducedMotion });
        uniforms.uMix.value = t.mix;
        uniforms.uPixel.value = t.pixel;
        uniforms.uScan.value = t.scan;
        uniforms.uChroma.value = t.chroma;
        if (state.swapped) setShown(state.transition.to);
      } else if (state.finished) {
        uniforms.uFrom.value = uniforms.uTo.value;
        uniforms.uMix.value = 0;
        uniforms.uPixel.value = 0;
        uniforms.uScan.value = 0;
        uniforms.uChroma.value = 0;
        setShown(state.index);
      }
    },
    // For screenshots and tests: hold the transition at progress p (0..1) towards the next season.
    preview(p) {
      if (!clock) return;
      const to = entries.length > 1 ? (clock.index + 1) % entries.length : clock.index;
      const target = entries[to]?.texture;
      if (target) uniforms.uTo.value = target;
      const t = transitionState(p, { reducedMotion });
      uniforms.uMix.value = t.mix;
      uniforms.uPixel.value = t.pixel;
      uniforms.uScan.value = t.scan;
      uniforms.uChroma.value = t.chroma;
    },
    // Jump straight to a season (no transition). Returns false if that season is not available.
    async show(season) {
      const index = entries.findIndex((entry) => entry.season === season);
      if (index < 0 || !clock || clock.transition) return false;
      const texture = await loadTexture(index);
      if (!texture) return false;
      clock.index = index;
      clock.elapsed = 0;
      uniforms.uFrom.value = texture;
      uniforms.uTo.value = texture;
      uniforms.uMix.value = 0;
      uniforms.uPixel.value = 0;
      uniforms.uScan.value = 0;
      uniforms.uChroma.value = 0;
      setShown(index);
      return true;
    },
    dispose() {
      running = false;
      if (typeof document !== "undefined") document.removeEventListener("visibilitychange", onVisibility);
      mesh.geometry.dispose();
      material.dispose();
      for (const entry of entries) entry.texture?.dispose();
      placeholder.dispose();
    },
  };

  function setShown(index) {
    if (index === shown) return;
    shown = index;
    onChange(api);
  }

  // Load the next usable image, then start the clock's transition. Seasons whose image fails are skipped.
  function queueTransition() {
    if (!clock || entries.length < 2) return false;
    pending = true;
    (async () => {
      for (let step = 1; step < entries.length; step += 1) {
        const index = (clock.index + step) % entries.length;
        const texture = await loadTexture(index);
        if (!texture) continue;
        uniforms.uTo.value = texture;
        clock.transition = { from: clock.index, to: index, time: 0, swapped: false };
        break;
      }
      pending = false;
    })();
    return true;
  }

  api.ready = (async () => {
    try {
      if (!manifestUrl) return false;
      const response = await fetch(manifestUrl, { cache: "no-cache" });
      if (!response.ok) return false;
      entries = normalizeManifest(await response.json());
      if (!entries.length) return false;
      // Start on today's season; if that image fails, try the others in order.
      const first = startIndex(entries);
      for (let step = 0; step < entries.length; step += 1) {
        const index = (first + step) % entries.length;
        const texture = await loadTexture(index);
        if (!texture) continue;
        clock = new SkyClock({ count: entries.length, index, interval, duration });
        uniforms.uFrom.value = texture;
        uniforms.uTo.value = texture;
        loaded = true;
        mesh.visible = enabled;
        setShown(index);
        break;
      }
      if (!loaded) return false;
      // Warm the rest in the background so transitions do not wait.
      entries.forEach((_, index) => { loadTexture(index); });
      onReady(api);
      return true;
    } catch {
      return false; // offline, missing, or malformed: the scene keeps its own background
    } finally {
      if (typeof document !== "undefined") document.documentElement.dataset.seasonSky = loaded ? "on" : "off";
    }
  })();

  return api;
}
