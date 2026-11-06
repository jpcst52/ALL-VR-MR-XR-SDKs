using System;
using System.Collections.Generic;
using UnityEngine;
using Rng = System.Random;

namespace PittBradford.XR
{
    public enum Surface { Concrete, Metal, Tile, Gravel, Wood }

    /// <summary>
    /// The kit's sound library. Call by name: <c>Sfx.Play("valve_turn", pos)</c>. Each name has several variations (a
    /// recorded CC0 sample from Resources/Audio when one exists, otherwise layered synthesis from <see cref="Synth"/>) and
    /// a different one is chosen each time, with a little pitch and volume jitter, so nothing machine-guns. One-shots play
    /// through a small pool of AudioSources, so many sounds do not allocate. Nothing plays while a scene bake is running.
    /// Every clip is levelled to a loudness target for its kind (ambience below machines below interaction below alarms),
    /// soft-limited under 0.85 and faded at the ends. See <see cref="SceneAudio"/> for per-scene reverb and ambience.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Master switch. Also forced off automatically during a scene bake and outside play mode.</summary>
        public static bool Enabled = true;
        /// <summary>Master volume applied to everything (0..1).</summary>
        public static float Master = 1f;

        const int PoolSize = 24;
        /// <summary>Overall level trim in dB added to every target below (they are relative to each other; this sets how loud the set is).</summary>
        const float Trim = 6f;

        sealed class Def
        {
            public Func<Rng, float[]> Gen;
            public int Rate = 32000, Variants = 4, Priority = 128;
            public bool Loop;
            /// <summary>Target loudness: RMS of the active part in dBFS. Ambience about -34, machines -27 to -33, interaction -21 to -26, alarms -20 to -23.</summary>
            public float Level = -23f;
            /// <summary>Gentle low-pass in Hz applied to the finished clip (keeps the top end soft on a headset).</summary>
            public float Lp = 9000f;
            /// <summary>How much of the scene reverb zone this sound gets (0..1).</summary>
            public float Mix = 1f;
        }

        static readonly Dictionary<string, Def> Defs = new Dictionary<string, Def>();
        static readonly Dictionary<string, AudioClip[]> Clips = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, bool> Sampled = new Dictionary<string, bool>();
        static readonly Dictionary<string, int> Last = new Dictionary<string, int>();
        static readonly HashSet<string> Warned = new HashSet<string>();
        static AudioSource[] pool;
        static int next;
        static Rng rng = new Rng(7);

