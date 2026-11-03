// Writes docs/images/pages-qr.png: a QR code of the GitHub Pages address, made with the repo's vendored generator.
//   node tools/pages/make-qr.mjs [url]
import { readFileSync, writeFileSync } from "node:fs";
import { deflateSync } from "node:zlib";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const repo = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const url = process.argv[2] ?? "https://jpcst52.github.io/ALL-VR-MR-XR-SDKs/";
const qrcode = new Function(readFileSync(join(repo, "vendor", "qrcode", "qrcode.js"), "utf8") + "; return qrcode;")();
const q = qrcode(0, "M");
q.addData(url);
q.make();
const n = q.getModuleCount(), cell = 10, margin = 4, size = (n + margin * 2) * cell;
const raw = Buffer.alloc((size + 1) * size, 255); // each row: filter byte 0, then 8-bit gray
for (let y = 0; y < size; y++) {
  raw[y * (size + 1)] = 0;
  for (let x = 0; x < size; x++) {
    const mx = Math.floor(x / cell) - margin, my = Math.floor(y / cell) - margin;
    if (mx >= 0 && my >= 0 && mx < n && my < n && q.isDark(my, mx)) raw[y * (size + 1) + 1 + x] = 0;
  }
}
const crcTable = Array.from({ length: 256 }, (_, c) => { for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
const crc = (b) => { let c = 0xffffffff; for (const x of b) c = crcTable[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const chunk = (type, body) => { const t = Buffer.from(type), len = Buffer.alloc(4), sum = Buffer.alloc(4); len.writeUInt32BE(body.length); sum.writeUInt32BE(crc(Buffer.concat([t, body]))); return Buffer.concat([len, t, body, sum]); };
const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(size, 0); ihdr.writeUInt32BE(size, 4); ihdr[8] = 8; ihdr[9] = 0;
const png = Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]);
writeFileSync(join(repo, "docs", "images", "pages-qr.png"), png);
console.log(`wrote docs/images/pages-qr.png (${size}x${size}) for ${url}`);
