using System;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Round 3 detail for the hydrostatic test bay: pipework, tool wall, safety stations, floor markings, cable tray and duct,
/// lighting effects (light shafts, dust, reflection probe, practical lights), a pressure bar, and the sound wiring.
/// Everything is made through the kit helpers so the scene bake saves it. Wall-mounted and room-edge items are skipped when
/// <see cref="ExternalShell"/> is true (the hub supplies its own building).
/// </summary>
public partial class HydroTest
{
    Transform dRoot, motorT, hvacT;
    Renderer beacon, barFillR;
    Transform clockHour, clockMin, localNeedle, barPivot;
    ParticleSystem ventSteam;
    AudioSource humLoop, toneLoop, pourLoop, leakLoop;
    int lastTick;
    float reliefHissAt;

    // ------------------------------------------------------------------------------------------------- small helpers

    // Not marked static on purpose: static batching combines meshes at scene load, and a re-bake would then save the combined
    // mesh instead of each object's own. Dynamic batching still merges these small shared-material meshes.
    static void S(GameObject g) { }

    GameObject B(Transform p, string n, Vector3 pos, Vector3 size, Material m, float yaw = 0f)
    {
        var g = P.Box(p, n, pos, size, m, false);
        if (P.IsNew(g) && yaw != 0f) g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        S(g);
        return g;
    }

    GameObject Cy(Transform p, string n, Vector3 pos, float r, float len, Vector3 axis, Material m)
    {
        var g = P.Cyl(p, n, pos, r, len, axis, m, false);
        S(g);
        return g;
    }

    GameObject Ba(Transform p, string n, Vector3 pos, float r, Material m)
    {
        var g = P.Ball(p, n, pos, r, m, false);
        S(g);
        return g;
    }

    Transform WallGroup(string name, Vector3 pos, float yaw)
    {
        var t = P.Node(name, dRoot).transform;
        if (P.IsNew(t.gameObject)) t.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        return t;
    }

    /// <summary>A tube following a list of points: straight cylinders with ball elbows.</summary>
    void Tube(Transform p, string n, Vector3[] pts, float r, Material m)
    {
        for (int i = 0; i < pts.Length - 1; i++) Cy(p, n, (pts[i] + pts[i + 1]) / 2f, r, Vector3.Distance(pts[i], pts[i + 1]), pts[i + 1] - pts[i], m);
        for (int i = 1; i < pts.Length - 1; i++) Ba(p, n + " elbow", pts[i], r * 1.15f, m);
    }

