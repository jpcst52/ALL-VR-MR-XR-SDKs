# Fieldwork XR Practice Lab

A browser-based XR training example with a learner name field and eleven short, hands-on practice scenarios in three groups: six high-risk industrial tasks, four manufacturing-quality exercises, and one **draft** life-science activity (Circulation Builder, see below). Every activity has immediate feedback, a takeaway principle, and a session score. Every scenario is **hands-on**: each has four tasks that you carry out yourself by selecting labelled hotspots on an animated 3D work area (inspect the scaffold tag, clip your lanyard to an anchor, apply your lock and tag, close a cylinder valve), rather than choosing from a list of answers. Wrong actions show their consequence, steps have to be done in a safe order, and dangerous mistakes are flagged as **critical safety misses**.

**Mechanical, not conversational.** An earlier version also had six everyday business and research scenarios (welcoming a walk-in customer, a spill in a stockroom, checking a delivery, inviting a research participant, sample custody, a field observation). They were removed on November 8, 2026: they were simplistic, and the customer-service and consent ones depended on talking to someone, which cannot be done in this lab. The lab now keeps only work that is done with the hands and equipment.

The industrial scenarios cover work at height, running a refinery unit from a control room, outdoor rounds at refinery battery limits (the edge of a process unit), window cleaning from a suspended platform on a skyscraper, welding and hot work, and electrical isolation. They are general awareness practice built around widely taught controls (permits, fall arrest, lockout/tag/test, gas alarms, fire watch) and do not replace site procedures.

The **Manufacturing quality** group (accent `#66B2E3`, heading MANUFACTURING QUALITY PRACTICE) is shaped around the kinds of work done at local manufacturers, and is **inspired by those kinds of work only**. It does not describe any real company's process, product, or procedure:

- **Sample and test a lighter lot** (`lighter-lot-sampling`): draw a random sample from a large lot and label it with the lot ID, check the fill in a ventilated hood away from ignition sources, flame-test one unit at a time with a fire extinguisher and blanket at hand, then record the result and quarantine a failed lot.
- **Hydrostatic test of a heat exchanger** (`heat-exchanger-hydrotest`): fit blind flanges, open the vent and fill, close the vent when full, raise to the test pressure without exceeding it and hold from a safe position, then stop the pump, drain, and vent.
- **Weld a refinery pipe spool** (`pipe-spool-welding`): verify the welding procedure, welder qualification, and heat numbers; clean, fit up, preheat, and tack; set up hot-work controls (permit, isolation, gas test, fire watch); then inspect, request NDE, mark, and record.
- **Select and check a resistor** (`resistor-selection`): ESD strap first, read the color code and measure out of circuit, choose 150 ohm (5 V supply, 20 mA, 2 V LED drop) with a 1/4 W rating and 5% tolerance, then choose an anti-sulfur type for a sulfur-rich place and check derating before installing.

The **Life science (draft for faculty review)** group (accent `#ff7b8a`, heading LIFE SCIENCE PRACTICE · DRAFT FOR FACULTY REVIEW) holds one scenario:

- **Circulation Builder** (`circulation-builder`), **Draft for faculty review**: on a simplified front-view schematic (lungs on top, heart in the middle, body tissues below), place the four heart chambers; connect the pulmonary circuit in blood-flow order (venae cavae, tricuspid valve, pulmonary artery, lung capillaries); connect the systemic circuit in order (pulmonary veins, mitral valve, aorta, body capillaries); then colour each major vessel oxygen-rich (red) or oxygen-poor (blue). Out-of-order connections are explained, and four common misconceptions are flagged as **key misconceptions** rather than safety misses: mirroring left and right in a front view, sending the right ventricle to the aorta, routing blood from the lungs straight to the body, and "every artery is red".

It was drafted before the biology faculty partner described his own project, so it uses only widely taught introductory (general) biology. It is **educational general biology, not clinical or medical guidance**; it is a schematic, not an anatomical model, and it cites no sources. The red/blue colours are the usual diagram convention (real blood is always red). Content, level, and wording are expected to change after faculty review. The page shows a "Draft for faculty review" badge on the card, a draft notice in the scenario panel, and "draft for faculty review" on the 3D sign. Code: `biology-scenarios.js` (tasks) and `biology-stations.js` (3D model).

