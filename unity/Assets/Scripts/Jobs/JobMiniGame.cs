using System;
using UnityEngine;
using UnityEngine.UI;

// HoboLife — Among-Us-style job mini-game (first instance: flipping burgers).
// A target jumps around; tap it `rounds` times to finish a shift. Pays per hit.
public class JobMiniGame : MonoBehaviour
{
    public static JobMiniGame Instance { get; private set; }
    public bool IsOpen { get; private set; }

    CanvasGroup group;
    Text titleText, progText;
    RectTransform target;
    int hits, rounds;
    Action<int> onDone;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    public void Open(string title, int rounds, Action<int> done)
    {
        if (IsOpen) return;
        this.rounds = rounds; onDone = done; hits = 0; IsOpen = true;
        titleText.text = title;
        group.alpha = 1f; group.blocksRaycasts = true;
        MoveTarget(); UpdateProg();
    }

    void OnHit()
    {
        hits++;
        if (hits >= rounds) { Finish(); return; }
        MoveTarget(); UpdateProg();
    }

    void Finish()
    {
        IsOpen = false;
        group.alpha = 0f; group.blocksRaycasts = false;
        var cb = onDone; onDone = null;
        cb?.Invoke(hits);
    }

    void MoveTarget()
    {
        target.anchoredPosition = new Vector2(UnityEngine.Random.Range(-300f, 300f), UnityEngine.Random.Range(-110f, 110f));
    }

    void UpdateProg() { progText.text = "Burgers flipped: " + hits + " / " + rounds; }

    void BuildUI()
    {
        var canvasGO = new GameObject("MiniGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 58;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = Panel(canvasGO.transform, "Bg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 460), new Color(0.06f, 0.09f, 0.12f, 0.98f));
        group = bg.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false;

        titleText = Label(bg, "Title", new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(740, 28), "Work Shift", 20, TextAnchor.MiddleCenter, new Color(0.96f, 0.83f, 0.39f));
        progText = Label(bg, "Prog", new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(740, 22), "", 14, TextAnchor.MiddleCenter, new Color(0.7f, 0.76f, 0.82f));
        Label(bg, "Hint", new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(740, 20), "Click the burger as fast as you can!", 12, TextAnchor.MiddleCenter, new Color(0.55f, 0.6f, 0.66f));

        var tgt = Panel(bg, "Target", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96), new Color(0.85f, 0.55f, 0.22f, 1f));
        target = tgt.GetComponent<RectTransform>();
        tgt.gameObject.AddComponent<Button>().onClick.AddListener(OnHit);
        Label(tgt, "TgtTxt", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96), "FLIP", 18, TextAnchor.MiddleCenter, Color.white);
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
