using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Optional co-worker hazard and SAGAT-style awareness check for the warehouse scenario.
///
/// After the learner has crossed into the racking area, the forklift stops in the west lane to put a pallet away on
/// the wall racking. While it is working, a co-worker in a hi-vis vest comes out of the staff door and walks close past
/// the forklift. A few seconds after the co-worker appears, the scene freezes (physics, forklift and walking stop;
/// the view is blanked behind a panel) and asks three multiple-choice questions about where things were at that moment.
/// Answers are scored against the true simulated positions. The scene then resumes, and the learner is expected to
/// stop, watch the co-worker, and wait until they are clear. Both results appear in the end-of-scenario summary as a
/// separate "Awareness check". General safety-awareness practice only: not a test, a certification, or a site procedure.
///
/// The freeze-and-ask method follows the SAGAT queries used by Choi, Ahn and Seo (2020) in a VR forklift study.
/// </summary>
public partial class WarehouseForklift
{
    [Header("Co-worker hazard and awareness check (optional)")]
    [Tooltip("A co-worker walks past the forklift while it puts a pallet away. Off = the original scenario.")]
    public bool CoworkerHazard = true;
    [Tooltip("Freeze the scene shortly after the co-worker appears and ask where things are (needs Coworker Hazard).")]
    public bool AwarenessCheck = true;
    [Tooltip("Seconds after the learner has crossed before the forklift is sent to put the pallet away.")]
    public float EventDelay = 8f;
    [Tooltip("Seconds after the co-worker appears before the scene freezes for the questions.")]
    public float CheckDelay = 4.5f;

    enum Ev { Off, Idle, Armed, Active, Done }

    class Question
    {
        public string Text;
        public string[] Options;
        public bool[] Correct;
        public string Truth;
        /// <summary>The answer the simulation gives with no tolerance (shown in the summary).</summary>
        public int Primary;
        public int Chosen = -1;
        public bool Right => Chosen >= 0 && Correct[Chosen];
        public int FirstWrong { get { for (int i = 0; i < Correct.Length; i++) if (!Correct[i]) return i; return 0; } }
    }

    // the west-wall racking the forklift puts the pallet on, and the co-worker's route
    static readonly Vector3 StaffDoor = new Vector3(-7.0f, 0f, 15.95f);
    static readonly Vector3[] WalkerRoute =
    {
        new Vector3(-7.0f, 0f, 15.3f),  // out of the staff door
        new Vector3(-8.0f, 0f, 11.0f),  // across the north lane
        new Vector3(-8.0f, 0f, 7.6f),   // down beside the west lane, close behind the working forklift
        new Vector3(-7.8f, 0f, 4.6f),
        new Vector3(-7.0f, 0f, 3.6f),   // stops near the cone station
    };
    static readonly Vector3 LoadingSignPos = new Vector3(-5.6f, 0f, 4.3f);

    Ev ev = Ev.Off;
    CoWorker coworker;
    Vector3 loadPoint;
    Vector3? shelf;
    float eligibleAt = -1f, evT, gazeT, watchT, maxDisp;
    bool checkTaken, measuring, enteredLane, held, watched, behaviourDone;
    Vector3 anchor;
    string behaviourText = "";

    // pause state
    bool paused;
    float resumeAt = -1f, savedMove, savedSnap;
    readonly List<Question> questions = new List<Question>();
    readonly List<GameObject> optionObjs = new List<GameObject>();
    readonly List<Pressable> optionButtons = new List<Pressable>();
    int qIndex;
    GameObject pauseRoot, dimSphere, promptRoot, summaryRoot;
    TextMesh qCounter, qText, qHint;
    float promptUntil = -1f;
    string truthLog = "";

    static readonly Color Good = new Color(0.3f, 0.9f, 0.62f);
    static readonly Color Warn = new Color(1f, 0.72f, 0.3f);

    // ------------------------------------------------------------------------------------------------- build

    // Static scenery of the event (may be baked into the scene): the staff door and its sign, the loading-zone paint and
    // the stand. The co-worker, the questions, prompts and summary are runtime events and are never baked.
    static readonly string[] CoworkerScenery = { "Co-worker event", "Loading zone markings", "Loading zone sign" };

