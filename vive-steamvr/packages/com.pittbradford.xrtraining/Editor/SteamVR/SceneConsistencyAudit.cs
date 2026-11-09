using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Lists, for each scene, the things every Vive scene must have in the same form: the XR Rig with Valve's Player (VRCamera, hands and hand models), Teleporting,
    /// PB Teleport Area, the Desk Hint under the camera, how many cameras and audio listeners are active, and how many Pressable and Grabbable objects the scene holds.
    /// It only reads scene files (it never saves). From a command line with the project not open in an editor:
    ///   Unity.exe -projectPath P -executeMethod PittBradford.XR.EditorTools.SceneConsistencyAudit.RunAndExit -pbScenes "Assets/Scenes/A.unity;Assets/Scenes/B.unity" -pbAuditOut report.txt
    /// With no -pbScenes it audits every scene in the build settings. Exit code 0 when every scene has the full rig, 3 otherwise.
    /// </summary>
    public static class SceneConsistencyAudit
    {
        [MenuItem("Pitt-Bradford/Audit scene consistency (open scene)", false, 47)]
        public static void RunMenu()
        {
            var sb = new StringBuilder();
            Audit(SceneManager.GetActiveScene(), sb);
            Debug.Log(sb.ToString());
        }

        public static void RunAndExit()
        {
            var paths = new List<string>();
            string outFile = null;
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-pbScenes") paths.AddRange(a[i + 1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                if (a[i] == "-pbAuditOut") outFile = a[i + 1];
            }
            if (paths.Count == 0) paths.AddRange(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
            var sb = new StringBuilder();
            int bad = 0;
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (!Audit(scene, sb)) bad++;
            }
            sb.AppendLine("SUMMARY scenes=" + paths.Count + " incomplete=" + bad);
            Debug.Log(sb.ToString());
            if (!string.IsNullOrEmpty(outFile)) File.WriteAllText(outFile, sb.ToString());
            EditorApplication.Exit(bad == 0 ? 0 : 3);
        }

        static int CountRenderers(Transform t)
        {
            int n = 0;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true)) if (!(r is LineRenderer)) n++;
            return n;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static bool Audit(Scene scene, StringBuilder sb)
        {
            bool ok = true;
            sb.AppendLine("SCENE " + scene.path);
            var all = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) all.Add(t.gameObject);
            sb.AppendLine("  objects: " + all.Count + ", roots: " + scene.rootCount);

            int cams = 0, camsActive = 0, listeners = 0, listenersActive = 0, missing = 0;
            var missingNames = new List<string>();
            foreach (var go in all)
            {
                var cam = go.GetComponent<Camera>();
                if (cam != null) { cams++; if (go.activeInHierarchy && cam.enabled) camsActive++; }
                var al = go.GetComponent<AudioListener>();
                if (al != null) { listeners++; if (go.activeInHierarchy && al.enabled) listenersActive++; }
                int m = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (m > 0 && missingNames.Count < 6) missingNames.Add(go.name + " (" + (go.transform.parent != null ? go.transform.parent.name : "root") + ")");
                missing += m;
            }
            if (missingNames.Count > 0) sb.AppendLine("  objects with a missing script, first ones: " + string.Join("; ", missingNames));
            sb.AppendLine("  cameras: " + cams + " (active " + camsActive + "), audio listeners: " + listeners + " (active " + listenersActive + "), missing scripts: " + missing);

            var player = UnityEngine.Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (player == null)
            {
                sb.AppendLine("  Player (SteamVR): MISSING");
                ok = false;
            }
            else
            {
                var parent = player.transform.parent;
                sb.AppendLine("  Player (SteamVR): " + (parent != null ? parent.name + " > " : "") + player.name + ", active " + player.gameObject.activeSelf + ", rig position " + (parent != null ? parent.position : player.transform.position).ToString("0.00"));
                var tree = new List<string>();
                foreach (Transform child in player.transform)
                {
                    var grand = new List<string>();
                    foreach (Transform g2 in child) grand.Add(g2.name);
                    tree.Add(child.name + (grand.Count > 0 ? " [" + string.Join(", ", grand) + "]" : ""));
                }
                sb.AppendLine("  Player children: " + string.Join("; ", tree));
                var cam = player.GetComponentInChildren<Camera>(true);
                sb.AppendLine("  VRCamera: " + (cam != null ? cam.name + ", tag " + cam.tag + ", enabled " + cam.enabled : "MISSING"));
                if (cam == null) ok = false;
                foreach (var hand in new[] { player.leftHand, player.rightHand })
                {
                    if (hand == null) { sb.AppendLine("  hand: MISSING"); ok = false; continue; }
                    var model = FindDeep(hand.transform, hand.handType.ToString() + " render model");
                    sb.AppendLine("  " + hand.handType + ": active " + hand.gameObject.activeSelf + ", hand model " + (model != null ? "present (renderers " + CountRenderers(model) + ")" : "MISSING"));
                    if (model == null) ok = false;
                }
                var hint = cam != null ? cam.transform.Find("Desk Hint") : null;
                sb.AppendLine("  Desk Hint under the camera: " + (hint != null ? "present, saved active " + hint.gameObject.activeSelf + " (the backend switches it on in desk mode)" : "MISSING"));
                if (hint == null) ok = false;
                var fallback = FindDeep(player.transform, "FallbackObjects");
                sb.AppendLine("  Valve FallbackObjects (observer camera when no headset): " + (fallback != null ? "present, active " + fallback.gameObject.activeSelf : "none"));
            }

            var teleporting = UnityEngine.Object.FindFirstObjectByType<Teleport>(FindObjectsInactive.Include);
            sb.AppendLine("  Teleporting (SteamVR): " + (teleporting != null ? "present, active " + teleporting.gameObject.activeSelf : "MISSING"));
            if (teleporting == null) ok = false;

            var areas = UnityEngine.Object.FindObjectsByType<TeleportArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var pbArea = all.FirstOrDefault(g => g.name == "PB Teleport Area");
            sb.AppendLine("  TeleportArea components: " + areas.Length + "; PB Teleport Area: " + (pbArea != null ? "present, active " + pbArea.activeSelf + ", mesh collider " + (pbArea.GetComponent<MeshCollider>() != null) + ", TeleportArea " + (pbArea.GetComponent<TeleportArea>() != null) : "MISSING"));
            if (pbArea == null || areas.Length == 0) ok = false;

            int press = UnityEngine.Object.FindObjectsByType<Pressable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int grab = UnityEngine.Object.FindObjectsByType<Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int boards = all.Count(g => g.name.StartsWith("Task Board"));
            int footers = all.Count(g => g.name == "Footer");
            int returnToLobby = all.Count(g => g.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "ReturnToLobby"));
            sb.AppendLine("  Pressable: " + press + ", Grabbable: " + grab + ", Task Board objects: " + boards + ", Footer texts: " + footers + ", Return-to-lobby components: " + returnToLobby);
            sb.AppendLine("  RESULT " + (ok ? "complete rig" : "INCOMPLETE rig"));
            return ok;
        }
    }
}
