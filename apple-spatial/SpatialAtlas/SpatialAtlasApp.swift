import SwiftUI

@main
struct SpatialAtlasApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }

        ImmersiveSpace(id: "SpatialAtlas") {
            AtlasView()
        }
    }
}
