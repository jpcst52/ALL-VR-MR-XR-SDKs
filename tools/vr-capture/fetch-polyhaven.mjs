// Downloads Poly Haven (CC0) models as 1k glTF into a work folder and prints each one's license/author record.
// Usage: node tools/vr-capture/fetch-polyhaven.mjs <workDir> <id> [id ...]
// Then shrink with glTF-Transform (see assets/models/CREDITS.md): textures to 512 px WebP, weld, simplify, pack to GLB.
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";

const [work, ...ids] = process.argv.slice(2);
if (!work || !ids.length) { console.error("usage: fetch-polyhaven.mjs <workDir> <id> ..."); process.exit(1); }
const get = async (url) => { const r = await fetch(url); if (!r.ok) throw new Error(`${r.status} ${url}`); return r; };
const all = await (await get("https://api.polyhaven.com/assets?t=models")).json();
for (const id of ids) {
  const meta = all[id];
  if (!meta) { console.log(id, "NOT FOUND"); continue; }
  const files = await (await get(`https://api.polyhaven.com/files/${id}`)).json();
  const entry = files.gltf["1k"].gltf;
  const dir = join(work, id);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, `${id}.gltf`), Buffer.from(await (await get(entry.url)).arrayBuffer()));
  for (const [rel, info] of Object.entries(entry.include ?? {})) {
    const target = join(dir, rel);
    mkdirSync(dirname(target), { recursive: true });
    writeFileSync(target, Buffer.from(await (await get(info.url)).arrayBuffer()));
  }
  console.log(`${id} | ${meta.name} | ${Object.keys(meta.authors).join(", ")} | tris ${meta.polycount} | dims(cm) ${meta.dimensions.map((d) => Math.round(d)).join("x")}`);
}
