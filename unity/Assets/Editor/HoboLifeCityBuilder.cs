using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — builds a bigger walkable LA block: a large ground, a road cross,
// and 10 named landmark buildings (solid, with a BuildingDoor for E-to-enter).
// Replaces the placeholder Building_A..D. Auto-builds once on reload.
public static class HoboLifeCityBuilder
{
    struct Bld
    {
        public string name, kind;
        public float x, z, w, d, h;
        public Color color;
        public Bld(string name, string kind, float x, float z, float w, float d, float h, Color color)
        { this.name = name; this.kind = kind; this.x = x; this.z = z; this.w = w; this.d = d; this.h = h; this.color = color; }
    }

    static readonly Bld[] Buildings =
    {
        new Bld("LA City University", "university", -24f, 20f, 13f, 11f, 12f, new Color(0.40f, 0.46f, 0.62f)),
        new Bld("Iron Paradise Gym",  "gym",         24f, 20f, 11f, 11f,  8f, new Color(0.62f, 0.34f, 0.30f)),
        new Bld("First National Bank","bank",       -24f,-20f, 11f, 11f, 15f, new Color(0.66f, 0.60f, 0.38f)),
        new Bld("Greasy Spoon Diner", "diner",       24f,-20f,  9f,  9f,  6f, new Color(0.78f, 0.55f, 0.30f)),
        new Bld("Mercy Hospital",     "hospital",     0f, 38f, 17f, 11f, 12f, new Color(0.80f, 0.82f, 0.84f)),
        new Bld("Threadbare Clothing","clothing",   -40f,  2f,  9f, 13f,  8f, new Color(0.52f, 0.40f, 0.60f)),
        new Bld("Lucky Stick Casino", "casino",      40f,  2f, 11f, 13f, 10f, new Color(0.66f, 0.32f, 0.52f)),
        new Bld("Honest Hal Autos",   "cardealer",    0f,-38f, 15f, 11f,  7f, new Color(0.34f, 0.55f, 0.55f)),
        new Bld("Skyline Realty",     "realtor",    -40f, 30f,  9f,  9f, 17f, new Color(0.40f, 0.62f, 0.66f)),
        new Bld("Quick Cash Pawn",    "pawn",        40f, 30f,  9f,  9f,  7f, new Color(0.55f, 0.45f, 0.32f)),
    };

    [MenuItem("HoboLife/Build City")]
    public static void BuildCity()
    {
        var existing = GameObject.Find("City");
        if (existing) Object.DestroyImmediate(existing);
        var root = new GameObject("City").transform;

        // Ground -> 130x130
        var ground = GameObject.Find("Ground");
        if (ground == null) { ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground"; }
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(13f, 1f, 13f);
        Paint(ground, new Color(0.27f, 0.30f, 0.29f));

        // Roads (thin slabs just above the ground; no colliders)
        Slab(root, "Road_NS", new Vector3(0f, 0.02f, 0f), new Vector3(9f, 0.04f, 118f), new Color(0.11f, 0.11f, 0.12f));
        Slab(root, "Road_EW", new Vector3(0f, 0.02f, 0f), new Vector3(118f, 0.04f, 9f), new Color(0.11f, 0.11f, 0.12f));
        Slab(root, "Sidewalk_NS", new Vector3(0f, 0.015f, 0f), new Vector3(13f, 0.03f, 118f), new Color(0.42f, 0.43f, 0.44f));
        Slab(root, "Sidewalk_EW", new Vector3(0f, 0.015f, 0f), new Vector3(118f, 0.03f, 13f), new Color(0.42f, 0.43f, 0.44f));

        // Buildings (solid colliders block movement)
        foreach (var b in Buildings)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Building_" + b.kind;
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(b.x, b.h / 2f, b.z);
            go.transform.localScale = new Vector3(b.w, b.h, b.d);
            Paint(go, b.color);
            var door = go.AddComponent<BuildingDoor>();
            door.displayName = b.name;
            door.kind = b.kind;
        }

        // Remove placeholder boxes
        foreach (var n in new[] { "Building_A", "Building_B", "Building_C", "Building_D" })
        {
            var g = GameObject.Find(n);
            if (g) Object.DestroyImmediate(g);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] City built (" + Buildings.Length + " landmarks).");
    }

    [DidReloadScripts]
    static void AutoBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && GameObject.Find("City") == null && GameObject.Find("Ground") != null)
                BuildCity();
        };
    }

    static void Slab(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
        Paint(go, color);
    }

    static void Paint(GameObject go, Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }
}
