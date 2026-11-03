// Orbit Garden: custom A-Frame components + the page glue. A-Frame itself (../vendor/aframe) provides the scene graph,
// input (mouse/gaze/laser controllers), lighting and the WebXR session.
const THREE = AFRAME.THREE;
const statusEl = document.querySelector("#status");
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

function radialTexture(stops, size = 512) {
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const context = canvas.getContext("2d");
  const gradient = context.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  stops.forEach(([at, color]) => gradient.addColorStop(at, color));
  context.fillStyle = gradient;
  context.fillRect(0, 0, size, size);
  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

// A round lawn with a soft edge that blends into the photo sky, plus a brass rim.
AFRAME.registerComponent("garden-floor", {
  init() {
    const lawn = new THREE.Mesh(
      new THREE.CircleGeometry(7, 96),
      new THREE.MeshStandardMaterial({ map: radialTexture([[0, "#3d6b4f"], [0.55, "#2c5440"], [0.8, "#1e3a33"], [1, "#16302f"]]), roughness: 0.95, transparent: true }),
    );
    lawn.rotation.x = -Math.PI / 2;
    lawn.receiveShadow = true;
    const rim = new THREE.Mesh(new THREE.TorusGeometry(3.9, 0.04, 10, 128), new THREE.MeshStandardMaterial({ color: "#ffb81c", metalness: 1, roughness: 0.3 }));
    rim.rotation.x = Math.PI / 2;
    rim.position.y = 0.02;
    this.el.object3D.add(lawn, rim);
  },
});

AFRAME.registerComponent("stone-ring", {
  schema: { count: { default: 12 }, radius: { default: 4 } },
  init() {
    const geometry = new THREE.CylinderGeometry(0.13, 0.17, 0.22, 8);
    const material = new THREE.MeshStandardMaterial({ color: "#8d99ae", roughness: 0.85 });
    for (let i = 0; i < this.data.count; i += 1) {
      const angle = (i / this.data.count) * Math.PI * 2;
      const stone = new THREE.Mesh(geometry, material);
      stone.position.set(Math.cos(angle) * this.data.radius, 0.11, Math.sin(angle) * this.data.radius);
      stone.scale.set(1, 0.7 + ((i * 37) % 5) * 0.12, 1);
      stone.rotation.y = i;
      stone.castShadow = stone.receiveShadow = true;
      this.el.object3D.add(stone);
    }
  },
});

// Slow drifting points of light.
AFRAME.registerComponent("fireflies", {
  schema: { count: { default: 100 } },
  init() {
    const { count } = this.data;
    const positions = new Float32Array(count * 3);
    this.seeds = new Float32Array(count * 2);
    for (let i = 0; i < count; i += 1) {
      const radius = 1.5 + Math.random() * 5, angle = Math.random() * Math.PI * 2;
      positions.set([Math.cos(angle) * radius, 0.3 + Math.random() * 3.5, Math.sin(angle) * radius - 3], i * 3);
      this.seeds.set([Math.random() * 6.28, 0.2 + Math.random() * 0.5], i * 2);
    }
    this.base = positions.slice();
    this.geometry = new THREE.BufferGeometry();
    this.geometry.setAttribute("position", new THREE.BufferAttribute(positions, 3));
    const material = new THREE.PointsMaterial({ map: radialTexture([[0, "rgba(255,255,255,1)"], [1, "rgba(255,255,255,0)"]], 64), size: 0.09, color: "#ffe9a8", transparent: true, depthWrite: false, blending: THREE.AdditiveBlending });
    this.el.object3D.add(new THREE.Points(this.geometry, material));
  },
  tick(time) {
    if (reducedMotion) return;
    const attribute = this.geometry.attributes.position;
    for (let i = 0; i < this.data.count; i += 1) {
      const [phase, speed] = [this.seeds[i * 2], this.seeds[i * 2 + 1]];
      attribute.setXYZ(i, this.base[i * 3] + Math.sin(time * 0.0004 * speed + phase) * 0.5, this.base[i * 3 + 1] + Math.sin(time * 0.0007 * speed + phase * 2) * 0.25, this.base[i * 3 + 2] + Math.cos(time * 0.0005 * speed + phase) * 0.5);
    }
    attribute.needsUpdate = true;
  },
});

// One lantern: the CC0 brass diya lantern model, a glowing shape above it, and a light that colours the garden.
AFRAME.registerComponent("lantern", {
  schema: { color: { default: "#ffffff" }, shape: { default: "sphere" } },
  init() {
    const { color, shape } = this.data;
    const model = document.createElement("a-entity");
    model.setAttribute("gltf-model", "#lantern-model");
    model.setAttribute("scale", "2 2 2");
    model.setAttribute("position", "0 -0.56 0");
    model.setAttribute("shadow", "cast: true; receive: true");
    model.addEventListener("model-error", () => {
      console.warn("Lantern model did not load; using a primitive instead.");
      const fallback = document.createElement("a-cylinder");
      fallback.setAttribute("radius", "0.18");
      fallback.setAttribute("height", "0.3");
      fallback.setAttribute("color", "#c9a14a");
      fallback.setAttribute("material", "metalness: 0.8; roughness: 0.35");
      this.el.appendChild(fallback);
      model.remove();
    });
    this.el.appendChild(model);

    const orb = document.createElement(shape === "sphere" ? "a-sphere" : shape === "octahedron" ? "a-octahedron" : "a-torus-knot");
    orb.setAttribute("position", "0 0.55 0");
    if (shape === "knot") { orb.setAttribute("radius", "0.12"); orb.setAttribute("radius-tubular", "0.035"); } else orb.setAttribute("radius", "0.15");
    orb.setAttribute("material", `shader: standard; color: ${color}; emissive: ${color}; emissiveIntensity: 0.75; roughness: 0.3`);
    this.orb = orb;
    this.el.appendChild(orb);
    const light = document.createElement("a-entity");
    light.setAttribute("light", `type: point; color: ${color}; intensity: 0.6; distance: 6; decay: 2`);
    light.setAttribute("position", "0 -0.2 0");
    this.el.appendChild(light);

    if (!reducedMotion) {
      const motion = { sphere: "property: object3D.position.y; to: 0.45; dir: alternate; dur: 1800; loop: true; easing: easeInOutSine", octahedron: "property: rotation; to: 360 360 0; loop: true; dur: 7000; easing: linear", knot: "property: scale; dir: alternate; dur: 1500; loop: true; to: 1.3 1.3 1.3; easing: easeInOutSine" }[shape];
      orb.setAttribute("animation", motion);
    }
  },
});

// A readable panel in the world (canvas texture, bundled Roboto/League Gothic), so the selection is visible in VR too.
AFRAME.registerComponent("info-panel", {
  init() {
    this.canvas = document.createElement("canvas");
    this.canvas.width = 1024;
    this.canvas.height = 320;
    this.texture = new THREE.CanvasTexture(this.canvas);
    this.texture.colorSpace = THREE.SRGBColorSpace;
    const mesh = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 0.75), new THREE.MeshBasicMaterial({ map: this.texture, transparent: true, toneMapped: false }));
    this.el.object3D.add(mesh);
    this.draw("Orbit Garden", "Select a lantern to light it.", "#ffb81c");
    document.fonts?.ready.then(() => this.draw(this.title, this.body, this.accent));
  },
  draw(title, body, accent) {
    Object.assign(this, { title, body, accent });
    const c = this.canvas.getContext("2d");
    const { width, height } = this.canvas;
    c.clearRect(0, 0, width, height);
    const g = c.createLinearGradient(0, 0, 0, height);
    g.addColorStop(0, "#0d2a66"); g.addColorStop(1, "#061a45");
    c.fillStyle = g;
    c.beginPath(); c.roundRect(4, 4, width - 8, height - 8, 36); c.fill();
    c.lineWidth = 5; c.strokeStyle = "rgba(219,238,255,.5)"; c.stroke();
    c.fillStyle = accent; c.fillRect(34, 34, 14, height - 68);
    c.fillStyle = "#ffb81c"; c.font = "700 30px Roboto, sans-serif"; c.textBaseline = "top";
    c.fillText("ORBIT GARDEN", 78, 40);
    c.fillStyle = "#ffffff"; c.font = "400 112px 'League Gothic', Impact, sans-serif";
    c.fillText(title.toUpperCase(), 78, 82);
    c.fillStyle = "#dbeeff"; c.font = "400 38px Merriweather, Georgia, serif";
    c.fillText(body, 78, 230, width - 130);
    this.texture.needsUpdate = true;
  },
});

