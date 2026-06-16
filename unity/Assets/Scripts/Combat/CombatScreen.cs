using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HoboLife — the Pokémon-style battle screen. Both HP bars, a rolling log, and
// four move buttons derived from what's in your hands when the fight starts.
// Opened by the dialogue "fight" option (and later by hostile-NPC contact).
public class CombatScreen : MonoBehaviour
{
    public static CombatScreen Instance { get; private set; }
    public bool IsActive { get; private set; }

    class Fighter
    {
        public string name;
        public int str, tool, hp, maxHp;
        public bool guarding;
        public List<Move> attacks = new List<Move>();
    }
    struct Move { public string name; public int baseDmg; public bool usesTool; }

    Fighter player, enemy;
    Action onClosed;

    CanvasGroup group;
    Text enemyName, playerName, logText;
    Image enemyHp, playerHp;
    Button[] btns = new Button[4];
    Text[] btnTexts = new Text[4];
    readonly Queue<string> log = new Queue<string>();
    bool busy;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    public void Begin(string enemyDisplayName, bool tough, Action onEnd = null)
    {
        if (IsActive) return;
        onClosed = onEnd;

        var stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        var inv = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();

        player = new Fighter { name = "You", str = stats.strength, tool = stats.toolSkill,
                               maxHp = Mathf.RoundToInt(stats.maxHealth), hp = Mathf.RoundToInt(stats.health) };
        player.attacks = MovesFromHands(inv);

        enemy = new Fighter { name = enemyDisplayName };
        enemy.str = UnityEngine.Random.Range(tough ? 20 : 8, tough ? 45 : 30);
        enemy.tool = UnityEngine.Random.Range(5, tough ? 40 : 20);
        enemy.maxHp = UnityEngine.Random.Range(90, 141);
        enemy.hp = enemy.maxHp;
        enemy.attacks = tough ? Weapon2("Stab", "Slash", 12, true) : Fists();

        IsActive = true;
        busy = false;
        group.alpha = 1f; group.blocksRaycasts = true;
        log.Clear();
        Log("A fight breaks out with " + enemy.name + "!");
        RefreshBars();
        SetButtons();
        Refresh();
    }

    List<Move> MovesFromHands(PlayerInventory inv)
    {
        ItemDef w = inv != null ? (FirstWeapon(inv.leftHand) ?? FirstWeapon(inv.rightHand)) : null;
        if (w == null) return Fists();
        switch (w.combatStyle)
        {
            case "knife": return Weapon2("Stab", "Slash", w.damageBonus, true);
            case "gun": return Weapon2("Shoot", "Pistol-whip", w.damageBonus, true);
            default: return Weapon2("Swing", "Jab", w.damageBonus, false);
        }
    }
    static ItemDef FirstWeapon(string id) { var d = ItemCatalog.Get(id); return (d != null && d.kind == ItemKind.Weapon) ? d : null; }
    static List<Move> Fists() => new List<Move> { new Move { name = "Punch", baseDmg = 6 }, new Move { name = "Kick", baseDmg = 9 } };
    static List<Move> Weapon2(string a, string b, int baseDmg, bool tool) => new List<Move> {
        new Move { name = a, baseDmg = baseDmg, usesTool = tool },
        new Move { name = b, baseDmg = Mathf.RoundToInt(baseDmg * 0.75f), usesTool = tool } };

    void SetButtons()
    {
        btnTexts[0].text = player.attacks[0].name;
        btnTexts[1].text = player.attacks[1].name;
        btnTexts[2].text = "Guard";
        btnTexts[3].text = "Run";
    }

    void OnButton(int i)
    {
        if (!IsActive || busy) return;
        busy = true;
        if (i == 0 || i == 1) { AttackBy(player, enemy, player.attacks[i]); }
        else if (i == 2) { player.guarding = true; Log("You brace for the next hit."); }
        else { TryRun(); return; }

        Refresh();
        if (enemy.hp <= 0) { End("win"); return; }
        Invoke(nameof(EnemyTurn), 0.7f);
    }

    void EnemyTurn()
    {
        if (!IsActive) return;
        var m = enemy.attacks[UnityEngine.Random.Range(0, enemy.attacks.Count)];
        AttackBy(enemy, player, m);
        Refresh();
        if (player.hp <= 0) { End("lose"); return; }
        busy = false;
    }

