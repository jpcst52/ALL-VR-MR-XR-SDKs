using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PittBradford.XR
{
    /// <summary>
    /// Round 7 visual pass, applied by scene name after the scenario has built its world (the same way <see cref="SceneAudioBoot"/>
    /// starts the scene audio), so no scenario script has to change and the standalone projects and the hub get the same look.
    /// Built-in pipeline, no packages, nothing baked into a scene file (every object is runtime only, <see cref="DontBake"/>):
    /// (1) lower flat ambient so light has something to do, plus a warm / cool split; (2) real fixture lights: soft-edged spot lights
    /// with a cookie, under every ceiling fixture, a handful per scene as pixel lights; (3) halos: one particle system draws a soft
    /// additive glow on every bright emissive fixture or lamp (a cheap stand-in for bloom); (4) one merged, vertex-coloured mesh of
    /// dark gradients at the foot of tall walls and at the ceiling line (ambient occlusion by hand). Start with <c>-pbNoLook</c> to
    /// switch it all off (used for before pictures). Desktop and headset behave the same. Not measured on a Vive Pro 2.
    /// </summary>
    public sealed class SceneLook : MonoBehaviour
    {
        public static bool AutoStart = true;
        public static bool Started;
        /// <summary>The look object of the running scene (null when none). Everything it made is below it.</summary>
        public static GameObject Root;

        float due;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { AutoStart = true; Started = false; Root = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Spawn();
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) { Started = false; Root = null; }
            Spawn();
        }

        static void Spawn()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("Scene look boot");
            go.AddComponent<DontBake>();
            go.AddComponent<SceneLook>();
        }

        static bool Flag(string name)
        {
            foreach (var a in System.Environment.GetCommandLineArgs()) if (a == name) return true;
            return false;
        }

        static bool BakeSession
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetString(Bake.StageKey, "") != "";
#else
                return false;
