using System;
using System.Text;

namespace PittBradford.XR
{
    /// <summary>
    /// Numbers about a finished clip: peak, RMS, active RMS, crest, spectral centroid, share of energy above 6 kHz, an approximate
    /// loudness (ITU-R BS.1770 K-weighting with the usual gating, "LUFS-equivalent"), and warning flags. It contains no Unity engine
    /// type, so the offline renderer (tools/audio-render) and the editor report (SfxReport) use the same code. It measures; it cannot hear.
    /// </summary>
    public static class SfxMeasure
    {
        public sealed class Row
        {
            public string Name, Clip, Flags = "";
            public float Seconds, PeakDb, RmsDb, ActDb, Centroid, HighShare, Crest, Lufs;
            public int Rate;
            public bool Loop, Sample;
            public float Target = -99f;
        }

        static float Db(float lin) => 20f * (float)Math.Log10(Math.Max(1e-9f, lin));

        public static Row Measure(string name, string clip, float[] m, int rate, bool loop)
        {
            int n = m.Length;
            var r = new Row { Name = name, Clip = clip, Rate = rate, Seconds = (float)n / rate, Loop = loop };
            double sum = 0; float peak = 1e-9f; int nan = 0;
            for (int i = 0; i < n; i++)
            {
                float v = m[i];
                if (float.IsNaN(v) || float.IsInfinity(v)) { nan++; continue; }
                peak = Math.Max(peak, Math.Abs(v)); sum += v * v;
            }
            float rms = (float)Math.Sqrt(sum / Math.Max(1, n));
            r.PeakDb = Db(peak); r.RmsDb = Db(rms); r.Crest = r.PeakDb - r.RmsDb;
            double sa = 0; int ca = 0; float th = peak * 0.05f;
            for (int i = 0; i < n; i++) if (Math.Abs(m[i]) >= th) { sa += m[i] * m[i]; ca++; }
            r.ActDb = Db((float)Math.Sqrt(sa / Math.Max(1, ca)));
            r.Lufs = Lufs(m, rate);
            int N = 1; while (N * 2 <= Math.Min(n, 32768)) N *= 2;
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
            var fl = new StringBuilder();
            if (nan > 0) fl.Append("NAN ");
            if (peak >= 0.99f) fl.Append("CLIP ");
            float startAbs = Math.Abs(m[0]), endAbs = Math.Abs(m[n - 1]);
            if (!loop)
            {
                if (startAbs > 0.02f) fl.Append("CLICK-START ");
                if (endAbs > 0.02f) fl.Append("CLICK-END ");
            }
            else
            {
                float jump = Math.Abs(m[0] - m[n - 1]), typ = 0; int cntj = 0;
                for (int i = 1; i < Math.Min(n, 4000); i++) { typ += Math.Abs(m[i] - m[i - 1]); cntj++; }
                typ /= Math.Max(1, cntj);
                if (jump > 6f * typ + 0.02f) fl.Append("LOOP-SEAM ");
            }
            if (r.HighShare > 0.35f) fl.Append("HARSH ");
            if (r.ActDb < -33f && !loop) fl.Append("QUIET ");
            r.Flags = fl.ToString();
            return r;
        }

        // ---------------------------------------------------------------- loudness (BS.1770, mono)

        static void Biquad(double[] x, double b0, double b1, double b2, double a1, double a2)
        {
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double y = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x[i]; y2 = y1; y1 = y; x[i] = y;
            }
        }

        /// <summary>Integrated loudness in LUFS-equivalent (K-weighted, 400 ms blocks, absolute gate -70, relative gate -10). A clip shorter than 400 ms is one block.</summary>
        public static float Lufs(float[] m, int rate)
        {
            int n = m.Length;
            var x = new double[Math.Max(n, (int)(0.4 * rate))];
            for (int i = 0; i < n; i++) x[i] = m[i];
            double K = Math.Tan(Math.PI * 1681.974450955533 / rate), Q = 0.7071752369554196, Vh = Math.Pow(10.0, 3.999843853973347 / 20.0), Vb = Math.Pow(Vh, 0.4996667741545416);
            double a0 = 1 + K / Q + K * K;
            Biquad(x, (Vh + Vb * K / Q + K * K) / a0, 2 * (K * K - Vh) / a0, (Vh - Vb * K / Q + K * K) / a0, 2 * (K * K - 1) / a0, (1 - K / Q + K * K) / a0);
            K = Math.Tan(Math.PI * 38.13547087602444 / rate); Q = 0.5003270373238773; a0 = 1 + K / Q + K * K;
            Biquad(x, 1, -2, 1, 2 * (K * K - 1) / a0, (1 - K / Q + K * K) / a0);
            int blk = (int)(0.4 * rate), hop = Math.Max(1, (int)(0.1 * rate));
            var ms = new System.Collections.Generic.List<double>();
            for (int s = 0; s + blk <= x.Length; s += hop)
            {
                double e = 0; for (int i = s; i < s + blk; i++) e += x[i] * x[i];
                ms.Add(e / blk);
            }
            double Lk(double e) => -0.691 + 10 * Math.Log10(Math.Max(1e-12, e));
            double sum = 0; int c = 0;
            foreach (var e in ms) if (Lk(e) > -70) { sum += e; c++; }
            if (c == 0) return -99f;
            double rel = Lk(sum / c) - 10;
            double sum2 = 0; int c2 = 0;
            foreach (var e in ms) if (Lk(e) > -70 && Lk(e) > rel) { sum2 += e; c2++; }
            return c2 == 0 ? (float)Lk(sum / c) : (float)Lk(sum2 / c2);
        }

        public static void Fft(double[] re, double[] im)
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
    }
}
