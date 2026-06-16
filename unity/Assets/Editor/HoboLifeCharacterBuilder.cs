using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — builds the rounded humanoid onto the Player (delegates to the shared
// runtime HumanoidFactory so player + NPCs always match). Auto-rebuilds the
// player's body when it's the old blocky version (no "Neck" part yet).
public static class HoboLifeCharacterBuilder
{
    static readonly Color Skin = new Color(0.74f, 0.55f, 0.40f);
    static readonly Color Undies = new Color(0.90f, 0.90f, 0.93f);
    static readonly Color Hair = new Color(0.24f, 0.17f, 0.11f);

    [MenuItem("HoboLife/Build Character On Player")]
    public static void BuildOnPlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[HoboLife] No 'Player' GameObject found."); return; }

        var mf = player.GetComponent<MeshFilter>(); if (mf) Object.DestroyImmediate(mf);
        var mr = player.GetComponent<MeshRenderer>(); if (mr) Object.DestroyImmediate(mr);
        var prevBody = player.transform.Find("Body"); if (prevBody) Object.DestroyImmediate(prevBody.gameObject);

        HumanoidFactory.BuildBody(player.transform, Skin, Undies, Hair);
        Debug.Log("[HoboLife] Rounded humanoid built on Player.");

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
        HumanoidFactory.BuildBody(root.transform, Skin, Undies, Hair);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, dir + "/Hobo.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("[HoboLife] NPC prefab saved -> " + AssetDatabase.GetAssetPath(prefab));
    }

    [DidReloadScripts]
    static void AutoRebuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying) return;
            var player = GameObject.Find("Player");
            if (player == null) return;
            var body = player.transform.Find("Body");
            // Rebuild if there's no body, or it's the old blocky one (no "Neck").
            if (body == null || body.Find("Torso/Neck") == null)
                BuildOnPlayer();
        };
    }
}
