using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Runs the keyboard-and-mouse self-test (Runtime/SteamVR/DeskSelfTest.cs) in the EDITOR's Play mode, the way the owner uses the scenes, and exits
    /// with 0 (PASS) or 3 (FAIL). From a command line, with SteamVR (simulated headset) running and no editor open on the project:
    ///   Unity.exe -projectPath P -executeMethod PittBradford.XR.EditorTools.DeskSelfTestRunner.RunAndExit -pbScenes Assets/Scenes/X.unity -pbDeskSelfTest -logFile F
    /// The test lines are in the log ("[desktest]"). Not for the hub editor the owner has open: use the menu there.
    /// </summary>
    [InitializeOnLoad]
    public static class DeskSelfTestRunner
    {
        const string ExitKey = "pbDeskSelfTestExit";

        static DeskSelfTestRunner()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ExitKey, false)) return;
                SessionState.SetBool(ExitKey, false);
                string result = SessionState.GetString("pbDeskSelfTestResult", "none");
                Debug.Log("[desktest] editor Play finished: " + result);
                EditorApplication.Exit(result == "PASS" ? 0 : 3);
            };
        }

        [MenuItem("Pitt-Bradford/Desk self-test: run in Play mode (needs -pbDeskSelfTest on the command line)", false, 46)]
        public static void RunMenu() => Run(false);

        public static void RunAndExit() => Run(true);

        static void Run(bool exit)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++)
                if (a[i] == "-pbScenes") { EditorSceneManager.OpenScene(a[i + 1].Split(';')[0], OpenSceneMode.Single); break; }
            SessionState.SetString("pbDeskSelfTestResult", "none");
            SessionState.SetBool(ExitKey, exit);
            EditorApplication.EnterPlaymode();
        }
    }
}