    void BuildCoworkerEvent()
    {
        if (!CoworkerHazard)
        {
            // Hazard off: build nothing. A scene baked with it on still holds the door, paint and stand, so hide them;
            // the rack cartons are always baked and simply stay on the shelf. No re-bake is needed after toggling.
            foreach (var n in CoworkerScenery)
            {
                var baked = transform.Find(n);
                if (baked != null && Bake.WasLoaded(baked)) baked.gameObject.SetActive(false);
            }
            return;
        }
        ev = Ev.Idle;
        var root = NewRoot("Co-worker event");

        // staff door in the north wall, west end
        var doorMat = P.Mat(new Color(0.32f, 0.36f, 0.42f), 0.3f, 0.4f);
        P.Box(root, "Staff door", StaffDoor + new Vector3(0, 1.1f, 0), new Vector3(1.2f, 2.2f, 0.05f), doorMat, false);
        P.Box(root, "Door frame L", StaffDoor + new Vector3(-0.66f, 1.15f, -0.02f), new Vector3(0.12f, 2.3f, 0.08f), hazard, false);
        P.Box(root, "Door frame R", StaffDoor + new Vector3(0.66f, 1.15f, -0.02f), new Vector3(0.12f, 2.3f, 0.08f), hazard, false);
        P.Box(root, "Door frame top", StaffDoor + new Vector3(0, 2.32f, -0.02f), new Vector3(1.44f, 0.12f, 0.08f), hazard, false);
        P.Box(root, "Door push bar", StaffDoor + new Vector3(0, 1.0f, -0.05f), new Vector3(0.9f, 0.05f, 0.05f), steel, false);
        var staff = Sign(root, "Staff door sign", StaffDoor + new Vector3(0, 2.75f, -0.03f), Vector3.back, new Vector2(1.5f, 0.36f), Navy, P.Gold, 0.1f);
        SignText(staff, "STAFF ENTRANCE", -0.01f, 0.14f, P.Gold);

        // the forklift puts its pallet on the middle shelf of the west-wall bay at z 4.5..6.5; clear the cartons the
        // racking builder put there so the pallet has room. This runs at every Play, on built or baked cartons alike;
        // it is skipped while baking so the scene file keeps every carton (and the toggle can be turned off later).
        Physics.SyncTransforms();
        loadPoint = new Vector3(-10f, 0f, 5.5f);
        shelf = new Vector3(-13.0f, 1.64f, loadPoint.z);
        if (!BakeSession)
            foreach (var c in Physics.OverlapBox(new Vector3(-13f, 2.15f, loadPoint.z), new Vector3(0.4f, 0.45f, 0.9f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (c.name == "Carton") Destroy(c.gameObject);

        // painted loading zone on the west lane in front of that bay
        var marks = NewRoot("Loading zone markings");
        float lz = loadPoint.z;
        Paint(marks, "Loading zone edge N", -11.6f, lz + 1.45f, -8.4f, lz + 1.6f, hazard, 0.011f);
        Paint(marks, "Loading zone edge S", -11.6f, lz - 1.6f, -8.4f, lz - 1.45f, hazard, 0.011f);
        FloorText(marks, "LOADING ZONE", new Vector3(-9.0f, 0.018f, lz), 0.22f, SafetyYellow, -90f); // reads from the walkway (east)

        // the safe-behaviour expectation, on a stand beside the walkway, facing the learner
        var stand = Stand("Loading zone sign", LoadingSignPos, new Vector3(-3.5f, 1.4f, 4.6f), 1.15f, 0.72f, 0.75f);
        P.Text(stand, "FORKLIFT LOADING?", new Vector3(0, 1.36f, -0.015f), 0.068f, P.Gold, TextAnchor.MiddleCenter, true);
        P.Text(stand, "1  STOP where you are\n2  WATCH people near it\n3  WAIT until they are clear", new Vector3(-0.5f, 1.27f, -0.015f), 0.046f, P.Sky, TextAnchor.UpperLeft);
        StartCoroutine(FitText(stand, 1.05f));

        coworker = CoWorker.Build(transform, WalkerRoute);
        forklift.Walker = coworker;
        forklift.LoadingStarted += OnLoadingStarted;
    }

    // ------------------------------------------------------------------------------------------------- event flow

    void UpdateCoworkerEvent(Zone zone, Vector3 feet, float dt)
    {
        if (ev == Ev.Off || ev == Ev.Done) { UpdatePrompt(); return; }

        if (ev == Ev.Idle)
        {
            if (!board.IsDone(2) || finished) return;
            if (eligibleAt < 0f) eligibleAt = Time.time + EventDelay;
            if (Time.time >= eligibleAt)
            {
                forklift.RequestLoad(loadPoint, Vector3.left, shelf);
                ev = Ev.Armed;
                Debug.Log($"[awareness] forklift sent to put a pallet away at z = {loadPoint.z:F1} (shelf {(shelf.HasValue ? "free" : "none: forks only")})");
            }
            return;
        }
        if (ev != Ev.Active) return;

        evT += dt;
        var cam = rig.Camera.transform;

        // did the learner look at the co-worker? (head direction within 15 degrees, line of sight)
        Vector3 to = coworker.ChestPosition - cam.position;
        if (to.magnitude < 25f && Vector3.Angle(cam.forward, to) < 15f && Visible(cam.position, coworker.ChestPosition))
            gazeT += dt;
        if (gazeT >= 0.5f) watched = true;

        if (AwarenessCheck && !checkTaken && evT >= CheckDelay)
        {
            StartCheck();
            return;
        }
        if (!measuring && (checkTaken || (!AwarenessCheck && evT >= 2f)))
        {
            // from here the learner is expected to stop, watch and wait
            measuring = true;
            anchor = feet;
            maxDisp = 0f;
            ShowPrompt("CO-WORKER NEAR A WORKING FORKLIFT", "STOP where you are.  WATCH them.  WAIT until they are clear.", 7f, Warn);
        }
        if (measuring)
        {
            maxDisp = Mathf.Max(maxDisp, FlatDistance(anchor, feet));
            if (zone == Zone.Lane || zone == Zone.Crossing) enteredLane = true;
            if (watched) watchT += dt;
        }
        UpdatePrompt();

        if (coworker.Done && forklift.Phase == Forklift.LoadPhase.None)
        {
            ev = Ev.Done;
            behaviourDone = true;
            held = maxDisp < 0.8f && !enteredLane;
            behaviourText = held && watched ? "Stopped, watched the co-worker, and waited until clear."
                : held ? "Stopped and waited, but did not look at the co-worker."
                : watched ? "Watched the co-worker, but kept moving" + (enteredLane ? " into a forklift lane." : ".")
                : "Kept moving and did not look at the co-worker.";
            ShowPrompt("CO-WORKER CLEAR", behaviourText, 5f, held && watched ? Good : Warn);
            Debug.Log($"[awareness] behaviour: held position = {held} (moved {maxDisp:F2} m, entered lane = {enteredLane}), looked at co-worker = {watched} (gaze {gazeT:F1} s)");
        }
    }

    void OnLoadingStarted()
    {
        if (ev != Ev.Armed) return;
        ev = Ev.Active;
        evT = 0f;
        coworker.Begin();
        Debug.Log("[awareness] forklift stopped to put the pallet away; co-worker appears at the staff door");
    }

    // ------------------------------------------------------------------------------------------------- the check

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    /// <summary>Freezes the scene and builds the three questions from the true positions at this moment.</summary>
    void StartCheck()
    {
        checkTaken = true;
        var cam = rig.Camera.transform;
        Vector3 head = cam.position;
        Vector3 fwd = Flat(cam.forward);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Flat(cam.up);
        fwd.Normalize();
        Vector3 feet = rig.FeetPosition;
        Vector3 cw = coworker.transform.position;
        Vector3 r = Flat(cw - head);
        float dist = r.magnitude;
        r.Normalize();

        // Q1: where was the co-worker, relative to the way you were facing? (15 degree tolerance at each boundary)
        float b = Vector3.SignedAngle(fwd, r, Vector3.up); // + = to the right
        float ab = Mathf.Abs(b);
        const float Tol = 15f;
        var q1 = new Question
        {
            Text = "Where was the co-worker in the hi-vis vest?",
            Options = new[] { "In front of you", "To your left", "To your right", "Behind you" },
            Correct = new[]
            {
                ab <= 45f + Tol,
                b < 0f && ab >= 45f - Tol && ab <= 135f + Tol,
                b > 0f && ab >= 45f - Tol && ab <= 135f + Tol,
                ab >= 135f - Tol,
            },
            Truth = $"{ab:F0} deg {(b >= 0 ? "right" : "left")} of where you faced, {dist:F1} m away",
            Primary = ab <= 45f ? 0 : ab > 135f ? 3 : b < 0f ? 1 : 2,
        };

        // Q2: which way were they walking, as seen from you? Any direction with a component of 0.4 or more counts.
        Vector3 v = Flat(coworker.Velocity);
        Vector3 right = Vector3.Cross(Vector3.up, r); // to the learner's right when looking at the co-worker
        float radial = 0f, lateral = 0f;
        bool moving = v.magnitude > 0.2f;
        if (moving)
        {
            v.Normalize();
            radial = Vector3.Dot(v, r);
            lateral = Vector3.Dot(v, right);
        }
        var q2 = new Question
        {
            Text = "Which way were they walking, as seen from you?",
            Options = new[] { "Toward you", "Away from you", "Across, from your left to your right", "Across, from your right to your left" },
            Correct = moving
                ? new[] { radial <= -0.4f, radial >= 0.4f, lateral >= 0.4f, lateral <= -0.4f }
                : new[] { false, false, false, false },
            Truth = moving ? $"toward/away component {-radial:F2} (toward +), across component {lateral:F2} (left-to-right +)" : "standing still",
            Primary = Mathf.Abs(radial) >= Mathf.Abs(lateral) ? (radial < 0f ? 0 : 1) : (lateral > 0f ? 2 : 3),
        };

        // Q3: how far was the forklift from you? (1 m tolerance at each boundary)
        float fd = FlatDistance(forklift.transform.position, feet);
        var q3 = new Question
        {
            Text = "How far away was the forklift?",
            Options = new[] { "Less than 6 m", "6 to 12 m", "More than 12 m" },
            Correct = new[] { fd <= 7f, fd >= 5f && fd <= 13f, fd >= 11f },
            Truth = $"{fd:F1} m",
            Primary = fd < 6f ? 0 : fd <= 12f ? 1 : 2,
        };

        questions.Clear();
        questions.Add(q1);
        questions.Add(q2);
        questions.Add(q3);
        truthLog = $"co-worker {q1.Truth}; walking {q2.Truth}; forklift {q3.Truth}";
        Debug.Log("[awareness] check starts. Truth: " + truthLog);
        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var ok = new List<string>();
            for (int k = 0; k < q.Options.Length; k++) if (q.Correct[k]) ok.Add(q.Options[k]);
            Debug.Log($"[awareness]   Q{i + 1} simulation answer: {q.Options[q.Primary]}; accepted within tolerance: {string.Join(" | ", ok)}");
        }

        // freeze everything that runs on scaled time: physics, the forklift, the co-worker, held objects
        paused = true;
        resumeAt = -1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        savedMove = rig.MoveSpeed;
        savedSnap = rig.SnapTurnDegrees;
        rig.MoveSpeed = 0f;        // looking around still works (to reach the buttons on a desktop)
        rig.SnapTurnDegrees = 0f;
        flashT = 0f;
        if (flashRoot.activeSelf) flashRoot.SetActive(false);

        BuildPauseUi(head, fwd);
        qIndex = 0;
        ShowQuestion();
    }

    void BuildPauseUi(Vector3 head, Vector3 fwd)
    {
        // A dark shell around the head blanks the scene. It is drawn after all opaque geometry, ignores depth and
        // writes its own, so nothing behind it shows through (not even text); the panel (1.15 m) and its buttons
        // are drawn after it and inside it.
        dimSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dimSphere.name = "Awareness check blank";
        Destroy(dimSphere.GetComponent<Collider>());
        dimSphere.transform.position = head;
        dimSphere.transform.localScale = Vector3.one * 3.2f; // radius 1.6 m
        var blankShader = Shader.Find("Hidden/Internal-Colored");
        Material dm;
        if (blankShader != null)
        {
            dm = new Material(blankShader) { color = new Color(0.01f, 0.03f, 0.09f, 0.95f), renderQueue = 2449 };
            dm.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            dm.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            dm.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            dm.SetInt("_ZWrite", 1);
            dm.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }
        else dm = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.01f, 0.03f, 0.09f, 0.95f), renderQueue = 2501 };
        var dr = dimSphere.GetComponent<MeshRenderer>();
        dr.sharedMaterial = dm;
        dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dr.receiveShadows = false;

        pauseRoot = new GameObject("Awareness check");
        var t = pauseRoot.transform;
        t.SetPositionAndRotation(head + fwd * 1.15f - Vector3.up * 0.1f, Quaternion.LookRotation(fwd, Vector3.up));
        Ui.Panel(t, "Panel", Vector3.zero, new Vector2(1.5f, 1.08f), Navy, P.Gold, 0.05f);
        P.Text(t, "AWARENESS CHECK", new Vector3(-0.68f, 0.44f, -0.015f), 0.075f, P.Gold, TextAnchor.MiddleLeft, true);
        qCounter = P.Text(t, "", new Vector3(0.68f, 0.44f, -0.015f), 0.045f, P.Sky, TextAnchor.MiddleRight, true);
        qHint = P.Text(t, "The scene is paused. Answer from memory.", new Vector3(-0.68f, 0.35f, -0.015f), 0.042f, P.Sky, TextAnchor.MiddleLeft);
        qText = P.Text(t, "", new Vector3(-0.68f, 0.235f, -0.015f), 0.058f, Color.white, TextAnchor.MiddleLeft, true);
        P.Text(t, "General safety-awareness practice. Not a test or a certification.", new Vector3(0, -0.475f, -0.015f), 0.032f, new Color(0.7f, 0.78f, 0.9f), TextAnchor.MiddleCenter);
    }

