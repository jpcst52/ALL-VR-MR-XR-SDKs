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
            Debug.Log($"[sfx-test] {(bad == 0 ? "PASS" : "FAIL")}: {total} clips checked, {bad} problems, {all.ElapsedMilliseconds} ms total");
            return bad == 0;
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
