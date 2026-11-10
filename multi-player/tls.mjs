// Self-signed certificate helper for the lab-PC demo.
// A certificate is generated once, cached in multi-player/.certs/ (gitignored),
// and regenerated when it is near expiry or the machine's IP addresses change.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { hostname, networkInterfaces } from "node:os";
import { dirname, join } from "node:path";
import { X509Certificate } from "node:crypto";
import { fileURLToPath } from "node:url";
import selfsigned from "selfsigned";

const HERE = dirname(fileURLToPath(import.meta.url));
export const CERT_DIR = join(HERE, ".certs");
const DAY_MS = 24 * 60 * 60 * 1000;

/** Non-internal IPv4 addresses, grouped per adapter: [{ name, address }]. */
export function lanAddresses() {
  const found = [];
  for (const [name, entries] of Object.entries(networkInterfaces())) {
    for (const entry of entries ?? []) {
      if (entry.family === "IPv4" && !entry.internal) found.push({ name, address: entry.address });
    }
  }
  return found;
}

/** Names the certificate should cover. */
export function certificateHosts() {
  const host = hostname();
  const ips = lanAddresses().map((item) => item.address);
  return { dns: [...new Set(["localhost", host])], ips: [...new Set(["127.0.0.1", ...ips])] };
}

function subjectAltNames({ dns, ips }) {
  return [
    ...dns.map((value) => ({ type: 2, value })),
    ...ips.map((ip) => ({ type: 7, ip })),
  ];
}

function cachedIsUsable(certPem, hosts) {
  try {
    const cert = new X509Certificate(certPem);
    if (new Date(cert.validTo).getTime() - Date.now() < 30 * DAY_MS) return false;
    const san = cert.subjectAltName ?? "";
    return hosts.dns.every((name) => san.includes(`DNS:${name}`))
      && hosts.ips.every((ip) => san.includes(`IP Address:${ip}`));
  } catch {
    return false;
  }
}

/** Generate a self-signed certificate in memory. Returns { key, cert } PEM strings. */
export async function generateCertificate({ hosts = certificateHosts(), days = 365 } = {}) {
  const pems = await selfsigned.generate(
    [{ name: "commonName", value: "XR Training Demo (self-signed)" }],
    {
      days,
      keySize: 2048,
      algorithm: "sha256",
      extensions: [
        { name: "basicConstraints", cA: false },
        { name: "keyUsage", digitalSignature: true, keyEncipherment: true },
        { name: "extKeyUsage", serverAuth: true },
        { name: "subjectAltName", altNames: subjectAltNames(hosts) },
      ],
    },
  );
  return { key: pems.private, cert: pems.cert };
}

/** Load the cached certificate or create it. Returns { key, cert, created, dir }. */
export async function ensureCertificate({ dir = CERT_DIR } = {}) {
  const keyPath = join(dir, "key.pem");
  const certPath = join(dir, "cert.pem");
  const hosts = certificateHosts();
  if (existsSync(keyPath) && existsSync(certPath)) {
    const cert = readFileSync(certPath, "utf8");
    if (cachedIsUsable(cert, hosts)) {
      return { key: readFileSync(keyPath, "utf8"), cert, created: false, dir };
    }
  }
  const made = await generateCertificate({ hosts });
  mkdirSync(dir, { recursive: true });
  writeFileSync(keyPath, made.key, { mode: 0o600 });
  writeFileSync(certPath, made.cert);
  return { ...made, created: true, dir };
}

/** Pages shown on the start screen and the QR page. */
export const DEMO_PAGES = [
  { id: "launcher", label: "Launcher", path: "/" },
  { id: "circuit", label: "Circuit Lab (VR)", path: "/multi-player/vr/" },
  { id: "safety", label: "Site Safety Layout (AR)", path: "/multi-player/ar/" },
  { id: "ar-circuit", label: "AR Circuit Lab", path: "/multi-player/ar-circuit/" },
];

/** URL list for each network adapter (plus localhost). */
export function demoUrls({ protocol, port }) {
  const hosts = [{ adapter: "this PC", host: "localhost" }, ...lanAddresses().map((item) => ({ adapter: item.name, host: item.address }))];
  return hosts.map(({ adapter, host }) => ({
    adapter,
    host,
    pages: DEMO_PAGES.map((page) => ({ ...page, url: `${protocol}://${host}:${port}${page.path}` })),
  }));
}
