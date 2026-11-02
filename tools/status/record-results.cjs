// Record a test run so the dashboard shows it. Usage:
//   node tools/status/record-results.cjs "<suite name>" <passed> <failed> <skipped> ["<note>"]
const fs = require('fs'), path = require('path');
const file = path.join(__dirname, 'test-results.json');
const data = JSON.parse(fs.readFileSync(file, 'utf8'));
const [name, passed, failed, skipped, note] = process.argv.slice(2);
const entry = { name, passed: +passed, failed: +failed, skipped: +skipped || 0, when: new Date().toLocaleString(), note: note || '' };
data.suites = data.suites.filter(s => s.name !== name); data.suites.push(entry); data.updated = entry.when;
fs.writeFileSync(file, JSON.stringify(data, null, 2));
