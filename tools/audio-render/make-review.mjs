// Builds docs/audio-review/README.md and index.html from the WAVs and report.csv that Unity's SfxReport wrote, plus catalog.csv from
// the offline renderer. It measures nothing itself and cannot hear.
//   node tools/audio-render/make-review.mjs <folder with SfxReport WAVs + report.csv> <folder with catalog.csv> [out=docs/audio-review]
import fs from 'node:fs';
import path from 'node:path';

const [src, catDir, out = 'docs/audio-review'] = process.argv.slice(2);
if (!src || !catDir) { console.error('usage: make-review.mjs <report dir> <catalog dir> [out]'); process.exit(1); }

const parse = (f) => {
  const lines = fs.readFileSync(f, 'utf8').trim().split(/\r?\n/);
  const head = lines[0].split(',');
  return lines.slice(1).map((l) => { const c = l.split(','); const o = {}; head.forEach((h, i) => (o[h] = c[i] ?? '')); return o; });
};
const report = parse(path.join(src, 'report.csv'));
const catalog = Object.fromEntries(parse(path.join(catDir, 'catalog.csv')).map((r) => [r.name, r]));

// group -> [name, what it is]
const GROUPS = [
  ['Interface', [
    ['ui_confirm', 'Task done / correct answer: two soft rising bell notes (G5 then D5 up a fifth), FM bell with a detuned twin and a quiet low body. Played by TaskBoard.Complete.'],
    ['ui_error', 'Mistake / wrong answer: two muted falling notes (a minor third). Played by TaskBoard.Mistake.'],
    ['ui_select', 'Tiny tick for hovering or selecting a menu item.'],
    ['ding', 'Longer single bell for a milestone.'],
    ['error_buzz', 'Critical-mistake warning: a low double pulse, rounded. Used instead of ui_error for critical mistakes.'],
  ]],
  ['Hands and objects', [
    ['button', 'Plastic push button, press and release.'],
    ['switch', 'Toggle switch (recorded, CC0).'],
    ['valve_turn', 'Hand wheel valve: stick-slip squeak, ratchet ticks, low metal grind, closing thunk.'],
    ['grab', 'Picking something up: a soft cloth/skin rub with a small low thump. Played by Grabbable.'],
    ['release', 'Letting go: lighter and shorter than grab.'],
    ['snap', 'Something seats into a snap zone (recorded, CC0). Played by SnapZone.'],
    ['clink_metal', 'Tool or part clinks on metal (recorded, CC0; the kit low-passes it at 5.5 kHz).'],
    ['thud_box', 'A box or case set down: low thump plus a wooden knock so it also reads on small headset drivers.'],
    ['cone_place', 'Safety cone put down: hollow plastic knock and a small bounce.'],
    ['tape', 'Hazard tape pulled off the roll.'],
    ['spray', 'Aerosol can: rattle then a soft pink-noise spray.'],
    ['radio_click', 'Push-to-talk radio key click and squelch tail.'],
    ['mirror_snap', 'Small glass / mirror latch.'],
    ['led_on', 'A little LED indicator clicking on.'],
    ['gauge_tick', 'One tick of a gauge needle.'],
    ['esd_click', 'Static discharge: a tiny crack and a short crackle.'],
  ]],
  ['Water, pressure, air', [
    ['water_drip', 'A drip: air-bubble (Minnaert) resonance with a rising pitch, a splash tick, a small room. Pooled with two CC0 recordings.'],
    ['water_pour', 'Loop. Water pouring or filling: hundreds of randomly sized bubbles over a soft pink-noise body.'],
    ['pump_hum', 'Loop. Pump motor: 60 Hz harmonics, blade-pass tone, water in the casing, slow load wobble.'],
    ['pressure_tone', 'Loop. A steady rich motor note to be pitched with src.pitch while pressure rises.'],
    ['gas_hiss', 'Relief valve / leak: jet roar in the low mids with a broad hiss above and slow turbulence.'],
    ['smoke_puff', 'Soft puff of air that darkens as it fades.'],
    ['hydraulic_whine', 'Hydraulic pump spinning up and down.'],
  ]],
  ['Alarms and beeps', [
    ['alarm_siren', 'Loop. Wailing electronic siren: pitch rises and falls once per 3 s, slow vibrato, detuned twin.'],
    ['alarm_siren_rise', 'One-shot rising sweep.'],
    ['alarm_siren_fall', 'One-shot falling sweep.'],
    ['gas_monitor_beep', 'Two short beeps of a personal gas monitor (about 1.75 kHz, soft square wave).'],
    ['monitor_alarm', 'Loop. Patient-monitor style alarm: three low beeps, two higher.'],
    ['meter_beep', 'Multimeter continuity beep.'],
    ['radio_static', 'Loop. Faint radio static with crackle.'],
  ]],
  ['Vehicles', [
    ['forklift_engine_loop', 'Loop. Low diesel idle: irregular firing, exhaust puffs, injector tick.'],
    ['forklift_beep_reverse', 'Loop. Back-up beeper, one beep per second.'],
    ['forklift_horn', 'Two detuned reeds with a slight tremolo.'],
    ['pallet_drop', 'Pallet dropped (recorded wood falling, CC0).'],
  ]],
  ['Electrical (resistor bench)', [
    ['spark_pop', 'Small spark: low pop plus a band-limited crackle.'],
    ['zap', 'New. Short circuit or arc: dense crackle that thins out, a low bwomp, a falling whine.'],
    ['electric_buzz', 'New loop. Live equipment: 120 Hz transformer hum with a slow flutter and sparse crackle.'],
  ]],
  ['Footsteps', [
    ['footstep_concrete', 'Recorded (CC0, "Stone"), 6 clips. Hydro bay, warehouse.'],
    ['footstep_gravel', 'Recorded (CC0, "Sand"), 6 clips. Refinery.'],
    ['footstep_metal', 'Synthesized: thump, ringing plate, slap, toe. Grating.'],
    ['footstep_tile', 'Synthesized: heel click over a hollow floor resonance. Lobby, resistor bench.'],
    ['footstep_wood', 'Synthesized: knock, board resonance, leather scuff, an occasional creak.'],
    ['door_open', 'Door opening (recorded, CC0).'],
    ['door_close', 'Door closing (recorded, CC0).'],
  ]],
  ['Scene beds and ambient loops', [
    ['amb_lobby', 'Loop, 12 s. Lobby: air handling, faint distant murmur of people, light mains hum.'],
    ['amb_hydro', 'Loop, 12 s. Hydro bay: water flow, pump hum, small bubbles.'],
    ['amb_refinery', 'Loop, 12 s. Refinery (outdoors): open-air hush, slow wind gusts, distant flare, plant hum, faint steam.'],
    ['amb_warehouse', 'Loop, 12 s. Warehouse: big-hall tone, light ballast hum, distant machinery, overhead ventilation.'],
    ['amb_bench', 'Loop, 12 s. Resistor bench: fan noise and mains hum from supplies.'],
    ['wind_loop', 'Loop, 8 s. Wind outdoors with slow gusts and a faint whistle.'],
    ['flare_roar_loop', 'Loop, 5 s. Distant gas flare: breathing roar and fine crackle.'],
    ['hvac_loop', 'Loop, 5 s. Air handler.'],
    ['plant_drone_loop', 'Loop, 8 s. Process plant drone.'],
  ]],
  ['Distant events (scene director, 3 to 20 m away)', [
    ['far_clank', 'Distant metal clank in a big space.'],
    ['far_creak', 'Distant structure creak.'],
    ['far_steam', 'Distant steam release.'],
    ['far_relay_tick', 'Distant relay tick.'],
    ['far_thud', 'Distant heavy thud.'],
    ['far_beep', 'Distant two-beep.'],
  ]],
];

