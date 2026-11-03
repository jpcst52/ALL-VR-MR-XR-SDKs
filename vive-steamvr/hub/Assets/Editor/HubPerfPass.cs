using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Cheap performance measures for the dressing: small props stop casting shadows and are culled by a one-level LODGroup when they cover
/// only a few pixels, people are culled the same way (and stop updating their animation when off screen). Called at the end of each
/// Dress* method so a rebuild reproduces it. There are no lower-detail meshes; this only removes what is too small or far to see.
/// </summary>
public static class HubPerfPass
{
    public static int Props, People;

    public static void Apply(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t == root) continue;
            var go = t.gameObject;
            bool person = go.GetComponent<Animator>() != null && go.GetComponentInChildren<SkinnedMeshRenderer>() != null;
            if (!person && !PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
            if (go.GetComponentInChildren<LODGroup>() != null || go.GetComponentInParent<LODGroup>() != null) continue;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float cull;
            if (person) cull = 0.02f;          // a 1.8 m person: culled beyond about 45 m (FOV 90)
            else if (size < 0.5f) cull = 0.04f; // a 0.4 m prop: culled beyond about 5 m
            else if (size < 1.2f) cull = 0.02f; // a 1 m prop: culled beyond about 25 m
            else continue;
            if (!person && size < 0.5f) foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lg = go.AddComponent<LODGroup>();
            lg.SetLODs(new[] { new LOD(cull, rs) });
            lg.fadeMode = LODFadeMode.None;
            lg.RecalculateBounds();
            if (person) { go.GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullUpdateTransforms; People++; } else Props++;
        }
    }

    /// <summary>
    /// Scenario groups that never move, per scene (paths under the scene root). The scenario script combines such meshes at Play in its own
    /// project, but a baked hub scene skips that, so each pallet-rack beam, wall piece and floor stripe stays a separate draw. Marking them
    /// batching-static lets Unity combine them when the scene loads.
    /// </summary>
    static readonly (string scene, string[] groups)[] StaticGroups =
    {
        ("WarehouseForklift_Hub", new[] { "Scenario/Racking", "Scenario/Shell", "Scenario/Markings" }),
    };

    [MenuItem("Pitt-Bradford/Hub: static-batch the baked scenes (run after baking)")]
    public static void StaticBatchBaked()
    {
        foreach (var (scene, groups) in StaticGroups)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/Local/{scene}.unity", OpenSceneMode.Single);
            int n = 0;
            foreach (var path in groups)
            {
                var t = GameObject.Find(path);
                if (t == null) { Debug.LogWarning($"[perf] {scene}: group {path} not found (bake first)"); continue; }
                foreach (var c in t.GetComponentsInChildren<Transform>(true))
                { GameObjectUtility.SetStaticEditorFlags(c.gameObject, StaticEditorFlags.BatchingStatic); n++; }
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"[perf] {scene}: {n} objects marked batching-static");
        }
    }
}
