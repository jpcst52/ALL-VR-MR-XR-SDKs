# SteamVR Boolean action color toggle

This example uses the actual SteamVR Unity Plugin input API. A SteamVR Boolean action toggles a target object's material color when the action transitions down.

## Prerequisites and dependencies

- Unity version supported by the installed Valve SteamVR Unity Plugin release.
- Valve's **SteamVR Unity Plugin** (`SteamVR Unity Plugin` from Valve's official repository); this example references `Valve.VR.SteamVR_Action_Boolean` and `SteamVR_Input_Sources`.
- SteamVR installed on the PC, a configured SteamVR-compatible headset/controller, and SteamVR selected as the active runtime.
- The action's SteamVR Input binding must be configured for the controller(s) being tested.

Official references:

- [Valve SteamVR Unity Plugin repository](https://github.com/ValveSoftware/steamvr_unity_plugin)
- [SteamVR Unity Plugin documentation](https://valvesoftware.github.io/steamvr_unity_plugin/)
- [SteamVR Input actions](https://valvesoftware.github.io/steamvr_unity_plugin/articles/SteamVR-Input.html)

## Setup

1. Import Valve's SteamVR Unity Plugin and follow its setup wizard. Create a SteamVR Input Boolean action (for example, a toggle action) in the plugin's Input window and generate/update the actions.
2. Copy `Assets/Scripts/SteamVRActionColorToggle.cs` into the same path in the project.
3. Create a cube or other renderer in the scene. Add `SteamVRActionColorToggle` to it and assign the Boolean action, the desired input source (default: `Any`), and the renderer in the Inspector.
4. Ensure the scene has a SteamVR tracking setup (for example, the plugin's camera rig), start SteamVR, and run the scene on the PC headset.
5. Bind the configured Boolean action to a controller button using the SteamVR Input binding UI. Pressing it toggles the object's color.

The script polls `SteamVR_Action_Boolean.GetStateDown(SteamVR_Input_Sources)` and does not define a binding or action asset itself; those remain user-configured in SteamVR Input.
