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

        static bool Execute(string dir)
        {
            Directory.CreateDirectory(dir);
            var clipsField = typeof(Sfx).GetField("Clips", BindingFlags.NonPublic | BindingFlags.Static);
            var names = new List<string>(Sfx.Names);
            names.Sort(StringComparer.Ordinal);
            var rows = new List<SfxMeasure.Row>();
            foreach (var n in names) Sfx.Preload(n);
            var clips = (Dictionary<string, AudioClip[]>)clipsField.GetValue(null);
            foreach (var n in names)
            {
                if (!clips.TryGetValue(n, out var set) || set == null) continue;
                foreach (var c in set)
                {
                    var d = new float[c.samples * c.channels];
                    if (!c.GetData(d, 0)) { Debug.LogWarning("[sfx-report] cannot read " + c.name); continue; }
                    var mono = new float[d.Length / Mathf.Max(1, c.channels)];
                    for (int q = 0; q < mono.Length; q++) { float acc = 0; for (int ch = 0; ch < c.channels; ch++) acc += d[q * c.channels + ch]; mono[q] = acc / c.channels; }
                    var row = SfxMeasure.Measure(n, c.name, mono, c.frequency, Sfx.IsLoop(n));
                    row.Sample = Sfx.IsSample(n);
                    if (SfxCatalog.Defs.TryGetValue(n, out var def))
                    {
                        float ceilDb = 20f * Mathf.Log10(def.Ceiling);
                        if (row.PeakDb > ceilDb + 0.6f) row.Flags += "PEAK-HIGH(" + row.PeakDb.ToString("F1") + ") ";
                        if (def.Loop && !float.IsNaN(def.Lufs) && Mathf.Abs(row.Lufs - def.Lufs) > 2.5f) row.Flags += "OFF-LUFS(" + (row.Lufs - def.Lufs).ToString("F1") + ") ";
                    }
                    rows.Add(row);
                    WriteWav(Path.Combine(dir, c.name + ".wav"), d, c.frequency, c.channels);
                }
            }
            var sb = new StringBuilder("name,clip,seconds,rate,peak_dbfs,rms_dbfs,active_rms_dbfs,lufs,crest_db,centroid_hz,share_above_6k,loop,recorded,flags\n");
            int flagged = 0;
            foreach (var r in rows)
            {
                sb.AppendLine($"{r.Name},{r.Clip},{r.Seconds:F2},{r.Rate},{r.PeakDb:F1},{r.RmsDb:F1},{r.ActDb:F1},{r.Lufs:F1},{r.Crest:F1},{r.Centroid:F0},{r.HighShare:F3},{r.Loop},{r.Sample},{r.Flags.Trim()}");
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
            float pkMax = -999f; foreach (var r in rows) if (!r.Loop) pkMax = Mathf.Max(pkMax, r.PeakDb);
            Debug.Log($"[sfx-report] highest one-shot peak {pkMax:F1} dBFS");
            Debug.Log($"[sfx-report] DONE {rows.Count} clips ({names.Count} names), {flagged} with flags; one-shot ACTIVE RMS min {min:F1} max {max:F1} mean {(cnt > 0 ? sum / cnt : 0):F1} dBFS; output {dir}");
            return true;
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
