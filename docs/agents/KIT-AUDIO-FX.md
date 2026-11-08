STATUS: READY

# Kit audio and effects API (round 3, sound revised in rounds 4 and 5)

**Round 5 (Nov 6, 2026) in short, unheard, measured only. Nobody, including the model that wrote it, has listened to any of it.** Same API, same names (two new: `zap`, `electric_buzz`). What changed: (1) reverb is now convolution with a synthetic impulse response (early reflections plus a three-band diffuse tail) instead of comb filters; (2) struck metal and glass have beating mode pairs; (3) water is a Minnaert bubble model (drips, pouring, hydro bed); (4) interface sounds are FM bells; beeps are soft square waves with the 5th and 7th harmonics removed; (5) the five beds and the machine loops were rebuilt with mid-range content (centroids were 68 to 254 Hz, now 340 to 520 Hz for the beds, none of the loops under 120 Hz), with a faint distant murmur in the lobby and distant flare, wind and plant hum in the refinery; (6) concrete and gravel footsteps are now CC0 recordings (metal, tile, wood remain synthesized, reworked); (7) new levelling: a one-shot is scaled so its PEAK lands at -6 dBFS (quieter kinds lower, footsteps -10), a loop is normalised to a LUFS-equivalent file level (beds -18, machines -16, alarms -14) and a per-name play gain brings it down to its in-scene level, so `volume` arguments keep their meaning; (8) spatial sounds use a custom rolloff that fades to zero at the maximum distance, far one-shots are darkened by distance (a low-pass on the pooled voice), and variations play in a shuffled round robin; (9) everything is generated on a background thread a few clips per frame (`Sfx.PreloadBackground`, started by itself) so nothing is built in the middle of play. Listenable review of every clip: `docs/audio-review/index.html`.

**Round 4 (Nov 5, 2026) in short, unheard, measured only.** Every old name still works with the same call. Changed: all sounds re-synthesized (modal impacts, pink noise, softer alarms, seamless loops), loudness targets per kind with a soft limiter, a distance low-pass on loops, and new per-scene audio (`SceneAudio`, see below) that starts by itself in the five known scenes, so scenario scripts do not need to change. New names: `amb_lobby amb_hydro amb_refinery amb_warehouse amb_bench` (loops), `far_clank far_creak far_steam far_relay_tick far_thud far_beep` (distant one-shots the scene director uses). Nobody has listened to any of it.

Namespace `PittBradford.XR`, in `vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/`: `Sfx.cs` (sounds), `Synth.cs` (synthesis, you do not call it), `Fx.cs` (lighting and particles). Built-in render pipeline, no packages. Everything is made in code.

Rules: never edit the kit. Call these from your scenario. Nothing plays during a scene bake (`Sfx` checks this itself) and `Sfx` calls are safe from `Start`, `Update`, event handlers.

## Sfx

```csharp
Sfx.Play("valve_turn", transform.position);                 // one-shot 3D; (name, pos, volume = 1, pitchJitter = 0.05)
Sfx.Play2D("ui_confirm", 0.8f);                             // non-spatial, for UI and global alarms
var hum = Sfx.Loop("pump_hum", pumpTransform, 0.6f, 1.5f, 14f);   // looping 3D source parented to pumpTransform
hum.pitch = 1f + pressure / 300f;                           // you own the returned AudioSource; hum.Stop() to end
Sfx.Footstep(feetPos, Surface.Metal);                       // Concrete, Metal, Tile, Gravel, Wood
Sfx.Enabled = false;                                        // master mute (also automatic during a bake)
```

- Several variations per name are generated and played in a shuffled round robin (each is used once before any repeats, and a new round never starts with the clip just played), plus a small pitch jitter and about 8 percent volume jitter (4 to 6 variations for most one-shots, 6 for footsteps).
- One-shots share a pool of 24 AudioSources: no allocation per sound. Spatial rolloff is a custom inverse-distance curve, 1.2 m to 25 m, fading to silence over the last third (so nothing cuts off), Doppler off, spread 20 degrees, priority by kind (alarms 40 to 60, interaction about 100, machines about 150, ambience 200+), and a per-name share of the scene reverb (2D sounds get none). `Loop` takes its own min and max distance.
- Every loop made with `Sfx.Loop` also gets a distance low-pass (`SfxFar`: 18 kHz near, 1.8 kHz at the far distance, updated 8 times a second). One-shots beyond 6 m get a low-pass on their pooled voice (14 kHz falling to 2.2 kHz at 25 m), set when the sound starts.
- `Loop` sources are created under your parent but kept out of the bake (DontBake). Keep the returned source if you want to stop it.
- Unknown names log one warning and play nothing. `Sfx.Names` lists the catalogue, `Sfx.IsLoop(name)` and `Sfx.IsSample(name)` describe a name.
- `Sfx.PreloadBackground()` builds the rest of the catalogue on a worker thread and turns the results into clips a few per frame (`SfxPump`); it starts by itself at the first `Play`, `Play2D` or `Loop` and from the scene boot. A sound asked for before its turn is built on the spot. Untested in play mode (see below).
- Call `Sfx.Preload()` once in `Start` if you want to avoid a small first-play hitch (it generates every clip, a fraction of a second).

