using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Sets up the two Microsoft Rocketbox people this project ships (MIT, shrunk copies under Assets/Resources/Rocketbox, see
/// tools/people/export-subset.py): makes the idle and walk clips loop and builds two Animator Controllers next to them.
///   Walker.controller  Idle and Walk states, switched by the float "Speed" (above 0.1 walks)
///   Idle.controller    one looping idle, used for the seated forklift driver
/// Menu: Pitt-Bradford > People > Set up Rocketbox controllers.
/// </summary>
public static class PeopleSetup
{
    const string Root = "Assets/Resources/Rocketbox";
    const string IdleClip = Root + "/Animations/all_animations_max_motextr_static/m_idle_neutral_01.max.fbx";
    const string WalkClip = Root + "/Animations/all_animations_max_motextr_xy/m_walk_neutral.max.fbx";

    [MenuItem("Pitt-Bradford/People/Set up Rocketbox controllers")]
    public static void Run()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Avatars" })) MakeMaterials(AssetDatabase.GUIDToAssetPath(guid));
        MakeLoop(IdleClip);
        MakeLoop(WalkClip);
        AssetDatabase.Refresh();
        var idle = LoadClip(IdleClip);
        var walk = LoadClip(WalkClip);
        if (idle == null || walk == null) { Debug.LogError("[people] idle or walk clip missing under " + Root); return; }

        AssetDatabase.DeleteAsset(Root + "/Idle.controller");
        var ci = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Idle.controller");
        var si = ci.layers[0].stateMachine.AddState("Idle");
        si.motion = idle;
        ci.layers[0].stateMachine.defaultState = si;

        AssetDatabase.DeleteAsset(Root + "/Walker.controller");
        var c = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Walker.controller");
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = c.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        sm.defaultState = sIdle;
        var t1 = sIdle.AddTransition(sWalk); t1.hasExitTime = false; t1.duration = 0.2f; t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var t2 = sWalk.AddTransition(sIdle); t2.hasExitTime = false; t2.duration = 0.25f; t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        AssetDatabase.SaveAssets();
        Debug.Log("[people] controllers ready in " + Root);
    }

    /// <summary>For -executeMethod: sets up the controllers and exits.</summary>
    public static void RunAndExit()
    {
        Run();
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// The committed textures are 1024 px PNGs, not the library's TGAs, so the FBX does not find them by itself. This makes one
    /// material per FBX material (colour map and normal map from the Textures folder beside Export) and remaps the FBX to them.
    /// </summary>
    static void MakeMaterials(string fbxPath)
    {
        var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (imp == null) return;
        string dir = System.IO.Path.GetDirectoryName(fbxPath).Replace('\\', '/');
        string textures = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/') + "/Textures";
        string materials = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/') + "/Materials";
        System.IO.Directory.CreateDirectory(materials);
        foreach (var source in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().ToArray())
        {
            var colour = AssetDatabase.LoadAssetAtPath<Texture2D>($"{textures}/{source.name}_color.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{textures}/{source.name}_normal.png");
            Debug.Log($"[people] {fbxPath} material {source.name}: colour {(colour != null)} normal {(normal != null)}");
            if (colour == null) continue;
            var path = $"{materials}/{source.name}.mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(source) { color = Color.white, mainTexture = colour };
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Glossiness", 0.25f);
            AssetDatabase.CreateAsset(m, path);
            imp.AddRemap(new AssetImporter.SourceAssetIdentifier(source), m);
        }
        imp.SaveAndReimport();
    }

    static void MakeLoop(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogWarning("[people] missing " + path); return; }
        var clips = imp.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;
        foreach (var c in clips) { c.loopTime = true; c.loop = true; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = false; }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    static AnimationClip LoadClip(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }
}
