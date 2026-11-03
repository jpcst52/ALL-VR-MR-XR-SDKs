using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds LOCAL hub scenes that dress the training scenarios with the imported Asset Store packs
/// (IGBlocks warehouse, Sci-Fi Warehouse Kit, Modern Office, ...). The scenes are saved under Assets/Scenes/Local,
/// which is git-ignored: the packs' license does not allow redistributing their files.
/// If a pack is missing, its pieces are skipped with a log line and the scenario still runs from primitives.
/// </summary>
public static class HubDressing
{
    const string LocalScenes = "Assets/Scenes/Local";

    // ------------------------------------------------------------------------------------------ helpers

    internal static GameObject FindPrefab(string name)
    {
        var guids = AssetDatabase.FindAssets(name + " t:Prefab");
        string best = null;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(path) == name) { best = path; break; }
        }
        return best == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(best);
    }

    internal static GameObject Spawn(Transform parent, string prefabName, Vector3 position, float yaw = 0f, float scale = 1f)
    {
        var prefab = FindPrefab(prefabName);
        if (prefab == null) { Debug.LogWarning($"[dress] prefab not found: {prefabName}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = prefab.transform.localScale * scale; // keep the prefab's own root scale (some packs import at 100x and rely on 0.01 here)
        return go;
    }

    internal static Bounds Union(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>
    /// Centers a group of pieces on x/z = (0,0) and sets the top of its floor piece to y = 0.
    /// (The group's lowest point is not the walkable floor: pieces can extend below it.)
    /// </summary>
    internal static void CenterOnFloor(GameObject group, GameObject floorPiece)
    {
        var b = Union(group);
        float floorTop = floorPiece != null ? Union(floorPiece).max.y : b.min.y;
        group.transform.position -= new Vector3(b.center.x, floorTop, b.center.z);
        var after = Union(group);
        Debug.Log($"[dress] {group.name}: size {after.size.x:F1} x {after.size.y:F1} x {after.size.z:F1} m, floor top was {floorTop:F2} m, lowest point {after.min.y:F2} m");
    }

    /// <summary>Some imported materials use shaders that do not compile in this project (they render magenta); switch them to Standard.</summary>
    internal static void FixBrokenMaterials()
    {
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null)
                {
                    mats[i] = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.93f, 0.7f) };
                    mats[i].EnableKeyword("_EMISSION");
                    mats[i].SetColor("_EmissionColor", new Color(1f, 0.9f, 0.6f) * 1.5f);
                    changed = true;
                }
                else if (m.shader == null || m.shader.name.StartsWith("Hidden/Internal") || !m.shader.isSupported)
                {
                    Color c = m.HasProperty("_Color") ? m.color : new Color(0.55f, 0.55f, 0.58f);
                    Texture t = m.HasProperty("_MainTex") ? m.mainTexture : null;
                    m.shader = Shader.Find("Standard");
                    m.color = c;
                    if (t != null) m.mainTexture = t;
                    EditorUtility.SetDirty(m);
                    Debug.Log("[dress] fixed shader on material " + m.name);
                }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    internal static void SaveScene(string name)
    {
        FixBrokenMaterials();
        Directory.CreateDirectory(LocalScenes);
        string path = $"{LocalScenes}/{name}.unity";
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        AddToBuild(path);
        Debug.Log("[dress] scene saved: " + path);
    }

    /// <summary>Registers a scene in the build settings (keeps the others; the Lobby is always first).</summary>
    internal static void AddToBuild(string path)
    {
        var list = EditorBuildSettings.scenes.Where(x => x.path != path).ToList();
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.OrderBy(x => x.path.EndsWith("/Lobby.unity") ? 0 : 1).ToArray();
    }

    /// <summary>Adds the realism pass: HubPolish (audio and post-processing) and warm work lights over the equipment.</summary>
    internal static void AddPolish(Vector3? brandOffset = null, Vector3? brandWorld = null, bool bigHall = false, bool showBrand = true)
    {
        var polish = new GameObject("Polish").AddComponent<HubPolish>();
        polish.ShowBrand = showBrand;
        if (brandOffset.HasValue) polish.BrandOffset = brandOffset.Value;
        if (brandWorld.HasValue) { polish.UseWorldBrand = true; polish.BrandWorld = brandWorld.Value; polish.BrandWidth = 2.8f; }
        polish.Resources = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
        if (polish.Resources == null) Debug.LogWarning("[dress] PostProcessResources not found; post-processing will be skipped");
        if (bigHall)
        {
            // The warehouse shell is about 39 x 37 m; lamps on a grid keep the far floor and walls from going black
            for (int ix = -2; ix <= 2; ix++)
            for (int iz = -2; iz <= 2; iz++)
            {
                var l = new GameObject("Hall lamp").AddComponent<Light>();
                l.transform.SetParent(polish.transform, false);
                l.transform.position = new Vector3(ix * 7f, 6.5f, iz * 7f);
                l.type = LightType.Point; l.range = 14f; l.intensity = 1.6f;
                l.color = new Color(1f, 0.96f, 0.88f);
                l.shadows = LightShadows.None;
            }
        }
        foreach (var x in new[] { -2.5f, 2.5f })
        {
            var l = new GameObject("Work light").AddComponent<Light>();
            l.transform.SetParent(polish.transform, false);
            l.transform.position = new Vector3(x, 3.4f, 0.8f);
            l.type = LightType.Spot; l.range = 9f; l.spotAngle = 85f; l.intensity = 1.6f;
            l.color = new Color(1f, 0.9f, 0.75f);
            l.transform.rotation = Quaternion.Euler(90, 0, 0);
            l.shadows = LightShadows.None;
        }
    }

    /// <summary>Spawns a wall strip, rotates it, and pushes it against one edge of the floor (0 north, 1 south, 2 east, 3 west).</summary>
    internal static void PlaceWall(Transform parent, string name, float yaw, Bounds floor, int side)
    {
        var wall = Spawn(parent, name, Vector3.zero, yaw);
        if (wall == null) return;
        var wb = Union(wall);
        Vector3 target = wb.center;
        switch (side)
        {
            case 0: target = new Vector3(floor.center.x, wb.center.y, floor.max.z - wb.extents.z); break;
            case 1: target = new Vector3(floor.center.x, wb.center.y, floor.min.z + wb.extents.z); break;
            case 2: target = new Vector3(floor.max.x - wb.extents.x, wb.center.y, floor.center.z); break;
            default: target = new Vector3(floor.min.x + wb.extents.x, wb.center.y, floor.center.z); break;
        }
        wall.transform.position += target - wb.center;
    }

    // ------------------------------------------------------------------------------------------ test bay

    [MenuItem("Pitt-Bradford/Build hub scene: Hydrostatic Test Bay (local assets)")]
    public static void BuildHydroTestBay()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var scenario = new GameObject("Scenario");
        scenario.AddComponent<HydroTest>().ExternalShell = true;
        new GameObject("Return to lobby").AddComponent<ReturnToLobby>();
        AddPolish(new Vector3(-3.8f, 3.1f, 3.5f), null, true);

        var dressing = new GameObject("Dressing").transform;

        // The building: the IGBlocks "medium" warehouse shell
        var shell = new GameObject("Building");
        shell.transform.SetParent(dressing, false);
        var floorPiece = Spawn(shell.transform, "WareMedium_Floor", Vector3.zero);
        // The wall prefabs are single wall strips, so ring the floor with four of them (one with windows)
        var fb = Union(floorPiece);
        PlaceWall(shell.transform, "WareMedium_Walls", 0f, fb, 0);
        PlaceWall(shell.transform, "WareMedium_Walls_Wind", 180f, fb, 1);
        PlaceWall(shell.transform, "WareMedium_Walls", 90f, fb, 2);
        PlaceWall(shell.transform, "WareMedium_Walls", -90f, fb, 3);
        Spawn(shell.transform, "WareMedium_Roof", Vector3.zero);
        CenterOnFloor(shell, floorPiece);

        // Plant dressing from the Sci-Fi kit: pipe runs, barrels, pallets and crates around the edges
        var props = new GameObject("Plant props").transform;
        props.SetParent(dressing, false);
        Spawn(props, "Pipes 01", new Vector3(-6.4f, 0f, 1.0f), 90f);
        Spawn(props, "Pipes 03", new Vector3(-6.4f, 0f, 3.2f), 90f);
        Spawn(props, "Barrel", new Vector3(6.0f, 0f, 4.2f));
        Spawn(props, "Barrel", new Vector3(6.5f, 0f, 3.6f), 40f);
        Spawn(props, "Barrel", new Vector3(5.6f, 0f, 3.5f), 80f);
        Spawn(props, "Pallet", new Vector3(-5.6f, 0f, 4.6f), 10f);
        Spawn(props, "Crate Long", new Vector3(-5.6f, 0.15f, 4.6f), 10f);
        Spawn(props, "Pallet", new Vector3(-4.2f, 0f, 4.9f), 80f);
        Spawn(props, "Crate Short", new Vector3(-4.2f, 0.15f, 4.9f), 80f);
        Spawn(props, "Exit Sign", new Vector3(-3.6f, 3.2f, -5.7f), 0f);

        // Courtyard props (local copy in Assets/Courtyard Props): a generator, fuel cans and work lights
        Spawn(props, "PowerGenerator", new Vector3(6.3f, 0f, 0.6f), -90f);
        Spawn(props, "Gascan_Red", new Vector3(5.5f, 0f, 1.6f), 25f);
        Spawn(props, "Gascan_Yellow", new Vector3(5.2f, 0f, 1.2f), 110f);
        Spawn(props, "StandingLight", new Vector3(-6.0f, 0f, -2.0f), 60f);
        Spawn(props, "Construction Light Floor", new Vector3(3.0f, 0f, 5.4f), 200f);

        // Supervisor's corner from the Modern Office pack
        var office = new GameObject("Supervisor corner").transform;
        office.SetParent(dressing, false);
        Spawn(office, "Table 1", new Vector3(5.4f, 0f, -3.4f), 90f);
        Spawn(office, "Chair 2", new Vector3(4.5f, 0f, -3.4f), -90f);
        Spawn(office, "Computer 1", new Vector3(5.4f, 0.75f, -3.4f), 90f);
        Spawn(office, "Lamp 1", new Vector3(5.4f, 0.75f, -2.8f), 0f);
        Spawn(office, "File Cabinet", new Vector3(5.8f, 0f, -4.6f), 180f);
        Spawn(office, "Water cooler", new Vector3(6.2f, 0f, -1.6f), -90f);
        Spawn(office, "Plant 1", new Vector3(6.3f, 0f, -5.2f));

        SaveScene("HydrostaticTest_Hub");
    }

    /// <summary>For -executeMethod HubDressing.BuildHydroTestBayCli.</summary>
    public static void BuildHydroTestBayCli()
    {
        BuildHydroTestBay();
        EditorApplication.Exit(0);
    }
}
