STATUS: READY

# Kit audio and effects API (round 3, sound revised in round 4)

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

- Several variations per name are generated and picked at random (never the same twice in a row), plus a small pitch jitter and about 8 percent volume jitter (4 to 6 variations for most one-shots, 6 for footsteps).
- One-shots share a pool of 24 AudioSources: no allocation per sound. Spatial rolloff is logarithmic, 1.2 m to 25 m, Doppler off, spread 20 degrees, priority by kind (alarms 40 to 60, interaction about 100, machines about 150, ambience 200+), and a per-name share of the scene reverb (2D sounds get none). `Loop` takes its own min and max distance.
- Every loop made with `Sfx.Loop` also gets a distance low-pass (`SfxFar`: 18 kHz near, 1.8 kHz at the far distance, updated 8 times a second). One-shots do not (it would need a filter on every pooled voice).
- `Loop` sources are created under your parent but kept out of the bake (DontBake). Keep the returned source if you want to stop it.
- Unknown names log one warning and play nothing. `Sfx.Names` lists the catalogue, `Sfx.IsLoop(name)` and `Sfx.IsSample(name)` describe a name.
- Call `Sfx.Preload()` once in `Start` if you want to avoid a small first-play hitch (it generates every clip, a fraction of a second).

### Catalogue (use these exact strings)

One-shots: `ui_confirm ui_error ui_select button switch valve_turn grab release snap clink_metal thud_box cone_place tape spray water_drip gas_hiss alarm_siren_rise alarm_siren_fall gas_monitor_beep radio_click forklift_horn hydraulic_whine pallet_drop mirror_snap led_on spark_pop smoke_puff esd_click meter_beep door_open door_close ding error_buzz gauge_tick footstep_concrete footstep_metal footstep_tile footstep_gravel footstep_wood far_clank far_creak far_steam far_relay_tick far_thud far_beep`

Loops (use `Sfx.Loop`): `water_pour pump_hum alarm_siren monitor_alarm radio_static forklift_engine_loop forklift_beep_reverse wind_loop hvac_loop plant_drone_loop flare_roar_loop pressure_tone amb_lobby amb_hydro amb_refinery amb_warehouse amb_bench`

`pressure_tone` is a soft steady tone meant to be pitched with `src.pitch` (0.8 to 1.8) while pressure rises. `alarm_siren` is a wailing loop; `alarm_siren_rise` and `alarm_siren_fall` are one-shot sweeps. `forklift_beep_reverse` is one beep per second, loop it while reversing.

Typical `volume` arguments: ambience beds 0.2 to 0.5, machines 0.4 to 0.7, interaction sounds 0.6 to 1, alarms 0.5 to 0.8.

**Loudness (round 4).** Each name is levelled to a target (RMS of the active part, dBFS, with a 6 dB overall trim already included), then a soft-knee limiter keeps the peak under 0.85 (about -1.4 dBFS). Targets, quietest to loudest: scene beds about -31, ambience loops -26 to -28, machines -21 to -22, footsteps and small objects -19 to -20, interaction -15 to -17, impacts and alarms -13 to -16. `volume` arguments at the call site multiply on top, so the old values keep their relative balance. Footsteps are the kit's quietest interaction sound by design.

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

- `-executeMethod PittBradford.XR.EditorTools.SfxReport.RunAndExit -pbWavDir <folder>` (no `-batchmode`) writes every variation of every name as a WAV plus `report.csv`: peak, RMS, active RMS, crest, spectral centroid, share of energy above 6 kHz, and flags `CLIP`, `CLICK-START`, `CLICK-END`, `LOOP-SEAM`, `HARSH`, `QUIET`, `LIMITED`. Menu: Pitt-Bradford > Sfx report.
- `-executeMethod PittBradford.XR.EditorTools.SceneAudioCheck.RunAndExit` enters play mode in the first build scene and checks the scene audio objects, reverb presets, the pooled voice settings, the loop low-pass, and that nothing plays while a bake flag is set.
- `SfxSelfTest.RunAndExit` (round 3) still runs: no NaN, not silent, peak under 0.99.

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
