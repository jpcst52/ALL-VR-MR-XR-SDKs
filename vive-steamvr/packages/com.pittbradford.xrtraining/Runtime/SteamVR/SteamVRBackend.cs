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

            public bool Trigger => (pinch ??= SteamVR_Input.GetBooleanAction("GrabPinch"))?.GetState(source) ?? false;
            public bool Grip => (grip ??= SteamVR_Input.GetBooleanAction("GrabGrip"))?.GetState(source) ?? false;
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
