using System;
using System.Collections.Generic;
using UnityEngine;
using Rng = System.Random;

namespace PittBradford.XR
{
    /// <summary>
    /// The kit's sound library. Call by name: <c>Sfx.Play("valve_turn", pos)</c>. Each name has several variations (a
    /// recorded CC0 sample from Resources/Audio when one exists, otherwise layered synthesis from <see cref="Synth"/>) and
    /// a different one is chosen each time, with a little pitch and volume jitter, so nothing machine-guns. One-shots play
    /// through a small pool of AudioSources, so many sounds do not allocate. Nothing plays while a scene bake is running.
    /// Levels (round 5): one-shots peak at about -6 dBFS (quieter kinds lower), loops are normalised to a LUFS-equivalent file level and
    /// brought to their in-scene level by a play gain (see <see cref="SfxDef.Gain"/>). Spatial sounds use a custom rolloff that fades to
    /// silence at the maximum distance, and far one-shots are darkened by distance. See <see cref="SceneAudio"/> for per-scene reverb and ambience.
    /// </summary>
    public static class Sfx
    {
        /// <summary>Master switch. Also forced off automatically during a scene bake and outside play mode.</summary>
        public static bool Enabled = true;
        /// <summary>Master volume applied to everything (0..1).</summary>
        public static float Master = 1f;

        const int PoolSize = 24;

