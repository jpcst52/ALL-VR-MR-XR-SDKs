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

    int target = 1;
    float yieldUntil, brakeUntil, waveUntil, bubbleUntil, nextHorn;
    Transform driverHead, arm;
    Material beaconMat;
    AudioSource hornLow, hornHigh;
    TextMesh bubble;
    static readonly Color Amber = new Color(1f, 0.62f, 0.05f);

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.normalized;
    }

    public static Forklift Build(Transform parent, Vector3[] route, Vector3 crossing)
    {
        var go = new GameObject("Forklift");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(route[0], Quaternion.LookRotation(Flat(route[1] - route[0]), Vector3.up));
        var f = go.AddComponent<Forklift>();
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

        // beacon
        beaconMat = P.Glow(Amber, 2f);
        P.Cyl(t, "Beacon base", new Vector3(0, 2.17f, -0.62f), 0.07f, 0.06f, Vector3.up, black, false);
        P.Ball(t, "Beacon", new Vector3(0, 2.27f, -0.62f), 0.09f, beaconMat, false);

        // mast, carriage, forks, and a pallet load
        foreach (float mx in new[] { -0.35f, 0.35f })
            P.Box(t, "Mast rail", new Vector3(mx, 1.15f, 1.15f), new Vector3(0.08f, 2.1f, 0.1f), dark, false);
        P.Box(t, "Mast cross low", new Vector3(0, 0.15f, 1.15f), new Vector3(0.8f, 0.08f, 0.1f), dark, false);
        P.Box(t, "Mast cross high", new Vector3(0, 2.15f, 1.15f), new Vector3(0.8f, 0.08f, 0.1f), dark, false);
        P.Box(t, "Carriage", new Vector3(0, 0.4f, 1.27f), new Vector3(1.0f, 0.5f, 0.05f), dark, false);
        foreach (float fx in new[] { -0.3f, 0.3f })
        {
            P.Box(t, "Fork blade", new Vector3(fx, 0.16f, 1.85f), new Vector3(0.12f, 0.06f, 1.1f), dark, false);
            P.Box(t, "Fork heel", new Vector3(fx, 0.35f, 1.31f), new Vector3(0.12f, 0.4f, 0.06f), dark, false);
        }
        P.Box(t, "Pallet", new Vector3(0, 0.25f, 1.85f), new Vector3(1.0f, 0.12f, 1.1f), wood, false);
        P.Box(t, "Load", new Vector3(0, 0.68f, 1.85f), new Vector3(0.9f, 0.75f, 0.9f), cardboard, false);

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
        var pivot = new GameObject("Driver arm").transform;
        pivot.SetParent(t, false);
        pivot.localPosition = new Vector3(0.22f, 1.35f, -0.3f);
        P.Prim(PrimitiveType.Capsule, pivot, "Arm", new Vector3(0, -0.2f, 0), new Vector3(0.08f, 0.2f, 0.08f), vestMat, false);
        arm = pivot;
        arm.localRotation = Quaternion.Euler(0, 0, 10f);

        // speech bubble above the cab
        bubble = P.Text(t, "", new Vector3(0, 2.9f, -0.3f), 0.14f, Color.white, TextAnchor.MiddleCenter, true);
        bubble.gameObject.AddComponent<WfBillboard>();

        // horn: two tones sounded together
        hornLow = P.Speaker(t, P.Tone("horn-low", 392f, 0.7f, 0.5f, 1.2f), false, 1f, 1f);
        hornHigh = P.Speaker(t, P.Tone("horn-high", 494f, 0.7f, 0.5f, 1.2f), false, 1f, 1f);
        hornLow.maxDistance = 35f;
        hornHigh.maxDistance = 35f;
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
        bubble.text = s;
        bubbleUntil = Time.time + seconds;
    }

    bool PlayerInPath(out float along)
    {
        along = 0f;
        if (!PlayerInLane) return false;
        Vector3 rel = PlayerFeet - transform.position;
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
        if (bubble.text.Length > 0 && Time.time > bubbleUntil) bubble.text = "";
        if (Frozen) return;

        float dt = Time.deltaTime;
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
