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

        static readonly Color Pending = new Color(0.62f, 0.7f, 0.82f);
        static readonly Color Active = Color.white;
        static readonly Color Done = new Color(0.3f, 0.9f, 0.62f);

        public static TaskBoard Create(Transform parent, Vector3 position, Quaternion rotation, string title, string subtitle, string[] steps, float width = 1.9f, float textScale = 1f)
        {
            var go = new GameObject("Task Board");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var board = go.AddComponent<TaskBoard>();
            board.textScale = textScale;

            float k = textScale;
            float lineHeight = 0.095f * k;
            float height = (0.7f + 0.35f) * k + steps.Length * lineHeight;
            Ui.Panel(go.transform, "Panel", new Vector3(0, -height / 2f + 0.25f, 0.02f), new Vector2(width + 0.06f, height + 0.06f), new Color(0.03f, 0.08f, 0.2f, 0.96f), P.Gold, 0.035f);

            P.Text(go.transform, title.ToUpperInvariant(), new Vector3(-width / 2f + 0.1f, 0.12f * k, -0.02f), 0.085f * k, P.Gold, TextAnchor.MiddleLeft, true);
            P.Text(go.transform, subtitle, new Vector3(-width / 2f + 0.1f, 0.0f, -0.02f), 0.05f * k, P.Sky, TextAnchor.MiddleLeft);

            for (int i = 0; i < steps.Length; i++)
            {
                var tm = P.Text(go.transform, $"{i + 1}.  {steps[i]}", new Vector3(-width / 2f + 0.1f, -0.15f * k - i * lineHeight, -0.02f), 0.058f * k, Pending);
                board.lines.Add(tm);
                board.done.Add(false);
            }
            float y = -0.15f * k - steps.Length * lineHeight - 0.1f * k;
            board.status = P.Text(go.transform, "", new Vector3(-width / 2f + 0.1f, y, -0.02f), 0.058f * k, Color.white, TextAnchor.MiddleLeft, true);
            board.footer = P.Text(go.transform, "", new Vector3(-width / 2f + 0.1f, y - 0.13f * k, -0.02f), 0.048f * k, P.Sky);
            board.SetCurrent(0);
            return board;
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

        public void SetFooter(string text) { footer.text = text; }

        /// <summary>
        /// An extra line under the footer for a result kept apart from the procedure score (for example the
        /// "Reasoning" result of a <see cref="WhyCue"/> question). Created on first use.
        /// </summary>
        public void SetResultLine(string text, Color? color = null)
        {
            if (resultLine == null)
                resultLine = P.Text(transform, "", footer.transform.localPosition + new Vector3(0f, -0.11f * textScale, 0f), 0.048f * textScale, P.Sky, TextAnchor.MiddleLeft, true);
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
