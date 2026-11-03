# Demo checklist

Items about headsets, Wi-Fi and SteamVR have not been tested by the development agents: verify all of them at rehearsal.

## One week before
- [ ] Full rehearsal on the real lab PC, real Wi-Fi, real headsets, with someone playing the audience.
- [ ] Record the backup video and take screenshots (see [BACKUP-PLAN.md](BACKUP-PLAN.md)).
- [ ] Confirm the lab PC address does not change (ask IT about a reserved address, or use the PC name).

## Day before
- [ ] Pull the latest repo on the lab PC; run `scripts\start-demo.cmd` once with internet so `npm install` completes.
- [ ] Check Windows Firewall allowed Node.js on **Private** networks (first run shows a prompt). If the lab network is classified Public, inbound connections may be blocked.
- [ ] Charge: every Quest, controllers (spare batteries), the Vive wireless adapter power bank, Vive controllers, the presenter laptop.
- [ ] Update SteamVR and confirm it is the active OpenXR runtime ([VIVE-SETUP.md](VIVE-SETUP.md)).
- [ ] Open the Unity projects once so the first import is already done. The hub's props and people are local-only (not in git): use the hub folder copied from the development PC, and check the lobby and each portal load (see README, "Unity hub scenes are local only").
- [ ] Open the server URL in each Quest Browser and accept the certificate warning; confirm Enter VR works.
- [ ] If the certificate cache is older than a few weeks or the PC changed network, delete `multi-player/.certs/` so it regenerates, then re-accept on each headset.
- [ ] Print or save the QR page and the URLs.
- [ ] Run `npm test` in `multi-player/` (31 tests) and `node tools/vr-capture/offline-check.mjs` (confirms the pages need no internet).

## Day of (60 minutes before)
- [ ] Connect the PC to the same Wi-Fi as the headsets (a dedicated travel router is more predictable than shared campus Wi-Fi: verify at rehearsal).
- [ ] Run `scripts\start-demo.cmd /dashboard`; confirm printed addresses match the network in use.
- [ ] Open the QR page; test one Quest and one phone.
- [ ] Start SteamVR, check base stations/tracking, controllers paired, wireless adapter linked.
- [ ] Open the fallback video on the presenter screen, paused at the start.
- [ ] Guest Wi-Fi isolation ("client isolation") blocks device-to-device traffic. If headsets cannot reach the PC, use the USB fallback in [QUEST-SETUP.md](QUEST-SETUP.md).
- [ ] Disable PC sleep and screen saver; plug the PC into power.
- [ ] Agree room names for the demo (for example `demo1`, `demo2`).

## After
- [ ] Stop the server (Ctrl+C), collect headsets and chargers, note what failed in [TROUBLESHOOTING.md](TROUBLESHOOTING.md).