// Page glue: selection from the cursor, the gaze fuse, VR controllers, the buttons and the 1-2-3 keys.
const lanterns = [...document.querySelectorAll(".lantern")];
const buttons = [...document.querySelectorAll("[data-lantern]")];
const panel = document.querySelector("#readout");
let selected = -1;
function select(index) {
  selected = index;
  lanterns.forEach((lantern, i) => {
    const on = i === index;
    lantern.object3D.scale.setScalar(on ? 1.3 : 1);
    const orb = lantern.components.lantern?.orb;
    if (orb) orb.setAttribute("material", "emissiveIntensity", on ? 1.6 : 0.75);
    buttons[i]?.setAttribute("aria-pressed", String(on));
  });
  const { name, note } = lanterns[index].dataset;
  const accent = lanterns[index].getAttribute("lantern").color;
  statusEl.textContent = `${name} selected. ${note}`;
  panel.components["info-panel"]?.draw(name, note, accent);
}
lanterns.forEach((lantern, index) => lantern.addEventListener("click", () => select(index)));
buttons.forEach((button) => button.addEventListener("click", () => select(Number(button.dataset.lantern))));
window.addEventListener("keydown", (event) => {
  if (event.target instanceof HTMLElement && event.target.closest("button, summary")) return;
  if (["1", "2", "3"].includes(event.key)) select(Number(event.key) - 1);
});

const scene = document.querySelector("a-scene");
// Image-based lighting from the same sky photo, so the glass and brass of the lanterns have something to reflect.
function applyEnvironment() {
  const image = document.querySelector("#sky");
  const make = () => {
    const texture = new THREE.Texture(image);
    texture.mapping = THREE.EquirectangularReflectionMapping;
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.needsUpdate = true;
    const generator = new THREE.PMREMGenerator(scene.renderer);
    scene.object3D.environment = generator.fromEquirectangular(texture).texture;
    scene.object3D.environmentIntensity = 0.9;
  };
  if (image.complete && image.naturalWidth) make(); else image.addEventListener("load", make, { once: true });
}
const ready = () => {
  applyEnvironment();
  statusEl.textContent = "Orbit Garden is ready. Select a lantern to learn about it.";
  window.__demo = { ready: true, select };
};
if (scene.hasLoaded) ready(); else scene.addEventListener("loaded", ready);
window.__demo = { ready: false, select };
