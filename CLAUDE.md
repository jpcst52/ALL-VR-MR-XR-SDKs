# Working notes for this repository

Read this at the start of a session. It records how the project owner likes to work and the conventions of this repo. The current task list is in [docs/PROJECT-STATUS.md](docs/PROJECT-STATUS.md).

## Who and what

- Owner: Jeremy Callinan, who teaches and builds XR training at the University of Pittsburgh at Bradford and also works at ARG. Primary GitHub remote is `jpcst52/ALL-VR-MR-XR-SDKs` (public). An older course repo, `jcallinan/CIST1423-Fall2026-Examples`, holds the WaterWorks example.
- Goal: XR training examples for an advisory-board demo on **Nov 13, 2026** at Pitt-Bradford, with an eye toward research papers. Audience includes local employers (Zippo, Allegheny Bradford Corporation, KOA Speer) and faculty (energy/industrial, and Orin James in biology).
- Hardware: Meta Quests (Quest Browser), HTC Vive Pro 2 wireless on SteamVR, Alienware Aurora desktops (RTX 5070 Ti 16 GB).

## How the owner likes to work (learned from the sessions)

- **Complete deliverables.** Each example should come with code, a README, screenshots, and (for the main work) deck slides. Several separate projects per platform are preferred so something works if another fails.
- **Be honest about what is verified.** The owner corrected a claim that a library "works on the Vive" that had only come from a README. Never state that hardware works unless it was tested. Say what was run, what was only emulated, and what is unverified.
- **No invented sources.** Research citations must be verified against a bibliographic record, with gaps flagged.
- **Simple over clever.** Prefers WaterWorks-style multi-user training and minimal setup; rejected local-AI/tunnel/domain complexity. Offline reliability matters for demos.
- **Show the results.** After visual changes, produce screenshots and show them. Lists of tasks with statuses are welcomed.
- **Ask what is needed, then proceed.** The owner answers questions in detail and attaches screenshots. Batch the questions, state a recommendation, and keep working on the unblocked parts.
- **Document everything in `.md` files** so future sessions have context. Keep `docs/PROJECT-STATUS.md` current.
- Commits are made when the owner asks. Do not push unless asked.

## Repository conventions

- Browser examples must run **offline**: libraries live in `vendor/`, no CDN. Verify with `node tools/vr-capture/offline-check.mjs`.
- Do not commit `branding/` (university branding) or `assets-private/` (licensed assets). They are in `.gitignore`. The one exception: the owner approved publishing the logo on 2026-11-02, so `branding/logo.png` and the Unity shield `vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/Resources/Branding/pitt-bradford.png` are tracked; the brand guidelines PDF and the other variants stay ignored.
- Web demos use CC0 assets only. Unity Asset Store packs may be used in local Unity builds, never committed.
- VR screenshots from the emulator are labelled "emulated Quest 3", never presented as a real headset.
- Scenario content is general safety awareness, not a procedure or certification; say so in READMEs.
- Brand palette (University of Pittsburgh): Royal `#003594`, Gold `#FFB81C`, Medium Blue `#00205B`, New Sky `#DBEEFF`. Fonts: League Gothic, Merriweather, Roboto (bundled).
- Shell notes (Windows): heredocs containing backticks or certain quote patterns can break in the Bash tool; write files with the Write tool and run helper scripts. A stray background server may hold ports 8000/8443; stop it by PID.

## Status dashboard: keep it current (standing instruction)

The owner uses a local status dashboard as the standard for every project (guide: [docs/status-dashboard-guide.md](docs/status-dashboard-guide.md)). Start it with `npm run status` and read the address it prints (the port is chosen automatically, so several projects can run dashboards at once; it is also saved in `tools/status/.dashboard-url`). The task list it reads is [docs/Task-Status.md](docs/Task-Status.md).

- When a task **starts, finishes, or becomes blocked**, edit its row in `docs/Task-Status.md` in the same turn (✅ done, 🔄 in progress, ⏳ waiting on something earlier, 👤 needs the owner, ❓ needs partners or faculty). Keep Notes short and factual.
- When you **launch an agent or subagent**: `node tools/status/agent.cjs start <id> "<name>" "<task>" <watch paths>`. When it finishes: `node tools/status/agent.cjs done <id> "<result>"`.
- After each **full test run**: `node tools/status/record-results.cjs "<suite>" <passed> <failed> <skipped>`.
- The owner should not have to ask for routine status. Still summarize at milestones in chat. Never put secrets in these files.
- Keep `docs/PROJECT-STATUS.md` for narrative detail (decisions, caveats); the dashboard task list is the source for done/not done.

## Where things are

| Path | Contents |
|---|---|
| `training-simulations/` | Fieldwork XR Practice Lab (Three.js, 12 hands-on scenarios) |
| `multi-player/` | Relay and the Circuit Lab / Site Safety Layout WebXR examples |
| `vive-steamvr/` | Unity (OpenXR/SteamVR) projects and the shared `com.pittbradford.xrtraining` kit |
| `tools/vr-capture`, `tools/decks`, `tools/status` | Emulated-VR screenshots; PowerPoint deck builder; local status dashboard |
| `docs/research/` | Annotated bibliography and study ideas |
| `docs/Task-Status.md`, `docs/PROJECT-STATUS.md` | Dashboard task list; narrative status and decisions |
| `vendor/` | Offline copies of Three.js, Babylon.js, A-Frame, fonts |
