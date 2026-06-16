using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — swaps the Player's primitive procedural body for a real rigged,
// skinned character driven by imported animation clips (idle/walk/run blend).
// Works on any glTFast-imported GLB that carries those clips, so the same setup
// upgrades from the RobotExpressive placeholder to a Higgsfield-generated hobo by
// just changing GlbPath. Re-runnable: rebuilds the controller + re-attaches.
public static class HoboLifeCharacterRig
{
    // Point this at whichever rigged character GLB is current.
    const string GlbPath = "Assets/Characters/RobotExpressive.glb";
    const string ControllerPath = "Assets/Characters/HoboLocomotion.controller";

    [MenuItem("HoboLife/Setup Rigged Character")]
    public static void Setup()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
        if (prefab == null) { Debug.LogError("[HoboLife] No character GLB at " + GlbPath); return; }

        // --- gather clips from the GLB ---
        var clips = new List<AnimationClip>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(GlbPath))
            if (o is AnimationClip c) clips.Add(c);
        var idle = FindClip(clips, "idle", "stand", "breathing");
        var walk = FindClip(clips, "walk");
        var run = FindClip(clips, "run", "jog", "sprint");
        if (walk == null) walk = idle;              // graceful fallbacks
        if (run == null) run = walk;
        if (idle == null) idle = walk;
        Debug.Log($"[HoboLife] clips -> idle={Name(idle)} walk={Name(walk)} run={Name(run)} (of {clips.Count})");

        // --- build a 1-D locomotion blend tree controller ---
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        var sm = controller.layers[0].stateMachine;
        var state = sm.AddState("Locomotion");
        state.motion = tree;
        sm.defaultState = state;
        if (idle != null) tree.AddChild(idle, 0f);
        if (walk != null) tree.AddChild(walk, 1.6f);
        if (run != null) tree.AddChild(run, 5.0f);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        // --- attach to the Player ---
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[HoboLife] No Player in scene."); return; }

        var oldRig = player.transform.Find("CharacterRig");
        if (oldRig) Object.DestroyImmediate(oldRig.gameObject);
        var body = player.transform.Find("Body");
        if (body) body.gameObject.SetActive(false);                 // hide procedural primitives
        var proc = player.GetComponent<CharacterAnimator>();
        if (proc) proc.enabled = false;                            // stop the procedural animator

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (inst == null) inst = (GameObject)Object.Instantiate(prefab);
        inst.name = "CharacterRig";
        inst.transform.SetParent(player.transform, false);
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        // auto-fit: scale so the character is ~1.8 m and stand its feet at the capsule bottom
        var rends = inst.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            float h = b.size.y;
            if (h > 0.01f) inst.transform.localScale = Vector3.one * (1.8f / h);
        }
        inst.transform.localPosition = new Vector3(0f, -1.0f, 0f);  // capsule half-height; tune if needed

        var anim = inst.GetComponent<Animator>();
        if (anim == null) anim = inst.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        var drv = inst.GetComponent<CharacterRigDriver>();
        if (drv == null) drv = inst.AddComponent<CharacterRigDriver>();
        drv.controller = player.GetComponent<CharacterController>();

        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Rigged character attached to Player (procedural body hidden).");
    }

    static AnimationClip FindClip(List<AnimationClip> clips, params string[] keys)
    {
        foreach (var k in keys)
            foreach (var c in clips)
                if (c != null && c.name.ToLowerInvariant().Contains(k)) return c;
        return null;
    }

    static string Name(Object o) { return o != null ? o.name : "<none>"; }
}
