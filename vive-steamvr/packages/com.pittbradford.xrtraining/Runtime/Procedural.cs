using System.Collections.Generic;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Builds materials, textures, audio, and simple shapes from code so the example projects need no imported art.
    /// Everything here is deterministic and cheap enough for a standalone headset.
    /// </summary>
    public static class P
    {
        // Pitt brand colors (University of Pittsburgh brand guidelines)
        public static readonly Color Royal = new Color32(0x00, 0x35, 0x94, 255);
        public static readonly Color Gold = new Color32(0xFF, 0xB8, 0x1C, 255);
        public static readonly Color MediumBlue = new Color32(0x00, 0x20, 0x5B, 255);
        public static readonly Color Sky = new Color32(0xDB, 0xEE, 0xFF, 255);

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Font font;

        /// <summary>True when a Scriptable Render Pipeline (URP) is active; materials then use the URP Lit shader.</summary>
        public static bool Urp => UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        static Shader LitShader => (Urp ? Shader.Find("Universal Render Pipeline/Lit") : null) ?? Shader.Find("Standard");
        static string SmoothnessProperty => Urp ? "_Smoothness" : "_Glossiness";

        // Name fragments of textures that are pictures, not surfaces (signs, decals, stripes): they get no relief.
        static readonly string[] FlatTextureNames = { "label", "decal", "stain", "tyre", "spill", "dot", "soft", "stripe", "hazard", "brand", "sign", "logo", "poster", "font", "-nrm", "icon" };

        public static Material Mat(Color color, float metallic = 0f, float smoothness = 0.35f, Texture tex = null, float tiling = 1f)
        {
            string key = $"{color}{metallic}{smoothness}{(tex ? tex.name : "")}{tiling}";
            if (Cache.TryGetValue(key, out var cached) && cached) return cached;
            var m = new Material(LitShader) { color = color };
            m.SetFloat("_Metallic", metallic);
            m.SetFloat(SmoothnessProperty, smoothness);
            if (tex)
            {
                m.mainTexture = tex;
                m.mainTextureScale = new Vector2(tiling, tiling);
                var nrm = !Urp ? NormalFor(tex as Texture2D) : null;
                if (nrm != null)
                {
                    m.SetTexture("_BumpMap", nrm);
                    m.SetFloat("_BumpScale", metallic > 0.5f ? 0.3f : 0.75f);
                    m.EnableKeyword("_NORMALMAP");
                }
            }
            Cache[key] = m;
            return m;
        }

        static readonly Dictionary<string, Texture2D> NormalCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// A tangent-space normal map made from the fine detail of a generated albedo texture (its brightness minus a local average, so
        /// stains and colour patches stay flat and grain, aggregate, planks and cardboard flutes get relief). Same size and wrap as the
        /// source, so it tiles the same way. Returns null for picture-like textures (signs, decals, stripes) and unreadable ones.
        /// </summary>
        public static Texture2D NormalFor(Texture2D src, float strength = 1.8f)
        {
            if (src == null || !src.isReadable || src.width < 128 || src.width > 1024) return null;
            string lower = src.name.ToLowerInvariant();
            foreach (var f in FlatTextureNames) if (lower.Contains(f)) return null;
            string key = src.name + src.width + strength;
            if (NormalCache.TryGetValue(key, out var done) && done) return done;
            int w = src.width, h = src.height;
            var px = src.GetPixels32(0);
            var lum = new float[w * h];
            for (int i = 0; i < lum.Length; i++) lum[i] = (px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f) / 255f;
            // local average (wrapping box blur, radius 4, two separable passes), subtracted to keep only the fine detail
            var tmp = new float[w * h]; var avg = new float[w * h];
            const int r = 4;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) { float a = 0; for (int k = -r; k <= r; k++) a += lum[y * w + (x + k + w) % w]; tmp[y * w + x] = a / (2 * r + 1); }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) { float a = 0; for (int k = -r; k <= r; k++) a += tmp[((y + k + h) % h) * w + x]; avg[y * w + x] = a / (2 * r + 1); }
            var ht = new float[w * h];
            for (int i = 0; i < ht.Length; i++) ht[i] = lum[i] - avg[i] + avg[i] * 0.25f; // a little of the large shape too
            var t = new Texture2D(w, h, TextureFormat.RGB24, true, true) { name = src.name + "-nrm", wrapMode = src.wrapMode, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var o = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = ht[y * w + (x + 1) % w] - ht[y * w + (x - 1 + w) % w];
                float dy = ht[((y + 1) % h) * w + x] - ht[((y - 1 + h) % h) * w + x];
                var n = new Vector3(-dx * strength * 8f, -dy * strength * 8f, 1f).normalized;
                o[y * w + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
            }
            t.SetPixels32(o);
            t.Apply(true);
            NormalCache[key] = t;
            return t;
        }

        public static Material Glow(Color color, float intensity = 1.6f)
        {
            var m = new Material(LitShader) { color = color * 0.25f };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        /// <summary>
        /// A bright daylight view for a window, open dock door or skylight: a lit emissive picture (sky gradient with soft clouds, a hazy
        /// tree line and a pale yard) instead of a flat blue box. Light only, no extra objects; one material per tint.
        /// </summary>
        public static Material Daylight(float intensity = 1.25f)
        {
            var tex = Make("daylight-view", 256, (u, v) =>
            {
                var top = new Color(0.36f, 0.56f, 0.88f); var horizon = new Color(0.86f, 0.93f, 1f);
                float k = Mathf.Clamp01((v - 0.42f) / 0.58f);
                var c = Color.Lerp(horizon, top, Mathf.Pow(k, 0.8f));
                float cloud = Mathf.PerlinNoise(u * 4.5f + 3f, v * 9f + 1f) * 0.6f + Mathf.PerlinNoise(u * 12f, v * 24f) * 0.4f;
                c = Color.Lerp(c, Color.white, Mathf.Clamp01((cloud - 0.55f) * 2.2f) * Mathf.Clamp01(k * 2f + 0.2f));
                float ridge = 0.40f + Mathf.PerlinNoise(u * 14f + 9f, 2f) * 0.07f + Mathf.PerlinNoise(u * 60f, 7f) * 0.025f;
                if (v < ridge) c = Color.Lerp(new Color(0.36f, 0.44f, 0.36f), new Color(0.55f, 0.62f, 0.58f), Mathf.Clamp01((v - ridge + 0.1f) * 5f)); // hazy trees
                if (v < 0.27f) c = Color.Lerp(new Color(0.66f, 0.67f, 0.66f), new Color(0.72f, 0.73f, 0.72f), Mathf.PerlinNoise(u * 6f, v * 40f)); // yard
                return c;
            });
            string key = "daylight" + intensity;
            if (Cache.TryGetValue(key, out var cached) && cached) return cached;
            var m = new Material(LitShader) { color = Color.black };
            m.SetFloat("_Metallic", 0f);
            m.SetFloat(SmoothnessProperty, 0f);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", Color.white * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Cache[key] = m;
            return m;
        }

        public static Material Transparent(Color color)
        {
            var m = new Material(LitShader) { color = color };
            if (Urp)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                // 2 = "Fade" in the Standard shader's editor, which is what these keywords and blend modes are. (3 was
                // used before; it looked the same at runtime, but the editor rewrites a saved mode-3 material to
                // premultiplied "Transparent", so baked fences and puffs came out brighter.)
                m.SetFloat("_Mode", 2);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
            }
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = 3000;
            m.SetFloat(SmoothnessProperty, 0.8f);
            return m;
        }

        // ------------------------------------------------------------------ textures

        public static Texture2D NoiseTexture(string name, Color a, Color b, float scale, int size = 256, float streak = 1f, int seed = 1)
        {
            size = Mathf.Max(size, 512); // Vive Pro 2: 256 px is visibly soft at arm's length
            var t = new Texture2D(size, size, TextureFormat.RGB24, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var px = new Color[size * size];
            float ox = seed * 17.3f, oy = seed * 9.1f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = Mathf.PerlinNoise(ox + u * scale, oy + v * scale * streak) * 0.6f
                        + Mathf.PerlinNoise(ox + u * scale * 4f, oy + v * scale * 4f * streak) * 0.3f
                        + Mathf.PerlinNoise(ox + u * scale * 16f, oy + v * scale * 16f * streak) * 0.1f;
                n = Mathf.Clamp01(n + (Mathf.PerlinNoise(ox + u * 180f, oy + v * 180f) - 0.5f) * 0.14f); // fine grain
                px[y * size + x] = Color.Lerp(a, b, n);
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        public static Texture2D StripeTexture(string name, Color a, Color b, int stripes = 8, int size = 128)
        {
            var t = new Texture2D(size, size, TextureFormat.RGB24, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                t.SetPixel(x, y, ((x + y) * stripes / size) % 2 == 0 ? a : b);
            t.Apply(true);
            return t;
        }

        public static Material Concrete() => Mat(Color.white, 0f, 0.12f, NoiseTexture("concrete", new Color(0.34f, 0.34f, 0.35f), new Color(0.6f, 0.6f, 0.6f), 6f, 256, 1f, 3), 8f);
        public static Material Steel() => Mat(Color.white, 0.85f, 0.55f, NoiseTexture("steel", new Color(0.52f, 0.55f, 0.58f), new Color(0.78f, 0.8f, 0.83f), 3f, 256, 14f, 5), 1f);
        public static Material DarkSteel() => Mat(Color.white, 0.8f, 0.4f, NoiseTexture("darksteel", new Color(0.18f, 0.2f, 0.22f), new Color(0.34f, 0.36f, 0.4f), 3f, 256, 10f, 7), 1f);
        public static Material Hazard() => Mat(Color.white, 0f, 0.3f, StripeTexture("hazard", new Color(1f, 0.78f, 0.1f), new Color(0.08f, 0.08f, 0.08f), 8), 1f);
        public static Material Painted(Color c) => Mat(c, 0.1f, 0.5f);

        // ------------------------------------------------------------------ round 3: textures with structure

        static readonly Dictionary<string, Texture2D> TexCache = new Dictionary<string, Texture2D>();

        static Texture2D Make(string name, int size, System.Func<float, float, Color> px, bool mips = true, FilterMode filter = FilterMode.Trilinear)
        {
            if (TexCache.TryGetValue(name, out var cached) && cached) return cached;
            var t = new Texture2D(size, size, TextureFormat.RGB24, mips) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = filter, anisoLevel = 8 };
            var c = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++) c[y * size + x] = px((x + 0.5f) / size, (y + 0.5f) / size);
            t.SetPixels(c);
            t.Apply(mips);
            TexCache[name] = t;
            return t;
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519u);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        /// <summary>Sealed workshop floor: aggregate flecks, oil and water stains, scuff streaks, saw-cut joints (one slab per texture repeat).</summary>
        public static Material ConcreteFloor(float tiling = 8f)
        {
            var tex = Make("floor-sealed", 512, (u, v) =>
            {
                float n = Mathf.PerlinNoise(u * 6f + 4f, v * 6f + 9f) * 0.55f + Mathf.PerlinNoise(u * 26f, v * 26f) * 0.3f + Mathf.PerlinNoise(u * 90f, v * 90f) * 0.15f;
                float g = Mathf.Lerp(0.36f, 0.58f, n);
                float fleck = Hash((int)(u * 512), (int)(v * 512), 3);
                if (fleck > 0.985f) g += 0.12f; else if (fleck < 0.02f) g -= 0.1f;
                var c = new Color(g, g, g * 1.02f);
                float stain = Mathf.PerlinNoise(u * 3.1f + 21f, v * 3.1f + 5f);
                if (stain > 0.6f) c = Color.Lerp(c, new Color(0.17f, 0.15f, 0.13f), Mathf.Clamp01((stain - 0.62f) * 1.2f));
                float oil = Mathf.PerlinNoise(u * 5f + 60f, v * 5f + 30f);
                if (oil > 0.72f) c = Color.Lerp(c, new Color(0.08f, 0.08f, 0.09f), Mathf.Clamp01((oil - 0.74f) * 1.6f));
                float scuff = Mathf.PerlinNoise(u * 2f + 7f, v * 70f + 3f);
                if (scuff > 0.7f) c = Color.Lerp(c, new Color(0.22f, 0.22f, 0.22f), (scuff - 0.7f) * 1.2f);
                float jx = Mathf.Min(u, 1f - u), jy = Mathf.Min(v, 1f - v);
                if (jx < 0.004f || jy < 0.004f) c *= 0.5f;
                return c;
            });
            return Mat(Color.white, 0f, 0.3f, tex, tiling);
        }

        /// <summary>Painted wall with vertical roller marks, a little dirt and scuffing near the bottom of the texture.</summary>
        public static Material PaintedWall(Color paint, float tiling = 3f)
        {
            var tex = Make("wall-" + ColorUtility.ToHtmlStringRGB(paint), 256, (u, v) =>
            {
                float roller = Mathf.PerlinNoise(u * 40f + 3f, v * 2.5f) * 0.5f + Mathf.PerlinNoise(u * 120f, v * 8f) * 0.5f;
                float k = 0.92f + roller * 0.14f;
                float dirt = Mathf.PerlinNoise(u * 5f + 11f, v * 5f + 2f);
                k *= 1f - Mathf.Clamp01((dirt - 0.6f) * 0.22f) - Mathf.Clamp01(0.15f - v) * 0.15f;
                return paint * k;
            });
            return Mat(Color.white, 0f, 0.22f, tex, tiling);
        }

        /// <summary>Weathered steel: dark base with orange-brown rust blooms, for old supports, grates and brackets.</summary>
        public static Material Rusty()
        {
            var tex = Make("rusty-steel", 256, (u, v) =>
            {
                float n = Mathf.PerlinNoise(u * 4f + 2f, v * 4f + 8f) * 0.6f + Mathf.PerlinNoise(u * 18f, v * 18f) * 0.4f;
                var steel = Color.Lerp(new Color(0.2f, 0.21f, 0.22f), new Color(0.32f, 0.33f, 0.35f), n);
                float r = Mathf.Clamp01((Mathf.PerlinNoise(u * 6f + 40f, v * 6f + 17f) - 0.5f) * 3f);
                return Color.Lerp(steel, Color.Lerp(new Color(0.42f, 0.2f, 0.08f), new Color(0.62f, 0.32f, 0.12f), n), r);
            });
            return Mat(Color.white, 0.4f, 0.25f, tex, 1f);
        }

        /// <summary>Tread (diamond) plate for walkways and platforms.</summary>
        public static Material DiamondPlate(float tiling = 3f)
        {
            var tex = Make("diamond-plate", 256, (u, v) =>
            {
                float cu = Mathf.Repeat(u * 8f, 1f) - 0.5f, cv = Mathf.Repeat(v * 8f, 1f) - 0.5f;
                int cell = (int)(u * 8f) + (int)(v * 8f);
                bool flip = (cell & 1) == 0;
                float d = flip ? Mathf.Abs(cu + cv) * 0.9f + Mathf.Abs(cu - cv) * 0.25f : Mathf.Abs(cu - cv) * 0.9f + Mathf.Abs(cu + cv) * 0.25f;
                float lift = d < 0.3f ? 1f : 0f;
                float n = Mathf.PerlinNoise(u * 20f, v * 20f);
                float g = 0.42f + lift * 0.16f + n * 0.06f;
                return new Color(g, g * 1.02f, g * 1.06f);
            });
            return Mat(Color.white, 0.8f, 0.4f, tex, tiling);
        }

        /// <summary>Floor grating (dark slots across a steel grid) for trenches and drains.</summary>
        public static Material Grating(float tiling = 4f)
        {
            var tex = Make("grating", 128, (u, v) =>
            {
                float bar = Mathf.Repeat(u * 12f, 1f);
                bool slot = bar > 0.28f && Mathf.Repeat(v * 3f, 1f) > 0.08f;
                return slot ? new Color(0.03f, 0.03f, 0.035f) : new Color(0.38f, 0.4f, 0.43f);
            }, true, FilterMode.Bilinear);
            return Mat(Color.white, 0.7f, 0.4f, tex, tiling);
        }

        /// <summary>Wood boards with grain along the long side (pallets, benches).</summary>
        public static Material Wood()
        {
            var tex = Make("wood-boards", 256, (u, v) =>
            {
                float grain = Mathf.PerlinNoise(u * 2f + 5f, v * 38f) * 0.65f + Mathf.PerlinNoise(u * 9f, v * 120f) * 0.35f;
                var c = Color.Lerp(new Color(0.44f, 0.3f, 0.17f), new Color(0.66f, 0.5f, 0.3f), grain);
                float board = Mathf.Repeat(v * 4f, 1f);
                if (board < 0.03f) c *= 0.55f;
                return c;
            });
            return Mat(Color.white, 0f, 0.18f, tex, 1f);
        }

        /// <summary>Corrugated cardboard.</summary>
        public static Material Cardboard()
        {
            var tex = Make("cardboard", 128, (u, v) =>
            {
                float line = 0.5f + 0.5f * Mathf.Sin(v * 220f);
                float n = Mathf.PerlinNoise(u * 12f, v * 12f);
                float g = Mathf.Lerp(0.5f, 0.64f, n) * (0.94f + line * 0.06f);
                return new Color(g * 1.05f, g * 0.82f, g * 0.55f);
            });
            return Mat(Color.white, 0f, 0.1f, tex, 1f);
        }

        public static Material Rubber() => Mat(new Color(0.05f, 0.05f, 0.055f), 0f, 0.15f);
        public static Material Glass(float alpha = 0.18f) => Transparent(new Color(0.78f, 0.9f, 0.95f, alpha));

        // ------------------------------------------------------------------ baked scenes (adopt or create)

        /// <summary>
        /// An empty object called <paramref name="name"/> under <paramref name="parent"/> (null = scene root). Use it instead
        /// of <c>new GameObject(name)</c> + SetParent. In a baked scene it returns the saved object untouched (see <see cref="Bake"/>).
        /// </summary>
        public static GameObject Node(string name, Transform parent = null)
        {
            var found = Bake.TryAdopt(parent, name);
            if (found != null) return found;
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            Bake.Created(go);
            return go;
        }

        /// <summary>GetComponent, or AddComponent when the object does not have one yet (baked objects already do).</summary>
        public static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>Declares that this group (or only its direct children with the given names) never moves; see <see cref="StaticDetail"/>. Returns the object.</summary>
        public static GameObject Static(GameObject go, params string[] onlyChildNames)
        {
            if (go == null) return go;
            var sd = Ensure<StaticDetail>(go);
            if (onlyChildNames != null && onlyChildNames.Length > 0) sd.OnlyNames = onlyChildNames;
            return go;
        }

        /// <summary>
        /// True when the helper created the object this session; false when it was adopted from a baked scene. Guard code
        /// that sets up a fresh object (position, scale, component settings) so edits made in the scene are kept.
        /// </summary>
        public static bool IsNew(Object o) => !Bake.IsAdopted(o);

        // ------------------------------------------------------------------ shapes

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool collider = true)
        {
            var found = Bake.TryAdopt(parent, name);
            if (found != null) return found;
            var go = GameObject.CreatePrimitive(type);
            Bake.Created(go);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true) =>
            Prim(PrimitiveType.Cube, parent, name, pos, size, mat, collider);

        /// <summary>A cylinder of the given radius and length lying along the chosen axis.</summary>
        public static GameObject Cyl(Transform parent, string name, Vector3 pos, float radius, float length, Vector3 axis, Material mat, bool collider = true)
        {
            var go = Prim(PrimitiveType.Cylinder, parent, name, pos, new Vector3(radius * 2f, length / 2f, radius * 2f), mat, collider);
            if (Bake.IsAdopted(go)) return go;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            return go;
        }

        public static GameObject Ball(Transform parent, string name, Vector3 pos, float radius, Material mat, bool collider = true) =>
            Prim(PrimitiveType.Sphere, parent, name, pos, Vector3.one * radius * 2f, mat, collider);

        // ------------------------------------------------------------------ text

        public static Font Font
        {
            get
            {
                if (!font) font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Roboto", "sans-serif" }, 160);
                return font;
            }
        }

        static Material textMat;
        /// <summary>
        /// Font material that respects depth. Unity's default font shader ignores it, so text would show through walls
        /// and panels; PittBradford/Text depth-tests and uses the font atlas as an alpha mask.
        /// </summary>
        public static Material TextMaterial
        {
            get
            {
                if (textMat != null) return textMat;
                textMat = new Material(Shader.Find("PittBradford/Text") ?? Shader.Find("GUI/Text Shader")) { name = "pb-text" };
                textMat.mainTexture = Font.material.mainTexture;
                UnityEngine.Font.textureRebuilt += f => { if (f == Font && textMat != null) textMat.mainTexture = f.material.mainTexture; };
                return textMat;
            }
        }

        /// <summary>
        /// A text label. In a baked scene the saved TextMesh is adopted as it is (text, size, colour and position from the
        /// scene) and only its font and material are put back, because the OS font and its atlas exist only at runtime.
        /// </summary>
        public static TextMesh Text(Transform parent, string s, Vector3 localPos, float height, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, string name = "Text")
        {
            var found = Bake.TryAdopt(parent, name);
            // A scene baked before a text was given its own name still holds it as "Text". Without this, the new named text is made
            // next to the old one and every title shows twice (the Lobby did exactly that after its texts were named for TextFit).
            if (found == null && name != "Text") found = Bake.TryAdopt(parent, "Text");
            if (found != null)
            {
                var baked = Ensure<TextMesh>(found);
                baked.font = Font;
                found.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial;
                return baked;
            }
            var go = new GameObject(name);
            Bake.Created(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = s;
            tm.fontSize = 160; // large raster size keeps text crisp in a headset
            tm.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            tm.characterSize = height / 160f * 10f;
            tm.anchor = anchor;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial;
            return tm;
        }

        // ------------------------------------------------------------------ audio

        // Legacy bare sounds, kept so existing code keeps working. New code should call Sfx.Play / Sfx.Loop (see Sfx.cs).
        public static AudioClip Tone(string name, float frequency, float seconds, float volume = 0.4f, float decay = 4f)
        {
            int rate = 22050, n = (int)(rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * Mathf.Exp(-decay * t / seconds);
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Noise(string name, float seconds, float volume = 0.15f)
        {
            int rate = 22050, n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(4);
            float prev = 0;
            for (int i = 0; i < n; i++)
            {
                prev = prev * 0.9f + ((float)rng.NextDouble() * 2f - 1f) * 0.1f;
                data[i] = prev * volume * 4f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// An AudioSource on <paramref name="parent"/>. On a baked object the saved source (the n-th for the n-th call) is
        /// reused with its settings and only the generated clip is put back (generated clips are not saved).
        /// </summary>
        public static AudioSource Speaker(Transform parent, AudioClip clip, bool loop = false, float volume = 1f, float spatial = 1f)
        {
            if (Bake.IsAdopted(parent.gameObject))
            {
                int n = Bake.NextSpeaker(parent.gameObject);
                var existing = parent.GetComponents<AudioSource>();
                if (n < existing.Length)
                {
                    existing[n].clip = clip;
                    return existing[n];
                }
            }
            var src = parent.gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = loop;
            src.volume = volume;
            src.spatialBlend = spatial;
            src.playOnAwake = false;
            return src;
        }
    }
}
