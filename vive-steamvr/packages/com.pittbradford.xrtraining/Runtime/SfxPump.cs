using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Turns sounds that a background thread has finished generating into AudioClips (that part must run on the main thread),
    /// a few per frame, so no sound has to be synthesized in the middle of play. Created by <see cref="Sfx.PreloadBackground"/>;
    /// removes itself when everything is built.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxPump : MonoBehaviour
    {
        void Update()
        {
            if (!Sfx.PumpReady(3)) Destroy(gameObject);
        }
    }
}
