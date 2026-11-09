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
                // The scene file still holds Valve's Player and Teleporting. With no headset, Valve's Player switches on its own 2D fallback (a second
                // camera and an on-screen "WASD EQ/Arrow Keys" text) which fights with the kit's desktop rig and shows the wrong keys. They are only
                // switched off for this run; the saved objects are untouched, and with a headset they are adopted as usual.
                var idlePlayer = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
                if (idlePlayer != null) idlePlayer.gameObject.SetActive(false);
                var idleTeleport = Object.FindFirstObjectByType<Teleport>(FindObjectsInactive.Include);
                if (idleTeleport != null) idleTeleport.gameObject.SetActive(false);
                Debug.Log("[steamvr] no headset is active: using the kit's desktop rig (Valve's Player and Teleporting in the scene are switched off for this run)");
                return null;
            }
            // Adopt the Player (and Teleporting) saved in the scene by the editor step (SteamVRRigPlacement), as in Valve's own sample scene.
            // Only when the scene has none (an older scene, or a headset project without the step) are they instantiated from Resources.
            var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            Transform root;
            if (player != null)
            {
                player.gameObject.SetActive(true);
                root = player.transform.parent != null ? player.transform.parent : player.transform;
                root.SetPositionAndRotation(position, Quaternion.Euler(0, yawDegrees, 0));
                Debug.Log("[steamvr] adopted the Player saved in the scene (" + root.name + " > " + player.name + ")");
            }
            else
            {
                var playerPrefab = Resources.Load<GameObject>("PBSteamVR/Player");
                if (playerPrefab == null)
                {
                    Debug.LogWarning("[steamvr] no Player in the scene and Resources/PBSteamVR/Player is missing (run the SteamVR project setup); using the kit's own rig");
                    return null;
                }
                root = new GameObject("XR Rig").transform;
                root.SetPositionAndRotation(position, Quaternion.Euler(0, yawDegrees, 0));
                var playerGo = Object.Instantiate(playerPrefab, root, false);
                playerGo.name = "Player (SteamVR)";
                player = playerGo.GetComponent<Player>();
                Debug.Log("[steamvr] no Player in the scene: instantiated one from Resources");
            }
            if (Object.FindFirstObjectByType<Teleport>(FindObjectsInactive.Include) == null)
            {
                var teleportPrefab = Resources.Load<GameObject>("PBSteamVR/Teleporting");
                if (teleportPrefab != null) Object.Instantiate(teleportPrefab).name = "Teleporting (SteamVR)";
            }
            var playerGo2 = player.gameObject;

            var camera = playerGo2.GetComponentInChildren<Camera>(true);
            camera.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 400f;
            camera.allowMSAA = true;
            var rig = XRRig.CreateExternal(root, camera);

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
            root.gameObject.AddComponent<FloorOrigin>().Camera = camera;
            var desk = root.gameObject.AddComponent<DeskMode>();
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
        internal sealed class DeskMode : MonoBehaviour
        {
            public Player Player; public Camera Camera; public XRRig Rig; public KitHand Right;
            public static bool Active;
            /// <summary>Seconds with no tracked controller before keyboard and mouse mode starts (the controller list can be empty for a moment at start-up).</summary>
            public const float StartDelay = 1f;
            /// <summary>Farthest object E can pick up with the pointer, metres.</summary>
            public const float GrabRange = 3f;
            float started, activeAt;
            bool diagLogged, noKeyLogged, gripWas;
            Transform hint;
            Grabbable deskHeld;

            // Keys and mouse are read from the Input System package first and the old Input class second, each guarded: the editor can have
            // either one switched off (Player Settings > Active Input Handling), and the old class throws when it is.
            public static bool Key(KeyCode legacy, System.Func<UnityEngine.InputSystem.Keyboard, UnityEngine.InputSystem.Controls.KeyControl> pick)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && pick(kb).isPressed) return true;
                try { return Input.GetKey(legacy); } catch (System.InvalidOperationException) { return false; }
            }
            public static bool Click()
            {
                var m = UnityEngine.InputSystem.Mouse.current;
                if (m != null && m.leftButton.isPressed) return true;
                try { return Input.GetMouseButton(0); } catch (System.InvalidOperationException) { return false; }
            }
            static bool ClickDown()
            {
                var m = UnityEngine.InputSystem.Mouse.current;
                if (m != null && m.leftButton.wasPressedThisFrame) return true;
                try { return Input.GetMouseButtonDown(0); } catch (System.InvalidOperationException) { return false; }
            }
            static bool EscDown()
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
                try { return Input.GetKeyDown(KeyCode.Escape); } catch (System.InvalidOperationException) { return false; }
            }
            static Vector2 MouseDelta()
            {
                var m = UnityEngine.InputSystem.Mouse.current;
                if (m != null) return m.delta.ReadValue() * 0.1f;
                try { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 3f; } catch (System.InvalidOperationException) { return Vector2.zero; }
            }

            void Start()
            {
                started = Time.realtimeSinceStartup; Active = false;
                // the hint panel is a saved scene object under the camera (placed by SteamVRRigPlacement); it is only switched on and off here
                hint = Camera != null ? Camera.transform.Find("Desk Hint") : null;
                if (hint != null)
                {
                    // the OS font and its atlas exist only at run time, so the saved text gets them here (the saved object is only adopted, not made)
                    foreach (var tm in hint.GetComponentsInChildren<TextMesh>(true))
                    {
                        tm.font = P.Font;
                        tm.GetComponent<MeshRenderer>().sharedMaterial = P.TextMaterial;
                    }
                    hint.gameObject.SetActive(false);
                }
            }
            void OnDestroy() { Active = false; GivePauseBack(); }

            // SteamVR pauses the game (Time.timeScale = 0) whenever it takes input focus from the app: the dashboard is open, or the headset is in
            // standby. A Vive on the desk that nobody wears, and SteamVR's simulated headset, go to standby after about 5 s. Walking, the pointer and
            // the lobby's pad timer all run on game time, so at the desk nothing would move or respond ("the controllers do nothing"). While desk mode
            // is active the pause is switched off; it is given back when a controller is tracked (someone put the headset on) or the scene ends.
            bool pauseSaved, pauseOff;
            void TakePauseOff()
            {
                var settings = SteamVR_Settings.instance;
                if (settings != null && !pauseOff) { pauseSaved = settings.pauseGameWhenDashboardVisible; settings.pauseGameWhenDashboardVisible = false; pauseOff = true; }
                if (Time.timeScale == 0f) { Time.timeScale = 1f; Debug.Log("[steamvr] desk mode: SteamVR had paused the game (headset in standby or dashboard open); it is running again"); }
            }
            // When SteamVR takes input focus away it also makes each Valve hand "hold" a dummy object (_application_lost_focus), which stops the hand
            // hovering and grabbing, and that dummy has no Interactable: with the game running again, Valve's Hand.FixedUpdate throws every physics
            // step. At the desk the focus is claimed back through SteamVR's own focus event, which makes the hands let the dummy go.
            static bool LostFocus(ValveHand hand)
            {
                var held = hand != null ? hand.currentAttachedObject : null;
                return held != null && held.name == "_application_lost_focus";
            }
            bool focusLogged;
            void ReclaimFocus()
            {
                if (!LostFocus(Player.leftHand) && !LostFocus(Player.rightHand)) return;
                SteamVR_Events.InputFocus.Send(true);
                if (!focusLogged) { focusLogged = true; Debug.Log("[steamvr] desk mode: SteamVR had taken input focus away from the app (headset in standby or dashboard open); the hands were not able to touch anything; focus claimed back"); }
            }
            void GivePauseBack()
            {
                if (!pauseOff) return;
                pauseOff = false;
                var settings = SteamVR_Settings.instance;
                if (settings != null) settings.pauseGameWhenDashboardVisible = pauseSaved;
            }

            /// <summary>
            /// E picks up the Grabbable the pointer ray hits within <see cref="GrabRange"/> and lets it go when E is released. It uses Valve's own
            /// Hand.AttachObject / DetachObject on the right hand (resting in front of the camera), so the object is held by the same Throwable a
            /// wand would use. A tracked hand grabs by being near the object; at the desk there is no hand reach, hence the ray.
            /// </summary>
            void DeskGrab()
            {
                bool e = Key(KeyCode.E, k => k.eKey);
                var valve = Player.rightHand;
                if (e && !gripWas && deskHeld == null && valve != null)
                {
                    var from = Right != null ? Right.transform : Camera.transform;
                    if (Physics.Raycast(from.position, from.forward, out var hit, GrabRange, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var g = hit.collider.GetComponentInParent<Grabbable>();
                        var t = g != null ? g.GetComponent<Throwable>() : null;
                        if (g != null && g.CanGrab && t != null)
                        {
                            valve.AttachObject(g.gameObject, GrabTypes.Grip, t.attachmentFlags);
                            deskHeld = g;
                            if (Right != null) Right.SetHeld(g);
                            Debug.Log("[steamvr] desk grab: holding " + g.name + " (" + hit.distance.ToString("0.00") + " m away)");
                        }
                    }
                }
                else if (!e && gripWas && deskHeld != null)
                {
                    DeskRelease();
                }
                gripWas = e;
            }

            void DeskRelease()
            {
                if (deskHeld == null) return;
                if (Player.rightHand != null) Player.rightHand.DetachObject(deskHeld.gameObject);
                if (Right != null) Right.SetHeld(null);
                Debug.Log("[steamvr] desk grab: released " + deskHeld.name);
                deskHeld = null;
            }

            public bool Holding => deskHeld != null;

            static bool ControllerTracked()
            {
                var list = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice, list);
                foreach (var d in list)
                    if (d.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) && tracked) return true;
                return false;
            }

            // Desk mode: Valve's hand/controller models stay visible. With no controller tracked they would sit at the tracking origin (the floor),
            // so each Valve hand is carried in front of the camera in a rest pose, as if held at the sides of the view. They go back when a controller
            // is tracked. Only the kit's pointing ray is thinned so it does not block the view.
            struct Saved { public Transform Hand, Parent; public Vector3 Pos; public Quaternion Rot; }
            readonly List<Saved> saved = new List<Saved>();
            readonly List<SteamVR_Behaviour_Pose> poseOff = new List<SteamVR_Behaviour_Pose>();

            void RestPose()
            {
                saved.Clear();
                PlaceAtRest(Player.leftHand, new Vector3(-0.12f, -0.2f, 0.5f));
                PlaceAtRest(Player.rightHand, new Vector3(0.12f, -0.2f, 0.5f));
                // The kit's pointing rays start at the hand. In desk mode the left kit hand has no input, and its ray (a gold line that begins at the
                // camera, seen from the side as a large yellow wedge) is switched off; the right ray is a very thin line.
                if (Rig.Left != null) Rig.Left.SuppressRay = true;
                if (Right != null)
                    foreach (var line in Right.GetComponentsInChildren<LineRenderer>(true))
                    { line.startWidth = 0.0008f; line.endWidth = 0.0004f; }
            }

            void PlaceAtRest(ValveHand hand, Vector3 local)
            {
                if (hand == null) return;
                saved.Add(new Saved { Hand = hand.transform, Parent = hand.transform.parent, Pos = hand.transform.localPosition, Rot = hand.transform.localRotation });
                // Valve's pose component writes the hand's local pose every frame (zero when nothing is tracked), which would undo the rest pose
                if (hand.trackedObject != null) { hand.trackedObject.enabled = false; poseOff.Add(hand.trackedObject); }
                hand.transform.SetParent(Camera.transform, false);
                hand.transform.localPosition = local;
                hand.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
                int shown = 0;
                foreach (var r in hand.GetComponentsInChildren<Renderer>(true)) if (!(r is LineRenderer)) { r.enabled = true; shown++; }
                Debug.Log("[steamvr] desk mode: " + hand.handType + " hand at rest pose, " + shown + " renderer(s) on its model (" + (hand.mainRenderModel != null ? hand.mainRenderModel.name : "no model") + ")");
            }

            void RestoreHands()
            {
                foreach (var s in saved)
                    if (s.Hand != null) { s.Hand.SetParent(s.Parent, false); s.Hand.localPosition = s.Pos; s.Hand.localRotation = s.Rot; }
                saved.Clear();
                foreach (var pose in poseOff) if (pose != null) pose.enabled = true;
                poseOff.Clear();
                if (Rig.Left != null) Rig.Left.SuppressRay = false;
            }

            void Update()
            {
                bool controller = ControllerTracked();
                if (!Active)
                {
                    if (Time.realtimeSinceStartup - started < StartDelay || controller) return;
                    Active = true; activeAt = Time.realtimeSinceStartup;
                    TakePauseOff();
                    if (hint != null) hint.gameObject.SetActive(true);
                    // the simulated headset also reports a head height of 0
                    if (Camera.transform.position.y - transform.position.y < 0.5f) Player.trackingOriginTransform.localPosition += Vector3.up * 1.6f;
                    if (Right != null) { Right.transform.SetParent(Camera.transform, false); Right.transform.localPosition = new Vector3(0.1f, -0.12f, 0.1f); Right.transform.localRotation = Quaternion.LookRotation(new Vector3(-0.1f, 0.12f, 2.9f)); /* the ray crosses the middle of the view 3 m ahead */ }
                    RestPose();
                    // lock the mouse only when the window has focus (in the editor the Game view must be clicked first; a click locks it)
                    if (Application.isFocused) Cursor.lockState = CursorLockMode.Locked;
                    // the window shows one eye of the headset picture; a window of another shape stretches it
                    if (!Screen.fullScreen) Screen.SetResolution(756, 850, false);
                    Debug.Log("[steamvr] no controller is tracked: keyboard and mouse mode (WASD walk, mouse look, left click trigger, E grip, Esc frees the mouse)");
                }
                else if (controller)
                {
                    DeskRelease(); Active = false; RestoreHands(); Cursor.lockState = CursorLockMode.None; GivePauseBack();
                    if (hint != null) hint.gameObject.SetActive(false);
                    return;
                }
                ReclaimFocus();
                DeskGrab();

                if (EscDown()) Cursor.lockState = CursorLockMode.None;
                if (ClickDown() && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; }

                var origin = Player.trackingOriginTransform;
                var head = Camera.transform;
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var d = MouseDelta();
                    origin.RotateAround(head.position, Vector3.up, d.x);
                    float pitch = -d.y;
                    float current = Mathf.Asin(Mathf.Clamp(head.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    if (current + (-pitch) < 80f && current + (-pitch) > -80f) origin.RotateAround(head.position, head.right, pitch);
                }
                bool w = Key(KeyCode.W, k => k.wKey), s = Key(KeyCode.S, k => k.sKey), a = Key(KeyCode.A, k => k.aKey), dk = Key(KeyCode.D, k => k.dKey);
                var flat = head.forward; flat.y = 0f; flat.Normalize();
                var right = head.right; right.y = 0f; right.Normalize();
                var move = flat * ((w ? 1f : 0f) - (s ? 1f : 0f)) + right * ((dk ? 1f : 0f) - (a ? 1f : 0f));
                if (move.sqrMagnitude > 0f)
                {
                    var next = head.position + move.normalized * 2f * Time.deltaTime;
                    if (Rig.Area.HasValue) next = Rig.Area.Value.ClosestPoint(new Vector3(next.x, Rig.Area.Value.center.y, next.z));
                    var delta = new Vector3(next.x - head.position.x, 0f, next.z - head.position.z);
                    origin.position += delta;
                    if (!diagLogged)
                    {
                        diagLogged = true;
                        var kb = UnityEngine.InputSystem.Keyboard.current;
                        Debug.Log("[steamvr] first W/A/S/D: origin " + origin.name + " moved by " + delta.ToString("0.000") + ", head now " + head.position.ToString("0.00") + ", cursor " + Cursor.lockState
                            + ", area " + (Rig.Area.HasValue ? Rig.Area.Value.ToString() : "none")
                            + ", InputSystem keyboard " + (kb != null ? "W=" + kb.wKey.isPressed : "none") + ", focused " + Application.isFocused);
                    }
                }
                else if (!noKeyLogged && !diagLogged && Time.realtimeSinceStartup - activeAt > 8f)
                {
                    noKeyLogged = true;
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    Debug.Log("[steamvr] 8 s in keyboard mode and no W/A/S/D seen. Input System keyboard " + (kb != null ? "present" : "NONE") + ", old Input W=" + SafeLegacy(KeyCode.W)
                        + ", Application.isFocused=" + Application.isFocused + ", cursor " + Cursor.lockState + ". Click the Game view, then hold W.");
                }
            }

            static string SafeLegacy(KeyCode k) { try { return Input.GetKey(k).ToString(); } catch (System.InvalidOperationException) { return "unavailable (old Input class is off)"; } }
        }

        public void Setup(Grabbable g)
        {
            if (g.GetComponent<Throwable>() != null) return;
            if (g.GetComponent<Interactable>() == null) g.gameObject.AddComponent<Interactable>();
            var t = g.gameObject.AddComponent<PBThrowable>();
            t.Grabbable = g;
            // A Throwable added in code starts with its UnityEvents null (Valve's prefabs have them saved), and its pick-up and release callbacks
            // call Invoke on them: a NullReferenceException on every grab. Found by the desk self-test (2026-11-08).
            if (t.onPickUp == null) t.onPickUp = new UnityEngine.Events.UnityEvent();
            if (t.onDetachFromHand == null) t.onDetachFromHand = new UnityEngine.Events.UnityEvent();
            if (t.onHeldUpdate == null) t.onHeldUpdate = new HandEvent();
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

            // Valve's Throwable lets go the moment its grab action is not held. In desk mode there is no action (the E key is read by DeskMode, which
            // does the releasing), so without this the object would drop one frame after it was picked up.
            protected override void HandAttachedUpdate(ValveHand hand)
            {
                if (DeskMode.Active && hand != null && hand.handType == SteamVR_Input_Sources.RightHand)
                {
                    if (onHeldUpdate != null) onHeldUpdate.Invoke(hand);
                    return;
                }
                base.HandAttachedUpdate(hand);
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
            public bool Trigger => Desk ? DeskMode.Click() : (pinch ??= SteamVR_Input.GetBooleanAction("GrabPinch"))?.GetState(source) ?? false;
            public bool Grip => Desk ? DeskMode.Key(KeyCode.E, k => k.eKey) : (grip ??= SteamVR_Input.GetBooleanAction("GrabGrip"))?.GetState(source) ?? false;
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
            // The scene file normally holds this object already (placed by SteamVRRigPlacement): adopt it, and only change its mesh if the
            // scenario's area differs from the saved one. With no area (a scenario that switches walking off) it is hidden.
            var existing = GameObject.Find("PB Teleport Area");
            if (!area.HasValue) { if (existing != null) existing.SetActive(false); RefreshMarkers(); return; }
            var b = area.Value;
            float y = rig.transform.position.y + 0.012f;
            float hx = b.extents.x, hz = b.extents.z;
            var go = existing != null ? existing : new GameObject("PB Teleport Area");
            go.SetActive(true);
            go.transform.position = new Vector3(b.center.x, y, b.center.z);
            // (Unity's destroyed-object null is not a C# null, so "a ?? b" would not add a missing component: use P.Ensure)
            var filter = P.Ensure<MeshFilter>(go);
            var current = filter.sharedMesh;
            bool same = current != null && Mathf.Abs(current.bounds.extents.x - hx) < 0.01f && Mathf.Abs(current.bounds.extents.z - hz) < 0.01f;
            if (!same)
            {
                var mesh = new Mesh { name = "teleport area" };
                mesh.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                filter.sharedMesh = mesh;
                var collider = P.Ensure<MeshCollider>(go);
                collider.sharedMesh = mesh;
            }
            var renderer = P.Ensure<MeshRenderer>(go);
            if (renderer.sharedMaterial == null && Teleport.instance != null) renderer.sharedMaterial = Teleport.instance.areaVisibleMaterial;
            P.Ensure<TeleportArea>(go);
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
