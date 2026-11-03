using UnityEngine;
using PittBradford.XR;

/// <summary>
/// A co-worker on foot, built from primitives: work shirt, hi-vis vest with reflective bands, white hard hat, and a
/// clipboard. Walks a fixed route at walking pace with swinging legs and arms, then stops and stands. Uses scaled time,
/// so it freezes when the scenario pauses (Time.timeScale = 0) for the awareness check.
/// </summary>
public class CoWorker : MonoBehaviour
{
    public Vector3[] Route;
    public float WalkSpeed = 1.2f;

    public bool Walking { get; private set; }
    public bool Done { get; private set; }
    /// <summary>Current walking velocity (zero when standing).</summary>
    public Vector3 Velocity { get; private set; }
    public Vector3 HeadPosition => head.position;
    public Vector3 ChestPosition => transform.position + Vector3.up * 1.25f;

    int target = 1;
    float phase, lastSin;
    Transform head, legL, legR, armL, armR;
    Animator animator;

    /// <summary>
    /// Optional: returns a finished, animated person (a model whose Animator has a float "Speed" that switches idle and
    /// walk) to use instead of the primitive figure. Left null in the stand-alone project; the hub sets it when it has
    /// real character models, so this project needs no extra assets.
    /// </summary>
    public static System.Func<GameObject> ModelFactory;

    /// <summary>
    /// Builds the co-worker (hidden until <see cref="Begin"/>). Always made at runtime: it appears and walks during
    /// play, so it is kept out of a scene bake (plain GameObject plus <see cref="DontBake"/>).
    /// </summary>
    public static CoWorker Build(Transform parent, Vector3[] route)
    {
        var go = new GameObject("Co-worker");
        go.transform.SetParent(parent, false);
        go.AddComponent<DontBake>();
        var c = go.AddComponent<CoWorker>();
        c.Route = route;
        c.BuildModel();
        c.PlaceAtStart();
        go.SetActive(false);
        return c;
    }

    void PlaceAtStart()
    {
        target = 1;
        Done = false;
        Velocity = Vector3.zero;
        Vector3 d = Route[1] - Route[0];
        d.y = 0f;
        transform.SetPositionAndRotation(Route[0], Quaternion.LookRotation(d.normalized, Vector3.up));
    }

    /// <summary>Appear at the start of the route and start walking.</summary>
    public void Begin()
    {
        PlaceAtStart();
        gameObject.SetActive(true);
        Walking = true;
    }

