// Keep tools/status/agents.json current (the dashboard shows it). Usage:
//   node tools/status/agent.cjs start <agentId> "<short name>" "<what it does>" [watchPath ...]   (watchPath = files/folders the agent writes, relative to status.config.json; newest change = last activity)
//   node tools/status/agent.cjs done  <agentId> "<one-line result>"
const fs = require('fs'), path = require('path');
const file = path.join(__dirname, 'agents.json');
const data = JSON.parse(fs.readFileSync(file, 'utf8'));
const [cmd, id, a, b, ...watch] = process.argv.slice(2);
if (cmd === 'start') { data.agents = data.agents.filter(x => x.id !== id); data.agents.push({ id, name: a, task: b || '', state: 'running', started: new Date().toISOString(), watch }); }
else if (cmd === 'done') { const x = data.agents.find(x => x.id === id); if (x) { x.state = 'done'; x.ended = new Date().toISOString(); x.note = a || ''; } }
else { console.error('usage: start|done'); process.exit(1); }
data.agents = data.agents.slice(-40);
fs.writeFileSync(file, JSON.stringify(data, null, 2));
