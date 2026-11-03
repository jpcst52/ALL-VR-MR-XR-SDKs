using UnityEngine;

/// <summary>
/// Keeps a group of people at a fixed spot relative to a named scene object that moves at runtime (for example the
/// refinery's muster point, which is placed upwind of the unit when the scene starts).
/// </summary>
public class FollowTarget : MonoBehaviour
{
    public string TargetName = "Muster point";
    public Vector3 LocalPosition;
    public float LocalYaw;
    Transform target;

    void LateUpdate()
    {
        if (target == null)
        {
            var g = GameObject.Find(TargetName);
            if (g == null) return;
            target = g.transform;
        }
        transform.SetPositionAndRotation(target.TransformPoint(LocalPosition), target.rotation * Quaternion.Euler(0f, LocalYaw, 0f));
    }
}
