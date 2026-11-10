using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using PittBradford.XR;

/// <summary>
/// The hub lobby: a hall with one portal per training scenario. Walk onto a portal's pad and stay for a moment to load
/// that scenario (desktop mode: number keys 1-4 also work). Built from primitives so it runs without the asset packs;
/// the editor builder (HubLobbyBuilder) adds the warehouse shell and props when the packs are present.
/// </summary>
public class LobbyHub : MonoBehaviour, IShotScript
{
    public const string LobbyScene = "Lobby";

    public class Entry
    {
        public string Title, Blurb, Scene;
        /// <summary>Thumbnail image in Resources/Thumbnails (made by hub/tools/prepare_lobby_images.py).</summary>
        public string Thumb;
        public Color Color;
    }

    public static readonly Entry[] Entries =
    {
        new Entry { Title = "Hydrostatic Test Bay", Blurb = "Pressure-test a heat exchanger\nfollowing the checklist", Scene = "HydrostaticTest_Hub", Thumb = "hydrostatic-test", Color = new Color32(0x00, 0x35, 0x94, 255) },
        new Entry { Title = "Refinery Rounds", Blurb = "Battery-limits walk-round\nwith live weather", Scene = "RefineryRounds_Hub", Thumb = "refinery-rounds", Color = new Color32(0xC9, 0x6A, 0x00, 255) },
        new Entry { Title = "Warehouse & Forklift", Blurb = "Walk the floor safely\naround forklifts", Scene = "WarehouseForklift_Hub", Thumb = "warehouse-forklift", Color = new Color32(0x1F, 0x7A, 0x4D, 255) },
        new Entry { Title = "Resistor Bench", Blurb = "Choose the right resistor\nfor the circuit", Scene = "ResistorBench_Hub", Thumb = "resistor-bench", Color = new Color32(0x7A, 0x2E, 0x8F, 255) },
    };

    const float HallWidth = 24f, HallDepth = 18f, ParapetHeight = 1.05f, FrameHeight = 7.6f;
    const float PortalZ = 7.2f, PortalSpacing = 4.4f, TriggerRadius = 1.3f, DwellSeconds = 1.4f;

    /// <summary>True when the editor builder supplies the building, so the primitive walls are skipped.</summary>
    public bool ExternalShell;

    XRRig rig;
    readonly List<PortalState> portals = new List<PortalState>();
    bool loading;

    class PortalState
    {
        public Entry Entry;
        public Transform Root;
        public TextMesh Status;
        public Material Gate;
        public Pressable Select;
        public float Dwell;
    }

    void Start()
    {
        Build();
        rig = XRRig.Create(new Vector3(0f, 0f, -6.5f), 0f);
        rig.Camera.backgroundColor = P.MediumBlue;
        rig.Camera.clearFlags = CameraClearFlags.SolidColor;
        rig.Area = new Bounds(Vector3.zero, new Vector3(HallWidth - 1.5f, 1, HallDepth - 1.5f));
        // The portals are up to 14 m away from where the visitor starts: let the pointing ray reach across the hall
        if (rig.Left != null) rig.Left.RayLength = 20f;
        if (rig.Right != null) rig.Right.RayLength = 20f;
        // Soft sky-lit hall: a gradient (brighter from above, cooler floor bounce) instead of one flat grey
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.66f, 0.74f, 0.86f);
        RenderSettings.ambientEquatorColor = new Color(0.56f, 0.59f, 0.65f);
        RenderSettings.ambientGroundColor = new Color(0.36f, 0.37f, 0.41f);

