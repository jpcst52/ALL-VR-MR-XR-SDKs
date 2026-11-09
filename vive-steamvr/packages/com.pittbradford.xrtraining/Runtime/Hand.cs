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
        /// <summary>Thumbstick or trackpad pressed in (Primary2DAxisClick).</summary>
        public bool StickClick { get; private set; }
        /// <summary>Set by the rig while a teleport arc is shown, so the pointing ray does not draw on top of it.</summary>
        public bool SuppressRay;
        public bool TriggerDown, TriggerUp, TriggerHeld, GripDown, GripUp, GripHeld;
        public Vector3 Velocity { get; private set; }
        public Grabbable Held { get; private set; }
        public Pressable Hover { get; private set; }

        InputAction trigger, grip, stick, stickClick;
        IHandInput external;
        bool lastTrigger, lastGrip, keepVisualHidden;
        /// <summary>True when a backend does the picking up (SteamVR's Throwable), so this hand does not.</summary>
        public bool ExternalGrab;
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
            hand.stickClick = Button("<XRController>{" + usage + "}/{Primary2DAxisClick}");
            hand.BuildVisual();
            return hand;
        }

        /// <summary>For a backend: a hand that follows <paramref name="host"/> (the backend's own tracked hand object) and reads its buttons from <paramref name="input"/>.</summary>
        public static Hand Attach(Transform host, XRRig rig, bool left, IHandInput input, bool ownVisual)
        {
            var go = new GameObject(left ? "PB Left Hand" : "PB Right Hand");
            go.transform.SetParent(host, false);
            var hand = go.AddComponent<Hand>();
            hand.IsLeft = left;
            hand.Rig = rig;
            hand.external = input;
            hand.ExternalGrab = true;
            hand.BuildVisual();
            hand.keepVisualHidden = !ownVisual;
            if (!ownVisual && hand.visual != null) hand.visual.gameObject.SetActive(false);
            return hand;
        }

        /// <summary>For a backend: the object this hand holds changed (the backend did the grabbing).</summary>
        public void SetHeld(Grabbable g) { Held = g; }

        static InputAction Button(string binding)
        {
            var a = new InputAction("button", InputActionType.Button, binding);
            a.Enable();
            return a;
        }

        /// <summary>The controller model: a blue handle, a gold ring and a gold tip, about the size of a Vive wand. Built from code so it can never be missing.</summary>
        void BuildVisual()
        {
            visual = new GameObject("Controller").transform;
            visual.SetParent(transform, false);
            visual.localScale = Vector3.one * 0.7f;
            var body = P.Mat(P.Royal, 0.1f, 0.55f);
            var gold = P.Mat(P.Gold, 0.2f, 0.7f);
            // tilted forward like a held wand: handle runs back toward the palm, ring and tip point ahead
            P.Prim(PrimitiveType.Capsule, visual, "Handle", new Vector3(0, -0.035f, -0.02f), new Vector3(0.04f, 0.075f, 0.04f), body, false)
                .transform.localRotation = Quaternion.Euler(70f, 0, 0);
            P.Prim(PrimitiveType.Cylinder, visual, "Ring", new Vector3(0, 0.0f, 0.055f), new Vector3(0.085f, 0.006f, 0.085f), gold, false)
                .transform.localRotation = Quaternion.Euler(90f, 0, 0);
            P.Prim(PrimitiveType.Sphere, visual, "Head", new Vector3(0, 0.0f, 0.08f), Vector3.one * 0.035f, gold, false);
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.enabled = true;
            }
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

        /// <summary>True when the controller model is switched on and has at least one enabled renderer with a material. Used by the hand check.</summary>
        public bool VisualOk(out string why)
        {
            why = "";
            if (visual == null) { why = "no visual"; return false; }
            if (!gameObject.activeInHierarchy || !visual.gameObject.activeInHierarchy) { why = "inactive"; return false; }
            int n = 0;
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) { why = "renderer disabled: " + r.name; return false; }
                if (r.sharedMaterial == null) { why = "no material: " + r.name; return false; }
                n++;
            }
            if (n == 0) { why = "no renderers"; return false; }
            if (visual.lossyScale.x < 0.4f || visual.lossyScale.x > 2f) { why = "odd scale " + visual.lossyScale.x; return false; }
            return true;
        }

        void Update()
        {
            bool headset = XRRig.Headset;
            if (!headset && external == null) DesktopPose();
            // the model is always on (also with no controller tracked); only a scenario's own hide call turns it off
            if (visual != null && !keepVisualHidden && !visual.gameObject.activeSelf) visual.gameObject.SetActive(true);
            ReadInput(headset);
            UpdateVelocity();
            UpdatePointer();
            if (ExternalGrab) return;
            if (GripDown && Held == null) TryGrab();
            if (GripUp && Held != null) Release();
            if (Held != null && !GripHeld && headset) Release();
        }

        void ReadInput(bool headset)
        {
            if (external != null)
            {
                bool t = external.Trigger, g = external.Grip;
                TriggerDown = t && !lastTrigger; TriggerUp = !t && lastTrigger; TriggerHeld = t;
                GripDown = g && !lastGrip; GripUp = !g && lastGrip; GripHeld = g;
                lastTrigger = t; lastGrip = g;
                Stick = external.Stick; StickClick = external.StickClick;
                return;
            }
            if (headset)
            {
                TriggerDown = trigger.WasPressedThisFrame(); TriggerUp = trigger.WasReleasedThisFrame(); TriggerHeld = trigger.IsPressed();
                GripDown = grip.WasPressedThisFrame(); GripUp = grip.WasReleasedThisFrame(); GripHeld = grip.IsPressed();
                Stick = stick.ReadValue<Vector2>(); StickClick = stickClick.IsPressed();
            }
            else
            {
                var mouse = Mouse.current; var kb = Keyboard.current;
                if (IsLeft || mouse == null || kb == null)
                {
                    TriggerDown = TriggerUp = TriggerHeld = GripDown = GripUp = GripHeld = false; Stick = Vector2.zero; StickClick = false; return;
                }
                TriggerDown = mouse.leftButton.wasPressedThisFrame; TriggerUp = mouse.leftButton.wasReleasedThisFrame; TriggerHeld = mouse.leftButton.isPressed;
                GripDown = kb.eKey.wasPressedThisFrame; GripUp = kb.eKey.wasReleasedThisFrame; GripHeld = kb.eKey.isPressed;
                Stick = Vector2.zero; StickClick = false;
            }
        }

        void DesktopPose()
        {
            var cam = Rig.Camera.transform;
            if (IsLeft)
            {
                // desktop: the left hand is just shown, resting at the lower left of the view (it has no input)
                transform.position = cam.position + cam.forward * 0.65f - cam.right * 0.22f - cam.up * 0.24f;
                transform.rotation = Quaternion.LookRotation(cam.forward + cam.right * 0.06f + cam.up * 0.04f);
                ray.enabled = false;
                return;
            }
            transform.position = cam.position + cam.forward * 0.65f + cam.right * 0.22f - cam.up * 0.24f;
            // Aim at the point the mouse crosshair is on (center of the view).
            transform.rotation = Quaternion.LookRotation(cam.forward + cam.right * -0.06f + cam.up * 0.04f);
        }

        void UpdateVelocity()
        {
            if (Time.deltaTime > 0) Velocity = Vector3.Lerp(Velocity, (transform.position - lastPosition) / Time.deltaTime, 0.35f);
            lastPosition = transform.position;
        }

        bool PointsFromHand => XRRig.Headset || external != null;
        Vector3 PointOrigin => PointsFromHand ? transform.position : Rig.Camera.transform.position;
        Vector3 PointDirection => PointsFromHand ? transform.forward : Rig.Camera.transform.forward;

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
            if (ray != null && (!IsLeft || PointsFromHand))
            {
                ray.enabled = (!IsLeft || PointsFromHand) && !SuppressRay;
                ray.SetPosition(0, PointsFromHand ? transform.position : transform.position + transform.forward * 0.05f);
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
            if (external != null) { external.Haptic(amplitude, seconds); return; }
            var device = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRControllerWithRumble>(IsLeft ? UnityEngine.InputSystem.CommonUsages.LeftHand : UnityEngine.InputSystem.CommonUsages.RightHand);
            device?.SendImpulse(amplitude, seconds);
        }

        /// <summary>Where a held object should sit relative to this hand.</summary>
        public Transform HoldPoint => transform;
    }
}
