using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using Valve.VR;

/// <summary>
/// Moves one of the training projects onto Valve's SteamVR plugin (copied into Assets/Editor by scripts/setup-steamvr.ps1, never committed to the project).
/// Run as separate Unity starts, in this order (the input step makes Unity recompile):
///   -executeMethod SteamVRProjectSetup.Step1Input    Valve's example Steam Input actions (trigger, grip, trackpad teleport, snap turn) and generated classes
///   -executeMethod SteamVRProjectSetup.Step2Project  OpenVR loader instead of OpenXR, Direct3D 11, "both" input systems, the PB_STEAMVR define that
///                                                   switches the kit's SteamVR backend on, and the Player and Teleporting prefabs the backend loads
/// </summary>
public static class SteamVRProjectSetup
{
    const string PlayerSource = "Assets/SteamVR/InteractionSystem/Core/Prefabs/Player.prefab";
    const string TeleportSource = "Assets/SteamVR/InteractionSystem/Teleport/Prefabs/Teleporting.prefab";
    const string ResourcesDir = "Assets/Resources/PBSteamVR";

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
        Debug.Log("[steamvr-setup] Steam Input actions copied and generated: " + SteamVR_Input.GetActionsFilePath());
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
        XRPackageMetadataStore.RemoveLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
        bool ok = XRPackageMetadataStore.AssignLoader(manager, "Unity.XR.OpenVR.OpenVRLoader", group);
        Debug.Log("[steamvr-setup] OpenVR loader assigned: " + ok);
        EditorUtility.SetDirty(general); EditorUtility.SetDirty(manager); EditorUtility.SetDirty(perTarget);

        // Direct3D 11 only: Unity 6 plus SteamVR has known problems on Direct3D 12 (ValveSoftware/steamvr_unity_plugin issue 1129).
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

        // The plugin's fallback code reads the old Input class, the kit's desktop rig uses the Input System: allow both.
        var projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var handler = projectSettings.FindProperty("activeInputHandler");
        if (handler != null && handler.intValue != 2) { handler.intValue = 2; projectSettings.ApplyModifiedProperties(); }

        // Switches on the kit's SteamVR backend (Runtime/SteamVR in the kit package).
        var target = NamedBuildTarget.Standalone;
        string defines = PlayerSettings.GetScriptingDefineSymbols(target);
        if (!defines.Contains("PB_STEAMVR")) PlayerSettings.SetScriptingDefineSymbols(target, string.IsNullOrEmpty(defines) ? "PB_STEAMVR" : defines + ";PB_STEAMVR");

        MakePrefab(PlayerSource, ResourcesDir + "/Player.prefab");
        MakePrefab(TeleportSource, ResourcesDir + "/Teleporting.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("[steamvr-setup] project settings done");
        EditorApplication.Exit(0);
    }

    // A variant of the plugin's own prefab in a Resources folder, so the backend can load it by name at run time.
    static void MakePrefab(string source, string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(source);
        if (prefab == null) { Debug.LogError("[steamvr-setup] missing " + source); return; }
        Directory.CreateDirectory(ResourcesDir);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        Debug.Log("[steamvr-setup] wrote " + path);
    }
}
