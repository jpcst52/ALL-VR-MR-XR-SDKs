using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Quality settings for a PC headset (Vive Pro 2, 2448 x 2448 per eye), applied once before the first scene loads:
    /// 4x MSAA (edges and thin wires), anisotropic filtering forced on (floors and signs at a grazing angle), two shadow
    /// cascades with soft shadows and a 40 m shadow distance (at most), and a slightly higher LOD bias. None of these adds an object, a
    /// material or a draw call; they cost GPU time (MSAA and shadow filtering), which the editor CPU counters do not show.
    /// The hub's HubPolish may lower the shadow distance and pixel lights afterwards. Turn off with <see cref="Enabled"/>.
    /// </summary>
    public static class VrQuality
    {
        public static bool Enabled = true;
        public static int Msaa = 4;
        public static float ShadowDistance = 40f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (!Enabled) return;
            QualitySettings.antiAliasing = Msaa;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture.SetGlobalAnisotropicFilteringLimits(16, 16);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowDistance = Mathf.Clamp(QualitySettings.shadowDistance, 20f, ShadowDistance);
            QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 1.5f);
        }
    }
}
