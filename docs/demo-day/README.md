# Demo day guide (Nov 13, 2026, Pitt-Bradford)

Everything needed to run the advisory-board demo from one lab PC.

Status: the start script, certificate generation, URL list and QR page were run on a development PC (Node 22, Windows 11). Every hardware step in these documents is marked "verify at rehearsal". Screenshots in the repo are desktop browser captures, Unity editor renders (desktop) or an emulated Quest 3.

What the one server serves: the main menu (launcher), the Practice Lab (11 scenarios), and the three multiplayer themes (Circuit Lab, Site Safety Layout, Hydrostatic Test Rig). It does **not** serve the Unity projects: those run from the Unity editor on the Vive PC.

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

Everyone in an activity uses the same **room name**. Rooms are separate for each of the three multiplayer themes.

## Documents

- [HARDWARE-TEST-SESSION.md](HARDWARE-TEST-SESSION.md) first real-headset run, with pass/fail rows
- [CHECKLIST.md](CHECKLIST.md) day-before and day-of
- [QUEST-SETUP.md](QUEST-SETUP.md), [VIVE-SETUP.md](VIVE-SETUP.md)
- [BACKUP-PLAN.md](BACKUP-PLAN.md), [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- [TALK-TRACK.md](TALK-TRACK.md) 15-minute outline

## Unity hub scenes are local only

The Unity hub (lobby and its five scenes) uses Asset Store props and Rocketbox people that are not in git. A fresh clone shows no art. For the Vive part of the demo, use the hub project folder as it exists on the development PC (copy the whole folder, including `Assets/Scenes/Local` and `Assets/Baked`), or open projects 01 to 04, which run from primitives in a fresh clone. Check this at rehearsal.

The Refinery Rounds project reads live wind from Open-Meteo when online and uses a simulated value when offline; everything else in the demo needs no internet.

Scenario content is general safety-awareness practice, not a procedure or certification.
