using System;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Lets another assembly supply the rig, the hands and the grabbing, without the kit knowing about it. The SteamVR plugin's Interaction System
    /// does this (see Runtime/SteamVR): its Player, hands, teleporting and Throwable replace the kit's own Input System rig on a Vive.
    /// With no backend (or when it returns null, for example on a desktop with no headset) the kit builds its own rig as before.
    /// </summary>
    public interface IRigBackend
    {
        /// <summary>Build the rig, or return null to use the kit's own.</summary>
        XRRig CreateRig(Vector3 position, float yawDegrees);
    }

    /// <summary>Button and haptic access for one hand, supplied by the backend.</summary>
    public interface IHandInput
    {
        bool Trigger { get; }
        bool Grip { get; }
        Vector2 Stick { get; }
        bool StickClick { get; }
        void Haptic(float amplitude, float seconds);
    }

    /// <summary>Lets the backend take over picking up and dropping a <see cref="Grabbable"/>.</summary>
    public interface IGrabBackend
    {
        void Setup(Grabbable grabbable);
    }

    public static class XRBackends
    {
        public static IRigBackend Rig;
        public static IGrabBackend Grab;
    }
}
