using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PittBradford.XR;

/// <summary>
/// Makes each lobby portal selectable by pointing at its doorway and pressing the trigger (desktop: left click), as in the other scenes, without
/// rebuilding the lobby: it adds a collider and a Pressable to every portal's "Gate" in the saved Lobby scene and updates the welcome board text.
/// LobbyHub.BuildPortal does the same when the lobby is rebuilt, so both ways give the same objects. Safe to run again.
///   menu  Pitt-Bradford > Hub: lobby portals selectable by pointing (open Lobby scene)
///   Unity.exe -projectPath hub -executeMethod HubLobbyPortalSelect.PatchAndExit
/// </summary>
public static class HubLobbyPortalSelect
{
    const string LobbyPath = "Assets/Scenes/Local/Lobby.unity";
    const string OldText = "Step onto a portal's pad and wait a moment to start.   Desktop: keys 1-4.";
    const string NewText = "Step on a pad and wait, or point at a doorway and press the trigger.   Desktop: click it, or keys 1-4.";

    [MenuItem("Pitt-Bradford/Hub: lobby portals selectable by pointing (open Lobby scene)", false, 60)]
    public static void PatchMenu()
    {
        var scene = SceneManager.GetActiveScene();
        int n = Patch(scene);
        if (n > 0) EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[lobby] " + n + " change(s) in " + scene.name + (n > 0 ? " (save the scene)" : ""));
    }

    public static void PatchAndExit()
    {
        var scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
        int n = Patch(scene);
        if (n > 0) EditorSceneManager.SaveScene(scene);
        Debug.Log("[lobby] portal select: " + n + " change(s) saved in " + LobbyPath);
        EditorApplication.Exit(0);
    }

    static int Patch(Scene scene)
    {
        int changes = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Portal ") && t.Find("Gate") != null)
                {
                    var gate = t.Find("Gate").gameObject;
                    if (gate.GetComponent<Collider>() == null) { gate.AddComponent<BoxCollider>(); changes++; }
                    var press = gate.GetComponent<Pressable>();
                    if (press == null) { press = gate.AddComponent<Pressable>(); changes++; }
                    int colon = t.name.IndexOf(": ", StringComparison.Ordinal);
                    press.Label = colon >= 0 ? t.name.Substring(colon + 2) : t.name;
                    EditorUtility.SetDirty(gate);
                }
                var tm = t.GetComponent<TextMesh>();
                if (tm != null && t.name == "Instructions" && tm.text.Contains("Step onto a portal"))
                {
                    var fit = t.GetComponent<TextFit>();
                    if (fit != null) fit.Source = NewText;
                    tm.text = NewText;
                    if (fit != null) { fit.Apply(); EditorUtility.SetDirty(fit); }
                    EditorUtility.SetDirty(tm);
                    changes++;
                }
            }
        }
        return changes;
    }
}