    void ShowQuestion()
    {
        foreach (var o in optionObjs) if (o) Destroy(o);
        optionObjs.Clear();
        optionButtons.Clear();
        var t = pauseRoot.transform;

        if (qIndex >= questions.Count)
        {
            qCounter.text = "";
            qText.text = "Thank you. The scene will resume.";
            qHint.text = "Your answers are scored in the end-of-scenario summary.";
            resumeAt = Time.unscaledTime + 1.4f;
            return;
        }
        var q = questions[qIndex];
        qCounter.text = $"Question {qIndex + 1} of {questions.Count}";
        qText.text = q.Text;
        qText.characterSize = 0.058f / 160f * 10f; // undo any shrink from the previous question
        StartCoroutine(FitText(qText.transform, 1.36f));
        for (int i = 0; i < q.Options.Length; i++)
        {
            int k = i;
            float y = 0.08f - i * 0.135f;
            var btnMat = P.Glow(new Color(0.12f, 0.36f, 0.82f), 0.9f);
            btnMat.renderQueue = 2460; // after the blanking shell, so the buttons are not darkened
            var btn = P.Box(t, "Answer " + (i + 1), new Vector3(0, y, -0.03f), new Vector3(1.32f, 0.105f, 0.04f), btnMat);
            var p = btn.AddComponent<Pressable>();
            p.Label = q.Options[i];
            p.Pressed += h => Answer(k);
            var label = P.Text(t, q.Options[i], new Vector3(0, y, -0.06f), 0.05f, Color.white, TextAnchor.MiddleCenter, true);
            optionObjs.Add(btn);
            optionObjs.Add(label.gameObject);
            optionButtons.Add(p);
        }
    }

