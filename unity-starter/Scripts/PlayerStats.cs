using UnityEngine;

// HoboLife — the core stat model, ported from the web prototype: four skills
// (0–999), health (scales with Strength), hunger (decays over time), and money.
public class PlayerStats : MonoBehaviour
{
    public const int SkillMax = 999;

    [Range(0, SkillMax)] public int intelligence;
    [Range(0, SkillMax)] public int charisma;
    [Range(0, SkillMax)] public int strength;
    [Range(0, SkillMax)] public int toolSkill;

    public float health = 100f;
    public float maxHealth = 100f;
    public float hunger = 100f;
    public int money;

    [Tooltip("Hunger lost per real second (tune for pacing).")]
    public float hungerDecayPerSecond = 0.06f;
    public float starvationDamagePerSecond = 3f;

    void Update()
    {
        hunger = Mathf.Max(0f, hunger - hungerDecayPerSecond * Time.deltaTime);
        if (hunger <= 0f)
            health = Mathf.Max(0f, health - starvationDamagePerSecond * Time.deltaTime);

        maxHealth = 100f + strength * 0.5f;
        health = Mathf.Min(health, maxHealth);
    }

    public void AddSkill(string key, int amount)
    {
        switch (key)
        {
            case "int": intelligence = Mathf.Clamp(intelligence + amount, 0, SkillMax); break;
            case "cha": charisma = Mathf.Clamp(charisma + amount, 0, SkillMax); break;
            case "str": strength = Mathf.Clamp(strength + amount, 0, SkillMax); break;
            case "tool": toolSkill = Mathf.Clamp(toolSkill + amount, 0, SkillMax); break;
        }
    }
}
