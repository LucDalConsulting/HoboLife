using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — builds a simple low-poly blocky humanoid ("hobo in underwear") out of
// primitive cubes, with limb pivot transforms wired to a CharacterAnimator.
// Reusable for the Player and for NPCs.
//
// Layout is sized for a CharacterController of height 2 centered at 0 (feet at
// local y = -1, head top near +1).
public static class HoboLifeCharacterBuilder
{
    [MenuItem("HoboLife/Build Character On Player")]
    public static void BuildOnPlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[HoboLife] No 'Player' GameObject found."); return; }

        // Drop the placeholder capsule mesh (keep CharacterController + scripts).
        var mf = player.GetComponent<MeshFilter>(); if (mf) Object.DestroyImmediate(mf);
        var mr = player.GetComponent<MeshRenderer>(); if (mr) Object.DestroyImmediate(mr);
        var prevBody = player.transform.Find("Body"); if (prevBody) Object.DestroyImmediate(prevBody.gameObject);

        BuildBody(player.transform);
        Debug.Log("[HoboLife] Humanoid built on Player.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("HoboLife/Build NPC Prefab (Hobo)")]
    public static void BuildNpcPrefab()
    {
        const string dir = "Assets/Prefabs";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var root = new GameObject("Hobo");
        var cc = root.AddComponent<CharacterController>();
        cc.center = Vector3.zero; cc.height = 2f; cc.radius = 0.45f;
        BuildBody(root.transform);

        string path = dir + "/Hobo.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[HoboLife] NPC prefab saved -> " + AssetDatabase.GetAssetPath(prefab));
    }

    // Builds the humanoid under 'root' and wires a CharacterAnimator on the root.
    public static void BuildBody(Transform root)
    {
        Material skin = Mat(new Color(0.74f, 0.55f, 0.40f));
        Material undies = Mat(new Color(0.90f, 0.90f, 0.93f));
        Material hair = Mat(new Color(0.24f, 0.17f, 0.11f));

        var body = new GameObject("Body").transform;
        body.SetParent(root, false);
        body.localPosition = Vector3.zero;

        // Pelvis / underwear
        Cube(body, "Pelvis", new Vector3(0f, -0.30f, 0f), new Vector3(0.52f, 0.30f, 0.32f), undies);

        // Torso (pivot) + chest, head, arms
        var torso = Empty(body, "Torso", new Vector3(0f, -0.15f, 0f));
        Cube(torso, "Chest", new Vector3(0f, 0.30f, 0f), new Vector3(0.56f, 0.60f, 0.34f), skin);

        var head = Empty(torso, "Head", new Vector3(0f, 0.70f, 0f));
        Cube(head, "HeadMesh", Vector3.zero, new Vector3(0.34f, 0.36f, 0.34f), skin);
        Cube(head, "Hair", new Vector3(0f, 0.18f, -0.02f), new Vector3(0.38f, 0.12f, 0.40f), hair);

        var armL = Empty(torso, "ArmL", new Vector3(0.40f, 0.45f, 0f));
        Cube(armL, "ArmLMesh", new Vector3(0f, -0.30f, 0f), new Vector3(0.15f, 0.60f, 0.15f), skin);
        var armR = Empty(torso, "ArmR", new Vector3(-0.40f, 0.45f, 0f));
        Cube(armR, "ArmRMesh", new Vector3(0f, -0.30f, 0f), new Vector3(0.15f, 0.60f, 0.15f), skin);

        // Legs (pivot at hips, under Body so torso lean doesn't move them)
        var legL = Empty(body, "LegL", new Vector3(0.16f, -0.35f, 0f));
        Cube(legL, "LegLMesh", new Vector3(0f, -0.325f, 0f), new Vector3(0.20f, 0.65f, 0.20f), skin);
        var legR = Empty(body, "LegR", new Vector3(-0.16f, -0.35f, 0f));
        Cube(legR, "LegRMesh", new Vector3(0f, -0.325f, 0f), new Vector3(0.20f, 0.65f, 0.20f), skin);

        var anim = root.GetComponent<CharacterAnimator>();
        if (anim == null) anim = root.gameObject.AddComponent<CharacterAnimator>();
        anim.armL = armL; anim.armR = armR; anim.legL = legL; anim.legR = legR;
        anim.torso = torso; anim.head = head;
    }

    static Transform Empty(Transform parent, string name, Vector3 lp)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = lp;
        return g.transform;
    }

    static void Cube(Transform parent, string name, Vector3 lp, Vector3 scale, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = name;
        c.transform.SetParent(parent, false);
        c.transform.localPosition = lp;
        c.transform.localScale = scale;
        var col = c.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);              // body parts have no colliders
        c.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Material Mat(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        return m;
    }
}
