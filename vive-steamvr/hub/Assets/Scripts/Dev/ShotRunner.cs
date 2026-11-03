using System;
using System.Collections;
using System.IO;
using System.Reflection;
using PittBradford.XR;
using UnityEngine;

/// <summary>
/// Development helper for the live editor: runs a scenario's own scripted screenshot/self-test flow (IShotScript, the one
/// the stand-alone projects run with -pbCapture) inside the open editor without quitting it, and mirrors the
/// [selftest]/[awareness]/[capture] log lines to a text file. Start it from play mode with ShotRunner.Begin(folder).
/// </summary>
public class ShotRunner : MonoBehaviour
{
    string dir, logPath;
    public static bool Finished;

    public static void Begin(string folder)
    {
        Finished = false;
        var go = new GameObject("Shot runner (dev)");
        go.AddComponent<ShotRunner>().dir = folder;
    }

    void OnLog(string msg, string stack, LogType type)
    {
        if (msg.StartsWith("[selftest]") || msg.StartsWith("[awareness]") || msg.StartsWith("[capture]") || type == LogType.Error || type == LogType.Exception)
            File.AppendAllText(logPath, type + " " + msg + "\n");
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, "run.log");
        File.WriteAllText(logPath, "");
        Application.logMessageReceived += OnLog;
        yield return new WaitForSeconds(1.5f);
        IShotScript script = null;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb is IShotScript s) { script = s; break; }
        var main = Camera.main;
        if (script == null || main == null) { File.AppendAllText(logPath, "no script or camera\n"); Finished = true; yield break; }
        var api = (ShotApi)Activator.CreateInstance(typeof(ShotApi), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { dir, main, 1600, 900 }, null);
        yield return script.Shots(api);
        File.AppendAllText(logPath, "[shotrunner] done, " + api.Count + " screenshots\n");
        Application.logMessageReceived -= OnLog;
        Finished = true;
    }
}
