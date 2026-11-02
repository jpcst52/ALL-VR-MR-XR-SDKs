using PittBradford.XR.EditorTools;
using UnityEditor;

/// <summary>Run once on a fresh clone: Pitt-Bradford > Set up project (or -executeMethod ProjectSetup.Run).</summary>
public static class ProjectSetup
{
    [MenuItem("Pitt-Bradford/Set up project")]
    public static void Run()
    {
        Configurator.ConfigureProject("Resistor Bench");
        Configurator.CreateScene<ResistorBench>("Assets/Scenes/ResistorBench.unity");
    }

    [MenuItem("Pitt-Bradford/Build Windows player")]
    public static void BuildWindows() => Configurator.BuildWindows("Builds/ResistorBench/ResistorBench.exe");
}
