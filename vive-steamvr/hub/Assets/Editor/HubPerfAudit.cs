using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Diagnostics for the render-cost work (editor only): where do the renderers, materials, lights and shadow casters come from.</summary>
public static class HubPerfAudit
{
    public static string Summary()
    {
        var rs = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var mats = new HashSet<Material>(); int stat = 0, shadow = 0, multi = 0, ps = 0, skinned = 0, trans = 0, glow = 0, tiny = 0, tinyShadow = 0;
        long tris = 0;
        foreach (var r in rs)
        {
            if (!r.enabled) continue;
            if (r is ParticleSystemRenderer) { ps++; continue; }
            if (r is SkinnedMeshRenderer) skinned++;
            foreach (var m in r.sharedMaterials) if (m != null) { mats.Add(m); if (m.renderQueue >= 3000) trans++; if (m.IsKeywordEnabled("_EMISSION")) glow++; }
            if (r.gameObject.isStatic) stat++;
            if (r.sharedMaterials.Length > 1) multi++;
            bool sh = r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
            if (sh) shadow++;
            if (r.bounds.size.magnitude < 0.8f) { tiny++; if (sh) tinyShadow++; }
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) tris += mf.sharedMesh.GetIndexCount(i) / 3;
        }
        var psys = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        int maxP = psys.Sum(p => p.main.maxParticles);
        return $"renderers={rs.Length} uniqueMats={mats.Count} static={stat} castShadow={shadow} multiMat={multi} particleSys={psys.Length} maxParticlesSum={maxP} " +
               $"skinned={skinned} transparentMats={trans} glowMats={glow} tiny(<0.8m)={tiny} tinyCasting={tinyShadow} meshTris(edit)={tris} " +
               $"probes={Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Length} pixelLightCount={QualitySettings.pixelLightCount} shadows={QualitySettings.shadows} shadowDist={QualitySettings.shadowDistance} " +
               $"cascades={QualitySettings.shadowCascades} dynBatch={PlayerSettings.GetDynamicBatchingForPlatform(BuildTarget.StandaloneWindows64)} staticBatch={PlayerSettings.GetStaticBatchingForPlatform(BuildTarget.StandaloneWindows64)} " +
               $"quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} lodBias={QualitySettings.lodBias}";
    }

    /// <summary>Renderers per material (top n): name, count, shader, colour.</summary>
    public static string MaterialTop(int n)
    {
        var d = new Dictionary<Material, int>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials) if (m != null) d[m] = d.TryGetValue(m, out int c) ? c + 1 : 1;
        return string.Join("\n", d.OrderByDescending(x => x.Value).Take(n).Select(x => x.Value + " " + x.Key.name + " (" + x.Key.shader.name + ")"));
    }

    public static string Histogram()
    {
        var d = new Dictionary<int, int>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            int c = 0; foreach (var m in r.sharedMaterials) if (m != null) c++;
            // mats used by only one renderer
        }
        var cnt = new Dictionary<Material, int>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials) if (m != null) cnt[m] = cnt.TryGetValue(m, out int c) ? c + 1 : 1;
        int one = cnt.Count(x => x.Value == 1), few = cnt.Count(x => x.Value > 1 && x.Value <= 5), many = cnt.Count(x => x.Value > 5);
        return $"materials used by 1 renderer={one}, 2-5={few}, >5={many}";
    }

    public static string ShadowTop(int n)
    {
        var d = new Dictionary<string, int>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r.enabled || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off || r is ParticleSystemRenderer) continue;
            var t = r.transform; string k = t.name;
            while (t.parent != null && t.parent.parent != null) t = t.parent;
            string top = t.name + (t.parent != null ? "" : "");
            k = top + "/" + (r.transform.parent != null && r.transform.parent.parent != null ? r.transform.parent.name : r.name);
            d[k] = d.TryGetValue(k, out int c) ? c + 1 : 1;
        }
        return string.Join("\n", d.OrderByDescending(x => x.Value).Take(n).Select(x => x.Value + " " + x.Key));
    }

    /// <summary>Renderer counts by a two-level path (scene root / child) for all renderers.</summary>
    public static string Paths(int depth, int n, bool onlyDynamic = false)
    {
        var d = new Dictionary<string, int[]>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (onlyDynamic && (r.gameObject.isStatic || r is ParticleSystemRenderer)) continue;
            var chain = new List<string>(); var t = r.transform;
            while (t != null) { chain.Insert(0, t.name); t = t.parent; }
            string k = string.Join("/", chain.Take(depth));
            if (!d.ContainsKey(k)) d[k] = new int[2];
            d[k][0]++; if (r.gameObject.isStatic) d[k][1]++;
        }
        return string.Join("\n", d.OrderByDescending(x => x.Value[0]).Take(n).Select(x => x.Value[0] + " (static " + x.Value[1] + ") " + x.Key));
    }
}