All scenarios are intentionally general rather than tied to one assumed town, employer, or research institution.

## Run it

From this directory, start a local static server:

```powershell
python -m http.server 8003
```

Open <http://localhost:8003>. Three.js is vendored in `../vendor/three`, so it runs offline (serve from the repository root or keep `vendor/` reachable one level up).

## Try a session

1. Enter a display name. It is held in page memory for the current browser session and is not sent to a server or saved to local storage.
2. Select one of the eleven simulations.
3. Read the short situation and choose one of three responses for each decision. Industrial, manufacturing-quality, and life-science scenarios show a pre-task checklist first, then four hands-on tasks (see below).
4. Review the immediate feedback and principle, then continue to the next decision.
5. Answer the reasoning question (one "why" question; every scenario except the draft Circulation Builder has one). It is reported separately from the task score.
6. View the score, retry the scenario, or choose another. Completion marks and scores remain in memory until the page is closed or refreshed.

The simulation selection is available in the page UI. If the browser supports immersive WebXR VR, the **ENTER VR** control appears after the learner enters a name. In VR, point a controller at a floating choice card and use its select/trigger action. You can also use the desktop UI before or during a session.

## Hands-on tasks

In every scenario, each task puts glowing hotspots on the 3D work area. Use the mouse (click a hotspot; drag to look around) or a VR controller trigger to carry the task out. The **Keyboard / list controls** disclosure under the task offers the same actions as buttons.

- **Required actions** must all be completed. Some have prerequisites (you cannot lock out a breaker you have not switched off, or strike an arc before the permit and fire watch are in place); doing one too early is a mistake.
- **Wrong actions** (such as clipping to a handrail, tightening a leaking flange, or letting a coworker re-energize a circuit) turn red and explain the consequence. Critical ones flash the scene.
- **The scene responds:** your lanyard connects to the anchor you picked, a lock and tag appear on the breaker, barricades and a fire watch appear, the platform sways in wind, a gas monitor alarms, and a pedestrian walks toward the exclusion zone.
- **Scoring:** a task earns 2 points with no mistakes, 1 with a single non-critical mistake, and 0 otherwise. A critical mistake always scores 0 for that task and is called out in the result along with the principles to review.

## Why this matters, and a reasoning question

A Penn State randomized study of a battery-storage procedure (Wang & Messner, 2026; see [docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md](../docs/research/UNIVERSITY-VR-AR-LANDSCAPE.md)) found that VR beat video on procedural and spatial scores, but that neither one taught *why* the steps mattered. Every scenario except the draft Circulation Builder now covers that in two ways:

- **"Why this matters" panel.** When you complete a key action, a short one-sentence reason appears for about nine seconds. Example from the hydrostatic test: "Trapped air stores far more energy than water, so venting it makes any failure much less violent." On desktop it is a panel at the lower left (on a phone, at the top) and **Esc** or **Got it** closes it. In VR it is a backed panel just below the scenario sign. It does not affect scoring.
- **Reasoning question.** After the last task there is one multiple-choice question about a step whose purpose is easy to miss, for example "why is the vent opened BEFORE the drain?" (so air can get in and no vacuum forms). It has three options and one best answer. After you answer, all three options are explained. In VR the three options are the floating cards; after you answer they show the explanations.
- **A separate result.** The result screen shows the task (procedure) score and, in its own box, a **Reasoning** result: which option you chose and the best answer. The reasoning answer is never added to the task score and is never called a pass or a fail. The catalog card shows both, for example "tasks 8/8 · reasoning: best answer". Keeping them apart lets a later study compare procedure and reasoning. The app still stores nothing beyond the page session.

Data format: an optional `why` on a correct hotspot and an optional `rationale` on a scenario (`{ question, options: [{ text, best, feedback }] }`). All reasons and questions are in one file, [`reasons.js`](reasons.js), so the owner and partners can review them in one place. `scenarios.js` copies them onto the scenarios (a `why` written inline in a scenario file takes precedence). The hydrostatic test and the three refinery-style scenarios (control room, battery-limits rounds, refinery pipe spool) have a reason for every required action. The other eleven have reasons for the key actions in every task. The draft Circulation Builder has none until faculty review.

