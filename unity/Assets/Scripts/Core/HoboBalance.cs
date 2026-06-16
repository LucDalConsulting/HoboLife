using UnityEngine;

// HoboLife — single source of truth for balance numbers, traced from the web
// prototype (client/src/core/constants.ts, version 0.3.0). New systems read
// these so no magic numbers leak.
public static class HoboBalance
{
    public const int SKILL_MAX = 999;
    public const int CREATION_POINTS = 20;

    public const float BASE_HEALTH = 100f;
    public const float HEALTH_PER_STRENGTH = 0.5f;

    public const float HUNGER_MAX = 100f;
    public const float HUNGER_DECAY_PER_GAME_HOUR = 4f;
    public const float STARVATION_HEALTH_PER_GAME_HOUR = 20f;

    public const float REAL_SECONDS_PER_GAME_DAY = 3600f;   // 1 real hour = 1 game day
    public const float GAME_HOURS_PER_DAY = 24f;
    public const float GAME_HOURS_PER_REAL_SECOND = GAME_HOURS_PER_DAY / REAL_SECONDS_PER_GAME_DAY; // 0.006667

    public const int DAY_START_HOUR = 6;
    public const int NIGHT_START_HOUR = 20;
    public const float NEW_GAME_START_HOUR = 9f;

    public const int STARTING_CASH = 0;
    public const int PACK_SIZE = 12;
    public const int STORAGE_SIZE = 24;

    public const string VERSION = "0.3.0";

    public static float MaxHealthFor(int strength) => Mathf.Round(BASE_HEALTH + strength * HEALTH_PER_STRENGTH);
}
