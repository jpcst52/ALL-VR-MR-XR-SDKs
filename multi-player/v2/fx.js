// Lab v2 effects that cost one draw call each however many parts there are:
//  - a pool of four real point lights (a fixed count, so shaders never recompile when bulbs come and go),
//    handed each frame to the brightest bulbs / beacons,
//  - instanced current-flow dots,
//  - instanced terminal status rings.
import * as THREE from "three";

export const POOL_SIZE = 4;
const MAX_DOTS = 256;
const MAX_RINGS = 96;

export function createFx({ scene, board }) {
  const lights = Array.from({ length: POOL_SIZE }, () => {
    const light = new THREE.PointLight("#ffd479", 0, 2.6, 2);
    light.name = "pool-light";
    scene.add(light);
    return light;
  });

  const dotMaterial = new THREE.MeshBasicMaterial({ color: "#ffc94a", toneMapped: false });
  const dots = new THREE.InstancedMesh(new THREE.IcosahedronGeometry(0.02, 1), dotMaterial, MAX_DOTS);
  dots.count = 0; dots.frustumCulled = false; dots.name = "flow-dots"; dots.renderOrder = 3;
  board.add(dots);

  const ringGeometry = new THREE.TorusGeometry(0.062, 0.011, 6, 20);
  ringGeometry.rotateX(Math.PI / 2);
  const ringMaterial = new THREE.MeshBasicMaterial({ color: "#ffffff", toneMapped: false });
  const rings = new THREE.InstancedMesh(ringGeometry, ringMaterial, MAX_RINGS);
  rings.setColorAt(0, new THREE.Color("#ffffff"));
  rings.count = 0; rings.frustumCulled = false; rings.name = "terminal-rings";
  board.add(rings);

  const matrix = new THREE.Matrix4();
  const colour = new THREE.Color();
  const emitters = [];
  const tmp = new THREE.Vector3();
  let dotCount = 0; let ringCount = 0;
  const stats = { dots: 0, rings: 0, lit: 0 };

  return {
    lights, dots, rings, stats,
    begin() { dotCount = 0; ringCount = 0; emitters.length = 0; },
    dot(x, y, z) {
      if (dotCount >= MAX_DOTS) return;
      matrix.makeTranslation(x, y, z);
      dots.setMatrixAt(dotCount, matrix);
      dotCount += 1;
    },
    ring(x, y, z, hex) {
      if (ringCount >= MAX_RINGS) return;
      matrix.makeTranslation(x, y, z);
      rings.setMatrixAt(ringCount, matrix);
      rings.setColorAt(ringCount, colour.set(hex));
      ringCount += 1;
    },
    // A glowing thing in board coordinates; the four strongest get a real light.
    emitter(x, y, z, hex, intensity) { if (intensity > 0.01) emitters.push({ x, y, z, hex, intensity }); },
    end() {
      dots.count = dotCount; rings.count = ringCount;
      dots.instanceMatrix.needsUpdate = true; rings.instanceMatrix.needsUpdate = true;
      if (rings.instanceColor) rings.instanceColor.needsUpdate = true;
      emitters.sort((a, b) => b.intensity - a.intensity);
      board.updateWorldMatrix(true, false);
      const scale = board.getWorldScale(tmp).x; // the board is a 0.4 scale table-top model in VR
      lights.forEach((light, index) => {
        const e = emitters[index];
        if (!e) { light.intensity = 0; return; }
        light.position.set(e.x, e.y, e.z).applyMatrix4(board.matrixWorld);
        light.color.set(e.hex);
        light.intensity = e.intensity * 2.4 * scale * scale; // illuminance falls with distance squared
        light.distance = 3.2 * scale;
      });
      stats.dots = dotCount; stats.rings = ringCount; stats.lit = Math.min(emitters.length, POOL_SIZE);
    },
    dispose() { lights.forEach((l) => scene.remove(l)); board.remove(dots, rings); },
  };
}
