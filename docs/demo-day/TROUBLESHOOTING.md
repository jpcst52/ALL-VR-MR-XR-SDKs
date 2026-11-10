# Troubleshooting

Confirm the items marked (verify) at rehearsal.

| Symptom | Likely cause | What to do |
|---|---|---|
| Start script says Node.js not found | Node not installed or not on PATH | Install the LTS from https://nodejs.org, then reopen the window |
| "Port 8443 is already in use" | another program (or an old copy of this demo) holds the port | `scriptsstart-demo.cmd` now moves to the next free port by itself and prints it; to stop an old copy run `scriptsstop-demo.cmd`; to see what is wrong run `scriptsdemo-doctor.cmd` |
| `npm install` fails | no internet on first run | connect once, or copy `node_modules` from another PC with the same Node version |
| Browser says "Your connection is not private" | expected: self-signed certificate | Advanced, then Proceed. Needed once per browser/device |
| Warning returns every time or after a network change | certificate was regenerated for new addresses | accept again; delete `multi-player/.certs/` to force a fresh certificate |
| Quest cannot open the page at all | different network, guest Wi-Fi client isolation, or Windows Firewall (verify) | same network; allow Node.js on Private networks; mark the network Private; or use the USB `adb reverse` path |
| Page loads but Enter VR is missing | not HTTPS, or the page was opened by an address not in the certificate (verify) | use an https:// address printed at start; restart so the certificate covers the current address |
| Two users do not see each other | different room names or different example (rooms are per example: the three multiplayer themes are separate) | same room name, same example |
| WebSocket will not connect | page opened over https but relay on http, or the other way round | use the single server started by the script; do not mix |
| SteamVR does not start the Unity view | SteamVR is not the active OpenXR runtime (verify) | see [VIVE-SETUP.md](VIVE-SETUP.md) section 2 |
| Unity hub scenes empty or without props or people | the pack and Rocketbox files are not in git | use the hub folder copied from the development PC; or use projects 01 to 04, which need no packs |
| Refinery Rounds wind says "simulated (no network)" | no internet; this is the built-in fallback | expected offline; nothing to fix |
| Unity project slow on first open | first asset import | open it the day before |
| Dashboard address unknown | started without `/dashboard`; the port is automatic, see tools/status/.dashboard-url | rerun with `/dashboard` or `npm run status` in the repo root |

Useful commands: `cd multi-player && npm test`; `node tools/vr-capture/offline-check.mjs`.

Never use port 8000 for the demo; it belongs to other tools on the owner's PC.
