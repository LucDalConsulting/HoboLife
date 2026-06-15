// Builds the LA city block with textured surfaces and props: asphalt roads with
// markings & crosswalks, concrete sidewalks, buildings with windowed facades
// that light up at night, street lamps, trees, benches, hydrants and trash cans.
// Also provides movement collision and the pickable meshes for click-to-move.

import * as THREE from 'three';
import { clamp, pick, randFloat } from '../core/rng';
import { BUILDINGS, SPAWN, WORLD_HALF, type BuildingDef } from '../data/city';
import { asphaltTexture, concreteTexture, facadeTexture, grassTexture } from '../engine/textures';

export interface BuildingInstance {
  def: BuildingDef;
  mesh: THREE.Mesh;
  door: { x: number; z: number };
}

const BODY_R = 0.5;
const ROAD_HALF = 5;

export class World {
  root = new THREE.Group();
  groundPlane: THREE.Mesh;
  buildings: BuildingInstance[] = [];

  private lampLights: THREE.PointLight[] = [];
  private lampBulbs: THREE.MeshStandardMaterial[] = [];
  private windowMats: THREE.MeshStandardMaterial[] = [];

  constructor() {
    this.buildGround();
    this.buildRoads();
    this.buildSidewalks();
    this.groundPlane = this.buildPickGround();
    this.root.add(this.groundPlane);
    for (const def of BUILDINGS) this.buildBuilding(def);
    this.buildParks();
    this.buildStreetFurniture();
  }

  // --- surfaces -------------------------------------------------------------

  private buildGround(): void {
    const mat = new THREE.MeshStandardMaterial({ map: concreteTexture(40), roughness: 0.95, metalness: 0 });
    const ground = new THREE.Mesh(new THREE.PlaneGeometry(WORLD_HALF * 2, WORLD_HALF * 2), mat);
    ground.rotation.x = -Math.PI / 2;
    ground.receiveShadow = true;
    this.root.add(ground);
  }

  private buildPickGround(): THREE.Mesh {
    const m = new THREE.Mesh(
      new THREE.PlaneGeometry(WORLD_HALF * 2, WORLD_HALF * 2),
      new THREE.MeshBasicMaterial({ visible: false }),
    );
    m.rotation.x = -Math.PI / 2;
    m.position.y = 0.02;
    m.userData.kind = 'ground';
    return m;
  }