The content is general safety and quality awareness written for this lab. It is not a procedure, permit, or certification, and it cites no standards or regulation numbers. It has **not** yet been reviewed by the owner, partners, or a subject-matter expert.

## Tests

From this directory, run the scenario-catalog checks with:

```powershell
node --test .\test\scenarios.test.mjs .\test\biology.test.mjs .\test\why.test.mjs .\test\thumbs.test.mjs .\test\models.test.mjs .\test\round2.test.mjs
```

`round2.test.mjs` checks the round-2 work from the source: every indoor station has a room kind and every outdoor one a ground colour; rooms never cast shadows and are freed with the station; skinned characters free their bone texture; late-loading models are dropped; ground props stay on the platform (or the room floor) and clear of the hotspots they could hide; reduced motion; keyboard focus handling; the camera follows the real panel width. Browser checks outside `node --test`: `node tools/vr-capture/lab-qa.mjs <outDir> [ids]` (screenshots at the start, after a correct action and after a wrong one, plus triangles and heap), `node tools/vr-capture/lab-a11y.mjs [id]` (keyboard-only run), `node tools/vr-capture/lab-leak.mjs [rounds]` (GPU texture and geometry counts over repeated visits to all 17 scenarios), and `node tools/vr-capture/ui-check.mjs --only lab`.

`models.test.mjs` checks that every model file the code references exists and is a GLB, that each file under `assets/models/` has a CC0 credits row with a source link, that the people have Idle and Walk clips, that the total stays under 25 MB, and that the loader falls back to the primitive on failure. `thumbs.test.mjs` checks that every scenario has a 320 x 180 JPEG thumbnail under 40 KB keyed by its id and listed in `thumbs/index.json`, and that the catalog only requests listed thumbnails and falls back to a placeholder tile. The seasonal-sky logic is tested in the brand kit (`node --test` in `../brand-kit`, file `test/season-sky.test.mjs`): season by date, manifest ordering and bad-input fallbacks, 2k/4k choice, path resolution, the Bradford wording rule, the transition curve (and that reduced motion has no mosaic, scan lines, or flicker), and the 30-second clock (one swap per transition, no time passes while paused).

`why.test.mjs` checks the "why this matters" content: every entry in `reasons.js` points at a real required action; every non-draft scenario has a reason in every task and one reasoning question; the hydrostatic test and the refinery-style scenarios explain every required action; reasons are single sentences of at most 110 characters with no standard numbers or citations; each question has three options of at most 64 characters, exactly one best answer, and feedback for each; the best answer is not always in the same position; the draft Circulation Builder has none; and the page has the panel and the separate reasoning result, which never touches the task score. `node tools/vr-capture/ui-check.mjs` also opens the "why" panel (and closes it with Esc) and answers the reasoning question at desktop and phone widths.

The tests verify the catalog (11 scenarios in 3 categories: six industrial, four manufacturing quality, one draft life science; the removed business and research groups must not come back), that every task in every scenario is completable: unique hotspot ids, positions and results, valid prerequisites, at least two required actions, and a critical mistake. Hands-on interactions were also exercised in a desktop browser (hotspot clicks on the canvas, the list controls, out-of-order and critical mistakes, scoring); VR controller selection uses the same code path. Browser interaction was also checked in desktop mode.

`biology.test.mjs` checks the draft Circulation Builder: it is labelled as a draft and as not clinical guidance, the station is registered in `app.js`, every revealed or hidden part exists in the model, the pulmonary and systemic circuits can only be completed in blood-flow order, chamber positions and vessel colours match introductory biology, every task has a key misconception, and the content names no partner companies and contains no citations or links. Circulation Builder was played through once in a desktop browser (Chrome via Playwright, using the list controls; one out-of-order action, one key misconception, score 5/8) with external network requests blocked and no page errors. Its hotspots were not clicked on the 3D canvas in that run (the list controls call the same handler). It has **not** been run in the VR emulator or on any headset.

## What it demonstrates

