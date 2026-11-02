# Hub project (Unity, built-in render pipeline)

The local Unity project where the training scenarios are dressed with higher-quality Asset Store art, and later where the lobby with portals to each scenario will live. It uses the shared kit in `../packages/com.pittbradford.xrtraining`.

**Status:** work in progress. The Hydrostatic Test Bay scene builds and plays in the Unity editor in desktop mode. It has not been tested on a Vive Pro 2. The lobby scene is not started.

![Hydrostatic Test Bay in the hub, Unity editor Game view (desktop mode, not a headset)](../../docs/images/hub-play.png)

## Third-party assets stay local

The hub imports Asset Store packs (IGBlocks warehouse, Sci-Fi Warehouse Kit, Modern Office, Shipping Container, Warehouse, and five Courtyard prop folders). Their licenses do not allow redistributing the files, so the `.gitignore` commits only our own code and settings (`Assets/Editor`, `Assets/Scripts`, `Assets/Settings`, `Assets/XR`). Scenes that reference the packs are saved under `Assets/Scenes/Local/`, which is also ignored. A fresh clone therefore has no art; the scenarios still run from primitives in the other projects.

## Build the Hydrostatic Test Bay scene

1. Open `vive-steamvr/hub` in Unity Hub with Unity **6000.3.20f1**, with the packs imported.
2. Run **Pitt-Bradford > Build hub scene: Hydrostatic Test Bay (local assets)**. `Assets/Editor/HubDressing.cs` builds the warehouse shell (a floor, four wall strips and a roof), plant props, a supervisor's corner and Courtyard props around the shared `HydroTest` scenario, then saves `Assets/Scenes/Local/HydrostaticTest_Hub.unity`. A missing pack is skipped with a log line.
3. Press Play. Controls are the same as in [../01-hydrostatic-test/README.md](../01-hydrostatic-test/README.md).

## Files

- `Assets/Editor/HubSetup.cs`: project configuration (built-in pipeline, OpenXR).
- `Assets/Editor/HubDressing.cs`: the scene builders.
- `Packages/manifest.json`: packages, including the kit by `file:` path.

## Known limits

- The shell is about 39 m by 37 m, much larger than the test area, so the equipment sits in the middle of a big empty hall. Lighting, audio and post-processing have not been tuned yet.
- Desktop editor screenshots only; nothing here has been run on a headset.