    /// <summary>A hose lying in a sagging curve between two points, made of short cylinders, with grey end couplings.</summary>
    void Hose(Transform p, string n, Vector3 a, Vector3 b, float sag, float r, Material m, int segs = 14)
    {
        Vector3 prev = a;
        for (int i = 1; i <= segs; i++)
        {
            float t = i / (float)segs;
            var pt = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t);
            pt.y = Mathf.Max(pt.y, r);
            Cy(p, n, (prev + pt) / 2f, r, Vector3.Distance(prev, pt) + 0.01f, pt - prev, m);
            prev = pt;
        }
        var coup = P.Mat(new Color(0.6f, 0.62f, 0.65f), 0.9f, 0.5f);
        Cy(p, n + " coupling", a + (b - a).normalized * 0.04f, r * 1.5f, 0.08f, b - a, coup);
        Cy(p, n + " coupling", b - (b - a).normalized * 0.04f, r * 1.5f, 0.08f, b - a, coup);
    }

    // ------------------------------------------------------------------------------------------------- entry

    void BuildDetail()
    {
        dRoot = P.Node("Detail").transform;
        BuildVesselDetail();
        BuildFloorDetail();
        BuildPressureBar();
        if (!ExternalShell) BuildWallDetail();
        BuildLightingFx();
        BuildAudio();
    }

    // ------------------------------------------------------------------------------------------------- vessel and utilities

    void BuildVesselDetail()
    {
        var root = P.Node("Vessel detail", dRoot).transform;
        P.Static(root.gameObject, "Flange bolt", "Header bolt", "Skid bolt", "Suction hose", "Motor fin", "Header flange", "Header support", "Header clamp", "Instrument tube", "Instrument tube elbow", "Nameplate");
        var dark = P.DarkSteel();
        var steel = P.Steel();
        var copper = P.Mat(new Color(0.72f, 0.42f, 0.24f), 0.9f, 0.55f);
        var yellow = P.Painted(new Color(1f, 0.78f, 0.08f));

        // flange bolt heads around both shell flanges
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                var p = C + new Vector3(s * (L / 2f - 0.05f), Mathf.Sin(a) * (R + 0.085f), Mathf.Cos(a) * (R + 0.085f));
                Cy(root, "Flange bolt", p, 0.014f, 0.1f, Vector3.right, dark);
            }

        // nameplate on the shell, facing the player
        var plate = Ui.Panel(root, "Nameplate", new Vector3(-0.55f, 0.98f, C.z - R + 0.0f), new Vector2(0.54f, 0.22f), new Color(0.05f, 0.09f, 0.16f, 0.98f), P.Gold, 0.1f);
        P.Text(plate, "HX-101  TRAINING VESSEL", new Vector3(0, 0.025f, -0.012f), 0.034f, Color.white, TextAnchor.MiddleCenter, true, "Plate line 1");
        P.Text(plate, "TEST PRESSURE 150 PSI", new Vector3(0, -0.035f, -0.012f), 0.028f, P.Sky, TextAnchor.MiddleCenter, false, "Plate line 2");

        // local pressure gauge on a stub, reading the same pressure as the big gauge
        var lg = P.Node("Local gauge", root).transform;
        if (P.IsNew(lg.gameObject)) lg.position = new Vector3(-1.5f, 1.45f, 2.05f);
        Cy(lg, "Stub", new Vector3(0, -0.05f, 0.06f), 0.012f, 0.1f, Vector3.forward, steel);
        Cy(lg, "Case", Vector3.zero, 0.07f, 0.035f, Vector3.forward, P.Mat(new Color(0.12f, 0.12f, 0.13f), 0.8f, 0.5f));
        Cy(lg, "Face", new Vector3(0, 0, -0.019f), 0.062f, 0.004f, Vector3.forward, P.Mat(Color.white, 0f, 0.2f));
        for (int i = 0; i <= 6; i++)
        {
            float a = Mathf.Lerp(-135f, 135f, i / 6f);
            var tk = B(lg, "Tick", Quaternion.Euler(0, 0, -a) * new Vector3(0, 0.05f, -0.022f), new Vector3(0.004f, 0.012f, 0.002f), P.Mat(Color.black));
            if (P.IsNew(tk)) tk.transform.localRotation = Quaternion.Euler(0, 0, -a);
        }
        var pv = P.Node("Needle pivot", lg).transform;
        if (P.IsNew(pv.gameObject)) pv.localPosition = new Vector3(0, 0, -0.024f);
        P.Box(pv, "Needle", new Vector3(0, 0.025f, 0), new Vector3(0.004f, 0.055f, 0.002f), P.Mat(new Color(0.9f, 0.1f, 0.1f)), false);
        Cy(lg, "Glass", new Vector3(0, 0, -0.023f), 0.066f, 0.003f, Vector3.forward, P.Glass(0.12f));
        localNeedle = pv;

        // instrument tubing from a tap on the shell to the back of the big gauge
        Cy(root, "Tap", new Vector3(0.55f, C.y + R + 0.03f, C.z), 0.025f, 0.08f, Vector3.up, steel);
        Tube(root, "Instrument tube", new[]
        {
            new Vector3(0.55f, C.y + R + 0.07f, C.z), new Vector3(0.55f, 1.98f, C.z), new Vector3(0.55f, 1.98f, 1.78f),
            new Vector3(0f, 1.98f, 1.78f), new Vector3(0f, 1.87f, 1.78f),
        }, 0.007f, copper);

        // bottom header: flanged joints, supports, a drain bucket
        foreach (float x in new[] { 0.4f, 2.4f })
        {
            Cy(root, "Header flange", new Vector3(x, 0.45f, C.z), 0.1f, 0.03f, Vector3.right, steel);
            Cy(root, "Header flange", new Vector3(x + 0.045f, 0.45f, C.z), 0.1f, 0.03f, Vector3.right, steel);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f + 0.4f;
                Cy(root, "Header bolt", new Vector3(x + 0.022f, 0.45f + Mathf.Sin(a) * 0.08f, C.z + Mathf.Cos(a) * 0.08f), 0.01f, 0.09f, Vector3.right, dark);
            }
        }
        foreach (float x in new[] { -0.2f, 1.4f, 3.1f })
        {
            Cy(root, "Header support", new Vector3(x, 0.2f, C.z), 0.025f, 0.4f, Vector3.up, dark);
            B(root, "Header clamp", new Vector3(x, 0.4f, C.z), new Vector3(0.05f, 0.03f, 0.16f), dark);
        }
        var bucket = Cy(root, "Bucket", new Vector3(-1.3f, 0.13f, C.z), 0.14f, 0.26f, Vector3.up, P.Painted(new Color(0.1f, 0.35f, 0.75f)));
        Cy(root, "Bucket rim", new Vector3(-1.3f, 0.26f, C.z), 0.15f, 0.015f, Vector3.up, P.Mat(new Color(0.8f, 0.82f, 0.86f), 0.9f, 0.6f));
        Cy(root, "Bucket water", new Vector3(-1.3f, 0.2f, C.z), 0.125f, 0.01f, Vector3.up, P.Mat(new Color(0.15f, 0.4f, 0.7f), 0f, 0.95f));

        // pump skid: motor fins, junction box, coupling guard, hold-down bolts
        for (int i = 0; i < 5; i++) Cy(root, "Motor fin", new Vector3(2.15f + i * 0.07f, 0.45f, 1.2f), 0.215f, 0.018f, Vector3.right, P.Painted(new Color(0f, 0.2f, 0.58f)));
        B(root, "Junction box", new Vector3(2.3f, 0.7f, 1.2f), new Vector3(0.14f, 0.09f, 0.12f), P.Painted(new Color(0.15f, 0.16f, 0.18f)));
        Cy(root, "Conduit", new Vector3(2.3f, 0.78f, 1.2f), 0.012f, 0.12f, Vector3.up, steel);
        B(root, "Coupling guard", new Vector3(2.58f, 0.45f, 1.2f), new Vector3(0.14f, 0.24f, 0.3f), yellow);
        foreach (var sx in new[] { -0.52f, 0.52f })
            foreach (var sz in new[] { -0.22f, 0.22f })
                Cy(root, "Skid bolt", new Vector3(2.6f + sx, 0.215f, 1.2f + sz), 0.015f, 0.03f, Vector3.up, steel);
        // pump discharge flange
        Cy(root, "Pump flange", new Vector3(2.85f, 0.66f, 1.2f), 0.09f, 0.03f, Vector3.up, steel);

        // test water tank: straps, outlet stub and hose to the pump suction
        foreach (float y in new[] { 0.45f, 1.45f }) Cy(root, "Tank band", new Vector3(4.2f, y, C.z), 0.56f, 0.035f, Vector3.up, steel);
        Cy(root, "Tank outlet", new Vector3(4.2f, 0.35f, C.z - 0.6f), 0.04f, 0.2f, Vector3.forward, steel);
        Hose(root, "Suction hose", new Vector3(4.2f, 0.35f, C.z - 0.7f), new Vector3(3.08f, 0.42f, 1.2f), 0.18f, 0.03f, P.Mat(new Color(0.08f, 0.3f, 0.62f), 0f, 0.3f));
        B(root, "Tank level plate", new Vector3(4.2f, 1.2f, C.z - 0.555f), new Vector3(0.05f, 0.55f, 0.01f), P.Mat(new Color(0.85f, 0.86f, 0.9f), 0.3f, 0.4f));

        // red zone on the big gauge: 165 to 300 psi
        var dial = needle != null ? needle.parent : null;
        if (dial != null)
        {
            for (int i = 0; i < 10; i++)
            {
                float psi = Mathf.Lerp(MaxPsi, 300f, i / 9f);
                float a = Mathf.Lerp(-135f, 135f, psi / 300f);
                var seg = B(dial, "Red zone", Quaternion.Euler(0, 0, -a) * new Vector3(0, 0.175f, -0.041f), new Vector3(0.016f, 0.014f, 0.003f), P.Mat(new Color(0.85f, 0.1f, 0.1f)));
                if (P.IsNew(seg)) seg.transform.localRotation = Quaternion.Euler(0, 0, -a);
            }
            Cy(dial, "Bezel", new Vector3(0, 0, -0.03f), 0.205f, 0.012f, Vector3.forward, P.Mat(new Color(0.82f, 0.84f, 0.88f), 0.95f, 0.7f));
            Cy(dial, "Glass", new Vector3(0, 0, -0.052f), 0.19f, 0.004f, Vector3.forward, P.Glass(0.1f));
        }
    }

    // ------------------------------------------------------------------------------------------------- floor

    void BuildFloorDetail()
    {
        var root = P.Node("Floor detail", dRoot).transform;
        P.Static(root.gameObject);
        var yellow = P.Mat(new Color(1f, 0.78f, 0.08f), 0f, 0.25f);
        var green = P.Mat(new Color(0.05f, 0.5f, 0.22f), 0f, 0.25f);

        // corner brackets around the vessel exclusion area
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                var c = new Vector3(sx * 2.6f, 0.007f, 2.5f + sz * 1.25f);
                B(root, "Zone corner", c + new Vector3(-sx * 0.3f, 0, 0), new Vector3(0.6f, 0.012f, 0.1f), yellow);
                B(root, "Zone corner", c + new Vector3(0, 0, -sz * 0.3f), new Vector3(0.1f, 0.012f, 0.6f), yellow);
            }
        // spill containment tray under the pump skid
        var tray = P.Mat(new Color(0.95f, 0.72f, 0.05f), 0.1f, 0.35f);
        B(root, "Spill tray", new Vector3(2.6f, 0.02f, 1.2f), new Vector3(1.9f, 0.04f, 1.15f), tray);
        B(root, "Spill rim", new Vector3(2.6f, 0.07f, 0.64f), new Vector3(1.9f, 0.1f, 0.05f), tray);
        B(root, "Spill rim", new Vector3(2.6f, 0.07f, 1.76f), new Vector3(1.9f, 0.1f, 0.05f), tray);
        B(root, "Spill rim", new Vector3(1.65f, 0.07f, 1.2f), new Vector3(0.05f, 0.1f, 1.15f), tray);
        B(root, "Spill rim", new Vector3(3.55f, 0.07f, 1.2f), new Vector3(0.05f, 0.1f, 1.15f), tray);
        // insulating mat at the control stand, with a yellow edge
        B(root, "Mat edge", new Vector3(2.6f, 0.006f, 0.5f), new Vector3(1.04f, 0.01f, 0.84f), yellow);
        B(root, "Rubber mat", new Vector3(2.6f, 0.012f, 0.5f), new Vector3(0.96f, 0.012f, 0.76f), P.Rubber());
        // bollards
        for (int i = 0; i < 2; i++)
        {
            float z = 0.2f + i * 1.0f;
            Cy(root, "Bollard", new Vector3(3.9f, 0.45f, z - 0.2f), 0.07f, 0.9f, Vector3.up, P.Painted(new Color(1f, 0.78f, 0.08f)));
            Cy(root, "Bollard band", new Vector3(3.9f, 0.62f, z - 0.2f), 0.072f, 0.1f, Vector3.up, P.Mat(new Color(0.05f, 0.05f, 0.05f)));
        }
        // coiled test hose near the tank
        var hose = P.Mat(new Color(0.08f, 0.3f, 0.62f), 0f, 0.3f);
        Vector3 hp = new Vector3(5.4f, 0f, 3.9f);
        Vector3 prev = hp + new Vector3(0.38f, 0.035f, 0f);
        for (int i = 1; i <= 36; i++)
        {
            float a = i / 36f * Mathf.PI * 4f, rr = 0.38f - i * 0.004f;
            var pt = hp + new Vector3(Mathf.Cos(a) * rr, 0.035f + (i > 18 ? 0.05f : 0f), Mathf.Sin(a) * rr);
            Cy(root, "Hose coil", (prev + pt) / 2f, 0.03f, Vector3.Distance(prev, pt) + 0.01f, pt - prev, hose);
            prev = pt;
        }
        // drain grate and trench along the back
        B(root, "Trench frame", new Vector3(-0.2f, 0.004f, 4.65f), new Vector3(8.2f, 0.01f, 0.5f), P.DarkSteel());
        B(root, "Trench grating", new Vector3(-0.2f, 0.012f, 4.65f), new Vector3(8.0f, 0.012f, 0.4f), P.Grating(3f));
        B(root, "Rusty drain plate", new Vector3(-1.5f, 0.024f, 2.5f), new Vector3(0.6f, 0.006f, 0.6f), P.Rusty());

        if (ExternalShell) return;

        // walkways and a muster area (general-awareness markings)
        B(root, "Walkway line", new Vector3(-6.0f, 0.006f, 0f), new Vector3(0.12f, 0.01f, 11f), yellow);
        B(root, "Walkway line", new Vector3(6.1f, 0.006f, 0f), new Vector3(0.12f, 0.01f, 11f), yellow);
        B(root, "Muster box", new Vector3(-4.6f, 0.005f, -4.6f), new Vector3(1.6f, 0.01f, 1.6f), green);
        B(root, "Muster box inner", new Vector3(-4.6f, 0.007f, -4.6f), new Vector3(1.4f, 0.01f, 1.4f), P.Mat(new Color(0.1f, 0.4f, 0.2f), 0f, 0.3f));
        var mt = P.Text(root, "MUSTER", new Vector3(-4.6f, 0.014f, -4.6f), 0.2f, Color.white, TextAnchor.MiddleCenter, true);
        if (P.IsNew(mt)) mt.transform.rotation = Quaternion.Euler(90, 0, 0);
        var tt = P.Text(root, "TEST AREA  -  AUTHORIZED PERSONNEL", new Vector3(0f, 0.014f, 3.9f), 0.15f, new Color(1f, 0.78f, 0.08f), TextAnchor.MiddleCenter, true);
        if (P.IsNew(tt)) tt.transform.rotation = Quaternion.Euler(90, 0, 0);

        // pallet with drums and boxes (back right corner)
        var wood = P.Wood();
        var pallet = P.Node("Pallet", root).transform;
        if (P.IsNew(pallet.gameObject)) pallet.position = new Vector3(5.6f, 0, 5.0f);
        for (int i = 0; i < 3; i++) B(pallet, "Stringer", new Vector3(-0.5f + i * 0.5f, 0.06f, 0), new Vector3(0.1f, 0.1f, 1.0f), wood);
        for (int i = 0; i < 6; i++) B(pallet, "Deck board", new Vector3(0, 0.125f + 0.0f, -0.45f + i * 0.18f), new Vector3(1.2f, 0.025f, 0.14f), wood);
        for (int i = 0; i < 2; i++)
        {
            float x = -0.28f + i * 0.56f;
            var col = i == 0 ? new Color(0.1f, 0.3f, 0.7f) : new Color(0.7f, 0.12f, 0.1f);
            Cy(pallet, "Drum", new Vector3(x, 0.6f, -0.15f), 0.29f, 0.9f, Vector3.up, P.Mat(col, 0.45f, 0.45f));
            foreach (float y in new[] { 0.3f, 0.6f, 0.9f }) Cy(pallet, "Drum ring", new Vector3(x, y, -0.15f), 0.296f, 0.025f, Vector3.up, P.Mat(col * 0.8f, 0.5f, 0.4f));
            Cy(pallet, "Drum lid", new Vector3(x, 1.065f, -0.15f), 0.27f, 0.012f, Vector3.up, P.Mat(new Color(0.6f, 0.62f, 0.66f), 0.8f, 0.5f));
        }
        var card = P.Cardboard();
        B(pallet, "Carton", new Vector3(-0.28f, 0.34f, 0.32f), new Vector3(0.5f, 0.4f, 0.4f), card, 8f);
        B(pallet, "Carton", new Vector3(0.3f, 0.3f, 0.34f), new Vector3(0.5f, 0.32f, 0.36f), card, -12f);
        B(pallet, "Carton", new Vector3(-0.2f, 0.7f, 0.3f), new Vector3(0.4f, 0.3f, 0.34f), card, 20f);
        B(pallet, "Carton tape", new Vector3(-0.28f, 0.545f, 0.32f), new Vector3(0.06f, 0.004f, 0.4f), P.Mat(new Color(0.75f, 0.6f, 0.3f), 0f, 0.6f), 8f);

        // storage shelves (back left corner)
        var sh = P.Node("Shelves", root).transform;
        if (P.IsNew(sh.gameObject)) sh.position = new Vector3(-6.2f, 0, 5.2f);
        var frame = P.Mat(new Color(0.12f, 0.3f, 0.62f), 0.2f, 0.4f);
        foreach (float x in new[] { -0.75f, 0.75f })
            foreach (float z in new[] { -0.25f, 0.25f })
                B(sh, "Upright", new Vector3(x, 1.0f, z), new Vector3(0.05f, 2.0f, 0.05f), frame);
        for (int i = 0; i < 4; i++) B(sh, "Shelf", new Vector3(0, 0.25f + i * 0.5f, 0), new Vector3(1.55f, 0.03f, 0.55f), P.Mat(new Color(0.55f, 0.58f, 0.62f), 0.6f, 0.45f));
        var bins = new[] { new Color(0.9f, 0.7f, 0.1f), new Color(0.2f, 0.45f, 0.8f), new Color(0.75f, 0.15f, 0.12f), new Color(0.25f, 0.55f, 0.3f) };
        for (int s2 = 0; s2 < 4; s2++)
            for (int i = 0; i < 3; i++)
            {
                float w = 0.3f + 0.06f * ((s2 + i) % 3);
                B(sh, "Bin", new Vector3(-0.5f + i * 0.5f, 0.3f + s2 * 0.5f + 0.09f, 0f), new Vector3(w, 0.16f + 0.04f * (i % 2), 0.38f), P.Mat(bins[(s2 + i) % 4], 0f, 0.4f), (s2 * 7 + i * 5) % 9 - 4f);
            }

        // step ladder leaning on the left wall
        var ladder = P.Node("Step ladder", root).transform;
        if (P.IsNew(ladder.gameObject)) { ladder.position = new Vector3(-6.65f, 0, -3.0f); ladder.rotation = Quaternion.Euler(0, 90, 0); }
        var rail = P.Mat(new Color(1f, 0.78f, 0.08f), 0.2f, 0.4f);
        foreach (float x in new[] { -0.22f, 0.22f })
        {
            var r = B(ladder, "Ladder rail", new Vector3(x, 0.9f, 0), new Vector3(0.05f, 1.9f, 0.03f), rail);
            if (P.IsNew(r)) r.transform.localRotation = Quaternion.Euler(-8f, 0, 0);
        }
        for (int i = 0; i < 6; i++) B(ladder, "Ladder rung", new Vector3(0, 0.25f + i * 0.3f, 0.04f), new Vector3(0.4f, 0.03f, 0.1f), P.DiamondPlate(1f));
    }

    // ------------------------------------------------------------------------------------------------- pressure bar (UI)

    void BuildPressureBar()
    {
        var root = P.Node("Pressure bar", dRoot).transform;
        var pnl = Ui.Panel(root, "Bar panel", new Vector3(1.05f, 1.135f, 1.71f), new Vector2(1.15f, 0.2f), new Color(0.03f, 0.08f, 0.2f, 0.92f), P.Gold, 0.07f);
        float x0 = 0.55f, w = 1.0f, y = 1.155f, z = 1.695f;
        P.Box(root, "Bar track", new Vector3(x0 + w / 2f, y, z), new Vector3(w, 0.045f, 0.01f), P.Painted(new Color(0.12f, 0.14f, 0.2f)), false);
        var pivot = P.Node("Bar pivot", root).transform;
        if (P.IsNew(pivot.gameObject)) pivot.position = new Vector3(x0, y, z - 0.008f);
        var fill = P.Box(pivot, "Bar fill", new Vector3(0.5f, 0, 0), new Vector3(1f, 0.036f, 0.008f), P.Glow(new Color(0.2f, 0.85f, 0.45f), 1.3f), false);
        barPivot = pivot; barFillR = fill.GetComponent<Renderer>();
        P.Box(root, "Test mark", new Vector3(x0 + w * TestPsi / 300f, y, z - 0.012f), new Vector3(0.008f, 0.07f, 0.008f), P.Mat(new Color(0.3f, 1f, 0.5f)), false);
        P.Box(root, "Limit mark", new Vector3(x0 + w * MaxPsi / 300f, y, z - 0.012f), new Vector3(0.008f, 0.07f, 0.008f), P.Mat(new Color(1f, 0.25f, 0.2f)), false);
        P.Text(root, "0", new Vector3(x0, 1.088f, z - 0.012f), 0.032f, Color.white, TextAnchor.MiddleCenter, false, "Bar label");
        P.Text(root, "150", new Vector3(x0 + w * TestPsi / 300f, 1.088f, z - 0.012f), 0.032f, new Color(0.3f, 1f, 0.5f), TextAnchor.MiddleCenter, false, "Bar label");
        P.Text(root, "300 psi", new Vector3(x0 + w + 0.07f, 1.088f, z - 0.012f), 0.032f, Color.white, TextAnchor.MiddleRight, false, "Bar label");
    }

    // ------------------------------------------------------------------------------------------------- walls and ceiling

    void BuildWallDetail()
    {
        var steel = P.Steel();
        var dark = P.DarkSteel();
        var white = P.Mat(new Color(0.86f, 0.87f, 0.9f), 0.1f, 0.4f);
        var red = P.Painted(new Color(0.8f, 0.1f, 0.1f));
        var green = P.Painted(new Color(0.05f, 0.5f, 0.22f));
        var yellow = P.Painted(new Color(1f, 0.78f, 0.08f));

        // ---------- back wall: pipe rack, signs, clock, electrical cabinet
        var bw = WallGroup("Back wall group", new Vector3(0, 0, 5.95f), 0f);
        var pipeCols = new[] { new Color(0.12f, 0.4f, 0.8f), new Color(0.78f, 0.8f, 0.84f), new Color(0.95f, 0.75f, 0.1f), new Color(0.8f, 0.12f, 0.1f) };
        var pipeNames = new[] { "PROCESS WATER", "STEAM CONDENSATE", "INSTRUMENT AIR", "FIRE WATER" };
        for (int i = 0; i < 4; i++)
        {
            float y = 4.55f + i * 0.25f;
            Cy(bw, "Rack pipe", new Vector3(0, y, -0.28f), 0.075f, 13.0f, Vector3.right, P.Mat(pipeCols[i], 0.5f, 0.45f));
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -4.0f + i * 0.4f : 3.0f - i * 0.5f;
                Cy(bw, "Rack flange", new Vector3(x, y, -0.28f), 0.11f, 0.03f, Vector3.right, steel);
                Cy(bw, "Rack flange", new Vector3(x + 0.05f, y, -0.28f), 0.11f, 0.03f, Vector3.right, steel);
            }
            P.Text(bw, pipeNames[i], new Vector3(-1.4f + i * 0.3f, y, -0.36f), 0.05f, i == 1 ? Color.black : Color.white, TextAnchor.MiddleCenter, true, "Pipe label");
        }
        for (int i = 0; i < 6; i++)
        {
            float x = -6.0f + i * 2.4f;
            B(bw, "Pipe support", new Vector3(x, 4.95f, -0.15f), new Vector3(0.06f, 1.0f, 0.06f), dark);
            B(bw, "Pipe support arm", new Vector3(x, 4.45f, -0.22f), new Vector3(0.06f, 0.05f, 0.28f), dark);
            B(bw, "Pipe support arm", new Vector3(x, 5.4f, -0.22f), new Vector3(0.06f, 0.05f, 0.28f), dark);
        }
        // a riser down the wall with a valve and a hose bib
        Cy(bw, "Riser", new Vector3(-6.35f, 2.9f, -0.28f), 0.06f, 3.4f, Vector3.up, P.Mat(pipeCols[0], 0.5f, 0.45f));
        B(bw, "Riser valve body", new Vector3(-6.35f, 2.0f, -0.28f), new Vector3(0.16f, 0.16f, 0.16f), red);
        Cy(bw, "Riser valve stem", new Vector3(-6.35f, 2.0f, -0.4f), 0.012f, 0.16f, Vector3.back, steel);
        Cy(bw, "Riser wheel", new Vector3(-6.35f, 2.0f, -0.5f), 0.09f, 0.018f, Vector3.back, red);
        Cy(bw, "Hose bib", new Vector3(-6.35f, 1.35f, -0.36f), 0.025f, 0.15f, Vector3.back, P.Mat(new Color(0.8f, 0.55f, 0.2f), 0.9f, 0.5f));
        for (int i = 0; i < 3; i++) B(bw, "Riser clamp", new Vector3(-6.35f, 1.4f + i * 1.2f, -0.2f), new Vector3(0.14f, 0.03f, 0.18f), dark);

        // signs beside the brand plate
        var s1 = Ui.Panel(bw, "Sign authorized", new Vector3(-3.5f, 3.5f, -0.04f), new Vector2(1.9f, 0.75f), new Color(0.03f, 0.08f, 0.2f, 0.96f), P.Gold, 0.09f);
        P.Text(s1, "AUTHORIZED PERSONNEL", new Vector3(0, 0.08f, -0.012f), 0.1f, Color.white, TextAnchor.MiddleCenter, true, "Sign line");
        P.Text(s1, "Test in progress when the lamp is lit", new Vector3(0, -0.14f, -0.012f), 0.072f, P.Sky, TextAnchor.MiddleCenter, false, "Sign line");
        var s2 = Ui.Panel(bw, "Sign PPE", new Vector3(3.5f, 3.5f, -0.04f), new Vector2(1.9f, 0.75f), new Color(0.0f, 0.2f, 0.58f, 0.97f), P.Gold, 0.09f);
        P.Text(s2, "EYE AND HEARING", new Vector3(0, 0.1f, -0.012f), 0.1f, Color.white, TextAnchor.MiddleCenter, true, "Sign line");
        P.Text(s2, "PROTECTION REQUIRED", new Vector3(0, -0.06f, -0.012f), 0.085f, P.Gold, TextAnchor.MiddleCenter, true, "Sign line");
        P.Text(s2, "General safety awareness only", new Vector3(0, -0.25f, -0.012f), 0.06f, P.Sky, TextAnchor.MiddleCenter, false, "Sign line");

        // wall clock (shows the real time)
        var clk = P.Node("Clock", bw).transform;
        if (P.IsNew(clk.gameObject)) clk.localPosition = new Vector3(5.6f, 3.4f, -0.05f);
        Cy(clk, "Clock rim", Vector3.zero, 0.24f, 0.05f, Vector3.forward, P.Mat(new Color(0.12f, 0.12f, 0.13f), 0.6f, 0.5f));
        Cy(clk, "Clock face", new Vector3(0, 0, -0.026f), 0.22f, 0.006f, Vector3.forward, P.Mat(Color.white, 0f, 0.3f));
        for (int i = 0; i < 12; i++)
        {
            var tk = B(clk, "Clock tick", Quaternion.Euler(0, 0, -i * 30f) * new Vector3(0, 0.185f, -0.032f), new Vector3(0.012f, i % 3 == 0 ? 0.05f : 0.028f, 0.004f), P.Mat(Color.black));
            if (P.IsNew(tk)) tk.transform.localRotation = Quaternion.Euler(0, 0, -i * 30f);
        }
        var hp = P.Node("Hour pivot", clk).transform; if (P.IsNew(hp.gameObject)) hp.localPosition = new Vector3(0, 0, -0.034f);
        var mp = P.Node("Minute pivot", clk).transform; if (P.IsNew(mp.gameObject)) mp.localPosition = new Vector3(0, 0, -0.04f);
        P.Box(hp, "Hour hand", new Vector3(0, 0.055f, 0), new Vector3(0.016f, 0.11f, 0.004f), P.Mat(Color.black), false);
        P.Box(mp, "Minute hand", new Vector3(0, 0.085f, 0), new Vector3(0.01f, 0.17f, 0.004f), P.Mat(Color.black), false);
        clockHour = hp; clockMin = mp;

        // electrical cabinet with indicator lamps
        B(bw, "Cabinet", new Vector3(-5.3f, 1.55f, -0.17f), new Vector3(0.9f, 1.3f, 0.3f), P.Painted(new Color(0.62f, 0.64f, 0.66f)));
        B(bw, "Cabinet door seam", new Vector3(-5.3f, 1.55f, -0.325f), new Vector3(0.02f, 1.25f, 0.005f), dark);
        B(bw, "Cabinet handle", new Vector3(-5.1f, 1.55f, -0.34f), new Vector3(0.03f, 0.18f, 0.03f), steel);
        for (int i = 0; i < 3; i++)
            Ba(bw, "Cabinet lamp", new Vector3(-5.55f + i * 0.1f, 2.0f, -0.33f), 0.025f, P.Glow(i == 0 ? new Color(0.2f, 1f, 0.4f) : i == 1 ? new Color(1f, 0.7f, 0.1f) : new Color(0.3f, 0.6f, 1f), 1.1f));
        B(bw, "Cabinet label", new Vector3(-5.3f, 2.12f, -0.33f), new Vector3(0.5f, 0.07f, 0.004f), P.Mat(new Color(1f, 0.78f, 0.08f)));
        P.Text(bw, "CONTROL PANEL", new Vector3(-5.3f, 2.12f, -0.337f), 0.04f, Color.black, TextAnchor.MiddleCenter, true, "Cabinet text");
        Cy(bw, "Conduit up", new Vector3(-5.6f, 3.4f, -0.1f), 0.022f, 1.9f, Vector3.up, steel);
        Cy(bw, "Conduit across", new Vector3(-3.0f, 4.35f, -0.1f), 0.022f, 5.2f, Vector3.right, steel);

        // steam trap with a small leak at the end of the condensate line
        Cy(bw, "Steam drop", new Vector3(5.2f, 3.9f, -0.28f), 0.04f, 0.9f, Vector3.up, P.Mat(pipeCols[1], 0.5f, 0.45f));
        B(bw, "Steam trap", new Vector3(5.2f, 3.4f, -0.28f), new Vector3(0.12f, 0.14f, 0.12f), P.Painted(new Color(0.3f, 0.32f, 0.35f)));
        Fx.Steam(bw, new Vector3(5.2f, 3.3f, -0.34f), new Vector3(0.3f, 0.5f, -1f), 7f, 0.1f);

        // ---------- left wall: tool shadow board, beacon
        var lw = WallGroup("Left wall group", new Vector3(-6.95f, 0, 0), -90f);
        var boardM = P.Mat(new Color(0.13f, 0.14f, 0.16f), 0.1f, 0.3f);
        B(lw, "Tool board", new Vector3(-0.4f, 1.75f, -0.03f), new Vector3(3.2f, 1.5f, 0.04f), boardM);
        var tp = Ui.Panel(lw, "Tool board title", new Vector3(-0.4f, 2.7f, -0.05f), new Vector2(1.6f, 0.32f), new Color(0.03f, 0.08f, 0.2f, 0.96f), P.Gold, 0.1f);
        P.Text(tp, "TOOL BOARD", new Vector3(0, -0.01f, -0.012f), 0.1f, Color.white, TextAnchor.MiddleCenter, true, "Title");
        var outline = P.Mat(new Color(0.9f, 0.72f, 0.1f), 0f, 0.3f);
        var toolSteel = P.Steel();
        Func<float, float, float, float, GameObject> tool = (x, y, w, h) =>
        {
            B(lw, "Tool outline", new Vector3(x, y, -0.052f), new Vector3(w + 0.03f, h + 0.03f, 0.004f), outline);
            return B(lw, "Tool", new Vector3(x, y, -0.062f), new Vector3(w, h, 0.012f), toolSteel);
        };
        // wrenches (handle + head), hammer, screwdrivers, pliers, tape measure, hard hats, gloves
        for (int i = 0; i < 4; i++)
        {
            float x = -1.7f + i * 0.28f, len = 0.5f - i * 0.04f;
            tool(x, 1.95f, 0.045f, len);
            B(lw, "Wrench head", new Vector3(x, 1.95f + len / 2f, -0.062f), new Vector3(0.11f, 0.08f, 0.012f), toolSteel);
        }
        tool(-0.5f, 1.95f, 0.05f, 0.5f);
        B(lw, "Hammer head", new Vector3(-0.5f, 2.22f, -0.062f), new Vector3(0.22f, 0.07f, 0.02f), dark);
        for (int i = 0; i < 3; i++)
        {
            tool(-0.1f + i * 0.14f, 1.95f, 0.03f, 0.4f);
            B(lw, "Screwdriver handle", new Vector3(-0.1f + i * 0.14f, 1.72f, -0.066f), new Vector3(0.05f, 0.14f, 0.02f), P.Painted(i == 1 ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.8f, 0.12f, 0.1f)));
        }
        for (int i = 0; i < 2; i++)
        {
            tool(0.55f + i * 0.22f, 1.95f, 0.04f, 0.34f);
            B(lw, "Pliers jaw", new Vector3(0.55f + i * 0.22f, 2.15f, -0.062f), new Vector3(0.09f, 0.1f, 0.012f), dark);
        }
        B(lw, "Tape measure outline", new Vector3(-1.2f, 1.28f, -0.052f), new Vector3(0.28f, 0.22f, 0.004f), outline);
        B(lw, "Tape measure", new Vector3(-1.2f, 1.28f, -0.066f), new Vector3(0.24f, 0.18f, 0.03f), P.Painted(new Color(1f, 0.78f, 0.08f)));
        for (int i = 0; i < 3; i++)
        {
            float x = -0.5f + i * 0.45f;
            B(lw, "Hat outline", new Vector3(x, 1.3f, -0.052f), new Vector3(0.36f, 0.26f, 0.004f), outline);
            var hat = Ba(lw, "Hard hat", new Vector3(x, 1.27f, -0.12f), 0.15f, P.Mat(i == 1 ? new Color(0.95f, 0.95f, 0.95f) : new Color(1f, 0.78f, 0.08f), 0.1f, 0.6f));
            if (P.IsNew(hat)) hat.transform.localScale = new Vector3(0.3f, 0.18f, 0.2f);
            B(lw, "Hat brim", new Vector3(x, 1.2f, -0.13f), new Vector3(0.3f, 0.012f, 0.12f), P.Mat(i == 1 ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.95f, 0.72f, 0.05f), 0.1f, 0.6f));
        }
        B(lw, "Gloves outline", new Vector3(1.2f, 1.35f, -0.052f), new Vector3(0.4f, 0.3f, 0.004f), outline);
        B(lw, "Gloves", new Vector3(1.2f, 1.35f, -0.066f), new Vector3(0.34f, 0.24f, 0.02f), P.Mat(new Color(0.85f, 0.4f, 0.1f), 0f, 0.5f));
        // pegs
        for (int i = 0; i < 10; i++) Cy(lw, "Peg", new Vector3(-1.85f + i * 0.33f, 2.4f, -0.07f), 0.008f, 0.07f, Vector3.back, dark);

        // alarm beacon above the left wall (lights while the relief valve is lifting)
        var bc = P.Node("Beacon", lw).transform;
        if (P.IsNew(bc.gameObject)) bc.localPosition = new Vector3(-4.5f, 3.6f, -0.1f);
        Cy(bc, "Beacon base", Vector3.zero, 0.1f, 0.05f, Vector3.back, dark);
        var dome = Ba(bc, "Beacon dome", new Vector3(0, 0, -0.07f), 0.09f, P.Glow(new Color(0.9f, 0.1f, 0.08f), 0.15f));
        beacon = dome.GetComponent<Renderer>();
        var bp = Ui.Panel(lw, "Beacon label", new Vector3(-4.5f, 3.3f, -0.04f), new Vector2(0.9f, 0.18f), new Color(0.5f, 0.05f, 0.05f, 0.96f), null, 0f);
        P.Text(bp, "OVER-PRESSURE ALARM", new Vector3(0, 0, -0.012f), 0.04f, Color.white, TextAnchor.MiddleCenter, true, "Alarm text");

        // ---------- right wall: windows, eye wash, shower, extinguisher, first aid, PPE
        var rw = WallGroup("Right wall group", new Vector3(6.95f, 0, 0), 90f);
        for (int i = 0; i < 3; i++)
        {
            float x = -3f + i * 3f;
            B(rw, "Window frame", new Vector3(x, 4.5f, -0.03f), new Vector3(1.5f, 1.0f, 0.06f), dark);
            B(rw, "Window sky", new Vector3(x, 4.5f, -0.065f), new Vector3(1.38f, 0.88f, 0.01f), P.Glow(new Color(0.42f, 0.58f, 0.85f), 1.0f));
            B(rw, "Window mullion", new Vector3(x, 4.5f, -0.075f), new Vector3(0.04f, 0.9f, 0.02f), dark);
            B(rw, "Window mullion", new Vector3(x, 4.5f, -0.075f), new Vector3(1.4f, 0.04f, 0.02f), dark);
        }
        // eye wash
        var ew = P.Node("Eyewash", rw).transform;
        if (P.IsNew(ew.gameObject)) ew.localPosition = new Vector3(-3.4f, 0, 0);
        Cy(ew, "Eyewash pipe", new Vector3(0, 0.55f, -0.18f), 0.03f, 1.1f, Vector3.up, steel);
        Cy(ew, "Eyewash bowl", new Vector3(0, 1.1f, -0.3f), 0.2f, 0.08f, Vector3.up, yellow);
        Cy(ew, "Eyewash bowl inner", new Vector3(0, 1.145f, -0.3f), 0.17f, 0.01f, Vector3.up, P.Mat(new Color(0.75f, 0.76f, 0.8f), 0.8f, 0.6f));
        for (int i = 0; i < 2; i++) Cy(ew, "Eyewash nozzle", new Vector3(-0.07f + i * 0.14f, 1.2f, -0.3f), 0.02f, 0.07f, Vector3.up, green);
        B(ew, "Eyewash paddle", new Vector3(0, 0.95f, -0.42f), new Vector3(0.18f, 0.05f, 0.14f), green);
        var ep = Ui.Panel(ew, "Eyewash sign", new Vector3(0, 2.1f, -0.03f), new Vector2(0.75f, 0.5f), new Color(0.03f, 0.35f, 0.16f, 0.97f), Color.white, 0.12f);
        P.Text(ep, "EYE WASH", new Vector3(0, -0.04f, -0.012f), 0.09f, Color.white, TextAnchor.MiddleCenter, true, "Sign line");
        B(ew, "Eyewash floor ring", new Vector3(0, 0.006f, -0.8f), new Vector3(1.1f, 0.01f, 1.4f), P.Mat(new Color(0.05f, 0.5f, 0.22f), 0f, 0.25f));
        // emergency shower with a pull rod
        var sw = P.Node("Shower", rw).transform;
        if (P.IsNew(sw.gameObject)) sw.localPosition = new Vector3(-1.6f, 0, 0);
        Cy(sw, "Shower pipe", new Vector3(0, 1.3f, -0.16f), 0.035f, 2.6f, Vector3.up, steel);
        Cy(sw, "Shower arm", new Vector3(0, 2.6f, -0.3f), 0.035f, 0.3f, Vector3.back, steel);
        Cy(sw, "Shower head", new Vector3(0, 2.55f, -0.46f), 0.17f, 0.05f, Vector3.up, yellow);
        Cy(sw, "Shower pull rod", new Vector3(0.2f, 2.0f, -0.4f), 0.008f, 1.2f, Vector3.up, steel);
        B(sw, "Shower pull handle", new Vector3(0.2f, 1.4f, -0.4f), new Vector3(0.16f, 0.015f, 0.015f), green);
        var swp = Ui.Panel(sw, "Shower sign", new Vector3(0, 3.0f, -0.03f), new Vector2(0.9f, 0.3f), new Color(0.03f, 0.35f, 0.16f, 0.97f), Color.white, 0.12f);
        P.Text(swp, "SAFETY SHOWER", new Vector3(0, -0.02f, -0.012f), 0.07f, Color.white, TextAnchor.MiddleCenter, true, "Sign line");
        // fire extinguisher on a bracket
        var fx = P.Node("Extinguisher", rw).transform;
        if (P.IsNew(fx.gameObject)) fx.localPosition = new Vector3(2.8f, 0, 0);
        Cy(fx, "Extinguisher body", new Vector3(0, 1.25f, -0.13f), 0.09f, 0.5f, Vector3.up, P.Mat(new Color(0.8f, 0.08f, 0.08f), 0.3f, 0.6f));
        Ba(fx, "Extinguisher shoulder", new Vector3(0, 1.5f, -0.13f), 0.09f, P.Mat(new Color(0.8f, 0.08f, 0.08f), 0.3f, 0.6f));
        Cy(fx, "Extinguisher neck", new Vector3(0, 1.58f, -0.13f), 0.03f, 0.08f, Vector3.up, dark);
        B(fx, "Extinguisher lever", new Vector3(0.04f, 1.64f, -0.13f), new Vector3(0.1f, 0.02f, 0.025f), dark);
        Tube(fx, "Extinguisher hose", new[] { new Vector3(-0.03f, 1.62f, -0.13f), new Vector3(-0.1f, 1.5f, -0.2f), new Vector3(-0.1f, 1.3f, -0.21f) }, 0.012f, P.Rubber());
        B(fx, "Extinguisher strap", new Vector3(0, 1.3f, -0.13f), new Vector3(0.21f, 0.04f, 0.2f), dark);
        var fp = Ui.Panel(fx, "Extinguisher sign", new Vector3(0, 2.1f, -0.03f), new Vector2(0.8f, 0.4f), new Color(0.65f, 0.06f, 0.06f, 0.97f), Color.white, 0.1f);
        P.Text(fp, "FIRE EXTINGUISHER", new Vector3(0, -0.03f, -0.012f), 0.06f, Color.white, TextAnchor.MiddleCenter, true, "Sign line");
        // first aid cabinet
        var fa = P.Node("First aid", rw).transform;
        if (P.IsNew(fa.gameObject)) fa.localPosition = new Vector3(1.7f, 0, 0);
        B(fa, "First aid box", new Vector3(0, 1.5f, -0.08f), new Vector3(0.5f, 0.4f, 0.14f), white);
        B(fa, "First aid cross", new Vector3(0, 1.5f, -0.16f), new Vector3(0.22f, 0.07f, 0.008f), green);
        B(fa, "First aid cross", new Vector3(0, 1.5f, -0.16f), new Vector3(0.07f, 0.22f, 0.008f), green);
        // PPE rack with hats, glasses and ear defenders
        var ppe = P.Node("PPE rack", rw).transform;
        if (P.IsNew(ppe.gameObject)) ppe.localPosition = new Vector3(-5.0f, 0, 0);
        B(ppe, "PPE shelf", new Vector3(0, 1.4f, -0.18f), new Vector3(1.0f, 0.03f, 0.35f), P.Wood());
        foreach (float x in new[] { -0.4f, 0.4f }) B(ppe, "PPE bracket", new Vector3(x, 1.33f, -0.12f), new Vector3(0.03f, 0.14f, 0.24f), dark);
        for (int i = 0; i < 3; i++)
        {
            var hat = Ba(ppe, "Hard hat", new Vector3(-0.3f + i * 0.28f, 1.47f, -0.2f), 0.12f, P.Mat(new Color(1f, 0.78f, 0.08f), 0.1f, 0.6f));
            if (P.IsNew(hat)) hat.transform.localScale = new Vector3(0.24f, 0.14f, 0.18f);
        }
        B(ppe, "Safety glasses", new Vector3(0.45f, 1.425f, -0.2f), new Vector3(0.14f, 0.02f, 0.05f), P.Glass(0.45f));
        var pp = Ui.Panel(ppe, "PPE sign", new Vector3(0, 1.9f, -0.03f), new Vector2(1.0f, 0.34f), new Color(0.0f, 0.2f, 0.58f, 0.97f), P.Gold, 0.12f);
        P.Text(pp, "PPE STATION", new Vector3(0, -0.03f, -0.012f), 0.08f, Color.white, TextAnchor.MiddleCenter, true, "Sign line");

        // ---------- ceiling: cable tray, duct, light fittings
        var tray = P.Mat(new Color(0.6f, 0.62f, 0.66f), 0.7f, 0.45f);
        var ct = P.Node("Cable tray", dRoot).transform;
        if (P.IsNew(ct.gameObject)) ct.position = new Vector3(-4.6f, 5.4f, 0f);
        B(ct, "Tray floor", Vector3.zero, new Vector3(0.45f, 0.02f, 11.0f), tray);
        B(ct, "Tray side", new Vector3(-0.22f, 0.04f, 0), new Vector3(0.02f, 0.08f, 11.0f), tray);
        B(ct, "Tray side", new Vector3(0.22f, 0.04f, 0), new Vector3(0.02f, 0.08f, 11.0f), tray);
        for (int i = 0; i < 12; i++) B(ct, "Tray rung", new Vector3(0, 0.005f, -5f + i * 0.92f), new Vector3(0.45f, 0.012f, 0.03f), dark);
        for (int i = 0; i < 4; i++) Cy(ct, "Cable", new Vector3(-0.15f + i * 0.1f, 0.04f, 0), 0.016f, 11.0f, Vector3.forward, P.Rubber());
        for (int i = 0; i < 4; i++) { B(ct, "Tray hanger", new Vector3(-0.22f, 0.25f, -4f + i * 2.7f), new Vector3(0.02f, 0.45f, 0.02f), dark); B(ct, "Tray hanger", new Vector3(0.22f, 0.25f, -4f + i * 2.7f), new Vector3(0.02f, 0.45f, 0.02f), dark); }

        var duct = P.Node("HVAC duct", dRoot).transform;
        if (P.IsNew(duct.gameObject)) duct.position = new Vector3(0, 5.3f, -4.3f);
        hvacT = duct;
        var galv = P.Mat(new Color(0.7f, 0.72f, 0.75f), 0.8f, 0.5f);
        B(duct, "Duct", Vector3.zero, new Vector3(12.8f, 0.55f, 0.7f), galv);
        for (int i = 0; i < 9; i++) B(duct, "Duct flange", new Vector3(-5.8f + i * 1.45f, 0, 0), new Vector3(0.04f, 0.6f, 0.76f), P.Mat(new Color(0.55f, 0.57f, 0.6f), 0.8f, 0.5f));
        for (int i = 0; i < 2; i++)
        {
            B(duct, "Diffuser", new Vector3(-3f + i * 6f, -0.3f, 0), new Vector3(0.6f, 0.03f, 0.6f), dark);
            for (int k = 0; k < 4; k++) B(duct, "Diffuser vane", new Vector3(-3f + i * 6f, -0.32f, -0.22f + k * 0.145f), new Vector3(0.56f, 0.01f, 0.03f), steel);
        }
        for (int i = 0; i < 4; i++) B(duct, "Duct hanger", new Vector3(-5f + i * 3.3f, 0.4f, 0), new Vector3(0.03f, 0.5f, 0.03f), dark);

        // light fittings: housings and hanging rods for the six ceiling panels
        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz++)
            {
                var pos = new Vector3(ix * 3f, 5.62f, iz * 3.2f);
                B(dRoot, "Light housing", pos + Vector3.up * 0.04f, new Vector3(1.7f, 0.05f, 0.58f), dark);
                foreach (var dx in new[] { -0.7f, 0.7f }) Cy(dRoot, "Light rod", pos + new Vector3(dx, 0.2f, 0), 0.008f, 0.35f, Vector3.up, steel);
            }
    }

    // ------------------------------------------------------------------------------------------------- lighting and effects

    void BuildLightingFx()
    {
        var root = P.Node("Lighting fx", dRoot).transform;
        // steam from the vessel vent: emits only while the vent is open and the vessel is filling or pressurized
        ventSteam = Fx.Steam(root, new Vector3(0, C.y + R + 0.62f, C.z), Vector3.up, 16f, 0.12f);
        ventSteam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        // drips: a weeping flange on the header, and a pipe over the floor drain
        Fx.Drip(root, new Vector3(2.4f, 0.38f, C.z + 0.08f), 0.7f, true, 0.38f);
        if (!ExternalShell) Fx.Drip(root, new Vector3(-1.5f, 3.0f, 2.5f), 0.35f, true, 3f);
        // work lamp over the flange table, warm
        var lampT = P.Node("Work lamp", root).transform;
        if (P.IsNew(lampT.gameObject)) lampT.position = new Vector3(-2.6f, 1.6f, 0.2f);
        Cy(lampT, "Lamp post", new Vector3(0, -0.6f, 0), 0.015f, 1.2f, Vector3.up, P.DarkSteel());
        Cy(lampT, "Lamp arm", new Vector3(0.12f, 0.05f, 0), 0.012f, 0.3f, Vector3.right, P.DarkSteel());
        var shade = Cy(lampT, "Lamp shade", new Vector3(0.26f, 0.0f, 0), 0.07f, 0.1f, Vector3.down, P.Painted(new Color(0.9f, 0.7f, 0.1f)));
        Cy(lampT, "Lamp bulb", new Vector3(0.26f, -0.05f, 0), 0.04f, 0.015f, Vector3.down, P.Glow(new Color(1f, 0.85f, 0.55f), 2.4f));
        Fx.Practical(lampT, new Vector3(0.26f, -0.15f, 0), new Color(1f, 0.82f, 0.55f), 3.2f, 0.9f);
        // pump status practical (red when idle, green while running; colour set in Update)
        pumpGlow = Fx.Practical(root, new Vector3(2.6f, 1.35f, 0.4f), new Color(1f, 0.2f, 0.2f), 1.6f, 0.5f);

        Fx.Dust(root, new Vector3(0, 2.4f, 1.0f), new Vector3(10f, 3.6f, 8f), 80);
        if (ExternalShell) return;
        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz++)
                Fx.LightShaft(root, new Vector3(ix * 3f, 5.58f, iz * 3.2f), 5.2f, 34f, new Color(1f, 0.95f, 0.85f), 0.045f);
        // slanting shafts from the three clerestory windows on the right wall
        for (int i = 0; i < 3; i++)
        {
            var sh = Fx.LightShaft(root, new Vector3(6.85f, 4.6f, -3f + i * 3f), 8.5f, 20f, new Color(0.75f, 0.85f, 1f), 0.05f);
            if (P.IsNew(sh)) sh.transform.rotation = Quaternion.Euler(0, 0, -38f);
            Fx.Practical(root, new Vector3(6.3f, 4.3f, -3f + i * 3f), new Color(0.7f, 0.82f, 1f), 5f, 0.45f);
        }
        Fx.ReflectionProbe(new Vector3(0, 2.6f, 0), new Vector3(14f, 6f, 12f), 64);
    }

    Light pumpGlow;

    // ------------------------------------------------------------------------------------------------- sound

    void BuildAudio()
    {
        Sfx.Preload();
        humLoop = Sfx.Loop("pump_hum", motorT != null ? motorT : dRoot, 0f, 1.5f, 14f);
        toneLoop = Sfx.Loop("pressure_tone", needle != null ? needle.parent : dRoot, 0f, 1.5f, 8f);
        pourLoop = Sfx.Loop("water_pour", fill != null ? fill.Wheel.parent : dRoot, 0f, 1.2f, 10f);
        leakLoop = Sfx.Loop("water_pour", spray != null ? spray.transform : dRoot, 0f, 1f, 9f);
        if (leakLoop != null) leakLoop.pitch = 1.3f;
        Sfx.Loop("hvac_loop", hvacT != null ? hvacT : dRoot, 0.2f, 4f, 30f);
        Sfx.Loop("plant_drone_loop", dRoot, 0.1f, 8f, 40f);
    }

    /// <summary>Per-frame visuals and sound that depend on the simulation state.</summary>
    void UpdateDetail(float dt, bool closedSystem, bool full, bool leaking)
    {
        // pressure bar
        if (barPivot != null)
        {
            barPivot.localScale = new Vector3(Mathf.Max(0.001f, Mathf.Clamp01(pressure / 300f)), 1f, 1f);
            var c = pressure > MaxPsi ? new Color(1f, 0.2f, 0.15f) : pressure >= TestPsi - 3f ? new Color(1f, 0.8f, 0.15f) : new Color(0.2f, 0.85f, 0.45f);
            barFillR.material.color = c * 0.3f;
            barFillR.material.SetColor("_EmissionColor", c * 1.3f);
        }
        if (localNeedle != null) localNeedle.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, pressure / 300f));
        // vent steam
        if (ventSteam != null)
        {
            bool venting = vent.Open && (fill.Open || pressure > 1f) && level < 1f + 0.001f;
            if (venting && !ventSteam.isEmitting) ventSteam.Play();
            else if (!venting && ventSteam.isEmitting) ventSteam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        // clock
        if (clockHour != null)
        {
            var now = DateTime.Now;
            clockMin.localRotation = Quaternion.Euler(0, 0, -(now.Minute * 6f + now.Second * 0.1f));
            clockHour.localRotation = Quaternion.Euler(0, 0, -((now.Hour % 12) * 30f + now.Minute * 0.5f));
        }
        // beacon and pump light
        if (beacon != null)
        {
            bool alarm = reliefLifted && pressure > MaxPsi - 8f;
            float k = alarm ? 0.5f + 0.5f * Mathf.Sin(Time.time * 9f) : 0.04f;
            beacon.material.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.06f) * (0.2f + 2.4f * k));
        }
        if (pumpGlow != null) pumpGlow.color = pump ? new Color(0.25f, 1f, 0.45f) : new Color(1f, 0.2f, 0.2f);

        // sound: loops follow the state
        float fillTarget = fill.Open && level < 1f && !drain.Open ? 0.5f : drain.Open && level > 0f ? 0.35f : 0f;
        if (pourLoop != null) { pourLoop.volume = Mathf.MoveTowards(pourLoop.volume, fillTarget, dt * 2f); pourLoop.pitch = drain.Open ? 0.85f : 1f; }
        if (humLoop != null)
        {
            humLoop.volume = Mathf.MoveTowards(humLoop.volume, pump ? 0.6f : 0f, dt * 1.5f);
            humLoop.pitch = Mathf.MoveTowards(humLoop.pitch, 0.9f + 0.3f * Mathf.Clamp01(pressure / TestPsi), dt * 0.5f);
        }
        if (toneLoop != null)
        {
            bool rising = pump && closedSystem && full && pressure > 0.5f && pressure < TestPsi - 0.5f;
            toneLoop.volume = Mathf.MoveTowards(toneLoop.volume, rising ? 0.16f : 0f, dt * 1.2f);
            toneLoop.pitch = 0.7f + 1.1f * Mathf.Clamp01(pressure / TestPsi);
        }
        if (leakLoop != null) leakLoop.volume = Mathf.MoveTowards(leakLoop.volume, leaking ? 0.55f : 0f, dt * 3f);
        // gauge needle ticks every 10 psi
        int tick = Mathf.FloorToInt(pressure / 10f);
        if (tick != lastTick) { if (Mathf.Abs(tick - lastTick) <= 2) Sfx.Play("gauge_tick", needle.position, 0.35f, 0.06f); lastTick = tick; }
        // relief valve: siren sweep when it lifts, gas hiss while it vents
        if (relief.isPlaying && Time.time >= reliefHissAt) { reliefHissAt = Time.time + 1.5f; Sfx.Play("gas_hiss", relief.transform.position, 0.8f); }
    }
}
