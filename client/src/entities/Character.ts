// A stylized low-poly humanoid built from rounded primitives (capsule limbs,
// hands, shoes, and a proper face) with PBR materials and a walk animation.
// Shared by the player, NPCs and remote players. The whole figure is one group,
// so it (head included) rotates as a unit when the body turns.

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
    this.skinMat = mat(app.skin, 0.65);
    this.hairMat = mat(app.hair, 0.8);
    this.shirtMat = mat(app.shirt, 0.9);
    this.pantsMat = mat(app.pants, 0.9);
    const shoeMat = mat(0x222428, 0.6);
    const darkMat = mat(0x16171b, 0.5);
    const whiteMat = mat(0xf3f1ec, 0.5);

    // Legs (capsule + shoe), pivoting at the hip.
    this.buildLeg(this.leftLeg, -0.14, shoeMat);
    this.buildLeg(this.rightLeg, 0.14, shoeMat);
    this.group.add(this.leftLeg, this.rightLeg);

    // Everything above the hips bobs/leans together while walking.
    this.group.add(this.upper);

    // Hips / shorts.
    const hips = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.26, 0.32), this.pantsMat);
    hips.position.y = 0.95; hips.castShadow = true; this.upper.add(hips);

    // Torso.
    const torso = new THREE.Mesh(new THREE.CapsuleGeometry(0.27, 0.46, 6, 16), this.shirtMat);
    torso.scale.set(1.04, 1.0, 0.72);
    torso.position.y = 1.22; torso.castShadow = true; this.upper.add(torso);

    // Arms (capsule + hand), pivoting at the shoulder.
    this.buildArm(this.leftArm, -0.37);
    this.buildArm(this.rightArm, 0.37);
    this.upper.add(this.leftArm, this.rightArm);

    // Neck + head, seated clearly above the torso.
    const neck = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.12, 0.14, 10), this.skinMat);
    neck.position.y = 1.66; this.upper.add(neck);

    const head = new THREE.Mesh(new THREE.SphereGeometry(0.235, 22, 18), this.skinMat);
    head.position.y = 1.95; head.scale.set(0.98, 1.06, 0.98); head.castShadow = true; this.upper.add(head);

    // Hair cap over the top/back (leaves the face clear).
    const hair = new THREE.Mesh(
      new THREE.SphereGeometry(0.25, 22, 18, 0, Math.PI * 2, 0, Math.PI * 0.58),
      this.hairMat,
    );
    hair.position.set(0, 1.96, -0.03); hair.scale.set(1, 1.05, 1.04); this.upper.add(hair);

    // --- Face (faces +z) ---
    for (const ex of [-0.09, 0.09]) {
      const eyeWhite = new THREE.Mesh(new THREE.SphereGeometry(0.052, 12, 10), whiteMat);
      eyeWhite.position.set(ex, 1.97, 0.18); eyeWhite.scale.set(1, 1.15, 0.7); this.upper.add(eyeWhite);
      const pupil = new THREE.Mesh(new THREE.SphereGeometry(0.026, 10, 8), darkMat);
      pupil.position.set(ex, 1.97, 0.215); this.upper.add(pupil);
      const brow = new THREE.Mesh(new THREE.BoxGeometry(0.1, 0.022, 0.04), this.hairMat);
      brow.position.set(ex, 2.05, 0.205); this.upper.add(brow);
    }
    const nose = new THREE.Mesh(new THREE.ConeGeometry(0.045, 0.12, 8), this.skinMat);
    nose.rotation.x = Math.PI / 2; nose.position.set(0, 1.92, 0.23); this.upper.add(nose);
    const mouth = new THREE.Mesh(new THREE.BoxGeometry(0.11, 0.022, 0.03), darkMat);
    mouth.position.set(0, 1.85, 0.215); this.upper.add(mouth);
  }

  private buildLeg(pivot: THREE.Group, x: number, shoeMat: THREE.MeshStandardMaterial): void {
    pivot.position.set(x, 0.88, 0);
    const leg = new THREE.Mesh(new THREE.CapsuleGeometry(0.13, 0.5, 5, 12), this.pantsMat);
    leg.position.y = -0.4; leg.castShadow = true; pivot.add(leg);
    const shoe = new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.13, 0.4), shoeMat);
    shoe.position.set(0, -0.82, 0.08); shoe.castShadow = true; pivot.add(shoe);
  }

  private buildArm(pivot: THREE.Group, x: number): void {
    pivot.position.set(x, 1.5, 0);
    const arm = new THREE.Mesh(new THREE.CapsuleGeometry(0.1, 0.44, 5, 12), this.shirtMat);
    arm.position.y = -0.32; arm.castShadow = true; pivot.add(arm);
    const hand = new THREE.Mesh(new THREE.SphereGeometry(0.11, 12, 10), this.skinMat);
    hand.position.y = -0.6; hand.castShadow = true; pivot.add(hand);
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
    this.upper.position.y = moving ? Math.abs(Math.sin(this.phase)) * 0.04 : 0;
    this.upper.rotation.x = moving ? 0.05 : 0;
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
    sprite.position.y = 2.4;
    sprite.renderOrder = 999;
    return sprite;
  }
}
