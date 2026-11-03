using System;
using UnityEngine;
using Rng = System.Random;

namespace PittBradford.XR
{
    /// <summary>
    /// Layered sound synthesis used by <see cref="Sfx"/> for every catalogue name that has no recorded sample.
    /// Not bare sine beeps: envelopes (attack and exponential decay), band-passed and swept noise, FM and inharmonic
    /// partials, pitch drops, tiny room reverb, and a different random variation per clip.
    /// All buffers are mono floats at <see cref="R"/> samples per second (set by the caller before generating).
    /// </summary>
    public static class Synth
    {
        public static int R = 32000;
        const float Tau = Mathf.PI * 2f;

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

        /// <summary>Attack, sustain, release gate over the whole buffer.</summary>
        public static float[] Gate(float[] x, float attack, float release)
        {
            float len = (float)x.Length / R;
            for (int i = 0; i < x.Length; i++)
            {
                float t = (float)i / R;
                x[i] *= Mathf.Clamp01(t / Mathf.Max(0.001f, attack)) * Mathf.Clamp01((len - t) / Mathf.Max(0.001f, release));
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

        /// <summary>A decaying sine (with optional FM) added at <paramref name="start"/> seconds.</summary>
        public static void Tone(float[] dst, float freq, float amp, float start, float dur, float attack, float decay, float fmRatio = 0f, float fmIndex = 0f)
        {
            int o = (int)(start * R), n = (int)(dur * R);
            for (int i = 0; i < n; i++)
            {
                int j = o + i;
                if (j < 0 || j >= dst.Length) continue;
                float t = (float)i / R;
                float ph = Tau * freq * t;
                if (fmRatio > 0f) ph += fmIndex * Mathf.Sin(Tau * freq * fmRatio * t);
                dst[j] += Mathf.Sin(ph) * amp * Mathf.Min(1f, t / Mathf.Max(0.0005f, attack)) * Mathf.Exp(-decay * t);
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
                dst[j] += Mathf.Sin(ph) * amp * Mathf.Min(1f, t / Mathf.Max(0.0005f, attack)) * Mathf.Exp(-decay * t);
            }
        }

        /// <summary>Harmonic series with 1/k amplitudes, phase-accumulated so the pitch can follow a function.</summary>
        public static void Harmonics(float[] dst, Func<float, float> freqAt, int count, float rolloff, float amp, float start = 0f, float dur = -1f)
        {
            int o = (int)(start * R), n = dur < 0f ? dst.Length - o : (int)(dur * R);
            float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = o + i;
                if (j < 0 || j >= dst.Length) continue;
                ph += Tau * freqAt((float)i / R) / R;
                float s = 0f;
                for (int k = 1; k <= count; k++) s += Mathf.Sin(ph * k) / Mathf.Pow(k, rolloff);
                dst[j] += s * amp;
            }
        }

        /// <summary>Inharmonic bell/metal partials, each decaying at its own rate.</summary>
        public static void Bell(float[] dst, float f, float amp, float start, float decay, float[] ratios = null, float[] amps = null)
        {
            ratios = ratios ?? new[] { 1f, 2.76f, 5.4f, 8.93f };
            amps = amps ?? new[] { 1f, 0.5f, 0.26f, 0.12f };
            for (int k = 0; k < ratios.Length; k++)
                Tone(dst, f * ratios[k], amp * amps[k], start, 3f / Mathf.Max(0.1f, decay) , 0.001f, decay * (1f + 0.6f * k));
        }

        /// <summary>A very short noise click (high-passed) at a time.</summary>
        public static void Click(float[] dst, Rng r, float start, float amp, float highpass = 2000f, float ms = 3f)
        {
            var c = White(r, ms / 1000f + 0.004f);
            Filter(c, F.High, highpass);
            Env(c, 0f, 1400f / Mathf.Max(0.5f, ms));
            Add(dst, c, amp, start);
        }

        /// <summary>Noise burst through a band-pass with an exponential decay.</summary>
        public static void NoiseHit(float[] dst, Rng r, float start, float dur, float amp, F type, float f, float q, float decay, float attack = 0.001f)
        {
            var n = White(r, dur);
            Filter(n, type, f, q);
            Env(n, attack, decay);
            Add(dst, n, amp, start);
        }

        /// <summary>Small room: four comb filters plus a mix. The buffer should already include the tail length.</summary>
        public static float[] Reverb(float[] x, float mix, float decaySeconds)
        {
            float[] delays = { 0.0297f, 0.0371f, 0.0411f, 0.0437f };
            var o = (float[])x.Clone();
            foreach (var d in delays)
            {
                int dn = (int)(d * R);
                float g = Mathf.Pow(0.001f, d / Mathf.Max(0.05f, decaySeconds));
                var buf = new float[dn];
                int p = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    float y = x[i] + buf[p] * g;
                    buf[p] = y;
                    p = (p + 1) % dn;
                    o[i] += y * mix * 0.25f;
                }
            }
            return o;
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

        /// <summary>2 ms fades at both ends so nothing pops at the clip boundaries.</summary>
        public static float[] Edges(float[] x, float ms = 2f)
        {
            int n = Mathf.Min(x.Length / 2, (int)(ms / 1000f * R));
            for (int i = 0; i < n; i++)
            {
                float w = (float)i / n;
                x[i] *= w; x[x.Length - 1 - i] *= w;
            }
            return x;
        }

        /// <summary>Makes a buffer loop seamlessly by cross-fading its tail into its head (equal power).</summary>
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

        /// <summary>Slowly varying noise (low-passed white), 0 mean, for turbulence and gusts.</summary>
        public static float[] SlowNoise(Rng r, float seconds, float cutoffHz)
        {
            var n = White(r, seconds);
            Filter(n, F.Low, cutoffHz);
            return Normalize(n, 1f);
        }

        // ------------------------------------------------------------------ catalogue generators

        public static float[] UiConfirm(Rng r)
        {
            var b = Buf(0.9f); float f = J(r, 1318f, 0.01f);
            Bell(b, f, 0.6f, 0f, 7f); Bell(b, f * 1.335f, 0.7f, 0.09f, 6f);
            return Reverb(b, 0.35f, 0.35f);
        }

        public static float[] UiError(Rng r)
        {
            var b = Buf(0.45f); float f = J(r, 196f, 0.02f);
            for (int p = 0; p < 2; p++)
            {
                var t = Buf(0.2f);
                Harmonics(t, x => f * (p == 0 ? 1f : 0.94f), 7, 1f, 0.3f);
                Gate(t, 0.006f, 0.05f);
                Add(b, t, 1f, p * 0.17f);
            }
            Filter(b, F.Low, 1800f);
            return b;
        }

        public static float[] UiSelect(Rng r)
        {
            var b = Buf(0.15f); float f = J(r, 1900f, 0.03f);
            Tone(b, f, 0.7f, 0f, 0.12f, 0.001f, 45f); Tone(b, f * 2f, 0.15f, 0f, 0.06f, 0.001f, 80f);
            Click(b, r, 0f, 0.15f, 3000f, 2f);
            return b;
        }

        public static float[] Button(Rng r)
        {
            var b = Buf(0.18f);
            Tone(b, J(r, 150f, 0.05f), 0.7f, 0f, 0.08f, 0.001f, 55f);
            Click(b, r, 0f, 0.6f, 1800f, 4f);
            Click(b, r, 0.06f, 0.3f, 2400f, 3f);
            Tone(b, 1900f, 0.12f, 0f, 0.05f, 0.001f, 90f);
            return b;
        }

        public static float[] Switch(Rng r)
        {
            var b = Buf(0.3f);
            NoiseHit(b, r, 0f, 0.05f, 0.8f, F.Band, J(r, 2600f, 0.06f), 3f, 120f);
            Tone(b, 1700f, 0.25f, 0f, 0.04f, 0.0005f, 110f);
            Tone(b, 110f, 0.4f, 0f, 0.06f, 0.001f, 60f);
            NoiseHit(b, r, 0.075f, 0.05f, 0.55f, F.Band, J(r, 2100f, 0.06f), 3f, 130f);
            Tone(b, 1300f, 0.18f, 0.075f, 0.04f, 0.0005f, 110f);
            return Reverb(b, 0.12f, 0.15f);
        }

        public static float[] ValveTurn(Rng r)
        {
            var b = Buf(0.9f);
            var sq = White(r, 0.9f);
            FilterVar(sq, F.Band, t => 900f + 700f * Mathf.Sin(t * 7f) + 500f * t, 9f);
            float chop = Range(r, 11f, 15f);
            Shape(sq, t => (0.5f + 0.5f * Mathf.Sin(t * chop)) * Mathf.Clamp01(t * 12f) * Mathf.Clamp01((0.9f - t) * 6f));
            Add(b, sq, 0.6f);
            for (float t = 0.02f; t < 0.8f; t += Range(r, 0.07f, 0.1f))
            {
                Click(b, r, t, 0.35f, 1500f, 3f);
                Tone(b, J(r, 380f, 0.1f), 0.12f, t, 0.03f, 0.001f, 90f);
            }
            var rum = White(r, 0.9f); Filter(rum, F.Low, 260f); Gate(rum, 0.08f, 0.15f);
            Add(b, rum, 1.2f);
            return b;
        }

        public static float[] Grab(Rng r)
        {
            var b = Buf(0.3f);
            NoiseHit(b, r, 0f, 0.2f, 0.8f, F.Low, J(r, 900f, 0.1f), 0.7f, 28f, 0.004f);
            Tone(b, J(r, 170f, 0.06f), 0.4f, 0f, 0.1f, 0.003f, 40f);
            Click(b, r, 0.004f, 0.2f, 2500f, 2f);
            return b;
        }

        public static float[] Release(Rng r)
        {
            var b = Buf(0.25f);
            NoiseHit(b, r, 0f, 0.15f, 0.6f, F.Low, J(r, 700f, 0.1f), 0.7f, 35f, 0.003f);
            Tone(b, J(r, 140f, 0.06f), 0.3f, 0f, 0.08f, 0.002f, 45f);
            Click(b, r, 0f, 0.15f, 2200f, 2f);
            return b;
        }

        public static float[] Snap(Rng r)
        {
            var b = Buf(0.6f); float f = J(r, 900f, 0.05f);
            Click(b, r, 0f, 0.8f, 2500f, 3f);
            Tone(b, 120f, 0.8f, 0f, 0.1f, 0.001f, 40f);
            Bell(b, f, 0.5f, 0f, 16f, new[] { 1f, 1.52f, 2.55f, 3.9f }, new[] { 1f, 0.7f, 0.4f, 0.2f });
            return Reverb(b, 0.2f, 0.25f);
        }

        public static float[] ClinkMetal(Rng r)
        {
            var b = Buf(0.9f);
            for (int hit = 0; hit < 2; hit++)
            {
                float at = hit == 0 ? 0f : Range(r, 0.05f, 0.09f), g = hit == 0 ? 1f : 0.5f;
                float f = Range(r, 1800f, 3200f);
                for (int k = 0; k < 6; k++)
                    Tone(b, f * Range(r, 1f, 4.2f), 0.35f * g / (1 + k * 0.4f), at, 0.7f, 0.0008f, Range(r, 7f, 16f));
                Click(b, r, at, 0.5f * g, 3500f, 2f);
            }
            return Reverb(b, 0.25f, 0.4f);
        }

        public static float[] ThudBox(Rng r)
        {
            var b = Buf(0.55f);
            Glide(b, J(r, 130f, 0.08f), 48f, 18f, 1f, 0f, 0.4f, 11f, 0.002f);
            NoiseHit(b, r, 0f, 0.15f, 0.7f, F.Low, 600f, 0.7f, 30f, 0.002f);
            NoiseHit(b, r, 0f, 0.2f, 0.35f, F.Band, 280f, 2f, 22f, 0.002f);
            return Reverb(b, 0.15f, 0.2f);
        }

        public static float[] ConePlace(Rng r)
        {
            var b = Buf(0.5f);
            Glide(b, J(r, 440f, 0.06f), 250f, 25f, 0.6f, 0f, 0.25f, 22f);
            NoiseHit(b, r, 0f, 0.15f, 0.6f, F.Band, 950f, 2f, 40f);
            Glide(b, 380f, 230f, 25f, 0.25f, 0.13f, 0.2f, 28f);
            NoiseHit(b, r, 0.13f, 0.1f, 0.25f, F.Band, 900f, 2f, 50f);
            return Reverb(b, 0.18f, 0.2f);
        }

        public static float[] Tape(Rng r)
        {
            var b = White(r, 0.9f);
            Filter(b, F.Band, 3200f, 0.8f);
            float rasp = Range(r, 70f, 110f);
            Shape(b, t => (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * rasp))) * Mathf.Clamp01(t * 25f) * Mathf.Clamp01((0.9f - t) * 8f) * (0.8f + 0.2f * Mathf.Sin(t * 9f)));
            return b;
        }

