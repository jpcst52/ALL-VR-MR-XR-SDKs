using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Valve.VR.InteractionSystem;
using KitHand = PittBradford.XR.Hand;

namespace PittBradford.XR.SteamVRBackend
{
    /// <summary>
    /// Self-test of keyboard and mouse mode ("desk mode") with no controllers. Start a build, or press Play in the editor, with
    /// <c>-pbDeskSelfTest</c> on the command line. It injects keyboard and mouse events through the Input System (the same path real keys take,
    /// Keyboard.current / Mouse.current) and logs a PASS or FAIL line for each of: desk mode starts, W moves the head, mouse delta turns the head,
    /// the pointer ray hits a Pressable and a click presses it, E picks up a Grabbable at range and lets it go, a hand model is in view, the hint
    /// panel is on. Then it prints "[desktest] RESULT PASS" or "FAIL" and quits. It only reads what is in the scene and moves the rig to aim; it
    /// does not need a controller or prove anything about one.
    /// </summary>
    public sealed class DeskSelfTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!HasArg("-pbDeskSelfTest")) return;
            var go = new GameObject("Desk Self Test");
            DontDestroyOnLoad(go);
            go.AddComponent<DeskSelfTest>();
        }

        static bool HasArg(string name)
        {
            foreach (var a in Environment.GetCommandLineArgs()) if (a == name) return true;
            return false;
        }

        bool failed;
        readonly StringBuilder summary = new StringBuilder();
        float nextBeat;

        // a heartbeat every 5 s of real time while the test runs, so a stalled test shows whether the game itself is still running
        void Update()
        {
            if (Time.realtimeSinceStartup < nextBeat) return;
            nextBeat = Time.realtimeSinceStartup + 5f;
            Debug.Log("[desktest] heartbeat: frame " + Time.frameCount + ", game time " + Time.time.ToString("0.0") + " s, timeScale " + Time.timeScale + ", scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void Result(string name, bool ok, string detail)
        {
            if (!ok) failed = true;
            string line = "[desktest] " + (ok ? "PASS " : "FAIL ") + name + " (" + detail + ")";
            summary.AppendLine(line);
            Debug.Log(line);
        }

        void Skip(string name, string why)
        {
            string line = "[desktest] SKIP " + name + " (" + why + ")";
            summary.AppendLine(line);
            Debug.Log(line);
        }

        // ---- injected input -------------------------------------------------------------------------------------------------------------------

        static void SetKey(Key key, bool down)
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            using (StateEvent.From(kb, out var ptr))
            {
                kb[key].WriteValueIntoEvent(down ? 1f : 0f, ptr);
                InputSystem.QueueEvent(ptr);
            }
        }

        static void SetLeftButton(bool down)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            using (StateEvent.From(mouse, out var ptr))
            {
                mouse.leftButton.WriteValueIntoEvent(down ? 1f : 0f, ptr);
                InputSystem.QueueEvent(ptr);
            }
        }

        static void MoveMouse(Vector2 delta)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            InputSystem.QueueDeltaStateEvent(mouse.delta, delta);
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        // ---- the test -------------------------------------------------------------------------------------------------------------------------

        IEnumerator Start()
        {
            Debug.Log("[desktest] start; keyboard " + (Keyboard.current != null ? "present" : "NONE") + ", mouse " + (Mouse.current != null ? "present" : "NONE")
                + ", focused " + Application.isFocused);
            // injected events must reach the game whether or not its window has focus (a build run by a script, an editor Game view not clicked)
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            SteamVRBackend.DeskMode desk = null;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 30f)
            {
                desk = FindFirstObjectByType<SteamVRBackend.DeskMode>();
                if (desk != null && SteamVRBackend.DeskMode.Active) break;
                yield return null;
            }
            bool active = desk != null && SteamVRBackend.DeskMode.Active;
            Result("desk mode starts with no controller", active, active ? "after " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s" : "never started (no headset, or a controller is tracked)");
            if (!active) { Finish(); yield break; }
            yield return new WaitForSeconds(1f);
            Debug.Log("[desktest] settled; clicking once to lock the mouse (timeScale " + Time.timeScale + ")");
            var cam = desk.Camera; var origin = desk.Player.trackingOriginTransform;

            // click first: it locks the mouse (needed for mouse look) the same way a user's first click does
            // (the view is tilted up 50 degrees for this click, so it cannot press anything: the hub lobby's portals are pressable and the start view looks at them)
            origin.RotateAround(cam.transform.position, cam.transform.right, -50f); yield return Frames(2);
            SetLeftButton(true); yield return Frames(3); SetLeftButton(false); yield return Frames(3);
            origin.RotateAround(cam.transform.position, cam.transform.right, 50f); yield return Frames(2);
            Debug.Log("[desktest] cursor lock after the first click: " + Cursor.lockState + ", scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            // W moves the head
            var p0 = cam.transform.position;
            SetKey(Key.W, true);
            float tw = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(0.6f);
            SetKey(Key.W, false); yield return Frames(3);
            float moved = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(cam.transform.position.x, 0, cam.transform.position.z));
            Result("W moves the head", moved > 0.2f, "moved " + moved.ToString("0.00") + " m in " + (Time.realtimeSinceStartup - tw).ToString("0.0") + " s, injected W seen by Keyboard.current " + (Keyboard.current != null));
            // S moves it back, A and D sideways (one check is enough to prove the key path; these only log)
            SetKey(Key.D, true); yield return new WaitForSeconds(0.3f); SetKey(Key.D, false); yield return Frames(2);

            // mouse look
            float y0 = cam.transform.eulerAngles.y;
            for (int i = 0; i < 10; i++) { MoveMouse(new Vector2(60f, 0f)); yield return null; }
            yield return Frames(2);
            float turned = Mathf.Abs(Mathf.DeltaAngle(y0, cam.transform.eulerAngles.y));
            Result("mouse movement turns the head", turned > 3f, "turned " + turned.ToString("0.0") + " degrees, cursor " + Cursor.lockState);

            // hand in view
            var vp = cam.WorldToViewportPoint(desk.Player.rightHand.transform.position);
            int rend = 0, inView = 0;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            foreach (var r in desk.Player.rightHand.GetComponentsInChildren<Renderer>(false))
            {
                if (r is LineRenderer || !r.enabled) continue;
                rend++;
                bool seen = GeometryUtility.TestPlanesAABB(planes, r.bounds);
                if (seen) inView++;
                Debug.Log("[desktest]   renderer " + r.name + " bounds size " + r.bounds.size.ToString("0.000") + " center in camera space " + cam.transform.InverseTransformPoint(r.bounds.center).ToString("0.00") + " in view " + seen);
            }
            Debug.Log("[desktest]   right Valve hand local position in camera space " + cam.transform.InverseTransformPoint(desk.Player.rightHand.transform.position).ToString("0.00")
                + "; kit hand visual on: " + (desk.Right != null && desk.Right.VisualOk(out _)));
            var kitRay = desk.Right != null ? desk.Right.GetComponentInChildren<LineRenderer>(false) : null;
            bool rayOn = kitRay != null && kitRay.enabled;
            bool handVisible = (inView > 0) || (vp.z > 0.1f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f && rend > 0);
            Result("hand model is in view", handVisible, rend + " enabled renderer(s) on the right hand, " + inView + " in the camera frustum, viewport " + vp.ToString("0.00")
                + "; pointer ray " + (rayOn ? "on" : "OFF"));
            var hint = cam.transform.Find("Desk Hint");
            Result("on-screen hint panel is shown", hint != null && hint.gameObject.activeInHierarchy, hint == null ? "no 'Desk Hint' object under the camera in this scene" : "found, active " + hint.gameObject.activeInHierarchy);

            // pointer + click
            Pressable target = null;
            var presses = new List<Pressable>(FindObjectsByType<Pressable>(FindObjectsSortMode.None));
            presses.RemoveAll(p => !p.isActiveAndEnabled || !p.Interactable);
            presses.Sort((a, b) => Vector3.Distance(a.transform.position, cam.transform.position).CompareTo(Vector3.Distance(b.transform.position, cam.transform.position)));
            foreach (var p in presses)
            {
                if (!StandAndAim(desk, p.transform, p)) continue;
                target = p; break;
            }
            if (target == null) Skip("pointer hover and click press", presses.Count == 0 ? "no Pressable in this scene" : "none of " + presses.Count + " Pressables could be reached with a clear line of sight");
            else
            {
                yield return Frames(3);
                // a target that moves by itself (a radio that follows the hand) can drift off the ray: re-aim a few times, as a user's mouse would
                for (int tries = 0; tries < 6 && desk.Right.Hover != target; tries++)
                {
                    var tc = target.GetComponentInChildren<Collider>();
                    TurnToward(desk, tc != null ? tc.bounds.center : target.transform.position);
                    yield return Frames(3);
                }
                bool hover = desk.Right.Hover != null; // any Pressable: neighbours of a drifting target count, the point is that the ray hovers one
                var hovered = desk.Right.Hover != null ? desk.Right.Hover : target;
                var rf = desk.Right.transform;
                string rayHit = Physics.Raycast(rf.position, rf.forward, out var rh, 4f, ~0, QueryTriggerInteraction.Ignore) ? rh.collider.name : "nothing";
                Result("pointer ray hovers a Pressable", hover, target.name + ", hover = " + (desk.Right.Hover != null ? desk.Right.Hover.name : "none") + ", ray now hits " + rayHit
                    + ", Interactable " + target.Interactable);
                bool pressed = false;
                Action<KitHand> on = h => pressed = true;
                hovered.Pressed += on;
                string sceneBefore = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                string hoveredName = hovered.name; // read now: a lobby portal's scene is unloaded by the press
                SetLeftButton(true); yield return Frames(3); SetLeftButton(false); yield return Frames(3);
                if (hovered != null) hovered.Pressed -= on;
                Result("left click presses it", pressed, hoveredName);
                string sceneAfter = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (sceneAfter != sceneBefore)
                {
                    // the pressed object was a lobby portal: the rig of the old scene is gone, so the grab check cannot run here
                    Result("the press loaded a scene (lobby portal)", true, sceneBefore + " -> " + sceneAfter);
                    Skip("E grab and release", "the scene changed after the portal press");
                    Finish();
                    yield break;
                }
            }

            // E grab at range
            Grabbable grab = null;
            var grabs = new List<Grabbable>(FindObjectsByType<Grabbable>(FindObjectsSortMode.None));
            grabs.RemoveAll(g => !g.isActiveAndEnabled || !g.CanGrab || g.GetComponent<Throwable>() == null);
            grabs.Sort((a, b) => Vector3.Distance(a.transform.position, cam.transform.position).CompareTo(Vector3.Distance(b.transform.position, cam.transform.position)));
            foreach (var g in grabs)
            {
                if (!StandAndAim(desk, g.transform, g)) continue;
                grab = g; break;
            }
            if (grab == null) Skip("E grab and release", grabs.Count == 0 ? "no Grabbable with a Throwable in this scene" : "none of " + grabs.Count + " Grabbables could be reached with a clear line of sight");
            else
            {
                yield return Frames(3);
                SetKey(Key.E, true); yield return Frames(4);
                bool held = desk.Holding && desk.Right.Held == grab;
                var th = grab.GetComponent<Throwable>();
                bool parented = grab.transform.IsChildOf(desk.Player.rightHand.transform);
                Result("E picks up a Grabbable (pointer, within " + SteamVRBackend.DeskMode.GrabRange + " m)", held && parented, grab.name + ", Held=" + held + ", parented to Valve's hand=" + parented);
                yield return new WaitForSeconds(0.4f);
                SetKey(Key.E, false); yield return Frames(4);
                bool released = !desk.Holding && desk.Right.Held == null && !grab.transform.IsChildOf(desk.Player.rightHand.transform);
                Result("releasing E lets go", released, grab.name + ", Holding=" + desk.Holding + ", parent=" + (grab.transform.parent != null ? grab.transform.parent.name : "none"));
            }
            Finish();
        }

        void Finish()
        {
            Debug.Log("[desktest] RESULT " + (failed ? "FAIL" : "PASS") + " in " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "\n" + summary);
#if UNITY_EDITOR
            UnityEditor.SessionState.SetString("pbDeskSelfTestResult", failed ? "FAIL" : "PASS"); // read by DeskSelfTestRunner (editor) after Play stops
            UnityEditor.EditorApplication.isPlaying = false;
#else
            if (HasArg("-pbHold")) { Debug.Log("[desktest] holding the scene open for a screenshot (-pbHold); close the window or stop the process to end"); return; }
            Application.Quit(failed ? 3 : 0);
#endif
        }

        /// <summary>
        /// Puts the head 1.2 to 2.4 m from the target with a clear line of sight (rig moved directly; the key under test is not W here), then turns the
        /// head so the right pointer ray runs through the target. Returns false when no spot has a clear line.
        /// </summary>
        bool StandAndAim(SteamVRBackend.DeskMode desk, Transform target, Component owner)
        {
            var cam = desk.Camera; var origin = desk.Player.trackingOriginTransform;
            var aim = target.position;
            var col = target.GetComponentInChildren<Collider>();
            if (col != null) aim = col.bounds.center;
            float headY = cam.transform.position.y;
            foreach (float dist in new[] { 1.5f, 2.2f, 1.1f })
            {
                for (int i = 0; i < 8; i++)
                {
                    var dir = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
                    var pos = new Vector3(aim.x, Mathf.Max(headY, aim.y + 0.2f), aim.z) + dir * dist;
                    if (Physics.CheckSphere(pos, 0.25f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (!Physics.Linecast(pos, aim, out var hit, ~0, QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent(owner.GetType()) == owner)
                    {
                        origin.position += pos - cam.transform.position;
                        for (int k = 0; k < 4; k++) TurnToward(desk, aim);
                        var from = desk.Right.transform;
                        if (Physics.Raycast(from.position, from.forward, out var h2, 4f, ~0, QueryTriggerInteraction.Ignore) && h2.collider.GetComponentInParent(owner.GetType()) == owner)
                        {
                            Debug.Log("[desktest] aimed at " + owner.name + " from " + dist.ToString("0.0") + " m (ray hits " + h2.collider.name + ")");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static void TurnToward(SteamVRBackend.DeskMode desk, Vector3 aim)
        {
            var cam = desk.Camera.transform; var origin = desk.Player.trackingOriginTransform;
            var from = desk.Right.transform;
            var want = aim - from.position;
            var flatWant = Vector3.ProjectOnPlane(want, Vector3.up); var flatNow = Vector3.ProjectOnPlane(from.forward, Vector3.up);
            if (flatWant.sqrMagnitude > 1e-6f && flatNow.sqrMagnitude > 1e-6f)
                origin.RotateAround(cam.position, Vector3.up, Vector3.SignedAngle(flatNow, flatWant, Vector3.up));
            float pitch = Vector3.SignedAngle(Vector3.ProjectOnPlane(from.forward, cam.right), Vector3.ProjectOnPlane(want, cam.right), cam.right);
            origin.RotateAround(cam.position, cam.right, pitch);
        }
    }
}
