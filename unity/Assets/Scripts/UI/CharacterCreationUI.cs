using System;
using UnityEngine;
using UnityEngine.UI;

// HoboLife — pre-game character creation: set your name, allocate the 20 starting
// skill points across the four skills, pick a look. Shown only on a brand-new
// save. On Start it hands the choices back to GameStateController.
public struct AppearanceConfig { public Color skin, hair; }

public class CharacterCreationUI : MonoBehaviour
{
    public static CharacterCreationUI Instance { get; private set; }

    Action<string, string, int[], AppearanceConfig> onDone;
    CanvasGroup group;
    InputField nameInput, dobInput;
    Text remainingText;
    Text[] skillVals = new Text[4];
    Button startBtn;
    int[] skills = new int[4];
    int skinIdx, hairIdx;

    static readonly string[] SkillNames = { "Intelligence", "Charisma", "Strength", "Tool Skill" };
    static readonly Color[] Skins = { new Color(0.85f, 0.72f, 0.55f), new Color(0.74f, 0.55f, 0.40f), new Color(0.55f, 0.40f, 0.28f) };
    static readonly Color[] Hairs = { new Color(0.24f, 0.17f, 0.11f), new Color(0.1f, 0.1f, 0.12f), new Color(0.7f, 0.6f, 0.3f) };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        group.alpha = 0f; group.blocksRaycasts = false;
    }

    public void Begin(Action<string, string, int[], AppearanceConfig> done)
    {
        onDone = done;
        skills = new int[4];
        skinIdx = 0; hairIdx = 0;
        nameInput.text = "Hobo";
        dobInput.text = "01/01/2000";
        group.alpha = 1f; group.blocksRaycasts = true;
        Time.timeScale = 0f; // pause the world during creation
        Rebuild();
    }

    int Remaining() { int s = 0; foreach (var v in skills) s += v; return HoboBalance.CREATION_POINTS - s; }

    void Step(int i, int delta)
    {
        int nv = Mathf.Clamp(skills[i] + delta, 0, HoboBalance.CREATION_POINTS);
        if (delta > 0 && Remaining() <= 0) return;
        skills[i] = nv;
        Rebuild();
    }

    void Rebuild()
    {
        for (int i = 0; i < 4; i++) skillVals[i].text = skills[i].ToString();
        int rem = Remaining();
        remainingText.text = "Points to spend: " + rem;
        startBtn.interactable = rem == 0 && !string.IsNullOrWhiteSpace(nameInput.text);
    }

    void OnStart()
    {
        if (Remaining() != 0) return;
        var look = new AppearanceConfig { skin = Skins[skinIdx], hair = Hairs[hairIdx] };
        group.alpha = 0f; group.blocksRaycasts = false;
        Time.timeScale = 1f;
        onDone?.Invoke(nameInput.text, dobInput.text, (int[])skills.Clone(), look);
    }

    // ---------- UI ----------
    void BuildUI()
    {
        var canvasGO = new GameObject("CreationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = Panel(canvasGO.transform, "Bg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080), new Color(0.05f, 0.07f, 0.10f, 0.98f));
        group = bg.gameObject.AddComponent<CanvasGroup>();

        var card = Panel(bg, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 560), new Color(0.09f, 0.12f, 0.16f, 1f));
        Label(card, "Title", new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(540, 30), "New Hobo", 26, TextAnchor.MiddleCenter, new Color(0.96f, 0.83f, 0.39f));
        Label(card, "Sub", new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(540, 20), "Every hobo gets a government ID. Make your start.", 13, TextAnchor.MiddleCenter, new Color(0.7f, 0.76f, 0.82f));

        Label(card, "NameLbl", new Vector2(0, 1), new Vector2(40, -86), new Vector2(120, 24), "Name", 15, TextAnchor.MiddleLeft, Color.white);
        nameInput = Input(card, "NameInput", new Vector2(0, 1), new Vector2(170, -86), new Vector2(330, 30), "Hobo");
        Label(card, "DobLbl", new Vector2(0, 1), new Vector2(40, -124), new Vector2(120, 24), "Birth date", 15, TextAnchor.MiddleLeft, Color.white);
        dobInput = Input(card, "DobInput", new Vector2(0, 1), new Vector2(170, -124), new Vector2(330, 30), "01/01/2000");

        remainingText = Label(card, "Remaining", new Vector2(0.5f, 1), new Vector2(0, -166), new Vector2(540, 22), "Points to spend: 20", 15, TextAnchor.MiddleCenter, new Color(0.55f, 0.85f, 1f));

        float y = -200;
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            Label(card, SkillNames[i] + "Lbl", new Vector2(0, 1), new Vector2(40, y), new Vector2(200, 28), SkillNames[i], 15, TextAnchor.MiddleLeft, Color.white);
            Btn(card, "Minus" + i, new Vector2(0, 1), new Vector2(300, y + 14), new Vector2(36, 28), "-", () => Step(idx, -1));
            skillVals[i] = Label(card, "Val" + i, new Vector2(0, 1), new Vector2(360, y), new Vector2(60, 28), "0", 17, TextAnchor.MiddleCenter, new Color(0.96f, 0.83f, 0.39f));
            Btn(card, "Plus" + i, new Vector2(0, 1), new Vector2(420, y + 14), new Vector2(36, 28), "+", () => Step(idx, +1));
            y -= 38;
        }

        // Appearance swatches
        Label(card, "LookLbl", new Vector2(0, 1), new Vector2(40, y - 4), new Vector2(120, 24), "Skin", 14, TextAnchor.MiddleLeft, Color.white);
        for (int i = 0; i < Skins.Length; i++) { int idx = i; Swatch(card, "Skin" + i, new Vector2(120 + i * 40, y + 10), Skins[i], () => { skinIdx = idx; }); }
        Label(card, "HairLbl", new Vector2(0, 1), new Vector2(260, y - 4), new Vector2(80, 24), "Hair", 14, TextAnchor.MiddleLeft, Color.white);
        for (int i = 0; i < Hairs.Length; i++) { int idx = i; Swatch(card, "Hair" + i, new Vector2(330 + i * 40, y + 10), Hairs[i], () => { hairIdx = idx; }); }

        var startGo = Panel(card, "Start", new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(220, 44), new Color(0.20f, 0.55f, 0.38f, 1f));
        startBtn = startGo.gameObject.AddComponent<Button>();
        startBtn.onClick.AddListener(OnStart);
        Label(startGo, "StartTxt", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 44), "Start Life", 18, TextAnchor.MiddleCenter, Color.white);
    }

    void Swatch(Transform parent, string name, Vector2 pos, Color c, UnityEngine.Events.UnityAction onClick)
    {
        var go = Panel(parent, name, new Vector2(0, 1), pos, new Vector2(30, 30), c);
        go.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
    }

    void Btn(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string text, UnityEngine.Events.UnityAction onClick)
    {
        var go = Panel(parent, name, anchor, pos, size, new Color(0.18f, 0.22f, 0.30f, 1f));
        go.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        Label(go, name + "T", new Vector2(0.5f, 0.5f), Vector2.zero, size, text, 18, TextAnchor.MiddleCenter, Color.white);
    }

    InputField Input(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string val)
    {
        var go = Panel(parent, name, anchor, pos, size, new Color(0.16f, 0.20f, 0.26f, 1f));
        var input = go.gameObject.AddComponent<InputField>();
        var txt = Label(go, name + "Txt", new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(size.x - 16, size.y), val, 15, TextAnchor.MiddleLeft, Color.white);
        input.textComponent = txt;
        input.text = val;
        input.onValueChanged.AddListener(_ => Rebuild());
        return input;
    }

    static Transform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
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
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
