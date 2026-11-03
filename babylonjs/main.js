const canvas = document.querySelector("#renderCanvas");
const statusEl = document.querySelector("#status");
const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const engine = new BABYLON.Engine(canvas, true, { preserveDrawingBuffer: true, stencil: true, adaptToDeviceRatio: true });

const SKY_URL = new URL("../assets/skies/winter-2k.jpg", document.baseURI).href; // Poly Haven "Passendorf Snow", CC0

const palette = [
  { name: "Teal", color: new BABYLON.Color3(0.05, 0.62, 0.75), glow: new BABYLON.Color3(0.1, 0.85, 0.95) },
  { name: "Violet", color: new BABYLON.Color3(0.5, 0.25, 0.85), glow: new BABYLON.Color3(0.7, 0.4, 1) },
  { name: "Ember", color: new BABYLON.Color3(0.9, 0.35, 0.12), glow: new BABYLON.Color3(1, 0.5, 0.2) },
  { name: "Green", color: new BABYLON.Color3(0.1, 0.7, 0.4), glow: new BABYLON.Color3(0.25, 0.95, 0.55) },
];

// A small canvas-drawn panel (used in the world so the current hue is readable inside a headset).
function drawPanel(texture, title, body, accent) {
  const { width, height } = texture.getSize();
  const c = texture.getContext();
  c.fillStyle = "#061a45"; c.fillRect(0, 0, width, height);
  const g = c.createLinearGradient(0, 0, 0, height);
  g.addColorStop(0, "#0d2a66"); g.addColorStop(1, "#061a45");
  c.fillStyle = g;
  c.beginPath(); c.roundRect(4, 4, width - 8, height - 8, 36); c.fill();
  c.lineWidth = 5; c.strokeStyle = "rgba(219,238,255,.5)"; c.stroke();
  c.fillStyle = accent; c.fillRect(34, 34, 14, height - 68);
  c.textBaseline = "top";
  c.fillStyle = "#ffb81c"; c.font = "700 30px Roboto, sans-serif"; c.fillText("LUMINOUS TOTEM", 78, 40);
  c.fillStyle = "#ffffff"; c.font = "400 112px 'League Gothic', Impact, sans-serif"; c.fillText(title.toUpperCase(), 78, 82);
  c.fillStyle = "#dbeeff"; c.font = "400 38px Merriweather, Georgia, serif"; c.fillText(body, 78, 230, width - 130);
  texture.update();
}

function glowTexture(scene, name) {
  const texture = new BABYLON.DynamicTexture(name, 128, scene, true);
  const c = texture.getContext();
  const g = c.createRadialGradient(64, 64, 0, 64, 64, 64);
  g.addColorStop(0, "rgba(255,255,255,1)"); g.addColorStop(0.35, "rgba(255,255,255,.35)"); g.addColorStop(1, "rgba(255,255,255,0)");
  c.fillStyle = g; c.fillRect(0, 0, 128, 128);
  texture.update();
  texture.hasAlpha = true;
  return texture;
}

