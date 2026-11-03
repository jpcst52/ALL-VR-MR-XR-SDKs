using System.Collections.Generic;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Round 3 detail for Refinery Rounds: process-unit fittings, ground wear, safety equipment, lit fixtures, a better
/// cabin, flare extras and particle effects (flare smoke, steam, a drip at the flange). Everything is made through the
/// kit helpers so it bakes with the rest of the scene. Nothing here has colliders, so it never blocks the player.
/// </summary>
public partial class RefineryRounds
{
    Transform detailRoot;
    readonly List<ParticleSystem> windFx = new List<ParticleSystem>();
    readonly List<float> windFxGain = new List<float>();
    Material flameOuterMat, flameCoreMat, airLampMat;
    AudioSource windLoop;
    float dripTimer;

    // ------------------------------------------------------------------------------------------------- textures

    static Texture2D MakeTex(string name, int size, System.Func<float, float, Color> f, bool mips, TextureWrapMode wrap)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, mips) { name = name, wrapMode = wrap, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = f((float)x / size, (float)y / size);
        t.SetPixels(px);
        t.Apply(mips);
        return t;
    }

    /// <summary>Corrugated, weathered cladding with a few vertical rust streaks.</summary>
    static Texture2D CladdingTex()
    {
        return MakeTex("pb-cladding", 256, (u, v) =>
        {
            float corr = 0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 2f * 12f);
            float b = 0.6f + 0.17f * corr + (Mathf.PerlinNoise(u * 9f + 3f, v * 2f) - 0.5f) * 0.2f;
            float s = Mathf.PerlinNoise(u * 28f + 7f, v * 1.4f);
            float rust = Mathf.Clamp01((s - 0.64f) * 4f) * (0.35f + 0.65f * (1f - v));
            Color c = new Color(b, b, b + 0.02f);
            return Color.Lerp(c, new Color(0.48f, 0.27f, 0.14f), rust);
        }, true, TextureWrapMode.Repeat);
    }

    /// <summary>Open-mesh grating: light bars, near-black gaps, a little wear.</summary>
    static Texture2D GratingTex()
    {
        return MakeTex("pb-grating", 128, (u, v) =>
        {
            float gx = (u * 8f) % 1f, gy = (v * 8f) % 1f;
            bool bar = gx < 0.16f || gy < 0.16f;
            float n = Mathf.PerlinNoise(u * 20f, v * 20f) * 0.14f;
            return bar ? new Color(0.5f + n, 0.52f + n, 0.54f + n) : new Color(0.03f, 0.03f, 0.035f);
        }, true, TextureWrapMode.Repeat);
    }

    static Texture2D SoftTex()
    {
        return MakeTex("pb-soft", 64, (u, v) =>
        {
            float d = 2f * Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            float a = Mathf.Clamp01(1f - d);
            a = a * a * (3f - 2f * a);
            return new Color(1f, 1f, 1f, a);
        }, false, TextureWrapMode.Clamp);
    }

    /// <summary>Irregular dark stain with a soft edge (oil, wet patches).</summary>
    static Texture2D StainTex()
    {
        return MakeTex("pb-stain", 128, (u, v) =>
        {
            float d = 2f * Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            float a = Mathf.Clamp01(1f - d + (Mathf.PerlinNoise(u * 6f + 2f, v * 6f + 5f) - 0.5f) * 0.7f);
            a = Mathf.Clamp01(a * 1.6f);
            return new Color(0.05f, 0.04f, 0.03f, a * 0.85f);
        }, true, TextureWrapMode.Clamp);
    }

    static Material SpriteFx(Texture2D tex)
    {
        var m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = tex;
        return m;
    }

    // ------------------------------------------------------------------------------------------------- helpers

    static void Mark(GameObject g)
    {
#if UNITY_EDITOR
        g.isStatic = true;
#endif
    }

    GameObject Bx(Transform p, string n, Vector3 pos, Vector3 size, Material m, Vector3 euler = default(Vector3))
    {
        var g = P.Box(p, n, pos, size, m, false);
        if (P.IsNew(g)) { g.transform.localRotation = Quaternion.Euler(euler); Mark(g); }
        return g;
    }

    GameObject Cy(Transform p, string n, Vector3 pos, float r, float len, Vector3 axis, Material m)
    {
        var g = P.Cyl(p, n, pos, r, len, axis, m, false);
        if (P.IsNew(g)) Mark(g);
        return g;
    }

    GameObject Rod(Transform p, string n, Vector3 a, Vector3 b, float r, Material m)
    {
        return Cy(p, n, (a + b) / 2f, r, Vector3.Distance(a, b), b - a, m);
    }

    GameObject Bl(Transform p, string n, Vector3 pos, float r, Material m)
    {
        var g = P.Ball(p, n, pos, r, m, false);
        if (P.IsNew(g)) Mark(g);
        return g;
    }

    static float Rf(System.Random rng, float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    ParticleSystem MakeFx(Transform parent, string name, Vector3 pos, Material mat, int order, System.Action<ParticleSystem> setup)
    {
        var go = P.Node(name, parent);
        bool fresh = P.IsNew(go);
        var ps = P.Ensure<ParticleSystem>(go);
        if (fresh)
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            go.transform.position = pos;
            setup(ps);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = order;
            if (go.activeInHierarchy) ps.Play();
        }
        return ps;
    }

    static ParticleSystem.MinMaxGradient FadeGrad(Color c, float peak)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.2f), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    static void Plume(ParticleSystem ps, Color c, float peak, float life, float size0, float size1, float speed, float rate, float spread, float radius)
    {
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.8f, life * 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.8f, speed * 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(size1 * 0.8f, size1 * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;
        var em = ps.emission;
        em.rateOverTime = rate;
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = spread;
        sh.radius = radius;
        sh.rotation = new Vector3(-90f, 0f, 0f);   // cone points up
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGrad(c, peak);
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, Mathf.Min(1f, size0 / size1), 1f, 1f));
    }

    void SetWindForce(ParticleSystem ps, float gain)
    {
        var f = ps.forceOverLifetime;
        f.enabled = true;
        f.space = ParticleSystemSimulationSpace.World;
        f.x = new ParticleSystem.MinMaxCurve(windDir.x * gain);
        f.y = new ParticleSystem.MinMaxCurve(0f);
        f.z = new ParticleSystem.MinMaxCurve(windDir.z * gain);
    }

    void ApplyWindFx()
    {
        for (int i = 0; i < windFx.Count; i++) if (windFx[i]) SetWindForce(windFx[i], windFxGain[i] * Mathf.Clamp(weather.WindMph / 8f, 0.4f, 2.2f));
    }

    // ------------------------------------------------------------------------------------------------- build

    void BuildDetail(Transform root)
    {
        detailRoot = P.Node("Process detail", root).transform;

        var cladTex = CladdingTex();
        var clad = P.Mat(Color.white, 0.65f, 0.5f, cladTex, 3f);
        var grate = P.Mat(Color.white, 0.6f, 0.4f, GratingTex(), 4f);
        var rust = P.Mat(Color.white, 0.2f, 0.25f, P.NoiseTexture("pb-rustpatch", new Color(0.3f, 0.15f, 0.08f), new Color(0.55f, 0.3f, 0.15f), 8f, 128, 1f, 21), 2f);
        var red = P.Mat(new Color(0.75f, 0.08f, 0.06f), 0.1f, 0.55f);
        var yellow = P.Mat(SafetyYellow, 0.05f, 0.45f);
        var green = P.Mat(SafetyGreen, 0.05f, 0.45f);
        var white = P.Mat(new Color(0.92f, 0.92f, 0.9f), 0f, 0.4f);
        var rubber = P.Mat(new Color(0.03f, 0.03f, 0.035f), 0f, 0.2f);
        var rack = P.Painted(new Color(0.15f, 0.3f, 0.55f));
        var dsteel = P.DarkSteel();
        var bsteel = P.Mat(new Color(0.85f, 0.87f, 0.9f), 0.9f, 0.7f);
        var lens = P.Glow(new Color(1f, 0.9f, 0.7f), 1.1f);
        var soft = SpriteFx(SoftTex());

        BuildRackDetail(P.Node("Rack detail", detailRoot).transform, clad, grate, rack, dsteel, bsteel, red, rubber);
        BuildVesselDetail(P.Node("Vessel detail", detailRoot).transform, grate, dsteel, bsteel, red, rack);
        BuildGroundDetail(P.Node("Ground detail", detailRoot).transform, grate, rust, white, dsteel);
        BuildSafetyDetail(P.Node("Safety detail", detailRoot).transform, yellow, green, red, white, rubber, dsteel, bsteel);
        BuildPoles(P.Node("Lighting poles", detailRoot).transform, dsteel, lens);
        BuildCabinDetail(P.Node("Cabin detail", detailRoot).transform, dsteel, bsteel, rubber, lens, white);
        // groups that never move are batched after a bake (the flare group is left out: its flame material changes at runtime)
        foreach (Transform c in detailRoot)
            if (c.name == "Rack detail" || c.name == "Vessel detail" || c.name == "Ground detail" || c.name == "Safety detail" || c.name == "Lighting poles" || c.name == "Cabin detail") P.Static(c.gameObject);
        BuildFlareDetail(P.Node("Flare detail", detailRoot).transform, dsteel, red, white);
        BuildMusterDetail(white, green, rack, dsteel);
        BuildEffects(soft);

        // reflection probe so wet ground, glass and steel pick up the sky and the unit (one capture when the scene loads)
        var pg = P.Node("Reflection probe", detailRoot);
        var probe = P.Ensure<ReflectionProbe>(pg);
        if (P.IsNew(pg))
        {
            pg.transform.position = V(0f, 6f, 4f);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 128;
            probe.size = V(80f, 40f, 80f);
            probe.intensity = 1f;
        }
    }

    // ---- pipe rack: bracing, shoes, cable tray, insulated line, valves, instruments
    void BuildRackDetail(Transform g, Material clad, Material grate, Material rack, Material dsteel, Material bsteel, Material red, Material rubber)
    {
        for (int i = 0; i < 5; i++)
        {
            float x0 = -10f + i * 4f;
            Vector3 a = V(i % 2 == 0 ? x0 : x0 + 4f, 0.15f, 5f), b = V(i % 2 == 0 ? x0 + 4f : x0, 3.45f, 5f);
            Rod(g, "Brace", a, b, 0.04f, rack);
        }
        var shoe = P.Mat(new Color(0.2f, 0.2f, 0.22f), 0.6f, 0.4f);
        for (int c = 0; c < 6; c += 2)
            for (int k = 0; k < 5; k++)
                Bx(g, "Pipe shoe", V(-10f + c * 4f, 5.18f, 3.2f + k * 0.4f), V(0.14f, 0.1f, 0.12f), shoe);

        // cable tray under the upper beam, with hangers and cable bundles
        Bx(g, "Tray floor", V(0f, 4.3f, 4f), V(20f, 0.03f, 0.42f), bsteel);
        Bx(g, "Tray side", V(0f, 4.36f, 3.79f), V(20f, 0.1f, 0.02f), bsteel);
        Bx(g, "Tray side", V(0f, 4.36f, 4.21f), V(20f, 0.1f, 0.02f), bsteel);
        for (int c = 0; c < 6; c++)
        {
            Bx(g, "Tray hanger", V(-10f + c * 4f, 4.63f, 3.78f), V(0.05f, 0.66f, 0.05f), dsteel);
            Bx(g, "Tray hanger", V(-10f + c * 4f, 4.63f, 4.22f), V(0.05f, 0.66f, 0.05f), dsteel);
        }
        Color[] cc = { new Color(0.03f, 0.03f, 0.03f), new Color(0.35f, 0.35f, 0.38f), new Color(0.03f, 0.03f, 0.03f), new Color(0.1f, 0.2f, 0.5f) };
        for (int k = 0; k < 4; k++)
            Bx(g, "Cable", V(0f, 4.34f, 3.88f + k * 0.08f), V(19.6f, 0.035f, 0.05f), P.Mat(cc[k], 0f, 0.3f));

        // insulated line with cladding bands
        Cy(g, "Insulated line", V(0f, 3.8f, 4.95f), 0.17f, 20f, Vector3.right, clad);
        for (int i = 0; i < 7; i++) Cy(g, "Insulation band", V(-9f + i * 3f, 3.8f, 4.95f), 0.18f, 0.05f, Vector3.right, dsteel);

        // branch line fittings: gate valves, a pressure gauge, a drain
        for (int v = 0; v < 2; v++) Valve(g, v == 0 ? -2f : -7.2f, bsteel, red);
        Rod(g, "Gauge stem", V(-6f, 1.2f, 3.2f), V(-6f, 1.55f, 3.2f), 0.015f, bsteel);
        Cy(g, "Gauge case", V(-6f, 1.62f, 3.2f), 0.075f, 0.05f, Vector3.back, dsteel);
        Cy(g, "Gauge face", V(-6f, 1.62f, 3.172f), 0.062f, 0.008f, Vector3.back, P.Mat(Color.white, 0f, 0.2f));
        Bx(g, "Gauge needle", V(-6f + 0.02f, 1.64f, 3.165f), V(0.006f, 0.05f, 0.003f), P.Mat(new Color(0.9f, 0.1f, 0.1f), 0f, 0.3f), V(0f, 0f, -35f));
        Rod(g, "Drain pipe", V(-3.5f, 1.1f, 3.2f), V(-3.5f, 0.4f, 3.2f), 0.03f, bsteel);
        Bl(g, "Drain valve", V(-3.5f, 0.38f, 3.2f), 0.055f, red);
        Bl(g, "Elbow", V(-9f, 1.2f, 3.2f), 0.108f, P.Steel());
        Bl(g, "Elbow", V(-9f, 3.7f, 3.2f), 0.108f, P.Steel());
        Cy(g, "Clad sleeve", V(-9f, 2.5f, 3.2f), 0.16f, 2.0f, Vector3.up, clad);
        for (int i = 0; i < 3; i++) Cy(g, "Sleeve band", V(-9f, 1.7f + i * 0.8f, 3.2f), 0.17f, 0.04f, Vector3.up, dsteel);
    }

    void Valve(Transform g, float x, Material bsteel, Material red)
    {
        Cy(g, "Valve body", V(x, 1.2f, 3.2f), 0.13f, 0.3f, Vector3.right, bsteel);
        Cy(g, "Valve flange", V(x - 0.17f, 1.2f, 3.2f), 0.18f, 0.035f, Vector3.right, bsteel);
        Cy(g, "Valve flange", V(x + 0.17f, 1.2f, 3.2f), 0.18f, 0.035f, Vector3.right, bsteel);
        Cy(g, "Valve bonnet", V(x, 1.5f, 3.2f), 0.06f, 0.32f, Vector3.up, P.DarkSteel());
        Rod(g, "Valve stem", V(x, 1.6f, 3.2f), V(x, 1.85f, 3.2f), 0.014f, bsteel);
        Cy(g, "Handwheel", V(x, 1.86f, 3.2f), 0.15f, 0.02f, Vector3.up, red);
        Bx(g, "Handwheel spoke", V(x, 1.875f, 3.2f), V(0.3f, 0.012f, 0.02f), red);
        Bx(g, "Handwheel spoke", V(x, 1.875f, 3.2f), V(0.02f, 0.012f, 0.3f), red);
    }

    // ---- vessels: bands, manway, grating platform with handrail, ladder cage, relief valve
    void BuildVesselDetail(Transform g, Material grate, Material dsteel, Material bsteel, Material red, Material rack)
    {
        for (int i = 0; i < 4; i++) Cy(g, "V-101 band", V(-7f, 1.5f + i * 2.4f, 10f), 1.33f, 0.07f, Vector3.up, dsteel);
        Cy(g, "Manway", V(-5.62f, 4.2f, 10f), 0.32f, 0.14f, Vector3.right, bsteel);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            Bl(g, "Manway bolt", V(-5.54f, 4.2f + Mathf.Cos(a) * 0.26f, 10f + Mathf.Sin(a) * 0.26f), 0.022f, dsteel);
        }
        Cy(g, "Platform grating", V(-7f, 6.575f, 10f), 1.95f, 0.03f, Vector3.up, grate);

        // handrail around the platform with a gap at the ladder (south)
        var rail = P.Mat(SafetyYellow, 0.3f, 0.4f);
        Vector3[] pts = new Vector3[12];
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            pts[i] = V(-7f + Mathf.Cos(a) * 1.9f, 0f, 10f + Mathf.Sin(a) * 1.9f);
            if (i != 9) Bx(g, "Rail post", V(pts[i].x, 7.07f, pts[i].z), V(0.04f, 1.0f, 0.04f), rail);
        }
        for (int i = 0; i < 12; i++)
        {
            int j = (i + 1) % 12;
            if (i == 9 || j == 9) continue;
            Rod(g, "Top rail", V(pts[i].x, 7.55f, pts[i].z), V(pts[j].x, 7.55f, pts[j].z), 0.02f, rail);
            Rod(g, "Mid rail", V(pts[i].x, 7.05f, pts[i].z), V(pts[j].x, 7.05f, pts[j].z), 0.015f, rail);
        }
        // toe board is skipped (platform is a visual detail); ladder cage hoops
        for (int h = 0; h < 4; h++)
        {
            float y = 2.8f + h * 1.0f;
            for (int s = 0; s < 5; s++)
            {
                float a0 = (-90f + s * 36f) * Mathf.Deg2Rad, a1 = (-90f + (s + 1) * 36f) * Mathf.Deg2Rad;
                Rod(g, "Cage hoop", V(-7f + 0.4f * Mathf.Sin(a0), y, 8.55f - 0.4f * Mathf.Cos(a0)), V(-7f + 0.4f * Mathf.Sin(a1), y, 8.55f - 0.4f * Mathf.Cos(a1)), 0.012f, dsteel);
            }
        }
        float[] bars = { -54f, 0f, 54f };
        for (int b = 0; b < 3; b++)
        {
            float a = bars[b] * Mathf.Deg2Rad;
            Rod(g, "Cage bar", V(-7f + 0.4f * Mathf.Sin(a), 2.8f, 8.55f - 0.4f * Mathf.Cos(a)), V(-7f + 0.4f * Mathf.Sin(a), 5.8f, 8.55f - 0.4f * Mathf.Cos(a)), 0.01f, dsteel);
        }
        // yellow bollards guarding the base
        for (int b = 0; b < 2; b++) Cy(g, "Bollard", V(b == 0 ? -8.7f : -5.3f, 0.5f, 7.9f), 0.09f, 1.0f, Vector3.up, P.Mat(SafetyYellow, 0.05f, 0.45f));

        // V-102: insulation bands and a relief valve with a vent line
        for (int i = 0; i < 4; i++) Cy(g, "V-102 band", V(6f + (i < 2 ? -2.9f + i * 2f : 0.9f + (i - 2) * 2f), 1.9f, 9f), 1.33f, 0.07f, Vector3.right, dsteel);
        Rod(g, "Relief nozzle", V(8f, 3.2f, 9f), V(8f, 3.5f, 9f), 0.06f, bsteel);
        Cy(g, "Relief body", V(8f, 3.65f, 9f), 0.13f, 0.3f, Vector3.up, red);
        Cy(g, "Relief bonnet", V(8f, 3.97f, 9f), 0.08f, 0.32f, Vector3.up, dsteel);
        Bl(g, "Relief cap", V(8f, 4.15f, 9f), 0.07f, bsteel);
        Rod(g, "Relief outlet", V(8f, 3.65f, 9f), V(8.5f, 3.65f, 9f), 0.05f, bsteel);
        Rod(g, "Relief vent", V(8.5f, 3.65f, 9f), V(8.5f, 5.5f, 9f), 0.05f, bsteel);
        // vent stack on the rack
        Rod(g, "Rack vent", V(9f, 5.4f, 3.2f), V(9f, 6.5f, 3.2f), 0.06f, bsteel);
        Cy(g, "Vent cap", V(9f, 6.52f, 3.2f), 0.1f, 0.03f, Vector3.up, dsteel);
    }

    // ---- ground: lines, stones, puddles, stains, drains, weeds
    void BuildGroundDetail(Transform g, Material grate, Material rust, Material white, Material dsteel)
    {
        var yellow = P.Mat(new Color(0.95f, 0.75f, 0.08f), 0f, 0.3f);
        Bx(g, "Pad line", V(-11.6f, 0.046f, 4f), V(0.12f, 0.01f, 19.2f), yellow);
        Bx(g, "Pad line", V(11.6f, 0.046f, 4f), V(0.12f, 0.01f, 19.2f), yellow);
        Bx(g, "Pad line", V(0f, 0.046f, 13.6f), V(23.2f, 0.01f, 0.12f), yellow);
        Bx(g, "Pad line", V(-6.8f, 0.046f, -5.6f), V(9.6f, 0.01f, 0.12f), yellow);
        Bx(g, "Pad line", V(6.8f, 0.046f, -5.6f), V(9.6f, 0.01f, 0.12f), yellow);
        Bx(g, "Walkway edge", V(-0.72f, 0.012f, -10f), V(0.08f, 0.01f, 8f), white);
        Bx(g, "Walkway edge", V(0.72f, 0.012f, -10f), V(0.08f, 0.01f, 8f), white);
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 5; i++)
                Bx(g, "Safe route line", V(s * 0.95f, 0.048f, -4.5f + i * 1.9f), V(0.1f, 0.01f, 1.2f), white);

        // gravel stones outside the pad (cubes: cheap), scattered with a fixed seed
        var rng = new System.Random(7);
        var gm = new[] { P.Mat(new Color(0.42f, 0.4f, 0.37f), 0f, 0.1f), P.Mat(new Color(0.55f, 0.53f, 0.5f), 0f, 0.1f), P.Mat(new Color(0.33f, 0.31f, 0.29f), 0f, 0.1f) };
        int placed = 0;
        for (int tries = 0; tries < 400 && placed < 70; tries++)
        {
            float x = Rf(rng, -24f, 24f), z = Rf(rng, -20f, 22f);
            bool onPad = x > -12.4f && x < 12.4f && z > -6.4f && z < 14.4f;
            bool walk = Mathf.Abs(x) < 0.9f && z < -6f;
            bool cabin = x > -12.6f && x < -7.4f && z > -11.8f && z < -8.2f;
            if (onPad || walk || cabin) continue;
            float s = Rf(rng, 0.05f, 0.16f);
            var st = Bx(g, "Stone", V(x, s * 0.25f, z), V(s, s * 0.5f, s * Rf(rng, 0.7f, 1.2f)), gm[placed % 3], V(0f, Rf(rng, 0f, 180f), 0f));
            placed++;
        }

        // puddles (opaque, very glossy so they mirror the sky and the unit) and oil stains
        var pud = P.Mat(new Color(0.07f, 0.08f, 0.1f), 0f, 0.96f);
        float[][] pp = { new[] { 3f, -2f, 0.9f, 0.6f }, new[] { -3f, 1f, 0.6f, 0.45f }, new[] { 7f, 5f, 1.1f, 0.7f }, new[] { -6f, 12.4f, 0.8f, 0.6f }, new[] { -2.5f, -10.5f, 1.0f, 0.5f } };
        for (int i = 0; i < pp.Length; i++)
        {
            float y = pp[i][1] < -6f ? 0.006f : 0.047f;
            var go = Cy(g, "Puddle", V(pp[i][0], y, pp[i][1]), pp[i][2], 0.004f, Vector3.up, pud);
            if (P.IsNew(go)) go.transform.localScale = V(pp[i][2] * 2f, 0.002f, pp[i][3] * 2f);
        }
        var oil = P.Transparent(Color.white);
        oil.mainTexture = StainTex();
        oil.SetFloat("_Glossiness", 0.5f);
        float[][] op = { new[] { 1.5f, 8.5f, 1.2f }, new[] { -8f, 6.5f, 0.9f }, new[] { 4.5f, 11.5f, 1.4f }, new[] { -1f, -3.5f, 1.0f }, new[] { 9f, -0.5f, 0.8f }, new[] { 8.2f, 7.4f, 0.7f } };
        for (int i = 0; i < op.Length; i++)
        {
            var go = Cy(g, "Oil stain", V(op[i][0], 0.043f + i * 0.0004f, op[i][1]), op[i][2], 0.002f, Vector3.up, oil);
            if (P.IsNew(go)) go.transform.localScale = V(op[i][2] * 2f, 0.001f, op[i][2] * 1.5f);
        }

        // drain grates and a manhole cover
        float[][] dg = { new[] { 4f, -1f }, new[] { -2.2f, 9.8f }, new[] { 2f, 12.5f } };
        for (int i = 0; i < dg.Length; i++)
        {
            Bx(g, "Drain frame", V(dg[i][0], 0.046f, dg[i][1]), V(0.72f, 0.012f, 0.46f), dsteel);
            Bx(g, "Drain grate", V(dg[i][0], 0.054f, dg[i][1]), V(0.64f, 0.006f, 0.38f), grate);
        }
        Cy(g, "Manhole cover", V(-6f, 0.05f, -1f), 0.36f, 0.012f, Vector3.up, rust);
        Cy(g, "Manhole ring", V(-6f, 0.046f, -1f), 0.42f, 0.008f, Vector3.up, dsteel);

        // weeds along the outside of the fence
        var wm = new[] { P.Mat(new Color(0.28f, 0.4f, 0.14f), 0f, 0.2f), P.Mat(new Color(0.4f, 0.42f, 0.17f), 0f, 0.2f), P.Mat(new Color(0.45f, 0.36f, 0.18f), 0f, 0.2f) };
        for (int i = 0; i < 22; i++)
        {
            float t = Rf(rng, 0f, 1f);
            float x, z;
            if (t < 0.4f) { x = Rf(rng, -12f, 12f); z = FenceMinZ - 0.25f; if (Mathf.Abs(x) < 2f) x += 3f; }
            else if (t < 0.7f) { x = FenceMaxX + 0.25f; z = Rf(rng, FenceMinZ, FenceMaxZ); }
            else { x = FenceMinX - 0.25f; z = Rf(rng, FenceMinZ, FenceMaxZ); }
            for (int b = 0; b < 3; b++)
            {
                float h = Rf(rng, 0.25f, 0.55f);
                Bx(g, "Weed", V(x + Rf(rng, -0.1f, 0.1f), h * 0.45f, z + Rf(rng, -0.1f, 0.1f)), V(0.015f, h, 0.015f), wm[(i + b) % 3], V(Rf(rng, -22f, 22f), Rf(rng, 0f, 180f), Rf(rng, -22f, 22f)));
            }
        }
    }

    // ---- safety equipment, signs, drums
    void BuildSafetyDetail(Transform g, Material yellow, Material green, Material red, Material white, Material rubber, Material dsteel, Material bsteel)
    {
        // safety shower and eyewash station
        Vector3 s = V(10.5f, 0f, -3f);
        Bx(g, "Shower pad", V(s.x, 0.048f, s.z - 0.15f), V(1.4f, 0.012f, 1.4f), green);
        Cy(g, "Shower pipe", V(s.x, 1.5f, s.z), 0.035f, 3.0f, Vector3.up, yellow);
        Rod(g, "Shower arm", V(s.x, 2.4f, s.z), V(s.x, 2.4f, s.z - 0.4f), 0.03f, yellow);
        Cy(g, "Shower head", V(s.x, 2.34f, s.z - 0.4f), 0.2f, 0.05f, Vector3.up, bsteel);
        Rod(g, "Pull rod", V(s.x, 2.3f, s.z - 0.4f), V(s.x, 1.45f, s.z - 0.4f), 0.01f, dsteel);
        Bx(g, "Pull handle", V(s.x, 1.43f, s.z - 0.4f), V(0.22f, 0.02f, 0.02f), yellow);
        Cy(g, "Eyewash bowl", V(s.x, 0.98f, s.z - 0.2f), 0.2f, 0.08f, Vector3.up, green);
        Bl(g, "Eyewash head", V(s.x - 0.08f, 1.05f, s.z - 0.2f), 0.03f, bsteel);
        Bl(g, "Eyewash head", V(s.x + 0.08f, 1.05f, s.z - 0.2f), 0.03f, bsteel);
        Rod(g, "Eyewash lever", V(s.x, 0.95f, s.z - 0.2f), V(s.x + 0.25f, 0.95f, s.z - 0.45f), 0.012f, red);
        Rod(g, "Sign arm", V(s.x, 2.95f, s.z), V(s.x + 0.7f, 2.95f, s.z), 0.015f, yellow);
        Rod(g, "Sign arm", V(s.x, 2.6f, s.z), V(s.x + 0.7f, 2.6f, s.z), 0.015f, yellow);
        var ss = Sign(g, "Shower sign", V(s.x + 0.75f, 2.78f, s.z + 0.03f), Vector3.back, new Vector2(1.1f, 0.36f), SafetyGreen, Color.white, 0.07f);
        SignText(ss, "SAFETY SHOWER", 0.04f, 0.085f, Color.white);
        SignText(ss, "AND EYEWASH", -0.08f, 0.06f, Color.white, false);
        StartCoroutine(FitText(ss, 1.0f));

        // fire extinguisher stands
        Extinguisher(g, V(11f, 0f, 2f), red, dsteel, rubber);
        Extinguisher(g, V(-11f, 0f, 8.5f), red, dsteel, rubber);

        // fence signs (outside face, facing the approach)
        FenceSign(g, -10.3f, "HARD HAT\nAND EYE\nPROTECTION", SafetyYellow, new Color(0.08f, 0.08f, 0.08f));
        FenceSign(g, -6.8f, "NO SMOKING\nNO PHONES", new Color(0.7f, 0.08f, 0.06f), Color.white);
        FenceSign(g, 6.8f, "NO OPEN\nFLAME", new Color(0.7f, 0.08f, 0.06f), Color.white);
        FenceSign(g, 10.3f, "KNOW YOUR\nMUSTER POINT", SafetyGreen, Color.white);

        // drums on a pallet, a spill kit cabinet
        var drumMat = new[] { P.Mat(new Color(0.1f, 0.25f, 0.55f), 0.3f, 0.5f), P.Mat(new Color(0.15f, 0.4f, 0.2f), 0.3f, 0.5f) };
        Bx(g, "Drum pallet", V(-10.6f, 0.07f, -1.3f), V(1.3f, 0.12f, 0.9f), P.Mat(new Color(0.55f, 0.42f, 0.28f), 0f, 0.25f));
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = V(-10.95f + (i % 2) * 0.55f, 0.52f, -1.3f + (i / 2) * 0.0f + (i == 2 ? 0.3f : -0.2f));
            Cy(g, "Drum", p, 0.29f, 0.84f, Vector3.up, drumMat[i % 2]);
            Cy(g, "Drum rib", p + V(0f, 0.2f, 0f), 0.3f, 0.03f, Vector3.up, dsteel);
            Cy(g, "Drum rib", p - V(0f, 0.2f, 0f), 0.3f, 0.03f, Vector3.up, dsteel);
            Cy(g, "Drum bung", p + V(0.12f, 0.43f, 0f), 0.04f, 0.02f, Vector3.up, dsteel);
        }
        Bx(g, "Spill kit", V(-11.3f, 0.5f, 1.2f), V(0.6f, 0.9f, 0.4f), yellow);
        var sk = Sign(g, "Spill kit label", V(-11.3f, 0.75f, 0.99f), Vector3.back, new Vector2(0.5f, 0.2f), new Color(0.08f, 0.08f, 0.08f), SafetyYellow, 0.1f);
        SignText(sk, "SPILL KIT", -0.01f, 0.07f, SafetyYellow);
    }

    void FenceSign(Transform g, float x, string text, Color fill, Color ink)
    {
        var sg = Sign(g, "Fence sign", V(x, 1.55f, FenceMinZ - 0.07f), Vector3.back, new Vector2(0.95f, 0.62f), fill, ink, 0.05f);
        SignText(sg, text, 0.0f, 0.075f, ink);
        StartCoroutine(FitText(sg, 0.85f));
    }

    void Extinguisher(Transform g, Vector3 p, Material red, Material dsteel, Material rubber)
    {
        Bx(g, "Extinguisher base", V(p.x, 0.06f, p.z), V(0.36f, 0.04f, 0.36f), dsteel);
        Cy(g, "Extinguisher post", V(p.x, 0.65f, p.z + 0.2f), 0.025f, 1.2f, Vector3.up, dsteel);
        Cy(g, "Extinguisher", V(p.x, 0.36f, p.z), 0.09f, 0.55f, Vector3.up, red);
        Bl(g, "Extinguisher shoulder", V(p.x, 0.62f, p.z), 0.09f, red);
        Cy(g, "Extinguisher neck", V(p.x, 0.72f, p.z), 0.03f, 0.1f, Vector3.up, dsteel);
        Bx(g, "Extinguisher lever", V(p.x, 0.79f, p.z), V(0.14f, 0.02f, 0.03f), dsteel);
        Rod(g, "Extinguisher hose", V(p.x + 0.04f, 0.74f, p.z - 0.04f), V(p.x + 0.12f, 0.35f, p.z - 0.12f), 0.012f, rubber);
        Cy(g, "Extinguisher band", V(p.x, 0.4f, p.z + 0.095f), 0.012f, 0.1f, Vector3.forward, dsteel);
        var sg = Sign(g, "Extinguisher sign", V(p.x, 1.6f, p.z + 0.2f - 0.03f), Vector3.back, new Vector2(0.55f, 0.24f), new Color(0.7f, 0.08f, 0.06f), Color.white, 0.07f);
        SignText(sg, "FIRE\nEXTINGUISHER", 0.0f, 0.055f, Color.white);
        StartCoroutine(FitText(sg, 0.5f));
    }

    // ---- lighting poles with lit fixtures
    void BuildPoles(Transform g, Material dsteel, Material lens)
    {
        Vector3[] pos = { V(-11.2f, 0f, -5f), V(11.2f, 0f, 5f), V(-11.2f, 0f, 11.5f), V(11.2f, 0f, 12.8f) };
        for (int i = 0; i < pos.Length; i++)
        {
            Vector3 p = pos[i];
            Vector3 dir = Flat(Center - p).normalized;
            Bx(g, "Pole base", V(p.x, 0.15f, p.z), V(0.36f, 0.3f, 0.36f), P.Concrete());
            Cy(g, "Pole", V(p.x, 3.5f, p.z), 0.07f, 7f, Vector3.up, dsteel);
            Vector3 tip = V(p.x, 7.15f, p.z) + dir * 0.9f;
            Rod(g, "Pole arm", V(p.x, 7.0f, p.z), tip, 0.035f, dsteel);
            Vector3 eul = Quaternion.LookRotation(dir).eulerAngles;
            Bx(g, "Fixture", tip + V(0f, -0.02f, 0f), V(0.3f, 0.1f, 0.55f), dsteel, eul);
            var l = Bx(g, "Fixture lens", tip + V(0f, -0.075f, 0f), V(0.26f, 0.02f, 0.48f), lens, eul);
            if (i == 0 || i == 3)
            {
                var lt = MakeLight(g, tip + V(0f, -0.4f, 0f), new Color(1f, 0.85f, 0.6f), 13f, 0.8f);
                if (lt.gameObject != null) lt.renderMode = LightRenderMode.ForceVertex;
            }
        }
    }

    // ---- control room cabin
    void BuildCabinDetail(Transform g, Material dsteel, Material bsteel, Material rubber, Material lens, Material white)
    {
        Vector3 c = CabinPos;
        float fz = c.z - 1.65f;
        var glow = P.Glow(new Color(1f, 0.8f, 0.5f), 0.7f);
        var frame = P.Mat(new Color(0.85f, 0.85f, 0.84f), 0.2f, 0.4f);
        foreach (float wx in new[] { c.x - 1.4f, c.x - 0.2f })
        {
            Bx(g, "Window top", V(wx, 2.03f, fz), V(1.0f, 0.06f, 0.05f), frame);
            Bx(g, "Window bottom", V(wx, 1.17f, fz), V(1.0f, 0.06f, 0.05f), frame);
            Bx(g, "Window side", V(wx - 0.47f, 1.6f, fz), V(0.06f, 0.9f, 0.05f), frame);
            Bx(g, "Window side", V(wx + 0.47f, 1.6f, fz), V(0.06f, 0.9f, 0.05f), frame);
            Bx(g, "Window light", V(wx, 1.6f, c.z - 1.646f), V(0.82f, 0.72f, 0.006f), glow);
            for (int k = 0; k < 4; k++) Bx(g, "Blind slat", V(wx, 1.93f - k * 0.11f, c.z - 1.652f), V(0.8f, 0.025f, 0.008f), P.Mat(new Color(0.8f, 0.76f, 0.68f), 0f, 0.3f));
            Bx(g, "Window sill", V(wx, 1.13f, c.z - 1.7f), V(1.1f, 0.04f, 0.12f), bsteel);
        }
        // door: handle, kick plate, vision panel, small sign
        Bx(g, "Door handle", V(c.x + 1.05f, 1.0f, c.z - 1.67f), V(0.04f, 0.16f, 0.04f), bsteel);
        Bx(g, "Door kick plate", V(c.x + 1.4f, 0.15f, c.z - 1.65f), V(0.8f, 0.2f, 0.01f), bsteel);
        Bx(g, "Door vision", V(c.x + 1.4f, 1.55f, c.z - 1.648f), V(0.25f, 0.5f, 0.008f), glow);
        var ds = Sign(g, "Door sign", V(c.x + 1.4f, 0.7f, c.z - 1.67f), Vector3.back, new Vector2(0.5f, 0.16f), SafetyGreen, null);
        SignText(ds, "STAFF ONLY", 0f, 0.05f, Color.white);
        Bx(g, "Door mat", V(c.x + 1.4f, 0.31f, c.z - 2.1f), V(0.9f, 0.02f, 0.6f), rubber);
        // wall lamp over the steps, lit
        Bx(g, "Wall lamp", V(c.x + 2.0f, 2.2f, c.z - 1.72f), V(0.22f, 0.14f, 0.14f), dsteel);
        Bx(g, "Wall lamp lens", V(c.x + 2.0f, 2.12f, c.z - 1.74f), V(0.18f, 0.02f, 0.1f), lens);
        var wl = MakeLight(g, V(c.x + 2.0f, 1.9f, c.z - 2.1f), new Color(1f, 0.85f, 0.6f), 7f, 0.9f);
        wl.renderMode = LightRenderMode.ForceVertex;
        // roof units, conduit and a junction box
        Bx(g, "AC unit", V(c.x - 0.7f, 3.19f, c.z + 0.4f), V(1.1f, 0.55f, 0.75f), P.Mat(new Color(0.8f, 0.82f, 0.82f), 0.3f, 0.4f));
        Cy(g, "AC fan grille", V(c.x - 0.7f, 3.48f, c.z + 0.4f), 0.28f, 0.03f, Vector3.up, dsteel);
        Bx(g, "AC fan blade", V(c.x - 0.7f, 3.5f, c.z + 0.4f), V(0.5f, 0.01f, 0.06f), rubber, V(0f, 30f, 0f));
        Bx(g, "AC fan blade", V(c.x - 0.7f, 3.5f, c.z + 0.4f), V(0.5f, 0.01f, 0.06f), rubber, V(0f, 120f, 0f));
        Rod(g, "Roof vent", V(c.x + 1.2f, 2.9f, c.z - 0.6f), V(c.x + 1.2f, 3.6f, c.z - 0.6f), 0.05f, bsteel);
        Cy(g, "Roof vent cap", V(c.x + 1.2f, 3.62f, c.z - 0.6f), 0.1f, 0.02f, Vector3.up, bsteel);
        var cond = P.Mat(new Color(0.5f, 0.52f, 0.55f), 0.6f, 0.4f);
        Rod(g, "Conduit", V(c.x - 2.1f, 1.0f, c.z - 1.66f), V(c.x + 0.45f, 1.0f, c.z - 1.66f), 0.022f, cond);
        for (int i = 0; i < 4; i++) Bx(g, "Conduit clamp", V(c.x - 1.9f + i * 0.7f, 1.0f, c.z - 1.665f), V(0.04f, 0.07f, 0.02f), dsteel);
        Rod(g, "Conduit drop", V(c.x - 2.1f, 1.0f, c.z - 1.66f), V(c.x - 2.1f, 0.05f, c.z - 1.66f), 0.022f, cond);
        Bx(g, "Junction box", V(c.x + 0.55f, 1.0f, c.z - 1.68f), V(0.2f, 0.24f, 0.08f), P.Mat(new Color(0.6f, 0.62f, 0.64f), 0.4f, 0.4f));
        // lit gas-detector style indicator lamp stays on the existing beacon; add a cabin radio mast base
        Bx(g, "Mast bracket", V(c.x + 1.6f, 3.0f, c.z + 0.8f), V(0.3f, 0.12f, 0.3f), dsteel);
    }

    // ---- flare: guy wires, stack bands, ladder, aviation light
    void BuildFlareDetail(Transform g, Material dsteel, Material red, Material white)
    {
        Vector3 f = FlarePos;
        float[] ang = { 30f, 150f, 270f };
        for (int i = 0; i < 3; i++)
        {
            float a = ang[i] * Mathf.Deg2Rad;
            Vector3 anchor = V(f.x + Mathf.Cos(a) * 9f, 0.1f, f.z + Mathf.Sin(a) * 9f);
            Rod(g, "Guy wire", V(f.x, 16f, f.z), anchor, 0.012f, dsteel);
            Bx(g, "Guy anchor", anchor, V(0.5f, 0.2f, 0.5f), P.Concrete());
        }
        for (int i = 0; i < 3; i++) Cy(g, "Stack band", V(f.x, 20.5f - i * 2.4f, f.z), 0.31f, 1.2f, Vector3.up, i % 2 == 0 ? red : white);
        Cy(g, "Stack platform", V(f.x, 14f, f.z), 0.7f, 0.04f, Vector3.up, dsteel);
        Rod(g, "Stack ladder rail", V(f.x - 0.1f, 0.5f, f.z - 0.32f), V(f.x - 0.1f, 14f, f.z - 0.32f), 0.015f, dsteel);
        Rod(g, "Stack ladder rail", V(f.x + 0.1f, 0.5f, f.z - 0.32f), V(f.x + 0.1f, 14f, f.z - 0.32f), 0.015f, dsteel);
        var lamp = P.Ball(g, "Aviation lamp", V(f.x, 14.3f, f.z - 0.4f), 0.1f, P.Glow(new Color(1f, 0.1f, 0.05f), 1f), false);
        airLampMat = lamp.GetComponent<Renderer>().material;
    }

    // ---- muster point extras (children of the muster point so they move with it)
    void BuildMusterDetail(Material white, Material green, Material rack, Material dsteel)
    {
        if (!musterRoot) return;
        var ring = Cy(musterRoot, "Pad ring", V(0f, 0.064f, 0f), 2.9f, 0.004f, Vector3.up, green);
        Cy(musterRoot, "Pad inner", V(0f, 0.067f, 0f), 2.7f, 0.004f, Vector3.up, concrete);
        var wood = P.Mat(new Color(0.5f, 0.38f, 0.25f), 0f, 0.3f);
        for (int s = -1; s <= 1; s += 2)
        {
            Bx(musterRoot, "Bench seat", V(s * 1.3f, 0.46f, 1.9f), V(1.5f, 0.06f, 0.4f), wood);
            Bx(musterRoot, "Bench leg", V(s * 1.3f - 0.65f, 0.22f, 1.9f), V(0.06f, 0.44f, 0.36f), dsteel);
            Bx(musterRoot, "Bench leg", V(s * 1.3f + 0.65f, 0.22f, 1.9f), V(0.06f, 0.44f, 0.36f), dsteel);
        }
        Bx(musterRoot, "First aid post", V(1.9f, 0.65f, 0.1f), V(0.06f, 1.3f, 0.06f), dsteel);
        Bx(musterRoot, "First aid box", V(1.9f, 1.3f, 0f), V(0.4f, 0.34f, 0.14f), white);
        Bx(musterRoot, "First aid cross", V(1.9f, 1.3f, -0.075f), V(0.24f, 0.07f, 0.01f), green);
        Bx(musterRoot, "First aid cross", V(1.9f, 1.3f, -0.076f), V(0.07f, 0.24f, 0.01f), green);
        P.Text(musterRoot, "FIRST AID", V(1.9f, 1.08f, -0.03f), 0.05f, Color.white, TextAnchor.MiddleCenter, true);
    }

    // ---- particle effects
    void BuildEffects(Material soft)
    {
        var g = P.Node("Effects", detailRoot).transform;
        // flare smoke, bent by the wind
        var smoke = MakeFx(g, "Flare smoke", V(FlarePos.x, 22.7f, FlarePos.z), soft, 1, ps =>
            Plume(ps, new Color(0.18f, 0.18f, 0.2f), 0.35f, 9f, 0.8f, 3.2f, 1.6f, 6f, 10f, 0.25f));
        windFx.Add(smoke); windFxGain.Add(0.9f);
        // steam from the relief vent and the rack vent
        var st1 = MakeFx(g, "Steam relief", V(8.5f, 5.55f, 9f), soft, 0, ps =>
            Plume(ps, new Color(0.95f, 0.97f, 1f), 0.4f, 3.2f, 0.25f, 1.1f, 1.6f, 14f, 8f, 0.05f));
        windFx.Add(st1); windFxGain.Add(0.35f);
        var st2 = MakeFx(g, "Steam rack", V(9f, 6.6f, 3.2f), soft, 0, ps =>
            Plume(ps, new Color(0.95f, 0.97f, 1f), 0.3f, 2.8f, 0.2f, 0.9f, 1.2f, 10f, 10f, 0.04f));
        windFx.Add(st2); windFxGain.Add(0.35f);
        // slow drip at the leaking flange (lives under the Weep object, so it only runs during task 3)
        if (weepVisual)
        {
            MakeFx(weepVisual.transform, "Flange drip", V(FlangePos.x, FlangePos.y - 0.2f, FlangePos.z), soft, 2, ps =>
            {
                var main = ps.main;
                main.duration = 2f; main.loop = true; main.prewarm = false;
                main.startLifetime = 0.45f;
                main.startSpeed = 0f;
                main.startSize = 0.03f;
                main.startColor = new Color(0.12f, 0.08f, 0.04f, 1f);
                main.gravityModifier = 1.2f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = 5f;
                var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.03f;
            });
        }
    }

    // ------------------------------------------------------------------------------------------------- per-frame

    void UpdateDetail()
    {
        if (flameRoot && flameOuterMat == null)
        {
            var o = flameRoot.Find("Flame outer");
            var c = flameRoot.Find("Flame core");
            if (o) flameOuterMat = o.GetComponent<Renderer>().material;
            if (c) flameCoreMat = c.GetComponent<Renderer>().material;
        }
        float t = Time.time;
        float n = Mathf.PerlinNoise(t * 11f, 4.2f), m = Mathf.PerlinNoise(t * 5f, 9.1f);
        if (flameOuterMat != null) flameOuterMat.SetColor("_EmissionColor", Color.Lerp(new Color(1f, 0.38f, 0.05f), new Color(1f, 0.6f, 0.15f), n) * (2.2f + 1.4f * m));
        if (flameCoreMat != null) flameCoreMat.SetColor("_EmissionColor", Color.Lerp(new Color(1f, 0.75f, 0.25f), new Color(1f, 0.95f, 0.6f), n) * (3f + 1.5f * m));
        if (flameLight != null) flameLight.color = Color.Lerp(new Color(1f, 0.5f, 0.2f), new Color(1f, 0.68f, 0.3f), n);
        if (flameRoot) flameRoot.localScale = V(0.9f + 0.2f * m, 0.85f + 0.35f * n, 0.9f + 0.2f * (1f - m));
        UpdateSound(Time.deltaTime);
        if (airLampMat != null)
        {
            bool on = Mathf.Repeat(t, 2f) < 0.3f;
            airLampMat.SetColor("_EmissionColor", on ? new Color(1f, 0.1f, 0.05f) * 2f : new Color(0.15f, 0f, 0f));
        }
    }

    // ------------------------------------------------------------------------------------------------- sound (kit Sfx)

    void BuildSound()
    {
        var g = P.Node("Sound emitters", detailRoot).transform;
        var drone = P.Node("Plant drone", g).transform;
        var hvac = P.Node("Cabin hvac", g).transform;
        if (P.IsNew(drone.gameObject)) drone.position = V(0f, 3f, 4f);
        if (P.IsNew(hvac.gameObject)) hvac.position = CabinPos + V(-0.7f, 3.2f, 0.4f);
        Sfx.Preload();
        windLoop = Sfx.Loop("wind_loop", rig.Camera.transform, 0.3f, 30f, 90f);
        Sfx.Loop("plant_drone_loop", drone, 0.35f, 8f, 50f);
        Sfx.Loop("hvac_loop", hvac, 0.35f, 2f, 16f);
        if (flameRoot) Sfx.Loop("flare_roar_loop", flameRoot, 0.5f, 12f, 70f);
    }

    void StartSiren()
    {
        Sfx.Play2D("alarm_siren_rise", 0.6f);
        if (sirenLoop == null) sirenLoop = Sfx.Loop("alarm_siren", sirenNode, 0.5f, 6f, 90f);
        else if (!sirenLoop.isPlaying) sirenLoop.Play();
    }

    void StopSiren(bool fall)
    {
        if (sirenLoop != null) sirenLoop.Stop();
        if (fall) Sfx.Play2D("alarm_siren_fall", 0.5f);
    }

    void UpdateSound(float dt)
    {
        if (windLoop != null)
        {
            windLoop.volume = Mathf.Clamp(0.1f + weather.WindMph / 25f * 0.45f, 0.1f, 0.6f);
            windLoop.pitch = 0.9f + 0.25f * Mathf.PerlinNoise(Time.time * 0.2f, 3.3f);
        }
        if (weepVisual && weepVisual.activeSelf && !shotMode)
        {
            dripTimer -= dt;
            if (dripTimer <= 0f) { dripTimer = Random.Range(0.8f, 1.8f); Sfx.Play("water_drip", FlangePos + V(0f, -1.1f, 0f), 0.4f); }
        }
    }
}
