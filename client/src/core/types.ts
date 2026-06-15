// Shared data shapes used across the game.

import type { SkillKey } from './constants';

export type Skills = Record<SkillKey, number>;

/** What a character looks like (simple primitive-based humanoid). */
export interface Appearance {
  skin: string;
  hair: string;
  shirt: string;
  pants: string;
}

/** Government ID — created once, permanent, ties all owned assets to a player. */
export interface IDCard {
  name: string;
  ssn: string;
  /** ISO date string YYYY-MM-DD. */
  dob: string;
}

export type ItemKind = 'weapon' | 'food' | 'tool' | 'clothing' | 'misc';
export type HandSlot = 'left' | 'right';

export interface ItemDef {
  id: string;
  name: string;
  /** Emoji / glyph shown in slots and the grid. */
  icon: string;
  kind: ItemKind;
  /** Requires both hands (e.g. a rifle, a big box). */
  twoHanded?: boolean;
  /** Weapon stats used by combat (base damage and whether it leans on tool skill). */
  weapon?: { base: number; usesTool: boolean; moves: string[] };
  /** Food restores hunger / health when used. */
  food?: { hunger?: number; health?: number };
  /** Flavour text. */
  desc?: string;
}

/** A live stack of an item sitting in a slot or grid cell. */
export interface ItemStack {
  defId: string;
  qty: number;
}

/** The persistent, ID-bound account. Survives character death. */
export interface AccountData {
  id: IDCard;
  /** Money safe in the bank — persists through death. */
  bank: number;
  /** Items stored away (house safe / closet / stash) — persist through death. */
  storage: (ItemStack | null)[];
  /** Owned assets (houses, cars) — placeholder for v0.2+. */
  assets: string[];
  /** How many characters this player has burned through. */
  deaths: number;
  createdAt: number;
}

/** The mortal character — re-rolled on death. */
export interface CharacterData {
  skills: Skills;
  appearance: Appearance;
  health: number;
  maxHealth: number;
  hunger: number;
  /** Cash on hand (lost on death). */
  cash: number;
  /** Items in hands (lost on death). */
  hands: { left: ItemStack | null; right: ItemStack | null };
  /** Backpack-style carried grid (lost on death). */
  pack: (ItemStack | null)[];
  /** Last position in the world. */
  pos: { x: number; z: number };
  alive: boolean;
}

/** What we serialise to local storage. */
export interface SaveData {
  version: string;
  account: AccountData;
  character: CharacterData;
  /** Game clock, in elapsed game-hours since a fixed epoch. */
  gameHours: number;
  savedAt: number;
}