### Catalogue (use these exact strings)

One-shots: `ui_confirm ui_error ui_select button switch valve_turn grab release snap clink_metal thud_box cone_place tape spray water_drip gas_hiss alarm_siren_rise alarm_siren_fall gas_monitor_beep radio_click forklift_horn hydraulic_whine pallet_drop mirror_snap led_on spark_pop smoke_puff esd_click meter_beep door_open door_close ding error_buzz gauge_tick footstep_concrete footstep_metal footstep_tile footstep_gravel footstep_wood far_clank far_creak far_steam far_relay_tick far_thud far_beep zap`

Loops (use `Sfx.Loop`): `water_pour pump_hum alarm_siren monitor_alarm radio_static forklift_engine_loop forklift_beep_reverse wind_loop hvac_loop plant_drone_loop flare_roar_loop pressure_tone electric_buzz amb_lobby amb_hydro amb_refinery amb_warehouse amb_bench`

`pressure_tone` is a soft steady tone meant to be pitched with `src.pitch` (0.8 to 1.8) while pressure rises. `alarm_siren` is a wailing loop; `alarm_siren_rise` and `alarm_siren_fall` are one-shot sweeps. `forklift_beep_reverse` is one beep per second, loop it while reversing.

Typical `volume` arguments: ambience beds 0.2 to 0.5, machines 0.4 to 0.7, interaction sounds 0.6 to 1, alarms 0.5 to 0.8.

**Loudness (round 5).** The table is `SfxCatalog.cs` (no Unity types; `Sfx.cs` reads it). One-shots: scaled so the peak is the entry's `peak` (default -6 dBFS; interface -8 to -11, small objects -7 to -12, footsteps -10, distant events -9 to -14, hisses -13 to -15), with an RMS cap for sustained sounds (alarms -14, beeps -17). No limiter squashes the transient. Loops: normalised to `lufs` (K-weighted, gated, "LUFS-equivalent": beds and ambience -18, machines and water -16, alarms -14) with a soft limit at -6 dBFS, and `Gain` (linear, play time) = in-scene loudness minus file loudness, so a bed that sits at -29 LUFS in the scene is stored at -18 and played at gain 0.28. `Sfx.Play`, `Play2D` and `Loop` multiply `volume` by that gain, so old call-site values keep their meaning. In-scene targets (LUFS-equivalent at volume 1): beds -29 to -30, wind/hvac/plant/flare -30 to -33, pump -25, engine -25, water pour -22, alarms -17 to -21, buzz -28. Change them in the table; `SfxReport` flags PEAK-HIGH and OFF-LUFS. Footsteps are the quietest interaction sound by design.

### Scene audio (`SceneAudio`, new in round 4)

```csharp
SceneAudio.Begin(Place.Refinery, null, 1f, rig);   // optional: the scenes below already do this by themselves
SceneAudio.Room(RoomKind.Warehouse);                // just the room reverb (one AudioReverbZone covering the scene)
SceneAudio.AutoStart = false;                       // opt out of the automatic start (set before the first frame)
```

- **Automatic:** `SceneAudioBoot` starts `Begin` 0.7 s after a scene loads when the scene name contains `lobby`, `hydro`, `refinery`, `warehouse`, `resistor` or `bench` (so `HydrostaticTest`, `RefineryRounds_Hub` and the rest). It does nothing during a bake. A scenario that calls `SceneAudio.Begin` itself takes over (no double start).
- **What `Begin` makes** (one helper object `Scene audio`, kept out of bakes): a reverb zone, the place's bed loop (`amb_*`, nearly head-locked, no distance filter, at its calibrated level times `bedVolume`), and a director that plays a rare distant one-shot every 5 to 120 s depending on the place (`far_*`, or `water_drip` in the hydro bay), 3 to 20 m from the listener.
- **Rooms:** SmallLab (resistor bench) uses Unity's `Room` preset, Lobby `Hallway`, Bay (hydro) `Stoneroom`, Warehouse `Auditorium`, Outdoor (refinery) `Plain`. Each sound's share of the reverb is set per name (`mix`); UI sounds get 0.2 to 0.3.
- **Surfaces:** `SceneAudio.SurfaceFor(place)` gives Lobby Tile, HydroBay Concrete, Refinery Gravel, Warehouse Concrete, ResistorBench Tile. `Begin` only applies it when you pass the rig; the automatic start does not touch the rig, so scenario code still owns `rig.FloorSurface` (the refinery and the bench already set theirs).
- Existing scenario loops (hvac, plant drone, wind, forklift) are unchanged and play on top of the bed.

### Measuring (it cannot hear)

