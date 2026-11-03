// Writes shared/models.json and CREDITS.md from the files present in assets/models-mp/ plus tools/polyhaven-sources.json.
// Run after prepare-models.mjs and prepare-polyhaven.mjs:   node tools/make-manifest.mjs
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const models = join(here, "..", "..", "assets", "models-mp");
const sources = JSON.parse(readFileSync(join(here, "polyhaven-sources.json"), "utf8"));
const CC0 = { license: "CC0 1.0 (Creative Commons Zero)", licenseUrl: "https://creativecommons.org/publicdomain/zero/1.0/" };
const packs = {
  polyhaven: { name: "Poly Haven models", author: "individual artists credited per file below", source: "https://polyhaven.com/models",
    licenseNote: "Poly Haven states that every asset is CC0: https://polyhaven.com/license", ...CC0 },
  adventurers: { name: "KayKit Character Pack: Adventurers 1.0", author: "Kay Lousberg (kaylousberg.com)",
    source: "https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0",
    licenseNote: "Stated in the LICENSE.txt at the root of the pack's repository (the official KayKit-Game-Assets GitHub organization).", ...CC0 },
};
const props = {};
for (const file of readdirSync(join(models, "props")).sort()) {
  const id = file.replace(/\.glb$/, "");
  const s = sources[id];
  if (!s) throw new Error(`No source record for ${id}`);
  props[id] = { file: `props/${file}`, pack: "polyhaven", source: s.page, author: s.author };
}
const characters = {};
for (const file of readdirSync(join(models, "characters")).sort()) {
  const id = file.replace(/\.glb$/, "");
  characters[id] = { file: `characters/${file}`, pack: "adventurers", source: packs.adventurers.source, author: "Kay Lousberg" };
}
writeFileSync(join(here, "..", "shared", "models.json"), `${JSON.stringify({ packs, props, characters }, null, 2)}\n`);

const rows = [...Object.values(props), ...Object.values(characters)]
  .map((m) => `| ${m.file} | ${m.author} | ${packs[m.pack].name} | <${m.source}> |`).join("\n");
const packLines = Object.values(packs)
  .map((p) => `- **${p.name}**: ${p.license}, <${p.licenseUrl}>. ${p.licenseNote}`).join("\n");
writeFileSync(join(here, "..", "CREDITS.md"), `# Credits and licenses

Everything under [\`assets/models-mp/\`](../assets/models-mp/) is CC0 (public domain dedication): free for personal, educational and
commercial use, credit not required. We credit the authors anyway. Licenses were checked at the sources listed below on November 3, 2026.

${packLines}

## What was changed

- **Props** (Poly Haven): downloaded at 1k, simplified with meshoptimizer to roughly 0.2k to 6k triangles each (most under 2.5k), merged to one mesh per material, textures shrunk to 512 px WebP,
  saved as one \`.glb\` each ([\`tools/prepare-polyhaven.mjs\`](./tools/prepare-polyhaven.mjs)).
- **Characters** (KayKit Adventurers): the medieval weapons, shields, hats, capes and hoods were removed (the examples add their own hard hats), and only
  three animations were kept (\`Unarmed_Idle\`, \`Walking_A\`, \`Interact\`) ([\`tools/prepare-models.mjs\`](./tools/prepare-models.mjs)).
- The room itself (floor, walls, bench tops) is generated in code; its textures are drawn on a canvas, not downloaded.

## Files

| File (under assets/models-mp/) | Author | Collection | Source |
|---|---|---|---|
${rows}

## Other third-party material

- Three.js (MIT), including GLTFLoader, SkeletonUtils and RoomEnvironment: see [\`vendor/three/LICENSE\`](../vendor/three/LICENSE).
- Fonts, brand bar and the Pitt palette come from the [brand kit](../brand-kit/README.md); the university logo only appears when the git-ignored logo file is present.

Nothing here implies that the authors or their sites endorse this project. The characters are stylised fantasy figures used as neutral stand-ins for
colleagues; they are not portraits of anyone, and they are cartoon-like, not photo-real (no CC0 realistic people were found that fit a Quest budget).
`);
console.log(Object.keys(props).length, "props,", Object.keys(characters).length, "characters");
