// Central runtime state: wraps the SaveData and exposes safe mutations with
// clamping, survival ticking, money/skill helpers, and death/respawn — all of
// which keep the persistent account intact while the character is mortal.

import {
  DAY_START_HOUR,
  GAME_HOURS_PER_REAL_SECOND,
  HUNGER_DECAY_PER_GAME_HOUR,
  HUNGER_MAX,
  NIGHT_START_HOUR,
  SKILL_MAX,
  STARVATION_HEALTH_PER_GAME_HOUR,
  type SkillKey,
} from '../core/constants';
import { clamp } from '../core/rng';
import type { Appearance, CharacterData, SaveData, Skills } from '../core/types';
import { maxHealthFor, newCharacter, writeSave } from './persistence';

export class GameState {
  save: SaveData;
  private listeners = new Set<() => void>();

  constructor(save: SaveData) {
    this.save = save;
    // Recompute max health in case balance changed since last save.
    this.save.character.maxHealth = maxHealthFor(this.save.character.skills.str);
  }

  get character(): CharacterData {
    return this.save.character;
  }
  get account() {
    return this.save.account;
  }
  get skills(): Skills {
    return this.save.character.skills;
  }

  // --- time -----------------------------------------------------------------

  /** Hour within the current game day, 0..24. */
  get gameHour(): number {
    return ((this.save.gameHours % 24) + 24) % 24;
  }
  get dayNumber(): number {
    return Math.floor(this.save.gameHours / 24) + 1;
  }
  isDay(): boolean {
    const h = this.gameHour;
    return h >= DAY_START_HOUR && h < NIGHT_START_HOUR;
  }

  /** Advance the clock and apply hunger decay / starvation for a real dt. */
  advanceTime(realDt: number): void {
    const dGameHours = realDt * GAME_HOURS_PER_REAL_SECOND;
    this.save.gameHours += dGameHours;

    const c = this.character;
    c.hunger = clamp(c.hunger - HUNGER_DECAY_PER_GAME_HOUR * dGameHours, 0, HUNGER_MAX);
    if (c.hunger <= 0 && c.alive) {
      this.damage(STARVATION_HEALTH_PER_GAME_HOUR * dGameHours, /*silent*/ true);
    }
  }

  /** Jump the clock forward by whole game-hours (an action that "takes time"),
   *  applying the corresponding hunger cost. */
  skipHours(hours: number): void {
    this.save.gameHours += hours;
    const c = this.character;
    c.hunger = clamp(c.hunger - HUNGER_DECAY_PER_GAME_HOUR * hours, 0, HUNGER_MAX);
    if (c.hunger <= 0 && c.alive) {
      this.damage(STARVATION_HEALTH_PER_GAME_HOUR * hours, true);
    }
    this.emit();
  }

  // --- skills & money -------------------------------------------------------

  addSkill(key: SkillKey, amount: number): void {
    const c = this.character;
    c.skills[key] = clamp(Math.round(c.skills[key] + amount), 0, SKILL_MAX);
    if (key === 'str') {
      // Strength raises max health; top up by the gained headroom.
      const newMax = maxHealthFor(c.skills.str);
      c.health += Math.max(0, newMax - c.maxHealth);
      c.maxHealth = newMax;
    }
    this.emit();
  }

  /** Cash on hand (lost on death). */
  addCash(amount: number): void {
    this.character.cash = Math.max(0, Math.round(this.character.cash + amount));
    this.emit();
  }
  spendCash(amount: number): boolean {
    if (this.character.cash < amount) return false;
    this.character.cash = Math.round(this.character.cash - amount);
    this.emit();
    return true;
  }

  /** Bank balance (survives death). */
  deposit(amount: number): boolean {
    if (!this.spendCash(amount)) return false;
    this.account.bank += amount;
    this.emit();
    return true;
  }
  withdraw(amount: number): boolean {
    if (this.account.bank < amount) return false;
    this.account.bank -= amount;
    this.addCash(amount);
    return true;
  }

  // --- survival -------------------------------------------------------------

  eat(hunger = 0, health = 0): void {
    const c = this.character;
    c.hunger = clamp(c.hunger + hunger, 0, HUNGER_MAX);
    if (health) this.heal(health);
    this.emit();
  }
  heal(amount: number): void {
    const c = this.character;
    c.health = clamp(c.health + amount, 0, c.maxHealth);
    this.emit();
  }
  damage(amount: number, silent = false): void {
    const c = this.character;
    c.health = clamp(c.health - amount, 0, c.maxHealth);
    if (c.health <= 0) c.alive = false;
    if (!silent) this.emit();
  }
  isDead(): boolean {
    return !this.character.alive || this.character.health <= 0;
  }

  // --- death / respawn ------------------------------------------------------

  /** Re-roll the character but keep the account (bank, storage, assets, ID). */
  respawn(skills: Skills, appearance: Appearance): void {
    this.account.deaths += 1;
    this.save.character = newCharacter(skills, appearance);
    this.emit();
    this.persist();
  }

  setPosition(x: number, z: number): void {
    this.character.pos.x = x;
    this.character.pos.z = z;
  }

  // --- persistence & events -------------------------------------------------

  persist(): void {
    writeSave(this.save);
  }
  onChange(cb: () => void): () => void {
    this.listeners.add(cb);
    return () => this.listeners.delete(cb);
  }
  emit(): void {
    for (const cb of this.listeners) cb();
  }
}
