const canvas = document.querySelector("#renderCanvas");
const engine = new BABYLON.Engine(canvas, true, { preserveDrawingBuffer: true, stencil: true });

async function createScene() {
  const scene = new BABYLON.Scene(engine);
  scene.clearColor = new BABYLON.Color4(0.04, 0.07, 0.11, 1);

  const camera = new BABYLON.ArcRotateCamera("camera", -Math.PI / 2, Math.PI / 2.5, 7, new BABYLON.Vector3(0, 1.4, 0), scene);
  camera.attachControl(canvas, true);
  camera.lowerRadiusLimit = 3;
  camera.upperRadiusLimit = 12;

  const hemi = new BABYLON.HemisphericLight("hemi", new BABYLON.Vector3(0, 1, 0), scene);
  hemi.intensity = 0.85;
  const point = new BABYLON.PointLight("glow", new BABYLON.Vector3(0, 2, 0), scene);
  point.diffuse = new BABYLON.Color3(0.35, 0.85, 1);
  point.intensity = 24;

  const ground = BABYLON.MeshBuilder.CreateGround("ground", { width: 12, height: 12 }, scene);
  const groundMaterial = new BABYLON.StandardMaterial("groundMaterial", scene);
  groundMaterial.diffuseColor = new BABYLON.Color3(0.08, 0.15, 0.19);
  groundMaterial.specularColor = new BABYLON.Color3(0.15, 0.3, 0.35);
  ground.material = groundMaterial;

  const totem = BABYLON.MeshBuilder.CreatePolyhedron("totem", { type: 1, size: 1.1 }, scene);
  totem.position.y = 1.45;
  const totemMaterial = new BABYLON.StandardMaterial("totemMaterial", scene);
  totemMaterial.diffuseColor = new BABYLON.Color3(0.14, 0.45, 0.58);
  totemMaterial.emissiveColor = new BABYLON.Color3(0.04, 0.3, 0.42);
  totemMaterial.specularColor = new BABYLON.Color3(0.8, 0.95, 1);
  totem.material = totemMaterial;

  const halo = BABYLON.MeshBuilder.CreateTorus("halo", { diameter: 3.4, thickness: 0.045, tessellation: 96 }, scene);
  halo.position.y = 1.35;
  halo.rotation.x = Math.PI / 2.35;
  const haloMaterial = new BABYLON.StandardMaterial("haloMaterial", scene);
  haloMaterial.emissiveColor = new BABYLON.Color3(0.1, 0.9, 0.8);
  halo.material = haloMaterial;

  scene.onBeforeRenderObservable.add(() => {
    const time = performance.now() * 0.001;
    totem.rotation.y = time * 0.45;
    totem.rotation.x = Math.sin(time * 0.5) * 0.14;
    halo.rotation.z = time * 0.18;
    totem.position.y = 1.45 + Math.sin(time * 1.2) * 0.12;
    point.position.y = totem.position.y + 0.3;
  });

  try {
    await scene.createDefaultXRExperienceAsync({
      floorMeshes: [ground],
      uiOptions: { sessionMode: "immersive-vr" },
    });
  } catch (error) {
    console.warn("Immersive VR is unavailable; the desktop scene remains active.", error);
    const notice = document.createElement("p");
    notice.textContent = "Immersive VR is unavailable in this browser/device; desktop orbit controls are active.";
    notice.style.cssText = "position:fixed;bottom:1rem;left:1rem;z-index:2;background:#101923dc;padding:.6rem .8rem;border-radius:.5rem";
    document.body.appendChild(notice);
  }
  return scene;
}

createScene().then((scene) => {
  engine.runRenderLoop(() => scene.render());
}).catch((error) => {
  console.error("Could not create Babylon.js scene.", error);
  const message = document.createElement("p");
  message.textContent = "The scene could not start. Check WebGL support and the browser console.";
  message.style.cssText = "position:fixed;bottom:1rem;left:1rem;background:#101923;padding:.6rem .8rem";
  document.body.appendChild(message);
});

window.addEventListener("resize", () => engine.resize());