  private buildRoads(): void {
    const mat = new THREE.MeshStandardMaterial({ map: asphaltTexture(20), roughness: 1, metalness: 0 });
    for (const road of [
      new THREE.Mesh(new THREE.PlaneGeometry(WORLD_HALF * 2, ROAD_HALF * 2), mat),
      new THREE.Mesh(new THREE.PlaneGeometry(ROAD_HALF * 2, WORLD_HALF * 2), mat),
    ]) {
      road.rotation.x = -Math.PI / 2;
      road.position.y = 0.012;
      road.receiveShadow = true;
      this.root.add(road);
    }

    // Dashed centre lines.
    const lineMat = new THREE.MeshStandardMaterial({ color: 0xf0d54a, roughness: 1, emissive: 0x3a3210, emissiveIntensity: 0.2 });
    for (let i = -WORLD_HALF + 3; i < WORLD_HALF; i += 6) {
      if (Math.abs(i) < ROAD_HALF + 1) continue;
      const a = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 0.28), lineMat);
      a.rotation.x = -Math.PI / 2; a.position.set(i, 0.02, 0); this.root.add(a);
      const b = new THREE.Mesh(new THREE.PlaneGeometry(0.28, 2.4), lineMat);
      b.rotation.x = -Math.PI / 2; b.position.set(0, 0.02, i); this.root.add(b);
    }

    // Crosswalk stripes around the central intersection.
    const wMat = new THREE.MeshStandardMaterial({ color: 0xdfe3e8, roughness: 1 });
    for (let k = -4; k <= 4; k++) {
      if (Math.abs(k) < 1) continue;
      for (const off of [ROAD_HALF + 1.2, -(ROAD_HALF + 1.2)]) {
        const h = new THREE.Mesh(new THREE.PlaneGeometry(0.5, 3), wMat);
        h.rotation.x = -Math.PI / 2; h.position.set(k * 0.9, 0.02, off); this.root.add(h);
        const v = new THREE.Mesh(new THREE.PlaneGeometry(3, 0.5), wMat);
        v.rotation.x = -Math.PI / 2; v.position.set(off, 0.02, k * 0.9); this.root.add(v);
      }
    }
  }

  private buildSidewalks(): void {
    const mat = new THREE.MeshStandardMaterial({ map: concreteTexture(3), roughness: 0.9 });
    const curb = new THREE.MeshStandardMaterial({ color: 0x8d887e, roughness: 0.9 });
    // Raised sidewalk slabs flanking both avenues.
    for (const side of [1, -1]) {
      for (const axis of ['x', 'z'] as const) {
        const long = WORLD_HALF * 2;
        const geo = axis === 'x' ? new THREE.BoxGeometry(long, 0.18, 2.4) : new THREE.BoxGeometry(2.4, 0.18, long);
        const sw = new THREE.Mesh(geo, [curb, curb, mat, curb, curb, curb]);
        if (axis === 'x') sw.position.set(0, 0.09, side * (ROAD_HALF + 1.3));
        else sw.position.set(side * (ROAD_HALF + 1.3), 0.09, 0);
        sw.receiveShadow = true;
        this.root.add(sw);
      }
    }
  }

  // --- buildings ------------------------------------------------------------

  private buildBuilding(def: BuildingDef): void {
    const group = new THREE.Group();

    const floors = clamp(Math.round(def.h / 3), 3, 8);
    const cols = clamp(Math.round(def.w / 3), 3, 7);
    const facade = facadeTexture(hex(def.color), floors, cols);
    const sideMat = new THREE.MeshStandardMaterial({
      map: facade.map,
      emissiveMap: facade.emissiveMap,
      emissive: 0xffffff,
      emissiveIntensity: 0,
      roughness: 0.78,
      metalness: 0.08,
    });
    this.windowMats.push(sideMat);
    const roofMat = new THREE.MeshStandardMaterial({ color: shadeHex(def.color, -36), roughness: 0.95 });
    const darkMat = new THREE.MeshStandardMaterial({ color: 0x15171b, roughness: 1 });
    const mats = [sideMat, sideMat, roofMat, darkMat, sideMat, sideMat];

    const box = new THREE.Mesh(new THREE.BoxGeometry(def.w, def.h, def.d), mats);
    box.position.set(def.x, def.h / 2, def.z);
    box.castShadow = true;
    box.receiveShadow = true;
    box.userData.kind = 'building';
    box.userData.buildingId = def.id;
    group.add(box);

    // Parapet + a couple of rooftop AC units.
    const parapet = new THREE.Mesh(new THREE.BoxGeometry(def.w + 0.2, 0.5, def.d + 0.2), roofMat);
    parapet.position.set(def.x, def.h + 0.25, def.z);
    group.add(parapet);
    for (let i = 0; i < 2; i++) {
      const ac = new THREE.Mesh(new THREE.BoxGeometry(1.6, 1, 1.6), new THREE.MeshStandardMaterial({ color: 0x9aa0a6, roughness: 0.6, metalness: 0.4 }));
      ac.position.set(def.x + randFloat(-def.w / 3, def.w / 3), def.h + 0.6, def.z + randFloat(-def.d / 3, def.d / 3));
      ac.castShadow = true;
      group.add(ac);
    }

    // Door facing the city centre + the stand point in front.
    const ox = -def.x;
    const oz = -def.z;
    let nx = 0;
    let nz = 0;
    if (Math.abs(ox) >= Math.abs(oz)) nx = Math.sign(ox) || 1;
    else nz = Math.sign(oz) || 1;

    const glassMat = new THREE.MeshStandardMaterial({ color: 0x1b2730, roughness: 0.2, metalness: 0.6, emissive: 0x0a0f14, emissiveIntensity: 0.2 });
    const door = new THREE.Mesh(new THREE.BoxGeometry(nx ? 0.3 : 2.4, 2.6, nz ? 0.3 : 2.4), glassMat);
    door.position.set(def.x + nx * (def.w / 2), 1.3, def.z + nz * (def.d / 2));
    group.add(door);

    // Lit entrance canopy.
    const canopy = new THREE.Mesh(new THREE.BoxGeometry(nx ? 1.4 : 3.4, 0.3, nz ? 1.4 : 3.4), new THREE.MeshStandardMaterial({ color: shadeHex(def.color, -10), roughness: 0.7 }));
    canopy.position.set(def.x + nx * (def.w / 2 + 0.6), 3.0, def.z + nz * (def.d / 2 + 0.6));
    canopy.castShadow = true;
    group.add(canopy);

    const stand = { x: def.x + nx * (def.w / 2 + 1.7), z: def.z + nz * (def.d / 2 + 1.7) };

    // Neon-ish sign above the door (glows via emissive canvas + bloom).
    const sign = makeSign(def.name, def.accent);
    sign.position.set(def.x + nx * (def.w / 2 + 0.12), Math.min(def.h - 0.6, 3.6), def.z + nz * (def.d / 2 + 0.12));
    group.add(sign);

    this.root.add(group);
    this.buildings.push({ def, mesh: box, door: stand });
  }

  // --- greenery & furniture -------------------------------------------------

  private buildParks(): void {
    const grassMat = new THREE.MeshStandardMaterial({ map: grassTexture(4), roughness: 1 });
    const parks: [number, number, number, number][] = [
      [-22, 22, 12, 12],
      [22, -22, 12, 12],
    ];
    for (const [x, z, w, d] of parks) {
      const g = new THREE.Mesh(new THREE.PlaneGeometry(w, d), grassMat);
      g.rotation.x = -Math.PI / 2; g.position.set(x, 0.015, z); g.receiveShadow = true;
      this.root.add(g);
      for (let i = 0; i < 5; i++) this.addTree(x + randFloat(-w / 2 + 1, w / 2 - 1), z + randFloat(-d / 2 + 1, d / 2 - 1));
    }
    // A few trees lining the avenues.
    for (let i = -WORLD_HALF + 12; i < WORLD_HALF; i += 14) {
      this.addTree(ROAD_HALF + 2.6, i);
      this.addTree(-(ROAD_HALF + 2.6), i);
    }
  }

  private addTree(x: number, z: number): void {
    if (this.insideAnyBuilding(x, z, 1.5)) return;
    const tree = new THREE.Group();
    const trunk = new THREE.Mesh(
      new THREE.CylinderGeometry(0.16, 0.26, 1.6, 7),
      new THREE.MeshStandardMaterial({ color: 0x5a3b22, roughness: 1 }),
    );
    trunk.position.y = 0.8; trunk.castShadow = true; tree.add(trunk);
    const leafMat = new THREE.MeshStandardMaterial({ color: pick([0x2f7d34, 0x376e2c, 0x44913f]), roughness: 1, flatShading: true });
    for (let i = 0; i < 3; i++) {
      const blob = new THREE.Mesh(new THREE.IcosahedronGeometry(randFloat(0.95, 1.4), 0), leafMat);
      blob.position.set(randFloat(-0.4, 0.4), 1.9 + randFloat(-0.2, 0.5), randFloat(-0.4, 0.4));
      blob.castShadow = true; tree.add(blob);
    }
    tree.position.set(x, 0, z);
    this.root.add(tree);
  }

  private buildStreetFurniture(): void {
    // Street lamps along both avenues.
    for (let i = -WORLD_HALF + 10; i < WORLD_HALF; i += 12) {
      this.addLamp(ROAD_HALF + 1.9, i);
      this.addLamp(-(ROAD_HALF + 1.9), i);
      this.addLamp(i, ROAD_HALF + 1.9);
      this.addLamp(i, -(ROAD_HALF + 1.9));
    }
    // Benches, trash cans, hydrants dotted along the sidewalks.
    for (let i = -WORLD_HALF + 16; i < WORLD_HALF; i += 18) {
      this.addBench(ROAD_HALF + 2.2, i, 0);
      this.addTrashCan(-(ROAD_HALF + 2.2), i + 4);
      this.addHydrant(i, -(ROAD_HALF + 2.2));
    }
  }

  private addLamp(x: number, z: number): void {
    if (this.insideAnyBuilding(x, z, 1)) return;
    const lamp = new THREE.Group();
    const poleMat = new THREE.MeshStandardMaterial({ color: 0x20262e, roughness: 0.5, metalness: 0.6 });
    const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.09, 0.12, 4.4, 8), poleMat);
    pole.position.y = 2.2; pole.castShadow = true; lamp.add(pole);
    const arm = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 0.9, 6), poleMat);
    arm.rotation.z = Math.PI / 2; arm.position.set(0.4, 4.3, 0); lamp.add(arm);
    const bulbMat = new THREE.MeshStandardMaterial({ color: 0xfff4d2, emissive: 0xffdf9e, emissiveIntensity: 0 });
    this.lampBulbs.push(bulbMat);
    const bulb = new THREE.Mesh(new THREE.SphereGeometry(0.2, 10, 8), bulbMat);
    bulb.position.set(0.8, 4.2, 0); lamp.add(bulb);
    const light = new THREE.PointLight(0xffe2a6, 0, 13, 2);
    light.position.set(0.8, 4.1, 0);
    this.lampLights.push(light); lamp.add(light);
    lamp.position.set(x, 0, z);
    this.root.add(lamp);
  }

  private addBench(x: number, z: number, rot: number): void {
    if (this.insideAnyBuilding(x, z, 1)) return;
    const b = new THREE.Group();
    const wood = new THREE.MeshStandardMaterial({ color: 0x6b4a2b, roughness: 0.8 });
    const metal = new THREE.MeshStandardMaterial({ color: 0x2a2f36, roughness: 0.5, metalness: 0.6 });
    const seat = new THREE.Mesh(new THREE.BoxGeometry(1.8, 0.12, 0.5), wood); seat.position.y = 0.5; seat.castShadow = true; b.add(seat);
    const back = new THREE.Mesh(new THREE.BoxGeometry(1.8, 0.5, 0.1), wood); back.position.set(0, 0.78, -0.2); b.add(back);
    for (const sx of [-0.7, 0.7]) { const leg = new THREE.Mesh(new THREE.BoxGeometry(0.12, 0.5, 0.45), metal); leg.position.set(sx, 0.25, 0); b.add(leg); }
    b.position.set(x, 0, z); b.rotation.y = rot;
    this.root.add(b);
  }

  private addTrashCan(x: number, z: number): void {
    if (this.insideAnyBuilding(x, z, 1)) return;
    const can = new THREE.Mesh(
      new THREE.CylinderGeometry(0.34, 0.3, 0.9, 12),
      new THREE.MeshStandardMaterial({ color: 0x2f4f3a, roughness: 0.6, metalness: 0.3 }),
    );
    can.position.set(x, 0.45, z); can.castShadow = true;
    this.root.add(can);
  }

  private addHydrant(x: number, z: number): void {
    if (this.insideAnyBuilding(x, z, 1)) return;
    const h = new THREE.Group();
    const red = new THREE.MeshStandardMaterial({ color: 0xb22a1f, roughness: 0.5 });
    const body = new THREE.Mesh(new THREE.CylinderGeometry(0.16, 0.2, 0.7, 10), red); body.position.y = 0.35; body.castShadow = true; h.add(body);
    const top = new THREE.Mesh(new THREE.SphereGeometry(0.17, 10, 8), red); top.position.y = 0.72; h.add(top);
    for (const a of [0, Math.PI]) { const cap = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 0.3, 8), red); cap.rotation.z = Math.PI / 2; cap.position.set(Math.cos(a) * 0.2, 0.4, 0); h.add(cap); }
    h.position.set(x, 0, z);
    this.root.add(h);
  }

  // --- collision ------------------------------------------------------------

  private insideAnyBuilding(x: number, z: number, pad: number): boolean {
    for (const b of this.buildings) {
      const { def } = b;
      if (x > def.x - def.w / 2 - pad && x < def.x + def.w / 2 + pad && z > def.z - def.d / 2 - pad && z < def.z + def.d / 2 + pad) return true;
    }
    return false;
  }

  collide(x: number, z: number): { x: number; z: number } {
    let nx = clamp(x, -WORLD_HALF + 1, WORLD_HALF - 1);
    let nz = clamp(z, -WORLD_HALF + 1, WORLD_HALF - 1);
    for (const b of this.buildings) {
      const { def } = b;
      const minX = def.x - def.w / 2 - BODY_R, maxX = def.x + def.w / 2 + BODY_R;
      const minZ = def.z - def.d / 2 - BODY_R, maxZ = def.z + def.d / 2 + BODY_R;
      if (nx > minX && nx < maxX && nz > minZ && nz < maxZ) {
        const dl = nx - minX, dr = maxX - nx, dn = nz - minZ, df = maxZ - nz;
        const m = Math.min(dl, dr, dn, df);
        if (m === dl) nx = minX; else if (m === dr) nx = maxX; else if (m === dn) nz = minZ; else nz = maxZ;
      }
    }
    return { x: nx, z: nz };
  }

  nearestDoor(x: number, z: number, range: number): BuildingInstance | null {
    let best: BuildingInstance | null = null;
    let bestD = range;
    for (const b of this.buildings) {
      const d = Math.hypot(b.door.x - x, b.door.z - z);
      if (d < bestD) { bestD = d; best = b; }
    }
    return best;
  }

  /** t: 0 = night, 1 = day. Lights and windows glow as it gets dark. */
  setNight(t: number): void {
    const night = 1 - Math.max(0, t);
    for (const l of this.lampLights) l.intensity = night * 1.5;
    for (const m of this.lampBulbs) m.emissiveIntensity = 0.15 + night * 1.1;
    for (const m of this.windowMats) m.emissiveIntensity = night * 1.15;
  }

  static spawn() {
    return { ...SPAWN };
  }
}