const byName = {};
for (const r of report) (byName[r.name] ??= []).push(r);
const known = new Set(GROUPS.flatMap(([, l]) => l.map(([n]) => n)));
for (const n of Object.keys(byName)) if (!known.has(n)) console.warn('no description for', n);

fs.mkdirSync(path.join(out, 'wav'), { recursive: true });
for (const f of fs.readdirSync(path.join(out, 'wav'))) fs.unlinkSync(path.join(out, 'wav', f));
let bytes = 0, files = 0;
for (const r of report) { const f = r.clip + '.wav'; fs.copyFileSync(path.join(src, f), path.join(out, 'wav', f)); bytes += fs.statSync(path.join(out, 'wav', f)).size; files++; }

const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;');
const num = (v, d = 1) => (v === '' ? '' : Number(v).toFixed(d));
const sum = (rows, k) => rows.reduce((a, r) => a + Number(r[k]), 0) / rows.length;

// ---------------------------------------------------------------- README.md
let md = `# Audio review: listen to every Unity sound

**Read this first.** The model that made these sounds cannot hear. Every number below is a measurement of the waveform (levels, spectrum, loop seams), not a judgement of how it sounds. Nothing has been played through a Vive Pro 2. Please open \`index.html\` in a browser, click the players, and tell us which sounds are wrong by name (for example "valve_turn squeaks too much", "water_pour sounds like static").

- Open [index.html](index.html) (works offline: relative paths, no scripts needed to play). Use headphones if you can: that is how it is heard in the headset.
- ${files} WAV files, ${(bytes / 1048576).toFixed(1)} MB, in \`wav/\`: 16-bit mono at each sound's own sample rate (32 kHz one-shots, 22.05 kHz loops; your browser resamples). One file per variation: the game picks them in a shuffled round robin.
- These are the exact clips the game builds (written by Unity's \`SfxReport\`), after the kit's finishing stage: gentle low-pass, fades, level set so a one-shot peaks at about -6 dBFS (quieter kinds lower) and a loop sits at its LUFS-equivalent file level. Spatial rolloff, reverb-zone share and the play gain of loops are applied in the game, not in these files.
- Columns: **peak** (dBFS), **LUFS** (BS.1770 K-weighted with gating, "LUFS-equivalent"; for a clip shorter than 0.4 s it is one block, so short effects read low), **centroid** (spectral centroid, Hz: higher is brighter), **>6 kHz** (share of energy above 6 kHz: harshness proxy), **flags** (CLIP, CLICK-START, CLICK-END, LOOP-SEAM, HARSH, QUIET, PEAK-HIGH, OFF-LUFS; none are set).
- Recorded clips (CC0, see the kit's \`Runtime/Resources/Audio/CREDITS.md\`) are marked **rec**. Everything else is synthesized in code.

`;
for (const [g, list] of GROUPS) {
  md += `## ${g}\n\n| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |\n|---|---|---|---|---|---|---|---|---|\n`;
  for (const [n, d] of list) {
    const rows = byName[n]; if (!rows) continue; const c = catalog[n] || {};
    const rec = rows.some((r) => r.recorded === 'True') ? ' **rec**' : '';
    md += `| \`${n}\`${rec} | ${d} | ${rows.length} | ${num(sum(rows, 'seconds'), 2)} s | ${num(sum(rows, 'peak_dbfs'))} | ${num(sum(rows, 'lufs'))} | ${num(sum(rows, 'centroid_hz'), 0)} Hz | ${num(sum(rows, 'share_above_6k') * 100)}% | ${c.kind === 'loop' ? `x${c.play_gain} (file ${c.lufs_file}, in scene ${c.lufs_in_scene} LUFS)` : ''} |\n`;
  }
  md += '\n';
}
md += `The full per-clip table is \`report.csv\` (every variation), and \`catalog.csv\` has the targets and play gains.

## Regenerating

\`\`\`
powershell -File scripts/unity-compile-check.ps1 -Project 04-resistor-bench
Unity.exe -batchmode -nographics -quit -projectPath vive-steamvr/04-resistor-bench -executeMethod PittBradford.XR.EditorTools.SfxReport.RunAndExit -pbWavDir <folder>
dotnet run -c Release --project tools/audio-render -- --out <catalog folder> --rec <decoded recordings>   (only for catalog.csv; also a quick offline renderer)
node tools/audio-render/make-review.mjs <folder> <catalog folder>
\`\`\`
`;
fs.writeFileSync(path.join(out, 'README.md'), md);
fs.copyFileSync(path.join(src, 'report.csv'), path.join(out, 'report.csv'));
fs.copyFileSync(path.join(catDir, 'catalog.csv'), path.join(out, 'catalog.csv'));

