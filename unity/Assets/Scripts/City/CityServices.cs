using UnityEngine;

// HoboLife — what happens when you enter a landmark. These are first-pass stubs
// that apply real effects (stat gains, food, healing); deeper systems (job
// mini-games, full banking, realtor) come in later milestones.
public static class CityServices
{
    public static string Enter(string kind, PlayerStats s)
    {
        if (s == null) return "";
        switch (kind)
        {
            case "university":
                s.AddSkill("int", 2);
                return "You studied. Intelligence +2.";
            case "gym":
                s.AddSkill("str", 2);
                return "You hit the gym. Strength +2.";
            case "diner":
                if (s.money >= 5) { s.money -= 5; s.hunger = Mathf.Min(HoboBalance.HUNGER_MAX, s.hunger + 35f); return "Ate a greasy burger. Hunger +35 (-$5)."; }
                return "Not enough cash for food ($5).";
            case "hospital":
                s.health = s.maxHealth;
                return "Patched up at Mercy Hospital. Health full.";
            case "bank":
                return "Bank balance: $" + s.money + " (full banking soon).";
            default:
                return kind + " — opening soon.";
        }
    }
}
