using UnityEngine;

namespace PittBradford.XR
{
    // In its own file so baked scenes can save it (Unity serializes a MonoBehaviour only from a file of the same name).
    /// <summary>Turns a <see cref="Ui.Tag"/> to face the viewer and keeps its leader line on the anchor.</summary>
    public class TagFacing : MonoBehaviour
    {
        /// <summary>Set by the screenshot driver so tags face the capture camera instead of the player.</summary>
        public static Transform ViewOverride;
        public Transform Anchor, Tag;
        public Vector3 AnchorLocal;
        public LineRenderer Line;

        void LateUpdate()
        {
            var cam = ViewOverride != null ? ViewOverride : (Camera.main != null ? Camera.main.transform : null);
            if (cam != null)
            {
                var away = Tag.position - cam.position;
                away.y = 0f;
                // Panels face -Z, so look in the direction pointing away from the viewer
                if (away.sqrMagnitude > 0.0001f) Tag.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
            var anchorWorld = Anchor.TransformPoint(AnchorLocal);
            // End the line at the plate's near edge instead of its centre
            Line.SetPosition(0, Tag.position - Vector3.up * 0.04f);
            Line.SetPosition(1, anchorWorld);
        }
    }
}
