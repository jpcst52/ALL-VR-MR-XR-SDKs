// Local live status dashboard. No dependencies.   Run:  node tools/status/server.cjs   (or npm run status / start-status.cmd)
// Configuration lives in status.config.json (same folder). Docs: tools/status/README.md  (and docs/status-dashboard-guide.md)
//
// Reads: the task list (markdown tables), git in each configured repo, whether each configured URL answers, test-results.json, agents.json and the
// modified-time of each running agent's output file ("last activity"). Writes: activity-log.jsonl (a heartbeat every minute + an event whenever
// something changes: new commit, agent started/finished/quiet, server up/down, task list or test results updated).
const http = require('http');
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const DIR = __dirname;
const CONFIG_FILE = process.env.STATUS_CONFIG || path.join(DIR, 'status.config.json');
const cfg = Object.assign({
  title: 'Project status', port: 5200, heartbeatSeconds: 60, taskFile: '', repos: [], servers: [], excludeFromDirty: [], quietAgentSeconds: 600,
}, (() => { try { return JSON.parse(fs.readFileSync(CONFIG_FILE, 'utf8')); } catch { return {}; } })());
const rel = p => path.resolve(path.dirname(CONFIG_FILE), p);
const PORT = Number(process.env.STATUS_PORT || cfg.port);
const TASK_FILE = process.env.STATUS_TASKS || (cfg.taskFile ? rel(cfg.taskFile) : '');
const AGENTS_FILE = path.join(DIR, 'agents.json');
const TESTS_FILE = path.join(DIR, 'test-results.json');
const LOG_FILE = path.join(DIR, 'activity-log.jsonl');
const MAX_LOG = 3000;

function readJson(file, fallback) { try { return JSON.parse(fs.readFileSync(file, 'utf8')); } catch { return fallback; } }
function git(repo, args) { try { return execFileSync('git', ['-C', repo, ...args], { encoding: 'utf8', timeout: 5000 }).trim(); } catch { return ''; } }

function repoInfo(r) {
  const repo = rel(r.path);
  const commits = git(repo, ['log', '-6', '--format=%h|%cr|%s']).split('\n').filter(Boolean).map(l => {
    const [hash, when, ...rest] = l.split('|'); return { hash, when, subject: rest.join('|').slice(0, 110) };
  });
  const skip = cfg.excludeFromDirty || [];
  const dirty = git(repo, ['status', '--short']).split('\n').filter(l => l && !skip.some(s => l.includes(s)));
  return { name: r.name, branch: git(repo, ['branch', '--show-current']), head: git(repo, ['rev-parse', '--short', 'HEAD']), commits,
    changed: dirty.filter(l => !l.startsWith('??')).length, untracked: dirty.filter(l => l.startsWith('??')).length };
}

function ping(url) {
  return new Promise(resolve => {
    let req;
    try { req = http.get(url, { timeout: 1500 }, res => { res.resume(); resolve(res.statusCode < 500); }); } catch { return resolve(false); }
    req.on('error', () => resolve(false)); req.on('timeout', () => { req.destroy(); resolve(false); });
  });
}

