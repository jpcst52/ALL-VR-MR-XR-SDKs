using System.Text;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Keeps a <see cref="TextMesh"/> inside a box, using the real font's glyph widths (not a guess per character).
    /// Put on the text object (it is saved in the scene, so the limits show in the Inspector). It breaks the text at
    /// spaces so no line is wider than <see cref="MaxWidth"/>, then shrinks the characters until the block is no taller
    /// than <see cref="MaxHeight"/>. Scenario code just sets <c>TextMesh.text</c>; the fit follows by itself.
    /// Limits are in metres in the text object's own space (the same space as <c>P.Text</c> sizes).
    /// </summary>
    [DisallowMultipleComponent]
    public class TextFit : MonoBehaviour
    {
        [Tooltip("Widest a line may be, in metres (0 = no limit).")]
        public float MaxWidth;
        [Tooltip("Tallest the whole block may be, in metres (0 = no limit).")]
        public float MaxHeight;
        [Tooltip("Break long text into lines at spaces. Off = one line per paragraph, shrunk to fit the width.")]
        public bool Wrap = true;
        [Tooltip("Smallest scale before the text is cut with an ellipsis.")]
        public float MinScale = 0.35f;
        [Tooltip("Character size before fitting (set from the TextMesh the first time).")]
        public float BaseSize;
        [Tooltip("The text as the scenario set it, before line breaks were added.")]
        [TextArea] public string Source = "";
        [HideInInspector] public string Applied = "";
        [Tooltip("Scale the last fit used (1 = unchanged).")]
        public float Scale = 1f;

        TextMesh tm;
        Font lastFont;
        float lastW, lastH;

        /// <summary>World size of one pixel of font atlas at characterSize 1: Unity draws a TextMesh at 0.1 m per pixel per unit of characterSize.</summary>
        const float WorldPerPixel = 0.1f;

        /// <summary>Adds (or updates) a fit on a text. Safe to call more than once.</summary>
        public static TextFit Add(TextMesh t, float maxWidth, float maxHeight = 0f, bool wrap = true)
        {
            var fit = t.GetComponent<TextFit>();
            if (fit == null) fit = t.gameObject.AddComponent<TextFit>();
            fit.MaxWidth = maxWidth;
            fit.MaxHeight = maxHeight;
            fit.Wrap = wrap;
            fit.tm = t;
            if (fit.BaseSize <= 0f) fit.BaseSize = t.characterSize;
            if (t.text != fit.Applied) fit.Source = t.text; // a baked text keeps the source it was saved with
            fit.Apply();
            return fit;
        }

        void OnEnable() { tm = GetComponent<TextMesh>(); }

        void LateUpdate()
        {
            if (tm == null) tm = GetComponent<TextMesh>();
            if (tm == null) return;
            if (tm.text != Applied) { Source = tm.text; Apply(); }
            else if (tm.font != lastFont || !Mathf.Approximately(lastW, MaxWidth) || !Mathf.Approximately(lastH, MaxHeight)) Apply();
        }

        /// <summary>Re-fits the text now.</summary>
        public void Apply()
        {
            if (tm == null) tm = GetComponent<TextMesh>();
            if (tm == null || tm.font == null) return;
            if (BaseSize <= 0f) BaseSize = tm.characterSize;
            lastFont = tm.font; lastW = MaxWidth; lastH = MaxHeight;
            string src = Source ?? "";
            float scale = 1f;
            string text = src;
            for (int pass = 0; pass < 14; pass++)
            {
                float size = BaseSize * scale;
                text = Wrap ? WrapAt(src, tm, size, MaxWidth) : src;
                Measure(tm.font, text, tm.fontSize, tm.fontStyle, size, out float w, out float h);
                float kw = MaxWidth > 0f && w > MaxWidth ? MaxWidth / w : 1f;
                float kh = MaxHeight > 0f && h > MaxHeight ? MaxHeight / h : 1f;
                float k = Mathf.Min(kw, kh);
                if (k >= 0.999f) break;
                scale *= Mathf.Max(0.5f, k * 0.985f);
                if (scale < MinScale) { scale = MinScale; text = Truncate(src, tm, BaseSize * scale); break; }
            }
            Scale = scale;
            tm.characterSize = BaseSize * scale;
            tm.text = text;
            Applied = text;
        }

        string Truncate(string src, TextMesh t, float size)
        {
            // last resort: keep what fits and end with an ellipsis, so nothing leaves its box
            string text = Wrap ? WrapAt(src, t, size, MaxWidth) : src;
            for (int i = 0; i < 400; i++)
            {
                Measure(t.font, text, t.fontSize, t.fontStyle, size, out float w, out float h);
                if ((MaxWidth <= 0f || w <= MaxWidth * 1.001f) && (MaxHeight <= 0f || h <= MaxHeight * 1.001f)) return text;
                int cut = Mathf.Max(0, text.Length - 5);
                text = text.Substring(0, cut).TrimEnd() + "...";
                if (cut == 0) return text;
            }
            return text;
        }

        // ------------------------------------------------------------------------------------------- measuring

        static int PixelSize(Font f, int fontSize) => fontSize > 0 ? fontSize : (f.fontSize > 0 ? f.fontSize : 16);

        /// <summary>Width of one line in font pixels at the given pixel size.</summary>
        public static float LinePixels(Font f, string line, int fontSize, FontStyle style)
        {
            if (f == null || string.IsNullOrEmpty(line)) return 0f;
            int px = PixelSize(f, fontSize);
            f.RequestCharactersInTexture(line, px, style);
            float w = 0f;
            foreach (char c in line)
            {
                if (f.GetCharacterInfo(c, out var ci, px, style)) w += ci.advance;
                else w += px * 0.5f;
            }
            return w;
        }

        /// <summary>Size in metres of a block of text for a TextMesh with this font, pixel size, style and characterSize.</summary>
        public static void Measure(Font f, string text, int fontSize, FontStyle style, float characterSize, out float width, out float height)
        {
            width = 0f; height = 0f;
            if (f == null) return;
            int px = PixelSize(f, fontSize);
            float lineH = f.lineHeight > 0 ? f.lineHeight * px / Mathf.Max(1, f.fontSize > 0 ? f.fontSize : px) : px * 1.2f;
            int lines = 1;
            float widest = 0f;
            foreach (var l in text.Split('\n'))
            {
                widest = Mathf.Max(widest, LinePixels(f, l, fontSize, style));
            }
            foreach (char c in text) if (c == '\n') lines++;
            float scale = characterSize * WorldPerPixel;
            width = widest * scale;
            // the drawn block: full line spacing between lines, but the last line only as tall as its glyphs
            // (checked against the renderer's bounds: width matches exactly, this height within about 5 percent)
            height = ((lines - 1) * lineH + Mathf.Min(lineH, 1.1f * px)) * scale;
        }

        static float LineHeightPixels(Font f, int fontSize)
        {
            int px = PixelSize(f, fontSize);
            return f.lineHeight > 0 ? f.lineHeight * px / Mathf.Max(1, f.fontSize > 0 ? f.fontSize : px) : px * 1.2f;
        }

        /// <summary>Size of the text a TextMesh shows right now (with the real font), in metres in its own space.</summary>
        public static void Measure(TextMesh t, out float width, out float height) =>
            Measure(t.font, t.text, t.fontSize, t.fontStyle, t.characterSize, out width, out height);

        /// <summary>Width in metres of one line drawn by <c>P.Text(..., height, ..., bold)</c> with the kit font.</summary>
        public static float WidthOf(string text, float height, bool bold = false)
        {
            // P.Text uses fontSize 160 and characterSize = height / 160 * 10
            float c = height / 160f * 10f;
            Measure(P.Font, text, 160, bold ? FontStyle.Bold : FontStyle.Normal, c, out float w, out _);
            return w;
        }

        /// <summary>Breaks <paramref name="text"/> at spaces so each line is at most <paramref name="maxWidth"/> metres wide when drawn by <c>P.Text(..., height, ..., bold)</c>.</summary>
        public static string WrapMeasured(string text, float height, bool bold, float maxWidth)
        {
            return WrapAt(text ?? "", P.Font, 160, bold ? FontStyle.Bold : FontStyle.Normal, height / 160f * 10f, maxWidth);
        }

        /// <summary>Height in metres of one line of text drawn by <c>P.Text(..., height, ..., bold)</c> (the font's own line spacing).</summary>
        public static float LineHeight(float height, bool bold = false)
        {
            return LineHeightPixels(P.Font, 160) * (height / 160f * 10f) * WorldPerPixel;
        }

        static string WrapAt(string src, TextMesh t, float size, float maxWidth) => WrapAt(src, t.font, t.fontSize, t.fontStyle, size, maxWidth);

        static string WrapAt(string src, Font font, int fontSize, FontStyle style, float size, float maxWidth)
        {
            if (maxWidth <= 0f) return src;
            int px = PixelSize(font, fontSize);
            float maxPx = maxWidth / (size * WorldPerPixel);
            var sb = new StringBuilder();
            bool firstPara = true;
            foreach (var para in src.Split('\n'))
            {
                if (!firstPara) sb.Append('\n');
                firstPara = false;
                string line = "";
                foreach (var word in para.Split(' '))
                {
                    if (word.Length == 0) continue;
                    string trial = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && LinePixels(font, trial, px, style) > maxPx)
                    {
                        sb.Append(line).Append('\n');
                        line = word;
                    }
                    else line = trial;
                }
                sb.Append(line);
            }
            return sb.ToString();
        }
    }
}