- `-executeMethod PittBradford.XR.EditorTools.SfxReport.RunAndExit -pbWavDir <folder>` writes (round 5: it does run with `-batchmode -nographics -quit`, no window; `SceneAudioCheck` still needs a window and a graphics device: in batch mode entering play mode crashed Unity here) every variation of every name as a WAV plus `report.csv`: peak, RMS, active RMS, crest, spectral centroid, share of energy above 6 kHz, and LUFS-equivalent, and flags `CLIP`, `CLICK-START`, `CLICK-END`, `LOOP-SEAM`, `HARSH`, `QUIET`, `PEAK-HIGH`, `OFF-LUFS`. The numbers come from `SfxMeasure.cs` (no Unity types). Menu: Pitt-Bradford > Sfx report.
- `-executeMethod PittBradford.XR.EditorTools.SceneAudioCheck.RunAndExit` enters play mode in the first build scene and checks the scene audio objects, reverb presets, the pooled voice settings, the loop low-pass, and that nothing plays while a bake flag is set.
- `SfxSelfTest.RunAndExit` still runs: no NaN, not silent, peak under 0.99; round 5 adds edit-mode checks of the custom rolloff curve, the catalogue gains and ceilings, and the shuffled round robin (384 clips, PASS).
- `tools/audio-render` is a fast offline renderer (`dotnet`, no Unity): it compiles `Synth.cs`, `SfxCatalog.cs` and `SfxMeasure.cs` with a stand-in for `Mathf`, renders every clip in about 6 seconds and writes `catalog.csv`. Its random streams differ slightly from Unity's Mono (reverberant sounds differ by a few dB), so it is for quick iteration; `docs/audio-review` uses the clips Unity itself builds.
- `node tools/audio-render/make-review.mjs <SfxReport folder> <catalog folder>` rebuilds `docs/audio-review` (WAVs, README, index.html with players).

### Built into the kit (do not add these yourself)

- `Grabbable` plays `grab` on pick-up and `release` when dropped; `SnapZone` plays `snap` when something snaps in.
- `XRRig` plays footsteps while the player walks. Set `rig.FloorSurface = Surface.Metal` (default Concrete) to match your floor, or `rig.Footsteps = false`.
- `TaskBoard.Complete` plays `ui_confirm`; `TaskBoard.Mistake` plays `ui_error` (or `error_buzz` when critical). Do not add these sounds again.
- `Pressable` plays nothing: choose the sound in your handler (`button`, `switch`, `valve_turn`, `ui_select` ...).
- `P.Tone` / `P.Noise` still work but are the old bare sounds: use `Sfx` instead.

## Fx

All effects are created through the kit's bake helpers, so a bake saves them and a baked scene adopts them. Call them from the same place you build the rest of the scene; setup is applied only to new objects. Positions are in the parent's local space (or world when `parent` is null).

```csharp
Fx.ApplyMood(MoodPreset.WarehouseDim, keyLight);            // ambient trilight, fog, pixel light cap, key light level
Fx.ApplyMood(MoodPreset.IndoorBright | WarehouseDim | OutdoorOvercast | LabNeutral);
Fx.Practical(root, lampPos, new Color(1f,.93f,.8f), 7f, 0.8f);       // point light for a visible fixture; first 6 are pixel lights, later ones vertex lights
Fx.LightShaft(root, lampPos, 4.5f, 40f, new Color(1f,.95f,.85f), 0.08f); // additive cone (length m, full angle deg, intensity)
Fx.Dust(root, shaftCentre, new Vector3(2f,3f,2f), 40);       // floating motes in a box
Fx.Steam(root, ventPos, Vector3.up, 18f);                    // plume; Fx.Smoke(root, pos, 6f, 0.35f) dark and slow
Fx.Sparks(root, pos, 2.5f, 14);                              // burst every 2.5 s; interval 0 = call .Play() yourself
Fx.Drip(root, pos, 0.8f, true);                              // drops plus a 3D drip sound on landing
Fx.ReflectionProbe(roomCentre, roomSize);                    // realtime, 64 px, refreshed once at start
```

`Fx.ParticleMat(additive)` and `Fx.SoftTexture()` are public if you need to make your own particles with the same look. `Fx.PixelLightBudget` (default 6) can be changed before you create practicals. There is no heat-shimmer helper: it would need a grab pass, which is not cheap in VR.

## Not provided

No Audio Mixer asset, no web audio. Recorded CC0 samples (when present) are listed in the kit README and `Runtime/Resources/Audio/CREDITS.md`; they override synthesis for the same name automatically, so your code does not change.

## What round 5 could not verify

Not listened to by anyone. Not run in play mode: the custom rolloff (only its curve is checked in edit mode), the per-voice distance low-pass, the background loader (`PreloadBackground` / `SfxPump`) and the play gains were compiled and reasoned about, and `SceneAudioCheck` could not be re-run (a first attempt in batch mode crashed Unity in XR start-up; a windowed attempt hung on the Unity "Recovering Scene Backups" dialog left by that crash). Not tried on a Vive Pro 2. `SceneAudioCheck` should be re-run once with a window before the demo. First-play cost on Unity's Mono: the whole catalogue generated in about 2.2 s of CPU (`SfxReport`), now spread over the first seconds of a scene on a background thread.
