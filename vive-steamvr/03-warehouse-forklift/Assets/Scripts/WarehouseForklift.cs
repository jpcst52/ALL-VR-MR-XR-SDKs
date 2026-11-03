using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Warehouse and forklift pedestrian safety practice. The player puts on PPE at the entry station, checks the corner
/// mirror, signals the forklift driver and crosses at the marked crossing, then cones a spill and a damaged rack
/// upright and radios the supervisor. Stepping into a forklift lane while the forklift is within about 6 m is a
/// critical miss. General safety-awareness practice only: it is not a certification or a site procedure.
/// Optional (inspector, on by default): a co-worker walks past the forklift while it puts a pallet away, with a
/// pause-and-ask awareness check; see WarehouseForklift.Awareness.cs.
/// </summary>
public partial class WarehouseForklift : MonoBehaviour, IShotScript
{
    enum Zone { Entry, Walk, Crossing, Lane, Floor }

    class Gear
    {
        public Grabbable G;
        public Collider[] Cols;
        public bool Off;
    }

    const float EntryZ = -6f;
    const float NearRange = 6f;

    // Floor layout (x east, z north). The forklift loops counter-clockwise around the racking.
    static readonly Rect CrossingRect = new Rect(-4.9f, -1.6f, 1.8f, 3.2f);
    static readonly Rect[] LaneRects =
    {
        new Rect(-11.6f, -1.6f, 23.2f, 3.2f),  // south lane
        new Rect(8.4f, -1.6f, 3.2f, 15.2f),    // east lane
        new Rect(-11.6f, 10.4f, 23.2f, 3.2f),  // north lane
        new Rect(-11.6f, -1.6f, 3.2f, 15.2f),  // west lane
    };
    static readonly Rect[] WalkRects =
    {
        new Rect(-4.9f, -6f, 1.8f, 4.4f),      // entry walkway, south of the crossing
        new Rect(-4.9f, 1.6f, 1.8f, 8.2f),     // interior walkway, north of the crossing
        new Rect(-4.9f, 4.6f, 7.2f, 1.8f),     // cross-aisle spur to the hazards
    };

    static readonly Vector3 SpillPos = new Vector3(-2.0f, 0f, 5.5f);
    static readonly Vector3 UprightPos = new Vector3(2.5f, 0f, 6.0f);
    const float MarkRadius = 2.0f;

    // scene objects
    XRRig rig;
    TaskBoard board;
    Forklift forklift;
    Transform body;
    SnapZone hatZone, vestZone, glassesZone;
    TextMesh ppeText, radioText;
    Camera mirrorCam;
    RenderTexture mirrorRt;
    Material mirrorLampMat, flashMat;
    GameObject flashRoot;
    TextMesh flashText;
    Vector3 mirrorCenter, mirrorNormal;
    readonly List<Gear> gear = new List<Gear>();
    readonly List<Grabbable> cones = new List<Grabbable>();
    readonly Dictionary<string, float> nextAllowed = new Dictionary<string, float>();

    // cached materials (P.Steel and friends build textures on every call)
    Material steel, darkSteel, hazard, concrete;

    // "Why this matters": one plain-language reason per step, shown after the step is done (general safety awareness,
    // not a procedure). The scenario summary already ends with the awareness check, so there is no extra question here.
    static readonly string[] StepWhy =
    {
        "A hard hat, vest and glasses guard your head and eyes and make you easy for drivers to see. They only help if they are on before you step onto the floor.",
        "A corner mirror shows traffic you cannot see around the bend. One second of looking costs less than walking out blind.",
        "Eye contact and a full stop show the driver has seen you, and the stripes tell drivers where to expect walkers. The driver also has to stop and sound the horn at crossings, so the safety is shared.",
        "A spill and a damaged rack are hazards for the next person. Cones keep others away until authorized crews fix them, and your radio call starts that.",
    };

    WhyCue why;

    /// <summary>Completes a step of the board and shows why it matters (not while screenshots are being staged).</summary>
    void Done(int step, string message)
    {
        board.Complete(step, message);
        if (why != null && !staging) why.Show(StepWhy[step]);
    }

    // state
    Zone prevZone = Zone.Entry;
    bool baking; // set in Start: this Play is the editor's bake session (see BakeSession)
    bool staging, finished, allPpe, mirrorChecked, crossingValid;
    bool spillMarked, uprightMarked, spillSpotted, uprightSpotted;
    float floorTimer, mirrorLook, lookDriver, lookSpill, lookUpright, flashT;
    float lastLookAt = -999f, ackUntil = -999f, nextNear;
    string ppeKey = "";

    // ------------------------------------------------------------------------------------------------- setup

    void Start()
    {
        baking = BakeSession;
        QualitySettings.shadows = ShadowQuality.All;
        // ceiling / wall / floor ambient: brighter from the lit roof, darker bounce from the concrete
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.5f, 0.52f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.43f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.26f, 0.26f, 0.27f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.5f, 0.53f, 0.57f);
        RenderSettings.fogStartDistance = 28f;
        RenderSettings.fogEndDistance = 70f;

        steel = P.Steel();
        darkSteel = P.DarkSteel();
        hazard = P.Hazard();
        concrete = P.Concrete();

        // Every world object is made through the kit helpers (P.Node, P.Box, P.Text, Ui.Panel ...). In a baked scene
        // (Pitt-Bradford > Bake scene) they return the saved objects instead, matched by parent, name and call order,
        // and the code below re-attaches handlers and fields. Do not rename baked objects that this script looks up.
        // Runtime-only (never baked): the player body and PPE zones, the XR rig, task board, near-miss flash, mirror
        // camera, the forklift's speech bubbles, the co-worker, and the awareness-check / prompt / summary panels.
        body = new GameObject("Player body").transform;

        BuildShell();
        BuildMarkings();
        BuildRacking();
        BuildCargoBlock();
        BuildDock();
        BuildPpeStation();
        BuildMirror();
        BuildSignalStation();
        BuildHazards();
        BuildCones();
        BuildRadio();

        var route = new[]
        {
            new Vector3(-10f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 12f), new Vector3(-10f, 0f, 12f),
        };
        forklift = Forklift.Build(transform, route, new Vector3(-4f, 0f, 0f));
        BuildCoworkerEvent(); // optional co-worker hazard and awareness check (WarehouseForklift.Awareness.cs)

        rig = XRRig.Create(new Vector3(-9f, 0f, -10.6f), 0f);
        rig.Area = new Bounds(new Vector3(0, 0, 2f), new Vector3(27.2f, 1f, 27.2f));
        rig.Camera.backgroundColor = new Color(0.1f, 0.11f, 0.13f);
        rig.Camera.clearFlags = CameraClearFlags.SolidColor;

        BuildFlash();
        BuildBoard();
        BuildBrand();
        why = WhyCue.Create(transform);
    }

    Transform NewRoot(string name) => P.Node(name, transform).transform;

    /// <summary>
    /// True while the editor's Bake scene tool is running Start to capture the world. Things that must happen only in
    /// real play (merging meshes for static batching, clearing rack cartons for the loading event) are skipped then, so
    /// the scene file keeps the plain objects and Start does those steps on them at every Play.
    /// </summary>
    static bool BakeSession
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetString(Bake.StageKey, "") == "play";
#else
            return false;
