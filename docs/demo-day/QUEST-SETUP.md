# Quest setup

**Verify at rehearsal.** The server side (HTTPS, certificate, URL list) was tested from a desktop.

## Option A: over Wi-Fi (primary)

1. PC and Quest on the same Wi-Fi network. Start the demo with `scripts\start-demo.cmd`.
2. In Quest Browser open `https://<PC-address>:8443/` using an address printed in the PC window (or scan a code from `https://localhost:8443/multi-player/qr.html`).
3. The first time, the browser warns that the certificate is not trusted because it is self-signed. Choose the Advanced option and proceed. Verify at rehearsal how Quest Browser words and remembers this choice.
4. Open the Practice Lab, Circuit Lab, Site Safety Layout or Hydrostatic Test Rig, enter a name (and the shared room name on the multiplayer pages), press Enter VR / Enter AR. Verify at rehearsal that the headset grants the immersive session.
5. If the PC's address changes (new network), the certificate is regenerated and each headset needs to accept it again.

Why HTTPS: WebXR is only available in secure contexts, so a headset reaching the PC by an IP address needs HTTPS.

## Option B: USB with `adb reverse` (offline fallback)

Uses a cable instead of Wi-Fi. Per Meta's browser-debugging documentation, you need Developer Mode on the headset, Android Platform Tools (`adb`) on the PC, and a USB connection; `adb reverse` maps a headset port to a PC port, after which Quest Browser opens `localhost` on that port. Source: https://developers.meta.com/vr/documentation/web/browser-remote-debugging/

1. Enable Developer Mode (Meta documentation: see the link above) and connect the Quest by USB; accept the USB debugging prompt in the headset.
2. On the PC: `adb devices` (the headset should be listed), then `adb reverse tcp:8443 tcp:8443`.
3. In Quest Browser open `https://localhost:8443/`. Accept the certificate warning if shown, or start the server with `INSECURE_HTTP=1` and open `http://localhost:8443/` (browsers generally treat localhost as a secure context; verify at rehearsal that Enter VR is offered).
4. Repeat `adb reverse` after unplugging and replugging the cable. Reverse mappings are for one headset connection at a time unless you pass `-s <serial>` for each (verify at rehearsal with more than one headset).

## Before guests arrive
- Test each headset once and note which pass.
- Keep a spare headset charged.
- Use the same room name on every device in a group.
