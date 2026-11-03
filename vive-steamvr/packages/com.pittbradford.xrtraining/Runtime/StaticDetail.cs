using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Marks a group whose meshes never move, scale or change material at runtime (racking, walls, shelves, clutter, signs).
    /// Add it from code with <see cref="P.Static"/>. After a bake, the render-budget pass (Pitt-Bradford > Render budget, run
    /// automatically at the end of a bake) marks every renderer under the group batching-static, so Unity merges them into a few
    /// draw calls when the scene loads. Interactive parts (anything with a Rigidbody, Grabbable, Pressable, SnapZone, Animator,
    /// light, particle system or another script) are skipped by the pass, so a group can safely contain a few of them.
    /// The scene stays fully editable: nothing is merged on disk, and removing the component and the flags undoes it.
    /// </summary>
    [DisallowMultipleComponent]
    public class StaticDetail : MonoBehaviour
    {
        [Tooltip("Empty: everything under this object is static. Otherwise only the direct children with these names (and what is under them).")]
        public string[] OnlyNames = new string[0];
    }
}
