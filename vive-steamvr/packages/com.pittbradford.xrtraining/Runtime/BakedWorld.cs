using System.Collections.Generic;
using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Marks a scene whose generated world has been baked into the scene file (menu Pitt-Bradford > Bake scene).
    /// The bake tool adds it to the scenario object. While it is present, the kit's object helpers (P.Node, P.Prim,
    /// P.Box, P.Cyl, P.Ball, P.Text, Ui.Panel, Ui.Tag, Ui.BrandPlate) adopt the saved objects instead of creating new ones.
    /// Remove it with Pitt-Bradford > Unbake scene (which also deletes the baked objects).
    /// </summary>
    [DisallowMultipleComponent]
    public class BakedWorld : MonoBehaviour
    {
        [Tooltip("Top-level objects written by the last bake. Unbake and re-bake delete exactly these.")]
        public List<GameObject> Roots = new List<GameObject>();
        [Tooltip("When the last bake ran (local time).")]
        public string BakedAt = "";
        [Tooltip("Number of GameObjects the last bake wrote into the scene.")]
        public int ObjectCount;
        [HideInInspector, Tooltip("Scene lighting before the first bake (JSON), restored by Unbake.")]
        public string LightingBefore = "";
    }
}
