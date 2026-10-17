#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
[OpenXRFeature(
    UiName = "Instance Lifecycle Example",
    Desc = "Logs when the Unity OpenXR provider creates an OpenXR instance.",
    Company = "All VR MR AR SDK Examples",
    Version = "1.0.0",
    FeatureId = "com.allvrmrsdks.openxr.instance-lifecycle",
    BuildTargetGroups = new[] { BuildTargetGroup.Standalone })]
#endif
public sealed class OpenXRInstanceLifecycleFeature : OpenXRFeature
{
    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        Debug.Log($"OpenXR instance created by the active runtime (handle: {xrInstance}).");
        return true;
    }
}
