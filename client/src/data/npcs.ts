// NPC roster generator for v0.1. A handful of wandering pedestrians plus a few
// hostile thugs scattered around the city. Expanded massively in later versions.

import type { Appearance } from '../core/types';
import { pick, randFloat, randInt } from '../core/rng';

export interface NPCSpec {
  id: string;
  name: string;
  appearance: Appearance;
  hostile: boolean;
  /** Which dialogue tree to open on Q. */
  tree: 'pedestrian' | 'thug';
  /** Combat profile if you fight them. */
  combat: { str: number; tool: number; maxHp: number; weapon?: string };
  /** Wander home and radius. */
  home: { x: number; z: number };
  wander: number;
}

const FIRST = [
  'Marcus', 'Dana', 'Trish', 'Hank', 'Lola', 'Vince', 'Rosa', 'Kyle',
  'Mona', 'Dwayne', 'Priya', 'Chad', 'Gwen', 'Omar', 'Sasha', 'Reggie',
];
const LAST = [
  'Diaz', 'Park', 'Boyle', 'Nguyen', 'Cole', 'Russo', 'Kemp', 'Frost',
  'Webb', 'Okafor', 'Mendez', 'Yates',
];

const SKINS = ['#f1c8a0', '#e8b98a', '#d49a6a', '#a9744f', '#7a4f33'];
const HAIRS = ['#1c1410', '#3b2412', '#6b4423', '#b07b3a', '#cccccc', '#222222'];
const SHIRTS = ['#3a6ea5', '#c0392b', '#27ae60', '#8e44ad', '#e67e22', '#16a085', '#555f6b'];
const PANTS = ['#2c3e50', '#34495e', '#4a3b2a', '#1f2a36', '#3d3d3d'];

function randomAppearance(): Appearance {
  return {
    skin: pick(SKINS),
    hair: pick(HAIRS),
    shirt: pick(SHIRTS),
    pants: pick(PANTS),
  };
}

/** Random point on the walkable street ring (avoids building footprints roughly). */
function streetPoint(): { x: number; z: number } {
  // Spawn along the central avenues / plaza band so NPCs read as "on the street".
  const onVertical = Math.random() < 0.5;
  if (onVertical) {
    return { x: randFloat(-6, 6), z: randFloat(-48, 48) };
  }
  return { x: randFloat(-48, 48), z: randFloat(-6, 6) };
}

/** Build the starting NPC population. */
export function makeCityNPCs(): NPCSpec[] {
  const npcs: NPCSpec[] = [];

  // 10 ordinary pedestrians.
  for (let i = 0; i < 10; i++) {
    const home = streetPoint();
    npcs.push({
      id: `ped_${i}`,
      name: `${pick(FIRST)} ${pick(LAST)}`,
      appearance: randomAppearance(),
      hostile: false,
      tree: 'pedestrian',
      combat: { str: randInt(5, 40), tool: randInt(0, 15), maxHp: randInt(60, 110) },
      home,
      wander: randFloat(6, 14),
    });
  }

  // 3 hostile thugs, slightly tougher, some armed.
  for (let i = 0; i < 3; i++) {
    const home = streetPoint();
    npcs.push({
      id: `thug_${i}`,
      name: `${pick(['Spike', 'Razor', 'Bruno', 'Knuckles', 'Tank'])}`,
      appearance: { ...randomAppearance(), shirt: '#2b2d42', pants: '#1a1a1a' },
      hostile: true,
      tree: 'thug',
      combat: {
        str: randInt(40, 90),
        tool: randInt(10, 40),
        maxHp: randInt(90, 140),
        weapon: i === 0 ? 'knife' : undefined,
      },
      home,
      wander: randFloat(5, 10),
    });
  }

  return npcs;
}
