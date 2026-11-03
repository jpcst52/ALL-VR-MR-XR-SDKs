using UnityEngine;

/// <summary>
/// Gives the warehouse scenario's walking co-worker a real character. Place one in a hub scene and assign a Rocketbox
/// avatar prefab and the Walker animator controller (Idle/Walk switched by the float "Speed"). In Awake it sets
/// <see cref="CoWorker.ModelFactory"/>; the scenario script then builds the co-worker around the avatar. The avatar's
/// feet are at its own origin, which is the co-worker's origin. Without these assets the co-worker stays a primitive figure.
/// </summary>
public class PeopleLibrary : MonoBehaviour
{
    public GameObject WalkerAvatar;
    public RuntimeAnimatorController WalkerController;

    System.Func<GameObject> factory;

    void Awake()
    {
        if (WalkerAvatar == null || WalkerController == null) return;
        factory = Make;
        CoWorker.ModelFactory = factory;
    }

    void OnDestroy()
    {
        // do not leave a factory behind for the next scene
        if (CoWorker.ModelFactory == factory) CoWorker.ModelFactory = null;
    }

    GameObject Make()
    {
        var go = Instantiate(WalkerAvatar);
        go.name = "Co-worker avatar";
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = WalkerController;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return go;
    }
}
