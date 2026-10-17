# Spatial Atlas for Apple Vision Pro

A native visionOS immersive-space example built with SwiftUI and RealityKit. Enter the immersive space to see a small network of world-anchored landmark spheres and connecting bars. It uses generated geometry and has no assets, credentials, or network dependency.

## Xcode setup and run

1. Open `apple-spatial/SpatialAtlas.xcodeproj` in Xcode 16 or later on a Mac with the visionOS 2 SDK installed.
2. Select the `SpatialAtlas` scheme and a visionOS simulator or Apple Vision Pro destination, then build and run. Select **Enter the immersive atlas** in the app window. Configure a development team in Signing & Capabilities when deploying to a physical device.
3. The simulator is useful for checking the SwiftUI flow and rendered scene. Use Apple Vision Pro to experience the immersive scene at room scale; the atlas is placed relative to its world anchor and is not a room-mapping or persistence demo.

**This project cannot be built on Windows.** Xcode, the visionOS SDK, and Apple’s simulator are macOS-only.

## Official references

- [Developing for visionOS](https://developer.apple.com/visionos/)
- [SwiftUI `ImmersiveSpace`](https://developer.apple.com/documentation/swiftui/immersivespace)
- [RealityKit `RealityView`](https://developer.apple.com/documentation/realitykit/realityview)
- [RealityKit `AnchorEntity`](https://developer.apple.com/documentation/realitykit/anchorentity)
