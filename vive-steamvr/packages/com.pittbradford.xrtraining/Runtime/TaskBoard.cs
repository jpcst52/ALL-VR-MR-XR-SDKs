using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// A world-space checklist panel: the scenario title, the steps (pending / current / done), a status line, and the
    /// running score. Mistakes and critical misses are tracked so each scenario can report them at the end.
    /// </summary>
    public class TaskBoard : MonoBehaviour
    {
        public int Mistakes { get; private set; }
        public int CriticalMisses { get; private set; }
        public int Current { get; private set; }

        readonly List<TextMesh> lines = new List<TextMesh>();
        readonly List<bool> done = new List<bool>();
        TextMesh status, footer, resultLine;
        float textScale = 1f;
        float width = 1.9f;
        const float FooterSize = 0.036f;

        static readonly Color Pending = new Color(0.62f, 0.7f, 0.82f);
        static readonly Color Active = Color.white;
        static readonly Color Done = new Color(0.3f, 0.9f, 0.62f);

        /// <summary>Height of the board panel for a text scale and number of steps (room for status, a footer of three lines and a result line).</summary>
        public static float PanelHeight(float textScale, int steps) => (0.7f + 0.35f + 0.12f) * textScale + steps * 0.095f * textScale;

        /// <summary>
        /// Builds the board from the kit helpers. In a baked scene (the normal case) every part is adopted from the scene file
        /// and only its text is set; the code that creates the parts runs only while the scene is being baked in the editor.
        /// <paramref name="name"/> tells boards under one parent apart (the baked object has that name in the Hierarchy).
        /// </summary>
        public static TaskBoard Create(Transform parent, Vector3 position, Quaternion rotation, string title, string subtitle, string[] steps, float width = 1.9f, float textScale = 1f, string name = "Task Board")
        {
            var go = P.Node(name, parent);
            if (P.IsNew(go)) go.transform.SetPositionAndRotation(position, rotation);
            var board = P.Ensure<TaskBoard>(go);
            board.textScale = textScale;
            board.width = width;

            float k = textScale;
            float lineHeight = 0.095f * k;
            float height = PanelHeight(k, steps.Length);
            float inner = width - 0.2f;
            Ui.Panel(go.transform, "Panel", new Vector3(0, -height / 2f + 0.25f, 0.02f), new Vector2(width + 0.06f, height + 0.06f), new Color(0.03f, 0.08f, 0.2f, 0.96f), P.Gold, 0.035f);

            Line(go.transform, "Title", title.ToUpperInvariant(), new Vector3(-width / 2f + 0.1f, 0.12f * k, -0.02f), 0.085f * k, P.Gold, TextAnchor.MiddleLeft, true, inner, 0f, false);
            Line(go.transform, "Subtitle", subtitle, new Vector3(-width / 2f + 0.1f, 0.0f, -0.02f), 0.05f * k, P.Sky, TextAnchor.MiddleLeft, false, inner, 0f, false);

            board.stepText = steps;
            board.lines.Clear(); board.done.Clear();
            for (int i = 0; i < steps.Length; i++)
            {
                var tm = Line(go.transform, "Step " + (i + 1), $"{i + 1}.  {steps[i]}", new Vector3(-width / 2f + 0.1f, -0.15f * k - i * lineHeight, -0.02f), 0.058f * k, Pending, TextAnchor.MiddleLeft, false, inner, 0f, false);
                board.lines.Add(tm);
                board.done.Add(false);
            }
            // Below the steps: status (two lines at most), footer (three lines at most), result line (two lines at most), each top-anchored in its own band
            float y = -0.15f * k - steps.Length * lineHeight - 0.1f * k;
            board.status = Line(go.transform, "Status", "", new Vector3(-width / 2f + 0.1f, y + 0.05f * k, -0.02f), 0.058f * k, Color.white, TextAnchor.UpperLeft, true, inner, 0.15f * k, true);
            board.footer = Line(go.transform, "Footer", "", new Vector3(-width / 2f + 0.1f, y - 0.11f * k, -0.02f), FooterSize * k, P.Sky, TextAnchor.UpperLeft, false, inner, 0.135f * k, true);
            board.resultLine = Line(go.transform, "Result", "", new Vector3(-width / 2f + 0.1f, y - 0.25f * k, -0.02f), 0.048f * k, P.Sky, TextAnchor.UpperLeft, true, inner, 0.12f * k, true);
            board.SetCurrent(0);
            return board;
        }

        /// <summary>One text of the board: adopted from the scene (or created while baking), then set and limited to its box.</summary>
        static TextMesh Line(Transform parent, string name, string text, Vector3 pos, float height, Color color, TextAnchor anchor, bool bold, float maxWidth, float maxHeight, bool wrap)
        {
            var tm = P.Text(parent, text, pos, height, color, anchor, bold, name);
            tm.text = text;
            tm.color = color;
            TextFit.Add(tm, maxWidth, maxHeight, wrap);
            return tm;
        }

        string[] stepText;

        /// <summary>Puts the board back to its start (no step done, no mistakes, empty status, footer and result) so a task can be played again on the same saved board.</summary>
        public void ResetProgress()
        {
            Mistakes = 0; CriticalMisses = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                done[i] = false;
                if (stepText != null && i < stepText.Length) lines[i].text = $"{i + 1}.  {stepText[i]}";
            }
            status.text = ""; footer.text = ""; resultLine.text = "";
            SetCurrent(0);
        }

        public void SetCurrent(int index)
        {
            Current = index;
            for (int i = 0; i < lines.Count; i++)
                lines[i].color = done[i] ? Done : i == index ? Active : Pending;
        }

        public void Complete(int index, string message = null)
        {
            if (index < 0 || index >= lines.Count || done[index]) return;
            done[index] = true;
            Sfx.Play2D("ui_confirm", 0.6f);
            lines[index].text = lines[index].text.Replace($"{index + 1}.  ", "OK  ");
            if (message != null) Say(message, Done);
            int next = index + 1;
            while (next < lines.Count && done[next]) next++;
            SetCurrent(next);
        }

        public bool IsDone(int index) => index >= 0 && index < done.Count && done[index];
        public bool AllDone { get { foreach (var d in done) if (!d) return false; return true; } }

        public void Mistake(string message, bool critical = false)
        {
            Mistakes++;
            if (critical) CriticalMisses++;
            Sfx.Play2D(critical ? "error_buzz" : "ui_error", critical ? 0.7f : 0.6f);
            Say((critical ? "CRITICAL: " : "Mistake: ") + message, critical ? new Color(1f, 0.38f, 0.38f) : new Color(1f, 0.72f, 0.3f));
            UpdateFooter();
        }

        public void Say(string message, Color? color = null)
        {
            status.text = message;
            status.color = color ?? Color.white;
        }

        /// <summary>The footer is small text: its <see cref="TextFit"/> breaks a long hint at spaces (measured with the font) so it stays inside the panel.</summary>
        public void SetFooter(string text) { footer.text = text; }

        /// <summary>
        /// A line under the footer for a result kept apart from the procedure score (for example the
        /// "Reasoning" result of a <see cref="WhyCue"/> question). The text object is part of the board in the scene.
        /// </summary>
        public void SetResultLine(string text, Color? color = null)
        {
            resultLine.text = text;
            resultLine.color = color ?? P.Sky;
        }

        void UpdateFooter()
        {
            footer.text = $"Mistakes: {Mistakes}   Critical: {CriticalMisses}";
        }

        public string Summary()
        {
            int points = CriticalMisses > 0 ? 0 : Mistakes == 0 ? 2 : Mistakes == 1 ? 1 : 0;
            return points == 2 ? "STRONG PRACTICE: no mistakes." : CriticalMisses > 0 ? $"CRITICAL SAFETY MISS ({CriticalMisses}). Review before real work." : $"Complete with {Mistakes} mistake{(Mistakes == 1 ? "" : "s")}.";
        }
    }
}
