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
        TextMesh status, footer;
        Coroutine fade;

        static readonly Color Pending = new Color(0.62f, 0.7f, 0.82f);
        static readonly Color Active = Color.white;
        static readonly Color Done = new Color(0.3f, 0.9f, 0.62f);

        public static TaskBoard Create(Transform parent, Vector3 position, Quaternion rotation, string title, string subtitle, string[] steps, float width = 1.9f)
        {
            var go = new GameObject("Task Board");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var board = go.AddComponent<TaskBoard>();

            float lineHeight = 0.095f;
            float height = 0.7f + steps.Length * lineHeight + 0.35f;
            var frame = P.Box(go.transform, "Frame", new Vector3(0, -height / 2f + 0.25f, 0.02f), new Vector3(width + 0.08f, height + 0.08f, 0.03f), P.Mat(P.Gold, 0.3f, 0.5f), false);
            P.Box(go.transform, "Panel", new Vector3(0, -height / 2f + 0.25f, 0.0f), new Vector3(width, height, 0.03f), P.Mat(new Color(0.03f, 0.08f, 0.2f), 0f, 0.3f), false);

            P.Text(go.transform, title.ToUpperInvariant(), new Vector3(-width / 2f + 0.1f, 0.12f, -0.02f), 0.085f, P.Gold, TextAnchor.MiddleLeft, true);
            P.Text(go.transform, subtitle, new Vector3(-width / 2f + 0.1f, 0.0f, -0.02f), 0.05f, P.Sky, TextAnchor.MiddleLeft);

            for (int i = 0; i < steps.Length; i++)
            {
                var tm = P.Text(go.transform, $"{i + 1}.  {steps[i]}", new Vector3(-width / 2f + 0.1f, -0.15f - i * lineHeight, -0.02f), 0.058f, Pending);
                board.lines.Add(tm);
                board.done.Add(false);
            }
            float y = -0.15f - steps.Length * lineHeight - 0.1f;
            board.status = P.Text(go.transform, "", new Vector3(-width / 2f + 0.1f, y, -0.02f), 0.058f, Color.white, TextAnchor.MiddleLeft, true);
            board.footer = P.Text(go.transform, "", new Vector3(-width / 2f + 0.1f, y - 0.13f, -0.02f), 0.048f, P.Sky);
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
            Say((critical ? "CRITICAL: " : "Mistake: ") + message, critical ? new Color(1f, 0.38f, 0.38f) : new Color(1f, 0.72f, 0.3f));
            UpdateFooter();
        }

        public void Say(string message, Color? color = null)
        {
            status.text = message;
            status.color = color ?? Color.white;
        }

        public void SetFooter(string text) { footer.text = text; }

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
