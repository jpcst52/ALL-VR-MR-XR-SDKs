using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Review helpers driven from the unity CLI (eval): open a hub scene, render desktop screenshots from a list of eye points,
/// and report scene statistics. Editor only; nothing here ships.
/// </summary>
public static class HubReview
{
    public static string Open(string scene)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Local/" + scene + ".unity", OpenSceneMode.Single);
        return scene + " objects=" + Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    /// <summary>spec: "px,py,pz>lx,ly,lz[>fov];..." ; files are written as dir/prefix-N.png.</summary>
    public static string Shoot(string dir, string prefix, string spec)
    {
        Directory.CreateDirectory(dir);
        int n = 1; var sb = new System.Text.StringBuilder();
        foreach (var part in spec.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var seg = part.Trim().Split('>');
            var p = seg[0].Split(','); var l = seg[1].Split(',');
            float fov = seg.Length > 2 ? float.Parse(seg[2], System.Globalization.CultureInfo.InvariantCulture) : 70f;
            System.Func<string, float> f = s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            string path = Path.Combine(dir, prefix + "-" + n + ".png");
            HubShots.Render(path, f(p[0]), f(p[1]), f(p[2]), f(l[0]), f(l[1]), f(l[2]), fov);
            sb.Append(path).Append('\n'); n++;
        }
        return sb.ToString();
    }

    /// <summary>Rebuilds only the dressing of the open lobby (fast loop, keeps the bake of everything else).</summary>
    public static string RedressLobby()
    {
        var old = GameObject.Find("Dressing"); if (old != null) Object.DestroyImmediate(old);
        var d = new GameObject("Dressing").transform;
        HubSceneDressing.DressLobby(d);
        return "lobby redressed, objects=" + Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
    }

