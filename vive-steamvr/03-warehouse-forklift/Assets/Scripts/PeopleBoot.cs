using UnityEngine;

/// <summary>
/// Gives the walking co-worker a realistic character when the Rocketbox avatar is present under Resources (it is committed
/// with this project, shrunk; MIT, see Assets/Resources/Rocketbox/LICENSE.md). Without it the co-worker stays a primitive
/// figure. Runs before the first scene loads, so the scenario script sees the factory when it builds the co-worker.
/// </summary>
public static class PeopleBoot
{
    public const string WalkerAvatar = "Rocketbox/Avatars/Professions/Construction_Male_02/Export/Construction_Male_02";
    public const string DriverAvatar = "Rocketbox/Avatars/Professions/Construction_Male_01/Export/Construction_Male_01";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        var avatar = Resources.Load<GameObject>(WalkerAvatar);
        var controller = Resources.Load<RuntimeAnimatorController>("Rocketbox/Walker");
        if (avatar == null || controller == null) return;
        CoWorker.ModelFactory = () =>
        {
            var go = Object.Instantiate(avatar);
            go.name = "Co-worker avatar";
            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return go;
        };
    }
}
