using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PittBradford.XR
{
    public enum MoodPreset { IndoorBright, WarehouseDim, OutdoorOvercast, LabNeutral }

    /// <summary>
    /// Cheap, code-built lighting and particle effects for the built-in render pipeline (no packages, no imported art).
    /// Every effect is made through <see cref="P.Node"/> / <see cref="P.Ensure{T}"/>, so a scene bake saves it with its
    /// settings and its material, and a baked scene adopts it again at play time (set-up is applied only to new objects).
    /// Small ambient particle counts and one additive cone per light shaft keep it VR-sensible.
    /// </summary>
    public static class Fx
    {
        /// <summary>Practical lights beyond this count are rendered as vertex lights (cheap) instead of pixel lights.</summary>
        public static int PixelLightBudget = 6;
        static int practicals;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { practicals = 0; soft = null; matCache.Clear(); }

        // ------------------------------------------------------------------ materials

        static Texture2D soft;
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        /// <summary>A soft round sprite (white, alpha falls off to the edge).</summary>
        public static Texture2D SoftTexture()
        {
            if (soft != null) return soft;
            const int n = 64;
            soft = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "fx-soft", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1, 1, 1, a);
            }
            soft.SetPixels(px);
            soft.Apply(true);
            return soft;
        }

        /// <summary>Particle material with the soft sprite. Colour comes from each particle's start colour.</summary>
        public static Material ParticleMat(bool additive)
        {
            string key = additive ? "add" : "alpha";
            if (matCache.TryGetValue(key, out var m) && m) return m;
            var sh = Shader.Find(additive ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended")
                     ?? Shader.Find("Sprites/Default");
            m = new Material(sh) { name = "fx-particle-" + key, mainTexture = SoftTexture() };
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            matCache[key] = m;
            return m;
        }

        static ParticleSystem MakePS(Transform parent, string name, Vector3 pos, Quaternion rot, bool additive, out bool fresh)
        {
            var go = P.Node(name, parent);
            fresh = P.IsNew(go);
            if (fresh) go.transform.SetPositionAndRotation(parent != null ? parent.TransformPoint(pos) : pos, rot);
            var ps = P.Ensure<ParticleSystem>(go);
            if (fresh)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = ParticleMat(additive);
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.lightProbeUsage = LightProbeUsage.Off;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            return ps;
        }

        // ------------------------------------------------------------------ particle effects

        /// <summary>Slow floating dust motes in a box. Looks best crossing a <see cref="LightShaft"/>. About 40 particles.</summary>
        public static ParticleSystem Dust(Transform parent, Vector3 centre, Vector3 size, int count = 40)
        {
            var ps = MakePS(parent, "Fx Dust", centre, Quaternion.identity, true, out bool fresh);
            if (fresh)
            {
                var main = ps.main;
                main.loop = true; main.playOnAwake = true; main.prewarm = true;
                main.startLifetime = 14f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
                main.startColor = new Color(1f, 0.96f, 0.88f, 0.55f);
                main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = count / 14f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
                var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f); vel.y = new ParticleSystem.MinMaxCurve(-0.015f, 0.025f); vel.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
                var noise = ps.noise; noise.enabled = true; noise.strength = 0.05f; noise.frequency = 0.4f;
                var col = ps.colorOverLifetime; col.enabled = true;
                col.color = Fade(0.12f, 0.88f);
            }
            if (!ps.isPlaying && Application.isPlaying) ps.Play();
            return ps;
        }

        /// <summary>A plume of steam, 18 particles/second, rising and widening. <paramref name="direction"/> is where it points.</summary>
        public static ParticleSystem Steam(Transform parent, Vector3 pos, Vector3 direction, float rate = 18f, float size = 0.18f)
        {
            var ps = MakePS(parent, "Fx Steam", pos, Quaternion.FromToRotation(Vector3.forward, direction.sqrMagnitude < 0.001f ? Vector3.up : direction), false, out bool fresh);
            if (fresh)
            {
                var main = ps.main;
                main.loop = true; main.playOnAwake = true; main.prewarm = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size); main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                main.startColor = new Color(0.95f, 0.97f, 1f, 0.35f); main.gravityModifier = -0.04f;
                main.maxParticles = 80; main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = rate;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 0.03f;
                var size3 = ps.sizeOverLifetime; size3.enabled = true;
                size3.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(1, 2.6f)));
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(0.2f, 0.6f);
                var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.5f;
            }
            if (!ps.isPlaying && Application.isPlaying) ps.Play();
            return ps;
        }

        /// <summary>Spark shower, bursts every <paramref name="interval"/> seconds (0 = one burst you trigger with Play()).</summary>
        public static ParticleSystem Sparks(Transform parent, Vector3 pos, float interval = 2.5f, int burst = 14)
        {
            var ps = MakePS(parent, "Fx Sparks", pos, Quaternion.Euler(-90f, 0, 0), true, out bool fresh);
            if (fresh)
            {
                var main = ps.main;
                main.loop = interval > 0f; main.playOnAwake = interval > 0f; main.duration = Mathf.Max(0.5f, interval);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.4f);
                main.startSize = 0.025f; main.gravityModifier = 1f; main.maxParticles = 60;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f, 1f), new Color(1f, 0.55f, 0.15f, 1f));
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35f; sh.radius = 0.01f;
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(0.02f, 0.5f);
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.5f; r.velocityScale = 0.12f;
            }
            return ps;
        }

        /// <summary>
        /// Water drips falling from a point, <paramref name="rate"/> per second, with a drip sound where they land (3D).
        /// </summary>
        public static ParticleSystem Drip(Transform parent, Vector3 pos, float rate = 0.8f, bool sound = true, float fall = 0.45f)
        {
            var ps = MakePS(parent, "Fx Drip", pos, Quaternion.Euler(90f, 0, 0), false, out bool fresh);
            if (fresh)
            {
                var main = ps.main;
                main.loop = true; main.playOnAwake = true;
                main.startLifetime = 1.2f; main.startSpeed = 0f; main.startSize = 0.018f; main.gravityModifier = 1f; main.maxParticles = 12;
                main.startColor = new Color(0.75f, 0.88f, 1f, 0.85f); main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = rate;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.004f;
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2f; r.velocityScale = 0.05f;
            }
            if (!ps.isPlaying && Application.isPlaying) ps.Play();
            if (sound && Application.isPlaying)
            {
                var go = new GameObject("fx drip sound");
                go.AddComponent<DontBake>();
                go.transform.SetParent(ps.transform, false);
                var d = go.AddComponent<FxDripSound>();
                d.Rate = rate; d.Fall = fall;
            }
            return ps;
        }

        /// <summary>Slow dark smoke, 6 particles/second, drifting up. For hot work, a smoulder, a fire.</summary>
        public static ParticleSystem Smoke(Transform parent, Vector3 pos, float rate = 6f, float size = 0.35f)
        {
            var ps = MakePS(parent, "Fx Smoke", pos, Quaternion.Euler(-90f, 0, 0), false, out bool fresh);
            if (fresh)
            {
                var main = ps.main;
                main.loop = true; main.playOnAwake = true; main.prewarm = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size); main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
                main.startColor = new Color(0.22f, 0.22f, 0.24f, 0.4f); main.gravityModifier = -0.03f; main.maxParticles = 60;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = rate;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.05f;
                var sz = ps.sizeOverLifetime; sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 3f)));
                var col = ps.colorOverLifetime; col.enabled = true; col.color = Fade(0.15f, 0.7f);
                var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.3f;
            }
            if (!ps.isPlaying && Application.isPlaying) ps.Play();
            return ps;
        }

        static ParticleSystem.MinMaxGradient Fade(float fadeIn, float fadeOutStart)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        // ------------------------------------------------------------------ light

        /// <summary>
        /// A faint light shaft: an open additive cone, widest at the bottom, fading out along its length. Put it under a
        /// ceiling light, pointing down by default. One draw call, no per-pixel cost beyond the fill.
        /// <paramref name="angle"/> is the full cone angle in degrees; <paramref name="intensity"/> 0.05..0.2 reads as haze.
        /// </summary>
        public static GameObject LightShaft(Transform parent, Vector3 position, float length, float angle, Color colour, float intensity = 0.1f)
        {
            var go = P.Node("Fx Light shaft", parent);
            bool fresh = P.IsNew(go);
            if (fresh)
            {
                go.transform.SetPositionAndRotation(parent != null ? parent.TransformPoint(position) : position, Quaternion.identity);
                var mf = P.Ensure<MeshFilter>(go);
                var mr = P.Ensure<MeshRenderer>(go);
                mf.sharedMesh = ConeMesh(length, angle, colour, intensity);
                mr.sharedMaterial = ParticleMat(true);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            return go;
        }

        static Mesh ConeMesh(float length, float angle, Color colour, float intensity)
        {
            const int seg = 20;
            float r1 = 0.06f, r2 = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * length;
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(dir * r1); c.Add(new Color(colour.r, colour.g, colour.b, intensity));
                v.Add(dir * r2 + Vector3.down * length); c.Add(new Color(colour.r, colour.g, colour.b, 0f));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2, b = i * 2 + 1, a2 = ((i + 1) % seg) * 2, b2 = a2 + 1;
                t.Add(a); t.Add(b); t.Add(a2);
                t.Add(a2); t.Add(b); t.Add(b2);
            }
            var m = new Mesh { name = "fx-shaft" };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
            var uv = new List<Vector2>();
            for (int i = 0; i < v.Count; i++) uv.Add(new Vector2(0.5f, 0.5f));
            m.SetUVs(0, uv);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// A point light that matches a visible fixture (lamp, window, indicator). The first <see cref="PixelLightBudget"/>
        /// practicals are pixel lights; later ones are vertex lights so a scene never exceeds the pixel-light budget.
        /// No shadows.
        /// </summary>
        public static Light Practical(Transform parent, Vector3 position, Color colour, float range, float intensity)
        {
            var go = P.Node("Fx Practical", parent);
            var l = P.Ensure<Light>(go);
            bool fresh = P.IsNew(go);
            if (fresh)
            {
                go.transform.position = parent != null ? parent.TransformPoint(position) : position;
                l.type = LightType.Point; l.color = colour; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            }
            l.renderMode = practicals++ < PixelLightBudget ? LightRenderMode.ForcePixel : LightRenderMode.ForceVertex;
            return l;
        }

        /// <summary>
        /// A reflection probe so metal and glass show the room instead of the sky. Realtime, refreshed once at start,
        /// 64 px per face (cheap), box-projected to <paramref name="size"/>.
        /// </summary>
        public static ReflectionProbe ReflectionProbe(Vector3 centre, Vector3 size, int resolution = 64)
        {
            var go = P.Node("Fx Reflection probe");
            var rp = P.Ensure<ReflectionProbe>(go);
            if (P.IsNew(go))
            {
                go.transform.position = centre;
                rp.mode = ReflectionProbeMode.Realtime;
                rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                rp.resolution = resolution;
                rp.size = size; rp.boxProjection = true; rp.intensity = 1f;
                rp.hdr = false;
                rp.shadowDistance = 0f;
                rp.clearFlags = ReflectionProbeClearFlags.SolidColor;
                rp.backgroundColor = new Color(0.25f, 0.26f, 0.28f);
            }
            return rp;
        }

        /// <summary>
        /// Sets ambient trilight, fog, the pixel light cap and (optionally) a directional key light for a mood.
        /// IndoorBright: clean bright room. WarehouseDim: darker, warmer pools, haze. OutdoorOvercast: soft sky light.
        /// LabNeutral: even and slightly cool. The scene bake saves these settings.
        /// </summary>
        public static void ApplyMood(MoodPreset mood, Light key = null)
        {
            Color sky, equator, ground, fog, keyColour; float fogStart, fogEnd, ambient, keyI;
            switch (mood)
            {
                case MoodPreset.WarehouseDim:
                    sky = new Color(0.30f, 0.32f, 0.36f); equator = new Color(0.22f, 0.22f, 0.24f); ground = new Color(0.10f, 0.10f, 0.11f);
                    fog = new Color(0.28f, 0.29f, 0.31f); fogStart = 8f; fogEnd = 36f; ambient = 0.9f; keyI = 0.35f; keyColour = new Color(1f, 0.9f, 0.75f);
                    break;
                case MoodPreset.OutdoorOvercast:
                    sky = new Color(0.62f, 0.67f, 0.74f); equator = new Color(0.52f, 0.55f, 0.6f); ground = new Color(0.28f, 0.28f, 0.27f);
                    fog = new Color(0.68f, 0.72f, 0.78f); fogStart = 25f; fogEnd = 120f; ambient = 1f; keyI = 0.55f; keyColour = new Color(0.92f, 0.94f, 1f);
                    break;
                case MoodPreset.LabNeutral:
                    sky = new Color(0.55f, 0.58f, 0.62f); equator = new Color(0.46f, 0.48f, 0.5f); ground = new Color(0.24f, 0.25f, 0.27f);
                    fog = new Color(0.6f, 0.63f, 0.67f); fogStart = 16f; fogEnd = 48f; ambient = 1f; keyI = 0.5f; keyColour = new Color(0.95f, 0.97f, 1f);
                    break;
                default: // IndoorBright
                    sky = new Color(0.62f, 0.64f, 0.68f); equator = new Color(0.5f, 0.5f, 0.52f); ground = new Color(0.26f, 0.26f, 0.27f);
                    fog = new Color(0.62f, 0.64f, 0.68f); fogStart = 14f; fogEnd = 42f; ambient = 1f; keyI = 0.6f; keyColour = new Color(1f, 0.95f, 0.87f);
                    break;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientIntensity = ambient;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, PixelLightBudget);
            if (key != null)
            {
                key.intensity = keyI; key.color = keyColour; key.shadows = LightShadows.Soft;
            }
        }
    }

    /// <summary>Plays a drip sound at the landing point of a drip emitter. Runtime only (kept out of bakes).</summary>
    public class FxDripSound : MonoBehaviour
    {
        public float Rate = 0.8f, Fall = 0.45f;
        float next;
        void Start() { next = Time.time + Random.Range(0.2f, 1f / Mathf.Max(0.05f, Rate)); }
        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + (1f / Mathf.Max(0.05f, Rate)) * Random.Range(0.8f, 1.2f);
            Sfx.Play("water_drip", transform.position + Vector3.down * Fall, 0.4f, 0.12f);
        }
    }
}
