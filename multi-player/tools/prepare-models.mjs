// Prepares the four CC0 KayKit characters shipped in assets/models-mp/characters/. Maintainer script, not part of `npm test`.
//
//   1. git clone --depth 1 https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0.git <scratch>/
//   2. npm i --no-save @gltf-transform/core @gltf-transform/functions   (in a scratch folder next to a copy of this script)
//   3. node prepare-models.mjs <scratch folder holding the clone> <repo root>
//
// Weapons, shields, hats, capes and hoods are removed (the examples add their own hard hat) and only the three
// animations the avatars use are kept, which brings each file from 3.5 MB to about 340 KB.
import { NodeIO } from "@gltf-transform/core";
import { prune, resample } from "@gltf-transform/functions";
import { mkdirSync, readdirSync } from "node:fs";
import { join } from "node:path";

const [clones, root] = process.argv.slice(2);
const out = join(root, "assets", "models-mp", "characters");
mkdirSync(out, { recursive: true });
const io = new NodeIO();
const repo = "KayKit-Character-Pack-Adventures-1.0";
const assetDir = join(clones, repo, "addons", readdirSync(join(clones, repo, "addons"))[0]);

const KEEP_ANIMATIONS = new Set(["Unarmed_Idle", "Walking_A", "Interact"]);
const DROP_NODE = /Sword|Shield|Axe|Mug|Hat|Helmet|Cape|Wand|Staff|Spellbook|Knife|Crossbow|Throwable|Hood/i;
for (const name of ["Knight", "Barbarian", "Mage", "Rogue"]) {
  const doc = await io.read(join(assetDir, "Characters", "gltf", `${name}.glb`));
  const gltfRoot = doc.getRoot();
  for (const anim of gltfRoot.listAnimations()) {
    if (KEEP_ANIMATIONS.has(anim.getName())) continue;
    for (const channel of anim.listChannels()) channel.dispose();
    for (const sampler of anim.listSamplers()) sampler.dispose();
    anim.dispose();
  }
  for (const node of gltfRoot.listNodes()) {
    if (node.getMesh() && DROP_NODE.test(node.getName())) node.setMesh(null);
  }
  await doc.transform(resample(), prune());
  await io.write(join(out, `${name.toLowerCase()}.glb`), doc);
}
console.log("done");
