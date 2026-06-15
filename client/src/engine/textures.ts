// Procedural canvas textures — no external asset downloads required, so the game
// stays a single self-contained build while still having real surfaces:
// speckled asphalt, concrete sidewalks, and building facades with windows that
// can light up at night (via a separate emissive map).

import * as THREE from 'three';

function makeCanvas(size = 256): [HTMLCanvasElement, CanvasRenderingContext2D] {
  const c = document.createElement('canvas');
  c.width = c.height = size;
  return [c, c.getContext('2d')!];
}

function finish(canvas: HTMLCanvasElement, repeat = 1, srgb = true): THREE.CanvasTexture {
  const tex = new THREE.CanvasTexture(canvas);
  tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
  tex.repeat.set(repeat, repeat);
  tex.anisotropy = 8;
  if (srgb) tex.colorSpace = THREE.SRGBColorSpace;
  return tex;
}

function speckle(ctx: CanvasRenderingContext2D, size: number, count: number, colors: string[], min = 1, max = 3): void {
  for (let i = 0; i < count; i++) {
    ctx.fillStyle = colors[(Math.random() * colors.length) | 0];
    const r = min + Math.random() * (max - min);
    ctx.beginPath();
    ctx.arc(Math.random() * size, Math.random() * size, r, 0, Math.PI * 2);
    ctx.fill();
  }
}

export function asphaltTexture(repeat = 14): THREE.CanvasTexture {
  const size = 256;
  const [c, ctx] = makeCanvas(size);
  ctx.fillStyle = '#2c2f35';
  ctx.fillRect(0, 0, size, size);
  speckle(ctx, size, 2600, ['#34373d', '#26282d', '#3a3d44', '#202227'], 0.6, 2.2);
  return finish(c, repeat);
}

export function concreteTexture(repeat = 6): THREE.CanvasTexture {
  const size = 256;
  const [c, ctx] = makeCanvas(size);
  ctx.fillStyle = '#9a958c';
  ctx.fillRect(0, 0, size, size);
  speckle(ctx, size, 1800, ['#a8a298', '#8c877e', '#b0aaa0'], 0.5, 1.8);
  // slab seams
  ctx.strokeStyle = 'rgba(60,58,54,0.5)';
  ctx.lineWidth = 2;
  for (let i = 0; i <= size; i += size / 4) {
    ctx.beginPath(); ctx.moveTo(i, 0); ctx.lineTo(i, size); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(0, i); ctx.lineTo(size, i); ctx.stroke();
  }
  return finish(c, repeat);
}

export function grassTexture(repeat = 10): THREE.CanvasTexture {
  const size = 256;
  const [c, ctx] = makeCanvas(size);
  ctx.fillStyle = '#46612e';
  ctx.fillRect(0, 0, size, size);
  speckle(ctx, size, 3000, ['#52703a', '#3c5326', '#5c7d40', '#41592a'], 0.8, 2.6);
  return finish(c, repeat);
}

export interface FacadeResult {
  map: THREE.CanvasTexture;
  emissiveMap: THREE.CanvasTexture;
}

/** Building facade with a grid of windows + an emissive map where lit windows glow. */
export function facadeTexture(wall: string, floors = 6, cols = 5): FacadeResult {
  const size = 256;
  const [c, ctx] = makeCanvas(size);
  const [ec, ectx] = makeCanvas(size);

  ctx.fillStyle = wall;
  ctx.fillRect(0, 0, size, size);
  speckle(ctx, size, 900, [shade(wall, 12), shade(wall, -12)], 0.6, 1.6);

  ectx.fillStyle = '#000';
  ectx.fillRect(0, 0, size, size);

  const margin = 10;
  const gapX = 8;
  const gapY = 8;
  const winW = (size - margin * 2 - gapX * (cols - 1)) / cols;
  const winH = (size - margin * 2 - gapY * (floors - 1)) / floors;

  for (let f = 0; f < floors; f++) {
    for (let w = 0; w < cols; w++) {
      const x = margin + w * (winW + gapX);
      const y = margin + f * (winH + gapY);
      // glass
      ctx.fillStyle = '#26303a';
      ctx.fillRect(x, y, winW, winH);
      ctx.fillStyle = 'rgba(120,150,170,0.25)';
      ctx.fillRect(x, y, winW, winH * 0.45);
      // frame
      ctx.strokeStyle = shade(wall, -20);
      ctx.lineWidth = 2;
      ctx.strokeRect(x, y, winW, winH);
      // lit?
      if (Math.random() < 0.5) {
        const warm = Math.random() < 0.8;
        ectx.fillStyle = warm ? '#ffd98a' : '#bfe3ff';
        ectx.fillRect(x + 1, y + 1, winW - 2, winH - 2);
      }
    }
  }

  return { map: finish(c, 1), emissiveMap: finish(ec, 1) };
}

/** Lighten/darken a hex color by amount (-255..255). */
function shade(hex: string, amt: number): string {
  const n = parseInt(hex.replace('#', ''), 16);
  const r = clamp8((n >> 16) + amt);
  const g = clamp8(((n >> 8) & 0xff) + amt);
  const b = clamp8((n & 0xff) + amt);
  return `#${((r << 16) | (g << 8) | b).toString(16).padStart(6, '0')}`;
}
function clamp8(v: number): number {
  return v < 0 ? 0 : v > 255 ? 255 : v | 0;
}