- A complete, eleven-scenario browser learning flow with lightweight name entry and session-only progress.
- Immediate, consequence-based feedback, a principle recap at each step, and a short score without collecting or transmitting learner data.
- High-risk industrial practice with animated 3D work areas (scaffold, control room, outdoor process unit, suspended platform, welding bay, electrical panel) and critical-miss feedback.
- One scenario catalog and scene presented in desktop mode and in a WebXR immersive VR view where supported.

## Rooms, props and people

Every scenario is built from primitives first, so it works with no extra files. Two layers are added on top.

**Rooms (`rooms.js`, no files).** The twelve indoor work areas stand in a procedural cut-away room: a floor, a back wall and two side walls with textures drawn on a canvas, a skirting board, and wall fittings (window, clock, notice board, exit sign, caged lamps). Six kinds: shop, warehouse, workshop, office, laboratory, and control room. The four outdoor work areas (height, battery limits, window cleaning, field) keep the seasonal sky and get a ground colour (gravel, slab, plaza, grass). The circulation schematic keeps the plain stage. Walls only receive shadows, and their textures are freed when you leave the scenario.

**Props and people (`dressing.js`, CC0 glTF).** `dressing.js` loads models from [`../assets/models/`](../assets/models/CREDITS.md) with the vendored `GLTFLoader` (no CDN, works offline) and adds them once they arrive:

- **People.** Every primitive "doll" (the supervisor in the spill scenario, the customer at the counter, the fire watch, the visitor, the pedestrian in the window-cleaning scenario, and so on) is swapped for a Quaternius character with an idle animation, about 1.7 m tall, turned toward the work. Hi-vis workers play the supervisor, operator and fire-watch roles. The control room has an outgoing operator and the lighter-lot area a quality technician (both added in round 2). People on the scaffold deck and in the window cradle are drawn at half size, and the ground props beside the scaffold are half size too, so the miniature stays consistent.
- **Props.** Poly Haven props in all twelve indoor work areas and the four outdoor ones: cartons, shelving, hand truck, wet-floor sign, jerrycan, welding cart, tool chest, utility box, barrels, generator and cash register (earlier pass); armchair, side table, office desk, microscope, stools, bins, plastic crates, pipes, bench lamp, circuit board, laptop, cement bags, fire alarm panels, wall lamps, security camera, dead tree trunk (round 2). Models are 512 px WebP textures, simplified to a few thousand triangles each. Positions are kept off the hotspot area; `test/round2.test.mjs` checks that no ground prop covers a hotspot.
- **Fallback.** If a file is missing or fails to parse, `loadModel()` resolves to `null`, one console warning is logged, and the primitive (or doll) stays. A model that finishes loading after you have left the scenario is dropped. Checked by blocking `assets/models/` in the browser (`BLOCK_MODELS=1`): every scenario still renders, no page error.
- **Light.** One hemisphere light, a key light with one 1024 shadow map, a fill light, and a low-intensity room environment. No post-processing.

Model weight: 15.4 MB in total (props 12.0 MB, people 3.5 MB); round 2 added 19 props and about 5.7 MB. Measured per frame in desktop Chrome with software GL, shadow pass included (`node tools/vr-capture/lab-qa.mjs <outDir>`): the heaviest scenarios are the delivery check (about 88k triangles, 141 draw calls), the spill (81k), welding (78k) and the pipe spool (72k); the rest are below 65k. A single pass, without the shadow pass, is about half of that. These counts are the likeliest thing to need trimming on a Quest or other headset.

Memory: each visit to a scenario with people used to leave a few GPU textures behind (the bone texture of every skinned character was not freed). That is fixed; `renderer.info.memory` is now flat over repeated visits to all 17 scenarios (`tools/vr-capture/lab-qa.mjs` prints the heap, and the check is described under Tests).

Licenses and sources for every model: [../assets/models/CREDITS.md](../assets/models/CREDITS.md), also linked in the page footer. All are CC0 and none is a real person. To add a model: `node tools/vr-capture/fetch-polyhaven.mjs <dir> <id>`, then `node tools/vr-capture/shrink-models.mjs ...` (see the comments in both), then add its credits row.

## Accessibility

What was checked in a desktop browser (Chrome, software rendering) and what was not.

