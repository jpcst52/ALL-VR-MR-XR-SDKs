# Hardware test session (lab, before Nov 13)

Purpose: the first real-headset run of everything. **Until this is done, nothing in this repo is verified on a Vive Pro 2 or a Quest**; every screenshot so far is a desktop editor render or an emulated Quest 3. Fill in the Result column as you go (Pass / Fail / Skipped, plus a note), then hand this file back so the docs can be updated honestly.

Time: about 60 minutes for the Vive part and 30 for the Quest part. Do the "P1" rows first; they are what the demo needs. Setup details are in [VIVE-SETUP.md](VIVE-SETUP.md) and [QUEST-SETUP.md](QUEST-SETUP.md).

## Before you start (10 minutes)

1. Lab PC: pull or copy the latest repo; in the hub Unity project run **Pitt-Bradford > Build hub: lobby and all scenarios** once (scenes are local-only and are not in git).
2. Start the demo server: `scripts\start-demo.cmd` and note the printed `https://<PC address>:8443/` address.
3. Vive: SteamVR running, headset tracked, SteamVR set as the OpenXR runtime (SteamVR > Settings > Developer > Set SteamVR as OpenXR runtime). Quest: on the same Wi-Fi as the PC.

## A. Vive Pro 2 with SteamVR (Unity projects)

Open the project in Unity 6000.3.20f1 with the headset on and SteamVR running, press Play, and put the headset on.

| # | P | Check | Pass looks like | If it fails (fallback) | Result |
|---|---|---|---|---|---|
| A1 | P1 | Hub project, scene **Lobby**: headset view shows the hall, both controllers visible | stereo view, head tracking follows, controller models track | run with desktop mouse and keyboard mode; note what is wrong | |
| A2 | P1 | Walk with the left stick; snap turn with the right stick | moves smoothly, no sickness spike, turns in 30 degree steps | note which stick or binding is wrong | |
| A3 | P1 | Stand on a portal pad for about 1.4 s | scenario loads | press 1 to 4 on the keyboard | |
| A4 | P1 | In a scenario, hold both grips for 1.5 s | returns to the lobby | press Backspace | |
| A5 | P1 | Hydrostatic Test Bay: point and press trigger on a valve; grab a blind flange with the grip; run the pump to 150 psi | valves turn, flange grabs and snaps, gauge climbs, hold timer runs | note which interaction fails | |
| A6 | P1 | Text legibility: can you read the task board, name tags and the logo plate without leaning in? | readable at arm's length | note which texts are too small | |
| A7 | P2 | Refinery Rounds (project 02) walk-through to the muster point | plays through without errors | skip, use desktop mode | |
| A8 | P2 | Warehouse and Forklift (03): mirror, signs, forklift controls | as above | skip | |
| A9 | P2 | Resistor Bench (04): pick up a part, use the meter, answer | as above | skip | |
| A10 | P2 | Frame rate in the hub scenes (SteamVR frame-time graph) | steady, no reprojection warnings | press P to switch off post-processing; note the result | |
| A11 | P2 | Room tone and pump hum audible, spatial | heard, not harsh | note | |
| A12 | P1 | Answer a "Why this matters" question with the trigger (Hydrostatic Test Bay or Refinery Rounds) | The three answer panels can be pointed at and pressed; feedback shows; the board adds a separate Reasoning line | use keys 1, 2, 3 or the mouse; note which is hard to point at | |
| A13 | P2 | Warehouse (03): the co-worker hazard and the pause-and-ask check | Co-worker appears and walks past; the view blanks and asks three questions; you can answer with the controller; no discomfort from the freeze | untick `CoworkerHazard` for the demo | |
| A14 | P1 | Lobby sky: watch two season changes (about 30 s each) | The pixelate and scan-line glitch is comfortable, not nauseating, and both eyes agree | set Interval to 0 on the SeasonSky component to stop rotation; note the discomfort | |

## B. Vive Pro 2 with the web Practice Lab (browser, WebXR)

| # | P | Check | Pass looks like | If it fails | Result |
|---|---|---|---|---|---|
| B1 | P1 | Open `https://<PC address>:8443/` in Chrome or Edge on the Vive PC; click Enter VR | session starts in the headset | use desktop mode on a monitor | |
| B2 | P1 | Play one scenario (for example the hydrostatic test) | controllers point, trigger works, results show | note | |

## C. Meta Quest (Quest Browser, WebXR)

| # | P | Check | Pass looks like | If it fails | Result |
|---|---|---|---|---|---|
| C1 | P1 | Open the launcher address; accept the certificate warning once | launcher loads | install the certificate or use plain HTTP (`INSECURE_HTTP=1`) | |
| C2 | P1 | Practice Lab: Enter VR, play one scenario | works as in B2 | note | |
| C3 | P1 | Frame rate and legibility of text on the Quest | smooth, readable | note which scenario is heavy | |
| C4 | P2 | Circuit Lab with two headsets in one room name | both see each other and the parts | single-user fallback | |
| C5 | P2 | Site Safety Layout (mixed reality layout) | loads; passthrough where supported | skip | |
| C6 | P2 | Scenario 17, Circulation Builder (draft): its 3D card position in VR | card reachable and readable | note position | |

## D. Anything else you saw

Write notes here (discomfort, tracking loss, wrong scale, text too small, sound issues). Screenshots from the headset (SteamVR or Quest capture) are very welcome and should be labelled with the headset used.

## What happens next

Give the filled-in file back. Failures become fix tasks; passes let the READMEs and decks drop the "unverified" wording for exactly those items and no others.
