# Resistor Selection and Check Bench (Vive Pro 2 / SteamVR, with desktop fallback)

**Status (November 1, 2026): set up and opened as its own project in Unity 6000.3.20f1; compiles with no errors; the scripted screenshot run plays the scenario in the editor (desktop, no headset) and stages each step with no scenario errors in the console. A person has not played it through by hand, and it has not been tested on a Vive Pro 2 or any headset.**

![Bench overview: desktop editor render, not a headset](screenshots/01-bench-overview.png)

## Screenshots

All pictures are **desktop editor renders** made by the scenario's scripted capture (`-pbCapture`), not headset captures. The scripted run places parts directly (it does not use the hands), so the pictures show the scene and its responses, not a hand-played session. Name tags turn to face the viewer, so they look slightly different from each viewpoint.

| | |
|---|---|
| ![ESD strap clipped to the ground hook](screenshots/02-esd-station.png) | ![Tiered parts rack, answer panel and the unknown part](screenshots/03-color-code-reading.png) |
| 2. ESD strap clipped to the ground hook (desktop editor render) | 3. Tiered parts rack with bin labels, answer panel, unknown part (desktop editor render) |
| ![Both probes on the unknown part; the meter reads 4.7 k](screenshots/04-measuring.png) | ![Anti-sulfur 150 ohm part in R1, LED lit at 5 V](screenshots/05-led-lit.png) |
| 4. Both probes on the unknown part; meter reads 4.7 k (desktop editor render) | 5. Anti-sulfur 150 ohm part in R1, LED lit (desktop editor render) |
| ![47 ohm part powered: the LED burns out with a puff of smoke](screenshots/06-burnt-led-mistake.png) | ![Wall references and the university plate](screenshots/07-wall-references.png) |
| 6. 47 ohm part powered: LED burned out, smoke puff (desktop editor render) | 7. Color-code poster, circuit / datasheet / environment cards, university plate (desktop editor render) |

### Look and signage

Signs and labels use the shared kit's signage helpers (`Ui.Panel`, `Ui.Tag`, `Ui.BrandPlate` in `Signage.cs`) instead of bare floating text:

- **Rounded panels** with a border, accent bar and soft shadow for the answer panel, the CHECK PART sign (both on a thin steel backing), the color-code poster and the three wall cards.
- **Name tags with leader lines** that turn to face the player for the ground hook, parts drawer, multimeter, LED test board and 5 V supply.
- **Laminated labels lying flat on the mat** for the wrist strap, selection tray, meter probes, spare LEDs, the anti-sulfur legend and the ESD mat.
- **Bin label cards** tipped back on the front of each bin; the back row of bins stands on a riser so its labels are not hidden.
- A **university / CIST plate** on the wall above the cards. The Pitt-Bradford logo appears only in local builds that have the git-ignored file `Resources/Branding/pitt-bradford`; otherwise it shows a text wordmark (as in the screenshots).
- Text printed on objects (meter and supply displays, button faces, breadboard silkscreen, the reading card) is left as it was.
- Cheap realism: concrete floor, painted wall with a base trim, ceiling tiles and fixture frames, wood-grain bench top with an apron, foot rails and a lower shelf, textured ESD mat, a light under each ceiling fixture plus a fill and one shadow-casting key light, soft round smoke puffs.

A Unity 6 (`6000.3.20f1`, built-in render pipeline) VR training example, inspired by the kind of work done by makers of passive electronic components (resistors including current-sense and anti-sulfur types, fuses, varistors, thermal sensors). It is **generic education only**: it makes no claims about any company's real processes, products, or datasheets. The derating curve, the pad size, and the LED limits are illustrative numbers invented for the exercise.

Everything is built from Unity primitives at run time (no imported art). It uses the shared kit in [`../packages/com.pittbradford.xrtraining`](../packages/com.pittbradford.xrtraining).

## Learning goals

- Ground yourself (ESD strap) before handling parts.
- Read a resistor color code and confirm it with a meter, including the tolerance band.
- Use Ohm's law to size an LED series resistor, pick the nearest standard (E24) value, and choose tolerance and power rating.
- Use a derating card: a part's usable power falls with temperature, so a rated 1/8 W part is not a 1/8 W part at 85 C. Reject both an under-rated part and an oversize "bigger is safer" part.
- Match the part type to the environment: in a sulfur-rich atmosphere pick an anti-sulfur type.
- See what happens when a clearly wrong part is powered (dim LED, or a burned-out LED).

## Scenario and steps

