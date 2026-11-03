using System;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Round 3 set dressing for the Resistor Bench: floor, wall, shelves, instrument cart (oscilloscope, soldering station,
/// monitor), pegboard tools, task lamp, stool, cables, plus the procedural textures they use. Everything is built with the
/// kit's P / Ui helpers, so it is baked into the scene with the rest of the static world. Particle effects are created
/// separately and stay runtime-only. Generic education only; no company names.
/// </summary>
public static class BenchDetail
{
    static readonly Color Navy = new Color(0.03f, 0.08f, 0.2f, 0.96f);
    static readonly Color LabelDark = new Color(0.05f, 0.09f, 0.16f, 0.96f);

    // ------------------------------------------------------------------------------------------ texture helpers

    static float Hash(int x, int y) => Mathf.Abs(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f;

    static Color Mul(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, 1f);

    public static Texture2D Paint(string name, int w, int h, Func<int, int, Color> f)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++) { var c = f(x, y); c.a = 1f; px[y * w + x] = c; }
        t.SetPixels(px);
        t.Apply(true);
        return t;
    }

    static Material Tex(Texture2D t, float metal, float smooth) => P.Mat(Color.white, metal, smooth, t, 1f);

    /// <summary>Translucent glass or epoxy with its reflections toned down so it does not bloom to white under the probe.</summary>
    public static Material Glass(Color c)
    {
        var m = P.Transparent(c);
        m.SetFloat("_Glossiness", 0.3f);
        m.SetFloat("_GlossyReflections", 0f);
        return m;
    }

    /// <summary>A lit screen: the texture is both the colour and the emission, so it glows like a display.</summary>
    static Material Screen(Texture2D t, float strength)
    {
        var m = new Material(P.Mat(Color.white, 0f, 0.5f, t, 1f));
        m.EnableKeyword("_EMISSION");
        m.SetTexture("_EmissionMap", t);
        m.SetColor("_EmissionColor", Color.white * strength);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }

    static void Line(Color[] px, int w, int h, float x0, float y0, float x1, float y1, float thick, Color c)
    {
        int n = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 1.5f) + 1;
        int r = Mathf.Max(0, Mathf.FloorToInt(thick / 2f));
        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            int cx = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), cy = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int x = cx + dx, y = cy + dy;
                if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
            }
        }
    }

    static Texture2D PaintLines(string name, int w, int h, Color bg, Action<Color[]> draw)
    {
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = bg;
        draw(px);
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        t.SetPixels(px);
        t.Apply(true);
        return t;
    }

    /// <summary>A rod between two points (an arm, a cable run, a pipe).</summary>
    static GameObject Link(Transform parent, string name, Vector3 a, Vector3 b, float r, Material m) =>
        P.Cyl(parent, name, (a + b) / 2f, r, Vector3.Distance(a, b), b - a, m, false);

    /// <summary>A small printed plate (rounded kit panel plus text) facing the player.</summary>
    static void Plate(Transform parent, string text, Vector3 pos, Vector2 size, float textH, Color accent)
    {
        Ui.Panel(parent, "Plate " + text, pos, size, LabelDark, accent, 0.12f);
        P.Text(parent, text, pos + new Vector3(0, -size.y * 0.04f, -0.004f), textH, Color.white, TextAnchor.MiddleCenter, true);
    }

    // ------------------------------------------------------------------------------------------ public entry points

    public static void BuildAll(Transform transform)
    {
        var root = P.Node("Detail", transform).transform;
        Floor(P.Node("Floor detail", root).transform);
        WallDetail(P.Node("Wall detail", root).transform);
        Shelving(P.Node("Shelving", root).transform);
        Cart(P.Node("Instrument cart", root).transform);
        Pegboard(P.Node("Pegboard", root).transform);
        Stool(P.Node("Stool", root).transform);
        Lamp(P.Node("Task lamp", root).transform);
        Magnifier(P.Node("Magnifier", root).transform);
        BenchExtras(P.Node("Bench extras", root).transform);
    }

    /// <summary>Materials for the room's existing boxes (ESD floor tile, wear) and the reflection probe.</summary>
    public static void Lighting(Transform root)
    {
        var go = P.Node("Reflection probe", root);
        var rp = P.Ensure<ReflectionProbe>(go);
        if (P.IsNew(go))
        {
            go.transform.position = new Vector3(0f, 1.6f, 0.4f);
            rp.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            rp.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            rp.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            rp.resolution = 128;
            rp.size = new Vector3(8f, 3.4f, 7f);
            rp.boxProjection = true;
            rp.intensity = 0.8f;
        }
    }

    // ------------------------------------------------------------------------------------------ floor

    static void Floor(Transform root)
    {
        // ESD floor tile: grey, grouted, with a faint speckle
        var tile = Paint("esd floor tile", 512, 512, (x, y) =>
        {
            int cell = 128;
            bool alt = ((x / cell) + (y / cell)) % 2 == 0;
            Color c = alt ? new Color(0.27f, 0.29f, 0.3f) : new Color(0.245f, 0.265f, 0.28f);
            float n = Mathf.PerlinNoise(x * 0.07f + 3f, y * 0.07f) * 0.06f + Hash(x, y) * 0.035f;
            c += new Color(n, n, n);
            int cx = x % cell, cy = y % cell;
            if (cx < 2 || cy < 2) c = Mul(c, 0.68f);
            return c;
        });
        P.Box(root, "ESD floor", new Vector3(0, 0.002f, 0.5f), new Vector3(7.8f, 0.004f, 6.8f), P.Mat(Color.white, 0f, 0.3f, tile, 13f), false);

        // anti-fatigue rubber mat where the player stands, diamond-pattern and edged with hazard tape
        var rubber = Paint("rubber mat", 256, 256, (x, y) =>
        {
            float d = Mathf.Abs(((x + y) % 32) - 16f) / 16f;
            float e = Mathf.Abs(((x - y + 256) % 32) - 16f) / 16f;
            float v = 0.08f + 0.05f * Mathf.Min(d, e) + Hash(x, y) * 0.02f;
            return new Color(v, v, v * 1.05f);
        });
        P.Box(root, "Anti-fatigue mat", new Vector3(0, 0.012f, -0.1f), new Vector3(2.6f, 0.016f, 0.8f), P.Mat(Color.white, 0f, 0.25f, rubber, 10f), false);
        P.Box(root, "Mat edge front", new Vector3(0, 0.013f, -0.5f), new Vector3(2.6f, 0.018f, 0.04f), P.Hazard(), false);
        P.Box(root, "Mat edge back", new Vector3(0, 0.013f, 0.3f), new Vector3(2.6f, 0.018f, 0.04f), P.Hazard(), false);

        // floor marking lines and a wall-side drain strip of dirt
        var yellow = P.Mat(new Color(0.95f, 0.75f, 0.1f), 0f, 0.3f);
        P.Box(root, "Marking A", new Vector3(-3.0f, 0.005f, -0.5f), new Vector3(0.06f, 0.004f, 3.2f), yellow, false);
        P.Box(root, "Marking B", new Vector3(3.6f, 0.005f, -0.5f), new Vector3(0.06f, 0.004f, 3.2f), yellow, false);
    }

    // ------------------------------------------------------------------------------------------ wall

    static void WallDetail(Transform root)
    {
        var steel = P.Steel();
        var dark = P.DarkSteel();
        // conduit run along the wall with clamps, drop to a bench power strip
        Link(root, "Conduit", new Vector3(-3.9f, 1.16f, 1.27f), new Vector3(3.9f, 1.16f, 1.27f), 0.014f, steel);
        for (int i = -7; i <= 7; i++)
            P.Box(root, "Conduit clamp", new Vector3(i * 0.5f, 1.16f, 1.285f), new Vector3(0.03f, 0.04f, 0.012f), dark, false);
        Link(root, "Conduit drop", new Vector3(1.62f, 1.16f, 1.27f), new Vector3(1.62f, 0.93f, 1.27f), 0.012f, steel);
        P.Box(root, "Junction box", new Vector3(1.62f, 1.16f, 1.26f), new Vector3(0.1f, 0.1f, 0.05f), P.Painted(new Color(0.7f, 0.72f, 0.74f)), false);
        // outlet boxes
        for (int i = 0; i < 3; i++)
        {
            float x = -1.4f + i * 1.0f;
            P.Box(root, "Outlet plate", new Vector3(x, 1.0f, 1.292f), new Vector3(0.08f, 0.12f, 0.008f), P.Painted(new Color(0.92f, 0.92f, 0.9f)), false);
            for (int k = 0; k < 2; k++)
                P.Box(root, "Outlet slot", new Vector3(x, 1.02f - k * 0.04f, 1.287f), new Vector3(0.03f, 0.008f, 0.004f), P.Mat(new Color(0.08f, 0.08f, 0.08f)), false);
        }
        // scuffs and a paint-roller band low on the wall
        var scuff = P.Mat(new Color(0.55f, 0.56f, 0.58f), 0f, 0.15f);
        for (int i = 0; i < 9; i++)
        {
            float x = -3.6f + Hash(i, 3) * 7.2f;
            P.Box(root, "Scuff", new Vector3(x, 0.2f + Hash(i, 9) * 0.5f, 1.298f), new Vector3(0.1f + Hash(i, 5) * 0.2f, 0.02f, 0.002f), scuff, false);
        }

        // ESD warning sign (yellow diamond built from a rotated panel) and an ESD-protected-area plate
        var sign = P.Node("ESD sign", root).transform;
        if (P.IsNew(sign)) sign.position = new Vector3(-2.15f, 1.95f, 1.285f);
        var diamond = P.Box(sign, "Diamond", Vector3.zero, new Vector3(0.28f, 0.28f, 0.01f), P.Mat(new Color(1f, 0.8f, 0.1f), 0f, 0.4f), false);
        if (P.IsNew(diamond)) diamond.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        P.Text(sign, "ESD", new Vector3(0, 0.02f, -0.012f), 0.07f, Color.black, TextAnchor.MiddleCenter, true);
        P.Text(sign, "static sensitive", new Vector3(0, -0.05f, -0.012f), 0.02f, Color.black, TextAnchor.MiddleCenter, true);
        Plate(root, "ESD PROTECTED AREA", new Vector3(-2.15f, 1.7f, 1.285f), new Vector2(0.5f, 0.1f), 0.026f, new Color(1f, 0.8f, 0.1f));

        // wall clock
        var clock = P.Node("Clock", root).transform;
        if (P.IsNew(clock)) clock.position = new Vector3(-3.4f, 2.5f, 1.28f);
        P.Cyl(clock, "Rim", Vector3.zero, 0.15f, 0.03f, Vector3.back, P.Mat(new Color(0.1f, 0.1f, 0.12f), 0.3f, 0.5f), false);
        P.Cyl(clock, "Face", new Vector3(0, 0, -0.016f), 0.135f, 0.006f, Vector3.back, P.Mat(new Color(0.95f, 0.95f, 0.93f), 0f, 0.4f), false);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            var tick = P.Box(clock, "Tick", new Vector3(Mathf.Sin(a) * 0.115f, Mathf.Cos(a) * 0.115f, -0.021f), new Vector3(0.008f, 0.02f, 0.004f), P.Mat(Color.black), false);
            if (P.IsNew(tick)) tick.transform.localRotation = Quaternion.Euler(0, 0, -i * 30f);
        }
        var hh = P.Box(clock, "Hour hand", new Vector3(0.02f, 0.025f, -0.024f), new Vector3(0.012f, 0.07f, 0.004f), P.Mat(Color.black), false);
        if (P.IsNew(hh)) hh.transform.localRotation = Quaternion.Euler(0, 0, -50f);
        P.Box(clock, "Minute hand", new Vector3(-0.025f, 0.04f, -0.026f), new Vector3(0.008f, 0.1f, 0.004f), P.Mat(Color.black), false)
            .transform.localRotation = Quaternion.Euler(0, 0, 32f);

        // side and front walls close the room (no dark void at the edges)
        var sideWall = P.Mat(new Color(0.74f, 0.76f, 0.79f), 0f, 0.18f);
        P.Box(root, "Left wall", new Vector3(-4.05f, 1.6f, 0.5f), new Vector3(0.1f, 3.2f, 7f), sideWall, false);
        P.Box(root, "Right wall", new Vector3(4.05f, 1.6f, 0.5f), new Vector3(0.1f, 3.2f, 7f), sideWall, false);
        P.Box(root, "Front wall", new Vector3(0f, 1.6f, -3.05f), new Vector3(8.2f, 3.2f, 0.1f), sideWall, false);
        P.Box(root, "Left base", new Vector3(-3.99f, 0.06f, 0.5f), new Vector3(0.02f, 0.12f, 7f), P.Mat(new Color(0.16f, 0.17f, 0.19f), 0f, 0.35f), false);
        P.Box(root, "Right base", new Vector3(3.99f, 0.06f, 0.5f), new Vector3(0.02f, 0.12f, 7f), P.Mat(new Color(0.16f, 0.17f, 0.19f), 0f, 0.35f), false);
        // ceiling-mounted fluorescent tubes inside the existing fixtures, and a cable tray along the ceiling
        for (int i = -1; i <= 1; i += 2)
        for (int k = 0; k < 3; k++)
            P.Cyl(root, "Tube", new Vector3(i * 1.3f, 3.15f, 0.48f + k * 0.12f), 0.012f, 1.3f, Vector3.right, P.Glow(new Color(0.92f, 0.97f, 1f), 2.4f), false);
        P.Box(root, "Ceiling tray", new Vector3(0, 3.1f, 1.1f), new Vector3(8f, 0.04f, 0.2f), dark, false);
    }

    // ------------------------------------------------------------------------------------------ shelving

    static void Shelving(Transform root)
    {
        var dark = P.DarkSteel();
        var plastic = P.Mat(new Color(0.85f, 0.87f, 0.9f), 0f, 0.4f);
        // left: a four-shelf rack with reels, tubes and boxes
        float cx = -2.9f, cz = 1.12f;
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            P.Box(root, "Rack post", new Vector3(cx + sx * 0.55f, 0.7f, cz + sz * 0.14f), new Vector3(0.04f, 1.4f, 0.04f), dark, false);
        float[] ys = { 0.12f, 0.5f, 0.88f, 1.26f };
        foreach (var y in ys)
            P.Box(root, "Rack shelf", new Vector3(cx, y, cz), new Vector3(1.14f, 0.03f, 0.34f), P.Mat(new Color(0.66f, 0.68f, 0.7f), 0.7f, 0.45f), false);

        // shelf 1 (y 0.5): component reels standing on edge, front face toward the player
        var reelMat = P.Mat(new Color(0.12f, 0.35f, 0.7f), 0f, 0.5f);
        var tape = P.Mat(new Color(0.78f, 0.72f, 0.5f), 0f, 0.3f);
        for (int i = 0; i < 4; i++)
        {
            float x = cx - 0.42f + i * 0.14f;
            P.Cyl(root, "Reel side", new Vector3(x, 0.5f + 0.015f + 0.1f, cz - 0.1f), 0.1f, 0.006f, Vector3.forward, reelMat, false);
            P.Cyl(root, "Reel tape", new Vector3(x, 0.5f + 0.015f + 0.1f, cz - 0.04f), 0.07f, 0.06f, Vector3.forward, tape, false);
            P.Cyl(root, "Reel side back", new Vector3(x, 0.5f + 0.015f + 0.1f, cz + 0.02f), 0.1f, 0.006f, Vector3.forward, reelMat, false);
            P.Cyl(root, "Reel hub", new Vector3(x, 0.5f + 0.015f + 0.1f, cz - 0.104f), 0.028f, 0.004f, Vector3.forward, P.Mat(new Color(0.85f, 0.85f, 0.88f), 0.1f, 0.4f), false);
        }
        // part tubes on shelf 2 (translucent sticks lying side by side, with coloured stripes of parts inside)
        var glass = Glass(new Color(0.85f, 0.92f, 1f, 0.2f));
        for (int i = 0; i < 6; i++)
        {
            P.Cyl(root, "Tube stick", new Vector3(cx + 0.05f, 0.88f + 0.028f + (i / 3) * 0.04f, cz - 0.1f + (i % 3) * 0.06f), 0.014f, 0.55f, Vector3.right, glass, false);
            var colour = new Color(Hash(i, 2), 0.3f + Hash(i, 7) * 0.5f, 0.2f + Hash(i, 4) * 0.5f);
            P.Cyl(root, "Tube parts", new Vector3(cx + 0.05f, 0.88f + 0.028f + (i / 3) * 0.04f, cz - 0.1f + (i % 3) * 0.06f), 0.009f, 0.4f, Vector3.right, P.Mat(colour, 0f, 0.5f), false);
        }
        // shelf 3 (y 1.26): labelled storage boxes
        for (int i = 0; i < 5; i++)
        {
            float x = cx - 0.45f + i * 0.2f;
            float h = 0.16f + Hash(i, 11) * 0.06f;
            P.Box(root, "Parts box", new Vector3(x, 1.26f + 0.015f + h / 2f, cz - 0.03f), new Vector3(0.17f, h, 0.2f), plastic, false);
            P.Box(root, "Parts box lid", new Vector3(x, 1.26f + 0.015f + h, cz - 0.03f), new Vector3(0.18f, 0.012f, 0.21f), P.Mat(new Color(0.3f + Hash(i, 1) * 0.4f, 0.5f, 0.9f), 0f, 0.5f), false);
            Ui.Panel(root, "Box label", new Vector3(x, 1.26f + 0.015f + h * 0.45f, cz - 0.133f), new Vector2(0.14f, 0.05f), LabelDark, new Color(0.55f, 0.62f, 0.72f), 0.12f);
            string[] names = { "LEDs", "CAPS", "DIODES", "SWITCH", "WIRE" };
            P.Text(root, names[i], new Vector3(x, 1.26f + 0.015f + h * 0.45f - 0.003f, cz - 0.137f), 0.016f, Color.white, TextAnchor.MiddleCenter, true);
        }
        // shelf 0 (y 0.12): spare ESD bags and a cardboard box
        P.Box(root, "Carton", new Vector3(cx - 0.2f, 0.12f + 0.015f + 0.1f, cz), new Vector3(0.4f, 0.2f, 0.28f), P.Mat(new Color(0.62f, 0.5f, 0.34f), 0f, 0.1f), false);
        P.Box(root, "ESD bag", new Vector3(cx + 0.25f, 0.12f + 0.015f + 0.015f, cz), new Vector3(0.28f, 0.03f, 0.2f), P.Mat(new Color(0.5f, 0.55f, 0.6f, 1f), 0f, 0.7f), false);
        Plate(root, "PARTS STOCK", new Vector3(cx, 1.5f, cz - 0.2f), new Vector2(0.4f, 0.08f), 0.024f, P.Gold);

        // drawer cabinet (small component drawers) at the far left
        float dx = -3.75f, dz = 1.12f;
        P.Box(root, "Cabinet body", new Vector3(dx, 0.6f, dz), new Vector3(0.62f, 1.2f, 0.3f), P.Mat(new Color(0.22f, 0.25f, 0.28f), 0.5f, 0.45f), false);
        for (int r = 0; r < 6; r++)
        for (int c = 0; c < 3; c++)
        {
            float x = dx - 0.2f + c * 0.2f, y = 0.18f + r * 0.19f;
            P.Box(root, "Drawer", new Vector3(x, y, dz - 0.152f), new Vector3(0.185f, 0.17f, 0.012f), P.Mat(Color.Lerp(new Color(0.72f, 0.74f, 0.78f), new Color(0.35f, 0.6f, 0.85f), Hash(r, c) * 0.6f), 0.2f, 0.45f), false);
            P.Box(root, "Drawer pull", new Vector3(x, y - 0.035f, dz - 0.162f), new Vector3(0.07f, 0.012f, 0.012f), P.Steel(), false);
            Ui.Panel(root, "Drawer label", new Vector3(x, y + 0.035f, dz - 0.16f), new Vector2(0.12f, 0.04f), LabelDark, null, 0f);
            string[] t = { "1 k", "10 k", "100", "LED", "2N", "0.1u", "10u", "BNC", "FUSE", "SW", "JMP", "M3", "M4", "NUT", "CLIP", "HEAT", "TIE", "TAPE" };
            P.Text(root, t[r * 3 + c], new Vector3(x, y + 0.035f - 0.002f, dz - 0.165f), 0.014f, Color.white, TextAnchor.MiddleCenter, true);
        }
    }

    // ------------------------------------------------------------------------------------------ instrument cart

    static void Cart(Transform root)
    {
        float cx = 2.7f, cz = 0.95f, top = 0.88f;
        var dark = P.DarkSteel();
        P.Box(root, "Cart top", new Vector3(cx, top, cz), new Vector3(1.3f, 0.04f, 0.7f), Tex(Paint("cart laminate", 256, 256, (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f + Hash(x, y) * 0.025f;
            return new Color(0.64f + n, 0.66f + n, 0.68f + n);
        }), 0.1f, 0.4f), false);
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
            P.Box(root, "Cart leg", new Vector3(cx + sx * 0.6f, top / 2f, cz + sz * 0.3f), new Vector3(0.05f, top, 0.05f), dark, false);
        P.Box(root, "Cart shelf", new Vector3(cx, 0.2f, cz), new Vector3(1.2f, 0.025f, 0.6f), dark, false);
        P.Box(root, "Cart lip", new Vector3(cx, top + 0.025f, cz - 0.34f), new Vector3(1.3f, 0.02f, 0.012f), P.Steel(), false);
        float y0 = top + 0.02f;

        // oscilloscope
        var sc = P.Node("Oscilloscope", root).transform;
        float sx0 = 2.32f, sz0 = 0.88f;
        P.Box(sc, "Scope body", new Vector3(sx0, y0 + 0.11f, sz0), new Vector3(0.4f, 0.22f, 0.34f), P.Mat(new Color(0.2f, 0.22f, 0.25f), 0.3f, 0.5f), false);
        P.Box(sc, "Scope front", new Vector3(sx0, y0 + 0.11f, sz0 - 0.1725f), new Vector3(0.4f, 0.22f, 0.005f), P.Mat(new Color(0.12f, 0.13f, 0.15f), 0.4f, 0.4f), false);
        P.Box(sc, "Scope bezel", new Vector3(sx0 - 0.06f, y0 + 0.125f, sz0 - 0.177f), new Vector3(0.22f, 0.165f, 0.004f), P.Mat(new Color(0.02f, 0.02f, 0.03f), 0.2f, 0.6f), false);
        var trace = PaintLines("scope screen", 256, 192, new Color(0.02f, 0.07f, 0.04f), px =>
        {
            for (int i = 0; i <= 8; i++) Line(px, 256, 192, i * 32f, 0, i * 32f, 191, 1f, new Color(0.08f, 0.22f, 0.12f));
            for (int j = 0; j <= 6; j++) Line(px, 256, 192, 0, j * 32f, 255, j * 32f, 1f, new Color(0.08f, 0.22f, 0.12f));
            float py = 96f;
            for (int x = 0; x < 255; x++)
            {
                float a = 96f + Mathf.Sin(x * 0.12f) * 52f + Mathf.Sin(x * 0.36f) * 10f;
                float b = 96f + Mathf.Sin((x + 1) * 0.12f) * 52f + Mathf.Sin((x + 1) * 0.36f) * 10f;
                Line(px, 256, 192, x, a, x + 1, b, 2f, new Color(0.35f, 1f, 0.5f));
                py = b;
            }
        });
        P.Prim(PrimitiveType.Quad, sc, "Scope screen", new Vector3(sx0 - 0.06f, y0 + 0.125f, sz0 - 0.1805f), new Vector3(0.2f, 0.15f, 1f), Screen(trace, 1.2f), false);
        for (int k = 0; k < 3; k++)
        for (int r = 0; r < 2; r++)
        {
            float kx = sx0 + 0.105f + k * 0.045f, ky = y0 + 0.16f - r * 0.06f;
            P.Cyl(sc, "Scope knob", new Vector3(kx, ky, sz0 - 0.19f), 0.016f, 0.02f, Vector3.back, P.Mat(new Color(0.08f, 0.08f, 0.1f), 0.3f, 0.5f), false);
            var mark = P.Box(sc, "Knob mark", new Vector3(kx, ky + 0.009f, sz0 - 0.2f), new Vector3(0.003f, 0.012f, 0.002f), P.Mat(Color.white), false);
            if (P.IsNew(mark)) mark.transform.RotateAround(new Vector3(kx, ky, sz0 - 0.2f), Vector3.forward, Hash(k, r) * 120f - 60f);
        }
        for (int i = 0; i < 3; i++)
        {
            P.Cyl(sc, "BNC", new Vector3(sx0 + 0.08f + i * 0.05f, y0 + 0.03f, sz0 - 0.19f), 0.011f, 0.025f, Vector3.back, P.Steel(), false);
            P.Cyl(sc, "BNC centre", new Vector3(sx0 + 0.08f + i * 0.05f, y0 + 0.03f, sz0 - 0.2f), 0.004f, 0.01f, Vector3.back, P.Mat(new Color(0.85f, 0.7f, 0.3f), 1f, 0.6f), false);
        }
        P.Ball(sc, "Scope status LED", new Vector3(sx0 + 0.17f, y0 + 0.2f, sz0 - 0.18f), 0.005f, P.Glow(new Color(0.2f, 1f, 0.35f), 2.2f), false);
        P.Box(sc, "Scope handle", new Vector3(sx0, y0 + 0.235f, sz0), new Vector3(0.28f, 0.012f, 0.03f), P.Mat(new Color(0.08f, 0.08f, 0.09f), 0f, 0.4f), false);
        for (int i = 0; i < 4; i++)
            P.Cyl(sc, "Scope foot", new Vector3(sx0 + (i % 2 == 0 ? -0.17f : 0.17f), y0 + 0.004f, sz0 + (i < 2 ? -0.13f : 0.13f)), 0.015f, 0.01f, Vector3.up, P.Mat(new Color(0.05f, 0.05f, 0.06f), 0f, 0.3f), false);

        // soldering station: base, display, knob, coil stand with an iron resting in it, sponge, solder reel
        var ss = P.Node("Soldering station", root).transform;
        float tx = 3.1f, tz = 0.82f;
        P.Box(ss, "Station base", new Vector3(tx, y0 + 0.04f, tz), new Vector3(0.2f, 0.08f, 0.22f), P.Mat(new Color(0.15f, 0.17f, 0.2f), 0.2f, 0.45f), false);
        P.Box(ss, "Station face", new Vector3(tx, y0 + 0.045f, tz - 0.111f), new Vector3(0.2f, 0.07f, 0.004f), P.Mat(new Color(0.9f, 0.45f, 0.1f), 0.2f, 0.5f), false);
        P.Box(ss, "Station display", new Vector3(tx - 0.04f, y0 + 0.05f, tz - 0.114f), new Vector3(0.07f, 0.03f, 0.003f), P.Mat(new Color(0.03f, 0.05f, 0.03f), 0f, 0.6f), false);
        P.Text(ss, "350 C", new Vector3(tx - 0.04f, y0 + 0.05f, tz - 0.12f), 0.02f, new Color(1f, 0.5f, 0.2f), TextAnchor.MiddleCenter, true);
        P.Cyl(ss, "Temp knob", new Vector3(tx + 0.05f, y0 + 0.045f, tz - 0.12f), 0.017f, 0.02f, Vector3.back, P.Mat(new Color(0.08f, 0.08f, 0.1f), 0.3f, 0.5f), false);
        P.Ball(ss, "Heater LED", new Vector3(tx + 0.082f, y0 + 0.07f, tz - 0.116f), 0.004f, P.Glow(new Color(1f, 0.3f, 0.1f), 2.2f), false);
        // stand
        var coil = P.Mat(new Color(0.7f, 0.72f, 0.75f), 0.9f, 0.55f);
        P.Box(ss, "Stand base", new Vector3(tx, y0 + 0.085f, tz + 0.02f), new Vector3(0.1f, 0.01f, 0.18f), P.DarkSteel(), false);
        for (int i = 0; i < 4; i++)
            P.Cyl(ss, "Stand coil", new Vector3(tx, y0 + 0.115f + i * 0.0001f, tz + 0.1f - i * 0.04f), 0.024f, 0.004f, Vector3.up, coil, false);
        P.Box(ss, "Stand post", new Vector3(tx + 0.04f, y0 + 0.12f, tz + 0.1f), new Vector3(0.008f, 0.07f, 0.008f), coil, false);
        // iron: handle sits in the coil, tip points toward the sponge
        var ironAxis = new Vector3(0f, 0.12f, -1f);
        var iron = P.Node("Iron", ss).transform;
        if (P.IsNew(iron)) iron.position = new Vector3(tx, y0 + 0.14f, tz + 0.03f);
        P.Cyl(iron, "Iron handle", new Vector3(0, 0, 0.13f), 0.011f, 0.16f, ironAxis, P.Mat(new Color(0.1f, 0.1f, 0.12f), 0f, 0.5f), false);
        P.Cyl(iron, "Iron grip", new Vector3(0, 0.014f, 0.17f), 0.0125f, 0.07f, ironAxis, P.Mat(new Color(0.9f, 0.45f, 0.1f), 0f, 0.35f), false);
        P.Cyl(iron, "Iron barrel", new Vector3(0, 0.001f, 0.035f), 0.007f, 0.05f, ironAxis, P.Mat(new Color(0.6f, 0.62f, 0.65f), 0.9f, 0.6f), false);
        P.Cyl(iron, "Iron tip", new Vector3(0, -0.005f, -0.012f), 0.0035f, 0.04f, ironAxis, P.Mat(new Color(0.85f, 0.82f, 0.7f), 0.95f, 0.7f), false);
        Link(ss, "Iron cable", new Vector3(tx, y0 + 0.165f, tz + 0.23f), new Vector3(tx + 0.06f, y0 + 0.05f, tz + 0.12f), 0.0035f, P.Mat(new Color(0.06f, 0.06f, 0.07f), 0f, 0.4f));
        // sponge tray
        P.Box(ss, "Sponge tray", new Vector3(tx - 0.13f, y0 + 0.015f, tz - 0.02f), new Vector3(0.1f, 0.03f, 0.1f), P.Mat(new Color(0.5f, 0.52f, 0.55f), 0.8f, 0.5f), false);
        P.Box(ss, "Sponge", new Vector3(tx - 0.13f, y0 + 0.035f, tz - 0.02f), new Vector3(0.085f, 0.022f, 0.085f), P.Mat(new Color(0.9f, 0.78f, 0.25f), 0f, 0.05f, Paint("sponge", 64, 64, (x, y) =>
        {
            float v = Hash(x / 2, y / 2);
            return Color.Lerp(new Color(0.75f, 0.6f, 0.15f), new Color(0.95f, 0.85f, 0.35f), v);
        }), 1f), false);
        // solder reel and tin
        P.Cyl(ss, "Solder reel", new Vector3(tx - 0.26f, y0 + 0.04f, tz + 0.05f), 0.04f, 0.045f, Vector3.up, P.Mat(new Color(0.65f, 0.67f, 0.7f), 0.9f, 0.45f), false);
        P.Cyl(ss, "Solder reel flange", new Vector3(tx - 0.26f, y0 + 0.015f, tz + 0.05f), 0.05f, 0.004f, Vector3.up, P.Mat(new Color(0.1f, 0.3f, 0.7f), 0.1f, 0.4f), false);
        P.Cyl(ss, "Solder reel flange top", new Vector3(tx - 0.26f, y0 + 0.062f, tz + 0.05f), 0.05f, 0.004f, Vector3.up, P.Mat(new Color(0.1f, 0.3f, 0.7f), 0.1f, 0.4f), false);
        Plate(ss, "SOLDERING STATION", new Vector3(tx, y0 + 0.215f, tz + 0.1f), new Vector2(0.32f, 0.06f), 0.02f, new Color(0.9f, 0.45f, 0.1f));

        // monitor showing a schematic, on a stand at the back of the cart
        var mon = P.Node("Monitor", root).transform;
        float mx = 2.75f, mz = 1.17f;
        var schem = PaintLines("schematic screen", 640, 384, new Color(0.04f, 0.06f, 0.12f), px =>
        {
            Color g = new Color(0.07f, 0.12f, 0.22f), w = new Color(0.9f, 0.95f, 1f), gold = new Color(1f, 0.72f, 0.11f);
            for (int i = 0; i < 640; i += 32) Line(px, 640, 384, i, 0, i, 383, 1f, g);
            for (int j = 0; j < 384; j += 32) Line(px, 640, 384, 0, j, 639, j, 1f, g);
            // supply on the left, resistor zig-zag on top, LED on the right, back to ground
            Line(px, 640, 384, 100, 90, 100, 290, 3f, w);
            Line(px, 640, 384, 100, 290, 540, 290, 3f, w);
            Line(px, 640, 384, 100, 90, 230, 90, 3f, w);
            float[] zx = { 230, 250, 270, 290, 310, 330, 350, 370 };
            float prevX = 230, prevY = 90;
            for (int i = 0; i < zx.Length; i++)
            {
                float yy = 90 + ((i % 2 == 0) ? 22 : -22);
                Line(px, 640, 384, prevX, prevY, zx[i], yy, 3f, gold);
                prevX = zx[i]; prevY = yy;
            }
            Line(px, 640, 384, prevX, prevY, 400, 90, 3f, gold);
            Line(px, 640, 384, 400, 90, 540, 90, 3f, w);
            Line(px, 640, 384, 540, 90, 540, 150, 3f, w);
            Line(px, 640, 384, 540, 230, 540, 290, 3f, w);
            // LED triangle with bar and rays
            Line(px, 640, 384, 510, 150, 570, 150, 3f, new Color(1f, 0.3f, 0.3f));
            Line(px, 640, 384, 510, 150, 540, 205, 3f, new Color(1f, 0.3f, 0.3f));
            Line(px, 640, 384, 570, 150, 540, 205, 3f, new Color(1f, 0.3f, 0.3f));
            Line(px, 640, 384, 508, 205, 572, 205, 3f, new Color(1f, 0.3f, 0.3f));
            Line(px, 640, 384, 540, 205, 540, 230, 3f, w);
            Line(px, 640, 384, 585, 165, 615, 140, 2f, new Color(1f, 0.5f, 0.4f));
            Line(px, 640, 384, 590, 185, 622, 165, 2f, new Color(1f, 0.5f, 0.4f));
        });
        P.Box(mon, "Monitor stand base", new Vector3(mx, y0 + 0.008f, mz), new Vector3(0.22f, 0.016f, 0.16f), P.DarkSteel(), false);
        P.Box(mon, "Monitor neck", new Vector3(mx, y0 + 0.13f, mz + 0.03f), new Vector3(0.04f, 0.26f, 0.025f), P.DarkSteel(), false);
        P.Box(mon, "Monitor frame", new Vector3(mx, y0 + 0.32f, mz), new Vector3(0.56f, 0.34f, 0.035f), P.Mat(new Color(0.06f, 0.06f, 0.07f), 0.3f, 0.5f), false);
        P.Prim(PrimitiveType.Quad, mon, "Monitor screen", new Vector3(mx, y0 + 0.32f, mz - 0.0185f), new Vector3(0.52f, 0.31f, 1f), Screen(schem, 0.9f), false);
        P.Text(mon, "R1", new Vector3(mx - 0.02f, y0 + 0.395f, mz - 0.021f), 0.022f, new Color(1f, 0.72f, 0.11f), TextAnchor.MiddleCenter, true);
        P.Text(mon, "D1", new Vector3(mx + 0.13f, y0 + 0.33f, mz - 0.021f), 0.022f, new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter, true);
        P.Text(mon, "5 V", new Vector3(mx - 0.2f, y0 + 0.35f, mz - 0.021f), 0.022f, Color.white, TextAnchor.MiddleCenter, true);
        P.Ball(mon, "Monitor power LED", new Vector3(mx + 0.24f, y0 + 0.165f, mz - 0.02f), 0.004f, P.Glow(new Color(0.3f, 0.9f, 1f), 2.2f), false);

        // cables on the cart
        var rubber = P.Mat(new Color(0.07f, 0.07f, 0.08f), 0f, 0.35f);
        Link(root, "Scope probe cable", new Vector3(2.38f, y0 + 0.03f, 0.7f), new Vector3(2.5f, y0 + 0.004f, 0.55f), 0.004f, rubber);
        Link(root, "Cart power cable", new Vector3(2.9f, y0 + 0.004f, 1.2f), new Vector3(3.3f, 0.5f, 1.27f), 0.006f, rubber);
        Link(root, "Cart power cable down", new Vector3(3.3f, 0.5f, 1.27f), new Vector3(3.3f, 0.01f, 1.27f), 0.006f, rubber);
        Plate(root, "INSTRUMENT CART", new Vector3(cx, 0.55f, cz - 0.36f), new Vector2(0.5f, 0.07f), 0.022f, P.Gold);
    }

    // ------------------------------------------------------------------------------------------ pegboard and tools

    static void Pegboard(Transform root)
    {
        float cx = 2.85f, cy = 1.78f;
        var pegTex = Paint("pegboard", 512, 384, (x, y) =>
        {
            int cell = 16;
            float dx = (x % cell) - cell / 2f + 0.5f, dy = (y % cell) - cell / 2f + 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            Color c = new Color(0.72f, 0.78f, 0.84f);
            float n = Mathf.PerlinNoise(x * 0.04f, y * 0.04f) * 0.05f;
            c += new Color(n, n, n);
            if (d < 2.6f) c = new Color(0.32f, 0.35f, 0.4f);
            else if (d < 3.6f) c = Mul(c, 0.8f);
            return c;
        });
        P.Box(root, "Pegboard", new Vector3(cx, cy, 1.29f), new Vector3(1.2f, 0.9f, 0.02f), P.Mat(Color.white, 0f, 0.35f, pegTex, 1f), false);
        P.Box(root, "Pegboard frame top", new Vector3(cx, cy + 0.455f, 1.282f), new Vector3(1.22f, 0.02f, 0.03f), P.DarkSteel(), false);
        P.Box(root, "Pegboard frame bottom", new Vector3(cx, cy - 0.455f, 1.282f), new Vector3(1.22f, 0.02f, 0.03f), P.DarkSteel(), false);
        P.Box(root, "Pegboard frame left", new Vector3(cx - 0.61f, cy, 1.282f), new Vector3(0.02f, 0.9f, 0.03f), P.DarkSteel(), false);
        P.Box(root, "Pegboard frame right", new Vector3(cx + 0.61f, cy, 1.282f), new Vector3(0.02f, 0.9f, 0.03f), P.DarkSteel(), false);
        float z = 1.262f;
        var steelM = P.Mat(new Color(0.78f, 0.8f, 0.83f), 0.9f, 0.55f);
        var red = P.Mat(new Color(0.8f, 0.12f, 0.1f), 0f, 0.4f);
        var yel = P.Mat(new Color(0.95f, 0.75f, 0.1f), 0f, 0.4f);
        var blue = P.Mat(new Color(0.1f, 0.3f, 0.75f), 0f, 0.4f);
        var blk = P.Mat(new Color(0.08f, 0.08f, 0.09f), 0f, 0.4f);

        // screwdrivers (shaft + handle), hung vertically with their tips down
        float[] sdX = { -0.5f, -0.4f, -0.3f, -0.2f };
        Material[] sdM = { red, yel, blue, blk };
        float[] sdLen = { 0.12f, 0.1f, 0.14f, 0.09f };
        for (int i = 0; i < 4; i++)
        {
            float x = cx + sdX[i], yt = cy + 0.28f;
            P.Cyl(root, "Screwdriver handle", new Vector3(x, yt - 0.04f, z), 0.014f, 0.08f, Vector3.up, sdM[i], false);
            P.Cyl(root, "Screwdriver shaft", new Vector3(x, yt - 0.08f - sdLen[i] / 2f, z), 0.0035f, sdLen[i], Vector3.up, steelM, false);
            P.Box(root, "Hook", new Vector3(x, yt + 0.005f, z + 0.012f), new Vector3(0.006f, 0.006f, 0.02f), blk, false);
        }
        // pliers and side cutters (heads plus two angled handles)
        for (int k = 0; k < 2; k++)
        {
            float x = cx - 0.02f + k * 0.17f, yt = cy + 0.3f;
            P.Box(root, "Plier head", new Vector3(x, yt - 0.03f, z), new Vector3(0.04f, 0.07f, 0.012f), steelM, false);
            var jaw = P.Box(root, "Plier jaw", new Vector3(x, yt - 0.075f, z), new Vector3(k == 0 ? 0.018f : 0.034f, 0.035f, 0.01f), steelM, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var h = P.Box(root, "Plier handle", new Vector3(x + s * 0.016f, yt + 0.045f, z), new Vector3(0.018f, 0.12f, 0.014f), k == 0 ? red : yel, false);
                if (P.IsNew(h)) h.transform.localRotation = Quaternion.Euler(0, 0, -s * 8f);
            }
            P.Box(root, "Hook", new Vector3(x, yt + 0.12f, z + 0.012f), new Vector3(0.006f, 0.006f, 0.02f), blk, false);
        }
        // tweezers
        for (int s = -1; s <= 1; s += 2)
        {
            var t = P.Box(root, "Tweezer arm", new Vector3(cx + 0.38f + s * 0.004f, cy + 0.2f, z), new Vector3(0.004f, 0.16f, 0.006f), steelM, false);
            if (P.IsNew(t)) t.transform.localRotation = Quaternion.Euler(0, 0, s * 1.6f);
        }
        // hex-key ring and a wire stripper
        for (int i = 0; i < 5; i++)
        {
            var a = P.Box(root, "Hex key", new Vector3(cx + 0.5f, cy + 0.22f - i * 0.002f, z), new Vector3(0.1f - i * 0.008f, 0.004f, 0.004f), blk, false);
            if (P.IsNew(a)) a.transform.localRotation = Quaternion.Euler(0, 0, -30f + i * 12f);
        }
        P.Box(root, "Stripper head", new Vector3(cx - 0.45f, cy - 0.08f, z), new Vector3(0.05f, 0.05f, 0.012f), steelM, false);
        for (int s = -1; s <= 1; s += 2)
        {
            var h = P.Box(root, "Stripper handle", new Vector3(cx - 0.45f + s * 0.014f, cy - 0.17f, z), new Vector3(0.016f, 0.11f, 0.012f), blue, false);
            if (P.IsNew(h)) h.transform.localRotation = Quaternion.Euler(0, 0, -s * 6f);
        }
        // a coil of hook-up wire and a tape roll on pegs
        P.Cyl(root, "Wire coil", new Vector3(cx - 0.2f, cy - 0.12f, z + 0.004f), 0.05f, 0.03f, Vector3.forward, P.Mat(new Color(0.8f, 0.1f, 0.1f), 0f, 0.5f), false);
        P.Cyl(root, "Wire coil hole", new Vector3(cx - 0.2f, cy - 0.12f, z - 0.012f), 0.02f, 0.006f, Vector3.forward, P.Mat(new Color(0.12f, 0.12f, 0.13f)), false);
        P.Cyl(root, "Tape roll", new Vector3(cx + 0.05f, cy - 0.12f, z + 0.004f), 0.045f, 0.03f, Vector3.forward, P.Mat(new Color(0.1f, 0.1f, 0.12f), 0f, 0.3f), false);
        P.Cyl(root, "Tape core", new Vector3(cx + 0.05f, cy - 0.12f, z - 0.012f), 0.022f, 0.006f, Vector3.forward, P.Mat(new Color(0.8f, 0.78f, 0.6f)), false);
        // small shelf with parts cups
        P.Box(root, "Pegboard shelf", new Vector3(cx + 0.32f, cy - 0.28f, 1.23f), new Vector3(0.42f, 0.015f, 0.09f), P.DarkSteel(), false);
        for (int i = 0; i < 3; i++)
            P.Cyl(root, "Parts cup", new Vector3(cx + 0.2f + i * 0.12f, cy - 0.25f, 1.23f), 0.04f, 0.05f, Vector3.up, P.Mat(new Color(0.85f, 0.87f, 0.9f), 0f, 0.4f), false);

        // labels
        Plate(root, "SCREWDRIVERS", new Vector3(cx - 0.35f, cy + 0.07f, 1.267f), new Vector2(0.3f, 0.045f), 0.014f, red.color);
        Plate(root, "PLIERS / CUTTERS", new Vector3(cx + 0.07f, cy + 0.07f, 1.267f), new Vector2(0.3f, 0.045f), 0.014f, yel.color);
        Plate(root, "TWEEZERS / KEYS", new Vector3(cx + 0.44f, cy + 0.07f, 1.267f), new Vector2(0.2f, 0.045f), 0.013f, steelM.color);
        Plate(root, "TOOL RACK", new Vector3(cx, cy + 0.38f, 1.267f), new Vector2(0.3f, 0.055f), 0.022f, P.Gold);
        Plate(root, "SMALL PARTS", new Vector3(cx + 0.32f, cy - 0.35f, 1.267f), new Vector2(0.3f, 0.045f), 0.014f, new Color(0.55f, 0.62f, 0.72f));
    }

    // ------------------------------------------------------------------------------------------ stool

    static void Stool(Transform root)
    {
        float sx = 0.95f, sz = 0.05f, h = 0.64f;
        var vinyl = P.Mat(new Color(0.08f, 0.08f, 0.09f), 0f, 0.5f, Paint("vinyl", 128, 128, (x, y) =>
        {
            float v = 0.1f + Hash(x, y) * 0.035f + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.03f;
            return new Color(v, v, v * 1.05f);
        }), 1f);
        P.Cyl(root, "Seat", new Vector3(sx, h, sz), 0.19f, 0.07f, Vector3.up, vinyl, false);
        P.Cyl(root, "Seat edge", new Vector3(sx, h - 0.04f, sz), 0.185f, 0.012f, Vector3.up, P.Mat(new Color(0.55f, 0.58f, 0.6f), 0.9f, 0.5f), false);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad + 0.4f;
            Link(root, "Stool leg", new Vector3(sx + Mathf.Sin(a) * 0.1f, h - 0.05f, sz + Mathf.Cos(a) * 0.1f), new Vector3(sx + Mathf.Sin(a) * 0.2f, 0.02f, sz + Mathf.Cos(a) * 0.2f), 0.012f, P.Steel());
        }
        P.Cyl(root, "Foot ring", new Vector3(sx, 0.25f, sz), 0.16f, 0.012f, Vector3.up, P.Steel(), false);
    }

    // ------------------------------------------------------------------------------------------ task lamp and magnifier

    /// <summary>Position of the lamp head (also where the light and dust effects are placed).</summary>
    public static readonly Vector3 LampHead = new Vector3(0.98f, 1.42f, 0.9f);
    public static readonly Vector3 LampTarget = new Vector3(1.0f, 0.92f, 0.66f);

    static void Lamp(Transform root)
    {
        var metal = P.Mat(new Color(0.2f, 0.22f, 0.25f), 0.7f, 0.5f);
        var basePos = new Vector3(0.95f, 0.935f, 1.17f);
        P.Box(root, "Lamp clamp", basePos, new Vector3(0.07f, 0.04f, 0.06f), metal, false);
        P.Cyl(root, "Lamp clamp screw", new Vector3(0.95f, 0.905f, 1.17f), 0.008f, 0.06f, Vector3.up, P.Steel(), false);
        var elbow = new Vector3(0.95f, 1.36f, 1.12f);
        Link(root, "Lamp arm lower", basePos + Vector3.up * 0.02f, elbow, 0.008f, metal);
        P.Ball(root, "Lamp joint", elbow, 0.014f, metal, false);
        var neck = LampHead + new Vector3(0f, 0.025f, 0.08f);
        Link(root, "Lamp arm upper", elbow, neck, 0.007f, metal);
        P.Ball(root, "Lamp joint 2", neck, 0.012f, metal, false);
        // shade: a cone built from two stacked cylinders, pointing at the bench; bright bulb inside
        Vector3 dir = (LampTarget - LampHead).normalized;
        var head = P.Node("Lamp head", root).transform;
        if (P.IsNew(head)) { head.position = LampHead; head.rotation = Quaternion.LookRotation(dir); }
        var shade = P.Cyl(head, "Shade", new Vector3(0, 0, 0.0f), 0.06f, 0.1f, Vector3.forward, P.Mat(new Color(0.1f, 0.3f, 0.65f), 0.4f, 0.5f), false);
        P.Cyl(head, "Shade rim", new Vector3(0, 0, 0.05f), 0.066f, 0.012f, Vector3.forward, metal, false);
        P.Cyl(head, "Shade back", new Vector3(0, 0, -0.055f), 0.035f, 0.02f, Vector3.forward, metal, false);
        P.Cyl(head, "Lamp bulb disc", new Vector3(0, 0, 0.052f), 0.052f, 0.004f, Vector3.forward, P.Glow(new Color(1f, 0.85f, 0.6f), 2.6f), false);

        var lightGo = P.Node("Lamp spot", root);
        var l = P.Ensure<Light>(lightGo);
        if (P.IsNew(lightGo))
        {
            lightGo.transform.position = LampHead + dir * 0.07f;
            lightGo.transform.rotation = Quaternion.LookRotation(dir);
            l.type = LightType.Spot; l.spotAngle = 62f; l.range = 2.2f; l.intensity = 1.5f;
            l.color = new Color(1f, 0.82f, 0.58f);
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
        }
    }

    static void Magnifier(Transform root)
    {
        // a floor-standing-style desk magnifier: base, post, swing arm, lens in a ring, tilted toward the player
        var metal = P.Mat(new Color(0.18f, 0.2f, 0.22f), 0.7f, 0.5f);
        float x = 1.64f, z = 0.55f;
        P.Cyl(root, "Magnifier base", new Vector3(x, 0.91f, z), 0.06f, 0.016f, Vector3.up, metal, false);
        Link(root, "Magnifier post", new Vector3(x, 0.91f, z), new Vector3(x, 1.1f, z), 0.006f, metal);
        Link(root, "Magnifier arm", new Vector3(x, 1.1f, z), new Vector3(x - 0.06f, 1.17f, z - 0.05f), 0.005f, metal);
        var ring = P.Node("Magnifier lens", root).transform;
        if (P.IsNew(ring)) { ring.position = new Vector3(x - 0.08f, 1.2f, z - 0.08f); ring.rotation = Quaternion.Euler(-12f, 0f, 0f); }
        for (int i = 0; i < 28; i++) { float a = i * (360f / 28f) * Mathf.Deg2Rad; P.Ball(ring, "Lens rim", new Vector3(Mathf.Sin(a) * 0.073f, Mathf.Cos(a) * 0.073f, 0f), 0.0048f, metal, false); }
        P.Cyl(ring, "Lens glass", new Vector3(0, 0, -0.002f), 0.066f, 0.006f, Vector3.forward, Glass(new Color(0.45f, 0.75f, 1f, 0.32f)), false);
        var ledMat = P.Glow(new Color(0.85f, 0.95f, 1f), 1.4f);
        for (int i = 0; i < 16; i++) { float a = i * 22.5f * Mathf.Deg2Rad; P.Ball(ring, "Lens LED", new Vector3(Mathf.Sin(a) * 0.058f, Mathf.Cos(a) * 0.058f, -0.008f), 0.0028f, ledMat, false); }
    }

    // ------------------------------------------------------------------------------------------ extras on the bench

    static void BenchExtras(Transform root)
    {
        var rubber = P.Mat(new Color(0.07f, 0.07f, 0.08f), 0f, 0.35f);
        // power strip with a lit switch, fed from the conduit drop
        P.Box(root, "Power strip", new Vector3(1.62f, 0.92f, 1.1f), new Vector3(0.06f, 0.036f, 0.2f), P.Mat(new Color(0.9f, 0.9f, 0.88f), 0f, 0.4f), false);
        P.Box(root, "Strip switch", new Vector3(1.62f, 0.942f, 1.02f), new Vector3(0.03f, 0.008f, 0.03f), P.Glow(new Color(1f, 0.25f, 0.2f), 1.6f), false);
        for (int i = 0; i < 3; i++)
            P.Box(root, "Strip socket", new Vector3(1.62f, 0.939f, 1.07f + i * 0.045f), new Vector3(0.04f, 0.004f, 0.035f), P.Mat(new Color(0.1f, 0.1f, 0.11f)), false);
        Link(root, "Strip cable", new Vector3(1.62f, 0.93f, 1.27f), new Vector3(1.62f, 0.93f, 1.2f), 0.006f, rubber);

        // cable management: velcro ties and a cable bundle behind the supply
        Link(root, "Supply mains cable", new Vector3(1.4f, 0.95f, 1.1f), new Vector3(1.62f, 0.93f, 1.15f), 0.006f, rubber);
        for (int i = 0; i < 3; i++)
            P.Cyl(root, "Cable tie", new Vector3(1.4f + 0.07f * (i + 1), 0.945f - 0.003f * i, 1.1f + 0.027f * (i + 1)), 0.0085f, 0.008f, new Vector3(0.22f, 0f, 0.08f), P.Mat(new Color(0.9f, 0.9f, 0.2f), 0f, 0.3f), false);

        // desk accessories: pen mug, a notepad, a small parts tray with SMD bits
        P.Cyl(root, "Pen mug", new Vector3(-0.03f, 0.95f, 1.1f), 0.03f, 0.1f, Vector3.up, P.Mat(new Color(0.1f, 0.3f, 0.65f), 0.1f, 0.5f), false);
        for (int i = 0; i < 3; i++)
        {
            var pen = P.Cyl(root, "Pen", new Vector3(-0.03f + (i - 1) * 0.012f, 1.0f, 1.1f), 0.004f, 0.1f, Vector3.up, P.Mat(new Color(Hash(i, 3), 0.3f, Hash(i, 8)), 0f, 0.5f), false);
            if (P.IsNew(pen)) pen.transform.localRotation = Quaternion.Euler((i - 1) * 9f, 0, (i - 1) * -7f);
        }
        P.Box(root, "Notepad", new Vector3(-1.05f, 0.9115f, 0.5f), new Vector3(0.1f, 0.008f, 0.14f), P.Mat(new Color(0.96f, 0.96f, 0.9f), 0f, 0.15f), false);
        P.Box(root, "Notepad cover", new Vector3(-1.05f, 0.9085f, 0.5f), new Vector3(0.105f, 0.004f, 0.145f), P.Mat(P.Royal), false);
        // wear on the ESD mat edge
        P.Box(root, "Mat tape strip", new Vector3(0, 0.9055f, 0.425f), new Vector3(3.2f, 0.0012f, 0.02f), P.Mat(new Color(0.9f, 0.75f, 0.1f), 0f, 0.25f), false);
        // bench rear stop (a low backboard so parts do not fall behind) and a ruler stuck to the front edge
        P.Box(root, "Backboard", new Vector3(0, 0.935f, 1.19f), new Vector3(3.3f, 0.07f, 0.02f), P.DarkSteel(), false);
        P.Box(root, "Edge ruler", new Vector3(-0.6f, 0.9025f, 0.428f), new Vector3(0.6f, 0.002f, 0.012f), P.Mat(new Color(0.9f, 0.9f, 0.85f), 0f, 0.3f), false);
        for (int i = 0; i <= 30; i++)
            P.Box(root, "Ruler tick", new Vector3(-0.9f + i * 0.02f, 0.9047f, 0.4285f), new Vector3(0.0008f, 0.0008f, i % 5 == 0 ? 0.008f : 0.005f), P.Mat(Color.black), false);
    }
}
