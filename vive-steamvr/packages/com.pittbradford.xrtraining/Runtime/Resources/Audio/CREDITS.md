# Audio credits (kit recorded samples)

Every recorded file in this folder is CC0 1.0 (public domain dedication). Licence checked on each source page on 2026-11-03: the page lists "License(s): CC0".
Files are unmodified Ogg Vorbis from the source packs, only renamed to the kit's sound names (`<name>_<n>.ogg`; Unity imports them mono, and `Sfx` normalises the level at load).
All other catalogue sounds are synthesized in code (`Synth.cs`) and need no credit.

Author of the first seven groups below: **rubberduck** (OpenGameArt.org user). The footsteps (last row) are by **Fantozzi**, uploaded to OpenGameArt.org by the user Raassh. Selected by file name; the clips were not listened to by the author of this list, so judge by ear.

| Kit file(s) | Source pack and URL | Original files |
|---|---|---|
| `door_open_1`, `door_open_2` | 100 CC0 SFX, https://opengameart.org/content/100-cc0-sfx ; 100 CC0 metal and wood SFX, https://opengameart.org/content/100-cc0-metal-and-wood-sfx | `door_open.ogg`; `door_open_01.ogg` |
| `door_close_1` to `door_close_3` | 100 CC0 SFX | `door_close_01.ogg` to `door_close_03.ogg` |
| `switch_1`, `switch_2` | 100 CC0 SFX | `switch_01.ogg`, `switch_02.ogg` |
| `water_drip_1`, `water_drip_2` | 100 CC0 SFX | `plop_01.ogg`, `plop_02.ogg` |
| `clink_metal_1` to `clink_metal_4` | 100 CC0 metal and wood SFX | `metal_hit_01.ogg` to `metal_hit_04.ogg` |
| `pallet_drop_1` to `pallet_drop_3` | 100 CC0 metal and wood SFX | `wood_falling_01.ogg` to `wood_falling_03.ogg` |
| `snap_1`, `snap_2` | 100 CC0 metal and wood SFX | `metal_close_01.ogg`, `lock_open_01.ogg` |
| `footstep_concrete_1` to `footstep_concrete_6` | Fantozzi's Footsteps (Grass/Sand & Stone), https://opengameart.org/content/fantozzis-footsteps-grasssand-stone (file `Fantozzi-footsteps.7z`, folder `ogg`) | `Fantozzi-StoneL1.ogg`, `StoneR1`, `StoneL2`, `StoneR2`, `StoneL3`, `StoneR3` |
| `footstep_gravel_1` to `footstep_gravel_6` | same pack | `Fantozzi-SandL1.ogg`, `SandR1`, `SandL2`, `SandR2`, `SandL3`, `SandR3` (sand and dirt, used for the refinery gravel) |

Licence: https://creativecommons.org/publicdomain/zero/1.0/

## Round 4 (November 5, 2026): no new recordings

No new recorded files were added in round 4. The one attempt to fetch more CC0 packs (Kenney, a second OpenGameArt pack) was not completed: the Kenney site refused an automated request and no workaround was used, so no source or licence could be verified, and an unverified file is never included. All new sounds are synthesized in code (`Synth.cs`) and need no credit.

The files above are still the same unmodified downloads. What changed is how the kit plays them: at load, `Sfx` now applies a 30 Hz high-pass, a gentle low-pass for the name (`clink_metal` has a steeper one, 5.5 kHz, because two of its four recordings measured as harsh: 56 and 43 percent of their energy above 6 kHz), fades at the ends, and the loudness target for that name, with a soft limiter. The files on disk are not rewritten. Whether the recordings sound better or worse than the new synthesized versions is unknown (unheard): to compare, rename a recording out of `Resources/Audio/` and the synthesized version takes over.

## Round 5 (November 6, 2026): footsteps added

Twelve new recordings: the concrete and gravel footsteps (a left and right step, three each, from the "Stone" and "Sand" sets of Fantozzi's Footsteps pack). Source page https://opengameart.org/content/fantozzis-footsteps-grasssand-stone; the page lists "License(s): CC0" (checked 2026-11-06; the download worked with an ordinary request, nothing was worked around). Files are the unmodified Ogg Vorbis files from the pack, renamed to the kit's names; the page notes the author's own site (Fantozzi/packs/10338) as a source of more steps. They were not listened to: the choice was made from the file names and from measurements (6 clips of 0.30 to 0.44 s, centroid about 2.5 kHz for stone and 2.8 kHz for sand, 3.5 and 7 percent of energy above 6 kHz before the kit's low-pass). Metal, tile and wood footsteps stay synthesized.

At load the kit applies a 30 Hz high-pass, a steeper low-pass for the name (6.5 kHz concrete, 6 kHz gravel), fades, and scales so the peak is -10 dBFS. To compare with the synthesized version, move the files out of `Resources/Audio/`.

`water_drip` now plays the two recordings from the first table together with six new synthesized drips (a bubble-resonance model), as one pool; the other recorded names are used alone, as before.
