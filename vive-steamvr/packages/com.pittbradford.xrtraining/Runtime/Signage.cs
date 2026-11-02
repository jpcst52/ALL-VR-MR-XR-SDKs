using System.Collections.Generic;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Professional-looking signage built from code: rounded panels with a border, accent bar and soft shadow, and
    /// name tags with a leader line for things that would otherwise have bare text floating in the air.
    /// Uses the always-included "Sprites/Default" shader, so it survives builds and needs no imported art.
    /// Panels face -Z like the rest of the kit's text: put the viewer on the -Z side.
    /// </summary>
    public static class Ui
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static Material SpriteMat(Texture2D tex)
        {
            var key = tex.name;
            if (Cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex, name = key };
            Cache[key] = m;
            return m;
        }

        /// <summary>Opaque-where-painted material (writes depth), so panels hide text and objects behind them.</summary>
        static Material CutoutMat(Texture2D tex)
        {
            var key = "cut-" + tex.name;
            if (Cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(Shader.Find("Unlit/Transparent Cutout")) { mainTexture = tex, name = key };
            m.SetFloat("_Cutoff", 0.5f);
            Cache[key] = m;
            return m;
        }

        /// <summary>Rounded rectangle texture; radius and border are fractions of the shorter side.</summary>
        public static Texture2D RoundedRect(string name, Color fill, Color border, float aspect, float radius, float borderWidth, Color? accent = null, float accentHeight = 0f)
        {
            int h = 256, w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), 128, 1536);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            float r = radius * h, b = borderWidth * h, ah = accentHeight * h;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // signed distance to a rounded rectangle
                float dx = Mathf.Abs(x + 0.5f - w / 2f) - (w / 2f - r);
                float dy = Mathf.Abs(y + 0.5f - h / 2f) - (h / 2f - r);
                float d = Mathf.Min(Mathf.Max(dx, dy), 0f) + new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude - r;
                float cover = Mathf.Clamp01(0.5f - d);
                Color c = d > -b ? border : fill;
                if (accent.HasValue && y >= h - ah && d < -b) c = accent.Value;
                c.a *= cover;
                px[y * w + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        static GameObject QuadWith(Transform parent, string name, Vector3 localPos, Vector2 size, Texture2D tex)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false);
            q.transform.localPosition = localPos;
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = SpriteMat(tex);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q;
        }

        /// <summary>
        /// A rounded panel centred on <paramref name="localPos"/> with a soft shadow behind it. Put text at local z = -0.01
        /// (a little in front of the panel). Returns the panel's root; its children are "Shadow" and "Face".
        /// </summary>
        public static Transform Panel(Transform parent, string name, Vector3 localPos, Vector2 size, Color fill, Color? accent = null, float accentHeight = 0.07f)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            float aspect = size.x / size.y;
            string suffix = $"{ColorUtility.ToHtmlStringRGBA(fill)}-{(accent.HasValue ? ColorUtility.ToHtmlStringRGBA(accent.Value) : "n")}-{aspect:F2}";
            var shadowTex = RoundedRect("shadow-" + aspect.ToString("F2"), new Color(0, 0, 0, 0.35f), new Color(0, 0, 0, 0.35f), aspect, 0.18f, 0f);
            var faceTex = RoundedRect("panel-" + suffix, fill, Color.Lerp(fill, Color.white, 0.35f), aspect, 0.12f, 0.022f, accent, accentHeight);
            QuadWith(root, "Shadow", new Vector3(0.012f, -0.014f, 0.004f), size * 1.03f, shadowTex);
            QuadWith(root, "Face", Vector3.zero, size, faceTex).GetComponent<MeshRenderer>().sharedMaterial = CutoutMat(faceTex);
            return root;
        }


        /// <summary>
        /// University and department plate: the Pitt-Bradford logo when the file is present at Resources/Branding/pitt-bradford
        /// (the logo is kept out of the public repo, so it is only in local builds), otherwise a text wordmark; plus the CIST line.
        /// </summary>
        public static Transform BrandPlate(Transform parent, Vector3 localPos, float width = 1.6f)
        {
            float height = width * 0.5f;
            var root = Panel(parent, "Brand plate", localPos, new Vector2(width, height), new Color(0.0f, 0.13f, 0.36f, 0.97f), P.Gold, 0.035f);
            var logo = Resources.Load<Texture2D>("Branding/pitt-bradford");
            if (logo != null)
            {
                float lw = width * 0.82f, lh = lw * logo.height / logo.width;
                var q = QuadWith(root, "Logo", new Vector3(0, height * 0.12f, -0.006f), new Vector2(lw, lh), logo);
                q.GetComponent<MeshRenderer>().sharedMaterial = SpriteMat(logo);
            }
            else
            {
                P.Text(root, "UNIVERSITY OF PITTSBURGH", new Vector3(0, height * 0.2f, -0.01f), width * 0.052f, Color.white, TextAnchor.MiddleCenter, true);
                P.Text(root, "AT BRADFORD", new Vector3(0, height * 0.04f, -0.01f), width * 0.052f, P.Gold, TextAnchor.MiddleCenter, true);
            }
            P.Text(root, "CIST  |  Computer Information Systems & Technology", new Vector3(0, -height * 0.25f, -0.01f), width * 0.034f, P.Sky, TextAnchor.MiddleCenter, true);
            P.Text(root, "XR Training Lab", new Vector3(0, -height * 0.39f, -0.01f), width * 0.03f, new Color(0.7f, 0.78f, 0.9f), TextAnchor.MiddleCenter);
            return root;
        }

        /// <summary>
        /// A name tag for a part of the scene: a small panel with the text, a coloured accent, and a leader line to
        /// <paramref name="anchor"/>. The tag turns to face the player (around the vertical axis only).
        /// </summary>
        public static Transform Tag(Transform parent, string text, Vector3 anchorLocal, Vector3 offsetLocal, float height = 0.09f, Color? accent = null)
        {
            var root = new GameObject("Tag: " + text).transform;
            root.SetParent(parent, false);
            root.localPosition = anchorLocal + offsetLocal;

            float textW = Mathf.Max(0.18f, text.Length * height * 0.62f) + height * 1.1f;
            var size = new Vector2(textW, height * 1.9f);
            var acc = accent ?? P.Gold;
            var plate = Panel(root, "Plate", Vector3.zero, size, new Color(0.02f, 0.07f, 0.18f, 0.94f), acc, 0.11f);
            var t = P.Text(plate, text, new Vector3(0, -height * 0.04f, -0.012f), height, Color.white, TextAnchor.MiddleCenter, true);
            t.name = "Label";

            // Leader line from the plate to the anchor, drawn in world space so the tag can turn freely
            var line = new GameObject("Leader").AddComponent<LineRenderer>();
            line.transform.SetParent(root, false);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.006f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = acc;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var face = root.gameObject.AddComponent<TagFacing>();
            face.Anchor = parent;
            face.AnchorLocal = anchorLocal;
            face.Line = line;
            face.Tag = root;
            return root;
        }
    }

    /// <summary>Turns a <see cref="Ui.Tag"/> to face the viewer and keeps its leader line on the anchor.</summary>
    public class TagFacing : MonoBehaviour
    {
        /// <summary>Set by the screenshot driver so tags face the capture camera instead of the player.</summary>
        public static Transform ViewOverride;
        public Transform Anchor, Tag;
        public Vector3 AnchorLocal;
        public LineRenderer Line;

        void LateUpdate()
        {
            var cam = ViewOverride != null ? ViewOverride : (Camera.main != null ? Camera.main.transform : null);
            if (cam != null)
            {
                var away = Tag.position - cam.position;
                away.y = 0f;
                // Panels face -Z, so look in the direction pointing away from the viewer
                if (away.sqrMagnitude > 0.0001f) Tag.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
            var anchorWorld = Anchor.TransformPoint(AnchorLocal);
            // End the line at the plate's near edge instead of its centre
            Line.SetPosition(0, Tag.position - Vector3.up * 0.04f);
            Line.SetPosition(1, anchorWorld);
        }
    }
}