// --- helpers ----------------------------------------------------------------

function hex(n: number): string {
  return '#' + n.toString(16).padStart(6, '0');
}
function shadeHex(n: number, amt: number): number {
  const r = clamp(((n >> 16) & 0xff) + amt, 0, 255);
  const g = clamp(((n >> 8) & 0xff) + amt, 0, 255);
  const b = clamp((n & 0xff) + amt, 0, 255);
  return (r << 16) | (g << 8) | b;
}

function makeSign(text: string, accent: number): THREE.Sprite {
  const canvas = document.createElement('canvas');
  canvas.width = 512;
  canvas.height = 110;
  const ctx = canvas.getContext('2d')!;
  ctx.fillStyle = 'rgba(8,10,14,0.78)';
  roundRect(ctx, 6, 24, 500, 62, 14);
  ctx.fill();
  ctx.strokeStyle = hex(accent);
  ctx.lineWidth = 3;
  roundRect(ctx, 6, 24, 500, 62, 14);
  ctx.stroke();
  ctx.font = 'bold 36px Arial';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.shadowColor = hex(accent);
  ctx.shadowBlur = 16;
  ctx.fillStyle = hex(accent);
  ctx.fillText(text, 256, 56);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 8;
  const spriteMat = new THREE.SpriteMaterial({ map: tex, transparent: true, depthWrite: false });
  const sprite = new THREE.Sprite(spriteMat);
  sprite.scale.set(6.4, 1.4, 1);
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
