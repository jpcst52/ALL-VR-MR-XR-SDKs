using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;
using ValveHand = Valve.VR.InteractionSystem.Hand;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Puts Valve's SteamVR Interaction System objects into a scene FILE, like Valve's own sample scene, so they show in the Hierarchy:
    ///   XR Rig > Player (SteamVR)       the Player prefab: VRCamera, LeftHand, RightHand (each with its controller/hand model as a child)
    ///   Teleporting (SteamVR)           the Teleporting prefab
    ///   PB Teleport Area                a TeleportArea over the training area (the scenario's rig.Area)
    /// At Play, SteamVRBackend adopts these (FindFirstObjectByType) instead of creating them. Run from the menu on the open scene, or with
    /// -executeMethod PittBradford.XR.EditorTools.SteamVRRigPlacement.PlaceInScenesAndExit [-pbScenes "path;path"] (no -batchmode needed, but allowed).
    /// Safe to run again: a scene that already has a Player is left alone except for a missing hand model or teleport area.
    /// </summary>
    [InitializeOnLoad]
    public static class SteamVRRigPlacement
    {
        const string PrefabDir = "PBSteamVR/";

        static SteamVRRigPlacement() { SceneRigHook.Placer = () => PlaceInScene(SceneManager.GetActiveScene()); }

        // Where each scenario puts the rig and the area it allows (the constants in the scenario scripts' Start()). The backend resizes the area to
        // the real rig.Area at Play, so these only decide what the saved scene shows.
        struct Setup { public Vector3 Start; public Vector3 AreaCenter; public Vector2 AreaSize; }
        static readonly Dictionary<string, Setup> Table = new Dictionary<string, Setup>
        {
            { "LobbyHub",          new Setup { Start = new Vector3(0f, 0f, -6.5f),   AreaCenter = Vector3.zero,                AreaSize = new Vector2(22.5f, 16.5f) } },
            { "HydroTest",         new Setup { Start = new Vector3(0f, 0f, -3.6f),   AreaCenter = Vector3.zero,                AreaSize = new Vector2(11.4f, 10.6f) } },
            { "RefineryRounds",    new Setup { Start = new Vector3(0f, 0f, -14f),    AreaCenter = new Vector3(0f, 0f, 4f),     AreaSize = new Vector2(64f, 56f) } },
            { "WarehouseForklift", new Setup { Start = new Vector3(-9f, 0f, -10.6f), AreaCenter = new Vector3(0f, 0f, 2f),     AreaSize = new Vector2(27.2f, 27.2f) } },
            { "ResistorBench",     new Setup { Start = new Vector3(0f, 0f, -0.35f),  AreaCenter = new Vector3(0f, 0f, -0.4f),  AreaSize = new Vector2(5f, 1.1f) } },
        };

        [MenuItem("Pitt-Bradford/SteamVR: place Player and Teleporting in open scene", false, 44)]
        public static void PlaceMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (PlaceInScene(scene)) { EditorSceneManager.MarkSceneDirty(scene); Debug.Log("[rig] placed in " + scene.name + " (save the scene)"); }
        }

        public static void PlaceInScenesAndExit()
        {
            var paths = new List<string>();
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == "-pbScenes") paths.AddRange(a[i + 1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            if (paths.Count == 0) paths.AddRange(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
            int failed = 0;
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (PlaceInScene(scene)) EditorSceneManager.SaveScene(scene);
                if (!HasRig(scene)) { Debug.LogError("[rig] no Player in " + path + " after placement"); failed++; }
                else Debug.Log("[rig] " + path + " has Player (SteamVR), Teleporting and a teleport area");
            }
            EditorApplication.Exit(failed == 0 ? 0 : 1);
        }

        /// <summary>
        /// "Desk Hint": a small panel under the camera that tells a keyboard-and-mouse user the keys. Saved in the scene (switched off); the backend's
        /// desk mode switches it on when no controller is tracked and sets the text font at Play. No collider, so the pointer ray passes through it.
        /// </summary>
        static void BuildDeskHint(Transform camera, Scene scene)
        {
            Directory.CreateDirectory("Assets/Baked/_PBRig");
            string matPath = "Assets/Baked/_PBRig/DeskHint-panel.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("PittBradford/FlatColor")) { name = "DeskHint-panel" };
                mat.SetColor("_Color", new Color(0f, 0.125f, 0.357f, 0.92f));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            var root = new GameObject("Desk Hint");
            root.transform.SetParent(camera, false);
            root.transform.localPosition = new Vector3(0f, -0.16f, 0.8f);
            var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "Panel";
            UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
            panel.transform.SetParent(root.transform, false);
            panel.transform.localScale = new Vector3(0.42f, 0.085f, 1f);
            var pr = panel.GetComponent<MeshRenderer>();
            pr.sharedMaterial = mat;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            var text = P.Text(root.transform, "KEYBOARD AND MOUSE (no controllers)\nWASD walk   Mouse look   Esc frees the mouse\nClick = press what the ray touches   Hold E = grab (3 m)",
                new Vector3(0f, 0f, -0.005f), 0.02f, Color.white, TextAnchor.MiddleCenter, false, "Hint text");
            TextFit.Add(text, 0.38f, 0.075f, false);
            root.SetActive(false);
            EditorUtility.SetDirty(root);
        }

        static bool HasRig(Scene s) => UnityEngine.Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include) != null;

        /// <summary>Returns true if the scene was changed.</summary>
        public static bool PlaceInScene(Scene scene)
        {
            var playerPrefab = Resources.Load<GameObject>(PrefabDir + "Player");
            var teleportPrefab = Resources.Load<GameObject>(PrefabDir + "Teleporting");
            if (playerPrefab == null) { Debug.LogWarning("[rig] Resources/PBSteamVR/Player is missing (run the SteamVR project setup)"); return false; }

            Setup setup = new Setup { Start = Vector3.zero, AreaCenter = Vector3.zero, AreaSize = new Vector2(4f, 4f) };
            bool known = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && !known && Table.TryGetValue(mb.GetType().Name, out var found)) { setup = found; known = true; }
            if (!known) Debug.LogWarning("[rig] " + scene.name + " has no known scenario component; rig placed at the origin with a 4 x 4 m area");

            bool changed = false;
            var player = UnityEngine.Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            if (player == null)
            {
                var root = new GameObject("XR Rig");
                root.transform.position = setup.Start;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                go.transform.SetParent(root.transform, false);
                go.name = "Player (SteamVR)";
                player = go.GetComponent<Player>();
                var cam = go.GetComponentInChildren<Camera>(true);
                if (cam != null) cam.tag = "MainCamera";
                changed = true;
            }
            // Valve's own sample shows the hand/controller model under each hand; it is normally spawned only once a controller is tracked.
            foreach (var hand in new[] { player.leftHand, player.rightHand })
                if (hand != null && hand.mainRenderModel == null && hand.renderModelPrefab != null)
                {
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(hand.renderModelPrefab, scene);
                    model.transform.SetParent(hand.transform, false);
                    model.name = hand.handType + " render model";
                    var rm = model.GetComponent<RenderModel>();
                    hand.mainRenderModel = rm; // Hand replaces it with the real one when a controller is tracked
                    if (rm != null) hand.renderModels.Add(rm);
                    EditorUtility.SetDirty(hand);
                    changed = true;
                }

            var hintHost = player.GetComponentInChildren<Camera>(true);
            if (hintHost != null)
            {
                // an older hint (first version of this panel, lower and larger) is replaced, so a re-run brings every scene to the current layout
                var oldHint = hintHost.transform.Find("Desk Hint");
                if (oldHint != null && Mathf.Abs(oldHint.localPosition.y - -0.16f) > 0.001f) { UnityEngine.Object.DestroyImmediate(oldHint.gameObject); oldHint = null; }
                if (oldHint == null) { BuildDeskHint(hintHost.transform, scene); changed = true; }
            }

            if (UnityEngine.Object.FindFirstObjectByType<Teleport>(FindObjectsInactive.Include) == null && teleportPrefab != null)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(teleportPrefab, scene);
                t.name = "Teleporting (SteamVR)";
                changed = true;
            }

            if (GameObject.Find("PB Teleport Area") == null)
            {
                var area = new GameObject("PB Teleport Area");
                SceneManager.MoveGameObjectToScene(area, scene);
                area.transform.position = new Vector3(setup.AreaCenter.x, setup.Start.y + 0.012f, setup.AreaCenter.z);
                float hx = setup.AreaSize.x / 2f, hz = setup.AreaSize.y / 2f;
                var mesh = new Mesh { name = "teleport area" };
                mesh.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                Directory.CreateDirectory("Assets/Baked/_PBRig");
                string meshPath = "Assets/Baked/_PBRig/" + scene.name + "-teleport-area.asset";
                AssetDatabase.CreateAsset(mesh, meshPath);
                area.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = area.AddComponent<MeshRenderer>();
                var teleport = teleportPrefab != null ? teleportPrefab.GetComponent<Teleport>() : null;
                if (teleport != null) renderer.sharedMaterial = teleport.areaVisibleMaterial;
                area.AddComponent<MeshCollider>().sharedMesh = mesh;
                area.AddComponent<TeleportArea>();
                changed = true;
            }
            return changed;
        }
    }
}
