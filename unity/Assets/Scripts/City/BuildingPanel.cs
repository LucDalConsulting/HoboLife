using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HoboLife — the panel that opens when you enter a landmark (E). Offers the
// building's services: study/train (skill grind w/ daily caps), work a shift
// (mini-game), bank deposit/withdraw/loan, casino gamble, hospital heal, shops.
public class BuildingPanel : MonoBehaviour
{
    public static BuildingPanel Instance { get; private set; }
    public bool IsOpen { get; private set; }

    PlayerStats stats;
    GameStateController gsc;
    GameClock clock;
    PlayerInventory inv;

    CanvasGroup group;
    Text titleText, resultText;
    Transform holder;
    readonly List<GameObject> buttons = new List<GameObject>();
    string kind;

    int studyDay = -1, studyCount;
    int gymDay = -1, gymCount;
    const int DailyCap = 3;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    public void Open(BuildingDoor door)
    {
        if (IsOpen) return;
        stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        gsc = UnityEngine.Object.FindFirstObjectByType<GameStateController>();
        clock = UnityEngine.Object.FindFirstObjectByType<GameClock>();
        inv = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
        kind = door.kind;
        IsOpen = true;
        group.alpha = 1f; group.blocksRaycasts = true;
        titleText.text = door.displayName;
        resultText.text = "";
        BuildActions();
    }

    public void Close() { IsOpen = false; group.alpha = 0f; group.blocksRaycasts = false; }

    void Update() { if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close(); }

    void BuildActions()
    {
        foreach (var b in buttons) Destroy(b);
        buttons.Clear();

        switch (kind)
        {
            case "university": Add("Study (+INT)", Study); break;
            case "gym": Add("Train (+STR)", Gym); break;
            case "diner": Add("Work a shift (flip burgers)", WorkDiner); Add("Buy a burger ($5)", BuyBurger); break;
            case "bank":
                Add("Deposit $50", () => Bank("dep", 50));
                Add("Withdraw $50", () => Bank("wd", 50));
                Add("Take a $500 loan", () => Bank("loan", 500));
                Add("Repay $50", () => Bank("repay", 50));
                break;
            case "casino": Add("Gamble $10 (charm the dealer)", Gamble); break;
            case "hospital": Add("Get patched up", Heal); break;
            case "clothing": Add("Buy a T-Shirt ($20)", () => BuyItem("tshirt", 20)); break;
            case "cardealer":
                Add("Buy a Car ($2000)", BuyCar);
                Add("Buy a Skateboard ($150)", BuySkateboard);
                Add("Take the driving test (Tool)", DrivingTest);
                break;
            default: resultText.text = "Open for business soon."; break;
        }
        Add("Leave", Close);
        LayoutButtons();
    }

    // ---- services ----
    void Study()
    {
        if (!Daytime()) { resultText.text = "The university is closed at night."; return; }
        if (clock != null && clock.DayNumber != studyDay) { studyDay = clock.DayNumber; studyCount = 0; }
        if (studyCount >= DailyCap) { resultText.text = "Your brain is fried for today. Come back tomorrow."; return; }
        studyCount++;
        stats.AddSkill("int", 2);
        SkipHours(2f);
        resultText.text = "You studied hard. Intelligence +2  (INT now " + stats.intelligence + ").";
    }

    void Gym()
    {
        if (clock != null && clock.DayNumber != gymDay) { gymDay = clock.DayNumber; gymCount = 0; }
        if (gymCount >= DailyCap) { resultText.text = "You're too sore to train again today."; return; }
        gymCount++;
        stats.AddSkill("str", 2);
        SkipHours(2f);
        resultText.text = "You pumped iron. Strength +2  (STR now " + stats.strength + ").";
    }

    void WorkDiner()
    {
        if (!Daytime()) { resultText.text = "The diner only hires during the day."; return; }
        if (JobMiniGame.Instance == null) { resultText.text = "..."; return; }
        Close();
        JobMiniGame.Instance.Open("Burger Shift — Greasy Spoon Diner", 10, hits =>
        {
            int pay = hits * 8 + stats.toolSkill / 5;
            stats.money += pay;
            SkipHours(8f);
            Debug.Log("[HoboLife] Diner shift: flipped " + hits + " burgers for $" + pay + ".");
        });
    }

    void BuyBurger()
    {
        if (stats.money < 5) { resultText.text = "Not enough cash for a burger ($5)."; return; }
        stats.money -= 5;
        stats.hunger = Mathf.Min(HoboBalance.HUNGER_MAX, stats.hunger + 35f);
        resultText.text = "Ate a burger. Hunger +35  (-$5).";
    }

