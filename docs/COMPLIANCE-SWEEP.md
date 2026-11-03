# Compliance sweep (November 3, 2026, round 2)

Scope: partner and company names, slogan-style titles, licenses and credits, private files in git, and review of the three decks and `docs/demo-day/`. Method: `git grep` over tracked files, `git ls-files` checks, text extraction and rendered slides (PowerPoint export) for the decks. Rules applied: the owner's decisions of November 1, 2026 (no company names on slides, say "inspired by regional industry"; the lighter scenario stays generic; scenario content is general safety awareness, never a procedure or certification).

## 1. Partner and company names

Searched all tracked files for the names of local employers, the owner's employer, and the biology faculty partner.

Result: **no company name appears in any public-facing file** (README, web page, scenario text, Unity on-screen text, deck slide or deck notes). The three .pptx files were unzipped and checked: no hits in slide text or notes.

Update, 2026-11-03: at the owner's request the names were also removed from the status docs (`docs/PROJECT-STATUS.md`, `docs/Task-Status.md`, `tools/status/agents.json`, the activity log and the research notes), which now say "local employers" and "biology faculty". `CLAUDE.md` holds the owner's working notes with those names, so it is no longer tracked: it is in `.gitignore` and stays on the owner's machine only. Earlier commits in the public history still contain the old text; removing it would mean rewriting history and force-pushing, which has not been done.

| File | Hit | Kind | Note |
|---|---|---|---|
| `training-simulations/test/biology.test.mjs:94` | Test asserts the names are NOT present in scenario text | Legitimate guard test | Keep. It lists the names as test data only |
| `assets/skies/CREDITS.md:36`, `docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md` | "Allegheny National Forest" (geography); research authors | Not a partner | None |

Generic wording that is fine as is (no name, says "inspired by"): `training-simulations/partner-scenarios.js` settings, `training-simulations/README.md:7`, `training-simulations/scenarios.js:77` ("inspired by local manufacturers"), `vive-steamvr/04-resistor-bench/README.md:32` ("makers of passive electronic components... makes no claims about any company"), `multi-player/README.md:22` and `multi-player/hydrotest/index.html:54` ("inspired by heat-exchanger and filter-housing makers").

Optional hardening (owner's call, other agents' files):

- **Lighter scenario title.** "Sample and test a lighter lot" (`training-simulations/partner-scenarios.js:15`, `training-simulations/README.md:9` and `:175`, `reasons.js:295`, id `lighter-lot-sampling`, thumbnails, `tools/vr-capture/readme-shots.mjs:70`, `test/scenarios.test.mjs:28`) points at one well-known local lighter maker even though no name is used. If the owner wants the link to any one local maker to be fully invisible: title "Sample and test a product lot" and "ignition-source" wording. Changing the id touches tests, thumbnails and capture scripts, so only the visible title/description would be the low-risk change.
- **"inspired by heat-exchanger and filter-housing makers"** (`multi-player/README.md:22`, `multi-player/hydrotest/index.html:54`) and **"makers of passive electronic components"** (`vive-steamvr/04-resistor-bench/README.md:32`) narrow the field to one or two local firms. Proposed text on the public page: "Inspired by regional industry; generic parts only." The deck slides already use this wording.
- **File and variable names** `partner-scenarios.js`, `partner-stations.js` (`training-simulations/`) say "partner" but contain no company. Harmless; rename only if the owner prefers `manufacturing-scenarios.js`.

Fixed in files I own: the decks. The verified-status slide used to say "Company names appear only as inspired by" (contradicts the decision); it now says no company names appear. Deck text "Inspired by local manufacturers", "inspired by component makers" and "inspired by heat-exchanger makers" now reads "generic, inspired by regional industry" or "Generic component bench". `docs/demo-day/TALK-TRACK.md` no longer says names may appear "unless approved".

## 2. Slogan-style titles

Searched README, HTML, launcher JSON, brand kit, Unity C# strings, and decks for two-short-phrase taglines. **No slogan-style title exists outside the decks** (no "Small scenarios. Better decisions." anywhere in tracked files).