You stand at an electronics bench. On it: an ESD mat with a wrist-strap hook, a two-tier parts rack ("parts drawer") with six labelled bins, a color-code poster on the wall, an unknown resistor on a card, a multimeter with two probes, a small circuit board with places for one resistor and one LED, a 5 V supply, a selection tray with a CHECK PART button, and three wall cards (LED circuit, generic datasheet with a derating line, environment).

1. **ESD strap.** Grab the wrist strap and release it near the hook (it snaps on). Touching parts first is a mistake.
2. **Read and measure.** Read the bands of the unknown resistor (poster), press the matching answer (470 ohm / 4.7 k ohm / 47 k ohm / 4.7 M ohm), then put both probes on its two leads. The meter shows a simulated reading within the part's tolerance. The step completes when the answer is right and the meter has read that part.
3. **Choose the LED resistor.** The circuit card gives 5 V, 2 V LED drop, 20 mA. R = (5 - 2) / 0.020 = 150 ohm, which is a standard value. Power = 0.02 x 0.02 x 150 = 0.06 W. The datasheet card derates a rated power linearly from 100% at 70 C to 0 at 155 C; at the board's 85 C that is 82%, so a 1/8 W part is good for about 0.103 W (enough), a 1/16 W part for 0.051 W (not enough), and a 1/2 W body is oversize for the pads. Take a part from a bin, put it in the selection tray, press CHECK PART. A wrong value, loose tolerance, too little derated power, or an oversize body is a mistake.
4. **Sulfur-rich air.** The environment card says the station air is sulfur-rich. Install an **anti-sulfur** 150 ohm part (green dot on the body, "ANTI-SULFUR" on the bin label) in the board's R1 place, put an LED in LED1, and switch on the supply. The LED lights at about 20 mA. A wrong value dims the LED or burns it out with a puff of smoke; powering with a clearly wrong part (over 30 mA, or no resistor at all) is a **critical miss**. A standard (non-anti-sulfur) part or an overheating part is a mistake. A spare LED is on the bench.

The task board tracks mistakes and critical misses like the other examples.

## Controls

| | Vive wands (SteamVR) | Desktop fallback |
|---|---|---|
| Move | Left trackpad / stick | W A S D |
| Turn | Right trackpad / stick (snap turn) | Hold right mouse and move |
| Press (buttons, switch, answers) | Point + trigger | Left click on the crosshair |
| Grab | Grip near the object | E (object under the crosshair) |
| Release | Let go of grip | Release E |

Probes: you can hold one in each hand and touch the leads, or release a probe near a lead and it clips on (needed on the desktop, where only one object can be held at a time).

## What is simulated and what is not

Simulated: color-code mapping and reading, a meter reading with a small repeatable error within half the part's tolerance, Ohm's law for the LED, a generic linear derating curve, LED brightness vs current, burn-out above 30 mA, an ESD strap "grounded when clipped to the hook" rule, and the idea that sulfur-rich air calls for an anti-sulfur type.

Not simulated: real ESD physics, meter loading and in-circuit measurement, temperature rise, real sulfur corrosion, real datasheet curves, current-sense (low-value) resistors, fuses, varistors, thermal sensors, or any manufacturer's processes. The strap is clipped to a bench hook instead of being worn. Sulfur and ambient temperature are fixed by the cards.

This is general awareness practice, not a procedure, qualification, or certification.

## Setup

1. Install Unity Hub and Unity **6000.3.20f1** (Windows Build Support).
2. Open `vive-steamvr/04-resistor-bench` in Unity Hub (the shared kit is referenced by a local `file:` package in `Packages/manifest.json`; it resolves on first open).
3. Run the menu **Pitt-Bradford > Set up project** (configures OpenXR and the Input System, creates `Assets/Scenes/ResistorBench.unity`). Restart the editor once if it asks about the input handling change.
4. Set **SteamVR as the OpenXR runtime** (SteamVR > Settings > Developer > Set SteamVR as OpenXR runtime), start SteamVR with the Vive Pro 2, then press **Play**. With no headset it runs on the desktop controls.
5. Optional build: **Pitt-Bradford > Build Windows player** writes `Builds/ResistorBench/ResistorBench.exe`.
6. Screenshots (optional), with the project closed in the editor (do not pass `-batchmode`; it needs graphics):
   `Unity.exe -projectPath <absolute path to 04-resistor-bench> -executeMethod PittBradford.XR.EditorTools.CaptureRunner.Run -pbCapture <absolute screenshots folder> -logFile <log>`.
   It enters play mode, takes seven shots (`01-bench-overview`, `02-esd-station`, `03-color-code-reading`, `04-measuring`, `05-led-lit`, `06-burnt-led-mistake`, `07-wall-references`) and exits. For the capture only, the scenario stops any OpenXR session that started (the editor may start the OpenXR Mock runtime, which otherwise tilts the pictures and blocks the field of view), hides the desktop hands, and moves the hidden player to each camera position so the name tags face the picture.

