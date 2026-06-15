// Three.js boilerplate: renderer, scene, camera, lights, sky, fog, resize, and a
// day/night tint driven by the game clock.

import * as THREE from 'three';

const DAY_SKY = new THREE.Color(0x9fc5e8);
const NIGHT_SKY = new THREE.Color(0x0b1020);
const DAY_GROUNDLIGHT = new THREE.Color(0x4a5a3a);
const NIGHT_GROUNDLIGHT = new THREE.Color(0x0a0e16);

export class Stage {
  renderer: THREE.WebGLRenderer;
  scene: THREE.Scene;
  camera: THREE.PerspectiveCamera;
  private hemi: THREE.HemisphereLight;
  private sun: THREE.DirectionalLight;
  private container: HTMLElement;

  constructor(container: HTMLElement) {
    this.container = container;
    this.renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    container.appendChild(this.renderer.domElement);

    this.scene = new THREE.Scene();
    this.scene.background = DAY_SKY.clone();
    this.scene.fog = new THREE.Fog(DAY_SKY.clone(), 60, 200);

    this.camera = new THREE.PerspectiveCamera(55, 1, 0.1, 500);
    this.camera.position.set(0, 12, 16);

    this.hemi = new THREE.HemisphereLight(0xffffff, DAY_GROUNDLIGHT.clone(), 0.9);
    this.scene.add(this.hemi);

    this.sun = new THREE.DirectionalLight(0xfff2d6, 1.1);
    this.sun.position.set(40, 70, 20);
    this.sun.castShadow = true;
    this.sun.shadow.mapSize.set(2048, 2048);
    const s = 90;
    this.sun.shadow.camera.left = -s;
    this.sun.shadow.camera.right = s;
    this.sun.shadow.camera.top = s;
    this.sun.shadow.camera.bottom = -s;
    this.sun.shadow.camera.far = 250;
    this.sun.shadow.bias = -0.0004;
    this.scene.add(this.sun);
    this.scene.add(this.sun.target);

    this.resize();
    window.addEventListener('resize', this.resize);
  }

  /** dayFactor: 0 = deep night, 1 = full day. */
  setDayFactor(t: number): void {
    const sky = NIGHT_SKY.clone().lerp(DAY_SKY, t);
    (this.scene.background as THREE.Color).copy(sky);
    (this.scene.fog as THREE.Fog).color.copy(sky);
    this.sun.intensity = 0.15 + t * 1.1;
    this.hemi.intensity = 0.25 + t * 0.75;
    this.hemi.groundColor.copy(NIGHT_GROUNDLIGHT.clone().lerp(DAY_GROUNDLIGHT, t));
  }

  /** Keep the sun shadow frustum following the focus point. */
  followShadow(x: number, z: number): void {
    this.sun.position.set(x + 40, 70, z + 20);
    this.sun.target.position.set(x, 0, z);
  }

  resize = () => {
    const w = this.container.clientWidth || window.innerWidth;
    const h = this.container.clientHeight || window.innerHeight;
    this.renderer.setSize(w, h, false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  };

  render(): void {
    this.renderer.render(this.scene, this.camera);
  }
}
