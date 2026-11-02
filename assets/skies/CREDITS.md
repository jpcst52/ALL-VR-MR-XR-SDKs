# Sky panoramas: credits and provenance

**None of these four images is a photo of Bradford, Pennsylvania.** They are regional-style stand-ins: CC0 panoramas from Poly Haven, picked because they look like northern-Appalachian country (fields, hardwood tree lines, rolling hills). Each `skies.json` entry has `"isBradford": false`. If you add a real campus photo, set that entry to `true` (see [README.md](README.md)).

## Licence

Poly Haven asset licence page: <https://polyhaven.com/license>. On 2026-11-01 that page said: "Our assets are all licensed as CC0 ... You do not need to give credit or attribution when using them (although it is appreciated)." CC0 1.0 deed: <https://creativecommons.org/publicdomain/zero/1.0/>. Attribution is not required, but we give it anyway.

## Files

All files were downloaded on **2026-11-01**. They came from the "Tonemapped JPG" entry in the Poly Haven files API (`https://api.polyhaven.com/files/<id>`, key `tonemapped`). Poly Haven made these JPGs from its own HDRs. We did not tonemap anything ourselves. Each download matched the MD5 that the API listed.

| Season | Files | Source asset | Asset page | Author (per API) | Capture date (per API) | Source MD5 |
|---|---|---|---|---|---|---|
| Spring | `spring-2k.jpg`, `spring-4k.jpg` | Lilienstein (`lilienstein`) | <https://polyhaven.com/a/lilienstein> | Andreas Mischok (per API) | per API timestamp 1608591600 | ec0101625ef45d9b589ee7dfa91d5abf |
| Summer | `summer-2k.jpg`, `summer-4k.jpg` | Spaichingen Hill (`spaichingen_hill`) | <https://polyhaven.com/a/spaichingen_hill> | Andreas Mischok | 2021-09-04 | ad22d97ad5cf7487b1d6eefd6ae19cc7 |
| Autumn | `autumn-2k.jpg`, `autumn-4k.jpg` | Autumn Meadow (`autumn_meadow`) | <https://polyhaven.com/a/autumn_meadow> | Sergej Majboroda | 2019-10-15 | 6fce393438dddde1d7f60e1b73b85bf0 |
| Winter | `winter-2k.jpg`, `winter-4k.jpg` | Passendorf Snow (`passendorf_snow`) | <https://polyhaven.com/a/passendorf_snow> | Grzegorz Wronkowski | 2025-02-19 | fcfbe3e857c95ae014b5c4752c174e5d |

Where the photos were taken: the API gives coordinates in Germany for Spaichingen Hill (48.09 N, 8.74 E) and in Poland for Passendorf Snow (50.48 N, 16.33 E). It gives no coordinates for Belfast Open Field or Autumn Meadow. None of them is in North America.

## What we changed

- Resized each 8192x4096 tonemapped JPG to 4096x2048 (`*-4k.jpg`) and 2048x1024 (`*-2k.jpg`). We used Python Pillow 12.3 with Lanczos resampling.
- Re-encoded as progressive JPEG, quality 85, 4:2:0 chroma subsampling, with the optimise option on. Metadata was not copied.
- Nothing else: no crop, colour change, retouching or rotation.

## Notes on the choices

- **Spring** was changed on 2026-11-01 from Belfast Open Field (grey sky, dry grass and eucalyptus-style trees, which did not read as spring in Pennsylvania) to Lilienstein: fresh green field, a treeline and a soft sky. It reads as late spring; no CC0 early-spring panorama on Poly Haven fit better. Its location is not verified here.
- **Autumn** has orange and red hardwood foliage next to a field. There are **power lines and two pylons** in the sky (the curved dark lines near the top). Rural Pennsylvania has these too, so we kept the image. If they distract, the next-best choice is `tiergarten` (a Berlin park with autumn oaks).
- **Winter** has full snow cover, a clear blue sky and wooded ridges with flat tops. Bradford winters are often overcast. The overcast option, `snowy_hillside_02`, had only patchy snow.

## Search for real Bradford-area panoramas (2026-11-01)

We spent a short, limited time on this. On Wikimedia Commons we searched for "Bradford Pennsylvania 360", "McKean County panorama equirectangular", "Kinzua 360" and "Allegheny National Forest 360 panorama". We listed the categories "Bradford, Pennsylvania" and "Kinzua Bridge State Park" and searched files that use the `Pano360` template with "Pennsylvania". **We found no 360 panorama from Bradford, McKean County, Kinzua or the Allegheny National Forest.** The Bradford and Kinzua categories hold only ordinary flat photos. The closest openly licensed 360 photos in Pennsylvania were near the Delaware Water Gap (CC BY 4.0, G. Edward Johnson), at the other end of the state. We did not use them. We did not search Flickr.
