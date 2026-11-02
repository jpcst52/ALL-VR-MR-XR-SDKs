using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Outdoor rounds at the battery limits of a refinery unit. The player checks the wind and the muster point, bump-tests a
/// gas monitor, and enters the unit; then reacts to a gas alarm, a flange weep, and a site emergency alarm.
/// Gas is drawn as drifting puffs that really move with the live wind, so "upwind" and "crosswind" are real.
/// General safety-awareness practice only: it does not replace a site procedure, permit, or training.
/// </summary>
public class RefineryRounds : MonoBehaviour, IShotScript
{
    // ------------------------------------------------------------------------------------------------- layout
    static readonly Vector3 Center = new Vector3(0f, 0f, 5f);            // middle of the unit
    static readonly Vector3 FlangePos = new Vector3(-4.5f, 1.2f, 3.2f);  // flange F-12 on the branch line
    static readonly Vector3 GaugePos = new Vector3(7.5f, 0f, 0f);        // round stop G-1
    static readonly Vector3 SockPos = new Vector3(5f, 0f, -9f);
    static readonly Vector3 TablePos = new Vector3(-2.6f, 0f, -9f);
    static readonly Vector3 CabinPos = new Vector3(-10f, 0f, -10f);
    static readonly Vector3 BagPos = new Vector3(-1.5f, 0.13f, 7.5f);
    static readonly Vector3 DetectorPos = new Vector3(3.5f, 0f, 6f);
    static readonly Vector3 FlarePos = new Vector3(15f, 0f, 6f);
    static readonly Vector3 ConePos = new Vector3(-10.2f, 0f, -3.5f);

    const float FenceMinX = -12f, FenceMaxX = 12f, FenceMinZ = -6f, FenceMaxZ = 14f, GateHalf = 1.6f;
    const float PuffLife = 20f;
    const int MaxPuffs = 36;
    const float SockSeg = 0.55f;
    // task board offset from the view: far enough right that the gate, its signs and the university plate stay clear
    const float BoardAhead = 3.6f, BoardRight = 2.3f;

    static readonly Color Good = new Color(0.3f, 0.9f, 0.62f);
    static readonly Color Warn = new Color(1f, 0.72f, 0.3f);
    static readonly Color Bad = new Color(1f, 0.38f, 0.38f);

    // ------------------------------------------------------------------------------------------------- gas model
    class Puff
    {
        public Transform T;
        public Material M;
        public Vector3 Origin, Jitter;
        public float Born;
        public bool Active;
    }

    class Emitter
    {
        public Vector3 Origin;
        public bool On;
        public float Rate, Peak, Next;
        public readonly List<Puff> Pool = new List<Puff>();
    }

    class Placed
    {
        public Vector3 Pos;
        public float A;
    }

    readonly Emitter emitA = new Emitter { Rate = 1.4f, Peak = 60f };   // task 2 release
    readonly Emitter emitW = new Emitter { Rate = 0.8f, Peak = 24f };   // task 3 weep
    readonly Emitter emitE = new Emitter { Rate = 1.6f, Peak = 70f };   // task 4 emergency release
    Emitter[] emitters;
    Transform gasRoot;

    // ------------------------------------------------------------------------------------------------- state
    XRRig rig;
    TaskBoard board;
    Transform boardAnchor;
    bool boardMoving;
    Vector3 lastFwd = Vector3.forward;

    Weather.Reading weather;
    bool weatherReady;
    Vector3 windDir = Vector3.right;   // direction the wind blows TOWARD
    float drift = 1.5f;                // gas drift speed, m/s

    int task, phase;
    float phaseTimer, gazeWind, gazeMuster, evalTimer, exposure, beepTimer;
    float alarmStartTime, emergencyStart;
    bool done, shotMode, wasInside, hintGiven;
    bool bumped, bumpRunning, monitorHeld, monitorClipped, phoneOff, alarmActive;
    float monReading, monitorForce = -1f;
    int totalMistakes, totalCritical;
    readonly HashSet<string> flagged = new HashSet<string>();
    readonly List<Placed> placedTmp = new List<Placed>();

    // scene objects
    Material steel, darkSteel, concrete, hazard;
    Transform sockPivot, musterRoot, musterFlag, radio, monitor, release, markerRing;
    readonly List<Transform> sockJoints = new List<Transform>();
    Transform flameRoot;
    Light flameLight, cabinBeaconLight;
    Material cabinLampMat, detLampMat, monLampMat;
    TextMesh weatherText, monScreen, monText, detText, phoneLabel;
    Transform markerTag;
    string markerText;
    Rigidbody monitorRb;
    Grabbable monitorGrab;
    AudioSource monAudio, siren;
    GameObject marker, weepVisual;
    LineRenderer tape;
    Transform toolBag;
    Vector3 releasePos, bumpPos;
    readonly List<Rigidbody> cones = new List<Rigidbody>();

    static readonly string[] TaskTitles =
    {
        "Task 1 of 4: prepare to enter", "Task 2 of 4: gas alarm", "Task 3 of 4: flange weep", "Task 4 of 4: site alarm",
    };
    static readonly string[] TaskSubs =
    {
        "Outdoor rounds at the battery limits  |  get ready before entering",
        "Your gas monitor alarms during rounds",
        "A small hydrocarbon weep at flange F-12",
        "The site emergency alarm is sounding",
    };
    static readonly string[][] TaskSteps =
    {
        new[]
        {
            "Look at the wind sock and note the wind direction",
            "Look at the muster point flag: know your exit",
            "Bump-test your gas monitor, then carry it",
            "Radio the control room: ENTERING",
            "Enter the unit through the gate",
        },
        new[]
        {
            "Move upwind or crosswind, out of the gas",
            "Warn nearby workers on the radio (WARN)",
            "Report to the control room (REPORT)",
        },
        new[]
        {
            "Stay 3.5 m clear and switch your phone off",
            "Barricade: place 3 cones around the leak",
            "Report the leak on the radio (REPORT)",
        },
        new[]
        {
            "Walk the safe route to the muster point",
            "Check in at the muster point for headcount",
        },
    };

    // "Why this matters": one plain-language reason per step, shown after the step is done (general safety awareness,
    // not a procedure). The rationale question at the end checks a reason whose purpose is easy to miss.
    static readonly string[][] StepWhy =
    {
        new[]
        {
            "Gas drifts with the wind. Knowing the wind tells you which way to move if there is a release.",
            "In an alarm there is no time to search. Knowing your muster point first saves seconds.",
            "A bump test shows the sensors and alarm respond to gas. A monitor can switch on fine and still miss gas.",
            "The control room tracks who is inside, so everyone can be accounted for in an emergency.",
            "The gate is the controlled way in, where entry is checked and others expect you to be.",
        },
        new[]
        {
            "Moving upwind or crosswind gets you out of the cloud fastest. Downwind keeps you in it.",
            "Nearby workers may not know about the gas yet. A warning keeps them from walking into it.",
            "The control room can isolate the release and start the response, but only once it knows.",
        },
        new[]
        {
            "Vapor near a leak can reach a flammable mix. Distance and no phone keep ignition sources away.",
            "Cones and tape stop others from walking into the hazard without knowing it is there.",
            "Leaks are repaired by authorized crews once the line is made safe. Your report starts that.",
        },
        new[]
        {
            "A route upwind of the release keeps you out of the gas on the way to safety.",
            "The headcount tells responders who is safe, so no one goes into the gas searching for you.",
        },
    };

    static readonly RationaleQuestion Question = new RationaleQuestion
    {
        Topic = "why the gas monitor is bump-tested",
        Prompt = "Your gas monitor switched on and read 0. Why bump-test it before you enter the unit?",
        Options = new[]
        {
            "To set the monitor for today's wind direction.",
            "It is only paperwork for the site log.",
            "To prove the sensors and alarm really respond to gas before you rely on them.",
        },
        Feedback = new[]
        {
            "A gas monitor does not measure wind. The wind sock and the conditions board tell you that.",
            "A record may be kept, but the point is knowing the alarm will work when you need it.",
            "A monitor can switch on and look normal yet miss gas. Test gas shows that it responds.",
        },
        Best = 2,
    };

    WhyCue why;
    int boardTask;

    // ------------------------------------------------------------------------------------------------- setup

