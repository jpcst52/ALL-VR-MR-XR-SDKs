STATUS: READY

# Kit audio and effects API (round 3)

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

- Several variations per name are generated and picked at random (never the same twice in a row), plus a small pitch jitter.
- One-shots share a pool of 24 AudioSources: no allocation per sound. Spatial rolloff is logarithmic, 1.2 m to 25 m. `Loop` takes its own min and max distance.
- `Loop` sources are created under your parent but kept out of the bake (DontBake). Keep the returned source if you want to stop it.
- Unknown names log one warning and play nothing. `Sfx.Names` lists the catalogue, `Sfx.IsLoop(name)` and `Sfx.IsSample(name)` describe a name.
- Call `Sfx.Preload()` once in `Start` if you want to avoid a small first-play hitch (it generates every clip, a fraction of a second).

### Catalogue (use these exact strings)

One-shots: `ui_confirm ui_error ui_select button switch valve_turn grab release snap clink_metal thud_box cone_place tape spray water_drip gas_hiss alarm_siren_rise alarm_siren_fall gas_monitor_beep radio_click forklift_horn hydraulic_whine pallet_drop mirror_snap led_on spark_pop smoke_puff esd_click meter_beep door_open door_close ding error_buzz gauge_tick footstep_concrete footstep_metal footstep_tile footstep_gravel footstep_wood`

Loops (use `Sfx.Loop`): `water_pour pump_hum alarm_siren monitor_alarm radio_static forklift_engine_loop forklift_beep_reverse wind_loop hvac_loop plant_drone_loop flare_roar_loop pressure_tone`

`pressure_tone` is a soft steady tone meant to be pitched with `src.pitch` (0.8 to 1.8) while pressure rises. `alarm_siren` is a wailing loop; `alarm_siren_rise` and `alarm_siren_fall` are one-shot sweeps. `forklift_beep_reverse` is one beep per second, loop it while reversing.

Typical levels: ambience beds 0.2 to 0.5, machines 0.4 to 0.7, interaction sounds 0.6 to 1, alarms 0.5 to 0.8 (already normalised so none clips).

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
