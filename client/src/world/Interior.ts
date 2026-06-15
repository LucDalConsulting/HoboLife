// Walk-in interiors. When you reach a building's door you step INSIDE a real
// room (built far from the city so the outside isn't visible): walls, a floor,
// lighting, a clerk behind a counter, and a glowing EXIT. Walk to the counter
// and press E to use the shop's services; walk to the EXIT (or press Esc) to leave.

import * as THREE from 'three';
import type { BuildingDef } from '../data/city';
import { concreteTexture } from '../engine/textures';
import { Character } from '../entities/Character';

export const INTERIOR_ORIGIN = { x: 0, z: 600 };
const W = 16;
const D = 14;
const H = 5;

export interface InteriorInstance {
  def: BuildingDef;
  group: THREE.Group;
  floor: THREE.Mesh;
  entrance: { x: number; z: number };
  exit: { x: number; z: number };
  counter: { x: number; z: number };
  bounds: { minX: number; maxX: number; minZ: number; maxZ: number };
  counterBox: { minX: number; maxX: number; minZ: number; maxZ: number };
  clerk: Character;
  lights: THREE.PointLight[];
}

const KIND_LABEL: Record<string, string> = {
  university: 'Lecture Hall — talk to the registrar',
  gym: 'Gym Floor — see the trainer',
  diner: 'Order at the counter',
  hospital: 'Reception — see the nurse',
  bank: 'Teller window',
  clothing: 'Fitting counter',
  casino: 'Cashier',
  cardealer: 'Sales desk',
  realtor: 'Front desk',
  pawn: 'Pawn counter',
  generic: 'Front desk',
};

export function buildInterior(def: BuildingDef): InteriorInstance {
  const o = INTERIOR_ORIGIN;
  const group = new THREE.Group();

  // Floor.
  const floorMat = new THREE.MeshStandardMaterial({ map: concreteTexture(3), color: 0xb9b2a4, roughness: 0.7 });
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(W, D), floorMat);
  floor.rotation.x = -Math.PI / 2;
  floor.position.set(o.x, 0.02, o.z);
  floor.receiveShadow = true;
  floor.userData.kind = 'ifloor';
  group.add(floor);

  // Ceiling.
  const ceil = new THREE.Mesh(new THREE.PlaneGeometry(W, D), new THREE.MeshStandardMaterial({ color: 0xe8e6e0, roughness: 1 }));
  ceil.rotation.x = Math.PI / 2;
  ceil.position.set(o.x, H, o.z);
  group.add(ceil);

  // Walls (accent-tinted).
  const wallMat = new THREE.MeshStandardMaterial({ color: tint(def.accent), roughness: 0.9 });
  const back = wall(W, H, 0.3); back.position.set(o.x, H / 2, o.z - D / 2); back.material = wallMat; group.add(back);
  const front = wall(W, H, 0.3); front.position.set(o.x, H / 2, o.z + D / 2); front.material = wallMat; group.add(front);
  const left = wall(0.3, H, D); left.position.set(o.x - W / 2, H / 2, o.z); left.material = wallMat; group.add(left);
  const right = wall(0.3, H, D); right.position.set(o.x + W / 2, H / 2, o.z); right.material = wallMat; group.add(right);

  // Counter near the back.
  const counterMat = new THREE.MeshStandardMaterial({ color: 0x6b4a2b, roughness: 0.6 });
  const counterTop = new THREE.Mesh(new THREE.BoxGeometry(8, 1.1, 1.4), counterMat);
  const counterZ = o.z - D / 2 + 2.6;
  counterTop.position.set(o.x, 0.55, counterZ);
  counterTop.castShadow = true; counterTop.receiveShadow = true;
  group.add(counterTop);

  // Clerk behind the counter, facing the entrance.
  const clerk = new Character({ skin: '#e8b98a', hair: '#2b2018', shirt: tintHex(def.accent), pants: '#2c3e50' });
  clerk.group.position.set(o.x, 0, counterZ - 1.1);
  clerk.group.rotation.y = Math.PI; // face +z (toward the door)
  clerk.group.add(Character.nameSprite('Clerk', '#ffe9a8'));
  group.add(clerk.group);

  // Shelves / decor along the side walls.
  const shelfMat = new THREE.MeshStandardMaterial({ color: 0x8a8f96, roughness: 0.7 });
  for (let i = -1; i <= 1; i++) {
    for (const sx of [-W / 2 + 0.8, W / 2 - 0.8]) {
      const shelf = new THREE.Mesh(new THREE.BoxGeometry(0.6, 2.4, 2.2), shelfMat);
      shelf.position.set(sx, 1.2, o.z + i * 3.2);
      shelf.castShadow = true;
      group.add(shelf);
    }
  }

  // Glowing EXIT by the front wall.
  const exitZ = o.z + D / 2 - 0.6;
  const exitMat = new THREE.MeshStandardMaterial({ color: 0x16331f, emissive: 0x35d07a, emissiveIntensity: 1.1 });
  const exitDoor = new THREE.Mesh(new THREE.BoxGeometry(2.2, 3, 0.2), exitMat);
  exitDoor.position.set(o.x, 1.5, exitZ);
  group.add(exitDoor);
  const exitSign = Character.nameSprite('EXIT', '#7dffb0');
  exitSign.position.set(o.x, 3.4, exitZ);
  exitSign.scale.set(2.4, 0.6, 1);
  group.add(exitSign);

  // Interior label.
  const label = Character.nameSprite(`${def.name}`, hex(def.accent));
  label.position.set(o.x, H - 0.7, o.z - D / 2 + 0.4);
  label.scale.set(7, 1.5, 1);
  group.add(label);
  const sub = Character.nameSprite(KIND_LABEL[def.kind] ?? 'Front desk', '#dfe6ee');
  sub.position.set(o.x, H - 1.5, o.z - D / 2 + 0.4);
  sub.scale.set(6, 1.1, 1);
  group.add(sub);

  // Warm interior lighting.
  const lights: THREE.PointLight[] = [];
  for (const lx of [-4, 4]) {
    const l = new THREE.PointLight(0xfff1d0, 12, 20, 2);
    l.position.set(o.x + lx, H - 0.6, o.z);
    group.add(l);
    lights.push(l);
  }

  return {
    def,
    group,
    floor,
    entrance: { x: o.x, z: o.z + D / 2 - 3 },
    exit: { x: o.x, z: exitZ - 0.2 },
    counter: { x: o.x, z: counterZ + 1.2 },
    bounds: { minX: o.x - W / 2 + 0.6, maxX: o.x + W / 2 - 0.6, minZ: o.z - D / 2 + 0.6, maxZ: o.z + D / 2 - 0.6 },
    counterBox: { minX: o.x - 4.3, maxX: o.x + 4.3, minZ: counterZ - 0.9, maxZ: counterZ + 0.9 },
    clerk,
    lights,
  };
}

