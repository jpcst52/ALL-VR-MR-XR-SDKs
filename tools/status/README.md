# Status dashboard

A local, live status page for work done by several people and AI agents at once. No installs (Node.js only), no cloud (binds to 127.0.0.1), no database.

- **Start:** `npm run status` from the repository root, or double-click `start-status.cmd`, then open http://localhost:5210. It refreshes every 10 seconds.
- **Task list it reads:** [docs/Task-Status.md](../../docs/Task-Status.md). Edit that file whenever work moves.
- **Configuration:** `status.config.json` (title, port, repos, servers to watch, owner and customer names).
- **Full guide** (file formats, configuration, troubleshooting, instructions for Claude): [docs/status-dashboard-guide.md](../../docs/status-dashboard-guide.md).

![The status dashboard for this project](../../docs/images/status-dashboard.png)

Helpers:

```
node tools/status/agent.cjs start <agentId> "<name>" "<what it does>" [watchPath ...]
node tools/status/agent.cjs done  <agentId> "<one-line result>"
node tools/status/record-results.cjs "<suite name>" <passed> <failed> <skipped> ["<note>"]
```

This folder is a copy of the tool built for the EVE Residential review package. To use it on another project, copy `tools/status/` and follow section 5 of the guide.
