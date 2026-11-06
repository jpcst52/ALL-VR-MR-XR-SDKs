using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Baked-lighting spike for the hub scenes (built-in pipeline). Run from the command line (no -batchmode, it needs graphics):
///   Unity.exe -projectPath hub -executeMethod HubLightmap.BakeCli -pbScene Warehouse [-pbLmRes 12] [-pbLmSize 1024]
///             [-pbLmCpu] [-pbLmTimeout 1500] [-pbLmBaked] -logFile log.txt
///   Unity.exe -projectPath hub -executeMethod HubLightmap.ClearCli -pbScene Warehouse -logFile log.txt   (puts the scene back)
/// Prepare: second UV set for imported models (ModelImporter.generateSecondaryUV) and for saved meshes
/// (Unwrapping.GenerateSecondaryUVSet), static + ContributeGI on solid geometry, a light probe grid for moving objects,
/// lights set to Mixed (realtime direct light, baked indirect), LightingSettings asset, then Lightmapping.BakeAsync with a timeout.
/// Everything is local (the hub scenes and Baked folder are git-ignored); the scenes are rebuilt from code, so a re-bake of the
/// scene by SceneBaker or the builder removes the lightmap flags again.
/// </summary>
public static class HubLightmap
{
    static string Arg(string name)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
        return null;
    }
    static bool Flag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    static float NumArg(string name, float def) => float.TryParse(Arg(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;

    static string ScenePath()
    {
        string part = Arg("-pbScene");
        var hit = EditorBuildSettings.scenes.FirstOrDefault(s => part != null && s.path.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0);
        return hit?.path;
    }

    static bool Skip(Renderer r)
    {
        if (!(r is MeshRenderer)) return true;
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return true;
        if (r.GetComponent<TextMesh>() != null) return true;
        string n = r.gameObject.name;
        if (n.IndexOf("Contact", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (n.IndexOf("shaft", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Decal", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("pool", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        // anything that moves or is picked up must stay dynamic
        if (r.GetComponentInParent<Rigidbody>() != null) return true;
        foreach (var m in r.sharedMaterials)
        {
            if (m == null) continue;
            if (m.renderQueue >= 3000) return true;                          // transparent: glass, decals, shafts
            if (m.shader != null && m.shader.name.IndexOf("PBText", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    public static void BakeCli()
    {
        try { Run(); }
        catch (Exception e) { Debug.LogError("[lm] failed: " + e); EditorApplication.Exit(1); }
    }

    static double t0;
    static void Run()
    {
        t0 = EditorApplication.timeSinceStartup;
        string path = ScenePath();
        if (path == null) { Debug.LogError("[lm] no scene matches"); EditorApplication.Exit(2); return; }
        var scene = EditorSceneManager.OpenScene(path);
        Debug.Log("[lm] opened " + path);
        float res = NumArg("-pbLmRes", 12f);
        int size = (int)NumArg("-pbLmSize", 1024f);
        bool cpu = Flag("-pbLmCpu");
        float timeout = NumArg("-pbLmTimeout", 1500f);
        bool bakedLights = Flag("-pbLmBaked");

        // ---- 1. which renderers, which meshes
        var all = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var chosen = all.Where(r => !Skip(r)).ToList();
        Debug.Log($"[lm] mesh renderers {all.Length}, lightmap candidates {chosen.Count}");

        var meshes = new HashSet<Mesh>(chosen.Select(r => r.GetComponent<MeshFilter>().sharedMesh));
        int builtin = 0, models = 0, assets = 0, noPath = 0, already = 0, unwrapped = 0;
        var modelPaths = new HashSet<string>();
        foreach (var m in meshes)
        {
            string p = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(p)) { noPath++; continue; }
            if (p.StartsWith("Library/") || p.Contains("unity default resources") || p.Contains("unity_builtin_extra")) { builtin++; continue; }
            var imp = AssetImporter.GetAtPath(p);
            if (imp is ModelImporter) { models++; modelPaths.Add(p); }
            else assets++;
        }
        Debug.Log($"[lm] meshes {meshes.Count}: builtin {builtin}, model files {modelPaths.Count} ({models} meshes), saved .asset {assets}, no path {noPath}");

        // ---- 2. second UV sets
        foreach (var p in modelPaths)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(p);
            if (mi.generateSecondaryUV) { already++; continue; }
            mi.generateSecondaryUV = true;
            mi.SaveAndReimport();
            Debug.Log("[lm] secondary UV on " + p);
        }
        foreach (var m in meshes)
        {
            string p = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(p) || AssetImporter.GetAtPath(p) is ModelImporter || p.StartsWith("Library/") || p.Contains("unity default resources") || p.Contains("unity_builtin_extra")) continue;
            if (m.uv2 != null && m.uv2.Length == m.vertexCount && m.vertexCount > 0) { already++; continue; }
            if (!m.isReadable) { Debug.LogWarning("[lm] mesh not readable: " + p); continue; }
            Unwrapping.GenerateSecondaryUVSet(m);
            EditorUtility.SetDirty(m);
            unwrapped++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[lm] uv2 already present {already}, generated {unwrapped}");

        // ---- 3. static flags, emission
        int flagged = 0, emissive = 0;
        foreach (var r in chosen)
        {
            var go = r.gameObject;
            var f = GameObjectUtility.GetStaticEditorFlags(go);
            var nf = f | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            if (nf != f) { GameObjectUtility.SetStaticEditorFlags(go, nf); flagged++; }
            var so = new SerializedObject(r);
            var prop = so.FindProperty("m_ReceiveGI");
            if (prop != null && prop.intValue != 1) { prop.intValue = 1; so.ApplyModifiedProperties(); }   // 1 = lightmaps
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !m.IsKeywordEnabled("_EMISSION")) continue;
                if (m.GetColor("_EmissionColor").maxColorComponent <= 0.01f) continue;
                if (m.globalIlluminationFlags != MaterialGlobalIlluminationFlags.BakedEmissive)
                { m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; EditorUtility.SetDirty(m); emissive++; }
            }
        }
        Debug.Log($"[lm] static+ContributeGI set on {flagged} objects, emissive materials set to baked {emissive}");

        // ---- 4. lights: Mixed (realtime direct, baked indirect), or fully baked with -pbLmBaked
        int lights = 0;
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(l);
            var mode = so.FindProperty("m_Lightmapping");
            if (mode == null) continue;
            if (l.type == LightType.Point || l.type == LightType.Spot || l.type == LightType.Directional)
            {
                if (l.renderMode == LightRenderMode.ForceVertex && !bakedLights) { Debug.Log("[lm] vertex light left realtime: " + l.name); continue; }
                mode.intValue = bakedLights ? 2 : 1;   // 2 baked, 1 mixed, 4 realtime
                so.ApplyModifiedProperties();
                l.bounceIntensity = 1f;
                lights++;
                Debug.Log($"[lm] light {l.name} {l.type} range {l.range} intensity {l.intensity} -> {(bakedLights ? "Baked" : "Mixed")}");
            }
        }

        // ---- 5. light probes for moving things (forklift, people)
        var old = GameObject.Find("LM Probes"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        Bounds b = default; bool have = false;
        foreach (var r in chosen) { if (!have) { b = r.bounds; have = true; } else b.Encapsulate(r.bounds); }
        if (have)
        {
            var pg = new GameObject("LM Probes").AddComponent<LightProbeGroup>();
            var pos = new List<Vector3>();
            float step = 3f;
            for (float x = b.min.x + 1f; x < b.max.x; x += step)
                for (float z = b.min.z + 1f; z < b.max.z; z += step)
                    foreach (float y in new[] { 0.8f, 2.4f }) pos.Add(new Vector3(x, y, z));
            pg.probePositions = pos.ToArray();
            Debug.Log($"[lm] probes {pos.Count} over bounds {b.size}");
        }

        // ---- 6. lighting settings
        string name = Path.GetFileNameWithoutExtension(path);
        string lsPath = "Assets/Baked/" + name + "_LightingSettings.lighting";
        var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lsPath);
        if (ls == null) { ls = new LightingSettings(); AssetDatabase.CreateAsset(ls, lsPath); }
        ls.bakedGI = true; ls.realtimeGI = false;
        ls.lightmapper = cpu ? LightingSettings.Lightmapper.ProgressiveCPU : LightingSettings.Lightmapper.ProgressiveGPU;
        ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
        ls.lightmapResolution = res;
        ls.lightmapPadding = 2;
        ls.lightmapMaxSize = size;
        ls.lightmapCompression = LightmapCompression.NormalQuality;
        ls.directionalityMode = LightmapsMode.NonDirectional;
        ls.ao = true; ls.aoMaxDistance = 1.2f; ls.aoExponentIndirect = 1f; ls.aoExponentDirect = 0f;
        ls.directSampleCount = 32; ls.indirectSampleCount = 128; ls.environmentSampleCount = 128;
        ls.maxBounces = 3;
        ls.indirectScale = NumArg("-pbLmIndirect", 1.4f);
        ls.albedoBoost = 1f;
        EditorUtility.SetDirty(ls);
        Lightmapping.lightingSettings = ls;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[lm] settings: {(cpu ? "CPU" : "GPU")} res {res} texels/m, max {size}, mixed {(!bakedLights)}; prepared in {EditorApplication.timeSinceStartup - t0:0.0} s; baking");

        // ---- 7. bake with a watchdog
        double bakeStart = EditorApplication.timeSinceStartup;
        if (!Lightmapping.BakeAsync()) { Debug.LogError("[lm] BakeAsync refused to start"); EditorApplication.Exit(3); return; }
        EditorApplication.update += () =>
        {
            double el = EditorApplication.timeSinceStartup - bakeStart;
            if (Lightmapping.isRunning)
            {
                if (el > timeout) { Debug.LogError("[lm] timeout, cancelling"); Lightmapping.Cancel(); EditorApplication.Exit(4); }
                return;
            }
            if (done) return;
            done = true;
            Finish(scene, el);
        };
    }
    static bool done;

    static void Finish(UnityEngine.SceneManagement.Scene scene, double bakeSeconds)
    {
        var lms = LightmapSettings.lightmaps;
        long bytes = 0; var sb = new System.Text.StringBuilder();
        foreach (var d in lms)
        {
            foreach (var t in new[] { d.lightmapColor, d.lightmapDir, d.shadowMask })
            {
                if (t == null) continue;
                long s = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
                bytes += s; sb.Append($" {t.name} {t.width}x{t.height} {s / 1048576.0:0.00} MB;");
            }
        }
        Debug.Log($"[lm] bake done in {bakeSeconds:0.0} s (total run {EditorApplication.timeSinceStartup - t0:0.0} s). lightmaps {lms.Length}, memory {bytes / 1048576.0:0.00} MB:{sb}");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }

    /// <summary>Turns the scene back to dynamic lighting: clears lightmap data, static flags, probes and light modes.</summary>
    public static void ClearCli()
    {
        string path = ScenePath();
        var scene = EditorSceneManager.OpenScene(path);
        Lightmapping.Clear();
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var f = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, f & ~StaticEditorFlags.ContributeGI);
        }
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(l); var m = so.FindProperty("m_Lightmapping");
            if (m != null) { m.intValue = 4; so.ApplyModifiedProperties(); }
        }
        var g = GameObject.Find("LM Probes"); if (g != null) UnityEngine.Object.DestroyImmediate(g);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("[lm] cleared");
        EditorApplication.Exit(0);
    }
}
