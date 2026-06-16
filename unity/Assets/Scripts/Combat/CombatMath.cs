using UnityEngine;

// HoboLife — combat resolution (ported formulas). A d10 scales every action;
// Strength and (for weapons) Tool skill weight the damage.
public static class CombatMath
{
    public static int RollD10() => Random.Range(1, 11);

    // Returns damage dealt (0 = miss on a natural 1). roll 10 = crit (x2).
    public static int Damage(int str, int tool, int baseDmg, bool usesTool, int roll, out bool crit, out bool miss)
    {
        crit = false; miss = false;
        if (roll <= 1) { miss = true; return 0; }
        float strScale = 1f + str / 250f;
        float toolScale = usesTool ? 1f + tool / 300f : 1f;
        float variance = 0.6f + roll * 0.08f;
        float critMult = (roll >= 10) ? 2f : 1f;
        crit = roll >= 10;
        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * strScale * toolScale * variance * critMult));
    }

    // Run-away success: low/high rolls decide outright; otherwise STR + HP edge.
    public static bool CanRun(int selfStr, int oppStr, int selfHp, int selfMax, int oppHp, int oppMax, int roll)
    {
        if (roll <= 1) return false;
        if (roll >= 10) return true;
        float strEdge = (selfStr - oppStr) / 100f;
        float hpEdge = ((float)selfHp / Mathf.Max(1, selfMax) - (float)oppHp / Mathf.Max(1, oppMax)) * 3f;
        return roll + strEdge + hpEdge >= 7f;
    }
}