async function createScene() {
  const scene = new BABYLON.Scene(engine);
  scene.clearColor = new BABYLON.Color4(0.03, 0.08, 0.17, 1);
  const world = new BABYLON.TransformNode("world", scene);

  // Tone mapping and a touch of contrast for the whole image.
  scene.imageProcessingConfiguration.toneMappingEnabled = true;
  scene.imageProcessingConfiguration.toneMappingType = BABYLON.ImageProcessingConfiguration.TONEMAPPING_ACES;
  scene.imageProcessingConfiguration.exposure = 1.0;
  scene.imageProcessingConfiguration.contrast = 1.15;

  const camera = new BABYLON.ArcRotateCamera("camera", -Math.PI / 2, Math.PI / 2.45, window.innerWidth < window.innerHeight ? 12 : 8, new BABYLON.Vector3(0, 1.3, 0), scene);
  camera.attachControl(canvas, true);
  camera.lowerRadiusLimit = 3.5;
  camera.upperRadiusLimit = 16;
  camera.upperBetaLimit = Math.PI / 2.05;
  camera.wheelDeltaPercentage = 0.01;
  camera.pinchDeltaPercentage = 0.01;
  camera.minZ = 0.05;
  camera.useAutoRotationBehavior = !reducedMotion;
  if (camera.autoRotationBehavior) { camera.autoRotationBehavior.idleRotationSpeed = 0.12; camera.autoRotationBehavior.idleRotationWaitTime = 2500; }
  const home = { alpha: camera.alpha, beta: camera.beta, radius: camera.radius };

  // Image-based lighting and sky from a CC0 photo; plain gradient clear colour if it cannot load.
  try {
    let env;
    await new Promise((resolve, reject) => {
      env = new BABYLON.EquiRectangularCubeTexture(SKY_URL, scene, 512, false, true, resolve, () => reject(new Error("sky photo failed")));
      setTimeout(() => reject(new Error("sky timeout")), 10000);
    });
    scene.environmentTexture = env;
    scene.environmentIntensity = 0.9;
    const dome = new BABYLON.PhotoDome("sky", SKY_URL, { resolution: 32, size: 900, useDirectMapping: false }, scene);
    dome.mesh.isPickable = false;
    dome.mesh.infiniteDistance = true;
    dome.imageMode = BABYLON.PhotoDome.MODE_MONOSCOPIC;
  } catch (error) {
    console.warn("Sky photo did not load; using a plain background.", error);
    statusEl.textContent = "The sky photo did not load; using a plain background.";
  }

  // Lights and shadows.
  const sun = new BABYLON.DirectionalLight("sun", new BABYLON.Vector3(0.5, -1, -0.6), scene);
  sun.position = new BABYLON.Vector3(-5, 9, 5);
  sun.intensity = 2.2;
  sun.shadowMinZ = 1; sun.shadowMaxZ = 30;
  const shadows = new BABYLON.ShadowGenerator(2048, sun);
  shadows.usePercentageCloserFiltering = true;
  shadows.filteringQuality = BABYLON.ShadowGenerator.QUALITY_MEDIUM;
  shadows.bias = 0.0005;
  const glowLight = new BABYLON.PointLight("glow", new BABYLON.Vector3(0, 2, 0), scene);
  glowLight.diffuse = palette[0].glow;
  glowLight.intensity = 6;
  glowLight.range = 9;
  glowLight.parent = world;

  // Pedestal: dark glossy disc with a brass rim.
  const pedestalMat = new BABYLON.PBRMaterial("pedestal", scene);
  pedestalMat.albedoColor = new BABYLON.Color3(0.05, 0.12, 0.28);
  pedestalMat.metallic = 0.3; pedestalMat.roughness = 0.28;
  const pedestal = BABYLON.MeshBuilder.CreateCylinder("pedestal", { diameterTop: 6.2, diameterBottom: 6.5, height: 0.2, tessellation: 96 }, scene);
  pedestal.position.y = -0.1;
  pedestal.material = pedestalMat;
  pedestal.receiveShadows = true;
  pedestal.parent = world;
  const brassMat = new BABYLON.PBRMaterial("brass", scene);
  brassMat.albedoColor = BABYLON.Color3.FromHexString("#ffb81c"); brassMat.metallic = 1; brassMat.roughness = 0.25;
  const rim = BABYLON.MeshBuilder.CreateTorus("rim", { diameter: 6.4, thickness: 0.09, tessellation: 128 }, scene);
  rim.material = brassMat; rim.parent = world;
  const ground = BABYLON.MeshBuilder.CreateDisc("ground", { radius: 18, tessellation: 64 }, scene);
  ground.rotation.x = Math.PI / 2;
  ground.position.y = -0.21;
  const groundMat = new BABYLON.PBRMaterial("groundMat", scene);
  groundMat.albedoColor = new BABYLON.Color3(0.8, 0.86, 0.93); groundMat.metallic = 0; groundMat.roughness = 0.9;
  const fade = new BABYLON.DynamicTexture("groundFade", 256, scene, true);
  const fc = fade.getContext(); const fg = fc.createRadialGradient(128, 128, 0, 128, 128, 128);
  fg.addColorStop(0, "#ffffff"); fg.addColorStop(0.4, "#d8d8d8"); fg.addColorStop(1, "#000000");
  fc.fillStyle = "#000000"; fc.fillRect(0, 0, 256, 256);
  fc.fillStyle = fg; fc.fillRect(0, 0, 256, 256); fade.update(); fade.getAlphaFromRGB = true;
  groundMat.opacityTexture = fade;
  ground.material = groundMat;
  ground.receiveShadows = true;
  ground.parent = world;

  // The totem: faceted PBR crystal with clear coat and iridescence, plus a halo ring and three beacons.
  const totem = BABYLON.MeshBuilder.CreatePolyhedron("totem", { type: 1, size: 1.1 }, scene);
  totem.position.y = 1.5;
  totem.parent = world;
  const totemMat = new BABYLON.PBRMaterial("totemMat", scene);
  totemMat.albedoColor = palette[0].color;
  totemMat.metallic = 0.25; totemMat.roughness = 0.12;
  totemMat.emissiveColor = palette[0].color.scale(0.55);
  totemMat.clearCoat.isEnabled = true;
  totemMat.iridescence.isEnabled = true;
  totemMat.iridescence.intensity = 0.6;
  totem.material = totemMat;
  shadows.addShadowCaster(totem);

  const haloMat = new BABYLON.PBRMaterial("haloMat", scene);
  haloMat.albedoColor = BABYLON.Color3.White(); haloMat.emissiveColor = new BABYLON.Color3(0.7, 0.95, 1); haloMat.metallic = 0; haloMat.roughness = 0.4;
  const halo = BABYLON.MeshBuilder.CreateTorus("halo", { diameter: 3.4, thickness: 0.045, tessellation: 128 }, scene);
  halo.position.y = 1.45; halo.rotation.x = Math.PI / 2.35; halo.material = haloMat; halo.parent = world;

  const beaconColors = [new BABYLON.Color3(1, 0.48, 0.25), new BABYLON.Color3(0.35, 0.95, 0.82), new BABYLON.Color3(0.48, 0.62, 1)];
  const beacons = beaconColors.map((color, index) => {
    const beacon = BABYLON.MeshBuilder.CreateSphere(`beacon-${index}`, { diameter: 0.26, segments: 24 }, scene);
    const mat = new BABYLON.PBRMaterial(`beacon-mat-${index}`, scene);
    mat.albedoColor = color; mat.emissiveColor = color; mat.metallic = 0; mat.roughness = 0.3;
    beacon.material = mat; beacon.parent = world;
    shadows.addShadowCaster(beacon);
    return beacon;
  });

  // A glow layer makes the emissive parts bloom; sparkles come from a particle system.
  const glow = new BABYLON.GlowLayer("glow", scene, { mainTextureSamples: 2, blurKernelSize: 48 });
  glow.intensity = 0.7;
  glow.addExcludedMesh(ground); glow.addExcludedMesh(pedestal);
  const sparkles = new BABYLON.ParticleSystem("sparkles", 160, scene);
  sparkles.particleTexture = glowTexture(scene, "sparkleTex");
  sparkles.emitter = new BABYLON.Vector3(0, 0.1, 0);
  sparkles.createCylinderEmitter(2.6, 0.2, 0, 0);
  sparkles.minLifeTime = 3; sparkles.maxLifeTime = 6;
  sparkles.emitRate = 30;
  sparkles.minSize = 0.04; sparkles.maxSize = 0.1;
  sparkles.direction1 = new BABYLON.Vector3(-0.05, 0.4, -0.05); sparkles.direction2 = new BABYLON.Vector3(0.05, 0.7, 0.05);
  sparkles.color1 = new BABYLON.Color4(0.8, 0.95, 1, 0.9); sparkles.color2 = new BABYLON.Color4(0.6, 0.8, 1, 0.7); sparkles.colorDead = new BABYLON.Color4(0.6, 0.8, 1, 0);
  sparkles.blendMode = BABYLON.ParticleSystem.BLENDMODE_ADD;
  sparkles.minEmitPower = 0.3; sparkles.maxEmitPower = 0.8;
  sparkles.start();

  // CC0 glTF props (primitives stand in if a model cannot load).
  async function placeModel(folder, file, { x, z, scale, rotation = 0 }, fallbackColor) {
    const holder = new BABYLON.TransformNode(`${folder}-holder`, scene);
    holder.position.set(x, 0, z); holder.rotation.y = rotation; holder.scaling.setAll(scale); holder.parent = world;
    try {
      const result = await BABYLON.SceneLoader.ImportMeshAsync("", `./assets/${folder}/`, file, scene);
      const root = result.meshes[0];
      root.parent = holder;
      result.meshes.forEach((mesh) => { if (mesh.getTotalVertices() > 0) { shadows.addShadowCaster(mesh); mesh.receiveShadows = true; } });
    } catch (error) {
      console.warn(`Model ${folder} did not load; using a primitive.`, error);
      const fallback = BABYLON.MeshBuilder.CreateCylinder(`${folder}-fallback`, { diameter: 0.4, height: 0.5 }, scene);
      fallback.position.y = 0.25; fallback.parent = holder; fallback.scaling.setAll(1 / scale);
      const mat = new BABYLON.PBRMaterial(`${folder}-mat`, scene);
      mat.albedoColor = BABYLON.Color3.FromHexString(fallbackColor); mat.metallic = 0.6; mat.roughness = 0.4;
      fallback.material = mat;
      shadows.addShadowCaster(fallback);
    }
  }
  placeModel("bronze_ray_statue", "bronze_ray_statue_1k.gltf", { x: -2.3, z: -0.7, scale: 1.9, rotation: 2.4 }, "#b87333");
  placeModel("ceramic_pot", "ceramic_pot_1k.gltf", { x: 2.3, z: -0.5, scale: 1.4, rotation: -0.4 }, "#d9e4f5");

  // In-world readout (readable in VR).
  const panelTexture = new BABYLON.DynamicTexture("panelTex", { width: 1024, height: 320 }, scene, true);
    const panelMat = new BABYLON.StandardMaterial("panelMat", scene);
  panelMat.diffuseColor = BABYLON.Color3.Black(); panelMat.specularColor = BABYLON.Color3.Black(); panelMat.emissiveTexture = panelTexture; panelMat.emissiveTexture.level = 1.1; panelMat.backFaceCulling = false;
  const panel = BABYLON.MeshBuilder.CreatePlane("panel", { width: 2.4, height: 0.75 }, scene);
  panel.material = panelMat; panel.position.set(0, 0.6, -2.7); panel.rotation.x = 0.55; panel.parent = world;
  glow.addExcludedMesh(panel);

  // Interaction.
  let paletteIndex = 0;
  let paused = reducedMotion;
  function setHue(index) {
    paletteIndex = (index + palette.length) % palette.length;
    const { name, color, glow: glowColor } = palette[paletteIndex];
    totemMat.albedoColor = color; totemMat.emissiveColor = color.scale(0.55);
    glowLight.diffuse = glowColor;
    statusEl.textContent = `Totem hue: ${name} (${paletteIndex + 1} of ${palette.length}).`;
    drawPanel(panelTexture, name, "Click the totem or press C to change its hue.", `#${glowColor.toHexString().slice(1, 7)}`);
  }
  const motionButton = document.querySelector("#toggle-motion");
  function setPaused(value) {
    paused = value;
    camera.useAutoRotationBehavior = !paused;
    if (camera.autoRotationBehavior) camera.autoRotationBehavior.idleRotationSpeed = 0.12;
    paused ? sparkles.stop() : sparkles.start();
    motionButton.textContent = paused ? "Resume motion" : "Pause motion";
    motionButton.setAttribute("aria-pressed", String(paused));
  }
  setPaused(paused);
  function resetView() {
    camera.alpha = home.alpha; camera.beta = home.beta; camera.radius = home.radius;
    camera.target.set(0, 1.3, 0);
    statusEl.textContent = "View reset.";
  }
  document.querySelector("#change-hue").addEventListener("click", () => setHue(paletteIndex + 1));
  motionButton.addEventListener("click", () => setPaused(!paused));
  document.querySelector("#reset-view").addEventListener("click", resetView);
  window.addEventListener("keydown", (event) => {
    if (event.target instanceof HTMLElement && event.target.closest("button, summary")) return;
    if (event.key === "c" || event.key === "C") setHue(paletteIndex + 1);
    else if (event.key === " ") { event.preventDefault(); setPaused(!paused); }
    else if (event.key === "r" || event.key === "R") resetView();
  });
  // Clicking (or a VR controller trigger on) the totem changes its hue.
  scene.onPointerObservable.add((info) => {
    if (info.type === BABYLON.PointerEventTypes.POINTERPICK && info.pickInfo?.pickedMesh === totem) setHue(paletteIndex + 1);
  });
  document.fonts?.ready.then(() => setHue(paletteIndex));
  setHue(0);

  let motionTime = 0;
  scene.onBeforeRenderObservable.add(() => {
    if (!paused) motionTime += engine.getDeltaTime() * 0.001;
    const t = motionTime;
    totem.rotation.y = t * 0.45;
    totem.rotation.x = Math.sin(t * 0.5) * 0.14;
    halo.rotation.z = t * 0.18;
    totem.position.y = 1.5 + Math.sin(t * 1.2) * 0.12;
    glowLight.position.y = totem.position.y + 0.3;
    beacons.forEach((beacon, index) => {
      const angle = t * 0.42 + (index / beacons.length) * Math.PI * 2;
      beacon.position.set(Math.cos(angle) * 2, 1.45 + Math.sin(t + index) * 0.18, Math.sin(angle) * 2);
    });
  });

  // WebXR: only the built-in generic controller profiles (the online profile repository is not reachable offline).
  BABYLON.WebXRMotionControllerManager.UseOnlineRepository = false;
  BABYLON.WebXRMotionControllerManager.BaseRepositoryUrl = new URL("../vendor/babylon/xr-profiles", document.baseURI).href;
  try {
    const xr = await scene.createDefaultXRExperienceAsync({
      floorMeshes: [ground],
      uiOptions: { sessionMode: "immersive-vr" },
      disableHandTracking: true, // hand meshes are fetched from assets.babylonjs.com; not available offline
      inputOptions: { doNotLoadControllerMeshes: true, disableOnlineControllerRepository: true }, // controller models are fetched from the web; skip them offline
    });
    xr.baseExperience?.onStateChangedObservable.add((state) => {
      // Put the exhibit a couple of metres in front of a standing player.
      if (state === BABYLON.WebXRState.IN_XR) world.position.z = 2.8;
      if (state === BABYLON.WebXRState.NOT_IN_XR) world.position.z = 0;
    });
  } catch (error) {
    console.warn("Immersive VR is unavailable; the desktop scene remains active.", error);
  }
  const xrNote = !("xr" in navigator) ? " This browser has no WebXR, so only the desktop view is available." : "";
  statusEl.textContent = `Luminous Totem is ready. Drag to orbit, press C to change the hue.${xrNote}`;
  window.__demo = { ready: true, setHue, scene };
  return scene;
}

createScene().then((scene) => {
  engine.runRenderLoop(() => scene.render());
}).catch((error) => {
  console.error("Could not create Babylon.js scene.", error);
  statusEl.textContent = "The scene could not start. Check WebGL support and the browser console.";
});

window.addEventListener("resize", () => engine.resize());