#endif
            }
        }

        void Awake() { due = Time.unscaledTime + 1.0f; }

        void Update()
        {
            if (Time.unscaledTime < due) return;
            if (AutoStart && !Started && !BakeSession && !Flag("-pbNoLook") && SceneAudio.TryPlaceOf(SceneManager.GetActiveScene().name, out var place))
            {
                Started = true;
                try { Apply(place); }
                catch (System.Exception e) { Debug.LogError("[look] failed: " + e); }
            }
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------------------------------------ profiles

        class Profile
        {
            public float AmbientScale = 1f;
            public Color AmbientTint = Color.white;
            public float KeyShadow = 0.5f, CastCut = 0.6f, CeilingY = 0f, KeyIntensity = 0f, KeyPitch = 62f, KeyYaw = 25f; public Color KeyColour = new Color(1f, 0.95f, 0.86f);
            public bool Halos = true; public float HaloSize = 1f, HaloAlpha = 0.75f;
            public float Patina = 0f, PatinaSize = 60f; public Color PatinaDark = new Color(0.14f, 0.11f, 0.08f), PatinaLight = new Color(0.62f, 0.6f, 0.55f);
            public float FloorGloss = 0f; public Color SunTint = Color.white; public float SunScale = 1f; public float FogStart = 0f, FogEnd = 0f; public Color FogTint = Color.white;
            public bool Grime = true; public float GrimeAlpha = 0.45f, GrimeHeight = 1.1f;
        }

        static Profile ProfileFor(Place p)
        {
            // Softened after owner feedback ("too harsh"): the key light is a gentle top light, shadows are weak and wide, ambient stays bright.
            switch (p)
            {
                case Place.Warehouse: return new Profile { AmbientScale = 1.0f, AmbientTint = new Color(1f, 0.99f, 0.97f), FogStart = 18f, FogEnd = 70f, KeyIntensity = 0.55f, KeyPitch = 62f, KeyYaw = 28f, KeyShadow = 0.45f, Patina = 0.5f, PatinaSize = 30f, FloorGloss = 0.38f, GrimeAlpha = 0.25f, HaloAlpha = 0.4f };
                case Place.Refinery: return new Profile { AmbientScale = 1.0f, Grime = false, SunTint = new Color(1f, 0.96f, 0.88f), SunScale = 1.0f, Patina = 0.6f, PatinaSize = 90f, FogStart = 35f, FogEnd = 150f, HaloAlpha = 0.35f };
                case Place.HydroBay: return new Profile { CeilingY = 3.4f, CastCut = 0.5f, AmbientScale = 1.0f, AmbientTint = new Color(1f, 0.99f, 0.97f), KeyIntensity = 0.6f, KeyPitch = 62f, KeyYaw = -35f, KeyShadow = 0.45f, Patina = 0.45f, PatinaSize = 20f, FloorGloss = 0.35f, GrimeAlpha = 0.25f, HaloAlpha = 0.4f };
                case Place.ResistorBench: return new Profile { CastCut = 0.5f, AmbientScale = 1.0f, AmbientTint = new Color(1f, 0.99f, 0.97f), KeyIntensity = 0.55f, KeyPitch = 62f, KeyYaw = 20f, KeyShadow = 0.45f, Patina = 0.35f, PatinaSize = 20f, FloorGloss = 0.3f, GrimeAlpha = 0.25f, HaloAlpha = 0.4f };
                default: return new Profile { AmbientScale = 0.95f, FloorGloss = 0.3f, Grime = false, SunTint = new Color(1f, 0.97f, 0.9f), SunScale = 1.0f, HaloAlpha = 0.35f };
            }
        }

        // ------------------------------------------------------------------------------------------------ apply

        public static void Apply(Place place)
        {
            var pf = ProfileFor(place);
            var root = new GameObject("Scene look");
            root.AddComponent<DontBake>();
            Root = root;

            // (1) ambient (ambientIntensity only scales a skybox ambient; trilight and flat colours have to be scaled directly)
            if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor *= pf.AmbientTint * pf.AmbientScale; RenderSettings.ambientEquatorColor *= pf.AmbientTint * pf.AmbientScale; RenderSettings.ambientGroundColor *= new Color(1.05f, 1f, 0.92f) * pf.AmbientScale;
            }
            else if (RenderSettings.ambientMode == AmbientMode.Flat) RenderSettings.ambientLight *= pf.AmbientTint * pf.AmbientScale;
            else RenderSettings.ambientIntensity *= pf.AmbientScale;

            // (2) key light: the interior "Key light" gets shadows and real strength (it was almost off), ceiling-level objects stop casting
            string key = KeyLight(pf);
            // (3) halos on bright emissive fixtures
            var em = FindEmitters();
            int shafts = BoostShafts(root.transform);
            int patches = Daylight(root.transform);
            int halos = pf.Halos ? Halos(root.transform, pf, em) : 0;
            // (4) wall-foot gradients
            int strips = pf.Grime ? Grime(root.transform, pf) : 0;
            Sun(pf);
            int gloss = pf.FloorGloss > 0f ? FloorSheen(pf.FloorGloss) : 0;
            string patina = pf.Patina > 0f ? Patina(root.transform, pf) : "none";
            Debug.Log($"[look] {place}: ambient x{pf.AmbientScale}, key light {key}, {halos} halos, {shafts} boosted light shafts, {patches} daylight patches, {strips} wall gradients, floor patina {patina}, {gloss} glossy floors");
        }

        // ------------------------------------------------------------------------------------------------ pieces

        struct Emitter { public Vector3 Pos; public Color Colour; public float Size; public bool Panel; }

        static List<Emitter> FindEmitters()
        {
            var list = new List<Emitter>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var m = r.sharedMaterial;
                if (m == null || !m.IsKeywordEnabled("_EMISSION") || !m.HasProperty("_EmissionColor")) continue;
                var e = m.GetColor("_EmissionColor");
                float peak = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
                if (peak < 1.0f) continue;
                var b = r.bounds;
                float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (big > 4f || big < 0.04f) continue;
                if (b.center.y < 0.2f) continue;
                bool panel = b.size.y < 0.35f && Mathf.Max(b.size.x, b.size.z) >= 0.4f && b.center.y >= 2.2f;
                list.Add(new Emitter { Pos = b.center, Colour = new Color(e.r, e.g, e.b) / peak, Size = big, Panel = panel });
                if (list.Count >= 200) break;
            }
            return list;
        }

        static int Halos(Transform parent, Profile pf, List<Emitter> em)
        {
            if (em.Count == 0) return 0;
            var go = new GameObject("Look halos");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.startLifetime = 1e6f; main.startSpeed = 0f; main.maxParticles = em.Count + 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emi = ps.emission; emi.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = Fx.ParticleMat(true);
            rend.shadowCastingMode = ShadowCastingMode.Off; rend.receiveShadows = false;
            rend.lightProbeUsage = LightProbeUsage.Off; rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
            ps.Play();
            foreach (var e in em)
            {
                float size = Mathf.Clamp(e.Size * (e.Panel ? 1.5f : 2.2f), 0.25f, 3.5f) * pf.HaloSize;
                var ep = new ParticleSystem.EmitParams { position = e.Pos + (e.Panel ? Vector3.down * 0.12f : Vector3.zero), velocity = Vector3.zero, startSize = size, startLifetime = 1e6f, startColor = new Color(e.Colour.r, e.Colour.g, e.Colour.b, pf.HaloAlpha) };
                ps.Emit(ep, 1);
            }
            return em.Count;
        }

        /// <summary>
        /// Interiors were lit by flat ambient plus a key light with shadows off and an intensity of 0.3 to 0.55, so every face of every box
        /// had the same brightness. This turns the existing "Key light" (directional, free in forward rendering: it is part of the base
        /// pass) into a real skylight: stronger, slanted, soft shadows, and everything at ceiling height stops casting so the roof does
        /// not block it. Returns a short description for the log.
        /// </summary>
        static string KeyLight(Profile pf)
        {
            if (Flag("-pbLookDebug"))
                foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if (r.bounds.size.x > 6f && r.bounds.size.z > 6f && r.bounds.size.y < 2f) Debug.Log($"[look-debug] slab {r.name} y {r.bounds.min.y:F2}..{r.bounds.max.y:F2} size {r.bounds.size.x:F1} x {r.bounds.size.z:F1}");
            if (pf.KeyIntensity <= 0f) return "unchanged";
            Light key = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled && (key == null || l.intensity > key.intensity)) key = l;
            if (key == null) return "none found";
            key.intensity = pf.KeyIntensity; key.color = pf.KeyColour;
            key.transform.rotation = Quaternion.Euler(pf.KeyPitch, pf.KeyYaw, 0f);
            key.shadows = LightShadows.Soft; key.shadowStrength = pf.KeyShadow; key.shadowBias = 0.04f; key.shadowNormalBias = 0.5f;
            // lowest bottom edge of a big flat slab above head height = the ceiling
            float ceil = float.MaxValue;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var b = r.bounds;
                if (b.size.x > 6.5f && b.size.z > 6.5f && b.size.y < 0.8f && b.center.y > 2.4f && b.min.y < ceil) ceil = b.min.y;
            }
            if (ceil == float.MaxValue && pf.CeilingY > 0f) ceil = pf.CeilingY;
            int cut = 0;
            if (ceil < float.MaxValue)
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (r.shadowCastingMode == ShadowCastingMode.Off) continue;
                    if (r.bounds.min.y >= ceil - pf.CastCut) { r.shadowCastingMode = ShadowCastingMode.Off; cut++; }
                }
            return $"{pf.KeyIntensity:F2} soft shadows, ceiling {(ceil < float.MaxValue ? ceil.ToString("F1") + " m" : "not found")}, {cut} ceiling renderers no longer cast";
        }

        /// <summary>A second, brighter additive copy of every "Fx Light shaft" (the originals are faint at 0.07). Own material, one draw call each.</summary>
        static int BoostShafts(Transform parent)
        {
            int n = 0; Material mat = null;
            foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mf.name != "Fx Light shaft" || mf.sharedMesh == null) continue;
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { name = "look-shaft", mainTexture = Fx.SoftTexture() };
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                }
                var go = new GameObject("Look shaft");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                n++;
            }
            return n;
        }

        /// <summary>Sunlight pooling on the floor inside every daylight window or open dock door (an additive soft patch, one quad each).</summary>
        static int Daylight(Transform parent)
        {
            int n = 0; Material mat = null;
            float floorY = 0f;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial;
                if (m == null || !m.HasProperty("_EmissionMap")) continue;
                var tex = m.GetTexture("_EmissionMap");
                if (tex == null || !tex.name.StartsWith("daylight-view")) continue;
                var b = r.bounds;
                var inward = new Vector3(-b.center.x, 0f, -b.center.z); // towards the middle of the scene
                // the wall normal: the thinnest horizontal axis of the pane
                Vector3 n3 = b.size.x < b.size.z ? new Vector3(Mathf.Sign(inward.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(inward.z));
                float width = Mathf.Max(b.size.x, b.size.z), height = b.size.y;
                float len = Mathf.Clamp(3f + b.center.y * 1.6f, 3.5f, 8f);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { name = "look-daylight", mainTexture = Fx.SoftTexture() };
                    if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(1f, 0.93f, 0.78f, 0.32f));
                }
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(go.GetComponent<Collider>());
                go.name = "Look daylight patch";
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(b.center.x, floorY + 0.02f, b.center.z) + n3 * (len * 0.5f + 0.2f);
                go.transform.rotation = Quaternion.LookRotation(n3, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = new Vector3(width * 1.15f + 0.6f, len, 1f);
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                n++;
            }
            return n;
        }

        static void Sun(Profile pf)
        {
            if (pf.FogStart > 0f) { RenderSettings.fogStartDistance = pf.FogStart; RenderSettings.fogEndDistance = pf.FogEnd; }
            RenderSettings.fogColor *= pf.FogTint;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled) { l.color *= pf.SunTint; l.intensity *= pf.SunScale; }
        }

        /// <summary>Gives big floor slabs a polished sheen (own material instance, so shared assets are not touched).</summary>
        static int FloorSheen(float gloss)
        {
            int n = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.name != "Floor" || r.bounds.size.x < 6f || r.bounds.size.z < 6f || r.bounds.size.y > 1f) continue;
                var m = r.material;
                if (m.HasProperty("_Glossiness")) { m.SetFloat("_Glossiness", gloss); n++; }
            }
            return n;
        }

        /// <summary>One big alpha-blended quad just above the floor or ground: low-frequency dirt, dust and oil patches that break up the repetition of the tiled floor texture.</summary>
        static string Patina(Transform parent, Profile pf)
        {
            MeshRenderer floor = null;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.name != "Floor" && r.name != "Ground") continue;
                if (r.bounds.size.x < 6f || r.bounds.size.z < 6f || r.bounds.size.y > 1f) continue;
                if (floor == null || r.bounds.size.x * r.bounds.size.z > floor.bounds.size.x * floor.bounds.size.z) floor = r;
            }
            if (floor == null) return "no floor found";
            var b = floor.bounds;
            float sx = Mathf.Min(b.size.x, pf.PatinaSize), sz = Mathf.Min(b.size.z, pf.PatinaSize);
            var centre = new Vector3(Mathf.Clamp(0f, b.min.x + sx / 2f, b.max.x - sx / 2f), b.max.y + 0.006f, Mathf.Clamp(0f, b.min.z + sz / 2f, b.max.z - sz / 2f));
            int n = pf.PatinaSize > 40f ? 1024 : 512;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "look-patina", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (float)x / n, v = (float)y / n;
                float broad = Mathf.PerlinNoise(u * 5f + 3.1f, v * 5f + 7.7f) * 0.6f + Mathf.PerlinNoise(u * 19f, v * 19f) * 0.4f;
                float dirt = Mathf.SmoothStep(0.5f, 0.78f, broad);
                float oil = Mathf.SmoothStep(0.72f, 0.82f, Mathf.PerlinNoise(u * 9f + 40f, v * 9f + 12f)) * 0.8f;
                float dust = Mathf.SmoothStep(0.55f, 0.85f, Mathf.PerlinNoise(u * 7f + 90f, v * 7f + 31f)) * (1f - dirt);
                float grain = Mathf.PerlinNoise(u * 220f, v * 220f);
                float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 14f); // feather the rim
                var col = Color.Lerp(pf.PatinaDark, pf.PatinaLight, dust * 0.9f);
                float a = Mathf.Clamp01(dirt * 0.5f + oil * 0.55f + dust * 0.28f) * (0.85f + grain * 0.3f) * pf.Patina * edge;
                if (oil > 0.1f) col = Color.Lerp(col, new Color(0.04f, 0.04f, 0.05f), oil);
                px[y * n + x] = new Color32((byte)(col.r * 255f), (byte)(col.g * 255f), (byte)(col.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));
            }
            tex.SetPixels32(px); tex.Apply(true);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Look floor patina";
            go.transform.SetParent(parent, false);
            go.transform.position = centre; go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); go.transform.localScale = new Vector3(sx, sz, 1f);
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { name = "look-patina", renderQueue = 2997, mainTexture = tex };
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return $"{sx:F0} x {sz:F0} m on {floor.name}";
        }

        static int Grime(Transform parent, Profile pf)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            int count = 0;
            var tint = new Color(0.03f, 0.025f, 0.02f);
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r.GetComponent<TextMesh>() != null) continue;
                var b = r.bounds;
                if (b.size.y < 2.4f || b.min.y > 0.4f || b.min.y < -0.3f) continue;
                float thin = Mathf.Min(b.size.x, b.size.z), longer = Mathf.Max(b.size.x, b.size.z);
                if (thin > 0.7f || longer < 3.5f || longer > 60f) continue;
                if (r.sharedMaterial != null && r.sharedMaterial.renderQueue >= 3000) continue;
                bool alongX = b.size.x >= b.size.z;
                float y0 = Mathf.Max(b.min.y, 0f) + 0.012f, y1 = y0 + pf.GrimeHeight;
                float yTop = b.max.y - 0.012f, yTop0 = yTop - 0.9f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float off = (alongX ? b.extents.z : b.extents.x) + 0.012f;
                    Vector3 a0, a1; // along the wall
                    if (alongX) { a0 = new Vector3(b.min.x, 0, b.center.z + side * off); a1 = new Vector3(b.max.x, 0, b.center.z + side * off); }
                    else { a0 = new Vector3(b.center.x + side * off, 0, b.min.z); a1 = new Vector3(b.center.x + side * off, 0, b.max.z); }
                    Quad(v, c, t, a0, a1, y0, y1, new Color(tint.r, tint.g, tint.b, pf.GrimeAlpha), new Color(tint.r, tint.g, tint.b, 0f));
                    Quad(v, c, t, a0, a1, yTop0, yTop, new Color(tint.r, tint.g, tint.b, 0f), new Color(tint.r, tint.g, tint.b, pf.GrimeAlpha * 0.7f));
                }
                count++;
                if (count >= 80) break;
            }
            if (count == 0) return 0;
            var mesh = new Mesh { name = "look-grime", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            var uv = new Vector2[v.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            mesh.uv = uv; mesh.RecalculateBounds();
            var go = new GameObject("Look wall gradients");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { name = "look-grime", renderQueue = 2998 };
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            mat.mainTexture = Texture2D.whiteTexture;
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return count;
        }

        static void Quad(List<Vector3> v, List<Color> c, List<int> t, Vector3 a0, Vector3 a1, float y0, float y1, Color cLow, Color cHigh)
        {
            int b = v.Count;
            v.Add(new Vector3(a0.x, y0, a0.z)); v.Add(new Vector3(a1.x, y0, a1.z)); v.Add(new Vector3(a1.x, y1, a1.z)); v.Add(new Vector3(a0.x, y1, a0.z));
            c.Add(cLow); c.Add(cLow); c.Add(cHigh); c.Add(cHigh);
            t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
        }
    }
}
