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

    static GameObject FindPrefab(string name)
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

    static GameObject Spawn(Transform parent, string prefabName, Vector3 position, float yaw = 0f, float scale = 1f)
    {
        var prefab = FindPrefab(prefabName);
        if (prefab == null) { Debug.LogWarning($"[dress] prefab not found: {prefabName}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static Bounds Union(GameObject root)
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
    static void CenterOnFloor(GameObject group, GameObject floorPiece)
    {
        var b = Union(group);
        float floorTop = floorPiece != null ? Union(floorPiece).max.y : b.min.y;
        group.transform.position -= new Vector3(b.center.x, floorTop, b.center.z);
        var after = Union(group);
        Debug.Log($"[dress] {group.name}: size {after.size.x:F1} x {after.size.y:F1} x {after.size.z:F1} m, floor top was {floorTop:F2} m, lowest point {after.min.y:F2} m");
    }

    static void SaveScene(string name)
    {
        Directory.CreateDirectory(LocalScenes);
        string path = $"{LocalScenes}/{name}.unity";
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        Debug.Log("[dress] scene saved: " + path);
    }

    /// <summary>Spawns a wall strip, rotates it, and pushes it against one edge of the floor (0 north, 1 south, 2 east, 3 west).</summary>
    static void PlaceWall(Transform parent, string name, float yaw, Bounds floor, int side)
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