    void Bank(string op, int amt)
    {
        var d = gsc != null ? gsc.Data : null;
        if (d == null) { resultText.text = "Bank systems offline."; return; }
        switch (op)
        {
            case "dep":
                if (stats.money < amt) { resultText.text = "You don't have $" + amt + " cash."; return; }
                stats.money -= amt; d.bank += amt; break;
            case "wd":
                if (d.bank < amt) { resultText.text = "Not enough in the bank."; return; }
                d.bank -= amt; stats.money += amt; break;
            case "loan":
                d.loanPrincipal += amt; stats.money += amt;
                resultText.text = "Loan approved. +$" + amt + " cash (owe $" + d.loanPrincipal + ")."; return;
            case "repay":
                if (d.loanPrincipal <= 0) { resultText.text = "You have no loan to repay."; return; }
                int pay = Mathf.Min(amt, Mathf.Min(stats.money, d.loanPrincipal));
                if (pay <= 0) { resultText.text = "No cash to repay with."; return; }
                stats.money -= pay; d.loanPrincipal -= pay;
                resultText.text = "Repaid $" + pay + " (owe $" + d.loanPrincipal + ")."; return;
        }
        resultText.text = "Cash $" + stats.money + "  |  Bank $" + d.bank + (d.loanPrincipal > 0 ? "  |  Loan $" + d.loanPrincipal : "");
    }

    void Gamble()
    {
        if (stats.money < 10) { resultText.text = "You need $10 to play."; return; }
        if (DiceRollUI.Instance == null) { resultText.text = "Table closed."; return; }
        stats.money -= 10;
        DiceRollUI.Instance.Roll(stats.charisma, 50, "Charm the poker dealer", res =>
        {
            if (res.success) { stats.money += 25; resultText.text = "You read the table — won $25!"; }
            else resultText.text = "House wins. Lost your $10.";
        });
    }

    void Heal()
    {
        int need = Mathf.CeilToInt(stats.maxHealth - stats.health);
        if (need <= 0) { resultText.text = "You're already in good health."; return; }
        if (stats.money < need) { resultText.text = "Treatment costs $" + need + " — you can't afford it."; return; }
        stats.money -= need; stats.health = stats.maxHealth;
        resultText.text = "Fully healed for $" + need + ".";
    }

    void BuyItem(string id, int cost)
    {
        if (stats.money < cost) { resultText.text = "Not enough cash ($" + cost + ")."; return; }
        var def = ItemCatalog.Get(id);
        if (inv == null || !inv.AddToPack(id)) { resultText.text = "Your pack is full."; return; }
        stats.money -= cost;
        resultText.text = "Bought a " + (def != null ? def.name : id) + " (-$" + cost + "). It's in your pack.";
    }

    void BuyCar()
    {
        var d = gsc != null ? gsc.Data : null; if (d == null) return;
        if (d.ownsCar) { resultText.text = "You already own a car."; return; }
        if (stats.money < 2000) { resultText.text = "A car costs $2000 — save up."; return; }
        stats.money -= 2000; d.ownsCar = true;
        resultText.text = "Bought a car! Get a license, then press V to drive.";
    }

    void BuySkateboard()
    {
        var d = gsc != null ? gsc.Data : null; if (d == null) return;
        if (d.ownsSkateboard) { resultText.text = "You already have a skateboard."; return; }
        if (stats.money < 150) { resultText.text = "A skateboard costs $150."; return; }
        stats.money -= 150; d.ownsSkateboard = true;
        resultText.text = "Bought a skateboard! Press V to ride.";
    }

    void DrivingTest()
    {
        var d = gsc != null ? gsc.Data : null; if (d == null) return;
        if (d.hasLicense) { resultText.text = "You already have a license."; return; }
        if (DiceRollUI.Instance == null) return;
        DiceRollUI.Instance.Roll(stats.toolSkill, 60, "Driving test", res =>
        {
            if (res.success) { d.hasLicense = true; resultText.text = "You passed! License granted — press V to drive."; }
            else resultText.text = "Failed the driving test. Raise your Tool skill and retry.";
        });
    }

    bool Daytime() { return clock == null || clock.IsDay; }
    void SkipHours(float h) { if (clock != null) clock.gameHours += h; }

    // ---- UI ----
    void Add(string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = Panel(holder, "Btn", new Vector2(0.5f, 1), Vector2.zero, new Vector2(440, 40), new Color(0.14f, 0.18f, 0.24f, 1f));
        go.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        Label(go, "T", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 40), label, 15, TextAnchor.MiddleCenter, Color.white);
        buttons.Add(go.gameObject);
    }

    void LayoutButtons()
    {
        float y = -10;
        foreach (var b in buttons)
        {
            var rt = b.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, y);
            y -= 48;
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("BuildingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 56;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var card = Panel(canvasGO.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 420), new Color(0.07f, 0.10f, 0.14f, 0.98f));
        group = card.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false;

        titleText = Label(card, "Title", new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(480, 30), "Building", 20, TextAnchor.MiddleCenter, new Color(0.96f, 0.83f, 0.39f));
        resultText = Label(card, "Result", new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(470, 36), "", 13, TextAnchor.UpperCenter, new Color(0.7f, 0.86f, 0.95f));

        holder = Panel(card, "Buttons", new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(460, 300), new Color(0, 0, 0, 0));
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
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
