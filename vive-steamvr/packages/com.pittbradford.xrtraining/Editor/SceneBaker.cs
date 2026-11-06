using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Bakes a scenario's code-generated world into its scene file, so every object is visible and editable in the
    /// Hierarchy and Scene view. The scenario's Start() still runs at play time and adopts the saved objects through the
    /// kit helpers (see <see cref="Bake"/>), re-attaching event handlers and fields exactly as before.
    ///
    /// Bake: opens the scene, enters play mode with time frozen, waits until Start has finished building, converts the
    /// generated materials, textures and meshes into assets under Assets/Baked/&lt;Scene&gt;/, saves the generated objects
    /// (everything a kit helper made, minus [DontBake] and runtime-only types), exits play mode, puts them into the scene
    /// as plain GameObjects, copies the lighting Start set up, marks the scenario with <see cref="BakedWorld"/>, and saves.
    /// Re-baking replaces the previous baked objects (adopted objects keep the edits made to them).
    /// Unbake: deletes the baked objects and assets so the world is generated at Start again.
    /// Both work from the menu or with -executeMethod (BakeAndExit / UnbakeAndExit, without -batchmode).
    /// </summary>
    [InitializeOnLoad]
    public static class SceneBaker
    {
        const string StageKey = Bake.StageKey;              // "", "play", "finish"
        const string StateKey = "PittBradford.Bake.State";  // JSON BakeState
        const string Prefix = "[bake] ";

        /// <summary>Objects with these components are never baked (unless they were already in the scene file).</summary>
        static readonly Type[] RuntimeOnly =
        {
            typeof(XRRig), typeof(Hand), typeof(TaskBoard), typeof(WhyCue), typeof(CaptureDriver), typeof(Camera), typeof(AudioListener),
        };

        [Serializable] class RootEntry { public string Host = ""; }

        [Serializable]
        class LightingState
        {
            public int AmbientMode;
            public Color AmbientSky, AmbientEquator, AmbientGround, AmbientLight;
            public float AmbientIntensity = 1f;
            public bool Fog;
            public int FogMode;
            public Color FogColor;
            public float FogStart, FogEnd, FogDensity;
            public string Skybox = "";
        }

        [Serializable]
        class BakeState
        {
            public string ScenePath = "";
            public bool ExitWhenDone;
            public double StartedAt;
            public List<RootEntry> Roots = new List<RootEntry>();
            public LightingState Lighting;
            public string PrefabPath = "";
            public int PlayModeObjects, BakedObjects, Materials, Textures, Meshes, LeftOut;
            public string LeftOutNames = "";
        }

        static int waitFrames;

        static SceneBaker()
        {
            EditorApplication.update += Tick;
        }

        // ------------------------------------------------------------------------------------------- menus

        [MenuItem("Pitt-Bradford/Bake scene (play mode)", false, 40)]
        public static void BakeMenu() => Begin(false);

        [MenuItem("Pitt-Bradford/Unbake scene", false, 41)]
        public static void UnbakeMenu() => Unbake(false);

        /// <summary>For -executeMethod: bake the first scene in Build Settings, save, and quit the editor.</summary>
        public static void BakeAndExit() => Begin(true);

        /// <summary>For -executeMethod: unbake the first scene in Build Settings, save, and quit the editor.</summary>
        public static void UnbakeAndExit() => Unbake(true);

        static string TargetScene(bool preferBuildScene)
        {
            var active = SceneManager.GetActiveScene();
            if (!preferBuildScene && !string.IsNullOrEmpty(active.path)) return active.path;
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length > 0) return scenes[0].path;
            return active.path;
        }

        static void Begin(bool exitWhenDone)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(Prefix + "stop play mode first");
                return;
            }
            string path = TargetScene(exitWhenDone);
            if (string.IsNullOrEmpty(path))
            {
                Fail("no saved scene to bake (open the scenario scene first)", exitWhenDone);
                return;
            }
            if (!exitWhenDone && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            StripBatchingFlags();
            var state = new BakeState { ScenePath = path, ExitWhenDone = exitWhenDone, StartedAt = EditorApplication.timeSinceStartup };
            SessionState.SetString(StateKey, JsonUtility.ToJson(state));
            SessionState.SetString(StageKey, "play");
            waitFrames = 0;
            Debug.Log(Prefix + "baking " + path + ": entering play mode (time is frozen while the world is built)");
            EditorApplication.EnterPlaymode();
        }

        /// <summary>
        /// The render-budget pass marks baked objects batching-static. If they were still static when play mode starts, Unity would merge
        /// them into combined meshes and the bake would save those merged slices instead of the original meshes. The flags are put back by
        /// the pass at the end of the bake.
        /// </summary>
        static void StripBatchingFlags()
        {
            var marker = Object.FindAnyObjectByType<BakedWorld>(FindObjectsInactive.Include);
            if (marker == null) return;
            int n = 0;
            foreach (var root in marker.Roots)
            {
                if (root == null) continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var f = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    if ((f & StaticEditorFlags.BatchingStatic) == 0) continue;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, f & ~StaticEditorFlags.BatchingStatic);
                    n++;
                }
            }
            if (n > 0) Debug.Log(Prefix + "cleared batching-static on " + n + " baked objects before the bake (the render-budget pass sets it again)");
        }

        static void Tick()
        {
            string stage = SessionState.GetString(StageKey, "");
            if (stage == "") return;
            var state = JsonUtility.FromJson<BakeState>(SessionState.GetString(StateKey, "{}"));
            if (stage == "play")
            {
                if (!EditorApplication.isPlaying)
                {
                    // play mode never started (for example compile errors): give up instead of baking a later session
                    if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.timeSinceStartup - state.StartedAt > 30.0 && !EditorApplication.isCompiling)
                    {
                        SessionState.EraseString(StageKey);
                        Fail("play mode did not start; check the console for compile errors", state.ExitWhenDone);
                    }
                    return;
                }
                waitFrames++;
                // Start has run, nothing has been built for a few frames, and at least one frame has rendered
                bool settled = Time.frameCount >= 10 && Time.frameCount - Bake.LastHelperFrame >= 5;
                if (!settled && waitFrames < 2000) return;
                try
                {
                    CaptureInPlayMode(state);
                    SessionState.SetString(StateKey, JsonUtility.ToJson(state));
                    SessionState.SetString(StageKey, "finish");
                }
                catch (Exception e)
                {
                    SessionState.EraseString(StageKey);
                    Debug.LogException(e);
                    Fail("bake failed in play mode: " + e.Message, state.ExitWhenDone);
                }
                EditorApplication.ExitPlaymode();
                return;
            }
            if (stage == "finish" && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            {
                SessionState.EraseString(StageKey);
                try { FinishInEditMode(state); }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Fail("bake failed after play mode: " + e.Message, state.ExitWhenDone);
                    return;
                }
                if (state.ExitWhenDone) EditorApplication.Exit(0);
            }
        }

        static void Fail(string message, bool exit)
        {
            Debug.LogError(Prefix + message);
            if (exit) EditorApplication.Exit(3);
        }

        // ------------------------------------------------------------------------------------------- play mode half

        static void CaptureInPlayMode(BakeState state)
        {
            var scene = SceneManager.GetSceneByPath(state.ScenePath);
            if (!scene.IsValid()) scene = SceneManager.GetActiveScene();
            state.PlayModeObjects = CountObjects(scene);

            bool Tracked(GameObject g) => g != null && Bake.Tracked.Contains(g);
            bool Excluded(Transform t)
            {
                if (t.GetComponent<DontBake>() != null) return true;
                if (Bake.WasLoaded(t)) return false;
                foreach (var type in RuntimeOnly) if (t.GetComponent(type) != null) return true;
                return false;
            }

            // Bake roots: helper-made (or adopted) objects that sit at the scene root or directly under an object
            // from the scene file (the scenario). Anything under a runtime-only parent (rig, task board) stays runtime.
            var roots = new List<Transform>();
            foreach (var g in Bake.Tracked)
            {
                if (g == null || g.scene != scene) continue;
                var t = g.transform;
                if (Excluded(t)) continue;
                var parent = t.parent;
                if (parent == null || (Bake.WasLoaded(parent) && !Tracked(parent.gameObject))) roots.Add(t);
            }
            // keep the scene order: sibling order under each host, then root order
            roots.Sort((a, b) => string.CompareOrdinal(OrderKey(a), OrderKey(b)));

            // Within the baked subtrees, drop anything a helper did not make and that was not in the scene file:
            // it was made by plain code, so it is still made by plain code at runtime (baking it would duplicate it).
            var drop = new List<GameObject>();
            var leftOutNames = new SortedDictionary<string, int>();
            var keep = new List<Transform>();
            foreach (var r in roots) Collect(r, keep, drop, leftOutNames, Tracked, Excluded);
            state.LeftOut = drop.Count;
            state.LeftOutNames = string.Join(", ", leftOutNames.Select(kv => kv.Value > 1 ? $"{kv.Key} x{kv.Value}" : kv.Key));

            var conv = new AssetConverter(BakedFolder(state.ScenePath));
            foreach (var t in keep) conv.ConvertComponents(t.gameObject);
            state.Lighting = ReadLighting(conv);
            state.Materials = conv.MaterialCount;
            state.Textures = conv.TextureCount;
            state.Meshes = conv.MeshCount;

            foreach (var g in drop) if (g != null) Object.DestroyImmediate(g);

            // Gather the roots under one container and save it as a prefab (the bridge out of play mode)
            var container = new GameObject("__PittBradford_BakedWorld");
            state.Roots.Clear();
            foreach (var r in roots)
            {
                state.Roots.Add(new RootEntry { Host = r.parent == null ? "" : PathOf(r.parent) });
                r.SetParent(container.transform, false);
            }
            state.BakedObjects = container.GetComponentsInChildren<Transform>(true).Length - 1;
            state.PrefabPath = conv.Folder + "/World.prefab";
            PrefabUtility.SaveAsPrefabAsset(container, state.PrefabPath, out bool ok);
            AssetDatabase.SaveAssets();
            if (!ok) throw new Exception("could not save " + state.PrefabPath);
            Debug.Log($"{Prefix}captured {state.BakedObjects} objects in {roots.Count} groups (play mode had {state.PlayModeObjects} objects in the scene); " +
                      $"assets: {state.Materials} materials, {state.Textures} textures, {state.Meshes} meshes");
            if (state.LeftOut > 0) Debug.Log($"{Prefix}left to runtime (not made by a kit helper): {state.LeftOutNames}");
        }

        static void Collect(Transform t, List<Transform> keep, List<GameObject> drop, SortedDictionary<string, int> leftOut,
            Func<GameObject, bool> tracked, Func<Transform, bool> excluded)
        {
            keep.Add(t);
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (excluded(c) || !(tracked(c.gameObject) || Bake.WasLoaded(c)))
                {
                    drop.Add(c.gameObject);
                    leftOut.TryGetValue(c.name, out int n);
                    leftOut[c.name] = n + 1;
                    continue;
                }
                Collect(c, keep, drop, leftOut, tracked, excluded);
            }
        }

        static string OrderKey(Transform t)
        {
            // host path + sibling index for hosted roots; "~" + root index for scene roots (after hosted ones)
            if (t.parent != null) return PathOf(t.parent) + "\u0001" + t.GetSiblingIndex().ToString("D6");
            return "~" + t.GetSiblingIndex().ToString("D6");
        }

        /// <summary>Path of a scene-file object by names and same-name ordinals, stable between play and edit mode.</summary>
        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                var siblings = t.parent != null ? Children(t.parent) : t.gameObject.scene.GetRootGameObjects().Select(g => g.transform);
                int ord = 0;
                foreach (var s in siblings)
                {
                    if (s == t) break;
                    if (s.name == t.name && (!Application.isPlaying || Bake.WasLoaded(s))) ord++;
                }
                parts.Insert(0, t.name + "\t" + ord);
                t = t.parent;
            }
            return string.Join("\n", parts);
        }

        static IEnumerable<Transform> Children(Transform t)
        {
            for (int i = 0; i < t.childCount; i++) yield return t.GetChild(i);
        }

        static Transform Resolve(Scene scene, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Transform cur = null;
            foreach (var part in path.Split('\n'))
            {
                var bits = part.Split('\t');
                string name = bits[0];
                int ord = int.Parse(bits[1]);
                var siblings = cur != null ? Children(cur) : scene.GetRootGameObjects().Select(g => g.transform);
                cur = siblings.Where(s => s.name == name).Skip(ord).FirstOrDefault();
                if (cur == null) return null;
            }
            return cur;
        }

        static int CountObjects(Scene scene) =>
            scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Transform>(true).Length);

        static string BakedFolder(string scenePath) => "Assets/Baked/" + Path.GetFileNameWithoutExtension(scenePath);

        // ------------------------------------------------------------------------------------------- edit mode half

        static void FinishInEditMode(BakeState state)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != state.ScenePath) scene = EditorSceneManager.OpenScene(state.ScenePath, OpenSceneMode.Single);
            int before = CountObjects(scene);

            // resolve hosts first (paths count the old baked objects too, as play mode did)
            var hosts = state.Roots.Select(r => Resolve(scene, r.Host)).ToList();
            for (int i = 0; i < hosts.Count; i++)
                if (!string.IsNullOrEmpty(state.Roots[i].Host) && hosts[i] == null)
                    throw new Exception("could not find the parent object " + state.Roots[i].Host.Replace("\n", "/").Replace("\t", "#"));

            var marker = Object.FindAnyObjectByType<BakedWorld>(FindObjectsInactive.Include);
            string lightingBefore = marker != null && !string.IsNullOrEmpty(marker.LightingBefore) ? marker.LightingBefore : JsonUtility.ToJson(ReadLighting(null));
            if (marker != null)
                foreach (var old in marker.Roots) if (old != null) Object.DestroyImmediate(old);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(state.PrefabPath);
            if (prefab == null) throw new Exception("baked prefab missing: " + state.PrefabPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var newRoots = new List<GameObject>();
            for (int i = 0; i < state.Roots.Count && inst.transform.childCount > 0; i++)
            {
                var r = inst.transform.GetChild(0);
                r.SetParent(hosts[i], false);
                newRoots.Add(r.gameObject);
            }
            Object.DestroyImmediate(inst);

            // the marker goes on the scenario (the host of the baked objects), or the first scene object that is not baked
            var markerHost = hosts.FirstOrDefault(h => h != null);
            if (markerHost == null)
                markerHost = scene.GetRootGameObjects().Select(g => g.transform).FirstOrDefault(t => !newRoots.Contains(t.gameObject));
            if (marker != null && marker.transform != markerHost) Object.DestroyImmediate(marker);
            if (markerHost == null) markerHost = new GameObject("Baked world").transform;
            marker = P.Ensure<BakedWorld>(markerHost.gameObject);
            marker.Roots = newRoots;
            marker.BakedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            marker.ObjectCount = state.BakedObjects;
            marker.LightingBefore = lightingBefore;

            if (state.Lighting != null) ApplyLighting(state.Lighting);
            // flags only, after the meshes are saved (marking objects static before the bake corrupted saved meshes)
            Debug.Log(RenderBudget.Apply(scene));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.DeleteAsset(state.PrefabPath);
            int removed = DeleteUnused(BakedFolder(state.ScenePath), state.ScenePath);
            AssetDatabase.SaveAssets();
            int after = CountObjects(scene);
            long bytes = new FileInfo(state.ScenePath).Length;
            Debug.Log($"{Prefix}done: {state.ScenePath} now holds {after} GameObjects (was {before}; play mode had {state.PlayModeObjects}); " +
                      $"baked {state.BakedObjects} objects; scene file {bytes / 1024} KB; {removed} unused baked assets removed");
        }

        /// <summary>Deletes assets in the baked folder that the scene no longer uses (after a re-bake).</summary>
        static int DeleteUnused(string folder, string scenePath)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return 0;
            var used = new HashSet<string>(AssetDatabase.GetDependencies(scenePath, true));
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(p) || used.Contains(p)) continue;
                if (AssetDatabase.DeleteAsset(p)) n++;
            }
            return n;
        }

        // ------------------------------------------------------------------------------------------- unbake

        static void Unbake(bool exitWhenDone)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(Prefix + "stop play mode first");
                return;
            }
            string path = TargetScene(exitWhenDone);
            if (!exitWhenDone && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var marker = Object.FindAnyObjectByType<BakedWorld>(FindObjectsInactive.Include);
            if (marker == null)
            {
                Debug.Log(Prefix + path + " is not baked");
                if (exitWhenDone) EditorApplication.Exit(0);
                return;
            }
            int before = CountObjects(scene);
            foreach (var r in marker.Roots) if (r != null) Object.DestroyImmediate(r);
            if (!string.IsNullOrEmpty(marker.LightingBefore)) ApplyLighting(JsonUtility.FromJson<LightingState>(marker.LightingBefore));
            Object.DestroyImmediate(marker);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            string folder = BakedFolder(path);
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            if (AssetDatabase.IsValidFolder("Assets/Baked") && AssetDatabase.FindAssets("", new[] { "Assets/Baked" }).Length == 0)
                AssetDatabase.DeleteAsset("Assets/Baked");
            Debug.Log($"{Prefix}unbaked {path}: {before} -> {CountObjects(scene)} GameObjects; the world is generated at Start again");
            if (exitWhenDone) EditorApplication.Exit(0);
        }

        // ------------------------------------------------------------------------------------------- lighting

        static LightingState ReadLighting(AssetConverter conv)
        {
            var sky = RenderSettings.skybox;
            if (sky != null && conv != null && !EditorUtility.IsPersistent(sky)) sky = conv.ToMaterial(sky);
            return new LightingState
            {
                AmbientMode = (int)RenderSettings.ambientMode,
                AmbientSky = RenderSettings.ambientSkyColor,
                AmbientEquator = RenderSettings.ambientEquatorColor,
                AmbientGround = RenderSettings.ambientGroundColor,
                AmbientLight = RenderSettings.ambientLight,
                AmbientIntensity = RenderSettings.ambientIntensity,
                Fog = RenderSettings.fog,
                FogMode = (int)RenderSettings.fogMode,
                FogColor = RenderSettings.fogColor,
                FogStart = RenderSettings.fogStartDistance,
                FogEnd = RenderSettings.fogEndDistance,
                FogDensity = RenderSettings.fogDensity,
                Skybox = sky != null && EditorUtility.IsPersistent(sky) ? GlobalObjectId.GetGlobalObjectIdSlow(sky).ToString() : "",
            };
        }

        static void ApplyLighting(LightingState l)
        {
            RenderSettings.ambientMode = (UnityEngine.Rendering.AmbientMode)l.AmbientMode;
            RenderSettings.ambientSkyColor = l.AmbientSky;
            RenderSettings.ambientEquatorColor = l.AmbientEquator;
            RenderSettings.ambientGroundColor = l.AmbientGround;
            if (l.AmbientMode == (int)UnityEngine.Rendering.AmbientMode.Flat) RenderSettings.ambientLight = l.AmbientLight;
            RenderSettings.ambientIntensity = l.AmbientIntensity;
            RenderSettings.fog = l.Fog;
            RenderSettings.fogMode = (FogMode)l.FogMode;
            RenderSettings.fogColor = l.FogColor;
            RenderSettings.fogStartDistance = l.FogStart;
            RenderSettings.fogEndDistance = l.FogEnd;
            RenderSettings.fogDensity = l.FogDensity;
            Material sky = null;
            if (!string.IsNullOrEmpty(l.Skybox) && GlobalObjectId.TryParse(l.Skybox, out var id))
                sky = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Material;
            RenderSettings.skybox = sky;
        }

        // ------------------------------------------------------------------------------------------- assets

        /// <summary>Turns runtime materials, textures and meshes into assets (one asset per unique content).</summary>
        class AssetConverter
        {
            public readonly string Folder;
            public int MaterialCount => materials.Values.Distinct().Count(m => m != null);
            public int TextureCount => textures.Values.Distinct().Count(t => t != null);
            public int MeshCount => meshes.Values.Distinct().Count(m => m != null);
            readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
            readonly Dictionary<Texture, Texture> textures = new Dictionary<Texture, Texture>();
            readonly Dictionary<Mesh, Mesh> meshes = new Dictionary<Mesh, Mesh>();
            readonly HashSet<string> warned = new HashSet<string>();
            Font builtinFont;

            public AssetConverter(string folder)
            {
                Folder = folder;
                EnsureFolder(folder);
            }

            static void EnsureFolder(string path)
            {
                if (AssetDatabase.IsValidFolder(path)) return;
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }

            public void ConvertComponents(GameObject go)
            {
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null || c is Transform) continue;
                    if (c is TextMesh tm)
                    {
                        // the OS font and its atlas exist only at runtime: save the built-in font so the text shows in the
                        // editor; Bake.RestoreText puts the runtime font back at play time
                        if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>(Bake.BuiltinFontName + ".ttf");
                        tm.font = builtinFont;
                        var r = tm.GetComponent<MeshRenderer>();
                        if (r != null) r.sharedMaterial = builtinFont.material;
                        continue;
                    }
                    if (c is MeshRenderer && c.GetComponent<TextMesh>() != null) continue;
                    if (c is MonoBehaviour mb)
                    {
                        var script = MonoScript.FromMonoBehaviour(mb);
                        if (script == null || !EditorUtility.IsPersistent(script))
                        {
                            // Unity can only save a MonoBehaviour that lives in a file of the same name
                            if (warned.Add("script:" + c.GetType().Name))
                                Debug.LogWarning(Prefix + c.GetType().Name + " cannot be saved in a scene (move it to " + c.GetType().Name + ".cs); left out of the bake, so add it with P.Ensure at runtime");
                            Object.DestroyImmediate(c);
                            continue;
                        }
                    }
                    var so = new SerializedObject(c);
                    var it = so.GetIterator();
                    bool changed = false;
                    while (it.Next(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var v = it.objectReferenceValue;
                        if (v == null || EditorUtility.IsPersistent(v) || v is GameObject || v is Component || v is MonoScript) continue;
                        it.objectReferenceValue = Convert(v);
                        changed = true;
                    }
                    if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Object Convert(Object o)
            {
                switch (o)
                {
                    case Material m: return ToMaterial(m);
                    case Texture t: return ToTexture(t);
                    case Mesh mesh: return ToMesh(mesh);
                    default:
                        // generated audio clips and similar are not saved; the helpers put them back at runtime
                        if (warned.Add(o.GetType().Name)) Debug.Log(Prefix + "not saved (recreated at runtime): " + o.GetType().Name + " " + o.name);
                        return null;
                }
            }

            public Material ToMaterial(Material src)
            {
                if (materials.TryGetValue(src, out var done)) return done;
                var copy = new Material(src);
                Quantise(copy);
                foreach (var prop in src.GetTexturePropertyNames())
                {
                    var tex = src.GetTexture(prop);
                    if (tex != null && !EditorUtility.IsPersistent(tex)) copy.SetTexture(prop, ToTexture(tex));
                }
                // readable names: the texture name, else the shader plus colour (runtime materials are often just "Standard")
                string label = copy.mainTexture != null ? copy.mainTexture.name
                    : !string.IsNullOrEmpty(src.name) && src.name != src.shader.name && !src.name.Contains("(Instance)") ? src.name
                    : src.shader.name.Split('/').Last() + (copy.HasProperty("_Color") ? "-" + ColorUtility.ToHtmlStringRGB(copy.color) : "");
                if (copy.IsKeywordEnabled("_EMISSION")) label += "-glow";
                if (copy.renderQueue >= 3000) label += "-transparent";
                string path = $"{Folder}/Materials/{Safe(label)}_{Hash(MaterialKey(copy))}.mat";
                var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (asset == null)
                {
                    EnsureFolder(Folder + "/Materials");
                    copy.name = Path.GetFileNameWithoutExtension(path);
                    AssetDatabase.CreateAsset(copy, path);
                    asset = copy;
                }
                else Object.DestroyImmediate(copy);
                materials[src] = asset;
                return asset;
            }

            /// <summary>
            /// Snaps colours to 5 bits per channel and smoothness / metallic to steps of 0.05, so near-identical runtime materials become one
            /// asset (fewer materials means bigger batches), and turns on GPU instancing. Differences are below what the eye can see.
            /// </summary>
            static void Quantise(Material m)
            {
                Color Q(Color c, bool hdr)
                {
                    if (hdr)
                    {
                        float max = Mathf.Max(c.r, c.g, c.b, 0.0001f);
                        if (max <= 1f) return new Color(Mathf.Round(c.r * 31f) / 31f, Mathf.Round(c.g * 31f) / 31f, Mathf.Round(c.b * 31f) / 31f, 1f);
                        float k = Mathf.Round(max * 4f) / 4f / max;
                        return new Color(Mathf.Round(c.r * k * 31f) / 31f, Mathf.Round(c.g * k * 31f) / 31f, Mathf.Round(c.b * k * 31f) / 31f, 1f);
                    }
                    return new Color(Mathf.Round(c.r * 31f) / 31f, Mathf.Round(c.g * 31f) / 31f, Mathf.Round(c.b * 31f) / 31f, Mathf.Round(c.a * 20f) / 20f);
                }
                if (m.shader != null && m.shader.name == "Standard")
                {
                    if (m.HasProperty("_Color")) m.SetColor("_Color", Q(m.GetColor("_Color"), false));
                    if (m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION")) m.SetColor("_EmissionColor", Q(m.GetColor("_EmissionColor"), true));
                    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", Mathf.Round(m.GetFloat("_Metallic") * 20f) / 20f);
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", Mathf.Round(m.GetFloat("_Glossiness") * 20f) / 20f);
                }
                m.enableInstancing = true;
            }

            Texture ToTexture(Texture src)
            {
                if (textures.TryGetValue(src, out var done)) return done;
                Texture result = null;
                var t2 = src as Texture2D;
                if (t2 != null && t2.isReadable && src.name != "Font Texture" && t2.format != TextureFormat.Alpha8)
                {
                    byte[] png = t2.EncodeToPNG();
                    string key = Hash(System.Convert.ToBase64String(png) + t2.wrapMode + t2.filterMode + t2.anisoLevel + t2.mipmapCount + t2.isDataSRGB);
                    string path = $"{Folder}/Textures/{Safe(string.IsNullOrEmpty(src.name) ? "texture" : src.name)}_{key}.png";
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (asset == null)
                    {
                        EnsureFolder(Folder + "/Textures");
                        File.WriteAllBytes(path, png);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                        bool isNormal = src.name.EndsWith("-nrm");
                        imp.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                        imp.sRGBTexture = !isNormal && t2.isDataSRGB;
                        imp.mipmapEnabled = t2.mipmapCount > 1;
                        imp.wrapMode = t2.wrapMode;
                        imp.filterMode = t2.filterMode;
                        imp.anisoLevel = t2.anisoLevel;
                        imp.npotScale = TextureImporterNPOTScale.None;
                        imp.textureCompression = TextureImporterCompression.Uncompressed;
                        imp.alphaSource = GraphicsFormatUtility.HasAlpha(t2) ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                        imp.alphaIsTransparency = false;
                        imp.maxTextureSize = 4096;
                        imp.isReadable = false;
                        imp.SaveAndReimport();
                        asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    }
                    result = asset;
                }
                else if (warned.Add("tex:" + src.name)) Debug.Log(Prefix + "texture not saved (runtime-only, put back at runtime): " + src.name);
                textures[src] = result;
                return result;
            }

            Mesh ToMesh(Mesh src)
            {
                if (meshes.TryGetValue(src, out var done)) return done;
                var copy = Object.Instantiate(src);
                string path = $"{Folder}/Meshes/{Safe(string.IsNullOrEmpty(src.name) ? "mesh" : src.name)}_{meshes.Count}.asset";
                EnsureFolder(Folder + "/Meshes");
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(copy, path);
                meshes[src] = copy;
                return copy;
            }

            static string MaterialKey(Material m)
            {
                var sb = new StringBuilder();
                sb.Append(m.shader.name).Append('|').Append(m.renderQueue).Append('|').Append(m.globalIlluminationFlags).Append('|')
                  .Append(m.enableInstancing).Append('|').Append(m.doubleSidedGI).Append('|').Append(m.GetTag("RenderType", false)).Append('|');
                sb.Append(string.Join(",", m.shaderKeywords.OrderBy(k => k))).Append('|');
                var s = m.shader;
                for (int i = 0; i < s.GetPropertyCount(); i++)
                {
                    string n = s.GetPropertyName(i);
                    sb.Append(n).Append('=');
                    switch (s.GetPropertyType(i))
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color: sb.Append(m.GetColor(n).ToString("F5")); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector: sb.Append(m.GetVector(n).ToString("F5")); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                        case UnityEngine.Rendering.ShaderPropertyType.Range: sb.Append(m.GetFloat(n).ToString("R")); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Int: sb.Append(m.GetInteger(n)); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            var t = m.GetTexture(n);
                            sb.Append(t == null ? "-" : AssetDatabase.GetAssetPath(t) + ":" + t.name).Append(m.GetTextureScale(n).ToString("F5")).Append(m.GetTextureOffset(n).ToString("F5"));
                            break;
                    }
                    sb.Append(';');
                }
                return sb.ToString();
            }

            static string Hash(string s)
            {
                using (var sha = SHA1.Create())
                {
                    var h = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                    return BitConverter.ToString(h, 0, 4).Replace("-", "").ToLowerInvariant();
                }
            }

            static string Safe(string s)
            {
                var bad = Path.GetInvalidFileNameChars();
                var sb = new StringBuilder();
                foreach (char ch in s) sb.Append(bad.Contains(ch) || ch == ' ' ? '-' : ch);
                var r = sb.ToString();
                return r.Length > 48 ? r.Substring(0, 48) : r;
            }
        }

        static class GraphicsFormatUtility
        {
            public static bool HasAlpha(Texture2D t) =>
                t.format == TextureFormat.RGBA32 || t.format == TextureFormat.ARGB32 || t.format == TextureFormat.BGRA32 ||
                t.format == TextureFormat.RGBAHalf || t.format == TextureFormat.RGBAFloat || t.format == TextureFormat.RGBA4444 || t.format == TextureFormat.ARGB4444;
        }
    }
}
