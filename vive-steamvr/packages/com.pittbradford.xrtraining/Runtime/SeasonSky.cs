using System;
using System.Collections.Generic;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// A 360 sky that shows one photo per season (spring, summer, autumn, winter) and moves to the next one every
    /// <see cref="Interval"/> seconds with a short glitch transition: the sky breaks into mosaic blocks, gets monitor
    /// scan lines and a little colour flicker, swaps to the next season at the peak, then sharpens again.
    ///
    /// Images: equirectangular (2:1) pictures in a Resources folder, by default Resources/Skies/&lt;id&gt; plus an optional
    /// Resources/Skies/skies.json manifest ([{id,label,credit,isBradford}, ...] in season order). The kit ships 2k
    /// versions in Runtime/Resources/Skies; a project can put its own photos with the same names in its own Resources/Skies.
    /// When no image loads the component does nothing and the scene keeps its flat or procedural sky.
    /// </summary>
    public class SeasonSky : MonoBehaviour
    {
        [Serializable]
        public class Season
        {
            public string id;
            public string label;
            public string credit;
            public bool isBradford;
            [NonSerialized] public Texture2D Texture;
        }

        [Serializable] class Manifest { public Season[] items; }

        /// <summary>Seconds each season stays on screen. 0 or less: no automatic rotation.</summary>
        public float Interval = 30f;
        /// <summary>Length of the whole glitch transition in seconds (the swap happens half-way).</summary>
        public float TransitionSeconds = 1.1f;
        /// <summary>Largest mosaic block at the peak of the transition, in degrees of view.</summary>
        public float PeakBlockDegrees = 9f;
        /// <summary>Start on the season of today's date (northern hemisphere) instead of the first image.</summary>
        public bool StartWithCalendarSeason = true;
        /// <summary>Resources folder to load from.</summary>
        public string Folder = "Skies";
        /// <summary>Yaw of the photo, degrees.</summary>
        public float Rotation;
        public float Exposure = 1f;
        /// <summary>Paused: no automatic rotation (Next() still works).</summary>
        public bool Paused;
        /// <summary>Also pull the scene's fog colour towards the horizon colour of the current photo (0 = leave the fog alone).</summary>
        [Range(0, 1)] public float FogTint;
        /// <summary>Set by an editor builder so the shader is referenced by the scene and survives a player build.</summary>
        public Shader SkyShader;

        /// <summary>Fired after each swap (at the peak of the transition) and once at start.</summary>
        public event Action<Season> SeasonChanged;

        public readonly List<Season> Seasons = new List<Season>();
        public bool Active => material != null && Seasons.Count > 0;
        public int Index { get; private set; }
        public Season Current => Active ? Seasons[Index] : null;
        public string CurrentLabel => Current != null ? Current.label : "";
        public string CurrentId => Current != null ? Current.id : "";
        public bool Transitioning => transitionT >= 0f;

        Material material;
        Material previousSkybox;
        Color baseFog;
        float timer;
        float transitionT = -1f;
        int nextIndex;
        bool swapped;
        float holdPhase = -1f;

        static readonly int TexA = Shader.PropertyToID("_TexA"), TexB = Shader.PropertyToID("_TexB"),
            Blend = Shader.PropertyToID("_Blend"), Block = Shader.PropertyToID("_Block"), Scan = Shader.PropertyToID("_Scan"),
            Flicker = Shader.PropertyToID("_Flicker"), Seed = Shader.PropertyToID("_Seed"),
            Rot = Shader.PropertyToID("_Rotation"), Exp = Shader.PropertyToID("_Exposure");

        static readonly string[] DefaultIds = { "spring", "summer", "autumn", "winter" };
        static readonly string[] DefaultLabels = { "Spring", "Summer", "Autumn", "Winter" };

        void Awake()
        {
            Load();
            var shader = SkyShader != null ? SkyShader : Shader.Find("PittBradford/SeasonSky");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[sky] PittBradford/SeasonSky shader not available; keeping the scene's own sky");
                Seasons.Clear();
                return;
            }
            if (Seasons.Count == 0)
            {
                Debug.LogWarning($"[sky] no sky images in Resources/{Folder}; keeping the scene's own sky");
                return;
            }
            material = new Material(shader) { name = "Season sky" };
            Index = StartWithCalendarSeason ? CalendarIndex() : 0;
            ShowNow(Index);
        }

        void Start()
        {
            if (!Active) return;
            baseFog = RenderSettings.fogColor;
            Apply();
            SeasonChanged?.Invoke(Current);
        }

        void Load()
        {
            Seasons.Clear();
            Season[] list = null;
            var json = Resources.Load<TextAsset>(Folder + "/skies");
            if (json != null)
            {
                try
                {
                    var text = json.text.Trim();
                    if (text.StartsWith("[")) text = "{\"items\":" + text + "}";
                    list = JsonUtility.FromJson<Manifest>(text)?.items;
                }
                catch (Exception e) { Debug.LogWarning("[sky] could not read skies.json: " + e.Message); }
            }
            if (list == null || list.Length == 0)
            {
                list = new Season[DefaultIds.Length];
                for (int i = 0; i < list.Length; i++) list[i] = new Season { id = DefaultIds[i], label = DefaultLabels[i] };
            }
            foreach (var s in list)
            {
                if (s == null || string.IsNullOrEmpty(s.id)) continue;
                var tex = Resources.Load<Texture2D>(Folder + "/" + s.id);
                if (tex == null) { Debug.LogWarning($"[sky] missing image Resources/{Folder}/{s.id}; skipping that season"); continue; }
                tex.wrapModeU = TextureWrapMode.Repeat;
                tex.wrapModeV = TextureWrapMode.Clamp;
                if (string.IsNullOrEmpty(s.label)) s.label = char.ToUpper(s.id[0]) + s.id.Substring(1);
                s.Texture = tex;
                Seasons.Add(s);
            }
        }

        int CalendarIndex()
        {
            int m = DateTime.Now.Month;
            string id = m >= 3 && m <= 5 ? "spring" : m >= 6 && m <= 8 ? "summer" : m >= 9 && m <= 11 ? "autumn" : "winter";
            int i = Seasons.FindIndex(s => s.id == id);
            return i < 0 ? 0 : i;
        }

        void ShowNow(int index)
        {
            Index = index;
            var tex = Seasons[index].Texture;
            material.SetTexture(TexA, tex);
            material.SetTexture(TexB, tex);
            material.SetFloat(Blend, 0f);
            SetGlitch(0f);
        }

        void SetGlitch(float amount)
        {
            // Mosaic grows faster than linear so the first frames already look "broken"
            float a = Mathf.Clamp01(amount);
            material.SetFloat(Block, a <= 0.001f ? 0f : Mathf.Lerp(0.6f, PeakBlockDegrees, Mathf.Pow(a, 0.7f)));
            material.SetFloat(Scan, Mathf.Clamp01(a * 1.6f) * 0.85f);
            material.SetFloat(Flicker, Mathf.Clamp01(a * 1.3f) * 0.8f);
        }

        void Apply()
        {
            material.SetFloat(Rot, Mathf.Repeat(Rotation, 360f));
            material.SetFloat(Exp, Exposure);
            if (RenderSettings.skybox != material)
            {
                if (previousSkybox == null) previousSkybox = RenderSettings.skybox;
                RenderSettings.skybox = material;
            }
            var cam = Camera.main;
            if (cam != null && cam.clearFlags != CameraClearFlags.Skybox) cam.clearFlags = CameraClearFlags.Skybox;
            if (FogTint > 0f && Current != null)
                RenderSettings.fogColor = Color.Lerp(baseFog, HorizonColour(Current), FogTint);
        }

        /// <summary>Starts the transition to the next season now.</summary>
        public void Next()
        {
            if (!Active || Seasons.Count < 2 || Transitioning) return;
            nextIndex = (Index + 1) % Seasons.Count;
            transitionT = 0f;
            swapped = false;
            material.SetTexture(TexA, Seasons[Index].Texture);
            material.SetTexture(TexB, Seasons[nextIndex].Texture);
            material.SetFloat(Seed, UnityEngine.Random.value * 100f);
            timer = 0f;
        }

        /// <summary>Jumps straight to a season (no transition). Returns false if that id is not loaded.</summary>
        public bool Show(string id)
        {
            if (!Active) return false;
            int i = Seasons.FindIndex(s => s.id == id);
            if (i < 0) return false;
            transitionT = -1f;
            holdPhase = -1f;
            ShowNow(i);
            timer = 0f;
            Apply();
            SeasonChanged?.Invoke(Current);
            return true;
        }

        /// <summary>
        /// For screenshots: freezes the glitch at <paramref name="phase"/> (0..1 of the transition to the next season;
        /// 0.5 is the peak). A negative value releases it.
        /// </summary>
        public void HoldTransition(float phase)
        {
            if (!Active) return;
            holdPhase = phase;
            if (phase < 0f) { ShowNow(Index); return; }
            int n = (Index + 1) % Seasons.Count;
            material.SetTexture(TexA, Seasons[Index].Texture);
            material.SetTexture(TexB, Seasons[n].Texture);
            Evaluate(Mathf.Clamp01(phase));
        }

        void Evaluate(float p)
        {
            // Triangle 0 -> 1 -> 0 peaking half-way; the picture swaps over the middle 20%
            float glitch = 1f - Mathf.Abs(p * 2f - 1f);
            SetGlitch(Mathf.SmoothStep(0f, 1f, glitch));
            material.SetFloat(Blend, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.6f, p)));
        }

        void Update()
        {
            if (!Active) return;
            if (holdPhase >= 0f) { Apply(); return; }

            if (Transitioning)
            {
                transitionT += Time.deltaTime / Mathf.Max(0.1f, TransitionSeconds);
                float p = Mathf.Clamp01(transitionT);
                Evaluate(p);
                if (!swapped && p >= 0.5f)
                {
                    swapped = true;
                    Index = nextIndex;
                    SeasonChanged?.Invoke(Current);
                }
                if (p >= 1f)
                {
                    transitionT = -1f;
                    ShowNow(Index);
                }
            }
            else if (!Paused && Interval > 0f && Seasons.Count > 1)
            {
                timer += Time.deltaTime;
                if (timer >= Interval) Next();
            }
            Apply();
        }

        void OnDisable()
        {
            if (material != null && RenderSettings.skybox == material && previousSkybox != null) RenderSettings.skybox = previousSkybox;
        }

        // ------------------------------------------------------------------ horizon colour (for fog)

        readonly Dictionary<Texture2D, Color> horizon = new Dictionary<Texture2D, Color>();

        Color HorizonColour(Season s)
        {
            if (s.Texture == null) return baseFog;
            if (horizon.TryGetValue(s.Texture, out var c)) return c;
            // Average of a band just above the horizon, read back through a tiny render texture (the import may not be readable)
            var rt = RenderTexture.GetTemporary(32, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var prev = RenderTexture.active;
            Graphics.Blit(s.Texture, rt);
            RenderTexture.active = rt;
            var read = new Texture2D(32, 16, TextureFormat.RGBA32, false, true);
            read.ReadPixels(new Rect(0, 0, 32, 16), 0, 0);
            read.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Color sum = Color.black;
            int n = 0;
            for (int y = 8; y <= 9; y++)
            for (int x = 0; x < 32; x++) { sum += read.GetPixel(x, y); n++; }
            Destroy(read);
            c = (sum / Mathf.Max(1, n)).gamma; // the render texture holds linear values; RenderSettings wants gamma
            c.a = 1f;
            horizon[s.Texture] = c;
            return c;
        }
    }
}
