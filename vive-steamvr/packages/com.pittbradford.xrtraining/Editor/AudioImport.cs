using UnityEditor;
using UnityEngine;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Import rules for the kit's recorded samples (Runtime/Resources/Audio): mono (they are played as 3D sources),
    /// short clips decompressed on load (no decode cost when played), loops and ambience compressed in memory.
    /// Names listed in <see cref="Loops"/> are the loop catalogue entries (see Sfx).
    /// </summary>
    public class AudioImport : AssetPostprocessor
    {
        static readonly string[] Loops =
        {
            "water_pour", "pump_hum", "alarm_siren", "monitor_alarm", "radio_static", "forklift_engine_loop", "forklift_beep_reverse",
            "wind_loop", "hvac_loop", "plant_drone_loop", "flare_roar_loop", "pressure_tone",
            "amb_lobby", "amb_hydro", "amb_refinery", "amb_warehouse", "amb_bench",
        };

        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("com.pittbradford.xrtraining/Runtime/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool loop = false;
            foreach (var l in Loops) if (file == l || file.StartsWith(l + "_")) loop = true;
            imp.forceToMono = true;
            imp.loadInBackground = false;
            var s = imp.defaultSampleSettings;
            s.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = s;
        }
    }
}
