using System;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// A hook so scene generators (Configurator.CreateScene, the hub builders) can put the SteamVR rig into a scene they are about to save without
    /// referencing the SteamVR plugin. The SteamVR editor assembly (only present when PB_STEAMVR is defined) registers the placer.
    /// </summary>
    public static class SceneRigHook
    {
        /// <summary>Places Player, Teleporting and a TeleportArea in the active scene. Null when the project has no SteamVR plugin.</summary>
        public static Action Placer;

        public static void PlaceInActiveScene() { Placer?.Invoke(); }
    }
}