## Code layout

- `Assets/Scripts/ResistorBench.cs`: the scenario (`ResistorBench : MonoBehaviour, IShotScript`) plus small components `BenchResistor`, `BenchProbe`, `BenchLed`.
- `Assets/Scripts/ResistorMath.cs`: pure C# (no Unity types): color bands, E24, Ohm's law, derating, simulated measurement, part check.
- `Assets/Editor/ProjectSetup.cs`: the menu items.

## Plain C# test list (`ResistorMath`)

Not yet run. Each line is input, then expected output.

- `ColorBands(150, 5)` -> `[1,5,1,10]` (brown green brown gold)
- `ColorBands(150, 1)` -> `[1,5,0,0,1]` (brown green black black brown)
- `ColorBands(4700, 5)` -> `[4,7,2,10]` (yellow violet red gold)
- `ColorBands(4.7, 5)` -> `[4,7,10,10]` (multiplier gold = x0.1)
- `ColorBands(1e6, 5)` -> `[1,0,5,10]`
- `ColorBands(99.6, 5)` -> `[1,0,1,10]` (rounds up to 100)
- `ColorBands(0.05, 5)` -> `null`; `ColorBands(100, 3)` -> `null` (no 3% color)
- `ValueFromBands([4,7,2,10])` -> `4700`; `ValueFromBands([1,5,0,0,1])` -> `150`; `ValueFromBands([12,0,0,10])` -> `-1`
- `ToleranceColor(5)` -> `10`; `ToleranceColor(1)` -> `1`; `ToleranceColor(3)` -> `-1`; `ToleranceFromColor(11)` -> `10`
- Round trip: for each value in the bin list, `ValueFromBands(ColorBands(v, t))` equals `v` for t in {1, 5}
- `LedResistor(5, 2, 0.02)` -> `150` (within 1e-9)
- `NearestStandard(150)` -> `150`; `NearestStandard(140)` -> `150`; `NearestStandard(98)` -> `100`; `NearestStandard(0)` -> `0`
- `LedCurrent(5, 2, 150)` -> `0.02`; `LedCurrent(5, 2, 0)` -> `Infinity`; `LedCurrent(1, 2, 150)` -> `0`
- `PowerW(0.02, 150)` -> `0.06`
- `DeratedWatts(0.125, 70)` -> `0.125`; `(0.125, 85)` -> `0.10294` (+/- 1e-5); `(0.125, 155)` -> `0`; `(0.125, 200)` -> `0`; `(0.5, 85)` -> `0.41176`; `(0.0625, 85)` -> `0.05147`
- `ClassifyLed(0.02)` -> `Normal`; `(0.0638)` -> `Burnt`; `(0.0091)` -> `Dim`; `(0.0245)` -> `Over`; `(0)` -> `Off`; `(Infinity)` -> `Burnt`
- `MeasuredOhms(4700, 5, 7)` lies in [4582.5, 4817.5] and returns the same value on repeat calls
- `Confirms(4731, 4700, 5)` -> true; `Confirms(5000, 4700, 5)` -> false
- `CheckPart(150 ohm 5% 1/8 W, 5, 2, 0.02, 85, 5, 0.25).Passed` -> true
- `CheckPart(150 ohm 1% 1/2 W, ...)`: `FitsOk` false, `PowerOk` true, `Passed` false
- `CheckPart(150 ohm 5% 1/16 W, ...)`: `PowerOk` false (0.051 W derated vs 0.060 W)
- `CheckPart(47 ohm 1% 1/8 W, ...)`: `ValueOk` false
- `CheckPart(150 ohm 10% 1/8 W, ...)`: `ToleranceOk` false
- `FormatOhms(4700)` -> `"4.7 k"`; `(150)` -> `"150"`; `(1e6)` -> `"1 M"`; `(150.4)` -> `"150.4"`; `(4731)` -> `"4.73 k"`
- `WattsText(0.125)` -> `"1/8 W"`; `(0.0625)` -> `"1/16 W"`

## Research

See [../../docs/research/README.md](../../docs/research/README.md) for the annotated bibliography and study ideas for this line of work.
