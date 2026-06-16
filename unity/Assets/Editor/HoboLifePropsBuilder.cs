using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — street dressing: palm trees and glowing street lamps along the
// roads for LA flavor and depth. Built from primitives. Auto-builds once.
public static class HoboLifePropsBuilder
{
    [MenuItem("HoboLife/Build Props (palms + lamps)")]
    public static void BuildProps()
    {
        var existing = GameObject.Find("Props");
        if (existing) Object.DestroyImmediate(existing);
        var root = new GameObject("Props").transform;

        float[] along = { -44f, -32f, -20f, 20f, 32f, 44f };
        foreach (float z in along) { PalmTree(root, new Vector3(8f, 0f, z)); PalmTree(root, new Vector3(-8f, 0f, z)); }
        foreach (float x in along) { PalmTree(root, new Vector3(x, 0f, 8f)); PalmTree(root, new Vector3(x, 0f, -8f)); }

        float[] lampZ = { -26f, 26f };
        foreach (float z in lampZ) { Lamp(root, new Vector3(8f, 0f, z)); Lamp(root, new Vector3(-8f, 0f, z)); }
        Lamp(root, new Vector3(8f, 0f, 8f)); Lamp(root, new Vector3(-8f, 0f, 8f));
        Lamp(root, new Vector3(8f, 0f, -8f)); Lamp(root, new Vector3(-8f, 0f, -8f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Props built: palm trees + street lamps.");
    }

    static void PalmTree(Transform parent, Vector3 pos)
    {
        var tree = new GameObject("PalmTree").transform;
        tree.SetParent(parent, false);
        tree.position = pos;
        tree.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        var bark = Mat(new Color(0.46f, 0.34f, 0.22f), 0.15f, false);
        var trunk = Prim(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2.4f, 0f), new Vector3(0.32f, 2.4f, 0.32f), bark);
        trunk.localRotation = Quaternion.Euler(4f, 0f, 3f);

        var leafMat = Mat(new Color(0.20f, 0.50f, 0.17f), 0.2f, false);
        var crown = new GameObject("Fronds").transform; crown.SetParent(tree, false); crown.localPosition = new Vector3(0.12f, 4.75f, 0.08f);
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * 360f;
            var f = Prim(crown, "Frond" + i, PrimitiveType.Capsule, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -0.25f, 1.0f), new Vector3(0.22f, 0.95f, 0.22f), leafMat);
            f.localRotation = Quaternion.Euler(62f, a, 0f);
        }
    }

    static void Lamp(Transform parent, Vector3 pos)
    {
        var lamp = new GameObject("Lamp").transform;
        lamp.SetParent(parent, false);
        lamp.position = pos;
        var poleMat = Mat(new Color(0.18f, 0.18f, 0.2f), 0.4f, false);
        Prim(lamp, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 2.3f, 0f), new Vector3(0.12f, 2.3f, 0.12f), poleMat);
        var headMat = Mat(new Color(1f, 0.86f, 0.5f), 0f, true);
        Prim(lamp, "Head", PrimitiveType.Sphere, new Vector3(0f, 4.55f, 0f), new Vector3(0.42f, 0.42f, 0.42f), headMat);
    }

    static Transform Prim(Transform parent, string name, PrimitiveType type, Vector3 lp, Vector3 scale, Material m)
    {
        var c = GameObject.CreatePrimitive(type);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.transform.localPosition = lp;
        c.transform.localScale = scale;
        var col = c.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
        c.GetComponent<Renderer>().sharedMaterial = m;
        return c.transform;
    }

    static Material Mat(Color c, float smooth, bool emissive)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(1f, 0.82f, 0.45f) * 2.6f);
        }
        return m;
    }

    [DidReloadScripts]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && GameObject.Find("Props") == null && GameObject.Find("City") != null)
                BuildProps();
        };
    }
}
