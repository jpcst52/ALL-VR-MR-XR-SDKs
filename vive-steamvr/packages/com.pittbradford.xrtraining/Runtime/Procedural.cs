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

        public static Material Mat(Color color, float metallic = 0f, float smoothness = 0.35f, Texture tex = null, float tiling = 1f)
        {
            string key = $"{color}{metallic}{smoothness}{(tex ? tex.name : "")}{tiling}";
            if (Cache.TryGetValue(key, out var cached) && cached) return cached;
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(shader) { color = color };
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            if (tex)
            {
                m.mainTexture = tex;
                m.mainTextureScale = new Vector2(tiling, tiling);
            }
            Cache[key] = m;
            return m;
        }

        public static Material Glow(Color color, float intensity = 1.6f)
        {
            var m = new Material(Shader.Find("Standard")) { color = color * 0.25f };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        public static Material Transparent(Color color)
        {
            var m = new Material(Shader.Find("Standard")) { color = color };
            m.SetFloat("_Mode", 3);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
            m.SetFloat("_Glossiness", 0.8f);
            return m;
        }

        // ------------------------------------------------------------------ textures

        public static Texture2D NoiseTexture(string name, Color a, Color b, float scale, int size = 256, float streak = 1f, int seed = 1)
        {
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

        // ------------------------------------------------------------------ shapes

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
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
                if (!font) font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Roboto", "sans-serif" }, 72);
                return font;
            }
        }

        public static TextMesh Text(Transform parent, string s, Vector3 localPos, float height, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = s;
            tm.fontSize = 72;
            tm.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            tm.characterSize = height / 72f * 10f;
            tm.anchor = anchor;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
            return tm;
        }

        // ------------------------------------------------------------------ audio

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

        public static AudioSource Speaker(Transform parent, AudioClip clip, bool loop = false, float volume = 1f, float spatial = 1f)
        {
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
