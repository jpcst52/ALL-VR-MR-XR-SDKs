using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Sets up the Microsoft Rocketbox people (MIT, imported locally under Assets/Rocketbox): turns the walk, idle and work
/// animation files into looping clips, and builds one small Animator Controller per activity under Assets/Rocketbox/Controllers.
/// Menu: Pitt-Bradford > People > Set up Rocketbox controllers.
/// </summary>
public static class PeopleSetup
{
    public const string Root = "Assets/Rocketbox";
    public const string ControllerDir = Root + "/Controllers";

    /// <summary>Avatars imported for the scenes (folder name under Avatars/*).</summary>
    public static readonly string[] Avatars =
    {
        "Professions/Construction_Male_01", "Professions/Construction_Male_02", "Professions/Construction_Male_04", "Professions/Construction_Female_01",
        "Professions/Business_Male_01", "Professions/Business_Female_01", "Professions/Security_Male_01", "Professions/Delivery_Male_01",
        "Professions/Medical_Female_01", "Adults/Male_Adult_05", "Adults/Female_Adult_03",
    };

    /// <summary>Activity name (used in controller names) and the animation file stem, for the male (m_) and female (f_) sets.</summary>
    static readonly (string name, string stem, bool xy)[] Activities =
    {
        ("Idle", "idle_neutral_01", false), ("IdleB", "idle_neutral_03", false), ("Look", "idle_look_around_01", false),
        ("Talk", "gestic_talk_neutral_01", false), ("Wait", "idle_waiting_01", false), ("Wave", "wave_01", false),
        ("Documents", "documents_idle", false), ("Note", "documents_note", false), ("Crouch", "crouch_idle", false),
        ("WorkMid", "work_mid", false), ("WorkTable", "work_table", false),
        ("Walk", "walk_neutral", true), ("WalkB", "walk_neutral_01", true), ("WalkSlow", "walk_slow_01", true),
    };

    static string AnimPath(string gender, string stem, bool xy) =>
        $"{Root}/Animations/all_animations_max_motextr_{(xy ? "xy" : "static")}/{gender}_{stem}.max.fbx";

    public static string ModelPath(string avatar) => $"{Root}/Avatars/{avatar}/Export/{Path.GetFileName(avatar)}.fbx";
    public static bool IsFemale(string avatar) => avatar.Contains("Female");

    [MenuItem("Pitt-Bradford/People/Set up Rocketbox controllers")]
    public static void Run()
    {
        if (!Directory.Exists(Root)) { Debug.LogWarning("[people] Assets/Rocketbox is not present; nothing to set up."); return; }
        Directory.CreateDirectory(ControllerDir);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var g in new[] { "m", "f" })
                foreach (var a in Activities)
                    MakeLoop(AnimPath(g, a.stem, a.xy));
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh();

        foreach (var g in new[] { "m", "f" })
        {
            foreach (var a in Activities)
            {
                var clip = LoadClip(AnimPath(g, a.stem, a.xy));
                if (clip == null) { Debug.LogWarning($"[people] no clip for {g}_{a.stem}"); continue; }
                var path = $"{ControllerDir}/{g}_{a.name}.controller";
                AssetDatabase.DeleteAsset(path);
                var c = AnimatorController.CreateAnimatorControllerAtPath(path);
                var st = c.layers[0].stateMachine.AddState(a.name);
                st.motion = clip;
                c.layers[0].stateMachine.defaultState = st;
            }
            MakeWalker(g);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[people] controllers ready in " + ControllerDir);
    }

    /// <summary>A controller with Idle and Walk states switched by the float "Speed" (above 0.1 walks).</summary>
    static void MakeWalker(string g)
    {
        var idle = LoadClip(AnimPath(g, "idle_neutral_01", false));
        var walk = LoadClip(AnimPath(g, "walk_neutral", true));
        if (idle == null || walk == null) return;
        var path = $"{ControllerDir}/{g}_Walker.controller";
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = c.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        sm.defaultState = sIdle;
        var t1 = sIdle.AddTransition(sWalk); t1.hasExitTime = false; t1.duration = 0.2f; t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var t2 = sWalk.AddTransition(sIdle); t2.hasExitTime = false; t2.duration = 0.25f; t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
    }

    static void MakeLoop(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { Debug.LogWarning("[people] missing " + path); return; }
        var clips = imp.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;
        bool changed = imp.clipAnimations == null || imp.clipAnimations.Length != clips.Length;
        foreach (var c in clips) { if (!c.loopTime) changed = true; c.loopTime = true; c.loop = true; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = false; }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    public static AnimationClip LoadClip(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    public static RuntimeAnimatorController Controller(string avatar, string activity)
    {
        var g = IsFemale(avatar) ? "f" : "m";
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ControllerDir}/{g}_{activity}.controller");
    }

    /// <summary>Puts an avatar in the scene with an Animator playing <paramref name="activity"/>. Returns null if the pack is missing.</summary>
    public static GameObject Spawn(Transform parent, string avatar, Vector3 position, float yaw, string activity = "Idle", float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(avatar));
        if (prefab == null) { Debug.LogWarning("[people] avatar not found: " + avatar); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = Path.GetFileName(avatar);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        go.transform.localScale = Vector3.one * scale;
        var anim = go.GetComponent<Animator>();
        if (!anim) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = Controller(avatar, activity);
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return go;
    }

    /// <summary>Renders a test line-up of the avatars with different activities to a PNG, using temporary objects in the open scene (removed afterwards).</summary>
    public static string RenderTest(string pngPath)
    {
        var temp = new GameObject("PeopleTest (temporary)");
        var oldAmbient = RenderSettings.ambientLight; var oldMode = RenderSettings.ambientMode; var oldFog = RenderSettings.fog;
        try
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.6f); RenderSettings.fog = false;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.SetParent(temp.transform, false); floor.transform.localScale = new Vector3(2, 1, 2);
            var light = new GameObject("Sun").AddComponent<Light>(); light.transform.SetParent(temp.transform, false); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45, -30, 0); light.intensity = 1.1f;
            string[] acts = { "Idle", "WorkTable", "Talk", "Note", "Wave", "Look", "Documents", "Idle", "Walk", "WalkSlow", "Crouch" };
            for (int i = 0; i < Avatars.Length; i++)
            {
                var p = Spawn(temp.transform, Avatars[i], new Vector3(-5f + i * 1.0f, 0, 0), 180f, acts[i]);
                if (p == null) continue;
                var anim = p.GetComponent<Animator>();
                var ac = anim.runtimeAnimatorController as AnimatorController;
                var clip = ac != null ? ac.layers[0].stateMachine.defaultState.motion as AnimationClip : null;
                if (clip != null) clip.SampleAnimation(p, 0.8f + 0.15f * i); else Debug.LogWarning("[people] no clip for " + Avatars[i]);
            }
            var cam = new GameObject("Cam").AddComponent<Camera>(); cam.transform.SetParent(temp.transform, false);
            cam.transform.position = new Vector3(0, 1.5f, -4.2f); cam.transform.LookAt(new Vector3(0, 1.0f, 0)); cam.fieldOfView = 50; cam.backgroundColor = new Color(0.55f, 0.65f, 0.78f); cam.clearFlags = CameraClearFlags.SolidColor;
            var rt = new RenderTexture(2200, 900, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = null; rt.Release();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            return pngPath;
        }
        finally
        {
            Object.DestroyImmediate(temp);
            RenderSettings.ambientLight = oldAmbient; RenderSettings.ambientMode = oldMode; RenderSettings.fog = oldFog;
        }
    }
}
