// A stylized low-poly humanoid built from rounded primitives (capsule limbs,
// hands, shoes, a simple face) with PBR materials and a walk animation. Shared
// by the player, NPCs and remote players. Deliberately game-stylized so it reads
// well under the cinematic lighting without needing external model downloads.

import * as THREE from 'three';
import type { Appearance } from '../core/types';

function mat(color: string | number, roughness = 0.85): THREE.MeshStandardMaterial {
  return new THREE.MeshStandardMaterial({ color, roughness, metalness: 0.0 });
}

export class Character {
  group = new THREE.Group();
  private leftLeg = new THREE.Group();
  private rightLeg = new THREE.Group();
  private leftArm = new THREE.Group();
  private rightArm = new THREE.Group();
  private upper = new THREE.Group();
  private phase = 0;

  private skinMat: THREE.MeshStandardMaterial;
  private hairMat: THREE.MeshStandardMaterial;
  private shirtMat: THREE.MeshStandardMaterial;
  private pantsMat: THREE.MeshStandardMaterial;

  constructor(app: Appearance) {
    this.skinMat = mat(app.skin, 0.7);
    this.hairMat = mat(app.hair, 0.8);
    this.shirtMat = mat(app.shirt, 0.9);
    this.pantsMat = mat(app.pants, 0.9);
    const shoeMat = mat(0x222428, 0.6);
    const eyeMat = mat(0x15161a, 0.4);

    // Legs (capsule + shoe), pivoting at the hip.
    this.buildLeg(this.leftLeg, -0.14, shoeMat);
    this.buildLeg(this.rightLeg, 0.14, shoeMat);
    this.group.add(this.leftLeg, this.rightLeg);

    // Everything above the hips bobs together a little while walking.
    this.upper.position.y = 0;
    this.group.add(this.upper);

    // Hips / shorts.
    const hips = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.26, 0.32), this.pantsMat);
    hips.position.y = 0.92; hips.castShadow = true; this.upper.add(hips);

    // Torso.
    const torso = new THREE.Mesh(new THREE.CapsuleGeometry(0.27, 0.42, 6, 14), this.shirtMat);
    torso.scale.set(1.0, 1.0, 0.74);
    torso.position.y = 1.2; torso.castShadow = true; this.upper.add(torso);

    // Arms (capsule + hand), pivoting at the shoulder.
    this.buildArm(this.leftArm, -0.36);
    this.buildArm(this.rightArm, 0.36);
    this.upper.add(this.leftArm, this.rightArm);

    // Neck + head.
    const neck = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.12, 0.12, 8), this.skinMat);
    neck.position.y = 1.5; this.upper.add(neck);
    const head = new THREE.Mesh(new THREE.SphereGeometry(0.22, 18, 14), this.skinMat);
    head.position.y = 1.68; head.scale.set(1, 1.08, 0.96); head.castShadow = true; this.upper.add(head);

    // Hair cap.
    const hair = new THREE.Mesh(new THREE.SphereGeometry(0.235, 18, 14, 0, Math.PI * 2, 0, Math.PI * 0.62), this.hairMat);
    hair.position.set(0, 1.7, -0.02); this.upper.add(hair);

    // Eyes.
    for (const ex of [-0.08, 0.08]) {
      const eye = new THREE.Mesh(new THREE.SphereGeometry(0.032, 8, 8), eyeMat);
      eye.position.set(ex, 1.69, 0.2); this.upper.add(eye);
    }
  }

  private buildLeg(pivot: THREE.Group, x: number, shoeMat: THREE.MeshStandardMaterial): void {
    pivot.position.set(x, 0.86, 0);
    const leg = new THREE.Mesh(new THREE.CapsuleGeometry(0.13, 0.5, 5, 10), this.pantsMat);
    leg.position.y = -0.38; leg.castShadow = true; pivot.add(leg);
    const shoe = new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.13, 0.38), shoeMat);
    shoe.position.set(0, -0.78, 0.07); shoe.castShadow = true; pivot.add(shoe);
  }

  private buildArm(pivot: THREE.Group, x: number): void {
    pivot.position.set(x, 1.4, 0);
    const arm = new THREE.Mesh(new THREE.CapsuleGeometry(0.1, 0.42, 5, 10), this.shirtMat);
    arm.position.y = -0.3; arm.castShadow = true; pivot.add(arm);
    const hand = new THREE.Mesh(new THREE.SphereGeometry(0.11, 10, 8), this.skinMat);
    hand.position.y = -0.56; hand.castShadow = true; pivot.add(hand);
  }

  setAppearance(app: Appearance): void {
    this.skinMat.color.set(app.skin);
    this.hairMat.color.set(app.hair);
    this.shirtMat.color.set(app.shirt);
    this.pantsMat.color.set(app.pants);
  }

  /** speed in units/sec drives the gait; 0 = gentle idle. */
  update(dt: number, speed: number): void {
    const moving = speed > 0.05;
    this.phase += dt * (moving ? 2.4 + speed * 1.4 : 1.6);
    const amp = moving ? Math.min(0.75, 0.3 + speed * 0.1) : 0.05;
    const s = Math.sin(this.phase) * amp;
    this.leftLeg.rotation.x = s;
    this.rightLeg.rotation.x = -s;
    this.leftArm.rotation.x = -s * 0.9;
    this.rightArm.rotation.x = s * 0.9;
    // Subtle bounce + lean while moving.
    this.upper.position.y = moving ? Math.abs(Math.sin(this.phase)) * 0.04 : 0;
    this.upper.rotation.x = moving ? 0.06 : 0;
  }

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
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = 4;
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthTest: false, transparent: true }));
    sprite.scale.set(2.0, 0.5, 1);
    sprite.position.y = 2.15;
    sprite.renderOrder = 999;
    return sprite;
  }
}
