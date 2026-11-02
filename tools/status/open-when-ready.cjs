// Waits for server.cjs to write .dashboard-url, then opens it in the default browser (used by start-status.cmd).
const fs = require('fs'), path = require('path'), { exec } = require('child_process');
const f = path.join(__dirname, '.dashboard-url');
try { fs.unlinkSync(f); } catch {}
let n = 0;
const t = setInterval(() => {
  try { const u = fs.readFileSync(f, 'utf8').trim(); if (u) { clearInterval(t); exec(`start "" ${u}`); } } catch {}
  if (++n > 40) clearInterval(t);
}, 250);
