using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Resistor selection and check bench. The player grounds with an ESD strap, reads a color code and confirms it with a
/// meter, chooses a resistor for a 5 V / 20 mA LED circuit using Ohm's law and a derating card, and installs an
/// anti-sulfur type for a sulfur-rich environment. General education only: not a procedure for any company or product.
/// </summary>
public class ResistorBench : MonoBehaviour, IShotScript
{
    // circuit and environment
    const double SupplyV = 5.0, LedDropV = 2.0, LedTargetA = 0.020, AmbientC = 85.0, MaxToleranceNeeded = 5.0, MaxFitWatts = 0.25;
    const float TouchDist = 0.035f, ClipRadius = 0.12f;

    // parts stocked in the drawer: three of each
    static readonly PartSpec[] BinParts =
    {
        new PartSpec(150, 5, 0.125, false),
        new PartSpec(150, 1, 0.125, true),
        new PartSpec(150, 1, 0.5, false),
        new PartSpec(150, 5, 0.0625, false),
        new PartSpec(47, 1, 0.125, false),
        new PartSpec(330, 1, 0.125, true),
    };
    static readonly PartSpec UnknownSpec = new PartSpec(4700, 5, 0.25, false);

    static readonly Color[] BandColors =
    {
        new Color(0.04f, 0.04f, 0.04f), new Color(0.4f, 0.2f, 0.08f), new Color(0.85f, 0.1f, 0.1f), new Color(0.95f, 0.5f, 0.05f),
        new Color(0.95f, 0.85f, 0.1f), new Color(0.1f, 0.6f, 0.2f), new Color(0.1f, 0.25f, 0.8f), new Color(0.5f, 0.2f, 0.7f),
        new Color(0.5f, 0.5f, 0.52f), new Color(0.95f, 0.95f, 0.95f), new Color(0.83f, 0.65f, 0.2f), new Color(0.75f, 0.75f, 0.78f),
    };
    static readonly string[] ShortNames = { "Blk", "Brn", "Red", "Org", "Yel", "Grn", "Blu", "Vio", "Gry", "Wht", "Gld", "Slv" };
    static readonly string[] MultNames = { "x1", "x10", "x100", "x1k", "x10k", "x100k", "x1M", "x10M", "x100M", "x1G", "x0.1", "x0.01" };

    class ZoneMarker
    {
        public SnapZone Zone;
        public Renderer Rend;
        public Transform T;
        public Vector3 Base;
    }

    // scene state
    XRRig rig;
    TaskBoard board;
    SnapZone strapZone, trayZone, boardResZone, ledZone;
    Grabbable strapObj;
    BenchResistor unknown;
    BenchProbe redProbe, blackProbe;
    BenchLed led1, led2;
    TextMesh meterText, psuText, psuState;
    Transform jackRed, jackBlack;
    Vector3 groundJack;
    LineRenderer strapCord, redLead, blackLead;
    Renderer switchRend;
    Material switchOn, switchOff;
    AudioSource audioSrc;
    AudioClip popClip, dingClip;
    readonly List<BenchResistor> resistors = new List<BenchResistor>();
    readonly List<ZoneMarker> markers = new List<ZoneMarker>();
    readonly HashSet<string> warned = new HashSet<string>();

    // rules state
    bool powerOn, strapWarned, answeredBands, unknownMeasured, finished;

    // ------------------------------------------------------------------------------------------------- setup

    // signage colours (University of Pittsburgh palette, darkened for panels)
    static readonly Color PanelNavy = new Color(0.03f, 0.08f, 0.2f, 0.96f);
    static readonly Color LabelDark = new Color(0.05f, 0.09f, 0.16f, 0.96f);
    static readonly Color SulfurGreen = new Color(0.35f, 0.95f, 0.5f);

    void Start()
    {
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 12f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.48f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.36f, 0.37f, 0.39f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.21f);

        audioSrc = P.Speaker(transform, null, false, 1f, 0f);
        popClip = P.Noise("pop", 0.25f, 0.6f);
        dingClip = P.Tone("ding", 880f, 0.4f, 0.3f);

        BuildRoom();
        BuildBench();
        BuildEsdStation();
        BuildBins();
        BuildReadingStation();
        BuildMeter();
        BuildBoard();
        BuildSupply();
        BuildWallPanels();
        BuildTaskBoard();

