// Item registry for v0.1. Add to this as the world grows.

import type { ItemDef } from '../core/types';

export const ITEMS: Record<string, ItemDef> = {
  cardboard_sign: {
    id: 'cardboard_sign',
    name: 'Cardboard Sign',
    icon: '🪧',
    kind: 'misc',
    desc: '"Anything helps. God bless." Doubles your panhandling charm.',
  },
  knife: {
    id: 'knife',
    name: 'Pocket Knife',
    icon: '🔪',
    kind: 'weapon',
    weapon: { base: 14, usesTool: true, moves: ['stab', 'slash'] },
    desc: 'Sharp and quiet. Rewards Tool skill in a fight.',
  },
  bat: {
    id: 'bat',
    name: 'Baseball Bat',
    icon: '🏏',
    kind: 'weapon',
    twoHanded: true,
    weapon: { base: 12, usesTool: false, moves: ['swing', 'jab'] },
    desc: 'Pure strength. Needs both hands.',
  },
  pistol: {
    id: 'pistol',
    name: 'Pistol',
    icon: '🔫',
    kind: 'weapon',
    weapon: { base: 26, usesTool: true, moves: ['shoot', 'pistol-whip'] },
    desc: 'Hits hard if your Tool skill can keep it steady.',
  },
  smoothie: {
    id: 'smoothie',
    name: 'Fruit Smoothie',
    icon: '🥤',
    kind: 'food',
    food: { hunger: 20, health: 5 },
    desc: 'Tasty. +20 hunger, +5 health.',
  },
  burger: {
    id: 'burger',
    name: 'Cheeseburger',
    icon: '🍔',
    kind: 'food',
    food: { hunger: 35 },
    desc: 'A whole meal. +35 hunger.',
  },
  sandwich: {
    id: 'sandwich',
    name: 'Gas-Station Sandwich',
    icon: '🥪',
    kind: 'food',
    food: { hunger: 18 },
    desc: 'Questionable, but food is food. +18 hunger.',
  },
};

export function getItem(id: string): ItemDef {
  const def = ITEMS[id];
  if (!def) throw new Error(`Unknown item id: ${id}`);
  return def;
}

export function maybeItem(id: string | undefined | null): ItemDef | null {
  if (!id) return null;
  return ITEMS[id] ?? null;
}
