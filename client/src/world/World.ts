// Builds the LA city block: ground, asphalt roads, buildings (with doors you can
// walk up to), trees and street lamps. Also provides movement collision and the
// list of pickable meshes used for click-to-move / click-to-interact.

import * as THREE from 'three';
import { clamp, pick, randFloat } from '../core/rng';
import { BUILDINGS, SPAWN, WORLD_HALF, type BuildingDef } from '../data/city';

export interface BuildingInstance {
  def: BuildingDef;
  mesh: THREE.Mesh;
  /** Where the player should stand to use the door. */
  door: { x: number; z: number };
}

/** Radius used to keep the player/NPCs out of building footprints. */
const BODY_R = 0.5;

export class World {
  root = new THREE.Group();
  groundPlane: THREE.Mesh;
  buildings: BuildingInstance[] = [];
  private lamps: THREE.PointLight[] = [];

  constructor() {
    this.buildGround();
    this.buildRoads();
    this.groundPlane = this.buildPickGround();
    this.root.add(this.groundPlane);
    for (const def of BUILDINGS) this.buildBuilding(def);
    this.scatterProps();
  }

  private buildGround(): void {
    const geo = new THREE.PlaneGeometry(WORLD_HALF * 2, WORLD_HALF * 2);
    const mat = new THREE.MeshStandardMaterial({ color: 0x9a9384, roughness: 1 });
    const ground = new THREE.Mesh(geo, mat);
    ground.rotation.x = -Math.PI / 2;
    ground.receiveShadow = true;
    this.root.add(ground);
  }

  /** A transparent plane used purely as a raycast target for click-to-move. */
  private buildPickGround(): THREE.Mesh {
    const geo = new THREE.PlaneGeometry(WORLD_HALF * 2, WORLD_HALF * 2);
    const mat = new THREE.MeshBasicMaterial({ visible: false });
    const m = new THREE.Mesh(geo, mat);
    m.rotation.x = -Math.PI / 2;
    m.position.y = 0.01;
    m.userData.kind = 'ground';
    return m;
  }

  private buildRoads(): void {
    const mat = new THREE.MeshStandardMaterial({ color: 0x33373d, roughness: 1 });
    const ew = new THREE.Mesh(new THREE.PlaneGeometry(WORLD_HALF * 2, 10), mat);
    ew.rotation.x = -Math.PI / 2;
    ew.position.y = 0.02;
    this.root.add(ew);
    const ns = new THREE.Mesh(new THREE.PlaneGeometry(10, WORLD_HALF * 2), mat);
    ns.rotation.x = -Math.PI / 2;
    ns.position.y = 0.02;
    this.root.add(ns);

    // Lane markings.
    const lineMat = new THREE.MeshStandardMaterial({ color: 0xf4d35e, roughness: 1 });
    for (let i = -WORLD_HALF + 4; i < WORLD_HALF; i += 8) {
      const a = new THREE.Mesh(new THREE.PlaneGeometry(3, 0.4), lineMat);
      a.rotation.x = -Math.PI / 2;
      a.position.set(i, 0.03, 0);
      this.root.add(a);
      const b = new THREE.Mesh(new THREE.PlaneGeometry(0.4, 3), lineMat);
      b.rotation.x = -Math.PI / 2;
      b.position.set(0, 0.03, i);
      this.root.add(b);
    }
  }

  private buildBuilding(def: BuildingDef): void {
    const group = new THREE.Group();

    const mat = new THREE.MeshStandardMaterial({ color: def.color, roughness: 0.9 });
    const box = new THREE.Mesh(new THREE.BoxGeometry(def.w, def.h, def.d), mat);
    box.position.set(def.x, def.h / 2, def.z);
    box.castShadow = true;
    box.receiveShadow = true;
    box.userData.kind = 'building';
    box.userData.buildingId = def.id;
    group.add(box);

    // Accent sign band near the top.
    const band = new THREE.Mesh(
      new THREE.BoxGeometry(def.w + 0.1, 1.2, def.d + 0.1),
      new THREE.MeshStandardMaterial({ color: def.accent, roughness: 0.7, emissive: def.accent, emissiveIntensity: 0.15 }),
    );
    band.position.set(def.x, def.h - 1.4, def.z);
    group.add(band);

    // Door on the face toward the city centre, plus the stand point in front.
    const ox = -def.x;
    const oz = -def.z;
    let nx = 0;
    let nz = 0;
    if (Math.abs(ox) >= Math.abs(oz)) nx = Math.sign(ox) || 1;
    else nz = Math.sign(oz) || 1;

    const doorMat = new THREE.MeshStandardMaterial({ color: 0x1a1a1a, roughness: 0.6 });
    const door = new THREE.Mesh(new THREE.BoxGeometry(nx ? 0.2 : 1.6, 2.4, nz ? 0.2 : 1.6), doorMat);
    door.position.set(def.x + nx * (def.w / 2), 1.2, def.z + nz * (def.d / 2));
    group.add(door);

    const stand = {
      x: def.x + nx * (def.w / 2 + 1.6),
      z: def.z + nz * (def.d / 2 + 1.6),
    };

    // Name label above the door.
    const label = makeSign(def.name, def.accent);
    label.position.set(def.x + nx * (def.w / 2 + 0.1), Math.min(def.h - 0.4, 3.2), def.z + nz * (def.d / 2 + 0.1));
    group.add(label);

    this.root.add(group);
    this.buildings.push({ def, mesh: box, door: stand });
  }

