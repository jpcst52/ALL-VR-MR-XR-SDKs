using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PittBradford.XR.EditorTools
{
    /// <summary>
    /// Play-mode check of the scene audio helpers in the project's first build scene: auto-start by scene name, the reverb zone and
    /// its preset for every place, the bed source, a pooled one-shot, loop distance filtering, and silence during a bake (the bake
    /// stage flag mutes everything and loop sources are not started). Logs lines tagged [sceneaudio] and a PASS or FAIL line.
    /// Command line (no -batchmode): <c>Unity.exe -projectPath P -executeMethod PittBradford.XR.EditorTools.SceneAudioCheck.RunAndExit -logFile log.txt</c>.
    /// It cannot hear anything: it checks that the objects and settings exist.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneAudioCheck
    {
        const string Key = "pb.sceneaudiocheck";
        static double t0;
        static bool done;

        static SceneAudioCheck()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
        }

        public static void RunAndExit()
        {
            string path = null;
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) { path = s.path; break; }
            if (path == null) { Debug.LogError("[sceneaudio] FAIL: no enabled scene in Build Settings"); EditorApplication.Exit(1); return; }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SessionState.SetBool(Key, true);
            SessionState.SetInt(Key + ".exit", -1);
            EditorApplication.EnterPlaymode();
        }

        static int bad;
        static void Check(bool ok, string what)
        {
            if (!ok) bad++;
            Debug.Log("[sceneaudio] " + (ok ? "ok   " : "FAIL ") + what);
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            int exit = SessionState.GetInt(Key + ".exit", -1);
            if (exit >= 0)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) { SessionState.SetBool(Key, false); EditorApplication.Exit(exit); }
                return;
            }
            if (!EditorApplication.isPlaying) return;
            if (t0 == 0) t0 = EditorApplication.timeSinceStartup;
            if (done || EditorApplication.timeSinceStartup - t0 < 3.5) return;
            done = true;
            try { Run(); } catch (System.Exception e) { bad++; Debug.LogError("[sceneaudio] exception " + e); }
            Debug.Log("[sceneaudio] " + (bad == 0 ? "PASS" : "FAIL") + ": " + bad + " problems");
            SessionState.SetInt(Key + ".exit", bad == 0 ? 0 : 1);
            EditorApplication.isPlaying = false;
        }

        static void Run()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Check(SceneAudio.TryPlaceOf(scene, out var place), "scene '" + scene + "' maps to a place: " + place);
            Check(SceneAudio.Started, "auto-start ran by itself (SceneAudio.Started)");
            Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "exactly one AudioListener");
            var rootGo = GameObject.Find("Scene audio");
            Check(rootGo != null, "'Scene audio' object exists");
            if (rootGo != null)
            {
                Check(rootGo.GetComponent<DontBake>() != null, "helper is marked DontBake (kept out of bakes)");
                var z = rootGo.GetComponent<AudioReverbZone>();
                Check(z != null && z.reverbPreset == SceneAudio.Preset(SceneAudio.RoomFor(place)), "reverb zone preset " + (z != null ? z.reverbPreset.ToString() : "none"));
                var bed = rootGo.GetComponentInChildren<AudioSource>();
                Check(bed != null && bed.clip != null && bed.clip.name.StartsWith(SceneAudio.BedFor(place)), "bed source plays " + SceneAudio.BedFor(place) + (bed != null ? " isPlaying=" + bed.isPlaying + " vol=" + bed.volume + " blend=" + bed.spatialBlend : ""));
                Check(rootGo.GetComponent<SceneAudioDirector>() != null, "distant-event director present");
            }
            var shot = Sfx.Play("button", Vector3.zero, 1f);
            Check(shot != null && shot.clip != null && shot.reverbZoneMix > 0f && shot.spatialBlend == 1f && shot.dopplerLevel == 0f, "pooled 3D one-shot configured (isPlaying=" + (shot != null && shot.isPlaying) + ")");
            var ui = Sfx.Play2D("ui_confirm");
            Check(ui != null && ui.spatialBlend == 0f && ui.reverbZoneMix == 0f, "2D UI sound has no reverb-zone mix");
            var lp = Sfx.Loop("hvac_loop", null, 0.1f, 2f, 14f);
            Check(lp != null && lp.GetComponent<AudioLowPassFilter>() != null && lp.GetComponent<SfxFar>() != null, "loop source has the distance low-pass");

            foreach (Place p in System.Enum.GetValues(typeof(Place)))
            {
                SceneAudio.Begin(p);
                var g = SceneAudio.Root;
                var zz = g != null ? g.GetComponent<AudioReverbZone>() : null;
                Check(zz != null && zz.reverbPreset == SceneAudio.Preset(SceneAudio.RoomFor(p)), "Begin(" + p + "): reverb " + (zz != null ? zz.reverbPreset.ToString() : "none") + ", bed " + SceneAudio.BedFor(p));
            }

            // a bake must be silent: the stage flag mutes Sfx, and loops are created but not started
            string old = SessionState.GetString(Bake.StageKey, "");
            SessionState.SetString(Bake.StageKey, "play");
            Check(!Sfx.Active && Sfx.Play("button", Vector3.zero) == null && Sfx.Play2D("ui_confirm") == null, "muted while a bake runs (Play and Play2D return null)");
            var muted = Sfx.Loop("hvac_loop", null, 1f);
            Check(muted != null && !muted.isPlaying && muted.GetComponent<DontBake>() != null, "loop made during a bake is not playing and is DontBake");
            SessionState.SetString(Bake.StageKey, old);
            Object.Destroy(lp.gameObject); Object.Destroy(muted.gameObject);
            SceneAudio.Stop();
        }
    }
}
