using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// HoboLife — builds the uGUI Stats HUD (health, hunger, 4 skill bars, money,
// in-game clock) and wires a HudController to PlayerStats + a GameClock with a
// day/night directional light. Re-runnable from the HoboLife menu.
public static class HoboLifeHudBuilder
{
    static readonly Color Panel = new Color(0.08f, 0.11f, 0.14f, 0.82f);
    static readonly Color Track = new Color(0.05f, 0.07f, 0.09f, 1f);
    static readonly Color Muted = new Color(0.70f, 0.76f, 0.82f);

    [MenuItem("HoboLife/Build HUD")]
    public static void BuildHud()
    {
        var old = GameObject.Find("HUDCanvas");
        if (old) Object.DestroyImmediate(old);

        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var hud = canvasGO.AddComponent<HudController>();

        // ---------- Top-left card: health, hunger, skills ----------
        var card = MakePanel(canvasGO.transform, "TopLeftCard", new Vector2(0, 1), new Vector2(16, -16), new Vector2(300, 250), Panel);

        Label(card, "HealthLabel", new Vector2(0, 1), new Vector2(12, -12), new Vector2(160, 22), "Health", 15, TextAnchor.MiddleLeft, Color.white);
        hud.healthText = Label(card, "HealthVal", new Vector2(1, 1), new Vector2(-12, -12), new Vector2(120, 22), "100/100", 14, TextAnchor.MiddleRight, new Color(1f, 0.6f, 0.55f));
        Bar(card, "HealthBar", new Vector2(12, -36), new Vector2(276, 16), new Color(0.9f, 0.33f, 0.23f), out hud.healthFill);

        Label(card, "HungerLabel", new Vector2(0, 1), new Vector2(12, -60), new Vector2(160, 22), "Hunger", 15, TextAnchor.MiddleLeft, Color.white);
        hud.hungerText = Label(card, "HungerVal", new Vector2(1, 1), new Vector2(-12, -60), new Vector2(120, 22), "100/100", 14, TextAnchor.MiddleRight, new Color(0.96f, 0.64f, 0.35f));
        Bar(card, "HungerBar", new Vector2(12, -84), new Vector2(276, 16), new Color(0.96f, 0.64f, 0.35f), out hud.hungerFill);

        string[] sk = { "INT", "CHA", "STR", "TOOL" };
        Color[] sc = { new Color(0.35f, 0.66f, 0.9f), new Color(0.94f, 0.42f, 0.58f), new Color(0.9f, 0.33f, 0.23f), new Color(0.96f, 0.64f, 0.35f) };
        var fills = new Image[4]; var vals = new Text[4];
        float y = -116;
        for (int i = 0; i < 4; i++)
        {
            Label(card, sk[i] + "Label", new Vector2(0, 1), new Vector2(12, y), new Vector2(80, 20), sk[i], 13, TextAnchor.MiddleLeft, Muted);
            vals[i] = Label(card, sk[i] + "Val", new Vector2(1, 1), new Vector2(-12, y), new Vector2(80, 20), "0", 13, TextAnchor.MiddleRight, sc[i]);
            Bar(card, sk[i] + "Bar", new Vector2(60, y - 4), new Vector2(228, 8), sc[i], out fills[i]);
            y -= 26;
        }
        hud.intFill = fills[0]; hud.chaFill = fills[1]; hud.strFill = fills[2]; hud.toolFill = fills[3];
        hud.intText = vals[0]; hud.chaText = vals[1]; hud.strText = vals[2]; hud.toolText = vals[3];

        // ---------- Top-right clock ----------
        var clockCard = MakePanel(canvasGO.transform, "TopRightClock", new Vector2(1, 1), new Vector2(-16, -16), new Vector2(150, 64), Panel);
        hud.clockText = Label(clockCard, "ClockText", new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(150, 28), "9:00 AM", 20, TextAnchor.MiddleCenter, Color.white);
        hud.dayText = Label(clockCard, "DayText", new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(150, 20), "Day 1", 13, TextAnchor.MiddleCenter, Muted);

        // ---------- Bottom money + hand slots ----------
        var moneyPill = MakePanel(canvasGO.transform, "MoneyPill", new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(150, 36), Panel);
        hud.moneyText = Label(moneyPill, "MoneyText", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(150, 36), "$0", 18, TextAnchor.MiddleCenter, new Color(0.3f, 0.72f, 0.49f));
        var handL = MakePanel(canvasGO.transform, "HandLeft", new Vector2(0.5f, 0), new Vector2(-80, 46), new Vector2(64, 64), Panel);
        Label(handL, "HandLKey", new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(64, 16), "L", 11, TextAnchor.LowerCenter, Muted);
        var handR = MakePanel(canvasGO.transform, "HandRight", new Vector2(0.5f, 0), new Vector2(80, 46), new Vector2(64, 64), Panel);
        Label(handR, "HandRKey", new Vector2(0.5f, 0), new Vector2(0, 3), new Vector2(64, 16), "R", 11, TextAnchor.LowerCenter, Muted);

        // ---------- Version ----------
        Label(canvasGO.transform, "VersionText", new Vector2(0, 0), new Vector2(10, 8), new Vector2(200, 18), "HoboLife v" + HoboBalance.VERSION, 11, TextAnchor.LowerLeft, new Color(0.36f, 0.42f, 0.47f));

        // ---------- GameClock + wiring ----------
        var clockGO = GameObject.Find("GameClock") ?? new GameObject("GameClock");
        var clock = clockGO.GetComponent<GameClock>() ?? clockGO.AddComponent<GameClock>();
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }
        clock.sun = sun;

        hud.clock = clock;
        hud.stats = Object.FindFirstObjectByType<PlayerStats>();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HoboLife] HUD built and wired (stats=" + (hud.stats != null) + ", sun=" + (sun != null) + ").");
    }

    // Auto-build the HUD once after a recompile if it isn't in the scene yet
    // (reliable trigger that doesn't depend on the editor menu registry).
    [UnityEditor.Callbacks.DidReloadScripts]
    static void AutoBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying && GameObject.Find("HUDCanvas") == null && GameObject.Find("Player") != null)
                BuildHud();
        };
    }

    static Transform MakePanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color; img.raycastTarget = false;
        return go.transform;
    }

    static Text Label(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        t.fontSize = fontSize; t.alignment = align; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void Bar(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor, out Image fill)
    {
        var bg = MakePanel(parent, name, new Vector2(0, 1), pos, size, Track);
        var fillGO = new GameObject(name + "_Fill", typeof(Image));
        fillGO.transform.SetParent(bg.transform, false);
        var rt = fillGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        fill = fillGO.GetComponent<Image>();
        fill.color = fillColor; fill.raycastTarget = false;
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
    }
}
