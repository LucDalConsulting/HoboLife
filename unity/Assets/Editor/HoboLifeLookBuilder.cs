using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// HoboLife — cinematic look: a global post-processing Volume (ACES tonemapping,
// bloom, color grading, warm white balance, vignette), warm soft-shadowed
// sunlight, trilight ambient, and atmospheric fog. The single biggest visual
// upgrade over flat unlit boxes.
public static class HoboLifeLookBuilder
{
    const string ProfilePath = "Assets/Settings/HoboLifePostFX.asset";

    [MenuItem("HoboLife/Build Look (Post-FX + Lighting)")]
    public static void BuildLook()
    {
        // ---- Post-processing volume + profile ----
        var existing = GameObject.Find("PostFX");
        if (existing) Object.DestroyImmediate(existing);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var tone = profile.Add<Tonemapping>();
        tone.mode.Override(TonemappingMode.ACES);

        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(0.75f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.62f);
        bloom.tint.Override(new Color(1f, 0.95f, 0.85f));

        var ca = profile.Add<ColorAdjustments>();
        ca.postExposure.Override(0.12f);
        ca.contrast.Override(14f);
        ca.saturation.Override(14f);

        var wb = profile.Add<WhiteBalance>();
        wb.temperature.Override(14f); // warm, sunny LA

        var vig = profile.Add<Vignette>();
        vig.intensity.Override(0.30f);
        vig.smoothness.Override(0.45f);

        if (!Directory.Exists("Assets/Settings")) Directory.CreateDirectory("Assets/Settings");
        AssetDatabase.CreateAsset(profile, ProfilePath);

        var volGo = new GameObject("PostFX");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true; vol.priority = 1f; vol.profile = profile;

        // ---- Camera ----
        var cam = Camera.main;
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cam.farClipPlane = 400f;
            cam.allowHDR = true;
        }

        // ---- Lighting ----
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun != null)
        {
            sun.color = new Color(1f, 0.95f, 0.83f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            sun.transform.rotation = Quaternion.Euler(48f, 150f, 0f);
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.49f, 0.44f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.2f);

        // ---- Fog ----
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.74f, 0.79f, 0.87f);
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 230f;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Look built: post-processing volume + warm lighting + fog.");
    }

    [DidReloadScripts]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && GameObject.Find("PostFX") == null && GameObject.Find("Player") != null)
                BuildLook();
        };
    }
}
