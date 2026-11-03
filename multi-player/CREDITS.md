# Credits and licenses

Everything under [`assets/models-mp/`](../assets/models-mp/) is CC0 (public domain dedication): free for personal, educational and
commercial use, credit not required. We credit the authors anyway. Licenses were checked at the sources listed below on November 3, 2026.

- **Poly Haven models**: CC0 1.0 (Creative Commons Zero), <https://creativecommons.org/publicdomain/zero/1.0/>. Poly Haven states that every asset is CC0: https://polyhaven.com/license
- **KayKit Character Pack: Adventurers 1.0**: CC0 1.0 (Creative Commons Zero), <https://creativecommons.org/publicdomain/zero/1.0/>. Stated in the LICENSE.txt at the root of the pack's repository (the official KayKit-Game-Assets GitHub organization).

## What was changed

- **Props** (Poly Haven): downloaded at 1k, simplified with meshoptimizer to roughly 0.2k to 6k triangles each (most under 2.5k), merged to one mesh per material, textures shrunk to 512 px WebP,
  saved as one `.glb` each ([`tools/prepare-polyhaven.mjs`](./tools/prepare-polyhaven.mjs)).
- **Characters** (KayKit Adventurers): the medieval weapons, shields, hats, capes and hoods were removed (the examples add their own hard hats), and only
  three animations were kept (`Unarmed_Idle`, `Walking_A`, `Interact`) ([`tools/prepare-models.mjs`](./tools/prepare-models.mjs)).
- The room itself (floor, walls, bench tops) is generated in code; its textures are drawn on a canvas, not downloaded.

## Files

| File (under assets/models-mp/) | Author | Collection | Source |
|---|---|---|---|
| props/Barrel_01.glb | Jorge Camacho | Poly Haven models | <https://polyhaven.com/a/Barrel_01> |
| props/WetFloorSign_01.glb | Fran Calvente | Poly Haven models | <https://polyhaven.com/a/WetFloorSign_01> |
| props/barrel_03.glb | Serhii Khromov | Poly Haven models | <https://polyhaven.com/a/barrel_03> |
| props/bench_vice_01.glb | Yann Kervran, Antanas Kep | Poly Haven models | <https://polyhaven.com/a/bench_vice_01> |
| props/classic_laptop.glb | Arrangemonk | Poly Haven models | <https://polyhaven.com/a/classic_laptop> |
| props/fire_alarm.glb | Slinc | Poly Haven models | <https://polyhaven.com/a/fire_alarm> |
| props/hanging_industrial_lamp.glb | Kuutti Siitonen | Poly Haven models | <https://polyhaven.com/a/hanging_industrial_lamp> |
| props/industrial_pastic_container.glb | Galo Benivegna | Poly Haven models | <https://polyhaven.com/a/industrial_pastic_container> |
| props/industrial_storage_cart.glb | Jule Bielitz | Poly Haven models | <https://polyhaven.com/a/industrial_storage_cart> |
| props/korean_fire_extinguisher_01.glb | UM JOORIN | Poly Haven models | <https://polyhaven.com/a/korean_fire_extinguisher_01> |
| props/metal_stool_01.glb | Ulan Cabanilla | Poly Haven models | <https://polyhaven.com/a/metal_stool_01> |
| props/metal_tool_chest.glb | Yann Kervran, John Hutcheson | Poly Haven models | <https://polyhaven.com/a/metal_tool_chest> |
| props/metal_toolbox.glb | Mateusz Sadek | Poly Haven models | <https://polyhaven.com/a/metal_toolbox> |
| props/modular_industrial_pipes_01.glb | Jorge Camacho | Poly Haven models | <https://polyhaven.com/a/modular_industrial_pipes_01> |
| props/portable_generator.glb | James Ray Cock | Poly Haven models | <https://polyhaven.com/a/portable_generator> |
| props/portable_searchlight.glb | Elijah Cragg | Poly Haven models | <https://polyhaven.com/a/portable_searchlight> |
| props/propane_tank.glb | Slinc | Poly Haven models | <https://polyhaven.com/a/propane_tank> |
| props/retro_multimeter.glb | elli moeller | Poly Haven models | <https://polyhaven.com/a/retro_multimeter> |
| props/security_light.glb | Maximilian Schuster | Poly Haven models | <https://polyhaven.com/a/security_light> |
| props/tool_cart.glb | Savva Zakharov | Poly Haven models | <https://polyhaven.com/a/tool_cart> |
| props/wooden_crate_01.glb | James Ray Cock | Poly Haven models | <https://polyhaven.com/a/wooden_crate_01> |
| characters/barbarian.glb | Kay Lousberg | KayKit Character Pack: Adventurers 1.0 | <https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0> |
| characters/knight.glb | Kay Lousberg | KayKit Character Pack: Adventurers 1.0 | <https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0> |
| characters/mage.glb | Kay Lousberg | KayKit Character Pack: Adventurers 1.0 | <https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0> |
| characters/rogue.glb | Kay Lousberg | KayKit Character Pack: Adventurers 1.0 | <https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0> |

## Other third-party material

- Three.js (MIT), including GLTFLoader, SkeletonUtils and RoomEnvironment: see [`vendor/three/LICENSE`](../vendor/three/LICENSE).
- Fonts, brand bar and the Pitt palette come from the [brand kit](../brand-kit/README.md); the university logo only appears when the git-ignored logo file is present.

Nothing here implies that the authors or their sites endorse this project. The characters are stylised fantasy figures used as neutral stand-ins for
colleagues; they are not portraits of anyone, and they are cartoon-like, not photo-real (no CC0 realistic people were found that fit a Quest budget).
