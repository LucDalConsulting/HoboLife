using UnityEngine;

// HoboLife — bridges the save file with the live game. On start it loads (or
// creates) a save keyed to an ID card, hydrates PlayerStats + GameClock,
// autosaves periodically, and handles death/respawn: the character resets but
// the ID card, bank, and death count persist.
public class GameStateController : MonoBehaviour
{
    public Vector3 spawn = new Vector3(0f, 1.1f, 14f);
    public float autosaveSeconds = 30f;

    PlayerStats stats;
    GameClock clock;
    SaveData data;
    float saveTimer;
    bool dying;

    void Start()
    {
        stats = Object.FindFirstObjectByType<PlayerStats>();
        clock = Object.FindFirstObjectByType<GameClock>();
        saveTimer = autosaveSeconds;

        data = SaveSystem.Load();
        if (data == null)
        {
            if (CharacterCreationUI.Instance != null)
            {
                CharacterCreationUI.Instance.Begin(OnCreationDone);
                return; // wait for the player; OnCreationDone finishes setup
            }
            data = NewGame();
            SaveSystem.Write(data);
        }
        else
        {
            Debug.Log("[HoboLife] Loaded save: '" + data.id.name + "' SSN " + data.id.ssn + ", bank $" + data.bank + ", deaths " + data.deaths);
        }
        Hydrate();
    }

    void Update()
    {
        if (stats == null || data == null) return;
        if (!dying && stats.health <= 0f) { Die(); return; }
        if ((saveTimer -= Time.deltaTime) <= 0f) { saveTimer = autosaveSeconds; Capture(); SaveSystem.Write(data); }
    }

    SaveData NewGame()
    {
        var d = new SaveData();
        d.id.name = "Hobo";
        d.id.ssn = SaveSystem.GenerateSSN();
        d.id.dob = "01/01/2000";
        d.bank = 0;
        d.deaths = 0;
        ResetCharacter(d);
        return d;
    }

    void ResetCharacter(SaveData d)
    {
        d.intelligence = 0; d.charisma = 0; d.strength = 0; d.toolSkill = 0;
        d.money = 0;
        d.hunger = HoboBalance.HUNGER_MAX;
        d.maxHealth = HoboBalance.MaxHealthFor(0);
        d.health = d.maxHealth;
        d.gameHours = HoboBalance.NEW_GAME_START_HOUR;
    }

    void Hydrate()
    {
        if (stats != null)
        {
            stats.intelligence = data.intelligence;
            stats.charisma = data.charisma;
            stats.strength = data.strength;
            stats.toolSkill = data.toolSkill;
            stats.money = data.money;
            stats.hunger = data.hunger;
            stats.maxHealth = data.maxHealth;
            stats.health = data.health;
        }
        if (clock != null) clock.gameHours = data.gameHours;
    }

    void Capture()
    {
        if (stats != null)
        {
            data.intelligence = stats.intelligence;
            data.charisma = stats.charisma;
            data.strength = stats.strength;
            data.toolSkill = stats.toolSkill;
            data.money = stats.money;
            data.hunger = stats.hunger;
            data.maxHealth = stats.maxHealth;
            data.health = stats.health;
        }
        if (clock != null) data.gameHours = clock.gameHours;
    }

    void Die()
    {
        dying = true;
        data.deaths += 1;
        ResetCharacter(data);   // wipe cash, skills, hunger, health (keep bank + ID)
        Hydrate();
        var player = GameObject.Find("Player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) { cc.enabled = false; player.transform.position = spawn; cc.enabled = true; }
        }
        SaveSystem.Write(data);
        Debug.Log("[HoboLife] You died and respawned as a new hobo. Deaths=" + data.deaths + " (bank $" + data.bank + " and ID kept).");
        dying = false;
    }

    void OnCreationDone(string name, string dob, int[] sk, AppearanceConfig look)
    {
        data = NewGame();
        data.id.name = string.IsNullOrWhiteSpace(name) ? "Hobo" : name.Trim();
        data.id.dob = dob;
        data.intelligence = sk[0]; data.charisma = sk[1]; data.strength = sk[2]; data.toolSkill = sk[3];
        data.maxHealth = HoboBalance.MaxHealthFor(sk[2]);
        data.health = data.maxHealth;
        SaveSystem.Write(data);
        Hydrate();
        ApplyAppearance(look);
        Debug.Log("[HoboLife] Created '" + data.id.name + "' SSN " + data.id.ssn + " skills "
                  + sk[0] + "/" + sk[1] + "/" + sk[2] + "/" + sk[3]);
    }

    void ApplyAppearance(AppearanceConfig look)
    {
        var player = GameObject.Find("Player");
        var body = player != null ? player.transform.Find("Body") : null;
        if (body == null) return;
        foreach (var r in body.GetComponentsInChildren<Renderer>())
        {
            string n = r.gameObject.name;
            if (n == "Pelvis") continue;                 // underwear stays
            Color c = (n == "Hair") ? look.hair : look.skin;
            var m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            m.color = c;
        }
    }

    public SaveData Data => data;

    void OnApplicationQuit() { Capture(); SaveSystem.Write(data); }
    void OnApplicationPause(bool paused) { if (paused) { Capture(); SaveSystem.Write(data); } }
}
