# Meta Quest passthrough camera preview

This Unity-ready example uses Meta's `Meta.XR.PassthroughCameraAccess` component to show the Quest headset's passthrough camera stream in a Unity UI `RawImage`. This is a Quest-specific camera capability, not a simulated WebXR feed or the environmental passthrough background.

## Prerequisites and dependencies

- Unity **6000.0.38f1 or newer**.
- Meta **Mixed Reality Utility Kit (MRUK) v81 or newer**, which supplies `Meta.XR.PassthroughCameraAccess`.
- Unity UI (`com.unity.ugui`), used by this example's `RawImage` and `Text` controls.
- Meta XR Core SDK project setup with passthrough enabled.
- A physical **Meta Quest 3 or Quest 3S** running Horizon OS v74 or newer; the example requires Android deployment. The sample project states that preview also works through Meta Horizon Link v2.1 or newer. The XR Simulator does not support this camera API.
- The app must declare and obtain `horizonos.permission.HEADSET_CAMERA`. Camera access is subject to the system permission prompt and device privacy policy.

Official references:

- [Meta Passthrough Camera API overview](https://developers.meta.com/horizon/documentation/unity/unity-pca-overview)
- [Meta Passthrough Camera API getting started](https://developers.meta.com/horizon/documentation/unity/unity-pca-documentation)
- [Official Meta Unity Passthrough Camera API samples](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples)
- [MRUK on the Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/meta-mr-utility-kit-272450)

## Setup

1. Install MRUK and configure the project for Quest using Meta's getting-started guide. Enable passthrough and configure the headset-camera permission in the Android/Meta project settings as documented by Meta.
2. Copy `Assets/Scripts/QuestPassthroughCameraPreview.cs` into the same path in the project.
3. In a scene, add a `PassthroughCameraAccess` component (from MRUK) and configure its camera position as desired.
4. Create a Canvas with a `RawImage` and a legacy UI `Text` status label. Add `QuestPassthroughCameraPreview` to a scene object and assign the camera component, image, and optional status label in the Inspector.
5. Deploy to a supported Quest device, grant camera permission when prompted, and verify the UI reports that the camera is streaming.

The script waits for `PassthroughCameraAccess.IsPlaying`, then assigns `GetTexture()` to the preview. It does not request permission itself; configure the app permission and use the documented Meta permission flow. No camera texture is available in the Editor simulator.
