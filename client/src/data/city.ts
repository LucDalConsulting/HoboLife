// The LA city block layout for v0.1. Buildings are box footprints that block
// movement; some expose an interaction when you stand near their door and press E.

export type BuildingKind =
  | 'university'
  | 'gym'
  | 'diner'
  | 'hospital'
  | 'bank'
  | 'clothing'
  | 'cardealer'
  | 'casino'
  | 'realtor'
  | 'pawn'
  | 'generic';

export interface BuildingDef {
  id: string;
  name: string;
  kind: BuildingKind;
  /** Centre position. */
  x: number;
  z: number;
  /** Footprint width (x) and depth (z), and height (y). */
  w: number;
  d: number;
  h: number;
  /** Wall colour. */
  color: number;
  /** Sign / accent colour. */
  accent: number;
}

/** Half-size of the square world; movement is clamped to ±WORLD_HALF. */
export const WORLD_HALF = 60;

/** Where a fresh character spawns (central plaza). */
export const SPAWN = { x: 0, z: 14 };

export const BUILDINGS: BuildingDef[] = [
  // --- NW quadrant ---
  { id: 'uni', name: 'LA City University', kind: 'university', x: -34, z: -32, w: 22, d: 16, h: 17, color: 0x9c6b3f, accent: 0xf4d35e },
  { id: 'gym', name: "Iron Paradise Gym", kind: 'gym', x: -12, z: -36, w: 14, d: 11, h: 9, color: 0x444b54, accent: 0xe5533b },

  // --- NE quadrant ---
  { id: 'bank', name: 'First National Bank', kind: 'bank', x: 34, z: -32, w: 20, d: 15, h: 19, color: 0x5b6770, accent: 0x4caf7d },
  { id: 'hospital', name: 'Mercy Hospital', kind: 'hospital', x: 12, z: -36, w: 16, d: 11, h: 12, color: 0xe7ecef, accent: 0xe5533b },

  // --- SW quadrant ---
  { id: 'diner', name: "Greasy Spoon Diner", kind: 'diner', x: -34, z: 32, w: 16, d: 13, h: 8, color: 0xc24b3a, accent: 0xf4d35e },
  { id: 'clothing', name: 'Threadbare Clothing', kind: 'clothing', x: -12, z: 36, w: 12, d: 11, h: 8, color: 0x6a4c93, accent: 0xf2c4de },

  // --- SE quadrant ---
  { id: 'casino', name: 'Lucky Stick Casino', kind: 'casino', x: 34, z: 32, w: 22, d: 17, h: 15, color: 0x2b2d42, accent: 0xf4d35e },
  { id: 'cardealer', name: 'Honest Hal Autos', kind: 'cardealer', x: 12, z: 36, w: 14, d: 11, h: 8, color: 0x3d5a80, accent: 0xee6c4d },

  // --- flanks ---
  { id: 'realtor', name: 'Skyline Realty', kind: 'realtor', x: -50, z: 4, w: 12, d: 14, h: 11, color: 0x577590, accent: 0xf4d35e },
  { id: 'pawn', name: 'Quick Cash Pawn', kind: 'pawn', x: 50, z: -4, w: 12, d: 14, h: 10, color: 0x7d5a3c, accent: 0xffd23f },
];

export function buildingById(id: string): BuildingDef | undefined {
  return BUILDINGS.find((b) => b.id === id);
}
