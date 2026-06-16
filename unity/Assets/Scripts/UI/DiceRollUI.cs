using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// HoboLife — the D&D-style d10 skill-check popup (top-right). Any gated action
// calls DiceRollUI.Instance.Roll(skill, required, label, onComplete). It resolves
// the check up front via DiceCheck, then animates a shuffling die toward the real
// roll and reports SUCCESS/FAIL with crit(gold)/auto-fail(red) styling.
public class DiceRollUI : MonoBehaviour
{
    public static DiceRollUI Instance { get; private set; }
    public bool IsRolling { get; private set; }

    CanvasGroup group;
    Text dieFace, labelText, mathText, resultText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    public void Roll(int skill, int required, string label, Action<DiceCheck.Result> onComplete = null)
    {
        if (IsRolling) return;
        var result = DiceCheck.Resolve(skill, required);
        StartCoroutine(RollRoutine(skill, required, label, result, onComplete));
    }

    IEnumerator RollRoutine(int skill, int required, string label, DiceCheck.Result result, Action<DiceCheck.Result> onComplete)
    {
        IsRolling = true;
        group.alpha = 1f;
        labelText.text = label;
        mathText.text = "";
        resultText.text = "rolling…";
        resultText.color = Color.white;

        float t = 0f;
        while (t < 1.1f)
        {
            dieFace.text = UnityEngine.Random.Range(1, 11).ToString();
            dieFace.color = new Color(1f, 1f, 1f, 0.85f);
            t += 0.07f;
            yield return new WaitForSeconds(0.07f);
        }

        dieFace.text = result.roll.ToString();
        if (result.critical) dieFace.color = new Color(0.96f, 0.77f, 0.19f);
        else if (result.autoFail) dieFace.color = new Color(0.90f, 0.33f, 0.23f);
        else dieFace.color = new Color(1f, 0.996f, 0.98f);

        mathText.text = skill + " × " + result.multiplier + " = " + result.effective + " vs " + required;
        resultText.text = result.success ? "SUCCESS" : "FAIL";
        resultText.color = result.success ? new Color(0.48f, 0.88f, 0.66f) : new Color(1f, 0.55f, 0.46f);

        yield return new WaitForSeconds(1.2f);
        group.alpha = 0f;
        IsRolling = false;
        onComplete?.Invoke(result);
    }

    // ---- runtime UI (self-contained overlay canvas) ----
    void BuildUI()
    {
        var canvasGO = new GameObject("DiceCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = MakePanel(canvasGO.transform, "DicePanel", new Vector2(1, 1), new Vector2(-16, -90), new Vector2(170, 150), new Color(0.08f, 0.11f, 0.14f, 0.9f));
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        dieFace = Label(panel, "DieFace", new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(170, 70), "0", 52, TextAnchor.MiddleCenter, Color.white);
        labelText = Label(panel, "LabelText", new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(160, 20), "", 13, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.92f));
        mathText = Label(panel, "MathText", new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(164, 18), "", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.76f, 0.82f));
        resultText = Label(panel, "ResultText", new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(164, 22), "", 16, TextAnchor.MiddleCenter, Color.white);
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
}
