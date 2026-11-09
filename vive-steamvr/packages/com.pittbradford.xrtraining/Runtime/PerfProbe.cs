using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Development helper: samples the render profiler counters (batches, SetPass calls, draw calls, triangles, vertices, shadow casters) for a
/// number of frames in play mode and writes the average and maximum to a text file. These are CPU-side render counts from the editor's
/// Game view; they are not a frame rate and say nothing about the GPU time on a headset.
/// Start from play mode with PerfProbe.Begin(path, label, frames).
/// </summary>
public class PerfProbe : MonoBehaviour
{
    string path, label; int frames, seen;
    ProfilerRecorder setPass, draw, batches, tris, verts, shadow;
    double[] sum = new double[6]; double[] max = new double[6]; System.Collections.Generic.List<double>[] all = { new(), new(), new(), new(), new(), new() };
    float t0;
    int gameCams, prevGameCams; bool sceneCam, prevSceneCam; int skipped;
    public static bool Finished;

    /// <summary>Headless use: start the editor or a player with <c>-pbPerf &lt;file&gt;</c> (and <c>-pbScene</c> for the editor runner); it appends one line and exits.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        string file = null;
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-pbPerf") file = args[i + 1];
        if (file == null) return;
        var go = new GameObject("Perf boot");
        DontDestroyOnLoad(go);
        go.AddComponent<PerfBoot>().file = file;
        PittBradford.XR.CaptureDriver.AddWatchdog(go);
    }

    class PerfBoot : MonoBehaviour
    {
        public string file;
        System.Collections.IEnumerator Start()
        {
            Debug.Log("[perf] boot, file " + file);
            yield return new WaitForSeconds(4f);
            PittBradford.XR.CaptureDriver.StopXrForPerf();
            yield return null;
            Debug.Log("[perf] begin");
            Begin(file, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, 120);
            while (!Finished) yield return null;
            yield return null;
            PittBradford.XR.CaptureDriver.QuitForPerf();
        }
    }

    // The counters cover every camera rendered in a frame, including the editor's Scene view; only frames in which just the Game camera
    // rendered are counted (the others are skipped), so the numbers are for one view.
    void OnPre(Camera c)
    {
        if (c.cameraType == CameraType.SceneView || c.cameraType == CameraType.Preview) sceneCam = true;
        else if (c.cameraType == CameraType.Game) gameCams++;
    }

    public static void Begin(string path, string label, int frames)
    {
        Finished = false;
        var go = new GameObject("Perf probe (dev)");
        var p = go.AddComponent<PerfProbe>(); p.path = path; p.label = label; p.frames = frames;
    }

    void OnEnable()
    {
        setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        verts = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        shadow = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count");
        t0 = Time.realtimeSinceStartup;
        Camera.onPreRender += OnPre;
    }

    void OnDisable()
    {
        Camera.onPreRender -= OnPre;
        setPass.Dispose(); draw.Dispose(); batches.Dispose(); tris.Dispose(); verts.Dispose(); shadow.Dispose();
    }

    double Med(int i) { var l = new System.Collections.Generic.List<double>(all[i]); l.Sort(); return l.Count == 0 ? 0 : l[l.Count / 2]; }

    void Update()
    {
        if (Time.frameCount % 50 == 0) File.AppendAllText(path + ".dbg", $"frame {Time.frameCount}: gameCams={prevGameCams} sceneCam={prevSceneCam}" + System.Environment.NewLine);
        bool clean = prevGameCams >= 1 && !prevSceneCam;
        prevGameCams = gameCams; prevSceneCam = sceneCam; gameCams = 0; sceneCam = false;
        if (Time.frameCount < 30) return; // let the scene settle (people, sky, shader warm-up)
        if (!clean) { if (++skipped < 4000) return; seen = frames; } // give up waiting for a Game-camera-only frame
        var v = new double[] { setPass.LastValue, draw.LastValue, batches.LastValue, tris.LastValue, verts.LastValue, shadow.LastValue };
        for (int i = 0; i < 6; i++) { sum[i] += v[i]; all[i].Add(v[i]); if (v[i] > max[i]) max[i] = v[i]; }
        if (++seen < frames) return;
        int lights = 0, pix = 0;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.enabled && l.gameObject.activeInHierarchy) { lights++; if (l.type != LightType.Directional) pix++; }
        var sb = new StringBuilder();
        sb.Append($"{label}: frames={seen} (skipped {skipped} frames with other cameras) median/avg/max setPass={Med(0):F0}/{sum[0] / seen:F0}/{max[0]:F0} drawCalls={Med(1):F0}/{sum[1] / seen:F0}/{max[1]:F0} batches={Med(2):F0}/{sum[2] / seen:F0}/{max[2]:F0} ");
        sb.Append($"triangles={Med(3):F0}/{sum[3] / seen:F0}/{max[3]:F0} vertices={Med(4):F0}/{sum[4] / seen:F0}/{max[4]:F0} shadowCasters={Med(5):F0}/{sum[5] / seen:F0}/{max[5]:F0} lights={lights} (non-directional {pix}) ");
        sb.Append($"editorFps={seen / (Time.realtimeSinceStartup - t0):F0}\n");
        File.AppendAllText(path, sb.ToString());
        Finished = true;
        Destroy(gameObject);
    }
}