    /// <summary>Puts the co-worker at a point for a screenshot, facing <paramref name="heading"/>, mid-stride.</summary>
    public void Pose(Vector3 position, Vector3 heading, float stride)
    {
        gameObject.SetActive(true);
        heading.y = 0f;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading.normalized, Vector3.up));
        Swing(stride);
    }

    void BuildModel()
    {
        var t = transform;
        var external = ModelFactory?.Invoke();
        if (external != null)
        {
            external.transform.SetParent(t, false);
            external.transform.localPosition = Vector3.zero;
            external.transform.localRotation = Quaternion.identity;
            animator = external.GetComponentInChildren<Animator>();
            head = FindDeep(external.transform, "Bip01 Head") ?? t;
            return;
        }
        var shirt = P.Mat(new Color(0.16f, 0.24f, 0.4f), 0f, 0.3f);
        var trousers = P.Mat(new Color(0.12f, 0.12f, 0.14f), 0f, 0.3f);
        var boots = P.Mat(new Color(0.22f, 0.14f, 0.07f), 0f, 0.3f);
        var skin = P.Mat(new Color(0.62f, 0.45f, 0.34f), 0f, 0.4f);
        var lime = P.Mat(new Color(0.78f, 1f, 0.05f), 0f, 0.4f);
        var silver = P.Mat(new Color(0.88f, 0.88f, 0.9f), 0.8f, 0.85f);
        var white = P.Mat(new Color(0.95f, 0.95f, 0.93f), 0.1f, 0.7f);
        var board = P.Mat(new Color(0.45f, 0.32f, 0.18f), 0f, 0.3f);
        var paper = P.Mat(new Color(0.96f, 0.96f, 0.94f), 0f, 0.2f);

        // legs swing from the hip
        legL = Limb(t, "Leg L", new Vector3(-0.1f, 0.9f, 0f), new Vector3(0.15f, 0.42f, 0.15f), -0.44f, trousers);
        legR = Limb(t, "Leg R", new Vector3(0.1f, 0.9f, 0f), new Vector3(0.15f, 0.42f, 0.15f), -0.44f, trousers);
        P.Box(legL, "Boot", new Vector3(0, -0.86f, 0.05f), new Vector3(0.13f, 0.1f, 0.26f), boots, false);
        P.Box(legR, "Boot", new Vector3(0, -0.86f, 0.05f), new Vector3(0.13f, 0.1f, 0.26f), boots, false);

        // torso and hi-vis vest (front, back, sides, reflective bands)
        P.Prim(PrimitiveType.Capsule, t, "Torso", new Vector3(0, 1.25f, 0), new Vector3(0.38f, 0.36f, 0.24f), shirt, false);
        P.Box(t, "Vest front", new Vector3(0, 1.22f, 0.115f), new Vector3(0.38f, 0.5f, 0.03f), lime, false);
        P.Box(t, "Vest back", new Vector3(0, 1.22f, -0.115f), new Vector3(0.38f, 0.5f, 0.03f), lime, false);
        P.Box(t, "Vest side", new Vector3(-0.185f, 1.2f, 0), new Vector3(0.03f, 0.44f, 0.22f), lime, false);
        P.Box(t, "Vest side", new Vector3(0.185f, 1.2f, 0), new Vector3(0.03f, 0.44f, 0.22f), lime, false);
        foreach (float y in new[] { 1.1f, 1.28f })
        {
            P.Box(t, "Reflective band", new Vector3(0, y, 0.132f), new Vector3(0.39f, 0.045f, 0.01f), silver, false);
            P.Box(t, "Reflective band", new Vector3(0, y, -0.132f), new Vector3(0.39f, 0.045f, 0.01f), silver, false);
        }

        // arms swing from the shoulder; the right hand carries a clipboard
        armL = Limb(t, "Arm L", new Vector3(-0.25f, 1.43f, 0f), new Vector3(0.1f, 0.3f, 0.1f), -0.3f, shirt);
        armR = Limb(t, "Arm R", new Vector3(0.25f, 1.43f, 0f), new Vector3(0.1f, 0.3f, 0.1f), -0.3f, shirt);
        P.Ball(armL, "Hand", new Vector3(0, -0.6f, 0), 0.05f, skin, false);
        P.Ball(armR, "Hand", new Vector3(0, -0.6f, 0), 0.05f, skin, false);
        P.Box(armR, "Clipboard", new Vector3(0.04f, -0.58f, 0.08f), new Vector3(0.02f, 0.3f, 0.22f), board, false);
        P.Box(armR, "Paper", new Vector3(0.052f, -0.6f, 0.08f), new Vector3(0.006f, 0.25f, 0.19f), paper, false);

        // head and a white hard hat
        head = P.Ball(t, "Head", new Vector3(0, 1.7f, 0), 0.115f, skin, false).transform;
        P.Prim(PrimitiveType.Sphere, t, "Hard hat", new Vector3(0, 1.78f, 0), new Vector3(0.27f, 0.16f, 0.29f), white, false);
        P.Cyl(t, "Hat brim", new Vector3(0, 1.74f, 0.02f), 0.15f, 0.012f, Vector3.up, white, false);

        // round 3 detail (all primitives): neck, ears, nose, hair, safety glasses, hat peak and ridge ribs, chin strap
        var hair = P.Mat(new Color(0.12f, 0.09f, 0.07f), 0f, 0.2f);
        var dark = P.Mat(new Color(0.08f, 0.08f, 0.09f), 0f, 0.3f);
        var denim = P.Mat(new Color(0.1f, 0.12f, 0.2f), 0f, 0.25f);
        var glove = P.Mat(new Color(0.9f, 0.5f, 0.05f), 0f, 0.3f);
        P.Cyl(t, "Neck", new Vector3(0, 1.58f, 0), 0.055f, 0.1f, Vector3.up, skin, false);
        P.Ball(t, "Ear L", new Vector3(-0.115f, 1.7f, 0f), 0.025f, skin, false);
        P.Ball(t, "Ear R", new Vector3(0.115f, 1.7f, 0f), 0.025f, skin, false);
        P.Ball(t, "Nose", new Vector3(0, 1.69f, 0.112f), 0.022f, skin, false);
        P.Ball(t, "Hair", new Vector3(0, 1.69f, -0.07f), 0.1f, hair, false);
        P.Box(t, "Safety glasses", new Vector3(0, 1.715f, 0.118f), new Vector3(0.17f, 0.04f, 0.02f), P.Transparent(new Color(0.75f, 0.9f, 1f, 0.45f)), false);
        P.Box(t, "Glasses frame", new Vector3(0, 1.738f, 0.122f), new Vector3(0.18f, 0.008f, 0.015f), dark, false);
        P.Box(t, "Hat peak", new Vector3(0, 1.76f, 0.15f), new Vector3(0.22f, 0.012f, 0.09f), white, false);
        for (int i = -1; i <= 1; i += 2) P.Box(t, "Hat rib", new Vector3(i * 0.045f, 1.855f, 0f), new Vector3(0.012f, 0.012f, 0.2f), white, false);
        P.Box(t, "Chin strap", new Vector3(0, 1.64f, 0.1f), new Vector3(0.19f, 0.006f, 0.006f), dark, false);

        // belt, trouser pockets, a radio on the vest with a short antenna, an ID badge, vest zip and pocket, shoulder straps
        P.Box(t, "Belt", new Vector3(0, 0.96f, 0), new Vector3(0.33f, 0.045f, 0.2f), dark, false);
        P.Box(t, "Belt buckle", new Vector3(0, 0.96f, 0.102f), new Vector3(0.05f, 0.035f, 0.01f), silver, false);
        P.Box(t, "Cargo pocket", new Vector3(-0.175f, 0.75f, 0.0f), new Vector3(0.02f, 0.16f, 0.1f), denim, false);
        P.Box(t, "Cargo pocket", new Vector3(0.175f, 0.75f, 0.0f), new Vector3(0.02f, 0.16f, 0.1f), denim, false);
        P.Box(t, "Radio", new Vector3(-0.12f, 1.38f, 0.13f), new Vector3(0.05f, 0.09f, 0.03f), dark, false);
        P.Cyl(t, "Radio antenna", new Vector3(-0.12f, 1.47f, 0.13f), 0.004f, 0.08f, Vector3.up, dark, false);
        P.Box(t, "ID badge", new Vector3(0.11f, 1.3f, 0.135f), new Vector3(0.06f, 0.08f, 0.006f), P.Mat(Color.white, 0f, 0.4f), false);
        P.Box(t, "Vest zip", new Vector3(0, 1.22f, 0.135f), new Vector3(0.012f, 0.48f, 0.005f), dark, false);
        P.Box(t, "Vest pocket", new Vector3(-0.11f, 1.1f, 0.135f), new Vector3(0.09f, 0.08f, 0.006f), P.Mat(new Color(0.7f, 0.92f, 0.05f), 0f, 0.4f), false);
        foreach (float sx in new[] { -0.1f, 0.1f }) P.Box(t, "Vest strap", new Vector3(sx, 1.47f, 0f), new Vector3(0.07f, 0.03f, 0.25f), lime, false);
        P.Ball(t, "Shoulder L", new Vector3(-0.22f, 1.43f, 0f), 0.065f, shirt, false);
        P.Ball(t, "Shoulder R", new Vector3(0.22f, 1.43f, 0f), 0.065f, shirt, false);
        // gloves on the hands, boot soles, trouser cuffs
        P.Ball(armL, "Glove", new Vector3(0, -0.6f, 0), 0.058f, glove, false);
        P.Ball(armR, "Glove", new Vector3(0, -0.6f, 0), 0.058f, glove, false);
        P.Box(legL, "Sole", new Vector3(0, -0.905f, 0.05f), new Vector3(0.14f, 0.02f, 0.28f), dark, false);
        P.Box(legR, "Sole", new Vector3(0, -0.905f, 0.05f), new Vector3(0.14f, 0.02f, 0.28f), dark, false);
        P.Box(legL, "Cuff", new Vector3(0, -0.8f, 0), new Vector3(0.155f, 0.03f, 0.155f), denim, false);
        P.Box(legR, "Cuff", new Vector3(0, -0.8f, 0), new Vector3(0.155f, 0.03f, 0.155f), denim, false);
    }

    /// <summary>A limb hanging from a pivot: the capsule's centre is <paramref name="drop"/> below the pivot.</summary>
    static Transform Limb(Transform parent, string name, Vector3 pivotPos, Vector3 scale, float drop, Material mat)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = pivotPos;
        P.Prim(PrimitiveType.Capsule, pivot, name + " capsule", new Vector3(0, drop, 0), scale, mat, false);
        return pivot;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var f = FindDeep(t.GetChild(i), name); if (f != null) return f; }
        return null;
    }

    void Swing(float s)
    {
        if (animator != null) { animator.SetFloat("Speed", Mathf.Abs(s) > 0f ? 1f : 0f); return; }
        float leg = 24f * s, arm = 18f * s;
        legL.localRotation = Quaternion.Euler(leg, 0, 0);
        legR.localRotation = Quaternion.Euler(-leg, 0, 0);
        armL.localRotation = Quaternion.Euler(-arm, 0, 4f);
        armR.localRotation = Quaternion.Euler(arm * 0.5f - 20f, 0, -4f); // the clipboard arm moves less
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (!Walking || dt <= 0f) return;

        Vector3 pos = transform.position;
        Vector3 to = Route[target] - pos;
        to.y = 0f;
        float step = WalkSpeed * dt;
        while (to.magnitude <= step)
        {
            if (target == Route.Length - 1)
            {
                transform.position = Route[target];
                Walking = false;
                Done = true;
                Velocity = Vector3.zero;
                Swing(0f);
                return;
            }
            target++;
            to = Route[target] - pos;
            to.y = 0f;
        }
        Vector3 dir = to.normalized;
        Velocity = dir * WalkSpeed;
        transform.position = pos + Velocity * dt;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 240f * dt);
        phase += dt * WalkSpeed * 4.2f;
        Swing(animator != null ? 1f : Mathf.Sin(phase));
        // a footstep on the concrete each time the stride changes foot
        float sn = Mathf.Sin(phase);
        if (sn * lastSin < 0f) Sfx.Footstep(transform.position, Surface.Concrete, 0.45f);
        lastSin = sn;
    }
}
