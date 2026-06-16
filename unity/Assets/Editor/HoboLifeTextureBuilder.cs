using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

// HoboLife — procedurally generates tiling textures (window facades, asphalt,
// concrete) and re-skins the city so buildings read as buildings (rows of
// windows) and the streets look paved. Generated when missing.
public static class HoboLifeTextureBuilder
{
    const string TexDir = "Assets/Textures";

    [MenuItem("HoboLife/Build Textures + Re-skin City")]
    public static void BuildTextures()
    {
        if (!Directory.Exists(TexDir)) Directory.CreateDirectory(TexDir);

        var facade = SaveTex(MakeFacade(256), "facade");
        var asphalt = SaveTex(MakeGrain(256, new Color(0.17f, 0.17f, 0.18f), 0.06f, 1, false, default), "asphalt");
        var concrete = SaveTex(MakeGrain(256, new Color(0.58f, 0.58f, 0.57f), 0.05f, 2, true, new Color(0.40f, 0.40f, 0.40f)), "concrete");

        // Re-skin buildings
        foreach (var door in Object.FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None))
        {
            var go = door.gameObject;
            var rend = go.GetComponent<Renderer>();
            if (rend == null) continue;
            Color tint = rend.sharedMaterial != null ? rend.sharedMaterial.color : Color.gray;
            var s = go.transform.localScale;
            var m = Mat(facade, tint);
            m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(1f, s.x / 12f), Mathf.Max(1f, s.y / 12f)));
            m.mainTextureScale = m.GetTextureScale("_BaseMap");
            rend.sharedMaterial = m;
        }

        // Streets
        Reskin("Road_NS", asphalt, new Color(0.5f, 0.5f, 0.52f), new Vector2(2f, 26f));
        Reskin("Road_EW", asphalt, new Color(0.5f, 0.5f, 0.52f), new Vector2(26f, 2f));
        Reskin("Sidewalk_NS", concrete, Color.white, new Vector2(3f, 26f));
        Reskin("Sidewalk_EW", concrete, Color.white, new Vector2(26f, 3f));
        Reskin("Ground", asphalt, new Color(0.6f, 0.6f, 0.6f), new Vector2(30f, 30f));

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] Textures built + city re-skinned (window facades + paved streets).");
    }

    static void Reskin(string name, Texture2D tex, Color tint, Vector2 tiling)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        var m = Mat(tex, tint);
        m.SetTextureScale("_BaseMap", tiling);
        m.mainTextureScale = tiling;
        rend.sharedMaterial = m;
    }

    static Material Mat(Texture2D tex, Color tint)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
        m.color = tint;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
        return m;
    }

    // 4x4 window grid on a wall; some windows warm-lit.
    static Texture2D MakeFacade(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGB24, true);
        var rnd = new System.Random(7);
        Color wall = new Color(0.80f, 0.80f, 0.78f);
        Color frame = new Color(0.45f, 0.45f, 0.47f);
        Color glass = new Color(0.16f, 0.20f, 0.27f);
        Color lit = new Color(1.6f, 1.35f, 0.7f); // HDR-ish for bloom
        int cells = 4, cell = size / cells;
        bool[] litCells = new bool[cells * cells];
        for (int i = 0; i < litCells.Length; i++) litCells[i] = rnd.NextDouble() < 0.28;

        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int cx = x / cell, cy = y / cell;
                float lx = (x % cell) / (float)cell, ly = (y % cell) / (float)cell;
                Color c = wall;
                bool inFrame = lx > 0.16f && lx < 0.84f && ly > 0.16f && ly < 0.84f;
                bool inGlass = lx > 0.24f && lx < 0.76f && ly > 0.24f && ly < 0.76f;
                if (inGlass) c = litCells[cy * cells + cx] ? lit : glass;
                else if (inFrame) c = frame;
                px[y * size + x] = c;
            }
        t.SetPixels(px); t.wrapMode = TextureWrapMode.Repeat; t.Apply();
        return t;
    }

    static Texture2D MakeGrain(int size, Color baseC, float amp, int seed, bool seams, Color seamC)
    {
        var t = new Texture2D(size, size, TextureFormat.RGB24, true);
        var rnd = new System.Random(seed);
        var px = new Color[size * size];
        int panel = size / 4;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = (float)(rnd.NextDouble() - 0.5) * amp;
                Color c = new Color(Mathf.Clamp01(baseC.r + n), Mathf.Clamp01(baseC.g + n), Mathf.Clamp01(baseC.b + n));
                if (seams && (x % panel < 2 || y % panel < 2)) c = seamC;
                px[y * size + x] = c;
            }
        t.SetPixels(px); t.wrapMode = TextureWrapMode.Repeat; t.Apply();
        return t;
    }

    static Texture2D SaveTex(Texture2D tex, string name)
    {
        string path = TexDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    [DidReloadScripts]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && !File.Exists(TexDir + "/facade.png") && GameObject.Find("City") != null)
                BuildTextures();
        };
    }
}
