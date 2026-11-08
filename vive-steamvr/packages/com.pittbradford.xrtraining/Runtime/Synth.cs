using System;
using UnityEngine;
using Rng = System.Random;

namespace PittBradford.XR
{
    /// <summary>
    /// Layered sound synthesis used by <see cref="Sfx"/> for every catalogue name that has no recorded sample.
    /// Round 4 (unheard, measured only): impacts are a transient plus a modal body (inharmonic partials with their own decay)
    /// plus a thump and a small damped room; noise beds use pink noise instead of white; loops close seamlessly (integer
    /// cycles, folded tails, cross-faded noise); alarms and beeps have soft attacks, fewer high harmonics and a low-pass;
    /// every clip goes through <see cref="Finish"/> (DC or rumble removal, gentle low-pass, fades, loudness target, soft limiter).
    /// All buffers are mono floats at <see cref="R"/> samples per second (set by the caller before generating).
    /// </summary>
    public static class Synth
    {
        public static int R = 32000;
        /// <summary>Hold this while setting <see cref="R"/> and generating: <see cref="Sfx"/> builds sounds on a background thread as well as on the main thread.</summary>
        public static readonly object GenLock = new object();
        const float Tau = Mathf.PI * 2f;

        public enum Mat { Metal, Wood, Plastic, Glass }

        // ------------------------------------------------------------------ building blocks

        public static float[] Buf(float seconds) => new float[Mathf.Max(8, (int)(seconds * R))];

        public static float Rnd(Rng r) => (float)r.NextDouble() * 2f - 1f;
        public static float Range(Rng r, float a, float b) => a + (float)r.NextDouble() * (b - a);
        /// <summary>value +/- pct (0.05 = 5 percent), random per call.</summary>
        public static float J(Rng r, float v, float pct) => v * (1f + Rnd(r) * pct);

        public static float[] White(Rng r, float seconds, float amp = 1f)
        {
            var b = Buf(seconds);
            for (int i = 0; i < b.Length; i++) b[i] = Rnd(r) * amp;
            return b;
        }

