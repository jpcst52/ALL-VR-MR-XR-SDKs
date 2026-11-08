using System;
using System.Collections.Generic;
using Rng = System.Random;

namespace PittBradford.XR
{
    public enum Surface { Concrete, Metal, Tile, Gravel, Wood }

    /// <summary>One catalogue entry: how to generate a sound and how loud and how bright it should end up.</summary>
    public sealed class SfxDef
    {
        public Func<Rng, float[]> Gen;
        public int Rate = 32000, Variants = 4, Priority = 128;
        public bool Loop;
        /// <summary>One-shots: cap on the active RMS in dBFS (the peak target below usually decides; this stops sustained sounds getting too loud).</summary>
        public float Level = -15f;
        /// <summary>Gentle low-pass in Hz applied to the finished clip.</summary>
        public float Lp = 9000f;
        /// <summary>How much of the scene reverb zone this sound gets (0..1).</summary>
        public float Mix = 1f;
        /// <summary>Loops only: loudness of the file in LUFS-equivalent (NaN = not a loop).</summary>
        public float Lufs = float.NaN;
        /// <summary>Peak ceiling, linear (0.5 = -6 dBFS). One-shots are scaled so the peak lands here (or lower, per name).</summary>
        public float Ceiling = 0.5f;
        /// <summary>Linear gain applied at play time (AudioSource volume): a loop file is normalised to <see cref="Lufs"/> and then sits at its in-scene level through this.</summary>
        public float Gain = 1f;
        /// <summary>Loops only: the in-scene loudness this entry is meant to have at a volume argument of 1.</summary>
        public float SceneLufs = float.NaN;
        /// <summary>When a recording exists for the name, use it together with the synthesized variations (one pool) instead of them.</summary>
        public bool Pool;
    }

    /// <summary>
    /// The sound table, free of any Unity engine type so the offline renderer (tools/audio-render) can use it.
    /// Round 5 levels: a one-shot is scaled so its PEAK is the given dBFS (default -6; quieter kinds are lower), with an RMS cap for sustained
    /// sounds; a loop is normalised to a LUFS-equivalent file level (beds -18, machines -16, alarms -14) and a play gain brings it down to its
    /// in-scene level, so the call-site <c>volume</c> arguments keep their meaning.
    /// </summary>
    public static class SfxCatalog
    {
        public static readonly Dictionary<string, SfxDef> Defs = new Dictionary<string, SfxDef>();

