using System.IO;
using PittBradford.XR.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sets up the hub project: OpenXR (Vive / Index / Touch profiles), the Input System, and the BUILT-IN render pipeline.
/// The imported asset packs (Courtyard, Modern Office, the warehouse kits) all use Standard-shader materials, so the
/// hub stays on the built-in pipeline; switching to URP would turn every one of them pink.
/// Run once after opening a fresh clone: Pitt-Bradford > Set up hub project.
/// </summary>
public static class HubSetup
{
    [MenuItem("Pitt-Bradford/Set up hub project")]
    public static void Run()
    {
        Configurator.ConfigureProject("Pitt-Bradford XR Training Hub");
        UseBuiltInPipeline();
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 45f;
        QualitySettings.antiAliasing = 4;
        AssetDatabase.SaveAssets();
        Debug.Log("[hub] setup complete (built-in render pipeline, OpenXR, Input System). Restart the editor if Unity asks.");
    }

    static void UseBuiltInPipeline()
    {
        GraphicsSettings.defaultRenderPipeline = null;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = null;
        }
        QualitySettings.SetQualityLevel(current, false);
        // Remove the URP assets created by an earlier version of this script.
        foreach (var path in new[] { "Assets/Settings/PB_URP.asset", "Assets/Settings/PB_Renderer.asset", "Assets/UniversalRenderPipelineGlobalSettings.asset", "Assets/DefaultVolumeProfile.asset" })
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
    }
}