/** Clamp a position to the room and keep it out of the counter. */
export function collideInterior(inst: InteriorInstance, x: number, z: number): { x: number; z: number } {
  const b = inst.bounds;
  let nx = Math.min(b.maxX, Math.max(b.minX, x));
  let nz = Math.min(b.maxZ, Math.max(b.minZ, z));
  const c = inst.counterBox;
  if (nx > c.minX && nx < c.maxX && nz > c.minZ && nz < c.maxZ) {
    // Push out the nearest counter edge (player approaches from +z).
    const df = c.maxZ - nz;
    const dn = nz - c.minZ;
    const dl = nx - c.minX;
    const dr = c.maxX - nx;
    const m = Math.min(df, dn, dl, dr);
    if (m === df) nz = c.maxZ; else if (m === dn) nz = c.minZ; else if (m === dl) nx = c.minX; else nx = c.maxX;
  }
  return { x: nx, z: nz };
}

function wall(w: number, h: number, d: number): THREE.Mesh {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), new THREE.MeshStandardMaterial({ color: 0x888888 }));
  m.receiveShadow = true;
  return m;
}

function hex(n: number): string {
  return '#' + n.toString(16).padStart(6, '0');
}
function tintHex(n: number): string {
  return hex(n);
}
function tint(n: number): number {
  // Soften the accent for walls.
  const r = ((n >> 16) & 0xff) * 0.5 + 90;
  const g = ((n >> 8) & 0xff) * 0.5 + 90;
  const b = (n & 0xff) * 0.5 + 90;
  return (Math.min(255, r) << 16) | (Math.min(255, g) << 8) | Math.min(255, b);
}