        static Sfx()
        {
            // interface family (one key, soft bells)
            Reg("ui_confirm", Synth.UiConfirm, level: -23f, prio: 60, mix: 0.3f);
            Reg("ui_error", Synth.UiError, level: -23f, prio: 60, mix: 0.3f);
            Reg("ui_select", Synth.UiSelect, level: -25f, prio: 60, mix: 0.2f);
            // hands and small objects
            Reg("button", Synth.Button, level: -22f, prio: 100);
            Reg("switch", Synth.Switch, level: -22f, prio: 100);
            Reg("valve_turn", Synth.ValveTurn, level: -23f, lp: 8000f, prio: 100, variants: 5);
            Reg("grab", Synth.Grab, level: -25f, lp: 7000f, prio: 100);
            Reg("release", Synth.Release, level: -26f, lp: 7000f, prio: 100);
            Reg("snap", Synth.Snap, level: -21f, prio: 100, variants: 5);
            Reg("clink_metal", Synth.ClinkMetal, level: -22f, lp: 5500f, prio: 100, variants: 5);
            Reg("thud_box", Synth.ThudBox, level: -19f, prio: 100);
            Reg("cone_place", Synth.ConePlace, level: -22f, prio: 100);
            Reg("tape", Synth.Tape, level: -26f, lp: 6500f, prio: 110);
            Reg("spray", Synth.Spray, level: -28f, lp: 7500f, prio: 110);
            Reg("water_drip", Synth.WaterDrip, level: -24f, prio: 120, variants: 5);
            Reg("water_pour", Synth.WaterPour, loop: true, rate: 22050, level: -29f, lp: 6500f, prio: 150, variants: 2, mix: 0.6f);
            Reg("pump_hum", Synth.PumpHum, loop: true, rate: 22050, level: -28f, lp: 3000f, prio: 150, variants: 1, mix: 0.6f);
            Reg("gas_hiss", Synth.GasHiss, level: -30f, lp: 6500f, prio: 110);
            // alarms: highest level, but soft attack and a low top end
            Reg("alarm_siren", Synth.AlarmSiren, loop: true, rate: 22050, level: -20f, lp: 3200f, prio: 40, variants: 1);
            Reg("alarm_siren_rise", Synth.AlarmSirenRise, level: -21f, lp: 3500f, prio: 40, variants: 1);
            Reg("alarm_siren_fall", Synth.AlarmSirenFall, level: -21f, lp: 3500f, prio: 40, variants: 1);
            Reg("gas_monitor_beep", Synth.GasMonitorBeep, level: -24f, lp: 4500f, prio: 50, variants: 2);
            Reg("monitor_alarm", Synth.MonitorAlarm, loop: true, rate: 22050, level: -23f, lp: 3800f, prio: 50, variants: 1);
            Reg("radio_click", Synth.RadioClick, level: -25f, lp: 5500f, prio: 110);
            Reg("radio_static", Synth.RadioStatic, loop: true, rate: 22050, level: -34f, lp: 5000f, prio: 190, variants: 1);
            // vehicles
            Reg("forklift_horn", Synth.ForkliftHorn, level: -21f, lp: 3000f, prio: 60, variants: 2);
            Reg("forklift_engine_loop", Synth.ForkliftEngineLoop, loop: true, rate: 22050, level: -27f, lp: 1800f, prio: 140, variants: 1, mix: 0.6f);
            Reg("forklift_beep_reverse", Synth.ForkliftBeepReverse, loop: true, rate: 22050, level: -24f, lp: 3500f, prio: 50, variants: 1);
            Reg("hydraulic_whine", Synth.HydraulicWhine, level: -28f, lp: 3500f, prio: 130, variants: 2);
            Reg("pallet_drop", Synth.PalletDrop, level: -20f, prio: 100, variants: 5);
            Reg("mirror_snap", Synth.MirrorSnap, level: -25f, lp: 8000f, prio: 100);
            Reg("led_on", Synth.LedOn, level: -27f, lp: 7000f, prio: 110);
            Reg("spark_pop", Synth.SparkPop, level: -26f, lp: 6500f, prio: 100);
            Reg("smoke_puff", Synth.SmokePuff, level: -30f, lp: 3500f, prio: 130);
            Reg("esd_click", Synth.EsdClick, level: -27f, lp: 6000f, prio: 110);
            Reg("meter_beep", Synth.MeterBeep, level: -26f, lp: 4000f, prio: 70, variants: 1);
            // ambience and machine loops
            Reg("wind_loop", Synth.WindLoop, loop: true, rate: 22050, level: -34f, lp: 3500f, prio: 200, variants: 1, mix: 0.2f);
            Reg("hvac_loop", Synth.HvacLoop, loop: true, rate: 22050, level: -33f, lp: 3500f, prio: 200, variants: 1, mix: 0.4f);
            Reg("plant_drone_loop", Synth.PlantDroneLoop, loop: true, rate: 22050, level: -32f, lp: 3000f, prio: 200, variants: 1, mix: 0.4f);
            Reg("flare_roar_loop", Synth.FlareRoarLoop, loop: true, rate: 22050, level: -30f, lp: 3500f, prio: 190, variants: 1, mix: 0.3f);
            Reg("door_open", Synth.DoorOpen, level: -23f, prio: 100, variants: 3);
            Reg("door_close", Synth.DoorClose, level: -21f, prio: 100, variants: 3);
            Reg("ding", Synth.Ding, level: -24f, prio: 60, variants: 2, mix: 0.5f);
            Reg("error_buzz", Synth.ErrorBuzz, level: -23f, lp: 3000f, prio: 50, variants: 2, mix: 0.3f);
            Reg("pressure_tone", Synth.PressureTone, loop: true, rate: 22050, level: -31f, lp: 3000f, prio: 160, variants: 1, mix: 0.3f);
            Reg("gauge_tick", Synth.GaugeTick, level: -27f, lp: 7000f, prio: 120);
            // scene beds (SceneAudio.Begin plays these) and the distant one-shots its director scatters
            Reg("amb_lobby", Synth.AmbLobby, loop: true, rate: 22050, level: -36f, lp: 3000f, prio: 230, variants: 1, mix: 0.3f);
            Reg("amb_hydro", Synth.AmbHydro, loop: true, rate: 22050, level: -35f, lp: 3200f, prio: 230, variants: 1, mix: 0.4f);
            Reg("amb_refinery", Synth.AmbRefinery, loop: true, rate: 22050, level: -35f, lp: 3200f, prio: 230, variants: 1, mix: 0.2f);
            Reg("amb_warehouse", Synth.AmbWarehouse, loop: true, rate: 22050, level: -36f, lp: 3000f, prio: 230, variants: 1, mix: 0.4f);
            Reg("amb_bench", Synth.AmbBench, loop: true, rate: 22050, level: -37f, lp: 3000f, prio: 230, variants: 1, mix: 0.3f);
            // levels are "at 1 m"; the director plays them 3 to 20 m away, so they arrive 10 to 26 dB lower
            Reg("far_clank", Synth.FarClank, level: -18f, lp: 3200f, prio: 180, variants: 4);
            Reg("far_creak", Synth.FarCreak, level: -22f, lp: 2800f, prio: 180, variants: 3);
            Reg("far_steam", Synth.FarSteam, level: -24f, lp: 4500f, prio: 180, variants: 2);
            Reg("far_relay_tick", Synth.FarRelayTick, level: -22f, lp: 4500f, prio: 180, variants: 3);
            Reg("far_thud", Synth.FarThud, level: -16f, lp: 1500f, prio: 180, variants: 3);
            Reg("far_beep", Synth.FarBeep, level: -22f, lp: 2400f, prio: 180, variants: 2);
            foreach (Surface s in Enum.GetValues(typeof(Surface)))
            {
                var surface = s;
                Reg("footstep_" + s.ToString().ToLowerInvariant(), r => Synth.Footstep(r, surface), level: -26f, lp: 7500f, prio: 130, variants: 6, mix: 0.8f);
            }
        }