    void Start()
    {
        QualitySettings.shadows = ShadowQuality.All;
        // sky / horizon / ground ambient reads more like daylight than one flat colour
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.56f, 0.64f, 0.76f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.54f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.66f, 0.76f, 0.86f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 190f;

        emitters = new[] { emitA, emitW, emitE };
        steel = P.Steel();
        darkSteel = P.DarkSteel();
        concrete = P.Concrete();
        hazard = P.Hazard();

        var root = new GameObject("Refinery unit").transform;
        root.SetParent(transform, false);
        gasRoot = new GameObject("Gas cloud").transform;
        gasRoot.SetParent(transform, false);

        BuildGround(root);
        BuildFence(root);
        BuildRack(root);
        BuildVessels(root);
        BuildFlange(root);
        BuildGauge(root);
        BuildFlare(root);
        BuildCabin(root);
        BuildSock(root);
        BuildConditionsBoard(root);
        BuildTable(root);
        BuildDetector(root);
        BuildMuster(root);
        BuildPlaceables(root);
        BuildMarker(root);
        BuildAudio(root);
        BuildBrand(root);

        rig = XRRig.Create(new Vector3(0f, 0f, -14f), 0f);
        rig.Area = new Bounds(new Vector3(0f, 0f, 4f), new Vector3(64f, 1f, 56f));
        rig.Camera.backgroundColor = new Color(0.62f, 0.76f, 0.9f);
        rig.Camera.clearFlags = CameraClearFlags.SolidColor;
        // a procedural daytime sky when the built-in shader is available (falls back to the flat colour)
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetFloat("_SunSize", 0.03f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.55f, 0.65f, 0.8f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.42f, 0.4f));
            sky.SetFloat("_Exposure", 1.15f);
            RenderSettings.skybox = sky;
            rig.Camera.clearFlags = CameraClearFlags.Skybox;
        }

        BuildRadio();
        why = WhyCue.Create(transform);
        // a little left of centre: the task board floats to the right of the view
        why.WhyOffset = new Vector2(-0.22f, -0.3f);
        why.AskOffset = new Vector2(-0.4f, -0.08f);
        boardAnchor = new GameObject("Task board anchor").transform;
        SnapBoardToView();
        StartTask(0);

        SetWind(Weather.Fallback);
        StartCoroutine(Weather.Fetch(r => { SetWind(r); weatherReady = true; }));
    }

    void SetWind(Weather.Reading r)
    {
        weather = r;
        float toward = (r.WindFromDegrees + 180f) * Mathf.Deg2Rad;
        windDir = new Vector3(Mathf.Sin(toward), 0f, Mathf.Cos(toward));
        drift = Mathf.Clamp(r.WindMph * 0.447f * 0.6f, 0.9f, 3.5f);
        PlaceMuster();
        UpdateWeatherText();
    }

    void UpdateWeatherText()
    {
        if (!weatherText) return;
        string src = weatherReady || weather.Live ? weather.Source : weather.Source + "  (live fetch pending)";
        weatherText.text =
            $"Wind: from {Compass(weather.WindFromDegrees)} ({weather.WindFromDegrees:F0} deg) at {weather.WindMph:F0} mph\n" +
            $"Blowing toward: {Compass(weather.WindFromDegrees + 180f)}\n" +
            $"Temperature: {weather.TemperatureF:F0} F\n" +
            $"Source: {src}\n" +
            "The wind sock and the gas drift follow this reading.";
    }

    static string Compass(float deg)
    {
        string[] n = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int i = Mathf.RoundToInt((((deg % 360f) + 360f) % 360f) / 45f) % 8;
        return n[i];
    }

    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    static float FlatDist(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }
    static bool Inside(Vector3 p) { return p.x > FenceMinX && p.x < FenceMaxX && p.z > FenceMinZ && p.z < FenceMaxZ; }

    static readonly Color Navy = new Color(0.03f, 0.08f, 0.2f, 0.96f);
    static readonly Color SafetyYellow = new Color(0.98f, 0.78f, 0.1f);
    static readonly Color SafetyGreen = new Color(0.04f, 0.45f, 0.24f);

    /// <summary>
    /// A rounded sign panel at a world position whose readable face points along <paramref name="faceDir"/> (toward the
    /// viewer). Text added to the returned transform goes at local z = -0.015.
    /// </summary>
    static Transform Sign(Transform parent, string name, Vector3 pos, Vector3 faceDir, Vector2 size, Color fill, Color? accent = null, float accentHeight = 0.08f)
    {
        var h = new GameObject(name).transform;
        h.SetParent(parent, false);
        h.position = pos;
        faceDir.y = 0f;
        h.rotation = Quaternion.LookRotation(-faceDir.normalized, Vector3.up);
        Ui.Panel(h, "Panel", Vector3.zero, size, fill, accent, accentHeight);
        SignBack(h, Vector3.zero, size);
        return h;
    }

    static Material backMat;

    /// <summary>
    /// A solid metal back for a one-sided panel, so from behind you see the sign's back and not its text reversed.
    /// Two crossed boxes stay inside the panel's rounded corners.
    /// </summary>
    static void SignBack(Transform parent, Vector3 center, Vector2 size)
    {
        if (backMat == null) backMat = P.Mat(new Color(0.42f, 0.44f, 0.47f), 0.6f, 0.45f);
        float r = 0.12f * Mathf.Min(size.x, size.y);
        const float d = 0.012f, z = 0.013f;
        P.Box(parent, "Sign back", center + V(0f, 0f, z), V(size.x * 0.99f, size.y - 2f * r, d), backMat, false);
        P.Box(parent, "Sign back", center + V(0f, 0f, z), V(size.x - 2f * r, size.y * 0.99f, d), backMat, false);
    }

    /// <summary>Shrinks any text under <paramref name="root"/> that is wider than <paramref name="maxWidth"/>.</summary>
    static IEnumerator FitText(Transform root, float maxWidth)
    {
        yield return null; // text meshes are built by then
        foreach (var tm in root.GetComponentsInChildren<TextMesh>())
        {
            float w = tm.GetComponent<MeshRenderer>().localBounds.size.x;
            if (w > maxWidth) tm.characterSize *= maxWidth / w;
        }
    }

    /// <summary>Text on a sign made by <see cref="Sign"/>.</summary>
    static TextMesh SignText(Transform sign, string s, float y, float height, Color color, bool bold = true, float x = 0f, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        return P.Text(sign, s, V(x, y, -0.015f), height, color, anchor, bold);
    }

    // ------------------------------------------------------------------------------------------------- world

    void BuildGround(Transform root)
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.transform.SetParent(root, false);
        sun.type = LightType.Directional;
        sun.intensity = 1.05f;
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        sun.shadows = LightShadows.Soft;

        var gravel = P.Mat(Color.white, 0f, 0.08f, P.NoiseTexture("gravel", new Color(0.30f, 0.28f, 0.24f), new Color(0.52f, 0.5f, 0.46f), 10f, 256, 1f, 11), 26f);
        P.Box(root, "Ground", V(0f, -0.1f, 5f), V(130f, 0.2f, 130f), gravel);
        P.Box(root, "Process pad", V(0f, 0.02f, 4f), V(24f, 0.04f, 20f), concrete, false);
        P.Box(root, "Gate line", V(0f, 0.045f, FenceMinZ), V(3.2f, 0.01f, 0.25f), hazard, false);
        P.Box(root, "Walkway", V(0f, 0.045f, -10f), V(1.2f, 0.01f, 8f), P.Mat(new Color(0.9f, 0.75f, 0.1f), 0f, 0.3f), false);
    }

    /// <summary>
    /// University plate standing on the gate lintel, centred on the walkway: in view from the start and on the whole
    /// approach, above head height so it blocks nothing, and clear of the task board (which floats to the right).
    /// </summary>
    void BuildBrand(Transform root)
    {
        const float w = 1.6f; // plate is w x w/2
        float y = 2.84f + 0.08f + w / 4f;
        var plate = Ui.BrandPlate(root, V(0f, y, FenceMinZ - 0.1f), w);
        SignBack(root, V(0f, y, FenceMinZ - 0.1f), new Vector2(w, w / 2f));
        StartCoroutine(FitText(plate, w * 0.9f));
        P.Box(root, "Plate stand", V(-0.5f, 2.88f, FenceMinZ - 0.05f), V(0.06f, 0.1f, 0.06f), darkSteel, false);
        P.Box(root, "Plate stand", V(0.5f, 2.88f, FenceMinZ - 0.05f), V(0.06f, 0.1f, 0.06f), darkSteel, false);
    }

    void BuildFence(Transform root)
    {
        var post = P.Mat(new Color(0.45f, 0.47f, 0.5f), 0.7f, 0.5f);
        var mesh = P.Transparent(new Color(0.7f, 0.72f, 0.75f, 0.16f));
        FenceRun(root, V(FenceMinX, 0f, FenceMinZ), V(FenceMinX, 0f, FenceMaxZ), post, mesh);
        FenceRun(root, V(FenceMaxX, 0f, FenceMinZ), V(FenceMaxX, 0f, FenceMaxZ), post, mesh);
        FenceRun(root, V(FenceMinX, 0f, FenceMaxZ), V(FenceMaxX, 0f, FenceMaxZ), post, mesh);
        FenceRun(root, V(FenceMinX, 0f, FenceMinZ), V(-GateHalf, 0f, FenceMinZ), post, mesh);
        FenceRun(root, V(GateHalf, 0f, FenceMinZ), V(FenceMaxX, 0f, FenceMinZ), post, mesh);
        // Gate posts, lintel, and sign
        P.Box(root, "Gate post L", V(-GateHalf, 1.4f, FenceMinZ), V(0.18f, 2.8f, 0.18f), P.Painted(P.Royal), false);
        P.Box(root, "Gate post R", V(GateHalf, 1.4f, FenceMinZ), V(0.18f, 2.8f, 0.18f), P.Painted(P.Royal), false);
        P.Box(root, "Gate lintel", V(0f, 2.75f, FenceMinZ), V(GateHalf * 2f + 0.2f, 0.18f, 0.18f), P.Painted(P.Royal), false);
        var gate = Sign(root, "Gate sign", V(0f, 2.3f, FenceMinZ - 0.11f), Vector3.back, new Vector2(3.0f, 0.6f), SafetyYellow, new Color(0.08f, 0.08f, 0.08f), 0.1f);
        SignText(gate, "UNIT ENTRY", 0.09f, 0.1f, new Color(0.08f, 0.08f, 0.08f));
        SignText(gate, "GAS MONITOR REQUIRED BEYOND THIS GATE", -0.1f, 0.075f, new Color(0.08f, 0.08f, 0.08f));
        // brackets that hang the sign from the lintel
        P.Box(root, "Sign bracket", V(-1.1f, 2.67f, FenceMinZ - 0.06f), V(0.05f, 0.14f, 0.08f), darkSteel, false);
        P.Box(root, "Sign bracket", V(1.1f, 2.67f, FenceMinZ - 0.06f), V(0.05f, 0.14f, 0.08f), darkSteel, false);
    }

    void FenceRun(Transform root, Vector3 a, Vector3 b, Material m, Material meshMat)
    {
        float len = Vector3.Distance(a, b);
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 3f));
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, (float)i / n);
            P.Box(root, "Fence post", p + Vector3.up * 1.1f, V(0.08f, 2.2f, 0.08f), m, false);
        }
        Vector3 mid = (a + b) / 2f, d = b - a;
        bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
        Vector3 railSize = alongX ? V(len, 0.05f, 0.04f) : V(0.04f, 0.05f, len);
        float[] ys = { 0.5f, 1.1f, 2.15f };
        for (int i = 0; i < ys.Length; i++) P.Box(root, "Fence rail", V(mid.x, ys[i], mid.z), railSize, m, false);
        P.Box(root, "Fence mesh", V(mid.x, 1.25f, mid.z), alongX ? V(len, 1.7f, 0.01f) : V(0.01f, 1.7f, len), meshMat, false);
    }

    void Pipe(Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
    {
        P.Cyl(parent, "Pipe", (a + b) / 2f, radius, Vector3.Distance(a, b), b - a, mat, false);
    }

    void BuildRack(Transform root)
    {
        var paint = P.Painted(new Color(0.15f, 0.3f, 0.55f));
        for (int i = 0; i < 6; i++)
        {
            float x = -10f + i * 4f;
            P.Box(root, "Rack column", V(x, 2.6f, 3f), V(0.22f, 5.2f, 0.22f), paint, false);
            P.Box(root, "Rack column", V(x, 2.6f, 5f), V(0.22f, 5.2f, 0.22f), paint, false);
            P.Box(root, "Rack beam", V(x, 3.55f, 4f), V(0.2f, 0.2f, 2.2f), paint, false);
            P.Box(root, "Rack beam", V(x, 5.05f, 4f), V(0.2f, 0.2f, 2.2f), paint, false);
        }
        Color[] cols =
        {
            new Color(0.75f, 0.76f, 0.78f), new Color(0.82f, 0.25f, 0.2f), new Color(0.2f, 0.45f, 0.75f),
            new Color(0.85f, 0.7f, 0.15f), new Color(0.3f, 0.6f, 0.35f),
        };
        for (int k = 0; k < 5; k++)
            P.Cyl(root, "Rack pipe", V(0f, 5.3f, 3.2f + k * 0.4f), 0.13f + 0.02f * (k % 3), 22f, Vector3.right, P.Mat(cols[k], 0.4f, 0.5f), false);
        for (int k = 0; k < 3; k++)
            P.Cyl(root, "Rack pipe low", V(0f, 3.8f, 3.4f + k * 0.55f), 0.12f, 22f, Vector3.right, P.Mat(cols[(k + 2) % 5], 0.4f, 0.5f), false);
    }

    void BuildVessels(Transform root)
    {
        // V-101 tower
        P.Cyl(root, "V-101 shell", V(-7f, 5f, 10f), 1.3f, 10f, Vector3.up, steel, false);
        var head = P.Ball(root, "V-101 head", V(-7f, 10f, 10f), 1.3f, steel, false);
        head.transform.localScale = V(2.6f, 1.2f, 2.6f);
        P.Cyl(root, "V-101 skirt", V(-7f, 0.4f, 10f), 1.35f, 0.8f, Vector3.up, darkSteel, false);
        P.Cyl(root, "V-101 platform", V(-7f, 6.5f, 10f), 2.0f, 0.08f, Vector3.up, darkSteel, false);
        P.Box(root, "Ladder rail", V(-7.2f, 3.5f, 8.55f), V(0.05f, 6f, 0.05f), darkSteel, false);
        P.Box(root, "Ladder rail", V(-6.8f, 3.5f, 8.55f), V(0.05f, 6f, 0.05f), darkSteel, false);
        for (int i = 0; i < 16; i++) P.Box(root, "Ladder rung", V(-7f, 0.7f + i * 0.35f, 8.55f), V(0.4f, 0.03f, 0.03f), darkSteel, false);
        // equipment ID plate fixed to the shell, clear of the ladder
        Vector3 out101 = V(-0.7f, 0f, -0.7f).normalized;
        var id101 = Sign(root, "V-101 ID plate", V(-7f, 2.3f, 10f) + out101 * 1.32f, out101, new Vector2(0.8f, 0.42f), Navy, P.Gold);
        SignText(id101, "V-101", 0.02f, 0.17f, Color.white);
        SignText(id101, "PROCESS TOWER", -0.13f, 0.05f, P.Sky, false);
        Pipe(root, V(-7f, 3.8f, 8.7f), V(-7f, 3.8f, 4.4f), 0.12f, steel);

        // V-102 horizontal drum
        P.Cyl(root, "V-102 shell", V(6f, 1.9f, 9f), 1.3f, 7f, Vector3.right, steel, false);
        for (int s = -1; s <= 1; s += 2)
        {
            var cap = P.Ball(root, "V-102 head", V(6f + s * 3.5f, 1.9f, 9f), 1.3f, steel, false);
            cap.transform.localScale = V(1.2f, 2.6f, 2.6f);
            P.Box(root, "V-102 saddle", V(6f + s * 2f, 0.3f, 9f), V(0.4f, 0.6f, 1.8f), P.Painted(P.Royal), false);
        }
        P.Cyl(root, "V-102 nozzle", V(6f, 3.4f, 9f), 0.12f, 0.5f, Vector3.up, steel, false);
        Pipe(root, V(6f, 3.6f, 9f), V(6f, 3.8f, 9f), 0.1f, steel);
        Pipe(root, V(6f, 3.8f, 9f), V(6f, 3.8f, 4.2f), 0.1f, steel);
        var id102 = Sign(root, "V-102 ID plate", V(6f, 1.9f, 7.68f), Vector3.back, new Vector2(0.9f, 0.42f), Navy, P.Gold);
        SignText(id102, "V-102", 0.02f, 0.17f, Color.white);
        SignText(id102, "PROCESS DRUM", -0.13f, 0.05f, P.Sky, false);
    }

    void BuildFlange(Transform root)
    {
        // Branch line at 1.2 m with flange F-12; the weep is hidden until task 3.
        Pipe(root, V(-9f, 1.2f, 3.2f), V(0f, 1.2f, 3.2f), 0.1f, steel);
        Pipe(root, V(-9f, 1.2f, 3.2f), V(-9f, 3.7f, 3.2f), 0.1f, steel);
        P.Box(root, "Pipe stand", V(-8f, 0.55f, 3.2f), V(0.14f, 1.1f, 0.14f), darkSteel, false);
        P.Box(root, "Pipe stand", V(-1f, 0.55f, 3.2f), V(0.14f, 1.1f, 0.14f), darkSteel, false);
        var bright = P.Mat(new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.75f);
        P.Cyl(root, "Flange A", V(FlangePos.x - 0.1f, FlangePos.y, FlangePos.z), 0.2f, 0.05f, Vector3.right, bright, false);
        P.Cyl(root, "Flange B", V(FlangePos.x + 0.1f, FlangePos.y, FlangePos.z), 0.2f, 0.05f, Vector3.right, bright, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            P.Cyl(root, "Flange bolt", FlangePos + V(0f, Mathf.Cos(a) * 0.16f, Mathf.Sin(a) * 0.16f), 0.014f, 0.2f, Vector3.right, darkSteel, false);
        }
        Ui.Tag(root, "FLANGE F-12", FlangePos + V(0f, 0.2f, 0f), V(0f, 0.75f, -0.25f), 0.09f, P.Gold);

        // The weep: a wet streak, a puddle, and the unsafe things you can press (all hidden until it starts).
        weepVisual = new GameObject("Weep");
        weepVisual.transform.SetParent(root, false);
        var wet = P.Mat(new Color(0.05f, 0.04f, 0.03f), 0.2f, 0.95f);
        P.Box(weepVisual.transform, "Wet streak", V(FlangePos.x, 0.65f, FlangePos.z), V(0.03f, 1.1f, 0.03f), wet, false);
        P.Cyl(weepVisual.transform, "Puddle", V(FlangePos.x, 0.025f, FlangePos.z), 0.55f, 0.012f, Vector3.up, wet, false);
        var bolts = P.Prim(PrimitiveType.Sphere, weepVisual.transform, "Bolts target", FlangePos, Vector3.one * 0.55f, null);
        bolts.GetComponent<Renderer>().enabled = false;
        bolts.AddComponent<Pressable>().Pressed += h =>
        {
            if (task == 2 && phase == 1) MistakeOnce("tighten", "Never tighten a leaking joint yourself.", true);
        };
        var rag = P.Box(weepVisual.transform, "Rag", V(FlangePos.x + 1.1f, 0.05f, FlangePos.z - 0.7f), V(0.35f, 0.06f, 0.25f), P.Mat(new Color(0.55f, 0.5f, 0.42f), 0f, 0.1f));
        rag.AddComponent<Pressable>().Pressed += h =>
        {
            if (task == 2 && phase == 1) MistakeOnce("rag", "A rag spreads the problem. Report the leak.");
        };
        Ui.Tag(weepVisual.transform, "RAG", V(FlangePos.x + 1.1f, 0.08f, FlangePos.z - 0.7f), V(0f, 0.45f, 0f), 0.06f, Warn);
        weepVisual.SetActive(false);
    }

    void BuildGauge(Transform root)
    {
        P.Cyl(root, "Gauge post", V(GaugePos.x, 0.75f, GaugePos.z), 0.04f, 1.5f, Vector3.up, steel, false);
        P.Cyl(root, "Gauge case", V(GaugePos.x, 1.5f, GaugePos.z), 0.2f, 0.06f, Vector3.forward, P.Mat(new Color(0.15f, 0.15f, 0.17f), 0.7f, 0.5f), false);
        P.Cyl(root, "Gauge face", V(GaugePos.x, 1.5f, GaugePos.z - 0.032f), 0.17f, 0.01f, Vector3.forward, P.Mat(Color.white, 0f, 0.2f), false);
        var needle = P.Box(root, "Gauge needle", V(GaugePos.x + 0.04f, 1.54f, GaugePos.z - 0.042f), V(0.01f, 0.14f, 0.004f), P.Mat(new Color(0.9f, 0.1f, 0.1f)), false);
        needle.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
        // tag plate on the gauge post, under the dial
        var g1 = Sign(root, "G-1 tag plate", V(GaugePos.x, 1.14f, GaugePos.z - 0.05f), Vector3.back, new Vector2(0.5f, 0.2f), Navy, P.Gold, 0.1f);
        SignText(g1, "G-1   52 PSI", -0.01f, 0.07f, P.Gold);
    }

    void BuildFlare(Transform root)
    {
        P.Box(root, "Flare base", V(FlarePos.x, 0.25f, FlarePos.z), V(1.6f, 0.5f, 1.6f), concrete, false);
        P.Cyl(root, "Flare stack", V(FlarePos.x, 11f, FlarePos.z), 0.3f, 22f, Vector3.up, darkSteel, false);
        P.Cyl(root, "Flare tip", V(FlarePos.x, 22.1f, FlarePos.z), 0.45f, 0.4f, Vector3.up, steel, false);
        // plate on the west face of the base, toward the unit
        var flare = Sign(root, "Flare plate", V(FlarePos.x - 0.82f, 0.27f, FlarePos.z), Vector3.left, new Vector2(1.3f, 0.36f), Navy, Bad);
        SignText(flare, "FLARE STACK", 0.0f, 0.14f, Color.white);
        flameRoot = new GameObject("Flare flame").transform;
        flameRoot.SetParent(root, false);
        flameRoot.position = V(FlarePos.x, 22.3f, FlarePos.z);
        var fire = P.Glow(new Color(1f, 0.5f, 0.1f), 2.4f);
        var core = P.Glow(new Color(1f, 0.85f, 0.3f), 3f);
        P.Ball(flameRoot, "Flame outer", V(0f, 0.9f, 0f), 0.5f, fire, false).transform.localScale = V(0.9f, 2.2f, 0.9f);
        P.Ball(flameRoot, "Flame core", V(0f, 0.6f, 0f), 0.3f, core, false).transform.localScale = V(0.5f, 1.4f, 0.5f);
        flameLight = MakeLight(flameRoot, flameRoot.position + V(0f, 1f, 0f), new Color(1f, 0.6f, 0.25f), 40f, 2f);
    }

    Light MakeLight(Transform parent, Vector3 pos, Color c, float range, float intensity)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = range;
        l.intensity = intensity;
        return l;
    }

    void BuildCabin(Transform root)
    {
        Vector3 c = CabinPos;
        P.Box(root, "Control room cabin", c + V(0f, 1.4f, 0f), V(4.4f, 2.8f, 3.2f), P.Mat(new Color(0.88f, 0.88f, 0.86f), 0.1f, 0.4f));
        P.Box(root, "Cabin roof", c + V(0f, 2.85f, 0f), V(4.6f, 0.12f, 3.4f), darkSteel, false);
        P.Box(root, "Cabin stripe", c + V(0f, 0.5f, -1.61f), V(4.4f, 0.5f, 0.02f), P.Painted(P.Royal), false);
        P.Box(root, "Cabin door", c + V(1.4f, 1.0f, -1.62f), V(0.9f, 2.0f, 0.04f), P.Painted(P.MediumBlue), false);
        var glass = P.Mat(new Color(0.6f, 0.75f, 0.9f), 0.6f, 0.9f);
        P.Box(root, "Cabin window", c + V(-1.4f, 1.6f, -1.62f), V(0.9f, 0.8f, 0.04f), glass, false);
        P.Box(root, "Cabin window", c + V(-0.2f, 1.6f, -1.62f), V(0.9f, 0.8f, 0.04f), glass, false);
        P.Box(root, "Cabin steps", c + V(1.4f, 0.15f, -2.1f), V(1.1f, 0.3f, 0.7f), concrete, false);
        var cr = Sign(root, "Control room sign", c + V(0f, 2.38f, -1.63f), Vector3.back, new Vector2(2.6f, 0.46f), P.MediumBlue, P.Gold);
        SignText(cr, "CONTROL ROOM", -0.01f, 0.2f, Color.white);
        P.Cyl(root, "Antenna", c + V(1.6f, 3.9f, 0.8f), 0.03f, 2f, Vector3.up, steel, false);
        cabinLampMat = P.Glow(new Color(1f, 0.1f, 0.05f), 1f);
        P.Ball(root, "Alarm beacon", c + V(-1.7f, 3.05f, 0f), 0.14f, cabinLampMat, false);
        cabinBeaconLight = MakeLight(root, c + V(-1.7f, 3.3f, 0f), new Color(1f, 0.15f, 0.1f), 25f, 3f);
        cabinBeaconLight.enabled = false;
        cabinLampMat.SetColor("_EmissionColor", Color.black);
    }

    void BuildSock(Transform root)
    {
        var pole = P.Mat(new Color(0.85f, 0.85f, 0.88f), 0.5f, 0.5f);
        P.Cyl(root, "Sock pole", V(SockPos.x, 2.6f, SockPos.z), 0.06f, 5.2f, Vector3.up, pole, false);
        P.Box(root, "Sock base", V(SockPos.x, 0.1f, SockPos.z), V(0.6f, 0.2f, 0.6f), concrete, false);
        P.Ball(root, "Sock cap", V(SockPos.x, 5.2f, SockPos.z), 0.09f, pole, false);
        sockPivot = new GameObject("Sock pivot").transform;
        sockPivot.SetParent(root, false);
        sockPivot.position = V(SockPos.x, 5.1f, SockPos.z);
        var orange = P.Painted(new Color(1f, 0.45f, 0.05f));
        var white = P.Painted(Color.white);
        Transform joint = sockPivot;
        for (int i = 0; i < 5; i++)
        {
            var j = new GameObject("Sock joint " + i).transform;
            j.SetParent(joint, false);
            j.localPosition = i == 0 ? Vector3.zero : V(0f, 0f, SockSeg);
            P.Cyl(j, "Sock segment", V(0f, 0f, SockSeg / 2f), 0.3f - 0.045f * i, SockSeg, Vector3.forward, i % 2 == 0 ? orange : white, false);
            sockJoints.Add(j);
            joint = j;
        }
        var ws = Sign(root, "Wind sock plate", V(SockPos.x, 1.7f, SockPos.z - 0.075f), Vector3.back, new Vector2(0.8f, 0.24f), Navy, P.Gold, 0.1f);
        SignText(ws, "WIND SOCK", -0.01f, 0.09f, Color.white);
    }

    void BuildConditionsBoard(Transform root)
    {
        Vector3 p = V(-5.2f, 2.4f, -9.8f);
        var cond = Sign(root, "Conditions board", p, Vector3.back, new Vector2(2.9f, 1.5f), Navy, P.Gold, 0.05f);
        P.Cyl(root, "Conditions post", V(p.x - 1.2f, 1.2f, p.z + 0.05f), 0.04f, 2.4f, Vector3.up, steel, false);
        P.Cyl(root, "Conditions post", V(p.x + 1.2f, 1.2f, p.z + 0.05f), 0.04f, 2.4f, Vector3.up, steel, false);
        SignText(cond, "WIND AND SITE CONDITIONS", 0.52f, 0.1f, P.Gold, true, -1.3f, TextAnchor.MiddleLeft);
        weatherText = SignText(cond, "", 0.06f, 0.07f, Color.white, false, -1.3f, TextAnchor.MiddleLeft);
    }

    void BuildTable(Transform root)
    {
        var wood = P.Mat(new Color(0.5f, 0.38f, 0.25f), 0f, 0.3f);
        P.Box(root, "Table top", V(TablePos.x, 0.85f, TablePos.z), V(1.6f, 0.06f, 0.75f), wood);
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            P.Box(root, "Table leg", V(TablePos.x + sx * 0.72f, 0.41f, TablePos.z + sz * 0.32f), V(0.06f, 0.82f, 0.06f), darkSteel, false);
        // instruction board standing at the back edge of the table, so the table top stays clear
        P.Box(root, "Board post", V(TablePos.x - 0.68f, 1.3f, TablePos.z + 0.4f), V(0.04f, 0.9f, 0.04f), darkSteel, false);
        P.Box(root, "Board post", V(TablePos.x + 0.68f, 1.3f, TablePos.z + 0.4f), V(0.04f, 0.9f, 0.04f), darkSteel, false);
        var pre = Sign(root, "Pre-entry board", V(TablePos.x, 1.45f, TablePos.z + 0.37f), Vector3.back, new Vector2(1.55f, 0.66f), Navy, P.Gold, 0.07f);
        SignText(pre, "PRE-ENTRY STATION", 0.19f, 0.09f, P.Gold);
        SignText(pre, "1. Grab the monitor (grip / E)\n2. Hold it at the bump-test unit\n3. Press the green BUMP TEST button", -0.08f, 0.05f, Color.white, false, -0.68f, TextAnchor.MiddleLeft);

        // Bump-test station: a small test-gas unit with a button.
        bumpPos = V(TablePos.x + 0.45f, 0.98f, TablePos.z);
        P.Box(root, "Bump station", V(TablePos.x + 0.45f, 0.94f, TablePos.z), V(0.36f, 0.12f, 0.24f), P.Painted(new Color(0.2f, 0.25f, 0.3f)), false);
        P.Cyl(root, "Test gas bottle", V(TablePos.x + 0.6f, 1.12f, TablePos.z + 0.04f), 0.05f, 0.28f, Vector3.up, P.Painted(new Color(0.15f, 0.55f, 0.3f)), false);
        var btn = P.Prim(PrimitiveType.Cylinder, root, "Bump button", V(TablePos.x + 0.35f, 1.012f, TablePos.z - 0.03f), V(0.13f, 0.012f, 0.13f), P.Glow(new Color(0.1f, 0.75f, 0.35f), 0.9f));
        var press = btn.AddComponent<Pressable>();
        press.Label = "BUMP TEST";
        press.Pressed += h => BumpPress();
        // label plate on the front face of the bump-test unit
        var bt = Sign(root, "Bump test label", V(TablePos.x + 0.45f, 0.94f, TablePos.z - 0.125f), Vector3.back, new Vector2(0.32f, 0.09f), new Color(0.06f, 0.4f, 0.2f), null);
        SignText(bt, "BUMP TEST", -0.002f, 0.04f, Color.white);

        BuildMonitor(V(TablePos.x - 0.45f, 0.97f, TablePos.z));
    }

    void BuildMonitor(Vector3 pos)
    {
        var go = new GameObject("Gas monitor");
        go.transform.position = pos;
        monitor = go.transform;
        monitorRb = go.AddComponent<Rigidbody>();
        monitorRb.mass = 0.3f;
        P.Box(monitor, "Body", Vector3.zero, V(0.07f, 0.12f, 0.03f), P.Painted(P.Gold), false);
        P.Box(monitor, "Screen", V(0f, 0.01f, -0.0155f), V(0.056f, 0.07f, 0.004f), P.Mat(new Color(0.03f, 0.05f, 0.04f), 0f, 0.7f), false);
        monScreen = P.Text(monitor, "0", V(0f, 0.012f, -0.0185f), 0.016f, Color.green, TextAnchor.MiddleCenter, true);
        var lamp = P.Ball(monitor, "LED", V(0f, 0.054f, -0.012f), 0.009f, P.Glow(Color.green, 1.2f), false);
        monLampMat = lamp.GetComponent<Renderer>().sharedMaterial;
        var col = go.AddComponent<BoxCollider>();
        col.size = V(0.12f, 0.18f, 0.08f);
        monitorGrab = go.AddComponent<Grabbable>();
        monitorGrab.Grabbed += g => { monitorHeld = true; monitorClipped = false; };
        monitorGrab.Dropped += g => { monitorHeld = false; if (bumped) monitorClipped = true; };
        monAudio = P.Speaker(monitor, P.Tone("beep", 2200f, 0.12f, 0.5f, 3f), false, 0.8f, 1f);
    }

    void BuildDetector(Transform root)
    {
        P.Cyl(root, "Detector post", V(DetectorPos.x, 1.5f, DetectorPos.z), 0.05f, 3f, Vector3.up, steel, false);
        P.Box(root, "Detector head", V(DetectorPos.x, 2.9f, DetectorPos.z), V(0.5f, 0.4f, 0.25f), P.Painted(new Color(0.2f, 0.25f, 0.3f)), false);
        var lampMat = P.Glow(Color.green, 1.2f);
        P.Ball(root, "Detector lamp", V(DetectorPos.x, 3.2f, DetectorPos.z), 0.1f, lampMat, false);
        detLampMat = lampMat;
        // display window on the head's face, and an ID plate on the post
        P.Box(root, "Detector display", V(DetectorPos.x, 2.9f, DetectorPos.z - 0.127f), V(0.44f, 0.14f, 0.006f), P.Mat(new Color(0.02f, 0.03f, 0.03f), 0f, 0.8f), false);
        detText = P.Text(root, "GD-3  0 %LEL", V(DetectorPos.x, 2.9f, DetectorPos.z - 0.135f), 0.06f, Color.green, TextAnchor.MiddleCenter, true);
        var gd = Sign(root, "Detector plate", V(DetectorPos.x, 2.3f, DetectorPos.z - 0.06f), Vector3.back, new Vector2(0.95f, 0.22f), Navy, P.Gold, 0.1f);
        SignText(gd, "FIXED GAS DETECTOR", -0.01f, 0.065f, Color.white);
    }

    void BuildMuster(Transform root)
    {
        musterRoot = new GameObject("Muster point").transform;
        musterRoot.SetParent(root, false);
        var green = P.Mat(new Color(0.05f, 0.5f, 0.25f), 0.1f, 0.5f);
        P.Cyl(musterRoot, "Pad", V(0f, 0.03f, 0f), 3.2f, 0.06f, Vector3.up, concrete, false);
        P.Cyl(musterRoot, "Flag pole", V(-2f, 3f, 0f), 0.05f, 6f, Vector3.up, steel, false);
        musterFlag = new GameObject("Flag pivot").transform;
        musterFlag.SetParent(musterRoot, false);
        musterFlag.localPosition = V(-2f, 5.5f, 0f);
        P.Box(musterFlag, "Flag", V(0.7f, 0f, 0f), V(1.4f, 0.9f, 0.03f), green, false);
        P.Box(musterFlag, "Flag mark", V(0.7f, 0f, -0.02f), V(0.5f, 0.5f, 0.01f), P.Painted(Color.white), false);
        P.Box(musterRoot, "Sign post", V(-0.9f, 1f, 0f), V(0.08f, 2f, 0.08f), steel, false);
        P.Box(musterRoot, "Sign post", V(0.9f, 1f, 0f), V(0.08f, 2f, 0.08f), steel, false);
        // the sign face sits just in front of the posts; the check-in button is mounted on it
        Ui.Panel(musterRoot, "Sign panel", V(0f, 1.8f, -0.05f), new Vector2(2.2f, 1.1f), SafetyGreen, Color.white, 0.06f);
        SignBack(musterRoot, V(0f, 1.8f, -0.05f), new Vector2(2.2f, 1.1f));
        P.Text(musterRoot, "MUSTER POINT", V(0f, 2.1f, -0.065f), 0.18f, Color.white, TextAnchor.MiddleCenter, true);
        P.Text(musterRoot, "ASSEMBLY AREA 1", V(0f, 1.86f, -0.065f), 0.09f, Color.white, TextAnchor.MiddleCenter);
        var btn = P.Box(musterRoot, "Check-in button", V(0f, 1.5f, -0.07f), V(0.9f, 0.22f, 0.05f), P.Glow(P.Gold, 1.2f));
        P.Text(musterRoot, "CHECK IN", V(0f, 1.5f, -0.105f), 0.12f, Color.black, TextAnchor.MiddleCenter, true);
        var press = btn.AddComponent<Pressable>();
        press.Label = "CHECK IN";
        press.Pressed += h => CheckInPress();
    }

    void PlaceMuster()
    {
        if (!musterRoot) return;
        Vector3 p = Center - windDir * 26f;
        p.y = 0f;
        p.x = Mathf.Clamp(p.x, -29f, 29f);
        p.z = Mathf.Clamp(p.z, -21f, 29f);
        musterRoot.position = p;
        Vector3 away = Flat(p - Center);
        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
        musterRoot.rotation = Quaternion.LookRotation(away);
    }

    void BuildPlaceables(Transform root)
    {
        // Cones and tape at the barricade station
        // free-standing station sign behind the cone pallet, turned toward the gate side of the unit
        var bs = Sign(root, "Barricade station sign", V(ConePos.x, 1.3f, ConePos.z + 0.55f), V(0.6f, 0f, -1f), new Vector2(1.6f, 0.52f), Navy, SafetyYellow, 0.09f);
        P.Box(bs, "Post", V(-0.68f, -0.65f, 0.03f), V(0.05f, 1.3f, 0.05f), darkSteel, false);
        P.Box(bs, "Post", V(0.68f, -0.65f, 0.03f), V(0.05f, 1.3f, 0.05f), darkSteel, false);
        SignText(bs, "BARRICADE STATION", 0.07f, 0.09f, SafetyYellow);
        SignText(bs, "Cones: grab and place around a leak", -0.11f, 0.05f, Color.white, false);
        P.Box(root, "Station pallet", V(ConePos.x, 0.05f, ConePos.z), V(1.8f, 0.1f, 0.7f), P.Mat(new Color(0.55f, 0.42f, 0.28f), 0f, 0.25f), false);
        for (int i = 0; i < 4; i++) cones.Add(MakeCone(V(ConePos.x - 0.6f + i * 0.4f, 0.14f, ConePos.z)));
        var tapeGo = new GameObject("Barrier tape");
        tapeGo.transform.SetParent(root, false);
        tape = tapeGo.AddComponent<LineRenderer>();
        tape.positionCount = 0;
        tape.widthMultiplier = 0.045f;
        tape.material = new Material(Shader.Find("Sprites/Default"));
        tape.startColor = new Color(1f, 0.8f, 0.05f, 1f);
        tape.endColor = new Color(1f, 0.8f, 0.05f, 1f);
        tape.useWorldSpace = true;

        // The tempting tool bag
        var bag = new GameObject("Tool bag");
        bag.transform.position = BagPos;
        toolBag = bag.transform;
        var rb = bag.AddComponent<Rigidbody>();
        rb.mass = 3f;
        P.Box(toolBag, "Bag body", Vector3.zero, V(0.5f, 0.26f, 0.26f), P.Painted(new Color(0.9f, 0.4f, 0.05f)), false);
        P.Box(toolBag, "Bag handle", V(0f, 0.16f, 0f), V(0.25f, 0.04f, 0.04f), darkSteel, false);
        Ui.Tag(toolBag, "TOOL BAG", V(0f, 0.18f, 0f), V(0f, 0.4f, 0f), 0.065f, P.Gold);
        var bc = bag.AddComponent<BoxCollider>();
        bc.size = V(0.5f, 0.3f, 0.3f);
        bag.AddComponent<Grabbable>().Grabbed += g =>
        {
            if (task == 3 && !done) MistakeOnce("bag", "Never go back for belongings during an alarm.", true);
        };

        // Release point for the task 2 gas alarm (moved next to the player when it starts)
        release = new GameObject("Release point").transform;
        release.SetParent(root, false);
        P.Cyl(release, "Stub", V(0f, 0.5f, 0f), 0.08f, 1f, Vector3.up, steel, false);
        P.Cyl(release, "Stub flange", V(0f, 1f, 0f), 0.14f, 0.04f, Vector3.up, P.Mat(new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.75f), false);
        Ui.Tag(release, "SAMPLE POINT", V(0f, 1.02f, 0f), V(0f, 0.55f, 0f), 0.09f, Bad);
        release.gameObject.SetActive(false);
    }

    Rigidbody MakeCone(Vector3 pos)
    {
        var go = new GameObject("Cone");
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.6f;
        var orange = P.Painted(new Color(1f, 0.4f, 0.05f));
        P.Box(go.transform, "Cone base", V(0f, 0.015f, 0f), V(0.32f, 0.03f, 0.32f), orange, false);
        P.Cyl(go.transform, "Cone lower", V(0f, 0.1f, 0f), 0.12f, 0.14f, Vector3.up, orange, false);
        P.Cyl(go.transform, "Cone stripe", V(0f, 0.19f, 0f), 0.095f, 0.05f, Vector3.up, P.Painted(Color.white), false);
        P.Cyl(go.transform, "Cone upper", V(0f, 0.28f, 0f), 0.07f, 0.14f, Vector3.up, orange, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.size = V(0.32f, 0.4f, 0.32f);
        bc.center = V(0f, 0.2f, 0f);
        go.AddComponent<Grabbable>();
        return rb;
    }

    void BuildMarker(Transform root)
    {
        marker = new GameObject("Objective marker");
        marker.transform.SetParent(root, false);
        var ring = P.Cyl(marker.transform, "Ring", V(0f, 0.05f, 0f), 1.1f, 0.04f, Vector3.up, P.Glow(P.Gold, 1.1f), false);
        markerRing = ring.transform;
        P.Cyl(marker.transform, "Beam", V(0f, 3f, 0f), 0.04f, 6f, Vector3.up, P.Glow(P.Gold, 1.5f), false);
        marker.SetActive(false);
    }

    void BuildAudio(Transform root)
    {
        var go = new GameObject("Site siren");
        go.transform.SetParent(root, false);
        siren = P.Speaker(go.transform, MakeSiren(), true, 0.35f, 0f);
    }

    static AudioClip MakeSiren()
    {
        int rate = 22050, n = rate * 3;
        var data = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float f = 600f + 250f * Mathf.Sin(2f * Mathf.PI * t / 1.5f);
            ph += 2f * Mathf.PI * f / rate;
            data[i] = Mathf.Sin(ph) * 0.35f;
        }
        var clip = AudioClip.Create("siren", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void BuildRadio()
    {
        radio = new GameObject("Radio and monitor panel").transform;
        P.Box(radio, "Trim", V(0f, 0f, 0.012f), V(0.52f, 0.32f, 0.01f), P.Mat(P.Gold, 0.3f, 0.5f), false);
        P.Box(radio, "Body", Vector3.zero, V(0.5f, 0.3f, 0.02f), P.Mat(new Color(0.05f, 0.06f, 0.08f), 0.3f, 0.5f), false);
        monText = P.Text(radio, "MONITOR  0 %LEL", V(-0.22f, 0.12f, -0.015f), 0.026f, Color.green, TextAnchor.MiddleLeft, true);
        MakeRadioButton("ENTERING", V(-0.12f, 0.03f, 0f), 0);
        MakeRadioButton("WARN", V(0.12f, 0.03f, 0f), 1);
        MakeRadioButton("REPORT", V(-0.12f, -0.06f, 0f), 2);
        phoneLabel = MakeRadioButton("PHONE OFF", V(0.12f, -0.06f, 0f), 3);
        P.Text(radio, "RADIO", V(0.22f, 0.12f, -0.015f), 0.022f, P.Sky, TextAnchor.MiddleRight);
    }

    TextMesh MakeRadioButton(string label, Vector3 pos, int id)
    {
        var b = P.Box(radio, label + " button", pos, V(0.22f, 0.07f, 0.02f), P.Mat(new Color(0.12f, 0.28f, 0.6f), 0.2f, 0.5f));
        var p = b.AddComponent<Pressable>();
        p.Label = label;
        p.Pressed += h => RadioPress(id);
        return P.Text(radio, label, pos + V(0f, 0f, -0.016f), 0.026f, Color.white, TextAnchor.MiddleCenter, true);
    }

    // ------------------------------------------------------------------------------------------------- gas

    float PuffRadius(float age) { return 0.45f + 0.22f * age; }

    void Burst(Emitter e, float upTo)
    {
        for (float a = upTo; a >= 0f; a -= 0.7f) Spawn(e, a);
        e.Next = Time.time + 1f / e.Rate;
    }

    void Spawn(Emitter e, float age)
    {
        Puff pf = null;
        for (int i = 0; i < e.Pool.Count; i++) if (!e.Pool[i].Active) { pf = e.Pool[i]; break; }
        if (pf == null)
        {
            if (e.Pool.Count < MaxPuffs)
            {
                pf = new Puff();
                pf.M = P.Transparent(new Color(0.95f, 0.8f, 0.25f, 0.3f));
                var go = P.Ball(gasRoot, "Gas puff", Vector3.zero, 0.5f, pf.M, false);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pf.T = go.transform;
                e.Pool.Add(pf);
            }
            else
            {
                pf = e.Pool[0];
                for (int i = 1; i < e.Pool.Count; i++) if (e.Pool[i].Born < pf.Born) pf = e.Pool[i];
            }
        }
        pf.Active = true;
        pf.Born = Time.time - age;
        pf.Origin = e.Origin;
        pf.Jitter = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.08f), Random.Range(-0.3f, 0.3f));
        pf.T.gameObject.SetActive(true);
        pf.T.position = e.Origin;
    }

    void ClearGas()
    {
        foreach (var e in emitters)
        {
            e.On = false;
            foreach (var p in e.Pool) { p.Active = false; p.T.gameObject.SetActive(false); }
        }
    }

    void UpdateGas()
    {
        foreach (var e in emitters)
        {
            if (e.On && Time.time >= e.Next)
            {
                Spawn(e, 0f);
                e.Next = Time.time + 1f / e.Rate;
            }
            foreach (var p in e.Pool)
            {
                if (!p.Active) continue;
                float age = Time.time - p.Born;
                if (age > PuffLife) { p.Active = false; p.T.gameObject.SetActive(false); continue; }
                p.T.position = p.Origin + windDir * (drift * age) + p.Jitter * age + Vector3.up * (0.06f * age);
                p.T.localScale = Vector3.one * (PuffRadius(age) * 2f);
                p.M.color = new Color(0.95f, 0.8f, 0.25f, 0.32f * (1f - age / PuffLife));
            }
        }
    }

    /// <summary>Gas concentration (illustrative % LEL) at a point: the sum of the puffs that contain it.</summary>
    float ConcentrationAt(Vector3 point)
    {
        float c = 0f;
        foreach (var e in emitters)
            foreach (var p in e.Pool)
            {
                if (!p.Active) continue;
                float age = Time.time - p.Born;
                float r = PuffRadius(age);
                float d = Vector3.Distance(point, p.T.position);
                if (d < r) c += e.Peak * (1f - d / r) * Mathf.Clamp01(1f - age / PuffLife * 0.7f);
            }
        return c;
    }

    // ------------------------------------------------------------------------------------------------- per-frame visuals

    void Update()
    {
        if (rig == null) return;
        float dt = Time.deltaTime;
        UpdateGas();
        UpdateSock();
        UpdateFlare();
        UpdateMonitor(dt);
        UpdateDetector();
        UpdateAlarmFx();
        if (marker.activeSelf) markerRing.localScale = V(2.2f, 0.02f, 2.2f) * (1f + 0.08f * Mathf.Sin(Time.time * 4f));
        if (!shotMode) RunScenario(dt);
    }

    void UpdateSock()
    {
        float gust = 1f + 0.18f * (Mathf.PerlinNoise(Time.time * 0.4f, 1.7f) - 0.5f);
        float mph = weather.WindMph * gust;
        float stiff = Mathf.Clamp01(mph / 16f);
        float yawJit = (Mathf.PerlinNoise(Time.time * 0.3f, 8.1f) - 0.5f) * 16f;
        Vector3 dir = Quaternion.Euler(0f, yawJit, 0f) * windDir;
        sockPivot.rotation = Quaternion.LookRotation(dir);
        float droopPer = (1f - stiff) * 85f / sockJoints.Count;
        for (int i = 0; i < sockJoints.Count; i++)
            sockJoints[i].localRotation = Quaternion.Euler(droopPer + Mathf.Sin(Time.time * 7f + i * 0.9f) * 4f * stiff, Mathf.Sin(Time.time * 5f + i) * 3f * stiff, 0f);
        musterFlag.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 3f) * 8f, 0f);
    }

    void UpdateFlare()
    {
        float f = 0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 9f, 0.3f);
        float bend = Mathf.Clamp(weather.WindMph / 20f, 0.2f, 1.2f);
        flameRoot.rotation = Quaternion.FromToRotation(Vector3.up, (Vector3.up + windDir * bend).normalized);
        flameRoot.localScale = V(1f, f, 1f);
        flameLight.intensity = 2f * f;
    }

    void UpdateMonitor(float dt)
    {
        float c = monitorForce >= 0f ? monitorForce : ConcentrationAt(monitor.position);
        monReading = Mathf.Lerp(monReading, c, 1f - Mathf.Exp(-6f * dt));
        bool low = monReading >= 10f, high = monReading >= 20f;
        string state = bumpRunning ? "BUMP..." : high ? "HIGH ALARM" : low ? "ALARM" : bumped ? "OK" : "NOT TESTED";
        Color col = high || low ? Bad : bumped ? Good : Warn;
        monScreen.text = $"{monReading:F0}\n%LEL";
        monScreen.color = col;
        monLampMat.color = col * 0.3f;
        monLampMat.SetColor("_EmissionColor", col * (low && Mathf.Repeat(Time.time, 0.4f) < 0.2f ? 0.2f : 1.4f));
        if (monText)
        {
            monText.text = $"MONITOR  {monReading:F0} %LEL  {state}";
            monText.color = col;
        }
        if (low && !shotMode)
        {
            beepTimer -= dt;
            if (beepTimer <= 0f) { beepTimer = high ? 0.18f : 0.45f; monAudio.Play(); }
        }
    }

    void UpdateDetector()
    {
        float c = ConcentrationAt(DetectorPos + V(0f, 2.9f, 0f));
        bool alarm = c >= 10f;
        detText.text = $"GD-3  {c:F0} %LEL";
        detText.color = alarm ? Bad : Good;
        Color lc = alarm ? Color.red : Color.green;
        detLampMat.color = lc * 0.3f;
        detLampMat.SetColor("_EmissionColor", lc * 1.4f);
    }

    void UpdateAlarmFx()
    {
        bool flash = alarmActive && Mathf.Repeat(Time.time, 0.8f) < 0.4f;
        cabinBeaconLight.enabled = flash;
        cabinLampMat.SetColor("_EmissionColor", flash ? new Color(1f, 0.1f, 0.05f) * 2.2f : Color.black);
    }

    void SetMarker(Vector3 pos, string label)
    {
        marker.transform.position = V(pos.x, 0f, pos.z);
        if (markerText != label)
        {
            // the tag is sized to its text, so a new label means a new tag
            if (markerTag) Destroy(markerTag.gameObject);
            markerTag = Ui.Tag(marker.transform, label, V(0f, 2.4f, 0f), V(0f, 1.1f, 0f), 0.16f, P.Gold);
            markerText = label;
        }
        marker.SetActive(true);
    }

    void HideMarker() { marker.SetActive(false); }

    void LateUpdate()
    {
        if (rig == null || shotMode) return;
        var cam = rig.Camera.transform;
        Vector3 fwd = Flat(cam.forward);
        if (fwd.sqrMagnitude < 0.05f) fwd = lastFwd;
        else { fwd.Normalize(); lastFwd = fwd; }
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        // chest-mounted radio panel
        Vector3 rp = cam.position + fwd * 0.55f - Vector3.up * 0.42f;
        radio.position = rp;
        radio.rotation = Quaternion.LookRotation(rp - cam.position);

        // clipped gas monitor on the chest
        if (monitorClipped && !monitorHeld)
        {
            monitorRb.isKinematic = true;
            monitor.position = cam.position + fwd * 0.45f + right * 0.34f - Vector3.up * 0.32f;
            monitor.rotation = Quaternion.LookRotation(monitor.position - cam.position);
        }

        // comfort-follow task board
        Vector3 to = boardAnchor.position - cam.position;
        float angle = Vector3.Angle(cam.forward, to);
        if (angle > 50f || to.magnitude > 5.5f || to.magnitude < 1.8f) boardMoving = true;
        Vector3 desired = cam.position + fwd * BoardAhead + right * BoardRight;
        desired.y = cam.position.y + 0.65f;
        if (boardMoving)
        {
            boardAnchor.position = Vector3.Lerp(boardAnchor.position, desired, 1f - Mathf.Exp(-3f * Time.deltaTime));
            if (Vector3.Distance(boardAnchor.position, desired) < 0.2f) boardMoving = false;
        }
        Vector3 face = Flat(boardAnchor.position - cam.position);
        if (face.sqrMagnitude > 0.01f) boardAnchor.rotation = Quaternion.LookRotation(face);
    }

    void SnapBoardToView()
    {
        var cam = rig.Camera.transform;
        Vector3 fwd = Flat(cam.forward).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 p = cam.position + fwd * BoardAhead + right * BoardRight;
        p.y = cam.position.y + 0.65f;
        boardAnchor.position = p;
        boardAnchor.rotation = Quaternion.LookRotation(Flat(p - cam.position));
    }

    // ------------------------------------------------------------------------------------------------- tasks and rules

    void StartTask(int t)
    {
        task = t;
        boardTask = t;
        if (board) Destroy(board.gameObject);
        board = TaskBoard.Create(boardAnchor, boardAnchor.position, boardAnchor.rotation, TaskTitles[t], TaskSubs[t], TaskSteps[t], 3.0f, 1.0f);
        RefreshFooter();
    }

    void RefreshFooter()
    {
        board.SetFooter($"Total mistakes: {totalMistakes}   Critical: {totalCritical}");
    }

    /// <summary>Completes a step of the current board and shows why it matters.</summary>
    void Done(int step, string message)
    {
        board.Complete(step, message);
        why.Show(StepWhy[boardTask][step]);
    }

    void Mistake(string msg, bool critical = false)
    {
        totalMistakes++;
        if (critical) totalCritical++;
        board.Mistake(msg, critical);
        RefreshFooter();
    }

    void MistakeOnce(string key, string msg, bool critical = false)
    {
        if (flagged.Add(key)) Mistake(msg, critical);
    }

    string FinalSummary()
    {
        if (totalCritical > 0) return $"CRITICAL SAFETY MISS ({totalCritical}). Review before real work.";
        if (totalMistakes == 0) return "STRONG PRACTICE: no mistakes.";
        return $"Complete with {totalMistakes} mistake{(totalMistakes == 1 ? "" : "s")}.";
    }

    bool Carrying { get { return monitorHeld || monitorClipped; } }

    bool Gazing(Vector3 target, float maxAngle, float maxDist)
    {
        var cam = rig.Camera.transform;
        Vector3 to = target - cam.position;
        return to.magnitude <= maxDist && Vector3.Angle(cam.forward, to) <= maxAngle;
    }

    void RunScenario(float dt)
    {
        if (done) return;
        switch (task)
        {
            case 0: RunPrepare(dt); break;
            case 1: RunGasAlarm(dt); break;
            case 2: RunWeep(dt); break;
            case 3: RunEmergency(dt); break;
        }
    }

    // ---- task 1
    void RunPrepare(float dt)
    {
        var feet = rig.FeetPosition;
        if (!board.IsDone(0))
        {
            bool g = Gazing(sockPivot.position, 22f, 60f);
            gazeWind = g ? gazeWind + dt : 0f;
            if (g && !weatherReady) board.Say("Reading the wind... one moment.", Color.white);
            if (gazeWind > 1.2f && weatherReady)
                Done(0, $"Wind from {Compass(weather.WindFromDegrees)} at {weather.WindMph:F0} mph. Stay upwind.");
        }
        if (!board.IsDone(1))
        {
            bool g = Gazing(musterRoot.position + V(0f, 4f, 0f), 22f, 70f);
            gazeMuster = g ? gazeMuster + dt : 0f;
            if (gazeMuster > 1.2f) Done(1, "Muster point noted: upwind side of the unit.");
        }

        bool inside = Inside(feet);
        if (inside && !wasInside) OnEnteredUnit(feet);
        wasInside = inside;

        if (board.IsDone(0) && board.IsDone(1) && board.IsDone(2) && board.IsDone(3) && inside && !board.IsDone(4))
        {
            if (!Carrying) MistakeOnce("nomonitor", "You entered without your gas monitor.", true);
            Done(4, "Entry logged. Next: walk to gauge G-1.");
            task = 1;
            phase = 0;
            phaseTimer = 0f;
            SetMarker(GaugePos, "ROUND STOP G-1");
        }
    }

    void OnEnteredUnit(Vector3 feet)
    {
        bool viaGate = Mathf.Abs(feet.x) <= GateHalf + 0.6f && feet.z < FenceMinZ + 2f;
        if (!viaGate) MistakeOnce("gate", "Enter through the gate, not across the fence line.");
        bool prepared = board.IsDone(0) && board.IsDone(1) && board.IsDone(2) && board.IsDone(3);
        if (!prepared) MistakeOnce("early", "Entered before finishing your checks. Hidden hazards disable fast.", true);
    }

    void BumpPress()
    {
        if (shotMode || bumpRunning) return;
        if (bumped) { if (task == 0) board.Say("Bump test already passed.", Good); return; }
        float reach = XRRig.Headset ? 0.8f : 2.6f;
        bool near = monitorHeld && Vector3.Distance(monitor.position, bumpPos) < reach;
        if (!near)
        {
            if (task == 0) board.Say("Pick up the monitor (grip / E) and hold it at the station.", Warn);
            return;
        }
        StartCoroutine(BumpRoutine());
    }

    IEnumerator BumpRoutine()
    {
        bumpRunning = true;
        if (task == 0) board.Say("Bump test: applying test gas...", Color.white);
        yield return new WaitForSeconds(2.5f);
        bumpRunning = false;
        bumped = true;
        if (task == 0) Done(2, "Bump test passed. Keep the monitor with you.");
    }

    // ---- task 2
    void RunGasAlarm(float dt)
    {
        var feet = rig.FeetPosition;
        if (phase == 0)
        {
            if (FlatDist(feet, GaugePos) < 2.2f)
            {
                phaseTimer += dt;
                if (phaseTimer > 1f) BeginAlarm(feet);
            }
            else phaseTimer = 0f;
            return;
        }

        float el = Time.time - alarmStartTime;
        float dS = FlatDist(feet, releasePos);
        float conc = ConcentrationAt(feet + V(0f, 1.2f, 0f));
        if (el > 1.5f && dS < 2.2f) MistakeOnce("toward", "Walking toward the source risks exposure. Move away.", true);
        if (!board.IsDone(0) && dS > 10f && conc < 3f) Done(0, "Clean air, away from the release.");
        if (!board.IsDone(0) && el > 30f) MistakeOnce("delay", "Delay: get to clean air before anything else.");
        if (board.IsDone(2))
        {
            phaseTimer += dt;
            if (phaseTimer > 2.5f)
            {
                emitA.On = false;
                release.gameObject.SetActive(false);
                task = 2;
                phase = 0;
                board.Say("Task 2 complete. Next: walk to flange F-12.", Good);
                SetMarker(V(FlangePos.x, 0f, FlangePos.z), "FLANGE F-12");
            }
        }
    }

    void BeginAlarm(Vector3 feet)
    {
        phase = 1;
        phaseTimer = 0f;
        HideMarker();
        releasePos = V(feet.x - windDir.x * 4f, 1f, feet.z - windDir.z * 4f);
        release.position = V(releasePos.x, 0f, releasePos.z);
        release.gameObject.SetActive(true);
        emitA.Origin = releasePos;
        emitA.On = true;
        Burst(emitA, 5f);
        alarmStartTime = Time.time;
        StartTask(1);
        board.Say("ALARM: your monitor reads gas. Act now.", Bad);
    }

    // ---- task 3
    void RunWeep(float dt)
    {
        var feet = rig.FeetPosition;
        float dF = FlatDist(feet, FlangePos);
        if (phase == 0)
        {
            if (dF < 9f) BeginWeep();
            return;
        }
        if (dF < 2.4f)
        {
            if (!phoneOff) MistakeOnce("ignite", "A phone is an ignition source near the leak.", true);
            else MistakeOnce("close", "Too close to the leak. Stay back.");
        }
        if (!board.IsDone(0) && phoneOff && dF >= 3.5f) Done(0, "Clear of the leak and your phone is off.");
        evalTimer += dt;
        if (evalTimer > 0.25f)
        {
            evalTimer = 0f;
            if (EvaluateBarricade() && board.IsDone(0) && !board.IsDone(1)) Done(1, "Barricade set around the leak.");
        }
        if (board.IsDone(2))
        {
            phaseTimer += dt;
            if (phaseTimer > 2.5f)
            {
                emitW.On = false;
                StartTask(3);
                BeginEmergency();
            }
        }
    }

    void BeginWeep()
    {
        phase = 1;
        phaseTimer = 0f;
        HideMarker();
        weepVisual.SetActive(true);
        emitW.Origin = FlangePos;
        emitW.On = true;
        Burst(emitW, 3f);
        StartTask(2);
        board.Say("You smell hydrocarbon: a weep at flange F-12.", Warn);
    }

    bool EvaluateBarricade()
    {
        placedTmp.Clear();
        foreach (var rb in cones)
        {
            if (rb.transform.parent != null || rb.isKinematic) continue;
            if (rb.linearVelocity.sqrMagnitude > 0.04f) continue;
            Vector3 p = rb.position;
            if (p.y > 0.4f) continue;
            float d = FlatDist(p, FlangePos);
            if (d < 2f || d > 7f) continue;
            bool apart = true;
            foreach (var q in placedTmp) if (FlatDist(p, q.Pos) < 1.5f) apart = false;
            if (!apart) continue;
            placedTmp.Add(new Placed { Pos = p, A = Mathf.Atan2(p.z - FlangePos.z, p.x - FlangePos.x) * Mathf.Rad2Deg });
        }
        placedTmp.Sort((x, y) => x.A.CompareTo(y.A));
        int n = placedTmp.Count;
        tape.loop = n >= 3;
        tape.positionCount = n >= 2 ? n : 0;
        if (n >= 2) for (int i = 0; i < n; i++) tape.SetPosition(i, placedTmp[i].Pos + Vector3.up * 0.3f);
        if (n < 3) return false;
        float maxGap = 0f;
        for (int i = 0; i < n; i++)
        {
            float next = i == n - 1 ? placedTmp[0].A + 360f : placedTmp[i + 1].A;
            maxGap = Mathf.Max(maxGap, next - placedTmp[i].A);
        }
        return maxGap <= 230f;
    }

    // ---- task 4
    void BeginEmergency()
    {
        phase = 1;
        phaseTimer = 0f;
        exposure = 0f;
        hintGiven = false;
        emergencyStart = Time.time;
        emitE.Origin = V(Center.x, 1.2f, Center.z);
        emitE.On = true;
        Burst(emitE, 6f);
        alarmActive = true;
        siren.Play();
        board.Say("SITE ALARM! Go to the muster point now.", Bad);
    }

    void RunEmergency(float dt)
    {
        var feet = rig.FeetPosition;
        float el = Time.time - emergencyStart;
        bool atMuster = FlatDist(feet, musterRoot.position) < 3.5f;
        if (!board.IsDone(0))
        {
            if (atMuster) Done(0, "At the muster point by an upwind route.");
            if (ConcentrationAt(feet + V(0f, 1.2f, 0f)) > 6f) exposure += dt;
            if (exposure > 2f) MistakeOnce("plume", "You walked through the gas cloud. Route around it.");
            if (FlatDist(feet, toolBag.position) < 1f) MistakeOnce("bag", "Never go back for belongings during an alarm.", true);
            if (el > 12f && !hintGiven) { hintGiven = true; board.Say("Look for the green muster flag, upwind of the release.", Warn); }
            if (el > 45f) MistakeOnce("wait", "Waiting leaves you unaccounted for. Go now.");
        }
    }

    void CheckInPress()
    {
        if (shotMode || task != 3 || done) return;
        if (!board.IsDone(0)) { board.Say("Walk to the muster point first.", Warn); return; }
        if (board.IsDone(1)) return;
        FinishScenario();
    }

    /// <summary>Last step done: show the procedure summary, then ask the rationale question (scored apart).</summary>
    void FinishScenario()
    {
        Done(1, "Checked in. Everyone is accounted for.");
        done = true;
        alarmActive = false;
        siren.Stop();
        emitE.On = false;
        board.Say(FinalSummary(), totalCritical > 0 ? Bad : Good);
        var finalBoard = board;
        why.Ask(Question, best => { if (finalBoard) finalBoard.SetResultLine(why.ReasoningLine, why.ReasoningColor); });
        board.SetResultLine(why.ReasoningLine, why.ReasoningColor);
    }

    void RadioPress(int id)
    {
        if (shotMode || done) return;
        switch (id)
        {
            case 0: // ENTERING
                if (task == 0 && !board.IsDone(3))
                {
                    if (board.IsDone(0) && board.IsDone(1) && board.IsDone(2)) Done(3, "Control room logs your entry.");
                    else board.Say("Finish your checks first: wind, muster point, monitor.", Warn);
                }
                break;
            case 1: // WARN
                if (task == 1 && phase == 1 && !board.IsDone(1))
                {
                    if (board.IsDone(0)) Done(1, "Nearby workers warned and moving away.");
                    else board.Say("Get to clean air first, then use the radio.", Warn);
                }
                break;
            case 2: // REPORT
                if (task == 1 && phase == 1 && !board.IsDone(2))
                {
                    if (board.IsDone(1)) Done(2, "Control room starts the gas release response.");
                    else board.Say("Warn nearby workers first, then report.", Warn);
                }
                else if (task == 2 && phase == 1 && !board.IsDone(2))
                {
                    if (board.IsDone(1)) Done(2, "Leak reported for authorized repair.");
                    else board.Say("Barricade the area first (3 cones), then report.", Warn);
                }
                break;
            case 3: // PHONE OFF
                phoneOff = true;
                phoneLabel.text = "PHONE OFF: OK";
                board.Say("Phone off: no ignition source on you.", Good);
                break;
        }
    }

    // ------------------------------------------------------------------------------------------------- screenshots

    void PlaceBoardFor(Vector3 camPos, Vector3 look)
    {
        Vector3 fwd = Flat(look - camPos);
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        Vector3 pos = camPos + fwd * 4.6f - right * 2.2f;
        // follow the camera's pitch so the board stays in frame when the shot looks down
        pos.y = camPos.y + (look - camPos).normalized.y * 4.6f + 0.55f;
        boardAnchor.position = pos;
        boardAnchor.rotation = Quaternion.LookRotation(Flat(pos - camPos));
    }

    /// <summary>
    /// Name tags turn toward Camera.main (the rig), but the screenshots use a separate capture camera, so turn the tags
    /// toward the shot position instead and keep their leader lines attached.
    /// </summary>
    static void AimTagsAt(Vector3 camPos)
    {
        foreach (var tf in FindObjectsByType<TagFacing>(FindObjectsSortMode.None))
        {
            tf.enabled = false;
            Vector3 away = tf.Tag.position - camPos;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f) tf.Tag.rotation = Quaternion.LookRotation(away, Vector3.up);
            tf.Line.SetPosition(0, tf.Tag.position - Vector3.up * 0.04f);
            tf.Line.SetPosition(1, tf.Anchor.TransformPoint(tf.AnchorLocal));
        }
    }

    /// <summary>
    /// Takes one screenshot. When an XR runtime is active the capture camera (copied from the rig camera) would be
    /// driven by the headset pose and refuse a field of view, so it is switched to a plain mono camera first. The view
    /// is also pre-rendered until the editor has finished compiling shaders, so nothing shows the cyan placeholder.
    /// </summary>
    IEnumerator Snap(ShotApi api, string name, Vector3 pos, Vector3 look, float fov)
    {
        AimTagsAt(pos);
        var capGo = GameObject.Find("Capture Camera");
        var cap = capGo ? capGo.GetComponent<Camera>() : null;
        if (cap != null)
        {
            cap.stereoTargetEye = StereoTargetEyeMask.None;
            cap.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
            cap.fieldOfView = fov;
            var rt = new RenderTexture(640, 360, 24);
            cap.targetTexture = rt;
            float t0 = Time.realtimeSinceStartup;
            for (int i = 0; i < 400; i++)
            {
                cap.Render();
                yield return null;
#if UNITY_EDITOR
                if (i >= 3 && !UnityEditor.ShaderUtil.anythingCompiling) break;
#else
                if (i >= 3) break;
#endif
                if (Time.realtimeSinceStartup - t0 > 20f) break;
            }
            cap.targetTexture = null;
            Destroy(rt);
        }
        yield return api.Shot(name, pos, look, fov);
    }

    public IEnumerator Shots(ShotApi api)
    {
        float waited = 0f;
        while (!weatherReady && waited < 9f) { waited += Time.deltaTime; yield return null; }
        shotMode = true;
        why.Muted = true; // staged views below; the why panels get their own shots at the end
        rig.LocomotionEnabled = false;
        radio.gameObject.SetActive(false);
        Vector3 w = windDir;
        Vector3 side = Vector3.Cross(Vector3.up, w);

        // 1 overview
        yield return Snap(api, "01-overview", V(-26f, 16f, -26f), V(2f, 3f, 6f), 62f);

        // 2 wind sock and conditions board
        yield return Snap(api, "02-wind-sock-and-board", V(-0.5f, 2.3f, -16.5f), V(0.5f, 3.2f, -9f), 72f);

        // 3 gas alarm cloud
        Vector3 s = V(2f, 1f, 0.5f);
        releasePos = s;
        release.position = V(s.x, 0f, s.z);
        release.gameObject.SetActive(true);
        emitA.Origin = s;
        emitA.On = true;
        Burst(emitA, 9f);
        Vector3 camPos = s + side * 8f + Vector3.up * 2.4f;
        Vector3 look = s + w * 5f + Vector3.up * 1.2f;
        monitorClipped = false;
        monitorForce = 42f;
        monitorRb.isKinematic = true;
        monitor.position = camPos + (look - camPos).normalized * 2.2f + Vector3.down * 0.35f;
        monitor.rotation = Quaternion.LookRotation(monitor.position - camPos);
        StartTask(1);
        board.Say("ALARM: your monitor reads gas. Act now.", Bad);
        PlaceBoardFor(camPos, look);
        yield return api.Wait(1.2f);
        yield return Snap(api, "03-gas-alarm-cloud", camPos, look, 62f);

        // 4 weep and barricade
        ClearGas();
        release.gameObject.SetActive(false);
        monitorForce = -1f;
        weepVisual.SetActive(true);
        emitW.Origin = FlangePos;
        emitW.On = true;
        Burst(emitW, 6f);
        phoneOff = true;
        StartTask(2);
        phase = 1;
        board.Complete(0, "Clear of the leak and your phone is off.");
        float[] angs = { 20f, 130f, 250f, 320f };
        for (int i = 0; i < cones.Count && i < angs.Length; i++)
        {
            float a = angs[i] * Mathf.Deg2Rad;
            cones[i].isKinematic = false;
            cones[i].position = FlangePos + V(Mathf.Cos(a) * 3.8f, 0f, Mathf.Sin(a) * 3.8f) - V(0f, FlangePos.y - 0.05f, 0f);
            cones[i].linearVelocity = Vector3.zero;
        }
        yield return api.Wait(2.0f);
        if (EvaluateBarricade()) board.Complete(1, "Barricade set around the leak.");
        camPos = FlangePos + V(-1f, 3.2f, -10f);
        look = FlangePos + V(0.5f, -0.8f, 0f);
        PlaceBoardFor(camPos, look);
        yield return api.Wait(0.5f);
        yield return Snap(api, "04-weep-and-barricade", camPos, look, 58f);

        // 5 muster point during the site alarm
        ClearGas();
        StartTask(3);
        BeginEmergency();
        board.Complete(0, "At the muster point by an upwind route.");
        Vector3 mp = musterRoot.position;
        Vector3 away = Flat(mp - Center).normalized;
        camPos = mp - away * 10f + Vector3.Cross(Vector3.up, away) * 3f + Vector3.up * 2.4f;
        look = mp + V(0f, 2.2f, 0f);
        PlaceBoardFor(camPos, look);
        yield return api.Wait(1.0f);
        yield return Snap(api, "05-muster-point", camPos, look, 58f);

        // 6 critical miss: going back for the tool bag
        totalMistakes = 0;
        totalCritical = 0;
        StartTask(3);
        board.Mistake("Going back for tools puts you in the gas. Go to the muster point.", true);
        totalMistakes = 1;
        totalCritical = 1;
        RefreshFooter();
        // from upwind of the bag, so the drifting release is beyond it and not in the lens
        camPos = BagPos - w * 5f + side * 1.5f + Vector3.up * 1.6f;
        look = BagPos + V(0f, 0.6f, 0f);
        PlaceBoardFor(camPos, look);
        yield return api.Wait(0.5f);
        yield return Snap(api, "06-critical-miss", camPos, look, 58f);

        // 7 the player's view at the start: pre-entry station, gate, conditions board, university plate
        ClearGas();
        alarmActive = false;
        siren.Stop();
        totalMistakes = 0;
        totalCritical = 0;
        StartTask(0);
        camPos = V(0f, 1.7f, -14f);
        look = V(0f, 1.6f, -8f);
        SnapBoardToView(); // where the board sits at the real start (the rig has not moved)
        yield return api.Wait(0.3f);
        yield return Snap(api, "07-start-view", camPos, look, 70f);

        // 8 close-up of the gate: entry sign and the university plate on the lintel
        board.gameObject.SetActive(false);
        yield return Snap(api, "08-gate-signage", V(-0.6f, 1.7f, -9.6f), V(0f, 2.65f, FenceMinZ), 50f);

        // 9 equipment labels inside the unit: ID plates on the vessels and the flange name tag
        yield return Snap(api, "09-equipment-labels", V(-10.2f, 1.75f, 0.2f), V(-6.2f, 1.9f, 7.5f), 60f);
        board.gameObject.SetActive(true);

        // 10 "why this matters" after the bump test at the pre-entry station
        StartTask(0);
        board.Complete(0, "Wind noted.");
        board.Complete(1, "Muster point noted: upwind side of the unit.");
        monitorRb.isKinematic = true;
        monitor.position = V(TablePos.x - 0.45f, 0.97f, TablePos.z);
        monitor.rotation = Quaternion.identity;
        bumped = true;
        camPos = V(TablePos.x - 0.4f, 1.7f, TablePos.z - 2.6f);
        look = V(TablePos.x + 0.3f, 1.15f, TablePos.z);
        // board to the right, as in play (the left side has sign posts in the way from here)
        Vector3 f10 = Flat(look - camPos).normalized, r10 = Vector3.Cross(Vector3.up, f10);
        boardAnchor.position = camPos + f10 * 4.4f + r10 * 2.3f + Vector3.up * 0.45f;
        boardAnchor.rotation = Quaternion.LookRotation(Flat(boardAnchor.position - camPos));
        why.Muted = false;
        Done(2, "Bump test passed. Keep the monitor with you.");
        yield return Snap(api, "10-why-this-matters", camPos, look, 62f);
        why.HideReason();

        // 11-13 the end: check-in at the muster point, then the rationale question and the summary
        ClearGas();
        totalMistakes = 0;
        totalCritical = 0;
        StartTask(3);
        board.Complete(0, "At the muster point by an upwind route.");
        why.Muted = true; // open the question at once instead of after the check-in reason
        FinishScenario();
        why.Muted = false;
        Debug.Log($"[why] scripted pass: done = {done}, question open = {why.Asking}");
        mp = musterRoot.position;
        away = Flat(mp - Center).normalized;
        camPos = mp - away * 7f + Vector3.up * 1.7f;
        look = mp + V(0f, 1.6f, 0f);
        PlaceBoardFor(camPos, look);
        why.AskOffset = new Vector2(0.45f, -0.08f);
        yield return Snap(api, "11-rationale-question", camPos, look, 62f);
        why.Answer(2);
        yield return Snap(api, "12-rationale-feedback", camPos, look, 62f);
        why.HideAll();
        Debug.Log("[why] board summary: " + FinalSummary() + " | " + why.ReasoningLine);
        yield return Snap(api, "13-summary-with-reasoning", camPos, boardAnchor.position + V(0f, -0.55f, 0f), 50f);
    }
}
