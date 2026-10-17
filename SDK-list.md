# VR, MR, and AR SDKs

This list focuses on current technologies that could have their own small example project. "Example idea" describes a feature the project could demonstrate; it does not imply the SDK is already integrated.

## Account and cost overview

| Technology | Account, key, or license considerations |
|---|---|
| OpenXR | No vendor account or API key for the standard. A compatible runtime/device is needed to run an example. |
| Meta XR SDK | Meta developer account and Quest developer mode for headset testing; an app ID may be needed for platform services. Meta SDK terms apply. |
| SteamVR | No Steamworks account for local experimentation. Steamworks onboarding and current publishing requirements apply to distributing on Steam. |
| WebXR, A-Frame, Three.js, Babylon.js | No vendor developer account or commercial SDK license generally required for development; device/browser requirements still apply. |
| ARCore | No separate commercial license for basic SDK use. Cloud-backed features such as Geospatial may require a Google Cloud project/API configuration and can have service billing. |
| ARKit / visionOS | Xcode and macOS required to build. Local development has different requirements from App Store distribution, which requires paid Apple Developer Program membership. |
| Unity XRI | Unity account/editor licensing and Unity package terms apply; check current plan eligibility for the project. |

Prices and terms change; these notes are an initial planning guide, not legal advice. Check the linked official vendor terms before committing to a paid feature or publishing.

## VR standards and runtimes

### OpenXR

An open, royalty-free API standard for XR devices and runtimes. It lets an application use a common interface instead of implementing a separate input and rendering path for every headset. OpenXR is an API specification, not a complete engine or headset SDK.

- **Example idea:** Observe the OpenXR instance lifecycle through Unity's OpenXR provider callback.
- **Access:** No vendor account or API key is required to use the standard. Running the example requires a compatible OpenXR runtime and device (or a suitable simulator).
- [Khronos OpenXR](https://www.khronos.org/openxr/)

### Meta XR SDK

Meta's Unity and native tools add Quest-specific capabilities such as hand interaction, passthrough, scene understanding, and platform services. Meta-specific extensions complement rather than replace OpenXR.

- **Example idea:** Preview the Quest passthrough camera stream in a Unity UI panel.
- **Access:** SDK packages are available for development, but using a Quest in developer mode requires a Meta developer account and compatible headset. Platform services may require an app ID. Follow Meta's SDK license and current platform terms.
- [Meta Horizon developer documentation](https://developers.meta.com/horizon/documentation/)

### SteamVR

Valve's PC VR runtime and developer tools support SteamVR headsets and PC VR applications. SteamVR also supports OpenXR applications; OpenVR is its older, Valve-specific API.

- **Example idea:** Toggle a Unity object's color from a SteamVR Boolean input action.
- **Access:** Local development requires the runtime and suitable PC VR hardware or a simulator. Steamworks onboarding and its current publishing requirements apply if distributing through Steam; that is separate from experimenting locally.
- [Steamworks VR documentation](https://partner.steamgames.com/doc/features/vr)

## Web XR and JavaScript 3D libraries

These are different things: WebXR is a browser API, while A-Frame, Three.js, and Babylon.js are libraries for building web 3D experiences. A web project can use WebXR and one of these libraries, but that does not make it an integration of native vendor SDKs.

### WebXR Device API

A web standard that allows compatible browsers to present immersive VR or AR sessions. Support depends on browser, device, and session mode; it is not available in every desktop browser.

- **Example idea:** Enter an immersive color-field session from a browser, with a desktop fallback.
- **Access:** No vendor developer account or SDK license is needed. Immersive sessions generally require a supported browser/device and a secure context (HTTPS or localhost).
- [MDN WebXR documentation](https://developer.mozilla.org/docs/Web/API/WebXR_Device_API)

### A-Frame

An open-source web framework for describing and composing 3D scenes with HTML-like elements. It can use WebXR for immersive presentation.

- **Example idea:** An animated Orbit Garden built from declarative A-Frame entities.
- [A-Frame documentation](https://aframe.io/docs/)

### Three.js

An open-source JavaScript 3D library for building custom WebGL/WebGPU scenes. WebXR presentation can be added through its renderer.

- **Example idea:** View the Prism Bloom scene and enter it in VR through Three.js WebXR support.
- [Three.js documentation](https://threejs.org/docs/)

### Babylon.js

An open-source 3D engine for web applications, with tools for rendering, interaction, and XR experiences.

- **Example idea:** Explore the Luminous Totem scene with Babylon.js's XR experience UI.
- [Babylon.js documentation](https://doc.babylonjs.com/)

## Mobile and spatial computing

### Google ARCore

Google's augmented reality SDK for supported Android devices. Its capabilities include motion tracking, environmental understanding, planes, depth on supported devices, and geospatial features.

- **Example idea:** Detect a real horizontal plane and place a cube at a camera hit-test location.
- **Access:** Core development requires a supported Android device and the appropriate ARCore SDK/tooling. Geospatial and other cloud-backed features can require a Google Cloud project, API configuration, and may be subject to service billing; verify current terms before using them.
- [ARCore documentation](https://developers.google.com/ar)

### Apple ARKit and visionOS SDK

Apple's ARKit provides augmented-reality capabilities on supported Apple devices. For Apple Vision Pro, spatial applications are built with the visionOS SDK and commonly use SwiftUI and RealityKit alongside ARKit where appropriate.

- **Example idea:** Enter an immersive SwiftUI/RealityKit spatial atlas with world-anchored landmarks.
- **Access:** Apple SDKs and Xcode are available for development, but building requires macOS and Xcode. A compatible Apple device or simulator is needed to test device-specific behavior. App Store distribution requires Apple Developer Program membership; basic local development has different requirements.
- [ARKit documentation](https://developer.apple.com/documentation/arkit)
- [visionOS documentation](https://developer.apple.com/visionos/)

## Unity interaction and engine tooling

### Unity XR Interaction Toolkit (XRI)

Unity's component-based interaction toolkit for building XR interactions such as grabbing, pointing, sockets, and locomotion. It works with supported XR providers, including OpenXR; it is not itself a headset runtime.

- **Example idea:** Highlight an object while it is selected by an XRI grab interactor.
- **Access:** Requires Unity and the relevant Unity packages. Unity's own editor licensing applies; check the current Unity plan and package terms for the intended use.
- [XR Interaction Toolkit documentation](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest)

## Planning notes

- The Apple example can include real Swift/SwiftUI/RealityKit/ARKit source code in this repository even though it cannot be built or device-tested from Windows. Building and testing it will require access to macOS and Xcode.
- Native SDK examples should use their actual APIs and be kept in platform-appropriate project folders. A browser/WebXR demo can be a separate project, but should not be presented as using a native SDK.
- Account requirements, pricing, supported hardware, and vendor terms can change. Recheck official documentation before adding credentials, committing SDK packages, or publishing an example.