        static void Reg(string name, Func<Rng, float[]> gen, bool loop = false, int rate = 32000, float level = -23f, float lp = 9000f,
                        int variants = 4, int prio = 128, float mix = 1f)
        {
            Defs[name] = new Def { Gen = gen, Loop = loop, Rate = rate, Level = level + Trim, Lp = lp, Variants = variants, Priority = prio, Mix = mix };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pool = null; next = 0; Enabled = true; Master = 1f;
            Clips.Clear(); Sampled.Clear(); Last.Clear(); Warned.Clear();
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Every catalogue name.</summary>
        public static IEnumerable<string> Names => Defs.Keys;

        /// <summary>True when the name is a seamless loop (use <see cref="Loop"/>).</summary>
        public static bool IsLoop(string name) => Defs.TryGetValue(name, out var d) && d.Loop;

        /// <summary>True when the name uses a recorded sample from Resources/Audio instead of synthesis.</summary>
        public static bool IsSample(string name)
        {
            Pick(name, false);
            return Sampled.TryGetValue(name, out var s) && s;
        }

        /// <summary>True when sounds may play right now (enabled, in play mode, and no bake running).</summary>
        public static bool Active
        {
            get
            {
                if (!Enabled || !Application.isPlaying) return false;
#if UNITY_EDITOR
                if (UnityEditor.SessionState.GetString(Bake.StageKey, "") != "") return false;
#endif
                return true;
            }
        }

        /// <summary>One-shot 3D sound at a world position. Returns the source (or null when muted or unknown).</summary>
        public static AudioSource Play(string name, Vector3 position, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (!Active) return null;
            var clip = Pick(name, true);
            if (clip == null) return null;
            var src = Acquire();
            src.transform.position = position;
            Setup(src, clip, name, volume, pitchJitter, 1f);
            src.Play();
            return src;
        }

        /// <summary>One-shot non-spatial sound (UI, confirmation chimes, alarms that should be heard everywhere).</summary>
        public static AudioSource Play2D(string name, float volume = 1f)
        {
            if (!Active) return null;
            var clip = Pick(name, true);
            if (clip == null) return null;
            var src = Acquire();
            Setup(src, clip, name, volume, 0.02f, 0f);
            src.Play();
            return src;
        }

        /// <summary>
        /// A looping 3D source parented to <paramref name="parent"/> (ambience, machines, alarms). The caller keeps it: stop
        /// with <c>src.Stop()</c>, change <c>src.volume</c> or <c>src.pitch</c> at any time. The helper object is kept out of
        /// scene bakes. Created even while muted, so callers never see null; it simply does not play. It darkens with distance
        /// (a low-pass that follows the listener, see <see cref="SfxFar"/>).
        /// </summary>
        public static AudioSource Loop(string name, Transform parent, float volume = 1f, float minDist = 1.5f, float maxDist = 20f)
        {
            var go = new GameObject("sfx loop " + name);
            go.AddComponent<DontBake>();
            if (parent != null) go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            var clip = Active ? Pick(name, true) : null;
            if (clip != null)
            {
                Defs.TryGetValue(name, out var d);
                src.clip = clip;
                src.volume = volume * Master;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = Mathf.Max(0.1f, minDist);
                src.maxDistance = Mathf.Max(minDist + 0.5f, maxDist);
                src.dopplerLevel = 0f;
                src.spread = 60f;
                src.priority = d != null ? d.Priority : 150;
                src.reverbZoneMix = d != null ? d.Mix : 0.5f;
                var lp = go.AddComponent<AudioLowPassFilter>();
                lp.cutoffFrequency = 18000f;
                var far = go.AddComponent<SfxFar>();
                far.Near = src.minDistance; far.Far = src.maxDistance;
                src.time = (float)(rng.NextDouble() * clip.length);
                src.Play();
            }
            return src;
        }

        /// <summary>A footstep for the floor surface the player (or a person) is standing on.</summary>
        public static AudioSource Footstep(Vector3 position, Surface surface, float volume = 0.6f) =>
            Play("footstep_" + surface.ToString().ToLowerInvariant(), position, volume, 0.08f);

        /// <summary>Generates the clips now (avoids a small hitch the first time a sound plays).</summary>
        public static void Preload(params string[] names)
        {
            if (names == null || names.Length == 0) foreach (var n in new List<string>(Defs.Keys)) Pick(n, false);
            else foreach (var n in names) Pick(n, false);
        }

        // ------------------------------------------------------------------ internals

        static void Setup(AudioSource src, AudioClip clip, string name, float volume, float jitter, float spatial)
        {
            Defs.TryGetValue(name, out var d);
            src.loop = false;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * Master * (1f + ((float)rng.NextDouble() * 2f - 1f) * 0.08f));
            src.pitch = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
            src.spatialBlend = spatial;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1.2f;
            src.maxDistance = 25f;
            src.dopplerLevel = 0f;
            src.spread = 20f;
            src.panStereo = 0f;
            src.priority = d != null ? d.Priority : 128;
            src.reverbZoneMix = spatial > 0f ? (d != null ? d.Mix : 1f) : 0f;
        }

