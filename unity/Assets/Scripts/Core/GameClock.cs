using UnityEngine;

// HoboLife — in-game clock (1 real hour = 1 full 24h game day) and a day/night
// cycle that rotates + dims the directional light. No sleeping; jobs/businesses
// run only in daytime (other systems read IsDay).
public class GameClock : MonoBehaviour
{
    [Tooltip("Total elapsed game hours (starts at the new-game hour).")]
    public float gameHours = HoboBalance.NEW_GAME_START_HOUR;

    public Light sun;
    [Range(0f, 3f)] public float dayIntensity = 1.25f;
    [Range(0f, 1f)] public float nightIntensity = 0.08f;

    public float GameHour => Mathf.Repeat(gameHours, 24f);
    public int DayNumber => Mathf.FloorToInt(gameHours / 24f) + 1;
    public bool IsDay => GameHour >= HoboBalance.DAY_START_HOUR && GameHour < HoboBalance.NIGHT_START_HOUR;

    void Update()
    {
        gameHours += Time.deltaTime * HoboBalance.GAME_HOURS_PER_REAL_SECOND;

        if (sun != null)
        {
            float h = GameHour;
            // Sun arcs overhead: horizontal at 6am, straight down at noon, below at midnight.
            sun.transform.rotation = Quaternion.Euler((h / 24f) * 360f - 90f, 170f, 0f);
            float dayT = Mathf.Clamp01(Mathf.Cos((h - 12f) / 24f * Mathf.PI * 2f) * 0.5f + 0.5f);
            sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dayT);
            sun.color = Color.Lerp(new Color(0.45f, 0.55f, 0.85f), new Color(1f, 0.96f, 0.86f), dayT);
        }
    }

    public string TimeString()
    {
        float h = GameHour;
        int hh = Mathf.FloorToInt(h);
        int mm = Mathf.FloorToInt((h - hh) * 60f);
        string ampm = hh >= 12 ? "PM" : "AM";
        int hr12 = ((hh + 11) % 12) + 1;
        return string.Format("{0}:{1:00} {2}", hr12, mm, ampm);
    }
}
