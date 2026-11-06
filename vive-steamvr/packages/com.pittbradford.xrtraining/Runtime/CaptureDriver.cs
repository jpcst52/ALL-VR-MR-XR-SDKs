using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>Implemented by a scenario to stage and photograph itself for the README screenshots.</summary>
    public interface IShotScript
    {
        IEnumerator Shots(ShotApi api);
    }

    public class ShotApi
    {
        readonly string dir;
        readonly Camera cam;
        readonly int width, height;
        public int Count { get; private set; }

        internal ShotApi(string dir, Camera source, int width, int height)
        {
            this.dir = dir;
            this.width = width;
            this.height = height;
            TagFacing.ViewOverride = null;
            var go = new GameObject("Capture Camera");
            cam = go.AddComponent<Camera>();
            cam.CopyFrom(source);
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.depth = 100;
            TagFacing.ViewOverride = go.transform;
        }

        public IEnumerator Shot(string name, Vector3 position, Vector3 lookAt, float fov = 62f)
        {
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            cam.fieldOfView = fov;
            yield return null;
            yield return new WaitForEndOfFrame();
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(tex);
            Count++;
            Debug.Log($"[capture] wrote {path}");
        }

        public IEnumerator Wait(float seconds) { yield return new WaitForSeconds(seconds); }
    }

    /// <summary>
    /// Runs automatically when the player is started with <c>-pbCapture &lt;folder&gt;</c>. It finds the scenario's
    /// <see cref="IShotScript"/>, takes the screenshots, and exits. Used to make the README images headlessly.
    /// </summary>
    public class CaptureDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string dir = Arg("-pbCapture");
            if (dir == null) return;
            UnityEngine.Random.InitState(1234); // same gas-puff jitter on every screenshot run
            var go = new GameObject("Capture Driver");
            DontDestroyOnLoad(go);
            go.AddComponent<CaptureDriver>().dir = dir;
        }

        string dir;

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(2.5f);
            int w = int.TryParse(Arg("-pbWidth"), out var pw) ? pw : 1920;
            int h = int.TryParse(Arg("-pbHeight"), out var ph) ? ph : 1080;
            IShotScript script = null;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is IShotScript s) { script = s; break; }
            var main = Camera.main;
            if (script == null || main == null)
            {
                Debug.LogError("[capture] no IShotScript or main camera found");
                Quit(2);
                yield break;
            }
            StopXr();
            yield return null;
#if UNITY_EDITOR
            while (UnityEditor.ShaderUtil.anythingCompiling) yield return null; // first renders come out cyan otherwise
#endif
            var api = new ShotApi(dir, main, w, h);
            yield return script.Shots(api);
            Debug.Log($"[capture] done, {api.Count} screenshots");
            Quit(0);
        }

        /// <summary>The editor can start the OpenXR mock runtime, which tilts and overrides cameras; screenshots want it off.</summary>
        internal static void StopXr()
        {
            try
            {
                var t = Type.GetType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
                var inst = t?.GetProperty("Instance")?.GetValue(null);
                var mgr = inst?.GetType().GetProperty("Manager")?.GetValue(inst);
                if (mgr == null) return;
                mgr.GetType().GetMethod("StopSubsystems")?.Invoke(mgr, null);
                mgr.GetType().GetMethod("DeinitializeLoader")?.Invoke(mgr, null);
                Debug.Log("[capture] XR stopped for screenshots");
            }
            catch (Exception e) { Debug.LogWarning("[capture] could not stop XR: " + e.Message); }
        }

        internal static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