  private scatterProps(): void {
    // Trees.
    const trunkMat = new THREE.MeshStandardMaterial({ color: 0x5a3a22, roughness: 1 });
    const leafColors = [0x2e7d32, 0x388e3c, 0x43a047];
    for (let i = 0; i < 16; i++) {
      const p = this.openSpot();
      const tree = new THREE.Group();
      const trunk = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.25, 1.4, 6), trunkMat);
      trunk.position.y = 0.7;
      trunk.castShadow = true;
      tree.add(trunk);
      const leaf = new THREE.Mesh(
        new THREE.SphereGeometry(randFloat(1.0, 1.6), 8, 6),
        new THREE.MeshStandardMaterial({ color: pick(leafColors), roughness: 1 }),
      );
      leaf.position.y = 2.0;
      leaf.castShadow = true;
      tree.add(leaf);
      tree.position.set(p.x, 0, p.z);
      this.root.add(tree);
    }

    // Street lamps along the avenues.
    const poleMat = new THREE.MeshStandardMaterial({ color: 0x222831, roughness: 0.6, metalness: 0.4 });
    for (let i = -WORLD_HALF + 10; i < WORLD_HALF; i += 16) {
      for (const [x, z] of [
        [6.5, i],
        [-6.5, i],
        [i, 6.5],
        [i, -6.5],
      ] as [number, number][]) {
        const lamp = new THREE.Group();
        const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.12, 4, 6), poleMat);
        pole.position.y = 2;
        lamp.add(pole);
        const bulb = new THREE.Mesh(
          new THREE.SphereGeometry(0.22, 8, 6),
          new THREE.MeshStandardMaterial({ color: 0xfff3c4, emissive: 0xffe08a, emissiveIntensity: 1 }),
        );
        bulb.position.y = 4;
        lamp.add(bulb);
        const light = new THREE.PointLight(0xffe6a8, 0.0, 14, 2);
        light.position.set(0, 4, 0);
        lamp.add(light);
        this.lamps.push(light);
        lamp.position.set(x, 0, z);
        this.root.add(lamp);
      }
    }
  }

  /** A random point that isn't on a road or inside a building. */
  private openSpot(): { x: number; z: number } {
    for (let tries = 0; tries < 40; tries++) {
      const x = randFloat(-WORLD_HALF + 4, WORLD_HALF - 4);
      const z = randFloat(-WORLD_HALF + 4, WORLD_HALF - 4);
      if (Math.abs(x) < 7 || Math.abs(z) < 7) continue; // keep off roads
      if (this.insideAnyBuilding(x, z, 2)) continue;
      return { x, z };
    }
    return { x: 20, z: 20 };
  }

  private insideAnyBuilding(x: number, z: number, pad: number): boolean {
    for (const b of this.buildings) {
      const { def } = b;
      if (
        x > def.x - def.w / 2 - pad &&
        x < def.x + def.w / 2 + pad &&
        z > def.z - def.d / 2 - pad &&
        z < def.z + def.d / 2 + pad
      ) {
        return true;
      }
    }
    return false;
  }

  /** Resolve a desired position against world bounds and building footprints. */
  collide(x: number, z: number): { x: number; z: number } {
    let nx = clamp(x, -WORLD_HALF + 1, WORLD_HALF - 1);
    let nz = clamp(z, -WORLD_HALF + 1, WORLD_HALF - 1);
    for (const b of this.buildings) {
      const { def } = b;
      const minX = def.x - def.w / 2 - BODY_R;
      const maxX = def.x + def.w / 2 + BODY_R;
      const minZ = def.z - def.d / 2 - BODY_R;
      const maxZ = def.z + def.d / 2 + BODY_R;
      if (nx > minX && nx < maxX && nz > minZ && nz < maxZ) {
        const dl = nx - minX;
        const dr = maxX - nx;
        const dn = nz - minZ;
        const df = maxZ - nz;
        const m = Math.min(dl, dr, dn, df);
        if (m === dl) nx = minX;
        else if (m === dr) nx = maxX;
        else if (m === dn) nz = minZ;
        else nz = maxZ;
      }
    }
    return { x: nx, z: nz };
  }

  /** Nearest building whose door is within `range` of (x,z). */
  nearestDoor(x: number, z: number, range: number): BuildingInstance | null {
    let best: BuildingInstance | null = null;
    let bestD = range;
    for (const b of this.buildings) {
      const d = Math.hypot(b.door.x - x, b.door.z - z);
      if (d < bestD) {
        bestD = d;
        best = b;
      }
    }
    return best;
  }

  /** Light up the lamps at night. */
  setNight(t: number): void {
    const intensity = (1 - t) * 1.4;
    for (const l of this.lamps) l.intensity = intensity;
  }

  static spawn() {
    return { ...SPAWN };
  }
}

function makeSign(text: string, accent: number): THREE.Sprite {
  const canvas = document.createElement('canvas');
  canvas.width = 512;
  canvas.height = 96;
  const ctx = canvas.getContext('2d')!;
  ctx.fillStyle = 'rgba(0,0,0,0.55)';
  roundRect(ctx, 4, 18, 504, 60, 12);
  ctx.fill();
  ctx.font = 'bold 34px Arial';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillStyle = '#' + accent.toString(16).padStart(6, '0');
  ctx.fillText(text, 256, 50);
  const tex = new THREE.CanvasTexture(canvas);
  tex.anisotropy = 4;
  const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, transparent: true, depthWrite: false }));
  sprite.scale.set(6, 1.1, 1);
  return sprite;
}

function roundRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number): void {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}