        rig = XRRig.Create(new Vector3(0, 0, -0.35f), 0f);
        rig.Area = new Bounds(new Vector3(0, 0, -0.4f), new Vector3(5f, 1f, 1.1f));
        rig.Camera.backgroundColor = new Color(0.1f, 0.11f, 0.13f);
        rig.Camera.clearFlags = CameraClearFlags.SolidColor;
        rig.LookAt(new Vector3(0, 1.0f, 0.8f));
        StartCoroutine(FitBrandText());
    }

    LineRenderer MakeLine(string name, Color c, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = c;
        lr.endColor = c;
        lr.useWorldSpace = true;
        return lr;
    }

    SnapZone MakeZone(Transform parent, string name, Vector3 pos, string id, float radius, float markerRadius)
    {
        var z = new GameObject(name);
        z.transform.SetParent(parent, false);
        z.transform.position = pos;
        var zone = z.AddComponent<SnapZone>();
        zone.Id = id;
        zone.Radius = radius;
        var marker = P.Cyl(z.transform, "Marker", Vector3.zero, markerRadius, 0.006f, Vector3.up, P.Glow(P.Gold, 1.2f), false);
        markers.Add(new ZoneMarker { Zone = zone, Rend = marker.GetComponent<Renderer>(), T = marker.transform, Base = marker.transform.localScale });
        return zone;
    }

    void BuildRoom()
    {
        var root = new GameObject("Room").transform;
        root.SetParent(transform, false);
        // sealed concrete floor, painted block wall with a dark base, acoustic-tile ceiling
        P.Box(root, "Floor", new Vector3(0, -0.1f, 0.5f), new Vector3(8f, 0.2f, 7f), P.Concrete());
        var wallTex = P.NoiseTexture("wall paint", new Color(0.76f, 0.78f, 0.8f), new Color(0.84f, 0.86f, 0.88f), 10f, 256, 1f, 21);
        P.Box(root, "Back wall", new Vector3(0, 1.6f, 1.35f), new Vector3(8f, 3.2f, 0.1f), P.Mat(Color.white, 0f, 0.18f, wallTex, 2f));
        P.Box(root, "Base trim", new Vector3(0, 0.06f, 1.295f), new Vector3(8f, 0.12f, 0.012f), P.Mat(new Color(0.16f, 0.17f, 0.19f), 0f, 0.35f), false);
        P.Box(root, "Blue band", new Vector3(0, 2.6f, 1.295f), new Vector3(8f, 0.25f, 0.01f), P.Mat(P.Royal, 0.1f, 0.4f), false);
        P.Box(root, "Gold band", new Vector3(0, 2.76f, 1.295f), new Vector3(8f, 0.04f, 0.01f), P.Mat(P.Gold, 0.3f, 0.5f), false);
        var tileTex = P.NoiseTexture("ceiling tile", new Color(0.62f, 0.63f, 0.64f), new Color(0.72f, 0.73f, 0.74f), 40f, 256, 1f, 23);
        P.Box(root, "Ceiling", new Vector3(0, 3.25f, 0.5f), new Vector3(8f, 0.1f, 7f), P.Mat(Color.white, 0f, 0.05f, tileTex, 3f), false);
        var frame = P.Mat(new Color(0.85f, 0.86f, 0.87f), 0.3f, 0.4f);
        for (int i = -1; i <= 1; i += 2)
        {
            P.Box(root, "Light frame", new Vector3(i * 1.3f, 3.19f, 0.6f), new Vector3(1.46f, 0.03f, 0.46f), frame, false);
            P.Box(root, "Light panel", new Vector3(i * 1.3f, 3.17f, 0.6f), new Vector3(1.4f, 0.02f, 0.4f), P.Glow(new Color(1f, 0.97f, 0.92f), 2.0f), false);
        }

        // a light under each ceiling fixture (no shadows, cheap), a fill from behind the player, and one
        // shadow-casting overhead key so parts and instruments sit on the bench instead of floating
        for (int i = -1; i <= 1; i += 2)
        {
            var lamp = new GameObject("Fixture light").AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.transform.position = new Vector3(i * 1.3f, 3.0f, 0.55f);
            lamp.type = LightType.Point; lamp.range = 7f; lamp.intensity = 1.0f; lamp.color = new Color(1f, 0.97f, 0.92f);
        }
        var fill = new GameObject("Room fill").AddComponent<Light>();
        fill.transform.SetParent(root, false);
        fill.transform.position = new Vector3(0, 2.4f, -1.4f);
        fill.type = LightType.Point; fill.range = 7f; fill.intensity = 0.55f; fill.color = new Color(0.92f, 0.95f, 1f);
        var key = new GameObject("Key light").AddComponent<Light>();
        key.transform.SetParent(root, false);
        key.type = LightType.Directional; key.intensity = 0.45f; key.color = new Color(1f, 0.96f, 0.9f);
        key.transform.rotation = Quaternion.Euler(70, -15, 0);
        key.shadows = LightShadows.Soft; key.shadowStrength = 0.55f;
    }

    // ------------------------------------------------------------------------------------------------- signage helpers

    /// <summary>A sign with a thin steel backing (so it has thickness from the side) and a rounded kit panel in front.</summary>
    Transform SignBoard(Transform parent, string name, Vector3 worldPos, Vector2 size, Color accent, float accentHeight = 0.07f)
    {
        var go = new GameObject(name).transform;
        go.SetParent(parent, false);
        go.position = worldPos;
        P.Box(go, "Backing", new Vector3(0, 0, 0.011f), new Vector3(size.x * 0.96f, size.y * 0.94f, 0.012f), P.DarkSteel(), false);
        Ui.Panel(go, "Panel", Vector3.zero, size, PanelNavy, accent, accentHeight);
        return go;
    }

    /// <summary>
    /// A laminated label lying flat on the bench mat: rounded panel with an accent edge and the text printed on it.
    /// Sized from the text so it never clips.
    /// </summary>
    Transform MatLabel(Transform parent, string text, Vector3 worldPos, float textHeight, Color accent, Color? textColor = null)
    {
        var lines = text.Split('\n');
        int longest = 0;
        foreach (var l in lines) longest = Mathf.Max(longest, l.Length);
        var size = new Vector2(longest * textHeight * 0.6f + textHeight * 1.6f, lines.Length * textHeight * 1.15f + textHeight * 1.3f);
        var go = new GameObject("Mat label").transform;
        go.SetParent(parent, false);
        go.position = worldPos;
        go.rotation = Quaternion.Euler(90, 0, 0); // panel faces -Z, so this makes it face up
        Ui.Panel(go, "Plate", Vector3.zero, size, LabelDark, accent, 0.14f);
        var t = P.Text(go, text, new Vector3(0, -size.y * 0.05f, -0.003f), textHeight, textColor ?? Color.white, TextAnchor.MiddleCenter, true);
        t.alignment = TextAlignment.Center;
        return go;
    }

    void BuildBench()
    {
        var root = new GameObject("Bench").transform;
        root.SetParent(transform, false);
        var grain = P.NoiseTexture("bench wood", new Color(0.42f, 0.29f, 0.17f), new Color(0.62f, 0.46f, 0.3f), 2.5f, 256, 14f, 31);
        var wood = P.Mat(Color.white, 0f, 0.35f, grain, 1f);
        P.Box(root, "Bench top", new Vector3(0, 0.87f, 0.8f), new Vector3(3.4f, 0.06f, 0.8f), wood);
        P.Box(root, "Front apron", new Vector3(0, 0.815f, 0.415f), new Vector3(3.3f, 0.05f, 0.02f), P.DarkSteel(), false);
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            P.Box(root, "Leg", new Vector3(sx * 1.6f, 0.42f, 0.8f + sz * 0.35f), new Vector3(0.06f, 0.84f, 0.06f), P.DarkSteel());
        for (int sx = -1; sx <= 1; sx += 2)
            P.Box(root, "Foot rail", new Vector3(sx * 1.6f, 0.12f, 0.8f), new Vector3(0.04f, 0.04f, 0.7f), P.DarkSteel(), false);
        P.Box(root, "Shelf", new Vector3(0, 0.15f, 0.85f), new Vector3(3.14f, 0.025f, 0.6f), P.DarkSteel(), false);
        var matTex = P.NoiseTexture("esd mat", new Color(0.13f, 0.27f, 0.24f), new Color(0.18f, 0.33f, 0.3f), 30f, 256, 1f, 33);
        P.Box(root, "ESD mat", new Vector3(0, 0.903f, 0.8f), new Vector3(3.2f, 0.006f, 0.74f), P.Mat(Color.white, 0f, 0.12f, matTex, 4f), false);
        MatLabel(root, "ESD-SAFE MAT\ngrounded through the hook", new Vector3(1.38f, 0.912f, 0.475f), 0.017f, new Color(0.3f, 0.75f, 0.45f));
    }

    void BuildEsdStation()
    {
        var root = new GameObject("ESD station").transform;
        root.SetParent(transform, false);
        var steel = P.Steel();
        groundJack = new Vector3(-1.55f, 0.91f, 0.95f);
        P.Cyl(root, "Hook post", new Vector3(-1.55f, 1.02f, 0.95f), 0.012f, 0.24f, Vector3.up, steel, false);
        P.Box(root, "Hook head", new Vector3(-1.55f, 1.15f, 0.95f), new Vector3(0.07f, 0.02f, 0.03f), P.Mat(P.Gold, 0.4f, 0.5f), false);
        P.Box(root, "Ground jack", new Vector3(-1.55f, 0.907f, 0.95f), new Vector3(0.06f, 0.012f, 0.06f), P.Painted(new Color(0.1f, 0.6f, 0.3f)), false);
        Ui.Tag(root, "GROUND HOOK", new Vector3(-1.55f, 1.15f, 0.95f), new Vector3(0.1f, 0.13f, -0.05f), 0.035f, new Color(0.3f, 0.75f, 0.45f));
        strapZone = MakeZone(root, "Strap zone", new Vector3(-1.55f, 1.12f, 0.92f), "strap", 0.22f, 0.07f);

        var go = new GameObject("ESD wrist strap");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(-1.3f, 0.93f, 0.55f);
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.14f, 0.05f, 0.14f);
        P.Cyl(go.transform, "Band", Vector3.zero, 0.055f, 0.03f, Vector3.up, P.Mat(new Color(0.15f, 0.35f, 0.85f), 0f, 0.5f), false);
        P.Cyl(go.transform, "Inner", Vector3.zero, 0.04f, 0.034f, Vector3.up, P.Mat(new Color(0.9f, 0.9f, 0.92f), 0f, 0.5f), false);
        P.Box(go.transform, "Buckle", new Vector3(0.055f, 0.0f, 0.0f), new Vector3(0.03f, 0.036f, 0.03f), P.Mat(P.Gold, 0.5f, 0.6f), false);
        strapObj = go.AddComponent<Grabbable>();
        strapObj.SnapId = "strap";
        strapCord = MakeLine("ESD cord", new Color(0.1f, 0.8f, 0.4f), 0.006f);
        MatLabel(root, "WRIST STRAP", new Vector3(-1.3f, 0.912f, 0.465f), 0.018f, new Color(0.3f, 0.75f, 0.45f));
    }

    void BuildBins()
    {
        var root = new GameObject("Parts drawer").transform;
        root.SetParent(transform, false);
        var plastic = P.Mat(new Color(0.85f, 0.87f, 0.9f), 0f, 0.4f);
        // the back row stands on a riser (a tiered parts rack) so its labels are not hidden behind the front row
        const float Riser = 0.075f;
        P.Box(root, "Riser", new Vector3(-0.96f, 0.906f + Riser / 2f, 1.04f), new Vector3(0.74f, Riser, 0.18f), P.DarkSteel());
        for (int i = 0; i < BinParts.Length; i++)
        {
            var spec = BinParts[i];
            float x = -1.2f + (i % 3) * 0.24f;
            float z = 1.04f - (i / 3) * 0.18f;
            float y = i < 3 ? Riser : 0f;
            P.Box(root, "Bin floor", new Vector3(x, 0.906f + y, z), new Vector3(0.22f, 0.012f, 0.16f), plastic);
            P.Box(root, "Bin front", new Vector3(x, 0.93f + y, z - 0.076f), new Vector3(0.22f, 0.06f, 0.008f), plastic);
            P.Box(root, "Bin back", new Vector3(x, 0.93f + y, z + 0.076f), new Vector3(0.22f, 0.06f, 0.008f), plastic);
            P.Box(root, "Bin left", new Vector3(x - 0.106f, 0.93f + y, z), new Vector3(0.008f, 0.06f, 0.16f), plastic);
            P.Box(root, "Bin right", new Vector3(x + 0.106f, 0.93f + y, z), new Vector3(0.008f, 0.06f, 0.16f), plastic);
            // label card in a holder on the bin front, tipped back so it reads from standing height
            var holder = new GameObject("Bin label").transform;
            holder.SetParent(root, false);
            holder.position = new Vector3(x, 0.945f + y, z - 0.095f);
            holder.rotation = Quaternion.Euler(38f, 0, 0);
            var acc = spec.AntiSulfur ? new Color(0.2f, 0.8f, 0.4f) : new Color(0.55f, 0.62f, 0.72f);
            Ui.Panel(holder, "Card", Vector3.zero, new Vector2(0.205f, 0.07f), LabelDark, acc, 0.12f);
            string tol = spec.TolerancePct.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            P.Text(holder, ResistorMath.FormatOhms(spec.Ohms) + " ohm " + tol + "% " + ResistorMath.WattsText(spec.RatedWatts), new Vector3(0, 0.01f, -0.004f), 0.017f, Color.white, TextAnchor.MiddleCenter, true);
            P.Text(holder, spec.AntiSulfur ? "ANTI-SULFUR" : "standard", new Vector3(0, -0.017f, -0.004f), 0.016f, spec.AntiSulfur ? SulfurGreen : new Color(0.75f, 0.8f, 0.88f), TextAnchor.MiddleCenter, spec.AntiSulfur);
            for (int k = 0; k < 3; k++)
                MakeResistor(spec, new Vector3(x - 0.01f, 0.94f + y + k * 0.012f, z - 0.045f + k * 0.045f), i * 10 + k + 1);
        }
        Ui.Tag(root, "PARTS DRAWER", new Vector3(-0.96f, 0.97f + Riser, 1.1f), new Vector3(0f, 0.2f, 0.0f), 0.035f, P.Gold);
        MatLabel(root, "Green dot on the body = ANTI-SULFUR type", new Vector3(-0.96f, 0.912f, 0.7f), 0.016f, new Color(0.2f, 0.8f, 0.4f));
    }

    BenchResistor MakeResistor(PartSpec spec, Vector3 pos, int seed)
    {
        float s = spec.RatedWatts >= 0.4 ? 1.45f : (spec.RatedWatts <= 0.07 ? 0.8f : 1f);
        float bodyLen = 0.05f * s, bodyR = 0.011f * s, leadLen = 0.035f;
        var go = new GameObject("Resistor " + ResistorMath.FormatOhms(spec.Ohms));
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.02f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(bodyLen + 2f * leadLen, 0.03f, 0.03f);

        bool precision = spec.TolerancePct <= 2.0;
        var bodyMat = P.Mat(precision ? new Color(0.45f, 0.62f, 0.78f) : new Color(0.84f, 0.72f, 0.5f), 0f, 0.45f);
        var leadMat = P.Mat(new Color(0.8f, 0.8f, 0.82f), 0.9f, 0.6f);
        P.Cyl(go.transform, "Body", Vector3.zero, bodyR, bodyLen, Vector3.right, bodyMat, false);
        P.Cyl(go.transform, "Lead A", new Vector3(-(bodyLen / 2f + leadLen / 2f), 0, 0), 0.0025f, leadLen, Vector3.right, leadMat, false);
        P.Cyl(go.transform, "Lead B", new Vector3(bodyLen / 2f + leadLen / 2f, 0, 0), 0.0025f, leadLen, Vector3.right, leadMat, false);

        int[] bands = ResistorMath.ColorBands(spec.Ohms, spec.TolerancePct);
        if (bands != null)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                float x = (i < bands.Length - 1) ? (-0.021f + i * 0.0085f) * s : 0.021f * s;
                P.Cyl(go.transform, "Band " + (i + 1), new Vector3(x, 0, 0), bodyR * 1.08f, 0.0045f * s, Vector3.right, P.Mat(BandColors[bands[i]], 0f, 0.4f), false);
            }
        }
        if (spec.AntiSulfur)
            P.Ball(go.transform, "Anti-sulfur dot", new Vector3(0, bodyR * 1.0f, 0), 0.006f, P.Mat(new Color(0.1f, 0.9f, 0.3f), 0f, 0.5f), false);

        var br = go.AddComponent<BenchResistor>();
        br.Spec = spec;
        br.BodyHalf = bodyLen / 2f;
        br.LeadLen = leadLen;
        br.Actual = ResistorMath.MeasuredOhms(spec.Ohms, spec.TolerancePct, seed);
        var g = go.AddComponent<Grabbable>();
        g.SnapId = "part";
        g.Grabbed += OnPartGrabbed;
        resistors.Add(br);
        return br;
    }

    void BuildReadingStation()
    {
        var root = new GameObject("Reading station").transform;
        root.SetParent(transform, false);
        // a white test card with a printed heading and a royal-blue rule; the unknown part lies on it
        P.Box(root, "Reading card", new Vector3(-0.4f, 0.9075f, 0.505f), new Vector3(0.22f, 0.003f, 0.15f), P.Mat(new Color(0.96f, 0.96f, 0.94f), 0f, 0.25f), false);
        P.Box(root, "Card rule", new Vector3(-0.4f, 0.9095f, 0.472f), new Vector3(0.2f, 0.001f, 0.003f), P.Mat(P.Royal), false);
        var t = P.Text(root, "UNKNOWN PART", new Vector3(-0.4f, 0.9105f, 0.452f), 0.02f, P.MediumBlue, TextAnchor.MiddleCenter, true);
        t.transform.rotation = Quaternion.Euler(90, 0, 0);
        unknown = MakeResistor(UnknownSpec, new Vector3(-0.4f, 0.93f, 0.525f), 7);

        // answer panel on two legs
        var panel = new GameObject("Answer panel").transform;
        panel.SetParent(root, false);
        panel.position = new Vector3(-0.25f, 1.1f, 1.1f);
        for (int sx = -1; sx <= 1; sx += 2)
            P.Box(panel, "Leg", new Vector3(sx * 0.28f, -0.1f, 0.012f), new Vector3(0.02f, 0.2f, 0.02f), P.DarkSteel(), false);
        P.Box(panel, "Backing", new Vector3(0, 0, 0.011f), new Vector3(0.62f, 0.28f, 0.012f), P.DarkSteel(), false);
        Ui.Panel(panel, "Plate", Vector3.zero, new Vector2(0.66f, 0.3f), PanelNavy, P.Gold, 0.08f);
        P.Text(panel, "WHAT IS THE VALUE?", new Vector3(0, 0.094f, -0.01f), 0.028f, P.Gold, TextAnchor.MiddleCenter, true);
        P.Text(panel, "read the bands, then measure", new Vector3(0, 0.062f, -0.01f), 0.018f, P.Sky, TextAnchor.MiddleCenter);
        string[] answers = { "470 ohm", "4.7 k ohm", "47 k ohm", "4.7 M ohm" };
        for (int i = 0; i < answers.Length; i++)
        {
            var pos = panel.position + new Vector3((i - 1.5f) * 0.15f, -0.008f, -0.02f);
            int idx = i;
            MakeButton(panel, answers[i], pos, new Color(0.2f, 0.5f, 0.95f), () => OnAnswer(idx));
            P.Text(panel, answers[i], new Vector3((i - 1.5f) * 0.15f, -0.09f, -0.01f), 0.024f, Color.white, TextAnchor.MiddleCenter, true);
        }

        // selection tray (a shallow white tray with a lip) and the CHECK PART sign behind it
        var trayMat = P.Mat(new Color(0.9f, 0.9f, 0.92f), 0f, 0.4f);
        P.Box(root, "Tray", new Vector3(0.2f, 0.909f, 0.78f), new Vector3(0.22f, 0.006f, 0.14f), trayMat, false);
        for (int s = -1; s <= 1; s += 2)
        {
            P.Box(root, "Tray lip", new Vector3(0.2f, 0.915f, 0.78f + s * 0.068f), new Vector3(0.22f, 0.014f, 0.004f), trayMat, false);
            P.Box(root, "Tray lip", new Vector3(0.2f + s * 0.108f, 0.915f, 0.78f), new Vector3(0.004f, 0.014f, 0.14f), trayMat, false);
        }
        trayZone = MakeZone(root, "Tray zone", new Vector3(0.2f, 0.93f, 0.78f), "part", 0.13f, 0.08f);
        MatLabel(root, "SELECTION TRAY", new Vector3(0.2f, 0.912f, 0.68f), 0.018f, P.Gold);
        SignBoard(root, "Check plate", new Vector3(0.2f, 1.012f, 0.975f), new Vector2(0.26f, 0.2f), new Color(0.1f, 0.75f, 0.35f), 0.08f);
        MakeButton(root, "CHECK", new Vector3(0.2f, 1.042f, 0.955f), new Color(0.1f, 0.75f, 0.35f), OnCheckPressed);
        P.Text(root, "CHECK PART", new Vector3(0.2f, 0.96f, 0.965f), 0.03f, Color.white, TextAnchor.MiddleCenter, true);
    }

    void BuildMeter()
    {
        var root = new GameObject("Multimeter").transform;
        root.SetParent(transform, false);
        root.position = new Vector3(0.62f, 1.05f, 0.82f);
        root.rotation = Quaternion.Euler(30, 0, 0);
        P.Box(root, "Body", Vector3.zero, new Vector3(0.2f, 0.3f, 0.04f), P.Mat(new Color(0.95f, 0.75f, 0.1f), 0.1f, 0.4f));
        P.Box(root, "Display bezel", new Vector3(0, 0.07f, -0.021f), new Vector3(0.17f, 0.085f, 0.006f), P.Mat(new Color(0.08f, 0.1f, 0.08f), 0f, 0.6f), false);
        meterText = P.Text(root, "OL", new Vector3(0, 0.068f, -0.027f), 0.036f, new Color(0.45f, 1f, 0.5f), TextAnchor.MiddleCenter, true);
        P.Text(root, "OHM", new Vector3(0.062f, 0.098f, -0.027f), 0.014f, new Color(0.45f, 1f, 0.5f), TextAnchor.MiddleCenter, true);
        P.Cyl(root, "Dial", new Vector3(0, -0.04f, -0.027f), 0.045f, 0.012f, Vector3.back, P.Mat(new Color(0.12f, 0.12f, 0.14f), 0.3f, 0.5f), false);
        P.Box(root, "Dial mark", new Vector3(0, -0.015f, -0.034f), new Vector3(0.006f, 0.03f, 0.004f), P.Mat(Color.white), false);
        P.Text(root, "RESISTANCE", new Vector3(0, -0.105f, -0.023f), 0.016f, Color.black, TextAnchor.MiddleCenter, true);
        jackRed = new GameObject("Red jack").transform;
        jackRed.SetParent(root, false);
        jackRed.localPosition = new Vector3(-0.055f, -0.14f, -0.024f);
        jackBlack = new GameObject("Black jack").transform;
        jackBlack.SetParent(root, false);
        jackBlack.localPosition = new Vector3(0.055f, -0.14f, -0.024f);
        P.Ball(jackRed, "Red socket", Vector3.zero, 0.01f, P.Mat(new Color(0.85f, 0.1f, 0.1f)), false);
        P.Ball(jackBlack, "Black socket", Vector3.zero, 0.01f, P.Mat(new Color(0.05f, 0.05f, 0.05f)), false);

        Ui.Tag(transform, "MULTIMETER", new Vector3(0.62f, 1.19f, 0.9f), new Vector3(0.0f, 0.1f, -0.02f), 0.032f, P.Gold);

        redProbe = MakeProbe(true, new Vector3(0.4f, 0.93f, 0.52f));
        blackProbe = MakeProbe(false, new Vector3(0.53f, 0.93f, 0.52f));
        redLead = MakeLine("Red lead", new Color(0.85f, 0.1f, 0.1f), 0.005f);
        blackLead = MakeLine("Black lead", new Color(0.05f, 0.05f, 0.05f), 0.005f);
        MatLabel(transform, "METER PROBES\ntouch both leads, or\nlet go near a lead to clip", new Vector3(0.12f, 0.912f, 0.52f), 0.016f, P.Gold);
    }

    BenchProbe MakeProbe(bool red, Vector3 pos)
    {
        var go = new GameObject(red ? "Red probe" : "Black probe");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0, -0.04f);
        col.size = new Vector3(0.04f, 0.04f, 0.14f);
        var c = red ? new Color(0.85f, 0.1f, 0.1f) : new Color(0.06f, 0.06f, 0.06f);
        P.Cyl(go.transform, "Handle", new Vector3(0, 0, -0.03f), 0.014f, 0.12f, Vector3.forward, P.Mat(c, 0f, 0.5f), false);
        P.Cyl(go.transform, "Needle", new Vector3(0, 0, 0.075f), 0.0035f, 0.07f, Vector3.forward, P.Steel(), false);
        var tip = new GameObject("Tip").transform;
        tip.SetParent(go.transform, false);
        tip.localPosition = new Vector3(0, 0, 0.11f);
        var bp = go.AddComponent<BenchProbe>();
        bp.Tip = tip;
        var g = go.AddComponent<Grabbable>();
        g.SnapId = "probe";
        g.Dropped += OnProbeDropped;
        g.Grabbed += x => bp.ClipRes = null;
        return bp;
    }

    void BuildBoard()
    {
        var root = new GameObject("Breadboard").transform;
        root.SetParent(transform, false);
        P.Box(root, "Board", new Vector3(1.0f, 0.91f, 0.65f), new Vector3(0.5f, 0.02f, 0.3f), P.Mat(new Color(0.93f, 0.9f, 0.82f), 0f, 0.3f));
        P.Box(root, "Plus rail", new Vector3(1.0f, 0.9205f, 0.775f), new Vector3(0.46f, 0.002f, 0.012f), P.Mat(new Color(0.85f, 0.15f, 0.15f)), false);
        P.Box(root, "Minus rail", new Vector3(1.0f, 0.9205f, 0.525f), new Vector3(0.46f, 0.002f, 0.012f), P.Mat(new Color(0.15f, 0.25f, 0.85f)), false);
        var hole = P.Mat(new Color(0.12f, 0.12f, 0.12f));
        for (int ix = 0; ix < 14; ix++)
        for (int iz = 0; iz < 4; iz++)
            P.Box(root, "Hole", new Vector3(0.78f + ix * 0.03f, 0.9205f, iz < 2 ? 0.6f + iz * 0.02f : 0.69f + (iz - 2) * 0.02f), new Vector3(0.008f, 0.002f, 0.008f), hole, false);
        boardResZone = MakeZone(root, "Resistor zone", new Vector3(0.9f, 0.93f, 0.65f), "part", 0.1f, 0.07f);
        ledZone = MakeZone(root, "LED zone", new Vector3(1.12f, 0.97f, 0.65f), "led", 0.1f, 0.05f);
        P.Text(root, "R1", new Vector3(0.9f, 0.923f, 0.58f), 0.026f, Color.black, TextAnchor.MiddleCenter, true).transform.rotation = Quaternion.Euler(90, 0, 0);
        P.Text(root, "LED1", new Vector3(1.12f, 0.923f, 0.58f), 0.026f, Color.black, TextAnchor.MiddleCenter, true).transform.rotation = Quaternion.Euler(90, 0, 0);
        P.Text(root, "5 V CIRCUIT BOARD", new Vector3(1.0f, 0.923f, 0.551f), 0.018f, Color.black, TextAnchor.MiddleCenter, true).transform.rotation = Quaternion.Euler(90, 0, 0);
        led1 = MakeLed(new Vector3(0.8f, 0.95f, 0.47f));
        led2 = MakeLed(new Vector3(0.92f, 0.95f, 0.47f));
        MatLabel(root, "LEDs (one spare)", new Vector3(1.06f, 0.912f, 0.465f), 0.016f, new Color(1f, 0.35f, 0.3f));
        Ui.Tag(root, "LED TEST BOARD", new Vector3(1.0f, 0.93f, 0.78f), new Vector3(-0.05f, 0.25f, 0.02f), 0.032f, P.Gold);
    }

    BenchLed MakeLed(Vector3 pos)
    {
        var go = new GameObject("LED");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.01f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.01f, 0);
        col.size = new Vector3(0.04f, 0.09f, 0.04f);
        var glow = P.Glow(new Color(1f, 0.1f, 0.08f), 0.2f);
        P.Cyl(go.transform, "Body", new Vector3(0, 0.02f, 0), 0.012f, 0.03f, Vector3.up, glow, false);
        var dome = P.Ball(go.transform, "Dome", new Vector3(0, 0.035f, 0), 0.012f, glow, false);
        P.Cyl(go.transform, "Flange", new Vector3(0, 0.005f, 0), 0.015f, 0.004f, Vector3.up, glow, false);
        var metal = P.Mat(new Color(0.8f, 0.8f, 0.82f), 0.9f, 0.6f);
        P.Cyl(go.transform, "Leg A", new Vector3(-0.005f, -0.02f, 0), 0.0015f, 0.05f, Vector3.up, metal, false);
        P.Cyl(go.transform, "Leg B", new Vector3(0.005f, -0.02f, 0), 0.0015f, 0.05f, Vector3.up, metal, false);
        var lightGo = new GameObject("LED light");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 0.04f, 0);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point; l.range = 0.6f; l.intensity = 0f; l.color = new Color(1f, 0.15f, 0.1f);
        l.enabled = false;

        var led = go.AddComponent<BenchLed>();
        led.Dome = dome.GetComponent<Renderer>();
        led.Lamp = l;
        led.Smoke = MakeSmoke(go.transform);
        var g = go.AddComponent<Grabbable>();
        g.SnapId = "led";
        g.Grabbed += OnPartGrabbed;
        led.Show(0f);
        return led;
    }

    ParticleSystem MakeSmoke(Transform parent)
    {
        var go = new GameObject("Smoke");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, 0.05f, 0);
        go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 2.2f;
        main.startSpeed = 0.25f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.gravityModifier = -0.03f;
        main.startColor = new Color(0.22f, 0.22f, 0.22f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 20f;
        sh.radius = 0.01f;
        go.GetComponent<ParticleSystemRenderer>().material = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftDot() };
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
        return ps;
    }

    static Texture2D softDot;
    /// <summary>Round, soft-edged particle texture so smoke reads as puffs instead of squares.</summary>
    static Texture2D SoftDot()
    {
        if (softDot != null) return softDot;
        const int n = 64;
        softDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "soft dot", wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f).magnitude / (n / 2f);
            float a = Mathf.Clamp01(1f - d);
            px[y * n + x] = new Color(1f, 1f, 1f, a * a);
        }
        softDot.SetPixels(px);
        softDot.Apply();
        return softDot;
    }

    void BuildSupply()
    {
        var root = new GameObject("Power supply").transform;
        root.SetParent(transform, false);
        P.Box(root, "Case", new Vector3(1.4f, 0.97f, 1.0f), new Vector3(0.3f, 0.14f, 0.2f), P.Mat(new Color(0.25f, 0.27f, 0.3f), 0.4f, 0.5f));
        P.Box(root, "Front panel", new Vector3(1.4f, 0.97f, 0.8995f), new Vector3(0.29f, 0.13f, 0.002f), P.Mat(new Color(0.13f, 0.14f, 0.16f), 0.5f, 0.45f), false);
        P.Box(root, "Handle", new Vector3(1.4f, 1.05f, 1.0f), new Vector3(0.22f, 0.012f, 0.03f), P.DarkSteel(), false);
        Ui.Tag(root, "5 V BENCH SUPPLY", new Vector3(1.4f, 1.056f, 1.0f), new Vector3(0.0f, 0.14f, -0.04f), 0.032f, P.Gold);
        P.Box(root, "Display bezel", new Vector3(1.4f, 1.0f, 0.896f), new Vector3(0.16f, 0.05f, 0.006f), P.Mat(new Color(0.05f, 0.07f, 0.05f)), false);
        psuText = P.Text(root, "0.00 V", new Vector3(1.4f, 1.003f, 0.89f), 0.036f, new Color(0.45f, 1f, 0.5f), TextAnchor.MiddleCenter, true);
        psuState = P.Text(root, "OUTPUT OFF", new Vector3(1.4f, 0.959f, 0.894f), 0.018f, Color.white, TextAnchor.MiddleCenter, true);
        P.Ball(root, "Plus terminal", new Vector3(1.3f, 0.935f, 0.893f), 0.01f, P.Mat(new Color(0.85f, 0.1f, 0.1f)), false);
        P.Ball(root, "Minus terminal", new Vector3(1.5f, 0.935f, 0.893f), 0.01f, P.Mat(new Color(0.05f, 0.05f, 0.05f)), false);
        switchOn = P.Glow(new Color(0.1f, 1f, 0.4f), 1.2f);
        switchOff = P.Glow(new Color(1f, 0.2f, 0.2f), 1.2f);
        var sw = P.Box(root, "Power switch", new Vector3(1.4f, 0.935f, 0.89f), new Vector3(0.06f, 0.025f, 0.015f), switchOff);
        switchRend = sw.GetComponent<Renderer>();
        var press = sw.AddComponent<Pressable>();
        press.Label = "POWER";
        press.Pressed += h => OnPowerToggled();

        var red = MakeLine("Supply plus wire", new Color(0.85f, 0.1f, 0.1f), 0.006f);
        red.SetPosition(0, new Vector3(1.3f, 0.935f, 0.893f));
        red.SetPosition(1, new Vector3(1.2f, 0.93f, 0.78f));
        var blk = MakeLine("Supply minus wire", new Color(0.05f, 0.05f, 0.05f), 0.006f);
        blk.SetPosition(0, new Vector3(1.5f, 0.935f, 0.893f));
        blk.SetPosition(1, new Vector3(1.22f, 0.93f, 0.52f));
    }

    Transform Card(Vector3 center, float w, float h, string title)
    {
        var go = new GameObject(title + " card").transform;
        go.SetParent(transform, false);
        go.position = center;
        Ui.Panel(go, "Panel", Vector3.zero, new Vector2(w, h), PanelNavy, P.Gold, 0.045f);
        P.Text(go, title, new Vector3(-w / 2f + 0.035f, h / 2f - 0.08f, -0.015f), 0.04f, P.Gold, TextAnchor.MiddleLeft, true);
        P.Box(go, "Title rule", new Vector3(0, h / 2f - 0.115f, -0.012f), new Vector3(w - 0.07f, 0.003f, 0.002f), P.Mat(new Color(0.35f, 0.45f, 0.62f)), false);
        return go;
    }

    void Seg(Transform parent, Vector2 a, Vector2 b, float thick, Material mat)
    {
        Vector2 mid = (a + b) / 2f;
        float len = Vector2.Distance(a, b);
        var go = P.Box(parent, "Line", new Vector3(mid.x, mid.y, -0.013f), new Vector3(len, thick, 0.004f), mat, false);
        go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
    }

    void BuildWallPanels()
    {
        // color code poster
        var poster = new GameObject("Color code poster").transform;
        poster.SetParent(transform, false);
        poster.position = new Vector3(-0.95f, 1.75f, 1.28f);
        Ui.Panel(poster, "Panel", Vector3.zero, new Vector2(1.52f, 1.0f), new Color(0.97f, 0.97f, 0.95f), P.Royal, 0.13f);
        P.Text(poster, "RESISTOR COLOR CODE", new Vector3(0, 0.435f, -0.015f), 0.07f, Color.white, TextAnchor.MiddleCenter, true);
        P.Box(poster, "Gold rule", new Vector3(0, 0.364f, -0.012f), new Vector3(1.44f, 0.006f, 0.002f), P.Mat(P.Gold, 0.2f, 0.5f), false);
        P.Text(poster, "color", new Vector3(-0.72f, 0.27f, -0.015f), 0.024f, Color.black, TextAnchor.MiddleLeft, true);
        P.Text(poster, "digit", new Vector3(-0.72f, 0.07f, -0.015f), 0.024f, Color.black, TextAnchor.MiddleLeft, true);
        P.Text(poster, "multiplier", new Vector3(-0.72f, -0.03f, -0.015f), 0.024f, Color.black, TextAnchor.MiddleLeft, true);
        for (int i = 0; i < 12; i++)
        {
            float x = (i - 5.5f) * 0.105f + 0.05f;
            P.Box(poster, "Swatch " + ShortNames[i], new Vector3(x, 0.24f, -0.013f), new Vector3(0.09f, 0.1f, 0.004f), P.Mat(BandColors[i], 0f, 0.3f), false);
            P.Text(poster, ShortNames[i], new Vector3(x, 0.15f, -0.015f), 0.028f, Color.black, TextAnchor.MiddleCenter, true);
            P.Text(poster, i <= 9 ? i.ToString() : "-", new Vector3(x, 0.07f, -0.015f), 0.034f, Color.black, TextAnchor.MiddleCenter, true);
            P.Text(poster, MultNames[i], new Vector3(x, -0.03f, -0.015f), 0.028f, Color.black, TextAnchor.MiddleCenter, true);
        }
        P.Text(poster, "Tolerance band:  brown 1%   red 2%   green 0.5%   blue 0.25%   violet 0.1%   gold 5%   silver 10%", new Vector3(0, -0.14f, -0.015f), 0.03f, Color.black, TextAnchor.MiddleCenter);
        P.Text(poster, "Example:  yellow violet red gold  =  4 7 x100  =  4.7 k ohm, 5%", new Vector3(0, -0.24f, -0.015f), 0.034f, P.Royal, TextAnchor.MiddleCenter, true);
        P.Text(poster, "4 bands: 2 digits, multiplier, tolerance.   5 bands: 3 digits, multiplier, tolerance.", new Vector3(0, -0.33f, -0.015f), 0.028f, Color.black, TextAnchor.MiddleCenter);
        P.Text(poster, "The tolerance band sits slightly apart from the others. Always confirm with a meter.", new Vector3(0, -0.41f, -0.015f), 0.028f, Color.black, TextAnchor.MiddleCenter);

        // circuit card
        var circuit = Card(new Vector3(0.4f, 1.75f, 1.28f), 0.62f, 0.85f, "LED CIRCUIT");
        P.Text(circuit, "Supply:  5.0 V\nLED drop:  2.0 V\nLED current:  20 mA\n\nR = (Vs - Vf) / I\nPick the nearest standard\nvalue (E24).\nTolerance: 5% or better.\nPower: P = I x I x R", new Vector3(-0.275f, 0.27f, -0.015f), 0.03f, Color.white, TextAnchor.UpperLeft);

        // datasheet card with the derating line
        var sheet = Card(new Vector3(1.07f, 1.75f, 1.28f), 0.62f, 0.85f, "DATASHEET (generic)");
        P.Text(sheet, "Rated power is given at 70 C.\nDerates linearly to 0 W at 155 C.\nThis board runs at 85 C:\nusable power = rated x 0.82.\nPads fit parts up to 1/4 W.", new Vector3(-0.275f, 0.27f, -0.015f), 0.027f, Color.white, TextAnchor.UpperLeft);
        var line = P.Mat(P.Gold, 0.2f, 0.5f);
        var axis = P.Mat(new Color(0.7f, 0.8f, 0.95f), 0f, 0.3f);
        System.Func<float, float> gx = t => -0.24f + (t - 25f) / 135f * 0.5f;
        System.Func<float, float> gy = p => -0.33f + p * 0.26f;
        Seg(sheet, new Vector2(gx(25), gy(0)), new Vector2(gx(160), gy(0)), 0.004f, axis);
        Seg(sheet, new Vector2(gx(25), gy(0)), new Vector2(gx(25), gy(1.05f)), 0.004f, axis);
        Seg(sheet, new Vector2(gx(25), gy(1)), new Vector2(gx(70), gy(1)), 0.008f, line);
        Seg(sheet, new Vector2(gx(70), gy(1)), new Vector2(gx(155), gy(0)), 0.008f, line);
        float p85 = (155f - 85f) / 85f;
        P.Ball(sheet, "85 C marker", new Vector3(gx(85), gy(p85), -0.016f), 0.014f, P.Glow(new Color(0.3f, 1f, 0.5f), 1.5f), false);
        P.Text(sheet, "85 C: 82%", new Vector3(gx(85) + 0.03f, gy(p85) + 0.04f, -0.015f), 0.022f, new Color(0.4f, 1f, 0.55f), TextAnchor.MiddleLeft, true);
        P.Text(sheet, "% of rated power", new Vector3(-0.28f, gy(1.15f), -0.015f), 0.02f, P.Sky, TextAnchor.MiddleLeft);
        P.Text(sheet, "70 C", new Vector3(gx(70), gy(0) - 0.03f, -0.015f), 0.02f, P.Sky, TextAnchor.MiddleCenter);
        P.Text(sheet, "155 C", new Vector3(gx(155), gy(0) - 0.03f, -0.015f), 0.02f, P.Sky, TextAnchor.MiddleCenter);

        // environment card
        var env = Card(new Vector3(1.74f, 1.75f, 1.28f), 0.62f, 0.85f, "ENVIRONMENT");
        P.Text(env, "Station air:\nSULFUR-RICH\n(process gases nearby).\n\nSulfur can attack the silver\nin standard terminations.\nAnti-sulfur types are built\nto resist it. Look for the\ngreen dot on the body.", new Vector3(-0.275f, 0.27f, -0.015f), 0.03f, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperLeft);

        // university / department plate on the wall facing the start position, above the cards and clear of the band text
        brandPlate = Ui.BrandPlate(transform, new Vector3(1.07f, 2.42f, 1.27f), 0.86f);
        brandWidth = 0.86f;
    }

    Transform brandPlate;
    float brandWidth;

    /// <summary>
    /// The kit's text wordmark (used when the logo file is absent) is wider than its plate with this font, so shrink
    /// any line that overruns. Runs one frame after setup, when the text meshes have bounds.
    /// </summary>
    IEnumerator FitBrandText()
    {
        yield return null;
        if (brandPlate == null) yield break;
        float max = brandWidth * 0.9f;
        foreach (var tm in brandPlate.GetComponentsInChildren<TextMesh>())
        {
            float w = tm.GetComponent<Renderer>().bounds.size.x;
            if (w > max) tm.characterSize *= max / w;
        }
    }

    void BuildTaskBoard()
    {
        var boardPos = new Vector3(-2.5f, 2.1f, 0.9f);
        var look = Quaternion.LookRotation(boardPos - new Vector3(0, 1.6f, -0.35f));
        board = TaskBoard.Create(transform, boardPos, look, "Resistor selection bench", "Generic education  |  5 V, 20 mA LED circuit", new[]
        {
            "Clip on the ESD strap before touching parts",
            "Read the bands of the unknown part, measure it, answer",
            "Work out the LED resistor; test a part in the tray",
            "Sulfur-rich air: fit an anti-sulfur part, switch on",
        }, 3.4f, 1.15f);
        board.SetFooter("Point + trigger = press. Grip = grab. Desktop: click = press, E = grab, WASD, right mouse = look.");
    }

    void MakeButton(Transform parent, string name, Vector3 pos, Color color, System.Action onPress)
    {
        var b = P.Prim(PrimitiveType.Cylinder, parent, name + " button", Vector3.zero, new Vector3(0.09f, 0.012f, 0.09f), P.Glow(color, 0.9f));
        b.transform.position = pos;
        b.transform.rotation = Quaternion.Euler(90, 0, 0);
        var p = b.AddComponent<Pressable>();
        p.Label = name;
        p.Pressed += h => onPress();
    }

    // ------------------------------------------------------------------------------------------------- events

    void OnPartGrabbed(Grabbable g)
    {
        if (strapZone.Occupant != null || strapWarned) return;
        strapWarned = true;
        board.Mistake("You touched parts without the ESD strap on. Clip it to the hook first.");
    }

    void OnProbeDropped(Grabbable g)
    {
        var probe = g.GetComponent<BenchProbe>();
        var other = probe == redProbe ? blackProbe : redProbe;
        BenchResistor bestR = null;
        int bestLead = 0;
        float best = ClipRadius;
        foreach (var r in resistors)
        {
            for (int lead = 0; lead < 2; lead++)
            {
                if (other != null && other.ClipRes == r && other.ClipLead == lead) continue;
                float d = r.DistanceToLead(lead, probe.Tip.position);
                if (d < best) { best = d; bestR = r; bestLead = lead; }
            }
        }
        probe.ClipRes = bestR;
        probe.ClipLead = bestLead;
    }

    void OnAnswer(int index)
    {
        if (board.IsDone(1)) return;
        if (index != 1)
        {
            board.Mistake("That is not what the bands say. Yellow-violet = 47, red = x100.");
            return;
        }
        answeredBands = true;
        if (!unknownMeasured) board.Say("Bands give 4.7 k ohm, 5%. Now confirm with the meter: both probes on the two leads.", new Color(0.3f, 0.9f, 0.62f));
    }

    void OnCheckPressed()
    {
        var g = trayZone.Occupant;
        var r = g != null ? g.GetComponent<BenchResistor>() : null;
        if (r == null)
        {
            board.Say("Put a resistor in the selection tray first.", new Color(1f, 0.72f, 0.3f));
            return;
        }
        var c = ResistorMath.CheckPart(r.Spec, SupplyV, LedDropV, LedTargetA, AmbientC, MaxToleranceNeeded, MaxFitWatts);
        if (c.Passed)
        {
            string msg = "Part OK: " + r.Spec.Describe() + ". Needs " + ResistorMath.FormatOhms(c.StandardOhms) + " ohm, "
                + c.RequiredWatts.ToString("0.000") + " W vs " + c.DeratedWatts.ToString("0.000") + " W derated.";
            Finish(2, msg);
        }
        else
        {
            board.Mistake(c.Problems[0]);
        }
    }

    void OnPowerToggled()
    {
        powerOn = !powerOn;
        if (!powerOn) warned.Clear();
        psuText.text = powerOn ? "5.00 V" : "0.00 V";
        psuState.text = powerOn ? "OUTPUT ON" : "OUTPUT OFF";
        switchRend.sharedMaterial = powerOn ? switchOn : switchOff;
    }

    void Finish(int index, string message)
    {
        if (board.IsDone(index)) return;
        board.Complete(index, message);
        audioSrc.PlayOneShot(dingClip);
    }

    void Warn(string key, string message, bool critical)
    {
        if (warned.Add(key)) board.Mistake(message, critical);
    }

    // ------------------------------------------------------------------------------------------------- rules

    BenchResistor FindMeasured()
    {
        Vector3 rp = redProbe.Tip.position, bp = blackProbe.Tip.position;
        foreach (var r in resistors)
        {
            bool r0 = r.DistanceToLead(0, rp) < TouchDist, r1 = r.DistanceToLead(1, rp) < TouchDist;
            bool b0 = r.DistanceToLead(0, bp) < TouchDist, b1 = r.DistanceToLead(1, bp) < TouchDist;
            if ((r0 && b1) || (r1 && b0)) return r;
        }
        return null;
    }

    void Update()
    {
        if (rig == null) return;

        foreach (var m in markers)
        {
            bool free = m.Zone.Occupant == null;
            m.Rend.enabled = free;
            if (free) m.T.localScale = m.Base * (1f + 0.1f * Mathf.Sin(Time.time * 5f));
        }

        // step 1: ESD strap
        bool strapOn = strapZone.Occupant != null;
        if (strapOn)
        {
            strapWarned = false;
            Finish(0, "Strap clipped to the ground hook. You can handle parts now.");
        }

        // meter
        var measured = FindMeasured();
        string reading = measured != null ? ResistorMath.FormatOhms(measured.Actual) : "OL";
        if (meterText.text != reading) meterText.text = reading;
        if (measured != null && measured == unknown) unknownMeasured = true;

        // step 2: bands read and confirmed by the meter
        if (!board.IsDone(1) && answeredBands && unknownMeasured && ResistorMath.Confirms(unknown.Actual, 4700.0, UnknownSpec.TolerancePct))
            Finish(1, "Meter reads " + ResistorMath.FormatOhms(unknown.Actual) + " ohm: matches the bands (4.7 k, 5%).");

        UpdateCircuit();

        if (board.AllDone && !finished)
        {
            finished = true;
            board.Say(board.Summary(), board.CriticalMisses > 0 ? new Color(1f, 0.38f, 0.38f) : new Color(0.3f, 0.9f, 0.62f));
        }
    }

    void UpdateCircuit()
    {
        var led = ledZone.Occupant != null ? ledZone.Occupant.GetComponent<BenchLed>() : null;
        var res = boardResZone.Occupant != null ? boardResZone.Occupant.GetComponent<BenchResistor>() : null;
        foreach (var l in new[] { led1, led2 }) if (l != led && !l.Burnt) l.Show(0f);
        if (led == null) return;
        if (!powerOn || led.Burnt)
        {
            if (!led.Burnt) led.Show(0f);
            return;
        }

        double amps = res == null ? double.PositiveInfinity : ResistorMath.LedCurrent(SupplyV, LedDropV, res.Actual);
        var state = ResistorMath.ClassifyLed(amps);
        if (state == LedState.Burnt)
        {
            led.Burn();
            audioSrc.PlayOneShot(popClip);
            if (res == null)
                board.Mistake("LED wired straight to 5 V with no series resistor. It burned out.", true);
            else
                board.Mistake(ResistorMath.FormatOhms(res.Actual) + " ohm lets " + (amps * 1000.0).ToString("0") + " mA through, over the LED's 30 mA limit. The LED burned out.", true);
            return;
        }

        led.Show(Mathf.Clamp((float)(amps / LedTargetA), 0.05f, 1.2f));
        string id = res.GetInstanceID().ToString();
        if (state == LedState.Dim)
            Warn("dim" + id, "LED is dim at " + (amps * 1000.0).ToString("0.0") + " mA. The resistor value is too high for 20 mA.", false);
        if (state == LedState.Over)
            Warn("over" + id, "LED current is " + (amps * 1000.0).ToString("0.0") + " mA, above the 20 mA design value. The resistor value is too low.", false);

        double watts = ResistorMath.PowerW(amps, res.Actual);
        double derated = ResistorMath.DeratedWatts(res.Spec.RatedWatts, AmbientC);
        bool powerOk = watts <= derated;
        if (!powerOk)
            Warn("heat" + id, "The resistor dissipates " + watts.ToString("0.000") + " W but its derated limit at 85 C is " + derated.ToString("0.000") + " W. It would overheat.", false);
        if (!res.Spec.AntiSulfur)
            Warn("sulfur" + id, "Sulfur-rich air: this is a standard part. Use an anti-sulfur type (green dot).", false);

        if (state == LedState.Normal && powerOk && res.Spec.AntiSulfur)
        {
            if (!board.IsDone(2))
            {
                if (warned.Add("needcheck")) board.Say("Test the part in the selection tray first (step 3).", new Color(1f, 0.72f, 0.3f));
            }
            else
            {
                Finish(3, "LED lit at about " + (amps * 1000.0).ToString("0") + " mA with an anti-sulfur " + ResistorMath.FormatOhms(res.Spec.Ohms) + " ohm part.");
            }
        }
    }

    void LateUpdate()
    {
        if (rig == null) return;
        strapCord.SetPosition(0, strapObj.transform.position);
        strapCord.SetPosition(1, groundJack);
        redLead.SetPosition(0, jackRed.position);
        redLead.SetPosition(1, redProbe.transform.TransformPoint(new Vector3(0, 0, -0.09f)));
        blackLead.SetPosition(0, jackBlack.position);
        blackLead.SetPosition(1, blackProbe.transform.TransformPoint(new Vector3(0, 0, -0.09f)));
    }

    // ------------------------------------------------------------------------------------------------- screenshots

    void Evict(SnapZone z)
    {
        var o = z.Occupant;
        if (o == null) return;
        z.Clear();
        o.Snapped = null;
        o.transform.SetParent(null, true);
        o.transform.position += new Vector3(0, 0.15f, -0.25f);
        o.GetComponent<Rigidbody>().isKinematic = false;
    }

    void Place(SnapZone z, Grabbable g)
    {
        if (z.Occupant == g) return;
        Evict(z);
        if (g.Snapped != null)
        {
            g.Snapped.Clear();
            g.Snapped = null;
        }
        g.transform.SetParent(null, true);
        g.transform.position = z.transform.position;
        SnapZone.TrySnap(g);
    }

    BenchResistor FindPart(double ohms, bool antiSulfur, double watts)
    {
        foreach (var r in resistors)
            if (r != unknown && System.Math.Abs(r.Spec.Ohms - ohms) < 0.01 && r.Spec.AntiSulfur == antiSulfur && System.Math.Abs(r.Spec.RatedWatts - watts) < 0.0001) return r;
        return null;
    }

    /// <summary>
    /// Screenshot path only: if an OpenXR runtime started (for example the editor's Mock runtime), stop it so the
    /// capture camera renders a normal desktop view with the requested field of view.
    /// </summary>
    static void StopXrForCapture()
    {
        var s = UnityEngine.XR.Management.XRGeneralSettings.Instance;
        if (s == null || s.Manager == null || !s.Manager.isInitializationComplete) return;
        s.Manager.StopSubsystems();
        s.Manager.DeinitializeLoader();
        Debug.Log("[capture] XR stopped for desktop screenshots");
    }

    /// <summary>
    /// Screenshot path only: stand the (hidden) player where the capture camera is, so the name tags turn toward the
    /// picture the way they turn toward a player, then take the shot.
    /// </summary>
    IEnumerator ShotAt(ShotApi api, string name, Vector3 position, Vector3 lookAt, float fov)
    {
        rig.transform.position += position - rig.Camera.transform.position;
        yield return null;
        yield return api.Shot(name, position, lookAt, fov);
    }

    public IEnumerator Shots(ShotApi api)
    {
        StopXrForCapture();
        // the desktop hands and pointer would sit in front of the capture camera; hide them for the screenshots
        rig.Area = null;
        rig.Left.gameObject.SetActive(false);
        rig.Right.gameObject.SetActive(false);
        yield return api.Wait(0.5f);
        // warm-up render so the editor finishes compiling shader variants (the first frame can show placeholders)
        yield return ShotAt(api, "01-bench-overview", new Vector3(-0.2f, 1.75f, -1.0f), new Vector3(-0.1f, 1.0f, 0.9f), 68f);
        yield return api.Wait(2.0f);
        yield return ShotAt(api, "01-bench-overview", new Vector3(-0.2f, 1.75f, -1.0f), new Vector3(-0.1f, 1.0f, 0.9f), 68f);

        Place(strapZone, strapObj);
        yield return api.Wait(0.3f);
        yield return ShotAt(api, "02-esd-station", new Vector3(-1.2f, 1.45f, 0.1f), new Vector3(-1.5f, 1.0f, 0.85f), 55f);

        yield return ShotAt(api, "03-color-code-reading", new Vector3(-0.35f, 1.5f, -0.2f), new Vector3(-0.5f, 1.25f, 0.8f), 62f);

        redProbe.ClipRes = unknown; redProbe.ClipLead = 0;
        blackProbe.ClipRes = unknown; blackProbe.ClipLead = 1;
        yield return api.Wait(0.4f);
        yield return ShotAt(api, "04-measuring", new Vector3(0.1f, 1.45f, -0.05f), new Vector3(0.1f, 0.98f, 0.7f), 62f);
        redProbe.Unclip();
        blackProbe.Unclip();

        var good = FindPart(150, true, 0.125);
        Place(boardResZone, good.GetComponent<Grabbable>());
        Place(ledZone, led1.GetComponent<Grabbable>());
        if (!powerOn) OnPowerToggled();
        yield return api.Wait(0.5f);
        yield return ShotAt(api, "05-led-lit", new Vector3(1.05f, 1.35f, 0.0f), new Vector3(1.05f, 0.96f, 0.75f), 52f);

        if (powerOn) OnPowerToggled();
        var bad = FindPart(47, false, 0.125);
        Place(boardResZone, bad.GetComponent<Grabbable>());
        Place(ledZone, led2.GetComponent<Grabbable>());
        yield return null;
        OnPowerToggled();
        yield return api.Wait(0.35f);
        yield return ShotAt(api, "06-burnt-led-mistake", new Vector3(1.0f, 1.25f, 0.1f), new Vector3(1.05f, 0.98f, 0.7f), 50f);

        yield return ShotAt(api, "07-wall-references", new Vector3(0.45f, 1.75f, -0.55f), new Vector3(0.45f, 1.85f, 1.28f), 72f);
    }

    // ------------------------------------------------------------------------------------------------- shared small types
    // (BenchResistor, BenchProbe, BenchLed live below)
}

