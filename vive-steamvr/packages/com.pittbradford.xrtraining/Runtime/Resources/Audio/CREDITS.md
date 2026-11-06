# Audio credits (kit recorded samples)

Every recorded file in this folder is CC0 1.0 (public domain dedication). Licence checked on each source page on 2026-11-03: the page lists "License(s): CC0".
Files are unmodified Ogg Vorbis from the source packs, only renamed to the kit's sound names (`<name>_<n>.ogg`; Unity imports them mono, and `Sfx` normalises the level at load).
All other catalogue sounds are synthesized in code (`Synth.cs`) and need no credit.

Author for all files below: **rubberduck** (OpenGameArt.org user). Selected by file name; the clips were not listened to by the author of this list, so judge by ear.

| Kit file(s) | Source pack and URL | Original files |
|---|---|---|
| `door_open_1`, `door_open_2` | 100 CC0 SFX, https://opengameart.org/content/100-cc0-sfx ; 100 CC0 metal and wood SFX, https://opengameart.org/content/100-cc0-metal-and-wood-sfx | `door_open.ogg`; `door_open_01.ogg` |
| `door_close_1` to `door_close_3` | 100 CC0 SFX | `door_close_01.ogg` to `door_close_03.ogg` |
| `switch_1`, `switch_2` | 100 CC0 SFX | `switch_01.ogg`, `switch_02.ogg` |
| `water_drip_1`, `water_drip_2` | 100 CC0 SFX | `plop_01.ogg`, `plop_02.ogg` |
| `clink_metal_1` to `clink_metal_4` | 100 CC0 metal and wood SFX | `metal_hit_01.ogg` to `metal_hit_04.ogg` |
| `pallet_drop_1` to `pallet_drop_3` | 100 CC0 metal and wood SFX | `wood_falling_01.ogg` to `wood_falling_03.ogg` |
| `snap_1`, `snap_2` | 100 CC0 metal and wood SFX | `metal_close_01.ogg`, `lock_open_01.ogg` |

Licence: https://creativecommons.org/publicdomain/zero/1.0/

## Round 4 (November 5, 2026): no new recordings

No new recorded files were added in round 4. The one attempt to fetch more CC0 packs (Kenney, a second OpenGameArt pack) was not completed: the Kenney site refused an automated request and no workaround was used, so no source or licence could be verified, and an unverified file is never included. All new sounds are synthesized in code (`Synth.cs`) and need no credit.

The files above are still the same unmodified downloads. What changed is how the kit plays them: at load, `Sfx` now applies a 30 Hz high-pass, a gentle low-pass for the name (`clink_metal` has a steeper one, 5.5 kHz, because two of its four recordings measured as harsh: 56 and 43 percent of their energy above 6 kHz), fades at the ends, and the loudness target for that name, with a soft limiter. The files on disk are not rewritten. Whether the recordings sound better or worse than the new synthesized versions is unknown (unheard): to compare, rename a recording out of `Resources/Audio/` and the synthesized version takes over.