In the decks (fixed): "A learning gallery, not a single XR product" became "What this repository is"; "Thirteen examples across five pathways" became "The thirteen examples, grouped by platform"; "The web lane: one API, four ways to build" became "Browser XR: WebXR and three libraries"; "Native and engine examples show distinct capabilities" became "Native and engine SDK examples"; "Mobile AR and spatial computing need different toolchains" became "Mobile AR and visionOS examples"; "Three themes, one relay: build together in VR and AR" became "Multiplayer WebXR: three themes on one relay"; "Sixteen scenarios, every one hands-on" became "Fieldwork XR Practice Lab: 17 hands-on scenarios"; "How it looks in a headset..." became "Emulated Quest 3 captures"; "Start with the capability, then check the environment" became "How to get started"; "Reality check: ..." became "Requirements and limits"; "A map of XR, best explored one capability at a time" became "Summary"; "A local status dashboard keeps the project honest" became "Local status dashboard"; "Third theme: build a hydrostatic test rig together" became "Hydrostatic Test Rig: build it together"; "Build together in VR and AR" (multiplayer title slide) became "Multiplayer WebXR examples"; the run-guide tagline "Separate samples. Real SDKs. Hardware where required." became "Hardware needed only where noted."; "A small, credible slice beats a broad claim." became a plain scope sentence.

## 3. Licenses and credits

| Item | Where it is credited | Status |
|---|---|---|---|
| Rocketbox (MIT, Microsoft) | `vive-steamvr/hub/README.md` "People" section: license, source URL, citation (Gonzalez-Franco 2020), license text kept at `Assets/Rocketbox/LICENSE.md` locally | Present. Suggest also adding one line to the root README credits so a reader of the public repo finds it (files are not committed, so the notice is informational) |
| Poly Haven (CC0) | `assets/models/CREDITS.md`, `assets/skies/CREDITS.md`, `multi-player/CREDITS.md`, `CREDITS-web-demos.md`, per-demo READMEs | Present, per-model author and URL |
| Quaternius (CC0) | `assets/models/CREDITS.md` | Present |
| KayKit (CC0) | `multi-player/CREDITS.md` (pack, author Kay Lousberg, per-file table and source URL) | Present |
| Babylon.js loaders | `CREDITS-web-demos.md:40` (7.42.0, Apache-2.0, `babylonjs/lib/babylonjs.loaders.min.js`, copied from the jsDelivr copy of the npm package); `babylonjs/README.md:8, 45` | Present. The file is vendored locally; no page loads from jsDelivr at run time (`git grep` found no CDN URLs outside `vendor/`) |
| Three.js, Babylon.js, A-Frame, QR code | `vendor/README.md` table; `vendor/*/LICENSE` files tracked | Present |
| Fonts (Roboto, Merriweather, League Gothic) | `vendor/README.md:10` (SIL OFL 1.1), `CREDITS-web-demos.md:42` | **Gap:** no OFL license text file is tracked under `vendor/fonts/` (only the table entry). Recommend adding the OFL 1.1 text plus each font's copyright line (Fontsource packages carry them) |
| Open-Meteo (live wind, Refinery Rounds) | `vive-steamvr/02-refinery-rounds/README.md` | Present. Open-Meteo's terms ask for attribution for free API use; confirm the on-screen board or README carries it |
| Meta IWER emulator | `tools/vr-capture/README.md` and several READMEs | Present |
| Asset Store packs (IG warehouse, Sci-Fi Warehouse, Modern Office, Shipping Container, Warehouse, Courtyard) | hub README ("licenses do not allow redistributing") | Not committed; no credit file in the public repo. Optional: list pack names and publishers in a hub credits section |

Private-file checks (`git ls-files`):

- `branding/`: only `branding/logo.png` is tracked, which is the logo the owner approved on 2026-11-02. No brand guidelines PDF and no other variant is tracked. OK.
- `assets-private/`: nothing tracked. OK.
- No `.pdf`, `.unitypackage`, or `Assets/Rocketbox`, `Assets/Scenes/Local` or pack folders tracked in `vive-steamvr/hub`. The only hub files matching "Rocketbox" are our own scripts (`Assets/Editor/Rocketbox/RocketboxImport.cs`, `.meta`). OK.
- `vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/Resources/Branding/pitt-bradford.png` (shield) is tracked: the documented exception.
- Screenshots under `vive-steamvr/*/screenshots/` and `docs/images/hub-dressed-*.png` are renders of local Asset Store and Rocketbox content. CLAUDE.md allows Asset Store packs "in local Unity builds, never committed" and the files themselves are not committed; whether publishing screenshots of licensed art is acceptable under each pack's terms is not checked here. Owner may want to confirm per pack.

