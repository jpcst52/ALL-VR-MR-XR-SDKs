using UnityEngine;
using UnityEngine.SceneManagement;

namespace PittBradford.XR
{
    /// <summary>
    /// Starts <see cref="SceneAudio.Begin"/> by itself for the five known scenes, chosen by scene name (Lobby, HydrostaticTest,
    /// RefineryRounds, WarehouseForklift, ResistorBench, with or without a "_Hub" suffix), so no scenario script has to change.
    /// A scenario can opt out with <c>SceneAudio.AutoStart = false</c> (before the first frame) or take over by calling
    /// <c>SceneAudio.Begin</c> itself. Does nothing during a scene bake.
    /// </summary>
    public sealed class SceneAudioBoot : MonoBehaviour
    {
        float due;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Spawn();
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) SceneAudio.Started = false;
            Spawn();
        }

        static void Spawn()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("Scene audio boot");
            go.AddComponent<DontBake>();
            go.AddComponent<SceneAudioBoot>();
        }

        void Awake()
        {
            due = Time.unscaledTime + 0.7f;   // let the scenario build its rig and objects first
            // generate the bed now, while the scene is still loading, so there is no hitch when it starts
            if (SceneAudio.AutoStart && Sfx.Active && SceneAudio.TryPlaceOf(SceneManager.GetActiveScene().name, out var place))
            {
                Sfx.Preload(SceneAudio.BedFor(place));
                Sfx.PreloadBackground();   // the rest of the catalogue is built on a background thread, a few clips per frame
            }
        }

        void Update()
        {
            if (Time.unscaledTime < due) return;
            if (SceneAudio.AutoStart && !SceneAudio.Started && Sfx.Active && SceneAudio.TryPlaceOf(SceneManager.GetActiveScene().name, out var place))
                SceneAudio.Begin(place);
            Destroy(gameObject);
        }
    }
}
