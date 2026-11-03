// Shrinks Poly Haven glTF downloads (see fetch-polyhaven.mjs) into web-sized GLB files for assets/models/props/.
// Textures are resized to 512 px and re-encoded as WebP, meshes are simplified to a triangle budget (ratio = budget / the
// triangle count Poly Haven lists), no Draco/Meshopt (the page's GLTFLoader has no decoder). Needs @gltf-transform/cli
// installed under <toolDir> (npm i @gltf-transform/cli sharp).
// Usage: node tools/vr-capture/shrink-models.mjs <workDir> <toolDir> <outDir> <id>[:maxTris] ...   (default budget 5000)
import { spawnSync } from "node:child_process";
import { join } from "node:path";
import { mkdirSync, statSync } from "node:fs";

const [work, toolDir, out, ...specs] = process.argv.slice(2);
mkdirSync(out, { recursive: true });
const cli = join(toolDir, "node_modules", "@gltf-transform", "cli", "bin", "cli.js");
const all = await (await fetch("https://api.polyhaven.com/assets?t=models")).json();
for (const spec of specs) {
  const [id, limit = "5000"] = spec.split(":");
  const polycount = all[id]?.polycount ?? Number(limit);
  const ratio = Math.min(1, Number(limit) / polycount).toFixed(3);
  const target = join(out, `${id}.glb`);
  const args = [cli, "optimize", join(work, id, `${id}.gltf`), target, "--compress", "false", "--texture-compress", "webp", "--texture-size", "512",
    "--simplify", ratio < 1 ? "true" : "false", "--simplify-ratio", ratio, "--simplify-error", "0.02", "--instance", "false", "--join", "false", "--flatten", "false", "--palette", "false"];
  const result = spawnSync(process.execPath, args, { encoding: "utf8", cwd: toolDir });
  if (result.status !== 0) { console.log(id, "FAILED", result.stderr.slice(0, 300)); continue; }
  console.log(id.padEnd(32), `${Math.round(statSync(target).size / 1024)} KB`, `ratio ${ratio} (listed ${polycount} tris)`);
}
