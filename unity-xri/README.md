# XR Interaction Toolkit grab feedback

This example uses Unity's XR Interaction Toolkit (XRI) `XRGrabInteractable` component and its selection events. Grabbing the object changes its renderer color; releasing it restores the original color.

## Prerequisites and dependencies

- Unity 2022.3 LTS or newer.
- Unity packages:
  - **XR Interaction Toolkit** (`com.unity.xr.interaction.toolkit`) 3.0.x.
  - **Input System** (`com.unity.inputsystem`).
  - **XR Plug-in Management** (`com.unity.xr.management`) and a provider suitable for the target device (for example, OpenXR).
- A compatible XR device/runtime and controller or hand interaction setup. For Editor-only practice, XRI's XR Device Simulator sample can provide simulated input.

Official references:

- [XR Interaction Toolkit manual](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html)
- [XR Grab Interactable manual](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/xr-grab-interactable.html)
- [XR Grab Interactable API](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/api/UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable.html)
- [Unity XR Plug-in Management](https://docs.unity3d.com/Packages/com.unity.xr.management@4.4/manual/index.html)

## Setup

1. Install XRI, the Input System, and XR Plug-in Management. Enable and configure a provider for the target platform.
2. Copy `Assets/Scripts/GrabColorFeedback.cs` into the same path in the project.
3. Add an **XR Origin (VR)** and **XR Interaction Manager** to the scene. Configure controller or hand interactors with the XRI input actions (the Starter Assets sample is an option).
4. Create a cube with a Collider and Rigidbody, add `XR Grab Interactable`, then add `GrabColorFeedback`. Assign its renderer if it is not on the same object.
5. Run with a connected headset/controller or the XR Device Simulator sample. Select/grab the cube to see the color feedback.

The component subscribes to XRI 3.0's `selectEntered` and `selectExited` events. It does not create an XR Origin, interaction manager, input actions, or platform loader automatically.
