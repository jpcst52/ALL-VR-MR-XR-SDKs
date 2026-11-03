using UnityEngine;
using PittBradford.XR;

/// <summary>
/// A primitive-built forklift with a driver. It drives a looping route through the forklift lanes, honks near the
/// crossing, slows and stops when a person is in its path, and holds still when the driver has been signaled.
/// Round 3: a more detailed model (tyres with tread that turn, mast chains and hydraulic ram, overhead guard, lamps,
/// strobe beacon with a light, seat and levers, driver) and the kit's sounds (engine loop pitched by speed, reversing
/// beeper, hydraulic whine, horn, pallet drop).
/// </summary>
public class Forklift : MonoBehaviour
{
    public Vector3[] Route;
    public Vector3 CrossingPoint;
    public float MaxSpeed = 2.2f;
    public float Accel = 1.3f;
    public float Decel = 3.2f;
    public float TurnRate = 80f;
    public bool Frozen;

    /// <summary>Set every frame by the scenario so the forklift can see the pedestrian.</summary>
    public Vector3 PlayerFeet;
    public bool PlayerInLane;

    public float Speed { get; private set; }
    public bool IsStopped => Speed < 0.12f;
    public Vector3 DriverHeadPosition => driverHead.position;

    /// <summary>A person on foot (the co-worker) that the driver also watches for. Optional; set at runtime (never saved).</summary>
    [System.NonSerialized] public CoWorker Walker;

    /// <summary>Steps of a put-away at the racking: stop in the lane, turn to the rack, raise, drive in, set the pallet down, back out, lower, turn back.</summary>
    public enum LoadPhase { None, Approach, TurnIn, Raise, DriveIn, Release, BackOut, Lower, TurnOut }
    public LoadPhase Phase { get; private set; }
    /// <summary>True from the moment the forklift stops at the rack until it has turned back into the lane.</summary>
    public bool Loading => Phase != LoadPhase.None && Phase != LoadPhase.Approach;
    /// <summary>True while the driver is waiting for the walker to get clear before moving.</summary>
    public bool HoldingForWalker { get; private set; }
    public event System.Action LoadingStarted, LoadingFinished;

    const float LiftHeight = 1.5f, Reach = 0.9f, WalkerClear = 3.2f;
    Vector3 loadPoint, loadFace, laneDir, shelfPos;
    bool putAway, honkedWalker;
    float liftY, moved, phaseT;
    Transform lift, cargo;
    LoadPhase lastPhase;

