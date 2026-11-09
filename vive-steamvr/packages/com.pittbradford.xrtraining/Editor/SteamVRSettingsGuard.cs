#if PB_STEAMVR
using System;
using UnityEditor;
using UnityEngine;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// SteamVR's "Recommended project settings" window (Assets/SteamVR/Editor/SteamVR_UnitySettingsWindow.cs) opens whenever a Player Setting
    /// differs from its list. This applies those values whenever the editor is NOT playing (Player Settings edited while playing are not reliably
    /// kept), and closes the window once nothing differs. Only active in projects that define PB_STEAMVR (the SteamVR projects).
    /// </summary>
    [InitializeOnLoad]
    static class SteamVRSettingsGuard
    {
        static int tries;

        static SteamVRSettingsGuard() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (tries++ % 30 != 0) return;   // check twice a second or so, for the whole editor session (the window can open late, after a domain reload)
            // Settings the window also checks but that these projects deliberately differ on (multi-pass stereo, the project's own render path):
            // tell the window to ignore them, as the Ignore button does. The window then has nothing to complain about.
            foreach (var key in new[] { "Single-Pass Stereo Rendering", "Rendering Path", "Display Resolution Dialog", "Build Target" })
                if (!EditorPrefs.HasKey("ignore." + key)) EditorPrefs.SetBool("ignore." + key, true);
            bool changed = false;
            if (PlayerSettings.d3d11FullscreenMode != D3D11FullscreenMode.FullscreenWindow) { PlayerSettings.d3d11FullscreenMode = D3D11FullscreenMode.FullscreenWindow; changed = true; }
            if (PlayerSettings.SplashScreen.show) { PlayerSettings.SplashScreen.show = false; changed = true; }
            if (PlayerSettings.defaultScreenWidth != 1024 || PlayerSettings.defaultScreenHeight != 768) { PlayerSettings.defaultScreenWidth = 1024; PlayerSettings.defaultScreenHeight = 768; changed = true; }
            if (!PlayerSettings.resizableWindow) { PlayerSettings.resizableWindow = true; changed = true; }
            if (!PlayerSettings.runInBackground) { PlayerSettings.runInBackground = true; changed = true; }
            if (!PlayerSettings.visibleInBackground) { PlayerSettings.visibleInBackground = true; changed = true; }
            if (PlayerSettings.fullScreenMode != FullScreenMode.FullScreenWindow) { PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; changed = true; }
            if (PlayerSettings.colorSpace != ColorSpace.Linear) { PlayerSettings.colorSpace = ColorSpace.Linear; changed = true; }
            if (!PlayerSettings.gpuSkinning) { PlayerSettings.gpuSkinning = true; changed = true; }
            if (changed) { AssetDatabase.SaveAssets(); Debug.Log("[steamvr] applied SteamVR's recommended Player Settings"); }

            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (w != null && w.GetType().Name == "SteamVR_UnitySettingsWindow") { try { w.Close(); } catch (Exception) { } }
        }
    }
}
#endif
