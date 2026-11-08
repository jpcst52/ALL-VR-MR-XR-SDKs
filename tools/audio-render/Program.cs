using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using PittBradford.XR;

// Renders every catalogue sound (every variation) to a 16-bit 44.1 kHz mono WAV and measures it, with no Unity window.
//   dotnet run -c Release --project tools/audio-render -- --out docs/audio-review/wav [--rec <folder of decoded recordings>] [--filter name]
// It uses the same Synth.cs, SfxCatalog.cs and finishing stage as the game. It cannot hear; the numbers are measurements.
static class Program
{
    const int OutRate = 44100;

    static int Main(string[] args)
    {
        string outDir = "audio-out", recDir = null, filter = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--out") outDir = args[i + 1];
            if (args[i] == "--rec") recDir = args[i + 1];
            if (args[i] == "--filter") filter = args[i + 1];
        }
        Directory.CreateDirectory(outDir);
        var names = new List<string>(SfxCatalog.Defs.Keys);
        names.Sort(StringComparer.Ordinal);
        var csv = new StringBuilder("name,clip,kind,seconds,rate,peak_dbfs,rms_dbfs,active_rms_dbfs,lufs,crest_db,centroid_hz,share_above_6k,loop,recorded,flags\n");
        int clips = 0, flagged = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var slow = new List<KeyValuePair<double, string>>();
        foreach (var name in names)
        {
            var nameWatch = System.Diagnostics.Stopwatch.StartNew();
            if (filter != null && !name.Contains(filter)) continue;
            var def = SfxCatalog.Defs[name];
            var recs = LoadRecordings(recDir, name);
            int count = recs.Count > 0 ? recs.Count + (def.Pool ? def.Variants : 0) : def.Variants;
            for (int v = 0; v < count; v++)
            {
                float[] data; int rate; bool rec = recs.Count > 0 && v < recs.Count;
                if (rec)
                {
                    data = recs[v].data; rate = recs[v].rate;
                    Synth.R = rate;
                    if (def.Lp < 7000f && !def.Loop) Synth.Filter(data, Synth.F.Low, def.Lp, 0.6f);
                    Synth.Finish(data, def.Loop, def.Lp, def.Level, def.Lufs, def.Ceiling);
                }
                else
                {
                    int seedBase = 0; foreach (char ch in name) seedBase = seedBase * 31 + ch;
                    Synth.R = def.Rate; rate = def.Rate;
                    data = def.Gen(new Random(seedBase + (v - recs.Count) * 7919));
                    Synth.Finish(data, def.Loop, def.Lp, def.Level, def.Lufs, def.Ceiling);
                }
                string clip = name + "_" + v;
                var row = SfxMeasure.Measure(name, clip, data, rate, def.Loop);
                float ceilDb = 20f * (float)Math.Log10(def.Ceiling);
                if (row.PeakDb > ceilDb + 0.6f) row.Flags += "PEAK-HIGH(" + row.PeakDb.ToString("F1", CultureInfo.InvariantCulture) + ") ";
                if (def.Loop && !float.IsNaN(def.Lufs) && Math.Abs(row.Lufs - def.Lufs) > 2.5f) row.Flags += "OFF-LUFS(" + (row.Lufs - def.Lufs).ToString("F1", CultureInfo.InvariantCulture) + ") ";
                if (row.Flags.Trim().Length > 0) flagged++;
                WriteWav(Path.Combine(outDir, clip + ".wav"), Resample(data, rate, OutRate), OutRate);
                var ci = CultureInfo.InvariantCulture;
                csv.AppendLine(string.Join(",", name, clip, def.Loop ? "loop" : "one-shot", row.Seconds.ToString("F2", ci), rate, row.PeakDb.ToString("F1", ci), row.RmsDb.ToString("F1", ci),
                    row.ActDb.ToString("F1", ci), row.Lufs.ToString("F1", ci), row.Crest.ToString("F1", ci), ((int)row.Centroid).ToString(ci), row.HighShare.ToString("F3", ci), def.Loop, rec, row.Flags.Trim()));
                clips++;
            }
            slow.Add(new KeyValuePair<double, string>(nameWatch.Elapsed.TotalSeconds, name));
        }
        var cat = new StringBuilder("name,kind,rate,variants,peak_target_dbfs,rms_cap_dbfs,lufs_file,lufs_in_scene,play_gain,lowpass_hz,priority,reverb_zone_mix,pooled_with_recordings\n");
        foreach (var name in names)
        {
            var d = SfxCatalog.Defs[name]; var ic = CultureInfo.InvariantCulture;
            cat.AppendLine(string.Join(",", name, d.Loop ? "loop" : "one-shot", d.Rate, d.Variants, d.Loop ? "" : (20.0 * Math.Log10(d.Ceiling)).ToString("F0", ic), d.Loop ? "" : d.Level.ToString("F0", ic),
                float.IsNaN(d.Lufs) ? "" : d.Lufs.ToString("F0", ic), float.IsNaN(d.SceneLufs) ? "" : d.SceneLufs.ToString("F0", ic), d.Gain.ToString("F3", ic), d.Lp.ToString("F0", ic), d.Priority, d.Mix.ToString("F1", ic), d.Pool));
        }
        File.WriteAllText(Path.Combine(outDir, "catalog.csv"), cat.ToString());
        slow.Sort((a, b) => b.Key.CompareTo(a.Key));
        Console.WriteLine("slowest to generate (all variations, includes WAV writing): " + string.Join(", ", slow.GetRange(0, Math.Min(8, slow.Count)).ConvertAll(x => x.Value + " " + x.Key.ToString("F2") + " s")));
        File.WriteAllText(Path.Combine(outDir, "report.csv"), csv.ToString());
        Console.WriteLine($"rendered {clips} clips ({names.Count} names), {flagged} flagged, {watch.Elapsed.TotalSeconds:F1} s -> {outDir}");
        return 0;
    }

    sealed class Rec { public float[] data; public int rate; }

    static List<Rec> LoadRecordings(string dir, string name)
    {
        var list = new List<Rec>();
        if (dir == null) return list;
        for (int i = 1; i <= 8; i++)
        {
            string p = Path.Combine(dir, name + "_" + i + ".wav");
            if (!File.Exists(p)) continue;
            using (var br = new BinaryReader(File.OpenRead(p)))
            {
                br.ReadBytes(22); int ch = br.ReadInt16(); int rate = br.ReadInt32(); br.ReadBytes(6); int bits = br.ReadInt16();
                br.ReadBytes(4); int bytes = br.ReadInt32();
                int n = bytes / (bits / 8) / ch; var d = new float[n];
                for (int k = 0; k < n; k++) { float acc = 0; for (int c = 0; c < ch; c++) acc += br.ReadInt16() / 32768f; d[k] = acc / ch; }
                list.Add(new Rec { data = d, rate = rate });
            }
        }
        return list;
    }

    /// <summary>Windowed-sinc resampler (Hann, 12 lobes), good enough for the review files; a copy at the same rate is returned unchanged.</summary>
    static float[] Resample(float[] x, int from, int to)
    {
        if (from == to) return x;
        int n = (int)((long)x.Length * to / from);
        var o = new float[n];
        double ratio = (double)from / to, fc = Math.Min(1.0, (double)to / from) * 0.97;
        const int L = 12;
        for (int i = 0; i < n; i++)
        {
            double t = i * ratio; int c = (int)Math.Floor(t); double acc = 0;
            for (int k = c - L + 1; k <= c + L; k++)
            {
                if (k < 0 || k >= x.Length) continue;
                double d = t - k, s = Math.Abs(d) < 1e-9 ? 1.0 : Math.Sin(Math.PI * d * fc) / (Math.PI * d * fc);
                double w = 0.5 + 0.5 * Math.Cos(Math.PI * d / L);
                acc += x[k] * s * w * fc;
            }
            o[i] = (float)acc;
        }
        return o;
    }

    static void WriteWav(string path, float[] data, int rate)
    {
        using (var w = new BinaryWriter(File.Create(path)))
        {
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data.Length * 2); w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(data.Length * 2);
            foreach (var v in data) w.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, v)) * 32767f));
        }
    }
}
