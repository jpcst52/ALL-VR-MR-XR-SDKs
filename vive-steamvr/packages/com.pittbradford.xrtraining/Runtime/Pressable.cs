using System;
using UnityEngine;

namespace PittBradford.XR
{
    // In its own file so baked scenes can save it (Unity serializes a MonoBehaviour only from a file of the same name).
    /// <summary>A world object you point at and press the trigger on (valves, buttons, switches).</summary>
    public class Pressable : MonoBehaviour
    {
        public event Action<Hand> Pressed;
        public bool Interactable = true;
        public string Label;
        Vector3 baseScale;
        bool hover;

        void Awake() { baseScale = transform.localScale; }
        public void SetHover(bool on) { hover = on; transform.localScale = on ? baseScale * 1.06f : baseScale; }
        public void Press(Hand h) { if (Interactable) { h.Haptic(0.3f, 0.05f); Pressed?.Invoke(h); } }
        void OnDisable() { if (hover) transform.localScale = baseScale; }
    }
}
