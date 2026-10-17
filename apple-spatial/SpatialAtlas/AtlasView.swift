import RealityKit
import SwiftUI
import UIKit
import simd

struct AtlasView: View {
    private let landmarks: [SIMD3<Float>] = [
        SIMD3(-0.48, 0.02, -1.35),
        SIMD3(-0.25, 0.25, -1.45),
        SIMD3(0.00, 0.05, -1.25),
        SIMD3(0.23, 0.32, -1.42),
        SIMD3(0.49, 0.08, -1.32),
        SIMD3(0.12, -0.20, -1.52)
    ]

    var body: some View {
        RealityView { content in
            let atlas = AnchorEntity(world: .zero)
            addConnections(to: atlas)
            addLandmarks(to: atlas)
            content.add(atlas)
        }
    }

    private func addLandmarks(to atlas: AnchorEntity) {
        for (index, position) in landmarks.enumerated() {
            let material = SimpleMaterial(
                color: index.isMultiple(of: 2) ? .cyan : .systemTeal,
                isMetallic: true
            )
            let node = ModelEntity(
                mesh: .generateSphere(radius: 0.045),
                materials: [material]
            )
            node.position = position
            atlas.addChild(node)
        }
    }

    private func addConnections(to atlas: AnchorEntity) {
        let links = [(0, 1), (1, 2), (2, 3), (3, 4), (2, 5), (5, 0)]
        for (startIndex, endIndex) in links {
            let start = landmarks[startIndex]
            let end = landmarks[endIndex]
            let delta = end - start
            let length = simd_length(delta)
            let link = ModelEntity(
                mesh: .generateBox(size: SIMD3(0.012, length, 0.012)),
                materials: [SimpleMaterial(color: .white, isMetallic: false)]
            )
            link.position = (start + end) / 2
            link.orientation = simd_quatf(
                from: SIMD3<Float>(0, 1, 0),
                to: simd_normalize(delta)
            )
            atlas.addChild(link)
        }
    }
}
