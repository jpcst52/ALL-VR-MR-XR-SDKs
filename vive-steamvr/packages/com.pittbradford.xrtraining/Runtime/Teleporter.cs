using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PittBradford.XR
{
    /// <summary>
    /// Comfort locomotion for the kit rig. Push either thumbstick (or the Vive trackpad) forward to show an arc and a target ring,
    /// let go to teleport there after a very short black blink. Left or right on either stick turns by <see cref="XRRig.SnapTurnDegrees"/>.
    /// With no headset, hold T and release. A target is accepted only on a roughly level surface that is about floor height,
    /// inside <see cref="XRRig.Area"/>, with room to stand; a red ring means it was refused.
    /// Created by <see cref="XRRig.Create"/>; the scenario scripts do not need to know about it.
    /// </summary>
    public class Teleporter : MonoBehaviour
    {
        public XRRig Rig;
        /// <summary>Launch speed of the arc in metres per second (longer arc = farther target).</summary>
        public float Speed = 7f;
        public LayerMask Mask = ~0;
        /// <summary>Highest slope still treated as floor (surface normal y).</summary>
        public float MinNormalY = 0.9f;
        /// <summary>How far above or below the current floor height a target may be (so a table top is refused but a step is fine).</summary>
        public float MaxStepUp = 0.4f, MaxStepDown = 0.6f;
        /// <summary>Optional extra rule from a scenario: return false to refuse a world point.</summary>
        public Func<Vector3, bool> Filter;

        public bool Aiming => aiming != null || keyAiming;
        public bool TargetValid { get; private set; }
        public Vector3 Target { get; private set; }
        public int TeleportCount { get; private set; }

        Hand aiming;
        bool keyAiming, blinking, turnArmed = true, hintShown;
        float snapCooldown, hintUntil;
        LineRenderer arc, ring, dot;
        Renderer fadeRenderer;
        Material fadeMat;
        TextMesh hint;
        readonly Vector3[] pts = new Vector3[40];
        static readonly RaycastHit[] hits = new RaycastHit[8];
        static readonly Collider[] overlaps = new Collider[16];
        static readonly Color Good = new Color(0.3f, 1f, 0.55f, 0.95f), Bad = new Color(1f, 0.3f, 0.25f, 0.9f);

        public static Teleporter Create(XRRig rig)
        {
            var t = rig.gameObject.AddComponent<Teleporter>();
            t.Rig = rig;
            t.Build();
            return t;
        }

        void Build()
        {
            arc = Line("Teleport arc", pts.Length, 0.012f, false);
            ring = Line("Teleport ring", 29, 0.025f, true);
            dot = Line("Teleport dot", 13, 0.03f, true);
            arc.enabled = ring.enabled = dot.enabled = false;

            // the blink: a black quad just in front of the eyes, drawn over everything, only visible during a teleport
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Teleport blink";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(Rig.Camera.transform, false);
            q.transform.localPosition = new Vector3(0, 0, 0.15f);
            q.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            fadeRenderer = q.GetComponent<Renderer>();
            var sh = Shader.Find("PittBradford/FlatColor");
            if (sh != null)
            {
                fadeMat = new Material(sh) { color = new Color(0, 0, 0, 0), renderQueue = 4000 };
                fadeMat.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
            }
            else fadeMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0, 0, 0, 0), renderQueue = 4000 };
            fadeRenderer.sharedMaterial = fadeMat;
            fadeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fadeRenderer.receiveShadows = false;
            fadeRenderer.enabled = false;

            string[] args = Environment.GetCommandLineArgs();
            bool capture = Array.IndexOf(args, "-pbCapture") >= 0; // README screenshots do not want the hint in the picture
            if (!capture)
            {
                hint = P.Text(Rig.Camera.transform, "", new Vector3(0, 0.30f, 1.15f), 0.034f, P.Sky, TextAnchor.MiddleCenter, false, "Teleport hint");
                hintUntil = Time.time + 18f;
            }
        }

        LineRenderer Line(string name, int count, float width, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = count;
            lr.loop = loop;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        void Update()
        {
            UpdateHint();
            if (Rig == null || !Rig.LocomotionEnabled || !Rig.TeleportEnabled || blinking || Rig.Left == null || Rig.Right == null)
            {
                if (!blinking) StopAiming();
                return;
            }
            if (XRRig.Headset) HeadsetInput(); else DesktopInput();
        }

        // ------------------------------------------------------------------ input

        static bool Forward(Vector2 s) => s.y > 0.7f && Mathf.Abs(s.x) < 0.5f;

        void HeadsetInput()
        {
            if (aiming == null)
            {
                foreach (var h in new[] { Rig.Right, Rig.Left })
                    if (Forward(h.Stick)) { aiming = h; break; }
            }
            if (aiming != null)
            {
                var s = aiming.Stick;
                // stays on while the stick is still pushed forward (or the trackpad is clicked in the upper half); letting go fires
                bool held = s.y > 0.4f || (aiming.StickClick && s.y > 0.2f);
                if (held)
                {
                    aiming.SuppressRay = true;
                    Aim(aiming.transform.position, aiming.transform.forward);
                    return;
                }
                var hand = aiming;
                hand.SuppressRay = false;
                aiming = null;
                Fire();
                return;
            }
            SnapTurn();
        }

        void SnapTurn()
        {
            snapCooldown -= Time.deltaTime;
            float x = 0f;
            if (Rig.SmoothLocomotion) x = Rig.Right.Stick.x; // the left stick walks then, so only the right one turns
            else { x = Mathf.Abs(Rig.Right.Stick.x) > Mathf.Abs(Rig.Left.Stick.x) ? Rig.Right.Stick.x : Rig.Left.Stick.x; }
            float y = Mathf.Max(Mathf.Abs(Rig.Right.Stick.y), Rig.SmoothLocomotion ? 0f : Mathf.Abs(Rig.Left.Stick.y));
            if (Mathf.Abs(x) < 0.4f) turnArmed = true;
            if (turnArmed && snapCooldown <= 0f && Mathf.Abs(x) > 0.7f && y < 0.5f)
            {
                SnapTurnBy(Mathf.Sign(x) * Rig.SnapTurnDegrees);
                turnArmed = false;
                snapCooldown = 0.25f;
            }
        }

        /// <summary>Turns the rig around the player's head so the view rotates in place.</summary>
        public void SnapTurnBy(float degrees)
        {
            Rig.transform.RotateAround(Rig.Camera.transform.position, Vector3.up, degrees);
        }

        void DesktopInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tKey.isPressed)
            {
                keyAiming = true;
                var cam = Rig.Camera.transform;
                var dir = Quaternion.AngleAxis(-10f, cam.right) * cam.forward;
                Aim(cam.position + cam.forward * 0.3f - cam.up * 0.2f, dir);
            }
            else if (keyAiming)
            {
                keyAiming = false;
                Fire();
            }
        }

        // ------------------------------------------------------------------ aiming

        /// <summary>Follows the arc from a point and direction. Returns true and the landing point when it ends on an acceptable spot.</summary>
        public bool TryAim(Vector3 origin, Vector3 dir, out Vector3 point) => Trace(origin, dir, out point, out _, out _);

        bool Trace(Vector3 origin, Vector3 dir, out Vector3 point, out bool hitSomething, out int count)
        {
            var v = dir.normalized * Speed;
            var p = origin;
            pts[0] = p;
            count = 1;
            point = origin;
            hitSomething = false;
            const float dt = 0.03f;
            for (int i = 1; i < pts.Length; i++)
            {
                v += Physics.gravity * dt;
                var next = p + v * dt;
                var seg = next - p;
                float len = seg.magnitude;
                int n = Physics.RaycastNonAlloc(p, seg / len, hits, len, Mask, QueryTriggerInteraction.Ignore);
                int best = -1;
                float bestD = float.MaxValue;
                for (int k = 0; k < n; k++)
                {
                    if (IsHeldOrRig(hits[k].collider)) continue;
                    if (hits[k].distance < bestD) { bestD = hits[k].distance; best = k; }
                }
                if (best >= 0)
                {
                    var hit = hits[best];
                    pts[count++] = hit.point;
                    point = hit.point;
                    hitSomething = true;
                    return Acceptable(hit);
                }
                pts[count++] = next;
                p = next;
                if (p.y < Rig.transform.position.y - 8f) break;
            }
            return false;
        }

        bool IsHeldOrRig(Collider c)
        {
            if (c.transform.IsChildOf(Rig.transform)) return true;
            var g = c.GetComponentInParent<Grabbable>();
            return g != null && (Rig.Left.Held == g || Rig.Right.Held == g);
        }

        bool Acceptable(RaycastHit hit)
        {
            if (hit.normal.y < MinNormalY) return false;                                  // wall, ceiling, steep slope
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) return false;        // a loose object or vehicle, not floor
            float dy = hit.point.y - Rig.transform.position.y;
            if (dy > MaxStepUp || dy < -MaxStepDown) return false;                        // table top, shelf, or a drop
            if (Rig.Area.HasValue)
            {
                var b = Rig.Area.Value;
                if (hit.point.x < b.min.x || hit.point.x > b.max.x || hit.point.z < b.min.z || hit.point.z > b.max.z) return false;
            }
            // room to stand: a capsule from knee to head must be clear
            var a = hit.point + Vector3.up * 0.35f;
            var c = hit.point + Vector3.up * 1.6f;
            int n = Physics.OverlapCapsuleNonAlloc(a, c, 0.22f, overlaps, Mask, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++) if (!IsHeldOrRig(overlaps[k])) return false;
            if (Filter != null && !Filter(hit.point)) return false;
            return true;
        }

        /// <summary>Shows the arc and ring for a given aim (used by the hand check for a screenshot).</summary>
        public void Preview(Vector3 origin, Vector3 dir) => Aim(origin, dir);
        public void HidePreview() => StopAiming();

        void Aim(Vector3 origin, Vector3 dir)
        {
            TargetValid = Trace(origin, dir, out var point, out bool hit, out int count);
            Target = point;
            var col = TargetValid ? Good : Bad;
            arc.enabled = true;
            arc.positionCount = count;
            for (int i = 0; i < count; i++) arc.SetPosition(i, pts[i]);
            arc.startColor = arc.endColor = col;
            ring.enabled = dot.enabled = hit;
            if (hit)
            {
                Circle(ring, point + Vector3.up * 0.02f, 0.28f);
                Circle(dot, point + Vector3.up * 0.02f, 0.05f);
                ring.startColor = ring.endColor = dot.startColor = dot.endColor = col;
            }
        }

        static void Circle(LineRenderer lr, Vector3 center, float radius)
        {
            for (int i = 0; i < lr.positionCount; i++)
            {
                float a = (i / (float)lr.positionCount) * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius);
            }
        }

        void StopAiming()
        {
            if (aiming != null) aiming.SuppressRay = false;
            aiming = null;
            keyAiming = false;
            if (arc != null) arc.enabled = ring.enabled = dot.enabled = false;
        }

        void Fire()
        {
            bool ok = TargetValid;
            var target = Target;
            StopAiming();
            if (ok) TeleportTo(target);
            else Sfx.Play("ui_error", Rig.FeetPosition, 0.4f);
        }

        // ------------------------------------------------------------------ the move

        /// <summary>Teleports so the player's head ends up over the point (floor). Skips every check; use <see cref="TryAim"/> first.</summary>
        public void TeleportTo(Vector3 floorPoint, bool blink = true)
        {
            if (blinking) return;
            if (blink && isActiveAndEnabled) StartCoroutine(Blink(floorPoint));
            else Move(floorPoint);
        }

        void Move(Vector3 p)
        {
            var cam = Rig.Camera.transform.position;
            Rig.transform.position += new Vector3(p.x - cam.x, p.y - Rig.transform.position.y, p.z - cam.z);
            TeleportCount++;
        }

        IEnumerator Blink(Vector3 p)
        {
            blinking = true;
            fadeRenderer.enabled = true;
            for (float t = 0; t < 0.07f; t += Time.unscaledDeltaTime) { SetFade(t / 0.07f); yield return null; }
            SetFade(1f);
            Move(p);
            hintUntil = Mathf.Min(hintUntil, Time.time + 3f);
            yield return null;
            for (float t = 0; t < 0.12f; t += Time.unscaledDeltaTime) { SetFade(1f - t / 0.12f); yield return null; }
            SetFade(0f);
            fadeRenderer.enabled = false;
            blinking = false;
        }

        void SetFade(float a) { var c = fadeMat.color; c.a = Mathf.Clamp01(a); fadeMat.color = c; }

        // ------------------------------------------------------------------ hint

        void UpdateHint()
        {
            if (hint == null) return;
            if (Time.time > hintUntil || Rig == null || !Rig.LocomotionEnabled || !Rig.TeleportEnabled)
            {
                Destroy(hint.gameObject);
                hint = null;
                return;
            }
            if (!hintShown || hint.text.Length == 0 || hint.text.StartsWith("Hold") != !XRRig.Headset)
            {
                hint.text = XRRig.Headset
                    ? "Push a thumbstick forward and let go to teleport.  Stick left or right: turn."
                    : "Hold T and let go to teleport.  WASD walks, right mouse looks.";
                hintShown = true;
            }
            float left = hintUntil - Time.time;
            var c = P.Sky; c.a = Mathf.Clamp01(left / 2f);
            hint.color = c;
        }
    }
}
