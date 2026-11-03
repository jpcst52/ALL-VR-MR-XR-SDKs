# Credits

Third-party material used by this repository, and where each item is credited in detail. Nothing here is loaded from the network by the browser examples; libraries, fonts, models and skies are bundled so the demo runs offline.

## Fonts (bundled in `vendor/fonts/`)

| Font | Author | License |
|---|---|---|
| League Gothic | The League of Moveable Type | SIL Open Font License 1.1 |
| Merriweather | Sorkin Type | SIL Open Font License 1.1 |
| Roboto | Christian Robertson (Google) | Apache License 2.0 |

Latin subsets, packaged by Fontsource. The brand guidelines of the University of Pittsburgh at Bradford approve these web fonts (League Gothic stands in for Alternate Gothic Compressed). See also [vendor/README.md](vendor/README.md).

## Live weather

Wind and temperature in Refinery Rounds (project 02, the hub copy) and the hydrostatic test bay conditions board come from **Weather data by [Open-Meteo.com](https://open-meteo.com/)**, licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The in-scene board names the source, and says "simulated (no network)" when it falls back to built-in values.

## People in the hub scenes

Realistic people in the local hub project come from the **Microsoft Rocketbox Avatar Library**, MIT license, https://github.com/microsoft/Microsoft-Rocketbox. Cite: Gonzalez-Franco, M., et al. (2020). The Rocketbox Library and the Utility of Freely Available Rigged Avatars. *Frontiers in Virtual Reality, 1*, 561558. https://doi.org/10.3389/frvir.2020.561558. The avatar files are imported locally and are not committed.

## Unity Asset Store packs (local only)

The hub project is dressed with Asset Store packs (a warehouse set, a sci-fi warehouse kit, a modern office, a shipping container, a warehouse pack and several courtyard prop folders; the exact list is in [vive-steamvr/hub/README.md](vive-steamvr/hub/README.md)). Their licenses do not allow redistributing the files, so the packs are never committed and the hub scenes are built on the lab PC that has them. The stand-alone Unity projects 01 to 04 contain no pack art.

Screenshots and gallery images in the repository that show the hub scenes are renders of those packs and of the Rocketbox people. They are published as images only (no source files). If a pack's terms do not allow that, remove the images listed in [docs/COMPLIANCE-SWEEP.md](docs/COMPLIANCE-SWEEP.md).

## 3D models, textures and skies (CC0)

- [vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/Resources/Audio/CREDITS.md](vive-steamvr/packages/com.pittbradford.xrtraining/Runtime/Resources/Audio/CREDITS.md): the Unity sound library (seven CC0 recordings by rubberduck from OpenGameArt; the rest is synthesized).
- [assets/models/CREDITS.md](assets/models/CREDITS.md): Practice Lab props and people (Poly Haven, Quaternius via Poly Pizza, KayKit).
- [multi-player/CREDITS.md](multi-player/CREDITS.md): multi-user demo models.
- [CREDITS-web-demos.md](CREDITS-web-demos.md): the single-SDK web demos.
- [assets/skies/CREDITS.md](assets/skies/CREDITS.md): sky images.

## Libraries (bundled in `vendor/`)

Three.js, Babylon.js, A-Frame and a QR-code generator, each under its own open-source license; versions and sources are in [vendor/README.md](vendor/README.md).

## University branding

The University of Pittsburgh at Bradford logo is published with the owner's approval; the brand guidelines and other logo variants are not in the repository.
