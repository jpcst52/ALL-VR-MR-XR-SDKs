using UnityEngine;

/// <summary>Keeps a world-space label or sign (front face on local -Z) facing the player's camera.</summary>
public class WfBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
