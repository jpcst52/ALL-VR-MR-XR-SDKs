using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Valve.VR.InteractionSystem;
using KitHand = PittBradford.XR.Hand;
using ValveHand = Valve.VR.InteractionSystem.Hand;

namespace PittBradford.XR.SteamVRBackend
{
    /// <summary>
    /// Builds the training rig on Valve's SteamVR Interaction System instead of the kit's own Input System rig:
    ///   head and hands     the Interaction System's Player prefab (tracked head, hands with the real controller models)
    ///   moving around      its Teleporting prefab: hold the Vive trackpad, aim the arc at a TeleportArea, release
    ///   picking things up  its Throwable and Interactable on every Grabbable
    /// The scenarios still talk to XRRig, Hand, Pressable and Grabbable as before. This assembly only exists when the project defines
    /// PB_STEAMVR (set by the SteamVR project setup), and it registers itself before the first scene loads. With no headset it steps aside
    /// and the kit's own desktop rig is used.
    /// Prefabs are read from Resources/PBSteamVR (Player and Teleporting), made by the project setup from the plugin's own prefabs.
    /// </summary>
    public sealed class SteamVRBackend : IRigBackend, IGrabBackend
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var backend = new SteamVRBackend();
            XRBackends.Rig = backend;
            XRBackends.Grab = backend;
        }

        public XRRig CreateRig(Vector3 position, float yawDegrees)
        {
            if (!XRSettings.isDeviceActive)
            {
                Debug.Log("[steamvr] no headset is active: using the kit's desktop rig");
                return null;
            }
            var playerPrefab = Resources.Load<GameObject>("PBSteamVR/Player");
            var teleportPrefab = Resources.Load<GameObject>("PBSteamVR/Teleporting");
            if (playerPrefab == null)
            {
                Debug.LogWarning("[steamvr] Resources/PBSteamVR/Player is missing (run the SteamVR project setup); using the kit's own rig");
                return null;
            }

            var root = new GameObject("XR Rig");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yawDegrees, 0));
            var playerGo = Object.Instantiate(playerPrefab, root.transform, false);
            playerGo.name = "Player (SteamVR)";
            var player = playerGo.GetComponent<Player>();
            if (teleportPrefab != null) Object.Instantiate(teleportPrefab).name = "Teleporting (SteamVR)";

            var camera = playerGo.GetComponentInChildren<Camera>(true);
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 400f;
            camera.allowMSAA = true;
            var rig = XRRig.CreateExternal(root.transform, camera);

            // the kit's Hand (pointing ray for Pressable, Held, Haptic) rides on each of Valve's tracked hands; the model shown is Valve's own
            var left = KitHand.Attach(player.leftHand.transform, rig, true, new HandInput(player.leftHand), false);
            var right = KitHand.Attach(player.rightHand.transform, rig, false, new HandInput(player.rightHand), false);
            rig.SetHands(left, right);

            rig.ExternalLookAt = target =>
            {
                var head = camera.transform;
                var d = target - head.position; d.y = 0f;
                if (d.sqrMagnitude < 1e-4f) return;
                float turn = Mathf.DeltaAngle(head.eulerAngles.y, Quaternion.LookRotation(d).eulerAngles.y);
                player.trackingOriginTransform.RotateAround(head.position, Vector3.up, turn);
            };
            root.AddComponent<FloorOrigin>().Camera = camera;
            var desk = root.AddComponent<DeskMode>();
            desk.Player = player; desk.Camera = camera; desk.Rig = rig; desk.Right = right;
            // Simulator only: SteamVR's simulated headset reports a height of 0, which puts the eyes on the floor. scripts/steamvr-simulator.ps1 -Run passes
            // -pbHeadOffset <metres> to lift the origin; a real headset reports its own height and is never given the argument.
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-pbHeadOffset" && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lift))
                {
                    player.trackingOriginTransform.localPosition += Vector3.up * lift;
                    Debug.Log("[steamvr] simulator head offset +" + lift.ToString("0.00") + " m");
                }
            rig.AreaChanged += area => TeleportAreas.Rebuild(rig, area);
            if (rig.Area.HasValue) TeleportAreas.Rebuild(rig, rig.Area);
            Debug.Log("[steamvr] rig built on the SteamVR Interaction System (Player, Teleporting, Throwable)");
            return rig;
        }

        /// <summary>
        /// Valve's plugin asks for a floor-level tracking origin with an API Unity 6 has deprecated, and in a Unity 6 player the origin can stay at the
        /// headset, which puts the player's eyes on the floor. This asks the XR input subsystem for the floor origin directly, for the first seconds
        /// (the subsystem can restart while the headset connects), and logs the head height so a wrong height is visible in the player log.
        /// </summary>
        sealed class FloorOrigin : MonoBehaviour
        {
            public Camera Camera;
            float until;
            float nextAsk;
            bool logged;

            void Start() { until = Time.realtimeSinceStartup + 10f; }

            void Update()
            {
                if (Time.realtimeSinceStartup < until && Time.realtimeSinceStartup >= nextAsk)
                {
                    nextAsk = Time.realtimeSinceStartup + 0.5f;
                    var subsystems = new List<XRInputSubsystem>();
                    SubsystemManager.GetSubsystems(subsystems);
                    foreach (var s in subsystems)
                    {
                        if ((s.GetTrackingOriginMode() & TrackingOriginModeFlags.Floor) == 0 && (s.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0)
                            s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                    }
                }
                if (!logged && Time.realtimeSinceStartup > until - 4f && Camera != null)
                {
                    logged = true;
                    var subsystems = new List<XRInputSubsystem>();
                    SubsystemManager.GetSubsystems(subsystems);
                    string mode = subsystems.Count > 0 ? subsystems[0].GetTrackingOriginMode().ToString() : "no input subsystem";
                    Debug.Log("[steamvr] tracking origin " + mode + "; head is " + (Camera.transform.position.y - transform.position.y).ToString("0.00") + " m above the rig floor");
                }
            }
        }

        /// <summary>
        /// For a headset with no tracked controllers (SteamVR's simulated headset, or a Vive whose wands are off): the keyboard and mouse stand in.
        /// W A S D walk, the mouse looks, left click is the trigger (presses what the ray points at), E is the grip. Picking things up with Valve's
        /// Throwable needs a tracked hand, so that part still needs real controllers. Off whenever a controller is tracked.
        /// </summary>
        sealed class DeskMode : MonoBehaviour
        {
            public Player Player; public Camera Camera; public XRRig Rig; public KitHand Right;
            public static bool Active;
            float started;

            void Start() { started = Time.realtimeSinceStartup; Active = false; }
            void OnDestroy() { Active = false; }

            static bool ControllerTracked()
            {
                var list = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice, list);
                foreach (var d in list)
                    if (d.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) && tracked) return true;
                return false;
            }

            void Update()
            {
                bool controller = ControllerTracked();
                if (!Active)
                {
                    if (Time.realtimeSinceStartup - started < 3f || controller) return;
                    Active = true;
                    // the simulated headset also reports a head height of 0
                    if (Camera.transform.position.y - transform.position.y < 0.5f) Player.trackingOriginTransform.localPosition += Vector3.up * 1.6f;
                    if (Right != null) { Right.transform.SetParent(Camera.transform, false); Right.transform.localPosition = new Vector3(0.12f, -0.1f, 0.25f); Right.transform.localRotation = Quaternion.identity; }
                    Cursor.lockState = CursorLockMode.Locked;
                    // the window shows one eye of the headset picture; a window of another shape stretches it
                    if (!Screen.fullScreen) Screen.SetResolution(756, 850, false);
                    Debug.Log("[steamvr] no controller is tracked: keyboard and mouse mode (WASD walk, mouse look, left click trigger, E grip, Esc frees the mouse)");
                }
                else if (controller) { Active = false; Cursor.lockState = CursorLockMode.None; return; }

                if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
                if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; }

                var origin = Player.trackingOriginTransform;
                var head = Camera.transform;
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    origin.RotateAround(head.position, Vector3.up, Input.GetAxis("Mouse X") * 3f);
                    float pitch = -Input.GetAxis("Mouse Y") * 3f;
                    float current = Mathf.Asin(Mathf.Clamp(head.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    if (current + (-pitch) < 80f && current + (-pitch) > -80f) origin.RotateAround(head.position, head.right, pitch);
                }
                var flat = head.forward; flat.y = 0f; flat.Normalize();
                var right = head.right; right.y = 0f; right.Normalize();
                var move = flat * ((Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f))
                         + right * ((Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f));
                if (move.sqrMagnitude > 0f)
                {
                    var next = head.position + move.normalized * 2f * Time.deltaTime;
                    if (Rig.Area.HasValue) next = Rig.Area.Value.ClosestPoint(new Vector3(next.x, Rig.Area.Value.center.y, next.z));
                    var delta = new Vector3(next.x - head.position.x, 0f, next.z - head.position.z);
                    origin.position += delta;
                }
            }
        }

        public void Setup(Grabbable g)
        {
            if (g.GetComponent<Throwable>() != null) return;
            if (g.GetComponent<Interactable>() == null) g.gameObject.AddComponent<Interactable>();
            var t = g.gameObject.AddComponent<PBThrowable>();
            t.Grabbable = g;
            if (g.GetComponent<AllowTeleportWhileAttachedToHand>() == null) g.gameObject.AddComponent<AllowTeleportWhileAttachedToHand>();
        }

        /// <summary>Valve's Throwable, telling the scenario's Grabbable when it was picked up and let go.</summary>
        sealed class PBThrowable : Throwable
        {
            public Grabbable Grabbable;

            static KitHand Bridge(ValveHand hand) => hand != null ? hand.GetComponentInChildren<KitHand>(true) : null;

            protected override void OnAttachedToHand(ValveHand hand)
            {
                base.OnAttachedToHand(hand);
                if (Grabbable != null) Grabbable.NotifyGrabbed(Bridge(hand));
            }

            protected override void OnDetachedFromHand(ValveHand hand)
            {
                base.OnDetachedFromHand(hand);
                if (Grabbable != null) Grabbable.NotifyDropped(Bridge(hand));
            }
        }

        sealed class HandInput : IHandInput
        {
            readonly ValveHand hand;
            readonly SteamVR_Input_Sources source;
            SteamVR_Action_Boolean pinch, grip;

            public HandInput(ValveHand hand) { this.hand = hand; source = hand.handType; }

            bool Desk => DeskMode.Active && source == SteamVR_Input_Sources.RightHand;
            public bool Trigger => Desk ? Input.GetMouseButton(0) : (pinch ??= SteamVR_Input.GetBooleanAction("GrabPinch"))?.GetState(source) ?? false;
            public bool Grip => Desk ? Input.GetKey(KeyCode.E) : (grip ??= SteamVR_Input.GetBooleanAction("GrabGrip"))?.GetState(source) ?? false;
            public Vector2 Stick => Vector2.zero;
            public bool StickClick => false;
            public void Haptic(float amplitude, float seconds) => hand.TriggerHapticPulse(Mathf.Max(0.01f, seconds), 150f, Mathf.Clamp01(amplitude));
        }
    }

    /// <summary>
    /// The places the player may teleport to: one TeleportArea (Valve's component) over the training area, at floor height. It is the only
    /// floor the trackpad arc accepts, so a player cannot teleport onto a table, out a wall or off the platform. The rig's Area sets its size.
    /// </summary>
    static class TeleportAreas
    {
        public static void Rebuild(XRRig rig, Bounds? area)
        {
            var old = GameObject.Find("PB Teleport Area");
            if (old != null) Object.Destroy(old);
            if (!area.HasValue) { RefreshMarkers(); return; }
            var b = area.Value;
            float y = rig.transform.position.y + 0.012f;
            var go = new GameObject("PB Teleport Area");
            go.transform.position = new Vector3(b.center.x, y, b.center.z);
            float hx = b.extents.x, hz = b.extents.z;
            var mesh = new Mesh { name = "teleport area" };
            mesh.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (Teleport.instance != null) renderer.sharedMaterial = Teleport.instance.areaVisibleMaterial;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.AddComponent<TeleportArea>();
            RefreshMarkers();
        }

        // Teleport finds its markers once, in Start; an area made later has to be added to that list.
        static void RefreshMarkers()
        {
            var teleport = Teleport.instance;
            if (teleport == null) return;
            var field = typeof(Teleport).GetField("teleportMarkers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) return;
            field.SetValue(teleport, Object.FindObjectsByType<TeleportMarkerBase>(FindObjectsSortMode.None));
        }
    }
}