    void Answer(int option)
    {
        if (!paused || qIndex >= questions.Count) return;
        var q = questions[qIndex];
        q.Chosen = option;
        UiSound("ui_select", 0.8f);
        Debug.Log($"[awareness]   Q{qIndex + 1} answered \"{q.Options[option]}\" -> {(q.Right ? "matches" : "does not match")} the simulation ({q.Truth})");
        qIndex++;
        ShowQuestion();
    }

    /// <summary>Runs every frame while paused. Returns true while the scene is frozen.</summary>
    bool UpdateAwarenessPause()
    {
        if (!paused) return false;
        if (dimSphere) dimSphere.transform.position = rig.Camera.transform.position;
        if (resumeAt > 0f && Time.unscaledTime >= resumeAt) Resume();
        return paused;
    }

    void Resume()
    {
        if (pauseRoot) Destroy(pauseRoot);
        if (dimSphere) Destroy(dimSphere);
        optionObjs.Clear();
        optionButtons.Clear();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        rig.MoveSpeed = savedMove;
        rig.SnapTurnDegrees = savedSnap;
        paused = false;
        resumeAt = -1f;
        Debug.Log($"[awareness] check finished: {CheckScore()} of {questions.Count} answers match the simulation; scene resumed");
    }

    int CheckScore()
    {
        int n = 0;
        foreach (var q in questions) if (q.Right) n++;
        return n;
    }