## 4. Deck review

Rendered all slides (PowerPoint export to PNG) and viewed them. Slide counts now: overview 26, run guide 26, multiplayer 13.

Stale facts fixed: scenario count 16 to 17 (title chip, portfolio, lab slide, run guide, verified slide); "Hydrostatic Test Rig (new)" and "NEW / THIRD THEME" labels; "The lobby with portals is not started" (hub slide) replaced by the current state; verified slide now lists the automated suite counts (Practice Lab 27, brand kit 16, relay 31, web layout 48), scripted self-tests, and "frame rate of the hub scenes" as unverified; launcher slide mentions search and keyboard navigation and the full tile set; kit list includes HARDWARE-TEST-SESSION; Practice Lab slide adds the life-science draft group; overlapping callout on the research-features slide.

Over-claim check: every Unity, Quest and Vive statement says desktop editor, emulated Quest 3, or not verified. No slide says anything works on hardware.

Slides added to the overview deck (5): "What the system is", "Practice Lab scenes with real props and people" (realism-after shots), "Multiplayer rooms with props and people" (avatar shots, also added to the multiplayer deck), "Unity hub: lobby with portals to each scenario" (hub-dressed shots, also added to the run guide), and "Status and next steps". Together with the existing slides these cover: what the system is, the four Unity projects with screenshots, the web lab, multiplayer, the research angle, status, and verified versus not.

Left for the owner:

- `docs/Task-Status.md` E5 still says "56 slides"; the decks now total 65. Counts quoted elsewhere may need the same refresh.
- The catalog screenshot on the Practice Lab slides (`training-simulations/screenshots/02-catalog.png`) may predate the 17-scenario catalog and the new menu; re-shoot it (not mine to regenerate).
- Practice Lab realism screenshots are full-page captures scaled to a quarter-slide, so their small text is not legible. Crop shots would read better.
- The research slide says "76 references". `references.bib` has 76 entries; 40 were checked only at abstract level (the slide says so).

## 5. Demo-day documents

Checked `docs/demo-day/*.md` against `scripts/start-demo.cmd/.ps1`, `multi-player/server.mjs`, the hub README and TESTING.md. Start script behavior (Node check, `npm install` on first run, port 8443 HTTPS, self-signed cert in `multi-player/.certs/`, `/dashboard` option, QR page at `/multi-player/qr.html`) matches the docs. Fixes made:

- README: states what the one server serves (launcher, Practice Lab with 17 scenarios, three multiplayer themes) and that it does not serve Unity; rooms are separate per theme (three, not two); new section that the Unity hub's props and people are local-only, so the lab PC needs the hub folder copied from the dev PC; live wind needs internet but falls back to a simulated value.
- TALK-TRACK: removed "nothing needs the internet" (live wind does, optionally); added the no-company-names rule and that nothing about learning outcomes was measured; Vive segment is now lobby plus one scenario; mentions the hard-hat characters in the multiplayer segment.
- CHECKLIST: hub art folder check; relay test count (31).
- VIVE-SETUP: lists all four projects plus the hub; replaced "not compiled or tested" with the accurate state (compiles and runs in the editor in desktop mode, no headset); Practice Lab URL for the Vive PC.
- QUEST-SETUP: Practice Lab and Hydrostatic Test Rig added; BACKUP-PLAN: hub art caveat for a second PC and a fuller backup video path; TROUBLESHOOTING: rows for missing hub art and the offline wind message; HARDWARE-TEST-SESSION: portal dwell time 1.4 s as in the hub README.

Unverified: nothing in the demo-day kit was run on a Quest or Vive. I did not start a server in this pass, so the URL paths were read from the code and docs, not re-run.
