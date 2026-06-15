// Pokémon-style combat resolution. Damage and escape are dice-scaled and
// weighted by Strength (all moves) and Tool skill (weapon moves).

import { rollD10 } from './rng';

export interface Combatant {
  name: string;
  str: number;
  tool: number;
  hp: number;
  maxHp: number;
}

export interface MoveDef {
  id: string;
  label: string;
  /** Base damage before scaling. */
  base: number;
  /** Weapon moves lean on Tool skill as well as Strength. */
  usesTool: boolean;
}

export interface AttackResult {
  roll: number;
  damage: number;
  miss: boolean; // roll === 1
  crit: boolean; // roll === 10
}

/**
 * Damage = base × strScale × toolScale × variance × crit, with a d10 driving
 * both the variance and the crit/miss bands.
 */
export function resolveAttack(move: MoveDef, attacker: Combatant): AttackResult {
  const roll = rollD10();
  if (roll === 1) return { roll, damage: 0, miss: true, crit: false };

  const crit = roll === 10;
  const strScale = 1 + attacker.str / 250; // up to ~5x at 999 STR
  const toolScale = move.usesTool ? 1 + attacker.tool / 300 : 1;
  const variance = 0.6 + roll * 0.08; // ~0.68 .. 1.4
  const critMult = crit ? 2 : 1;

  const damage = Math.max(
    1,
    Math.round(move.base * strScale * toolScale * variance * critMult),
  );
  return { roll, damage, miss: false, crit };
}

export interface RunResult {
  roll: number;
  success: boolean;
  score: number;
}

/**
 * Escape check. A bigger roll, more Strength, and more remaining health than the
 * opponent all make fleeing easier. Roll of 1 always fails, 10 always succeeds.
 */
export function resolveRun(self: Combatant, opp: Combatant): RunResult {
  const roll = rollD10();
  if (roll === 1) return { roll, success: false, score: 0 };
  if (roll === 10) return { roll, success: true, score: 99 };

  const strEdge = (self.str - opp.str) / 100; // ±~10
  const hpEdge = (self.hp / self.maxHp - opp.hp / opp.maxHp) * 3; // ±3
  const score = roll + strEdge + hpEdge;
  return { roll, success: score >= 7, score };
}
