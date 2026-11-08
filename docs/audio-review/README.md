# Audio review: listen to every Unity sound

**Read this first.** The model that made these sounds cannot hear. Every number below is a measurement of the waveform (levels, spectrum, loop seams), not a judgement of how it sounds. Nothing has been played through a Vive Pro 2. Please open `index.html` in a browser, click the players, and tell us which sounds are wrong by name (for example "valve_turn squeaks too much", "water_pour sounds like static").

- Open [index.html](index.html) (works offline: relative paths, no scripts needed to play). Use headphones if you can: that is how it is heard in the headset.
- 208 WAV files, 14.9 MB, in `wav/`: 16-bit mono at each sound's own sample rate (32 kHz one-shots, 22.05 kHz loops; your browser resamples). One file per variation: the game picks them in a shuffled round robin.
- These are the exact clips the game builds (written by Unity's `SfxReport`), after the kit's finishing stage: gentle low-pass, fades, level set so a one-shot peaks at about -6 dBFS (quieter kinds lower) and a loop sits at its LUFS-equivalent file level. Spatial rolloff, reverb-zone share and the play gain of loops are applied in the game, not in these files.
- Columns: **peak** (dBFS), **LUFS** (BS.1770 K-weighted with gating, "LUFS-equivalent"; for a clip shorter than 0.4 s it is one block, so short effects read low), **centroid** (spectral centroid, Hz: higher is brighter), **>6 kHz** (share of energy above 6 kHz: harshness proxy), **flags** (CLIP, CLICK-START, CLICK-END, LOOP-SEAM, HARSH, QUIET, PEAK-HIGH, OFF-LUFS; none are set).
- Recorded clips (CC0, see the kit's `Runtime/Resources/Audio/CREDITS.md`) are marked **rec**. Everything else is synthesized in code.

## Interface

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `ui_confirm` | Task done / correct answer: two soft rising bell notes (G5 then D5 up a fifth), FM bell with a detuned twin and a quiet low body. Played by TaskBoard.Complete. | 4 | 1.61 s | -8.0 | -22.8 | 1085 Hz | 0.0% |  |
| `ui_error` | Mistake / wrong answer: two muted falling notes (a minor third). Played by TaskBoard.Mistake. | 4 | 0.87 s | -8.0 | -22.4 | 368 Hz | 0.0% |  |
| `ui_select` | Tiny tick for hovering or selecting a menu item. | 4 | 0.32 s | -11.0 | -26.5 | 1592 Hz | 0.1% |  |
| `ding` | Longer single bell for a milestone. | 3 | 2.12 s | -9.0 | -20.7 | 1206 Hz | 0.0% |  |
| `error_buzz` | Critical-mistake warning: a low double pulse, rounded. Used instead of ui_error for critical mistakes. | 2 | 0.60 s | -10.8 | -17.5 | 197 Hz | 0.0% |  |

## Hands and objects

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `button` | Plastic push button, press and release. | 5 | 0.22 s | -7.0 | -28.4 | 287 Hz | 0.1% |  |
| `switch` **rec** | Toggle switch (recorded, CC0). | 2 | 0.35 s | -7.0 | -31.9 | 1213 Hz | 0.3% |  |
| `valve_turn` | Hand wheel valve: stick-slip squeak, ratchet ticks, low metal grind, closing thunk. | 5 | 1.26 s | -7.0 | -24.5 | 463 Hz | 0.1% |  |
| `grab` | Picking something up: a soft cloth/skin rub with a small low thump. Played by Grabbable. | 5 | 0.32 s | -10.0 | -29.9 | 266 Hz | 0.0% |  |
| `release` | Letting go: lighter and shorter than grab. | 5 | 0.28 s | -11.0 | -32.0 | 233 Hz | 0.0% |  |
| `snap` **rec** | Something seats into a snap zone (recorded, CC0). Played by SnapZone. | 2 | 0.80 s | -6.0 | -26.8 | 1792 Hz | 2.9% |  |
| `clink_metal` **rec** | Tool or part clinks on metal (recorded, CC0; the kit low-passes it at 5.5 kHz). | 4 | 0.37 s | -7.0 | -26.3 | 2151 Hz | 4.0% |  |
| `thud_box` | A box or case set down: low thump plus a wooden knock so it also reads on small headset drivers. | 5 | 0.87 s | -6.0 | -24.8 | 112 Hz | 0.0% |  |
| `cone_place` | Safety cone put down: hollow plastic knock and a small bounce. | 5 | 0.69 s | -8.0 | -27.9 | 339 Hz | 0.0% |  |
| `tape` | Hazard tape pulled off the roll. | 4 | 0.90 s | -12.0 | -22.4 | 2570 Hz | 2.2% |  |
| `spray` | Aerosol can: rattle then a soft pink-noise spray. | 4 | 1.40 s | -13.0 | -22.6 | 3346 Hz | 7.0% |  |
| `radio_click` | Push-to-talk radio key click and squelch tail. | 4 | 0.40 s | -10.0 | -31.7 | 2046 Hz | 0.9% |  |
| `mirror_snap` | Small glass / mirror latch. | 4 | 0.67 s | -10.0 | -28.4 | 2431 Hz | 2.0% |  |
| `led_on` | A little LED indicator clicking on. | 4 | 0.20 s | -12.0 | -31.2 | 290 Hz | 0.0% |  |
| `gauge_tick` | One tick of a gauge needle. | 5 | 0.12 s | -12.0 | -34.6 | 2270 Hz | 0.3% |  |
| `esd_click` | Static discharge: a tiny crack and a short crackle. | 5 | 0.41 s | -10.0 | -32.7 | 1041 Hz | 0.0% |  |

## Water, pressure, air

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `water_drip` **rec** | A drip: air-bubble (Minnaert) resonance with a rising pitch, a splash tick, a small room. Pooled with two CC0 recordings. | 8 | 1.07 s | -10.0 | -30.6 | 1048 Hz | 0.1% |  |
| `water_pour` | Loop. Water pouring or filling: hundreds of randomly sized bubbles over a soft pink-noise body. | 2 | 4.00 s | -6.1 | -16.1 | 989 Hz | 0.1% | x0.501 (file -16, in scene -22 LUFS) |
| `pump_hum` | Loop. Pump motor: 60 Hz harmonics, blade-pass tone, water in the casing, slow load wobble. | 1 | 2.50 s | -6.3 | -16.4 | 170 Hz | 0.0% | x0.355 (file -16, in scene -25 LUFS) |
| `pressure_tone` | Loop. A steady rich motor note to be pitched with src.pitch while pressure rises. | 1 | 1.00 s | -7.6 | -16.0 | 313 Hz | 0.0% | x0.316 (file -16, in scene -26 LUFS) |
| `gas_hiss` | Relief valve / leak: jet roar in the low mids with a broad hiss above and slow turbulence. | 4 | 2.04 s | -14.0 | -25.4 | 2039 Hz | 2.4% |  |
| `smoke_puff` | Soft puff of air that darkens as it fades. | 4 | 1.00 s | -15.0 | -33.0 | 353 Hz | 0.0% |  |
| `hydraulic_whine` | Hydraulic pump spinning up and down. | 2 | 1.60 s | -13.0 | -23.2 | 347 Hz | 0.0% |  |

## Alarms and beeps

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `alarm_siren` | Loop. Wailing electronic siren: pitch rises and falls once per 3 s, slow vibrato, detuned twin. | 1 | 3.00 s | -8.4 | -14.0 | 727 Hz | 0.0% | x0.708 (file -14, in scene -17 LUFS) |
| `alarm_siren_rise` | One-shot rising sweep. | 1 | 1.80 s | -10.1 | -14.3 | 539 Hz | 0.0% |  |
| `alarm_siren_fall` | One-shot falling sweep. | 1 | 1.80 s | -10.0 | -14.3 | 689 Hz | 0.0% |  |
| `gas_monitor_beep` | Two short beeps of a personal gas monitor (about 1.75 kHz, soft square wave). | 3 | 0.71 s | -11.5 | -18.6 | 1791 Hz | 0.0% |  |
| `monitor_alarm` | Loop. Patient-monitor style alarm: three low beeps, two higher. | 1 | 1.70 s | -7.5 | -16.0 | 1149 Hz | 0.0% | x0.562 (file -16, in scene -21 LUFS) |
| `meter_beep` | Multimeter continuity beep. | 2 | 0.47 s | -13.4 | -18.2 | 1027 Hz | 0.0% |  |
| `radio_static` | Loop. Faint radio static with crackle. | 1 | 2.00 s | -7.8 | -18.0 | 1920 Hz | 0.4% | x0.355 (file -18, in scene -27 LUFS) |

## Vehicles

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `forklift_engine_loop` | Loop. Low diesel idle: irregular firing, exhaust puffs, injector tick. | 1 | 2.00 s | -6.1 | -16.5 | 135 Hz | 0.0% | x0.355 (file -16, in scene -25 LUFS) |
| `forklift_beep_reverse` | Loop. Back-up beeper, one beep per second. | 1 | 1.00 s | -10.5 | -16.0 | 1032 Hz | 0.0% | x0.562 (file -16, in scene -21 LUFS) |
| `forklift_horn` | Two detuned reeds with a slight tremolo. | 3 | 0.80 s | -6.0 | -15.8 | 665 Hz | 0.0% |  |
| `pallet_drop` **rec** | Pallet dropped (recorded wood falling, CC0). | 3 | 0.53 s | -6.0 | -26.7 | 1861 Hz | 1.0% |  |

## Electrical (resistor bench)

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `spark_pop` | Small spark: low pop plus a band-limited crackle. | 5 | 0.67 s | -8.0 | -28.5 | 2442 Hz | 0.0% |  |
| `zap` | New. Short circuit or arc: dense crackle that thins out, a low bwomp, a falling whine. | 4 | 0.91 s | -6.0 | -24.4 | 893 Hz | 0.0% |  |
| `electric_buzz` | New loop. Live equipment: 120 Hz transformer hum with a slow flutter and sparse crackle. | 1 | 2.50 s | -6.2 | -16.6 | 239 Hz | 0.0% | x0.251 (file -16, in scene -28 LUFS) |

## Footsteps

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `footstep_concrete` **rec** | Recorded (CC0, "Stone"), 6 clips. Hydro bay, warehouse. | 6 | 0.34 s | -10.0 | -28.3 | 1940 Hz | 0.7% |  |
| `footstep_gravel` **rec** | Recorded (CC0, "Sand"), 6 clips. Refinery. | 6 | 0.36 s | -10.0 | -31.3 | 2306 Hz | 1.1% |  |
| `footstep_metal` | Synthesized: thump, ringing plate, slap, toe. Grating. | 8 | 0.72 s | -10.0 | -30.0 | 637 Hz | 0.0% |  |
| `footstep_tile` | Synthesized: heel click over a hollow floor resonance. Lobby, resistor bench. | 8 | 0.72 s | -10.0 | -33.5 | 414 Hz | 0.1% |  |
| `footstep_wood` | Synthesized: knock, board resonance, leather scuff, an occasional creak. | 8 | 0.72 s | -10.0 | -30.1 | 223 Hz | 0.0% |  |
| `door_open` **rec** | Door opening (recorded, CC0). | 2 | 0.44 s | -8.0 | -24.7 | 2697 Hz | 0.8% |  |
| `door_close` **rec** | Door closing (recorded, CC0). | 3 | 0.59 s | -7.0 | -26.6 | 1064 Hz | 0.7% |  |

## Scene beds and ambient loops

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `amb_lobby` | Loop, 12 s. Lobby: air handling, faint distant murmur of people, light mains hum. | 1 | 12.00 s | -6.2 | -18.0 | 404 Hz | 0.0% | x0.282 (file -18, in scene -29 LUFS) |
| `amb_hydro` | Loop, 12 s. Hydro bay: water flow, pump hum, small bubbles. | 1 | 12.00 s | -6.4 | -18.0 | 345 Hz | 0.0% | x0.282 (file -18, in scene -29 LUFS) |
| `amb_refinery` | Loop, 12 s. Refinery (outdoors): open-air hush, slow wind gusts, distant flare, plant hum, faint steam. | 1 | 12.00 s | -6.7 | -18.0 | 523 Hz | 0.0% | x0.282 (file -18, in scene -29 LUFS) |
| `amb_warehouse` | Loop, 12 s. Warehouse: big-hall tone, light ballast hum, distant machinery, overhead ventilation. | 1 | 12.00 s | -6.4 | -18.0 | 353 Hz | 0.0% | x0.282 (file -18, in scene -29 LUFS) |
| `amb_bench` | Loop, 12 s. Resistor bench: fan noise and mains hum from supplies. | 1 | 12.00 s | -6.3 | -18.0 | 488 Hz | 0.0% | x0.251 (file -18, in scene -30 LUFS) |
| `wind_loop` | Loop, 8 s. Wind outdoors with slow gusts and a faint whistle. | 1 | 8.00 s | -6.2 | -18.0 | 548 Hz | 0.0% | x0.200 (file -18, in scene -32 LUFS) |
| `flare_roar_loop` | Loop, 5 s. Distant gas flare: breathing roar and fine crackle. | 1 | 5.00 s | -6.3 | -18.0 | 558 Hz | 0.0% | x0.251 (file -18, in scene -30 LUFS) |
| `hvac_loop` | Loop, 5 s. Air handler. | 1 | 5.00 s | -6.6 | -18.0 | 340 Hz | 0.0% | x0.178 (file -18, in scene -33 LUFS) |
| `plant_drone_loop` | Loop, 8 s. Process plant drone. | 1 | 8.00 s | -6.4 | -18.1 | 169 Hz | 0.0% | x0.224 (file -18, in scene -31 LUFS) |

## Distant events (scene director, 3 to 20 m away)

| Sound | What it is | Clips | Length | Peak | LUFS | Centroid | >6 kHz | Loop gain in scene |
|---|---|---|---|---|---|---|---|---|
| `far_clank` | Distant metal clank in a big space. | 5 | 2.03 s | -9.0 | -26.0 | 475 Hz | 0.0% |  |
| `far_creak` | Distant structure creak. | 4 | 1.79 s | -12.0 | -25.4 | 499 Hz | 0.0% |  |
| `far_steam` | Distant steam release. | 3 | 1.60 s | -14.0 | -24.7 | 2058 Hz | 0.2% |  |
| `far_relay_tick` | Distant relay tick. | 4 | 0.44 s | -12.0 | -32.6 | 1147 Hz | 0.0% |  |
| `far_thud` | Distant heavy thud. | 4 | 1.63 s | -9.0 | -25.9 | 99 Hz | 0.0% |  |
| `far_beep` | Distant two-beep. | 3 | 2.03 s | -12.0 | -20.8 | 1003 Hz | 0.0% |  |

The full per-clip table is `report.csv` (every variation), and `catalog.csv` has the targets and play gains.

## Regenerating

```
powershell -File scripts/unity-compile-check.ps1 -Project 04-resistor-bench
Unity.exe -batchmode -nographics -quit -projectPath vive-steamvr/04-resistor-bench -executeMethod PittBradford.XR.EditorTools.SfxReport.RunAndExit -pbWavDir <folder>
dotnet run -c Release --project tools/audio-render -- --out <catalog folder> --rec <decoded recordings>   (only for catalog.csv; also a quick offline renderer)
node tools/audio-render/make-review.mjs <folder> <catalog folder>
```