// Task list: markdown tables under headings like "## A. Code and tests"; rows "| A1 | task | status | notes |".
// Status cell tokens: ✅ done · 🔄 in progress · ⏳ waiting · 👤 needs the owner · ❓ needs the customer.
function parseTasks() {
  let text = '';
  try { text = fs.readFileSync(TASK_FILE, 'utf8'); } catch { return { sections: [], needsMe: [], updated: null }; }
  const sections = []; let cur = null;
  for (const line of text.split(/\r?\n/)) {
    const h = line.match(/^##\s+([A-Z])\.\s+(.*)$/);
    if (h) { cur = { key: h[1], title: h[2], rows: [] }; sections.push(cur); continue; }
    if (!cur || !line.startsWith('|')) continue;
    const cells = line.split('|').slice(1, -1).map(c => c.trim());
    if (cells.length < 3 || /^-+$/.test(cells[0].replace(/\s/g, '')) || /^#$/i.test(cells[0])) continue;
    const row = { id: cells[0], task: cells[1], status: cells[2], notes: cells[3] || '' };
    row.state = /🔄/.test(row.status) ? 'progress' : /✅/.test(row.status) ? 'done' : /⏳/.test(row.status) ? 'waiting' : /❓/.test(row.status) ? 'ask' : /👤/.test(row.status) ? 'you' : 'open';
    row.needsOwner = /👤/.test(row.status) && row.state !== 'done';
    row.needsCustomer = /❓/.test(row.status) && row.state !== 'done';
    cur.rows.push(row);
  }
  const out = sections.filter(s => s.rows.length).map(s => ({
    key: s.key, title: s.title, total: s.rows.length,
    done: s.rows.filter(r => r.state === 'done').length, progress: s.rows.filter(r => r.state === 'progress').length, rows: s.rows,
  }));
  const needsMe = [];
  for (const s of sections) for (const r of s.rows) {
    if (r.needsOwner) needsMe.push({ who: cfg.ownerName || 'Owner', owner: true, id: r.id, task: r.task, notes: r.notes, later: /⏳/.test(r.status) });
    else if (r.needsCustomer) needsMe.push({ who: cfg.customerName || 'Customer', owner: false, id: r.id, task: r.task, notes: r.notes, later: false });
  }
  needsMe.sort((a, b) => Number(a.later) - Number(b.later));
  return { sections: out, needsMe, updated: fs.statSync(TASK_FILE).mtime.toISOString() };
}

// Newest modified-time under a file or folder (folders: two levels deep), in ms; null if nothing exists.
function newestMtime(p) {
  let best = null;
  const visit = (f, depth) => {
    let st; try { st = fs.statSync(f); } catch { return; }
    if (st.isDirectory()) { if (depth > 2) return; let names = []; try { names = fs.readdirSync(f); } catch { return; } for (const n of names) visit(path.join(f, n), depth + 1); }
    else if (best == null || st.mtimeMs > best) best = st.mtimeMs;
  };
  visit(p, 0); return best;
}

function agentsInfo() {
  const data = readJson(AGENTS_FILE, { outputDir: '', agents: [] });
  const now = Date.now();
  const agents = (data.agents || []).map(a => {
    let lastActivity = null, bytes = null;
    if (a.state === 'running') {
      // "watch": files/folders the agent writes (relative to the config file); the newest change = its last activity.
      for (const w of a.watch || []) { const m = newestMtime(rel(w)); if (m != null && (lastActivity == null || m > lastActivity)) lastActivity = m; }
      if (data.outputDir && a.id) {
        try { const st = fs.statSync(path.join(data.outputDir, a.id + '.output')); bytes = st.size; if (st.size > 0 && (lastActivity == null || st.mtimeMs > lastActivity)) lastActivity = st.mtimeMs; } catch { /* not there yet */ }
      }
    }
    const started = a.started ? Date.parse(a.started) : null, ended = a.ended ? Date.parse(a.ended) : null;
    return { ...a,
      runningMinutes: a.minutes != null ? a.minutes : a.state === 'running' && started ? Math.round((now - started) / 60000) : (started && ended ? Math.round((ended - started) / 60000) : null),
      secondsSinceActivity: lastActivity ? Math.round((now - lastActivity) / 1000) : null, outputBytes: bytes };
  });
  return agents.sort((a, b) => (a.state === 'running' ? 0 : 1) - (b.state === 'running' ? 0 : 1) || String(b.started).localeCompare(String(a.started)));
}

async function snapshot() {
  const servers = await Promise.all((cfg.servers || []).map(async s => ({ name: s.name, up: await ping(s.url) })));
  return {
    now: new Date().toISOString(), title: cfg.title, heartbeatSeconds: cfg.heartbeatSeconds, ownerName: cfg.ownerName || 'Owner', customerName: cfg.customerName || 'Customer',
    tasks: parseTasks(), tests: readJson(TESTS_FILE, { updated: null, suites: [] }), agents: agentsInfo(),
    servers, repos: (cfg.repos || []).map(repoInfo),
  };
}

// ---------- activity log ----------
let log = [];
try { log = fs.readFileSync(LOG_FILE, 'utf8').split('\n').filter(Boolean).map(l => JSON.parse(l)).slice(-MAX_LOG); } catch { log = []; }
function addLog(kind, msg) {
  const e = { t: new Date().toISOString(), kind, msg };
  log.push(e);
  try { fs.appendFileSync(LOG_FILE, JSON.stringify(e) + '\n'); } catch { /* ignore */ }
  if (log.length > MAX_LOG + 200) { log = log.slice(-MAX_LOG); try { fs.writeFileSync(LOG_FILE, log.map(x => JSON.stringify(x)).join('\n') + '\n'); } catch { /* ignore */ } }
}

let prev = null;
function detectEvents(s) {
  if (!prev) { addLog('event', 'Dashboard started.'); prev = s; return; }
  for (const r of s.repos) {
    const p = prev.repos.find(x => x.name === r.name);
    if (p && p.head && r.head && p.head !== r.head) addLog('event', `${r.name}: new commit ${r.head} — ${r.commits[0] ? r.commits[0].subject : ''}`);
  }
  for (const sv of s.servers) {
    const p = prev.servers.find(x => x.name === sv.name);
    if (p && p.up !== sv.up) addLog('event', `${sv.name} went ${sv.up ? 'UP' : 'DOWN'}.`);
  }
  for (const a of s.agents) {
    const p = prev.agents.find(x => x.id === a.id);
    if (!p && a.state === 'running') addLog('event', `Agent started: ${a.name}.`);
    else if (p && p.state === 'running' && a.state !== 'running') addLog('event', `Agent finished: ${a.name}${a.runningMinutes != null ? ` (${a.runningMinutes} min)` : ''}${a.note ? ' — ' + a.note : ''}.`);
    if (a.state === 'running' && p) {
      const quiet = a.secondsSinceActivity != null && a.secondsSinceActivity > cfg.quietAgentSeconds;
      const wasQuiet = p.secondsSinceActivity != null && p.secondsSinceActivity > cfg.quietAgentSeconds;
      if (quiet && !wasQuiet) addLog('warn', `Agent quiet for ${Math.round(a.secondsSinceActivity / 60)}+ min: ${a.name} (may be in a long test run).`);
      if (!quiet && wasQuiet) addLog('event', `Agent active again: ${a.name}.`);
    }
  }
  if (prev.tasks.updated !== s.tasks.updated) {
    const tot = s.tasks.sections.reduce((n, x) => n + x.total, 0), done = s.tasks.sections.reduce((n, x) => n + x.done, 0);
    addLog('event', `Task list updated — ${done} of ${tot} done.`);
  }
  if (prev.tests.updated !== s.tests.updated) addLog('event', `Test results recorded — ${(s.tests.suites || []).map(x => `${x.name}: ${x.passed} passed, ${x.failed} failed`).join('; ')}.`);
  prev = s;
}
function heartbeatText(s) {
  const run = s.agents.filter(a => a.state === 'running');
  const acts = run.map(a => a.secondsSinceActivity).filter(x => x != null);
  const freshest = acts.length ? Math.min(...acts) : null;
  const agentsTxt = run.length ? `${run.length} agent${run.length > 1 ? 's' : ''} running (most recent activity ${freshest == null ? 'unknown' : freshest < 90 ? freshest + 's' : Math.round(freshest / 60) + ' min'} ago)` : 'no agents running';
  const sv = s.servers.map(x => `${x.name.split(' (')[0]} ${x.up ? 'up' : 'DOWN'}`).join(', ');
  const dirty = s.repos.map(r => `${r.name.split(' ')[0]} ${r.changed + r.untracked}`).join(', ');
  const tot = s.tasks.sections.reduce((n, x) => n + x.total, 0), done = s.tasks.sections.reduce((n, x) => n + x.done, 0);
  return `♥ ${agentsTxt} · tasks ${done}/${tot} · ${sv || 'no servers'} · uncommitted files: ${dirty || 'n/a'}`;
}
async function beat(heartbeat) {
  try { const s = await snapshot(); detectEvents(s); if (heartbeat) addLog('heartbeat', heartbeatText(s)); } catch (e) { addLog('warn', 'Status check failed: ' + e.message); }
}

http.createServer(async (req, res) => {
  try {
    if (req.url.startsWith('/api/status')) {
      res.writeHead(200, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
      const s = await snapshot();
      s.log = log.slice(-400).reverse();
      res.end(JSON.stringify(s));
    } else if (req.url === '/' || req.url.startsWith('/index')) {
      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
      res.end(fs.readFileSync(path.join(DIR, 'index.html')));
    } else { res.writeHead(404); res.end('not found'); }
  } catch (e) { res.writeHead(500); res.end(String(e)); }
}).listen(PORT, '127.0.0.1', () => {
  console.log(`Status dashboard: http://localhost:${PORT}   (Ctrl+C to stop)`);
  beat(true);
  setInterval(() => beat(true), Math.max(10, cfg.heartbeatSeconds) * 1000);
  setInterval(() => beat(false), 15000);   // detect events between heartbeats
});
