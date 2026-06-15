using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// HoboLife — one-click builder for the Milestone 1 "move around" scene.
//
// Menu:  HoboLife > Build Milestone 1 Scene
//
// Creates a ground plane, a few box "buildings", a capsule Player with a
// CharacterController + ThirdPersonController + PlayerStats, and wires the
// Main Camera up with OrbitCamera. Saves to Assets/Scenes/Milestone1.unity.
// Re-runnable: it always starts from a fresh default scene.
public static class HoboLifeSceneBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/Milestone1.unity";

    [MenuItem("HoboLife/Build Milestone 1 Scene")]
    public static void BuildMilestone1()
    {
        // Fresh scene with a default Main Camera + Directional Light.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Ground (60 x 60 units) ---
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        Paint(ground, new Color(0.30f, 0.32f, 0.30f)); // asphalt grey

        // --- A few "buildings" ---
        CreateBuilding("Building_A", new Vector3(-10f, 3f, 8f),  new Vector3(6f, 6f, 6f),  new Color(0.55f, 0.35f, 0.30f));
        CreateBuilding("Building_B", new Vector3(9f, 5f, 11f),   new Vector3(7f, 10f, 7f), new Color(0.40f, 0.45f, 0.55f));
        CreateBuilding("Building_C", new Vector3(15f, 2f, -6f),  new Vector3(5f, 4f, 8f),  new Color(0.60f, 0.55f, 0.40f));
        CreateBuilding("Building_D", new Vector3(-13f, 4f, -10f), new Vector3(8f, 8f, 6f), new Color(0.45f, 0.50f, 0.45f));

        // --- Player (capsule) ---
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 0f);
        // CharacterController handles collision, so drop the primitive's capsule collider.
        Collider primCol = player.GetComponent<Collider>();
        if (primCol != null) Object.DestroyImmediate(primCol);
        Paint(player, new Color(0.72f, 0.52f, 0.38f)); // skin/hobo tone

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = Vector3.zero;
        cc.height = 2f;
        cc.radius = 0.5f;

        player.AddComponent<PlayerStats>();

        // --- Camera wiring ---
        Camera cam = Camera.main;
        OrbitCamera orbit = cam.gameObject.AddComponent<OrbitCamera>();
        orbit.target = player.transform;

        // --- Movement wiring ---
        ThirdPersonController tpc = player.AddComponent<ThirdPersonController>();
        tpc.cameraTransform = cam.transform;

        // --- Save & register the scene ---
        if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        Selection.activeGameObject = player;

        Debug.Log("HoboLife: Milestone 1 scene built at " + ScenePath +
                  ". Press Play — WASD to move, drag mouse to orbit, scroll to zoom.");
    }

    // One-time bootstrap: if the Milestone 1 scene doesn't exist yet, build it
    // automatically after scripts compile. Goes inert once the scene exists, so
    // it never disturbs later edits. (Lets the scene build without a menu click.)
    [UnityEditor.Callbacks.DidReloadScripts]
    static void AutoBuildOnce()
    {
        if (File.Exists(ScenePath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                BuildMilestone1();
        };
    }

    static void CreateBuilding(string name, Vector3 pos, Vector3 size, Color color)
    {
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = name;
        b.transform.position = pos;
        b.transform.localScale = size;
        Paint(b, color);
    }

    // Assigns a URP/Lit (or Standard fallback) material of the given colour.
    static void Paint(GameObject go, Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.color = c;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == path))
        {
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
