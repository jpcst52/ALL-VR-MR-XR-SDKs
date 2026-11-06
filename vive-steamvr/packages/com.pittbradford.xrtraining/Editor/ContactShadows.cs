using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Soft contact shadows under standing objects, as ONE merged mesh with ONE alpha-blended material per scene (one draw call).
    /// Built-in forward lighting with few shadow-casting lights leaves furniture, cabinets, drums, cartons and machines looking
    /// pasted on the floor; a faint dark halo under each footprint grounds them. Runs at the end of the bake (RenderBudget.Apply) and
    /// from the menu. It only adds the "Contact shadows" object (replacing the previous one); nothing else in the scene changes.
    /// The mesh and material are saved under Assets/Baked/&lt;scene&gt;/ContactShadows so the scene file can reference them.
    /// </summary>
    public static class ContactShadows
    {
        public const string ObjectName = "Contact shadows";
        public const int MaxBlobs = 1500;
        public const float Alpha = 0.5f, Margin = 0.22f, FloorLift = 0.007f;
        static readonly string[] SkipNames = { "forklift", "fork lift", "coworker", "co-worker", "person", "driver", "pallet jack", "player", "xr rig", "xrrig", "floor", "ground", "wall", "roof", "ceiling", "shadow", "decal", "marking" };

        [MenuItem("Pitt-Bradford/Contact shadows: rebuild for open scene", false, 43)]
        public static void BuildMenu() => Debug.Log(Build(SceneManager.GetActiveScene()));

        static bool Skip(Renderer r)
        {
            if (!(r is MeshRenderer) || !r.enabled || !r.gameObject.activeInHierarchy) return true;
            if (r.GetComponent<TextMesh>() != null) return true;
            for (var t = r.transform; t != null; t = t.parent)
            {
                string n = t.name.ToLowerInvariant();
                foreach (var s in SkipNames) if (n.Contains(s)) return true;
                if (t.GetComponent<Rigidbody>() != null || t.GetComponent<Animator>() != null || t.GetComponent<Light>() != null || t.GetComponent<LODGroup>() != null) return true;
            }
            if (r.sharedMaterial == null || r.sharedMaterial.renderQueue >= 3000) return true;
            return false;
        }

        public static string Build(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == ObjectName) Object.DestroyImmediate(root);

            var rects = new List<Vector4>(); // minX, minZ, maxX, maxZ
            foreach (var root in scene.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (rects.Count >= MaxBlobs) break;
                var b = r.bounds;
                if (b.min.y < -0.05f || b.min.y > 0.3f || b.size.y < 0.45f) continue;
                if (b.size.x < 0.35f || b.size.z < 0.35f || b.size.x > 9f || b.size.z > 9f) continue;
                if (Skip(r)) continue;
                rects.Add(new Vector4(b.min.x, b.min.z, b.max.x, b.max.z));
            }
            if (rects.Count == 0) return $"[contact shadows] {scene.name}: nothing to ground";

            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var inner = new Color(0f, 0f, 0f, Alpha); var outer = new Color(0f, 0f, 0f, 0f);
            foreach (var q in rects)
            {
                float i0 = q.x + 0.04f, j0 = q.y + 0.04f, i1 = q.z - 0.04f, j1 = q.w - 0.04f; // inner (full strength)
                float o0 = q.x - Margin, p0 = q.y - Margin, o1 = q.z + Margin, p1 = q.w + Margin; // outer (fades to nothing)
                int b = v.Count;
                float y = FloorLift;
                v.Add(new Vector3(o0, y, p0)); v.Add(new Vector3(o1, y, p0)); v.Add(new Vector3(o1, y, p1)); v.Add(new Vector3(o0, y, p1)); // 0..3 outer
                v.Add(new Vector3(i0, y, j0)); v.Add(new Vector3(i1, y, j0)); v.Add(new Vector3(i1, y, j1)); v.Add(new Vector3(i0, y, j1)); // 4..7 inner
                for (int k = 0; k < 4; k++) c.Add(outer);
                for (int k = 0; k < 4; k++) c.Add(inner);
                for (int k = 0; k < 4; k++)
                {
                    int a = b + k, a2 = b + (k + 1) % 4, ia = b + 4 + k, ia2 = b + 4 + (k + 1) % 4;
                    // upward-facing (clockwise seen from above, Unity's front face)
                    t.Add(a); t.Add(a2); t.Add(ia); t.Add(a2); t.Add(ia2); t.Add(ia);
                }
                t.Add(b + 4); t.Add(b + 5); t.Add(b + 7); t.Add(b + 5); t.Add(b + 6); t.Add(b + 7);
            }
            // keep the 16-bit index limit in mind: 8 vertices per blob, 1500 blobs = 12,000 vertices
            var mesh = new Mesh { name = "contact-shadows" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            var uv = new Vector2[v.Count]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            string folder = "Assets/Baked/" + Path.GetFileNameWithoutExtension(scene.path) + "/ContactShadows";
            Directory.CreateDirectory(folder);
            AssetDatabase.DeleteAsset(folder + "/contact-shadows.asset");
            AssetDatabase.CreateAsset(mesh, folder + "/contact-shadows.asset");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(folder + "/contact-shadows.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended")) { name = "contact-shadows" };
                if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                mat.renderQueue = 2999; // after opaque floor decals, before other transparents
                AssetDatabase.CreateAsset(mat, folder + "/contact-shadows.mat");
            }
            var go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return $"[contact shadows] {scene.name}: {rects.Count} footprints, 1 mesh, 1 draw call";
        }
    }
}
