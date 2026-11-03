// Exit code 0 when the given port is served by this demo's relay (it answers /healthz), 1 otherwise.
// Used by start-demo, stop-demo and demo-doctor: Node is already required, and unlike Windows PowerShell 5 it does not
// stall on proxy auto-detection or complain about the self-signed certificate.
// Usage: node scripts/relay-probe.mjs <port>
process.removeAllListeners("warning"); // the self-signed-certificate warning is expected here
process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const port = Number(process.argv[2]);
if (!Number.isInteger(port)) { console.error("usage: node relay-probe.mjs <port>"); process.exit(2); }
for (const scheme of ["https", "http"]) {
  try {
    const response = await fetch(`${scheme}://127.0.0.1:${port}/healthz`, { signal: AbortSignal.timeout(2500) });
    const body = await response.json();
    if (body?.relay === true) { console.log(scheme); process.exit(0); }
  } catch { /* not this scheme */ }
}
process.exit(1);