    void AttackBy(Fighter atk, Fighter def, Move m)
    {
        int roll = CombatMath.RollD10();
        int dmg = CombatMath.Damage(atk.str, atk.tool, m.baseDmg, m.usesTool, roll, out bool crit, out bool miss);
        if (def.guarding && dmg > 0) { dmg = Mathf.RoundToInt(dmg * 0.5f); def.guarding = false; }
        def.hp = Mathf.Max(0, def.hp - dmg);
        if (miss) Log(atk.name + "'s " + m.name + " missed! (rolled 1)");
        else Log(atk.name + " used " + m.name + " — " + dmg + (crit ? " CRIT!" : "") + " dmg.");
    }

    void TryRun()
    {
        int roll = CombatMath.RollD10();
        bool ok = CombatMath.CanRun(player.str, enemy.str, player.hp, player.maxHp, enemy.hp, enemy.maxHp, roll);
        if (ok) { Log("You got away!"); End("flee"); return; }
        Log("Couldn't escape!");
        Refresh();
        Invoke(nameof(EnemyTurn), 0.7f);
    }

    void End(string result)
    {
        IsActive = false;
        group.alpha = 0f; group.blocksRaycasts = false;
        var stats = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
        if (stats != null) stats.health = Mathf.Clamp(player.hp, 0f, stats.maxHealth);
        Debug.Log("[HoboLife] Combat " + result + " — player HP " + player.hp + "/" + player.maxHp);
        onClosed?.Invoke();
    }

    void Log(string s) { log.Enqueue(s); while (log.Count > 5) log.Dequeue(); logText.text = string.Join("\n", log.ToArray()); }

    void Refresh() { RefreshBars(); }
    void RefreshBars()
    {
        enemyName.text = enemy.name + "   " + enemy.hp + "/" + enemy.maxHp;
        playerName.text = player.name + "   " + player.hp + "/" + player.maxHp;
        enemyHp.fillAmount = Mathf.Clamp01((float)enemy.hp / Mathf.Max(1, enemy.maxHp));
        playerHp.fillAmount = Mathf.Clamp01((float)player.hp / Mathf.Max(1, player.maxHp));
    }

    // ---------- UI ----------
    void BuildUI()
    {
        var canvasGO = new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = MakePanel(canvasGO.transform, "Bg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080), new Color(0.04f, 0.06f, 0.09f, 0.97f));
        group = bg.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false;

        // Enemy (top)
        enemyName = Label(bg, "EnemyName", new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(700, 30), "Enemy", 22, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.6f));
        Bar(bg, "EnemyHpBar", new Vector2(0.5f, 1), new Vector2(0, -98), new Vector2(520, 22), new Color(0.9f, 0.33f, 0.23f), out enemyHp);

        // Player (lower-middle)
        playerName = Label(bg, "PlayerName", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(700, 30), "You", 22, TextAnchor.MiddleCenter, new Color(0.6f, 0.9f, 0.7f));
        Bar(bg, "PlayerHpBar", new Vector2(0.5f, 0.5f), new Vector2(0, -78), new Vector2(520, 22), new Color(0.42f, 0.82f, 0.5f), out playerHp);

        // Log
        logText = Label(bg, "Log", new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(760, 130), "", 16, TextAnchor.UpperCenter, new Color(0.86f, 0.9f, 0.94f));

        // 4 move buttons (2x2 at the bottom)
        float[] xs = { -200, 200, -200, 200 };
        float[] ys = { 150, 150, 96, 96 };
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var b = MakePanel(bg, "Btn" + i, new Vector2(0.5f, 0), new Vector2(xs[i], ys[i]), new Vector2(360, 44), new Color(0.13f, 0.17f, 0.23f, 1f));
            var btn = b.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => OnButton(idx));
            btns[i] = btn;
            btnTexts[i] = Label(b, "T" + i, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 44), "", 18, TextAnchor.MiddleCenter, Color.white);
        }
    }

    static void Bar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor, out Image fill)
    {
        var bg = MakePanel(parent, name, anchor, pos, size, new Color(0.05f, 0.07f, 0.09f, 1f));
        var fillGO = new GameObject(name + "_Fill", typeof(Image));
        fillGO.transform.SetParent(bg, false);
        var rt = fillGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        fill = fillGO.GetComponent<Image>();
        fill.color = fillColor; fill.raycastTarget = false;
        fill.sprite = UnityEditorSpriteOrNull();
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 1f;
    }

    static Sprite UnityEditorSpriteOrNull()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
#else
        return null;
#endif
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
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
