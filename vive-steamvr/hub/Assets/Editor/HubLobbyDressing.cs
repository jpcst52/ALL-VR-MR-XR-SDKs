using System.IO;
using UnityEditor;
using UnityEngine;
using PittBradford.XR;

/// <summary>
/// Dresses the lobby (round 2): tiled floor, taller reception and lounge walls with signs, a two-part scenario gallery with framed
/// pictures, a CIST corner and a "how it works" board, a rug and seating clusters, plants, a hanging pergola with lights, banners, and
/// ten Rocketbox people in small groups. Everything sits outside the portal row (z above 4.4), the pads, the central path (|x| below
/// 5) and the welcome board. Called from <see cref="HubSceneDressing.DressLobby"/> so a rebuild reproduces it.
/// </summary>
public static class HubLobbyDressing
{
    const string CM1 = "Professions/Construction_Male_01", CM2 = "Professions/Construction_Male_02", CF1 = "Professions/Construction_Female_01";
    const string BM1 = "Professions/Business_Male_01", BF1 = "Professions/Business_Female_01", SEC = "Professions/Security_Male_01";
    const string DEL = "Professions/Delivery_Male_01", MED = "Professions/Medical_Female_01", AM5 = "Adults/Male_Adult_05", AF3 = "Adults/Female_Adult_03";

    static readonly Color Royal = new Color(0f, 0.208f, 0.58f), Gold = new Color(1f, 0.722f, 0.11f), Navy = new Color(0f, 0.13f, 0.36f);
    static readonly Color Wall = new Color(0.92f, 0.935f, 0.96f);

    public const string GalleryDir = "Assets/Scripts/Lobby/Resources/Gallery";

    // ------------------------------------------------------------------------------------------ small helpers

    static float Yaw(float x, float z, float tx, float tz) => Mathf.Atan2(tx - x, tz - z) * Mathf.Rad2Deg;

    /// <summary>A box in the parent's local space (BatchingStatic, no collider, Standard material).</summary>
    static GameObject BoxL(Transform parent, string name, Vector3 localPos, Vector3 size, Color color, float gloss = 0.3f, float metallic = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", metallic);
        go.GetComponent<Renderer>().sharedMaterial = m;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    /// <summary>An empty node at a world point whose local -z axis points along faceDir (so kit panels and text read from that side).</summary>
    internal static Transform Facing(Transform parent, string name, Vector3 pos, Vector3 faceDir)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = Quaternion.LookRotation(-new Vector3(faceDir.x, 0, faceDir.z).normalized, Vector3.up);
        return t;
    }

    /// <summary>A rounded sign panel with a title and an optional second line.</summary>
    static Transform Sign(Transform parent, string name, Vector3 pos, Vector3 faceDir, Vector2 size, string title, string sub, Color fill, float titleH = 0.3f, bool bold = true)
    {
        var s = Facing(parent, name, pos, faceDir);
        Ui.Panel(s, "Panel", Vector3.zero, size, fill, Gold, 0.05f);
        if (string.IsNullOrEmpty(sub))
            P.Text(s, title, new Vector3(0, 0, -0.012f), titleH, Color.white, TextAnchor.MiddleCenter, bold);
        else
        {
            P.Text(s, title, new Vector3(0, size.y * 0.14f, -0.012f), titleH, Color.white, TextAnchor.MiddleCenter, bold);
            P.Text(s, sub, new Vector3(0, -size.y * 0.24f, -0.012f), titleH * 0.5f, P.Sky, TextAnchor.MiddleCenter);
        }
        return s;
    }

    /// <summary>A notice-board panel (title and up to a few short lines, left aligned) facing faceDir; used for bulletin boards and posters in the scenario rooms.</summary>
    internal static Transform Notice(Transform parent, string name, Vector3 pos, Vector3 faceDir, Vector2 size, string title, string[] lines, Color fill, float lineH = 0.075f)
    {
        var s = Facing(parent, name, pos, faceDir);
        Ui.Panel(s, "Panel", Vector3.zero, size, fill, Gold, 0.045f);
        P.Text(s, title, new Vector3(0, size.y * 0.5f - 0.17f, -0.012f), 0.15f, Color.white, TextAnchor.MiddleCenter, true);
        for (int i = 0; i < lines.Length; i++)
            P.Text(s, lines[i], new Vector3(-size.x * 0.5f + 0.14f, size.y * 0.5f - 0.38f - i * lineH * 1.7f, -0.012f), lineH, P.Sky, TextAnchor.MiddleLeft);
        return s;
    }

