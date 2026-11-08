using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Generates every sound in the Sfx catalogue and checks it: not empty, no NaN or infinity, peak below 1, not silent.
    /// Logs one line per name (length, peak, RMS, generation time) and a final "[sfx-test] PASS/FAIL" line.
    /// Menu: Pitt-Bradford/Sfx self-test. Command line: -executeMethod PittBradford.XR.EditorTools.SfxSelfTest.RunAndExit
    /// (optional -pbWavDir &lt;dir&gt; writes each clip as a WAV file so it can be listened to).
    /// </summary>
    public static class SfxSelfTest
    {
        [MenuItem("Pitt-Bradford/Sfx self-test", false, 60)]
        public static void Run() => Execute(null);

        public static void RunAndExit()
        {
            string dir = null;
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-pbWavDir") dir = a[i + 1];
            bool ok = Execute(dir);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Execute(string wavDir)
        {
            int bad = 0, total = 0;
            var all = new Stopwatch();
            all.Start();
            foreach (var name in new System.Collections.Generic.List<string>(Sfx.Names))
            {
                var sw = Stopwatch.StartNew();
                Sfx.Preload(name);
                sw.Stop();
                // pick every variation by asking repeatedly
                for (int k = 0; k < 6; k++)
                {
                    var src = PickForTest(name);
                    if (src == null) { Debug.LogError("[sfx-test] no clip for " + name); bad++; continue; }
                    total++;
                    var data = new float[src.samples];
                    src.GetData(data, 0);
                    float peak = 0, sum = 0; bool nan = false;
                    foreach (var v in data) { if (float.IsNaN(v) || float.IsInfinity(v)) nan = true; peak = Mathf.Max(peak, Mathf.Abs(v)); sum += v * v; }
                    float rms = Mathf.Sqrt(sum / Mathf.Max(1, data.Length));
                    bool fail = nan || peak > 0.99f || peak < 0.05f || data.Length < 100 || rms < 0.003f;
                    if (fail) bad++;
                    if (k == 0 || fail)
                        Debug.Log($"[sfx-test] {(fail ? "FAIL" : "ok  ")} {name} {src.name}: {src.length:F2}s @{src.frequency} Hz peak {peak:F2} rms {rms:F3} gen {sw.ElapsedMilliseconds} ms{(Sfx.IsLoop(name) ? " loop" : "")}{(Sfx.IsSample(name) ? " sample" : "")}");
                    if (wavDir != null && k == 0) WriteWav(Path.Combine(wavDir, src.name + ".wav"), data, src.frequency);
                }
            }
            bad += RuntimeParts();
            Debug.Log($"[sfx-test] {(bad == 0 ? "PASS" : "FAIL")}: {total} clips checked, {bad} problems, {all.ElapsedMilliseconds} ms total");
            return bad == 0;
        }

        /// <summary>Edit-mode checks of the round 5 playback code that does not need play mode: custom rolloff, play gains, shuffled round robin.</summary>
        static int RuntimeParts()
        {
            int bad = 0;
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var go = new GameObject("sfx-test source");
            try
            {
                var src = go.AddComponent<AudioSource>();
                src.minDistance = 1.2f; src.maxDistance = 25f;
                typeof(Sfx).GetMethod("ApplyRolloff", flags).Invoke(null, new object[] { src });
                var c = src.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
                float near = c.Evaluate(1.2f / 25f), mid = c.Evaluate(5f / 25f), far = c.Evaluate(1f), past = c.Evaluate(0.9f);
                bool ok = src.rolloffMode == AudioRolloffMode.Custom && near > 0.9f && mid < near && mid > 0.05f && far < 0.001f && past < mid;
                Debug.Log($"[sfx-test] {(ok ? "ok  " : "FAIL")} custom rolloff: 1.2 m {near:F2}, 5 m {mid:F2}, 22.5 m {past:F3}, 25 m {far:F3}");
                if (!ok) bad++;
            }
            catch (System.Exception e) { Debug.LogError("[sfx-test] FAIL custom rolloff: " + e.Message); bad++; }
            finally { Object.DestroyImmediate(go); }

            // loops: file level and in-scene level give a play gain between 0 and 1; one-shots peak at or under -6 dBFS
            foreach (var kv in SfxCatalog.Defs)
            {
                var d = kv.Value;
                bool ok = d.Loop ? (d.Gain > 0.01f && d.Gain <= 1f && !float.IsNaN(d.Lufs)) : (d.Ceiling <= 0.502f && d.Gain == 1f);
                if (!ok) { Debug.LogError($"[sfx-test] FAIL catalogue entry {kv.Key}: gain {d.Gain} ceiling {d.Ceiling} lufs {d.Lufs}"); bad++; }
            }

            // shuffled round robin: over 3 rounds every variation appears once per round and a round never starts with the last clip of the one before
            var pick = typeof(Sfx).GetMethod("Pick", flags);
            foreach (var name in new[] { "button", "footstep_metal", "water_drip" })
            {
                var seen = new System.Collections.Generic.List<string>();
                pick.Invoke(null, new object[] { name, false });
                var set = ((System.Collections.Generic.Dictionary<string, AudioClip[]>)typeof(Sfx).GetField("Clips", flags).GetValue(null))[name];
                int n = set.Length;
                ((System.Collections.Generic.Dictionary<string, int>)typeof(Sfx).GetField("OrderPos", flags).GetValue(null))[name] = n;   // start a fresh round
                for (int i = 0; i < n * 3; i++) seen.Add(((AudioClip)pick.Invoke(null, new object[] { name, false })).GetInstanceID().ToString());
                bool ok = true;
                for (int r = 0; r < 3; r++)
                {
                    var round = new System.Collections.Generic.HashSet<string>();
                    for (int i = 0; i < n; i++) round.Add(seen[r * n + i]);
                    if (round.Count != n) ok = false;
                    if (r > 0 && seen[r * n] == seen[r * n - 1]) ok = false;
                }
                Debug.Log($"[sfx-test] {(ok ? "ok  " : "FAIL")} round robin {name}: {n} variations, 3 rounds");
                if (!ok) bad++;
            }
            return bad;
        }

        static AudioClip PickForTest(string name)
        {
            var m = typeof(Sfx).GetMethod("Pick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (AudioClip)m.Invoke(null, new object[] { name, false });
        }

        static void WriteWav(string path, float[] data, int rate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data.Length * 2); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(data.Length * 2);
                foreach (var v in data) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
            }
        }
    }
}
