using System;
using System.Collections;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Hydrostatic test of a shell-and-tube heat exchanger. The player installs blind flanges, fills and vents the shell,
/// raises the test pressure, holds it from behind the line, and depressurizes in the right order.
/// General safety-awareness practice only: it does not replace a site procedure or test permit.
/// </summary>
public class HydroTest : MonoBehaviour, IShotScript
{
    const float TestPsi = 150f, MaxPsi = 165f, HoldSeconds = 15f, LineZ = 0.6f;

    // simulation state
    float level, pressure, hold;
    bool pump, reliefLifted, ventFirstWarned, ventOpenedBeforeFill, standClearWarned;
    int blinds;
    bool done;

    XRRig rig;
    TaskBoard board;
    Valve vent, fill, drain;
    Transform needle, waterInGlass;
    TextMesh gaugeText, weatherText;
    ParticleSystem relief, spray;
    Renderer pumpLamp;
    Weather.Reading weather;
    readonly SnapZone[] zones = new SnapZone[2];
    readonly Renderer[] zoneMarkers = new Renderer[2];

    static readonly Vector3 C = new Vector3(0, 1.15f, 2.5f); // exchanger shell center
    const float R = 0.46f, L = 3.2f;

    class Valve
    {
        public string Name; public bool Open; public Transform Wheel; public Renderer Lamp; public Action<Valve> Toggled;
        public float Angle;
    }

    // ------------------------------------------------------------------------------------------------- setup

    void Start()
    {
        QualitySettings.shadows = ShadowQuality.All;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.4f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.55f, 0.58f, 0.62f);
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 38f;

        BuildBay();
        BuildExchanger();
        BuildUtilities();
        BuildBoardAndSigns();

        rig = XRRig.Create(new Vector3(0, 0, -3.6f), 0f);
        rig.Area = new Bounds(new Vector3(0, 0, 0), new Vector3(11.4f, 1f, 10.6f));
        rig.Camera.backgroundColor = new Color(0.1f, 0.11f, 0.13f);
        rig.Camera.clearFlags = CameraClearFlags.SolidColor;