    static Texture2D Gallery(string name)
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GalleryDir}/{name}.jpg");
        if (t == null) Debug.LogWarning("[lobby] gallery picture missing: " + name);
        return t;
    }

    /// <summary>A framed picture (white rounded frame, unlit picture) with a one-line caption below.</summary>
    static void Picture(Transform wallRoot, string name, Vector3 local, float w, string tex, string caption)
    {
        float h = w * 9f / 16f;
        var frame = Ui.Panel(wallRoot, "Frame " + name, local, new Vector2(w + 0.1f, h + 0.1f), new Color(0.97f, 0.97f, 0.98f, 1f));
        var t = Gallery(tex);
        if (t != null)
        {
            var mat = new Material(Shader.Find("Unlit/Texture")) { mainTexture = t, name = "gallery-" + tex };
            var q = P.Prim(PrimitiveType.Quad, frame, "Picture " + name, new Vector3(0, 0, -0.01f), new Vector3(w, h, 1f), mat, false);
            var r = q.GetComponent<MeshRenderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
        P.Text(wallRoot, caption, local + new Vector3(0, -h / 2 - 0.17f, -0.012f), 0.085f, new Color(0.1f, 0.14f, 0.25f), TextAnchor.MiddleCenter, true);
    }

    /// <summary>A freestanding gallery wall (4.4 m by 3.4 m) at a floor point, its front facing faceDir; four pictures in two rows.</summary>
    static void GalleryWall(Transform parent, string name, Vector3 pos, Vector3 faceDir, string header, string[] tex, string[] captions)
    {
        var g = Facing(parent, name, pos, faceDir);
        BoxL(g, "Slab", new Vector3(0, 1.7f, 0.06f), new Vector3(4.4f, 3.4f, 0.12f), Wall, 0.15f);
        BoxL(g, "Header bar", new Vector3(0, 3.15f, -0.004f), new Vector3(4.4f, 0.5f, 0.02f), Royal);
        BoxL(g, "Header gold", new Vector3(0, 2.89f, -0.004f), new Vector3(4.4f, 0.035f, 0.022f), Gold);
        BoxL(g, "Foot L", new Vector3(-1.7f, 0.025f, 0.06f), new Vector3(0.12f, 0.05f, 1.0f), new Color(0.16f, 0.17f, 0.2f), 0.5f, 0.6f);
        BoxL(g, "Foot R", new Vector3(1.7f, 0.025f, 0.06f), new Vector3(0.12f, 0.05f, 1.0f), new Color(0.16f, 0.17f, 0.2f), 0.5f, 0.6f);
        P.Text(g, header, new Vector3(0, 3.15f, -0.02f), 0.2f, Color.white, TextAnchor.MiddleCenter, true);
        // 1.6 m pictures: the caption under the top row now sits clear of the frames of the row below (it used to touch their top edge)
        float[] xs = { -1.05f, 1.05f, -1.05f, 1.05f }, ys = { 2.2f, 2.2f, 0.88f, 0.88f };
        for (int i = 0; i < 4 && i < tex.Length; i++) Picture(g, "pic" + (i + 1), new Vector3(xs[i], ys[i], -0.02f), 1.6f, tex[i], captions[i]);
        // a track light bar above (no real light: pictures are unlit, and every extra pixel light costs a pass per object)
        var bar = Facing(g, "Light bar", pos + Vector3.up * 3.75f + faceDir.normalized * 0.9f, faceDir);
        BoxL(bar, "Bar", Vector3.zero, new Vector3(2.4f, 0.06f, 0.1f), new Color(0.16f, 0.17f, 0.2f), 0.5f, 0.6f);
    }

    /// <summary>A pergola: four slat beams on two thin posts each end, over a seating area (centre c, size in x and z).</summary>
    static void Pergola(Transform parent, string name, Vector3 c, Vector2 size)
    {
        var g = HubSceneDressing.Group(parent, name);
        var steel = new Color(0.16f, 0.17f, 0.2f);
        float y = 3.3f;
        foreach (var sx in new[] { -1, 1 })
        foreach (var sz in new[] { -1, 1 })
            BoxL(g, "Post", new Vector3(c.x + sx * size.x / 2, y / 2, c.z + sz * size.y / 2), new Vector3(0.1f, y, 0.1f), steel, 0.5f, 0.6f);
        foreach (var sz in new[] { -1, 1 })
            BoxL(g, "Beam", new Vector3(c.x, y, c.z + sz * size.y / 2), new Vector3(size.x + 0.3f, 0.14f, 0.14f), steel, 0.5f, 0.6f);
        for (int i = 0; i <= 4; i++)
            BoxL(g, "Slat", new Vector3(c.x - size.x / 2 + i * size.x / 4f, y + 0.12f, c.z), new Vector3(0.08f, 0.1f, size.y + 0.3f), new Color(0.55f, 0.4f, 0.26f), 0.25f);
        // two pendant lamps (emissive discs, no real light: the ambient and sun do the work)
        for (int i = 0; i < 2; i++)
        {
            float px = c.x + (i - 0.5f) * size.x * 0.5f;
            BoxL(g, "Cable", new Vector3(px, y - 0.2f, c.z), new Vector3(0.015f, 0.4f, 0.015f), steel);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Pendant"; lamp.transform.SetParent(g, false);
            lamp.transform.localPosition = new Vector3(px, y - 0.45f, c.z); lamp.transform.localScale = new Vector3(0.4f, 0.22f, 0.4f);
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.GetComponent<Renderer>().sharedMaterial = P.Glow(new Color(1f, 0.9f, 0.7f), 1.2f);
            GameObjectUtility.SetStaticEditorFlags(lamp, StaticEditorFlags.BatchingStatic);
        }
    }

    static void Banner(Transform parent, Vector3 pos, Vector3 faceDir, string line1, string line2)
    {
        var s = Facing(parent, "Banner", pos, faceDir);
        BoxL(s, "Rod", new Vector3(0, 1.58f, 0.02f), new Vector3(1.1f, 0.05f, 0.05f), new Color(0.16f, 0.17f, 0.2f), 0.5f, 0.6f);
        BoxL(s, "Cloth", new Vector3(0, 0f, 0.01f), new Vector3(0.95f, 3.0f, 0.02f), Royal, 0.1f);
        BoxL(s, "Gold edge", new Vector3(0, -1.46f, -0.004f), new Vector3(0.95f, 0.08f, 0.02f), Gold, 0.1f);
        BoxL(s, "Gold edge top", new Vector3(0, 1.46f, -0.004f), new Vector3(0.95f, 0.08f, 0.02f), Gold, 0.1f);
        P.Text(s, line1, new Vector3(0, 0.65f, -0.02f), 0.17f, Color.white, TextAnchor.MiddleCenter, true);
        P.Text(s, line2, new Vector3(0, 0.3f, -0.02f), 0.075f, P.Sky, TextAnchor.MiddleCenter);
        P.Text(s, "XR", new Vector3(0, -0.8f, -0.02f), 0.55f, Gold, TextAnchor.MiddleCenter, true);
    }

    static Texture2D FloorTexture()
    {
        const string path = "Assets/Baked/lobby-floor-tiles2.png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        Directory.CreateDirectory("Assets/Baked");
        const int n = 256; // 2 x 2 m of tiles, 128 px per metre
        var tex = new Texture2D(n, n, TextureFormat.RGB24, true);
        float[] tone = { 0.70f, 0.66f, 0.68f, 0.72f };
        var rnd = new System.Random(11);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int tx = x / 128, ty = y / 128;
            float v = tone[ty * 2 + tx] + (float)(rnd.NextDouble() - 0.5) * 0.025f;
            int lx = x % 128, ly = y % 128;
            bool grout = lx < 2 || ly < 2 || lx > 125 || ly > 125;
            if (grout) v = 0.60f;
            tex.SetPixel(x, y, new Color(v * 0.97f, v * 0.99f, v * 1.03f));
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------------------------------ build

    public static void Build(Transform dressing)
    {
        var root = HubSceneDressing.Group(dressing, "Reception and lounge");
        var steel = new Color(0.16f, 0.17f, 0.2f);

        // --- floor: light porcelain tiles over the concrete slab (1 m tiles), then a gold-edged royal rug in the lounge and a mat at reception
        var floor = HubSceneDressing.Group(root, "Floor");
        var tiles = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tiles.name = "Floor tiles"; tiles.transform.SetParent(floor, false);
        tiles.transform.position = new Vector3(0, 0.003f, 0); tiles.transform.rotation = Quaternion.Euler(90, 0, 0);
        tiles.transform.localScale = new Vector3(23.7f, 17.7f, 1f);
        Object.DestroyImmediate(tiles.GetComponent<Collider>());
        var tm = new Material(Shader.Find("Standard")) { mainTexture = FloorTexture(), color = Color.white };
        tm.mainTextureScale = new Vector2(23.7f / 2f, 17.7f / 2f); tm.SetFloat("_Glossiness", 0.45f);
        tiles.GetComponent<Renderer>().sharedMaterial = tm;
        GameObjectUtility.SetStaticEditorFlags(tiles, StaticEditorFlags.BatchingStatic);

        BoxL(floor, "Lounge rug border", new Vector3(-9.4f, 0.012f, -5.6f), new Vector3(4.6f, 0.012f, 3.6f), Gold, 0.1f);
        BoxL(floor, "Lounge rug", new Vector3(-9.4f, 0.016f, -5.6f), new Vector3(4.4f, 0.012f, 3.4f), Royal, 0.05f);
        BoxL(floor, "Lounge rug inner", new Vector3(-9.4f, 0.021f, -5.6f), new Vector3(3.2f, 0.012f, 2.2f), new Color(0.05f, 0.27f, 0.66f), 0.05f);
        BoxL(floor, "Reception mat border", new Vector3(8.0f, 0.012f, -5.5f), new Vector3(2.2f, 0.012f, 3.0f), Gold, 0.1f);
        BoxL(floor, "Reception mat", new Vector3(8.0f, 0.016f, -5.5f), new Vector3(2.0f, 0.012f, 2.8f), Royal, 0.05f);
        // welcome medallion near the entrance: two rings and a disc, crossed by a gold path to the portals
        var med = new[] { (2.2f, Gold), (2.05f, Royal), (1.4f, Gold), (1.32f, new Color(0.93f, 0.95f, 0.98f)) };
        for (int i = 0; i < med.Length; i++)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            d.name = "Medallion ring " + i; d.transform.SetParent(floor, false);
            d.transform.position = new Vector3(0, 0.006f + i * 0.0015f, -3.2f); d.transform.localScale = new Vector3(med[i].Item1 * 2f, 0.003f, med[i].Item1 * 2f);
            Object.DestroyImmediate(d.GetComponent<Collider>());
            var dm = new Material(Shader.Find("Standard")) { color = med[i].Item2 }; dm.SetFloat("_Glossiness", 0.4f);
            d.GetComponent<Renderer>().sharedMaterial = dm;
            GameObjectUtility.SetStaticEditorFlags(d, StaticEditorFlags.BatchingStatic);
        }
        var mt = Facing(floor, "Medallion text", new Vector3(0, 0.014f, -3.2f), Vector3.up);
        mt.rotation = Quaternion.Euler(90, 0, 0);
        P.Text(mt, "PITT-BRADFORD", new Vector3(0, 0.22f, 0), 0.2f, Royal, TextAnchor.MiddleCenter, true);
        P.Text(mt, "CIST  XR TRAINING LAB", new Vector3(0, -0.18f, 0), 0.13f, Navy, TextAnchor.MiddleCenter, true);
        BoxL(floor, "Path over medallion", new Vector3(0, 0.0145f, -3.2f), new Vector3(0.18f, 0.003f, 2.6f), Gold, 0.3f);

        // --- reception (east): taller backdrop wall with a sign, map and clock; desk with a branded front panel; waiting chairs; banner stand
        var rec = HubSceneDressing.Group(root, "Reception desk");
        HubSceneDressing.PaintedWall(rec, "Backdrop wall", new Vector3(11.7f, 2.5f, -5.8f), new Vector3(0.2f, 5.0f, 6.8f), Wall, true);
        Sign(rec, "Reception sign", new Vector3(11.52f, 4.35f, -5.8f), Vector3.left, new Vector2(3.6f, 0.9f), "RECEPTION", "Welcome - check in here", Royal, 0.34f);
        HubSceneDressing.PutAt(rec, "World map", new Vector3(11.5f, 2.25f, -7.55f), -90f);
        HubSceneDressing.PutAt(rec, "Clock", new Vector3(11.58f, 2.95f, -4.0f), -90f);
        HubSceneDressing.Put(rec, "Table 1", 9.2f, -5.8f, 90f);
        BoxL(rec, "Desk front", new Vector3(8.78f, 0.55f, -5.8f), new Vector3(0.05f, 0.9f, 2.0f), Royal, 0.3f);
        BoxL(rec, "Desk front gold", new Vector3(8.755f, 0.97f, -5.8f), new Vector3(0.03f, 0.04f, 2.0f), Gold, 0.3f);
        HubSceneDressing.Put(rec, "Computer 1", 9.15f, -6.5f, 90f, 0.82f);
        HubSceneDressing.Put(rec, "Laptop", 9.2f, -5.2f, 90f, 0.82f);
        HubSceneDressing.Put(rec, "Lamp 1", 9.0f, -4.5f, 90f, 0.82f);
        HubSceneDressing.Put(rec, "Phone 1", 9.3f, -5.8f, 90f, 0.82f);
        HubSceneDressing.Put(rec, "Papers", 8.9f, -6.1f, 80f, 0.82f);
        HubSceneDressing.Put(rec, "Notebook", 8.8f, -5.5f, 100f, 0.82f);
        HubSceneDressing.Put(rec, "Chair 2", 10.3f, -6.6f, 90f);
        HubSceneDressing.Put(rec, "File Cabinet", 11.2f, -3.2f, -90f);
        HubSceneDressing.Put(rec, "Plant 2", 9.1f, -3.4f);
        HubSceneDressing.Put(rec, "Plant 4", 11.2f, -8.3f);
        HubSceneDressing.Put(rec, "Bin", 10.3f, -4.2f);
        // waiting area: two chairs and a side table by the reception mat's south end, brochures on the table
        HubSceneDressing.Put(rec, "Chair", 7.0f, -8.0f, 180f);
        HubSceneDressing.Put(rec, "Chair", 8.1f, -8.2f, 180f);
        HubSceneDressing.Put(rec, "Table 2", 9.7f, -8.1f, 0f);
        HubSceneDressing.Put(rec, "Files 2", 9.7f, -8.1f, 15f, 0.77f, 0.8f);
        HubSceneDressing.Put(rec, "books 2", 9.5f, -8.1f, 0f, 0.77f);
        // roll-up banner stand and a framed welcome sign on a stand beside the desk
        var stand = Facing(rec, "Roll-up banner", new Vector3(5.8f, 0f, -3.6f), new Vector3(-0.4f, 0f, 1f));
        BoxL(stand, "Base", new Vector3(0, 0.03f, 0.05f), new Vector3(0.9f, 0.06f, 0.3f), steel, 0.5f, 0.6f);
        BoxL(stand, "Cloth", new Vector3(0, 1.0f, 0.05f), new Vector3(0.85f, 1.9f, 0.02f), Royal, 0.1f);
        BoxL(stand, "Gold", new Vector3(0, 0.12f, 0.04f), new Vector3(0.85f, 0.06f, 0.022f), Gold, 0.1f);
        P.Text(stand, "Try a", new Vector3(0, 1.55f, -0.012f), 0.11f, P.Sky, TextAnchor.MiddleCenter);
        P.Text(stand, "scenario", new Vector3(0, 1.36f, -0.012f), 0.13f, Color.white, TextAnchor.MiddleCenter, true);
        P.Text(stand, "Step on a pad", new Vector3(0, 0.95f, -0.012f), 0.075f, Gold, TextAnchor.MiddleCenter);
        P.Text(stand, "ahead of you", new Vector3(0, 0.8f, -0.012f), 0.075f, Gold, TextAnchor.MiddleCenter);
        Pergola(rec, "Reception pergola", new Vector3(8.8f, 0f, -5.8f), new Vector2(4.2f, 5.6f));

        // --- lounge (west): taller backdrop wall with a sign and a slide on the projector screen, meeting table, coffee corner, rug
        var lounge = HubSceneDressing.Group(root, "Lounge");
        HubSceneDressing.PaintedWall(lounge, "Backdrop wall", new Vector3(-11.7f, 2.5f, -5.8f), new Vector3(0.2f, 5.0f, 6.8f), Wall, true);
        Sign(lounge, "Lounge sign", new Vector3(-11.52f, 4.35f, -5.8f), Vector3.right, new Vector2(3.6f, 0.9f), "LOUNGE", "Meeting area and coffee", Royal, 0.34f);
        var screen = HubSceneDressing.PutAt(lounge, "Proyector screen", new Vector3(-11.45f, 2.2f, -5.8f), 90f);
        var slide = Facing(lounge, "Slide", new Vector3(-11.40f, 2.15f, -5.8f), Vector3.right);
        var slideTex = Gallery("lobby-slide");
        if (slideTex != null)
        {
            var sm = new Material(Shader.Find("Unlit/Texture")) { mainTexture = slideTex, name = "slide" };
            P.Prim(PrimitiveType.Quad, slide, "Slide picture", Vector3.zero, new Vector3(2.5f, 2.5f * 9f / 16f, 1f), sm, false);
        }
        HubSceneDressing.Put(lounge, "Table 2", -9.4f, -5.8f, 90f);
        HubSceneDressing.Put(lounge, "Chair", -10.35f, -6.5f, 90f);
        HubSceneDressing.Put(lounge, "Chair", -10.35f, -5.0f, 90f);
        HubSceneDressing.Put(lounge, "Chair", -8.45f, -6.5f, -90f);
        HubSceneDressing.Put(lounge, "Chair", -8.45f, -5.0f, -90f);
        HubSceneDressing.Put(lounge, "Notebook", -9.4f, -5.6f, 20f, 0.77f);
        HubSceneDressing.Put(lounge, "Bottle", -9.2f, -6.3f, 0f, 0.77f);
        HubSceneDressing.Put(lounge, "Papers", -9.6f, -6.2f, 70f, 0.77f);
        HubSceneDressing.Put(lounge, "Table 1", -9.0f, -8.2f, 0f);
        HubSceneDressing.Put(lounge, "machine coffee", -9.7f, -8.2f, 180f, 0.82f);
        HubSceneDressing.Put(lounge, "paper cup", -9.1f, -8.2f, 0f, 0.82f);
        HubSceneDressing.Put(lounge, "Water cooler", -11.2f, -8.2f, 90f);
        HubSceneDressing.Put(lounge, "Plant 4", -11.3f, -3.0f);
        HubSceneDressing.Put(lounge, "Plant 2", -7.9f, -3.1f);
        HubSceneDressing.Put(lounge, "Bin", -7.4f, -8.3f);
        // soft seats along the south parapet (west part): a bench-style row facing north
        HubSceneDressing.Put(lounge, "Chair", -6.6f, -8.2f, 0f);
        HubSceneDressing.Put(lounge, "Chair", -5.6f, -8.2f, 0f);
        HubSceneDressing.Put(lounge, "Table 2", -6.1f, -8.2f, 90f, 0f, 0.5f);
        Pergola(lounge, "Lounge pergola", new Vector3(-9.2f, 0f, -5.8f), new Vector2(4.4f, 5.6f));

        // --- scenario gallery: two angled display walls facing the entrance, four pictures each
        var gal = HubSceneDressing.Group(root, "Scenario gallery");
        GalleryWall(gal, "Gallery west", new Vector3(-8.4f, 0f, 0.9f), new Vector3(0.86f, 0f, -0.5f),
            "SCENARIO GALLERY  |  INDUSTRIAL", new[] { "hydro-1", "hydro-2", "refinery-1", "refinery-2" },
            new[] { "Hydrostatic Test Bay", "Pump panel and gauge", "Refinery Rounds", "Muster point and wind" });
        GalleryWall(gal, "Gallery east", new Vector3(8.4f, 0f, 0.9f), new Vector3(-0.86f, 0f, -0.5f),
            "SCENARIO GALLERY  |  WAREHOUSE AND LAB", new[] { "warehouse-1", "warehouse-2", "resistor-1", "resistor-2" },
            new[] { "Warehouse & Forklift", "Aisle and loading dock", "Resistor Bench", "Wall cards and parts bins" });

        // --- CIST corner (north-west): wall with the department name, a display table and a few plants
        var cist = HubSceneDressing.Group(root, "CIST corner");
        HubSceneDressing.PaintedWall(cist, "CIST wall", new Vector3(-11.7f, 2.0f, 6.2f), new Vector3(0.2f, 4.0f, 4.4f), Royal, false);
        BoxL(cist, "CIST wall stripe", new Vector3(-11.58f, 0.5f, 6.2f), new Vector3(0.02f, 0.06f, 4.4f), Gold);
        var cs = Facing(cist, "CIST sign", new Vector3(-11.55f, 2.4f, 6.2f), Vector3.right);
        P.Text(cs, "CIST", new Vector3(0, 0.55f, -0.01f), 0.9f, Color.white, TextAnchor.MiddleCenter, true);
        P.Text(cs, "Computer Information Systems & Technology", new Vector3(0, -0.15f, -0.01f), 0.14f, P.Sky, TextAnchor.MiddleCenter, true);
        P.Text(cs, "University of Pittsburgh at Bradford", new Vector3(0, -0.5f, -0.01f), 0.11f, Gold, TextAnchor.MiddleCenter);
        P.Text(cs, "Student and faculty XR training projects", new Vector3(0, -0.85f, -0.01f), 0.09f, Color.white, TextAnchor.MiddleCenter);
        HubSceneDressing.Put(cist, "Table 2", -10.5f, 6.2f, 90f);
        HubSceneDressing.Put(cist, "Laptop", -10.5f, 5.7f, 90f, 0.77f);
        HubSceneDressing.Put(cist, "Papers", -10.4f, 6.4f, 80f, 0.77f);
        HubSceneDressing.Put(cist, "Files 3", -10.5f, 6.9f, 100f, 0.77f);
        HubSceneDressing.Put(cist, "Plant 4", -11.1f, 8.2f);
        HubSceneDressing.Put(cist, "Plant 2", -11.1f, 4.1f);

        // --- how-it-works board (north-east), mirroring the CIST corner
        var info = HubSceneDressing.Group(root, "How it works");
        HubSceneDressing.PaintedWall(info, "Board wall", new Vector3(11.7f, 2.0f, 6.2f), new Vector3(0.2f, 4.0f, 4.4f), Wall, false);
        BoxL(info, "Board wall stripe", new Vector3(11.58f, 0.5f, 6.2f), new Vector3(0.02f, 0.06f, 4.4f), Gold);
        var ib = Facing(info, "Board", new Vector3(11.58f, 2.25f, 6.2f), Vector3.left);
        Ui.Panel(ib, "Panel", Vector3.zero, new Vector2(3.6f, 2.4f), Navy, Gold, 0.05f);
        P.Text(ib, "How it works", new Vector3(0, 0.88f, -0.012f), 0.26f, Color.white, TextAnchor.MiddleCenter, true);
        P.Text(ib, "1  Walk to a pad (or press 1 to 4 on a desktop).", new Vector3(-1.62f, 0.4f, -0.012f), 0.11f, P.Sky, TextAnchor.MiddleLeft);
        P.Text(ib, "2  Wait a moment: the scenario loads.", new Vector3(-1.62f, 0.12f, -0.012f), 0.11f, P.Sky, TextAnchor.MiddleLeft);
        P.Text(ib, "3  Follow the task board and the why-panels.", new Vector3(-1.62f, -0.16f, -0.012f), 0.11f, P.Sky, TextAnchor.MiddleLeft);
        P.Text(ib, "4  Point at the Lobby panel on your left hand to return.", new Vector3(-1.62f, -0.44f, -0.012f), 0.11f, P.Sky, TextAnchor.MiddleLeft);
        P.Text(ib, "General safety awareness, not a procedure or certification.", new Vector3(0, -0.93f, -0.012f), 0.085f, Gold, TextAnchor.MiddleCenter);
        HubSceneDressing.Put(info, "Plant 4", 11.1f, 8.2f);
        HubSceneDressing.Put(info, "Plant 2", 11.1f, 4.1f);
        HubSceneDressing.Put(info, "File Cabinet", 11.2f, 3.0f, -90f);

        // --- banners on the side beams (hang inward), plants between and beside the portals, planters along the north parapet
        var deco = HubSceneDressing.Group(root, "Banners and plants");
        Banner(deco, new Vector3(-11.8f, 5.9f, -1.0f), Vector3.right, "PITT", "CIST XR Training Lab");
        Banner(deco, new Vector3(11.8f, 5.9f, -1.0f), Vector3.left, "PITT", "CIST XR Training Lab");
        Banner(deco, new Vector3(-11.8f, 5.9f, 3.2f), Vector3.right, "SAFETY", "Practice before the job");
        Banner(deco, new Vector3(11.8f, 5.9f, 3.2f), Vector3.left, "SAFETY", "Practice before the job");
        foreach (var x in new[] { -4.4f, 0f, 4.4f }) HubSceneDressing.Put(deco, "Plant 4", x, 7.9f);
        HubSceneDressing.Put(deco, "Plant 2", -9.4f, 7.9f); HubSceneDressing.Put(deco, "Plant 2", 9.4f, 7.9f);
        HubSceneDressing.Put(deco, "Plant 4", -9.9f, 2.6f); HubSceneDressing.Put(deco, "Plant 4", 9.9f, 2.6f);
        HubSceneDressing.Put(deco, "Plant 2", -3.8f, -8.3f); HubSceneDressing.Put(deco, "Plant 2", 3.8f, -8.3f);
        HubSceneDressing.Put(deco, "Plant 4", -11.3f, 0.2f); HubSceneDressing.Put(deco, "Plant 4", 11.3f, 0.2f);

        // --- two small seating clusters on either side of the entrance path (coffee table, two chairs, a plant)
        var seats = HubSceneDressing.Group(root, "Seating clusters");
        foreach (var sx in new[] { -1f, 1f })
        {
            float cx = sx * 5.3f, cz = -5.6f;
            HubSceneDressing.Put(seats, "Table 2", cx, cz, 90f, 0f, 0.55f);
            HubSceneDressing.Put(seats, "Chair", cx, cz + 1.0f, 180f);
            HubSceneDressing.Put(seats, "Chair", cx, cz - 1.0f, 0f);
            HubSceneDressing.Put(seats, "Plant 2", cx + sx * 1.1f, cz + 0.2f);
            HubSceneDressing.Put(seats, "Bottle", cx, cz + 0.2f, 0f, 0.55f * 0.77f);
        }

        // --- people (ten), in small groups; all stand clear of the pads and the path
        var people = HubSceneDressing.Group(root, "People");
        HubSceneDressing.Person(people, BF1, 10.25f, -5.2f, -90f, "Documents");            // receptionist behind the desk
        HubSceneDressing.Person(people, MED, 7.7f, -5.2f, 90f, "Wait");                     // visitor at the desk
        HubSceneDressing.Person(people, DEL, 7.6f, -6.5f, 90f, "Idle");                     // delivery driver waiting behind her
        HubSceneDressing.Person(people, AM5, -7.7f, -7.3f, 90f, "Talk");                    // coffee chat
        HubSceneDressing.Person(people, AF3, -6.7f, -7.3f, -90f, "Talk");
        HubSceneDressing.Person(people, BM1, 3.0f, -3.3f, Yaw(3.0f, -3.3f, 0f, -6.5f), "Wave"); // faculty greeting the arrival
        HubSceneDressing.Person(people, SEC, -3.6f, -7.6f, 0f, "Idle");                    // security guard by the entrance
        HubSceneDressing.Person(people, CM1, 6.0f, 2.3f, Yaw(6.0f, 2.3f, 8.0f, 2.0f), "Look"); // visitors at the east gallery wall
        HubSceneDressing.Person(people, CF1, 6.9f, 1.0f, Yaw(6.9f, 1.0f, 8.1f, 1.3f), "Note");
        HubSceneDressing.Person(people, BF1, -6.2f, 2.0f, Yaw(-6.2f, 2.0f, -8.0f, 1.8f), "Look");  // one at the west gallery wall
        HubPerfPass.Apply(root);
    }

    // ------------------------------------------------------------------------------------------ gallery pictures

    /// <summary>
    /// Renders a gallery picture (960 x 540 JPG) of the open scene into the lobby's Resources/Gallery folder and sets the importer
    /// so Unity keeps the size. Spec as in HubReview.Shoot, one view.
    /// </summary>
    public static string SnapGallery(string name, float px, float py, float pz, float lx, float ly, float lz, float fov = 65f)
    {
        Directory.CreateDirectory(GalleryDir);
        string tmp = Path.Combine(Path.GetTempPath(), "gal-" + name + ".png");
        HubShots.Render(tmp, px, py, pz, lx, ly, lz, fov, 960, 540);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(tmp));
        string path = $"{GalleryDir}/{name}.jpg";
        File.WriteAllBytes(path, tex.EncodeToJPG(88));
        Object.DestroyImmediate(tex);
        File.Delete(tmp);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.npotScale = TextureImporterNPOTScale.None; ti.maxTextureSize = 1024; ti.mipmapEnabled = false;
        ti.SaveAndReimport();
        return path;
    }
}