        static AudioSource Acquire()
        {
            if (pool == null || pool[0] == null)
            {
                var root = new GameObject("Sfx pool");
                root.AddComponent<DontBake>();
                UnityEngine.Object.DontDestroyOnLoad(root);
                pool = new AudioSource[PoolSize];
                for (int i = 0; i < PoolSize; i++)
                {
                    var g = new GameObject("voice " + i);
                    g.transform.SetParent(root.transform, false);
                    pool[i] = g.AddComponent<AudioSource>();
                    pool[i].playOnAwake = false;
                }
            }
            for (int i = 0; i < PoolSize; i++)
            {
                next = (next + 1) % PoolSize;
                if (!pool[next].isPlaying) return pool[next];
            }
            next = (next + 1) % PoolSize; // all busy: steal the next one
            pool[next].Stop();
            return pool[next];
        }

        static AudioClip Pick(string name, bool warn)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!Clips.TryGetValue(name, out var set))
            {
                set = Build(name);
                Clips[name] = set;
            }
            if (set == null || set.Length == 0)
            {
                if (warn && Warned.Add(name)) Debug.LogWarning("[Sfx] unknown sound name: " + name);
                return null;
            }
            int i = rng.Next(set.Length);
            if (set.Length > 1 && Last.TryGetValue(name, out int prev) && i == prev) i = (i + 1 + rng.Next(set.Length - 1)) % set.Length;
            Last[name] = i;
            return set[i];
        }

        /// <summary>Polishes a recorded clip like a synthesized one (high-pass, gentle low-pass, fades, loudness target, limiter). Works on clips decompressed on load.</summary>
        static void LevelSample(AudioClip c, Def def)
        {
            try
            {
                var d = new float[c.samples * c.channels];
                if (!c.GetData(d, 0)) return;
                if (def == null) def = new Def();
                if (c.channels == 1)
                {
                    Synth.R = c.frequency;
                    if (def.Lp < 7000f && !def.Loop) Synth.Filter(d, Synth.F.Low, def.Lp, 0.6f); // steeper (24 dB/oct) for bright recordings
                    Synth.Finish(d, def.Loop, def.Lp, def.Level);
                }
                else Synth.Level(d, def.Level, 0.85f);
                c.SetData(d, 0);
            }
            catch (Exception) { /* compressed-in-memory clips cannot be edited; they play as imported */ }
        }

        static AudioClip[] Build(string name)
        {
            Defs.TryGetValue(name, out var def);
            // 1) recorded samples: Resources/Audio/<name>.wav|.ogg, then <name>_1, <name>_2 ...
            var found = new List<AudioClip>();
            var one = Resources.Load<AudioClip>("Audio/" + name);
            if (one != null) found.Add(one);
            for (int i = 1; i <= 8; i++)
            {
                var c = Resources.Load<AudioClip>("Audio/" + name + "_" + i);
                if (c != null) found.Add(c);
            }
            if (found.Count > 0)
            {
                Sampled[name] = true;
                foreach (var c in found) LevelSample(c, def);
                return found.ToArray();
            }
            Sampled[name] = false;

            // 2) synthesis
            if (def == null) return null;
            var list = new AudioClip[def.Variants];
            int seedBase = 0;
            foreach (char ch in name) seedBase = seedBase * 31 + ch;
            for (int v = 0; v < def.Variants; v++)
            {
                Synth.R = def.Rate;
                var data = def.Gen(new Rng(seedBase + v * 7919));
                Synth.Finish(data, def.Loop, def.Lp, def.Level);
                var clip = AudioClip.Create(name + "_" + v, data.Length, 1, def.Rate, false);
                clip.SetData(data, 0);
                list[v] = clip;
            }
            return list;
        }
    }
}