        /// <summary>Pink (1/f) noise, Paul Kellet's filter: warmer and less hissy than white noise, roughly unit level.</summary>
        public static float[] Pink(Rng r, float seconds, float amp = 1f)
        {
            var b = Buf(seconds);
            float b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float w = Rnd(r);
                b0 = 0.99886f * b0 + w * 0.0555179f; b1 = 0.99332f * b1 + w * 0.0750759f;
                b2 = 0.96900f * b2 + w * 0.1538520f; b3 = 0.86650f * b3 + w * 0.3104856f;
                b4 = 0.55000f * b4 + w * 0.5329522f; b5 = -0.7616f * b5 - w * 0.0168980f;
                b[i] = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + w * 0.5362f) * 0.22f * amp;
                b6 = w * 0.115926f;
            }
            return b;
        }

        public enum F { Low, High, Band }

        static void Coef(F type, float f, float q, out float b0, out float b1, out float b2, out float a1, out float a2)
        {
            f = Mathf.Clamp(f, 20f, R * 0.45f);
            float w = Tau * f / R, cs = Mathf.Cos(w), al = Mathf.Sin(w) / (2f * Mathf.Max(0.1f, q));
            float a0 = 1f + al;
            switch (type)
            {
                case F.Low: b0 = (1f - cs) / 2f; b1 = 1f - cs; b2 = b0; break;
                case F.High: b0 = (1f + cs) / 2f; b1 = -(1f + cs); b2 = b0; break;
                default: b0 = al; b1 = 0f; b2 = -al; break;
            }
            b0 /= a0; b1 /= a0; b2 /= a0; a1 = -2f * cs / a0; a2 = (1f - al) / a0;
        }

        /// <summary>Biquad filter in place (RBJ cookbook). Returns the same array.</summary>
        public static float[] Filter(float[] x, F type, float f, float q = 0.707f)
        {
            Coef(type, f, q, out float b0, out float b1, out float b2, out float a1, out float a2);
            float x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < x.Length; i++)
            {
                float y = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x[i]; y2 = y1; y1 = y; x[i] = y;
            }
            return x;
        }

        /// <summary>Filter for a seamless loop: runs over the buffer twice and keeps the second pass, so the filter state at the loop point is right (no start-up ramp).</summary>
        public static float[] FilterLoop(float[] x, F type, float f, float q = 0.707f)
        {
            int n = x.Length;
            var d = new float[n * 2];
            Array.Copy(x, 0, d, 0, n); Array.Copy(x, 0, d, n, n);
            Filter(d, type, f, q);
            Array.Copy(d, n, x, 0, n);
            return x;
        }

        /// <summary>Biquad whose cutoff follows a function of time in seconds (sweeps, wind, squeaks).</summary>
        public static float[] FilterVar(float[] x, F type, Func<float, float> fAt, float q = 0.707f)
        {
            float b0 = 0, b1 = 0, b2 = 0, a1 = 0, a2 = 0, x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < x.Length; i++)
            {
                if ((i & 31) == 0) Coef(type, fAt((float)i / R), q, out b0, out b1, out b2, out a1, out a2);
                float y = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x[i]; y2 = y1; y1 = y; x[i] = y;
            }
            return x;
        }

        /// <summary>Multiplies by a linear attack and an exponential decay (decay in 1/seconds).</summary>
        public static float[] Env(float[] x, float attack, float decay)
        {
            for (int i = 0; i < x.Length; i++)
            {
                float t = (float)i / R;
                x[i] *= (attack > 0f ? Mathf.Min(1f, t / attack) : 1f) * Mathf.Exp(-decay * t);
            }
            return x;
        }

        /// <summary>Attack, sustain, release gate over the whole buffer (smooth: raised-cosine ramps).</summary>
        public static float[] Gate(float[] x, float attack, float release)
        {
            float len = (float)x.Length / R;
            for (int i = 0; i < x.Length; i++)
            {
                float t = (float)i / R;
                float a = Mathf.Clamp01(t / Mathf.Max(0.001f, attack)), d = Mathf.Clamp01((len - t) / Mathf.Max(0.001f, release));
                x[i] *= (0.5f - 0.5f * Mathf.Cos(a * Mathf.PI)) * (0.5f - 0.5f * Mathf.Cos(d * Mathf.PI));
            }
            return x;
        }

        public static float[] Shape(float[] x, Func<float, float> amp)
        {
            for (int i = 0; i < x.Length; i++) x[i] *= amp((float)i / R);
            return x;
        }

        public static void Add(float[] dst, float[] src, float gain = 1f, float at = 0f)
        {
            int o = (int)(at * R);
            for (int i = 0; i < src.Length; i++)
            {
                int j = o + i;
                if (j >= 0 && j < dst.Length) dst[j] += src[i] * gain;
            }
        }

        /// <summary>
        /// A decaying sine (with optional FM) added at <paramref name="start"/> seconds. It fades out over its last 4 ms so a segment that
        /// is cut off while still audible does not click. A steady tone (no decay) that fills the whole buffer is left alone: it is a
        /// loop component, whole cycles, and must not dip at the loop point.
        /// </summary>
        public static void Tone(float[] dst, float freq, float amp, float start, float dur, float attack, float decay, float fmRatio = 0f, float fmIndex = 0f)
        {
            if (freq > R * 0.45f) return;
            int o = (int)(start * R), n = (int)(dur * R);
            bool steady = decay == 0f && o == 0 && n >= dst.Length - 1;
            int fade = Mathf.Max(1, (int)(0.004f * R));
            for (int i = 0; i < n; i++)
            {
                int j = o + i;
                if (j < 0 || j >= dst.Length) continue;
                float t = (float)i / R;
                float ph = Tau * freq * t;
                if (fmRatio > 0f) ph += fmIndex * Mathf.Sin(Tau * freq * fmRatio * t);
                float env = steady ? 1f : Mathf.Min(1f, t / Mathf.Max(0.0005f, attack)) * Mathf.Exp(-decay * t) * Mathf.Min(1f, (float)(n - i) / fade);
                dst[j] += Mathf.Sin(ph) * amp * env;
            }
        }

        /// <summary>A pitch glide f0 to f1 (exponential approach, rate k per second) with an exponential decay.</summary>
        public static void Glide(float[] dst, float f0, float f1, float k, float amp, float start, float dur, float decay, float attack = 0.002f)
        {
            int o = (int)(start * R), n = (int)(dur * R);
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = o + i;
                float t = (float)i / R;
                float f = f1 + (f0 - f1) * Mathf.Exp(-k * t);
                ph += Tau * f / R;
                if (j < 0 || j >= dst.Length) continue;
                dst[j] += Mathf.Sin(ph) * amp * Mathf.Min(1f, t / Mathf.Max(0.0005f, attack)) * Mathf.Exp(-decay * t) * Mathf.Min(1f, (float)(n - i) / Mathf.Max(1, (int)(0.004f * R)));
            }
        }

        /// <summary>Harmonic series with 1/k^rolloff amplitudes, phase-accumulated so the pitch can follow a function.</summary>
        public static void Harmonics(float[] dst, Func<float, float> freqAt, int count, float rolloff, float amp, float start = 0f, float dur = -1f)
        {
            int o = (int)(start * R), n = dur < 0f ? dst.Length - o : (int)(dur * R);
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = o + i;
                if (j < 0 || j >= dst.Length) continue;
                float f = freqAt((float)i / R);
                ph += Tau * f / R;
                float s = 0f;
                for (int k = 1; k <= count; k++) if (f * k < R * 0.45f) s += Mathf.Sin(ph * k) / Mathf.Pow(k, rolloff);
                dst[j] += s * amp;
            }
        }

        /// <summary>
        /// Like <see cref="Harmonics"/> over the whole buffer but nudges the pitch by a constant so the total phase is a whole
        /// number of cycles: a sweep that starts and ends on the same pitch then loops with no seam.
        /// </summary>
        public static void LoopHarmonics(float[] dst, Func<float, float> freqAt, int count, float rolloff, float amp)
        {
            double p = 0; int n = dst.Length;
            for (int i = 0; i < n; i++) p += freqAt((float)i / R) / (double)R;
            float df = (float)((Math.Ceiling(p) - p) * R / n);
            Harmonics(dst, t => freqAt(t) + df, count, rolloff, amp);
        }

        /// <summary>Inharmonic bell/metal partials, each decaying at its own rate.</summary>
        public static void Bell(float[] dst, float f, float amp, float start, float decay, float[] ratios = null, float[] amps = null)
        {
            ratios = ratios ?? new[] { 1f, 2.76f, 5.4f, 8.93f };
            amps = amps ?? new[] { 1f, 0.5f, 0.26f, 0.12f };
            for (int k = 0; k < ratios.Length; k++)
                Tone(dst, f * ratios[k], amp * amps[k], start, 3f / Mathf.Max(0.1f, decay), 0.001f, decay * (1f + 0.6f * k));
        }

        /// <summary>A soft chime: fundamental plus a quieter second and third partial that die faster. Pleasant, never shrill.</summary>
        public static void Chime(float[] dst, float f, float amp, float start, float decay, float dur = 1.2f)
        {
            Tone(dst, f, amp, start, dur, 0.004f, decay);
            Tone(dst, f * 2.003f, amp * 0.22f, start, dur, 0.003f, decay * 2.2f);
            Tone(dst, f * 3.99f, amp * 0.06f, start, dur * 0.5f, 0.002f, decay * 4.5f);
        }

        /// <summary>Struck-object body: partials of a material (inharmonic for metal and glass) with slightly random ratios.</summary>
        public static void Modal(float[] dst, Rng r, float f, float amp, float start, float decay, Mat m)
        {
            float[] ratios, amps, dm;
            switch (m)
            {
                case Mat.Metal: ratios = new[] { 1f, 2.32f, 4.25f, 6.63f, 9.38f }; amps = new[] { 1f, 0.6f, 0.45f, 0.28f, 0.15f }; dm = new[] { 1f, 1.3f, 1.8f, 2.4f, 3.2f }; break;
                case Mat.Wood: ratios = new[] { 1f, 1.58f, 2.32f, 3.1f }; amps = new[] { 1f, 0.5f, 0.3f, 0.15f }; dm = new[] { 1f, 1.6f, 2.4f, 3.5f }; break;
                case Mat.Glass: ratios = new[] { 1f, 2.71f, 5.15f, 8.2f }; amps = new[] { 1f, 0.55f, 0.3f, 0.15f }; dm = new[] { 1f, 1.1f, 1.3f, 1.6f }; break;
                default: ratios = new[] { 1f, 2.09f, 3.38f }; amps = new[] { 1f, 0.45f, 0.22f }; dm = new[] { 1f, 1.5f, 2.2f }; break;
            }
            for (int k = 0; k < ratios.Length; k++)
            {
                float ff = f * ratios[k] * (1f + Rnd(r) * 0.012f);
                if (ff > R * 0.42f) continue;
                float d = decay * dm[k];
                float a = amp * amps[k] * Range(r, 0.8f, 1f), dur = Mathf.Min(1.6f, 6f / Mathf.Max(1f, d));
                Tone(dst, ff, a, start, dur, 0.0006f, d);
                // real struck metal and glass have mode pairs a fraction of a percent apart: they beat slowly, which is what makes a ring sound alive
                if ((m == Mat.Metal || m == Mat.Glass) && k < 3) Tone(dst, ff * (1f + Range(r, 0.0025f, 0.007f)), a * 0.55f, start, dur, 0.0006f, d * 1.1f);
            }
        }

        /// <summary>
        /// One strike: a short band-passed noise transient, a modal body, and an optional low thump (Hz) that glides down.
        /// </summary>
        public static void Impact(float[] dst, Rng r, float start, float amp, float body, Mat m, float bright = 2500f, float decay = 20f, float thump = 0f)
        {
            NoiseHit(dst, r, start, 0.03f, amp * 0.8f, F.Band, bright, 1.2f, 160f, 0.0004f);
            Modal(dst, r, body, amp * 0.7f, start, decay, m);
            if (thump > 0f) Glide(dst, thump * 1.6f, thump, 30f, amp * 0.8f, start, 0.18f, 22f, 0.001f);
        }

        /// <summary>A very short noise click (high-passed) at a time.</summary>
        public static void Click(float[] dst, Rng r, float start, float amp, float highpass = 2000f, float ms = 3f)
        {
            var c = White(r, ms / 1000f + 0.004f);
            Filter(c, F.High, highpass);
            Env(c, 0f, 1400f / Mathf.Max(0.5f, ms));
            Add(dst, c, amp, start);
        }

        /// <summary>Noise burst through a filter with an exponential decay.</summary>
        public static void NoiseHit(float[] dst, Rng r, float start, float dur, float amp, F type, float f, float q, float decay, float attack = 0.001f)
        {
            var n = White(r, dur);
            Filter(n, type, f, q);
            Env(n, attack, decay);
            Add(dst, n, amp, start);
        }

        // ------------------------------------------------------------------ round 5 helpers

        /// <summary>
        /// Small-room reverb by convolution with a synthetic impulse response: a few discrete early reflections, then a diffuse tail
        /// made of three noise bands that decay at different rates (lows ring longest, highs die first, like air and soft walls).
        /// Smoother and less metallic than the round 4 comb filters. The result is longer than the input by part of the tail.
        /// <paramref name="size"/> stretches the early reflections (0.6 small booth, 1 room, 2 hall).
        /// </summary>
        public static float[] Reverb(float[] x, float mix, float decaySeconds, float size = 1f)
        {
            var ir = Ir(decaySeconds, size);
            int outLen = x.Length + (int)(Mathf.Min(0.9f, decaySeconds * 0.7f) * R);
            int n = 1; while (n < x.Length + ir.Length) n <<= 1;
            var xr = new double[n]; var xi = new double[n]; var hr = new double[n]; var hi = new double[n];
            for (int i = 0; i < x.Length; i++) xr[i] = x[i];
            for (int i = 0; i < ir.Length; i++) hr[i] = ir[i];
            SfxMeasure.Fft(xr, xi); SfxMeasure.Fft(hr, hi);
            for (int i = 0; i < n; i++)
            {
                double a = xr[i] * hr[i] - xi[i] * hi[i], b = xr[i] * hi[i] + xi[i] * hr[i];
                xr[i] = a; xi[i] = -b;                    // conjugate, so the forward transform below acts as the inverse
            }
            SfxMeasure.Fft(xr, xi);
            var o = new float[outLen];
            float g = mix * 1.3f / n;
            for (int i = 0; i < outLen; i++) o[i] = (i < x.Length ? x[i] : 0f) + (i < n ? (float)xr[i] * g : 0f);
            return o;
        }

        static readonly System.Collections.Generic.Dictionary<string, float[]> irCache = new System.Collections.Generic.Dictionary<string, float[]>();

        static float[] Ir(float rt60, float size)
        {
            string key = R + "/" + (int)(rt60 * 1000f) + "/" + (int)(size * 100f);
            if (irCache.TryGetValue(key, out var hit)) return hit;
            var rng = new Rng((int)(rt60 * 1000f) + 17);
            int len = (int)(Mathf.Min(1.5f, rt60 * 1.15f) * R);
            var ir = new float[len];
            // early reflections: eight taps with falling, alternating-sign gains (walls, floor, ceiling)
            float[] et = { 0.0071f, 0.0113f, 0.0171f, 0.0236f, 0.0309f, 0.0392f, 0.0488f, 0.0601f };
            float[] eg = { 0.9f, -0.75f, 0.62f, -0.5f, 0.42f, -0.33f, 0.27f, -0.2f };
            for (int k = 0; k < et.Length; k++)
            {
                int p = (int)(et[k] * size * R) + (int)(Rnd(rng) * 0.0006f * R);
                if (p > 0 && p < len) ir[p] += eg[k] * 0.55f * Mathf.Pow(0.02f, et[k] * size / Mathf.Max(0.1f, rt60));
            }
            // diffuse tail from 12 ms: the low band lasts 1.2x, the mid band 1x and the high band 0.4x of the reverberation time
            float tail0 = 0.012f * size;
            var lo = White(rng, len / (float)R); Filter(lo, F.Low, 500f, 0.7f);
            var mid = White(rng, len / (float)R); Filter(mid, F.High, 500f, 0.7f); Filter(mid, F.Low, 3000f, 0.7f);
            var hi = White(rng, len / (float)R); Filter(hi, F.High, 3000f, 0.7f); Filter(hi, F.Low, 7000f, 0.7f);
            for (int i = 0; i < len && i < lo.Length; i++)
            {
                float t = (float)i / R - tail0;
                if (t < 0f) continue;
                float fade = Mathf.Clamp01(t / 0.01f);
                float e1 = Mathf.Exp(-6.9f * t / (rt60 * 1.2f)), e2 = Mathf.Exp(-6.9f * t / rt60), e3 = Mathf.Exp(-6.9f * t / (rt60 * 0.4f));
                ir[i] += fade * (lo[i] * e1 * 1.4f + mid[i] * e2 * 1.0f + hi[i] * e3 * 0.5f) * 0.9f;
            }
            double en = 0; for (int i = 0; i < len; i++) en += ir[i] * ir[i];
            float sc = (float)(1.0 / Math.Sqrt(Math.Max(1e-9, en)));
            for (int i = 0; i < len; i++) ir[i] *= sc;
            irCache[key] = ir;
            return ir;
        }

        /// <summary>
        /// One air bubble in water (Minnaert resonance): it rings at about 3.26 / radius Hz, the pitch rises a little as it rings out,
        /// and it decays in about ten milliseconds. Many of these at random radii and times are what flowing and dripping water sounds like.
        /// </summary>
        public static void Bubble(float[] dst, Rng r, float start, float radiusMm, float amp)
        {
            float f0 = 3.26f / (radiusMm * 0.001f);
            if (f0 > R * 0.42f) return;
            float d = 0.043f * f0 + 0.0014f * Mathf.Pow(f0, 1.5f), sigma = 0.1f * d * Range(r, 0.6f, 1.4f);
            int o = (int)(start * R), n = (int)(Mathf.Min(0.25f, 6f / d) * R);
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / R;
                ph += Tau * f0 * (1f + sigma * t) / R;
                int j = o + i;
                if (j < 0 || j >= dst.Length) continue;
                dst[j] += Mathf.Sin(ph) * amp * Mathf.Min(1f, t / 0.0012f) * Mathf.Exp(-d * t);
            }
        }

        /// <summary>
        /// Sparse crackle (sparks, fire, arcs): clicks at random times, each a very short damped sine at a random frequency between
        /// <paramref name="fLo"/> and <paramref name="fHi"/>, with heavy-tailed amplitudes. Band-limited by construction, so it does not hiss.
        /// </summary>
        public static void Crackle(float[] dst, Rng r, float start, float dur, float perSecond, float amp, float fLo, float fHi, float decay = 500f)
        {
            int n = Mathf.Max(1, (int)(dur * perSecond));
            for (int k = 0; k < n; k++)
            {
                float t = start + (float)r.NextDouble() * dur, a = amp * (0.15f + 0.85f * Mathf.Pow((float)r.NextDouble(), 2.5f));
                Tone(dst, Range(r, fLo, fHi), a, t, 6f / decay, 0.0003f, decay * Range(r, 0.7f, 1.4f));
            }
        }

        /// <summary>FM bell: a bright attack that mellows as the modulation index decays (real bells and tines do this); sine body.</summary>
        public static void FmBell(float[] dst, float f, float amp, float start, float dur, float ratio, float index, float decay, float indexDecay)
        {
            int o = (int)(start * R), n = (int)(dur * R);
            for (int i = 0; i < n; i++)
            {
                int j = o + i;
                if (j < 0 || j >= dst.Length) continue;
                float t = (float)i / R;
                float idx = index * Mathf.Exp(-indexDecay * t);
                dst[j] += Mathf.Sin(Tau * f * t + idx * Mathf.Sin(Tau * f * ratio * t)) * amp * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-decay * t) * Mathf.Min(1f, (float)(n - i) / Mathf.Max(1, (int)(0.004f * R)));
            }
        }

        /// <summary>A soft rubbing noise (shoe leather, cloth, a hand on a surface): band-limited pink noise with a smooth hump.</summary>
        public static void Scuff(float[] dst, Rng r, float start, float dur, float amp, float lo, float hi)
        {
            var n = Pink(r, dur);
            Filter(n, F.High, lo, 0.7f); Filter(n, F.Low, hi, 0.7f);
            for (int i = 0; i < n.Length; i++) n[i] *= Mathf.Sin(Mathf.PI * (float)i / n.Length);
            Add(dst, n, amp, start);
        }

        public static float[] Normalize(float[] x, float peak)
        {
            float m = 0.0001f;
            for (int i = 0; i < x.Length; i++) m = Mathf.Max(m, Mathf.Abs(x[i]));
            float g = peak / m;
            for (int i = 0; i < x.Length; i++) x[i] *= g;
            return x;
        }

        /// <summary>Soft-saturates (tanh-like) to tame peaks without hard clipping.</summary>
        public static float[] Soft(float[] x, float drive)
        {
            for (int i = 0; i < x.Length; i++) x[i] = (float)Math.Tanh(x[i] * drive);
            return x;
        }

        /// <summary>Fades at both ends so nothing pops at the clip boundaries (in and out lengths in ms).</summary>
        public static float[] Edges(float[] x, float ms = 2f, float outMs = -1f)
        {
            if (outMs < 0f) outMs = ms;
            int ni = Mathf.Min(x.Length / 4, (int)(ms / 1000f * R)), no = Mathf.Min(x.Length / 4, (int)(outMs / 1000f * R));
            for (int i = 0; i < ni; i++) x[i] *= 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / ni);
            for (int i = 0; i < no; i++) x[x.Length - 1 - i] *= 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / no);
            return x;
        }

        /// <summary>Makes a buffer loop seamlessly by cross-fading its tail into its head (equal power; right for noise).</summary>
        public static float[] MakeLoop(float[] x, float xfadeSeconds)
        {
            int xf = Mathf.Min(x.Length / 3, (int)(xfadeSeconds * R));
            int n = x.Length - xf;
            var o = new float[n];
            Array.Copy(x, o, n);
            for (int i = 0; i < xf; i++)
            {
                float w = (float)i / xf;
                o[i] = x[i] * Mathf.Sin(w * Mathf.PI * 0.5f) + x[n + i] * Mathf.Cos(w * Mathf.PI * 0.5f);
            }
            return o;
        }

        /// <summary>Folds everything after <paramref name="len"/> samples back onto the start: transient tails wrap round, so a loop has no cut-off ring.</summary>
        public static float[] Fold(float[] x, int len)
        {
            var o = new float[len];
            for (int i = 0; i < x.Length; i++) o[i % len] += x[i];
            return o;
        }

        /// <summary>Looping noise of exactly <paramref name="len"/> seconds, shaped by <paramref name="shape"/> (filters), pink by default.</summary>
        public static float[] LoopNoise(Rng r, float len, Action<float[]> shape, float xf = 0.4f, bool pink = true)
        {
            var b = pink ? Pink(r, len + xf) : White(r, len + xf);
            shape?.Invoke(b);
            var o = MakeLoop(b, xf);
            int n = Mathf.Max(8, (int)(len * R));
            if (o.Length != n) Array.Resize(ref o, n);   // rounding: always exactly len seconds
            return o;
        }

        /// <summary>Slowly varying noise (low-passed white), 0 mean, for turbulence and gusts.</summary>
        public static float[] SlowNoise(Rng r, float seconds, float cutoffHz)
        {
            var n = White(r, seconds);
            Filter(n, F.Low, cutoffHz);
            return Normalize(n, 1f);
        }

        /// <summary>Seamless slow modulator in 0..1 with the given length (cutoff in Hz, e.g. 0.5).</summary>
        public static float[] LoopMod(Rng r, float len, float cutoffHz)
        {
            var m = LoopNoise(r, len, x => Filter(x, F.Low, cutoffHz), 0.6f, false);
            Normalize(m, 1f);
            for (int i = 0; i < m.Length; i++) m[i] = 0.5f + 0.5f * m[i];
            return m;
        }

        // ------------------------------------------------------------------ final stage

        /// <summary>
        /// Final polish for every clip: one-shots get a 30 Hz high-pass (no rumble or DC) and loops have their mean removed;
        /// a gentle low-pass at <paramref name="lowpass"/> Hz (0 = none) keeps the top end soft on a headset; one-shots get a short
        /// fade in and a longer fade out; then the level is set to <paramref name="levelDb"/> (RMS of the active part, dBFS)
        /// and a soft-knee limiter keeps the peak below <paramref name="ceiling"/>.
        /// </summary>
        public static float[] Finish(float[] x, bool loop, float lowpass, float levelDb, float lufs = float.NaN, float ceiling = 0.85f)
        {
            if (loop)
            {
                double mean = 0; for (int i = 0; i < x.Length; i++) mean += x[i];
                float m = (float)(mean / Mathf.Max(1, x.Length));
                for (int i = 0; i < x.Length; i++) x[i] -= m;
            }
            else Filter(x, F.High, 30f, 0.707f);
            if (lowpass > 0f && lowpass < R * 0.45f) { if (loop) FilterLoop(x, F.Low, lowpass, 0.6f); else Filter(x, F.Low, lowpass, 0.6f); }
            if (!loop) Edges(x, 1.5f, 14f);
            if (loop) return float.IsNaN(lufs) ? Level(x, levelDb, ceiling) : LevelLufs(x, lufs, ceiling);
            return LevelPeak(x, levelDb, ceiling);
        }

        /// <summary>
        /// One-shot level: scales so the PEAK is <paramref name="ceiling"/> (linear), unless that would make the active RMS exceed
        /// <paramref name="rmsCapDb"/> (sustained sounds). No limiter squashes the transient; a last soft guard only touches samples above 0.97.
        /// </summary>
        public static float[] LevelPeak(float[] x, float rmsCapDb, float ceiling)
        {
            float pk = 1e-6f;
            for (int i = 0; i < x.Length; i++) pk = Mathf.Max(pk, Mathf.Abs(x[i]));
            double s = 0; int c = 0; float th = pk * 0.05f;
            for (int i = 0; i < x.Length; i++) if (Mathf.Abs(x[i]) >= th) { s += x[i] * x[i]; c++; }
            float rms = Mathf.Sqrt((float)(s / Mathf.Max(1, c)));
            float g = Mathf.Min(ceiling / pk, Mathf.Pow(10f, rmsCapDb / 20f) / Mathf.Max(1e-6f, rms));
            for (int i = 0; i < x.Length; i++)
            {
                float v = x[i] * g, a = Mathf.Abs(v);
                if (a > 0.97f) a = 0.97f + 0.03f * (float)Math.Tanh((a - 0.97f) / 0.03f);
                x[i] = v < 0f ? -a : a;
            }
            return x;
        }

        /// <summary>Gain so the integrated loudness (BS.1770 K-weighted, see <see cref="SfxMeasure.Lufs"/>) equals <paramref name="lufs"/>, then soft-limit under the ceiling.</summary>
        public static float[] LevelLufs(float[] x, float lufs, float ceiling)
        {
            float now = SfxMeasure.Lufs(x, R);
            if (now < -90f) return x;
            float g = Mathf.Pow(10f, (lufs - now) / 20f);
            float knee = ceiling * 0.6f, span = ceiling - knee;
            for (int i = 0; i < x.Length; i++)
            {
                float v = x[i] * g, a = Mathf.Abs(v);
                if (a > knee) a = knee + span * (float)Math.Tanh((a - knee) / span);
                x[i] = v < 0f ? -a : a;
            }
            return x;
        }

        /// <summary>Gain so the RMS of the active samples (above 5 percent of the peak) equals <paramref name="db"/> dBFS, then soft-limit under the ceiling.</summary>
        public static float[] Level(float[] x, float db, float ceiling)
        {
            float pk = 1e-6f;
            for (int i = 0; i < x.Length; i++) pk = Mathf.Max(pk, Mathf.Abs(x[i]));
            double s = 0; int c = 0; float th = pk * 0.05f;
            for (int i = 0; i < x.Length; i++) if (Mathf.Abs(x[i]) >= th) { s += x[i] * x[i]; c++; }
            float rms = Mathf.Sqrt((float)(s / Mathf.Max(1, c)));
            float g = Mathf.Pow(10f, db / 20f) / Mathf.Max(1e-6f, rms);
            float knee = ceiling * 0.6f, span = ceiling - knee;
            for (int i = 0; i < x.Length; i++)
            {
                float v = x[i] * g, a = Mathf.Abs(v);
                if (a > knee) a = knee + span * (float)Math.Tanh((a - knee) / span);
                x[i] = v < 0f ? -a : a;
            }
            return x;
        }

        // ------------------------------------------------------------------ catalogue generators: interface




        public static float[] Button(Rng r)
        {
            var b = Buf(0.22f);
            Impact(b, r, 0f, 0.7f, J(r, 520f, 0.06f), Mat.Plastic, 2400f, 55f, 120f);
            Click(b, r, 0.055f, 0.2f, 1800f, 3f);
            Impact(b, r, 0.06f, 0.3f, J(r, 640f, 0.06f), Mat.Plastic, 2000f, 70f);
            return b;
        }

        public static float[] Switch(Rng r)
        {
            var b = Buf(0.34f);
            Impact(b, r, 0f, 0.8f, J(r, 780f, 0.05f), Mat.Plastic, 2600f, 70f, 100f);
            Impact(b, r, 0.075f, 0.55f, J(r, 640f, 0.05f), Mat.Plastic, 2100f, 80f);
            return Reverb(b, 0.1f, 0.15f);
        }

        public static float[] ValveTurn(Rng r)
        {
            // stick-slip squeak, a hiss of air, ratchet ticks, a low metallic grind, and a soft closing thunk
            var b = Buf(1.05f); float f0 = Range(r, 850f, 1250f), rate = Range(r, 8f, 12f);
            float ph = 0f;
            for (int i = 0; i < (int)(0.82f * R); i++)
            {
                float t = (float)i / R;
                float f = f0 * (1f + 0.35f * t + 0.04f * Mathf.Sin(t * 19f));
                ph += Tau * f / R;
                float saw = Mathf.Repeat(t * rate, 1f);
                float slip = Mathf.Pow(Mathf.Min(1f, saw * 5f), 0.7f) * Mathf.Exp(-saw * 2.6f);
                float env = Mathf.Clamp01(t * 14f) * Mathf.Clamp01((0.82f - t) * 7f);
                b[i] += (Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2.01f)) * slip * env * 0.22f;
            }
            var sq = Pink(r, 1.0f); FilterVar(sq, F.Band, t => f0 * (1.4f + 0.3f * t), 6f);
            Shape(sq, t => Mathf.Clamp01(t * 14f) * Mathf.Clamp01((0.82f - t) * 7f) * (0.35f + 0.65f * Mathf.Pow(Mathf.Repeat(t * rate, 1f), 0.6f)));
            Add(b, sq, 0.25f);
            var hiss = Pink(r, 1.0f); Filter(hiss, F.Band, 3000f, 0.6f); Gate(hiss, 0.1f, 0.3f); Add(b, hiss, 0.12f);
            for (float t = 0.03f; t < 0.78f; t += Range(r, 0.08f, 0.11f))
                Impact(b, r, t, 0.14f, J(r, 420f, 0.1f), Mat.Metal, 1800f, 60f);
            var rum = Pink(r, 1.0f); Filter(rum, F.Low, 240f); Shape(rum, t => Mathf.Clamp01(t * 10f) * Mathf.Clamp01((0.82f - t) * 6f) * (0.6f + 0.4f * Mathf.Sin(t * rate * Tau * 0.5f)));
            Add(b, rum, 0.8f);
            Impact(b, r, 0.84f, 0.5f, J(r, 190f, 0.08f), Mat.Metal, 1200f, 22f, 70f);
            return Reverb(b, 0.18f, 0.3f);
        }

        public static float[] Grab(Rng r)
        {
            var b = Buf(0.32f);
            var n = Pink(r, 0.2f); Filter(n, F.Low, J(r, 1100f, 0.1f), 0.7f); Env(n, 0.006f, 24f); Add(b, n, 1.3f);
            Glide(b, J(r, 210f, 0.06f), 150f, 30f, 0.45f, 0f, 0.1f, 35f, 0.003f);
            NoiseHit(b, r, 0.004f, 0.02f, 0.12f, F.Band, 2400f, 1f, 200f);
            return b;
        }

        public static float[] Release(Rng r)
        {
            var b = Buf(0.28f);
            var n = Pink(r, 0.15f); Filter(n, F.Low, J(r, 850f, 0.1f), 0.7f); Env(n, 0.004f, 32f); Add(b, n, 1f);
            Glide(b, J(r, 170f, 0.06f), 120f, 30f, 0.3f, 0f, 0.08f, 40f, 0.002f);
            return b;
        }

        public static float[] Snap(Rng r)
        {
            var b = Buf(0.7f);
            Impact(b, r, 0f, 0.9f, J(r, 880f, 0.06f), Mat.Metal, 2600f, 24f, 110f);
            Click(b, r, 0f, 0.3f, 2500f, 2f);
            return Reverb(b, 0.2f, 0.25f);
        }

        // ------------------------------------------------------------------ objects and impacts

        public static float[] ClinkMetal(Rng r)
        {
            var b = Buf(1.0f);
            for (int hit = 0; hit < 2; hit++)
            {
                float at = hit == 0 ? 0f : Range(r, 0.05f, 0.09f), g = hit == 0 ? 1f : 0.45f;
                Impact(b, r, at, 0.7f * g, Range(r, 1500f, 2600f), Mat.Metal, 3200f, Range(r, 9f, 15f));
            }
            Filter(b, F.High, 450f);
            return Reverb(b, 0.22f, 0.4f);
        }


        public static float[] ConePlace(Rng r)
        {
            var b = Buf(0.55f);
            Impact(b, r, 0f, 0.8f, J(r, 380f, 0.07f), Mat.Plastic, 1300f, 26f, 140f);
            Impact(b, r, Range(r, 0.12f, 0.15f), 0.3f, J(r, 340f, 0.07f), Mat.Plastic, 1200f, 32f);
            return Reverb(b, 0.18f, 0.2f);
        }

        public static float[] Tape(Rng r)
        {
            var b = Pink(r, 0.9f);
            Filter(b, F.Band, 2800f, 0.7f);
            float rasp = Range(r, 70f, 110f);
            var wob = SlowNoise(r, 0.9f, 14f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / R;
                b[i] *= (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * rasp))) * (0.8f + 0.2f * wob[i]) * Mathf.Clamp01(t * 25f) * Mathf.Clamp01((0.9f - t) * 8f);
            }
            return b;
        }

        public static float[] Spray(Rng r)
        {
            var b = Buf(1.4f);
            // can rattle at the start, then a soft broadband psssst (pink, band-limited, no piercing top)
            for (int i = 0; i < 3; i++) Impact(b, r, i * 0.05f, 0.3f, 2200f + i * 250f, Mat.Metal, 2600f, 60f);
            var s = Pink(r, 1.1f); Filter(s, F.High, 1800f); Filter(s, F.Low, 7000f);
            Gate(s, 0.06f, 0.3f);
            var turb = SlowNoise(r, 1.1f, 18f);
            for (int i = 0; i < s.Length; i++) s[i] *= 0.85f + 0.15f * turb[i];
            Add(b, s, 1f, 0.15f);
            return b;
        }




        public static float[] GasHiss(Rng r)
        {
            // a relief valve / pressurised leak: a jet roar in the low mids, a broad hiss above it (pink, band-limited), and slow turbulence
            var b = Pink(r, 1.9f);
            Filter(b, F.Band, 3000f, 0.5f); Filter(b, F.High, 1400f);
            var roar = Pink(r, 1.9f); Filter(roar, F.Band, 850f, 0.7f);
            Add(b, roar, 0.7f);
            var low = Pink(r, 1.9f); Filter(low, F.Band, 380f, 0.6f);
            Add(b, low, 0.25f);
            var turb = SlowNoise(r, 1.9f, 22f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.88f + 0.12f * turb[i];
            Gate(b, 0.07f, 0.7f);
            return Reverb(b, 0.12f, 0.2f);
        }

        // ------------------------------------------------------------------ alarms and beeps (softer than before)


        public static float[] AlarmSirenRise(Rng r)
        {
            var b = Buf(1.8f);
            Harmonics(b, t => 360f * Mathf.Pow(2.2f, Mathf.Min(1f, t / 1.5f)), 5, 1.7f, 0.5f);
            Soft(b, 1.2f);
            return Gate(b, 0.06f, 0.3f);
        }

        public static float[] AlarmSirenFall(Rng r)
        {
            var b = Buf(1.8f);
            Harmonics(b, t => 800f * Mathf.Pow(0.45f, Mathf.Min(1f, t / 1.5f)), 5, 1.7f, 0.5f);
            Soft(b, 1.2f);
            return Gate(b, 0.06f, 0.35f);
        }




        public static float[] RadioClick(Rng r)
        {
            var b = Buf(0.4f);
            NoiseHit(b, r, 0f, 0.04f, 0.8f, F.Band, 1700f, 1f, 90f);
            Impact(b, r, 0f, 0.25f, 900f, Mat.Plastic, 1800f, 80f);
            var tail = Pink(r, 0.25f); Filter(tail, F.Band, 2200f, 0.6f); Env(tail, 0.01f, 14f); Add(b, tail, 0.28f, 0.03f);
            return b;
        }

        public static float[] RadioStatic(Rng r)
        {
            float len = 2f;
            var b = LoopNoise(r, len, x => Filter(x, F.Band, 2200f, 0.5f), 0.2f);
            var am = LoopMod(r, len, 9f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.4f + 0.5f * am[i];
            var cr = Buf(len + 0.05f);
            for (int k = 0; k < 22; k++) Click(cr, r, Range(r, 0f, len), Range(r, 0.2f, 1f), 1500f, 2f);
            Filter(cr, F.Low, 6000f);
            Add(b, Fold(cr, b.Length), 0.6f);
            return b;
        }

        public static float[] ForkliftHorn(Rng r)
        {
            // two detuned reeds (a minor-third-ish pair), a little tremolo, soft attack
            var b = Buf(0.8f); float f = J(r, 400f, 0.012f);
            Harmonics(b, t => f, 6, 1.15f, 0.4f); Harmonics(b, t => f * 1.006f, 6, 1.15f, 0.25f);
            Harmonics(b, t => f * 1.26f, 6, 1.15f, 0.35f); Harmonics(b, t => f * 1.26f * 0.994f, 6, 1.15f, 0.2f);
            Shape(b, t => 1f + 0.04f * Mathf.Sin(t * 38f));
            var n = Pink(r, 0.8f); Filter(n, F.Low, 800f); Add(b, n, 0.08f);
            return Gate(b, 0.04f, 0.1f);
        }



        public static float[] HydraulicWhine(Rng r)
        {
            var b = Buf(1.6f);
            Harmonics(b, t => 520f + 380f * Mathf.Sin(Mathf.Clamp01(t / 1.15f) * Mathf.PI) * 0.8f + 100f * t, 3, 1.8f, 0.3f);
            Tone(b, 82f, 0.45f, 0f, 1.6f, 0.1f, 0f); Tone(b, 164f, 0.15f, 0f, 1.6f, 0.1f, 0f);
            var n = Pink(r, 1.6f); Filter(n, F.Band, 1700f, 0.9f); Add(b, n, 0.3f);
            Shape(b, t => 1f + 0.05f * Mathf.Sin(t * 31f));
            return Gate(b, 0.25f, 0.4f);
        }

        public static float[] PalletDrop(Rng r)
        {
            var b = Buf(1.2f);
            Glide(b, 110f, 44f, 12f, 1f, 0f, 0.6f, 7f, 0.002f);
            var n = Pink(r, 0.3f); Filter(n, F.Low, 550f, 0.7f); Env(n, 0.002f, 16f); Add(b, n, 1.2f);
            for (int k = 0; k < 5; k++) Impact(b, r, Range(r, 0.02f, 0.4f), Range(r, 0.15f, 0.35f), Range(r, 280f, 700f), Mat.Wood, 1500f, 38f);
            return Reverb(b, 0.3f, 0.5f);
        }

        public static float[] MirrorSnap(Rng r)
        {
            var b = Buf(0.5f);
            Click(b, r, 0f, 0.5f, 2500f, 2f);
            Modal(b, r, J(r, 3300f, 0.03f), 0.45f, 0f, 28f, Mat.Glass);
            Glide(b, 260f, 150f, 40f, 0.3f, 0f, 0.05f, 70f, 0.001f);
            return Reverb(b, 0.2f, 0.25f);
        }

        public static float[] LedOn(Rng r)
        {
            var b = Buf(0.2f);
            Impact(b, r, 0f, 0.4f, J(r, 1500f, 0.04f), Mat.Plastic, 2600f, 70f, 110f);
            Chime(b, J(r, 1960f, 0.03f), 0.25f, 0.005f, 50f, 0.1f);
            return b;
        }


        public static float[] SmokePuff(Rng r)
        {
            var b = Pink(r, 1.0f);
            FilterVar(b, F.Low, t => 1500f * Mathf.Exp(-t * 2.2f) + 260f, 0.8f);
            return Env(b, 0.07f, 4f);
        }



        // ------------------------------------------------------------------ ambience and machine loops





        // five scene beds, 12 s each, low level; every tone is a whole number of cycles in 12 s so each bed loops exactly






        // one-shots for the scene ambience director: distant, dull, rare

        public static float[] FarClank(Rng r)
        {
            var b = Buf(1.4f);
            Impact(b, r, 0f, 0.9f, Range(r, 350f, 650f), Mat.Metal, 1800f, 7f, 90f);
            if (r.NextDouble() < 0.5) Impact(b, r, Range(r, 0.12f, 0.3f), 0.4f, Range(r, 300f, 520f), Mat.Metal, 1500f, 8f);
            Filter(b, F.Low, 3200f);
            return Reverb(b, 0.5f, 0.9f);
        }

        public static float[] FarCreak(Rng r)
        {
            var b = Pink(r, 1.3f); float f0 = Range(r, 260f, 420f); float rate = Range(r, 14f, 24f);
            FilterVar(b, F.Band, t => f0 * (1f + 0.5f * Mathf.Sin(Mathf.Clamp01(t / 1.1f) * Mathf.PI)), 7f);
            Shape(b, t => Mathf.Sin(Mathf.Clamp01(t / 1.2f) * Mathf.PI) * (0.4f + 0.6f * Mathf.Pow(Mathf.Repeat(t * rate, 1f), 0.5f)));
            return Reverb(b, 0.3f, 0.7f);
        }

        public static float[] FarSteam(Rng r)
        {
            var b = Pink(r, 1.6f);
            Filter(b, F.Band, 2800f, 0.6f); Filter(b, F.Low, 5000f);
            return Gate(b, 0.5f, 0.8f);
        }

        public static float[] FarRelayTick(Rng r)
        {
            var b = Buf(0.3f);
            Impact(b, r, 0f, 0.7f, Range(r, 900f, 1300f), Mat.Plastic, 2200f, 90f);
            Impact(b, r, Range(r, 0.05f, 0.08f), 0.4f, Range(r, 800f, 1100f), Mat.Plastic, 2000f, 100f);
            return Reverb(b, 0.12f, 0.2f);
        }



        // ------------------------------------------------------------------ doors, steps, misc

        public static float[] DoorOpen(Rng r)
        {
            var b = Buf(1.1f);
            Impact(b, r, 0f, 0.5f, 240f, Mat.Metal, 1800f, 30f, 90f);
            var c = Pink(r, 0.7f); float f0 = Range(r, 380f, 520f);
            FilterVar(c, F.Band, t => f0 + 420f * t / 0.7f, 8f);
            Shape(c, t => (0.5f + 0.5f * Mathf.Sin(t * 22f)) * Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI));
            Add(b, c, 0.5f, 0.12f);
            return Reverb(b, 0.15f, 0.3f);
        }

        public static float[] DoorClose(Rng r)
        {
            var b = Buf(0.9f);
            var c = Pink(r, 0.3f); float f0 = Range(r, 520f, 760f);
            FilterVar(c, F.Band, t => f0 - 300f * t / 0.3f, 8f);
            Shape(c, t => Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * 0.8f);
            Add(b, c, 0.4f);
            Glide(b, 120f, 46f, 16f, 1f, 0.28f, 0.45f, 9f, 0.002f);
            var n = Pink(r, 0.2f); Filter(n, F.Low, 700f, 0.7f); Env(n, 0.002f, 22f); Add(b, n, 0.9f, 0.28f);
            Impact(b, r, 0.36f, 0.4f, 230f, Mat.Metal, 1800f, 30f);
            return Reverb(b, 0.22f, 0.35f);
        }


        public static float[] Ding(Rng r)
        {
            var b = Buf(1.7f); float f = J(r, 1175f, 0.01f);
            Tone(b, f, 0.8f, 0f, 1.6f, 0.002f, 3.2f); Tone(b, f * 2f, 0.22f, 0f, 1.2f, 0.002f, 5f); Tone(b, f * 3.01f, 0.08f, 0f, 1f, 0.002f, 8f);
            Tone(b, f * 4.2f, 0.03f, 0f, 0.5f, 0.002f, 14f);
            return Reverb(b, 0.3f, 0.6f);
        }

        public static float[] ErrorBuzz(Rng r)
        {
            // softened: a low double pulse with six harmonics, rounded by a low-pass; clearly a warning, not a saw buzz
            var b = Buf(0.6f);
            for (int p = 0; p < 2; p++)
            {
                var t = Buf(0.22f);
                Harmonics(t, x => 165f, 6, 1.3f, 0.35f);
                Tone(t, 165f, 0.4f, 0f, 0.22f, 0.001f, 0f, 2f, 0.9f);
                Gate(t, 0.012f, 0.06f);
                Add(b, t, 1f, p * 0.26f);
            }
            Filter(b, F.Low, 1500f);
            return Soft(b, 1.3f);
        }


        public static float[] GaugeTick(Rng r)
        {
            var b = Buf(0.12f);
            Impact(b, r, 0f, 0.6f, J(r, 2200f, 0.05f), Mat.Plastic, 3000f, 120f);
            NoiseHit(b, r, 0f, 0.015f, 0.4f, F.Band, J(r, 2800f, 0.05f), 2f, 160f);
            return b;
        }
        // ================================================================== round 5 generators (replace the round 4 ones of the same name)

        static float Det(Rng r, float f, float pct) => f * (1f + Rnd(r) * pct);

        // interface family: FM bells (bright attack that mellows, like a real struck bell or tine) with a soft sine body and a detuned twin for width
        static void UiNote(float[] b, Rng r, float f, float amp, float start, float decay, float ratio = 2f, float index = 1.1f)
        {
            FmBell(b, f, amp, start, 1.1f, ratio, index, decay, decay * 1.8f);
            FmBell(b, f * 1.0035f, amp * 0.45f, start + 0.002f, 1.0f, ratio, index * 0.8f, decay * 1.1f, decay * 2f);
            Tone(b, f * 0.5f, amp * 0.18f, start, 0.5f, 0.004f, decay * 1.6f);
        }

        public static float[] UiConfirm(Rng r)
        {
            var b = Buf(1.3f); float k = J(r, 1f, 0.004f);
            UiNote(b, r, 784f * k, 0.8f, 0f, 7f);
            UiNote(b, r, 1174.7f * k, 0.9f, 0.09f, 6f);
            Filter(b, F.Low, 6500f);
            return Reverb(b, 0.3f, 0.45f);
        }

        public static float[] UiError(Rng r)
        {
            // two soft falling notes (a minor third), muted: clearly "no" without a buzzer
            var b = Buf(0.7f); float k = J(r, 1f, 0.01f);
            for (int p = 0; p < 2; p++)
            {
                float f = (p == 0 ? 392f : 311.1f) * k;
                FmBell(b, f, 0.8f, p * 0.15f, 0.4f, 1f, 0.9f, 11f, 22f);
                Tone(b, f * 2f, 0.12f, p * 0.15f, 0.2f, 0.004f, 20f);
            }
            Filter(b, F.Low, 2200f);
            return Reverb(b, 0.18f, 0.25f);
        }

        public static float[] UiSelect(Rng r)
        {
            var b = Buf(0.22f);
            FmBell(b, J(r, 1568f, 0.01f), 0.7f, 0f, 0.16f, 3.5f, 1.2f, 32f, 70f);
            NoiseHit(b, r, 0f, 0.008f, 0.06f, F.Band, 3500f, 1f, 350f);
            return Reverb(b, 0.12f, 0.15f);
        }

        public static float[] ThudBox(Rng r)
        {
            // low thump for weight, a wooden knock in the mids so it still reads on small headset drivers, a dull scrape of cardboard
            var b = Buf(0.7f);
            Glide(b, J(r, 170f, 0.08f), 70f, 20f, 0.8f, 0f, 0.35f, 12f, 0.002f);
            var n = Pink(r, 0.2f); Filter(n, F.Band, 600f, 0.6f); Env(n, 0.002f, 24f); Add(b, n, 1.2f);
            Modal(b, r, J(r, 240f, 0.1f), 0.6f, 0f, 24f, Mat.Wood);
            Modal(b, r, J(r, 420f, 0.1f), 0.22f, 0.004f, 34f, Mat.Wood);
            Scuff(b, r, 0f, 0.09f, 0.25f, 500f, 2400f);
            return Reverb(b, 0.18f, 0.25f);
        }

        // ------------------------------------------------------------------ water (Minnaert bubbles: each bubble rings at 3.26/radius Hz with a rising pitch)

        public static float[] WaterDrip(Rng r)
        {
            var b = Buf(0.9f); float rad = Range(r, 2.4f, 5.5f);
            Bubble(b, r, 0.004f, rad, 0.8f);
            if (r.NextDouble() < 0.6) Bubble(b, r, Range(r, 0.035f, 0.09f), rad * Range(r, 0.45f, 0.75f), 0.3f);
            NoiseHit(b, r, 0f, 0.03f, 0.1f, F.Band, 1800f, 0.8f, 150f);
            Modal(b, r, J(r, 900f, 0.2f), 0.05f, 0f, 60f, Mat.Glass);
            return Reverb(b, 0.55f, 0.6f, 1.2f);
        }

        public static float[] WaterPour(Rng r)
        {
            float len = 4f;
            var b = LoopNoise(r, len, x => Filter(x, F.Band, 900f, 0.5f), 0.5f);
            var hi = LoopNoise(r, len, x => Filter(x, F.Band, 2600f, 0.7f), 0.5f);
            var mod = LoopMod(r, len, 5f);
            for (int i = 0; i < b.Length; i++) b[i] = (b[i] * 0.55f + hi[i] * 0.12f) * (0.7f + 0.3f * mod[i]);
            var bub = Buf(len + 0.3f);
            for (int k = 0; k < 520; k++)
            {
                float rad = Mathf.Exp(Range(r, Mathf.Log(1.6f), Mathf.Log(7.5f))), t0 = Range(r, 0f, len);
                Bubble(bub, r, t0, rad, Range(r, 0.04f, 0.2f) * (0.5f + mod[Mathf.Min(mod.Length - 1, (int)(t0 * R))]));
            }
            Add(b, Fold(bub, b.Length), 1f);
            return b;
        }

        public static float[] PumpHum(Rng r)
        {
            // 60 Hz motor and its harmonics (whole cycles in 2.5 s, so the loop is exact), blade-pass tone, water in the casing, a slow load wobble
            float len = 2.5f;
            var b = Buf(len);
            Tone(b, 60f, 0.28f, 0f, len, 0.01f, 0f); Tone(b, 120f, 0.6f, 0f, len, 0.01f, 0f); Tone(b, 180f, 0.4f, 0f, len, 0.01f, 0f);
            Tone(b, 240f, 0.22f, 0f, len, 0.01f, 0f); Tone(b, 300f, 0.1f, 0f, len, 0.01f, 0f); Tone(b, 360f, 0.06f, 0f, len, 0.01f, 0f);
            var vane = Buf(len); Tone(vane, 420f, 0.16f, 0f, len, 0.01f, 0f); Tone(vane, 840f, 0.05f, 0f, len, 0.01f, 0f);
            Shape(vane, t => 0.6f + 0.4f * Mathf.Sin(Tau * t * 4f / len * 3f));
            Add(b, vane, 1f);
            Shape(b, t => 0.92f + 0.08f * Mathf.Sin(Tau * t / len));
            var n = LoopNoise(r, len, x => { Filter(x, F.Band, 700f, 0.5f); }, 0.3f); Add(b, n, 0.5f);
            var n2 = LoopNoise(r, len, x => Filter(x, F.Band, 1700f, 0.8f), 0.3f); Add(b, n2, 0.12f);
            FilterLoop(b, F.High, 70f);
            return b;
        }

        public static float[] PressureTone(Rng r)
        {
            // a steady pump-and-motor note meant to be pitched with src.pitch while pressure rises: a rich low stack (no pure sine), a light noise band
            var b = Buf(1f);
            for (int k = 1; k <= 7; k++) { Tone(b, 190f * k, 0.6f / Mathf.Pow(k, 1.25f), 0f, 1f, 0.01f, 0f); Tone(b, 191f * k, 0.3f / Mathf.Pow(k, 1.25f), 0f, 1f, 0.01f, 0f); }
            var n = LoopNoise(r, 1f, x => Filter(x, F.Band, 900f, 1f), 0.15f); Add(b, n, 0.12f);
            return b;
        }

        // ------------------------------------------------------------------ alarms and beeps

        static void Beep(float[] dst, float f, float amp, float start, float dur, float rel)
        {
            // electronic sounder: a soft square-ish wave (odd harmonics, rolled off) that warms up in pitch, a short ring-down after the gate
            var t = Buf(dur + 0.08f); float ph = 0f;
            for (int i = 0; i < t.Length; i++)
            {
                float tt = (float)i / R, ff = f * (1f + 0.005f * Mathf.Exp(-tt * 80f));
                ph += Tau * ff / R;
                float s = Mathf.Sin(ph) + 0.16f * Mathf.Sin(3f * ph) + 0.04f * Mathf.Sin(5f * ph);
                t[i] = s * amp * 0.7f;
            }
            Gate(t, 0.005f, rel);
            Tone(t, f, amp * 0.12f, dur * 0.97f, 0.08f, 0.001f, 55f);
            Add(dst, t, 1f, start);
        }

        public static float[] GasMonitorBeep(Rng r)
        {
            var b = Buf(0.6f); float f = J(r, 1750f, 0.01f);
            Beep(b, f, 0.8f, 0f, 0.1f, 0.03f); Beep(b, f, 0.8f, 0.16f, 0.1f, 0.03f);
            return Reverb(b, 0.1f, 0.15f);
        }

        public static float[] MonitorAlarm(Rng r)
        {
            var b = Buf(1.7f);
            float[] times = { 0f, 0.14f, 0.28f, 0.56f, 0.70f };
            float[] freqs = { 880f, 880f, 880f, 1175f, 1175f };
            for (int i = 0; i < times.Length; i++) Beep(b, freqs[i], 0.8f, times[i], 0.11f, 0.04f);
            return b;
        }

        public static float[] MeterBeep(Rng r)
        {
            var b = Buf(0.36f);
            Beep(b, J(r, 1000f, 0.01f), 0.8f, 0.01f, 0.2f, 0.05f);
            return Reverb(b, 0.1f, 0.15f);
        }

        public static float[] ForkliftBeepReverse(Rng r)
        {
            // the back-up beeper: one beep a second, a little hollow (a horn-type sounder), the tail wraps round
            var b = Buf(1f + 0.1f);
            Beep(b, 1000f, 0.8f, 0.02f, 0.44f, 0.02f);
            Tone(b, 2000f, 0.05f, 0.02f, 0.44f, 0.01f, 0f);
            return Fold(b, (int)(R * 1f));
        }

        public static float[] FarBeep(Rng r)
        {
            var b = Buf(1.4f);
            Beep(b, 1000f, 0.8f, 0f, 0.3f, 0.03f); Beep(b, 1000f, 0.8f, 0.6f, 0.3f, 0.03f);
            Filter(b, F.Low, 2400f);
            return Reverb(b, 0.45f, 0.9f, 2f);
        }

        public static float[] AlarmSiren(Rng r)
        {
            // a wailing electronic siren: pitch rises and falls once (it ends where it began, so it loops), a slow vibrato, a detuned twin, a broad resonance
            var b = Buf(3f);
            LoopHarmonics(b, t => 450f + 320f * (0.5f - 0.5f * Mathf.Cos(Tau * t / 3f)), 6, 1.5f, 0.5f);
            var b2 = Buf(3f);
            LoopHarmonics(b2, t => 453f * 1.007f + 320f * 1.007f * (0.5f - 0.5f * Mathf.Cos(Tau * t / 3f)), 4, 1.8f, 0.3f);
            Add(b, b2, 1f);
            var res = Buf(3f); Add(res, b, 1f); FilterLoop(res, F.Band, 1100f, 0.9f); Add(b, res, 0.6f);
            Soft(b, 1.1f);
            return b;
        }

        // ------------------------------------------------------------------ engines and machines

        public static float[] ForkliftEngineLoop(Rng r)
        {
            // low diesel idle: irregular firing, each pulse a thump plus an exhaust puff plus a tick of injector knock; the tail wraps round the loop
            float len = 2f; var b = Buf(len + 0.25f);
            float rate = 22f;
            for (int p = 0; p < (int)(len * rate); p++)
            {
                float t = p / rate + Rnd(r) * 0.004f, a = 0.8f + 0.2f * Rnd(r);
                Tone(b, 64f, a * 0.5f, t, 0.1f, 0.004f, 34f); Tone(b, 128f, a * 0.5f, t, 0.07f, 0.003f, 45f); Tone(b, 192f, a * 0.3f, t, 0.06f, 0.003f, 55f);
                Tone(b, 256f, a * 0.14f, t, 0.05f, 0.003f, 70f);
                NoiseHit(b, r, t, 0.06f, a * 0.45f, F.Band, 520f, 0.8f, 55f, 0.003f);
                NoiseHit(b, r, t + 0.002f, 0.03f, a * 0.28f, F.Band, 1500f, 1.2f, 140f);   // injector knock
            }
            var o = Fold(b, (int)(len * R));
            var rum = LoopNoise(r, len, x => Filter(x, F.Band, 350f, 0.5f), 0.3f); Add(o, rum, 0.3f);
            Tone(o, 88f, 0.08f, 0f, len, 0.01f, 0f);
            FilterLoop(o, F.High, 55f);
            FilterLoop(o, F.Low, 1800f);
            return o;
        }

        public static float[] FarThud(Rng r)
        {
            var b = Buf(1.0f);
            Glide(b, J(r, 120f, 0.1f), 60f, 14f, 0.8f, 0f, 0.6f, 8f, 0.004f);
            var n = Pink(r, 0.3f); Filter(n, F.Band, 260f, 0.5f); Env(n, 0.004f, 14f); Add(b, n, 1.1f);
            Modal(b, r, J(r, 190f, 0.1f), 0.3f, 0f, 14f, Mat.Wood);
            return Reverb(b, 0.45f, 0.9f, 2f);
        }

        public static float[] HvacLoop(Rng r)
        {
            float len = 5f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Low, 900f, 0.6f); Filter(x, F.High, 70f); }, 0.4f);
            var air = LoopNoise(r, len, x => Filter(x, F.Band, 1700f, 0.5f), 0.4f); var am = LoopMod(r, len, 0.4f);
            for (int i = 0; i < air.Length; i++) air[i] *= 0.3f * (0.7f + 0.3f * am[i]);
            Add(b, air, 1f);
            var t0 = Buf(len);
            Tone(t0, 120f, 0.1f, 0f, len, 0.01f, 0f); Tone(t0, 240f, 0.06f, 0f, len, 0.01f, 0f); Tone(t0, 360f, 0.03f, 0f, len, 0.01f, 0f);
            Add(b, t0, 1f);
            return b;
        }

        public static float[] PlantDroneLoop(Rng r)
        {
            float len = 8f; var b = Buf(len);
            Tone(b, 55f, 0.2f, 0f, len, 0.01f, 0f); Tone(b, 55.5f, 0.15f, 0f, len, 0.01f, 0f);
            Tone(b, 110f, 0.3f, 0f, len, 0.01f, 0f); Tone(b, 165f, 0.22f, 0f, len, 0.01f, 0f); Tone(b, 220f, 0.14f, 0f, len, 0.01f, 0f); Tone(b, 330f, 0.06f, 0f, len, 0.01f, 0f);
            Shape(b, t => 0.85f + 0.15f * Mathf.Sin(Tau * t / len * 4f));
            var rum = LoopNoise(r, len, x => { Filter(x, F.Band, 260f, 0.5f); }, 0.8f); Add(b, rum, 0.5f);
            var air = LoopNoise(r, len, x => Filter(x, F.Band, 1100f, 0.6f), 0.8f); Add(b, air, 0.18f);
            FilterLoop(b, F.High, 60f);
            return b;
        }

        public static float[] FlareRoarLoop(Rng r)
        {
            // a gas flare heard from a distance: a broad breathing roar (mids), a lower rumble, and a fine crackle
            float len = 5f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Band, 500f, 0.45f); }, 0.5f);
            var turb = LoopMod(r, len, 6f);
            var rum = LoopNoise(r, len, x => Filter(x, F.Low, 180f), 0.5f);
            var hi = LoopNoise(r, len, x => Filter(x, F.Band, 1600f, 0.6f), 0.5f);
            for (int i = 0; i < b.Length; i++) b[i] = b[i] * (0.45f + 0.55f * turb[i]) + rum[i] * 0.5f + hi[i] * 0.25f * turb[i];
            var cr = Buf(len + 0.05f);
            Crackle(cr, r, 0f, len, 70f, 0.25f, 1200f, 3500f, 600f);
            Add(b, Fold(cr, b.Length), 1f);
            FilterLoop(b, F.High, 60f);
            return b;
        }

        public static float[] WindLoop(Rng r)
        {
            float len = 8f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Band, 450f, 0.5f); }, 0.7f);
            var lo = LoopNoise(r, len, x => Filter(x, F.Low, 300f, 0.7f), 0.7f);
            var whistle = LoopNoise(r, len, x => Filter(x, F.Band, 700f, 8f), 0.7f);
            var gust = LoopMod(r, len, 0.35f);
            for (int i = 0; i < b.Length; i++) b[i] = (b[i] * 1.2f + lo[i] * 0.4f + whistle[i] * 0.45f * gust[i]) * (0.35f + 0.65f * gust[i]);
            FilterLoop(b, F.High, 60f);
            return b;
        }

        // five scene beds, 12 s each; every tone is a whole number of cycles in 12 s (loops exactly); balanced across the mids so small headset drivers can show them

        public static float[] AmbLobby(Rng r)
        {
            float len = 12f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Low, 900f, 0.7f); Filter(x, F.High, 70f); }, 0.8f);
            var air = LoopNoise(r, len, x => Filter(x, F.Band, 1500f, 0.5f), 0.8f);
            var sw = LoopMod(r, len, 0.25f);
            for (int i = 0; i < b.Length; i++) b[i] = (b[i] + air[i] * 0.3f) * (0.75f + 0.25f * sw[i]);
            // distant murmur of people: syllable-rate modulated noise through vowel-like formants (far and soft, so it never reads as speech)
            for (int v = 0; v < 4; v++)
            {
                float f1 = Range(r, 350f, 700f), f2 = Range(r, 900f, 1800f), rate = Range(r, 2.5f, 4.5f);
                var g = LoopNoise(r, len, x => { Filter(x, F.Band, f1, 3f); }, 0.8f);
                var g2 = LoopNoise(r, len, x => { Filter(x, F.Band, f2, 3f); }, 0.8f);
                var m1 = LoopMod(r, len, rate); var m2 = LoopMod(r, len, 0.4f);
                for (int i = 0; i < g.Length; i++) b[i] += (g[i] + g2[i] * 0.5f) * 0.11f * Mathf.Pow(m1[i], 2f) * m2[i];
            }
            var h = Buf(len); Tone(h, 60f, 0.03f, 0f, len, 0.01f, 0f); Tone(h, 120f, 0.04f, 0f, len, 0.01f, 0f); Tone(h, 180f, 0.02f, 0f, len, 0.01f, 0f);
            Add(b, h, 1f);
            FilterLoop(b, F.High, 70f);
            return b;
        }

        public static float[] AmbHydro(Rng r)
        {
            float len = 12f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Low, 800f, 0.7f); Filter(x, F.High, 70f); }, 0.8f);
            var flow = LoopNoise(r, len, x => Filter(x, F.Band, 1300f, 0.6f), 0.8f); var fm = LoopMod(r, len, 0.8f);
            for (int i = 0; i < flow.Length; i++) flow[i] *= 0.4f * (0.6f + 0.4f * fm[i]);
            Add(b, flow, 1f);
            var h = Buf(len);
            Tone(h, 120f, 0.1f, 0f, len, 0.01f, 0f); Tone(h, 120.25f, 0.08f, 0f, len, 0.01f, 0f); Tone(h, 180f, 0.06f, 0f, len, 0.01f, 0f); Tone(h, 240f, 0.04f, 0f, len, 0.01f, 0f);
            Add(b, h, 1f);
            var bub = Buf(len + 0.4f);
            for (int k = 0; k < 60; k++) Bubble(bub, r, Range(r, 0f, len), Mathf.Exp(Range(r, Mathf.Log(2f), Mathf.Log(8f))), Range(r, 0.03f, 0.14f));
            Add(b, Fold(bub, b.Length), 1f);
            FilterLoop(b, F.High, 70f);
            return b;
        }

        public static float[] AmbRefinery(Rng r)
        {
            float len = 12f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Band, 380f, 0.4f); }, 0.8f);                                  // open-air hush
            var wind = LoopNoise(r, len, x => Filter(x, F.Band, 700f, 0.7f), 0.8f); var wm = LoopMod(r, len, 0.18f);   // slow gusts
            for (int i = 0; i < wind.Length; i++) wind[i] *= 0.7f * wm[i] * wm[i];
            Add(b, wind, 1f);
            var fl = LoopNoise(r, len, x => Filter(x, F.Band, 520f, 0.5f), 0.8f); var fm = LoopMod(r, len, 0.5f);       // distant flare
            for (int i = 0; i < fl.Length; i++) fl[i] *= 0.5f * (0.5f + 0.5f * fm[i]);
            Add(b, fl, 1f);
            var h = Buf(len); Tone(h, 110f, 0.06f, 0f, len, 0.01f, 0f); Tone(h, 165f, 0.035f, 0f, len, 0.01f, 0f); Tone(h, 82.5f, 0.05f, 0f, len, 0.01f, 0f);
            Add(b, h, 1f);                                                                                              // plant hum
            var wh = LoopNoise(r, len, x => Filter(x, F.Band, 640f, 10f), 0.8f); var wm2 = LoopMod(r, len, 0.3f);
            for (int i = 0; i < wh.Length; i++) wh[i] *= 0.2f * wm2[i] * wm2[i];
            Add(b, wh, 1f);
            var st = LoopNoise(r, len, x => Filter(x, F.Band, 3000f, 0.6f), 0.8f); var sm = LoopMod(r, len, 0.2f);       // faint steam
            for (int i = 0; i < st.Length; i++) st[i] *= 0.06f * sm[i] * sm[i];
            Add(b, st, 1f);
            FilterLoop(b, F.High, 60f);
            return b;
        }

        public static float[] AmbWarehouse(Rng r)
        {
            float len = 12f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Low, 700f, 0.7f); Filter(x, F.High, 70f); }, 0.8f);           // big hall tone
            var air = LoopNoise(r, len, x => Filter(x, F.Band, 1100f, 0.4f), 0.8f);
            var m = LoopMod(r, len, 0.2f);
            for (int i = 0; i < b.Length; i++) b[i] = (b[i] + air[i] * 0.3f) * (0.7f + 0.3f * m[i]);
            var h = Buf(len); Tone(h, 100f, 0.06f, 0f, len, 0.01f, 0f); Tone(h, 200f, 0.04f, 0f, len, 0.01f, 0f); Tone(h, 300f, 0.025f, 0f, len, 0.01f, 0f);
            Add(b, h, 1f);                                                                                              // transformer / light ballast hum
            var dock = LoopNoise(r, len, x => Filter(x, F.Band, 480f, 3f), 0.8f); var dm = LoopMod(r, len, 0.25f);   // distant machinery
            for (int i = 0; i < dock.Length; i++) dock[i] *= 0.2f * dm[i];
            Add(b, dock, 1f);
            var vent = LoopNoise(r, len, x => Filter(x, F.Band, 2200f, 0.6f), 0.8f); var vm = LoopMod(r, len, 0.35f);   // overhead ventilation
            for (int i = 0; i < vent.Length; i++) vent[i] *= 0.07f * vm[i];
            Add(b, vent, 1f);
            FilterLoop(b, F.High, 70f);
            return b;
        }

        public static float[] AmbBench(Rng r)
        {
            float len = 12f;
            var b = LoopNoise(r, len, x => { Filter(x, F.Low, 1600f, 0.6f); Filter(x, F.High, 120f); }, 0.8f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.5f;
            var h = Buf(len);
            Tone(h, 120f, 0.1f, 0f, len, 0.01f, 0f); Tone(h, 180f, 0.05f, 0f, len, 0.01f, 0f); Tone(h, 240f, 0.04f, 0f, len, 0.01f, 0f); Tone(h, 360f, 0.02f, 0f, len, 0.01f, 0f);
            Add(b, h, 1f);                                                                                              // mains hum from supplies
            var fan = LoopNoise(r, len, x => Filter(x, F.Band, 900f, 0.5f), 0.8f);
            Add(b, fan, 0.3f);
            var fan2 = LoopNoise(r, len, x => Filter(x, F.Band, 2400f, 0.7f), 0.8f); var f2m = LoopMod(r, len, 0.3f);
            for (int i = 0; i < fan2.Length; i++) fan2[i] *= 0.06f * (0.5f + 0.5f * f2m[i]);
            Add(b, fan2, 1f);
            FilterLoop(b, F.High, 90f);
            return b;
        }

        // ------------------------------------------------------------------ electrical

        public static float[] SparkPop(Rng r)
        {
            var b = Buf(0.5f);
            Glide(b, 300f, 130f, 40f, 0.35f, 0f, 0.08f, 40f, 0.001f);
            NoiseHit(b, r, 0f, 0.15f, 0.3f, F.Band, 1900f, 0.6f, 26f, 0.001f);
            Crackle(b, r, 0f, 0.22f, 60f, 0.7f, 1400f, 4200f, 500f);
            return Reverb(b, 0.2f, 0.25f);
        }

        public static float[] EsdClick(Rng r)
        {
            var b = Buf(0.3f);
            NoiseHit(b, r, 0f, 0.02f, 0.7f, F.Band, 2500f, 0.8f, 220f, 0.0004f);
            Tone(b, 2600f, 0.12f, 0f, 0.05f, 0.0004f, 130f);
            Glide(b, 320f, 160f, 40f, 0.3f, 0f, 0.05f, 80f, 0.001f);
            Crackle(b, r, 0.005f, 0.07f, 90f, 0.25f, 1800f, 3600f, 700f);
            return Reverb(b, 0.12f, 0.15f);
        }

        /// <summary>An electric arc / short circuit zap: a dense crackle that thins out, a low "bwomp" and a falling whine.</summary>
        public static float[] Zap(Rng r)
        {
            var b = Buf(0.7f);
            Crackle(b, r, 0f, 0.18f, 380f, 0.7f, 1300f, 4200f, 450f);
            Crackle(b, r, 0.12f, 0.3f, 90f, 0.4f, 1200f, 3400f, 450f);
            Glide(b, 240f, 70f, 14f, 0.7f, 0f, 0.3f, 12f, 0.002f);
            Glide(b, 1800f, 500f, 9f, 0.18f, 0f, 0.3f, 12f, 0.004f);
            NoiseHit(b, r, 0f, 0.2f, 0.25f, F.Band, 1700f, 0.5f, 14f, 0.002f);
            return Reverb(b, 0.25f, 0.3f);
        }

        /// <summary>A live bench: transformer hum (120 Hz and harmonics) with a slow flutter and sparse crackle.</summary>
        public static float[] ElectricBuzz(Rng r)
        {
            float len = 2.5f; var b = Buf(len);
            for (int k = 1; k <= 8; k++) Tone(b, 120f * k, 0.5f / Mathf.Pow(k, 0.9f), 0f, len, 0.01f, 0f);
            Shape(b, t => 0.85f + 0.15f * Mathf.Sin(Tau * t / len * 3f));
            var n = LoopNoise(r, len, x => Filter(x, F.Band, 1500f, 0.7f), 0.3f); Add(b, n, 0.1f);
            var cr = Buf(len + 0.05f); Crackle(cr, r, 0f, len, 7f, 0.5f, 1500f, 3500f, 500f);
            Add(b, Fold(cr, b.Length), 1f);
            FilterLoop(b, F.High, 80f);
            return b;
        }

        // ------------------------------------------------------------------ footsteps (concrete and gravel use recordings; these three are synthesized)

        public static float[] Footstep(Rng r, Surface s)
        {
            var b = Buf(0.55f); float toe = Range(r, 0.07f, 0.11f), w = Range(r, 0.85f, 1.15f);
            switch (s)
            {
                case Surface.Concrete:
                    Glide(b, J(r, 130f, 0.1f), 70f, 30f, 0.7f * w, 0f, 0.12f, 38f, 0.002f);
                    NoiseHit(b, r, 0f, 0.1f, 0.5f * w, F.Band, J(r, 900f, 0.15f), 0.6f, 50f, 0.002f);
                    NoiseHit(b, r, 0f, 0.03f, 0.3f, F.Band, J(r, 2600f, 0.1f), 0.8f, 150f, 0.0005f);
                    Scuff(b, r, 0f, 0.05f, 0.18f, 1200f, 4000f);
                    Glide(b, 140f, 85f, 30f, 0.3f, toe, 0.08f, 45f, 0.002f);
                    NoiseHit(b, r, toe, 0.07f, 0.28f, F.Band, 1000f, 0.6f, 60f, 0.002f);
                    break;
                case Surface.Metal:
                    // a steel grating or plate: thump, ringing plate, slap, then the toe
                    Glide(b, 140f, 80f, 30f, 0.4f * w, 0f, 0.1f, 40f, 0.002f);
                    Impact(b, r, 0f, 0.75f * w, J(r, 420f, 0.1f), Mat.Metal, 2400f, 20f, 0f);
                    Modal(b, r, J(r, 870f, 0.1f), 0.18f, 0.001f, 26f, Mat.Metal);
                    Scuff(b, r, 0f, 0.05f, 0.2f, 900f, 3200f);
                    Impact(b, r, toe, 0.35f * w, J(r, 470f, 0.1f), Mat.Metal, 2200f, 14f);
                    break;
                case Surface.Tile:
                    // hard shoe on tile: a clear heel click over a hollow floor resonance
                    NoiseHit(b, r, 0f, 0.012f, 0.55f * w, F.Band, J(r, 2200f, 0.1f), 1f, 260f, 0.0004f);
                    Modal(b, r, J(r, 1250f, 0.06f), 0.4f * w, 0f, 90f, Mat.Glass);
                    Glide(b, 170f, 95f, 30f, 0.45f * w, 0f, 0.1f, 36f, 0.002f);
                    Tone(b, J(r, 220f, 0.05f), 0.14f, 0f, 0.2f, 0.004f, 22f);
                    Scuff(b, r, 0f, 0.04f, 0.14f, 1500f, 4500f);
                    NoiseHit(b, r, toe, 0.01f, 0.28f, F.Band, 2000f, 1f, 280f, 0.0004f);
                    Modal(b, r, J(r, 1350f, 0.06f), 0.18f, toe, 100f, Mat.Glass);
                    Glide(b, 180f, 105f, 30f, 0.25f, toe, 0.08f, 40f, 0.002f);
                    break;
                case Surface.Gravel:
                    for (int k = 0; k < 40; k++)
                        NoiseHit(b, r, Range(r, 0f, 0.26f), 0.02f, Range(r, 0.1f, 0.45f), F.Band, Range(r, 1500f, 4500f), 2f, 150f, 0.0005f);
                    NoiseHit(b, r, 0f, 0.15f, 0.5f, F.Band, 800f, 0.6f, 22f, 0.003f);
                    Glide(b, 120f, 75f, 30f, 0.25f, 0f, 0.09f, 45f, 0.003f);
                    break;
                default: // Wood
                    Modal(b, r, J(r, 190f, 0.08f), 0.7f * w, 0f, 26f, Mat.Wood);
                    Modal(b, r, J(r, 410f, 0.08f), 0.25f, 0f, 36f, Mat.Wood);
                    NoiseHit(b, r, 0f, 0.1f, 0.4f, F.Band, J(r, 600f, 0.12f), 1.2f, 38f, 0.002f);
                    Glide(b, 140f, 85f, 30f, 0.35f * w, 0f, 0.1f, 36f, 0.002f);
                    Scuff(b, r, 0f, 0.05f, 0.16f, 800f, 3000f);
                    Modal(b, r, J(r, 215f, 0.08f), 0.3f, toe, 32f, Mat.Wood);
                    if (r.NextDouble() < 0.25)
                    {
                        var cr = Pink(r, 0.18f); float cf = Range(r, 380f, 520f);
                        FilterVar(cr, F.Band, t => cf * (1f + t * 1.2f), 8f);
                        Shape(cr, t => Mathf.Sin(Mathf.Clamp01(t / 0.18f) * Mathf.PI));
                        Add(b, cr, 0.16f, 0.1f);
                    }
                    break;
            }
            return Reverb(b, 0.14f, 0.25f);
        }

    }
}
