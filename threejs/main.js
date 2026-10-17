import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { VRButton } from "three/addons/webxr/VRButton.js";

const scene = new THREE.Scene();
scene.background = new THREE.Color("#101723");
scene.fog = new THREE.Fog("#101723", 8, 22);

const camera = new THREE.PerspectiveCamera(65, window.innerWidth / window.innerHeight, 0.1, 100);
camera.position.set(0, 1.5, 5);

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.xr.enabled = true;
document.body.appendChild(renderer.domElement);
document.body.appendChild(VRButton.createButton(renderer));

const controls = new OrbitControls(camera, renderer.domElement);
controls.enableDamping = true;

scene.add(new THREE.HemisphereLight(0xb9e4ff, 0x482854, 2));
const keyLight = new THREE.DirectionalLight(0xffe0aa, 3);
keyLight.position.set(-3, 5, 4);
scene.add(keyLight);

const prism = new THREE.Mesh(
  new THREE.IcosahedronGeometry(1.1, 1),
  new THREE.MeshStandardMaterial({ color: "#b8a5ff", roughness: 0.24, metalness: 0.35, flatShading: true }),
);
prism.position.y = 1.3;
scene.add(prism);

const floor = new THREE.Mesh(
  new THREE.CircleGeometry(4, 64),
  new THREE.MeshStandardMaterial({ color: "#243449", roughness: 0.9 }),
);
floor.rotation.x = -Math.PI / 2;
floor.position.y = -0.02;
scene.add(floor);

const ring = new THREE.Mesh(
  new THREE.TorusGeometry(2.1, 0.025, 8, 100),
  new THREE.MeshBasicMaterial({ color: "#76e7d4" }),
);
ring.rotation.x = Math.PI / 2.4;
ring.position.y = 1.3;
scene.add(ring);

const clock = new THREE.Clock();
renderer.setAnimationLoop(() => {
  const elapsed = clock.getElapsedTime();
  prism.rotation.set(elapsed * 0.28, elapsed * 0.45, elapsed * 0.18);
  ring.rotation.z = elapsed * 0.2;
  controls.update();
  renderer.render(scene, camera);
});

window.addEventListener("resize", () => {
  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(window.innerWidth, window.innerHeight);
});
