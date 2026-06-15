// The core D&D-style skill-check engine.
//
//   effective = skillPoints × roll
//     • roll of 10 → counts as ×10 and is then doubled (×20)  [critical success]
//     • roll of 1  → automatic failure regardless of stats     [critical failure]
//   success = effective ≥ requiredLevel (the task's hidden DC)
//
// Example: a date needs 50 charisma.
//   5 CHA  × roll 9  = 45 → fail
//   10 CHA × roll 9  = 90 → success

import { rollD10 } from './rng';

export interface SkillCheckResult {
  /** The raw die face, 1..10. */
  roll: number;
  /** Skill points that were applied. */
  skill: number;
  /** Effective multiplier after crit rules (roll, or 20 on a 10, or 0 on a 1). */
  multiplier: number;
  /** skill × multiplier. */
  effective: number;
  /** The difficulty class that had to be met. */
  required: number;
  success: boolean;
  /** roll === 1 */
  autoFail: boolean;
  /** roll === 10 */
  critical: boolean;
}

/**
 * Resolve a skill check. Pass a fixed `roll` to make it deterministic (tests,
 * replays); otherwise a fresh d10 is rolled.
 */
export function resolveSkillCheck(
  skill: number,
  required: number,
  roll: number = rollD10(),
): SkillCheckResult {
  const autoFail = roll === 1;
  const critical = roll === 10;
  const multiplier = autoFail ? 0 : critical ? 20 : roll;
  const effective = skill * multiplier;
  const success = !autoFail && effective >= required;
  return { roll, skill, multiplier, effective, required, success, autoFail, critical };
}

/** Human-readable one-liner for the dice overlay / log. */
export function describeCheck(r: SkillCheckResult): string {
  if (r.autoFail) return `Rolled a 1 — critical failure!`;
  const crit = r.critical ? ' (critical x20!)' : '';
  return `${r.skill} × ${r.multiplier}${crit} = ${r.effective} vs ${r.required} → ${
    r.success ? 'SUCCESS' : 'fail'
  }`;
}
