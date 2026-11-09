using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the glass building around the lobby as ordinary scene objects (saved in Lobby.unity, visible in the Hierarchy under "Glass building"):
/// steel mullions and transoms, one large tinted glass pane per wall with a frosted band at waist-to-eye height, a glazed roof on rafters,
/// a ground apron outside, and a modest reflection probe. Nothing is created at Play. Everything is procedural (no imported art): the frost
/// strip is a small texture written to Assets/Baked/LobbyGlass. The glass is a transparent Standard material with a tint and that baked strip; there is
/// no refraction and no grab pass, so it costs one transparent draw per pane (and the steel batches statically).
/// Menu: Pitt-Bradford > Lobby: rebuild glass building (open Lobby scene). It is also called by HubLobbyBuilder.BuildLobby.
/// </summary>
public static class HubLobbyGlass
{
    public const string RootName = "Glass building";
    const string Folder = "Assets/Baked/LobbyGlass";
    // The hall as LobbyHub builds it
    const float HallWidth = 24f, HallDepth = 18f, ParapetHeight = 1.05f, FrameHeight = 7.6f;
    const float FrostBottom = 1.12f, FrostTop = 1.95f, MullionStep = 3f;

    [MenuItem("Pitt-Bradford/Lobby: rebuild glass building (open Lobby scene)")]
    public static void RebuildInOpenScene()
    {
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>Menu-less entry for -executeMethod: opens Lobby.unity, rebuilds the glass, saves and quits.</summary>
    public static void RebuildAndExit()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Local/Lobby.unity", OpenSceneMode.Single);
        Build();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorApplication.Exit(0);
    }

