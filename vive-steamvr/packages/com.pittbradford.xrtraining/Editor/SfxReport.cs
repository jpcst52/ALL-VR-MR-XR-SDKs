using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Renders every variation of every Sfx catalogue clip to a WAV file and measures it: peak, RMS (dB full scale),
    /// duration, spectral centroid, share of energy above 6 kHz ("harsh"), crest factor, and start/end click flags.
    /// Writes report.csv next to the WAVs and logs a summary. It cannot hear anything: it only measures.
    /// Command line (no -batchmode, needs the editor):
    /// <c>Unity.exe -projectPath P -executeMethod PittBradford.XR.EditorTools.SfxReport.RunAndExit -pbWavDir C:\out\sfx -logFile log.txt</c>
    /// Menu: Pitt-Bradford / Sfx report (writes to Temp/SfxReport).
    /// </summary>
    public static class SfxReport
    {
        [MenuItem("Pitt-Bradford/Sfx report (WAV + numbers)", false, 61)]
        public static void Run() => Execute(Path.Combine(Directory.GetCurrentDirectory(), "Temp", "SfxReport"));

        public static void RunAndExit()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "SfxReport");
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-pbWavDir") dir = a[i + 1];
            bool ok = false;
            try { ok = Execute(dir); }
            catch (Exception e) { Debug.LogError("[sfx-report] exception: " + e); }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        class Row
        {
            public string Name, Clip, Flags = "";
            public float Seconds, PeakDb, RmsDb, ActDb, Centroid, HighShare, Crest;
            public int Rate; public bool Loop, Sample;
            public float Target = -99f;
        }

        /// <summary>The catalogue's loudness target (dBFS active RMS) for a name, read from the private definition table; -99 when unknown.</summary>
        static float TargetOf(string name)
        {
            try
            {
                var f = typeof(Sfx).GetField("Defs", BindingFlags.NonPublic | BindingFlags.Static);
                var dict = (System.Collections.IDictionary)f.GetValue(null);
                if (!dict.Contains(name)) return -99f;
                var def = dict[name];
                return (float)def.GetType().GetField("Level").GetValue(def);
            }
            catch (Exception) { return -99f; }
        }

        static bool Execute(string dir)
        {
            Directory.CreateDirectory(dir);
            var clipsField = typeof(Sfx).GetField("Clips", BindingFlags.NonPublic | BindingFlags.Static);
            var names = new List<string>(Sfx.Names);
            names.Sort(StringComparer.Ordinal);
            var rows = new List<Row>();
            foreach (var n in names) Sfx.Preload(n);
            var clips = (Dictionary<string, AudioClip[]>)clipsField.GetValue(null);
            foreach (var n in names)
            {
                if (!clips.TryGetValue(n, out var set) || set == null) continue;
                foreach (var c in set)
                {
                    var d = new float[c.samples * c.channels];
                    if (!c.GetData(d, 0)) { Debug.LogWarning("[sfx-report] cannot read " + c.name); continue; }
                    var row = Measure(n, c.name, d, c.frequency, c.channels);
                    row.Loop = Sfx.IsLoop(n); row.Sample = Sfx.IsSample(n);
                    row.Target = TargetOf(n);
                    if (row.Target > -90f && Mathf.Abs(row.ActDb - row.Target) > 1.5f) row.Flags += "LIMITED(" + (row.ActDb - row.Target).ToString("F1") + " dB vs target) ";
                    rows.Add(row);
                    WriteWav(Path.Combine(dir, c.name + ".wav"), d, c.frequency, c.channels);
                }
            }
            var sb = new StringBuilder("name,clip,seconds,rate,peak_dbfs,rms_dbfs,active_rms_dbfs,crest_db,centroid_hz,share_above_6k,loop,recorded,flags\n");
            int flagged = 0;
            foreach (var r in rows)
            {
                sb.AppendLine($"{r.Name},{r.Clip},{r.Seconds:F2},{r.Rate},{r.PeakDb:F1},{r.RmsDb:F1},{r.ActDb:F1},{r.Crest:F1},{r.Centroid:F0},{r.HighShare:F3},{r.Loop},{r.Sample},{r.Flags.Trim()}");
                if (r.Flags.Length > 0) flagged++;
            }
            File.WriteAllText(Path.Combine(dir, "report.csv"), sb.ToString());

            // one summary line per name (first variation) so the log is readable
            var seen = new HashSet<string>();
            foreach (var r in rows)
            {
                if (!seen.Add(r.Name)) continue;
                Debug.Log($"[sfx-report] {r.Name,-22} {r.Seconds,5:F2}s peak {r.PeakDb,6:F1} dB rms {r.RmsDb,6:F1} dB active {r.ActDb,6:F1} dB centroid {r.Centroid,5:F0} Hz >6k {r.HighShare * 100,4:F1}% crest {r.Crest,4:F1}{(r.Loop ? " loop" : "")}{(r.Sample ? " rec" : "")} {r.Flags}");
            }
            // loudness spread of one-shots (excluding loops): the consistency number
            float min = 999, max = -999; double sum = 0; int cnt = 0;
            foreach (var r in rows) { if (r.Loop) continue; min = Mathf.Min(min, r.ActDb); max = Mathf.Max(max, r.ActDb); sum += r.ActDb; cnt++; }
            int limited = 0; foreach (var r in rows) if (r.Flags.Contains("LIMITED")) limited++;
            Debug.Log($"[sfx-report] {rows.Count - limited} of {rows.Count} clips are within 1.5 dB of their loudness target; {limited} are held below it by the peak limiter (very peaky transients)");
            Debug.Log($"[sfx-report] DONE {rows.Count} clips ({names.Count} names), {flagged} with flags; one-shot ACTIVE RMS min {min:F1} max {max:F1} mean {(cnt > 0 ? sum / cnt : 0):F1} dBFS; output {dir}");
            return true;
        }

        static Row Measure(string name, string clip, float[] d, int rate, int ch)
        {
            // mono mixdown
            int n = d.Length / Mathf.Max(1, ch);
            var m = new float[n];
            for (int i = 0; i < n; i++) { float s = 0; for (int c = 0; c < ch; c++) s += d[i * ch + c]; m[i] = s / ch; }
            var r = new Row { Name = name, Clip = clip, Rate = rate, Seconds = (float)n / rate };
            double sum = 0; float peak = 1e-9f; int nan = 0;
            for (int i = 0; i < n; i++)
            {
                float v = m[i];
                if (float.IsNaN(v) || float.IsInfinity(v)) { nan++; continue; }
                peak = Mathf.Max(peak, Mathf.Abs(v)); sum += v * v;
            }
            float rms = (float)Math.Sqrt(sum / Mathf.Max(1, n));
            r.PeakDb = 20f * Mathf.Log10(peak); r.RmsDb = 20f * Mathf.Log10(Mathf.Max(1e-9f, rms));
            r.Crest = r.PeakDb - r.RmsDb;
            double sa = 0; int ca = 0; float th = peak * 0.05f;
            for (int i = 0; i < n; i++) if (Mathf.Abs(m[i]) >= th) { sa += m[i] * m[i]; ca++; }
            r.ActDb = 20f * Mathf.Log10(Mathf.Max(1e-9f, Mathf.Sqrt((float)(sa / Mathf.Max(1, ca)))));
            // spectrum of up to the first 32768 samples (power of two), Hann window
            int N = 1; while (N * 2 <= Mathf.Min(n, 32768)) N *= 2;
            var re = new double[N]; var im = new double[N];
            for (int i = 0; i < N; i++) re[i] = m[i] * (0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1)));
            Fft(re, im);
            double tot = 0, wsum = 0, hi = 0;
            for (int k = 1; k < N / 2; k++)
            {
                double p = re[k] * re[k] + im[k] * im[k], f = (double)k * rate / N;
                tot += p; wsum += p * f; if (f > 6000.0) hi += p;
            }
            r.Centroid = tot > 0 ? (float)(wsum / tot) : 0; r.HighShare = tot > 0 ? (float)(hi / tot) : 0;
            // flags
            var fl = new StringBuilder();
            if (nan > 0) fl.Append("NAN ");
            if (peak >= 0.99f) fl.Append("CLIP ");
            float startAbs = Mathf.Abs(m[0]), endAbs = Mathf.Abs(m[n - 1]);
            if (!(Sfx.IsLoop(name)))
            {
                if (startAbs > 0.02f) fl.Append("CLICK-START ");
                if (endAbs > 0.02f) fl.Append("CLICK-END ");
            }
            else
            {
                // a loop must close: the jump from last to first sample should be like any neighbouring step
                float jump = Mathf.Abs(m[0] - m[n - 1]), typ = 0; int cntj = 0;
                for (int i = 1; i < Mathf.Min(n, 4000); i++) { typ += Mathf.Abs(m[i] - m[i - 1]); cntj++; }
                typ /= Mathf.Max(1, cntj);
                if (jump > 6f * typ + 0.02f) fl.Append("LOOP-SEAM ");
            }
            if (r.HighShare > 0.35f) fl.Append("HARSH ");
            if (r.ActDb < -33f && !Sfx.IsLoop(name)) fl.Append("QUIET ");
            r.Flags = fl.ToString();
            return r;
        }

        static void Fft(double[] re, double[] im)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j) { double t = re[i]; re[i] = re[j]; re[j] = t; t = im[i]; im[i] = im[j]; im[j] = t; }
            }
            for (int len = 2; len <= n; len <<= 1)
            {
                double ang = -2 * Math.PI / len, wr = Math.Cos(ang), wi = Math.Sin(ang);
                for (int i = 0; i < n; i += len)
                {
                    double cr = 1, ci = 0;
                    for (int k = 0; k < len / 2; k++)
                    {
                        int a = i + k, b = i + k + len / 2;
                        double xr = re[b] * cr - im[b] * ci, xi = re[b] * ci + im[b] * cr;
                        re[b] = re[a] - xr; im[b] = im[a] - xi; re[a] += xr; im[a] += xi;
                        double nr = cr * wr - ci * wi; ci = cr * wi + ci * wr; cr = nr;
                    }
                }
            }
        }

        static void WriteWav(string path, float[] data, int rate, int ch)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data.Length * 2); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate * 2 * ch); w.Write((short)(2 * ch)); w.Write((short)16);
                w.Write(Encoding.ASCII.GetBytes("data")); w.Write(data.Length * 2);
                foreach (var v in data) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
            }
        }
    }
}