#endif
        }
    }

    /// <summary>Static batching merges meshes at runtime; never during a bake (the merged mesh must not be saved).</summary>
    static void CombineStatic(Transform root)
    {
        if (!BakeSession) StaticBatchingUtility.Combine(root.gameObject);
    }

    // ------------------------------------------------------------------------------------------------- signage helpers

    static readonly Color Navy = new Color(0.03f, 0.08f, 0.2f, 0.96f);
    static readonly Color SafetyYellow = new Color(0.98f, 0.78f, 0.1f);
    static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f);
    static Material backMat;

    /// <summary>
    /// A rounded sign panel at a world position whose readable face points along <paramref name="faceDir"/> (toward
    /// the viewer), with a metal back. Text added to the returned transform goes at local z = -0.015.
    /// </summary>
    Transform Sign(Transform parent, string name, Vector3 pos, Vector3 faceDir, Vector2 size, Color fill, Color? accent = null, float accentHeight = 0.08f)
    {
        var h = P.Node(name, parent).transform;
        if (P.IsNew(h))
        {
            h.position = pos;
            faceDir.y = 0f;
            h.rotation = Quaternion.LookRotation(-faceDir.normalized, Vector3.up);
        }
        Ui.Panel(h, "Panel", Vector3.zero, size, fill, accent, accentHeight);
        SignBack(h, Vector3.zero, size);
        return h;
    }

    static TextMesh SignText(Transform sign, string s, float y, float height, Color color, bool bold = true, float x = 0f, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        return P.Text(sign, s, new Vector3(x, y, -0.015f), height, color, anchor, bold);
    }

    /// <summary>
    /// A solid back for a one-sided panel, so from behind you see a plain metal back and not reversed text.
    /// Two crossed boxes stay inside the panel's rounded corners.
    /// </summary>
    static void SignBack(Transform parent, Vector3 center, Vector2 size)
    {
        if (backMat == null) backMat = P.Mat(new Color(0.42f, 0.44f, 0.47f), 0.6f, 0.45f);
        float r = 0.12f * Mathf.Min(size.x, size.y);
        const float d = 0.012f, z = 0.013f;
        P.Box(parent, "Sign back", center + new Vector3(0f, 0f, z), new Vector3(size.x * 0.99f, size.y - 2f * r, d), backMat, false);
        P.Box(parent, "Sign back", center + new Vector3(0f, 0f, z), new Vector3(size.x - 2f * r, size.y * 0.99f, d), backMat, false);
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

    /// <summary>
    /// University plate hung from the entry gantry, just right of the task board: in view from the start position,
    /// above head height, and clear of the PPE sign in the middle of the header.
    /// </summary>
    void BuildBrand()
    {
        const float w = 1.6f; // plate is w x w/2
        var pos = new Vector3(-5.85f, 3.86f, -6.3f);
        var plate = Ui.BrandPlate(transform, pos, w);
        SignBack(transform, pos, new Vector2(w, w / 2f));
        P.Box(transform, "Plate hanger", pos + new Vector3(-0.55f, w / 4f + 0.03f, 0.04f), new Vector3(0.04f, 0.1f, 0.04f), darkSteel, false);
        P.Box(transform, "Plate hanger", pos + new Vector3(0.55f, w / 4f + 0.03f, 0.04f), new Vector3(0.04f, 0.1f, 0.04f), darkSteel, false);
        StartCoroutine(FitText(plate, w * 0.9f));
    }

    void LateUpdate()
    {
        if (rig == null) return;
        var cam = rig.Camera.transform;
        body.position = cam.position;
        body.rotation = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);
    }

    // ------------------------------------------------------------------------------------------------- building shell

    void BuildShell()
    {
        var root = NewRoot("Shell");
        P.Box(root, "Floor", new Vector3(0, -0.1f, 2f), new Vector3(28f, 0.2f, 28f), concrete);
        var wall = P.Mat(new Color(0.78f, 0.79f, 0.8f), 0f, 0.2f);
        P.Box(root, "Wall south", new Vector3(0, 3.5f, -12.15f), new Vector3(28.6f, 7f, 0.3f), wall);
        P.Box(root, "Wall north", new Vector3(0, 3.5f, 16.15f), new Vector3(28.6f, 7f, 0.3f), wall);
        P.Box(root, "Wall west", new Vector3(-14.15f, 3.5f, 2f), new Vector3(0.3f, 7f, 28.6f), wall);
        P.Box(root, "Wall east", new Vector3(14.15f, 3.5f, 2f), new Vector3(0.3f, 7f, 28.6f), wall);
        P.Box(root, "Ceiling", new Vector3(0, 7.1f, 2f), new Vector3(28f, 0.2f, 28f), P.Mat(new Color(0.55f, 0.56f, 0.58f)), false);

        // branded wainscot bands
        var blue = P.Mat(P.Royal, 0.1f, 0.4f);
        var gold = P.Mat(P.Gold, 0.3f, 0.5f);
        P.Box(root, "Band south", new Vector3(0, 0.9f, -11.97f), new Vector3(27.8f, 1.8f, 0.05f), blue, false);
        P.Box(root, "Band north", new Vector3(0, 0.9f, 15.97f), new Vector3(27.8f, 1.8f, 0.05f), blue, false);
        P.Box(root, "Band west", new Vector3(-13.97f, 0.9f, 2f), new Vector3(0.05f, 1.8f, 27.8f), blue, false);
        P.Box(root, "Band east", new Vector3(13.97f, 0.9f, 2f), new Vector3(0.05f, 1.8f, 27.8f), blue, false);
        P.Box(root, "Stripe south", new Vector3(0, 1.85f, -11.96f), new Vector3(27.8f, 0.08f, 0.05f), gold, false);
        P.Box(root, "Stripe north", new Vector3(0, 1.85f, 15.96f), new Vector3(27.8f, 0.08f, 0.05f), gold, false);
        P.Box(root, "Stripe west", new Vector3(-13.96f, 1.85f, 2f), new Vector3(0.05f, 0.08f, 27.8f), gold, false);
        P.Box(root, "Stripe east", new Vector3(13.96f, 1.85f, 2f), new Vector3(0.05f, 0.08f, 27.8f), gold, false);

        // roof trusses and light panels
        for (int i = 0; i < 9; i++)
            P.Box(root, "Truss", new Vector3(0, 6.7f, -10f + i * 3f), new Vector3(27.6f, 0.35f, 0.2f), darkSteel, false);
        foreach (float lx in new[] { -7f, 7f })
        foreach (float lz in new[] { -6f, 3f, 12f })
        {
            var pos = new Vector3(lx, 6.55f, lz);
            P.Box(root, "Light panel", pos, new Vector3(2.2f, 0.06f, 0.6f), P.Glow(new Color(1f, 0.96f, 0.88f), 2.2f), false);
            var lgo = P.Node("Point", root);
            var l = P.Ensure<Light>(lgo);
            if (P.IsNew(lgo))
            {
                l.transform.position = pos - Vector3.up * 0.5f;
                l.type = LightType.Point;
                l.range = 17f;
                l.intensity = 0.85f;
                l.color = new Color(1f, 0.95f, 0.86f);
            }
        }
        var sunGo = P.Node("Key light", root);
        var sun = P.Ensure<Light>(sunGo);
        if (P.IsNew(sunGo))
        {
            sun.type = LightType.Directional;
            sun.intensity = 0.4f;
            sun.color = new Color(1f, 0.94f, 0.85f);
            sun.transform.rotation = Quaternion.Euler(60, -30, 0);
            sun.shadows = LightShadows.Soft;
        }

        // entry gantry over the PPE line, with the task board hung from it
        P.Box(root, "Gantry post W", new Vector3(-13.7f, 2.3f, -6.1f), new Vector3(0.3f, 4.6f, 0.3f), darkSteel, false);
        P.Box(root, "Gantry post E", new Vector3(13.7f, 2.3f, -6.1f), new Vector3(0.3f, 4.6f, 0.3f), darkSteel, false);
        P.Box(root, "Gantry header", new Vector3(0, 4.55f, -6.1f), new Vector3(27.4f, 0.45f, 0.3f), P.Mat(P.MediumBlue, 0.2f, 0.5f), false);
        var ppeHeader = Sign(root, "PPE header sign", new Vector3(0, 4.55f, -6.28f), Vector3.back, new Vector2(5.6f, 0.62f), SafetyYellow, Ink, 0.08f);
        SignText(ppeHeader, "PPE REQUIRED BEYOND THIS POINT", -0.02f, 0.26f, Ink);

        // entry barrier rails with one gate opening at the walkway
        BuildRail(root, -13.6f, -5.2f);
        BuildRail(root, -2.8f, 13.6f);

        // wall signage
        // wall signage on rounded panels; the west sign sits above the 4.2 m racking so it is not hidden
        var recv = Sign(root, "Receiving sign", new Vector3(0, 3.3f, 15.96f), Vector3.back, new Vector2(7.6f, 1.0f), Navy, P.Gold, 0.07f);
        SignText(recv, "RECEIVING AND STORAGE", -0.02f, 0.5f, P.Gold);
        var watch = Sign(root, "Watch for forklifts sign", new Vector3(-13.96f, 5.1f, 6f), Vector3.right, new Vector2(6.4f, 0.9f), SafetyYellow, Ink, 0.08f);
        SignText(watch, "WATCH FOR FORKLIFTS", -0.02f, 0.42f, Ink);
    }

    void BuildRail(Transform root, float x0, float x1)
    {
        var yellow = P.Mat(new Color(1f, 0.8f, 0.05f), 0.1f, 0.4f);
        float len = x1 - x0;
        P.Box(root, "Rail", new Vector3((x0 + x1) / 2f, 1.0f, -6.0f), new Vector3(len, 0.07f, 0.07f), yellow, false);
        P.Box(root, "Rail low", new Vector3((x0 + x1) / 2f, 0.55f, -6.0f), new Vector3(len, 0.07f, 0.07f), yellow, false);
        int posts = Mathf.Max(2, Mathf.CeilToInt(len / 3f) + 1);
        for (int i = 0; i < posts; i++)
            P.Box(root, "Rail post", new Vector3(Mathf.Lerp(x0, x1, i / (float)(posts - 1)), 0.5f, -6.0f), new Vector3(0.08f, 1.0f, 0.08f), yellow, false);
    }

    // ------------------------------------------------------------------------------------------------- floor markings

    GameObject Paint(Transform root, string name, float x0, float z0, float x1, float z1, Material mat, float y = 0.006f)
    {
        return P.Box(root, name, new Vector3((x0 + x1) / 2f, y, (z0 + z1) / 2f), new Vector3(Mathf.Abs(x1 - x0), 0.012f, Mathf.Abs(z1 - z0)), mat, false);
    }

    void HLine(Transform root, float z, float x0, float x1, Material mat) { Paint(root, "Line", x0, z - 0.06f, x1, z + 0.06f, mat, 0.008f); }
    void VLine(Transform root, float x, float z0, float z1, Material mat) { Paint(root, "Line", x - 0.06f, z0, x + 0.06f, z1, mat, 0.008f); }

    void BuildMarkings()
    {
        var root = NewRoot("Markings");
        var laneMat = P.Mat(new Color(0.23f, 0.24f, 0.27f), 0f, 0.15f);
        var green = P.Mat(new Color(0.08f, 0.5f, 0.24f), 0f, 0.25f);
        var yellow = P.Mat(new Color(1f, 0.8f, 0.05f), 0f, 0.3f);
        var white = P.Mat(new Color(0.95f, 0.95f, 0.95f), 0f, 0.3f);

        // forklift lanes (a rectangular loop)
        Paint(root, "Lane south", -11.6f, -1.6f, 11.6f, 1.6f, laneMat, 0.004f);
        Paint(root, "Lane north", -11.6f, 10.4f, 11.6f, 13.6f, laneMat, 0.004f);
        Paint(root, "Lane east", 8.4f, 1.6f, 11.6f, 10.4f, laneMat, 0.004f);
        Paint(root, "Lane west", -11.6f, 1.6f, -8.4f, 10.4f, laneMat, 0.004f);

        // yellow lane edges, interrupted at the crossing
        HLine(root, -1.6f, -11.6f, -4.9f, yellow);
        HLine(root, -1.6f, -3.1f, 11.6f, yellow);
        HLine(root, 1.6f, -8.4f, -4.9f, yellow);
        HLine(root, 1.6f, -3.1f, 8.4f, yellow);
        HLine(root, 13.6f, -11.6f, 11.6f, yellow);
        HLine(root, 10.4f, -8.4f, 8.4f, yellow);
        VLine(root, -11.6f, -1.6f, 13.6f, yellow);
        VLine(root, 11.6f, -1.6f, 13.6f, yellow);
        VLine(root, -8.4f, 1.6f, 10.4f, yellow);
        VLine(root, 8.4f, 1.6f, 10.4f, yellow);

        // pedestrian walkways (green)
        Paint(root, "Walkway south", -4.8f, -6f, -3.2f, -1.6f, green, 0.005f);
        Paint(root, "Walkway north", -4.8f, 1.6f, -3.2f, 9.8f, green, 0.005f);
        Paint(root, "Walkway spur", -4.8f, 4.6f, 2.3f, 6.4f, green, 0.006f);
        VLine(root, -4.8f, -6f, -1.6f, white);
        VLine(root, -3.2f, -6f, -1.6f, white);
        VLine(root, -4.8f, 1.6f, 9.8f, white);
        VLine(root, -3.2f, 1.6f, 4.6f, white);
        VLine(root, -3.2f, 6.4f, 9.8f, white);
        HLine(root, 4.6f, -3.2f, 2.3f, white);
        HLine(root, 6.4f, -3.2f, 2.3f, white);
        HLine(root, 9.8f, -4.8f, -3.2f, white);

        // the striped pedestrian crossing, with hazard strips at both ends
        for (int i = 0; i < 8; i++)
        {
            float x0 = -4.8f + i * 0.2f;
            Paint(root, "Crossing stripe", x0 + 0.03f, -1.5f, x0 + 0.17f, 1.5f, white, 0.01f);
        }
        Paint(root, "Crossing guard south", -4.8f, -1.95f, -3.2f, -1.65f, hazard, 0.01f);
        Paint(root, "Crossing guard north", -4.8f, 1.65f, -3.2f, 1.95f, hazard, 0.01f);

        // PPE line at the floor entry
        Paint(root, "Entry line", -14f, -6.15f, 14f, -5.85f, hazard, 0.009f);

        // floor text
        FloorText(root, "PPE REQUIRED BEYOND THIS LINE", new Vector3(0, 0.016f, -6.7f), 0.3f, P.Gold);
        FloorText(root, "PEDESTRIANS", new Vector3(-4f, 0.016f, -4.0f), 0.16f, Color.white);
        FloorText(root, "PEDESTRIANS", new Vector3(-4f, 0.016f, 3.4f), 0.16f, Color.white);
        FloorText(root, "FORKLIFT LANE  >>>", new Vector3(4f, 0.016f, -1.0f), 0.3f, Color.white);
    }

    /// <summary>Text lying on the floor; <paramref name="yaw"/> turns it to read from another side (0 = from the south).</summary>
    TextMesh FloorText(Transform root, string s, Vector3 pos, float height, Color color, float yaw = 0f)
    {
        var t = P.Text(root, s, pos, height, color, TextAnchor.MiddleCenter, true);
        if (P.IsNew(t)) t.transform.rotation = Quaternion.Euler(90, yaw, 0);
        return t;
    }

    // ------------------------------------------------------------------------------------------------- racking and cargo

    void BuildRacking()
    {
        var root = NewRoot("Racking");
        Rack(root, -1.0f, 2.6f, 4.6f, 1, 11, float.NaN);
        Rack(root, -1.0f, 6.4f, 9.4f, 2, 12, float.NaN);
        Rack(root, 3.0f, 2.6f, 9.4f, 4, 13, UprightPos.z);   // this row has the bent upright on its aisle face
        Rack(root, 6.0f, 2.6f, 9.4f, 4, 14, float.NaN);
        Rack(root, -13.0f, 0.5f, 12.5f, 6, 15, float.NaN);   // scenery along the west wall
        // merge the many small static meshes to keep draw calls low (at Play only, not while baking)
        CombineStatic(root);
    }

    /// <summary>A pallet-rack row along z. The aisle face is the -x side. dentZ is the z of a bent aisle-side upright.</summary>
    void Rack(Transform root, float cx, float z0, float z1, int bays, int seed, float dentZ)
    {
        var rng = new System.Random(seed);
        const float depth = 1.0f, height = 4.2f;
        float bay = (z1 - z0) / bays;
        var upright = P.Painted(P.Royal);
        var beam = P.Mat(new Color(0.92f, 0.42f, 0.08f), 0.1f, 0.4f);
        var deck = P.Mat(new Color(0.2f, 0.2f, 0.22f), 0f, 0.3f);
        var guard = hazard;
        var cardboard = new[]
        {
            P.Mat(new Color(0.72f, 0.56f, 0.36f), 0f, 0.2f),
            P.Mat(new Color(0.62f, 0.47f, 0.3f), 0f, 0.2f),
            P.Mat(new Color(0.8f, 0.7f, 0.55f), 0f, 0.2f),
        };
        float[] levels = { 0.25f, 1.55f, 2.85f };

        for (int i = 0; i <= bays; i++)
        {
            float z = z0 + i * bay;
            for (int s = -1; s <= 1; s += 2)
            {
                float x = cx + s * depth / 2f;
                bool dent = !float.IsNaN(dentZ) && s == -1 && Mathf.Abs(z - dentZ) < 0.01f;
                if (dent)
                {
                    // bent upright: leans into the aisle from its base
                    float tilt = 6f;
                    float shift = (height / 2f) * Mathf.Sin(tilt * Mathf.Deg2Rad);
                    var bent = P.Box(root, "Bent upright", new Vector3(x - shift, height / 2f, z), new Vector3(0.08f, height, 0.08f), upright);
                    if (P.IsNew(bent)) bent.transform.localRotation = Quaternion.Euler(0, 0, tilt);
                    P.Box(root, "Impact scuff", new Vector3(x - 0.05f, 0.35f, z), new Vector3(0.14f, 0.3f, 0.14f), P.Mat(new Color(0.12f, 0.12f, 0.13f), 0f, 0.2f), false);
                }
                else
                {
                    P.Box(root, "Upright", new Vector3(x, height / 2f, z), new Vector3(0.08f, height, 0.08f), upright);
                    P.Box(root, "Upright guard", new Vector3(x, 0.2f, z), new Vector3(0.14f, 0.4f, 0.14f), guard, false);
                }
            }
        }

        for (int b = 0; b < bays; b++)
        {
            float zm = z0 + (b + 0.5f) * bay;
            foreach (float level in levels)
            {
                P.Box(root, "Beam", new Vector3(cx - depth / 2f, level, zm), new Vector3(0.06f, 0.12f, bay), beam, false);
                P.Box(root, "Beam", new Vector3(cx + depth / 2f, level, zm), new Vector3(0.06f, 0.12f, bay), beam, false);
                P.Box(root, "Deck", new Vector3(cx, level + 0.07f, zm), new Vector3(depth, 0.03f, bay - 0.1f), deck, false);
                int n = rng.Next(0, 3);
                for (int k = 0; k < n; k++)
                {
                    float w = 0.55f + (float)rng.NextDouble() * 0.3f;
                    float h = 0.4f + (float)rng.NextDouble() * 0.6f;
                    float l = Mathf.Min(bay - 0.3f, 0.6f + (float)rng.NextDouble() * 0.5f);
                    float zz = zm + (n == 1 ? 0f : (k == 0 ? -1 : 1) * (bay * 0.25f));
                    P.Box(root, "Carton", new Vector3(cx, level + 0.085f + h / 2f, zz), new Vector3(w, h, l), cardboard[rng.Next(0, 3)]);
                }
            }
        }
    }

    /// <summary>Stacked cargo that hides the south lane from the walkway: the blind corner.</summary>
    void BuildCargoBlock()
    {
        var root = NewRoot("Cargo block");
        var rng = new System.Random(21);
        var mats = new[]
        {
            P.Mat(new Color(0.72f, 0.56f, 0.36f), 0f, 0.2f),
            P.Mat(new Color(0.62f, 0.47f, 0.3f), 0f, 0.2f),
            P.Mat(new Color(0.3f, 0.45f, 0.7f), 0f, 0.3f),
        };
        float[] xs = { -8.35f, -7.2f, -6.05f };
        float[] zs = { -4.65f, -2.75f };
        float[] ys = { 0.6f, 1.85f };
        foreach (float x in xs)
        foreach (float z in zs)
        foreach (float y in ys)
            P.Box(root, "Cargo", new Vector3(x, y, z), new Vector3(1.1f, 1.2f, 1.5f), mats[rng.Next(0, 3)]);
        CombineStatic(root);
        // the sign is created after combining and outside the batched root (TextMesh has no MeshFilter)
        // Fixed to the stack's east face, readable from the walkway (+x). The old text faced -x, so it read mirrored.
        var blind = Sign(transform, "Blind corner sign", new Vector3(-5.475f, 2.2f, -3.8f), Vector3.right, new Vector2(1.7f, 0.62f), SafetyYellow, Ink, 0.09f);
        SignText(blind, "BLIND CORNER", 0.1f, 0.15f, Ink);
        SignText(blind, "CHECK THE MIRROR", -0.12f, 0.1f, Ink);
    }

    void BuildDock()
    {
        var root = NewRoot("Loading dock");
        float x = 13.82f;
        P.Box(root, "Dock opening", new Vector3(x, 1.9f, 6f), new Vector3(0.06f, 3.8f, 4.6f), P.Glow(new Color(0.75f, 0.88f, 1f), 1.6f), false);
        P.Box(root, "Dock frame N", new Vector3(x - 0.04f, 1.9f, 8.4f), new Vector3(0.14f, 3.9f, 0.2f), hazard, false);
        P.Box(root, "Dock frame S", new Vector3(x - 0.04f, 1.9f, 3.6f), new Vector3(0.14f, 3.9f, 0.2f), hazard, false);
        P.Box(root, "Dock frame top", new Vector3(x - 0.04f, 3.85f, 6f), new Vector3(0.14f, 0.2f, 5.0f), hazard, false);
        P.Cyl(root, "Door roll", new Vector3(x - 0.2f, 3.55f, 6f), 0.22f, 4.4f, Vector3.forward, steel, false);
        P.Box(root, "Bumper N", new Vector3(x - 0.1f, 0.5f, 8.1f), new Vector3(0.2f, 0.4f, 0.3f), P.Mat(new Color(0.08f, 0.08f, 0.08f)), false);
        P.Box(root, "Bumper S", new Vector3(x - 0.1f, 0.5f, 3.9f), new Vector3(0.2f, 0.4f, 0.3f), P.Mat(new Color(0.08f, 0.08f, 0.08f)), false);
        var dock = Sign(root, "Dock sign", new Vector3(13.94f, 4.5f, 6f), Vector3.left, new Vector2(4.6f, 0.62f), Navy, P.Gold, 0.08f);
        SignText(dock, "DOCK 2   LOADING DOOR", -0.02f, 0.3f, P.Gold);
        Paint(root, "Dock apron", 11.7f, 3.4f, 12.0f, 8.6f, hazard, 0.009f);
    }

    // ------------------------------------------------------------------------------------------------- stands

    /// <summary>A free-standing sign panel facing a point. The returned transform has its origin on the floor; the front is local -z.</summary>
    Transform Stand(string name, Vector3 basePos, Vector3 faceToward, float width, float height, float bottom)
    {
        var t = P.Node(name, transform).transform;
        if (P.IsNew(t))
        {
            Vector3 d = basePos - faceToward;
            d.y = 0f;
            t.SetPositionAndRotation(basePos, Quaternion.LookRotation(d.normalized, Vector3.up));
        }
        P.Box(t, "Post L", new Vector3(-width / 2f + 0.08f, bottom / 2f, 0.05f), new Vector3(0.06f, bottom, 0.06f), steel);
        P.Box(t, "Post R", new Vector3(width / 2f - 0.08f, bottom / 2f, 0.05f), new Vector3(0.06f, bottom, 0.06f), steel);
        Ui.Panel(t, "Panel", new Vector3(0, bottom + height / 2f, 0f), new Vector2(width, height), Navy, P.Gold, 0.05f);
        SignBack(t, new Vector3(0, bottom + height / 2f, 0f), new Vector2(width, height));
        return t;
    }

    Pressable MakeButton(Transform parent, string name, Vector3 localPos, Vector3 size, Color color, System.Action onPress)
    {
        var b = P.Box(parent, name + " button", localPos, size, P.Glow(color, 0.9f));
        var p = P.Ensure<Pressable>(b);
        p.Label = name;
        p.Pressed += h => onPress();
        return p;
    }

    // ------------------------------------------------------------------------------------------------- PPE station

    void BuildPpeStation()
    {
        var root = NewRoot("PPE station");
        var wood = P.Mat(new Color(0.5f, 0.38f, 0.25f), 0f, 0.3f);
        float tx = -9.5f, tz = -8.0f;
        P.Box(root, "Table top", new Vector3(tx, 0.8f, tz), new Vector3(2.0f, 0.06f, 0.8f), wood);
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            P.Box(root, "Table leg", new Vector3(tx + sx * 0.92f, 0.4f, tz + sz * 0.34f), new Vector3(0.06f, 0.8f, 0.06f), darkSteel);

        // body zones follow the player's head and torso
        hatZone = MakeZone("ppe-hat", new Vector3(0, 0.12f, 0.0f), 0.22f);
        glassesZone = MakeZone("ppe-glasses", new Vector3(0, -0.02f, 0.03f), 0.2f);
        vestZone = MakeZone("ppe-vest", new Vector3(0, -0.45f, 0.0f), 0.35f);

        MakeHat(new Vector3(tx - 0.6f, 0.97f, tz));
        MakeVest(new Vector3(tx, 1.07f, tz));
        MakeGlasses(new Vector3(tx + 0.6f, 0.88f, tz));

        var sign = Stand("PPE sign", new Vector3(-7.5f, 0f, -8.2f), new Vector3(-9f, 1.6f, -10.6f), 1.8f, 1.1f, 0.7f);
        P.Text(sign, "PPE STATION", new Vector3(-0.8f, 1.67f, -0.015f), 0.09f, P.Gold, TextAnchor.MiddleLeft, true);
        ppeText = P.Text(sign, "", new Vector3(-0.8f, 1.57f, -0.015f), 0.065f, Color.white, TextAnchor.UpperLeft, true);
        P.Text(sign, "Grab each item (grip).\nVR: bring it to your head or chest.\nDesktop: hold it, then press WEAR.", new Vector3(-0.8f, 1.2f, -0.015f), 0.04f, P.Sky, TextAnchor.UpperLeft);
        MakeButton(sign, "WEAR", new Vector3(0.52f, 1.45f, -0.05f), new Vector3(0.5f, 0.22f, 0.07f), new Color(0.1f, 0.75f, 0.35f), WearHeld);
        P.Text(sign, "WEAR", new Vector3(0.52f, 1.45f, -0.1f), 0.08f, Color.white, TextAnchor.MiddleCenter, true);
        UpdatePpeText(true);
    }

    SnapZone MakeZone(string id, Vector3 localPos, float radius)
    {
        var go = new GameObject("Zone " + id);
        go.transform.SetParent(body, false);
        go.transform.localPosition = localPos;
        var z = go.AddComponent<SnapZone>();
        z.Id = id;
        z.Radius = radius;
        return z;
    }

    void Register(Grabbable g)
    {
        gear.Add(new Gear { G = g, Cols = g.GetComponentsInChildren<Collider>() });
    }

    void MakeHat(Vector3 pos)
    {
        var go = P.Node("Hard hat");
        bool fresh = P.IsNew(go);
        if (fresh) go.transform.position = pos;
        var rb = P.Ensure<Rigidbody>(go);
        if (fresh) rb.mass = 0.4f;
        var yellow = P.Mat(new Color(1f, 0.82f, 0.05f), 0.1f, 0.7f);
        P.Prim(PrimitiveType.Sphere, go.transform, "Dome", new Vector3(0, 0.03f, 0), new Vector3(0.27f, 0.2f, 0.3f), yellow, false);
        P.Cyl(go.transform, "Brim", new Vector3(0, -0.04f, 0.02f), 0.15f, 0.012f, Vector3.up, yellow, false);
        P.Box(go.transform, "Ridge", new Vector3(0, 0.1f, 0), new Vector3(0.04f, 0.03f, 0.26f), yellow, false);
        var col = P.Ensure<BoxCollider>(go);
        if (fresh) col.size = new Vector3(0.3f, 0.2f, 0.32f);
        var g = P.Ensure<Grabbable>(go);
        g.SnapId = "ppe-hat";
        Register(g);
    }

    void MakeVest(Vector3 pos)
    {
        var go = P.Node("Hi-vis vest");
        bool fresh = P.IsNew(go);
        if (fresh) go.transform.position = pos;
        var rb = P.Ensure<Rigidbody>(go);
        if (fresh) rb.mass = 0.5f;
        var lime = P.Mat(new Color(0.78f, 1f, 0.05f), 0f, 0.4f);
        var silver = P.Mat(new Color(0.85f, 0.85f, 0.88f), 0.8f, 0.8f);
        foreach (float zs in new[] { -0.11f, 0.11f })
        {
            P.Box(go.transform, "Panel", new Vector3(0, 0, zs), new Vector3(0.4f, 0.46f, 0.03f), lime, false);
            P.Box(go.transform, "Reflective band", new Vector3(0, -0.06f, zs), new Vector3(0.41f, 0.04f, 0.035f), silver, false);
            P.Box(go.transform, "Reflective band", new Vector3(0, 0.1f, zs), new Vector3(0.41f, 0.04f, 0.035f), silver, false);
        }
        foreach (float xs in new[] { -0.12f, 0.12f })
            P.Box(go.transform, "Strap", new Vector3(xs, 0.23f, 0), new Vector3(0.06f, 0.03f, 0.24f), lime, false);
        var col = P.Ensure<BoxCollider>(go);
        if (fresh) col.size = new Vector3(0.42f, 0.48f, 0.26f);
        var g = P.Ensure<Grabbable>(go);
        g.SnapId = "ppe-vest";
        Register(g);
    }

    void MakeGlasses(Vector3 pos)
    {
        var go = P.Node("Safety glasses");
        bool fresh = P.IsNew(go);
        if (fresh) go.transform.position = pos;
        var rb = P.Ensure<Rigidbody>(go);
        if (fresh) rb.mass = 0.1f;
        var lens = P.Transparent(new Color(0.7f, 0.85f, 1f, 0.4f));
        var frame = P.Mat(new Color(0.08f, 0.08f, 0.1f), 0f, 0.4f);
        P.Box(go.transform, "Lens L", new Vector3(-0.045f, 0, 0), new Vector3(0.07f, 0.04f, 0.008f), lens, false);
        P.Box(go.transform, "Lens R", new Vector3(0.045f, 0, 0), new Vector3(0.07f, 0.04f, 0.008f), lens, false);
        P.Box(go.transform, "Brow", new Vector3(0, 0.024f, 0), new Vector3(0.17f, 0.01f, 0.01f), frame, false);
        P.Box(go.transform, "Temple L", new Vector3(-0.085f, 0, -0.065f), new Vector3(0.008f, 0.008f, 0.13f), frame, false);
        P.Box(go.transform, "Temple R", new Vector3(0.085f, 0, -0.065f), new Vector3(0.008f, 0.008f, 0.13f), frame, false);
        var col = P.Ensure<BoxCollider>(go);
        if (fresh) col.size = new Vector3(0.22f, 0.12f, 0.16f);
        var g = P.Ensure<Grabbable>(go);
        g.SnapId = "ppe-glasses";
        Register(g);
    }

    /// <summary>The "wear" action: whatever PPE is in a hand goes onto the matching body zone.</summary>
    void WearHeld()
    {
        if (rig == null) return;
        foreach (var hand in new[] { rig.Left, rig.Right })
        {
            var g = hand.Held;
            if (g == null || g.SnapId == null || !g.SnapId.StartsWith("ppe-")) continue;
            SnapZone zone = g.SnapId == "ppe-hat" ? hatZone : g.SnapId == "ppe-vest" ? vestZone : glassesZone;
            if (zone.Occupant != null) continue;
            hand.Release();
            if (g.Snapped == null)
            {
                g.transform.position = zone.transform.position;
                SnapZone.TrySnap(g);
            }
        }
    }

    void UpdatePpeText(bool force)
    {
        bool hat = hatZone.Occupant != null, vest = vestZone.Occupant != null, glasses = glassesZone.Occupant != null;
        allPpe = hat && vest && glasses;
        string key = (hat ? "1" : "0") + (vest ? "1" : "0") + (glasses ? "1" : "0");
        if (!force && key == ppeKey) return;
        ppeKey = key;
        ppeText.text = "HARD HAT        " + (hat ? "ON" : "--") + "\nHI-VIS VEST     " + (vest ? "ON" : "--") + "\nSAFETY GLASSES  " + (glasses ? "ON" : "--");
    }

    // ------------------------------------------------------------------------------------------------- mirror, signal, hazards, cones, radio

    void BuildMirror()
    {
        var root = NewRoot("Corner mirror");
        mirrorCenter = new Vector3(-5.15f, 1.95f, -2.25f);
        Vector3 viewer = new Vector3(-4.0f, 1.7f, -4.2f);
        mirrorNormal = viewer - mirrorCenter;
        mirrorNormal.y = 0f;
        mirrorNormal.Normalize();

        P.Cyl(root, "Mirror post", mirrorCenter - mirrorNormal * 0.08f - new Vector3(0, mirrorCenter.y / 2f, 0), 0.04f, mirrorCenter.y, Vector3.up, steel, false);
        var m = P.Node("Mirror", root).transform;
        if (P.IsNew(m)) m.SetPositionAndRotation(mirrorCenter, Quaternion.LookRotation(-mirrorNormal, Vector3.up));
        P.Box(m, "Frame", new Vector3(0, 0, 0.04f), new Vector3(1.0f, 1.0f, 0.06f), P.Mat(new Color(0.95f, 0.4f, 0.05f), 0.1f, 0.5f), false);
        var face = P.Prim(PrimitiveType.Quad, m, "Face", Vector3.zero, new Vector3(0.86f, 0.86f, 1f), null, false);

        // The mirror image is a render texture that exists only at runtime: the face gets its material at every Play
        // (a baked face has a placeholder material), and the mirror camera is never baked.
        mirrorRt = new RenderTexture(256, 256, 16) { name = "MirrorRT" };
        var shader = Shader.Find("Unlit/Texture");
        Material mat = shader != null ? new Material(shader) : P.Glow(Color.white, 1f);
        mat.mainTexture = mirrorRt;
        face.GetComponent<Renderer>().sharedMaterial = mat;

        // the mirror shows the south lane to the west, which the cargo block hides from the walkway
        var camGo = new GameObject("Mirror camera");
        camGo.transform.SetParent(root, false);
        camGo.transform.position = new Vector3(-4.8f, 1.8f, -1.3f);
        camGo.transform.rotation = Quaternion.LookRotation(new Vector3(-12f, 1.3f, 0.2f) - camGo.transform.position, Vector3.up);
        mirrorCam = camGo.AddComponent<Camera>();
        mirrorCam.targetTexture = mirrorRt;
        mirrorCam.clearFlags = CameraClearFlags.SolidColor;
        mirrorCam.backgroundColor = new Color(0.3f, 0.32f, 0.36f);
        mirrorCam.fieldOfView = 80f;
        mirrorCam.nearClipPlane = 0.1f;
        mirrorCam.farClipPlane = 60f;
        mirrorCam.depth = -10f;
        mirrorCam.stereoTargetEye = StereoTargetEyeMask.None;

        // the lamp turns green when the mirror is checked: take the renderer's own copy (a baked one shares an asset)
        var lamp = P.Ball(root, "Mirror lamp", mirrorCenter + new Vector3(0, -0.65f, 0) + mirrorNormal * 0.05f, 0.05f, P.Glow(Color.red, 1.2f), false);
        mirrorLampMat = lamp.GetComponent<Renderer>().material;
    }

    void BuildSignalStation()
    {
        var sign = Stand("Signal station", new Vector3(-2.6f, 0f, -3.0f), new Vector3(-4f, 1.4f, -4.5f), 1.1f, 0.85f, 0.8f);
        P.Text(sign, "SIGNAL THE DRIVER", new Vector3(0, 1.53f, -0.015f), 0.07f, P.Gold, TextAnchor.MiddleCenter, true);
        P.Text(sign, "1  LOOK at the driver\n2  PRESS the button\n3  WAIT for the stop", new Vector3(-0.45f, 1.46f, -0.015f), 0.045f, P.Sky, TextAnchor.UpperLeft);
        MakeButton(sign, "SIGNAL", new Vector3(0, 1.08f, -0.05f), new Vector3(0.5f, 0.2f, 0.07f), new Color(1f, 0.62f, 0.05f), SignalPressed);
        P.Text(sign, "SIGNAL", new Vector3(0, 1.08f, -0.1f), 0.07f, Color.white, TextAnchor.MiddleCenter, true);
    }

    void BuildHazards()
    {
        var root = NewRoot("Hazards");
        // oily spill from a tipped drum
        P.Cyl(root, "Spill", SpillPos + new Vector3(0, 0.008f, 0), 0.9f, 0.012f, Vector3.up, P.Mat(new Color(0.04f, 0.07f, 0.1f), 0f, 0.95f), false);
        P.Cyl(root, "Spill edge", SpillPos + new Vector3(0.5f, 0.007f, 0.3f), 0.6f, 0.012f, Vector3.up, P.Mat(new Color(0.05f, 0.08f, 0.11f), 0f, 0.95f), false);
        P.Cyl(root, "Tipped drum", SpillPos + new Vector3(1.0f, 0.29f, 0.5f), 0.29f, 0.85f, new Vector3(1f, 0f, 0.3f), P.Mat(new Color(0.12f, 0.35f, 0.7f), 0.5f, 0.6f), false);
    }

    void BuildCones()
    {
        var root = NewRoot("Cone station");
        P.Box(root, "Cone tray", new Vector3(-5.4f, 0.04f, 2.9f), new Vector3(0.9f, 0.08f, 0.9f), hazard);
        P.Cyl(root, "Cone sign post", new Vector3(-5.9f, 0.72f, 2.4f), 0.03f, 1.44f, Vector3.up, steel, false);
        // plate on the post, readable from the walkway to the east (+x)
        var coneSign = Sign(root, "Cone sign", new Vector3(-5.86f, 1.3f, 2.4f), Vector3.right, new Vector2(0.95f, 0.26f), Navy, SafetyYellow, 0.1f);
        SignText(coneSign, "SAFETY CONES", -0.01f, 0.09f, SafetyYellow);
        MakeCone(new Vector3(-5.6f, 0.36f, 2.7f));
        MakeCone(new Vector3(-5.2f, 0.36f, 2.7f));
        MakeCone(new Vector3(-5.6f, 0.36f, 3.1f));
        MakeCone(new Vector3(-5.2f, 0.36f, 3.1f));
    }

    void MakeCone(Vector3 pos)
    {
        var go = P.Node("Safety cone");
        bool fresh = P.IsNew(go);
        if (fresh) go.transform.position = pos;
        var rb = P.Ensure<Rigidbody>(go);
        if (fresh) rb.mass = 0.6f;
        var orange = P.Mat(new Color(1f, 0.38f, 0.04f), 0f, 0.4f);
        var white = P.Mat(new Color(0.95f, 0.95f, 0.95f), 0f, 0.4f);
        P.Box(go.transform, "Base", new Vector3(0, -0.235f, 0), new Vector3(0.32f, 0.03f, 0.32f), P.Mat(new Color(0.1f, 0.1f, 0.1f), 0f, 0.3f), false);
        P.Cyl(go.transform, "Lower", new Vector3(0, -0.13f, 0), 0.12f, 0.18f, Vector3.up, orange, false);
        P.Cyl(go.transform, "Band", new Vector3(0, 0.04f, 0), 0.09f, 0.16f, Vector3.up, white, false);
        P.Cyl(go.transform, "Tip", new Vector3(0, 0.2f, 0), 0.06f, 0.16f, Vector3.up, orange, false);
        var col = P.Ensure<BoxCollider>(go);
        if (fresh) col.size = new Vector3(0.32f, 0.5f, 0.32f);
        // a low centre of mass keeps a dropped cone upright (a baked Rigidbody saves it; set it if the scene did not)
        if (fresh || rb.automaticCenterOfMass) rb.centerOfMass = new Vector3(0, -0.15f, 0);
        var g = P.Ensure<Grabbable>(go);
        g.SnapId = "cone";
        Register(g);
        cones.Add(g);
    }

    void BuildRadio()
    {
        var sign = Stand("Radio station", new Vector3(-5.9f, 0f, 8.4f), new Vector3(-4f, 1.4f, 8.4f), 1.2f, 0.95f, 0.8f);
        P.Text(sign, "SUPERVISOR RADIO", new Vector3(0, 1.61f, -0.015f), 0.07f, P.Gold, TextAnchor.MiddleCenter, true);
        radioText = P.Text(sign, "Cone both hazards, then\nreport them here.", new Vector3(-0.5f, 1.52f, -0.015f), 0.045f, P.Sky, TextAnchor.UpperLeft);
        MakeButton(sign, "RADIO", new Vector3(0, 1.0f, -0.05f), new Vector3(0.6f, 0.2f, 0.07f), new Color(0.1f, 0.6f, 0.9f), RadioPressed);
        P.Text(sign, "RADIO", new Vector3(0, 1.0f, -0.1f), 0.07f, Color.white, TextAnchor.MiddleCenter, true);
    }

    void BuildFlash()
    {
        flashRoot = new GameObject("Near-miss flash");
        flashRoot.transform.SetParent(rig.Camera.transform, false);
        flashRoot.transform.localPosition = new Vector3(0, 0, 0.6f);
        flashMat = P.Transparent(new Color(1f, 0.1f, 0.1f, 0f));
        P.Prim(PrimitiveType.Quad, flashRoot.transform, "Tint", Vector3.zero, new Vector3(2.6f, 1.6f, 1f), flashMat, false);
        flashText = P.Text(flashRoot.transform, "NEAR MISS\nForklift within 6 m", new Vector3(0, 0.04f, -0.02f), 0.09f, Color.white, TextAnchor.MiddleCenter, true);
        flashRoot.SetActive(false);
    }

    void BuildBoard()
    {
        board = TaskBoard.Create(transform, new Vector3(-9f, 3.9f, -6.2f), Quaternion.identity,
            "Warehouse forklift safety", "Pedestrian awareness practice  |  forklift lanes and crossings", new[]
            {
                "Put on hard hat, vest and glasses before entering the floor",
                "Look at the corner mirror for 1.5 s before stepping out",
                "Signal the driver, wait for the stop, cross at the stripes",
                "Cone the spill and the bent rack upright, then radio",
            }, 4.0f, 1.35f);
        board.SetFooter("Trigger = press, grip = grab. Desktop: WASD, hold right mouse to look, click, E to grab.");
        // The board hangs in a fixed spot, so players on the floor see its back: give it a solid one
        // (same panel size as TaskBoard.Create uses for width 4.0, text scale 1.35, four steps).
        const float k = 1.35f, bw = 4.0f;
        float bh = 1.05f * k + 4 * 0.095f * k;
        SignBack(board.transform, new Vector3(0, -bh / 2f + 0.25f, 0.02f), new Vector2(bw + 0.06f, bh + 0.06f));
    }

    // ------------------------------------------------------------------------------------------------- rules

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void MistakeCd(string key, string message, float cooldown)
    {
        if (nextAllowed.TryGetValue(key, out float t) && Time.time < t) return;
        nextAllowed[key] = Time.time + cooldown;
        board.Mistake(message);
    }

    Zone ZoneAt(Vector3 p)
    {
        if (p.z < EntryZ) return Zone.Entry;
        var v = new Vector2(p.x, p.z);
        if (CrossingRect.Contains(v)) return Zone.Crossing;
        foreach (var r in LaneRects) if (r.Contains(v)) return Zone.Lane;
        foreach (var r in WalkRects) if (r.Contains(v)) return Zone.Walk;
        return Zone.Floor;
    }

    bool Visible(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float len = dir.magnitude;
        if (len < 0.01f) return true;
        var hits = Physics.RaycastAll(from, dir / len, len, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<Grabbable>() != null) continue;
            return false;
        }
        return true;
    }

    void SignalPressed()
    {
        if (Time.time - lastLookAt > 8f)
        {
            board.Say("Look at the driver first and hold your gaze, then press SIGNAL.", new Color(1f, 0.72f, 0.3f));
            return;
        }
        lastLookAt = -999f;
        ackUntil = Time.time + 14f;
        forklift.Acknowledge(14f);
        board.Say("The driver waved and is stopping. Cross only when it has stopped.", new Color(0.3f, 0.9f, 0.62f));
    }

    void RadioPressed()
    {
        if (board.IsDone(3)) return;
        if (!(spillMarked && uprightMarked))
        {
            board.Mistake("Cone both hazards before you radio the supervisor.");
            return;
        }
        radioText.text = "SUPERVISOR: Copy. Spill and damaged\nupright reported. Crew on the way.";
        Done(3, "Reported by radio: spill and damaged upright, both coned off.");
    }

    void NearMiss(string why)
    {
        nextNear = Time.time + 4f;
        board.Mistake(why, true);
        forklift.EmergencyStop(4f);
        flashT = 1.8f;
        rig.Right.Haptic(0.9f, 0.3f);
        rig.Left.Haptic(0.9f, 0.3f);
    }

    void Update()
    {
        // while the Bake scene tool captures the world nothing should play (it removes runtime-only parts such as the mirror camera)
        if (rig == null || staging || baking) return;
        if (UpdateAwarenessPause()) return; // the scene is frozen for the awareness check
        float dt = Time.deltaTime;
        Vector3 feet = rig.FeetPosition;
        Zone zone = ZoneAt(feet);

        forklift.PlayerFeet = feet;
        forklift.PlayerInLane = zone == Zone.Lane || zone == Zone.Crossing;

        UpdateGearColliders();
        UpdatePpe();
        UpdateMirror(dt);
        UpdateDriverContact(dt);
        UpdateTraffic(zone, feet, dt);
        UpdateHazards(dt);
        UpdateFlash(dt);
        UpdateCoworkerEvent(zone, feet, dt);

        prevZone = zone;
        if (board.AllDone && !finished)
        {
            finished = true;
            board.Say(board.Summary(), board.CriticalMisses > 0 ? new Color(1f, 0.38f, 0.38f) : new Color(0.3f, 0.9f, 0.62f));
            ShowSummary();
        }
    }

    /// <summary>Held or worn gear has its colliders off so it never blocks the pointing ray.</summary>
    void UpdateGearColliders()
    {
        foreach (var gr in gear)
        {
            if (gr.G == null) continue;
            bool off = gr.G.Snapped != null || gr.G.GetComponentInParent<Hand>() != null;
            if (off == gr.Off) continue;
            gr.Off = off;
            foreach (var c in gr.Cols) if (c) c.enabled = !off;
        }
    }

    void UpdatePpe()
    {
        UpdatePpeText(false);
        if (allPpe && !board.IsDone(0)) Done(0, "PPE on: hard hat, vest and safety glasses.");
    }

    void UpdateMirror(float dt)
    {
        var cam = rig.Camera.transform;
        Vector3 to = mirrorCenter - cam.position;
        float d = to.magnitude;
        bool looking = d < 9f && Vector3.Angle(cam.forward, to) < 14f && Vector3.Dot(mirrorNormal, -to.normalized) > 0.25f;
        mirrorLook = looking ? mirrorLook + dt : Mathf.Max(0f, mirrorLook - dt * 2f);
        mirrorCam.enabled = d < 16f; // render the mirror only when it is near
        if (!mirrorChecked && mirrorLook >= 1.5f)
        {
            mirrorChecked = true;
            mirrorLampMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.45f) * 1.4f);
            Done(1, "Mirror checked: forklift traffic comes from the west.");
        }
    }

    void UpdateDriverContact(float dt)
    {
        var cam = rig.Camera.transform;
        Vector3 head = forklift.DriverHeadPosition;
        Vector3 to = head - cam.position;
        float d = to.magnitude;
        bool looking = d < 28f && Vector3.Angle(cam.forward, to) < 12f && Visible(cam.position, head);
        lookDriver = looking ? lookDriver + dt : 0f;
        if (lookDriver >= 0.6f) lastLookAt = Time.time;
    }

    void UpdateTraffic(Zone zone, Vector3 feet, float dt)
    {
        bool inLane = zone == Zone.Lane || zone == Zone.Crossing;
        bool wasLane = prevZone == Zone.Lane || prevZone == Zone.Crossing;
        float fd = FlatDistance(forklift.transform.position, feet);

        if (zone != Zone.Entry && !allPpe)
            MistakeCd("ppe", "Put on hard hat, vest and glasses before entering the floor.", 12f);

        if (zone == Zone.Floor)
        {
            floorTimer += dt;
            if (floorTimer > 1.2f) MistakeCd("floor", "Stay in the green pedestrian walkway.", 10f);
        }
        else floorTimer = 0f;

        if (inLane && !wasLane)
        {
            crossingValid = false;
            bool safeStop = forklift.IsStopped && Time.time < ackUntil;
            if (fd <= NearRange && !safeStop)
                NearMiss("Near miss! Forklift within 6 m. Wait at the edge and signal.");
            else if (zone == Zone.Lane)
                MistakeCd("lane", "Cross a forklift lane only at the striped crossing.", 6f);
            else if (!mirrorChecked)
                board.Mistake("Check the corner mirror before you step out.");
            else if (Time.time >= ackUntil)
                board.Mistake("Look at the driver, press SIGNAL and wait before you cross.");
            else if (!forklift.IsStopped)
                board.Mistake("Wait until the forklift has stopped before you cross.");
            else
            {
                crossingValid = true;
                board.Say("Forklift stopped and driver aware. Cross now.", new Color(0.3f, 0.9f, 0.62f));
            }
        }
        else if (inLane && wasLane)
        {
            if (zone == Zone.Lane && prevZone == Zone.Crossing)
                MistakeCd("lane", "You left the crossing. Stay on the stripes.", 6f);
            if (!crossingValid && forklift.Speed > 0.6f && fd <= NearRange && Time.time > nextNear)
                NearMiss("Near miss! A moving forklift is within 6 m of you.");
        }

        if (!inLane && wasLane)
        {
            if (prevZone == Zone.Crossing && zone == Zone.Walk && feet.z > 0f && !board.IsDone(2))
                Done(2, crossingValid ? "Crossed at the stripes after eye contact and a full stop." : "Crossed, but eye contact and a full stop come first.");
            crossingValid = false;
        }
    }

    void UpdateHazards(float dt)
    {
        var cam = rig.Camera.transform;
        lookSpill = Spotted(cam, SpillPos, lookSpill, dt);
        lookUpright = Spotted(cam, UprightPos + new Vector3(0, 1.0f, 0), lookUpright, dt);
        if (!spillSpotted && lookSpill > 0.6f)
        {
            spillSpotted = true;
            board.Say("You spotted a spill on the floor. Mark it with a cone.", new Color(1f, 0.72f, 0.3f));
        }
        if (!uprightSpotted && lookUpright > 0.6f)
        {
            uprightSpotted = true;
            board.Say("You spotted a bent rack upright. Mark it with a cone.", new Color(1f, 0.72f, 0.3f));
        }

        bool s = false, u = false;
        foreach (var cone in cones)
        {
            if (cone == null || cone.transform.parent != null) continue;
            var rb = cone.GetComponent<Rigidbody>();
            if (rb.linearVelocity.sqrMagnitude > 0.05f) continue;
            Vector3 p = cone.transform.position;
            if (FlatDistance(p, SpillPos) < MarkRadius) s = true;
            if (FlatDistance(p, UprightPos) < MarkRadius) u = true;
        }
        if (s && !spillMarked) board.Say("Spill marked with a cone.", new Color(0.3f, 0.9f, 0.62f));
        if (u && !uprightMarked) board.Say("Damaged upright marked with a cone.", new Color(0.3f, 0.9f, 0.62f));
        spillMarked = s;
        uprightMarked = u;
        if (s && u && !board.IsDone(3) && !finished)
            board.SetFooter("Both hazards are coned. Now press RADIO at the supervisor station.");
    }

    float Spotted(Transform cam, Vector3 point, float timer, float dt)
    {
        Vector3 to = point + new Vector3(0, 0.3f, 0) - cam.position;
        bool looking = to.magnitude < 10f && Vector3.Angle(cam.forward, to) < 22f;
        return looking ? timer + dt : 0f;
    }

    void UpdateFlash(float dt)
    {
        if (flashT <= 0f)
        {
            if (flashRoot.activeSelf) flashRoot.SetActive(false);
            return;
        }
        flashT -= dt;
        float a = Mathf.Clamp01(flashT / 0.8f);
        flashRoot.SetActive(flashT > 0f);
        flashMat.color = new Color(1f, 0.1f, 0.1f, 0.35f * a);
        flashText.color = new Color(1f, 1f, 1f, a);
    }

    // ------------------------------------------------------------------------------------------------- screenshots

    void ShowFlashAt(Vector3 camPos, Vector3 lookAt)
    {
        Vector3 dir = (lookAt - camPos).normalized;
        flashRoot.SetActive(true);
        flashRoot.transform.SetParent(null, false);
        flashRoot.transform.SetPositionAndRotation(camPos + dir * 0.6f, Quaternion.LookRotation(dir, Vector3.up));
        flashMat.color = new Color(1f, 0.1f, 0.1f, 0.3f);
        flashText.color = Color.white;
    }

    void HideFlash()
    {
        flashRoot.transform.SetParent(rig.Camera.transform, false);
        flashRoot.transform.localPosition = new Vector3(0, 0, 0.6f);
        flashRoot.transform.localRotation = Quaternion.identity;
        flashRoot.SetActive(false);
    }

    /// <summary>
    /// Labels that turn toward Camera.main (the rig) are turned toward the shot position instead, because the
    /// screenshots use a separate capture camera.
    /// </summary>
    static void AimLabelsAt(Vector3 camPos)
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
        foreach (var bb in FindObjectsByType<WfBillboard>(FindObjectsSortMode.None))
        {
            bb.enabled = false;
            bb.transform.rotation = Quaternion.LookRotation(bb.transform.position - camPos);
        }
    }

    /// <summary>
    /// Takes one screenshot. When an XR runtime is active the capture camera (copied from the rig camera) would be
    /// driven by the headset pose and refuse a field of view, so it is switched to a plain mono camera first. The view
    /// is also pre-rendered until the editor has finished compiling shaders, so nothing shows the cyan placeholder.
    /// </summary>
    IEnumerator Snap(ShotApi api, string name, Vector3 pos, Vector3 look, float fov)
    {
        AimLabelsAt(pos);
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
        staging = true;
        forklift.Frozen = true;
        mirrorCam.enabled = true;

        yield return Snap(api, "01-warehouse-overview", new Vector3(12.8f, 6.3f, 14.8f), new Vector3(-3f, 0.4f, 1f), 70f);

        yield return Snap(api, "02-ppe-station", new Vector3(-11.2f, 1.9f, -10.9f), new Vector3(-9f, 0.9f, -8f), 60f);

        forklift.Place(new Vector3(-8.5f, 0f, 0f), Vector3.right, 0f);
        forklift.Acknowledge(60f);
        // from the crossing, looking west down the lane at the stopped forklift (the cargo stack hides it from the walkway)
        yield return Snap(api, "03-crossing-forklift-stopped", new Vector3(-3.4f, 1.7f, -1.1f), new Vector3(-8.5f, 1.3f, 0.2f), 62f);
        // the "why this matters" panel for the crossing step (the staging flag keeps Done() from showing it by itself)
        why.Show(StepWhy[2]);
        yield return Snap(api, "14-why-this-matters", new Vector3(-3.4f, 1.7f, -1.1f), new Vector3(-8.5f, 1.3f, 0.2f), 62f);
        why.HideReason();

        yield return Snap(api, "04-corner-mirror", new Vector3(-4f, 1.7f, -4.2f), new Vector3(-5.15f, 1.9f, -2.25f), 40f);

        // hazard markers: drop two cones and let them settle
        cones[0].transform.position = SpillPos + new Vector3(0.9f, 0.4f, -0.5f);
        cones[0].GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        cones[1].transform.position = UprightPos + new Vector3(-0.9f, 0.4f, -0.4f);
        cones[1].GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        yield return api.Wait(1.5f);
        // from the west end of the cross-aisle spur: the spill in front, the bent upright at the far end
        yield return Snap(api, "05-hazard-markers", new Vector3(-4.3f, 1.8f, 5.5f), new Vector3(1.5f, 0.5f, 5.8f), 62f);

        // near miss: the forklift is close and the warning flash fills the view
        forklift.Place(new Vector3(-6.3f, 0f, 0f), Vector3.right, 1.8f);
        Vector3 nearCam = new Vector3(-3.9f, 1.7f, -1.9f);
        Vector3 nearLook = new Vector3(-6.3f, 1.2f, 0f);
        ShowFlashAt(nearCam, nearLook);
        yield return Snap(api, "06-near-miss", nearCam, nearLook, 60f);
        HideFlash();

        // the player's view at the start: PPE table and sign, task board, university plate, PPE header
        forklift.Place(new Vector3(4f, 0f, 12f), Vector3.left, 0f);
        yield return Snap(api, "07-start-view", new Vector3(-9f, 1.7f, -10.6f), new Vector3(-7.6f, 2.2f, -5f), 72f);

        // blind-corner sign on the cargo stack (seen from the entry walkway) and the cone station sign
        yield return Snap(api, "08-blind-corner-sign", new Vector3(-3.4f, 1.7f, -5.8f), new Vector3(-5.4f, 1.9f, -3.2f), 55f);
        yield return Snap(api, "09-cone-station", new Vector3(-3.5f, 1.7f, 0.9f), new Vector3(-5.6f, 1.0f, 2.7f), 55f);

        // the co-worker event and awareness check, driven live by a scripted self-test (logs its score)
        yield return CoworkerShots(api);
    }
}
