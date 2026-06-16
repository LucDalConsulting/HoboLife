using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — street life: painted crosswalks + dashed lane lines at the central
// intersection, and a handful of parked cars along the curbs. Reads the road
// bounds at build time so it self-fits, and is built from primitives (no assets).
public static class HoboLifeStreetDetail
{
    [MenuItem("HoboLife/Build Street Detail (crosswalks + cars)")]
    public static void Build()
    {
        var old = GameObject.Find("StreetDetail");
        if (old) Object.DestroyImmediate(old);
        var root = new GameObject("StreetDetail").transform;

        // figure out the road footprint from the actual meshes
        float halfW = 7f, ext = 48f;            // sensible fallbacks
        var roadNS = GameObject.Find("Road_NS");
        if (roadNS != null)
        {
            var r = roadNS.GetComponent<Renderer>();
            if (r != null) { halfW = Mathf.Min(r.bounds.size.x, r.bounds.size.z) * 0.5f; ext = Mathf.Max(r.bounds.size.x, r.bounds.size.z) * 0.5f; }
        }
        float cross = halfW + 1.2f;             // crosswalk sits just outside the intersection
        var paint = Mat(new Color(0.93f, 0.93f, 0.9f), 0.05f);
        var paintFaint = Mat(new Color(0.85f, 0.85f, 0.8f), 0.05f);

        // --- crosswalks on all four approaches ---
        Crosswalk(root, paint, new Vector3(0f, 0f, cross), true, halfW);   // north
        Crosswalk(root, paint, new Vector3(0f, 0f, -cross), true, halfW);  // south
        Crosswalk(root, paint, new Vector3(cross, 0f, 0f), false, halfW);  // east
        Crosswalk(root, paint, new Vector3(-cross, 0f, 0f), false, halfW); // west

        // --- dashed centre lines down each road (skipping the intersection) ---
        LaneDashes(root, paintFaint, true, halfW, ext);   // N-S road, dashes along Z
        LaneDashes(root, paintFaint, false, halfW, ext);  // E-W road, dashes along X

        // --- parked cars along the curbs ---
        float curb = halfW - 1.1f;
        Car(root, new Vector3(curb, 0f, 16f), 0f, new Color(0.75f, 0.18f, 0.16f));   // red, facing +Z
        Car(root, new Vector3(curb, 0f, 24f), 0f, new Color(0.2f, 0.3f, 0.55f));     // blue
        Car(root, new Vector3(-curb, 0f, -18f), 180f, new Color(0.85f, 0.8f, 0.2f)); // yellow, facing -Z
        Car(root, new Vector3(-16f, 0f, curb), 90f, new Color(0.3f, 0.5f, 0.3f));    // green, facing +X
        Car(root, new Vector3(22f, 0f, -curb), 270f, new Color(0.8f, 0.8f, 0.82f));  // white-ish

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Street detail built: crosswalks + lane dashes + parked cars.");
    }

    static void Crosswalk(Transform parent, Material m, Vector3 center, bool acrossX, float halfW)
    {
        var g = new GameObject("Crosswalk").transform; g.SetParent(parent, false); g.position = center;
        int stripes = 6;
        float span = halfW * 1.7f;             // total band width across the road
        for (int i = 0; i < stripes; i++)
        {
            float t = (i / (float)(stripes - 1) - 0.5f) * span;
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            s.name = "Stripe" + i; s.SetParent(g, false);
            Strip(s);
            if (acrossX) { s.localPosition = new Vector3(t, 0.02f, 0f); s.localScale = new Vector3(span / stripes * 0.55f, 0.04f, 2.2f); }
            else { s.localPosition = new Vector3(0f, 0.02f, t); s.localScale = new Vector3(2.2f, 0.04f, span / stripes * 0.55f); }
            s.GetComponent<Renderer>().sharedMaterial = m;
        }
    }

    static void LaneDashes(Transform parent, Material m, bool alongZ, float halfW, float ext)
    {
        var g = new GameObject("LaneLine").transform; g.SetParent(parent, false);
        float gap = 4.5f;
        for (float d = halfW + 3f; d < ext - 2f; d += gap)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                s.name = "Dash"; s.SetParent(g, false); Strip(s);
                if (alongZ) { s.localPosition = new Vector3(0f, 0.02f, sign * d); s.localScale = new Vector3(0.28f, 0.04f, 2.0f); }
                else { s.localPosition = new Vector3(sign * d, 0.02f, 0f); s.localScale = new Vector3(2.0f, 0.04f, 0.28f); }
                s.GetComponent<Renderer>().sharedMaterial = m;
            }
        }
    }

    static void Car(Transform parent, Vector3 pos, float yaw, Color color)
    {
        var car = new GameObject("ParkedCar").transform;
        car.SetParent(parent, false); car.position = pos; car.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var bodyMat = Mat(color, 0.7f);
        var glassMat = Mat(new Color(0.12f, 0.14f, 0.18f), 0.85f);
        var tyreMat = Mat(new Color(0.07f, 0.07f, 0.08f), 0.2f);

        var body = Prim(car, "Body", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.8f, 0.6f, 4.0f), bodyMat);
        var cabin = Prim(car, "Cabin", PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.2f), new Vector3(1.6f, 0.55f, 2.0f), bodyMat);
        Prim(car, "Glass", PrimitiveType.Cube, new Vector3(0f, 1.06f, -0.2f), new Vector3(1.62f, 0.4f, 1.6f), glassMat);
        float wx = 0.82f, wz = 1.25f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var w = Prim(car, "Wheel", PrimitiveType.Cylinder, new Vector3(sx * wx, 0.32f, sz * wz), new Vector3(0.34f, 0.12f, 0.34f), tyreMat);
                w.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
    }

    static void Strip(Transform t) { var c = t.GetComponent<Collider>(); if (c) Object.DestroyImmediate(c); }

    static Transform Prim(Transform parent, string name, PrimitiveType type, Vector3 lp, Vector3 scale, Material m)
    {
        var c = GameObject.CreatePrimitive(type);
        c.name = name; c.transform.SetParent(parent, false);
        c.transform.localPosition = lp; c.transform.localScale = scale;
        var col = c.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
        c.GetComponent<Renderer>().sharedMaterial = m;
        return c.transform;
    }

    static Material Mat(Color c, float smooth)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    [DidReloadScripts]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && GameObject.Find("StreetDetail") == null && GameObject.Find("Road_NS") != null)
                Build();
        };
    }
}
