using System;
using UnityEngine;

namespace PittBradford.XR
{
    // In its own file so baked scenes can save it (Unity serializes a MonoBehaviour only from a file of the same name).
    /// <summary>A place a <see cref="Grabbable"/> with a matching id snaps into when released nearby.</summary>
    public class SnapZone : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<SnapZone> All = new System.Collections.Generic.List<SnapZone>();
        public string Id = "";
        public float Radius = 0.18f;
        public Grabbable Occupant { get; private set; }
        public event Action<SnapZone, Grabbable> Snapped;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Clear() { Occupant = null; }

        public static bool TrySnap(Grabbable g)
        {
            SnapZone best = null;
            float bestDist = float.MaxValue;
            foreach (var z in All)
            {
                if (z.Occupant != null || z.Id != g.SnapId) continue;
                float d = Vector3.Distance(z.transform.position, g.transform.position);
                if (d < z.Radius && d < bestDist) { best = z; bestDist = d; }
            }
            if (best == null) return false;
            g.transform.SetParent(best.transform, false);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            g.GetComponent<Rigidbody>().isKinematic = true;
            g.Snapped = best;
            best.Occupant = g;
            Sfx.Play("snap", best.transform.position, 0.8f);
            best.Snapped?.Invoke(best, g);
            return true;
        }
    }
}
