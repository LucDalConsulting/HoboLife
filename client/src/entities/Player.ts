// The local player: a Character plus movement (WASD relative to camera, or
// click-to-move) with collision resolution supplied by the world.

import * as THREE from 'three';
import { PLAYER_SPEED, PLAYER_TURN_SPEED } from '../core/constants';
import type { Appearance } from '../core/types';
import type { FollowCamera } from '../engine/FollowCamera';
import type { Input } from '../engine/Input';
import { Character } from './Character';

type CollideFn = (x: number, z: number, fromX: number, fromZ: number) => { x: number; z: number };

export class Player {
  character: Character;
  group: THREE.Group;
  position = new THREE.Vector3();
  heading = 0;
  /** Current ground speed (units/sec) for animation and footsteps. */
  speed = 0;
  moveTarget: THREE.Vector3 | null = null;

  constructor(app: Appearance, start: { x: number; z: number }) {
    this.character = new Character(app);
    this.group = this.character.group;
    this.position.set(start.x, 0, start.z);
    this.group.position.copy(this.position);
  }

  setAppearance(app: Appearance): void {
    this.character.setAppearance(app);
  }

  setMoveTarget(x: number, z: number): void {
    this.moveTarget = new THREE.Vector3(x, 0, z);
  }

  update(dt: number, input: Input, cam: FollowCamera, collide: CollideFn): void {
    let dx = 0;
    let dz = 0;
    const usingKeys =
      input.isDown('KeyW') || input.isDown('KeyA') || input.isDown('KeyS') || input.isDown('KeyD') ||
      input.isDown('ArrowUp') || input.isDown('ArrowDown') || input.isDown('ArrowLeft') || input.isDown('ArrowRight');

    if (usingKeys) {
      this.moveTarget = null;
      if (input.isDown('KeyW') || input.isDown('ArrowUp')) { dx += cam.forward.x; dz += cam.forward.z; }
      if (input.isDown('KeyS') || input.isDown('ArrowDown')) { dx -= cam.forward.x; dz -= cam.forward.z; }
      if (input.isDown('KeyD') || input.isDown('ArrowRight')) { dx += cam.right.x; dz += cam.right.z; }
      if (input.isDown('KeyA') || input.isDown('ArrowLeft')) { dx -= cam.right.x; dz -= cam.right.z; }
    } else if (this.moveTarget) {
      dx = this.moveTarget.x - this.position.x;
      dz = this.moveTarget.z - this.position.z;
      if (Math.hypot(dx, dz) < 0.2) {
        this.moveTarget = null;
        dx = dz = 0;
      }
    }

    const len = Math.hypot(dx, dz);
    if (len > 0.0001) {
      dx /= len;
      dz /= len;
      const step = PLAYER_SPEED * dt;
      const before = { x: this.position.x, z: this.position.z };
      const resolved = collide(this.position.x + dx * step, this.position.z + dz * step, before.x, before.z);
      const moved = Math.hypot(resolved.x - before.x, resolved.z - before.z);
      this.position.x = resolved.x;
      this.position.z = resolved.z;
      this.speed = moved / dt;

      // Smoothly turn to face the movement direction.
      const targetHeading = Math.atan2(dx, dz);
      this.heading = turnToward(this.heading, targetHeading, PLAYER_TURN_SPEED * dt);
    } else {
      this.speed = 0;
    }

    this.group.position.set(this.position.x, 0, this.position.z);
    this.group.rotation.y = this.heading;
    this.character.update(dt, this.speed);
  }
}

/** Rotate `current` toward `target` by at most `maxStep`, wrapping at ±π. */
export function turnToward(current: number, target: number, maxStep: number): number {
  let delta = target - current;
  while (delta > Math.PI) delta -= Math.PI * 2;
  while (delta < -Math.PI) delta += Math.PI * 2;
  if (Math.abs(delta) <= maxStep) return target;
  return current + Math.sign(delta) * maxStep;
}
