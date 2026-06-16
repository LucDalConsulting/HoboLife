using System;

// HoboLife — JSON save model (faithful to the web prototype's persistence split):
// the ID card + bank survive death; the character (skills/cash/hunger/health) is
// re-rolled on death.
[Serializable]
public class IDCard
{
    public string name = "Hobo";
    public string ssn = "000-00-0000";
    public string dob = "01/01/2000";
}

[Serializable]
public class SaveData
{
    public string version = HoboBalance.VERSION;

    // --- persists through death ---
    public IDCard id = new IDCard();
    public int bank;
    public int loanPrincipal;
    public bool ownsCar, ownsSkateboard, hasLicense;
    public int deaths;

    // --- character (reset on death) ---
    public int intelligence, charisma, strength, toolSkill;
    public int money;
    public float health, maxHealth, hunger;

    public float gameHours;
    public long savedAt;
}
