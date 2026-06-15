// Three.js stage with a modern, "real game" rendering pipeline:
//   • physically-based lighting via an image-based environment (IBL)
//   • a real atmospheric Sky shader with a sun tied to the game clock
//   • soft shadows + ACES filmic tone mapping
//   • a post-processing chain (bloom + filmic output + SMAA anti-aliasing)
// All driven by a day/night factor so dusk, night and noon each read clearly.

import * as THREE from 'three';
import { Sky } from 'three/examples/jsm/objects/Sky.js';
import { RoomEnvironment } from 'three/examples/jsm/environments/RoomEnvironment.js';
import { EffectComposer } from 'three/examples/jsm/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/examples/jsm/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/examples/jsm/postprocessing/UnrealBloomPass.js';
import { OutputPass } from 'three/examples/jsm/postprocessing/OutputPass.js';
import { SMAAPass } from 'three/examples/jsm/postprocessing/SMAAPass.js';

const DAY_FOG = new THREE.Color(0xaecbe6);
const NIGHT_FOG = new THREE.Color(0x0a0e1a);

export class Stage {
  renderer: THREE.WebGLRenderer;
  scene: THREE.Scene;
  camera: THREE.PerspectiveCamera;

  private composer: EffectComposer;
  private bloom: UnrealBloomPass;
  private hemi: THREE.HemisphereLight;
  private sun: THREE.DirectionalLight;
  private sky: Sky;
  private container: HTMLElement;

  constructor(container: HTMLElement) {
    this.container = container;
    this.renderer = new THREE.WebGLRenderer({ antialias: false, powerPreference: 'high-performance' });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.0;
    container.appendChild(this.renderer.domElement);

    this.scene = new THREE.Scene();
    this.scene.fog = new THREE.Fog(DAY_FOG.clone(), 55, 230);

    // Image-based lighting so PBR materials catch soft, believable light.
    const pmrem = new THREE.PMREMGenerator(this.renderer);
    this.scene.environment = pmrem.fromScene(new RoomEnvironment(), 0.04).texture;

    this.camera = new THREE.PerspectiveCamera(52, 1, 0.1, 1000);
    this.camera.position.set(0, 12, 16);

    // Sky dome.
    this.sky = new Sky();
    this.sky.scale.setScalar(8000);
    const u = this.sky.material.uniforms;
    u.turbidity.value = 6;
    u.rayleigh.value = 2.2;
    u.mieCoefficient.value = 0.005;
    u.mieDirectionalG.value = 0.8;
    this.scene.add(this.sky);

    // Lights.
    this.hemi = new THREE.HemisphereLight(0xbcd6ff, 0x4a4034, 0.6);
    this.scene.add(this.hemi);

    this.sun = new THREE.DirectionalLight(0xfff0d6, 2.6);
    this.sun.castShadow = true;
    this.sun.shadow.mapSize.set(2048, 2048);
    const s = 70;
    this.sun.shadow.camera.left = -s;
    this.sun.shadow.camera.right = s;
    this.sun.shadow.camera.top = s;
    this.sun.shadow.camera.bottom = -s;
    this.sun.shadow.camera.near = 0.5;
    this.sun.shadow.camera.far = 400;
    this.sun.shadow.bias = -0.0003;
    this.sun.shadow.normalBias = 0.02;
    this.scene.add(this.sun);
    this.scene.add(this.sun.target);

    // Post-processing chain on a HDR buffer so bloom/tone-mapping have headroom.
    const rt = new THREE.WebGLRenderTarget(1, 1, { type: THREE.HalfFloatType });
    this.composer = new EffectComposer(this.renderer, rt);
    this.composer.addPass(new RenderPass(this.scene, this.camera));
    this.bloom = new UnrealBloomPass(new THREE.Vector2(1, 1), 0.32, 0.5, 0.85);
    this.composer.addPass(this.bloom);
    this.composer.addPass(new OutputPass());
    this.composer.addPass(new SMAAPass(1, 1));

    this.resize();
    window.addEventListener('resize', this.resize);
  }

  /** dayFactor: 0 = deep night, 1 = full midday. */
  setDayFactor(t: number): void {
    const elevation = THREE.MathUtils.lerp(-8, 56, t); // sun height in degrees
    const azimuth = 150;
    const phi = THREE.MathUtils.degToRad(90 - elevation);
    const theta = THREE.MathUtils.degToRad(azimuth);
    const sunDir = new THREE.Vector3().setFromSphericalCoords(1, phi, theta);
    this.sky.material.uniforms.sunPosition.value.copy(sunDir);

    this.sunDir.copy(sunDir);
    this.sun.intensity = 0.05 + Math.max(0, t) * 3.0;
    this.sun.color.setHSL(0.09, 0.5, 0.55 + t * 0.1); // warmer at dawn/dusk
    this.hemi.intensity = 0.18 + t * 0.55;

    // Darker, moodier exposure at night; bright and crisp at noon.
    this.renderer.toneMappingExposure = 0.55 + t * 0.65;
    this.bloom.strength = 0.55 - t * 0.25; // night neon/lamps bloom more

    const fog = NIGHT_FOG.clone().lerp(DAY_FOG, Math.max(0, t));
    (this.scene.fog as THREE.Fog).color.copy(fog);
  }

  private sunDir = new THREE.Vector3(0, 1, 0);

  /** Keep the sun + shadow frustum following the player. */
  followShadow(x: number, z: number): void {
    this.sun.position.set(x + this.sunDir.x * 120, 80 + this.sunDir.y * 60, z + this.sunDir.z * 120);
    this.sun.target.position.set(x, 0, z);
  }

  resize = () => {
    const w = this.container.clientWidth || window.innerWidth;
    const h = this.container.clientHeight || window.innerHeight;
    this.renderer.setSize(w, h, false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
    this.composer.setSize(w, h);
    this.bloom.setSize(w, h);
  };

  render(): void {
    this.composer.render();
  }
}
