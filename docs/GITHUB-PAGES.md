# GitHub Pages: the WebXR examples on any headset browser

The browser examples are published as a static website so a headset or phone can open them by typing or scanning an address. No PC, relay or certificate warning is needed.

**Address:** `https://jpcst52.github.io/ALL-VR-MR-XR-SDKs/`

![QR code for the GitHub Pages address](images/pages-qr.png)

The same code is drawn offline by the site's own `qr.html` page (`https://jpcst52.github.io/ALL-VR-MR-XR-SDKs/qr.html`), which also lets you type a different address. I generated the QR image with the repository's bundled generator but have not scanned it with a phone, so scan it once before you print it.

**Status: set up but not yet live or tested.** The site builds and passes the local check described below (served under the same sub-path). It has not been pushed or opened on the real Pages host, and nothing here has been tried on any headset.

## What works on Pages and what does not

| Item | On Pages | Notes |
|---|---|---|
| Main menu (launcher) and the guide viewer | Works | Guides open in the menu's built-in Markdown viewer, offline once loaded |
| Fieldwork XR Practice Lab (`training-simulations/`) | Works | Single user, all scenarios; desktop view and ENTER VR where the browser supports it |
| Three.js, A-Frame, Babylon.js, plain WebXR demos | Work | Each has a desktop fallback |
| Circuit Lab, Site Safety Layout, Hydrostatic Test builder (`multi-player/`) | Pages load but **cannot join a room** | They need the WebSocket relay (`multi-player/server.mjs`), and Pages serves static files only. The pages and the menu say so |
| Unity projects (`vive-steamvr/`) | Not on Pages | Unity players are built with `scripts\build-vive.ps1` ([BUILDS.md](BUILDS.md)). Their README guides are readable in the menu |
| Slide decks (`.pptx`) | Not on Pages by default | About 90 MB together. `node scripts/build-webxr.mjs --with-decks` includes them |

### The multiplayer pages and the relay

A relay on the lab PC is an HTTPS server with a self-signed certificate. A browser blocks a plain `ws://` connection from an `https://` page, and it also refuses a `wss://` relay whose certificate it does not trust. So a Pages copy of the page cannot simply be pointed at the lab PC. The simple way that works is to open the lab PC's own address (`https://<lab-pc>:8443/`, printed by `scripts\start-demo.cmd`), which serves the same pages and the relay together; see [HOW-TO-RUN.md](HOW-TO-RUN.md).

For a relay that has a certificate the browser trusts, the multiplayer pages accept `?relay=wss://host:port` (for example `.../multi-player/vr/?relay=wss://relay.example.org:8443`). That option is untested against a real relay.

## Open it on a headset or phone

WebXR needs HTTPS, which Pages provides. The first visit downloads the files it needs: the menu is small; the single-library demos are roughly 4 to 6 MB each; the Practice Lab loads 3D models and sky images and is much larger (tens of MB; the whole site is about 130 MB). Sizes are from the built folder, not measured on a network. After the first load the browser caches them.

1. Put the headset's browser at the address above (type it, or scan the QR code with a phone and send the link, or use `qr.html`).
2. Pick a tile. Open the example on the page. Where the browser and device offer an immersive session, an **ENTER VR** (or ENTER AR) button appears; press it. If there is no button, the page stays in its desktop view.
3. Allow any permission prompt the browser shows.

| Device and browser | Where to type the address | Immersive mode | Tested |
|---|---|---|---|
| Meta Quest 2, 3, 3S (Quest Browser) | Browser address bar, or Meta's phone app can send a link | VR and passthrough AR; press ENTER VR. Check the browser's WebXR support for your firmware | NOT TESTED |
| HoloLens 2 (Microsoft Edge) | Edge address bar with the keyboard or voice | AR only (see-through display). Content looks small and dark colours disappear: read [HOLOLENS-AR-NOTES.md](HOLOLENS-AR-NOTES.md). Check the browser's WebXR support | NOT TESTED |
| HTC Vive XR Elite, Vive Focus 3 (the headset's own browser) | Browser address bar | Check the browser's WebXR support on your headset's software version | NOT TESTED |
| Pico headsets (Pico Browser) | Browser address bar | Check the browser's WebXR support on your headset's software version | NOT TESTED |
| Android phone (Chrome) | Address bar or scan the QR code | AR sessions need ARCore support on the phone; check the browser's WebXR support | NOT TESTED |
| iPhone or iPad (Safari) | Address bar or scan the QR code | Check the browser's WebXR support: the pages still run as normal 3D pages | NOT TESTED |
| Windows PC with a SteamVR headset (Chrome or Edge) | Desktop browser | Check the browser's WebXR support with SteamVR set as the OpenXR runtime | NOT TESTED |

For this repository's tested state of each example, see [TESTING.md](TESTING.md) and the badges in the menu. Nothing on Pages has been run on a headset.

## Update it

Push to `main`. The workflow `.github/workflows/pages.yml` builds the site, runs the check and redeploys; it can also be started by hand from the repository's Actions tab ("Deploy WebXR site to GitHub Pages", Run workflow). A deploy takes a few minutes.

One-time setup (repository owner): Settings, Pages, Build and deployment, Source: **GitHub Actions**.

## Build and check the site locally

From the repository root (Node 22; the check uses the Playwright installed in `tools/vr-capture` and Chrome):

```
npm run build:site        # node scripts/build-webxr.mjs  -> site/
npm run check:site        # node tools/pages/site-check.mjs
node tools/pages/serve-subpath.mjs        # serve site/ at http://localhost:8151/ALL-VR-MR-XR-SDKs/
```

`scripts/build-webxr.mjs` assembles `site/` (git-ignored): the launcher as `index.html`, the browser examples, `vendor/`, `assets/`, the multi-player pages (not the relay server, tests or `node_modules`), the guides the menu opens and the images they use (pictures in the browser examples' guides up to 3 MB, in other guides up to 700 KB; larger ones show as a missing image in the viewer), `.nojekyll`, `404.html` and `qr.html`. It leaves out `vive-steamvr`, `tools`, `branding` (except the approved `logo.png`), `assets-private`, tests and `node_modules`. It prints the total size and the biggest files.

The check serves `site/` under `/ALL-VR-MR-XR-SDKs/` (a sub-path, like Pages, so a link starting with `/` would fail), blocks every non-local request, and then: loads each web page in the menu with no console error or failed request, confirms every tile, screenshot and guide link resolves, opens guides in the viewer, checks `404.html`, and pretends the host is `github.io` to confirm the menu and the relay pages say there is no relay. It uses an emulated desktop browser, not a headset.

## Related

[BUILDS.md](BUILDS.md) (Vive player builds), [HOW-TO-RUN.md](HOW-TO-RUN.md) (running everything on the lab PC), [HOLOLENS-AR-NOTES.md](HOLOLENS-AR-NOTES.md).

## The flat list page (`pages.html`)

`pages.html` (`https://jpcst52.github.io/ALL-VR-MR-XR-SDKs/pages.html`) is one plain page with a large link for every WebXR page in this repository (read from `launcher/examples.json`, so it stays current) and for the other GitHub Pages sites: Alumni Weekend 2026 and the BMW 325xi 3D viewer. It is meant to be opened once in a headset browser and bookmarked. To add another site, add a line to the `OTHER_SITES` list at the bottom of the file.
