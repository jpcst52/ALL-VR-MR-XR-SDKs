# Multiplayer VR — Waterworks Workshop

A browser-based shared workshop for joining one room from a VR headset, an ordinary desktop browser, or both. Add and move a small collection of procedural waterworks parts. Everyone in the room sees the same objects, valve state, roster, and participant presence.

This is a WebXR/WebSocket teaching example, not a native SteamVR or vendor-specific headset app.

## Run it

From `multi-player/`, install dependencies and start the shared HTTP/WebSocket server:

```powershell
npm install
npm test
npm start
```

Open `http://localhost:8443/multi-player/vr/` and join the same room on each client. Set the client name before joining. You can also use the sibling [AR example](../ar/) in the same room.

For a headset or LAN device, WebXR requires HTTPS with a certificate trusted by the headset. See the [multiplayer setup guide](../README.md#connect-a-headset-or-another-computer) for an `mkcert` development configuration and LAN URL.

## What it demonstrates

- WebXR immersive VR entry with a normal browser view as a fallback.
- Joining a named multiplayer room through a WebSocket relay.
- A server-held snapshot so participants who join later see existing parts.
- Peer roster and remote presence.
- Procedurally generated tank, valve, pipe, and pump.
- Shared part placement, drag/transform updates, valve open/close state, and deletion.
- Server-enforced single-person grab ownership. A competing grab is denied; an owner disconnect releases their part.

## Controls

- Click a part button to spawn that part at the shared work surface.
- On desktop, select a part by clicking it and drag it to move. Click an empty location to place the selected part at the cursor.
- Use **Delete selected** to remove a part; **V** toggles the selected valve.
- On a headset, select a part with the controller trigger. Select an empty work-surface location to add the current part.
- Use **Enter VR** while the page is in a user gesture and the headset/runtime is ready.
- Join another browser in the same room to observe updates and presence.

## Limitations

WebXR availability depends on the browser, headset, runtime, operating system, and secure context. Desktop fallback mode allows interaction testing but is not a VR session. This example does not implement voice, accounts, durable storage, networking authentication, prediction, or public deployment protections. Room state is in memory only.

See the parent [README](../README.md) for the shared relay protocol, room behavior, tests, security scope, and references.

The [combined slide deck](../Multiplayer-WebXR-Examples.pptx) walks through both modes. The [VR screenshot](../screenshots/multiplayer-vr.png) shows the desktop fallback with shared parts; it is not a headset capture.
