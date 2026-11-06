using System.Collections.Generic;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Round 3 detail for the warehouse: procedural textures (concrete with aggregate, painted block wall, corrugated
/// cardboard, wood, barcode labels, tyre marks, oil stains), floor wear, columns and guards, ceiling services, light
/// fixtures with light shafts, a polished-floor reflection probe, dock fittings, bollards, staging, pallet jacks, safety
/// equipment, a break area, lockers and tote shelving. Everything is built from primitives and generated textures
/// (no imported art) through the kit helpers, so it is baked with the rest of the scene. Dust motes are runtime-only.
/// </summary>
public partial class WarehouseForklift
{
    // detail materials (built once per Start)
    Material mWall, mCard0, mCard1, mCard2, mWood, mRubber, mGalv, mRed, mYellow, mLabel, mWrap, mCrate, mCrate2, mLampWhite;
    Material mDecalTyreA, mDecalTyreB, mDecalStain, mSpill, mGlowPool;
    Texture2D softDot;

    // ------------------------------------------------------------------------------------------------- textures

    static float Hash(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffff) / 65535f;
        }
    }

    static float Pn(float u, float v, float sc, float o) => Mathf.PerlinNoise(o + u * sc, o * 0.7f + v * sc);

    /// <summary>Perlin noise that repeats seamlessly over the unit square (so a tiled texture shows no seams).</summary>
    static float TileN(float u, float v, float sc, float seed)
    {
        float o = 40f + seed * 7.7f;
        float a = Pn(u, v, sc, o), b = Pn(u - 1f, v, sc, o), c = Pn(u, v - 1f, sc, o), d = Pn(u - 1f, v - 1f, sc, o);
        return a * (1f - u) * (1f - v) + b * u * (1f - v) + c * (1f - u) * v + d * u * v;
    }

    static Texture2D MakeTex(string name, int w, int h, bool alpha, bool tile, System.Func<float, float, int, int, Color> f)
    {
        var t = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, true)
        {
            name = name,
            wrapMode = tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 8,
        };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            px[y * w + x] = f((x + 0.5f) / w, (y + 0.5f) / h, x, y);
        t.SetPixels(px);
        t.Apply(true);
        return t;
    }

    /// <summary>Sealed concrete: broad tone patches, fine grain and scattered dark and light aggregate.</summary>
    static Texture2D FloorTex() => MakeTex("wf-floor", 512, 512, false, true, (u, v, x, y) =>
    {
        float b = Mathf.Lerp(0.50f, 0.70f, TileN(u, v, 3f, 1) * 0.55f + TileN(u, v, 11f, 2) * 0.3f + TileN(u, v, 29f, 3) * 0.15f);
        float s = Hash(x, y, 1);
        if (s > 0.986f) b *= 0.6f; else if (s < 0.01f) b *= 1.3f;
        b += (Hash(x, y, 2) - 0.5f) * 0.05f;
        return new Color(b * 0.99f, b, b * 1.02f);
    });

    /// <summary>Painted block wall: roller streaks, block joints, grime near the floor.</summary>
    static Texture2D WallTex() => MakeTex("wf-wall", 512, 512, false, true, (u, v, x, y) =>
    {
        float b = 0.80f + (TileN(u, v, 5f, 4) - 0.5f) * 0.10f;
        b += (Mathf.PerlinNoise(31f + u * 60f, 5f + v * 2f) - 0.5f) * 0.05f; // vertical roller streaks (do not tile exactly; faint)
        float row = Mathf.Floor(v / 0.0625f);
        float ub = Mathf.Repeat(u * 5f + (row % 2f) * 0.5f, 1f);
        float rv = Mathf.Repeat(v / 0.0625f, 1f);
        if (ub < 0.012f || rv < 0.025f) b *= 0.93f;
        float grime = Mathf.Clamp01(1f - v / 0.16f);
        b *= 1f - grime * grime * 0.3f;
        b += (Hash(x, y, 3) - 0.5f) * 0.02f;
        return new Color(b * 0.97f, b, b * 1.02f);
    });

    static Texture2D CardTex(string name, int seed) => MakeTex(name, 256, 256, false, true, (u, v, x, y) =>
    {
        float b = 0.86f + (TileN(u, v, 6f, seed) - 0.5f) * 0.22f;
        b += Mathf.Sin(v * Mathf.PI * 2f * 24f) * 0.025f;              // corrugation lines
        b += (Hash(x, y, seed) - 0.5f) * 0.04f;
        return new Color(0.80f * b, 0.63f * b, 0.42f * b);
    });

    static Texture2D WoodTex() => MakeTex("wf-wood", 256, 256, false, true, (u, v, x, y) =>
    {
        float g = TileN(u, v * 0.25f, 5f, 6) * 0.6f + Mathf.PerlinNoise(77f + u * 2f, 11f + v * 70f) * 0.4f;
        float plank = Mathf.Repeat(v * 4f, 1f) < 0.03f ? 0.55f : 1f;
        float b = Mathf.Lerp(0.5f, 0.78f, g) * plank;
        return new Color(b, b * 0.74f, b * 0.48f);
    });

    /// <summary>A white shipping label with a few text lines and a barcode.</summary>
    static Texture2D LabelTex() => MakeTex("wf-label", 128, 64, false, false, (u, v, x, y) =>
    {
        if (v > 0.62f) return (v > 0.68f && v < 0.74f && u < 0.7f) || (v > 0.84f && v < 0.9f && u < 0.45f) ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.96f, 0.96f, 0.94f);
        if (u < 0.08f || u > 0.92f || v < 0.08f) return new Color(0.96f, 0.96f, 0.94f);
        int bar = x / 2;
        return Hash(bar, 1, 8) > 0.45f ? new Color(0.06f, 0.06f, 0.06f) : new Color(0.96f, 0.96f, 0.94f);
    });

    /// <summary>Two dark wheel tracks with broken edges, fading at both ends (a decal, drawn with alpha).</summary>
    static Texture2D TyreTex(string name, int seed) => MakeTex(name, 512, 128, true, false, (u, v, x, y) =>
    {
        float a = 0f;
        foreach (float c in new[] { 0.22f, 0.78f })
        {
            float d = (v - c) / 0.085f;
            float k = Mathf.Exp(-d * d);
            a = Mathf.Max(a, k * (0.15f + 0.6f * Mathf.PerlinNoise(seed * 5f + u * 38f, v * 5f + seed)));
        }
        float ends = Mathf.Clamp01(Mathf.Min(u, 1f - u) / 0.12f);
        return new Color(0.04f, 0.04f, 0.045f, a * ends * 0.8f);
    });

    static Texture2D StainTex(string name, int seed) => MakeTex(name, 128, 128, true, false, (u, v, x, y) =>
    {
        float dx = u - 0.5f, dy = v - 0.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        float edge = 0.72f + (Mathf.PerlinNoise(seed * 3f + u * 6f, seed + v * 6f) - 0.5f) * 0.5f;
        float a = Mathf.Clamp01((edge - r) / 0.25f);
        return new Color(0.02f, 0.025f, 0.03f, a * 0.85f);
    });

    static Texture2D SoftDotTex() => MakeTex("wf-softdot", 64, 64, true, false, (u, v, x, y) =>
    {
        float r = Mathf.Clamp01(Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f);
        float a = (1f - r) * (1f - r);
        return new Color(1f, 1f, 1f, a);
    });

    Material DecalMat(Texture tex, float smooth, Color? tint = null)
    {
        var m = P.Transparent(tint ?? Color.white);
        m.mainTexture = tex;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        return m;
    }

    void InitDetailMaterials()
    {
        mWall = P.Mat(Color.white, 0f, 0.2f, WallTex(), 1f);
        mWall.mainTextureScale = new Vector2(6f, 1f);
        var card = CardTex("wf-card-a", 11);
        mCard0 = P.Mat(Color.white, 0f, 0.15f, card, 1f);
        mCard1 = P.Mat(new Color(0.88f, 0.82f, 0.74f), 0f, 0.15f, CardTex("wf-card-b", 12), 1f);
        mCard2 = P.Mat(new Color(1.08f, 1.02f, 0.92f), 0f, 0.15f, CardTex("wf-card-c", 13), 1f);
        mWood = P.Mat(Color.white, 0f, 0.1f, WoodTex(), 1f);
        mRubber = P.Mat(new Color(0.05f, 0.05f, 0.055f), 0f, 0.2f);
        mGalv = P.Mat(new Color(0.72f, 0.74f, 0.76f), 0.85f, 0.5f);
        mRed = P.Mat(new Color(0.78f, 0.07f, 0.05f), 0.1f, 0.55f);
        mYellow = P.Mat(new Color(1f, 0.8f, 0.05f), 0.1f, 0.5f);
        mLabel = P.Mat(Color.white, 0f, 0.3f, LabelTex(), 1f);
        mWrap = P.Transparent(new Color(0.85f, 0.93f, 1f, 0.13f));
        if (mWrap.HasProperty("_Glossiness")) mWrap.SetFloat("_Glossiness", 0.95f);
        mCrate = P.Mat(new Color(0.12f, 0.3f, 0.6f), 0f, 0.5f);
        mCrate2 = P.Mat(new Color(0.45f, 0.47f, 0.5f), 0f, 0.5f);
        mLampWhite = P.Glow(new Color(1f, 0.97f, 0.9f), 1.8f);
        concrete = P.Mat(Color.white, 0f, 0.52f, FloorTex(), 8f); // sealed concrete: picks up the reflection probe
        softDot = SoftDotTex();
        mDecalTyreA = DecalMat(TyreTex("wf-tyre-a", 1), 0.3f);
        mDecalTyreB = DecalMat(TyreTex("wf-tyre-b", 2), 0.3f);
        mDecalStain = DecalMat(StainTex("wf-stain", 3), 0.9f);
        mSpill = DecalMat(StainTex("wf-spill", 7), 0.97f, new Color(0.5f, 0.6f, 0.75f, 1f));
        mGlowPool = DecalMat(softDot, 0f, new Color(1f, 0.95f, 0.8f, 0.55f));
        mGlowPool.EnableKeyword("_EMISSION");
        mGlowPool.SetColor("_EmissionColor", new Color(1f, 0.93f, 0.75f) * 1.4f);
    }

    // ------------------------------------------------------------------------------------------------- small helpers

    GameObject Bx(Transform p, string n, Vector3 pos, Vector3 size, Material m, float pitch = 0f, float yaw = 0f, float roll = 0f)
    {
        var go = P.Box(p, n, pos, size, m, false);
        if ((pitch != 0f || yaw != 0f || roll != 0f) && P.IsNew(go)) go.transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
        return go;
    }

    GameObject Cy(Transform p, string n, Vector3 pos, float r, float len, Vector3 axis, Material m) => P.Cyl(p, n, pos, r, len, axis, m, false);

    GameObject Decal(Transform root, string name, Vector3 pos, float yaw, Vector2 size, Material m)
    {
        var go = P.Prim(PrimitiveType.Quad, root, name, pos, new Vector3(size.x, size.y, 1f), m, false);
        if (P.IsNew(go)) go.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
        return go;
    }

    Transform Local(Transform parent, string name, Vector3 pos, float yaw)
    {
        var t = P.Node(name, parent).transform;
        if (P.IsNew(t)) t.SetLocalPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        return t;
    }

    static Mesh ConeMesh(string name, float rTop, float rBottom, float height, int seg)
    {
        var verts = new Vector3[(seg + 1) * 2];
        var tris = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float a = 2f * Mathf.PI * i / seg;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            verts[i] = new Vector3(c * rTop, 0f, s * rTop);
            verts[i + seg + 1] = new Vector3(c * rBottom, -height, s * rBottom);
        }
        for (int i = 0; i < seg; i++)
        {
            int a0 = i, a1 = i + 1, b0 = i + seg + 1, b1 = i + seg + 2;
            tris.AddRange(new[] { a0, b0, a1, a1, b0, b1 });   // outside
            tris.AddRange(new[] { a0, a1, b0, a1, b1, b0 });   // inside (the shaft is visible from within too)
        }
        var m = new Mesh { name = name, vertices = verts, triangles = tris.ToArray() };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// <summary>A pallet with a wood-textured deck and three stringers; origin on the floor at its centre.</summary>
    Transform Pallet(Transform parent, string name, Vector3 pos, float yaw, float w = 1.0f, float l = 1.2f)
    {
        var n = Local(parent, name, pos, yaw);
        Bx(n, "Deck", new Vector3(0, 0.125f, 0), new Vector3(w, 0.03f, l), mWood);
        Bx(n, "Bottom", new Vector3(0, 0.02f, 0), new Vector3(w, 0.02f, l * 0.9f), mWood);
        for (int i = -1; i <= 1; i++) Bx(n, "Stringer", new Vector3(i * (w / 2f - 0.05f), 0.07f, 0), new Vector3(0.09f, 0.09f, l), mWood);
        return n;
    }

    /// <summary>A pallet of boxed goods under stretch-wrap (the wrap is a thin glossy transparent shell).</summary>
    void WrappedPallet(Transform parent, string name, Vector3 pos, float yaw, float height, Material goods, System.Random rng)
    {
        var n = Pallet(parent, name, pos, yaw);
        float h = height;
        Bx(n, "Goods", new Vector3(0, 0.14f + h / 2f, 0), new Vector3(0.9f, h, 1.1f), goods);
        Bx(n, "Goods seam", new Vector3(0, 0.14f + h * 0.5f, 0), new Vector3(0.905f, 0.012f, 1.105f), mCard1);
        Bx(n, "Wrap", new Vector3(0, 0.14f + h / 2f, 0), new Vector3(0.95f, h + 0.04f, 1.15f), mWrap);
        Bx(n, "Pallet label", new Vector3(0.457f, 0.14f + h * 0.55f, 0.2f), new Vector3(0.01f, 0.16f, 0.3f), mLabel);
        Bx(n, "Pallet label", new Vector3(-0.457f, 0.14f + h * 0.55f, -0.2f), new Vector3(0.01f, 0.16f, 0.3f), mLabel);
    }

    void Drum(Transform parent, string name, Vector3 pos, Material mat)
    {
        Cy(parent, name, pos + new Vector3(0, 0.44f, 0), 0.29f, 0.88f, Vector3.up, mat);
        for (int i = 0; i < 3; i++) Cy(parent, name + " rib", pos + new Vector3(0, 0.2f + i * 0.24f, 0), 0.295f, 0.025f, Vector3.up, mat);
        Cy(parent, name + " lid", pos + new Vector3(0, 0.885f, 0), 0.26f, 0.02f, Vector3.up, mGalv);
    }

    void PalletJack(Transform root, string name, Vector3 pos, float yaw)
    {
        var n = Local(root, name, pos, yaw);
        foreach (float sx in new[] { -0.27f, 0.27f })
        {
            Bx(n, "Fork", new Vector3(sx, 0.07f, 0.6f), new Vector3(0.15f, 0.07f, 1.1f), darkSteel);
            Cy(n, "Fork wheel", new Vector3(sx, 0.05f, 1.05f), 0.05f, 0.07f, Vector3.right, mRubber);
        }
        Bx(n, "Body", new Vector3(0, 0.16f, -0.02f), new Vector3(0.72f, 0.2f, 0.3f), mRed);
        Cy(n, "Drive wheel", new Vector3(0, 0.1f, -0.2f), 0.1f, 0.09f, Vector3.right, mRubber);
        Cy(n, "Handle", new Vector3(0, 0.62f, -0.45f), 0.02f, 1.0f, new Vector3(0f, 1f, -0.7f), steel);
        Bx(n, "Grip", new Vector3(0, 1.08f, -0.74f), new Vector3(0.34f, 0.04f, 0.05f), mYellow);
        Bx(n, "Lever", new Vector3(0.1f, 1.04f, -0.7f), new Vector3(0.03f, 0.1f, 0.03f), mRubber);
    }

    // ------------------------------------------------------------------------------------------------- entry

    void BuildDetail()
    {
        BuildFloorWear();
        BuildColumns();
        BuildCeilingServices();
        BuildFixtures();
        BuildDockDetail();
        BuildBollards();
        BuildStaging();
        BuildSafetyEquipment();
        BuildBreakArea();
        BuildLockersAndShelving();
        BuildNoticeBoard();
        BuildProbeAndDust();
    }

    // ------------------------------------------------------------------------------------------------- floor wear

    void BuildFloorWear()
    {
        var root = NewRoot("Floor wear");
        var joint = P.Mat(new Color(0.2f, 0.2f, 0.21f), 0f, 0.1f);
        foreach (float x in new[] { -10.5f, -7f, -3.5f, 0f, 3.5f, 7f, 10.5f })
            Bx(root, "Saw cut", new Vector3(x, 0.002f, 2f), new Vector3(0.025f, 0.004f, 27.6f), joint);
        foreach (float z in new[] { -10.5f, -7f, -3.5f, 0f, 3.5f, 7f, 10.5f, 14f })
            Bx(root, "Saw cut", new Vector3(0f, 0.002f, z), new Vector3(27.6f, 0.004f, 0.025f), joint);

        // wheel tracks along the forklift route (south, east, north, west) and across the corners
        Decal(root, "Tyre marks", new Vector3(-5f, 0.0125f, 0.05f), 0f, new Vector2(10f, 1.5f), mDecalTyreA);
        Decal(root, "Tyre marks", new Vector3(5f, 0.0125f, -0.05f), 0f, new Vector2(10f, 1.5f), mDecalTyreB);
        Decal(root, "Tyre marks", new Vector3(10.05f, 0.0125f, 3f), 90f, new Vector2(6f, 1.5f), mDecalTyreB);
        Decal(root, "Tyre marks", new Vector3(9.95f, 0.0125f, 9f), 90f, new Vector2(6f, 1.5f), mDecalTyreA);
        Decal(root, "Tyre marks", new Vector3(5f, 0.0125f, 12.05f), 0f, new Vector2(10f, 1.5f), mDecalTyreA);
        Decal(root, "Tyre marks", new Vector3(-5f, 0.0125f, 11.95f), 0f, new Vector2(10f, 1.5f), mDecalTyreB);
        Decal(root, "Tyre marks", new Vector3(-10.05f, 0.0125f, 9f), 90f, new Vector2(6f, 1.5f), mDecalTyreB);
        Decal(root, "Tyre marks", new Vector3(-9.95f, 0.0125f, 3f), 90f, new Vector2(6f, 1.5f), mDecalTyreA);
        Decal(root, "Tyre marks corner", new Vector3(9.3f, 0.0126f, 0.8f), 45f, new Vector2(2.6f, 1.5f), mDecalTyreA);
        Decal(root, "Tyre marks corner", new Vector3(9.3f, 0.0126f, 11.2f), -45f, new Vector2(2.6f, 1.5f), mDecalTyreB);
        Decal(root, "Tyre marks corner", new Vector3(-9.3f, 0.0126f, 11.2f), 45f, new Vector2(2.6f, 1.5f), mDecalTyreA);
        Decal(root, "Tyre marks corner", new Vector3(-9.3f, 0.0126f, 0.8f), -45f, new Vector2(2.6f, 1.5f), mDecalTyreB);

        // oil and water stains, and a scuffed patch where pallets are set down
        var rng = new System.Random(5);
        Vector3[] spots =
        {
            new Vector3(5.0f, 0, -0.9f), new Vector3(-9.9f, 0, 3.0f), new Vector3(9.6f, 0, 8.2f), new Vector3(0.5f, 0, 11.3f),
            new Vector3(-3.8f, 0, 7.4f), new Vector3(7.2f, 0, -3.5f), new Vector3(-11f, 0, -8f), new Vector3(1.8f, 0, -0.3f),
            new Vector3(-6.6f, 0, 12.2f), new Vector3(12.8f, 0, 11.0f),
        };
        foreach (var s in spots)
        {
            float sz = 0.5f + (float)rng.NextDouble() * 0.9f;
            Decal(root, "Stain", new Vector3(s.x, 0.0135f, s.z), (float)rng.NextDouble() * 180f, new Vector2(sz, sz * (0.7f + (float)rng.NextDouble() * 0.4f)), mDecalStain);
        }
        // the spill itself: a glossy bluish pool with a stained edge, on top of the existing dark disc
        Decal(root, "Spill gloss", new Vector3(SpillPos.x + 0.1f, 0.0135f, SpillPos.z), 20f, new Vector2(2.6f, 2.2f), mSpill);
        Decal(root, "Spill gloss", new Vector3(SpillPos.x - 0.7f, 0.0136f, SpillPos.z + 0.55f), 70f, new Vector2(1.0f, 0.8f), mSpill);

        // battery charging bay, painted in the south-east (hatched, with a stop line)
        float bx = 7.5f, bz = -4.2f;
        Paint(root, "Bay line", bx - 1.6f, bz - 1.1f, bx + 1.6f, bz - 1.04f, mYellow, 0.0115f);
        Paint(root, "Bay line", bx - 1.6f, bz + 1.04f, bx + 1.6f, bz + 1.1f, mYellow, 0.0115f);
        Paint(root, "Bay line", bx - 1.6f, bz - 1.1f, bx - 1.54f, bz + 1.1f, mYellow, 0.0115f);
        Paint(root, "Bay line", bx + 1.54f, bz - 1.1f, bx + 1.6f, bz + 1.1f, mYellow, 0.0115f);
        for (int i = 0; i < 7; i++)
            Bx(root, "Bay hatch", new Vector3(bx - 1.2f + i * 0.4f, 0.0118f, bz), new Vector3(0.07f, 0.008f, 1.6f), mYellow, 0f, 35f);
        FloorText(root, "CHARGING", new Vector3(bx, 0.0175f, bz - 1.45f), 0.2f, SafetyYellow);
        FloorText(root, "PALLET JACKS", new Vector3(-10.5f, 0.0175f, -10.4f), 0.14f, Color.white);

        // red fire-point squares under each extinguisher
        foreach (var p in new[] { new Vector3(-12.4f, 0, -11.2f), new Vector3(13.2f, 0, -2.4f), new Vector3(8.0f, 0, 15.2f) })
        {
            Paint(root, "Fire point", p.x - 0.4f, p.z - 0.4f, p.x + 0.4f, p.z - 0.32f, mRed, 0.0112f);
            Paint(root, "Fire point", p.x - 0.4f, p.z + 0.32f, p.x + 0.4f, p.z + 0.4f, mRed, 0.0112f);
            Paint(root, "Fire point", p.x - 0.4f, p.z - 0.4f, p.x - 0.32f, p.z + 0.4f, mRed, 0.0112f);
            Paint(root, "Fire point", p.x + 0.32f, p.z - 0.4f, p.x + 0.4f, p.z + 0.4f, mRed, 0.0112f);
        }
    }

    // ------------------------------------------------------------------------------------------------- columns

    void BuildColumns()
    {
        var root = NewRoot("Columns");
        var paint = P.Mat(new Color(0.32f, 0.38f, 0.5f), 0.3f, 0.45f);
        Vector3[] cols =
        {
            new Vector3(-10.5f, 0, 15.75f), new Vector3(-5.5f, 0, 15.75f), new Vector3(5.5f, 0, 15.75f), new Vector3(10.5f, 0, 15.75f),
            new Vector3(-6f, 0, -11.75f), new Vector3(6f, 0, -11.75f), new Vector3(11f, 0, -11.75f),
            new Vector3(13.75f, 0, -4f), new Vector3(13.75f, 0, 11f), new Vector3(-13.75f, 0, -3f), new Vector3(-13.75f, 0, 14.5f),
        };
        foreach (var c in cols)
        {
            Bx(root, "Column", c + new Vector3(0, 3.5f, 0), new Vector3(0.36f, 7f, 0.36f), paint);
            Bx(root, "Column flange", c + new Vector3(0, 3.5f, 0), new Vector3(0.42f, 6.9f, 0.1f), paint);
            Bx(root, "Column guard", c + new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1.0f, 0.5f), hazard);
            Bx(root, "Column guard cap", c + new Vector3(0, 1.02f, 0), new Vector3(0.54f, 0.05f, 0.54f), darkSteel);
            Bx(root, "Column base", c + new Vector3(0, 0.015f, 0), new Vector3(0.6f, 0.03f, 0.6f), darkSteel);
            Bx(root, "Column bracket", c + new Vector3(0, 6.7f, 0), new Vector3(0.5f, 0.3f, 0.5f), darkSteel);
        }
        CombineStatic(root);
    }

    // ------------------------------------------------------------------------------------------------- ceiling services

    void BuildCeilingServices()
    {
        var root = NewRoot("Ceiling services");
        var duct = mGalv;
        // round supply duct along x, with joint bands and hangers
        Cy(root, "Duct", new Vector3(0, 6.0f, 7.5f), 0.45f, 27f, Vector3.right, duct);
        for (int i = -4; i <= 4; i++)
        {
            Cy(root, "Duct band", new Vector3(i * 3f, 6.0f, 7.5f), 0.47f, 0.08f, Vector3.right, steel);
            Bx(root, "Duct hanger", new Vector3(i * 3f + 1.5f, 6.55f, 7.5f - 0.0f), new Vector3(0.03f, 0.9f, 0.03f), darkSteel);
            Bx(root, "Duct strap", new Vector3(i * 3f + 1.5f, 6.0f, 7.5f), new Vector3(0.05f, 0.95f, 1.0f), darkSteel);
        }
        // rectangular return duct along z
        Bx(root, "Return duct", new Vector3(0f, 5.8f, 2f), new Vector3(0.8f, 0.5f, 26f), duct);
        for (int i = 0; i < 6; i++)
        {
            Bx(root, "Return duct seam", new Vector3(0f, 5.8f, -9.5f + i * 4.8f), new Vector3(0.84f, 0.54f, 0.05f), steel);
            Bx(root, "Return hanger", new Vector3(0.0f, 6.45f, -9.5f + i * 4.8f + 2.4f), new Vector3(0.7f, 0.04f, 0.04f), darkSteel);
        }
        // entry-zone duct over the PPE station
        Cy(root, "Entry duct", new Vector3(-6f, 6.1f, -9f), 0.35f, 15f, Vector3.right, duct);
        for (int i = 0; i < 5; i++) Cy(root, "Entry duct band", new Vector3(-12f + i * 3f, 6.1f, -9f), 0.37f, 0.07f, Vector3.right, steel);

        // sprinkler main (red) with branch lines and heads
        Cy(root, "Sprinkler main", new Vector3(5f, 6.3f, 2f), 0.07f, 26f, Vector3.forward, mRed);
        foreach (float z in new[] { -8f, 0f, 8f })
        {
            Cy(root, "Sprinkler branch", new Vector3(-1f, 6.3f, z), 0.045f, 24f, Vector3.right, mRed);
            for (int i = 0; i < 6; i++)
            {
                float x = -10f + i * 4.4f;
                Cy(root, "Sprinkler drop", new Vector3(x, 6.18f, z), 0.015f, 0.25f, Vector3.up, steel);
                Cy(root, "Sprinkler head", new Vector3(x, 6.04f, z), 0.04f, 0.03f, Vector3.up, mYellow);
            }
        }
        // cable tray with cable bundles, and a conduit run
        Bx(root, "Cable tray", new Vector3(10.5f, 6.25f, 2f), new Vector3(0.5f, 0.06f, 26f), darkSteel);
        Bx(root, "Cable tray rail", new Vector3(10.27f, 6.31f, 2f), new Vector3(0.03f, 0.1f, 26f), darkSteel);
        Bx(root, "Cable tray rail", new Vector3(10.73f, 6.31f, 2f), new Vector3(0.03f, 0.1f, 26f), darkSteel);
        for (int i = 0; i < 3; i++)
            Cy(root, "Cable bundle", new Vector3(10.38f + i * 0.12f, 6.34f, 2f), 0.035f, 25.5f, Vector3.forward, P.Mat(i == 1 ? new Color(0.1f, 0.2f, 0.6f) : new Color(0.05f, 0.05f, 0.06f), 0f, 0.3f));
        Cy(root, "Conduit", new Vector3(-12.2f, 6.2f, 2f), 0.03f, 26f, Vector3.forward, steel);
        Cy(root, "Conduit", new Vector3(-12.0f, 6.2f, 2f), 0.03f, 26f, Vector3.forward, steel);
        CombineStatic(root);
    }

    // ------------------------------------------------------------------------------------------------- fixtures, light shafts

    void BuildFixtures()
    {
        var root = NewRoot("Light fixtures");
        var housing = P.Mat(new Color(0.12f, 0.13f, 0.15f), 0.5f, 0.4f);
        foreach (float lx in new[] { -7f, 7f })
        foreach (float lz in new[] { -6f, 3f, 12f })
        {
            var pos = new Vector3(lx, 6.55f, lz);
            // housing around the glowing panel, hangers up to the roof
            Bx(root, "Fixture frame", pos + new Vector3(0, 0.02f, 0.33f), new Vector3(2.4f, 0.12f, 0.06f), housing);
            Bx(root, "Fixture frame", pos + new Vector3(0, 0.02f, -0.33f), new Vector3(2.4f, 0.12f, 0.06f), housing);
            Bx(root, "Fixture frame", pos + new Vector3(1.17f, 0.02f, 0), new Vector3(0.06f, 0.12f, 0.66f), housing);
            Bx(root, "Fixture frame", pos + new Vector3(-1.17f, 0.02f, 0), new Vector3(0.06f, 0.12f, 0.66f), housing);
            Bx(root, "Fixture top", pos + new Vector3(0, 0.1f, 0), new Vector3(2.3f, 0.06f, 0.62f), housing);
            Bx(root, "Fixture hanger", pos + new Vector3(-0.9f, 0.3f, 0), new Vector3(0.02f, 0.4f, 0.02f), steel);
            Bx(root, "Fixture hanger", pos + new Vector3(0.9f, 0.3f, 0), new Vector3(0.02f, 0.4f, 0.02f), steel);
            // soft light pool on the floor and an additive light shaft from the kit (one draw each)
            Decal(root, "Light pool", new Vector3(lx, 0.0145f, lz), 0f, new Vector2(9f, 9f), mGlowPool);
            Fx.LightShaft(root, pos + new Vector3(0, -0.05f, 0), 6.1f, 30f, new Color(1f, 0.94f, 0.8f), 0.07f);
        }
        // cool daylight from the open dock door against the warm fixtures; a warm low light on the break area.
        // Only the dock light is a pixel light (budget 1); the break light is a vertex light.
        Fx.PixelLightBudget = 1;
        Fx.Practical(root, new Vector3(12.4f, 2.4f, 6f), new Color(0.68f, 0.82f, 1f), 11f, 1.4f);
        Fx.Practical(root, new Vector3(10.5f, 3.2f, -9f), new Color(1f, 0.86f, 0.65f), 8f, 0.8f);
    }

    // ------------------------------------------------------------------------------------------------- dock

    void BuildDockDetail()
    {
        var root = NewRoot("Dock detail");
        float x = 13.8f;
        // dock leveller plate with hazard lip, rubber seals, door guides
        Bx(root, "Leveller plate", new Vector3(13.05f, 0.025f, 6f), new Vector3(1.5f, 0.05f, 2.6f), P.Mat(new Color(0.5f, 0.52f, 0.55f), 0.7f, 0.5f));
        Bx(root, "Leveller lip", new Vector3(12.27f, 0.026f, 6f), new Vector3(0.06f, 0.052f, 2.6f), hazard);
        Bx(root, "Leveller pit", new Vector3(13.05f, 0.012f, 6f), new Vector3(1.6f, 0.02f, 2.75f), mRubber);
        Bx(root, "Dock seal", new Vector3(x - 0.3f, 1.8f, 3.95f), new Vector3(0.5f, 3.6f, 0.3f), mRubber);
        Bx(root, "Dock seal", new Vector3(x - 0.3f, 1.8f, 8.05f), new Vector3(0.5f, 3.6f, 0.3f), mRubber);
        Bx(root, "Dock seal head", new Vector3(x - 0.3f, 3.55f, 6f), new Vector3(0.5f, 0.3f, 4.2f), mRubber);
        Bx(root, "Door guide", new Vector3(x - 0.1f, 1.9f, 3.6f), new Vector3(0.1f, 3.8f, 0.08f), steel);
        Bx(root, "Door guide", new Vector3(x - 0.1f, 1.9f, 8.4f), new Vector3(0.1f, 3.8f, 0.08f), steel);
        // rolled-up door slats at the top
        Cy(root, "Door slats", new Vector3(x - 0.2f, 3.55f, 6f), 0.25f, 4.2f, Vector3.forward, P.Mat(new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.4f));
        // control box and traffic lights (red outside, green inside), wall mounted at the north side of the door
        Bx(root, "Dock control box", new Vector3(13.88f, 1.4f, 9.0f), new Vector3(0.12f, 0.4f, 0.26f), mYellow);
        Bx(root, "Dock button", new Vector3(13.8f, 1.5f, 9.0f), new Vector3(0.04f, 0.07f, 0.07f), P.Glow(new Color(0.1f, 0.9f, 0.3f), 1.4f));
        Bx(root, "Dock button", new Vector3(13.8f, 1.35f, 9.0f), new Vector3(0.04f, 0.07f, 0.07f), P.Glow(new Color(1f, 0.15f, 0.1f), 1.4f));
        Bx(root, "Dock light box", new Vector3(13.88f, 3.0f, 9.0f), new Vector3(0.1f, 0.5f, 0.2f), mRubber);
        P.Ball(root, "Dock lamp red", new Vector3(13.8f, 3.12f, 9.0f), 0.07f, P.Glow(new Color(1f, 0.12f, 0.08f), 1.6f), false);
        P.Ball(root, "Dock lamp green", new Vector3(13.8f, 2.9f, 9.0f), 0.07f, P.Glow(new Color(0.1f, 1f, 0.3f), 1.2f), false);
        // light spill from the door onto the floor
        var spill = DecalMat(softDot, 0f, new Color(0.75f, 0.88f, 1f, 0.4f));
        spill.EnableKeyword("_EMISSION");
        spill.SetColor("_EmissionColor", new Color(0.7f, 0.85f, 1f) * 0.9f);
        Decal(root, "Door light spill", new Vector3(12.0f, 0.0148f, 6f), 0f, new Vector2(4.4f, 6.2f), spill);
        // wheel chocks and a dock plate sign stand (floor)
        Bx(root, "Wheel chock", new Vector3(12.8f, 0.07f, 3.2f), new Vector3(0.25f, 0.14f, 0.3f), mYellow, 0f, 0f, 0f);
        Bx(root, "Wheel chock", new Vector3(12.8f, 0.07f, 8.8f), new Vector3(0.25f, 0.14f, 0.3f), mYellow, 0f, 0f, 0f);
        CombineStatic(root);
    }

    // ------------------------------------------------------------------------------------------------- bollards

    void BuildBollards()
    {
        var root = NewRoot("Bollards");
        void Bollard(float bx, float bz)
        {
            Cy(root, "Bollard", new Vector3(bx, 0.45f, bz), 0.1f, 0.9f, Vector3.up, mYellow);
            Cy(root, "Bollard band", new Vector3(bx, 0.62f, bz), 0.102f, 0.1f, Vector3.up, mRubber);
            Cy(root, "Bollard cap", new Vector3(bx, 0.905f, bz), 0.105f, 0.02f, Vector3.up, mYellow);
            Cy(root, "Bollard base", new Vector3(bx, 0.012f, bz), 0.16f, 0.024f, Vector3.up, darkSteel);
        }
        foreach (float cx in new[] { -1f, 3f, 6f })
        {
            foreach (float dx in new[] { -0.4f, 0.4f })
            {
                Bollard(cx + dx, 2.15f);
                Bollard(cx + dx, 9.85f);
            }
        }
        // dock and entrance protection
        Bollard(13.2f, 3.1f);
        Bollard(13.2f, 8.9f);
        Bollard(-13.1f, 13.0f);
        Bollard(-13.1f, -0.4f);
        // low guard rail around the charging bay
        Bx(root, "Charger guard", new Vector3(7.5f, 0.4f, -5.45f), new Vector3(3.2f, 0.07f, 0.07f), mYellow);
        Bx(root, "Charger guard", new Vector3(7.5f, 0.2f, -5.45f), new Vector3(3.2f, 0.07f, 0.07f), mYellow);
        foreach (float px in new[] { 5.9f, 7.5f, 9.1f }) Bx(root, "Charger guard post", new Vector3(px, 0.25f, -5.45f), new Vector3(0.07f, 0.5f, 0.07f), mYellow);
        // the charger itself, with a status lamp and cable
        Bx(root, "Battery charger", new Vector3(7.5f, 0.75f, -2.35f), new Vector3(1.1f, 1.5f, 0.5f), P.Mat(new Color(0.78f, 0.8f, 0.82f), 0.3f, 0.5f));
        Bx(root, "Charger panel", new Vector3(7.5f, 1.15f, -2.6f), new Vector3(0.7f, 0.3f, 0.02f), mRubber);
        P.Ball(root, "Charger lamp", new Vector3(7.7f, 1.25f, -2.62f), 0.03f, P.Glow(new Color(0.1f, 1f, 0.3f), 1.6f), false);
        P.Ball(root, "Charger lamp", new Vector3(7.3f, 1.25f, -2.62f), 0.03f, P.Glow(new Color(1f, 0.6f, 0.1f), 1.6f), false);
        Cy(root, "Charger cable", new Vector3(7.5f, 0.12f, -3.0f), 0.025f, 1.3f, Vector3.forward, mRubber);
        // aisle signs hung on the rack ends (face north, toward the north lane)
        CombineStatic(root);
        var sign = Sign(transform, "Aisle sign 1", new Vector3(-1f, 3.5f, 9.46f), Vector3.forward, new Vector2(1.0f, 0.34f), Navy, SafetyYellow, 0.1f);
        SignText(sign, "AISLE 1", -0.01f, 0.13f, SafetyYellow);
        sign = Sign(transform, "Aisle sign 2", new Vector3(3f, 3.5f, 9.46f), Vector3.forward, new Vector2(1.0f, 0.34f), Navy, SafetyYellow, 0.1f);
        SignText(sign, "AISLE 2", -0.01f, 0.13f, SafetyYellow);
        sign = Sign(transform, "Aisle sign 3", new Vector3(6f, 3.5f, 9.46f), Vector3.forward, new Vector2(1.0f, 0.34f), Navy, SafetyYellow, 0.1f);
        SignText(sign, "AISLE 3", -0.01f, 0.13f, SafetyYellow);
    }

    // ------------------------------------------------------------------------------------------------- staging, pallet jacks, drums

    void BuildStaging()
    {
        var root = NewRoot("Staging");
        var rng = new System.Random(31);
        // east side, beside the dock: wrapped pallets, an empty pallet stack, drums, crates
        WrappedPallet(root, "Wrapped pallet", new Vector3(13.0f, 0, 10.4f), 8f, 1.2f, mCard0, rng);
        WrappedPallet(root, "Wrapped pallet", new Vector3(13.0f, 0, 11.9f), -5f, 0.9f, mCard1, rng);
        WrappedPallet(root, "Wrapped pallet", new Vector3(13.05f, 0, 1.0f), 4f, 1.3f, mCard2, rng);
        WrappedPallet(root, "Wrapped pallet", new Vector3(13.0f, 0, -0.6f), -9f, 0.8f, mCard0, rng);
        for (int i = 0; i < 6; i++) Pallet(root, "Stacked pallet", new Vector3(12.95f + (float)(rng.NextDouble() - 0.5) * 0.06f, i * 0.155f, 13.2f), (float)(rng.NextDouble() - 0.5) * 6f);
        var blueDrum = P.Mat(new Color(0.1f, 0.3f, 0.65f), 0.5f, 0.55f);
        var greenDrum = P.Mat(new Color(0.1f, 0.45f, 0.25f), 0.5f, 0.55f);
        Pallet(root, "Drum pallet", new Vector3(13.0f, 0, 14.3f), 0f, 1.2f, 1.2f);
        Drum(root, "Drum", new Vector3(12.7f, 0.15f, 14.0f), blueDrum);
        Drum(root, "Drum", new Vector3(13.3f, 0.15f, 14.0f), blueDrum);
        Drum(root, "Drum", new Vector3(12.7f, 0.15f, 14.6f), greenDrum);
        Drum(root, "Drum", new Vector3(13.3f, 0.15f, 14.6f), greenDrum);
        // stacked crates
        for (int i = 0; i < 3; i++)
        {
            Bx(root, "Crate", new Vector3(11.8f + i * 0.0f, 0.15f + i * 0.31f, 15.2f), new Vector3(0.6f, 0.3f, 0.4f), i == 1 ? mCrate2 : mCrate);
            Bx(root, "Crate rim", new Vector3(11.8f, 0.31f + i * 0.31f, 15.2f), new Vector3(0.62f, 0.02f, 0.42f), mCrate2);
        }
        // pallet jacks: one at the entry bay, two near the charger
        PalletJack(root, "Pallet jack", new Vector3(-10.5f, 0, -9.8f), 70f);
        PalletJack(root, "Pallet jack", new Vector3(4.6f, 0, -4.0f), 10f);
        PalletJack(root, "Pallet jack", new Vector3(4.9f, 0, -3.2f), -20f);
        // empty pallets leaned at the charging bay and a pallet with a few loose boxes near the entry
        for (int i = 0; i < 3; i++) Pallet(root, "Stacked pallet", new Vector3(10.6f, i * 0.155f, -4.0f), 12f * i);
        Pallet(root, "Floor pallet", new Vector3(-12.2f, 0, -7.2f), 15f);
        Bx(root, "Loose carton", new Vector3(-12.3f, 0.43f, -7.3f), new Vector3(0.5f, 0.4f, 0.4f), mCard1, 0f, 12f);
        Bx(root, "Loose carton", new Vector3(-12.1f, 0.22f, -7.0f), new Vector3(0.4f, 0.3f, 0.5f), mCard0, 0f, -20f);
        // spill kit bin and the drum that spilled is already placed in BuildHazards
        var kit = Local(root, "Spill kit", new Vector3(-5.85f, 0, 6.9f), 90f);
        Bx(kit, "Bin", new Vector3(0, 0.35f, 0), new Vector3(0.5f, 0.7f, 0.5f), mYellow);
        Bx(kit, "Lid", new Vector3(0, 0.72f, 0), new Vector3(0.54f, 0.05f, 0.54f), P.Mat(new Color(0.9f, 0.7f, 0.05f), 0.1f, 0.5f));
        Bx(kit, "Label", new Vector3(0, 0.4f, -0.255f), new Vector3(0.3f, 0.14f, 0.01f), mLabel);
        // tidy: the combined static meshes keep draw calls down
        CombineStatic(root);
    }

    // ------------------------------------------------------------------------------------------------- safety equipment

    void BuildSafetyEquipment()
    {
        var root = NewRoot("Safety equipment");
        var cyl = mRed;
        void Extinguisher(Vector3 pos, Vector3 face)
        {
            var n = Local(root, "Fire extinguisher", pos, Quaternion.LookRotation(face).eulerAngles.y);
            Bx(n, "Bracket", new Vector3(0, 0.4f, -0.08f), new Vector3(0.14f, 0.05f, 0.04f), darkSteel);
            Cy(n, "Cylinder", new Vector3(0, 0.3f, 0), 0.085f, 0.5f, Vector3.up, cyl);
            P.Ball(n, "Dome", new Vector3(0, 0.55f, 0), 0.085f, cyl, false);
            Cy(n, "Valve", new Vector3(0, 0.66f, 0), 0.03f, 0.08f, Vector3.up, darkSteel);
            Bx(n, "Handle", new Vector3(0, 0.71f, 0.03f), new Vector3(0.03f, 0.03f, 0.14f), darkSteel);
            Cy(n, "Hose", new Vector3(0.1f, 0.4f, 0.04f), 0.012f, 0.4f, Vector3.up, mRubber);
            Bx(n, "Label", new Vector3(0, 0.34f, 0.086f), new Vector3(0.09f, 0.14f, 0.01f), mLabel);
        }
        Extinguisher(new Vector3(-12.4f, 0.95f, -11.85f), Vector3.forward);
        Extinguisher(new Vector3(13.85f, 0.95f, -2.4f), Vector3.left);
        Extinguisher(new Vector3(8.0f, 0.95f, 15.85f), Vector3.back);
        // red location signs above each (red plate with a white flame and a bar, from primitives)
        void FireSign(Vector3 pos, Vector3 face)
        {
            var n = Local(root, "Fire sign", pos, Quaternion.LookRotation(face).eulerAngles.y);
            Bx(n, "Plate", Vector3.zero, new Vector3(0.36f, 0.36f, 0.02f), mRed);
            Bx(n, "Frame", new Vector3(0, 0, 0.001f), new Vector3(0.39f, 0.39f, 0.015f), P.Mat(Color.white, 0f, 0.4f));
            Bx(n, "Plate", new Vector3(0, 0, 0.012f), new Vector3(0.36f, 0.36f, 0.01f), mRed);
            var white = P.Mat(Color.white, 0f, 0.4f);
            Bx(n, "Flame", new Vector3(0, 0.02f, 0.02f), new Vector3(0.1f, 0.16f, 0.01f), white, 0f, 0f, 15f);
            Bx(n, "Flame", new Vector3(0.03f, 0.0f, 0.02f), new Vector3(0.07f, 0.1f, 0.01f), white, 0f, 0f, -25f);
            Bx(n, "Base", new Vector3(0, -0.12f, 0.02f), new Vector3(0.24f, 0.03f, 0.01f), white);
        }
        FireSign(new Vector3(-12.4f, 2.0f, -11.9f), Vector3.forward);
        FireSign(new Vector3(13.88f, 2.0f, -2.4f), Vector3.left);
        FireSign(new Vector3(8.0f, 2.0f, 15.9f), Vector3.back);

        // first aid cabinet, eyewash station and AED-style defibrillator box (generic) on the south wall
        var green = P.Mat(new Color(0.05f, 0.55f, 0.3f), 0.1f, 0.5f);
        var white2 = P.Mat(new Color(0.94f, 0.95f, 0.95f), 0.1f, 0.5f);
        Bx(root, "First aid cabinet", new Vector3(-1.5f, 1.45f, -11.85f), new Vector3(0.7f, 0.55f, 0.16f), white2);
        Bx(root, "First aid cross", new Vector3(-1.5f, 1.5f, -11.76f), new Vector3(0.28f, 0.08f, 0.02f), green);
        Bx(root, "First aid cross", new Vector3(-1.5f, 1.5f, -11.76f), new Vector3(0.08f, 0.28f, 0.02f), green);
        Bx(root, "First aid handle", new Vector3(-1.5f, 1.22f, -11.76f), new Vector3(0.2f, 0.03f, 0.03f), darkSteel);
        Bx(root, "Eyewash unit", new Vector3(-0.3f, 1.0f, -11.8f), new Vector3(0.45f, 0.1f, 0.3f), green);
        Cy(root, "Eyewash bowl", new Vector3(-0.3f, 1.08f, -11.7f), 0.17f, 0.06f, Vector3.up, steel);
        Cy(root, "Eyewash nozzle", new Vector3(-0.38f, 1.2f, -11.73f), 0.015f, 0.1f, Vector3.up, mYellow);
        Cy(root, "Eyewash nozzle", new Vector3(-0.22f, 1.2f, -11.73f), 0.015f, 0.1f, Vector3.up, mYellow);
        Bx(root, "Eyewash pipe", new Vector3(-0.3f, 0.55f, -11.88f), new Vector3(0.06f, 1.0f, 0.06f), steel);
        var fa = Sign(transform, "First aid sign", new Vector3(-0.9f, 2.35f, -11.9f), Vector3.forward, new Vector2(1.6f, 0.4f), new Color(0.05f, 0.5f, 0.28f, 0.96f), Color.white, 0.07f);
        SignText(fa, "FIRST AID AND EYEWASH", -0.01f, 0.13f, Color.white);

        // exit signs (green panel with white text)
        var ex = Sign(transform, "Exit sign", new Vector3(-7f, 3.45f, 15.9f), Vector3.back, new Vector2(0.8f, 0.26f), new Color(0.05f, 0.5f, 0.26f, 0.98f), Color.white, 0.06f);
        SignText(ex, "EXIT", -0.01f, 0.12f, Color.white);
        ex = Sign(transform, "Exit sign", new Vector3(13.9f, 3.0f, -8f), Vector3.left, new Vector2(0.8f, 0.26f), new Color(0.05f, 0.5f, 0.26f, 0.98f), Color.white, 0.06f);
        SignText(ex, "EXIT", -0.01f, 0.12f, Color.white);
        CombineStatic(root);
    }

    // ------------------------------------------------------------------------------------------------- break area and lockers

    void BuildBreakArea()
    {
        var root = NewRoot("Break area");
        var table = P.Mat(new Color(0.85f, 0.86f, 0.84f), 0f, 0.5f);
        var chairMat = P.Mat(new Color(0.15f, 0.3f, 0.6f), 0f, 0.4f);
        var white = P.Mat(new Color(0.92f, 0.93f, 0.93f), 0.1f, 0.55f);
        // table with four chairs
        Bx(root, "Table top", new Vector3(10.5f, 0.75f, -9.0f), new Vector3(1.6f, 0.05f, 0.9f), table);
        foreach (float sx in new[] { -0.7f, 0.7f })
        foreach (float sz in new[] { -0.35f, 0.35f })
            Bx(root, "Table leg", new Vector3(10.5f + sx, 0.37f, -9f + sz), new Vector3(0.05f, 0.74f, 0.05f), darkSteel);
        for (int i = 0; i < 4; i++)
        {
            float cx = 10.5f + (i % 2 == 0 ? -0.4f : 0.4f);
            float cz = -9f + (i < 2 ? -0.75f : 0.75f);
            float yaw = i < 2 ? 0f : 180f;
            var c = Local(root, "Chair", new Vector3(cx, 0, cz), yaw + (i == 1 ? 8f : 0f));
            Bx(c, "Seat", new Vector3(0, 0.45f, 0), new Vector3(0.42f, 0.05f, 0.42f), chairMat);
            Bx(c, "Back", new Vector3(0, 0.75f, -0.19f), new Vector3(0.42f, 0.45f, 0.04f), chairMat);
            foreach (float lx in new[] { -0.18f, 0.18f })
            foreach (float lz in new[] { -0.18f, 0.18f })
                Bx(c, "Leg", new Vector3(lx, 0.22f, lz), new Vector3(0.03f, 0.44f, 0.03f), darkSteel);
        }
        // mugs, a newspaper and a lunch box on the table
        Cy(root, "Mug", new Vector3(10.2f, 0.82f, -9.1f), 0.04f, 0.09f, Vector3.up, white);
        Cy(root, "Mug", new Vector3(10.9f, 0.82f, -8.9f), 0.04f, 0.09f, Vector3.up, mRed);
        Bx(root, "Paper", new Vector3(10.55f, 0.78f, -9.15f), new Vector3(0.4f, 0.01f, 0.3f), mLabel, 0f, 14f);
        Bx(root, "Lunch box", new Vector3(10.7f, 0.83f, -8.8f), new Vector3(0.22f, 0.1f, 0.14f), mCrate);
        // counter with a coffee maker and a microwave
        Bx(root, "Counter", new Vector3(8.0f, 0.45f, -11.6f), new Vector3(2.6f, 0.9f, 0.6f), white);
        Bx(root, "Counter top", new Vector3(8.0f, 0.92f, -11.6f), new Vector3(2.7f, 0.04f, 0.66f), table);
        Bx(root, "Microwave", new Vector3(7.2f, 1.1f, -11.6f), new Vector3(0.5f, 0.3f, 0.38f), darkSteel);
        Bx(root, "Microwave window", new Vector3(7.12f, 1.1f, -11.4f), new Vector3(0.3f, 0.2f, 0.01f), P.Glow(new Color(0.2f, 0.4f, 0.55f), 0.4f));
        Bx(root, "Coffee maker", new Vector3(8.6f, 1.12f, -11.6f), new Vector3(0.28f, 0.34f, 0.28f), mRubber);
        Cy(root, "Coffee pot", new Vector3(8.6f, 1.0f, -11.4f), 0.07f, 0.16f, Vector3.up, P.Transparent(new Color(0.25f, 0.15f, 0.08f, 0.8f)));
        P.Ball(root, "Coffee lamp", new Vector3(8.6f, 1.25f, -11.44f), 0.015f, P.Glow(new Color(1f, 0.2f, 0.1f), 1.5f), false);
        Bx(root, "Sink", new Vector3(9.0f, 0.96f, -11.6f), new Vector3(0.5f, 0.03f, 0.4f), steel);
        Cy(root, "Tap", new Vector3(9.0f, 1.05f, -11.85f), 0.015f, 0.18f, Vector3.up, steel);
        // vending machine with a lit front, fridge, water cooler, bins
        Bx(root, "Vending machine", new Vector3(13.35f, 0.95f, -10.0f), new Vector3(0.85f, 1.9f, 0.95f), P.Mat(new Color(0.1f, 0.2f, 0.55f), 0.3f, 0.5f));
        Bx(root, "Vending front", new Vector3(12.9f, 1.25f, -10.0f), new Vector3(0.02f, 1.0f, 0.7f), P.Glow(new Color(0.85f, 0.92f, 1f), 0.9f));
        for (int r = 0; r < 4; r++)
        for (int c = 0; c < 3; c++)
            Bx(root, "Vending item", new Vector3(12.88f, 0.95f + r * 0.22f, -10.22f + c * 0.22f), new Vector3(0.03f, 0.12f, 0.1f), c % 2 == 0 ? mRed : mCrate);
        Bx(root, "Vending slot", new Vector3(12.9f, 0.4f, -10.0f), new Vector3(0.02f, 0.18f, 0.5f), mRubber);
        Bx(root, "Fridge", new Vector3(13.4f, 0.85f, -8.2f), new Vector3(0.7f, 1.7f, 0.7f), white);
        Bx(root, "Fridge door line", new Vector3(13.03f, 1.0f, -8.2f), new Vector3(0.01f, 0.02f, 0.68f), darkSteel);
        Bx(root, "Fridge handle", new Vector3(13.02f, 1.2f, -7.95f), new Vector3(0.03f, 0.4f, 0.03f), steel);
        Bx(root, "Cooler", new Vector3(13.45f, 0.5f, -6.8f), new Vector3(0.35f, 1.0f, 0.35f), white);
        Cy(root, "Cooler bottle", new Vector3(13.45f, 1.2f, -6.8f), 0.14f, 0.4f, Vector3.up, P.Transparent(new Color(0.3f, 0.6f, 0.95f, 0.5f)));
        Cy(root, "Waste bin", new Vector3(12.4f, 0.25f, -11.5f), 0.2f, 0.5f, Vector3.up, mCrate2);
        Cy(root, "Recycling bin", new Vector3(12.9f, 0.25f, -11.5f), 0.2f, 0.5f, Vector3.up, mCrate);
        // wall clock and a break-area sign
        Cy(root, "Clock", new Vector3(10.5f, 2.6f, -11.9f), 0.2f, 0.04f, Vector3.forward, P.Mat(Color.white, 0f, 0.5f));
        Bx(root, "Clock hand", new Vector3(10.5f, 2.64f, -11.87f), new Vector3(0.015f, 0.14f, 0.01f), mRubber);
        Bx(root, "Clock hand", new Vector3(10.55f, 2.6f, -11.87f), new Vector3(0.1f, 0.015f, 0.01f), mRubber);
        CombineStatic(root);
        var brk = Sign(transform, "Break area sign", new Vector3(9.0f, 3.3f, -11.9f), Vector3.forward, new Vector2(1.7f, 0.42f), Navy, P.Gold, 0.07f);
        SignText(brk, "BREAK AREA", -0.01f, 0.18f, P.Gold);
    }

    void BuildLockersAndShelving()
    {
        var root = NewRoot("Lockers and shelving");
        var locker = P.Mat(new Color(0.0f, 0.21f, 0.58f), 0.3f, 0.5f);
        for (int i = 0; i < 6; i++)
        {
            float x = 0.9f + i * 0.52f;
            Bx(root, "Locker", new Vector3(x, 0.95f, -11.65f), new Vector3(0.5f, 1.9f, 0.5f), locker);
            for (int s = 0; s < 3; s++) Bx(root, "Locker vent", new Vector3(x, 1.65f + s * 0.07f, -11.395f), new Vector3(0.3f, 0.02f, 0.01f), mRubber);
            Bx(root, "Locker handle", new Vector3(x + 0.15f, 1.15f, -11.38f), new Vector3(0.03f, 0.12f, 0.03f), steel);
            Bx(root, "Locker tag", new Vector3(x, 1.85f, -11.395f), new Vector3(0.1f, 0.05f, 0.01f), mLabel);
        }
        Bx(root, "Locker top", new Vector3(2.5f, 1.93f, -11.65f), new Vector3(3.14f, 0.04f, 0.52f), darkSteel);
        // bench in front of the lockers
        Bx(root, "Bench", new Vector3(2.5f, 0.45f, -10.7f), new Vector3(2.2f, 0.05f, 0.3f), mWood);
        foreach (float bx in new[] { 1.6f, 3.4f }) Bx(root, "Bench leg", new Vector3(bx, 0.22f, -10.7f), new Vector3(0.05f, 0.44f, 0.25f), darkSteel);

        // tote shelving along the north wall, full of plastic totes
        var rng = new System.Random(44);
        var frame = P.Mat(new Color(0.1f, 0.2f, 0.45f), 0.4f, 0.5f);
        var tote = new[] { mCrate, mCrate2, P.Mat(new Color(0.7f, 0.15f, 0.1f), 0f, 0.5f), P.Mat(new Color(0.1f, 0.5f, 0.3f), 0f, 0.5f) };
        float z0 = 15.4f;
        for (int bay = 0; bay < 4; bay++)
        {
            float cx = 1.5f + bay * 2.4f;
            foreach (float sx in new[] { -1.15f, 1.15f }) Bx(root, "Shelf upright", new Vector3(cx + sx, 1.1f, z0), new Vector3(0.05f, 2.2f, 0.5f), frame);
            for (int lv = 0; lv < 4; lv++)
            {
                float y = 0.3f + lv * 0.62f;
                Bx(root, "Shelf board", new Vector3(cx, y, z0), new Vector3(2.35f, 0.04f, 0.5f), mGalv);
                int n = 2 + rng.Next(0, 3);
                for (int k = 0; k < n; k++)
                {
                    float w = 0.4f + (float)rng.NextDouble() * 0.2f;
                    float h = 0.18f + (float)rng.NextDouble() * 0.18f;
                    float tx = cx - 0.9f + k * (1.8f / Mathf.Max(1, n - 1)) * 0.95f;
                    Bx(root, "Tote", new Vector3(tx, y + 0.02f + h / 2f, z0), new Vector3(w, h, 0.4f), tote[rng.Next(0, 4)]);
                    Bx(root, "Tote label", new Vector3(tx, y + 0.02f + h / 2f, z0 - 0.202f), new Vector3(0.14f, 0.07f, 0.01f), mLabel);
                }
            }
        }
        CombineStatic(root);
    }

    // ------------------------------------------------------------------------------------------------- notice board

    void BuildNoticeBoard()
    {
        var root = NewRoot("Notice board");
        var cork = P.Mat(new Color(0.62f, 0.45f, 0.27f), 0f, 0.15f, CardTex("wf-cork", 21), 1f);
        float x = -13.9f, z = -9.3f;
        Bx(root, "Frame", new Vector3(x, 1.65f, z), new Vector3(0.04f, 1.14f, 1.64f), darkSteel);
        Bx(root, "Cork", new Vector3(x + 0.02f, 1.65f, z), new Vector3(0.03f, 1.0f, 1.5f), cork);
        var rng = new System.Random(9);
        var paper = new[] { P.Mat(new Color(0.96f, 0.96f, 0.94f), 0f, 0.2f), P.Mat(new Color(1f, 0.93f, 0.55f), 0f, 0.2f), P.Mat(new Color(0.75f, 0.92f, 1f), 0f, 0.2f) };
        for (int i = 0; i < 9; i++)
        {
            float pz = z - 0.6f + (i % 3) * 0.6f + ((float)rng.NextDouble() - 0.5f) * 0.1f;
            float py = 1.95f - (i / 3) * 0.32f + ((float)rng.NextDouble() - 0.5f) * 0.06f;
            var n = Bx(root, "Notice", new Vector3(x + 0.045f, py, pz), new Vector3(0.006f, 0.24f, 0.18f), paper[rng.Next(0, 3)], ((float)rng.NextDouble() - 0.5f) * 14f);
            Bx(root, "Pin", new Vector3(x + 0.05f, py + 0.1f, pz), new Vector3(0.012f, 0.02f, 0.02f), mRed);
        }
        CombineStatic(root);
        var plate = Sign(transform, "Notice board sign", new Vector3(x + 0.04f, 2.45f, z), Vector3.right, new Vector2(1.4f, 0.3f), Navy, P.Gold, 0.08f);
        SignText(plate, "NOTICES", -0.01f, 0.14f, P.Gold);
    }

    // ------------------------------------------------------------------------------------------------- reflection probe and dust

    ReflectionProbe probe;

    void BuildProbeAndDust()
    {
        // realtime reflection probe (kit): the sealed floor and metal pick up the lights and racking. It is rendered
        // again a few frames after the scene is built, so it sees the finished warehouse.
        probe = Fx.ReflectionProbe(new Vector3(0f, 3f, 2f), new Vector3(28f, 7f, 28f), 128);
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        if (!baking) StartCoroutine(RenderProbeSoon());
        // dust motes drifting through the light shafts (kit particles; saved with the bake, played at runtime)
        Fx.Dust(NewRoot("Lighting extras"), new Vector3(0f, 3.2f, 2f), new Vector3(24f, 5.5f, 24f), 240);
    }

    System.Collections.IEnumerator RenderProbeSoon()
    {
        for (int i = 0; i < 4; i++) yield return null;
        if (probe != null) probe.RenderProbe();
    }
}