        weather = Weather.Fallback;
        UpdateWeatherText();
        StartCoroutine(Weather.Fetch(r => { weather = r; UpdateWeatherText(); }));
    }

    void BuildBay()
    {
        var root = new GameObject("Bay").transform;
        var concrete = P.Concrete();
        P.Box(root, "Floor", new Vector3(0, -0.1f, 0), new Vector3(14, 0.2f, 12), concrete);
        var wall = P.Mat(new Color(0.78f, 0.79f, 0.8f), 0f, 0.2f);
        P.Box(root, "Back wall", new Vector3(0, 3, 6.1f), new Vector3(14, 6, 0.3f), wall);
        P.Box(root, "Front wall", new Vector3(0, 3, -6.1f), new Vector3(14, 6, 0.3f), wall);
        P.Box(root, "Left wall", new Vector3(-7.1f, 3, 0), new Vector3(0.3f, 6, 12.4f), wall);
        P.Box(root, "Right wall", new Vector3(7.1f, 3, 0), new Vector3(0.3f, 6, 12.4f), wall);
        P.Box(root, "Ceiling", new Vector3(0, 6.1f, 0), new Vector3(14, 0.2f, 12), P.Mat(new Color(0.55f, 0.56f, 0.58f)), false);
        // Wainscot stripe in Pitt blue for a clean, branded look
        P.Box(root, "Blue band back", new Vector3(0, 0.9f, 5.93f), new Vector3(13.9f, 1.8f, 0.05f), P.Mat(P.Royal, 0.1f, 0.4f), false);
        P.Box(root, "Gold band back", new Vector3(0, 1.85f, 5.93f), new Vector3(13.9f, 0.08f, 0.05f), P.Mat(P.Gold, 0.3f, 0.5f), false);
        // Roof beams and light panels
        var beam = P.DarkSteel();
        for (int i = -3; i <= 3; i++) P.Box(root, "Beam", new Vector3(i * 2f, 5.8f, 0), new Vector3(0.18f, 0.4f, 11.8f), beam, false);
        for (int ix = -1; ix <= 1; ix += 2)
        for (int iz = -1; iz <= 1; iz++)
        {
            var pos = new Vector3(ix * 3f, 5.62f, iz * 3.2f);
            P.Box(root, "Light panel", pos, new Vector3(1.6f, 0.06f, 0.5f), P.Glow(new Color(1f, 0.96f, 0.88f), 2.2f), false);
            var l = new GameObject("Point").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = pos - Vector3.up * 0.4f;
            l.type = LightType.Point; l.range = 11f; l.intensity = 0.9f; l.color = new Color(1f, 0.95f, 0.86f);
        }
        var sun = new GameObject("Key light").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 0.55f; sun.color = new Color(1f, 0.94f, 0.85f);
        sun.transform.rotation = Quaternion.Euler(52, -35, 0);
        sun.shadows = LightShadows.Soft;

        // Floor markings: the stay-clear line and the exclusion zone around the vessel
        P.Box(root, "Safety line", new Vector3(0, 0.006f, LineZ), new Vector3(12.8f, 0.012f, 0.14f), P.Hazard(), false);
        var t = P.Text(root, "STAY BEHIND THIS LINE DURING THE PRESSURE HOLD", new Vector3(0, 0.014f, LineZ - 0.45f), 0.34f, P.Gold, TextAnchor.MiddleCenter, true);
        t.transform.rotation = Quaternion.Euler(90, 0, 0);
        // Rolling door on the front wall for scale
        P.Box(root, "Door", new Vector3(-3.6f, 1.6f, -5.93f), new Vector3(3.2f, 3.2f, 0.06f), P.Mat(new Color(0.45f, 0.47f, 0.5f), 0.6f, 0.5f), false);
        // Columns
        for (int i = -1; i <= 1; i += 2) P.Box(root, "Column", new Vector3(i * 6.7f, 3, -3.5f), new Vector3(0.5f, 6, 0.5f), P.Mat(new Color(0.62f, 0.63f, 0.65f)));
        // Floor drain grate
        P.Box(root, "Drain grate", new Vector3(-1.5f, 0.01f, 2.5f), new Vector3(0.7f, 0.02f, 0.7f), P.DarkSteel(), false);
    }

    void BuildExchanger()
    {
        var root = new GameObject("Heat exchanger").transform;
        var steel = P.Steel();
        var bright = P.Mat(new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.75f);
        P.Cyl(root, "Shell", C, R, L, Vector3.right, steel);
        for (int s = -1; s <= 1; s += 2)
        {
            P.Cyl(root, "Shell flange", C + Vector3.right * (s * (L / 2f - 0.05f)), R + 0.1f, 0.08f, Vector3.right, bright);
            P.Cyl(root, "Channel", C + Vector3.right * (s * (L / 2f + 0.22f)), R + 0.02f, 0.36f, Vector3.right, steel);
            var cap = P.Ball(root, "Dished head", C + Vector3.right * (s * (L / 2f + 0.4f)), R, steel);
            cap.transform.localScale = new Vector3(R * 2f * 0.5f, R * 2f, R * 2f);
            P.Cyl(root, "Saddle ring", C + Vector3.right * (s * 1.1f), R + 0.03f, 0.12f, Vector3.right, P.Painted(P.Royal));
            P.Box(root, "Saddle", new Vector3(s * 1.1f, (C.y - R) / 2f, C.z), new Vector3(0.3f, C.y - R, 0.9f), P.Painted(P.Royal));
            P.Box(root, "Saddle foot", new Vector3(s * 1.1f, 0.03f, C.z), new Vector3(0.5f, 0.06f, 1.1f), P.DarkSteel());
        }
        // Top nozzles (with blind-flange snap zones)
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.95f : 0.95f;
            var baseY = C.y + R;
            P.Cyl(root, "Nozzle", new Vector3(x, baseY + 0.2f, C.z), 0.1f, 0.4f, Vector3.up, steel);
            P.Cyl(root, "Nozzle flange", new Vector3(x, baseY + 0.4f, C.z), 0.17f, 0.05f, Vector3.up, bright);
            var z = new GameObject(i == 0 ? "Blind zone A" : "Blind zone B");
            z.transform.SetParent(root, false);
            z.transform.position = new Vector3(x, baseY + 0.455f, C.z);
            var zone = z.AddComponent<SnapZone>();
            zone.Id = "blind"; zone.Radius = 0.3f;
            int idx = i;
            zone.Snapped += (zn, g) => { blinds++; zoneMarkers[idx].enabled = false; Haptic(); };
            zones[i] = zone;
            var marker = P.Cyl(z.transform, "Marker", Vector3.zero, 0.19f, 0.01f, Vector3.up, P.Glow(P.Gold, 1.4f), false);
            zoneMarkers[i] = marker.GetComponent<Renderer>();
        }
        // Vent (top high point) and gauge connection
        P.Cyl(root, "Vent nozzle", new Vector3(0, C.y + R + 0.12f, C.z), 0.05f, 0.25f, Vector3.up, steel);
        vent = MakeValve(root, "VENT", new Vector3(0, C.y + R + 0.42f, C.z), false, v => { });
        // Bottom fill/drain header
        Pipe(root, new Vector3(0, C.y - R, C.z), new Vector3(0, 0.45f, C.z), 0.06f, steel);
        Pipe(root, new Vector3(-1.3f, 0.45f, C.z), new Vector3(3.5f, 0.45f, C.z), 0.06f, steel);
        P.Cyl(root, "Drain tail", new Vector3(-1.3f, 0.225f, C.z), 0.05f, 0.45f, Vector3.up, steel);
        fill = MakeValve(root, "FILL", new Vector3(1.6f, 0.45f, C.z), false, v => { });
        drain = MakeValve(root, "DRAIN", new Vector3(-0.8f, 0.45f, C.z), false, v => { });
        P.Text(root, "HEAT EXCHANGER  HX-101", new Vector3(0, 0.22f, C.z - 0.52f), 0.1f, P.Gold, TextAnchor.MiddleCenter, true).transform.rotation = Quaternion.identity;

        // Particle effects: leak at an open nozzle, and the relief valve jet
        spray = MakeJet(root, "Spray", new Vector3(0.95f, C.y + R + 0.5f, C.z), new Color(0.65f, 0.82f, 1f));
        relief = MakeJet(root, "Relief jet", new Vector3(-0.2f, C.y + R + 0.05f, C.z - 0.25f), new Color(0.8f, 0.9f, 1f));
        relief.transform.rotation = Quaternion.Euler(-60f, 0, 0);
        P.Cyl(root, "Relief valve", new Vector3(-0.2f, C.y + R + 0.02f, C.z - 0.2f), 0.06f, 0.2f, new Vector3(0, 0.7f, -0.7f), P.Painted(new Color(0.8f, 0.1f, 0.1f)));
    }

    void BuildUtilities()
    {
        var root = new GameObject("Utilities").transform;
        var steel = P.Steel();
        // Water tank
        P.Cyl(root, "Water tank", new Vector3(4.2f, 0.95f, C.z), 0.55f, 1.9f, Vector3.up, P.Mat(new Color(0.12f, 0.35f, 0.7f), 0.5f, 0.6f));
        P.Cyl(root, "Tank cap", new Vector3(4.2f, 1.92f, C.z), 0.5f, 0.05f, Vector3.up, steel, false);
        P.Text(root, "TEST WATER", new Vector3(4.2f, 1.3f, C.z - 0.56f), 0.09f, Color.white, TextAnchor.MiddleCenter, true);
        // Pump skid
        P.Box(root, "Skid", new Vector3(2.6f, 0.1f, 1.2f), new Vector3(1.2f, 0.2f, 0.6f), P.DarkSteel());
        P.Cyl(root, "Motor", new Vector3(2.3f, 0.45f, 1.2f), 0.2f, 0.5f, Vector3.right, P.Painted(P.Royal));
        P.Ball(root, "Pump casing", new Vector3(2.85f, 0.42f, 1.2f), 0.22f, P.Painted(new Color(0.15f, 0.15f, 0.16f)));
        Pipe(root, new Vector3(2.85f, 0.45f, 1.2f), new Vector3(2.85f, 0.45f, C.z), 0.05f, steel);
        P.Text(root, "TEST PUMP  P-101", new Vector3(2.6f, 0.78f, 0.88f), 0.07f, Color.white, TextAnchor.MiddleCenter, true);
        // Control stand with START / STOP buttons
        P.Cyl(root, "Stand", new Vector3(2.6f, 0.55f, 0.55f), 0.04f, 1.1f, Vector3.up, steel);
        var panel = P.Box(root, "Panel", new Vector3(2.6f, 1.12f, 0.55f), new Vector3(0.5f, 0.3f, 0.06f), P.Painted(new Color(0.12f, 0.13f, 0.15f)));
        MakeButton(root, "START", new Vector3(2.47f, 1.12f, 0.5f), new Color(0.1f, 0.75f, 0.35f), PumpStart);
        MakeButton(root, "STOP", new Vector3(2.73f, 1.12f, 0.5f), new Color(0.85f, 0.15f, 0.15f), () => pump = false);
        var lamp = P.Ball(root, "Pump lamp", new Vector3(2.6f, 1.31f, 0.52f), 0.035f, P.Glow(Color.red, 1.2f), false);
        pumpLamp = lamp.GetComponent<Renderer>();
        P.Text(root, "PRESSURIZE", new Vector3(2.47f, 1.03f, 0.5f), 0.045f, Color.white, TextAnchor.MiddleCenter);
        P.Text(root, "STOP", new Vector3(2.73f, 1.03f, 0.5f), 0.045f, Color.white, TextAnchor.MiddleCenter);

        // Pressure gauge on a stand facing the player
        P.Cyl(root, "Gauge post", new Vector3(0, 0.9f, 1.7f), 0.03f, 1.8f, Vector3.up, steel);
        var dial = new GameObject("Gauge").transform;
        dial.SetParent(root, false);
        dial.position = new Vector3(0, 1.85f, 1.7f);
        P.Cyl(dial, "Case", new Vector3(0, 0, 0.0f), 0.2f, 0.06f, Vector3.forward, P.Mat(new Color(0.15f, 0.15f, 0.17f), 0.7f, 0.5f), false);
        var face = P.Cyl(dial, "Face", new Vector3(0, 0, -0.032f), 0.18f, 0.01f, Vector3.forward, P.Mat(Color.white, 0f, 0.2f), false);
        for (int i = 0; i <= 10; i++)
        {
            float a = Mathf.Lerp(-135f, 135f, i / 10f);
            var tick = P.Box(dial, "Tick", Quaternion.Euler(0, 0, -a) * new Vector3(0, 0.155f, -0.04f), new Vector3(0.008f, i % 5 == 0 ? 0.035f : 0.02f, 0.004f), P.Mat(Color.black), false);
            tick.transform.localRotation = Quaternion.Euler(0, 0, -a);
            if (i % 2 == 0) P.Text(dial, (i * 30).ToString(), Quaternion.Euler(0, 0, -a) * new Vector3(0, 0.115f, -0.045f), 0.028f, Color.black, TextAnchor.MiddleCenter, true);
        }
        // Red band for the test pressure and a red line at the maximum
        var test = P.Box(dial, "Test mark", Quaternion.Euler(0, 0, -(Mathf.Lerp(-135f, 135f, TestPsi / 300f))) * new Vector3(0, 0.16f, -0.042f), new Vector3(0.012f, 0.05f, 0.004f), P.Mat(new Color(0.1f, 0.65f, 0.3f)), false);
        test.transform.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, TestPsi / 300f));
        var pivot = new GameObject("Needle pivot").transform;
        pivot.SetParent(dial, false);
        pivot.localPosition = new Vector3(0, 0, -0.05f);
        P.Box(pivot, "Needle", new Vector3(0, 0.07f, 0), new Vector3(0.008f, 0.15f, 0.004f), P.Mat(new Color(0.9f, 0.1f, 0.1f)), false);
        P.Ball(pivot, "Hub", Vector3.zero, 0.014f, P.Mat(Color.black), false);
        needle = pivot;
        gaugeText = P.Text(root, "0 PSI", new Vector3(0, 1.55f, 1.68f), 0.1f, P.Gold, TextAnchor.MiddleCenter, true);
        P.Text(root, "TEST PRESSURE 150 PSI", new Vector3(0, 1.45f, 1.68f), 0.05f, Color.white, TextAnchor.MiddleCenter);

        // Sight glass beside the vessel shows the water level
        var glassRoot = new GameObject("Sight glass").transform;
        glassRoot.SetParent(root, false);
        glassRoot.position = new Vector3(2.15f, 0.5f, C.z - 0.1f);
        P.Cyl(glassRoot, "Glass", new Vector3(0, 0.6f, 0), 0.06f, 1.2f, Vector3.up, P.Transparent(new Color(0.8f, 0.9f, 1f, 0.25f)), false);
        var water = P.Cyl(glassRoot, "Water", new Vector3(0, 0.0f, 0), 0.05f, 1.2f, Vector3.up, P.Mat(new Color(0.15f, 0.5f, 0.95f), 0f, 0.9f), false);
        waterInGlass = water.transform;
        waterInGlass.localPosition = new Vector3(0, 0, 0);
        P.Box(glassRoot, "Top cap", new Vector3(0, 1.22f, 0), new Vector3(0.16f, 0.04f, 0.16f), steel, false);
        P.Box(glassRoot, "Bottom cap", new Vector3(0, -0.02f, 0), new Vector3(0.16f, 0.04f, 0.16f), steel, false);
        P.Text(glassRoot, "LEVEL", new Vector3(0, 1.32f, -0.09f), 0.06f, Color.white, TextAnchor.MiddleCenter, true);

        // Blind flanges on a table
        P.Box(root, "Table", new Vector3(-2.6f, 0.4f, 0.2f), new Vector3(1.4f, 0.06f, 0.7f), P.Mat(new Color(0.5f, 0.38f, 0.25f), 0f, 0.3f));
        for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
            P.Box(root, "Table leg", new Vector3(-2.6f + sx * 0.62f, 0.2f, 0.2f + sz * 0.3f), new Vector3(0.06f, 0.4f, 0.06f), P.DarkSteel());
        P.Text(root, "BLIND FLANGES", new Vector3(-2.6f, 0.52f, -0.2f), 0.06f, P.Gold, TextAnchor.MiddleCenter, true);
        for (int i = 0; i < 2; i++) MakeBlind(new Vector3(-2.95f + i * 0.7f, 0.5f, 0.2f));
    }

    void BuildBoardAndSigns()
    {
        var boardPos = new Vector3(-4.2f, 2.3f, 0.2f);
        var look = Quaternion.LookRotation(boardPos - new Vector3(0, 1.6f, -3.6f));
        board = TaskBoard.Create(transform, boardPos, look, "Hydrostatic test", "Shell-and-tube heat exchanger HX-101  |  150 psi test", new[]
        {
            "Fit both blind flanges on the top nozzles",
            "Open the VENT first, then the FILL valve",
            "Close VENT when full, then close FILL",
            "Start the pump; raise to 150 psi (no more)",
            "Hold 15 s from behind the yellow line",
            "Stop pump, open DRAIN, then open VENT",
        }, 3.0f);
        board.SetFooter("Point at valves and buttons and press the trigger. Grab blind flanges with the grip.");
        // Conditions board on the right wall
        var wp = new Vector3(5.6f, 2.4f, -1.0f);
        var go = new GameObject("Conditions").transform;
        go.SetParent(transform, false);
        go.SetPositionAndRotation(wp, Quaternion.LookRotation(wp - new Vector3(0, 1.6f, -3.6f)));
        P.Box(go, "Frame", new Vector3(0, 0, 0.02f), new Vector3(2.0f, 0.9f, 0.03f), P.Mat(P.Gold, 0.3f, 0.5f), false);
        P.Box(go, "Panel", Vector3.zero, new Vector3(1.96f, 0.86f, 0.03f), P.Mat(new Color(0.03f, 0.08f, 0.2f)), false);
        P.Text(go, "BAY CONDITIONS", new Vector3(-0.9f, 0.28f, -0.02f), 0.09f, P.Gold, TextAnchor.MiddleLeft, true);
        weatherText = P.Text(go, "", new Vector3(-0.9f, 0.0f, -0.02f), 0.06f, Color.white, TextAnchor.MiddleLeft);
    }

    void UpdateWeatherText()
    {
        if (!weatherText) return;
        weatherText.text = $"Outside: {weather.TemperatureF:F0} F, wind {weather.WindMph:F0} mph from {weather.WindFromDegrees:F0} deg\nSource: {weather.Source}\nWater temperature should be above 40 F.";
    }

    // ------------------------------------------------------------------------------------------------- parts

    Valve MakeValve(Transform parent, string name, Vector3 pos, bool open, Action<Valve> onToggle)
    {
        var v = new Valve { Name = name, Open = open };
        var root = new GameObject(name + " valve").transform;
        root.SetParent(parent, false);
        root.position = pos;
        P.Box(root, "Body", Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), P.Painted(new Color(0.75f, 0.1f, 0.1f)), false);
        P.Cyl(root, "Stem", new Vector3(0, 0.12f, 0), 0.012f, 0.2f, Vector3.up, P.Steel(), false);
        var wheel = new GameObject("Wheel").transform;
        wheel.SetParent(root, false);
        wheel.localPosition = new Vector3(0, 0.22f, 0);
        P.Cyl(wheel, "Rim", Vector3.zero, 0.11f, 0.02f, Vector3.up, P.Painted(new Color(0.9f, 0.15f, 0.12f)));
        P.Box(wheel, "Spoke A", Vector3.zero, new Vector3(0.22f, 0.018f, 0.02f), P.Painted(new Color(0.9f, 0.15f, 0.12f)), false);
        P.Box(wheel, "Spoke B", Vector3.zero, new Vector3(0.02f, 0.018f, 0.22f), P.Painted(new Color(0.9f, 0.15f, 0.12f)), false);
        // A bigger invisible target makes the wheel easy to point at
        var target = P.Prim(PrimitiveType.Sphere, wheel, "Target", Vector3.zero, Vector3.one * 0.3f, null);
        target.GetComponent<Renderer>().enabled = false;
        var press = target.AddComponent<Pressable>();
        press.Label = name;
        var lamp = P.Ball(root, "Lamp", new Vector3(0, 0.0f, -0.09f), 0.03f, P.Glow(Color.red, 1.2f), false);
        P.Text(root, name, new Vector3(0, 0.45f, 0), 0.06f, Color.white, TextAnchor.MiddleCenter, true).gameObject.AddComponent<Billboard>();
        v.Wheel = wheel; v.Lamp = lamp.GetComponent<Renderer>(); v.Toggled = onToggle;
        press.Pressed += h => ToggleValve(v);
        PaintLamp(v);
        return v;
    }

    void ToggleValve(Valve v)
    {
        v.Open = !v.Open;
        PaintLamp(v);
        v.Toggled?.Invoke(v);
        OnValveChanged(v);
    }

    void SetValve(Valve v, bool open)
    {
        v.Open = open;
        PaintLamp(v);
        v.Angle = open ? 180f : 0f;
        v.Wheel.localRotation = Quaternion.Euler(0, v.Angle, 0);
    }

    void PaintLamp(Valve v)
    {
        var c = v.Open ? new Color(0.2f, 1f, 0.45f) : new Color(1f, 0.2f, 0.2f);
        v.Lamp.material.color = c * 0.3f;
        v.Lamp.material.SetColor("_EmissionColor", c * 1.4f);
    }

    void MakeButton(Transform parent, string name, Vector3 pos, Color color, Action onPress)
    {
        var b = P.Prim(PrimitiveType.Cylinder, parent, name + " button", pos, new Vector3(0.1f, 0.012f, 0.1f), P.Glow(color, 0.9f));
        b.transform.rotation = Quaternion.Euler(90, 0, 0);
        var p = b.AddComponent<Pressable>();
        p.Label = name;
        p.Pressed += h => onPress();
    }

    void MakeBlind(Vector3 pos)
    {
        var go = new GameObject("Blind flange");
        go.transform.position = pos;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 2f;
        var disc = P.Cyl(go.transform, "Disc", Vector3.zero, 0.17f, 0.04f, Vector3.up, P.Mat(new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.7f), false);
        P.Cyl(go.transform, "Boss", new Vector3(0, 0.03f, 0), 0.05f, 0.03f, Vector3.up, P.Steel(), false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            P.Cyl(go.transform, "Bolt", new Vector3(Mathf.Cos(a) * 0.13f, 0.025f, Mathf.Sin(a) * 0.13f), 0.012f, 0.025f, Vector3.up, P.DarkSteel(), false);
        }
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.36f, 0.06f, 0.36f);
        var g = go.AddComponent<Grabbable>();
        g.SnapId = "blind";
    }

    void Pipe(Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
    {
        P.Cyl(parent, "Pipe", (a + b) / 2f, radius, Vector3.Distance(a, b), b - a, mat, false);
    }

    ParticleSystem MakeJet(Transform parent, string name, Vector3 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(-90, 0, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.9f; main.startSpeed = 4f; main.startSize = 0.05f; main.gravityModifier = 0.8f;
        main.startColor = color; main.loop = true; main.playOnAwake = false;
        var em = ps.emission; em.rateOverTime = 90;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.02f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Sprites/Default"));
        ps.Stop();
        return ps;
    }

    void Haptic() { rig?.Right.Haptic(0.5f, 0.1f); }

    // ------------------------------------------------------------------------------------------------- rules

    void PumpStart()
    {
        pump = true;
        if (blinds < 2) { board.Mistake("The nozzles are open. Fit both blind flanges before pressurizing.", true); }
        else if (vent.Open) board.Mistake("The vent is open. Close it once the vessel is full.");
        else if (level < 0.99f) board.Mistake("The vessel is not full. Air trapped in the shell is dangerous under pressure.");
    }

    void OnValveChanged(Valve v)
    {
        if (v == fill && v.Open && !vent.Open && !ventOpenedBeforeFill && !ventFirstWarned)
        {
            ventFirstWarned = true;
            board.Mistake("Open the vent first so air can escape while the vessel fills.");
        }
        if (v == vent && v.Open && fill.Open == false) ventOpenedBeforeFill = true;
        if (v == vent && v.Open && pressure > 20f)
            board.Mistake("Release pressure through the drain first; open the vent last.");
        if (v == drain && v.Open && pressure > 5f && board.IsDone(3) == false)
            board.Mistake("Draining under test pressure before the hold is complete.");
    }

    void Update()
    {
        if (rig == null) return;
        float dt = Time.deltaTime;

        // fill
        if (fill.Open && level < 1f && !drain.Open) level = Mathf.Min(1f, level + dt * 0.12f);
        if (drain.Open && level > 0f) level = Mathf.Max(0f, level - dt * 0.2f);
        // pressure
        bool closedSystem = !vent.Open && !drain.Open && blinds >= 2;
        bool full = level >= 0.99f;
        if (pump && closedSystem && full)
        {
            if (pressure < TestPsi) pressure = Mathf.Min(TestPsi + 25f, pressure + dt * 11f);
            else if (!board.IsDone(3) || pressure < TestPsi + 0.5f) pressure = TestPsi; // regulator holds the test pressure
        }
        else if (pump && !closedSystem && pressure > 1f) { /* leaking out */ }
        if (drain.Open && pressure > 0f) pressure = Mathf.Max(0f, pressure - dt * 28f);
        if (vent.Open && pressure > 0f) pressure = Mathf.Max(0f, pressure - dt * 45f);
        if (!pump && closedSystem && pressure > 0f) pressure = Mathf.Max(0f, pressure - dt * 0.05f);
        if (pressure > MaxPsi)
        {
            if (!reliefLifted) { reliefLifted = true; board.Mistake("Over-pressure: the relief valve lifted. Never exceed the test pressure.", true); }
            relief.Play();
            pressure -= dt * 60f;
        }
        else if (relief.isPlaying) relief.Stop();
        bool open = blinds < 2 && pump && full;
        if (open && !spray.isPlaying) spray.Play(); else if (!open && spray.isPlaying) spray.Stop();

        // visuals
        needle.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, pressure / 300f));
        gaugeText.text = $"{pressure:F0} PSI";
        waterInGlass.localScale = new Vector3(0.1f, Mathf.Max(0.001f, level * 0.6f), 0.1f);
        waterInGlass.localPosition = new Vector3(0, level * 0.6f, 0);
        pumpLamp.material.SetColor("_EmissionColor", (pump ? Color.green : Color.red) * 1.4f);
        foreach (var v in new[] { vent, fill, drain })
        {
            float target = v.Open ? 180f : 0f;
            v.Angle = Mathf.MoveTowards(v.Angle, target, dt * 420f);
            v.Wheel.localRotation = Quaternion.Euler(0, v.Angle, 0);
        }
        for (int i = 0; i < 2; i++) if (zoneMarkers[i].enabled) zoneMarkers[i].transform.localScale = new Vector3(0.38f, 0.005f, 0.38f) * (1f + 0.1f * Mathf.Sin(Time.time * 5f));

        UpdateSteps(dt);
    }

    void UpdateSteps(float dt)
    {
        if (done) return;
        if (!board.IsDone(0) && blinds >= 2) board.Complete(0, "Blind flanges fitted.");
        if (board.IsDone(0) && !board.IsDone(1) && vent.Open && fill.Open) board.Complete(1, "Vent and fill open. Watch the sight glass.");
        if (board.IsDone(1) && !board.IsDone(2) && level >= 0.99f && !vent.Open && !fill.Open) board.Complete(2, "Full and sealed.");
        if (board.IsDone(2) && !board.IsDone(3) && pressure >= TestPsi - 0.5f) board.Complete(3, "At test pressure: 150 psi.");
        if (board.IsDone(3) && !board.IsDone(4))
        {
            bool behind = rig.FeetPosition.z < LineZ - 0.15f;
            if (!behind)
            {
                hold = 0f;
                if (!standClearWarned) { standClearWarned = true; board.Mistake("Stay behind the yellow line while the vessel is under pressure.", true); }
                else board.Say("Step back behind the yellow line. The hold restarts.", new Color(1f, 0.72f, 0.3f));
            }
            else if (pressure >= TestPsi - 3f)
            {
                hold += dt;
                board.SetFooter($"Holding: {Mathf.Min(hold, HoldSeconds):F0} / {HoldSeconds:F0} s   Mistakes: {board.Mistakes}   Critical: {board.CriticalMisses}");
                if (hold >= HoldSeconds) board.Complete(4, "Held 15 s. No leaks seen at the gauge.");
            }
        }
        if (board.IsDone(4) && !board.IsDone(5) && !pump && pressure < 1f && drain.Open && vent.Open) board.Complete(5, "Depressurized and vented.");
        if (board.AllDone && !done)
        {
            done = true;
            board.Say(board.Summary(), board.CriticalMisses > 0 ? new Color(1f, 0.38f, 0.38f) : new Color(0.3f, 0.9f, 0.62f));
        }
    }

    // ------------------------------------------------------------------------------------------------- screenshots

    void Stage(bool ventOpen, bool fillOpen, bool drainOpen, float lvl, float psi, bool pumpOn, int blindCount)
    {
        SetValve(vent, ventOpen); SetValve(fill, fillOpen); SetValve(drain, drainOpen);
        level = lvl; pressure = psi; pump = pumpOn;
        if (blindCount >= 2 && blinds < 2)
        {
            foreach (var g in FindObjectsByType<Grabbable>(FindObjectsSortMode.None))
                if (g.Snapped == null && !SnapZone_TrySnap(g)) { }
        }
    }

    bool SnapZone_TrySnap(Grabbable g)
    {
        for (int i = 0; i < 2; i++)
        {
            if (zones[i].Occupant != null) continue;
            g.transform.position = zones[i].transform.position;
            return SnapZone.TrySnap(g);
        }
        return false;
    }

    public IEnumerator Shots(ShotApi api)
    {
        var look = new Vector3(0.4f, 1.0f, 2.6f);
        yield return api.Shot("01-test-bay", new Vector3(-5.6f, 3.1f, -4.6f), look, 60f);
        yield return api.Shot("02-blind-flanges", new Vector3(-3.3f, 1.55f, -0.9f), new Vector3(-1.2f, 1.1f, 2.2f), 55f);
        Stage(true, true, false, 0.55f, 0f, false, 2);
        yield return api.Shot("03-filling-and-venting", new Vector3(1.6f, 1.65f, 0.2f), new Vector3(1.0f, 1.0f, 2.5f), 60f);
        Stage(false, false, false, 1f, 150f, true, 2);
        yield return api.Shot("04-test-pressure", new Vector3(0.7f, 1.9f, 0.0f), new Vector3(0.2f, 1.55f, 2.4f), 52f);
        yield return api.Shot("05-hold-from-behind-the-line", new Vector3(-0.8f, 1.7f, -3.6f), new Vector3(0.3f, 1.2f, 2.6f), 62f);
        reliefLifted = true; Stage(false, false, false, 1f, 168f, true, 2); pressure = 168f;
        yield return api.Shot("06-relief-valve-lifts", new Vector3(-1.2f, 1.95f, 0.1f), new Vector3(0f, 1.8f, 2.5f), 55f);
    }
}

/// <summary>Keeps a label facing the player.</summary>
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
