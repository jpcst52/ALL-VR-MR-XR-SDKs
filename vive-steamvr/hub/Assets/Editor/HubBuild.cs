using PittBradford.XR.EditorTools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line player build for the hub (lobby plus the four scenarios), used by scripts/build-vive.ps1 -IncludeHub.
/// Builds the scenes enabled in Build Settings, Lobby first. The output contains licensed Asset Store packs and
/// Rocketbox people: keep it on the lab PC, never publish it.
/// </summary>
public static class HubBuild
{
    [MenuItem("Pitt-Bradford/Build Windows player (hub)")]
    public static void BuildWindows() => Configurator.BuildWindows("Builds/XRTrainingHub/XRTrainingHub.exe");
}

/// <summary>
/// SeasonSky finds its shader with Shader.Find and no scene references it, so a player build strips it and the lobby falls
/// back to a plain sky. Listing it under Always Included Shaders keeps it. Runs before every build of this project.
/// </summary>
public class HubKeepShaders : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => Keep("PittBradford/SeasonSky");

    static void Keep(string name)
    {
        var shader = Shader.Find(name);
        if (shader == null) { Debug.LogWarning("[hub build] shader not found: " + name); return; }
        var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var list = so.FindProperty("m_AlwaysIncludedShaders");
        for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[hub build] added to Always Included Shaders: " + name);
    }
}
