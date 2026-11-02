using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace PittBradford.XR
{
    /// <summary>
    /// One controller: pose, trigger/grip/stick input, a pointing ray for <see cref="Pressable"/> objects, and grabbing
    /// of <see cref="Grabbable"/> objects. With no headset the right hand follows the mouse (left click = trigger,
    /// E = grip) so the examples can run on a desktop.
    /// </summary>
    public class Hand : MonoBehaviour
    {
        public bool IsLeft { get; private set; }
        public XRRig Rig { get; private set; }
        public Vector2 Stick { get; private set; }
        public bool TriggerDown, TriggerUp, TriggerHeld, GripDown, GripUp, GripHeld;
        public Vector3 Velocity { get; private set; }
        public Grabbable Held { get; private set; }
        public Pressable Hover { get; private set; }

        InputAction trigger, grip, stick;
        LineRenderer ray;
        Transform visual;
        Vector3 lastPosition;
        const float GrabRadius = 0.16f;
        const float RayLength = 4f;

        public static Hand Create(XRRig rig, bool left)
        {
            string usage = left ? "LeftHand" : "RightHand";
            var go = new GameObject(left ? "Left Hand" : "Right Hand");
            go.transform.SetParent(rig.transform, false);
            go.transform.localPosition = new Vector3(left ? -0.25f : 0.25f, 1.1f, 0.35f);

            var driver = go.AddComponent<TrackedPoseDriver>();
            driver.positionInput = XRRig.Prop("<XRController>{" + usage + "}/devicePosition");
            driver.rotationInput = XRRig.Prop("<XRController>{" + usage + "}/deviceRotation");
            driver.ignoreTrackingState = true;

            var hand = go.AddComponent<Hand>();
            hand.IsLeft = left;
            hand.Rig = rig;
            hand.trigger = Button("<XRController>{" + usage + "}/{TriggerButton}");
            hand.grip = Button("<XRController>{" + usage + "}/{GripButton}");
            hand.stick = new InputAction("stick", InputActionType.Value, "<XRController>{" + usage + "}/{Primary2DAxis}");
            hand.stick.Enable();
            hand.BuildVisual();
            return hand;
        }

        static InputAction Button(string binding)
        {
            var a = new InputAction("button", InputActionType.Button, binding);
            a.Enable();
            return a;
        }

        void BuildVisual()
        {
            visual = new GameObject("Controller").transform;
            visual.SetParent(transform, false);
            // A simple controller: a handle and a ring, tilted forward like a held wand.
            P.Prim(PrimitiveType.Capsule, visual, "Handle", new Vector3(0, -0.03f, 0.0f), new Vector3(0.035f, 0.07f, 0.035f), P.Mat(P.MediumBlue, 0.2f, 0.6f), false)
                .transform.localRotation = Quaternion.Euler(70f, 0, 0);
            P.Prim(PrimitiveType.Sphere, visual, "Head", new Vector3(0, 0.0f, 0.06f), Vector3.one * 0.06f, P.Mat(P.Gold, 0.2f, 0.7f), false);
            var rayGo = new GameObject("Ray");
            rayGo.transform.SetParent(transform, false);
            ray = rayGo.AddComponent<LineRenderer>();
            ray.positionCount = 2;
            ray.startWidth = 0.003f;
            ray.endWidth = 0.0015f;
            ray.material = new Material(Shader.Find("Sprites/Default"));
            ray.startColor = new Color(P.Gold.r, P.Gold.g, P.Gold.b, 0.9f);
            ray.endColor = new Color(P.Gold.r, P.Gold.g, P.Gold.b, 0.1f);
            ray.useWorldSpace = true;
        }

        void Update()
        {
            bool headset = XRRig.Headset;
            if (!headset) DesktopPose();
            ReadInput(headset);
            UpdateVelocity();
            UpdatePointer();
            if (GripDown && Held == null) TryGrab();
            if (GripUp && Held != null) Release();
            if (Held != null && !GripHeld && headset) Release();
        }

        void ReadInput(bool headset)
        {
            if (headset)
            {
                TriggerDown = trigger.WasPressedThisFrame(); TriggerUp = trigger.WasReleasedThisFrame(); TriggerHeld = trigger.IsPressed();
                GripDown = grip.WasPressedThisFrame(); GripUp = grip.WasReleasedThisFrame(); GripHeld = grip.IsPressed();
                Stick = stick.ReadValue<Vector2>();
            }
            else
            {
                var mouse = Mouse.current; var kb = Keyboard.current;
                if (IsLeft || mouse == null || kb == null)
                {
                    TriggerDown = TriggerUp = TriggerHeld = GripDown = GripUp = GripHeld = false; Stick = Vector2.zero; return;
                }
                TriggerDown = mouse.leftButton.wasPressedThisFrame; TriggerUp = mouse.leftButton.wasReleasedThisFrame; TriggerHeld = mouse.leftButton.isPressed;
                GripDown = kb.eKey.wasPressedThisFrame; GripUp = kb.eKey.wasReleasedThisFrame; GripHeld = kb.eKey.isPressed;
                Stick = Vector2.zero;
            }
        }

        void DesktopPose()
        {
            if (IsLeft) { visual.gameObject.SetActive(false); ray.enabled = false; return; }
            var cam = Rig.Camera.transform;
            transform.position = cam.position + cam.forward * 0.55f + cam.right * 0.22f - cam.up * 0.2f;
            // Aim at the point the mouse crosshair is on (center of the view).
            transform.rotation = Quaternion.LookRotation(cam.forward + cam.right * -0.06f + cam.up * 0.04f);
        }

        void UpdateVelocity()
        {
            if (Time.deltaTime > 0) Velocity = Vector3.Lerp(Velocity, (transform.position - lastPosition) / Time.deltaTime, 0.35f);
            lastPosition = transform.position;
        }

        Vector3 PointOrigin => XRRig.Headset ? transform.position : Rig.Camera.transform.position;
        Vector3 PointDirection => XRRig.Headset ? transform.forward : Rig.Camera.transform.forward;

        void UpdatePointer()
        {
            Pressable found = null;
            Vector3 end = PointOrigin + PointDirection * RayLength;
            if (Physics.Raycast(PointOrigin, PointDirection, out var hit, RayLength, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                found = hit.collider.GetComponentInParent<Pressable>();
                if (found != null && !found.Interactable) found = null;
            }
            if (found != Hover)
            {
                if (Hover != null) Hover.SetHover(false);
                Hover = found;
                if (Hover != null) Hover.SetHover(true);
            }
            if (ray != null && ray.enabled || !IsLeft)
            {
                ray.enabled = !IsLeft || XRRig.Headset;
                ray.SetPosition(0, XRRig.Headset ? transform.position : transform.position + transform.forward * 0.05f);
                ray.SetPosition(1, end);
            }
            if (TriggerDown && Hover != null) Hover.Press(this);
        }

        void TryGrab()
        {
            Grabbable best = null;
            float bestDist = float.MaxValue;
            if (XRRig.Headset)
            {
                foreach (var c in Physics.OverlapSphere(transform.position, GrabRadius, ~0, QueryTriggerInteraction.Ignore))
                {
                    var g = c.GetComponentInParent<Grabbable>();
                    if (g == null || !g.CanGrab) continue;
                    float d = Vector3.Distance(transform.position, c.ClosestPoint(transform.position));
                    if (d < bestDist) { best = g; bestDist = d; }
                }
            }
            else if (Physics.Raycast(PointOrigin, PointDirection, out var hit, 3.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                var g = hit.collider.GetComponentInParent<Grabbable>();
                if (g != null && g.CanGrab) best = g;
            }
            if (best != null) { Held = best; best.BeginGrab(this); }
        }

        public void Release()
        {
            if (Held == null) return;
            var g = Held;
            Held = null;
            g.EndGrab(this);
        }

        public void Haptic(float amplitude = 0.4f, float seconds = 0.08f)
        {
            var device = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRControllerWithRumble>(IsLeft ? UnityEngine.InputSystem.CommonUsages.LeftHand : UnityEngine.InputSystem.CommonUsages.RightHand);
            device?.SendImpulse(amplitude, seconds);
        }

        /// <summary>Where a held object should sit relative to this hand.</summary>
        public Transform HoldPoint => transform;
    }
}
