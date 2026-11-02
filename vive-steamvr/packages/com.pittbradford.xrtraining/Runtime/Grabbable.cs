using System;
using UnityEngine;

namespace PittBradford.XR
{
    // In its own file so baked scenes can save it (Unity serializes a MonoBehaviour only from a file of the same name).
    /// <summary>An object you can pick up with the grip. Parented to the hand while held; thrown on release.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour
    {
        public string SnapId = "";
        public bool UseGravity = true;
        public bool CanGrab = true;
        public SnapZone Snapped;
        public event Action<Grabbable> Grabbed, Dropped;
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = UseGravity;
        }

        public void BeginGrab(Hand hand)
        {
            if (Snapped != null) { Snapped.Clear(); Snapped = null; }
            rb.isKinematic = true;
            transform.SetParent(hand.HoldPoint, true);
            if (!XRRig.Headset)
            {
                // Hold it in front of the camera on desktop.
                var cam = hand.Rig.Camera.transform;
                transform.position = cam.position + cam.forward * 0.9f - cam.up * 0.15f;
            }
            hand.Haptic(0.5f, 0.1f);
            Grabbed?.Invoke(this);
        }

        public void EndGrab(Hand hand)
        {
            transform.SetParent(null, true);
            Dropped?.Invoke(this);
            if (SnapZone.TrySnap(this)) return;
            rb.isKinematic = false;
            rb.useGravity = UseGravity;
            rb.linearVelocity = hand.Velocity;
        }

        public void Freeze()
        {
            rb.isKinematic = true;
        }
    }
}
