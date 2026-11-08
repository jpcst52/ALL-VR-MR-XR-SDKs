using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// The kit makes its materials at run time with Shader.Find, and no scene references those shaders, so a player build strips them
    /// and the materials fail (null shader, or a pink surface). Listing them under Always Included Shaders keeps them. This runs before
    /// every build of every project that uses the kit, so a project does not depend on its setup menu having been run.
    /// </summary>
    public class KeepShadersOnBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report) => Configurator.KeepShaderVariants();
    }
}
