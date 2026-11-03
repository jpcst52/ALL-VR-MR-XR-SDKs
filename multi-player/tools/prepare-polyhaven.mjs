// Downloads a short list of CC0 Poly Haven models (https://polyhaven.com/models, license https://polyhaven.com/license),
// simplifies them for Quest-class budgets, shrinks the textures to 512 px WebP and writes one .glb each into
// assets/models-mp/props/. Maintainer script, not part of `npm test`; the browser examples do not need it.
//
//   npm i --no-save @gltf-transform/core @gltf-transform/functions sharp meshoptimizer   (in a scratch folder)
//   node prepare-polyhaven.mjs <repo root> <scratch download folder>
//
// It also writes tools/polyhaven-sources.json (id, author, polycount before, page URL), which make-manifest.mjs reads.
import { NodeIO } from "@gltf-transform/core";
import { ALL_EXTENSIONS } from "@gltf-transform/extensions";
import { dedup, flatten, join as joinPrimitives, prune, simplify, textureCompress, weld } from "@gltf-transform/functions";
import { MeshoptSimplifier } from "meshoptimizer";
import sharp from "sharp";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";

const [root, scratch] = process.argv.slice(2);
const out = join(root, "assets", "models-mp", "props");
mkdirSync(out, { recursive: true });

// id: share of triangles to keep (1 = untouched). Targets are about 1.5k to 5k triangles per prop.
const MODELS = {
  retro_multimeter: 0.12, metal_toolbox: 0.12, classic_laptop: 0.1, metal_stool_01: 0.2, hanging_industrial_lamp: 0.2, bench_vice_01: 0.5,
  WetFloorSign_01: 1, barrel_03: 0.7, Barrel_01: 0.5, korean_fire_extinguisher_01: 0.15, fire_alarm: 0.5, propane_tank: 0.35,
  security_light: 0.4, wooden_crate_01: 0.25, industrial_pastic_container: 0.3, portable_searchlight: 0.1,
  modular_industrial_pipes_01: 0.08, tool_cart: 0.06, industrial_storage_cart: 0.06, portable_generator: 0.06, metal_tool_chest: 0.12,
};

await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const sourcesPath = join(root, "multi-player", "tools", "polyhaven-sources.json");
const sources = existsSync(sourcesPath) ? JSON.parse(readFileSync(sourcesPath, "utf8")) : {};
for (const [id, ratio] of Object.entries(MODELS)) {
  if (process.env.ONLY && !process.env.ONLY.split(",").includes(id)) continue;
  const files = await (await fetch(`https://api.polyhaven.com/files/${id}`)).json();
  const info = await (await fetch(`https://api.polyhaven.com/info/${id}`)).json();
  const entry = files.gltf["1k"].gltf;
  const dir = join(scratch, id);
  mkdirSync(dir, { recursive: true });
  const save = async (url, path) => {
    mkdirSync(dirname(path), { recursive: true });
    writeFileSync(path, Buffer.from(await (await fetch(url)).arrayBuffer()));
  };
  await save(entry.url, join(dir, `${id}.gltf`));
  for (const [rel, file] of Object.entries(entry.include)) await save(file.url, join(dir, rel));
  const doc = await io.read(join(dir, `${id}.gltf`));
  const steps = [flatten(), joinPrimitives(), weld(), dedup()]; // flatten + join: one draw call per material instead of per part
  if (ratio < 1) steps.push(simplify({ simplifier: MeshoptSimplifier, ratio, error: 0.1 }));
  steps.push(textureCompress({ encoder: sharp, targetFormat: "webp", resize: [512, 512], quality: 80 }), prune());
  await doc.transform(...steps);
  await io.write(join(out, `${id}.glb`), doc);
  sources[id] = { author: Object.keys(info.authors).join(", "), polycountBefore: info.polycount, page: `https://polyhaven.com/a/${id}` };
  console.log(id, "ok");
}
writeFileSync(sourcesPath, `${JSON.stringify(sources, null, 2)}\n`);
