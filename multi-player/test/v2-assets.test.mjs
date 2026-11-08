import test from "node:test";
import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { createRelay } from "../server.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const v2 = join(here, "..", "v2");
const repo = join(here, "..", "..");
const sources = readdirSync(v2).filter((name) => /\.(js|mjs|html|css)$/.test(name));

test("v2: every relative import and stylesheet resolves to a file in the repository", () => {
  for (const name of sources) {
    const text = readFileSync(join(v2, name), "utf8");
    const specs = [...text.matchAll(/(?:from\s+|import\s*\(\s*)"(\.{1,2}\/[^"]+)"/g)].map((m) => m[1]);
    for (const spec of specs) assert.ok(existsSync(resolve(v2, spec)), `${name} imports ${spec}`);
    if (name.endsWith(".html")) {
      for (const m of text.matchAll(/(?:href|src)="(\.{1,2}\/[^"]+)"/g)) assert.ok(existsSync(resolve(v2, m[1])), `${name} links ${m[1]}`);
    }
  }
});

test("v2: three and its add-ons come from vendor/ (importmap), and nothing points at a CDN", () => {
  const html = readFileSync(join(v2, "index.html"), "utf8");
  assert.match(html, /"three": "\.\.\/\.\.\/vendor\/three\/three\.module\.js"/);
  assert.ok(existsSync(join(repo, "vendor", "three", "three.module.js")));
  for (const name of sources) {
    const text = readFileSync(join(v2, name), "utf8");
    // brand font URLs and the spec namespace in code comments are fine; a real fetch of an http(s) resource is not
    const urls = [...text.matchAll(/(?:src|href)="(https?:\/\/[^"]+)"|url\(\s*["']?(https?:\/\/[^)"']+)|import\s+[^;]*from\s+"(https?:\/\/[^"]+)"|fetch\("(https?:\/\/[^"]+)"/g)];
    assert.equal(urls.length, 0, `${name} references ${urls.map((u) => u[0]).join(", ")}`);
  }
});

test("v2: the relay serves the page and its modules, and the v1 pages are still served", async () => {
  const relay = createRelay();
  const { port } = await relay.listen({ host: "127.0.0.1", port: 0, autoPort: true });
  try {
    for (const [path, type] of [["/multi-player/v2/", /text\/html/], ["/multi-player/v2/app.js", /javascript/], ["/multi-player/v2/themes2.js", /javascript/], ["/multi-player/v2/v2.css", /text\/css/], ["/multi-player/vr/", /text\/html/], ["/multi-player/ar/", /text\/html/], ["/multi-player/shared/app.js", /javascript/]]) {
      const response = await fetch(`http://127.0.0.1:${port}${path}`);
      assert.equal(response.status, 200, path);
      assert.match(response.headers.get("content-type"), type, path);
      await response.arrayBuffer();
    }
  } finally {
    await relay.close();
  }
});

test("v2 shares the v1 rules: themes2 is built on the v1 themes (same ids, parts, examples, toolbox labels)", () => {
  // themes.js imports "three" through the browser importmap, so it cannot be imported in Node: check the source instead.
  const text = readFileSync(join(v2, "themes2.js"), "utf8");
  assert.match(text, /import \{ themes as v1 \} from "\.\.\/shared\/themes\.js"/);
  assert.match(text, /\.\.\.v1\.circuits,/);
  assert.match(text, /\.\.\.v1\.safety,/);
  assert.match(text, /evaluateCircuit/);
  assert.match(readFileSync(join(v2, "app.js"), "utf8"), /createToolbox\(\{/);
});