    /// <summary>Samples each person's default animation clip at time t, so desktop renders in edit mode show real poses instead of T-poses (changes bone transforms; reopen the scene before saving).</summary>
    public static string PoseAll(float t)
    {
        int n = 0;
        foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            var c = a.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (c == null || c.layers.Length == 0) continue;
            var clip = c.layers[0].stateMachine.defaultState != null ? c.layers[0].stateMachine.defaultState.motion as AnimationClip : null;
            if (clip == null) continue;
            clip.SampleAnimation(a.gameObject, t + (a.transform.position.x * 0.37f) % 1f); n++;
        }
        return "posed " + n;
    }

    /// <summary>Rebuilds the dressing of the open scene from code (same calls as the builders), keeping the baked scenario.</summary>
    public static string Redress(string scene)
    {
        var old = GameObject.Find("Dressing"); if (old != null) Object.DestroyImmediate(old);
        switch (scene)
        {
            case "Lobby": return RedressLobby();
            case "WarehouseForklift_Hub": HubSceneDressing.DressWarehouse(); break;
            case "RefineryRounds_Hub": HubSceneDressing.DressRefinery(); break;
            case "ResistorBench_Hub": HubSceneDressing.DressResistor(); break;
            case "HydrostaticTest_Hub": HubDressing.RedressHydro(); break;
        }
        return scene + " redressed";
    }

    /// <summary>
    /// Renders one frame with a temporary camera and returns UnityStats (batches, SetPass calls, triangles, shadow casters) for it,
    /// plus the number of lights and renderers. Works in edit mode and play mode; static batching combines only in play mode or builds.
    /// </summary>
    public static string Perf(float px, float py, float pz, float lx, float ly, float lz)
    {
        var go = new GameObject("PerfCam");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = new Vector3(px, py, pz); cam.transform.LookAt(new Vector3(lx, ly, lz));
        cam.fieldOfView = 100f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f;
        var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt;
        cam.Render(); cam.Render();
        string r = $"batches={UnityEditor.UnityStats.batches} setPass={UnityEditor.UnityStats.setPassCalls} tris={UnityEditor.UnityStats.triangles} verts={UnityEditor.UnityStats.vertices} shadowCasters={UnityEditor.UnityStats.shadowCasters} dynBatched={UnityEditor.UnityStats.dynamicBatchedDrawCalls} staticBatched={UnityEditor.UnityStats.staticBatchedDrawCalls} instanced={UnityEditor.UnityStats.instancedBatchedDrawCalls}";
        cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(go);
        int lights = 0, rt_ = 0; foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.enabled && l.gameObject.activeInHierarchy) { lights++; if (l.type != LightType.Directional) rt_++; }
        int rend = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
        return r + $" lights={lights} (non-directional {rt_}) renderers={rend} playing={EditorApplication.isPlaying}";
    }

    public static string Lights()
    {
        var d = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            string k = l.name + "|" + l.type + "|" + l.shadows + "|i" + l.intensity.ToString("F1") + "|r" + l.range.ToString("F0") + "|" + l.transform.root.name;
            d[k] = d.ContainsKey(k) ? d[k] + 1 : 1;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in d) sb.Append(kv.Key + " x" + kv.Value + "; ");
        return sb.ToString();
    }

    /// <summary>Renderer counts per top-level object and per second-level child of the open scene (to see where the draw calls come from).</summary>
    public static string Groups(int top)
    {
        var d = new System.Collections.Generic.Dictionary<string, int[]>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var t = r.transform; var chain = new System.Collections.Generic.List<string>();
            while (t != null) { chain.Insert(0, t.name); t = t.parent; }
            string k = chain.Count > 2 ? chain[0] + "/" + chain[1] : chain[0];
            if (!d.ContainsKey(k)) d[k] = new int[3];
            d[k][0]++; if (r.gameObject.isStatic) d[k][1]++; if (r.sharedMaterials.Length > 1) d[k][2]++;
        }
        return string.Join(" ; ", d.OrderByDescending(x => x.Value[0]).Take(top).Select(x => x.Key + " renderers=" + x.Value[0] + " static=" + x.Value[1] + " multiMat=" + x.Value[2]));
    }

    static Bounds B(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    /// <summary>
    /// Lists every prefab instance under "Dressing" (name, centre, size, bottom y) that is suspicious: floating (bottom above 0.15 and not
    /// obviously on something), sunk (bottom below -0.1), or large (any size above maxSize), plus pairs of dressing props whose bounds overlap
    /// by more than the given fraction of the smaller one.
    /// </summary>
    public static string Audit(float maxSize, float overlapFrac)
    {
        var sb = new System.Text.StringBuilder();
        var items = new System.Collections.Generic.List<GameObject>();
        var dressing = GameObject.Find("Dressing");
        if (dressing == null) return "no Dressing root";
        foreach (var t in dressing.GetComponentsInChildren<Transform>())
            if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && t.GetComponentInChildren<Renderer>() != null) items.Add(t.gameObject);
        sb.AppendLine("items=" + items.Count);
        foreach (var g in items)
        {
            var b = B(g);
            string note = "";
            if (b.min.y < -0.12f) note += " SUNK(" + b.min.y.ToString("F2") + ")";
            if (b.size.x > maxSize || b.size.z > maxSize || b.size.y > maxSize) note += " BIG";
            if (note != "") sb.AppendLine($"{g.name} c=({b.center.x:F1},{b.center.y:F1},{b.center.z:F1}) size=({b.size.x:F1},{b.size.y:F1},{b.size.z:F1}){note}");
        }
        for (int i = 0; i < items.Count; i++)
            for (int j = i + 1; j < items.Count; j++)
            {
                var a = B(items[i]); var c = B(items[j]);
                if (!a.Intersects(c)) continue;
                var lo = Vector3.Max(a.min, c.min); var hi = Vector3.Min(a.max, c.max);
                var d = hi - lo; if (d.x <= 0 || d.y <= 0 || d.z <= 0) continue;
                float iv = d.x * d.y * d.z, sv = Mathf.Min(a.size.x * a.size.y * a.size.z, c.size.x * c.size.y * c.size.z);
                if (sv > 0.0001f && iv / sv > overlapFrac)
                    sb.AppendLine($"OVERLAP {items[i].name} ({a.center.x:F1},{a.center.z:F1}) x {items[j].name} ({c.center.x:F1},{c.center.z:F1}) {(iv / sv * 100):F0}%");
            }
        return sb.ToString();
    }

    /// <summary>Counts of renderers by prefab-ish name, lights, and the biggest triangle contributors (edit mode, before static batching).</summary>
    public static string Top(int count)
    {
        var list = new System.Collections.Generic.Dictionary<string, long[]>();
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf.sharedMesh == null) continue;
            var r = mf.GetComponent<Renderer>(); if (r == null || !r.enabled) continue;
            string key = mf.sharedMesh.name; long t = 0;
            for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) t += mf.sharedMesh.GetIndexCount(i) / 3;
            if (!list.ContainsKey(key)) list[key] = new long[2];
            list[key][0]++; list[key][1] += t;
        }
        return string.Join("\n", list.OrderByDescending(k => k.Value[1]).Take(count).Select(k => k.Key + " x" + k.Value[0] + " tris=" + k.Value[1]));
    }
}