- **Keyboard only.** `node tools/vr-capture/lab-a11y.mjs [scenario id]` plays a whole scenario with the keyboard and checks where focus goes: to the scenario title on opening, to the next available action after each hotspot is done (a finished action is disabled, which used to drop focus to the page), to **Continue** when a task finishes, to the next prompt, to the result heading, and to the catalog heading when you go back. The "Keyboard / list controls" disclosure holds the same actions as the 3D hotspots and says so in the task hint. A done or wrong action is named in the button text, for example "Hot work permit (done)", not only coloured.
- **Screen readers.** The 3D canvas is hidden from assistive technology (it has no text equivalent of its own); everything a learner needs is in the panel. The scenario section no longer announces itself as one big live region (it duplicated the feedback, task log and result announcements).
- **Colour contrast.** Text and status colours in the panel were computed against their backgrounds: every pair is at least 5.5:1 (muted text 10.8:1, small grey text 6.4:1, critical red 5.5:1, gold on royal blue 6.3:1). Small captions were raised to 12 px.
- **Reduced motion.** With `prefers-reduced-motion: reduce` the hotspot halos do not pulse, the critical miss does not flash the screen, people stand still, the platform ring does not turn, alarm lights hold a fixed state, and the sky cross-fades. The red feedback panel still reports a critical miss.
- **Small screens.** The panel narrows on tablets and small laptops (701 to 1100 px) and the camera follows its real width, so the work area is no longer hidden behind it (at 1100 px it was mostly covered). On phones it is a bottom sheet.
- **Not done:** a skip link, a high-contrast mode, text scaling tests above 200%, captions or audio (there is no audio), and any test in VR. The 3D scene itself cannot be used without sight; the list controls make the tasks completable, but the spatial content is lost.

## Rendering notes