    int target = 1;
    float yieldUntil, brakeUntil, waveUntil, bubbleUntil, nextHorn;
    Transform driverHead, arm;
    Material beaconMat;
    Light beaconLight;
    Transform chainL, chainR;
    Transform[] wheels;
    float[] wheelRadius;
    AudioSource engine, beeper;
    // speech bubble: one rounded panel per message, sized to its text, shown above the cab
    Transform bubbleRoot;
    readonly System.Collections.Generic.Dictionary<string, GameObject> bubbles = new System.Collections.Generic.Dictionary<string, GameObject>();
    string bubbleText = "";
    static readonly Color Amber = new Color(1f, 0.62f, 0.05f);

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.normalized;
    }

    /// <summary>
    /// Builds the forklift (or, in a baked scene, adopts the saved one in its start pose and re-finds its parts: the
    /// lift, cargo, driver head and arm, beacon, chains and wheels). The speech bubbles and sounds are made at runtime
    /// and never baked.
    /// </summary>
    public static Forklift Build(Transform parent, Vector3[] route, Vector3 crossing)
    {
        var go = P.Node("Forklift", parent);
        if (P.IsNew(go)) go.transform.SetPositionAndRotation(route[0], Quaternion.LookRotation(Flat(route[1] - route[0]), Vector3.up));
        var f = P.Ensure<Forklift>(go);
        f.Route = route;
        f.CrossingPoint = crossing;
        f.BuildModel();
        return f;
    }

    /// <summary>Puts the forklift somewhere for a screenshot.</summary>
    public void Place(Vector3 position, Vector3 heading, float speed)
    {
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(Flat(heading), Vector3.up));
        Speed = speed;
    }

    /// <summary>Which route point the forklift drives toward next (used by the scripted self-test).</summary>
    public void SetTarget(int index) { target = Mathf.Clamp(index, 0, Route.Length - 1); }

    /// <summary>Cancels any hold from a signal or an emergency stop (used when a staged screenshot hands back to live play).</summary>
    public void ClearHolds()
    {
        yieldUntil = brakeUntil = waveUntil = 0f;
        HideBubble();
    }

    /// <summary>
    /// Asks the forklift to stop at <paramref name="point"/> (on its lane) the next time it drives up to it, turn to
    /// <paramref name="faceDir"/> and put its pallet away. With a shelf position the pallet is left there; without
    /// one the forks are raised and lowered with the load still on them.
    /// </summary>
    public void RequestLoad(Vector3 point, Vector3 faceDir, Vector3? shelf)
    {
        loadPoint = point;
        loadFace = Flat(faceDir);
        putAway = shelf.HasValue;
        shelfPos = shelf ?? Vector3.zero;
        Phase = LoadPhase.Approach;
    }

    /// <summary>For screenshots: shows the forklift turned toward the rack with the forks raised.</summary>
    public void PoseLoading(Vector3 position, Vector3 faceDir, float lift01)
    {
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(Flat(faceDir), Vector3.up));
        Speed = 0f;
        liftY = LiftHeight * Mathf.Clamp01(lift01);
        ApplyLift();
    }

    void ApplyLift()
    {
        lift.localPosition = new Vector3(0, liftY, 0);
        if (chainL == null) return;
        // the chains run from the sprockets at the top of the mast down to the carriage, so they shorten as it rises
        float bottom = 0.68f + liftY, top = 2.12f;
        float len = Mathf.Max(0.05f, top - bottom);
        foreach (var c in new[] { chainL, chainR })
        {
            c.localPosition = new Vector3(c.localPosition.x, bottom + len / 2f, c.localPosition.z);
            c.localScale = new Vector3(0.02f, len, 0.012f);
        }
    }

    bool WalkerNear => Walker != null && Walker.gameObject.activeInHierarchy && FlatDist(Walker.transform.position, transform.position) < WalkerClear;

    static float FlatDist(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>One frame of the put-away. Each step waits until it is finished before the next one starts.</summary>
    void UpdateLoading(float dt)
    {
        if (Walker != null && Walker.gameObject.activeInHierarchy && !honkedWalker && FlatDist(Walker.transform.position, transform.position) < 4.5f)
        {
            honkedWalker = true;
            Honk();
            Say("HONK - person close by", 1.6f);
        }
        HoldingForWalker = false;
        switch (Phase)
        {
            case LoadPhase.TurnIn:
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(loadFace, Vector3.up), 70f * dt);
                if (Vector3.Angle(transform.forward, loadFace) < 0.5f) Phase = LoadPhase.Raise;
                break;
            case LoadPhase.Raise:
                liftY = Mathf.MoveTowards(liftY, LiftHeight, 0.6f * dt);
                if (liftY >= LiftHeight) { Phase = LoadPhase.DriveIn; moved = 0f; }
                break;
            case LoadPhase.DriveIn:
                moved += 0.45f * dt;
                transform.position += transform.forward * (0.45f * dt);
                if (moved >= Reach) { Phase = LoadPhase.Release; phaseT = 0f; }
                break;
            case LoadPhase.Release:
                phaseT += dt;
                if (phaseT >= 0.8f)
                {
                    if (putAway)
                    {
                        cargo.SetParent(transform.parent, true);
                        cargo.SetPositionAndRotation(shelfPos, Quaternion.LookRotation(loadFace, Vector3.up));
                        Sfx.Play("pallet_drop", shelfPos, 0.9f);
                    }
                    Phase = LoadPhase.BackOut;
                    moved = 0f;
                }
                break;
            case LoadPhase.BackOut:
                if (WalkerNear) { HoldingForWalker = true; break; }
                moved += 0.45f * dt;
                transform.position -= transform.forward * (0.45f * dt);
                if (moved >= Reach) Phase = LoadPhase.Lower;
                break;
            case LoadPhase.Lower:
                liftY = Mathf.MoveTowards(liftY, 0f, 0.7f * dt);
                if (liftY <= 0f) Phase = LoadPhase.TurnOut;
                break;
            case LoadPhase.TurnOut:
                if (WalkerNear) { HoldingForWalker = true; break; }
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(laneDir, Vector3.up), 70f * dt);
                if (Vector3.Angle(transform.forward, laneDir) < 0.5f)
                {
                    transform.rotation = Quaternion.LookRotation(laneDir, Vector3.up);
                    Phase = LoadPhase.None;
                    Say("Moving off", 2f);
                    LoadingFinished?.Invoke();
                }
                break;
        }
        ApplyLift();
        // hydraulic whine at the start of each lift and each lowering
        if (Phase != lastPhase)
        {
            if (Phase == LoadPhase.Raise || Phase == LoadPhase.Lower) Sfx.Play("hydraulic_whine", transform.position + transform.forward * 1.2f, 0.7f, 0.02f);
            lastPhase = Phase;
        }
        if (Phase == LoadPhase.None) return;
        if (HoldingForWalker) { if (bubbleText != "Holding: person behind me") Say("Holding: person behind me", 30f); }
        else if (bubbleText.Length == 0 || bubbleText == "Holding: person behind me") Say("Putting a pallet away", 30f);
    }

    // ------------------------------------------------------------------------------------------------- model

    GameObject Bx(Transform p, string n, Vector3 pos, Vector3 size, Material m, float pitch = 0f, float yaw = 0f, float roll = 0f)
    {
        var go = P.Box(p, n, pos, size, m, false);
        if ((pitch != 0f || yaw != 0f || roll != 0f) && P.IsNew(go)) go.transform.localRotation = Quaternion.Euler(pitch, yaw, roll);
        return go;
    }

    void BuildModel()
    {
        var t = transform;
        var orange = P.Painted(new Color(0.96f, 0.58f, 0.04f));
        var darkOrange = P.Painted(new Color(0.62f, 0.34f, 0.04f));
        var dark = P.DarkSteel();
        var steel = P.Steel();
        var black = P.Mat(new Color(0.06f, 0.06f, 0.07f), 0f, 0.3f);
        var rubber = P.Mat(new Color(0.04f, 0.04f, 0.045f), 0f, 0.15f);
        var hubMat = P.Mat(new Color(0.62f, 0.64f, 0.67f), 0.8f, 0.5f);
        var seatMat = P.Mat(new Color(0.1f, 0.11f, 0.14f), 0f, 0.12f);
        var skin = P.Mat(new Color(0.85f, 0.65f, 0.5f), 0f, 0.4f);
        var hatMat = P.Mat(new Color(1f, 0.82f, 0.05f), 0.1f, 0.7f);
        var vestMat = P.Mat(new Color(0.78f, 1f, 0.05f), 0f, 0.4f);
        var jeans = P.Mat(new Color(0.15f, 0.2f, 0.32f), 0f, 0.2f);
        var boots = P.Mat(new Color(0.2f, 0.13f, 0.07f), 0f, 0.3f);
        var wood = P.Mat(new Color(0.6f, 0.45f, 0.26f), 0f, 0.15f);
        var cardboard = P.Mat(new Color(0.7f, 0.55f, 0.35f), 0f, 0.15f);
        var cardboard2 = P.Mat(new Color(0.62f, 0.48f, 0.3f), 0f, 0.15f);
        var wrap = P.Transparent(new Color(0.85f, 0.93f, 1f, 0.13f));
        var hazardStripe = P.Hazard();
        var white = P.Mat(new Color(0.93f, 0.93f, 0.93f), 0f, 0.4f);
        var lampWhite = P.Glow(new Color(1f, 0.97f, 0.88f), 2.0f);
        var lampRed = P.Glow(new Color(1f, 0.08f, 0.04f), 1.8f);
        var screenMat = P.Glow(new Color(0.2f, 1f, 0.5f), 1.2f);

        // body: chassis, hazard-striped skirts, fenders, floor plate and mat, cowl and dash
        P.Box(t, "Chassis", new Vector3(0, 0.5f, -0.2f), new Vector3(1.1f, 0.5f, 2.0f), orange, false);
        foreach (float sx in new[] { -0.555f, 0.555f })
        {
            Bx(t, "Skirt stripe", new Vector3(sx, 0.32f, -0.2f), new Vector3(0.012f, 0.1f, 1.7f), hazardStripe);
            Bx(t, "Fender", new Vector3(sx * 0.98f, 0.64f, 0.55f), new Vector3(0.3f, 0.05f, 0.75f), orange);
            Bx(t, "Fender lip", new Vector3(sx * 1.05f, 0.58f, 0.55f), new Vector3(0.04f, 0.14f, 0.75f), orange);
            Bx(t, "Step", new Vector3(sx * 1.03f, 0.42f, 0.0f), new Vector3(0.12f, 0.03f, 0.35f), dark);
        }
        Bx(t, "Floor mat", new Vector3(0, 0.765f, 0.05f), new Vector3(0.8f, 0.015f, 0.6f), rubber);
        Bx(t, "Cowl", new Vector3(0, 0.86f, 0.5f), new Vector3(0.9f, 0.22f, 0.4f), orange);
        P.Box(t, "Dash", new Vector3(0, 1.0f, 0.15f), new Vector3(0.8f, 0.3f, 0.1f), black, false);
        Bx(t, "Dash display", new Vector3(0.18f, 1.06f, 0.098f), new Vector3(0.16f, 0.08f, 0.01f), screenMat);
        Bx(t, "Dash lamp", new Vector3(-0.1f, 1.06f, 0.098f), new Vector3(0.03f, 0.03f, 0.01f), P.Glow(new Color(1f, 0.55f, 0.05f), 1.4f));
        Bx(t, "Dash lamp", new Vector3(-0.2f, 1.06f, 0.098f), new Vector3(0.03f, 0.03f, 0.01f), P.Glow(new Color(0.2f, 1f, 0.3f), 1.4f));
        P.Cyl(t, "Steering column", new Vector3(0, 1.1f, 0.06f), 0.025f, 0.34f, new Vector3(0, 0.55f, -0.8f), black, false);
        P.Cyl(t, "Steering wheel", new Vector3(0, 1.2f, 0.0f), 0.13f, 0.02f, new Vector3(0, 1f, -0.6f), black, false);
        P.Cyl(t, "Steering knob", new Vector3(0.09f, 1.22f, -0.02f), 0.02f, 0.06f, Vector3.up, steel, false);
        // control levers beside the seat
        foreach (float lx in new[] { 0.24f, 0.32f })
        {
            P.Cyl(t, "Lever", new Vector3(lx, 1.0f, 0.0f), 0.01f, 0.3f, new Vector3(0, 1f, -0.25f), dark, false);
            P.Ball(t, "Lever knob", new Vector3(lx, 1.15f, -0.04f), 0.022f, lx < 0.3f ? P.Mat(new Color(0.9f, 0.1f, 0.1f)) : black, false);
        }

        // seat: cushion, back, headrest, armrests
        P.Box(t, "Seat", new Vector3(0, 0.82f, -0.35f), new Vector3(0.45f, 0.1f, 0.45f), seatMat, false);
        P.Box(t, "Seat back", new Vector3(0, 1.1f, -0.62f), new Vector3(0.45f, 0.5f, 0.08f), seatMat, false);
        Bx(t, "Headrest", new Vector3(0, 1.42f, -0.63f), new Vector3(0.28f, 0.14f, 0.07f), seatMat);
        foreach (float ax in new[] { -0.26f, 0.26f }) Bx(t, "Armrest", new Vector3(ax, 0.98f, -0.4f), new Vector3(0.05f, 0.04f, 0.3f), black);
        Bx(t, "Seat base", new Vector3(0, 0.77f, -0.35f), new Vector3(0.3f, 0.06f, 0.3f), dark);

        // rear: counterweight, bumper stripe, tail lights, tow pin, battery hood
        P.Box(t, "Counterweight", new Vector3(0, 0.7f, -1.35f), new Vector3(1.1f, 0.7f, 0.6f), darkOrange, false);
        Bx(t, "Rear stripe", new Vector3(0, 0.42f, -1.655f), new Vector3(1.1f, 0.12f, 0.012f), hazardStripe);
        foreach (float lx in new[] { -0.45f, 0.45f })
        {
            Bx(t, "Tail light", new Vector3(lx, 0.95f, -1.655f), new Vector3(0.12f, 0.07f, 0.02f), lampRed);
            Bx(t, "Counterweight corner", new Vector3(lx * 1.22f, 0.7f, -1.35f), new Vector3(0.05f, 0.72f, 0.62f), darkOrange);
        }
        P.Cyl(t, "Tow pin", new Vector3(0, 0.55f, -1.69f), 0.025f, 0.12f, Vector3.forward, steel, false);
        Bx(t, "Battery hood", new Vector3(0, 0.93f, -0.98f), new Vector3(0.98f, 0.28f, 0.5f), orange);
        Bx(t, "Hood seam", new Vector3(0, 1.075f, -0.98f), new Vector3(0.98f, 0.012f, 0.02f), dark);
        Bx(t, "Data plate", new Vector3(0.556f, 0.55f, -0.2f), new Vector3(0.01f, 0.1f, 0.18f), P.Mat(new Color(0.85f, 0.86f, 0.88f), 0.8f, 0.6f));
        Bx(t, "Capacity plate", new Vector3(-0.556f, 0.55f, -0.2f), new Vector3(0.01f, 0.1f, 0.18f), P.Mat(new Color(0.85f, 0.86f, 0.88f), 0.8f, 0.6f));

        // overhead guard: four posts, a frame, slats, a lamp at each front corner
        foreach (float px in new[] { -0.5f, 0.5f })
        foreach (float pz in new[] { 0.2f, -0.65f })
            P.Box(t, "Guard post", new Vector3(px, 1.55f, pz), new Vector3(0.05f, 1.1f, 0.05f), black, false);
        for (int i = 0; i < 5; i++)
            P.Box(t, "Guard slat", new Vector3(0, 2.1f, 0.2f - i * 0.2125f), new Vector3(1.15f, 0.04f, 0.1f), black, false);
        foreach (float gx in new[] { -0.5f, 0.5f })
        {
            Bx(t, "Guard rail", new Vector3(gx, 2.1f, -0.22f), new Vector3(0.05f, 0.05f, 0.95f), black);
            Bx(t, "Work lamp", new Vector3(gx, 2.0f, 0.24f), new Vector3(0.12f, 0.08f, 0.06f), lampWhite);
            Bx(t, "Work lamp housing", new Vector3(gx, 2.0f, 0.205f), new Vector3(0.14f, 0.1f, 0.03f), black);
        }
        P.Cyl(t, "Grab handle", new Vector3(0.56f, 1.2f, 0.18f), 0.014f, 0.5f, Vector3.up, P.Mat(new Color(1f, 0.8f, 0.05f), 0f, 0.5f), false);
        Bx(t, "Extinguisher", new Vector3(-0.55f, 1.15f, -0.5f), new Vector3(0.09f, 0.3f, 0.09f), P.Mat(new Color(0.78f, 0.07f, 0.05f), 0.1f, 0.55f));

        // beacon with its own light: it flashes by changing its material, so take the renderer's own copy
        P.Cyl(t, "Beacon base", new Vector3(0, 2.17f, -0.62f), 0.07f, 0.06f, Vector3.up, black, false);
        var beacon = P.Ball(t, "Beacon", new Vector3(0, 2.27f, -0.62f), 0.09f, P.Glow(Amber, 2f), false);
        beaconMat = beacon.GetComponent<Renderer>().material;
        beaconLight = P.Ensure<Light>(beacon);
        if (P.IsNew(beacon))
        {
            beaconLight.type = LightType.Point;
            beaconLight.color = Amber;
            beaconLight.range = 5f;
            beaconLight.intensity = 0f;
            beaconLight.shadows = LightShadows.None;
        }
        beaconLight.renderMode = LightRenderMode.ForceVertex; // a flashing vertex light: no per-pixel cost

        // mast: outer rails, inner rails, cross members, hydraulic ram, chain sprockets, chains (scaled with the lift)
        foreach (float mx in new[] { -0.35f, 0.35f })
        {
            P.Box(t, "Mast rail", new Vector3(mx, 1.15f, 1.15f), new Vector3(0.08f, 2.1f, 0.1f), dark, false);
            Bx(t, "Mast flange", new Vector3(mx * 1.12f, 1.15f, 1.15f), new Vector3(0.03f, 2.1f, 0.16f), dark);
            Bx(t, "Inner rail", new Vector3(mx * 0.62f, 1.1f, 1.2f), new Vector3(0.06f, 2.0f, 0.06f), steel);
            P.Cyl(t, "Tilt cylinder", new Vector3(mx * 0.85f, 0.95f, 0.95f), 0.025f, 0.8f, new Vector3(0, 0.6f, 0.8f), steel, false);
        }
        P.Box(t, "Mast cross low", new Vector3(0, 0.15f, 1.15f), new Vector3(0.8f, 0.08f, 0.1f), dark, false);
        P.Box(t, "Mast cross high", new Vector3(0, 2.15f, 1.15f), new Vector3(0.8f, 0.08f, 0.1f), dark, false);
        P.Cyl(t, "Lift ram", new Vector3(0, 1.1f, 1.08f), 0.05f, 2.0f, Vector3.up, steel, false);
        foreach (float cx in new[] { -0.15f, 0.15f })
            P.Cyl(t, "Chain sprocket", new Vector3(cx, 2.12f, 1.22f), 0.06f, 0.04f, Vector3.right, steel, false);
        chainL = Bx(t, "Lift chain", new Vector3(-0.15f, 1.4f, 1.22f), new Vector3(0.02f, 1.0f, 0.012f), black).transform;
        chainR = Bx(t, "Lift chain", new Vector3(0.15f, 1.4f, 1.22f), new Vector3(0.02f, 1.0f, 0.012f), black).transform;

        // carriage, backrest, forks and a wrapped pallet load: they move together on the mast
        lift = P.Node("Lift", t).transform;
        P.Box(lift, "Carriage", new Vector3(0, 0.4f, 1.27f), new Vector3(1.0f, 0.5f, 0.05f), dark, false);
        Bx(lift, "Carriage bar", new Vector3(0, 0.64f, 1.3f), new Vector3(1.0f, 0.06f, 0.08f), dark);
        Bx(lift, "Carriage bar", new Vector3(0, 0.18f, 1.3f), new Vector3(1.0f, 0.06f, 0.08f), dark);
        for (int i = 0; i < 5; i++) Bx(lift, "Backrest bar", new Vector3(-0.4f + i * 0.2f, 0.95f, 1.3f), new Vector3(0.03f, 0.62f, 0.03f), dark);
        Bx(lift, "Backrest rail", new Vector3(0, 0.78f, 1.3f), new Vector3(0.88f, 0.03f, 0.03f), dark);
        Bx(lift, "Backrest rail", new Vector3(0, 1.22f, 1.3f), new Vector3(0.88f, 0.03f, 0.03f), dark);
        foreach (float fx in new[] { -0.3f, 0.3f })
        {
            P.Box(lift, "Fork blade", new Vector3(fx, 0.16f, 1.85f), new Vector3(0.12f, 0.06f, 1.1f), dark, false);
            P.Box(lift, "Fork heel", new Vector3(fx, 0.35f, 1.31f), new Vector3(0.12f, 0.4f, 0.06f), dark, false);
            Bx(lift, "Fork tip", new Vector3(fx, 0.145f, 2.42f), new Vector3(0.12f, 0.035f, 0.18f), dark, 6f);
        }
        cargo = P.Node("Cargo", lift).transform;
        if (P.IsNew(cargo)) cargo.localPosition = new Vector3(0, 0.19f, 1.85f); // origin at the underside of the pallet
        P.Box(cargo, "Pallet", new Vector3(0, 0.06f, 0), new Vector3(1.0f, 0.12f, 1.1f), wood, false);
        foreach (float sx in new[] { -0.45f, 0f, 0.45f }) Bx(cargo, "Pallet gap", new Vector3(sx, 0.0f, 0f), new Vector3(0.12f, 0.04f, 1.12f), black);
        P.Box(cargo, "Load", new Vector3(0, 0.49f, 0), new Vector3(0.9f, 0.75f, 0.9f), cardboard, false);
        Bx(cargo, "Load top", new Vector3(0, 0.88f, 0), new Vector3(0.8f, 0.04f, 0.8f), cardboard2);
        Bx(cargo, "Load strap", new Vector3(0, 0.49f, 0), new Vector3(0.92f, 0.77f, 0.04f), black);
        Bx(cargo, "Load strap", new Vector3(0, 0.49f, 0.25f), new Vector3(0.92f, 0.77f, 0.04f), black);
        Bx(cargo, "Load wrap", new Vector3(0, 0.49f, 0), new Vector3(0.94f, 0.79f, 0.94f), wrap);
        Bx(cargo, "Load label", new Vector3(0.455f, 0.55f, 0.1f), new Vector3(0.01f, 0.14f, 0.26f), white);

        // wheels: tyre, hub and eight tread blocks; the roots turn with the speed
        wheels = new Transform[4];
        wheelRadius = new[] { 0.3f, 0.3f, 0.25f, 0.25f };
        var wpos = new[] { new Vector3(-0.55f, 0.3f, 0.55f), new Vector3(0.55f, 0.3f, 0.55f), new Vector3(-0.5f, 0.25f, -1.0f), new Vector3(0.5f, 0.25f, -1.0f) };
        for (int w = 0; w < 4; w++)
        {
            float r = wheelRadius[w];
            var root = P.Node("Wheel", t).transform;
            if (P.IsNew(root)) root.localPosition = wpos[w];
            wheels[w] = root;
            P.Cyl(root, "Tyre", Vector3.zero, r, 0.22f, Vector3.right, rubber, false);
            float outX = wpos[w].x < 0f ? -0.115f : 0.115f;
            P.Cyl(root, "Hub", new Vector3(outX, 0f, 0f), r * 0.5f, 0.02f, Vector3.right, hubMat, false);
            P.Cyl(root, "Hub cap", new Vector3(outX * 1.12f, 0f, 0f), r * 0.18f, 0.02f, Vector3.right, steel, false);
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f;
                var tread = Bx(root, "Tread", new Vector3(0, r * Mathf.Cos(a * Mathf.Deg2Rad), r * Mathf.Sin(a * Mathf.Deg2Rad)), new Vector3(0.23f, 0.03f, 0.08f), rubber, a);
            }
        }

        // driver: torso, legs, boots, head with glasses and hard hat, a static left arm and a waving right arm
        P.Prim(PrimitiveType.Capsule, t, "Driver torso", new Vector3(0, 1.15f, -0.35f), new Vector3(0.3f, 0.28f, 0.3f), vestMat, false);
        foreach (float dx in new[] { -0.1f, 0.1f })
        {
            Bx(t, "Driver thigh", new Vector3(dx, 0.9f, -0.15f), new Vector3(0.12f, 0.12f, 0.42f), jeans);
            Bx(t, "Driver shin", new Vector3(dx, 0.84f, 0.1f), new Vector3(0.11f, 0.11f, 0.3f), jeans, -35f);
            Bx(t, "Driver boot", new Vector3(dx, 0.79f, 0.27f), new Vector3(0.12f, 0.08f, 0.26f), boots);
        }
        var armL = P.Prim(PrimitiveType.Capsule, t, "Driver arm L", new Vector3(-0.2f, 1.2f, -0.15f), new Vector3(0.08f, 0.2f, 0.08f), vestMat, false);
        if (P.IsNew(armL)) armL.transform.localRotation = Quaternion.Euler(65f, 0f, -8f);
        P.Cyl(t, "Driver neck", new Vector3(0, 1.43f, -0.35f), 0.05f, 0.1f, Vector3.up, skin, false);
        var head = P.Ball(t, "Driver head", new Vector3(0, 1.55f, -0.35f), 0.12f, skin, false);
        driverHead = head.transform;
        P.Prim(PrimitiveType.Sphere, t, "Driver hard hat", new Vector3(0, 1.64f, -0.35f), new Vector3(0.28f, 0.16f, 0.3f), hatMat, false);
        Bx(t, "Driver hat peak", new Vector3(0, 1.6f, -0.2f), new Vector3(0.24f, 0.012f, 0.1f), hatMat);
        Bx(t, "Driver glasses", new Vector3(0, 1.57f, -0.232f), new Vector3(0.17f, 0.035f, 0.01f), P.Mat(new Color(0.12f, 0.14f, 0.18f), 0.2f, 0.9f));
        var pivot = P.Node("Driver arm", t).transform;
        if (P.IsNew(pivot)) pivot.localPosition = new Vector3(0.22f, 1.35f, -0.3f);
        P.Prim(PrimitiveType.Capsule, pivot, "Arm", new Vector3(0, -0.2f, 0), new Vector3(0.08f, 0.2f, 0.08f), vestMat, false);
        P.Ball(pivot, "Hand", new Vector3(0, -0.42f, 0), 0.045f, P.Mat(new Color(0.15f, 0.15f, 0.17f), 0f, 0.3f), false);
        arm = pivot;
        arm.localRotation = Quaternion.Euler(0, 0, 10f);

        // speech bubble above the cab: runtime only (plain GameObject, never baked); its bubbles are made as needed
        bubbleRoot = new GameObject("Speech bubble").transform;
        bubbleRoot.SetParent(t, false);
        bubbleRoot.localPosition = new Vector3(0, 2.95f, -0.3f);
        bubbleRoot.gameObject.AddComponent<WfBillboard>();

        // sounds (kit): engine loop pitched by speed, reversing beeper. The kit keeps these loops out of the bake.
        engine = Sfx.Loop("forklift_engine_loop", t, 0.4f, 2f, 22f);
        beeper = Sfx.Loop("forklift_beep_reverse", t, 0f, 2f, 20f);
        ApplyLift();
    }

    // ------------------------------------------------------------------------------------------------- behavior

    /// <summary>The driver has seen the pedestrian: wave, stop, and hold for a while.</summary>
    public void Acknowledge(float seconds)
    {
        yieldUntil = Time.time + seconds;
        waveUntil = Time.time + 4f;
        Say("Driver: I see you. Holding.", seconds);
    }

    /// <summary>Slam the brakes and honk (used after a near miss).</summary>
    public void EmergencyStop(float seconds)
    {
        brakeUntil = Time.time + seconds;
        Speed = Mathf.Min(Speed, 0.8f);
        Honk();
        Say("HORN! Stopped.", seconds);
    }

    public void Honk()
    {
        Sfx.Play("forklift_horn", transform.position + Vector3.up * 1.2f, 1f, 0.01f);
    }

    void Say(string s, float seconds)
    {
        if (!bubbles.TryGetValue(s, out var go))
        {
            const float h = 0.13f;
            go = new GameObject("Bubble: " + s);
            go.transform.SetParent(bubbleRoot, false);
            var size = new Vector2(s.Length * h * 0.62f + h * 1.4f, h * 2.3f);
            Ui.Panel(go.transform, "Panel", Vector3.zero, size, new Color(0.97f, 0.97f, 0.95f), Amber, 0.1f);
            P.Text(go.transform, s, new Vector3(0, -h * 0.08f, -0.015f), h, new Color(0.05f, 0.08f, 0.16f), TextAnchor.MiddleCenter, true);
            bubbles[s] = go;
        }
        foreach (var b in bubbles.Values) b.SetActive(b == go);
        bubbleText = s;
        bubbleUntil = Time.time + seconds;
    }

    void HideBubble()
    {
        foreach (var b in bubbles.Values) b.SetActive(false);
        bubbleText = "";
    }

    bool PlayerInPath(out float along)
    {
        along = 0f;
        if (!PlayerInLane) return false;
        return PointInPath(PlayerFeet, out along);
    }

    bool PointInPath(Vector3 feet, out float along)
    {
        Vector3 rel = feet - transform.position;
        rel.y = 0f;
        along = Vector3.Dot(rel, transform.forward);
        float lateral = Mathf.Abs(Vector3.Dot(rel, transform.right));
        return along > 0.2f && along < 10f && lateral < 1.5f;
    }

    void SpinWheels(float dt)
    {
        if (wheels == null) return;
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i] != null) wheels[i].Rotate(Vector3.right, Speed * dt / wheelRadius[i] * Mathf.Rad2Deg, Space.Self);
    }

    /// <summary>Engine pitch and volume follow the speed; the beeper sounds only while backing out of the rack.</summary>
    void UpdateSound()
    {
        if (engine != null)
        {
            engine.pitch = 0.78f + Mathf.Clamp01(Speed / MaxSpeed) * 0.5f + (Phase == LoadPhase.Raise || Phase == LoadPhase.Lower ? 0.12f : 0f);
            engine.volume = Frozen ? 0.25f : 0.3f + Mathf.Clamp01(Speed / MaxSpeed) * 0.2f;
        }
        if (beeper != null) beeper.volume = Phase == LoadPhase.BackOut && !HoldingForWalker ? 0.55f : 0f;
    }

    void Update()
    {
        // beacon flash, wave, and bubble run even when frozen for screenshots
        bool on = Mathf.Repeat(Time.time, 0.7f) < 0.35f;
        beaconMat.SetColor("_EmissionColor", Amber * (on ? 2.5f : 0.15f));
        if (beaconLight != null) beaconLight.intensity = on ? 1.6f : 0f;
        if (Time.time < waveUntil) arm.localRotation = Quaternion.Euler(0, 0, 150f + 25f * Mathf.Sin(Time.time * 10f));
        else arm.localRotation = Quaternion.Euler(0, 0, 10f);
        if (bubbleText.Length > 0 && Time.time > bubbleUntil) HideBubble();
        UpdateSound();
        if (Frozen) return;

        float dt = Time.deltaTime;
        if (Loading)
        {
            Speed = 0f;
            UpdateLoading(dt);
            return;
        }
        Vector3 pos = transform.position;
        Vector3 to = Route[target] - pos;
        to.y = 0f;
        if (to.magnitude < 2.0f)
        {
            target = (target + 1) % Route.Length;
            to = Route[target] - pos;
            to.y = 0f;
        }
        Vector3 desired = to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;

        float targetSpeed = Vector3.Angle(transform.forward, desired) > 25f ? 1.2f : MaxSpeed;
        float along;
        if (Time.time < yieldUntil || Time.time < brakeUntil) targetSpeed = 0f;
        else if (PlayerInPath(out along)) targetSpeed = along < 3.2f ? 0f : Mathf.Min(targetSpeed, (along - 3.2f) * 0.7f);
        if (Walker != null && Walker.gameObject.activeInHierarchy && PointInPath(Walker.transform.position, out along))
            targetSpeed = along < 3.2f ? 0f : Mathf.Min(targetSpeed, (along - 3.2f) * 0.7f);

        // a requested put-away: slow down and stop at the load point on the lane
        if (Phase == LoadPhase.Approach)
        {
            Vector3 rel0 = loadPoint - pos;
            rel0.y = 0f;
            float ahead0 = Vector3.Dot(rel0, transform.forward);
            float side0 = Mathf.Abs(Vector3.Dot(rel0, transform.right));
            if (side0 < 0.8f && ahead0 > -0.2f && ahead0 < 4f && Vector3.Angle(transform.forward, desired) < 10f)
            {
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Max(0.25f, ahead0 * 0.9f));
                if (ahead0 < 0.12f)
                {
                    Speed = 0f;
                    laneDir = Flat(transform.forward);
                    honkedWalker = false;
                    Phase = LoadPhase.TurnIn;
                    Say("Putting a pallet away", 30f);
                    LoadingStarted?.Invoke();
                    return;
                }
            }
        }

        float rate = targetSpeed < Speed ? Decel : Accel;
        Speed = Mathf.MoveTowards(Speed, targetSpeed, rate * dt);

        if (Speed > 0.05f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desired, Vector3.up), TurnRate * dt);
            transform.position += transform.forward * (Speed * dt);
            SpinWheels(dt);
        }

        // sound the horn when approaching the pedestrian crossing
        Vector3 rel = CrossingPoint - transform.position;
        rel.y = 0f;
        float ahead = Vector3.Dot(rel, transform.forward);
        if (Speed > 0.5f && ahead > 0.5f && ahead < 9f && Mathf.Abs(Vector3.Dot(rel, transform.right)) < 1.5f && Time.time > nextHorn)
        {
            nextHorn = Time.time + 10f;
            Honk();
            Say("HONK", 1.2f);
        }
    }
}
