using UnityEngine;
using UnityEngine.UI;

// HoboLife — drives the on-screen HUD from PlayerStats + GameClock each frame.
// References are wired by HoboLifeHudBuilder.
public class HudController : MonoBehaviour
{
    public PlayerStats stats;
    public GameClock clock;

    public Image healthFill, hungerFill;
    public Text healthText, hungerText;
    public Image intFill, chaFill, strFill, toolFill;
    public Text intText, chaText, strText, toolText;
    public Text moneyText, clockText, dayText;

    static readonly Color HungerNormal = new Color(0.96f, 0.64f, 0.35f);
    static readonly Color HungerLow = new Color(0.90f, 0.33f, 0.23f);

    void Update()
    {
        if (stats != null)
        {
            float maxH = Mathf.Max(1f, stats.maxHealth);
            if (healthFill) healthFill.fillAmount = Mathf.Clamp01(stats.health / maxH);
            if (healthText) healthText.text = Mathf.CeilToInt(stats.health) + "/" + Mathf.RoundToInt(maxH);

            if (hungerFill)
            {
                hungerFill.fillAmount = Mathf.Clamp01(stats.hunger / HoboBalance.HUNGER_MAX);
                hungerFill.color = stats.hunger < 20f ? HungerLow : HungerNormal;
            }
            if (hungerText) hungerText.text = Mathf.CeilToInt(stats.hunger) + "/100";

            SetSkill(intFill, intText, stats.intelligence);
            SetSkill(chaFill, chaText, stats.charisma);
            SetSkill(strFill, strText, stats.strength);
            SetSkill(toolFill, toolText, stats.toolSkill);

            if (moneyText) moneyText.text = "$" + stats.money.ToString("N0");
        }

        if (clock != null)
        {
            if (clockText) clockText.text = (clock.IsDay ? "☀ " : "☽ ") + clock.TimeString();
            if (dayText) dayText.text = "Day " + clock.DayNumber;
        }
    }

    void SetSkill(Image fill, Text txt, int val)
    {
        if (fill) fill.fillAmount = Mathf.Clamp01(val / (float)HoboBalance.SKILL_MAX);
        if (txt) txt.text = val.ToString();
    }
}
