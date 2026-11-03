using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the lobby and the hub's scenario scenes (all saved under the git-ignored Assets/Scenes/Local, because they
/// may reference the Asset Store packs). Run Pitt-Bradford > Build hub: lobby and all scenarios.
/// Scenario scripts for projects 02-04 are copies in Assets/Scripts/Scenarios (kept in step with the project folders).
/// </summary>
public static class HubLobbyBuilder
{
    [MenuItem("Pitt-Bradford/Build hub: lobby and all scenarios")]
    public static void BuildAll()
    {
        HubDressing.BuildHydroTestBay(); // saves HydrostaticTest_Hub (dressed)
        BuildPlain<RefineryRounds>("RefineryRounds_Hub", new Vector3(-2.4f, 2.0f, 2.8f), true, HubSceneDressing.DressRefinery);
        BuildPlain<WarehouseForklift>("WarehouseForklift_Hub", new Vector3(-2.4f, 2.0f, 2.8f), false, HubSceneDressing.DressWarehouse);
        BuildPlain<ResistorBench>("ResistorBench_Hub", new Vector3(1.2f, 1.9f, 0.9f));
        BuildLobby();
        Debug.Log("[lobby] built " + EditorBuildSettings.scenes.Length + " scenes in build settings");
    }

    /// <summary>Rebuilds only the two dressed industrial scenes (they lose their bake; bake them again afterwards).</summary>
    [MenuItem("Pitt-Bradford/Build hub: warehouse and refinery only")]
    public static void BuildIndustrial()
    {
        BuildPlain<RefineryRounds>("RefineryRounds_Hub", new Vector3(-2.4f, 2.0f, 2.8f), true, HubSceneDressing.DressRefinery);
        BuildPlain<WarehouseForklift>("WarehouseForklift_Hub", new Vector3(-2.4f, 2.0f, 2.8f), false, HubSceneDressing.DressWarehouse);
    }

    static void BuildPlain<T>(string sceneName, Vector3 brandOffset, bool seasonSky = false, System.Action dress = null) where T : MonoBehaviour
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Scenario").AddComponent<T>();
        if (seasonSky)
        {
            // Outdoor scenario: the photo sky of today's season, held still (no rotation or glitch during training).
            // The scenario keeps its own sun and trilight ambient; the far fog is pulled halfway to the photo's horizon.
            var sky = AddSeasonSky();
            sky.Interval = 0f;
            sky.FogTint = 0.5f;
        }
        new GameObject("Return to lobby").AddComponent<ReturnToLobby>();
        dress?.Invoke(); // props and people from the local packs (HubSceneDressing)
        HubDressing.AddPolish(brandOffset, null, false, false); // these scenarios place their own university plate
        HubDressing.SaveScene(sceneName);
    }

    [MenuItem("Pitt-Bradford/Build hub: lobby only")]
    public static void BuildLobby()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Lobby hub").AddComponent<LobbyHub>();
        AddSeasonSky(); // four season photos, a new one every 30 s with the glitch transition
        HubDressing.AddPolish(null, new Vector3(-10.1f, 3.4f, 8.6f));

        // The hall keeps its own walls (the IGBlocks shell is far larger than the lobby); the packs only add props.
        var dressing = new GameObject("Dressing").transform;

        // Office furniture by the entrance as a reception corner
        var reception = new GameObject("Reception").transform;
        reception.SetParent(dressing, false);
        HubDressing.Spawn(reception, "Table 1", new Vector3(8.5f, 0f, -6.5f), 90f);
        HubDressing.Spawn(reception, "Chair 2", new Vector3(7.6f, 0f, -6.5f), -90f);
        HubDressing.Spawn(reception, "Plant 1", new Vector3(10.4f, 0f, -7.6f));
        HubDressing.Spawn(reception, "Water cooler", new Vector3(-9.5f, 0f, -7.2f), 90f);
        HubDressing.Spawn(reception, "Plant 1", new Vector3(-10.4f, 0f, -4.5f));

        Directory.CreateDirectory("Assets/Scenes/Local");
        HubDressing.SaveScene("Lobby");
    }

    /// <summary>Adds the kit's SeasonSky; the shader reference keeps the shader in player builds.</summary>
    static PittBradford.XR.SeasonSky AddSeasonSky()
    {
        var sky = new GameObject("Season sky").AddComponent<PittBradford.XR.SeasonSky>();
        sky.SkyShader = Shader.Find("PittBradford/SeasonSky");
        if (sky.SkyShader == null) Debug.LogWarning("[lobby] PittBradford/SeasonSky shader not found; scenes keep their own sky");
        return sky;
    }

    /// <summary>For -executeMethod HubLobbyBuilder.BuildAllCli.</summary>
    public static void BuildAllCli()
    {
        BuildAll();
        EditorApplication.Exit(0);
    }
}
