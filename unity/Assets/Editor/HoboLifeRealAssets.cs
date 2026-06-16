using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// HoboLife — wires up downloaded CC0 assets (Poly Haven): a real HDRI sky as the
// skybox (with image-based ambient) and a PBR asphalt (diffuse + normal +
// roughness) on the streets. Auto-runs once the files are present.
public static class HoboLifeRealAssets
{
    const string Hdr = "Assets/Sky/sky_puresky_1k.hdr";
    const string SkyMat = "Assets/Sky/SkyboxMat.mat";
    const string Diff = "Assets/Textures/PolyHaven/asphalt_diff.png";
    const string Nor = "Assets/Textures/PolyHaven/asphalt_nor.png";
    const string Rough = "Assets/Textures/PolyHaven/asphalt_rough.png";

    [MenuItem("HoboLife/Apply Real Assets (sky + asphalt)")]
    public static void Apply()
    {
        if (!File.Exists(Hdr)) { Debug.LogWarning("[HoboLife] HDRI not downloaded yet."); return; }

        // --- import settings ---
        SetNormalMap(Nor);
        SetLinear(Rough);

        // --- skybox from HDRI ---
        var skyTex = AssetDatabase.LoadAssetAtPath<Texture>(Hdr);
        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMat);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(sky, SkyMat);
        }
        sky.SetTexture("_MainTex", skyTex);
        sky.SetFloat("_Mapping", 1f);     // latitude-longitude (equirectangular)
        sky.SetFloat("_ImageType", 0f);   // 360 degrees
        sky.SetFloat("_Exposure", 1.1f);
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.fogColor = new Color(0.78f, 0.83f, 0.9f);
        DynamicGI.UpdateEnvironment();

        // --- asphalt PBR on the streets ---
        var diff = AssetDatabase.LoadAssetAtPath<Texture>(Diff);
        var nor = AssetDatabase.LoadAssetAtPath<Texture>(Nor);
        ApplyAsphalt("Road_NS", diff, nor, new Vector2(3f, 30f));
        ApplyAsphalt("Road_EW", diff, nor, new Vector2(30f, 3f));
        ApplyAsphalt("Ground", diff, nor, new Vector2(34f, 34f));

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Real assets applied: HDRI sky + image-based ambient + PBR asphalt streets.");
    }

    static void ApplyAsphalt(string name, Texture diff, Texture nor, Vector2 tiling)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (diff != null) { m.SetTexture("_BaseMap", diff); m.mainTexture = diff; }
        if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
        m.SetFloat("_Smoothness", 0.22f);
        m.SetColor("_BaseColor", Color.white);
        m.SetTextureScale("_BaseMap", tiling);
        m.mainTextureScale = tiling;
        r.sharedMaterial = m;
    }

    static void SetNormalMap(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        { imp.textureType = TextureImporterType.NormalMap; imp.SaveAndReimport(); }
    }

    static void SetLinear(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.sRGBTexture) { imp.sRGBTexture = false; imp.SaveAndReimport(); }
    }

    [DidReloadScripts]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && File.Exists(Hdr) && !File.Exists(SkyMat) && GameObject.Find("City") != null)
                Apply();
        };
    }
}
