using UnityEngine;

// HoboLife — the D&D-style d10 skill check, ported from the web build:
//   effective = skill * roll
//     roll of 10 -> counts as x20 (critical success)
//     roll of 1  -> automatic failure
//   success = effective >= required (the task's hidden difficulty)
public static class DiceCheck
{
    public struct Result
    {
        public int roll;
        public int skill;
        public int multiplier;
        public int effective;
        public int required;
        public bool success;
        public bool autoFail;
        public bool critical;
    }

    public static Result Resolve(int skill, int required, int forcedRoll = 0)
    {
        int roll = forcedRoll > 0 ? forcedRoll : Random.Range(1, 11); // 1..10
        bool autoFail = roll == 1;
        bool critical = roll == 10;
        int multiplier = autoFail ? 0 : critical ? 20 : roll;
        int effective = skill * multiplier;

        return new Result
        {
            roll = roll,
            skill = skill,
            multiplier = multiplier,
            effective = effective,
            required = required,
            success = !autoFail && effective >= required,
            autoFail = autoFail,
            critical = critical,
        };
    }
}
