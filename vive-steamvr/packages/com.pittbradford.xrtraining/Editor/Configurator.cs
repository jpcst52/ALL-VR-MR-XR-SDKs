using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// One-call project setup so a fresh clone is ready for SteamVR: OpenXR enabled with the Vive / Index / Touch
    /// controller profiles, the Input System selected, and shader variants kept for builds.
    /// Run from the menu (Pitt-Bradford > Configure project) or with -executeMethod.
    /// </summary>
    public static class Configurator
    {
        public static void ConfigureProject(string productName)
        {
            PlayerSettings.companyName = "University of Pittsburgh at Bradford";
            PlayerSettings.productName = productName;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            SetActiveInputHandler();
            EnableOpenXR(BuildTargetGroup.Standalone);
            KeepShaderVariants();
            AssetDatabase.SaveAssets();
            Debug.Log("[setup] project configured for OpenXR / SteamVR");
        }

        static void SetActiveInputHandler()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/ProjectSettings.asset");
            var so = new SerializedObject(asset);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != 1)
            {
                prop.intValue = 1; // Input System package only
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[setup] active input handling set to Input System (applies after an editor restart)");
            }
        }

        public static void EnableOpenXR(BuildTargetGroup group)
        {
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
            XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);

            // Controller profiles: HTC Vive wands, Valve Index, and Oculus/Meta Touch (so the same project runs on Quest).
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings != null)
            {
                foreach (var feature in settings.GetFeatures<OpenXRInteractionFeature>())
                {
                    string n = feature.GetType().Name;
                    feature.enabled = n.Contains("HTCVive") || n.Contains("ValveIndex") || n.Contains("OculusTouch") || n.Contains("MetaQuestTouch");
                    EditorUtility.SetDirty(feature);
                }
                EditorUtility.SetDirty(settings);
            }
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(perTarget);
        }

        /// <summary>Materials are created at runtime from "Standard", so make sure the shader and its variants survive a build.</summary>
        static void KeepShaderVariants()
        {
            var shaderNames = new[] { "Standard", "Sprites/Default", "Unlit/Color" };
            var gs = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/GraphicsSettings.asset");
            var so = new SerializedObject(gs);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in shaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                bool has = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) has = true;
                if (has) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Keep emission and alpha-blend variants of Standard by referencing them from a Resources material.
            Directory.CreateDirectory("Assets/Resources");
            const string path = "Assets/Resources/ShaderVariantKeeper.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                var m = new Material(Shader.Find("Standard"));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                AssetDatabase.CreateAsset(m, path);
                var t = new Material(Shader.Find("Standard"));
                t.SetFloat("_Mode", 3);
                t.EnableKeyword("_ALPHABLEND_ON");
                AssetDatabase.CreateAsset(t, "Assets/Resources/ShaderVariantKeeperAlpha.mat");
            }
        }

        public static void CreateScene<T>(string path) where T : MonoBehaviour
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Scenario").AddComponent<T>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            Debug.Log("[setup] scene saved: " + path);
        }

        public static void BuildWindows(string outputExe)
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outputExe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[build] " + report.summary.result + " -> " + outputExe);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }

    /// <summary>Opens the project's main scene and enters play mode; the runtime CaptureDriver does the rest.</summary>
    public static class CaptureRunner
    {
        public static void Run()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0) { Debug.LogError("[capture] no scene in build settings"); EditorApplication.Exit(2); return; }
            EditorSceneManager.OpenScene(scenes[0].path);
            EditorApplication.EnterPlaymode();
        }
    }
}
