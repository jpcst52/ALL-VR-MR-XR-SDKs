using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Cheap distance low-pass for a looping 3D source: the further the listener is, the darker the sound (air absorption).
    /// Added by <see cref="Sfx.Loop"/> to loop sources only (a handful per scene), updated about 8 times a second.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxFar : MonoBehaviour
    {
        public float Near = 1.5f, Far = 20f;
        AudioLowPassFilter lp;
        AudioListener listener;
        float nextCheck, nextFind, last = 22000f;

        void Awake() { lp = GetComponent<AudioLowPassFilter>(); }

        void Update()
        {
            if (Time.unscaledTime < nextCheck || lp == null) return;
            nextCheck = Time.unscaledTime + 0.12f;
            if (listener == null)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 1f;
                listener = FindFirstObjectByType<AudioListener>();
                if (listener == null) return;
            }
            float d = Vector3.Distance(listener.transform.position, transform.position);
            float t = Mathf.Clamp01((d - Near) / Mathf.Max(0.5f, Far - Near));
            float fc = Mathf.Lerp(18000f, 1800f, Mathf.Pow(t, 0.6f));
            if (Mathf.Abs(fc - last) > last * 0.03f) { lp.cutoffFrequency = fc; last = fc; }
        }
    }
}
