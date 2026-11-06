using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Post-bake render-cost pass (runs at the end of <see cref="SceneBaker"/> and from the menu). It changes only flags and settings
    /// on objects already in the scene, so it never touches meshes and can be run again at any time:
    ///  1. groups marked with <see cref="StaticDetail"/> are made batching-static (Unity merges them when the scene loads);
    ///  2. small, thin, emissive or transparent renderers stop casting shadows (they are invisible in a shadow map anyway);
    ///  3. particle, line and trail renderers do not cast or receive shadows;
    ///  4. every non-directional light is capped: the most important ones stay pixel lights, the rest render as cheap vertex lights.
    /// The editor render counters before and after are logged by the hub's PerfProbe, not here.
    /// </summary>
    public static class RenderBudget
    {
        /// <summary>Renderers whose bounds diagonal is below this (metres) do not cast shadows.</summary>
        public const float MinShadowDiagonal = 1.3f;
        /// <summary>Renderers thinner than this (metres, smallest bounds dimension) do not cast shadows.</summary>
        public const float MinShadowThickness = 0.04f;
        /// <summary>Renderers whose second-smallest bounds dimension is below this (thin rods, rails, pipes) do not cast shadows.</summary>
        public const float MinShadowRod = 0.16f;
        /// <summary>Non-directional lights that stay pixel lights (the rest are vertex lights).</summary>
        public const int PixelLights = 2;

        [MenuItem("Pitt-Bradford/Render budget: apply to open scene", false, 42)]
        public static void ApplyMenu() => Debug.Log(Apply(SceneManager.GetActiveScene()));

        public static string Apply(Scene scene)
        {
            int made = 0, staticSet = 0, shadowOff = 0, vertexLights = 0;
            Debug.Log(ContactShadows.Build(scene)); // before the static pass, so it is merged with the rest
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var marker in root.GetComponentsInChildren<StaticDetail>(true))
                    staticSet += MarkStatic(marker.transform);
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    if (ShadowRule(r)) shadowOff++;
            }
            vertexLights = CapLights(roots);
            made = staticSet;
            EditorSceneManager.MarkSceneDirty(scene);
            return $"[render budget] {scene.name}: {made} renderers batching-static, {shadowOff} stopped casting shadows, {vertexLights} lights switched off (kept at most 4, range capped)";
        }

        // ---------------------------------------------------------------------------------------------- static

        static bool Dynamic(Component c)
        {
            return c is Rigidbody || c is Animator || c is Light || c is ParticleSystem || c is LODGroup || c is SkinnedMeshRenderer
                   || c is Cloth || c is ReflectionProbe || c is Camera || c is AudioSource
                   || (c is MonoBehaviour && !(c is StaticDetail));
        }

        static int MarkStatic(Transform group)
        {
            int n = 0;
            var only = group.GetComponent<StaticDetail>().OnlyNames;
            if (only == null || only.Length == 0) { Walk(group, false, ref n); return n; }
            for (int i = 0; i < group.childCount; i++)
            {
                var c = group.GetChild(i);
                if (System.Array.IndexOf(only, c.name) >= 0) Walk(c, false, ref n);
            }
            return n;
        }

        static void Walk(Transform t, bool dynamicAbove, ref int n)
        {
            bool dyn = dynamicAbove || t.GetComponents<Component>().Any(c => c != null && Dynamic(c));
            if (!dyn && t.GetComponent<MeshRenderer>() != null && t.GetComponent<TextMesh>() == null && t.GetComponent<MeshFilter>() != null
                && t.GetComponent<MeshFilter>().sharedMesh != null)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                if ((flags & StaticEditorFlags.BatchingStatic) == 0)
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags | StaticEditorFlags.BatchingStatic);
                n++;
            }
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), dyn, ref n);
        }

        // ---------------------------------------------------------------------------------------------- shadows

        static bool ShadowRule(Renderer r)
        {
            if (r.shadowCastingMode == ShadowCastingMode.Off) return false;
            bool off = false;
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer)
            {
                off = true;
                r.receiveShadows = false;
            }
            else if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                var b = r.bounds;
                var s = new[] { b.size.x, b.size.y, b.size.z };
                System.Array.Sort(s);
                if (b.size.magnitude < MinShadowDiagonal) off = true;
                else if (s[0] < MinShadowThickness) off = true;
                else if (s[1] < MinShadowRod) off = true;
                else if (b.size.magnitude < 2.2f && b.center.y > 0.8f) off = true;
                else
                    foreach (var m in r.sharedMaterials)
                        if (m != null && (m.renderQueue >= 3000 || (m.IsKeywordEnabled("_EMISSION") && EmissionBright(m)))) { off = true; break; }
            }
            if (off) r.shadowCastingMode = ShadowCastingMode.Off;
            return off;
        }

        /// <summary>Switched-off fill lights are made up with a little more ambient light (at most +35 %), so the rooms do not go dark.</summary>
        static void CompensateAmbient(int switchedOff)
        {
            float k = 1f + Mathf.Min(0.35f, 0.06f * switchedOff);
            if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Skybox) RenderSettings.ambientIntensity = Mathf.Min(RenderSettings.ambientIntensity * k, 4f);
            else if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor *= k; RenderSettings.ambientEquatorColor *= k; RenderSettings.ambientGroundColor *= k;
            }
            else if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat) RenderSettings.ambientLight *= k;
        }

        static bool EmissionBright(Material m)
        {
            if (!m.HasProperty("_EmissionColor")) return false;
            var c = m.GetColor("_EmissionColor");
            return Mathf.Max(c.r, c.g, c.b) > 0.05f;
        }

        // ---------------------------------------------------------------------------------------------- lights

        /// <summary>Longest range (metres) a kept practical light keeps; its intensity is raised to compensate (fewer objects get an extra pass).</summary>
        public const float MaxLightRange = 6f;
        /// <summary>A kept light's range shrinks (down to this) until it reaches fewer renderers than the remaining budget.</summary>
        public const float MinLightRange = 2.5f;
        /// <summary>Total renderers all kept lights may reach: each one costs an extra pass per light.</summary>
        public const int LightInfluenceBudget = 380;
        /// <summary>Directional lights up to this intensity do not cast realtime shadows (see CapLights).</summary>
        public const float KeyShadowMaxIntensity = 0.7f;

        static int CapLights(GameObject[] roots)
        {
            // Every pixel light adds a pass for every object it touches and makes neighbouring objects unbatchable (their light sets differ),
            // so this is the biggest render cost in the built-in pipeline. Keep the most important few, shorten their reach, switch off the rest.
            // A shadow-casting directional light makes Unity render the whole scene's depth again (screen-space shadows), which about doubles
            // the draw calls. Soft indoor key lights add little, so they stop casting; bright sun-like lights keep their shadows.
            foreach (var root in roots)
                foreach (var d in root.GetComponentsInChildren<Light>(false))
                    if (d.type == LightType.Directional && d.shadows != LightShadows.None && d.intensity <= KeyShadowMaxIntensity) d.shadows = LightShadows.None;
            var lights = new List<Light>();
            foreach (var root in roots) lights.AddRange(root.GetComponentsInChildren<Light>(false).Where(l => l.enabled && l.type != LightType.Directional));
            if (lights.Count == 0) return 0;
            var ordered = lights.OrderByDescending(l => (l.shadows != LightShadows.None ? 1000f : 0f) + (l.type == LightType.Spot ? 5f : 0f) + l.intensity * Mathf.Min(l.range, 12f)).ToList();
            int off = 0, budget = LightInfluenceBudget;
            var rends = new List<Renderer>();
            foreach (var root in roots) rends.AddRange(root.GetComponentsInChildren<Renderer>(false).Where(r => r is MeshRenderer && r.enabled));
            int Touched(Light l, float range)
            {
                int n = 0;
                foreach (var r in rends) if (r.bounds.SqrDistance(l.transform.position) <= range * range) n++;
                return n;
            }
            int kept = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                var l = ordered[i];
                if (kept >= PixelLights) { l.enabled = false; off++; continue; }
                float range = Mathf.Min(l.range, MaxLightRange), orig = l.range;
                int n = Touched(l, range);
                while (n > budget && range > MinLightRange) { range = Mathf.Max(MinLightRange, range - 0.5f); n = Touched(l, range); }
                if (n > budget) { l.enabled = false; off++; continue; }
                budget -= n; kept++;
                if (range < orig)
                {
                    l.intensity = Mathf.Min(l.intensity * Mathf.Sqrt(orig / range), 4f);
                    l.range = range;
                }
            }
            if (off > 0) CompensateAmbient(off);
            return off;
        }
    }
}
