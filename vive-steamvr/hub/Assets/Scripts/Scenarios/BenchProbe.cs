using UnityEngine;
using PittBradford.XR;

// In its own file so the baked scene can save it (Unity serializes a MonoBehaviour only from a file of the same name).
/// <summary>A meter probe. When released near a lead it clips on and follows the part.</summary>
public class BenchProbe : MonoBehaviour
{
    public Transform Tip;
    // the clipped part is a runtime object, so the reference is not saved with the baked probe
    [System.NonSerialized] public BenchResistor ClipRes;
    [System.NonSerialized] public int ClipLead;
    Rigidbody rb;

    void Awake() { rb = GetComponent<Rigidbody>(); }

    public void Unclip()
    {
        ClipRes = null;
        rb.isKinematic = false;
    }

    void LateUpdate()
    {
        if (ClipRes == null || GetComponentInParent<Hand>() != null) return;
        rb.isKinematic = true;
        Quaternion rot = Quaternion.LookRotation(new Vector3(0.1f, -0.85f, 0.5f));
        transform.rotation = rot;
        transform.position = ClipRes.LeadMid(ClipLead) - rot * Tip.localPosition;
    }
}
