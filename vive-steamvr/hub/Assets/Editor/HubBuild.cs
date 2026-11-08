using PittBradford.XR.EditorTools;
using UnityEditor;

/// <summary>
/// Command-line player build for the hub (lobby plus the four scenarios), used by scripts/build-vive.ps1 -IncludeHub.
/// Builds the scenes enabled in Build Settings, Lobby first. The output contains licensed Asset Store packs and
/// Rocketbox people: keep it on the lab PC, never publish it.
/// </summary>
public static class HubBuild
{
    [MenuItem("Pitt-Bradford/Build Windows player (hub)")]
    public static void BuildWindows() => Configurator.BuildWindows("Builds/XRTrainingHub/XRTrainingHub.exe");
}