The shared scene uses sRGB output, ACES filmic tone mapping, a hemisphere light plus a soft-shadow key light (one 1024 shadow map), a low-intensity room environment for metal reflections, and procedurally generated canvas textures for the floor and platform. The image assets are the scenario thumbnails (made from this example's own screenshots) and the four CC0 sky panoramas described below.

## Scenario thumbnails and seasonal skies

**Thumbnails.** Every scenario card shows a small picture of its 3D work area: on top of the card in the page catalog (on a phone, to the left of the title), and on the left of each floating card in the VR menu, with the category, minutes, and title beside it. The images are `thumbs/<scenario id>.jpg` (320 x 180, about 10 KB each). Re-make them after re-shooting the README screenshots:

```powershell
node ..\tools\vr-capture\thumbs.mjs
```

It crops the work area from `screenshots/03-scenario-*.png` (left of the page panel, below the in-scene sign, so neither brand band is in a thumbnail; those screenshots were taken with the git-ignored logo blocked) and writes `thumbs/index.json`. The catalog only requests ids listed there and inserts an image only after it has loaded, so a missing thumbnail shows a plain tinted tile, never a broken image.

**Seasonal skies.** Behind the scenario menu, one sky per season (spring, summer, autumn, winter) rotates every 30 seconds, starting with the current season. The change looks like an old monitor: the picture breaks into mosaic blocks that grow, scan lines and a slight red/blue flicker appear for about one second, the next season swaps in at the peak, and the blocks shrink away. With **prefers-reduced-motion** it is a plain cross-fade. A small "Autumn sky" chip next to the progress count (and a small tag beside the plaque in VR) names the current season.

- The effect is drawn inside the sky sphere's own shader (`../brand-kit/season-sky.js`) in view-direction space, so it renders the same way in both eyes in WebXR; there is no screen-space post-processing.
- The outdoor scenarios (work at height, outdoor rounds at the battery limits, skyscraper window cleaning) keep the sky behind their work area; indoor scenarios stand in a procedural room (`rooms.js`). The sky steps aside for the red critical-miss flash. Every scene label is already on a backed panel, so the photo does not reduce readability (checked in desktop screenshots only).
- Images come from `../assets/skies/skies.json` (2k files; `?sky=4k` in the address uses the 4k files on a desktop). If the manifest or an image is missing, the scene keeps its navy background and nothing errors; a season whose image fails is skipped. Rotation pauses while the tab is hidden.
- **Image credits** (a link at the foot of the page panel) lists each image's credit and source from the manifest. The current images are CC0 Poly Haven panoramas used as **regional-style stand-ins, not photos of Bradford, PA**; the page says "Seasonal skies, regional style (not photos of Bradford)" and only names Bradford for an image whose manifest entry has `isBradford: true`.
- The page needs the repository root served (it loads `../vendor/`, `../brand-kit/` and `../assets/`); it makes no network requests outside the local server.

`node ..\tools\vr-capture\sky-shots.mjs` re-shoots the desktop screenshots below; `CAPTURE_ONLY=menu` with `..\tools\vr-capture\capture.mjs` re-shoots the emulated Quest 3 menu.

## Research background

The hands-on design follows the argument that hand-based manipulation is a distinctive educational strength of VR (Johnson-Glenberg, 2018), and the consequence-driven feedback targets what Slater (2009) calls plausibility, the sense that events respond to you. The industrial scenarios address hazard-recognition training, where an immersive condition outperformed 2D training in one construction study (Sacks et al., 2013). A caution applies to realism: immersion can raise presence while lowering learning if it adds cognitive load (Makransky et al., 2019), so visual detail should be tested, not assumed to help. Full references, evidence gaps, and study ideas are in [docs/research](../docs/research/README.md).

## Scope and limitations

This is a non-certified learning prototype—not an employer's safety policy, a permit, a competence assessment, professional qualification, research-ethics approval, laboratory procedure, or substitute for an approved study protocol. Employers and research teams should review and adapt examples to their jurisdiction, workplace, institution, and current procedures. The research examples teach process habits; they do not collect real participants' data or handle real samples.

The application has no login, analytics, backend, persistence, or export. Names and scores are kept in page memory only. Only a compatible WebXR browser, secure context, VR runtime, and headset can start immersive VR.

## Look and branding

The page and the 3D scene use the University of Pittsburgh palette and bundled fonts (League Gothic headings, Roboto text) through the shared [brand kit](../brand-kit/README.md):

- A Pitt-Bradford + **CIST** (Computer Information Systems & Technology) brand bar heads the panel, with a link back to the launcher. It shows the official logo when `branding/logo.png` is on the machine (that folder is git-ignored and never committed) and a text wordmark otherwise; a missing logo never shows as a broken image.
- Every piece of text in the scene sits on a backed panel: the scenario sign (with a brand band), hotspot labels (pills with a status chip: gold dot to do, green check done, red cross wrong), choice and scenario cards, and the station plates. A Pitt-Bradford + CIST plaque floats above the scenario cards.
- After you enter your name, the heading and name form collapse so the task has the room. On a phone the panel becomes a bottom sheet and the scene shows above it.

Checked with `node tools/vr-capture/ui-check.mjs` at 1440 x 900 and 390 x 844 (desktop browser; phone width emulated, not a real phone).

## Screenshots

All captures are from the running example in a desktop browser at the default (non-headset) view. WebXR VR was unavailable in the capture environment, so the page shows **VR NOT SUPPORTED**; these are not headset recordings. They were re-shot on November 1, 2026 after the professional-look pass (`node tools/vr-capture/readme-shots.mjs lab`), with the git-ignored logo blocked, so the brand bar and the in-scene sign show the text wordmark.

| Step | Capture |
|---|---|
| Welcome and name entry | ![Welcome screen](screenshots/01-welcome.png) |
| Scenario catalog | ![Eleven-scenario catalog](screenshots/02-catalog.png) |
| Task feedback | ![Feedback after finishing a task](screenshots/04-feedback.png) |
| Scenario complete | ![Completion and score](screenshots/05-complete.png) |

### The eleven scenarios

| Scenario | Capture |
|---|---|
| Work at height (mid-task) | ![Work at height](screenshots/03-scenario-1-work-at-height.png) |
| Run a unit from the control room | ![Run a unit from the control room](screenshots/03-scenario-2-run-a-unit-from-the-control-room.png) |
| Outdoor rounds at the battery limits | ![Outdoor rounds at the battery limits](screenshots/03-scenario-3-outdoor-rounds-at-the-battery-limits.png) |
| Clean windows on a skyscraper | ![Clean windows on a skyscraper](screenshots/03-scenario-4-clean-windows-on-a-skyscraper.png) |
| Weld safely | ![Weld safely](screenshots/03-scenario-5-weld-safely.png) |
| Electrical isolation and work | ![Electrical isolation and work](screenshots/03-scenario-6-electrical-isolation-and-work.png) |
| Sample and test a lighter lot (task 3) | ![Sample and test a lighter lot](screenshots/03-scenario-7-sample-and-test-a-lighter-lot.png) |
| Hydrostatic test of a heat exchanger (task 3) | ![Hydrostatic test of a heat exchanger](screenshots/03-scenario-8-hydrostatic-test-of-a-heat-exchanger.png) |
| Weld a refinery pipe spool (task 3) | ![Weld a refinery pipe spool](screenshots/03-scenario-9-weld-a-refinery-pipe-spool.png) |
| Select and check a resistor (task 2) | ![Select and check a resistor](screenshots/03-scenario-10-select-and-check-a-resistor.png) |
| Circulation Builder, draft (task 1) | ![Circulation Builder](screenshots/03-scenario-11-circulation-builder.png) |
| Critical safety miss | ![Critical safety miss feedback](screenshots/06-critical-miss.png) |
| Industrial result and review list | ![Industrial result with review list](screenshots/07-industrial-result.png) |

### Scenario thumbnails and seasonal skies

Desktop browser captures (1440 x 900, November 1, 2026, `node tools/vr-capture/sky-shots.mjs`, logo blocked), not headset recordings. The sky images are CC0 regional-style stand-ins, not photos of Bradford.

| What | Capture |
|---|---|
| Catalog with thumbnails (page cards, and the floating thumbnail cards behind) | ![Catalog with thumbnails](screenshots/skies/menu-thumbnails.png) |
| Catalog at phone width (390 x 844, emulated, not a real phone) | ![Phone catalog with thumbnails](screenshots/skies/menu-phone.png) |
| Spring | ![Spring sky](screenshots/skies/season-spring.png) |
| Summer | ![Summer sky](screenshots/skies/season-summer.png) |
| Autumn | ![Autumn sky](screenshots/skies/season-autumn.png) |
| Winter | ![Winter sky](screenshots/skies/season-winter.png) |
| Transition, a quarter of the way: mosaic blocks growing, scan lines | ![Transition early](screenshots/skies/transition-early.png) |
| Transition at its peak (swap point): largest blocks, scan lines, colour split | ![Transition at its peak](screenshots/skies/transition-mid.png) |
| Reduced motion: plain cross-fade, half way | ![Reduced-motion cross-fade](screenshots/skies/reduced-motion-crossfade.png) |
| Outdoor scenario (battery limits) with the sky behind | ![Outdoor scenario with sky](screenshots/skies/scenario-outdoor.png) |
| Indoor scenario (electrical): workshop room | ![Indoor scenario without sky](screenshots/skies/scenario-indoor.png) |

### Why this matters and the reasoning question

Desktop browser captures (1280 x 1100, November 1, 2026, `node tools/vr-capture/readme-shots.mjs why`), not headset recordings.

| Step | Capture |
|---|---|
| "Why this matters" after opening the vent (hydrostatic test, task 2) | ![Why this matters panel](screenshots/09-why-this-matters.png) |
| The reasoning question after the last task | ![Reasoning question](screenshots/10-reasoning-question.png) |
| Feedback with each option explained (a not-best answer chosen) | ![Reasoning feedback](screenshots/11-reasoning-feedback.png) |
| Result: task score, with the reasoning result in its own box | ![Result with separate reasoning](screenshots/12-result-with-reasoning.png) |

### Circulation Builder (draft for faculty review)

Desktop browser captures (1280 x 1100), not headset recordings.

| Step | Capture |
|---|---|
| Life-science group in the catalog, with the draft badge | ![Catalog with the draft badge](screenshots/08-circulation-catalog.png) |
| Task 2: pulmonary circuit, after an out-of-order valve | ![Pulmonary circuit](screenshots/08-circulation-task2-pulmonary.png) |
| Task 3: a key misconception (lungs straight to the body) | ![Key misconception feedback](screenshots/08-circulation-misconception.png) |
| Task 4: colouring vessels oxygen-rich or oxygen-poor | ![Colouring vessels](screenshots/08-circulation-task4-colouring.png) |
| Result and review list | ![Circulation Builder result](screenshots/08-circulation-result.png) |

In the desktop view the floating 3D card for Circulation Builder sits in a fifth column to the right of the 4 x 4 card grid, partly behind the page panel; in VR there is no panel. That VR card position has not been captured in the emulator.

## In VR (emulated headset)

These captures come from an **emulated Meta Quest 3** (Meta's IWER runtime in Chrome; re-captured November 1, 2026 after the professional-look pass, with the 17-card catalog and the git-ignored logo blocked), using the page's real WebXR session and a scripted controller. They show layout, scale, and interaction in a headset-style view; they are not recordings from a physical headset. The controller appears as its pointer ray. See [tools/vr-capture](../tools/vr-capture/README.md) for how they are made and what the emulation does not cover.

In VR the training world is placed about 2.4 m in front of you and scaled up slightly. The scenario cards float above the platform; point a controller at a card and pull the trigger to start. In a task, point at a glowing hotspot and pull the trigger to carry out that step; the floating sign above the scene shows each result, and a "continue" card appears when the task is done.

| Step | Capture |
|---|---|
| Scenario catalog | ![Scenario catalog in VR](screenshots/vr/01-catalog.png) |
| Pointing at a scenario | ![Pointing a controller at a scenario card](screenshots/vr/02-pointing-at-a-scenario.png) |
| Hands-on task | ![Work at height task in VR](screenshots/vr/03-hands-on-task.png) |
| Selecting hotspots | ![Selecting hotspots with a controller ray](screenshots/vr/04-selecting-hotspots.png) |
| Task complete | ![Task complete in VR](screenshots/vr/05-task-complete.png) |
| A wrong action | ![Wrong action marked in red](screenshots/vr/06-wrong-action.png) |
| Electrical lockout | ![Lock and tag applied in VR](screenshots/vr/07-electrical-lockout.png) |
| "Why this matters" panel below the sign (emulated Quest 3) | ![Why this matters in VR, emulated Quest 3](screenshots/vr/08-why-this-matters.png) |
| Reasoning question on the three cards (emulated Quest 3) | ![Reasoning question in VR, emulated Quest 3](screenshots/vr/09-reasoning-question.png) |
| Answered: each card explains its option (emulated Quest 3) | ![Reasoning feedback in VR, emulated Quest 3](screenshots/vr/10-reasoning-feedback.png) |

| Season transition held at its peak behind the menu (emulated Quest 3) | ![Menu sky mid-transition in VR, emulated Quest 3](screenshots/vr/11-menu-sky-transition.png) |

The catalog, pointing, and sky-transition captures (01, 02, 11) were re-taken on November 1, 2026 after the thumbnail and seasonal-sky change with `CAPTURE_ONLY=menu node capture.mjs` (emulated Quest 3; autumn sky, rotation held still for the capture). The hands-on captures 03-07 predate the sky: in the work-at-height scenario the sky now shows behind the scaffold, which these older frames do not show. Thumbnails in the VR cards are small at menu distance; they help recognition rather than show detail.

The "why" captures (08-10) were captured on November 1, 2026 with `CAPTURE_ONLY=why node capture.mjs`, which plays the hydrostatic test through with the scripted controller. In the emulator the explanation text on the answered cards is small (it shrinks to fit), so the sign above repeats the key feedback.

## Design values and review status

Numbers in the scenarios are conventions chosen for the exercise, not standards: the 200 kg safe working load on the scaffold tag, the lot size and sampling plan in the lighter lot scenario, and the green-dot marking of anti-sulfur parts. The manufacturing scenarios (hydrostatic test, pipe spool, lighter lot, resistor selection) are general awareness practice and have **not been reviewed by an industry expert**; the standards behind them were not opened (see [docs/research/CONTENT-REVIEW.md](../docs/research/CONTENT-REVIEW.md)). The Circulation Builder is a draft for faculty review.
