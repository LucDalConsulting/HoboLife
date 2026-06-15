// RuneScape-style third-person orbit camera: right-drag rotates, wheel zooms,
// the camera always frames the player. Exposes the on-ground forward direction
// so WASD movement is relative to where the camera looks.

import * as THREE from 'three';
import type { Input } from './Input';

export class FollowCamera {
  yaw = Math.PI; // looking toward -z initially (behind a +z-facing world)
  pitch = 0.5;
  distance = 13;

  private readonly minPitch = 0.12;
  private readonly maxPitch = 1.35;
  private readonly minDist = 5;
  private readonly maxDist = 30;

  readonly target = new THREE.Vector3();
  /** Normalised XZ direction the camera is looking (player "forward"). */
  readonly forward = new THREE.Vector3(0, 0, -1);
  readonly right = new THREE.Vector3(1, 0, 0);

  update(input: Input, camera: THREE.PerspectiveCamera): void {
    this.yaw -= input.orbitDX * 0.005;
    this.pitch += input.orbitDY * 0.005;
    this.pitch = Math.min(this.maxPitch, Math.max(this.minPitch, this.pitch));
    this.distance += input.zoomDelta * 0.01;
    this.distance = Math.min(this.maxDist, Math.max(this.minDist, this.distance));
    input.orbitDX = 0;
    input.orbitDY = 0;
    input.zoomDelta = 0;

    const cosP = Math.cos(this.pitch);
    const ox = this.distance * cosP * Math.sin(this.yaw);
    const oy = this.distance * Math.sin(this.pitch);
    const oz = this.distance * cosP * Math.cos(this.yaw);

    const headY = 1.4;
    camera.position.set(this.target.x + ox, this.target.y + oy + headY, this.target.z + oz);
    camera.lookAt(this.target.x, this.target.y + headY, this.target.z);

    // Forward = from camera toward target, flattened onto the ground plane.
    this.forward.set(-ox, 0, -oz).normalize();
    this.right.set(this.forward.z, 0, -this.forward.x); // 90° clockwise
  }
}
