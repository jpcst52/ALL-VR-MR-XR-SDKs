import assert from "node:assert/strict";
import { X509Certificate } from "node:crypto";
import { mkdtempSync, rmSync } from "node:fs";
import { request } from "node:https";
import { hostname, tmpdir } from "node:os";
import { join } from "node:path";
import { after, before, test } from "node:test";
import { createRelay } from "../server.mjs";
import { ensureCertificate, lanAddresses } from "../tls.mjs";

let dir;
let certificate;

before(async () => {
  dir = mkdtempSync(join(tmpdir(), "xr-certs-"));
  certificate = await ensureCertificate({ dir });
});

after(() => rmSync(dir, { recursive: true, force: true }));

test("certificate has the expected SANs and about a year of validity", () => {
  assert.equal(certificate.created, true);
  const cert = new X509Certificate(certificate.cert);
  const san = cert.subjectAltName;
  assert.match(san, /DNS:localhost/);
  assert.match(san, /IP Address:127\.0\.0\.1/);
  assert.ok(san.toLowerCase().includes(`dns:${hostname().toLowerCase()}`), `hostname in ${san}`);
  for (const { address } of lanAddresses()) assert.ok(san.includes(`IP Address:${address}`), address);
  const days = (new Date(cert.validTo) - new Date(cert.validFrom)) / 86400000;
  assert.ok(days > 360 && days < 370, `validity ${days} days`);
});

test("certificate is cached and reused on the second call", async () => {
  const again = await ensureCertificate({ dir });
  assert.equal(again.created, false);
  assert.equal(again.cert, certificate.cert);
});

test("HTTPS relay answers with TLS_KEY/TLS_CERT-style options and /api/urls", async () => {
  const keyFile = join(dir, "key.pem");
  const certFile = join(dir, "cert.pem");
  process.env.TLS_KEY = keyFile;
  process.env.TLS_CERT = certFile;
  const relay = createRelay();
  try {
    const address = await relay.listen({ host: "127.0.0.1", port: 0 });
    assert.equal(address.protocol, "https");
    const get = (path) => new Promise((resolveGet, rejectGet) => {
      // Trust-all is for this test client only.
      request({ host: "127.0.0.1", port: address.port, path, rejectUnauthorized: false }, (response) => {
        let body = "";
        response.on("data", (chunk) => { body += chunk; });
        response.on("end", () => resolveGet({ status: response.statusCode, body }));
      }).on("error", rejectGet).end();
    });
    const page = await get("/multi-player/vr/");
    assert.equal(page.status, 200);
    const urls = JSON.parse((await get("/api/urls")).body);
    assert.equal(urls.protocol, "https");
    assert.equal(urls.port, address.port);
    assert.ok(urls.hosts[0].pages.some((item) => item.url.endsWith("/multi-player/ar/")));
  } finally {
    delete process.env.TLS_KEY;
    delete process.env.TLS_CERT;
    await relay.close();
  }
});

test("autoTls serves HTTPS; INSECURE_HTTP=1 keeps plain HTTP", async () => {
  // autoTls uses the real cache dir (multi-player/.certs, gitignored).
  const secure = createRelay();
  try {
    assert.equal((await secure.listen({ host: "127.0.0.1", port: 0, autoTls: true })).protocol, "https");
  } finally {
    await secure.close();
  }
  process.env.INSECURE_HTTP = "1";
  const plain = createRelay();
  try {
    assert.equal((await plain.listen({ host: "127.0.0.1", port: 0, autoTls: true })).protocol, "http");
  } finally {
    delete process.env.INSECURE_HTTP;
    await plain.close();
  }
});
