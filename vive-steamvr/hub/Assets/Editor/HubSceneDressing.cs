using UnityEditor;
using UnityEngine;
using PittBradford.XR;

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
    const string MED = "Professions/Medical_Female_01", AM5 = "Adults/Male_Adult_05", AF3 = "Adults/Female_Adult_03";

    // ------------------------------------------------------------------------------------------ helpers

    /// <summary>
    /// Places a prefab so that the centre of its rendered bounds is at (x, z) and the bottom of the bounds is at y.
    /// (The packs' pivots are inconsistent, so bounds are the reliable anchor.) Colliders are disabled unless solid.
    /// </summary>
    /// <summary>Switches off what dressing does not need: colliders, probes, sounds, NavMesh blockers; rigidbodies become kinematic.</summary>
    internal static void Tidy(GameObject go, bool solid)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            if (t != null && t.name.Contains("NavMeshBlocker")) t.gameObject.SetActive(false);
        if (!solid)
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var p in go.GetComponentsInChildren<ReflectionProbe>()) p.enabled = false;
        foreach (var a in go.GetComponentsInChildren<AudioSource>()) a.enabled = false;
        foreach (var l in go.GetComponentsInChildren<Light>()) l.enabled = false; // prop lamps (stand lights, generator): their glow stays in the material; a real light costs a pass per object
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var t in go.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    /// <summary>Gives a prop its own copies of its materials multiplied by a tint (values above 1 brighten); for the Sci-Fi containers whose textures render near black.</summary>
    internal static void Tint(GameObject go, Color mul)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null) { var c = new Material(mats[i]); c.color = mats[i].color * mul; c.name = mats[i].name + " (tinted)"; mats[i] = c; }
            r.sharedMaterials = mats;
        }
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

    /// <summary>Like Put but with a non-uniform scale (multiplies the prefab's own scale), for wall and ceiling pieces stretched to fit a room.</summary>
    public static GameObject PutS(Transform parent, string prefab, float x, float z, float yaw, Vector3 scale, float y = 0f)
    {
        var p = HubDressing.FindPrefab(prefab);
        if (p == null) { Debug.LogWarning($"[dress] prefab not found: {prefab}"); return null; }
        var go = HubDressing.Spawn(parent, prefab, Vector3.zero, yaw, 1f);
        go.transform.localScale = Vector3.Scale(p.transform.localScale, scale);
        Tidy(go, false);
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

    /// <summary>A thin coloured slab (wainscot band, stripe) with a plain Standard material, kept in the scene file.</summary>
    internal static GameObject Band(Transform parent, string name, Vector3 center, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.SetFloat("_Glossiness", 0.35f);
        go.GetComponent<Renderer>().sharedMaterial = m;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    /// <summary>A painted wall slab (Standard material, low gloss); optionally with a Pitt blue and gold base stripe on its +x/-x face.</summary>
    internal static GameObject PaintedWall(Transform parent, string name, Vector3 center, Vector3 size, Color color, bool brandStripe)
    {
        var w = Band(parent, name, center, size, color);
        w.GetComponent<Renderer>().sharedMaterial.SetFloat("_Glossiness", 0.12f);
        if (brandStripe)
        {
            float face = center.x + (center.x > 0 ? -0.11f : 0.11f);
            Band(parent, name + " blue stripe", new Vector3(face, 0.45f, center.z), new Vector3(0.02f, 0.9f, size.z), new Color(0f, 0.208f, 0.58f));
            Band(parent, name + " gold stripe", new Vector3(face, 0.94f, center.z), new Vector3(0.022f, 0.06f, size.z), new Color(1f, 0.722f, 0.11f));
        }
        return w;
    }

    /// <summary>
    /// Puts files, books and boxes on the planks of a Modern Office "Shelving" unit placed at (cx, cz) with the given yaw
    /// (plank tops are at y 0.10, 0.58, 1.02, 1.46, 1.92; the unit is 1.44 wide).
    /// </summary>
    internal static void Stock(Transform parent, float cx, float cz, float yaw)
    {
        float a = yaw * Mathf.Deg2Rad;
        Vector2 ax = new Vector2(Mathf.Cos(a), -Mathf.Sin(a)); // the unit's own x axis in the room plane
        System.Action<string, float, float, float> add = (name, off, y, sc) =>
            Put(parent, name, cx + ax.x * off, cz + ax.y * off, yaw, y, sc);
        add("Files 1", 0f, 0.58f, 0.85f);
        add("Paper box", -0.35f, 1.02f, 1f); add("Box1", 0.1f, 1.02f, 1f); add("Paper box", 0.45f, 1.02f, 1f);
        add("books 1", 0f, 1.46f, 0.85f);
        add("Box1", -0.4f, 0.10f, 1.4f); add("Box1", 0.3f, 0.10f, 1.4f);
        add("Files 1", 0.1f, 1.92f, 0.8f);
    }

    internal static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        return g;
    }

    /// <summary>A person at the floor point, facing yaw (0 = +z, 90 = +x), playing an activity from the Rocketbox controllers.</summary>
    internal static GameObject Person(Transform parent, string avatar, float x, float z, float yaw, string activity, float y = 0f)
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
        Put(south, "Box_Pallet04", -1.8f, -9.6f, 10f);
        Put(south, "Box_Pallet09", 0.0f, -10.9f, 0f);
        Put(south, "Wooden_boards1", 5.5f, -7.6f, 90f);
        Put(south, "Crate01", 7.4f, -7.6f, 25f);
        Put(south, "Cardboard_boxGroup01a", 9.7f, -7.0f, 0f);
        Put(south, "HandTruck", -1.3f, -7.7f, 70f);
        Put(south, "Fire Extinguisher", -4.2f, -11.8f, 0f, 0.9f);
        Put(south, "Garbage Bin", -1.2f, -11.6f);

        // --- supervisor's office corner (Modern Office), against the south wall
        var office = Group(root, "Office corner");
        Put(office, "Table 1", 12.3f, -11.0f, 0f);
        Put(office, "Chair 2", 13.2f, -9.6f, 140f);
        Put(office, "Computer 1", 12.4f, -11.0f, 0f, 0.82f);
        Put(office, "Papers", 11.3f, -11.0f, 0f, 0.82f);
        Put(office, "Lamp 1", 13.3f, -11.1f, 0f, 0.82f);
        Put(office, "File Cabinet", 10.9f, -11.6f, 0f);
        Put(office, "Water cooler", 10.2f, -11.5f, 0f);
        Put(office, "Plant 1", 13.7f, -8.2f);
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
        HubPerfPass.Apply(root);
    }

    // ------------------------------------------------------------------------------------------ refinery

    /// <summary>
    /// Refinery Rounds: props outside the fence and in the corners, kept within about 22 m of the unit centre (0, 5) so
    /// they stay clear of the muster point (26 m upwind). Fence is x -12..12, z -6..14, gate at z -6, cabin at (-10, -10).
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
        Tint(Put(yard, "ShippingContainer_Red", -19.0f, -3.5f, 0f), new Color(3.2f, 1.5f, 1.2f));
        Tint(Put(yard, "ShippingContainer_Blue", -19.0f, -3.5f, 0f, 2.48f), new Color(1.1f, 2.0f, 3.6f));
        Tint(Put(yard, "ShippingContainer_Green", -16.0f, -6.5f, 90f), new Color(1.3f, 2.6f, 1.5f));
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
        Put(east, "Barrel", 19.4f, -1.4f);
        Put(east, "Barrel", 20.1f, -0.8f, 40f);
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
        HubPerfPass.Apply(root);
    }

    // ------------------------------------------------------------------------------------------ hydrostatic test bay

    /// <summary>
    /// Hydrostatic Test Bay: the scenario sits in the middle of the big IGBlocks hall, so this builds a room around it
    /// (inner x -7.4..7.4, z -6.6..6.6, drop ceiling at y 5.7 under the scenario's light panels) from the Sci-Fi kit's wall and
    /// ceiling pieces, then fills it. The equipment (x -3..5, z 0..4.5), valves, pump panel, blind flange table, gauge, task board
    /// and the stay-behind line (z 0.6) stay clear; people stand behind the line.
    /// </summary>
    public static void DressHydro(Transform dressing)
    {
        var root = Group(dressing, "Bay room and props");

        // --- room shell: stretched plain wall pieces, a door in the front wall, drop ceiling
        var shell = Group(root, "Room shell");
        float h = 5.7f / 10f; // the wall pieces are 10 m tall
        PutS(shell, "Wall Plain", 0f, 6.75f, 90f, new Vector3(1f, h, 15.8f / 6f));            // back
        PutS(shell, "Wall Plain", -7.55f, 0f, 0f, new Vector3(1f, h, 13.8f / 6f));             // left
        PutS(shell, "Wall Plain", 7.55f, 0f, 0f, new Vector3(1f, h, 13.8f / 6f));              // right
        PutS(shell, "Wall Plain", 0f, -6.75f, 90f, new Vector3(1f, h, 15.8f / 6f));            // front (with a door prop, below)
        Put(shell, "Door", -3.6f, -6.5f, 0f);
        // Pitt blue wainscot with a gold stripe on all four inner faces, as the stand-alone bay has
        Color blue = new Color(0f, 0.208f, 0.58f), gold = new Color(1f, 0.722f, 0.11f);
        Band(shell, "Blue band", new Vector3(0f, 0.9f, 6.59f), new Vector3(14.8f, 1.8f, 0.04f), blue);
        Band(shell, "Gold stripe", new Vector3(0f, 1.84f, 6.57f), new Vector3(14.8f, 0.08f, 0.04f), gold);
        Band(shell, "Blue band", new Vector3(0f, 0.9f, -6.59f), new Vector3(14.8f, 1.8f, 0.04f), blue);
        Band(shell, "Gold stripe", new Vector3(0f, 1.84f, -6.57f), new Vector3(14.8f, 0.08f, 0.04f), gold);
        Band(shell, "Blue band", new Vector3(7.39f, 0.9f, 0f), new Vector3(0.04f, 1.8f, 13.2f), blue);
        Band(shell, "Gold stripe", new Vector3(7.37f, 1.84f, 0f), new Vector3(0.04f, 0.08f, 13.2f), gold);
        Band(shell, "Blue band", new Vector3(-7.39f, 0.9f, 0f), new Vector3(0.04f, 1.8f, 13.2f), blue);
        Band(shell, "Gold stripe", new Vector3(-7.37f, 1.84f, 0f), new Vector3(0.04f, 0.08f, 13.2f), gold);
        for (int ix = -1; ix <= 1; ix++)
        for (int iz = -1; iz <= 1; iz++)
            PutS(shell, "Ceiling Closed", ix * 5.4f, iz * 4.6f, 0f, Vector3.one, 5.68f);
        PutAt(shell, "Exit Sign", new Vector3(-3.6f, 3.2f, -6.45f), 90f);

        // --- overhead: duct runs under the ceiling, hanging lights, a pipe bundle along the back wall
        var over = Group(root, "Overhead");
        for (int i = 0; i < 2; i++) PutAt(over, "Duct Straight", new Vector3(-5.6f, 4.85f, -3f + i * 6f), 0f);
        for (int i = 0; i < 2; i++) PutAt(over, "Duct Straight", new Vector3(5.6f, 4.85f, -3f + i * 6f), 0f);
        for (int i = 0; i < 3; i++) PutAt(over, "Duct Straight", new Vector3(-4.5f + i * 3f, 5.1f, 6.1f), 90f);
        foreach (var p in new[] { new Vector2(-6.2f, -4f), new Vector2(6.2f, -4f), new Vector2(-6.2f, 2f), new Vector2(6.2f, 2f) })
            PutAt(over, "Hanging Light", new Vector3(p.x, 5.1f, p.y), 0f);

        // --- back wall: fuse box, pipe runs, a ladder and the storage staging (pallets with crates) in the corner
        var back = Group(root, "Back wall");
        Put(back, "Fusebox 01", -1.5f, 6.4f, 0f);
        Put(back, "Pipes 04", -7.2f, 6.0f, 90f, 0f, 0.85f);
        Put(back, "Pipes 06", 7.1f, 6.1f, 90f, 0f, 0.8f);
        Put(back, "Ladder_03a", 4.6f, 6.0f, 0f);
        Tint(Put(back, "Pallet", -5.6f, 5.2f, 10f), new Color(2.4f, 2.1f, 1.7f));
        Tint(Put(back, "Pallet", -3.9f, 5.5f, 80f), new Color(2.4f, 2.1f, 1.7f));
        Put(back, "Barrel", 6.4f, 5.7f);
        Put(back, "Barrel", 6.9f, 5.0f, 40f);
        Put(back, "Barrel", 5.9f, 4.9f, 80f);
        Put(back, "Construction Light Floor", 2.2f, 6.1f, 200f);

        // --- left wall: tool bench area (work table, wall shelving, tool cart), step ladder, a standing light
        var tools = Group(root, "Tool bench area");
        Put(tools, "Table 2", -6.4f, 3.0f, 90f);
        Put(tools, "Shelving", -7.2f, 1.4f, 90f);
        Put(tools, "Shelving", -7.2f, 4.2f, 90f);
        Put(tools, "Shelving", -7.2f, 5.7f, 90f);
        Stock(tools, -7.2f, 1.4f, 90f); Stock(tools, -7.2f, 4.2f, 90f); Stock(tools, -7.2f, 5.7f, 90f);
        Put(tools, "Cart", -6.0f, 5.9f, 90f);
        Put(tools, "Laptop", -6.3f, 3.5f, 100f, 0.77f);
        Put(tools, "Notebook", -6.35f, 2.3f, 80f, 0.77f);
        Put(tools, "Bin", -6.9f, 0.4f);
        Put(tools, "StandingLight", -6.4f, -2.2f, 60f);
        Put(tools, "Ladder_02", -7.15f, -4.6f, 90f);
        PutAt(tools, "Proyector screen", new Vector3(-7.25f, 2.6f, -0.2f), 90f);   // pre-job briefing screen

        // --- front wall: receiving stack beside the door (hand truck, cartons, pallets, barrels), sprinkler runs overhead
        var front = Group(root, "Front wall storage");
        Put(front, "HandTruck", -0.4f, -6.0f, 20f);
        // (removed 2026-11-08: "Cardboard_boxGroup01a" is a stack of raspberry and banana produce boxes, which read as fruit crates in a plant bay)
        Put(front, "Box_Pallet03", 2.6f, -5.9f, 0f);
        Put(front, "Crate_closed", 4.0f, -6.0f, 15f);
        Put(front, "Garbage Bin", 5.3f, -6.2f);
        Put(front, "Fire Extinguisher", -2.0f, -6.45f, 0f, 0.9f);
        Put(front, "Fire Extinguisher", -1.5f, -6.45f, 0f, 0.9f);
        Put(front, "Barrel", -6.5f, -5.9f);
        Put(front, "Barrel", -6.0f, -6.3f, 40f);
        Put(front, "Barrel", -5.9f, -5.5f, 80f);
        Put(front, "Pipes 02", 7.25f, 3.6f, 0f, 0f, 0.75f);
        for (int i = 0; i < 2; i++) PutAt(over, "Sprinkler 01", new Vector3(-2.8f + i * 5.6f, 5.2f, 0f), 0f);


        // --- supervisor's corner (right wall, front): desk, chair, computer, lamp, cabinet, water cooler, plant (right wall, front)
        var office = Group(root, "Supervisor corner");
        Put(office, "Table 1", 6.75f, -3.1f, 90f);
        Put(office, "Chair 2", 5.75f, -3.1f, 90f);
        Put(office, "Computer 1", 6.85f, -3.1f, -90f, 0.82f);
        Put(office, "Papers", 6.5f, -3.9f, 0f, 0.82f);
        Put(office, "Lamp 1", 7.1f, -2.4f, 90f, 0.82f);
        Put(office, "File Cabinet", 7.15f, -1.0f, 90f, 0f, 0.5f);
        Put(office, "Water cooler", 7.1f, -5.4f, -90f);
        Put(office, "Plant 4", 6.5f, -6.0f);

        // --- notice board, posters and floor markings (kit panels): front wall above the receiving stack, back wall, right wall
        var notes = Group(root, "Notices and floor markings");
        HubLobbyDressing.Notice(notes, "Bay notice board", new Vector3(2.4f, 2.7f, -6.55f), Vector3.forward, new Vector2(2.2f, 1.3f), "BAY NOTICES",
            new[] { "Eye protection in the bay.", "Keep clear of the line during a hold.", "Report any leak or drip." }, new Color(0f, 0.13f, 0.36f, 0.97f));
        HubLobbyDressing.Notice(notes, "Poster: stay clear", new Vector3(-3.0f, 2.9f, 6.55f), Vector3.back, new Vector2(1.5f, 1.0f), "STAY CLEAR",
            new[] { "Never stand over a", "pressurised line." }, new Color(0.55f, 0.1f, 0.05f, 0.97f), 0.07f);
        HubLobbyDressing.Notice(notes, "Poster: housekeeping", new Vector3(-5.0f, 2.9f, 6.55f), Vector3.back, new Vector2(1.5f, 1.0f), "HOUSEKEEPING",
            new[] { "Tools back on the", "cart after each task." }, new Color(0.1f, 0.4f, 0.25f, 0.97f), 0.07f);
        PutAt(notes, "World map", new Vector3(7.33f, 2.7f, -3.2f), -90f);
        Color yellow = new Color(1f, 0.8f, 0.1f);
        Band(notes, "Walkway line", new Vector3(-4.35f, 0.008f, -4.0f), new Vector3(0.1f, 0.012f, 4.4f), yellow);
        Band(notes, "Walkway line", new Vector3(-2.85f, 0.008f, -4.0f), new Vector3(0.1f, 0.012f, 4.4f), yellow);

        // --- people: everyone stays behind the stay-behind line (z 0.6)
        var people = Group(root, "People");
        Person(people, BM1, 2.8f, -1.7f, -35f, "Note");          // test supervisor logging the gauge readout
        Person(people, CM2, -5.6f, 3.0f, -90f, "WorkTable");     // technician at the tool bench
        Person(people, CM4, 4.2f, -1.5f, 90f, "Talk");           // two operators talking near the pump skid
        Person(people, CF1, 5.2f, -1.5f, -90f, "Talk");
        Person(people, SEC, -5.2f, 0.0f, 0f, "Wait");            // safety officer at the line
        Person(people, BF1, 5.6f, -4.3f, 90f, "Documents");      // at the supervisor's desk
        HubPerfPass.Apply(root);
    }

    // ------------------------------------------------------------------------------------------ lobby

    /// <summary>
    /// Lobby: a reception corner (east), a seating and coffee lounge (west) and plants, around the four portal pads, the welcome
    /// board, the season chip, the brand plate and the walking lanes (the centre strip and the diagonals to the pads stay clear).
    /// Hall is x -12..12, z -9..9; the pads are at z 5.7; the start is at (0, -6.5).
    /// </summary>
    public static void DressLobby(Transform dressing) => HubLobbyDressing.Build(dressing);

    // ------------------------------------------------------------------------------------------ resistor bench

    /// <summary>
    /// Resistor Bench: closes the open room (side and front walls from the Sci-Fi kit) and fills it as a small electronics lab with
    /// Modern Office furniture. The room is x -4..4, z -3..1.35 (the scenario's own back wall carries the wall cards), ceiling 3.25.
    /// The bench (x +-1.7, z 0.4..1.2), the parts bins, wall cards, answer panel and rig area (x +-2.5, z -0.95..0.15) stay clear.
    /// </summary>
    public static void DressResistor()
    {
        var root = new GameObject("Dressing").transform;

        // --- walls (side and front), with the same blue and gold stripes as the back wall
        var shell = Group(root, "Walls");
        var paint = new Color(0.8f, 0.82f, 0.84f);
        PaintedWall(shell, "Left wall", new Vector3(-4.05f, 1.6f, -0.85f), new Vector3(0.1f, 3.2f, 4.6f), paint, false);
        PaintedWall(shell, "Right wall", new Vector3(4.05f, 1.6f, -0.85f), new Vector3(0.1f, 3.2f, 4.6f), paint, false);
        PaintedWall(shell, "Front wall", new Vector3(0f, 1.6f, -3.05f), new Vector3(8.2f, 3.2f, 0.1f), paint, false);
        Band(shell, "Base trim left", new Vector3(-3.995f, 0.06f, -0.85f), new Vector3(0.02f, 0.12f, 4.5f), new Color(0.16f, 0.17f, 0.19f));
        Band(shell, "Base trim right", new Vector3(3.995f, 0.06f, -0.85f), new Vector3(0.02f, 0.12f, 4.5f), new Color(0.16f, 0.17f, 0.19f));
        Band(shell, "Base trim front", new Vector3(0f, 0.06f, -2.995f), new Vector3(8f, 0.12f, 0.02f), new Color(0.16f, 0.17f, 0.19f));
        Color blue = new Color(0f, 0.208f, 0.58f), gold = new Color(1f, 0.722f, 0.11f);
        Band(shell, "Blue band", new Vector3(-3.99f, 2.6f, -0.82f), new Vector3(0.02f, 0.25f, 4.4f), blue);
        Band(shell, "Gold stripe", new Vector3(-3.985f, 2.76f, -0.82f), new Vector3(0.02f, 0.04f, 4.4f), gold);
        Band(shell, "Blue band", new Vector3(3.99f, 2.6f, -0.82f), new Vector3(0.02f, 0.25f, 4.4f), blue);
        Band(shell, "Gold stripe", new Vector3(3.985f, 2.76f, -0.82f), new Vector3(0.02f, 0.04f, 4.4f), gold);
        Band(shell, "Blue band", new Vector3(0f, 2.6f, -2.99f), new Vector3(8f, 0.25f, 0.02f), blue);
        Band(shell, "Gold stripe", new Vector3(0f, 2.76f, -2.985f), new Vector3(8f, 0.04f, 0.02f), gold);

        // --- left wall: side table with two computers and a laptop for the students, shelving, cabinet, plant
        var left = Group(root, "Student table and shelving");
        Put(left, "Table 2", -3.45f, -1.65f, 90f);
        Put(left, "Computer 1", -3.55f, -1.05f, 90f, 0.77f);
        Put(left, "Laptop", -3.45f, -2.3f, 90f, 0.77f);
        Put(left, "Notebook", -3.2f, -1.6f, 80f, 0.77f);
        Put(left, "Pens", -3.1f, -1.95f, 0f, 0.77f);
        Put(left, "Lamp 1", -3.75f, -1.7f, 90f, 0.77f);
        Put(left, "Shelving", -3.7f, 0.5f, 90f);
        Stock(left, -3.7f, 0.5f, 90f);
        Put(left, "Bin", -3.4f, -0.1f);
        PutAt(left, "World map", new Vector3(-3.96f, 1.85f, -1.65f), 90f);

        // --- right wall: projector screen with a world map beside it, shelving, cabinets, plant, clock on the back wall
        var right = Group(root, "Projector wall");
        PutAt(right, "Proyector screen", new Vector3(3.95f, 1.95f, -1.5f), -90f);
        Put(right, "Shelving", 3.7f, 0.5f, -90f);
        Stock(right, 3.7f, 0.5f, -90f);
        Put(right, "File Cabinet", 3.75f, -2.0f, -90f, 0f, 0.5f);
        Put(right, "Plant 4", 3.6f, -0.25f);
        Put(right, "Bin", 3.5f, -2.8f);
        PutAt(right, "Clock", new Vector3(3.0f, 2.55f, 1.27f), 180f);
        Put(right, "Cardboard_box03", 3.45f, -2.6f, 20f, 0f, 0.6f);

        // --- front wall (behind the rig): door, shelving, plants, map, exit sign
        var front = Group(root, "Front wall");
        Put(front, "Door", 2.0f, -2.95f, 0f);
        Put(front, "Shelving", -1.2f, -2.7f, 0f);
        Put(front, "Shelving", 0.3f, -2.7f, 0f);
        Stock(front, -1.2f, -2.7f, 0f); Stock(front, 0.3f, -2.7f, 0f);
        Put(front, "Plant 4", -2.4f, -2.65f);
        Put(front, "Plant 2", 3.3f, -2.7f);
        PutAt(front, "Exit Sign", new Vector3(2.0f, 2.8f, -2.9f), 90f);
        Put(front, "Fire Extinguisher", 2.95f, -2.93f, 180f, 0.9f);

        // --- people
        var people = Group(root, "People");
        Person(people, BM1, 2.5f, 0.7f, -113f, "Talk");         // instructor beside the wall references
        Person(people, AM5, -2.85f, -2.3f, -90f, "WorkTable"); // two students at the side table
        Person(people, AF3, -2.85f, -1.15f, -90f, "Note");
        Person(people, MED, 3.1f, -2.0f, -40f, "Look");         // someone looking around
        HubPerfPass.Apply(root);
    }
}
