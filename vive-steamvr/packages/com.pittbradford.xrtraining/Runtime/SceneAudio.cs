using UnityEngine;

namespace PittBradford.XR
{
    /// <summary>The five scenes the kit knows how to dress with sound.</summary>
    public enum Place { Lobby, HydroBay, Refinery, Warehouse, ResistorBench }

    /// <summary>Room acoustics presets (they map to Unity's built-in reverb zone presets).</summary>
    public enum RoomKind { Off, SmallLab, Lobby, Bay, Warehouse, Outdoor }

    /// <summary>
    /// Per-scene audio in one call: a room reverb (one <see cref="AudioReverbZone"/> that covers the whole scene, which only
    /// processes sources that opt in, so it is cheap), a low ambience bed, a director that scatters rare distant sounds, and the
    /// floor surface for footsteps. Nothing is created that a scene bake would save, and nothing plays during a bake.
    /// <code>SceneAudio.Begin(Place.Refinery, null, 1f, rig);   // from Start, after the rig exists</code>
    /// Unheard: the choices (preset per room, bed levels, event timing) were measured and reasoned about, not listened to.
    /// </summary>
    public static class SceneAudio
    {
        static GameObject root;
        static AudioReverbZone zone;

        /// <summary>Scenes with a known name start their audio automatically (see <see cref="SceneAudioBoot"/>). Set false to opt out.</summary>
        public static bool AutoStart = true;
        /// <summary>The helper object of the running scene audio (null when none).</summary>
        public static GameObject Root => root;
        /// <summary>True once <see cref="Begin"/> has run in the current scene.</summary>
        public static bool Started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { root = null; zone = null; AutoStart = true; Started = false; }

        /// <summary>Maps a scene name to a place (case-insensitive substring: lobby, hydro, refinery, warehouse, resistor or bench).</summary>
        public static bool TryPlaceOf(string sceneName, out Place place)
        {
            string n = (sceneName ?? "").ToLowerInvariant();
            place = Place.Lobby;
            if (n.Contains("lobby")) place = Place.Lobby;
            else if (n.Contains("hydro")) place = Place.HydroBay;
            else if (n.Contains("refinery")) place = Place.Refinery;
            else if (n.Contains("warehouse")) place = Place.Warehouse;
            else if (n.Contains("resistor") || n.Contains("bench")) place = Place.ResistorBench;
            else return false;
            return true;
        }

        /// <summary>Sets the scene's room reverb (creates one zone on first use). Returns the zone, or null when kind is Off or not playing.</summary>
        public static AudioReverbZone Room(RoomKind kind)
        {
            if (!Application.isPlaying) return null;
            if (kind == RoomKind.Off)
            {
                if (zone != null) zone.reverbPreset = AudioReverbPreset.Off;
                return zone;
            }
            EnsureRoot();
            if (zone == null)
            {
                zone = root.AddComponent<AudioReverbZone>();
                zone.minDistance = 2000f;   // covers the whole scene
                zone.maxDistance = 2100f;
            }
            zone.reverbPreset = Preset(kind);
            return zone;
        }

        public static AudioReverbPreset Preset(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.SmallLab: return AudioReverbPreset.Room;          // short, soft
                case RoomKind.Lobby: return AudioReverbPreset.Hallway;          // medium, hard walls
                case RoomKind.Bay: return AudioReverbPreset.Stoneroom;          // concrete and tanks
                case RoomKind.Warehouse: return AudioReverbPreset.Auditorium;   // big steel hall
                case RoomKind.Outdoor: return AudioReverbPreset.Plain;          // open, thin
                default: return AudioReverbPreset.Off;
            }
        }

        public static RoomKind RoomFor(Place p)
        {
            switch (p)
            {
                case Place.Lobby: return RoomKind.Lobby;
                case Place.HydroBay: return RoomKind.Bay;
                case Place.Refinery: return RoomKind.Outdoor;
                case Place.Warehouse: return RoomKind.Warehouse;
                default: return RoomKind.SmallLab;
            }
        }

        public static string BedFor(Place p)
        {
            switch (p)
            {
                case Place.Lobby: return "amb_lobby";
                case Place.HydroBay: return "amb_hydro";
                case Place.Refinery: return "amb_refinery";
                case Place.Warehouse: return "amb_warehouse";
                default: return "amb_bench";
            }
        }

        public static Surface SurfaceFor(Place p)
        {
            switch (p)
            {
                case Place.Lobby: return Surface.Tile;
                case Place.HydroBay: return Surface.Concrete;
                case Place.Refinery: return Surface.Gravel;
                case Place.Warehouse: return Surface.Concrete;
                default: return Surface.Tile;
            }
        }

        /// <summary>
        /// Starts the room reverb, the ambience bed (nearly head-locked, no distance filtering) and the distant-event director for a
        /// place. Safe to call again (the previous one is replaced). <paramref name="bedVolume"/> scales the bed (1 = its calibrated level).
        /// Pass the rig to also set <c>rig.FloorSurface</c> to the place's default (skip it when the scenario sets its own).
        /// Does nothing outside play mode. Returns the helper object (kept out of bakes).
        /// </summary>
        public static GameObject Begin(Place place, Transform parent = null, float bedVolume = 1f, XRRig rig = null)
        {
            if (!Application.isPlaying) return null;
            Stop();
            Started = true;
            EnsureRoot();
            if (parent != null) root.transform.SetParent(parent, false);
            Room(RoomFor(place));
            var bed = Sfx.Loop(BedFor(place), root.transform, bedVolume, 2f, 60f);
            if (bed != null)
            {
                bed.spatialBlend = 0.1f;   // a room tone surrounds you; it should not sit in one place
                bed.spread = 180f;
                var far = bed.GetComponent<SfxFar>(); if (far != null) Object.Destroy(far);
                var lp = bed.GetComponent<AudioLowPassFilter>(); if (lp != null) Object.Destroy(lp);
            }
            var dir = root.AddComponent<SceneAudioDirector>();
            dir.Setup(place);
            if (rig != null) rig.FloorSurface = SurfaceFor(place);
            return root;
        }

        /// <summary>Removes the bed, the director and the reverb zone.</summary>
        public static void Stop()
        {
            if (root != null) Object.Destroy(root);
            root = null; zone = null;
        }

        static void EnsureRoot()
        {
            if (root != null) return;
            root = new GameObject("Scene audio");
            root.AddComponent<DontBake>();
        }
    }
}
