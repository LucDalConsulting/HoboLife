// Save/load to local storage, plus factories for new accounts and characters.
// The account (ID card, bank, stored items) is permanent; the character is
// mortal and re-rolled on death.

import {
  BASE_HEALTH,
  HEALTH_PER_STRENGTH,
  HUNGER_MAX,
  LS_SAVE,
  STARTING_CASH,
  VERSION,
} from '../core/constants';
import { randInt } from '../core/rng';
import { SPAWN } from '../data/city';
import type {
  AccountData,
  Appearance,
  CharacterData,
  IDCard,
  SaveData,
  Skills,
} from '../core/types';

export const PACK_SIZE = 12;
export const STORAGE_SIZE = 24;

/** Max health for a given strength. */
export function maxHealthFor(strength: number): number {
  return Math.round(BASE_HEALTH + strength * HEALTH_PER_STRENGTH);
}

/** Generate a plausible-looking (fake) SSN: XXX-XX-XXXX. */
export function generateSSN(): string {
  const a = String(randInt(100, 899)).padStart(3, '0');
  const b = String(randInt(10, 99)).padStart(2, '0');
  const c = String(randInt(1000, 9999)).padStart(4, '0');
  return `${a}-${b}-${c}`;
}

export function defaultAppearance(): Appearance {
  return { skin: '#e8b98a', hair: '#3b2412', shirt: '#6b8f3a', pants: '#2c3e50' };
}

export function newAccount(id: IDCard): AccountData {
  return {
    id,
    bank: 0,
    storage: new Array(STORAGE_SIZE).fill(null),
    assets: [],
    deaths: 0,
    createdAt: Date.now(),
  };
}

export function newCharacter(skills: Skills, appearance: Appearance): CharacterData {
  const maxHealth = maxHealthFor(skills.str);
  return {
    skills: { ...skills },
    appearance: { ...appearance },
    health: maxHealth,
    maxHealth,
    hunger: HUNGER_MAX,
    cash: STARTING_CASH,
    hands: { left: null, right: { defId: 'cardboard_sign', qty: 1 } },
    pack: new Array(PACK_SIZE).fill(null),
    pos: { x: SPAWN.x, z: SPAWN.z },
    alive: true,
  };
}

export function newSave(account: AccountData, character: CharacterData): SaveData {
  return {
    version: VERSION,
    account,
    character,
    gameHours: 9, // start at 9am, in daytime
    savedAt: Date.now(),
  };
}

export function loadSave(): SaveData | null {
  try {
    const raw = localStorage.getItem(LS_SAVE);
    if (!raw) return null;
    const data = JSON.parse(raw) as SaveData;
    if (!data.account || !data.character) return null;
    return data;
  } catch {
    return null;
  }
}

export function writeSave(save: SaveData): void {
  save.savedAt = Date.now();
  save.version = VERSION;
  try {
    localStorage.setItem(LS_SAVE, JSON.stringify(save));
  } catch {
    /* storage full / disabled — ignore */
  }
}

export function clearSave(): void {
  try {
    localStorage.removeItem(LS_SAVE);
  } catch {
    /* ignore */
  }
}
