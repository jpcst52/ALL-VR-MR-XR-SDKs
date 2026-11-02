using UnityEngine;
using PittBradford.XR;

/// <summary>
/// A primitive-built forklift with a driver. It drives a looping route through the forklift lanes, honks near the
/// crossing, slows and stops when a person is in its path, and holds still when the driver has been signaled.
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

    int target = 1;
    float yieldUntil, brakeUntil, waveUntil, bubbleUntil, nextHorn;
    Transform driverHead, arm;
    Material beaconMat;
    AudioSource hornLow, hornHigh;
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
    /// lift, cargo, driver head and arm, beacon and horns). The speech bubbles are made at runtime and never baked.
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
        lift.localPosition = new Vector3(0, liftY, 0);
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
        lift.localPosition = new Vector3(0, liftY, 0);
        if (Phase == LoadPhase.None) return;
        if (HoldingForWalker) { if (bubbleText != "Holding: person behind me") Say("Holding: person behind me", 30f); }
        else if (bubbleText.Length == 0 || bubbleText == "Holding: person behind me") Say("Putting a pallet away", 30f);
    }

    // ------------------------------------------------------------------------------------------------- model

    void BuildModel()
    {
        var t = transform;
        var orange = P.Painted(new Color(0.96f, 0.58f, 0.04f));
        var dark = P.DarkSteel();
        var black = P.Mat(new Color(0.06f, 0.06f, 0.07f), 0f, 0.3f);
        var seatMat = P.Mat(new Color(0.1f, 0.1f, 0.12f), 0f, 0.3f);
        var skin = P.Mat(new Color(0.85f, 0.65f, 0.5f), 0f, 0.4f);
        var hatMat = P.Mat(new Color(1f, 0.82f, 0.05f), 0.1f, 0.7f);
        var vestMat = P.Mat(new Color(0.78f, 1f, 0.05f), 0f, 0.4f);
        var wood = P.Mat(new Color(0.6f, 0.45f, 0.26f), 0f, 0.2f);
        var cardboard = P.Mat(new Color(0.7f, 0.55f, 0.35f), 0f, 0.2f);

        // body
        P.Box(t, "Chassis", new Vector3(0, 0.5f, -0.2f), new Vector3(1.1f, 0.5f, 2.0f), orange, false);
        P.Box(t, "Counterweight", new Vector3(0, 0.7f, -1.35f), new Vector3(1.1f, 0.7f, 0.6f), P.Painted(new Color(0.55f, 0.32f, 0.04f)), false);
        P.Box(t, "Dash", new Vector3(0, 1.0f, 0.15f), new Vector3(0.8f, 0.3f, 0.1f), black, false);
        P.Cyl(t, "Steering wheel", new Vector3(0, 1.2f, 0.0f), 0.13f, 0.02f, new Vector3(0, 1f, -0.6f), black, false);
        P.Box(t, "Seat", new Vector3(0, 0.82f, -0.35f), new Vector3(0.45f, 0.1f, 0.45f), seatMat, false);
        P.Box(t, "Seat back", new Vector3(0, 1.1f, -0.62f), new Vector3(0.45f, 0.5f, 0.08f), seatMat, false);

        // overhead guard
        foreach (float px in new[] { -0.5f, 0.5f })
        foreach (float pz in new[] { 0.2f, -0.65f })
            P.Box(t, "Guard post", new Vector3(px, 1.55f, pz), new Vector3(0.05f, 1.1f, 0.05f), black, false);
        for (int i = 0; i < 5; i++)
            P.Box(t, "Guard slat", new Vector3(0, 2.1f, 0.2f - i * 0.2125f), new Vector3(1.15f, 0.04f, 0.1f), black, false);

        // beacon: it flashes by changing its material, so take the renderer's own copy (a baked one shares an asset)
        P.Cyl(t, "Beacon base", new Vector3(0, 2.17f, -0.62f), 0.07f, 0.06f, Vector3.up, black, false);
        var beacon = P.Ball(t, "Beacon", new Vector3(0, 2.27f, -0.62f), 0.09f, P.Glow(Amber, 2f), false);
        beaconMat = beacon.GetComponent<Renderer>().material;

        // mast, carriage, forks, and a pallet load
        foreach (float mx in new[] { -0.35f, 0.35f })
            P.Box(t, "Mast rail", new Vector3(mx, 1.15f, 1.15f), new Vector3(0.08f, 2.1f, 0.1f), dark, false);
        P.Box(t, "Mast cross low", new Vector3(0, 0.15f, 1.15f), new Vector3(0.8f, 0.08f, 0.1f), dark, false);
        P.Box(t, "Mast cross high", new Vector3(0, 2.15f, 1.15f), new Vector3(0.8f, 0.08f, 0.1f), dark, false);
        // the carriage, forks and load move together on the mast (raised when putting a pallet away)
        lift = P.Node("Lift", t).transform;
        P.Box(lift, "Carriage", new Vector3(0, 0.4f, 1.27f), new Vector3(1.0f, 0.5f, 0.05f), dark, false);
        foreach (float fx in new[] { -0.3f, 0.3f })
        {
            P.Box(lift, "Fork blade", new Vector3(fx, 0.16f, 1.85f), new Vector3(0.12f, 0.06f, 1.1f), dark, false);
            P.Box(lift, "Fork heel", new Vector3(fx, 0.35f, 1.31f), new Vector3(0.12f, 0.4f, 0.06f), dark, false);
        }
        cargo = P.Node("Cargo", lift).transform;
        if (P.IsNew(cargo)) cargo.localPosition = new Vector3(0, 0.19f, 1.85f); // origin at the underside of the pallet
        P.Box(cargo, "Pallet", new Vector3(0, 0.06f, 0), new Vector3(1.0f, 0.12f, 1.1f), wood, false);
        P.Box(cargo, "Load", new Vector3(0, 0.49f, 0), new Vector3(0.9f, 0.75f, 0.9f), cardboard, false);

        // wheels
        foreach (float wx in new[] { -0.55f, 0.55f })
        {
            P.Cyl(t, "Front wheel", new Vector3(wx, 0.3f, 0.55f), 0.3f, 0.22f, Vector3.right, black, false);
            P.Cyl(t, "Rear wheel", new Vector3(wx * 0.9f, 0.25f, -1.0f), 0.25f, 0.22f, Vector3.right, black, false);
        }

        // driver: torso, head with hard hat, and a waving arm
        P.Prim(PrimitiveType.Capsule, t, "Driver torso", new Vector3(0, 1.15f, -0.35f), new Vector3(0.3f, 0.28f, 0.3f), vestMat, false);
        var head = P.Ball(t, "Driver head", new Vector3(0, 1.55f, -0.35f), 0.12f, skin, false);
        driverHead = head.transform;
        P.Prim(PrimitiveType.Sphere, t, "Driver hard hat", new Vector3(0, 1.64f, -0.35f), new Vector3(0.28f, 0.16f, 0.3f), hatMat, false);
        var pivot = P.Node("Driver arm", t).transform;
        if (P.IsNew(pivot)) pivot.localPosition = new Vector3(0.22f, 1.35f, -0.3f);
        P.Prim(PrimitiveType.Capsule, pivot, "Arm", new Vector3(0, -0.2f, 0), new Vector3(0.08f, 0.2f, 0.08f), vestMat, false);
        arm = pivot;
        arm.localRotation = Quaternion.Euler(0, 0, 10f);

        // speech bubble above the cab: runtime only (plain GameObject, never baked); its bubbles are made as needed
        bubbleRoot = new GameObject("Speech bubble").transform;
        bubbleRoot.SetParent(t, false);
        bubbleRoot.localPosition = new Vector3(0, 2.95f, -0.3f);
        bubbleRoot.gameObject.AddComponent<WfBillboard>();

        // horn: two tones sounded together
        hornLow = P.Speaker(t, P.Tone("horn-low", 392f, 0.7f, 0.5f, 1.2f), false, 1f, 1f);
        hornHigh = P.Speaker(t, P.Tone("horn-high", 494f, 0.7f, 0.5f, 1.2f), false, 1f, 1f);
        if (P.IsNew(t))
        {
            hornLow.maxDistance = 35f;
            hornHigh.maxDistance = 35f;
        }
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
        hornLow.Play();
        hornHigh.Play();
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

    void Update()
    {
        // beacon flash, wave, and bubble run even when frozen for screenshots
        bool on = Mathf.Repeat(Time.time, 0.7f) < 0.35f;
        beaconMat.SetColor("_EmissionColor", Amber * (on ? 2.5f : 0.15f));
        if (Time.time < waveUntil) arm.localRotation = Quaternion.Euler(0, 0, 150f + 25f * Mathf.Sin(Time.time * 10f));
        else arm.localRotation = Quaternion.Euler(0, 0, 10f);
        if (bubbleText.Length > 0 && Time.time > bubbleUntil) HideBubble();
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
