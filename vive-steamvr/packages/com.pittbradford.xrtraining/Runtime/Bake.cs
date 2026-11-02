using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PittBradford.XR
{
    /// <summary>
    /// "Adopt or create" support for baked scenes. Each helper call asks for a child with a given name under a given
    /// parent; the n-th call for the same (parent, name) pair adopts the n-th child of that name that was in the scene when
    /// it loaded. If there is none, the helper creates the object as it always did. Adoption is only switched on in scenes
    /// that contain a <see cref="BakedWorld"/>, so unbaked projects behave exactly as before.
    /// </summary>
    public static class Bake
    {
        // objects that were in the scene file when it loaded
        static readonly HashSet<Transform> Loaded = new HashSet<Transform>();
        static readonly List<Transform> LoadedRoots = new List<Transform>();
        static bool adoption;
        // per parent: name -> loaded children of that name in sibling order; and the next ordinal to hand out
        static readonly Dictionary<Transform, Dictionary<string, List<Transform>>> Index = new Dictionary<Transform, Dictionary<string, List<Transform>>>();
        static readonly Dictionary<Transform, Dictionary<string, int>> Next = new Dictionary<Transform, Dictionary<string, int>>();
        static readonly Dictionary<string, List<Transform>> RootIndex = new Dictionary<string, List<Transform>>();
        static readonly Dictionary<string, int> RootNext = new Dictionary<string, int>();
        static readonly HashSet<int> Adopted = new HashSet<int>();
        static readonly Dictionary<int, int> SpeakerNext = new Dictionary<int, int>();

        /// <summary>True while a baked world is present and its objects are being adopted.</summary>
        public static bool Active => adoption;

#if UNITY_EDITOR
        /// <summary>Editor only: every object a helper created or adopted this play session (what the bake tool saves).</summary>
        public static readonly HashSet<GameObject> Tracked = new HashSet<GameObject>();
        /// <summary>Editor only: frame of the last helper call, so the bake tool can wait until building has finished.</summary>
        public static int LastHelperFrame;
        /// <summary>Editor only: set by the bake tool so the bake session runs frozen (no physics settling, no gameplay).</summary>
        public const string StageKey = "PittBradford.Bake.Stage";
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR
            Tracked.Clear();
            LastHelperFrame = 0;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeSceneLoad()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetString(StageKey, "") == "play") Time.timeScale = 0f;
#endif
        }

        static void Clear()
        {
            Loaded.Clear();
            LoadedRoots.Clear();
            Index.Clear();
            Next.Clear();
            RootIndex.Clear();
            RootNext.Clear();
            Adopted.Clear();
            SpeakerNext.Clear();
            adoption = false;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Clear();
            Snapshot(scene);
        }

        static void Snapshot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                LoadedRoots.Add(root.transform);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    Loaded.Add(t);
                    if (!adoption && t.GetComponent<BakedWorld>() != null) adoption = true;
                }
            }
            if (adoption) RestoreText(scene);
        }

        /// <summary>
        /// Baked text is saved with Unity's built-in font so it shows in the editor. At runtime it gets the kit's OS font
        /// and depth-tested text material back, so it looks exactly as unbaked text does (including text inside baked
        /// panels and tags that no helper call touches again).
        /// </summary>
        static void RestoreText(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tm in root.GetComponentsInChildren<TextMesh>(true))
                {
                    if (tm.font != null && tm.font.name != BuiltinFontName) continue; // a font someone chose on purpose
                    tm.font = P.Font;
                    var r = tm.GetComponent<MeshRenderer>();
                    if (r != null) r.sharedMaterial = P.TextMaterial;
                }
        }

        /// <summary>The built-in font the bake tool gives baked text so it is visible in the editor.</summary>
        public const string BuiltinFontName = "LegacyRuntime";

        static bool snapshotChecked;
        static void EnsureSnapshot()
        {
            // sceneLoaded normally fills this before any Start; this covers a helper called earlier (e.g. from Awake)
            if (snapshotChecked || Loaded.Count > 0) return;
            snapshotChecked = true;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded) Snapshot(s);
            }
        }

        /// <summary>True when the object was in the scene file when it loaded (a baked object, or one placed by hand).</summary>
        public static bool WasLoaded(Transform t) => t != null && Loaded.Contains(t);

        /// <summary>
        /// Called by the helpers: returns the loaded object to adopt for the next (parent, name) request, or null when
        /// the helper should create a new one. Every call advances the ordinal for that pair, adopted or not.
        /// </summary>
        public static GameObject TryAdopt(Transform parent, string name)
        {
            if (!Application.isPlaying) return null;
            EnsureSnapshot();
            if (!adoption) return null;
            List<Transform> list;
            int ordinal;
            if (parent == null)
            {
                if (!RootIndex.TryGetValue(name, out list))
                {
                    list = new List<Transform>();
                    foreach (var r in LoadedRoots) if (r != null && r.name == name) list.Add(r);
                    RootIndex[name] = list;
                }
                RootNext.TryGetValue(name, out ordinal);
                RootNext[name] = ordinal + 1;
            }
            else
            {
                if (!Loaded.Contains(parent)) return null; // a parent made at runtime has nothing baked under it
                if (!Index.TryGetValue(parent, out var byName))
                {
                    byName = new Dictionary<string, List<Transform>>();
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var c = parent.GetChild(i);
                        if (!Loaded.Contains(c)) continue;
                        if (!byName.TryGetValue(c.name, out var l)) byName[c.name] = l = new List<Transform>();
                        l.Add(c);
                    }
                    Index[parent] = byName;
                    Next[parent] = new Dictionary<string, int>();
                }
                if (!byName.TryGetValue(name, out list)) list = null;
                var next = Next[parent];
                next.TryGetValue(name, out ordinal);
                next[name] = ordinal + 1;
            }
            if (list == null || ordinal >= list.Count || list[ordinal] == null) return null;
            var go = list[ordinal].gameObject;
            Adopted.Add(go.GetInstanceID());
            Track(go);
            return go;
        }

        /// <summary>Records an object a helper just created (the bake tool saves created and adopted objects).</summary>
        public static void Created(GameObject go)
        {
            Track(go);
        }

        static void Track(GameObject go)
        {
#if UNITY_EDITOR
            Tracked.Add(go);
            LastHelperFrame = Time.frameCount;
#endif
        }

        /// <summary>True when the helper returned an object from the baked scene instead of creating it.</summary>
        public static bool IsAdopted(Object o)
        {
            if (!adoption || o == null) return false;
            var go = o as GameObject ?? (o as Component)?.gameObject;
            return go != null && Adopted.Contains(go.GetInstanceID());
        }

        /// <summary>Ordinal for the n-th <see cref="P.Speaker"/> call on the same object (it can hold several sources).</summary>
        internal static int NextSpeaker(GameObject go)
        {
            int id = go.GetInstanceID();
            SpeakerNext.TryGetValue(id, out int n);
            SpeakerNext[id] = n + 1;
            return n;
        }
    }
}