    // ------------------------------------------------------------------------------------------------- prompt and summary

    /// <summary>A short message panel placed in front of the learner (the task board is back at the entry).</summary>
    void ShowPrompt(string title, string body, float seconds, Color accent)
    {
        if (promptRoot) Destroy(promptRoot);
        var cam = rig.Camera.transform;
        Vector3 fwd = Flat(cam.forward);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();
        promptRoot = new GameObject("Prompt");
        var t = promptRoot.transform;
        t.SetPositionAndRotation(cam.position + fwd * 1.7f - Vector3.up * 0.35f, Quaternion.LookRotation(fwd, Vector3.up));
        Ui.Panel(t, "Panel", Vector3.zero, new Vector2(1.5f, 0.34f), Navy, accent, 0.12f);
        P.Text(t, title, new Vector3(0, 0.055f, -0.015f), 0.06f, accent, TextAnchor.MiddleCenter, true);
        P.Text(t, body, new Vector3(0, -0.06f, -0.015f), 0.048f, Color.white, TextAnchor.MiddleCenter);
        StartCoroutine(FitText(t, 1.38f));
        promptUntil = Time.time + seconds;
    }

    void UpdatePrompt()
    {
        if (promptRoot && Time.time > promptUntil) Destroy(promptRoot);
    }

