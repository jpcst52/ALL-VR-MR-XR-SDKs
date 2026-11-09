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
            XRBackends.Grab?.Setup(this);
        }

        /// <summary>For a backend that picks the object up itself (SteamVR's Throwable): tell the scenario it was grabbed.</summary>
        public void NotifyGrabbed(Hand hand)
        {
            if (Snapped != null) { Snapped.Clear(); Snapped = null; }
            if (hand != null) { hand.SetHeld(this); hand.Haptic(0.5f, 0.1f); }
            Sfx.Play("grab", transform.position, 0.6f);
            Grabbed?.Invoke(this);
        }

        /// <summary>For a backend: the object was let go. It snaps into a matching zone if one is close.</summary>
        public void NotifyDropped(Hand hand)
        {
            if (hand != null) hand.SetHeld(null);
            Dropped?.Invoke(this);
            if (SnapZone.TrySnap(this)) return;
            Sfx.Play("release", transform.position, 0.5f);
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
            Sfx.Play("grab", transform.position, 0.6f);
            Grabbed?.Invoke(this);
        }

        public void EndGrab(Hand hand)
        {
            transform.SetParent(null, true);
            Dropped?.Invoke(this);
            if (SnapZone.TrySnap(this)) return;
            Sfx.Play("release", transform.position, 0.5f);
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
