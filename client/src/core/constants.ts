// Global constants and balance values for HoboLife.
// Keeping the tunable numbers in one place makes iteration easy.

/** Semver of the game build. Bump this on each release; the HUD shows it and
 *  the update banner compares it against the deployed version.json. */
export const VERSION = '0.1.0';

/** The four skills. */
export type SkillKey = 'int' | 'cha' | 'str' | 'tool';

export const SKILL_KEYS: SkillKey[] = ['int', 'cha', 'str', 'tool'];

export const SKILL_LABELS: Record<SkillKey, string> = {
  int: 'Intelligence',
  cha: 'Charisma',
  str: 'Strength',
  tool: 'Tool Skill',
};

export const SKILL_SHORT: Record<SkillKey, string> = {
  int: 'INT',
  cha: 'CHA',
  str: 'STR',
  tool: 'TOOL',
};

/** Skills are capped at 999 in play; goal is 999 in all four. */
export const SKILL_MAX = 999;

/** Points available to distribute at character creation. */
export const CREATION_POINTS = 20;

/** Survival. */
export const BASE_HEALTH = 100;
/** Each point of Strength adds this much to max health. */
export const HEALTH_PER_STRENGTH = 0.5;
export const HUNGER_MAX = 100;
/** Hunger lost per in-game hour. */
export const HUNGER_DECAY_PER_GAME_HOUR = 4;
/** When hunger hits 0, health lost per in-game hour. */
export const STARVATION_HEALTH_PER_GAME_HOUR = 20;

/** Time: one real hour equals one full 24h game day. */
export const REAL_SECONDS_PER_GAME_DAY = 3600;
export const GAME_HOURS_PER_DAY = 24;
/** Game hours that elapse per real second. */
export const GAME_HOURS_PER_REAL_SECOND =
  GAME_HOURS_PER_DAY / REAL_SECONDS_PER_GAME_DAY;

/** Daytime window (businesses/jobs open). */
export const DAY_START_HOUR = 6;
export const NIGHT_START_HOUR = 20;

/** Starting money for a fresh hobo. */
export const STARTING_CASH = 0;

/** Movement. */
export const PLAYER_SPEED = 4.2; // metres / second
export const PLAYER_TURN_SPEED = 10; // rad / second toward heading

/** How close (metres) you must be to talk to / interact with something. */
export const INTERACT_RANGE = 3.0;

/** Local-storage keys. */
export const LS_ACCOUNT = 'hobolife.account.v1';
export const LS_SAVE = 'hobolife.save.v1';
