# Seasonal sky panoramas

Four 360-degree sky and landscape backdrops for the Practice Lab menus and the Unity lobby: spring, summer, autumn and winter. They are CC0 stand-ins in a northern-Appalachian style. **They are not photos of Bradford, PA.** Sources and licence are in [CREDITS.md](CREDITS.md).

## Files

| File | Size | Use |
|---|---|---|
| `<season>-2k.jpg` | 2048x1024, about 0.3 to 0.5 MB | Web and Quest Browser (lighter on memory) |
| `<season>-4k.jpg` | 4096x2048, about 1.3 to 2.2 MB | Desktop browsers, Unity, Vive |
| `skies.json` | | List the apps read: `id`, `label`, `file4k`, `file2k`, `credit`, `source`, `license`, `isBradford` |

`<season>` is `spring`, `summer`, `autumn` or `winter`. All images are equirectangular, 2:1 width to height, and right side up. The horizon sits at mid-height. The left and right edges meet behind the viewer.

## Replacing one with your own campus photo

The code reads file names from `skies.json`. To swap in your own photo, either keep the file names or update the entry.

1. **Take the photo.** Use a 360 camera (Insta360, Ricoh Theta, GoPro Max and similar) or a phone app that makes full spheres (for example Google Street View's photo-sphere mode). An ordinary phone "panorama" is a wide strip, not a full sphere. It will not fill the view properly. Hold the camera level at eye height. A tripod or monopod helps.
2. **Export as an equirectangular JPG with a 2:1 ratio.** Most 360 camera apps call this "360 photo" or "equirectangular". Check that the width is exactly twice the height.
3. **Make two sizes.** Use 4096x2048 for `<season>-4k.jpg` and 2048x1024 for `<season>-2k.jpg`. JPG quality 80 to 85 is enough. Try to keep the 4k file under about 3 MB and the 2k file under about 1 MB. Any image editor works. Keep the 2:1 ratio and do not crop.
4. **Use the same file name** (for example `winter-4k.jpg` and `winter-2k.jpg`) to replace one season. Or add new files and change `file4k` and `file2k` in that entry of `skies.json`.
5. **Update the entry** in `skies.json`. Set `"isBradford": true`, write your own `credit` (for example "Photo by Jeremy Callinan, Pitt-Bradford campus, January 2027"), and set `source` and `license` to match. Update the table in `CREDITS.md` as well.
6. **Check it.** Open the image. The sky should be at the top and the ground at the bottom, with the horizon near the middle line. Then open the Practice Lab menu (or the Unity lobby) and look all the way around for a visible seam.

Keep the order spring, summer, autumn, winter in `skies.json`, and keep the `id` values. The other entries keep working when you change one. Do not put photos with recognisable people or licensed university branding here. Web demos must stay CC0, or be your own photos that you choose to share.
