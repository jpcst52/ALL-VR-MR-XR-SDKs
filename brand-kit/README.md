# Brand kit (web)

Shared look for the browser examples: the University of Pittsburgh at Bradford + CIST (Computer Information Systems & Technology) brand bar for HTML pages, and a small in-world panel kit for the Three.js scenes. No dependencies and no network access: fonts come from `../vendor/fonts`.

| File | What it does |
|---|---|
| `brand.css` | Pitt palette tokens (Royal `#003594`, Gold `#FFB81C`, Medium Blue `#00205B`, New Sky `#DBEEFF`), `@font-face` for League Gothic, Merriweather and Roboto, and the `.pitt-brand` bar |
| `brand.js` | Fills every `<div data-pitt-brand>` (or `data-pitt-brand="compact"`) with the logo or the text wordmark, a divider, and the CIST mark |
| `panels.js` | Three.js helpers: `textPanelTexture` / `textPanelMesh` (kicker + wrapped body on a rounded, bordered panel with an accent bar; optional brand band), `tagSprite` (name tags and hotspot labels: a pill with a status chip), `plateTexture` and `drawSignLines` (text on physical signs and plates), `brandPlate` (the Pitt-Bradford + CIST plaque in a scene) |
| `panels.js` (menu cards) | `thumbCardTexture` / `drawThumbCard`: a menu card with a rounded 16:9 picture on the left and kicker + title on the right; with no picture it draws a tinted placeholder tile (used by the Practice Lab's VR scenario menu) |
| `season-sky.js` | `createSeasonSky({ manifestUrl })`: a sky sphere with one equirect image per season, rotating every 30 s with a pixelate + scan-line + colour-flicker transition done in the sky's own shader (stereo-safe, no post-processing); cross-fade with prefers-reduced-motion; pauses while the tab is hidden; stays hidden (no errors) if the manifest or images are missing. API: `start()`, `stop()`, `nextSeason()`, `label`, `caption`, `credits`, `setEnabled()`, `update(time, camera)` each frame |
| `season-sky-core.js` | The pure parts of the sky (season by date, manifest contract `assets/skies/skies.json`, fallbacks, transition curve, clock, "regional style" wording unless `isBradford`); tested in `test/season-sky.test.mjs` |
| `layout.js` | Pure text wrapping and fit-to-box (shrink, then ellipsis) used by `panels.js`; unit-tested |
| `test/brand.test.mjs` | Static checks (run `npm test` here, or `node --test` in this folder) |

## The logo is optional and never committed

The official logo is `branding/logo.png`, which is git-ignored (public repository; permission to publish it is still an open question for the owner). The kit only **references** it by URL:

- If the file is on the machine (the lab PC), the brand bar and the in-world plaque show it.
- If it is missing (a fresh clone), they show a text wordmark: "University of Pittsburgh / Bradford" + "CIST".
- The `<img>` is inserted only after the file has loaded, so a missing logo never shows as a broken image. The only trace is one 404 request for `branding/logo.png`, which the checks allow.

Never copy the logo into `brand-kit/` or any other tracked folder. The screenshot tools (`tools/vr-capture/readme-shots.mjs`, `capture.mjs`, `ui-check.mjs`) block `branding/` when taking README screenshots, so committed images show the wordmark only.

## Rules for scene text

- All text in a 3D scene goes through `panels.js`. Do not call `fillText` in scene code; `test/brand.test.mjs` fails if a file in `training-simulations/` or `multi-player/shared/` does.
- Canvas width follows the plane's aspect ratio (see `canvasWidthFor` in `training-simulations/app.js`) so text is never stretched.
- Panels redraw once the bundled fonts (and the optional logo) have loaded (`brandAssetsReady`).

## Checks

- `node --test` in this folder: brand bar on every page, no CDN, no hard-coded logo `<img>`, `branding/` untracked, no bare canvas text in scene code, palette in the stylesheets, and text fitting.
- `node tools/vr-capture/ui-check.mjs`: loads the launcher, the Practice Lab (welcome, catalog, task, feedback, result, Circulation Builder) and the three multiplayer pages at 1440 x 900 and 390 x 844, with the logo blocked and (if present) allowed. See [tools/vr-capture/README.md](../tools/vr-capture/README.md).
