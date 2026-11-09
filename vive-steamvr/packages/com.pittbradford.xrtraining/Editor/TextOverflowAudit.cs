using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Finds text that runs past its panel. For every TextMesh in a scene (inactive ones too) it measures the block the
    /// kit font would draw (real glyph widths and line spacing, after the text's <see cref="TextFit"/> if it has one),
    /// finds the smallest panel face (a <c>Face</c> quad made by Ui.Panel) that holds the text's anchor point, and reports
    /// any corner of the text block that lies outside that face. Texts with no panel around them (floor markings, labels on
    /// props) are counted as "free" and not judged.
    /// Menu: Pitt-Bradford > Audit text overflow (open scene). Batch: -executeMethod PittBradford.XR.EditorTools.TextOverflowAudit.RunAndExit
    /// [-pbScenes "Assets/a.unity;Assets/b.unity"] [-pbAuditOut file.txt]  (no scenes given = every scene in Build Settings).
    /// The audit changes nothing it measures: texts it fits are put back afterwards.
    /// </summary>
    public static class TextOverflowAudit
    {
        public class SceneResult
        {
            public string Scene;
            public int Texts, Boxed, Free, Over;
            public readonly List<string> Details = new List<string>();
        }

        [MenuItem("Pitt-Bradford/Audit text overflow (open scene)", false, 60)]
        public static void AuditOpenScene()
        {
            var r = Audit(SceneManager.GetActiveScene());
            Debug.Log(Format(new[] { r }));
        }

        public static void RunAndExit()
        {
            string[] scenes = null;
            string outFile = null;
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
            {
                if (a[i] == "-pbScenes") scenes = a[i + 1].Split(';').Where(s => s.Length > 0).ToArray();
                if (a[i] == "-pbAuditOut") outFile = a[i + 1];
            }
            if (scenes == null) scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var results = new List<SceneResult>();
            foreach (var path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                results.Add(Audit(scene));
            }
            string text = Format(results);
            Debug.Log(text);
            if (!string.IsNullOrEmpty(outFile)) File.WriteAllText(outFile, text);
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Checks the measuring formula against Unity's own text mesh: makes a few texts with P.Text and prints the renderer's bounds
        /// next to what <see cref="TextFit.Measure(TextMesh, out float, out float)"/> says. Batch: -executeMethod ...TextOverflowAudit.CalibrateAndExit
        /// </summary>
        public static void CalibrateAndExit()
        {
            var samples = new (string text, float height, bool bold)[]
            {
                ("Pitt-Bradford XR Training Hub", 0.62f, true),
                ("Step onto a portal's pad and wait a moment to start.   Desktop: keys 1-4.", 0.24f, false),
                ("3  Warehouse & Forklift", 0.3f, true),
                ("Walk the floor safely\naround forklifts", 0.14f, false),
            };
            var made = new List<TextMesh>();
            var root = new GameObject("calibrate");
            foreach (var s in samples)
            {
                var tm = P.Text(root.transform, s.text, Vector3.zero, s.height, Color.white, TextAnchor.MiddleCenter, s.bold, "t");
                tm.text = s.text;
                made.Add(tm);
            }
            // the same texts on Unity's built-in font, which is what a baked scene shows in the editor before play
            var builtin = Resources.GetBuiltinResource<Font>(Bake.BuiltinFontName + ".ttf");
            foreach (var s in samples)
            {
                var tm = P.Text(root.transform, s.text, Vector3.zero, s.height, Color.white, TextAnchor.MiddleCenter, s.bold, "b");
                tm.text = s.text;
                tm.font = builtin;
                tm.GetComponent<MeshRenderer>().sharedMaterial = builtin.material;
                made.Add(tm);
            }
            int frames = 0;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (++frames < 8) return;
                EditorApplication.update -= tick;
                for (int i = 0; i < made.Count; i++)
                {
                    var tm = made[i];
                    var r = tm.GetComponent<MeshRenderer>();
                    var b = r.localBounds;
                    TextFit.Measure(tm, out float w, out float h);
                    Debug.Log($"[calibrate] {tm.font.name} \"{samples[i % samples.Length].text.Replace("\n", " / ")}\" h={samples[i % samples.Length].height}: renderer {b.size.x:F3} x {b.size.y:F3} m, measured {w:F3} x {h:F3} m, ratio w {b.size.x / Mathf.Max(0.0001f, w):F3} h {b.size.y / Mathf.Max(0.0001f, h):F3}");
                }
                EditorApplication.Exit(0);
            };
            EditorApplication.update += tick;
        }

        static string Format(IEnumerable<SceneResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[textaudit] ---- text overflow audit ----");
            foreach (var r in results)
            {
                sb.AppendLine($"[textaudit] {r.Scene}: {r.Texts} texts, {r.Boxed} inside a panel, {r.Free} free (no panel), OVERFLOWING: {r.Over}");
                foreach (var d in r.Details) sb.AppendLine("[textaudit]    " + d);
            }
            sb.AppendLine($"[textaudit] TOTAL texts {results.Sum(r => r.Texts)}, in panels {results.Sum(r => r.Boxed)}, overflowing {results.Sum(r => r.Over)}");
            return sb.ToString();
        }

        class Box
        {
            public Transform T;
            public Vector2 Size; // world metres (lossy scale of the unit quad)
            public float Area => Size.x * Size.y;
        }

        [MenuItem("Pitt-Bradford/Fix text overflow (open scene)", false, 61)]
        public static void FixOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            var r = Audit(scene, true);
            Debug.Log(Format(new[] { r }));
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Batch: opens each scene (-pbScenes "a;b", else Build Settings), adds a TextFit to every overflowing text, saves, audits again and quits.</summary>
        public static void FixAndExit()
        {
            string[] scenes = null;
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-pbScenes") scenes = a[i + 1].Split(';').Where(s => s.Length > 0).ToArray();
            if (scenes == null) scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var results = new List<SceneResult>();
            foreach (var path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var fixedUp = Audit(scene, true);
                if (fixedUp.Over > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                results.Add(Audit(scene)); // after the fix
                Debug.Log($"[textaudit] fixed {fixedUp.Over} text(s) in {path}");
            }
            Debug.Log(Format(results));
            EditorApplication.Exit(0);
        }

        public static SceneResult Audit(Scene scene, bool fix = false)
        {
            var res = new SceneResult { Scene = scene.path };
            var boxes = new List<Box>();
            var texts = new List<TextMesh>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Face" && t.parent != null && t.parent.Find("Shadow") != null && t.GetComponent<MeshFilter>() != null)
                    {
                        var s = t.lossyScale;
                        boxes.Add(new Box { T = t, Size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y)) });
                    }
                    var tm = t.GetComponent<TextMesh>();
                    if (tm != null) texts.Add(tm);
                }
            }
            var font = P.Font;
            foreach (var tm in texts)
            {
                if (string.IsNullOrWhiteSpace(tm.text) && !(tm.GetComponent<TextFit>() != null && !string.IsNullOrWhiteSpace(tm.GetComponent<TextFit>().Source))) continue;
                res.Texts++;
                // measure what the text will look like with the kit font, after its fit
                string shown = tm.text; float size = tm.characterSize;
                var fit = tm.GetComponent<TextFit>();
                if (fit != null)
                {
                    string origText = tm.text; float origSize = tm.characterSize; Font origFont = tm.font;
                    string origApplied = fit.Applied, origSource = fit.Source; float origScale = fit.Scale, origBase = fit.BaseSize;
                    tm.font = font;
                    fit.Apply();
                    shown = tm.text; size = tm.characterSize;
                    // put everything back: the audit leaves the scene as it was
                    tm.text = origText; tm.characterSize = origSize; tm.font = origFont;
                    fit.Applied = origApplied; fit.Source = origSource; fit.Scale = origScale; fit.BaseSize = origBase;
                }
                TextFit.Measure(font, shown, tm.fontSize, tm.fontStyle, size, out float w, out float h);

                // text block in the text's own space
                GetExtent(tm.anchor, w, h, out float x0, out float x1, out float y0, out float y1);
                var tt = tm.transform;
                Box best = null;
                foreach (var b in boxes)
                {
                    var local = b.T.InverseTransformPoint(tt.position);
                    if (Mathf.Abs(local.x) > 0.5f + 0.02f / Mathf.Max(0.01f, b.Size.x) || Mathf.Abs(local.y) > 0.5f + 0.02f / Mathf.Max(0.01f, b.Size.y)) continue;
                    if (Mathf.Abs(local.z * Mathf.Abs(b.T.lossyScale.z)) > 0.3f) continue;
                    if (Mathf.Abs(Vector3.Dot(b.T.forward, tt.forward)) < 0.9f) continue;
                    if (best == null || b.Area < best.Area) best = b;
                }
                if (best == null)
                {
                    res.Free++;
                    if (Environment.GetCommandLineArgs().Contains("-pbAuditFree"))
                        res.Details.Add($"free: {PathOf(tt)}  \"{shown.Replace("\n", " / ")}\"  ({w:F2} x {h:F2} m)");
                    continue;
                }
                res.Boxed++;
                // The panel has rounded corners and a border, so text must stay a little inside the edge: 4% of the shorter side, at most 4 cm
                float inset = Mathf.Min(0.04f, 0.04f * Mathf.Min(best.Size.x, best.Size.y));
                float over = 0f; string where = "";
                var corners = new[] { new Vector3(x0, y0, 0), new Vector3(x1, y0, 0), new Vector3(x0, y1, 0), new Vector3(x1, y1, 0) };
                foreach (var c in corners)
                {
                    var lp = best.T.InverseTransformPoint(tt.TransformPoint(c));
                    float ox = (Mathf.Abs(lp.x) - 0.5f) * best.Size.x + inset, oy = (Mathf.Abs(lp.y) - 0.5f) * best.Size.y + inset;
                    if (ox > 0.004f && ox > over) { over = ox; where = lp.x < 0 ? "left" : "right"; }
                    if (oy > 0.004f && oy > over) { over = oy; where = lp.y < 0 ? "bottom" : "top"; }
                }
                if (over > 0f)
                {
                    res.Over++;
                    if (fix)
                    {
                        // shrink in steps until all four corners are inside the inset panel; the TextFit keeps it so when the text changes
                        float k = 1f;
                        for (int step = 0; step < 40 && k > 0.2f; step++)
                        {
                            k -= 0.025f;
                            GetExtent(tm.anchor, w * k, h * k, out float fx0, out float fx1, out float fy0, out float fy1);
                            bool inside = true;
                            foreach (var c in new[] { new Vector3(fx0, fy0, 0), new Vector3(fx1, fy0, 0), new Vector3(fx0, fy1, 0), new Vector3(fx1, fy1, 0) })
                            {
                                var lp = best.T.InverseTransformPoint(tt.TransformPoint(c));
                                if ((Mathf.Abs(lp.x) - 0.5f) * best.Size.x + inset > 0.004f || (Mathf.Abs(lp.y) - 0.5f) * best.Size.y + inset > 0.004f) inside = false;
                            }
                            if (inside) break;
                        }
                        float baseW = fit != null ? w / Mathf.Max(0.01f, fit.Scale) : w; // width the text has at its own size
                        TextFit.Add(tm, w * k, 0f, false);
                        if (fit == null) tm.GetComponent<TextFit>().Apply();
                    }
                    string snippet = shown.Replace("\n", " / ");
                    if (snippet.Length > 60) snippet = snippet.Substring(0, 57) + "...";
                    res.Details.Add($"{PathOf(tt)}  \"{snippet}\"  runs {over:F2} m past the {where} edge (text {w:F2} x {h:F2} m, panel {best.Size.x:F2} x {best.Size.y:F2} m)");
                }
            }
            return res;
        }

        static void GetExtent(TextAnchor a, float w, float h, out float x0, out float x1, out float y0, out float y1)
        {
            int ai = (int)a; // 0..8: row-major, Upper/Middle/Lower x Left/Center/Right
            int col = ai % 3, row = ai / 3;
            x0 = col == 0 ? 0 : col == 1 ? -w / 2 : -w; x1 = x0 + w;
            y1 = row == 0 ? 0 : row == 1 ? h / 2 : h; y0 = y1 - h;
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            string p = string.Join("/", parts);
            return p.Length > 90 ? "..." + p.Substring(p.Length - 87) : p;
        }
    }
}
