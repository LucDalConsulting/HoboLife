using UnityEngine;
using UnityEngine.UI;

// HoboLife — Minecraft-style grid inventory (toggle with I). Shows the 12-slot
// pack plus the two hands; click a pack slot to equip, a hand slot to use/stash.
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public bool IsOpen { get; private set; }

    PlayerInventory inv;
    CanvasGroup group;
    Text[] packLabels = new Text[PlayerInventory.PackSize];
    Text leftLabel, rightLabel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        group.alpha = IsOpen ? 1f : 0f;
        group.blocksRaycasts = IsOpen;
        if (IsOpen)
        {
            inv = Object.FindFirstObjectByType<PlayerInventory>();
            Rebuild(inv);
        }
    }

    public void Rebuild(PlayerInventory who)
    {
        if (who != null) inv = who;
        if (inv == null) return;
        for (int i = 0; i < packLabels.Length; i++)
            packLabels[i].text = Icon(i < inv.pack.Length ? inv.pack[i] : null);
        if (leftLabel) leftLabel.text = Icon(inv.leftHand, "L");
        if (rightLabel) rightLabel.text = Icon(inv.rightHand, "R");
    }

    static string Icon(string id, string empty = "")
    {
        var d = ItemCatalog.Get(id);
        return d != null ? d.icon : empty;
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = MakePanel(canvasGO.transform, "InventoryPanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 320), new Color(0.07f, 0.10f, 0.13f, 0.96f));
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false;

        Label(panel, "Title", new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(360, 24), "Inventory  (I to close)", 16, TextAnchor.MiddleCenter, new Color(0.9f, 0.92f, 0.95f));

        // Pack grid 4 x 3
        const int cols = 4, rows = 3, slot = 70, pad = 8;
        float gridW = cols * slot + (cols - 1) * pad;
        float startX = -gridW / 2f + slot / 2f;
        float startY = 30f;
        for (int i = 0; i < PlayerInventory.PackSize; i++)
        {
            int r = i / cols, c = i % cols;
            float x = startX + c * (slot + pad);
            float y = startY - r * (slot + pad);
            int idx = i;
            packLabels[i] = SlotButton(panel, "Pack" + i, new Vector2(x, y), new Vector2(slot, slot), () => { if (inv != null) inv.EquipFromPack(idx); });
        }

        // Hands row at the bottom
        Label(panel, "HandsLbl", new Vector2(0.5f, 0), new Vector2(0, 96), new Vector2(360, 18), "Hands", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.76f, 0.82f));
        leftLabel = SlotButton(panel, "HandL", new Vector2(-50, 60), new Vector2(70, 70), () => { if (inv != null) inv.HandSlotClicked(true); }, new Color(0.18f, 0.22f, 0.30f, 1f));
        rightLabel = SlotButton(panel, "HandR", new Vector2(50, 60), new Vector2(70, 70), () => { if (inv != null) inv.HandSlotClicked(false); }, new Color(0.18f, 0.22f, 0.30f, 1f));
    }

    Text SlotButton(Transform parent, string name, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, Color? bg = null)
    {
        var go = new GameObject(name, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = bg ?? new Color(0.13f, 0.16f, 0.21f, 1f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        return Label(go.transform, name + "Txt", new Vector2(0.5f, 0.5f), Vector2.zero, size, "", 14, TextAnchor.MiddleCenter, Color.white);
    }

    static Transform MakePanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
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
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
