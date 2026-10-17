import SwiftUI

struct ContentView: View {
    @Environment(\.openImmersiveSpace) private var openImmersiveSpace
    @Environment(\.dismissImmersiveSpace) private var dismissImmersiveSpace

    var body: some View {
        VStack(spacing: 18) {
            Text("Spatial Atlas")
                .font(.largeTitle)
            Text("Explore a world-anchored constellation of spatial landmarks.")
                .multilineTextAlignment(.center)
            Button("Enter the immersive atlas") {
                Task {
                    await openImmersiveSpace(id: "SpatialAtlas")
                }
            }
            .buttonStyle(.borderedProminent)
            Button("Close immersive space") {
                Task {
                    await dismissImmersiveSpace()
                }
            }
            .buttonStyle(.bordered)
        }
        .padding(32)
        .frame(minWidth: 420, minHeight: 280)
    }
}