        // Seasonal sky (SeasonSky, from the kit): show it through the open roof and name the season on the welcome board
        sky = FindFirstObjectByType<SeasonSky>();
        if (sky != null && sky.Active)
        {
            rig.Camera.clearFlags = CameraClearFlags.Skybox;
            sky.SeasonChanged += ShowSeason;
            ShowSeason(sky.Current);
        }
        else if (seasonChip != null) seasonChip.gameObject.SetActive(false);
    }

    SeasonSky sky;
    Transform seasonChip;
    TextMesh seasonText, seasonNote;

    void ShowSeason(SeasonSky.Season s)
    {
        if (seasonText == null || s == null) return;
        seasonText.text = s.label;
        // Honest labelling: the pictures are stand-ins unless the manifest marks them as taken in Bradford
        seasonNote.text = s.isBradford ? "Sky: Bradford, PA" : "Sky: stand-in photo";
    }

    void OnDestroy()
    {
        if (sky != null) sky.SeasonChanged -= ShowSeason;
    }

    // ------------------------------------------------------------------ build

    void Build()
    {
        var root = P.Node("Lobby", transform).transform;

        var floor = P.Box(root, "Floor", new Vector3(0, -0.05f, 0), new Vector3(HallWidth, 0.1f, HallDepth), P.Concrete());
        if (ExternalShell) floor.GetComponent<Renderer>().enabled = false; // keep the collider, show the building's floor
        // Stripe guiding the visitor towards the portals
        P.Box(root, "Guide stripe", new Vector3(0, 0.005f, -1.2f), new Vector3(0.18f, 0.01f, 12f), P.Mat(P.Gold, 0f, 0.3f), false);

        if (!ExternalShell)
        {
            // An open roof terrace instead of a closed hall, so the seasonal sky is visible all around:
            // a waist-high parapet with a gold rail, slim columns and an open ring beam overhead (no ceiling).
            var wall = P.Mat(new Color(0.86f, 0.88f, 0.92f), 0f, 0.2f);
            var rail = P.Mat(P.Gold, 0.3f, 0.5f);
            var steel = P.Mat(new Color(0.16f, 0.17f, 0.2f), 0.7f, 0.5f);
            P.Box(root, "Parapet N", new Vector3(0, ParapetHeight / 2, HallDepth / 2), new Vector3(HallWidth, ParapetHeight, 0.25f), wall);
            P.Box(root, "Parapet S", new Vector3(0, ParapetHeight / 2, -HallDepth / 2), new Vector3(HallWidth, ParapetHeight, 0.25f), wall);
            P.Box(root, "Parapet E", new Vector3(HallWidth / 2, ParapetHeight / 2, 0), new Vector3(0.25f, ParapetHeight, HallDepth), wall);
            P.Box(root, "Parapet W", new Vector3(-HallWidth / 2, ParapetHeight / 2, 0), new Vector3(0.25f, ParapetHeight, HallDepth), wall);
            P.Box(root, "Rail N", new Vector3(0, ParapetHeight + 0.03f, HallDepth / 2), new Vector3(HallWidth + 0.1f, 0.06f, 0.32f), rail, false);
            P.Box(root, "Rail S", new Vector3(0, ParapetHeight + 0.03f, -HallDepth / 2), new Vector3(HallWidth + 0.1f, 0.06f, 0.32f), rail, false);
            P.Box(root, "Rail E", new Vector3(HallWidth / 2, ParapetHeight + 0.03f, 0), new Vector3(0.32f, 0.06f, HallDepth + 0.1f), rail, false);
            P.Box(root, "Rail W", new Vector3(-HallWidth / 2, ParapetHeight + 0.03f, 0), new Vector3(0.32f, 0.06f, HallDepth + 0.1f), rail, false);
            foreach (var x in new[] { -HallWidth / 2, HallWidth / 2 })
            foreach (var z in new[] { -HallDepth / 2, HallDepth / 2 })
                P.Box(root, "Column", new Vector3(x, FrameHeight / 2, z), new Vector3(0.35f, FrameHeight, 0.35f), steel);
            P.Box(root, "Beam N", new Vector3(0, FrameHeight, HallDepth / 2), new Vector3(HallWidth, 0.3f, 0.3f), steel, false);
            P.Box(root, "Beam S", new Vector3(0, FrameHeight, -HallDepth / 2), new Vector3(HallWidth, 0.3f, 0.3f), steel, false);
            P.Box(root, "Beam E", new Vector3(HallWidth / 2, FrameHeight, 0), new Vector3(0.3f, 0.3f, HallDepth), steel, false);
            P.Box(root, "Beam W", new Vector3(-HallWidth / 2, FrameHeight, 0), new Vector3(0.3f, 0.3f, HallDepth), steel, false);
            // Posts carrying the welcome board and the university plate (they used to hang on the north wall)
            foreach (var x in new[] { -4.6f, 4.6f })
                P.Box(root, "Board post", new Vector3(x, 2.8f, HallDepth / 2 - 0.2f), new Vector3(0.18f, 5.6f, 0.18f), steel);
            P.Box(root, "Plate post", new Vector3(-10.1f, 1.4f, HallDepth / 2 - 0.2f), new Vector3(0.12f, 2.8f, 0.12f), steel);
        }

        var sunGo = P.Node("Light", root);
        var sun = P.Ensure<Light>(sunGo);
        if (P.IsNew(sunGo))
        {
            sun.type = LightType.Directional;
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55, -20, 0);
        }

        // Welcome board on the north wall
        var boardGo = P.Node("Welcome board", root);
        var board = boardGo.transform;
        if (P.IsNew(boardGo)) board.localPosition = new Vector3(0, 6.6f, HallDepth / 2 - 0.3f);
        Ui.Panel(board, "Panel", new Vector3(0, 0, 0.02f), new Vector2(11.2f, 2.0f), new Color(0f, 0.13f, 0.36f, 0.98f), P.Gold, 0.05f);
        // Every text is limited to its panel by a TextFit (saved on the text): measured with the real font, 0.5 m margin each side of the 11.2 m board
        const float boardText = 10.2f;
        TextFit.Add(P.Text(board, "Pitt-Bradford XR Training Hub", new Vector3(0, 0.38f, -0.06f), 0.62f, Color.white, TextAnchor.MiddleCenter, true, "Title"), boardText, 0f, false);
        TextFit.Add(P.Text(board, "Step on a pad and wait, or point at a doorway and press the trigger.   Desktop: click it, or keys 1-4.", new Vector3(0, -0.22f, -0.06f), 0.24f, P.Sky, TextAnchor.MiddleCenter, false, "Instructions"), boardText, 0f, false);
        TextFit.Add(P.Text(board, "General safety-awareness practice, not a procedure or certification.", new Vector3(0, -0.62f, -0.06f), 0.18f, P.Gold, TextAnchor.MiddleCenter, false, "Disclaimer"), boardText, 0f, false);

        // Season chip: a tab on top of the board's right-hand corner (filled in by ShowSeason; hidden without a season sky)
        seasonChip = Ui.Panel(board, "Season chip", new Vector3(4.3f, 1.3f, 0.0f), new Vector2(2.1f, 0.72f), new Color(0.05f, 0.2f, 0.48f, 1f), P.Gold, 0.09f);
        seasonText = P.Text(seasonChip, "", new Vector3(0, 0.06f, -0.012f), 0.3f, Color.white, TextAnchor.MiddleCenter, true, "Season");
        TextFit.Add(seasonText, 1.8f, 0f, false);
        seasonNote = P.Text(seasonChip, "", new Vector3(0, -0.21f, -0.012f), 0.12f, P.Sky, TextAnchor.MiddleCenter, false, "Season note");
        TextFit.Add(seasonNote, 1.8f, 0f, false);

        for (int i = 0; i < Entries.Length; i++)
            BuildPortal(root, Entries[i], i, new Vector3((i - (Entries.Length - 1) / 2f) * PortalSpacing, 0f, PortalZ));

        // Return hint on the south parapet, readable when you turn round
        var hintGo = P.Node("Hint", root);
        var hint = hintGo.transform;
        if (P.IsNew(hintGo))
        {
            hint.localPosition = new Vector3(0, 0.62f, -HallDepth / 2 + 0.16f);
            hint.localRotation = Quaternion.Euler(0, 180, 0);
        }
        Ui.Panel(hint, "Panel", Vector3.zero, new Vector2(8.6f, 0.5f), new Color(0f, 0.13f, 0.36f, 0.97f), P.Gold, 0.08f);
        TextFit.Add(P.Text(hint, "In any scenario: point at the Lobby panel on your left hand (or hold both grips) to come back here.", new Vector3(0, -0.02f, -0.012f), 0.2f, Color.white, TextAnchor.MiddleCenter, false, "Hint text"), 8.0f, 0f, false);
    }

    void BuildPortal(Transform parent, Entry e, int index, Vector3 pos)
    {
        var pGo = P.Node("Portal " + (index + 1) + ": " + e.Title, parent);
        var p = pGo.transform;
        if (P.IsNew(pGo)) p.localPosition = pos;
        var frameMat = P.Mat(new Color(0.16f, 0.17f, 0.2f), 0.7f, 0.5f);
        var accent = P.Mat(e.Color, 0f, 0.4f);
        const float w = 2.6f, h = 3.2f, t = 0.22f;

        P.Box(p, "Post L", new Vector3(-w / 2, h / 2, 0), new Vector3(t, h, t), frameMat);
        P.Box(p, "Post R", new Vector3(w / 2, h / 2, 0), new Vector3(t, h, t), frameMat);
        P.Box(p, "Lintel", new Vector3(0, h + t / 2, 0), new Vector3(w + t, t, t), frameMat);
        // The glowing doorway; its own material instance so the glow can pulse
        // It has a collider and a Pressable (both saved in the scene), so pointing at the doorway and pressing the trigger (desktop: left click) starts the scenario
        var gate = P.Box(p, "Gate", new Vector3(0, h / 2, 0), new Vector3(w - t, h, 0.04f), P.Transparent(new Color(e.Color.r, e.Color.g, e.Color.b, 0.45f)));
        var select = P.Ensure<Pressable>(gate);
        select.Label = e.Title;
        select.Pressed += hand => Go(e);
        var gateMat = gate.GetComponent<Renderer>().material;
        // A pad on the floor marks where to stand
        P.Cyl(p, "Pad", new Vector3(0, 0.01f, -1.5f), TriggerRadius, 0.02f, Vector3.up, accent, false);
        P.Cyl(p, "Pad inner", new Vector3(0, 0.02f, -1.5f), TriggerRadius * 0.82f, 0.02f, Vector3.up, P.Mat(new Color(0.95f, 0.96f, 0.98f), 0f, 0.3f), false);

        // 4.0 m sign (portals are 4.4 m apart); title and blurb are fitted to 3.6 m so the text keeps 0.2 m inside each edge
        Ui.Panel(p, "Sign", new Vector3(0, h + 0.95f, 0.03f), new Vector2(4.0f, 1.2f), Color.Lerp(e.Color, Color.black, 0.25f), Color.white, 0.04f);
        TextFit.Add(P.Text(p, (index + 1) + "  " + e.Title, new Vector3(0, h + 1.27f, -0.05f), 0.3f, Color.white, TextAnchor.MiddleCenter, true, "Sign title"), 3.6f, 0f, false);   // title and blurb are 0.5 m apart so the two lines of the blurb never touch the title
        TextFit.Add(P.Text(p, e.Blurb, new Vector3(0, h + 0.74f, -0.05f), 0.14f, Color.white, TextAnchor.MiddleCenter, false, "Sign blurb"), 3.6f, 0.4f, false);

        // Thumbnail of the scenario inside the doorway, on a rounded white frame (skipped if the image is missing)
        var thumb = string.IsNullOrEmpty(e.Thumb) ? null : Resources.Load<Texture2D>("Thumbnails/" + e.Thumb);
        if (thumb != null)
        {
            const float tw = 2.0f, th = tw * 9f / 16f;
            var frame = Ui.Panel(p, "Thumbnail", new Vector3(0, 2.1f, -0.07f), new Vector2(tw + 0.12f, th + 0.12f), new Color(0.97f, 0.97f, 0.98f, 1f));
            var thumbMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = thumb, name = "thumb-" + e.Thumb };
            var q = P.Prim(PrimitiveType.Quad, frame, "Picture", new Vector3(0, 0, -0.008f), new Vector3(tw, th, 1f), thumbMat, false);
            if (P.IsNew(q))
            {
                var r = q.GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        var status = P.Text(p, "Step on the pad", new Vector3(0, 0.04f, -1.5f), 0.14f, new Color(0.1f, 0.12f, 0.18f), TextAnchor.MiddleCenter, true, "Pad status");
        TextFit.Add(status, 2.0f, 0f, false);
        status.transform.localRotation = Quaternion.Euler(90, 0, 0); // lies flat, readable from the south

        var lampGo = P.Node("Lamp", p);
        var lamp = P.Ensure<Light>(lampGo);
        if (P.IsNew(lampGo))
        {
            lamp.transform.localPosition = new Vector3(0, 3.2f, -1.2f);
            lamp.type = LightType.Point;
            lamp.range = 6f;
            lamp.intensity = 0.9f;
            lamp.color = Color.Lerp(e.Color, Color.white, 0.65f);
        }

        portals.Add(new PortalState { Entry = e, Root = p, Status = status, Gate = gateMat, Select = select });
    }

    // ------------------------------------------------------------------ play

    void Update()
    {
        if (rig == null || loading) return;
        Vector3 feet = rig.FeetPosition;
        var kb = Keyboard.current;
        for (int i = 0; i < portals.Count; i++)
        {
            var ps = portals[i];
            var pad = ps.Root.position + new Vector3(0, 0, -1.5f);
            bool on = Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(pad.x, pad.z)) < TriggerRadius;
            ps.Dwell = on ? ps.Dwell + Time.deltaTime : Mathf.Max(0f, ps.Dwell - Time.deltaTime * 2f);
            float f = Mathf.Clamp01(ps.Dwell / DwellSeconds);
            bool pointed = ps.Select != null && ((rig.Right != null && rig.Right.Hover == ps.Select) || (rig.Left != null && rig.Left.Hover == ps.Select));
            ps.Status.text = on ? "Starting " + Mathf.RoundToInt(f * 100f) + "%" : pointed ? "Press the trigger" : "Step on the pad";
            var c = ps.Entry.Color;
            ps.Gate.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.35f, 0.9f, f));
            if (f >= 1f) { Go(ps.Entry); return; }
            if (kb != null && i < 9 && kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { Go(ps.Entry); return; }
        }
    }

    void Go(Entry e)
    {
        if (loading) return;
        loading = true;
        Debug.Log("[lobby] loading " + e.Scene);
        rig.Left.Haptic(0.5f, 0.15f);
        rig.Right.Haptic(0.5f, 0.15f);
        SceneManager.LoadScene(e.Scene);
    }

    // ------------------------------------------------------------------ screenshots

    public IEnumerator Shots(ShotApi api)
    {
        yield return api.Shot("lobby-1-entrance", new Vector3(0f, 1.7f, -7.5f), new Vector3(0f, 2.2f, 7f), 70f);
        yield return api.Shot("lobby-2-portals", new Vector3(-3f, 1.7f, 0.5f), new Vector3(0f, 2.4f, 7.2f), 62f);
        yield return api.Shot("lobby-3-pad", new Vector3(-1.6f, 1.7f, 3.0f), new Vector3(-2.2f, 0.4f, 5.7f), 62f);
    }
}
