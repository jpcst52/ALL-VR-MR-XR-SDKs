// Static server for the built site, serving it under a sub-path like GitHub Pages does for a project site.
//   node tools/pages/serve-subpath.mjs [port] [prefix]     defaults: 8151 /ALL-VR-MR-XR-SDKs/
// Anything outside the prefix is a 404 (so an absolute "/something" link that works at a root host fails here, as it would
// on Pages). Unknown paths inside the prefix return site/404.html with status 404, like Pages.
import { createServer } from "node:http";
import { createReadStream, existsSync, statSync } from "node:fs";
import { dirname, extname, join, normalize, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const types = { ".html": "text/html; charset=utf-8", ".js": "text/javascript; charset=utf-8", ".mjs": "text/javascript; charset=utf-8", ".css": "text/css; charset=utf-8", ".json": "application/json", ".gltf": "model/gltf+json", ".glb": "model/gltf-binary", ".bin": "application/octet-stream", ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".png": "image/png", ".gif": "image/gif", ".svg": "image/svg+xml", ".woff2": "font/woff2", ".md": "text/markdown; charset=utf-8", ".wasm": "application/wasm", ".pptx": "application/vnd.openxmlformats-officedocument.presentationml.presentation", ".txt": "text/plain; charset=utf-8" };

export function startSubpathServer({ port = 8151, prefix = "/ALL-VR-MR-XR-SDKs/", root = resolve(here, "..", "..", "site") } = {}) {
  const server = createServer((request, response) => {
    const url = new URL(request.url, "http://x");
    const send404 = () => {
      const page = join(root, "404.html");
      response.writeHead(404, { "Content-Type": "text/html; charset=utf-8" });
      if (existsSync(page)) createReadStream(page).pipe(response); else response.end("not found");
    };
    let path = decodeURIComponent(url.pathname);
    if (path === prefix.slice(0, -1)) { response.writeHead(301, { Location: prefix }).end(); return; }
    if (!path.startsWith(prefix)) return send404();
    path = normalize(path.slice(prefix.length)).replace(/^([/\\])+/, "");
    if (path.includes("..")) { response.writeHead(403).end(); return; }
    let file = join(root, path);
    if (existsSync(file) && statSync(file).isDirectory()) {
      if (!url.pathname.endsWith("/")) { response.writeHead(301, { Location: url.pathname + "/" + url.search }).end(); return; }
      file = join(file, "index.html");
    }
    if (!existsSync(file)) return send404();
    response.writeHead(200, { "Content-Type": types[extname(file).toLowerCase()] ?? "application/octet-stream", "Cache-Control": "no-store" });
    createReadStream(file).pipe(response);
  });
  return new Promise((ok, fail) => { server.once("error", fail); server.listen(port, () => ok(server)); });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const port = Number(process.argv[2] ?? 8151), prefix = process.argv[3] ?? "/ALL-VR-MR-XR-SDKs/";
  await startSubpathServer({ port, prefix });
  console.log(`serving site/ on http://localhost:${port}${prefix}`);
}
