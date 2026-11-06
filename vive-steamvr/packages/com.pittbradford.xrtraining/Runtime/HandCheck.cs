using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Self-check for the rig, run with <c>-pbHandCheck &lt;folder&gt;</c> (see CaptureRunner). In the loaded scene it checks that both
    /// controller models exist, are switched on, have renderers and materials, and sit inside the camera view in the desktop fallback;
    /// then it tests the teleport rules (a floor target is accepted, the sky and a Filter-refused target are not, the move puts the head
    /// over the target, snap turn turns in place) and writes hands-SCENE.png and teleport-SCENE.png. Exit code 0 = all checks passed,
    /// 3 = a check failed. These are desktop renders: nothing here uses a headset.
    /// </summary>
    public class HandCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string dir = Arg("-pbHandCheck");
            if (dir == null) return;
            var go = new GameObject("Hand Check");
            DontDestroyOnLoad(go);
            go.AddComponent<HandCheck>().dir = dir;
        }

        string dir;
        readonly StringBuilder report = new StringBuilder();
        bool failed;

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        void Check(bool ok, string what, string detail = "")
        {
            if (!ok) failed = true;
            report.AppendLine((ok ? "[handcheck] ok   " : "[handcheck] FAIL ") + what + (detail.Length > 0 ? " (" + detail + ")" : ""));
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(4f);
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            CaptureDriver.StopXr();
            yield return null;
#if UNITY_EDITOR
            while (UnityEditor.ShaderUtil.anythingCompiling) yield return null;
#endif
            var rig = FindFirstObjectByType<XRRig>();
            Check(rig != null, "rig exists in " + scene);
            if (rig != null)
            {
                Check(!XRRig.Headset, "desktop fallback active (no headset)");
                yield return null;
                yield return null;
                var cam = rig.Camera;
                foreach (var h in new[] { rig.Left, rig.Right })
                {
                    string n = h == null ? "hand" : h.name;
                    Check(h != null, n + " exists");
                    if (h == null) continue;
                    Check(h.VisualOk(out var why), n + " model on, renderers and materials set", why);
                    var vp = cam.WorldToViewportPoint(h.transform.position);
                    Check(vp.z > 0.1f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f, n + " inside the camera view", vp.ToString("F2"));
                }
                yield return Shot(cam, "hands-" + scene);

                var tp = rig.Teleport;
                Check(tp != null, "teleporter exists");
                if (tp != null)
                {
                    var head = cam.transform.position;
                    var fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                    Vector3 good = Vector3.zero, aimDir = fwd;
                    bool found = false;
                    for (int yaw = 0; yaw < 360 && !found; yaw += 45)
                        foreach (float pitch in new[] { 5f, 15f, 30f })
                        {
                            var flat = Quaternion.AngleAxis(yaw, Vector3.up) * fwd;
                            var d = Quaternion.AngleAxis(pitch, Vector3.Cross(Vector3.up, flat)) * flat; // tilts downward
                            if (tp.TryAim(head, d, out var p)) { good = p; aimDir = d; found = true; break; }
                        }
                    Check(found, "an aim at the floor is accepted", found ? good.ToString("F2") : "none of 24 aims landed on valid floor");
                    Check(!tp.TryAim(head, Vector3.up, out _), "aiming at the sky is refused");
                    tp.Filter = _ => false;
                    Check(found && !tp.TryAim(head, aimDir, out _), "a scenario Filter can refuse a target");
                    tp.Filter = null;
                    if (found)
                    {
                        tp.Preview(rig.Right.transform.position, aimDir);
                        yield return null;
                        yield return Shot(cam, "teleport-" + scene);
                        tp.HidePreview();
                        var before = cam.transform.position;
                        tp.TeleportTo(good, false);
                        var after = cam.transform.position;
                        Check(Mathf.Abs(after.x - good.x) < 0.05f && Mathf.Abs(after.z - good.z) < 0.05f, "teleport puts the head over the target", before.ToString("F2") + " -> " + after.ToString("F2"));
                        float yawBefore = cam.transform.eulerAngles.y;
                        tp.SnapTurnBy(rig.SnapTurnDegrees);
                        Check(Mathf.Abs(Mathf.DeltaAngle(yawBefore, cam.transform.eulerAngles.y) - rig.SnapTurnDegrees) < 0.5f, "snap turn rotates the view by " + rig.SnapTurnDegrees + " degrees");
                        Check(Mathf.Abs(cam.transform.position.x - after.x) < 0.01f && Mathf.Abs(cam.transform.position.z - after.z) < 0.01f, "snap turn keeps the head in place");
                    }
                }
            }
            Debug.Log(report.ToString());
            Debug.Log("[handcheck] " + (failed ? "FAILED" : "PASSED") + " in " + scene);
            CaptureDriver.Quit(failed ? 3 : 0);
        }

        IEnumerator Shot(Camera main, string name)
        {
            yield return new WaitForEndOfFrame();
            var go = new GameObject("Hand Check Camera");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Destroy(rt);
            Destroy(tex);
            Destroy(go);
            Debug.Log("[handcheck] wrote " + name + ".png");
        }
    }
}
