using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HoboLife — dialogue panel (bottom) + interaction prompt (center-bottom).
// Numbered 1-4 options; dice-gated options route through DiceRollUI and apply
// the success/fail outcome to PlayerStats. Self-contained overlay canvas.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public bool IsOpen { get; private set; }

    PlayerStats stats;
    Dictionary<string, DialogueData.Node> tree;
    string npcName;
    string currentTreeId;
    DialogueData.Node node;

    CanvasGroup group;
    Text whoText, sayText;
    Text[] optTexts = new Text[4];
    GameObject[] optRows = new GameObject[4];
    CanvasGroup promptGroup;
    Text promptText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        stats = Object.FindFirstObjectByType<PlayerStats>();
        BuildUI();
    }

    public void ShowPrompt(string text)
    {
        if (IsOpen) return;
        if (promptText) promptText.text = text;
        if (promptGroup) promptGroup.alpha = 1f;
    }

    public void HidePrompt()
    {
        if (promptGroup) promptGroup.alpha = 0f;
    }

    public void Open(string treeId, string name)
    {
        tree = DialogueData.Tree(treeId);
        currentTreeId = treeId;
        npcName = name;
        IsOpen = true;
        HidePrompt();
        group.alpha = 1f;
        Goto("start");
    }

    void Close()
    {
        IsOpen = false;
        group.alpha = 0f;
        CancelInvoke();
    }

    void Goto(string nodeId)
    {
        if (nodeId == "end" || tree == null || !tree.ContainsKey(nodeId)) { Close(); return; }
        node = tree[nodeId];
        whoText.text = npcName;
        sayText.text = node.text;
        for (int i = 0; i < 4; i++)
        {
            bool has = i < node.options.Count;
            optRows[i].SetActive(has);
            if (has) optTexts[i].text = (i + 1) + ".  " + node.options[i].label;
        }
    }

    void Update()
    {
        if (!IsOpen || node == null) return;
        if (DiceRollUI.Instance != null && DiceRollUI.Instance.IsRolling) return;
        if (IsInvoking()) return;

        for (int i = 0; i < node.options.Count && i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { Choose(i); return; }
        }
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    void Choose(int i)
    {
        var o = node.options[i];
        if (o.checkSkill != null && DiceRollUI.Instance != null)
        {
            int skill = SkillVal(o.checkSkill);
            DiceRollUI.Instance.Roll(skill, o.dc, o.label, res => Apply(res.success ? o.success : o.fail));
        }
        else
        {
            DoThen(o.then, null);
        }
    }

    void Apply(DialogueData.Outcome oc)
    {
        if (oc == null) { Close(); return; }
        if (oc.money != 0 && stats != null) stats.money += oc.money;
        if (oc.addSkill != null && stats != null) stats.AddSkill(oc.addSkill, oc.addAmount);
        DoThen(oc.then, oc.text);
    }

    void DoThen(string then, string line)
    {
        if (then == "fight")
        {
            string foe = npcName;
            bool tough = currentTreeId == "thug";
            Close();
            if (CombatScreen.Instance != null) CombatScreen.Instance.Begin(foe, tough);
            return;
        }
        if (then == "end" || string.IsNullOrEmpty(then))
        {
            if (!string.IsNullOrEmpty(line)) { sayText.text = line; Invoke(nameof(Close), 1.5f); }
            else Close();
            return;
        }
        Goto(then);
    }

    int SkillVal(string k)
    {
        if (stats == null) return 0;
        switch (k)
        {
            case "int": return stats.intelligence;
            case "cha": return stats.charisma;
            case "str": return stats.strength;
            case "tool": return stats.toolSkill;
        }
        return 0;
    }

    // ---------- UI ----------
    void BuildUI()
    {
        var canvasGO = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Interaction prompt
        var promptPanel = MakePanel(canvasGO.transform, "InteractPrompt", new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(360, 34), new Color(0.08f, 0.11f, 0.14f, 0.85f));
        promptGroup = promptPanel.gameObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptText = Label(promptPanel, "PromptText", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 34), "Press Q to talk", 15, TextAnchor.MiddleCenter, Color.white);

        // Dialogue panel
        var panel = MakePanel(canvasGO.transform, "DialoguePanel", new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(680, 220), new Color(0.06f, 0.09f, 0.12f, 0.92f));
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        whoText = Label(panel, "WhoText", new Vector2(0, 1), new Vector2(20, -12), new Vector2(400, 24), "NPC", 17, TextAnchor.MiddleLeft, new Color(0.96f, 0.83f, 0.39f));
        sayText = Label(panel, "SayText", new Vector2(0, 1), new Vector2(20, -40), new Vector2(640, 48), "...", 15, TextAnchor.UpperLeft, new Color(0.88f, 0.91f, 0.94f));

        float y = -96;
        for (int i = 0; i < 4; i++)
        {
            var row = MakePanel(panel, "Opt" + i, new Vector2(0, 1), new Vector2(20, y), new Vector2(640, 26), new Color(0.12f, 0.16f, 0.21f, 0.9f));
            optTexts[i] = Label(row, "OptText" + i, new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(620, 26), "", 14, TextAnchor.MiddleLeft, Color.white);
            optRows[i] = row.gameObject;
            optRows[i].SetActive(false);
            y -= 30;
        }
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
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
