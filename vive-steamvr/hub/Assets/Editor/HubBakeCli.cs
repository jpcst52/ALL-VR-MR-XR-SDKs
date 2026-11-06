using System.Linq;
using UnityEditor;
using UnityEngine;
using PittBradford.XR.EditorTools;

/// <summary>
/// Headless helpers for the hub (no -batchmode; the bake needs play mode):
///   Unity.exe -projectPath hub -executeMethod HubBakeCli.BakeScene -pbScene Warehouse -logFile log.txt
/// moves the named scene to the front of the build settings (the bake and the capture runner read the first one) and bakes it.
/// </summary>
public static class HubBakeCli
{
    static string Arg(string name)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
        return null;
    }

    public static void BakeScene()
    {
        string part = Arg("-pbScene");
        var scenes = EditorBuildSettings.scenes.ToList();
        var hit = scenes.FirstOrDefault(s => part != null && s.path.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (hit == null) { Debug.LogError("[hubbake] no scene matches " + part); EditorApplication.Exit(2); return; }
        scenes.Remove(hit); scenes.Insert(0, hit);
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[hubbake] baking " + hit.path);
        SceneBaker.BakeAndExit();
    }
}