    /// <summary>End-of-scenario summary in front of the learner, with the awareness check as a separate result.</summary>
    void ShowSummary()
    {
        if (!CoworkerHazard || rig == null) return;
        if (summaryRoot) Destroy(summaryRoot);
        if (promptRoot) Destroy(promptRoot);
        var cam = rig.Camera.transform;
        Vector3 fwd = Flat(cam.forward);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();
        summaryRoot = new GameObject("Scenario summary");
        var t = summaryRoot.transform;
        t.SetPositionAndRotation(cam.position + fwd * 1.9f - Vector3.up * 0.1f, Quaternion.LookRotation(fwd, Vector3.up));
        const float w = 1.9f, h = 1.12f, x0 = -0.86f;
        Ui.Panel(t, "Panel", Vector3.zero, new Vector2(w, h), Navy, P.Gold, 0.05f);
        SignBack(t, Vector3.zero, new Vector2(w, h));

        float y = 0.45f;
        P.Text(t, "SCENARIO SUMMARY", new Vector3(x0, y, -0.015f), 0.075f, P.Gold, TextAnchor.MiddleLeft, true);
        y -= 0.1f;
        P.Text(t, board.Summary(), new Vector3(x0, y, -0.015f), 0.05f, board.CriticalMisses > 0 ? new Color(1f, 0.45f, 0.45f) : Good, TextAnchor.MiddleLeft, true);
        y -= 0.075f;
        P.Text(t, $"Mistakes: {board.Mistakes}    Critical: {board.CriticalMisses}", new Vector3(x0, y, -0.015f), 0.042f, P.Sky, TextAnchor.MiddleLeft);

        y -= 0.12f;
        P.Text(t, "AWARENESS CHECK", new Vector3(x0, y, -0.015f), 0.06f, P.Gold, TextAnchor.MiddleLeft, true);
        y -= 0.085f;
        if (questions.Count > 0 && !paused && qIndex >= questions.Count)
        {
            int s = CheckScore();
            P.Text(t, $"{s} of {questions.Count} answers matched the simulation", new Vector3(x0, y, -0.015f), 0.046f, s == questions.Count ? Good : Warn, TextAnchor.MiddleLeft, true);
            string[] shortQ = { "Where was the co-worker?", "Which way were they walking?", "How far was the forklift?" };
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                y -= 0.07f;
                string actual = q.Options[q.Primary];
                string close = q.Right && q.Chosen != q.Primary ? " (close to the line, accepted)" : "";
                P.Text(t, $"{(q.Right ? "OK" : "--")}  {shortQ[i]}  You: {q.Options[q.Chosen]}.  Simulation: {actual}{close}.", new Vector3(x0, y, -0.015f), 0.036f, q.Right ? Color.white : new Color(1f, 0.8f, 0.55f), TextAnchor.MiddleLeft);
            }
        }
        else
        {
            string why = ev == Ev.Idle || ev == Ev.Armed ? "Not run: the scenario ended before the co-worker appeared."
                : !AwarenessCheck ? "Questions turned off (Awareness Check is off)."
                : "Not finished.";
            P.Text(t, why, new Vector3(x0, y, -0.015f), 0.042f, P.Sky, TextAnchor.MiddleLeft);
        }
        y -= 0.1f;
        string stop = behaviourDone ? behaviourText : ev == Ev.Active ? "Co-worker event still in progress." : "Co-worker event did not take place.";
        P.Text(t, "Stop, watch, wait: " + stop, new Vector3(x0, y, -0.015f), 0.04f, behaviourDone && held && watched ? Good : P.Sky, TextAnchor.MiddleLeft);
        P.Text(t, "General safety-awareness practice. Not a test, a certification, or a training record.", new Vector3(0, -h / 2f + 0.07f, -0.015f), 0.032f, new Color(0.7f, 0.78f, 0.9f), TextAnchor.MiddleCenter);
        StartCoroutine(FitText(t, w * 0.92f));
        Debug.Log($"[awareness] summary shown. Board: {board.Summary()} Awareness: {(questions.Count > 0 ? CheckScore() + "/" + questions.Count : "not run")}. Behaviour: {stop}");
    }

    // ------------------------------------------------------------------------------------------------- scripted self-test and screenshots

    void WearAllPpe()
    {
        foreach (var gr in gear)
        {
            var g = gr.G;
            if (g == null || g.SnapId == null || !g.SnapId.StartsWith("ppe-") || g.Snapped != null) continue;
            SnapZone zone = g.SnapId == "ppe-hat" ? hatZone : g.SnapId == "ppe-vest" ? vestZone : glassesZone;
            g.transform.position = zone.transform.position;
            SnapZone.TrySnap(g);
        }
    }

    /// <summary>Worn PPE sits at the camera, so the hard hat brim shows at the top of a shot taken from the learner's eyes.</summary>
    void SetWornPpeVisible(bool on)
    {
        foreach (var gr in gear)
            if (gr.G != null && gr.G.Snapped != null)
                foreach (var r in gr.G.GetComponentsInChildren<Renderer>()) r.enabled = on;
    }

    static IEnumerator WaitUntilOr(System.Func<bool> cond, float realSeconds)
    {
        float t0 = Time.realtimeSinceStartup;
        while (!cond() && Time.realtimeSinceStartup - t0 < realSeconds) yield return null;
    }

    /// <summary>
    /// Plays the co-worker event live with a scripted learner and photographs it. The learner stands still in the
    /// interior walkway facing the loading forklift. During the check it answers question 1 and 3 with an answer that
    /// matches the simulation and question 2 with one that does not, so the expected score is 2 of 3. Results go to the
    /// log as [selftest] lines.
    /// </summary>
    IEnumerator CoworkerShots(ShotApi api)
    {
        if (!CoworkerHazard) yield break;
        var fails = new List<string>();
        Debug.Log("[selftest] co-worker event and awareness check: start");

        // back to live play
        foreach (var bb in FindObjectsByType<WfBillboard>(FindObjectsSortMode.None)) bb.enabled = true;
        foreach (var tf in FindObjectsByType<TagFacing>(FindObjectsSortMode.None)) tf.enabled = true;
        HideFlash();

        // The capture run stops XR, and the head's TrackedPoseDriver then resets the camera rotation every frame,
        // which would undo the scripted head direction before the scenario reads it. Turn it off for this run.
        if (rig.Camera.GetComponent("TrackedPoseDriver") is Behaviour tpd) tpd.enabled = false;

        // the learner: PPE on, in the interior walkway, facing the west lane
        Vector3 standAt = new Vector3(-4.0f, 0f, 6.0f);
        var camLocal = rig.Camera.transform.localPosition;
        rig.transform.position = standAt - new Vector3(camLocal.x, 0f, camLocal.z);
        rig.LookAt(new Vector3(-10.5f, 1.2f, loadPoint.z));
        prevZone = Zone.Walk;
        yield return null;
        yield return null;
        WearAllPpe(); // while still staged, so the PPE rule does not fire on the first live frame
        yield return null;
        staging = false;
        forklift.Frozen = false;
        forklift.ClearHolds(); // screenshot 03 left the forklift "acknowledged" for 60 s
        forklift.Place(new Vector3(4f, 0f, 12f), Vector3.left, 0f);
        forklift.SetTarget(3);
        yield return null;
        mirrorChecked = true;
        board.Complete(1, "(self-test) mirror step marked done by the script");
        board.Complete(2, "(self-test) crossing step marked done by the script");
        int mistakes0 = board.Mistakes, critical0 = board.CriticalMisses;
        Debug.Log($"[selftest] learner placed at {rig.FeetPosition}; PPE all on = {allPpe}; mistakes {mistakes0}, critical {critical0}");

        // wait for the event to arm, then bring the forklift round to the west lane
        yield return WaitUntilOr(() => ev == Ev.Armed, 20f);
        if (ev != Ev.Armed) fails.Add("event did not arm after the crossing step");
        forklift.Place(new Vector3(-10f, 0f, 10.8f), Vector3.back, 1.2f);
        forklift.SetTarget(0);
        yield return WaitUntilOr(() => ev == Ev.Active, 25f);
        if (ev != Ev.Active) fails.Add("forklift did not reach the loading point / co-worker did not appear");
        Debug.Log($"[selftest] co-worker appeared; forklift phase {forklift.Phase} at {forklift.transform.position}");

        // the co-worker crossing the north lane toward the working forklift (before the freeze)
        yield return WaitUntilOr(() => evT >= 2.8f || paused, 10f);
        yield return Snap(api, "10-coworker-appears", new Vector3(-3.6f, 2.5f, 1.6f), new Vector3(-9.0f, 0.9f, 9.5f), 60f);

        // the freeze
        yield return WaitUntilOr(() => paused, 10f);
        if (!paused) fails.Add("scene did not freeze for the awareness check");
        else
        {
            Vector3 f0 = forklift.transform.position, c0 = coworker.transform.position;
            yield return new WaitForSecondsRealtime(0.6f);
            float drift = Vector3.Distance(f0, forklift.transform.position) + Vector3.Distance(c0, coworker.transform.position);
            Debug.Log($"[selftest] paused: timeScale = {Time.timeScale}, forklift + co-worker moved {drift:F4} m in 0.6 s of real time");
            if (Time.timeScale != 0f || drift > 0.001f) fails.Add("scene kept moving during the pause");

            var cam = rig.Camera.transform;
            Debug.Log($"[selftest] learner head {cam.position}, facing {cam.forward}; question panel at {(pauseRoot ? pauseRoot.transform.position.ToString() : "missing")}");
            SetWornPpeVisible(false);
            yield return Snap(api, "11-awareness-check", cam.position, cam.position + cam.forward * 2f, 62f);
            SetWornPpeVisible(true);

            // answer through the real buttons: Q1 and Q3 matching the simulation, Q2 not
            int expected = 0;
            for (int i = 0; i < 3 && paused && qIndex < questions.Count; i++)
            {
                var q = questions[qIndex];
                int pick = i == 1 ? q.FirstWrong : q.Primary;
                if (q.Correct[pick]) expected++;
                if (pick < optionButtons.Count) optionButtons[pick].Press(rig.Right);
                yield return new WaitForSecondsRealtime(0.25f);
            }
            int score = CheckScore();
            Debug.Log($"[selftest] awareness check score {score} of {questions.Count}; expected {expected} (Q2 deliberately answered wrong)");
            if (score != expected || questions.Count != 3) fails.Add($"score {score} != expected {expected}");
            yield return WaitUntilOr(() => !paused, 5f);
            if (paused) fails.Add("scene did not resume");
            Debug.Log($"[selftest] resumed: timeScale = {Time.timeScale}, rig move speed = {rig.MoveSpeed}");
        }

        // after the check: stand still and watch the co-worker until they are clear
        bool shot12 = false;
        float t0 = Time.realtimeSinceStartup;
        while (ev == Ev.Active && Time.realtimeSinceStartup - t0 < 40f)
        {
            rig.LookAt(coworker.ChestPosition);
            if (!shot12 && (forklift.HoldingForWalker || evT >= 8.5f))
            {
                shot12 = true;
                Debug.Log($"[selftest] forklift holding for the co-worker = {forklift.HoldingForWalker} (phase {forklift.Phase}), co-worker at {coworker.transform.position}");
                yield return Snap(api, "12-coworker-behind-forklift", new Vector3(-4.4f, 2.3f, 1.2f), new Vector3(-9.2f, 0.9f, loadPoint.z + 0.4f), 58f);
            }
            yield return null;
        }
        if (ev != Ev.Done) fails.Add("co-worker event did not finish");
        if (!behaviourDone || !held || !watched) fails.Add($"stop-watch-wait not recorded as done (held {held}, watched {watched})");
        Vector3 fp = forklift.transform.position;
        yield return WaitUntilOr(() => Vector3.Distance(fp, forklift.transform.position) > 1f, 8f);
        bool movedOn = Vector3.Distance(fp, forklift.transform.position) > 1f;
        Debug.Log($"[selftest] forklift moved off after loading = {movedOn}; mistakes {board.Mistakes} (was {mistakes0}), critical {board.CriticalMisses} (was {critical0})");
        if (!movedOn) fails.Add("forklift did not resume its route");
        if (board.Mistakes != mistakes0 || board.CriticalMisses != critical0) fails.Add("the event changed the existing mistake counts");

        // finish the board: the end-of-scenario summary appears in front of the learner
        board.Complete(3, "(self-test) report step marked done by the script");
        yield return null;
        yield return null;
        if (summaryRoot == null) fails.Add("summary panel not shown");
        rig.LookAt(summaryRoot ? summaryRoot.transform.position : rig.Camera.transform.position + Vector3.forward);
        forklift.Frozen = true;
        yield return null;
        var c = rig.Camera.transform;
        SetWornPpeVisible(false);
        yield return Snap(api, "13-summary", c.position, c.position + c.forward * 2f, 58f);
        SetWornPpeVisible(true);

        Debug.Log(fails.Count == 0 ? "[selftest] PASS" : "[selftest] FAIL: " + string.Join("; ", fails));
        staging = true;
    }
}