        public static float[] Spray(Rng r)
        {
            var b = Buf(1.3f);
            for (int i = 0; i < 3; i++) { float t = i * 0.045f; Click(b, r, t, 0.5f, 3500f, 3f); Tone(b, 4200f + i * 300f, 0.15f, t, 0.03f, 0.001f, 100f); }
            var s = White(r, 1.0f); Filter(s, F.High, 3500f); Filter(s, F.Band, 6500f, 0.5f);
            Gate(s, 0.05f, 0.25f);
            Shape(s, t => 0.85f + 0.15f * Mathf.Sin(t * 27f));
            Add(b, s, 1.1f, 0.15f);
            return b;
        }

        public static float[] WaterDrip(Rng r)
        {
            var b = Buf(0.6f); float lo = J(r, 650f, 0.15f);
            Glide(b, lo, lo * 2.8f, 35f, 0.7f, 0f, 0.09f, 38f, 0.001f);
            Click(b, r, 0f, 0.15f, 3000f, 1.5f);
            Glide(b, lo * 1.4f, lo * 2.9f, 30f, 0.25f, 0.05f, 0.08f, 45f, 0.001f);
            return Reverb(b, 0.45f, 0.5f);
        }

        public static float[] WaterPour(Rng r)
        {
            float len = 3f;
            var b = White(r, len);
            var hi = (float[])b.Clone();
            Filter(b, F.Band, 1700f, 0.7f); Filter(hi, F.Band, 4600f, 0.9f);
            var tex = SlowNoise(r, len, 18f);
            Shape(b, t => 0.7f); Add(b, hi, 0.45f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.7f + 0.3f * tex[i];
            for (int k = 0; k < 40; k++) Glide(b, 0f + Range(r, 300f, 700f), Range(r, 900f, 1700f), 12f, Range(r, 0.04f, 0.1f), Range(r, 0f, len - 0.2f), 0.08f, 25f, 0.004f);
            return MakeLoop(b, 0.5f);
        }

        public static float[] PumpHum(Rng r)
        {
            float len = 2.5f;
            var b = Buf(len);
            // integer multiples of 1/len Hz keep the loop seamless; 100 Hz = 250 cycles
            Harmonics(b, t => 100f, 1, 0f, 1f);
            Tone(b, 200f, 0.5f, 0f, len, 0.01f, 0f); Tone(b, 300f, 0.3f, 0f, len, 0.01f, 0f);
            Tone(b, 50f, 0.35f, 0f, len, 0.01f, 0f); Tone(b, 400f, 0.14f, 0f, len, 0.01f, 0f);
            Tone(b, 1400f, 0.035f, 0f, len, 0.01f, 0f, 0.5f, 0.6f);
            var n = White(r, len); Filter(n, F.Low, 700f); Add(b, n, 0.35f);
            return MakeLoop(b, 0.3f);
        }

        public static float[] GasHiss(Rng r)
        {
            var b = White(r, 1.8f);
            var low = (float[])b.Clone();
            Filter(b, F.High, 2500f); Filter(b, F.Band, 5500f, 0.55f);
            Filter(low, F.Band, 1500f, 0.6f);
            Add(b, low, 0.12f);
            Shape(b, t => 1f + 0.1f * Mathf.Sin(t * 31f));
            return Gate(b, 0.06f, 0.6f);
        }

        // wail: pitch rises and falls once over the clip, so it loops cleanly
        public static float[] AlarmSiren(Rng r)
        {
            var b = Buf(3f);
            Harmonics(b, t => 520f + 430f * (0.5f - 0.5f * Mathf.Cos(Tau * t / 3f)), 4, 1.2f, 0.5f);
            Soft(b, 1.4f);
            Filter(b, F.Low, 4200f);
            return b;
        }

        public static float[] AlarmSirenRise(Rng r)
        {
            var b = Buf(1.8f);
            Harmonics(b, t => 380f * Mathf.Pow(2.6f, Mathf.Min(1f, t / 1.5f)), 4, 1.2f, 0.5f);
            Soft(b, 1.4f); Filter(b, F.Low, 4500f);
            return Gate(b, 0.03f, 0.25f);
        }

        public static float[] AlarmSirenFall(Rng r)
        {
            var b = Buf(1.8f);
            Harmonics(b, t => 1000f * Mathf.Pow(0.4f, Mathf.Min(1f, t / 1.5f)), 4, 1.2f, 0.5f);
            Soft(b, 1.4f); Filter(b, F.Low, 4500f);
            return Gate(b, 0.03f, 0.3f);
        }

        public static float[] GasMonitorBeep(Rng r)
        {
            var b = Buf(0.5f); float f = J(r, 2900f, 0.01f);
            for (int p = 0; p < 2; p++)
            {
                var t = Buf(0.09f);
                Tone(t, f, 0.8f, 0f, 0.09f, 0.002f, 0f); Tone(t, f * 3f, 0.25f, 0f, 0.09f, 0.002f, 0f); Tone(t, f * 0.5f, 0.15f, 0f, 0.09f, 0.002f, 0f);
                Gate(t, 0.003f, 0.015f);
                Add(b, t, 1f, p * 0.16f);
            }
            return Reverb(b, 0.08f, 0.12f);
        }

        public static float[] MonitorAlarm(Rng r)
        {
            var b = Buf(1.6f);
            float[] times = { 0f, 0.13f, 0.26f, 0.52f, 0.65f };
            float[] freqs = { 1046f, 1046f, 1046f, 1318f, 1318f };
            for (int i = 0; i < times.Length; i++)
            {
                var t = Buf(0.1f);
                Tone(t, freqs[i], 0.8f, 0f, 0.1f, 0.003f, 0f); Tone(t, freqs[i] * 2f, 0.3f, 0f, 0.1f, 0.003f, 0f); Tone(t, freqs[i] * 3f, 0.12f, 0f, 0.1f, 0.003f, 0f);
                Gate(t, 0.004f, 0.02f);
                Add(b, t, 1f, times[i]);
            }
            return b;
        }

        public static float[] RadioClick(Rng r)
        {
            var b = Buf(0.35f);
            NoiseHit(b, r, 0f, 0.04f, 0.9f, F.Band, 1800f, 1f, 90f);
            Tone(b, 1000f, 0.3f, 0f, 0.02f, 0.0005f, 120f);
            NoiseHit(b, r, 0.03f, 0.25f, 0.28f, F.Band, 2400f, 0.6f, 14f, 0.01f);
            return b;
        }

        public static float[] RadioStatic(Rng r)
        {
            float len = 2f;
            var b = White(r, len);
            Filter(b, F.Band, 2300f, 0.5f);
            var am = SlowNoise(r, len, 9f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.45f + 0.35f * am[i];
            for (int k = 0; k < 25; k++) Click(b, r, Range(r, 0f, len - 0.02f), Range(r, 0.3f, 1.2f), 1500f, 2f);
            return MakeLoop(b, 0.2f);
        }

        public static float[] ForkliftHorn(Rng r)
        {
            var b = Buf(0.75f); float f = J(r, 420f, 0.015f);
            Harmonics(b, t => f, 9, 1f, 0.4f); Harmonics(b, t => f * 1.25f, 9, 1f, 0.35f);
            Shape(b, t => 1f + 0.04f * Mathf.Sin(t * 38f));
            Filter(b, F.Low, 3200f);
            Add(b, FilterNoise(r, 0.75f, 900f), 0.05f);
            return Gate(b, 0.035f, 0.09f);
        }

        static float[] FilterNoise(Rng r, float len, float fc) { var n = White(r, len); return Filter(n, F.Low, fc); }

        public static float[] ForkliftEngineLoop(Rng r)
        {
            float len = 2f; var b = Buf(len);
            float rate = 22f; // firing pulses per second (44 in 2 s, so the loop closes)
            for (int p = 0; p < (int)(len * rate); p++)
            {
                float t = p / rate, a = 0.8f + 0.2f * Rnd(r);
                Tone(b, 68f, a, t, 0.07f, 0.004f, 40f); Tone(b, 135f, a * 0.5f, t, 0.05f, 0.003f, 55f);
                NoiseHit(b, r, t, 0.04f, a * 0.25f, F.Low, 500f, 0.7f, 70f, 0.002f);
            }
            var rum = White(r, len); Filter(rum, F.Low, 300f); Add(b, rum, 0.25f);
            Tone(b, 11f * 8f, 0.12f, 0f, len, 0.01f, 0f);
            Filter(b, F.Low, 1400f);
            return MakeLoop(b, 0.2f);
        }

        public static float[] ForkliftBeepReverse(Rng r)
        {
            var b = Buf(1f);
            var t = Buf(0.46f);
            Tone(t, 1000f, 0.8f, 0f, 0.46f, 0.002f, 0f); Tone(t, 3000f, 0.25f, 0f, 0.46f, 0.002f, 0f); Tone(t, 2000f, 0.2f, 0f, 0.46f, 0.002f, 0f);
            Gate(t, 0.004f, 0.01f);
            Add(b, t, 1f, 0.02f);
            return b;
        }

        public static float[] HydraulicWhine(Rng r)
        {
            var b = Buf(1.5f);
            Harmonics(b, t => 620f + 480f * Mathf.Sin(Mathf.Clamp01(t / 1.1f) * Mathf.PI) * 0.8f + 120f * t, 2, 1.5f, 0.35f);
            Tone(b, 90f, 0.4f, 0f, 1.5f, 0.1f, 0f);
            var n = White(r, 1.5f); Filter(n, F.Band, 2100f, 1.5f); Add(b, n, 0.12f);
            Gate(b, 0.2f, 0.35f);
            return b;
        }

        public static float[] PalletDrop(Rng r)
        {
            var b = Buf(1.1f);
            Glide(b, 95f, 40f, 12f, 1f, 0f, 0.6f, 7f, 0.002f);
            NoiseHit(b, r, 0f, 0.25f, 0.9f, F.Low, 500f, 0.7f, 18f, 0.002f);
            for (int k = 0; k < 4; k++)
            {
                float t = Range(r, 0.03f, 0.4f);
                Tone(b, Range(r, 700f, 1400f), 0.25f, t, 0.08f, 0.001f, 45f);
                NoiseHit(b, r, t, 0.05f, 0.35f, F.Band, Range(r, 1200f, 2400f), 2f, 60f);
            }
            return Reverb(b, 0.3f, 0.5f);
        }

        public static float[] MirrorSnap(Rng r)
        {
            var b = Buf(0.5f);
            Click(b, r, 0f, 0.9f, 2500f, 2f);
            Tone(b, J(r, 4200f, 0.03f), 0.4f, 0f, 0.25f, 0.0006f, 28f);
            Tone(b, J(r, 5900f, 0.03f), 0.25f, 0f, 0.2f, 0.0006f, 38f);
            Tone(b, 150f, 0.3f, 0f, 0.05f, 0.001f, 70f);
            return Reverb(b, 0.2f, 0.25f);
        }

        public static float[] LedOn(Rng r)
        {
            var b = Buf(0.2f);
            Click(b, r, 0f, 0.35f, 3000f, 2f);
            Tone(b, J(r, 2300f, 0.04f), 0.4f, 0f, 0.08f, 0.001f, 50f);
            Tone(b, 90f, 0.2f, 0f, 0.05f, 0.001f, 60f);
            return b;
        }

        public static float[] SparkPop(Rng r)
        {
            var b = Buf(0.4f);
            NoiseHit(b, r, 0f, 0.15f, 0.5f, F.Low, 1800f, 0.7f, 28f, 0.001f);
            for (int k = 0; k < 8; k++) Click(b, r, Range(r, 0f, 0.18f), Range(r, 0.3f, 1f), Range(r, 3000f, 6000f), Range(r, 1f, 3f));
            return b;
        }

        public static float[] SmokePuff(Rng r)
        {
            var b = White(r, 0.9f);
            FilterVar(b, F.Low, t => 1800f * Mathf.Exp(-t * 2.2f) + 300f, 0.8f);
            return Env(b, 0.06f, 4.5f);
        }

        public static float[] EsdClick(Rng r)
        {
            var b = Buf(0.25f);
            Click(b, r, 0f, 1f, 3500f, 1f);
            Tone(b, 6200f, 0.3f, 0f, 0.05f, 0.0003f, 140f);
            for (int k = 0; k < 4; k++) Click(b, r, Range(r, 0.01f, 0.08f), Range(r, 0.1f, 0.35f), 4500f, 1f);
            return b;
        }

        public static float[] MeterBeep(Rng r)
        {
            var b = Buf(0.28f);
            Tone(b, 1000f, 0.8f, 0f, 0.2f, 0.003f, 0f); Tone(b, 3000f, 0.08f, 0f, 0.2f, 0.003f, 0f);
            Gate(b, 0.005f, 0.04f);
            return b;
        }

        public static float[] WindLoop(Rng r)
        {
            float len = 8f; var b = White(r, len);
            var gust = SlowNoise(r, len, 0.6f);
            FilterVar(b, F.Low, t => 500f + 350f * Mathf.Sin(Tau * t / 8f * 2f) + 200f * Mathf.Sin(Tau * t / 8f * 3f + 1f), 0.9f);
            var whistle = White(r, len); FilterVar(whistle, F.Band, t => 700f + 250f * Mathf.Sin(Tau * t / 8f), 12f);
            for (int i = 0; i < b.Length; i++) b[i] = (b[i] * 1.4f + whistle[i] * 0.5f) * (0.55f + 0.45f * gust[i]);
            return MakeLoop(b, 0.7f);
        }

        public static float[] HvacLoop(Rng r)
        {
            float len = 5f; var b = White(r, len);
            Filter(b, F.Low, 520f, 0.6f); Filter(b, F.Low, 700f);
            var air = White(r, len); Filter(air, F.Band, 1900f, 0.6f); Add(b, air, 0.1f);
            Tone(b, 120f, 0.06f * 3f, 0f, len, 0.01f, 0f); Tone(b, 240f, 0.03f * 3f, 0f, len, 0.01f, 0f);
            Tone(b, 60f, 0.05f * 3f, 0f, len, 0.01f, 0f);
            return MakeLoop(b, 0.4f);
        }

        public static float[] PlantDroneLoop(Rng r)
        {
            float len = 8f; var b = Buf(len);
            Tone(b, 55f, 0.5f, 0f, len, 0.01f, 0f); Tone(b, 55.5f, 0.35f, 0f, len, 0.01f, 0f);
            Tone(b, 82.5f, 0.28f, 0f, len, 0.01f, 0f); Tone(b, 110f, 0.2f, 0f, len, 0.01f, 0f); Tone(b, 165f, 0.07f, 0f, len, 0.01f, 0f);
            var rum = White(r, len); Filter(rum, F.Low, 220f); Add(b, rum, 0.5f);
            var air = White(r, len); Filter(air, F.Band, 1100f, 0.7f); Add(b, air, 0.05f);
            Shape(b, t => 0.85f + 0.15f * Mathf.Sin(Tau * t / len * 4f));
            return MakeLoop(b, 0.8f);
        }

        public static float[] FlareRoarLoop(Rng r)
        {
            float len = 5f; var b = White(r, len);
            var turb = SlowNoise(r, len, 7f);
            Filter(b, F.Low, 1300f, 0.6f);
            var rum = White(r, len); Filter(rum, F.Low, 140f);
            for (int i = 0; i < b.Length; i++) b[i] = (b[i] * (0.5f + 0.5f * turb[i]) + rum[i] * 1.2f);
            for (int k = 0; k < 90; k++) Click(b, r, Range(r, 0f, len - 0.02f), Range(r, 0.05f, 0.25f), 800f, 4f);
            return MakeLoop(b, 0.5f);
        }

        public static float[] DoorOpen(Rng r)
        {
            var b = Buf(1.1f);
            Click(b, r, 0f, 0.6f, 1800f, 3f); Tone(b, 220f, 0.4f, 0f, 0.08f, 0.001f, 40f);
            Bell(b, 1200f, 0.12f, 0f, 22f);
            var c = White(r, 0.7f); float f0 = Range(r, 380f, 520f);
            FilterVar(c, F.Band, t => f0 + 420f * t / 0.7f, 10f);
            Shape(c, t => (0.5f + 0.5f * Mathf.Sin(t * 22f)) * Mathf.Sin(Mathf.Clamp01(t / 0.7f) * Mathf.PI));
            Add(b, c, 0.5f, 0.12f);
            return Reverb(b, 0.15f, 0.3f);
        }

        public static float[] DoorClose(Rng r)
        {
            var b = Buf(0.9f);
            var c = White(r, 0.3f); float f0 = Range(r, 520f, 760f);
            FilterVar(c, F.Band, t => f0 - 300f * t / 0.3f, 10f);
            Shape(c, t => Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI) * 0.8f);
            Add(b, c, 0.4f);
            Glide(b, 110f, 45f, 16f, 1f, 0.28f, 0.45f, 9f, 0.002f);
            NoiseHit(b, r, 0.28f, 0.15f, 0.7f, F.Low, 700f, 0.7f, 25f);
            Click(b, r, 0.36f, 0.5f, 1800f, 3f); Tone(b, 200f, 0.3f, 0.36f, 0.06f, 0.001f, 45f);
            return Reverb(b, 0.22f, 0.35f);
        }

        public static float[] Footstep(Rng r, Surface s)
        {
            var b = Buf(0.35f);
            switch (s)
            {
                case Surface.Concrete:
                    NoiseHit(b, r, 0f, 0.15f, 0.8f, F.Low, J(r, 1400f, 0.15f), 0.7f, 32f, 0.002f);
                    NoiseHit(b, r, 0f, 0.05f, 0.3f, F.High, 3000f, 0.7f, 90f);
                    Tone(b, J(r, 95f, 0.1f), 0.6f, 0f, 0.08f, 0.002f, 50f);
                    break;
                case Surface.Metal:
                    NoiseHit(b, r, 0f, 0.06f, 0.6f, F.Band, 1800f, 1.2f, 70f);
                    float f = J(r, 560f, 0.1f);
                    Tone(b, f, 0.4f, 0f, 0.3f, 0.002f, 14f); Tone(b, f * 2.1f, 0.22f, 0f, 0.25f, 0.002f, 20f); Tone(b, f * 4.3f, 0.1f, 0f, 0.2f, 0.002f, 30f);
                    Tone(b, 90f, 0.4f, 0f, 0.08f, 0.002f, 50f);
                    break;
                case Surface.Tile:
                    Click(b, r, 0f, 0.9f, 2500f, 2f);
                    Tone(b, J(r, 1800f, 0.06f), 0.18f, 0f, 0.12f, 0.001f, 40f);
                    Tone(b, J(r, 3200f, 0.06f), 0.1f, 0f, 0.1f, 0.001f, 55f);
                    Tone(b, 120f, 0.45f, 0f, 0.07f, 0.002f, 55f);
                    break;
                case Surface.Gravel:
                    for (int k = 0; k < 30; k++)
                        NoiseHit(b, r, Range(r, 0f, 0.2f), 0.02f, Range(r, 0.15f, 0.55f), F.Band, Range(r, 1800f, 6500f), 2f, 140f, 0.0005f);
                    NoiseHit(b, r, 0f, 0.15f, 0.5f, F.Low, 1000f, 0.7f, 25f, 0.003f);
                    Tone(b, 80f, 0.3f, 0f, 0.08f, 0.003f, 45f);
                    break;
                default: // Wood
                    NoiseHit(b, r, 0f, 0.1f, 0.55f, F.Band, J(r, 420f, 0.12f), 1.6f, 38f, 0.002f);
                    Tone(b, J(r, 210f, 0.08f), 0.5f, 0f, 0.15f, 0.002f, 30f);
                    Tone(b, 130f, 0.5f, 0f, 0.1f, 0.002f, 40f);
                    Click(b, r, 0.001f, 0.25f, 1800f, 3f);
                    break;
            }
            return Reverb(b, 0.1f, 0.18f);
        }

        public static float[] Ding(Rng r)
        {
            var b = Buf(1.6f); float f = J(r, 1175f, 0.01f);
            Tone(b, f, 0.8f, 0f, 1.5f, 0.001f, 3.2f); Tone(b, f * 2f, 0.25f, 0f, 1.2f, 0.001f, 5f); Tone(b, f * 3.01f, 0.1f, 0f, 1f, 0.001f, 8f);
            Tone(b, f * 4.2f, 0.05f, 0f, 0.5f, 0.001f, 14f);
            return Reverb(b, 0.3f, 0.6f);
        }

        public static float[] ErrorBuzz(Rng r)
        {
            var b = Buf(0.55f);
            for (int p = 0; p < 2; p++)
            {
                var t = Buf(0.2f);
                Harmonics(t, x => 150f, 12, 0.9f, 0.3f);
                Tone(t, 150f, 0.5f, 0f, 0.2f, 0.001f, 0f, 2f, 1.5f);
                Gate(t, 0.004f, 0.05f);
                Add(b, t, 1f, p * 0.24f);
            }
            Filter(b, F.Low, 2200f);
            return Soft(b, 1.5f);
        }

        public static float[] PressureTone(Rng r)
        {
            var b = Buf(1f);
            Tone(b, 220f, 0.7f, 0f, 1f, 0.01f, 0f); Tone(b, 440f, 0.22f, 0f, 1f, 0.01f, 0f); Tone(b, 330f, 0.12f, 0f, 1f, 0.01f, 0f);
            var n = White(r, 1f); Filter(n, F.Band, 800f, 2f); Add(b, n, 0.05f);
            return MakeLoop(b, 0.15f);
        }

        public static float[] GaugeTick(Rng r)
        {
            var b = Buf(0.1f);
            NoiseHit(b, r, 0f, 0.02f, 0.7f, F.Band, J(r, 3200f, 0.05f), 2f, 140f);
            Tone(b, 2400f, 0.25f, 0f, 0.03f, 0.0005f, 140f);
            return b;
        }
    }
}