// ---------------------------------------------------------------- index.html
let h = `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Unity sound review</title>
<style>
:root{--bg:#fff;--fg:#14213d;--mut:#5a6578;--card:#f3f6fb;--line:#d5dcea;--acc:#003594;--gold:#b07c00;--warn:#fff4d6}
@media (prefers-color-scheme:dark){:root{--bg:#0f1624;--fg:#e8edf7;--mut:#9aa6bd;--card:#18223a;--line:#2b3957;--acc:#8fb3ff;--gold:#ffb81c;--warn:#3a3012}}
body{margin:0;background:var(--bg);color:var(--fg);font:16px/1.5 Roboto,system-ui,Segoe UI,sans-serif}
main{max-width:1100px;margin:0 auto;padding:16px}
h1{font-size:1.6rem;margin:.2em 0}h2{margin:1.6em 0 .4em;border-bottom:2px solid var(--line);padding-bottom:.2em;font-size:1.2rem;color:var(--acc)}
.note{background:var(--warn);border-left:4px solid var(--gold);padding:10px 14px;border-radius:4px}
.s{background:var(--card);border:1px solid var(--line);border-radius:8px;padding:10px 12px;margin:10px 0}
.s h3{margin:0;font:600 1rem ui-monospace,Consolas,monospace}.tag{font-size:.75rem;background:var(--acc);color:var(--bg);border-radius:3px;padding:1px 6px;margin-left:6px}
.d{margin:.2em 0 .4em;color:var(--mut)}.n{font-size:.82rem;color:var(--mut)}
.p{display:flex;flex-wrap:wrap;gap:6px 14px;margin-top:6px}.p label{font-size:.75rem;color:var(--mut);display:block}audio{height:34px;width:260px;max-width:100%}
</style></head><body><main>
<h1>Unity sound review</h1>
<p class="note"><strong>The model that made these sounds cannot hear.</strong> The numbers are measurements of the waveform, not opinions. Nothing has been played through a Vive Pro 2. Please listen (headphones if you can), then tell us which sounds are wrong, by name. Every clip is the exact one the game builds; ${files} files, ${(bytes / 1048576).toFixed(1)} MB. <a href="README.md">README</a>, <a href="report.csv">report.csv</a>.</p>
`;
for (const [g, list] of GROUPS) {
  h += `<h2>${esc(g)}</h2>\n`;
  for (const [n, d] of list) {
    const rows = byName[n]; if (!rows) continue; const c = catalog[n] || {}; const isLoop = c.kind === 'loop';
    const rec = rows.some((r) => r.recorded === 'True');
    h += `<section class="s"><h3>${n}${isLoop ? '<span class="tag">loop</span>' : ''}${rec ? '<span class="tag">recorded CC0</span>' : ''}</h3><div class="d">${esc(d)}</div>
<div class="n">${num(sum(rows, 'seconds'), 2)} s &middot; peak ${num(sum(rows, 'peak_dbfs'))} dBFS &middot; ${num(sum(rows, 'lufs'))} LUFS-eq &middot; centroid ${num(sum(rows, 'centroid_hz'), 0)} Hz &middot; above 6 kHz ${num(sum(rows, 'share_above_6k') * 100)}% &middot; crest ${num(sum(rows, 'crest_db'))} dB${isLoop ? ` &middot; in-scene gain x${c.play_gain} (${c.lufs_in_scene} LUFS)` : ''}${rows.some((r) => r.flags) ? ' &middot; FLAGS: ' + esc(rows.map((r) => r.flags).filter(Boolean).join('; ')) : ''}</div>
<div class="p">`;
    for (const r of rows) h += `<div><label>${r.clip}</label><audio controls preload="none"${isLoop ? ' loop' : ''} src="wav/${r.clip}.wav"></audio></div>`;
    h += '</div></section>\n';
  }
}
h += '</main></body></html>\n';
fs.writeFileSync(path.join(out, 'index.html'), h);
console.log(`wrote ${out}: ${files} wav, ${(bytes / 1048576).toFixed(1)} MB`);
