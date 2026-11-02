using PittBradford.XR.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>Run once on a fresh clone: Pitt-Bradford > Set up project (or -executeMethod ProjectSetup.Run).</summary>
public static class ProjectSetup
{
    [MenuItem("Pitt-Bradford/Set up project")]
    public static void Run()
    {
        Configurator.ConfigureProject("Warehouse Forklift Safety");
        KeepShader("Unlit/Texture"); // the corner mirror shows a render texture with this shader
        Configurator.CreateScene<WarehouseForklift>("Assets/Scenes/WarehouseForklift.unity");
    }

    [MenuItem("Pitt-Bradford/Build Windows player")]
    public static void BuildWindows() => Configurator.BuildWindows("Builds/WarehouseForklift/WarehouseForklift.exe");

    static void KeepShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) return;
        var gs = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/GraphicsSettings.asset");
        var so = new SerializedObject(gs);
        var list = so.FindProperty("m_AlwaysIncludedShaders");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