/// <summary>A stocked resistor: its part data, simulated true value, and where its leads are.</summary>
public class BenchResistor : MonoBehaviour
{
    public PartSpec Spec;
    public double Actual;
    public float BodyHalf, LeadLen;

    static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
        return (p - (a + ab * t)).magnitude;
    }

    /// <summary>Distance from a world point to a lead (0 = left, 1 = right).</summary>
    public float DistanceToLead(int lead, Vector3 worldPoint)
    {
        float sign = lead == 0 ? -1f : 1f;
        Vector3 a = transform.TransformPoint(new Vector3(sign * BodyHalf, 0, 0));
        Vector3 b = transform.TransformPoint(new Vector3(sign * (BodyHalf + LeadLen), 0, 0));
        return SegmentDistance(worldPoint, a, b);
    }

    public Vector3 LeadMid(int lead)
    {
        float sign = lead == 0 ? -1f : 1f;
        return transform.TransformPoint(new Vector3(sign * (BodyHalf + LeadLen * 0.6f), 0, 0));
    }
}

/// <summary>A meter probe. When released near a lead it clips on and follows the part.</summary>
public class BenchProbe : MonoBehaviour
{
    public Transform Tip;
    public BenchResistor ClipRes;
    public int ClipLead;
    Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    public void Unclip()
    {
        ClipRes = null;
        rb.isKinematic = false;
    }

