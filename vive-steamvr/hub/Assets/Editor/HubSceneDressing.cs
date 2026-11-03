using UnityEditor;
using UnityEngine;

/// <summary>
/// Dresses the Warehouse and Forklift and Refinery Rounds hub scenes with Asset Store props (local packs) and
/// Rocketbox people (local). Called from <see cref="HubLobbyBuilder"/> after the scenario object is created, so a rebuild
/// reproduces everything. Dressing sits around the walkable paths, the forklift route, task boards and interactive items;
/// colliders on dressing are switched off so nothing blocks the rig or the hands. A missing pack is skipped with a log line.
/// </summary>
public static class HubSceneDressing
{
    const string CM1 = "Professions/Construction_Male_01", CM2 = "Professions/Construction_Male_02", CM4 = "Professions/Construction_Male_04";
    const string CF1 = "Professions/Construction_Female_01", BM1 = "Professions/Business_Male_01", BF1 = "Professions/Business_Female_01";
    const string SEC = "Professions/Security_Male_01", DEL = "Professions/Delivery_Male_01";

    // ------------------------------------------------------------------------------------------ helpers

    /// <summary>
    /// Places a prefab so that the centre of its rendered bounds is at (x, z) and the bottom of the bounds is at y.
    /// (The packs' pivots are inconsistent, so bounds are the reliable anchor.) Colliders are disabled unless solid.
    /// </summary>
    /// <summary>Switches off what dressing does not need: colliders, probes, sounds, NavMesh blockers; rigidbodies become kinematic.</summary>
    static void Tidy(GameObject go, bool solid)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            if (t != null && t.name.Contains("NavMeshBlocker")) t.gameObject.SetActive(false);
        if (!solid)
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var p in go.GetComponentsInChildren<ReflectionProbe>()) p.enabled = false;
        foreach (var a in go.GetComponentsInChildren<AudioSource>()) a.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var t in go.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    public static GameObject Put(Transform parent, string prefab, float x, float z, float yaw = 0f, float y = 0f, float scale = 1f, bool solid = false)
    {
        var go = HubDressing.Spawn(parent, prefab, Vector3.zero, yaw, scale);
        if (go == null) return null;
        Tidy(go, solid);
        var b = HubDressing.Union(go);
        go.transform.position += new Vector3(x - b.center.x, y - b.min.y, z - b.center.z);
        return go;
    }

    /// <summary>Like Put but anchored by the bounds centre in all three axes (for overhead pieces).</summary>
    public static GameObject PutAt(Transform parent, string prefab, Vector3 center, float yaw = 0f, float scale = 1f)
    {
        var go = HubDressing.Spawn(parent, prefab, Vector3.zero, yaw, scale);
        if (go == null) return null;
        Tidy(go, false);
        go.transform.position += center - HubDressing.Union(go).center;
        return go;
    }

    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        return g;
    }

    /// <summary>A person at the floor point, facing yaw (0 = +z, 90 = +x), playing an activity from the Rocketbox controllers.</summary>
    static GameObject Person(Transform parent, string avatar, float x, float z, float yaw, string activity, float y = 0f)
    {
        var p = PeopleSetup.Spawn(parent, avatar, new Vector3(x, y, z), yaw, activity);
        if (p != null) p.name = System.IO.Path.GetFileName(avatar) + " (" + activity + ")";
        return p;
    }

    static void AddPeopleLibrary(Transform parent)
    {
        var lib = new GameObject("People library").AddComponent<PeopleLibrary>();
        lib.transform.SetParent(parent, false);
        lib.WalkerAvatar = AssetDatabase.LoadAssetAtPath<GameObject>(PeopleSetup.ModelPath(CM1));
        lib.WalkerController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PeopleSetup.ControllerDir + "/m_Walker.controller");
        if (lib.WalkerAvatar == null || lib.WalkerController == null)
            Debug.LogWarning("[dress] Rocketbox avatar or Walker controller missing; the co-worker stays a primitive figure");
    }

    // ------------------------------------------------------------------------------------------ warehouse

    /// <summary>Warehouse and Forklift: props around the aisles and lanes, plus workers. Scenario floor y = 0, walls at x = -14..14, z = -12..16.</summary>
    public static void DressWarehouse()
    {
        var root = new GameObject("Dressing").transform;

        // --- north wall: loaded racking (east half; the receiving sign hangs on the wall in the middle) and a pallet staging row
        var north = Group(root, "North wall racking");
        Put(north, "BigShelves01Group1", 8.9f, 15.15f, 0f, 0f, 0.52f);
        Put(north, "Shelves_big01", -12.3f, 15.0f, 90f, 0f, 0.8f);
        Put(north, "Shelves_big02", -10.0f, 15.0f, 90f, 0f, 0.8f);
        string[] stage = { "Box_Pallet01", "Box_Pallet03", "Box_Pallet06", "Box_Pallet08" };
        for (int i = 0; i < stage.Length; i++) Put(north, stage[i], -2.7f + i * 1.9f, 14.95f, i * 7f);

        // --- east strip (x 12..14): parked forklift, loaded pallets by the dock, a hand truck, extinguisher stations.
        // The dock door (z 3.6..8.4) and the forklift lane (x < 11.6) stay clear.
        var east = Group(root, "East strip");
        Put(east, "Forklift02", 13.05f, -3.3f, 0f);
        Put(east, "Box_Pallet05", 13.0f, 2.7f, 0f);
        Put(east, "Box_Pallet02", 13.0f, 10.6f, 90f);
        Put(east, "HandTruck", 13.1f, -7.2f, 90f);
        Put(east, "Fire Extinguisher", 13.85f, 9.0f, -90f, 0.9f);
        Put(east, "Fire Extinguisher", 13.85f, -1.8f, -90f, 0.9f);

        // --- south-east staging: shipping containers (one stacked), pallets, boards
        var south = Group(root, "South staging");
        Put(south, "Containerbig01", 5.9f, -10.4f, 90f);
        Put(south, "Container", 9.2f, -10.9f, 20f);
        Put(south, "Box_Pallet04", -1.8f, -9.6f, 10f);
        Put(south, "Box_Pallet09", 0.0f, -10.9f, 0f);
        Put(south, "Wooden_boards1", 5.5f, -7.6f, 90f);
        Put(south, "Crate01", 8.0f, -7.8f, 25f);
        Put(south, "Cardboard_boxGroup01a", 9.0f, -7.6f, 0f);
        Put(south, "HandTruck", -1.3f, -7.7f, 70f);
        Put(south, "Fire Extinguisher", -4.2f, -11.8f, 0f, 0.9f);
        Put(south, "Garbage Bin", -0.4f, -11.6f);

        // --- supervisor's office corner (Modern Office), against the south wall
        var office = Group(root, "Office corner");
        Put(office, "Table 1", 12.3f, -11.0f, 0f);
        Put(office, "Chair 2", 13.2f, -9.6f, 140f);
        Put(office, "Computer 1", 12.4f, -11.0f, 0f, 0.82f);
        Put(office, "Papers", 11.3f, -11.0f, 0f, 0.82f);
        Put(office, "Lamp 1", 13.3f, -11.1f, 0f, 0.82f);
        Put(office, "File Cabinet", 10.9f, -11.6f, 0f);
        Put(office, "Water cooler", 10.2f, -11.5f, 0f);
        Put(office, "Plant 1", 13.6f, -9.0f);
        PutAt(office, "World map", new Vector3(12.3f, 2.4f, -11.9f), 0f);

        // --- west strip: ladders, a parked forklift, catwalk with rails on pillars, barrels, pallets
        var west = Group(root, "West strip");
        Put(west, "Forklift03", -13.0f, -9.4f, 0f);
        Put(west, "Ladder_02", -13.4f, -5.5f, 90f, 0f, 1f);
        Put(west, "Barrel", -12.4f, -3.6f);
        Put(west, "Barrel", -13.2f, -3.2f, 60f);
        Put(west, "Box_Pallet07", -12.9f, -1.0f, 0f);

        // --- overhead: duct runs, pipes, hanging lights (trusses are at y 6.7; the ceiling at 7.1)
        var over = Group(root, "Overhead");
        for (int i = 0; i < 4; i++) PutAt(over, "Duct Straight", new Vector3(0f, 5.6f, -9f + i * 6f), 0f);
        for (int i = 0; i < 4; i++) PutAt(over, "Duct Straight", new Vector3(-9f + i * 6f + 3f, 5.6f, -0.2f), 90f);
        for (int i = 0; i < 7; i++)
        {
            float lx = (i % 2 == 0) ? -3.6f : 3.6f;
            PutAt(over, "Hanging Light", new Vector3(lx, 6.2f, -9.5f + i * 4.2f), 0f);
        }
        Put(over, "Pipes 04", -13.75f, 13.5f, 0f, 0f, 0.75f);
        Put(over, "Pipes 02", 13.75f, 14.8f, 0f, 0f, 0.75f);
        Put(over, "Pipes 05", 13.75f, -4.2f, 0f, 0f, 0.75f);

        // --- security cameras and wall lights
        var fix = Group(root, "Wall fixtures");
        PutAt(fix, "Security Cam", new Vector3(-13.6f, 3.4f, 14.8f), 180f);
        PutAt(fix, "Security Cam", new Vector3(13.6f, 3.4f, -10.8f), 0f);
        foreach (float z in new[] { -2f, 7f, 11f })
        {
            PutAt(fix, "Wall Light", new Vector3(13.8f, 3.1f, z + 3f), -90f);
        }

        // --- people
        var people = Group(root, "People");
        Person(people, CM1, 6.7f, 14.15f, 0f, "WorkMid");                  // picking at the racking
        Person(people, CF1, 11.0f, 14.15f, 0f, "Note");                    // counting stock
        Person(people, CM2, 12.8f, -0.5f, 0f, "Talk");                      // conversation by the east wall
        Person(people, CM4, 12.8f, 0.7f, 180f, "Talk");
        Person(people, BM1, 12.3f, 12.2f, -90f, "Documents");              // supervisor watching the lane
        Person(people, DEL, -0.4f, -9.6f, -90f, "Crouch");                  // checking a pallet
        Person(people, BF1, 11.4f, -10.1f, 180f, "WorkTable");              // office
        Person(people, SEC, -2.3f, -7.6f, 0f, "Wait");                     // by the entry
        AddPeopleLibrary(root);
    }

    // ------------------------------------------------------------------------------------------ refinery

    /// <summary>
    /// Refinery Rounds: props outside the fence and in the corners, kept within about 22 m of the unit centre (0, 5) so
    /// they stay clear of the muster point (26 m downwind). Fence is x -12..12, z -6..14, gate at z -6, cabin at (-10, -10).
    /// </summary>
    public static void DressRefinery()
    {
        var root = new GameObject("Dressing").transform;

        // --- west compound: boilers with a service catwalk, ladders, ducts
        var west = Group(root, "West compound");
        Put(west, "Boilers", -19.5f, 8.5f, 90f);
        Put(west, "Ladder_02", -17.2f, 4.2f, 90f);
        PutAt(west, "Duct Straight", new Vector3(-16.0f, 1.0f, 14.5f), 90f);
        PutAt(west, "Duct Straight", new Vector3(-21.0f, 1.0f, 14.5f), 90f);
        Put(west, "Pipes 03", -23.2f, 6.2f, 0f, 0f, 0.55f);       // flue stacks beside the boiler
        Put(west, "Pipes 06", -23.2f, 11.0f, 0f, 0f, 0.45f);
        Put(west, "Box_Pallet03", -14.4f, 1.6f, 80f);
        Put(west, "Crate_wood02", -15.5f, 14.8f, 15f);
        Put(west, "Crate_wood03", -14.4f, 15.0f, 70f);
        Put(west, "Cart", -14.2f, -13.0f, 100f);

        // --- containers and crates on the west side of the gate
        var yard = Group(root, "Container yard");
        Put(yard, "ShippingContainer_Red", -19.0f, -3.5f, 0f);
        Put(yard, "ShippingContainer_Blue", -19.0f, -3.5f, 0f, 2.48f);
        Put(yard, "ShippingContainer_Green", -16.0f, -6.5f, 90f);
        Put(yard, "Crate_tarp01big", -14.4f, -2.0f, 25f);
        Put(yard, "Cratemetall", -22.0f, 0.5f, 90f);
        Put(yard, "Barrel", -14.3f, -9.5f);
        Put(yard, "Barrel", -14.9f, -9.0f, 35f);
        Put(yard, "Barrel", -14.1f, -8.5f, 70f);

        // --- control-room cabin details
        var cab = Group(root, "Cabin details");
        Put(cab, "Fire Extinguisher", -12.2f, -11.5f, 0f);
        PutAt(cab, "Security Cam", new Vector3(-7.8f, 2.6f, -8.5f), 0f);
        PutAt(cab, "Wall Light", new Vector3(-12.25f, 2.3f, -10.0f), -90f);
        Put(cab, "Garbage Bin", -8.0f, -12.4f);

        // --- east side: generator set with fuel cans and floodlights near the flare
        var east = Group(root, "East utility");
        Put(east, "PowerGenerator", 17.5f, -3.5f, 90f);
        Put(east, "Gascan_Red", 16.0f, -5.0f, 25f);
        Put(east, "Gascan_Yellow", 15.6f, -4.6f, 110f);
        Put(east, "StandingLight", 18.5f, -6.5f, 200f);
        Put(east, "Construction Light Floor", 15.0f, -1.6f, 250f);
        Put(east, "Boilers", 18.2f, 3.2f, 90f);
        Put(east, "Pipes 02", 20.6f, 7.8f, 0f, 0f, 0.5f);
        Put(east, "Box_Pallet06", 17.5f, 12.8f, 20f);
        Put(east, "Crate_wood01", 20.0f, 11.2f, 40f);
        Put(east, "Ladder_02", 15.6f, 3.2f, -90f);
        Put(east, "Barrel", 18.6f, 0.8f);
        Put(east, "Barrel", 19.3f, 1.4f, 40f);
        Put(east, "Pallet Variation 02", 18.0f, 10.5f, 20f, 0.0f, 0.9f);
        Put(east, "Crate_tarp01small", 19.0f, 13.0f, 0f);
        Put(east, "StandingLight", 14.5f, 13.5f, 250f);

        // --- north side outside the fence: pipe run on sleepers
        var north = Group(root, "North pipe run");
        for (int i = 0; i < 4; i++) PutAt(north, "Duct Straight", new Vector3(-9f + i * 6f, 0.9f, 17.2f), 90f);
        Put(north, "Barrel", 11.5f, 16.2f);
        Put(north, "Barrel", 12.2f, 16.8f, 30f);

        // --- inside the fence, in the corners only
        var inner = Group(root, "Unit corners");
        Put(inner, "Barrel", 11.2f, 12.6f);
        Put(inner, "Gascan_Red", 10.6f, 12.4f, 60f);
        Put(inner, "Crate_s02", -11.0f, 12.4f, 20f);

        // --- people
        var people = Group(root, "People");
        Person(people, SEC, 3.2f, -7.4f, 180f, "Wait");                   // guard beside the gate, facing the approach
        Person(people, CM1, -5.2f, 12.0f, 200f, "Note");                  // operator logging V-101
        Person(people, CF1, 10.9f, 9.0f, -90f, "Documents");              // operator at the drum
        Person(people, CM2, 2.0f, 12.4f, 0f, "WorkMid");
        Person(people, CM4, -6.6f, -11.8f, 90f, "Talk");                  // two operators by the cabin
        Person(people, BM1, -5.5f, -11.1f, -90f, "Talk");
        Person(people, DEL, 16.8f, -6.0f, 0f, "Crouch");                  // at the generator
        // a pair at the muster point, which moves with the wind: follow it
        var crew = Group(people, "Muster crew").gameObject.AddComponent<FollowTarget>();
        crew.TargetName = "Muster point";
        crew.LocalPosition = Vector3.zero;
        Person(crew.transform, CM2, -0.5f, -2.4f, 90f, "Talk");
        Person(crew.transform, CF1, 0.5f, -2.4f, -90f, "Talk");
    }
}
