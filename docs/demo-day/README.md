# Demo day guide (Nov 13, 2026, Pitt-Bradford)

Everything needed to run the advisory-board demo from one lab PC.

Status: the start script, certificate generation, URL list and QR page were run on a development PC (Node 22, Windows 11). **No Meta Quest or HTC Vive was used in any test.** Every hardware step in these documents is marked "verify at rehearsal".

## The one-command start

Double-click `scripts\start-demo.cmd` (or run `powershell -File scripts\start-demo.ps1`). Add `/dashboard` to the .cmd (or `-Dashboard` to the .ps1) to also start the status dashboard at the address it prints (a free port is chosen automatically).

It will:
1. Check Node.js is installed (and run `npm install` in `multi-player/` the first time; that needs internet once).
2. Start one server on **port 8443 over HTTPS** that serves the whole site (launcher, Circuit Lab, Site Safety Layout, training simulations) and the multiplayer relay.
3. Create a self-signed certificate on first run (cached in `multi-player/.certs/`, never committed) that covers `localhost`, the PC name and every network address of the PC.
4. Print every URL for every network adapter and open the launcher on the PC.

Manual equivalent: `cd multi-player`, `npm install`, `npm start`. Set `INSECURE_HTTP=1` for plain HTTP, or `TLS_KEY` and `TLS_CERT` to use your own certificate. `PORT` changes the port.

## Who opens what

| Device | Opens | Notes |
|---|---|---|
| Quest headsets | `https://<lab-PC-address>:8443/` in Quest Browser | accept the certificate warning once per headset; see [QUEST-SETUP.md](QUEST-SETUP.md) |
| QR codes | `https://localhost:8443/multi-player/qr.html` on the PC | scan to avoid typing addresses |
| Vive Pro 2 PC | desktop Chrome/Edge page, or the Unity project | see [VIVE-SETUP.md](VIVE-SETUP.md) |
| Status dashboard | the address it prints (a free port is chosen automatically) | optional, for the presenter only |

Everyone in an activity uses the same **room name**. Rooms are separate for Circuit Lab and Site Safety Layout.

## Documents

- [CHECKLIST.md](CHECKLIST.md) day-before and day-of
- [QUEST-SETUP.md](QUEST-SETUP.md), [VIVE-SETUP.md](VIVE-SETUP.md)
- [BACKUP-PLAN.md](BACKUP-PLAN.md), [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- [TALK-TRACK.md](TALK-TRACK.md) 15-minute outline

Scenario content is general safety-awareness practice, not a procedure or certification.
