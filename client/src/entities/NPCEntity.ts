// A wandering NPC: a Character that meanders around its home point, pausing
// occasionally, with a floating name label (red for hostiles).

import * as THREE from 'three';
import { randFloat } from '../core/rng';
import type { NPCSpec } from '../data/npcs';
import { turnToward } from './Player';
import { Character } from './Character';

type CollideFn = (x: number, z: number, fromX: number, fromZ: number) => { x: number; z: number };

const NPC_SPEED = 1.6;

export class NPCEntity {
  spec: NPCSpec;
  character: Character;
  group: THREE.Group;
  position = new THREE.Vector3();
  private heading = 0;
  private target = new THREE.Vector3();
  private pause = 0;

  constructor(spec: NPCSpec) {
    this.spec = spec;
    this.character = new Character(spec.appearance);
    this.group = this.character.group;
    this.position.set(spec.home.x, 0, spec.home.z);
    this.group.position.copy(this.position);
    this.group.add(Character.nameSprite(spec.name, spec.hostile ? '#ff6b6b' : '#ffffff'));
    this.pickTarget();
  }

  private pickTarget(): void {
    const a = Math.random() * Math.PI * 2;
    const r = randFloat(0, this.spec.wander);
    this.target.set(this.spec.home.x + Math.cos(a) * r, 0, this.spec.home.z + Math.sin(a) * r);
  }

  update(dt: number, collide: CollideFn): void {
    if (this.pause > 0) {
      this.pause -= dt;
      this.character.update(dt, 0);
      return;
    }

    let dx = this.target.x - this.position.x;
    let dz = this.target.z - this.position.z;
    const dist = Math.hypot(dx, dz);
    if (dist < 0.3) {
      this.pause = randFloat(1, 4);
      this.pickTarget();
      this.character.update(dt, 0);
      return;
    }

    dx /= dist;
    dz /= dist;
    const step = NPC_SPEED * dt;
    const before = { x: this.position.x, z: this.position.z };
    const resolved = collide(this.position.x + dx * step, this.position.z + dz * step, before.x, before.z);
    const moved = Math.hypot(resolved.x - before.x, resolved.z - before.z);
    this.position.set(resolved.x, 0, resolved.z);
    if (moved < step * 0.2) {
      // Stuck against a wall — pick somewhere new.
      this.pause = randFloat(0.5, 1.5);
      this.pickTarget();
    }

    this.heading = turnToward(this.heading, Math.atan2(dx, dz), 6 * dt);
    this.group.position.set(this.position.x, 0, this.position.z);
    this.group.rotation.y = this.heading;
    this.character.update(dt, NPC_SPEED);
  }
}
