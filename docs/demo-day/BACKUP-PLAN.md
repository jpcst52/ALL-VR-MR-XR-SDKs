# Backup plan

Assume one thing will fail. Decide before the day which fallback you will use, and say so calmly.

## Prepare in advance
- **Recorded video** (3 to 5 minutes) of the full path: launcher, Circuit Lab with two users, Site Safety Layout, and the Hydrostatic Test Bay. Save it on the PC, on a USB stick and on the presenter laptop. Label recordings from the emulator or desktop honestly.
- **Screenshots** of each scenario in a slide deck or folder (existing images are in each example's `screenshots/` folder; headset-free ones are labelled as emulated).
- The repo itself, copied to a USB stick, so the demo runs from a second PC with Node installed.

## Fallbacks, in order
1. **Headset cannot reach the PC over Wi-Fi:** use the USB `adb reverse` path ([QUEST-SETUP.md](QUEST-SETUP.md)), or a travel router the PC and headsets join.
2. **Quest fails entirely:** show the same pages on the PC in desktop Chrome/Edge (mouse and keyboard); two browser windows in one room show multiplayer without any headset.
3. **Vive or SteamVR fails:** run the page or Unity scene in its desktop view on the monitor, or play the recorded video.
4. **PC fails:** start the server on a second PC (`npm start` in `multi-player/`), re-open URLs, re-accept certificates, or play the video from the laptop.
5. **Everything fails:** play the recorded video and walk through the screenshots; offer hands-on time afterwards.

## Rule
Do not debug in front of the room for more than about 2 minutes. Switch to the next fallback.
