using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Put on an object (from code or in the editor) to keep it, and everything under it, out of a scene bake.
    /// Objects the bake leaves out are simply created at runtime, as before baking.
    /// </summary>
    [DisallowMultipleComponent]
    public class DontBake : MonoBehaviour { }
}
