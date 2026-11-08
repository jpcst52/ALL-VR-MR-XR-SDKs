using UnityEngine;

/// <summary>
/// A Rocketbox avatar sitting on the forklift seat. The avatar has no sitting animation, so an idle clip plays and this
/// component bends the hips and knees on top of it every frame (LateUpdate runs after the Animator). While
/// <see cref="Waving"/> the right arm is raised and swung, which is how the driver signals the walker.
/// </summary>
public class SeatedDriver : MonoBehaviour
{
    public bool Waving;
    public Transform Head;

    // Local-space bends added to the idle pose, in degrees. Tuned by eye against the rig's bone axes.
    public static Vector3 ThighBend = new Vector3(0f, 0f, -90f);
    public static Vector3 CalfBend = new Vector3(0f, 0f, 90f);
    public static Vector3 ArmRaise = new Vector3(0f, 0f, 150f);

    Transform thighL, thighR, calfL, calfR, upperArmR, spine;

    /// <summary>Builds the seated driver under <paramref name="forklift"/>; null when the avatar is not in the project.</summary>
    public static SeatedDriver Create(Transform forklift, Vector3 seatLocal)
    {
        var avatar = Resources.Load<GameObject>(PeopleBoot.DriverAvatar);
        var controller = Resources.Load<RuntimeAnimatorController>("Rocketbox/Idle");
        if (avatar == null || controller == null) return null;
        var go = Object.Instantiate(avatar, forklift, false);
        go.name = "Driver avatar";
        go.transform.localPosition = seatLocal;
        go.transform.localRotation = Quaternion.identity;
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var d = go.AddComponent<SeatedDriver>();
        d.thighL = Find(go.transform, "Bip01 L Thigh");
        d.thighR = Find(go.transform, "Bip01 R Thigh");
        d.calfL = Find(go.transform, "Bip01 L Calf");
        d.calfR = Find(go.transform, "Bip01 R Calf");
        d.upperArmR = Find(go.transform, "Bip01 R UpperArm");
        d.Head = Find(go.transform, "Bip01 Head") ?? go.transform;
        return d;
    }

    void LateUpdate()
    {
        Bend(thighL, ThighBend);
        Bend(thighR, ThighBend);
        Bend(calfL, CalfBend);
        Bend(calfR, CalfBend);
        if (Waving && upperArmR != null)
            upperArmR.localRotation *= Quaternion.Euler(ArmRaise + new Vector3(0f, 0f, 20f * Mathf.Sin(Time.time * 10f)));
    }

    static void Bend(Transform bone, Vector3 euler)
    {
        if (bone != null) bone.localRotation *= Quaternion.Euler(euler);
    }

    static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var f = Find(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }
}
