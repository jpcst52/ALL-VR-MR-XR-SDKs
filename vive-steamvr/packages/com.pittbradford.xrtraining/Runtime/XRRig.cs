using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace PittBradford.XR
{
    /// <summary>
    /// A minimal OpenXR rig: head and two controllers driven by the Input System, thumbstick/trackpad locomotion,
    /// and a keyboard-and-mouse fallback so every example can be tried (and tested) without a headset.
    /// </summary>
    public class XRRig : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public Hand Left { get; private set; }
        public Hand Right { get; private set; }
        public static bool Headset => XRSettings.isDeviceActive;

        public float MoveSpeed = 2.0f;
        public float SnapTurnDegrees = 30f;
        public bool LocomotionEnabled = true;
        /// <summary>Optional limits so a player cannot walk out of the training area.</summary>
        public Bounds? Area;

        float pitch, yaw, snapCooldown;

        public static XRRig Create(Vector3 position, float yawDegrees = 0f)
        {
            var root = new GameObject("XR Rig");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yawDegrees, 0));
            var rig = root.AddComponent<XRRig>();

            var head = new GameObject("Head") { tag = "MainCamera" };
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            rig.Camera = head.AddComponent<Camera>();
            rig.Camera.nearClipPlane = 0.05f;
            rig.Camera.farClipPlane = 400f;
            rig.Camera.allowMSAA = true;
            head.AddComponent<AudioListener>();

            var driver = head.AddComponent<TrackedPoseDriver>();
            driver.positionInput = Prop("<XRHMD>/centerEyePosition");
            driver.rotationInput = Prop("<XRHMD>/centerEyeRotation");
            driver.ignoreTrackingState = true;

            rig.Left = Hand.Create(rig, true);
            rig.Right = Hand.Create(rig, false);
            rig.yaw = yawDegrees;
            return rig;
        }

        internal static InputActionProperty Prop(string binding)
        {
            var action = new InputAction("pose", InputActionType.Value, binding);
            action.Enable();
            return new InputActionProperty(action);
        }

        void Update()
        {
            if (!LocomotionEnabled) return;
            if (Headset) HeadsetLocomotion();
            else DesktopLocomotion();
            ClampToArea();
        }

        void HeadsetLocomotion()
        {
            Vector2 move = Left.Stick;
            if (move.sqrMagnitude > 0.04f)
            {
                var forward = Vector3.ProjectOnPlane(Camera.transform.forward, Vector3.up).normalized;
                var right = Vector3.ProjectOnPlane(Camera.transform.right, Vector3.up).normalized;
                transform.position += (forward * move.y + right * move.x) * (MoveSpeed * Time.deltaTime);
            }
            snapCooldown -= Time.deltaTime;
            float turn = Right.Stick.x;
            if (Mathf.Abs(turn) > 0.7f && snapCooldown <= 0f)
            {
                transform.RotateAround(Camera.transform.position, Vector3.up, Mathf.Sign(turn) * SnapTurnDegrees);
                snapCooldown = 0.35f;
            }
        }

        void DesktopLocomotion()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue() * 0.12f;
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
            }
            Camera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = Vector3.zero;
            if (kb.wKey.isPressed) dir += Vector3.forward;
            if (kb.sKey.isPressed) dir += Vector3.back;
            if (kb.aKey.isPressed) dir += Vector3.left;
            if (kb.dKey.isPressed) dir += Vector3.right;
            if (dir != Vector3.zero)
            {
                var flat = Quaternion.Euler(0, yaw, 0);
                transform.position += flat * dir.normalized * (MoveSpeed * Time.deltaTime);
            }
        }

        void ClampToArea()
        {
            if (!Area.HasValue) return;
            var b = Area.Value;
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, b.min.x, b.max.x);
            p.z = Mathf.Clamp(p.z, b.min.z, b.max.z);
            transform.position = p;
        }

        /// <summary>Position of the player's head in world space (feet are directly below).</summary>
        public Vector3 HeadPosition => Camera.transform.position;

        public Vector3 FeetPosition
        {
            get { var p = Camera.transform.position; return new Vector3(p.x, transform.position.y, p.z); }
        }

        public void LookAt(Vector3 target)
        {
            var d = target - Camera.transform.position;
            yaw = Quaternion.LookRotation(d).eulerAngles.y;
            pitch = Mathf.DeltaAngle(0, Quaternion.LookRotation(d).eulerAngles.x);
            if (!Headset) Camera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }
}