        static readonly Dictionary<string, SfxDef> Defs = SfxCatalog.Defs;
        static readonly Dictionary<string, AudioClip[]> Clips = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, bool> Sampled = new Dictionary<string, bool>();
        static readonly Dictionary<string, int> Last = new Dictionary<string, int>();
        static readonly HashSet<string> Warned = new HashSet<string>();
        static readonly Dictionary<string, int[]> Order = new Dictionary<string, int[]>();
        static readonly Dictionary<string, int> OrderPos = new Dictionary<string, int>();
        static readonly Dictionary<long, AnimationCurve> Curves = new Dictionary<long, AnimationCurve>();
        // background generation (see PreloadBackground)
        sealed class Ready { public string Name; public int V, Rate; public float[] Data; }
        static readonly System.Collections.Concurrent.ConcurrentQueue<Ready> readyQ = new System.Collections.Concurrent.ConcurrentQueue<Ready>();
        static readonly Dictionary<string, AudioClip[]> Partial = new Dictionary<string, AudioClip[]>();
        static int bgGen, bgPending;
        static bool bgStarted;
        static AudioSource[] pool;
        static AudioLowPassFilter[] poolLp;
        static AudioListener listener;
        static float nextFind;
        static int next;
        static Rng rng = new Rng(7);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pool = null; poolLp = null; listener = null; next = 0; Enabled = true; Master = 1f;
            Clips.Clear(); Sampled.Clear(); Last.Clear(); Warned.Clear(); Order.Clear(); OrderPos.Clear(); Curves.Clear(); bgGen++; bgStarted = false; Partial.Clear(); while (readyQ.TryDequeue(out _)) { }
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
            PreloadBackground();
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
            PreloadBackground();
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
            if (Active) PreloadBackground();
            var clip = Active ? Pick(name, true) : null;
            if (clip != null)
            {
                Defs.TryGetValue(name, out var d);
                src.clip = clip;
                src.volume = Mathf.Clamp01(volume * Master * (d != null ? d.Gain : 1f));
                src.spatialBlend = 1f;
                src.minDistance = Mathf.Max(0.1f, minDist);
                src.maxDistance = Mathf.Max(minDist + 0.5f, maxDist);
                ApplyRolloff(src);
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

        /// <summary>
        /// Generates every synthesized sound on a background thread and turns the results into clips a few per frame (see <see cref="SfxPump"/>),
        /// so nothing has to be built in the middle of play. Starts by itself at the first <see cref="Play"/> or <see cref="Loop"/> and from
        /// <see cref="SceneAudioBoot"/>; safe to call again. A sound asked for before its turn is built at once on the calling thread.
        /// Does nothing while muted, outside play mode or during a bake.
        /// </summary>
        public static void PreloadBackground()
        {
            if (bgStarted || !Active) return;
            bgStarted = true;
            int gen = bgGen;
            var jobs = new List<KeyValuePair<int, string>>();
            foreach (var kv in Defs)
            {
                if (Clips.ContainsKey(kv.Key)) continue;
                if (Resources.Load<AudioClip>("Audio/" + kv.Key) != null || Resources.Load<AudioClip>("Audio/" + kv.Key + "_1") != null) continue;   // recordings load synchronously
                jobs.Add(new KeyValuePair<int, string>(kv.Value.Priority, kv.Key));
            }
            jobs.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : string.CompareOrdinal(a.Value, b.Value));
            int total = 0; foreach (var j in jobs) total += Defs[j.Value].Variants;
            bgPending = total;
            if (total == 0) return;
            var go = new GameObject("Sfx loader");
            go.AddComponent<DontBake>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SfxPump>();
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    foreach (var j in jobs)
                    {
                        var def = Defs[j.Value];
                        for (int v = 0; v < def.Variants; v++)
                        {
                            if (gen != bgGen) return;
                            readyQ.Enqueue(new Ready { Name = j.Value, V = v, Rate = def.Rate, Data = Make(j.Value, def, v) });
                        }
                    }
                }
                catch (Exception) { /* the main thread builds anything that is missing when it is first asked for */ }
            });
        }

        /// <summary>Main-thread half of <see cref="PreloadBackground"/>: builds up to <paramref name="max"/> clips. Returns false when there is nothing left to do.</summary>
        internal static bool PumpReady(int max)
        {
            for (int n = 0; n < max && readyQ.TryDequeue(out var r); n++)
            {
                bgPending--;
                if (Clips.ContainsKey(r.Name)) continue;   // already built synchronously
                var def = Defs[r.Name];
                if (!Partial.TryGetValue(r.Name, out var set)) { set = new AudioClip[def.Variants]; Partial[r.Name] = set; }
                var clip = AudioClip.Create(r.Name + "_" + r.V, r.Data.Length, 1, r.Rate, false);
                clip.SetData(r.Data, 0);
                set[r.V] = clip;
                bool all = true; foreach (var c in set) if (c == null) { all = false; break; }
                if (all) { Clips[r.Name] = set; Sampled[r.Name] = false; Partial.Remove(r.Name); }
            }
            return bgPending > 0 || !readyQ.IsEmpty;
        }

        // ------------------------------------------------------------------ internals

        static void Setup(AudioSource src, AudioClip clip, string name, float volume, float jitter, float spatial)
        {
            Defs.TryGetValue(name, out var d);
            src.loop = false;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * Master * (d != null ? d.Gain : 1f) * (1f + ((float)rng.NextDouble() * 2f - 1f) * 0.08f));
            src.pitch = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
            src.spatialBlend = spatial;
            src.minDistance = 1.2f;
            src.maxDistance = 25f;
            ApplyRolloff(src);
            src.dopplerLevel = 0f;
            src.spread = 20f;
            src.panStereo = 0f;
            src.priority = d != null ? d.Priority : 128;
            src.reverbZoneMix = spatial > 0f ? (d != null ? d.Mix : 1f) : 0f;
            DarkenByDistance(src, spatial > 0f);
        }

        /// <summary>Inverse-distance rolloff (like the built-in logarithmic one) that fades to silence over the last third of the range, so sounds never cut off.</summary>
        static void ApplyRolloff(AudioSource src)
        {
            src.rolloffMode = AudioRolloffMode.Custom;
            float min = src.minDistance, max = src.maxDistance;
            long key = ((long)Mathf.RoundToInt(min * 10f) << 32) | (long)Mathf.RoundToInt(max * 10f);
            if (!Curves.TryGetValue(key, out var c))
            {
                var keys = new List<Keyframe>();
                keys.Add(new Keyframe(0f, 1f));
                for (float dist = min; dist < max; dist *= 1.5f) keys.Add(new Keyframe(dist / max, Fade(dist, min, max)));
                keys.Add(new Keyframe(1f, 0f));
                c = new AnimationCurve(keys.ToArray());
                for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
                Curves[key] = c;
            }
            src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, c);
        }

        static float Fade(float dist, float min, float max)
        {
            float v = dist <= min ? 1f : min / dist;
            float x = Mathf.Clamp01(((dist / max) - 0.65f) / 0.35f);
            return v * (1f - x * x * (3f - 2f * x));
        }

        /// <summary>Far one-shots are darker (air absorbs the highs): a low-pass on the pooled voice, set once when the sound starts.</summary>
        static void DarkenByDistance(AudioSource src, bool spatial)
        {
            if (poolLp == null) return;
            int idx = System.Array.IndexOf(pool, src);
            if (idx < 0 || poolLp[idx] == null) return;
            var lp = poolLp[idx];
            if (!spatial) { lp.enabled = false; return; }
            if (listener == null && Time.unscaledTime >= nextFind) { nextFind = Time.unscaledTime + 2f; listener = UnityEngine.Object.FindFirstObjectByType<AudioListener>(); }
            if (listener == null) { lp.enabled = false; return; }
            float dist = Vector3.Distance(listener.transform.position, src.transform.position);
            if (dist < 6f) { lp.enabled = false; return; }
            float t = Mathf.Clamp01((dist - 6f) / 19f);
            lp.cutoffFrequency = Mathf.Lerp(14000f, 2200f, Mathf.Pow(t, 0.6f));
            lp.enabled = true;
        }

        static AudioSource Acquire()
        {
            if (pool == null || pool[0] == null)
            {
                var root = new GameObject("Sfx pool");
                root.AddComponent<DontBake>();
                UnityEngine.Object.DontDestroyOnLoad(root);
                pool = new AudioSource[PoolSize];
                poolLp = new AudioLowPassFilter[PoolSize];
                for (int i = 0; i < PoolSize; i++)
                {
                    var g = new GameObject("voice " + i);
                    g.transform.SetParent(root.transform, false);
                    pool[i] = g.AddComponent<AudioSource>();
                    pool[i].playOnAwake = false;
                    poolLp[i] = g.AddComponent<AudioLowPassFilter>();   // disabled until a far sound uses the voice
                    poolLp[i].enabled = false;
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
            if (set.Length == 1) return set[0];
            // shuffled round robin: every variation is used once before any repeats, and the first of a new round is never the last of the old one
            if (!Order.TryGetValue(name, out var ord) || ord.Length != set.Length) { ord = new int[set.Length]; Order[name] = ord; OrderPos[name] = ord.Length; }
            int pos = OrderPos[name];
            if (pos >= ord.Length)
            {
                int prev = Last.TryGetValue(name, out int p0) ? p0 : -1;
                for (int k = 0; k < ord.Length; k++) ord[k] = k;
                for (int k = ord.Length - 1; k > 0; k--) { int j = rng.Next(k + 1); int t = ord[k]; ord[k] = ord[j]; ord[j] = t; }
                if (ord[0] == prev) { int t = ord[0]; ord[0] = ord[ord.Length - 1]; ord[ord.Length - 1] = t; }
                pos = 0;
            }
            int i = ord[pos]; OrderPos[name] = pos + 1;
            Last[name] = i;
            return set[i];
        }

        /// <summary>Polishes a recorded clip like a synthesized one (high-pass, gentle low-pass, fades, loudness target, limiter). Works on clips decompressed on load.</summary>
        static void LevelSample(AudioClip c, SfxDef def)
        {
            try
            {
                var d = new float[c.samples * c.channels];
                if (!c.GetData(d, 0)) return;
                if (def == null) def = new SfxDef();
                if (c.channels == 1)
                {
                    lock (Synth.GenLock)
                    {
                        Synth.R = c.frequency;
                        if (def.Lp < 7000f && !def.Loop) Synth.Filter(d, Synth.F.Low, def.Lp, 0.6f); // steeper (24 dB/oct) for bright recordings
                        Synth.Finish(d, def.Loop, def.Lp, def.Level, def.Lufs, def.Ceiling);
                    }
                }
                else Synth.LevelPeak(d, def.Level, def.Ceiling);
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
                if (def == null || !def.Pool) return found.ToArray();
                var both = new List<AudioClip>(found);
                both.AddRange(Synthesize(name, def));
                return both.ToArray();
            }
            Sampled[name] = false;

            // 2) synthesis
            if (def == null) return null;
            return Synthesize(name, def);
        }

        /// <summary>Generates and finishes one variation. Locked because <see cref="Synth.R"/> is shared: the background thread and the main thread never generate at the same moment.</summary>
        static float[] Make(string name, SfxDef def, int v)
        {
            int seedBase = 0;
            foreach (char ch in name) seedBase = seedBase * 31 + ch;
            lock (Synth.GenLock)
            {
                Synth.R = def.Rate;
                var data = def.Gen(new Rng(seedBase + v * 7919));
                Synth.Finish(data, def.Loop, def.Lp, def.Level, def.Lufs, def.Ceiling);
                return data;
            }
        }

        static AudioClip[] Synthesize(string name, SfxDef def)
        {
            var list = new AudioClip[def.Variants];
            for (int v = 0; v < def.Variants; v++)
            {
                var data = Make(name, def, v);
                var clip = AudioClip.Create(name + "_" + v, data.Length, 1, def.Rate, false);
                clip.SetData(data, 0);
                list[v] = clip;
            }
            return list;
        }
    }
}
