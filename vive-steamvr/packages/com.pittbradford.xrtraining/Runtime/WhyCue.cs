using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PittBradford.XR
{
    /// <summary>
    /// One multiple-choice rationale ("why") question: three options, one best answer, and a line of feedback for each
    /// option that explains why it is or is not the best answer.
    /// </summary>
    public class RationaleQuestion
    {
        /// <summary>Short name for the summary, for example "why the shell must be full of water".</summary>
        public string Topic;
        public string Prompt;
        public string[] Options;
        public string[] Feedback;
        public int Best;
    }

    /// <summary>
    /// "Why this matters" cues for a training scenario, the same in every scenario that uses them:
    /// <list type="bullet">
    /// <item><see cref="Show"/>: after the learner completes a step, a styled panel with the heading WHY THIS MATTERS and
    /// a one-sentence reason appears for a few seconds, a little below the line of sight, and follows the view lazily.</item>
    /// <item><see cref="Ask"/>: after the main tasks, one multiple-choice rationale question on a styled panel. Answer
    /// by pointing and pulling the trigger (VR) or by clicking / pressing 1, 2, 3 (desktop). Feedback then explains every
    /// option, and <see cref="ReasoningLine"/> gives a "Reasoning" result that scenarios show apart from the procedure
    /// score. It is practice feedback, never a pass/fail certification.</item>
    /// </list>
    /// Panels are built with <see cref="Ui.Panel"/>; no bare floating text. While the screenshot driver runs
    /// (<see cref="TagFacing.ViewOverride"/> set) the panels compose themselves in front of the capture camera.
    /// </summary>
    public class WhyCue : MonoBehaviour
    {
        public const string Heading = "WHY THIS MATTERS";

        /// <summary>When true, <see cref="Show"/> only logs (used while a scenario stages screenshots).</summary>
        public bool Muted;
        /// <summary>How long a reason stays up.</summary>
        public float ShowSeconds = 7f;
        /// <summary>A pending question opens after the current reason has been up this long.</summary>
        public float AskDelay = 4.5f;
        /// <summary>Distance of the reason panel from the eye, and its offset (right, up) in metres.</summary>
        public float WhyDistance = 1.6f;
        public Vector2 WhyOffset = new Vector2(0f, -0.3f);
        /// <summary>Distance of the question panel from the eye, and its offset (right, up) in metres.</summary>
        public float AskDistance = 1.7f;
        public Vector2 AskOffset = new Vector2(0f, -0.08f);
        /// <summary>Scale of the question panel (its layout is in metres at scale 1).</summary>
        public float AskScale = 0.85f;

        public RationaleQuestion Question { get; private set; }
        public bool Asking { get; private set; }
        public bool Answered { get; private set; }
        public bool AnsweredBest { get; private set; }
        public int Chosen { get; private set; } = -1;
        public bool ReasonVisible => why != null && why.gameObject.activeSelf;
        /// <summary>Raised once when the learner answers the question.</summary>
        public event Action<WhyCue> AnsweredQuestion;

        static readonly Color Fill = new Color(0.03f, 0.08f, 0.2f, 0.96f);
        static readonly Color RowFill = new Color(0.08f, 0.2f, 0.45f, 0.98f);
        static readonly Color BestFill = new Color(0.04f, 0.36f, 0.2f, 0.98f);
        static readonly Color OtherFill = new Color(0.42f, 0.27f, 0.04f, 0.98f);
        static readonly Color Good = new Color(0.3f, 0.9f, 0.62f);
        static readonly Color Warn = new Color(1f, 0.72f, 0.3f);

        // reason panel
        const float WhyW = 1.12f, WhyH = 0.4f;
        Transform why;
        TextMesh whyBody;
        float whyShownAt, whyUntil;
        bool whyMoving;

        // question panel
        Transform ask;
        readonly List<Pressable> rowPress = new List<Pressable>();
        readonly List<GameObject[]> rowFaces = new List<GameObject[]>();
        readonly List<TextMesh> rowFeedback = new List<TextMesh>();
        TextMesh result, resultSub, hint;
        bool askMoving;
        RationaleQuestion pending;
        Action<bool> pendingDone, onDone;

        readonly Dictionary<TextMesh, float> baseSize = new Dictionary<TextMesh, float>();
        struct FitItem { public TextMesh Text; public float MaxWidth; public int Frame; }
        readonly List<FitItem> toFit = new List<FitItem>();

        /// <summary>Creates the cue object (panels are built hidden).</summary>
        public static WhyCue Create(Transform parent = null)
        {
            var go = new GameObject("Why cue");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<WhyCue>();
        }

        void Awake()
        {
            BuildWhyPanel();
        }

        // ------------------------------------------------------------------------------------------- reasons

        void BuildWhyPanel()
        {
            why = new GameObject("Why this matters").transform;
            why.SetParent(transform, false);
            Ui.Panel(why, "Panel", Vector3.zero, new Vector2(WhyW, WhyH), Fill, P.Gold, 0.08f);
            float left = -WhyW / 2f + 0.07f;
            P.Text(why, Heading, new Vector3(left, WhyH / 2f - 0.085f, -0.012f), 0.058f, P.Gold, TextAnchor.MiddleLeft, true);
            whyBody = P.Text(why, "", new Vector3(left, WhyH / 2f - 0.14f, -0.012f), 0.047f, Color.white, TextAnchor.UpperLeft);
            why.gameObject.SetActive(false);
        }

        /// <summary>Shows a one-sentence reason under the heading WHY THIS MATTERS for <see cref="ShowSeconds"/>.</summary>
        public void Show(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return;
            Debug.Log("[why] " + (Muted ? "(muted) " : "") + reason);
            if (Muted) return;
            SetText(whyBody, Wrap(reason, 42), WhyW - 0.14f);
            bool wasVisible = why.gameObject.activeSelf;
            why.gameObject.SetActive(true);
            whyShownAt = Time.time;
            whyUntil = Time.time + ShowSeconds;
            if (!wasVisible) PlaceNow(why, WhyDistance, WhyOffset, false);
        }

        public void HideReason() { if (why) why.gameObject.SetActive(false); }

        // ------------------------------------------------------------------------------------------- question

        /// <summary>
        /// Opens the rationale question (after the current reason, if one is showing). <paramref name="onAnswered"/>
        /// receives true when the best answer was chosen.
        /// </summary>
        public void Ask(RationaleQuestion q, Action<bool> onAnswered = null)
        {
            if (q == null || q.Options == null || q.Options.Length == 0) return;
            pending = q;
            pendingDone = onAnswered;
            if (!ReasonVisible) OpenPending();
        }

        void OpenPending()
        {
            var q = pending;
            onDone = pendingDone;
            pending = null;
            pendingDone = null;
            HideReason();
            Question = q;
            Asking = true;
            Answered = false;
            AnsweredBest = false;
            Chosen = -1;
            BuildQuestion(q);
            PlaceNow(ask, AskDistance, AskOffset, true);
            Debug.Log("[why] question: " + q.Prompt);
        }

        void BuildQuestion(RationaleQuestion q)
        {
            if (ask != null) Destroy(ask.gameObject);
            rowPress.Clear(); rowFaces.Clear(); rowFeedback.Clear();
            ask = new GameObject("Rationale question").transform;
            ask.SetParent(transform, false);
            ask.localScale = Vector3.one * AskScale;

            const float W = 1.56f, pad = 0.08f;
            float inner = W - 2f * pad;
            string prompt = Wrap(q.Prompt, 54);
            int promptLines = Lines(prompt);
            var opts = new string[q.Options.Length];
            var optLines = new int[q.Options.Length];
            int fbLines = 1;
            for (int i = 0; i < q.Options.Length; i++)
            {
                opts[i] = Wrap(q.Options[i], 50);
                optLines[i] = Lines(opts[i]);
                string fb = FeedbackText(q, i, i); // longest prefix this row can get
                fbLines = Mathf.Max(fbLines, Lines(Wrap(fb, FeedbackChars)));
            }
            const float headBlock = 0.2f, promptLine = 0.074f, optLine = 0.064f, fbLine = 0.054f;
            float rowsH = 0f;
            for (int i = 0; i < opts.Length; i++) rowsH += 0.085f + optLines[i] * optLine + fbLines * fbLine + 0.05f;
            float H = 0.06f + headBlock + promptLines * promptLine + 0.05f + rowsH + 0.2f;

            Ui.Panel(ask, "Panel", Vector3.zero, new Vector2(W, H), Fill, P.Gold, 0.045f);
            float left = -W / 2f + pad;
            float y = H / 2f - 0.11f;
            P.Text(ask, Heading + ": ONE QUESTION", new Vector3(left, y, -0.012f), 0.06f, P.Gold, TextAnchor.MiddleLeft, true);
            y -= 0.075f;
            hint = P.Text(ask, "Point and pull the trigger to choose.  Desktop: click, or press 1, 2 or 3.", new Vector3(left, y, -0.012f), 0.036f, P.Sky, TextAnchor.MiddleLeft);
            Fit(hint, inner);
            y -= 0.06f;
            var pt = P.Text(ask, prompt, new Vector3(left, y, -0.012f), 0.054f, Color.white, TextAnchor.UpperLeft, true);
            Fit(pt, inner);
            y -= promptLines * promptLine + 0.05f;

            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < opts.Length; i++)
            {
                float rowH = 0.06f + optLines[i] * optLine;
                var row = new GameObject("Option " + letters[Mathf.Min(i, 3)]).transform;
                row.SetParent(ask, false);
                row.localPosition = new Vector3(0f, y - rowH / 2f, -0.006f);
                var faces = new GameObject[3];
                faces[0] = Ui.Panel(row, "Face", Vector3.zero, new Vector2(inner, rowH), RowFill, null).gameObject;
                faces[1] = Ui.Panel(row, "Face best", Vector3.zero, new Vector2(inner, rowH), BestFill, null).gameObject;
                faces[2] = Ui.Panel(row, "Face other", Vector3.zero, new Vector2(inner, rowH), OtherFill, null).gameObject;
                faces[1].SetActive(false); faces[2].SetActive(false);
                rowFaces.Add(faces);
                P.Text(row, letters[Mathf.Min(i, 3)], new Vector3(-inner / 2f + 0.06f, 0f, -0.012f), 0.06f, P.Gold, TextAnchor.MiddleCenter, true);
                var ot = P.Text(row, opts[i], new Vector3(-inner / 2f + 0.13f, (optLines[i] - 1) * optLine / 2f, -0.012f), 0.05f, Color.white, TextAnchor.MiddleLeft);
                Fit(ot, inner - 0.18f);
                var col = row.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(inner, rowH, 0.03f);
                var press = row.gameObject.AddComponent<Pressable>();
                press.Label = "Option " + letters[Mathf.Min(i, 3)];
                int idx = i;
                press.Pressed += h => Answer(idx);
                rowPress.Add(press);
                y -= rowH + 0.025f;
                var fb = P.Text(ask, "", new Vector3(left + 0.02f, y, -0.012f), 0.04f, P.Sky, TextAnchor.UpperLeft);
                rowFeedback.Add(fb);
                y -= fbLines * fbLine + 0.05f;
            }
            y -= 0.02f;
            result = P.Text(ask, "", new Vector3(left, y, -0.012f), 0.052f, Color.white, TextAnchor.MiddleLeft, true);
            resultSub = P.Text(ask, "", new Vector3(left, y - 0.065f, -0.012f), 0.036f, P.Sky, TextAnchor.MiddleLeft);
        }

        const int FeedbackChars = 70;

        static string FeedbackText(RationaleQuestion q, int i, int chosen)
        {
            string fb = q.Feedback != null && i < q.Feedback.Length ? q.Feedback[i] : "";
            string tag = i == q.Best ? (i == chosen ? "YOUR ANSWER (BEST): " : "BEST ANSWER: ") : i == chosen ? "YOUR ANSWER: " : "";
            return tag + fb;
        }

        /// <summary>Chooses option <paramref name="index"/> (called by the panel; public so scripted runs can answer).</summary>
        public void Answer(int index)
        {
            if (!Asking || Answered || Question == null || index < 0 || index >= Question.Options.Length) return;
            Answered = true;
            Chosen = index;
            AnsweredBest = index == Question.Best;
            for (int i = 0; i < rowPress.Count; i++)
            {
                rowPress[i].SetHover(false);
                rowPress[i].Interactable = false;
                bool best = i == Question.Best, mine = i == index;
                rowFaces[i][0].SetActive(!best && !mine);
                rowFaces[i][1].SetActive(best);
                rowFaces[i][2].SetActive(mine && !best);
                SetText(rowFeedback[i], Wrap(FeedbackText(Question, i, index), FeedbackChars), 1.38f);
                rowFeedback[i].color = best ? Good : mine ? Warn : P.Sky;
            }
            result.text = AnsweredBest ? "Reasoning: best answer chosen." : "Reasoning: not the best answer this time. See the feedback above.";
            result.color = AnsweredBest ? Good : Warn;
            Fit(result, 1.4f);
            resultSub.text = "Practice feedback, recorded apart from the procedure score. Not a certification.";
            Fit(resultSub, 1.4f);
            if (hint) hint.text = "Answer recorded.";
            Debug.Log($"[why] answered {"ABCD"[Mathf.Min(index, 3)]} ({(AnsweredBest ? "best answer" : "not the best answer")}) for: {Question.Topic}");
            onDone?.Invoke(AnsweredBest);
            AnsweredQuestion?.Invoke(this);
        }

        /// <summary>The separate "Reasoning" result for an end-of-scenario summary.</summary>
        public string ReasoningLine
        {
            get
            {
                if (Question == null && pending == null) return "Reasoning: no question asked.";
                if (!Answered) return "Reasoning: answer the question panel (scored apart from the procedure).";
                return AnsweredBest
                    ? $"Reasoning: best answer ({Question.Topic}). Scored apart from the procedure."
                    : $"Reasoning: review {Question.Topic}. Scored apart from the procedure.";
            }
        }

        public Color ReasoningColor => !Answered ? P.Sky : AnsweredBest ? Good : Warn;

        public void HideQuestion() { if (ask) ask.gameObject.SetActive(false); }

        public void HideAll() { HideReason(); HideQuestion(); }

        // ------------------------------------------------------------------------------------------- per frame

        void Update()
        {
            if (why.gameObject.activeSelf && Time.time >= whyUntil) why.gameObject.SetActive(false);
            if (pending != null && (!ReasonVisible || Time.time - whyShownAt >= AskDelay)) OpenPending();

            if (Asking && !Answered)
            {
                var kb = Keyboard.current;
                if (kb != null && !XRRig.Headset)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Answer(0);
                    else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Answer(1);
                    else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Answer(2);
                }
            }
        }

        void LateUpdate()
        {
            FitPending();
            var view = View;
            if (view == null) return;
            bool capture = TagFacing.ViewOverride != null;
            if (why.gameObject.activeSelf) Follow(why, WhyDistance, WhyOffset, capture, ref whyMoving, 22f, 0.5f);
            if (ask != null && ask.gameObject.activeSelf) Follow(ask, AskDistance, AskOffset, capture, ref askMoving, 65f, 1.8f);
        }

        static Transform View => TagFacing.ViewOverride != null ? TagFacing.ViewOverride : Camera.main != null ? Camera.main.transform : null;

        /// <summary>Where a panel goes: in front of the eye along the view (pitch limited), shifted by the offset.</summary>
        Vector3 Target(Transform view, float distance, Vector2 offset, bool avoidWalls)
        {
            Vector3 fwd = view.forward;
            Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.ProjectOnPlane(view.up, Vector3.up);
            flat.Normalize();
            float pitch = Mathf.Clamp(Vector3.SignedAngle(flat, fwd, Vector3.Cross(Vector3.up, flat)), -35f, 35f);
            Vector3 dir = Quaternion.AngleAxis(pitch, Vector3.Cross(Vector3.up, flat)) * flat;
            Vector3 right = Vector3.Cross(Vector3.up, flat);
            if (avoidWalls)
            {
                // keep the panel in front of nearby geometry so its text is not hidden inside a wall or a vessel
                float best = distance;
                foreach (var hit in Physics.RaycastAll(view.position, dir, distance + 0.3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(transform)) continue;
                    best = Mathf.Min(best, hit.distance - 0.25f);
                }
                distance = Mathf.Max(0.9f, best);
            }
            return view.position + dir * distance + right * offset.x + Vector3.up * offset.y;
        }

        void PlaceNow(Transform panel, float distance, Vector2 offset, bool avoidWalls)
        {
            var view = View;
            if (view == null) return;
            panel.position = Target(view, distance, offset, avoidWalls);
            Face(panel, view.position);
        }

        void Follow(Transform panel, float distance, Vector2 offset, bool snap, ref bool moving, float maxAngle, float maxShift)
        {
            var view = View;
            if (snap)
            {
                panel.position = Target(view, distance, offset, false);
                Face(panel, view.position);
                return;
            }
            Vector3 target = Target(view, distance, offset, panel == ask);
            Vector3 a = panel.position - view.position, b = target - view.position;
            if (Vector3.Angle(a, b) > maxAngle || Mathf.Abs(a.magnitude - b.magnitude) > maxShift) moving = true;
            if (moving)
            {
                panel.position = Vector3.Lerp(panel.position, target, 1f - Mathf.Exp(-4f * Time.deltaTime));
                if (Vector3.Distance(panel.position, target) < 0.03f) moving = false;
            }
            Face(panel, view.position);
        }

        static void Face(Transform panel, Vector3 eye)
        {
            Vector3 away = panel.position - eye;
            if (away.sqrMagnitude > 0.0001f) panel.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        // ------------------------------------------------------------------------------------------- text helpers

        /// <summary>Breaks <paramref name="s"/> into lines of at most <paramref name="maxChars"/> characters at spaces.</summary>
        public static string Wrap(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            int lineLen = 0;
            foreach (var word in s.Split(' '))
            {
                if (word.Length == 0) continue;
                if (lineLen > 0 && lineLen + 1 + word.Length > maxChars) { sb.Append('\n'); lineLen = 0; }
                else if (lineLen > 0) { sb.Append(' '); lineLen++; }
                sb.Append(word);
                lineLen += word.Length;
            }
            return sb.ToString();
        }

        static int Lines(string s) { int n = 1; foreach (char c in s) if (c == '\n') n++; return n; }

        void SetText(TextMesh tm, string s, float maxWidth)
        {
            if (baseSize.TryGetValue(tm, out var size)) tm.characterSize = size;
            tm.text = s;
            Fit(tm, maxWidth);
        }

        /// <summary>Shrinks the text next frame if it came out wider than <paramref name="maxWidth"/> (fonts vary by machine).</summary>
        void Fit(TextMesh tm, float maxWidth)
        {
            if (!baseSize.ContainsKey(tm)) baseSize[tm] = tm.characterSize;
            toFit.Add(new FitItem { Text = tm, MaxWidth = maxWidth, Frame = Time.frameCount });
        }

        void FitPending()
        {
            for (int i = toFit.Count - 1; i >= 0; i--)
            {
                var item = toFit[i];
                if (item.Text == null) { toFit.RemoveAt(i); continue; }
                if (Time.frameCount <= item.Frame) continue; // the text mesh is rebuilt by the next frame
                float w = item.Text.GetComponent<MeshRenderer>().localBounds.size.x;
                if (w > item.MaxWidth && w > 0f) item.Text.characterSize *= item.MaxWidth / w;
                toFit.RemoveAt(i);
            }
        }
    }
}