    public static Transform Build()
    {
        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        EnsureFolder(Folder);

        var steel = Mat("lobby-steel", new Color(0.17f, 0.18f, 0.21f), 0.55f, 0.6f, false);
        var gold = Mat("lobby-gold-trim", new Color(1f, 0.72f, 0.11f), 0.5f, 0.55f, false);
        var clear = Mat("lobby-glass-clear", new Color(0.70f, 0.85f, 0.95f, 0.10f), 0f, 0.92f, true);
        var roofGlass = Mat("lobby-glass-roof", new Color(0.78f, 0.9f, 0.97f, 0.07f), 0f, 0.92f, true);
        var frost = Mat("lobby-glass-frost", new Color(0.93f, 0.97f, 1f, 1f), 0f, 0.35f, true);
        frost.mainTexture = FrostStrip();
        EditorUtility.SetDirty(frost);
        var ground = Mat("lobby-ground", new Color(0.30f, 0.33f, 0.31f), 0f, 0.08f, false);

        var root = new GameObject(RootName).transform;
        float w = HallWidth, d = HallDepth, hw = w / 2f, hd = d / 2f;
        float wallH = FrameHeight - ParapetHeight, wallY = ParapetHeight + wallH / 2f;

        // Mullions: vertical steel posts, a gold cap rail on top of the glass line
        var mull = Group(root, "Mullions");
        for (float x = -hw; x <= hw + 0.01f; x += MullionStep)
        {
            Box(mull, "Mullion N", new Vector3(x, wallY, hd), new Vector3(0.12f, wallH, 0.22f), steel);
            Box(mull, "Mullion S", new Vector3(x, wallY, -hd), new Vector3(0.12f, wallH, 0.22f), steel);
        }
        for (float z = -hd + MullionStep; z < hd - 0.01f; z += MullionStep)
        {
            Box(mull, "Mullion E", new Vector3(hw, wallY, z), new Vector3(0.22f, wallH, 0.12f), steel);
            Box(mull, "Mullion W", new Vector3(-hw, wallY, z), new Vector3(0.22f, wallH, 0.12f), steel);
        }

        // Transoms: horizontal rails at the top of the frosted band and at mid height
        var rails = Group(root, "Transoms");
        foreach (float y in new[] { FrostTop, 4.3f })
        {
            Box(rails, "Transom N", new Vector3(0, y, hd), new Vector3(w, 0.09f, 0.2f), steel);
            Box(rails, "Transom S", new Vector3(0, y, -hd), new Vector3(w, 0.09f, 0.2f), steel);
            Box(rails, "Transom E", new Vector3(hw, y, 0), new Vector3(0.2f, 0.09f, d), steel);
            Box(rails, "Transom W", new Vector3(-hw, y, 0), new Vector3(0.2f, 0.09f, d), steel);
        }
        Box(rails, "Gold sill N", new Vector3(0, FrostBottom - 0.04f, hd), new Vector3(w, 0.06f, 0.24f), gold);
        Box(rails, "Gold sill S", new Vector3(0, FrostBottom - 0.04f, -hd), new Vector3(w, 0.06f, 0.24f), gold);
        Box(rails, "Gold sill E", new Vector3(hw, FrostBottom - 0.04f, 0), new Vector3(0.24f, 0.06f, d), gold);
        Box(rails, "Gold sill W", new Vector3(-hw, FrostBottom - 0.04f, 0), new Vector3(0.24f, 0.06f, d), gold);

        // Panes: per wall one frosted band and one clear tinted pane above it (no shadows, no colliders)
        var panes = Group(root, "Glass panes");
        float frostH = FrostTop - FrostBottom, clearH = FrameHeight - FrostTop - 0.05f;
        Pane(panes, "Frost N", new Vector3(0, FrostBottom + frostH / 2f, hd), new Vector3(w - 0.2f, frostH, 0.02f), frost, w / 3f);
        Pane(panes, "Frost S", new Vector3(0, FrostBottom + frostH / 2f, -hd), new Vector3(w - 0.2f, frostH, 0.02f), frost, w / 3f);
        Pane(panes, "Frost E", new Vector3(hw, FrostBottom + frostH / 2f, 0), new Vector3(0.02f, frostH, d - 0.2f), frost, d / 3f);
        Pane(panes, "Frost W", new Vector3(-hw, FrostBottom + frostH / 2f, 0), new Vector3(0.02f, frostH, d - 0.2f), frost, d / 3f);
        Pane(panes, "Clear N", new Vector3(0, FrostTop + clearH / 2f, hd), new Vector3(w - 0.2f, clearH, 0.02f), clear, 1f);
        Pane(panes, "Clear S", new Vector3(0, FrostTop + clearH / 2f, -hd), new Vector3(w - 0.2f, clearH, 0.02f), clear, 1f);
        Pane(panes, "Clear E", new Vector3(hw, FrostTop + clearH / 2f, 0), new Vector3(0.02f, clearH, d - 0.2f), clear, 1f);
        Pane(panes, "Clear W", new Vector3(-hw, FrostTop + clearH / 2f, 0), new Vector3(0.02f, clearH, d - 0.2f), clear, 1f);

        // Roof: rafters across the hall and one very light glass sheet (the season sky still shows through it)
        var roof = Group(root, "Roof");
        for (float x = -hw + MullionStep; x < hw - 0.01f; x += MullionStep)
            Box(roof, "Rafter", new Vector3(x, FrameHeight + 0.1f, 0), new Vector3(0.12f, 0.22f, d), steel);
        for (float z = -hd + 4.5f; z < hd - 0.01f; z += 4.5f)
            Box(roof, "Purlin", new Vector3(0, FrameHeight + 0.17f, z), new Vector3(w, 0.07f, 0.1f), steel);
        Pane(roof, "Roof glass", new Vector3(0, FrameHeight + 0.01f, 0), new Vector3(w, 0.02f, d), roofGlass, 1f);

        // Ground outside the glass so the building stands on something (below the lobby floor so nothing z-fights)
        var gr = Group(root, "Ground");
        Box(gr, "Ground apron", new Vector3(0, -0.14f, 0), new Vector3(160f, 0.1f, 160f), ground);

        // Draw-call budget: unity did not batch these pieces in the editor (about 60 extra draws and 60 extra set-pass calls). Each material's pieces are
        // therefore also saved as ONE combined mesh (an object under "Combined", the ones that render); the individual parts stay in the scene,
        // switched off, as the editable source: change a part, then run the menu "Lobby: rebuild glass building" (or re-enable and combine).
        var combined = Group(root, "Combined (these render)");
        var byMat = new System.Collections.Generic.Dictionary<Material, System.Collections.Generic.List<MeshFilter>>();
        foreach (var part in new[] { mull, rails, panes, roof, gr })
            foreach (var mf in part.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.GetComponent<Renderer>().sharedMaterial;
                if (!byMat.TryGetValue(m, out var list)) byMat[m] = list = new System.Collections.Generic.List<MeshFilter>();
                list.Add(mf);
            }
        var rootInverse = root.worldToLocalMatrix;
        foreach (var kv in byMat)
        {
            var ci = new CombineInstance[kv.Value.Count];
            for (int i = 0; i < ci.Length; i++) { ci[i].mesh = kv.Value[i].sharedMesh; ci[i].transform = rootInverse * kv.Value[i].transform.localToWorldMatrix; }
            var mesh = new Mesh { name = kv.Key.name + "-combined", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(ci, true, true);
            string meshPath = $"{Folder}/{mesh.name}.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);
            var go = new GameObject(kv.Key.name + " (" + ci.Length + " parts)");
            go.transform.SetParent(combined, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = kv.Key;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        foreach (var part in new[] { mull, rails, panes, roof, gr }) part.gameObject.SetActive(false);

        // Reflection probe (baked once here, box sized to the hall); the glass and the metal pick up a modest sky reflection
        var probeGo = new GameObject("Lobby reflection probe");
        probeGo.transform.SetParent(root, false);
        probeGo.transform.position = new Vector3(0, 2.2f, 0);
        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
        probe.size = new Vector3(w + 2f, 9f, d + 2f);
        probe.resolution = 128;
        probe.intensity = 0.6f;
        probe.importance = 1;
        probe.boxProjection = true;
        try
        {
            string probePath = Folder + "/lobby-probe.exr";
            if (Lightmapping.BakeReflectionProbe(probe, probePath))
            {
                AssetDatabase.ImportAsset(probePath);
                probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(probePath);
            }
            else Debug.LogWarning("[glass] reflection probe bake returned false");
        }
        catch (System.Exception e) { Debug.LogWarning("[glass] reflection probe not baked: " + e.Message); }

        AssetDatabase.SaveAssets();
        int n = root.GetComponentsInChildren<Transform>(true).Length - 1;
        Debug.Log("[glass] built '" + RootName + "' with " + n + " objects (saved with the scene)");
        return root;
    }

    // ------------------------------------------------------------------------------------------ helpers

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        // Thin frame pieces are invisible in the shadow map and each shadow caster costs a draw: none of the building casts or receives shadows
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static GameObject Pane(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, float tileX)
    {
        var go = Box(parent, name, pos, size, mat);
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    /// <summary>A Standard material saved as an asset (opaque, or Fade for glass: tint colour with alpha, no refraction).</summary>
    static Material Mat(string name, Color color, float metallic, float smooth, bool transparent)
    {
        string path = $"{Folder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = m == null;
        if (isNew) m = new Material(Shader.Find("Standard"));
        m.color = color;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smooth);
        if (transparent)
        {
            m.SetFloat("_Mode", 2); // Fade
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
        m.enableInstancing = true;
        if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>
    /// The frost: a 512 x 64 strip, milky white with a little grain, clearer at the top and bottom edges, so the band reads as sandblasted glass.
    /// Written once as a PNG and reused (delete the file to regenerate).
    /// </summary>
    static Texture2D FrostStrip()
    {
        string path = Folder + "/frost-strip.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        const int W = 512, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float v = y / (float)(H - 1);
            float edge = Mathf.Clamp01(Mathf.Min(v, 1f - v) / 0.18f);          // clearer at the edges
            float grain = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.5f + Mathf.PerlinNoise(x * 0.07f, y * 0.9f) * 0.5f;
            float a = Mathf.Lerp(0.20f, 0.52f + 0.16f * grain, edge);
            float c = 0.94f + 0.05f * grain;
            px[y * W + x] = new Color(c, c, 1f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.mipmapEnabled = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