    void LateUpdate()
    {
        if (ClipRes == null || GetComponentInParent<Hand>() != null) return;
        rb.isKinematic = true;
        Quaternion rot = Quaternion.LookRotation(new Vector3(0.1f, -0.85f, 0.5f));
        transform.rotation = rot;
        transform.position = ClipRes.LeadMid(ClipLead) - rot * Tip.localPosition;
    }
}

/// <summary>The LED: glows with the current, or burns out with a puff of smoke.</summary>
public class BenchLed : MonoBehaviour
{
    public Renderer Dome;
    public Light Lamp;
    public ParticleSystem Smoke;
    public bool Burnt;

    public void Show(float level)
    {
        var m = Dome.sharedMaterial;
        var bright = new Color(1f, 0.1f, 0.08f);
        if (Burnt)
        {
            m.color = new Color(0.08f, 0.05f, 0.04f);
            m.SetColor("_EmissionColor", Color.black);
            Lamp.enabled = false;
            return;
        }
        m.color = Color.Lerp(new Color(0.35f, 0.05f, 0.05f), bright, Mathf.Clamp01(level));
        m.SetColor("_EmissionColor", bright * (level * 2.4f));
        Lamp.intensity = level * 0.7f;
        Lamp.enabled = level > 0.01f;
    }

    public void Burn()
    {
        Burnt = true;
        Show(0f);
        Smoke.Emit(40);
    }
}
