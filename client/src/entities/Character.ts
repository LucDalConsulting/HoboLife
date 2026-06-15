// A simple primitive-built humanoid with a walk animation — deliberately
// RuneScape-simple. Shared by the player, NPCs, and remote players.

import * as THREE from 'three';
import type { Appearance } from '../core/types';

function mat(color: string | number): THREE.MeshStandardMaterial {
  return new THREE.MeshStandardMaterial({ color, roughness: 0.85, metalness: 0.0 });
}

export class Character {
  group = new THREE.Group();
  private leftLeg = new THREE.Group();
  private rightLeg = new THREE.Group();
  private leftArm = new THREE.Group();
  private rightArm = new THREE.Group();
  private phase = 0;

  private skinMat: THREE.MeshStandardMaterial;
  private hairMat: THREE.MeshStandardMaterial;
  private shirtMat: THREE.MeshStandardMaterial;
  private pantsMat: THREE.MeshStandardMaterial;

  constructor(app: Appearance) {
    this.skinMat = mat(app.skin);
    this.hairMat = mat(app.hair);
    this.shirtMat = mat(app.shirt);
    this.pantsMat = mat(app.pants);

    // Legs (pivot at the hip so they swing).
    this.buildLimb(this.leftLeg, -0.13, 0.72, 0.2, 0.7, 0.24, this.pantsMat);
    this.buildLimb(this.rightLeg, 0.13, 0.72, 0.2, 0.7, 0.24, this.pantsMat);

    // Torso.
    const torso = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.72, 0.3), this.shirtMat);
    torso.position.y = 1.08;
    torso.castShadow = true;
    this.group.add(torso);

    // Arms (pivot at the shoulder).
    this.buildLimb(this.leftArm, -0.34, 1.36, 0.15, 0.62, 0.18, this.shirtMat);
    this.buildLimb(this.rightArm, 0.34, 1.36, 0.15, 0.62, 0.18, this.shirtMat);

    // Head + hair.
    const head = new THREE.Mesh(new THREE.SphereGeometry(0.22, 16, 12), this.skinMat);
    head.position.y = 1.66;
    head.castShadow = true;
    this.group.add(head);

    const hair = new THREE.Mesh(new THREE.SphereGeometry(0.235, 16, 12, 0, Math.PI * 2, 0, Math.PI / 2), this.hairMat);
    hair.position.y = 1.66;
    this.group.add(hair);
  }

  private buildLimb(
    pivot: THREE.Group,
    x: number,
    y: number,
    w: number,
    h: number,
    d: number,
    material: THREE.MeshStandardMaterial,
  ): void {
    pivot.position.set(x, y, 0);
    const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material);
    mesh.position.y = -h / 2;
    mesh.castShadow = true;
    pivot.add(mesh);
    this.group.add(pivot);
  }

  setAppearance(app: Appearance): void {
    this.skinMat.color.set(app.skin);
    this.hairMat.color.set(app.hair);
    this.shirtMat.color.set(app.shirt);
    this.pantsMat.color.set(app.pants);
  }

  /** speed in units/sec drives the gait; 0 = idle bob. */
  update(dt: number, speed: number): void {
    const moving = speed > 0.05;
    this.phase += dt * (moving ? 2.2 + speed * 1.5 : 1.5);
    const amp = moving ? Math.min(0.7, 0.25 + speed * 0.1) : 0.04;
    const s = Math.sin(this.phase) * amp;
    this.leftLeg.rotation.x = s;
    this.rightLeg.rotation.x = -s;
    this.leftArm.rotation.x = -s;
    this.rightArm.rotation.x = s;
  }

  /** A floating name/health label that always faces the camera. */
  static nameSprite(text: string, color = '#ffffff'): THREE.Sprite {
    const canvas = document.createElement('canvas');
    canvas.width = 256;
    canvas.height = 64;
    const ctx = canvas.getContext('2d')!;
    ctx.font = 'bold 30px Arial';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.lineWidth = 6;
    ctx.strokeStyle = 'rgba(0,0,0,0.85)';
    ctx.strokeText(text, 128, 32);
    ctx.fillStyle = color;
    ctx.fillText(text, 128, 32);
    const tex = new THREE.CanvasTexture(canvas);
    tex.anisotropy = 4;
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthTest: false, transparent: true }));
    sprite.scale.set(2.0, 0.5, 1);
    sprite.position.y = 2.1;
    sprite.renderOrder = 999;
    return sprite;
  }
}
