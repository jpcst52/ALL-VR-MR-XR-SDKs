# Rules for sub-agents working in this repository

Every agent reads this file and the owner's local working notes (`CLAUDE.md`, kept private) before starting. The main session coordinates, reviews, updates the dashboard, and commits.

## Ownership and collisions
- **Stay inside your assigned files and folders.** Each task lists what you own. Do not edit anything else.
- **Never edit** `docs/Task-Status.md`, `docs/PROJECT-STATUS.md`, `CLAUDE.md`, the root `README.md`, `tools/status/*`, or `vive-steamvr/packages/` (the shared Unity kit) or `vive-steamvr/hub/` (the main session is using them live and edits trigger recompiles). Suggest changes in your report instead.
- **Shared files** (for example `multi-player/server.mjs`, `training-simulations/app.js`): re-read the file immediately before editing, make small targeted edits with unique match strings, and re-read after to confirm another agent's change was not lost.
- **Do not run `git add`, `git commit`, or `git push`.** The main session commits. Never touch `branding/`, `assets-private/`, or any Asset Store files.

## Honesty
- Report only what you actually ran. Label anything unverified, emulated, or untested on real hardware as such. Never claim something works on a Vive or Quest unless it was tested there (no agent can do that).
- Research claims need a verified source. Do not invent citations, statistics, or facts about companies or products.
- Scenario content is general safety awareness practice, not a procedure or certification. Company names appear only as "inspired by" unless the main session says otherwise.

## Practical
- Windows. The Bash tool is Git Bash. Use the Write tool for files containing backticks or tricky quoting; write helper scripts to files instead of long heredocs. Python on Windows cannot open `/c/...` paths (use `C:/...`).
- **Use your assigned web port** for servers and stop them when you finish. Ports 8000 and 8443 belong to the owner and the main session. Do not kill processes you did not start.
- Browser screenshots: Playwright is installed in `tools/vr-capture` (`chromium.launch({ channel: "chrome", args: ["--use-gl=angle","--use-angle=swiftshader","--enable-unsafe-swiftshader","--ignore-gpu-blocklist"] })`). Screenshots go in the owning folder's `screenshots/`.
- Browser examples must work offline: no CDN, use `vendor/`. Verify with `node tools/vr-capture/offline-check.mjs` style checks when relevant.
- Brand: Pitt Royal `#003594`, Gold `#FFB81C`, Medium Blue `#00205B`, New Sky `#DBEEFF`; fonts League Gothic, Merriweather, Roboto from `vendor/fonts/`.
- Unity code cannot be compiled by agents (no editor access). Write it carefully against the kit's real signatures and say it is uncompiled.

## Final report (under 300 words)
1. Files created and changed. 2. Tests run and results. 3. Screenshot paths. 4. What is unverified or limited. 5. Suggested rows or notes for `docs/Task-Status.md`.
