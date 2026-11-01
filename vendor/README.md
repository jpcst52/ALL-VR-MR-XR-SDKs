# Vendored libraries

Everything the demos need to run is stored here so the examples work with **no internet connection** (a requirement for the demo room). Nothing is loaded from a CDN.

| Folder | Library | Version | License |
|---|---|---|---|
| `three/` | [Three.js](https://threejs.org) (`three.module.js` and the add-ons used by the examples) | 0.169.0 | MIT (`three/LICENSE`) |
| `babylon/` | [Babylon.js](https://www.babylonjs.com) | 7.42.0 | Apache-2.0 (`babylon/LICENSE.md`) |
| `aframe/` | [A-Frame](https://aframe.io) | 1.6.0 | MIT (`aframe/LICENSE`) |
| `fonts/` | Roboto, Merriweather, League Gothic (Latin subsets, via Fontsource) | see below | SIL Open Font License 1.1 |

The fonts are the approved web fonts from the University of Pittsburgh brand guidelines (League Gothic is the approved substitute for Alternate Gothic Compressed).

To upgrade Three.js, replace `three/three.module.js` and the add-on folders with the same release from npm (`npm pack three@<version>`), then re-run the examples. The import maps in each page point at `vendor/three/`.
