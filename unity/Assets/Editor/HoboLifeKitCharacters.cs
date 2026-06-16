using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — swaps the player + every NPC from primitives/placeholder to the real
// rigged Kenney Mini Characters (CC0). All 12 characters share ONE 7-bone skeleton,
// so a single locomotion controller (built from one character's looped idle/walk/
// sprint clips) drives all of them. The player picks one character; NPCs pick at
// random for a varied crowd. Re-runnable.
public static class HoboLifeKitCharacters
{
    const string CharDir = "Assets/Kit/mini-characters/";
    const string PlayerChar = "character-male-b";          // scruffy older guy = our stand-in hobo
    const string ControllerPath = "Assets/Kit/KenneyLocomotion.controller";

    static readonly string[] AllChars = {
        "character-male-a","character-male-b","character-male-c","character-male-d","character-male-e","character-male-f",
        "character-female-a","character-female-b","character-female-c","character-female-d","character-female-e","character-female-f"
    };

    [MenuItem("HoboLife/Setup Kenney Characters (player + NPCs)")]
    public static void Setup()
    {
        var ctrl = BuildController(CharDir + PlayerChar + ".fbx");
        if (ctrl == null) { Debug.LogError("[HoboLife] Could not build character controller."); return; }
        SetupPlayer(ctrl);
        SetupNpcs(ctrl);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Kenney characters wired: player = " + PlayerChar + ", NPCs randomized across " + AllChars.Length + ".");
    }

    static AnimatorController BuildController(string charPath)
    {
        // make the locomotion clips loop (FBX takes import non-looping by default)
        var imp = AssetImporter.GetAtPath(charPath) as ModelImporter;
        if (imp != null)
        {
            var clips = imp.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                var n = clips[i].name.ToLowerInvariant();
                if (n.Contains("idle") || n.Contains("walk") || n.Contains("sprint") || n.Contains("run") || n.Contains("static"))
                    clips[i].loopTime = true;
            }
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        var found = new List<AnimationClip>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(charPath))
        {
            var ac = o as AnimationClip;
            if (ac != null && !ac.name.StartsWith("__preview__")) found.Add(ac);
        }
        var idle = Find(found, "idle");
        var walk = Find(found, "walk");
        var run = Find(found, "sprint", "run", "jog");
        if (walk == null) walk = idle;
        if (run == null) run = walk;
        if (idle == null) idle = walk;
        if (idle == null) { Debug.LogError("[HoboLife] no clips in " + charPath); return null; }

        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        var tree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        var sm = controller.layers[0].stateMachine;
        var state = sm.AddState("Locomotion");
        state.motion = tree;
        sm.defaultState = state;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 1.4f);
        tree.AddChild(run, 4.0f);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HoboLife] controller built: idle={Nm(idle)} walk={Nm(walk)} run={Nm(run)}");
        return controller;
    }

    static void SetupPlayer(AnimatorController ctrl)
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[HoboLife] no Player"); return; }
        foreach (var nm in new[] { "KenneyRig", "CharacterRig" })
        {
            var t = player.transform.Find(nm);
            if (t) Object.DestroyImmediate(t.gameObject);
        }
        var body = player.transform.Find("Body");
        if (body) body.gameObject.SetActive(false);
        var proc = player.GetComponent<CharacterAnimator>();
        if (proc) proc.enabled = false;

        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + PlayerChar + ".fbx");
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        inst.name = "KenneyRig";
        inst.transform.SetParent(player.transform, false);
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        FitAndStand(inst);

        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        var drv = inst.GetComponent<CharacterRigDriver>();
        if (drv == null) drv = inst.AddComponent<CharacterRigDriver>();
        drv.controller = player.GetComponent<CharacterController>();
    }

    static void SetupNpcs(AnimatorController ctrl)
    {
        var spawner = Object.FindFirstObjectByType<NpcSpawner>();
        if (spawner == null) { Debug.LogWarning("[HoboLife] no NpcSpawner in scene"); return; }
        var prefabs = new List<GameObject>();
        foreach (var c in AllChars)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + c + ".fbx");
            if (pf != null) prefabs.Add(pf);
        }
        // compute a scale that makes a character ~1.75 m, reused by the runtime spawner
        float scale = 2.68f;
        if (prefabs.Count > 0)
        {
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[0]);
            probe.transform.localScale = Vector3.one;
            float h = HeightOf(probe);
            if (h > 0.01f) scale = 1.75f / h;
            Object.DestroyImmediate(probe);
        }
        spawner.characterPrefabs = prefabs.ToArray();
        spawner.animController = ctrl;
        spawner.charScale = scale;
        spawner.charYOffset = -1.0f;
        EditorUtility.SetDirty(spawner);
        Debug.Log("[HoboLife] NpcSpawner populated with " + prefabs.Count + " character prefabs, scale " + scale.ToString("0.00"));
    }

    static void FitAndStand(GameObject inst)
    {
        float h = HeightOf(inst);
        if (h > 0.01f) inst.transform.localScale = Vector3.one * (1.75f / h);
        inst.transform.localPosition = new Vector3(0f, -1.0f, 0f);   // base pivot -> feet at capsule bottom
    }

    static float HeightOf(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 0f;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b.size.y;
    }

    static AnimationClip Find(List<AnimationClip> clips, params string[] keys)
    {
        foreach (var k in keys)
            foreach (var c in clips)
                if (c.name.ToLowerInvariant().Contains(k)) return c;
        return null;
    }

    static string Nm(Object o) { return o != null ? o.name : "<none>"; }
}
