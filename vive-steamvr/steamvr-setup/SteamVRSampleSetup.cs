using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using Valve.VR;

/// <summary>
/// Sets up this project so Valve's own SteamVR plugin sample (Interactions_Example) builds and runs, with nothing of ours in the way.
/// Run in this order, each as its own Unity start (the input step makes Unity recompile):
///   -executeMethod SteamVRSampleSetup.Step1Input    copies Valve's example Steam Input actions and generates the action classes
///   -executeMethod SteamVRSampleSetup.Step2Project  OpenVR loader, Direct3D 11, the sample scene in Build Settings
///   -executeMethod SteamVRSampleSetup.BuildWindows  builds Builds/SteamVRSample/SteamVRSample.exe
/// </summary>
public static class SteamVRSampleSetup
{
    const string Scene = "Assets/SteamVR/InteractionSystem/Samples/Interactions_Example.unity";

    public static void Step1Input()
    {
        SteamVR_CopyExampleInputFiles.CopyFiles(true);
        AssetDatabase.Refresh();
        SteamVR_Input.InitializeFile(true, false);
        SteamVR_Input.actionFile.SaveHelperLists();
        SteamVR_Input.actionFile.Save(SteamVR_Input.GetActionsFilePath());
        SteamVR_Input_ActionManifest_Manager.CleanBindings(true);
        SteamVR_Input_Generator.BeginGeneration();
        AssetDatabase.SaveAssets();
        Debug.Log("[sample] Steam Input actions copied and generated: " + SteamVR_Input.GetActionsFilePath());
        EditorApplication.Exit(0);
    }

    public static void Step2Project()
    {
        var group = BuildTargetGroup.Standalone;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            Directory.CreateDirectory("Assets/XR");
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }
        if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
        if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
        var general = perTarget.SettingsForBuildTarget(group);
        general.InitManagerOnStart = true;
        var manager = perTarget.ManagerSettingsForBuildTarget(group);
        bool ok = XRPackageMetadataStore.AssignLoader(manager, "Unity.XR.OpenVR.OpenVRLoader", group);
        Debug.Log("[sample] OpenVR loader assigned: " + ok);
        EditorUtility.SetDirty(general); EditorUtility.SetDirty(manager); EditorUtility.SetDirty(perTarget);

        // Direct3D 11 only: Unity 6 plus SteamVR has known problems on Direct3D 12 (ValveSoftware/steamvr_unity_plugin issue 1129).
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        PlayerSettings.productName = "SteamVR Sample";
        PlayerSettings.companyName = "University of Pittsburgh at Bradford";
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[sample] project settings done");
        EditorApplication.Exit(0);
    }

    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = "Builds/SteamVRSample/SteamVRSample.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("[build] " + report.summary.result + " -> " + options.locationPathName);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
