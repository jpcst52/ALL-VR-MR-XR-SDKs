using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Background music for the hub lobby: an original electronic loop (124 BPM, A minor, four on the floor, pumping bass, arpeggio and
/// pads) generated in code, so there is nothing to license or download and nothing in the repository but this script. It is made on a
/// background thread when the lobby loads and fades in when ready. It stops when a scenario loads (the scenarios have their own sound).
/// Desktop: press M to mute or unmute. Not a copy of any existing track.
/// </summary>
public class HubMusic : MonoBehaviour
{
    public const float Volume = 0.32f;
    const int Rate = 32000;
    const float Bpm = 124f;
    const int Bars = 16;

    AudioSource source;
    float[] data;
    bool muted;
    float fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (!Application.isPlaying) return;
        var name = SceneManager.GetActiveScene().name;
        if (!name.StartsWith(LobbyHub.LobbyScene)) return;
        new GameObject("Hub music").AddComponent<HubMusic>();
    }

    void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.playOnAwake = false;
        Task.Run(() => { data = Generate(); });
    }

    void Update()
    {
        if (data != null && source.clip == null)
        {
            var clip = AudioClip.Create("Hub music", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            source.clip = clip;
            source.Play();
            Debug.Log($"[music] hub loop ready: {clip.length:0.0} s, {Bpm} BPM");
            data = null;
        }
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) muted = !muted;
        float target = muted ? 0f : Volume;
        fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime * 0.12f);
        if (source != null) source.volume = fade;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // Synthesis (runs off the main thread: plain math only, no Unity API)
    // ---------------------------------------------------------------------------------------------------------------------

    static float[] Generate()
    {
        float beat = 60f / Bpm;
        float bar = beat * 4f;
        int n = (int)(bar * Bars * Rate);
        var mix = new float[n];
        var bass = new float[n];      // ducked by the kick
        var rng = new System.Random(124);

        // A minor: Am - F - C - G, four bars each pass, 16 bars in all (the loop point is a bar line)
        float[] roots = { 55f, 43.65f, 65.41f, 49f };                         // A1 F1 C2 G1
        float[][] chords =
        {
            new[] { 220f, 261.63f, 329.63f },     // Am
            new[] { 174.61f, 220f, 261.63f },     // F
            new[] { 196f, 261.63f, 329.63f },     // C (second inversion)
            new[] { 196f, 246.94f, 293.66f },     // G
        };

        for (int b = 0; b < Bars; b++)
        {
            int chord = b % 4;
            float t0 = b * bar;
            bool intro = b < 4, breakdown = b >= 12 && b < 14;

            // kick on every beat (not in the breakdown)
            if (!breakdown)
                for (int k = 0; k < 4; k++) Kick(mix, t0 + k * beat, b < 2 ? 0.7f : 0.9f);

            // clap on 2 and 4, open hat on the off-beat, closed hat on the 16ths
            if (b >= 2 && !breakdown)
            {
                for (int k = 1; k < 4; k += 2) Clap(mix, rng, t0 + k * beat, 0.35f);
                for (int k = 0; k < 4; k++) Hat(mix, rng, t0 + k * beat + beat * 0.5f, 0.28f, 0.09f);
            }
            if (b >= 6 && !breakdown)
                for (int k = 0; k < 16; k++) if (k % 2 == 1) Hat(mix, rng, t0 + k * beat * 0.25f, 0.12f, 0.025f);

            // bass: off-beat eighths, filtered saw, ducked later by the kick
            if (b >= 1 && !breakdown)
                for (int k = 0; k < 8; k++)
                {
                    if (k % 2 == 0) continue;
                    float f = roots[chord] * (k == 7 ? 2f : 1f);
                    Bass(bass, t0 + k * beat * 0.5f, beat * 0.45f, f, 0.55f);
                }

            // pad chord under every bar
            Pad(mix, t0, bar, chords[chord], intro ? 0.10f : 0.14f);

            // arpeggio: sixteenths up and down the chord, filter opening over the first eight bars
            if (b >= 4 && !(b >= 12 && b < 14))
            {
                float open = 700f + 3200f * Mathf.Clamp01((b - 4) / 8f);
                float[] ch = chords[chord];
                int[] pattern = { 0, 1, 2, 1, 0, 2, 1, 2, 0, 1, 2, 1, 2, 1, 0, 1 };
                for (int k = 0; k < 16; k++)
                {
                    float f = ch[pattern[k]] * 2f;
                    Pluck(mix, t0 + k * beat * 0.25f, beat * 0.22f, f, open, 0.16f);
                }
            }

            // riser into the loop point
            if (b == Bars - 1) Riser(mix, rng, t0, bar, 0.22f);
        }

        // sidechain: the bass dips on every kick so the beat pumps
        for (int b = 0; b < Bars; b++)
            for (int k = 0; k < 4; k++)
            {
                int s = (int)((b * bar + k * beat) * Rate);
                int len = (int)(beat * 0.6f * Rate);
                for (int i = 0; i < len && s + i < n; i++)
                {
                    float x = i / (float)len;
                    float duck = 1f - Mathf.Pow(1f - x, 3f) * 0.85f;
                    bass[s + i] *= duck;
                }
            }
        for (int i = 0; i < n; i++) mix[i] += bass[i];

        // soft clip and normalise
        float peak = 0f;
        for (int i = 0; i < n; i++) { mix[i] = (float)Math.Tanh(mix[i] * 1.2f); peak = Mathf.Max(peak, Mathf.Abs(mix[i])); }
        float scale = peak > 0f ? 0.8f / peak : 1f;
        for (int i = 0; i < n; i++) mix[i] *= scale;
        return mix;
    }

    static void Kick(float[] d, float t, float amp)
    {
        int s = (int)(t * Rate), len = (int)(0.3f * Rate);
        double phase = 0;
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            float x = i / (float)Rate;
            float f = 48f + 110f * Mathf.Exp(-x * 28f);
            phase += 2 * Math.PI * f / Rate;
            d[s + i] += (float)Math.Sin(phase) * Mathf.Exp(-x * 9f) * amp;
        }
    }

    static void Clap(float[] d, System.Random r, float t, float amp)
    {
        int s = (int)(t * Rate), len = (int)(0.22f * Rate);
        float lp = 0f;
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            float x = i / (float)Rate;
            float env = Mathf.Exp(-x * 20f) * (0.55f + 0.45f * Mathf.Sin(x * 190f)); // a few quick slaps
            float white = (float)(r.NextDouble() * 2 - 1);
            lp += (white - lp) * 0.45f;
            d[s + i] += (white - lp) * env * amp;
        }
    }

    static void Hat(float[] d, System.Random r, float t, float amp, float dur)
    {
        int s = (int)(t * Rate), len = (int)(dur * Rate * 2f);
        float lp = 0f;
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            float x = i / (float)Rate;
            float white = (float)(r.NextDouble() * 2 - 1);
            lp += (white - lp) * 0.7f;
            d[s + i] += (white - lp) * Mathf.Exp(-x / dur * 4f) * amp * 0.5f;
        }
    }

    static void Bass(float[] d, float t, float dur, float f, float amp)
    {
        int s = (int)(t * Rate), len = (int)(dur * Rate);
        float lp = 0f, ph = 0f, cut = 0.07f;
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            ph += f / Rate; if (ph >= 1f) ph -= 1f;
            float saw = ph * 2f - 1f;
            lp += (saw - lp) * cut;
            float env = Mathf.Min(1f, i / (0.004f * Rate)) * Mathf.Exp(-i / (float)Rate * 5f);
            d[s + i] += lp * env * amp;
        }
    }

    static void Pluck(float[] d, float t, float dur, float f, float cutoffHz, float amp)
    {
        int s = (int)(t * Rate), len = (int)(dur * Rate * 2f);
        float lp = 0f, ph1 = 0f, ph2 = 0f;
        float cut = Mathf.Clamp01(2f * Mathf.PI * cutoffHz / Rate);
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            ph1 += f / Rate; if (ph1 >= 1f) ph1 -= 1f;
            ph2 += f * 1.006f / Rate; if (ph2 >= 1f) ph2 -= 1f;
            float saw = (ph1 + ph2) - 1f;
            lp += (saw - lp) * cut;
            float env = Mathf.Min(1f, i / (0.002f * Rate)) * Mathf.Exp(-i / (float)Rate / dur * 3.2f);
            d[s + i] += lp * env * amp;
        }
    }

    static void Pad(float[] d, float t, float dur, float[] freqs, float amp)
    {
        int s = (int)(t * Rate), len = (int)(dur * Rate);
        var ph = new float[freqs.Length * 2];
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            float x = i / (float)len;
            float env = Mathf.Min(1f, x * 6f) * Mathf.Min(1f, (1f - x) * 6f);
            float sum = 0f;
            for (int k = 0; k < freqs.Length; k++)
            {
                ph[k * 2] += freqs[k] / Rate; if (ph[k * 2] >= 1f) ph[k * 2] -= 1f;
                ph[k * 2 + 1] += freqs[k] * 1.004f / Rate; if (ph[k * 2 + 1] >= 1f) ph[k * 2 + 1] -= 1f;
                sum += Mathf.Sin(ph[k * 2] * 2f * Mathf.PI) + Mathf.Sin(ph[k * 2 + 1] * 2f * Mathf.PI);
            }
            d[s + i] += sum / freqs.Length * 0.5f * env * amp;
        }
    }

    static void Riser(float[] d, System.Random r, float t, float dur, float amp)
    {
        int s = (int)(t * Rate), len = (int)(dur * Rate);
        float lp = 0f;
        for (int i = 0; i < len && s + i < d.Length; i++)
        {
            float x = i / (float)len;
            float white = (float)(r.NextDouble() * 2 - 1);
            lp += (white - lp) * (0.05f + 0.6f * x * x);
            d[s + i] += lp * x * x * amp;
        }
    }
}
