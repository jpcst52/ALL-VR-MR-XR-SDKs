using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using PittBradford.XR;

/// <summary>
/// Realism pass for the hub scenes: ambient room tone, a pump hum that follows the scenario's pump, and light
/// post-processing (tone mapping, bloom, vignette). Press P on the desktop to switch post-processing off and on.
/// The effects are deliberately mild; check the frame rate on the headset before leaving them on.
/// </summary>
public class HubPolish : MonoBehaviour
{
    /// <summary>Set by the editor builder (the post-processing package's resources asset).</summary>
    public PostProcessResources Resources;
    public Vector3 PumpPosition = new Vector3(2.85f, 0.42f, 1.2f);

    /// <summary>Where the university plate goes, relative to the rig start: metres to the right, forward, and height.</summary>
    public Vector3 BrandOffset = new Vector3(-2.4f, 2.0f, 2.8f);
    public bool ShowBrand = true;
    /// <summary>When set, the plate goes at this world position instead (facing -Z, i.e. towards players south of it).</summary>
    public bool UseWorldBrand;
    public float BrandWidth = 1.7f;
    public Vector3 BrandWorld;

    /// <summary>
    /// Render budget (set by the builder). In the built-in forward pipeline every extra pixel light is another pass over every object in its
    /// range; the scenes' fill lights barely show, so only the sun is pixel-lit by default (the rest are per-vertex). Editor measurement:
    /// the warehouse went from about 6,700 to about 1,700 SetPass calls with this one setting (see the README table).
    /// </summary>
    public int PixelLights = 1;
    public float ShadowDistance = 25f;

    int oldPixelLights, oldCascades; float oldShadowDistance; bool qualityChanged;
    bool brandPlaced;
    PostProcessLayer layer;
    bool postFxTried;
    PostProcessVolume volume;
    AudioSource hum;
    HydroTest scenario;
    FieldInfo pumpField;

    void Start()
    {
        oldPixelLights = QualitySettings.pixelLightCount; oldShadowDistance = QualitySettings.shadowDistance; oldCascades = QualitySettings.shadowCascades;
        qualityChanged = true;
        QualitySettings.pixelLightCount = Mathf.Min(oldPixelLights, PixelLights);
        QualitySettings.shadowDistance = Mathf.Min(oldShadowDistance, ShadowDistance);
        QualitySettings.shadowCascades = Mathf.Min(oldCascades, 2);

        // The hall is big; lift the ambient light so equipment away from the lamps is not lost in the dark
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.62f, 0.64f, 0.7f), 0.8f);

        // Room tone: low filtered noise, heard everywhere
        var room = new GameObject("Room tone").transform;
        room.SetParent(transform, false);
        P.Speaker(room, P.Noise("room-tone", 4f, 0.05f), true, 0.35f, 0f).Play();

        scenario = FindFirstObjectByType<HydroTest>();
        if (scenario == null) return; // lobby and other scenes: room tone only
        pumpField = typeof(HydroTest).GetField("pump", BindingFlags.NonPublic | BindingFlags.Instance);

        // Pump hum: a low tone at the pump, louder while the pump runs
        var pumpGo = new GameObject("Pump hum").transform;
        pumpGo.SetParent(transform, false);
        pumpGo.position = PumpPosition;
        hum = P.Speaker(pumpGo, P.Tone("pump-hum", 92f, 1f, 0.5f, 0f), true, 0f, 1f);
        hum.minDistance = 1.5f;
        hum.maxDistance = 14f;
        hum.Play();
    }

    void Update()
    {
        if (hum != null)
        {
            bool running = scenario != null && pumpField != null && (bool)pumpField.GetValue(scenario);
            hum.volume = Mathf.MoveTowards(hum.volume, running ? 0.45f : 0.05f, Time.deltaTime * 0.8f);
        }
        if (layer == null) TryAttachPostFx();
        var kb = Keyboard.current;
        if (layer != null && kb != null && kb.pKey.wasPressedThisFrame) layer.enabled = !layer.enabled;
    }

    void PlaceBrand(XRRig rig)
    {
        if (!ShowBrand) return;
        var rt = rig.transform;
        var pos = rt.position + rt.right * BrandOffset.x + rt.forward * BrandOffset.z + Vector3.up * BrandOffset.y;
        var plate = Ui.BrandPlate(transform, Vector3.zero, BrandWidth);
        if (UseWorldBrand) { plate.position = BrandWorld; return; }
        plate.position = pos;
        // Panels face -Z, so look away from the player
        var away = pos - rt.position; away.y = 0f;
        plate.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    void TryAttachPostFx()
    {
        var rig = FindFirstObjectByType<XRRig>();
        if (rig == null || rig.Camera == null) return;
        if (!brandPlaced) { PlaceBrand(rig); brandPlaced = true; }
        if (Resources == null) return;
        var cam = rig.Camera;
        // the camera may already carry a PostProcessLayer (only one is allowed per object): use it, and never try twice
        if (postFxTried) return;
        postFxTried = true;
        layer = cam.gameObject.GetComponent<PostProcessLayer>();
        if (layer == null) layer = cam.gameObject.AddComponent<PostProcessLayer>();
        if (layer == null) { Debug.LogWarning("[polish] could not get a PostProcessLayer on " + cam.name + "; post-processing skipped"); return; }
        layer.Init(Resources);
        layer.volumeTrigger = cam.transform;
        layer.volumeLayer = 1 << 0;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.None; // the camera already uses MSAA

        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        var grading = profile.AddSettings<ColorGrading>();
        grading.tonemapper.Override(Tonemapper.ACES);
        grading.postExposure.Override(0.45f);
        grading.contrast.Override(8f);
        grading.saturation.Override(6f);
        var bloom = profile.AddSettings<Bloom>();
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(1.1f);
        var vignette = profile.AddSettings<Vignette>();
        vignette.intensity.Override(0.18f);

        volume = PostProcessManager.instance.QuickVolume(0, 100f, profile.settings.ToArray());
        volume.gameObject.transform.SetParent(transform, false);
        volume.isGlobal = true;
    }

    void OnDestroy()
    {
        if (volume != null) Destroy(volume.gameObject);
        if (!qualityChanged) return; // leave the project's quality settings as they were
        QualitySettings.pixelLightCount = oldPixelLights; QualitySettings.shadowDistance = oldShadowDistance; QualitySettings.shadowCascades = oldCascades;
    }
}