        static SfxCatalog()
        {
            // interface family (one key, FM bells)
            One("ui_confirm", Synth.UiConfirm, peak: -8f, prio: 60, mix: 0.3f);
            One("ui_error", Synth.UiError, peak: -8f, prio: 60, mix: 0.3f);
            One("ui_select", Synth.UiSelect, peak: -11f, prio: 60, mix: 0.2f);
            // hands and small objects
            One("button", Synth.Button, peak: -7f, lp: 6500f, prio: 100, variants: 5);
            One("switch", Synth.Switch, peak: -7f, lp: 6500f, prio: 100, variants: 5);
            One("valve_turn", Synth.ValveTurn, peak: -7f, lp: 8000f, prio: 100, variants: 5);
            One("grab", Synth.Grab, peak: -10f, lp: 7000f, prio: 100, variants: 5);
            One("release", Synth.Release, peak: -11f, lp: 7000f, prio: 100, variants: 5);
            One("snap", Synth.Snap, peak: -6f, lp: 6000f, prio: 100, variants: 5);
            One("clink_metal", Synth.ClinkMetal, peak: -7f, lp: 5000f, prio: 100, variants: 5);
            One("thud_box", Synth.ThudBox, peak: -6f, prio: 100, variants: 5);
            One("cone_place", Synth.ConePlace, peak: -8f, prio: 100, variants: 5);
            One("tape", Synth.Tape, peak: -12f, lp: 6500f, prio: 110);
            One("spray", Synth.Spray, peak: -13f, lp: 7500f, prio: 110);
            One("water_drip", Synth.WaterDrip, peak: -10f, prio: 120, variants: 6, pool: true);
            Loop("water_pour", Synth.WaterPour, lufs: -16f, scene: -22f, rate: 22050, lp: 6500f, prio: 150, variants: 2, mix: 0.6f);
            Loop("pump_hum", Synth.PumpHum, lufs: -16f, scene: -25f, rate: 22050, lp: 3000f, prio: 150, variants: 1, mix: 0.6f);
            One("gas_hiss", Synth.GasHiss, peak: -14f, lp: 6500f, prio: 110);
            // alarms: the loudest things in a scene, but soft attacks and a low top end
            Loop("alarm_siren", Synth.AlarmSiren, lufs: -14f, scene: -17f, rate: 22050, lp: 3200f, prio: 40, variants: 1);
            One("alarm_siren_rise", Synth.AlarmSirenRise, level: -14f, peak: -6f, lp: 3500f, prio: 40, variants: 1);
            One("alarm_siren_fall", Synth.AlarmSirenFall, level: -14f, peak: -6f, lp: 3500f, prio: 40, variants: 1);
            One("gas_monitor_beep", Synth.GasMonitorBeep, level: -17f, peak: -9f, lp: 4500f, prio: 50, variants: 3);
            Loop("monitor_alarm", Synth.MonitorAlarm, lufs: -16f, scene: -21f, rate: 22050, lp: 3800f, prio: 50, variants: 1);
            One("radio_click", Synth.RadioClick, peak: -10f, lp: 5500f, prio: 110);
            Loop("radio_static", Synth.RadioStatic, lufs: -18f, scene: -27f, rate: 22050, lp: 5000f, prio: 190, variants: 1);
            // vehicles
            One("forklift_horn", Synth.ForkliftHorn, level: -15f, peak: -6f, lp: 3000f, prio: 60, variants: 3);
            Loop("forklift_engine_loop", Synth.ForkliftEngineLoop, lufs: -16f, scene: -25f, rate: 22050, lp: 1800f, prio: 140, variants: 1, mix: 0.6f);
            Loop("forklift_beep_reverse", Synth.ForkliftBeepReverse, lufs: -16f, scene: -21f, rate: 22050, lp: 3500f, prio: 50, variants: 1);
            One("hydraulic_whine", Synth.HydraulicWhine, peak: -13f, lp: 3500f, prio: 130, variants: 2);
            One("pallet_drop", Synth.PalletDrop, peak: -6f, prio: 100, variants: 5);
            One("mirror_snap", Synth.MirrorSnap, peak: -10f, lp: 8000f, prio: 100);
            One("led_on", Synth.LedOn, peak: -12f, lp: 7000f, prio: 110);
            One("spark_pop", Synth.SparkPop, peak: -8f, lp: 6500f, prio: 100, variants: 5);
            One("zap", Synth.Zap, peak: -6f, lp: 6500f, prio: 90, variants: 4);
            One("smoke_puff", Synth.SmokePuff, peak: -15f, lp: 3500f, prio: 130);
            One("esd_click", Synth.EsdClick, peak: -10f, lp: 6000f, prio: 110, variants: 5);
            One("meter_beep", Synth.MeterBeep, level: -17f, peak: -10f, lp: 4000f, prio: 70, variants: 2);
            // ambience and machine loops
            Loop("wind_loop", Synth.WindLoop, lufs: -18f, scene: -32f, rate: 22050, lp: 3500f, prio: 200, variants: 1, mix: 0.2f);
            Loop("hvac_loop", Synth.HvacLoop, lufs: -18f, scene: -33f, rate: 22050, lp: 3500f, prio: 200, variants: 1, mix: 0.4f);
            Loop("plant_drone_loop", Synth.PlantDroneLoop, lufs: -18f, scene: -31f, rate: 22050, lp: 3000f, prio: 200, variants: 1, mix: 0.4f);
            Loop("flare_roar_loop", Synth.FlareRoarLoop, lufs: -18f, scene: -30f, rate: 22050, lp: 3500f, prio: 190, variants: 1, mix: 0.3f);
            Loop("electric_buzz", Synth.ElectricBuzz, lufs: -16f, scene: -28f, rate: 22050, lp: 3500f, prio: 160, variants: 1, mix: 0.5f);
            One("door_open", Synth.DoorOpen, peak: -8f, lp: 6500f, prio: 100, variants: 3);
            One("door_close", Synth.DoorClose, peak: -7f, lp: 6500f, prio: 100, variants: 3);
            One("ding", Synth.Ding, peak: -9f, prio: 60, variants: 3, mix: 0.5f);
            One("error_buzz", Synth.ErrorBuzz, peak: -8f, lp: 3000f, prio: 50, variants: 2, mix: 0.3f);
            Loop("pressure_tone", Synth.PressureTone, lufs: -16f, scene: -26f, rate: 22050, lp: 3000f, prio: 160, variants: 1, mix: 0.3f);
            One("gauge_tick", Synth.GaugeTick, peak: -12f, lp: 7000f, prio: 120, variants: 5);
            // scene beds (SceneAudio.Begin plays these) and the distant one-shots its director scatters
            Loop("amb_lobby", Synth.AmbLobby, lufs: -18f, scene: -29f, rate: 22050, lp: 3500f, prio: 230, variants: 1, mix: 0.3f);
            Loop("amb_hydro", Synth.AmbHydro, lufs: -18f, scene: -29f, rate: 22050, lp: 3500f, prio: 230, variants: 1, mix: 0.4f);
            Loop("amb_refinery", Synth.AmbRefinery, lufs: -18f, scene: -29f, rate: 22050, lp: 3500f, prio: 230, variants: 1, mix: 0.2f);
            Loop("amb_warehouse", Synth.AmbWarehouse, lufs: -18f, scene: -29f, rate: 22050, lp: 3500f, prio: 230, variants: 1, mix: 0.4f);
            Loop("amb_bench", Synth.AmbBench, lufs: -18f, scene: -30f, rate: 22050, lp: 3500f, prio: 230, variants: 1, mix: 0.3f);
            // the director plays these 3 to 20 m away, so they arrive 10 to 26 dB lower than at 1 m
            One("far_clank", Synth.FarClank, peak: -9f, lp: 3200f, prio: 180, variants: 5);
            One("far_creak", Synth.FarCreak, peak: -12f, lp: 2800f, prio: 180, variants: 4);
            One("far_steam", Synth.FarSteam, peak: -14f, lp: 4500f, prio: 180, variants: 3);
            One("far_relay_tick", Synth.FarRelayTick, peak: -12f, lp: 4500f, prio: 180, variants: 4);
            One("far_thud", Synth.FarThud, peak: -9f, lp: 1800f, prio: 180, variants: 4);
            One("far_beep", Synth.FarBeep, level: -17f, peak: -12f, lp: 2400f, prio: 180, variants: 3);
            foreach (Surface s in Enum.GetValues(typeof(Surface)))
            {
                var surface = s;
                float lp = s == Surface.Metal ? 5500f : (s == Surface.Gravel || s == Surface.Tile ? 6000f : 6500f);
                One("footstep_" + s.ToString().ToLowerInvariant(), r => Synth.Footstep(r, surface), peak: -10f, lp: lp, prio: 130, variants: 8, mix: 0.8f);
            }
        }

        static void One(string name, Func<Rng, float[]> gen, float level = -15f, float peak = -6f, int rate = 32000, float lp = 9000f,
                        int variants = 4, int prio = 128, float mix = 1f, bool pool = false)
        {
            Defs[name] = new SfxDef { Pool = pool, Gen = gen, Loop = false, Rate = rate, Level = level, Ceiling = (float)Math.Pow(10.0, peak / 20.0), Lp = lp, Variants = variants, Priority = prio, Mix = mix };
        }

        static void Loop(string name, Func<Rng, float[]> gen, float lufs, float scene, int rate = 22050, float lp = 9000f, int variants = 1, int prio = 150, float mix = 1f)
        {
            Defs[name] = new SfxDef
            {
                Gen = gen, Loop = true, Rate = rate, Lufs = lufs, SceneLufs = scene, Gain = (float)Math.Pow(10.0, (scene - lufs) / 20.0),
                Ceiling = 0.5f, Level = -15f, Lp = lp, Variants = variants, Priority = prio, Mix = mix
            };
        }
    }
}
