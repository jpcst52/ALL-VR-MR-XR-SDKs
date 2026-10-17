# Unity OpenXR instance lifecycle example

This is a Unity-ready example script, not a complete Unity project. It demonstrates a small, real Unity OpenXR provider feature: `OpenXRInstanceLifecycleFeature` derives from `OpenXRFeature` and receives the runtime's `OnInstanceCreate` callback. When enabled, it logs the instance handle and allows initialization to continue. It does not call raw OpenXR extension functions.

## Prerequisites and dependencies

- Unity 2022.3 LTS or newer.
- Unity packages:
  - **XR Plug-in Management** (`com.unity.xr.management`).
  - **OpenXR Plugin** (`com.unity.xr.openxr`), version 1.14.x or a compatible release for your Unity editor.
- A PC OpenXR runtime and a headset supported by that runtime (for example SteamVR or Meta Quest Link); the runtime must be installed and selected as the system's active OpenXR runtime.

Official references:

- [Unity OpenXR Plugin manual](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.14/manual/index.html)
- [OpenXR Features manual](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.14/manual/features.html)
- [XR Plug-in Management manual](https://docs.unity3d.com/Packages/com.unity.xr.management@4.4/manual/index.html)
- [Khronos OpenXR](https://www.khronos.org/openxr/)

## Setup

1. Create or open a Unity project and install the packages above through **Window > Package Manager**.
2. Copy `Assets/Scripts/OpenXRInstanceLifecycleFeature.cs` into the same path in the project.
3. Under **Edit > Project Settings > XR Plug-in Management**, enable **OpenXR** for the **PC, Mac & Linux Standalone** target.
4. In **Project Settings > XR Plug-in Management > OpenXR**, select the interaction profile(s) supported by your controllers.
5. The feature is registered in the OpenXR feature list as **Instance Lifecycle Example**. Enable it under **OpenXR > Features**.
6. Build and run with a headset connected and the intended OpenXR runtime active. The Unity Console/player log should report the instance creation callback.

No scene object is needed: OpenXR features are selected in project settings and invoked by the OpenXR provider. This sample is intentionally limited to that callback; it does not implement input, rendering, or runtime selection.
