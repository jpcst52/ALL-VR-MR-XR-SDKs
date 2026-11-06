using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>
    /// Plays rare, distant one-shots around the listener so a scene's ambience bed does not feel like a loop (a far clank, a
    /// creak, a relay tick). Created by <see cref="SceneAudio.Begin"/>; costs one comparison per event per frame.
    /// </summary>
    public sealed class SceneAudioDirector : MonoBehaviour
    {
        struct Ev { public string Name; public float MinGap, MaxGap, Volume, MinR, MaxR, Next; }

        Ev[] events = new Ev[0];
        AudioListener listener;
        float nextFind;

        static Ev E(string n, float a, float b, float v, float r0, float r1) => new Ev { Name = n, MinGap = a, MaxGap = b, Volume = v, MinR = r0, MaxR = r1 };

        public void Setup(Place place)
        {
            switch (place)
            {
                case Place.Lobby:
                    events = new[] { E("far_creak", 25f, 60f, 0.8f, 5f, 10f), E("far_thud", 40f, 90f, 0.7f, 6f, 12f) };
                    break;
                case Place.HydroBay:
                    events = new[] { E("water_drip", 5f, 14f, 0.35f, 3f, 8f), E("far_clank", 30f, 70f, 0.8f, 6f, 12f), E("far_creak", 40f, 90f, 0.8f, 5f, 10f) };
                    break;
                case Place.Refinery:
                    events = new[] { E("far_clank", 12f, 35f, 0.9f, 8f, 18f), E("far_steam", 20f, 50f, 0.9f, 8f, 16f), E("far_thud", 40f, 80f, 0.8f, 8f, 16f), E("far_creak", 25f, 60f, 0.8f, 6f, 14f) };
                    break;
                case Place.Warehouse:
                    events = new[] { E("far_clank", 10f, 30f, 0.9f, 8f, 18f), E("far_beep", 18f, 40f, 0.8f, 10f, 20f), E("far_thud", 20f, 50f, 0.8f, 8f, 16f) };
                    break;
                default:
                    events = new[] { E("far_relay_tick", 8f, 20f, 0.8f, 3f, 7f), E("far_creak", 60f, 120f, 0.7f, 5f, 9f) };
                    break;
            }
            for (int i = 0; i < events.Length; i++) events[i].Next = Time.time + Random.Range(events[i].MinGap * 0.3f, events[i].MaxGap);
        }

        void Update()
        {
            if (!Sfx.Active) return;
            if (listener == null)
            {
                if (Time.unscaledTime < nextFind) return;
                nextFind = Time.unscaledTime + 1f;
                listener = FindFirstObjectByType<AudioListener>();
                if (listener == null) return;
            }
            for (int i = 0; i < events.Length; i++)
            {
                if (Time.time < events[i].Next) continue;
                var d = Random.insideUnitCircle.normalized * Random.Range(events[i].MinR, events[i].MaxR);
                var p = listener.transform.position + new Vector3(d.x, Random.Range(-0.5f, 2f), d.y);
                Sfx.Play(events[i].Name, p, events[i].Volume, 0.08f);
                events[i].Next = Time.time + Random.Range(events[i].MinGap, events[i].MaxGap);
            }
        }
    }
}
