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
    /// a different one is chosen each time, with a little pitch jitter, so nothing machine-guns. One-shots play through a
    /// small pool of AudioSources, so many sounds do not allocate. Nothing plays while a scene bake is running.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Master switch. Also forced off automatically during a scene bake and outside play mode.</summary>
        public static bool Enabled = true;
        /// <summary>Master volume applied to everything (0..1).</summary>
        public static float Master = 1f;

        const int PoolSize = 24;

        sealed class Def
        {
            public Func<Rng, float[]> Gen;
            public int Rate = 32000, Variants = 3;
            public bool Loop;
            public float Peak = 0.7f;
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
            Reg("ui_confirm", Synth.UiConfirm);
            Reg("ui_error", Synth.UiError);
            Reg("ui_select", Synth.UiSelect);
            Reg("button", Synth.Button);
            Reg("switch", Synth.Switch);
            Reg("valve_turn", Synth.ValveTurn);
            Reg("grab", Synth.Grab);
            Reg("release", Synth.Release);
            Reg("snap", Synth.Snap);
            Reg("clink_metal", Synth.ClinkMetal);
            Reg("thud_box", Synth.ThudBox);
            Reg("cone_place", Synth.ConePlace);
            Reg("tape", Synth.Tape);
            Reg("spray", Synth.Spray, peak: 0.6f);
            Reg("water_drip", Synth.WaterDrip, peak: 0.6f);
            Reg("water_pour", Synth.WaterPour, loop: true, peak: 0.55f, variants: 2);
            Reg("pump_hum", Synth.PumpHum, loop: true, peak: 0.5f, variants: 1);
            Reg("gas_hiss", Synth.GasHiss, peak: 0.55f);
            Reg("alarm_siren", Synth.AlarmSiren, loop: true, peak: 0.5f, variants: 1);
            Reg("alarm_siren_rise", Synth.AlarmSirenRise, peak: 0.5f, variants: 1);
            Reg("alarm_siren_fall", Synth.AlarmSirenFall, peak: 0.5f, variants: 1);
            Reg("gas_monitor_beep", Synth.GasMonitorBeep, peak: 0.55f, variants: 2);
            Reg("monitor_alarm", Synth.MonitorAlarm, loop: true, peak: 0.5f, variants: 1);
            Reg("radio_click", Synth.RadioClick);
            Reg("radio_static", Synth.RadioStatic, loop: true, peak: 0.4f, variants: 1);
            Reg("forklift_horn", Synth.ForkliftHorn, peak: 0.6f, variants: 2);
            Reg("forklift_engine_loop", Synth.ForkliftEngineLoop, loop: true, peak: 0.55f, variants: 1);
            Reg("forklift_beep_reverse", Synth.ForkliftBeepReverse, loop: true, peak: 0.5f, variants: 1);
            Reg("hydraulic_whine", Synth.HydraulicWhine, peak: 0.5f, variants: 2);
            Reg("pallet_drop", Synth.PalletDrop, peak: 0.8f);
            Reg("mirror_snap", Synth.MirrorSnap);
            Reg("led_on", Synth.LedOn, peak: 0.5f);
            Reg("spark_pop", Synth.SparkPop, peak: 0.6f);
            Reg("smoke_puff", Synth.SmokePuff, peak: 0.45f);
            Reg("esd_click", Synth.EsdClick, peak: 0.6f);
            Reg("meter_beep", Synth.MeterBeep, peak: 0.5f, variants: 1);
            Reg("wind_loop", Synth.WindLoop, loop: true, rate: 16000, peak: 0.5f, variants: 1);
            Reg("hvac_loop", Synth.HvacLoop, loop: true, rate: 16000, peak: 0.4f, variants: 1);
            Reg("plant_drone_loop", Synth.PlantDroneLoop, loop: true, rate: 16000, peak: 0.45f, variants: 1);
            Reg("flare_roar_loop", Synth.FlareRoarLoop, loop: true, rate: 22050, peak: 0.55f, variants: 1);
            Reg("door_open", Synth.DoorOpen, peak: 0.6f, variants: 2);
            Reg("door_close", Synth.DoorClose, peak: 0.75f, variants: 2);
            Reg("ding", Synth.Ding, peak: 0.55f, variants: 2);
            Reg("error_buzz", Synth.ErrorBuzz, peak: 0.55f, variants: 2);
            Reg("pressure_tone", Synth.PressureTone, loop: true, peak: 0.35f, variants: 1);
            Reg("gauge_tick", Synth.GaugeTick, peak: 0.5f);
            foreach (Surface s in Enum.GetValues(typeof(Surface)))
            {
                var surface = s;
                Reg("footstep_" + s.ToString().ToLowerInvariant(), r => Synth.Footstep(r, surface), peak: 0.6f, variants: 4);
            }
        }

        static void Reg(string name, Func<Rng, float[]> gen, bool loop = false, int rate = 32000, float peak = 0.7f, int variants = 3)
        {
            Defs[name] = new Def { Gen = gen, Loop = loop, Rate = rate, Peak = peak, Variants = variants };
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
            Setup(src, clip, volume, pitchJitter, 1f);
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
            Setup(src, clip, volume, 0.02f, 0f);
            src.Play();
            return src;
        }

        /// <summary>
        /// A looping 3D source parented to <paramref name="parent"/> (ambience, machines, alarms). The caller keeps it: stop
        /// with <c>src.Stop()</c>, change <c>src.volume</c> or <c>src.pitch</c> at any time. The helper object is kept out of
        /// scene bakes. Created even while muted, so callers never see null; it simply does not play.
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
                src.clip = clip;
                src.volume = volume * Master;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = Mathf.Max(0.1f, minDist);
                src.maxDistance = Mathf.Max(minDist + 0.5f, maxDist);
                src.dopplerLevel = 0f;
                src.spread = 60f;
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
            if (names == null || names.Length == 0) foreach (var n in Defs.Keys) Pick(n, false);
            else foreach (var n in names) Pick(n, false);
        }

        // ------------------------------------------------------------------ internals

        static void Setup(AudioSource src, AudioClip clip, float volume, float jitter, float spatial)
        {
            src.loop = false;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * Master);
            src.pitch = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
            src.spatialBlend = spatial;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1.2f;
            src.maxDistance = 25f;
            src.dopplerLevel = 0f;
            src.spread = 30f;
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

        /// <summary>Normalises a recorded clip to the catalogue peak (works on clips decompressed on load).</summary>
        static void LevelSample(AudioClip c, float peak)
        {
            try
            {
                var d = new float[c.samples * c.channels];
                if (!c.GetData(d, 0)) return;
                float m = 0.0001f;
                for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
                float g = peak / m;
                for (int i = 0; i < d.Length; i++) d[i] *= g;
                c.SetData(d, 0);
            }
            catch (Exception) { /* compressed-in-memory clips cannot be edited; they play as imported */ }
        }

        static AudioClip[] Build(string name)
        {
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
                foreach (var c in found) LevelSample(c, Defs.TryGetValue(name, out var d0) ? d0.Peak : 0.7f);
                return found.ToArray();
            }
            Sampled[name] = false;

            // 2) synthesis
            if (!Defs.TryGetValue(name, out var def)) return null;
            var list = new AudioClip[def.Variants];
            int seedBase = 0;
            foreach (char ch in name) seedBase = seedBase * 31 + ch;
            for (int v = 0; v < def.Variants; v++)
            {
                Synth.R = def.Rate;
                var data = def.Gen(new Rng(seedBase + v * 7919));
                if (!def.Loop) Synth.Edges(data);
                Synth.Normalize(data, def.Peak);
                var clip = AudioClip.Create(name + "_" + v, data.Length, 1, def.Rate, false);
                clip.SetData(data, 0);
                list[v] = clip;
            }
            return list;
        }
    }
}
