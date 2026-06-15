// The orchestrator: owns the scene, the player, NPCs, the camera, every UI
// surface, the game loop, input routing, interactions, combat and networking.

import * as THREE from 'three';
import { DAY_START_HOUR, INTERACT_RANGE, NIGHT_START_HOUR } from '../core/constants';
import type { MoveDef } from '../core/combatmath';
import { clamp, randInt } from '../core/rng';
import type { CharacterData, HandSlot, ItemStack } from '../core/types';
import { TREES, type CheckOutcome } from '../data/dialogue';
import { getItem, maybeItem } from '../data/items';
import { makeCityNPCs } from '../data/npcs';
import { SPAWN, type BuildingDef } from '../data/city';
import { FollowCamera } from '../engine/FollowCamera';
import { Input } from '../engine/Input';
import { Stage } from '../engine/Stage';
import { Character } from '../entities/Character';
import { NPCEntity } from '../entities/NPCEntity';
import { Player } from '../entities/Player';
import type { GameState } from '../state/GameState';
import type { CombatResult } from '../ui/Combat';
import { Combat } from '../ui/Combat';
import { BuildingPanel, type BuildingCtx } from '../ui/BuildingPanel';
import { CharacterCreation } from '../ui/CharacterCreation';
import { DiceOverlay } from '../ui/DiceOverlay';
import { Dialogue } from '../ui/Dialogue';
import { HUD } from '../ui/HUD';
import { Inventory } from '../ui/Inventory';
import { JobMiniGame } from '../ui/JobMiniGame';
import { Toast } from '../ui/Toast';
import { World } from '../world/World';
import { buildInterior, collideInterior, type InteriorInstance } from '../world/Interior';
import { NetClient, type RemotePlayer } from '../net/NetClient';

type Pending =
  | { type: 'npc'; entity: NPCEntity }
  | { type: 'door'; building: World['buildings'][number] }
  | null;

interface Remote {
  char: Character;
  target: THREE.Vector3;
  heading: number;
}

export class Game {
  private state: GameState;
  private stage: Stage;
  private input: Input;
  private cam = new FollowCamera();
  private world = new World();
  private player: Player;
  private npcs: NPCEntity[];

  private hud = new HUD();
  private toast = new Toast();
  private dice = new DiceOverlay();
  private dialogue = new Dialogue();
  private combat = new Combat();
  private inventory = new Inventory();
  private buildingPanel = new BuildingPanel();
  private job = new JobMiniGame();
  private creation = new CharacterCreation();

  private raycaster = new THREE.Raycaster();
  private pointer = new THREE.Vector2();
  private clock = new THREE.Clock();

  private pending: Pending = null;
  private combatOpen = false;
  private creationOpen = false;
  private persistAccum = 0;

  private net = new NetClient();
  private remotes = new Map<string, Remote>();
  private netSendAccum = 0;

  private interior: InteriorInstance | null = null;
  private interiorReturn = { x: 0, z: 0 };

  constructor(state: GameState) {
    this.state = state;
    const app = document.getElementById('app')!;
    this.stage = new Stage(app);
    this.input = new Input(this.stage.renderer.domElement);

    this.stage.scene.add(this.world.root);

    this.player = new Player(state.character.appearance, state.character.pos);
    this.stage.scene.add(this.player.group);

    this.npcs = makeCityNPCs().map((spec) => {
      const npc = new NPCEntity(spec);
      this.stage.scene.add(npc.group);
      return npc;
    });

    this.hud.onUseHand = (slot) => this.useHand(slot);
    this.input.onKey((code) => this.onKey(code));
    this.input.onClick((ev) => this.onClick(ev));

    this.setupNet();
  }

  start(): void {
    this.hud.update(this.state);
    this.loop();
  }

  // --- main loop ------------------------------------------------------------

  private loop = (): void => {
    const dt = Math.min(this.clock.getDelta(), 0.05);
    const modal = this.isModalOpen();
    this.input.enabled = !modal;

    if (!modal && !this.state.isDead()) {
      if (this.interior) {
        this.player.update(dt, this.input, this.cam, (x, z) => collideInterior(this.interior!, x, z));
        this.interior.clerk.update(dt, 0);
        this.cam.distance = Math.min(this.cam.distance, 7.5);
      } else {
        this.player.update(dt, this.input, this.cam, (x, z) => this.world.collide(x, z));
        this.state.setPosition(this.player.position.x, this.player.position.z);
        for (const npc of this.npcs) npc.update(dt, (x, z) => this.world.collide(x, z));
        this.netTick(dt);
      }
      this.state.advanceTime(dt);
      this.resolvePending();
      this.updatePrompt();
    }

    this.cam.target.set(this.player.position.x, 0, this.player.position.z);
    this.cam.update(this.input, this.stage.camera);
    this.updateRemotes(dt);

    const f = this.dayFactor();
    this.stage.setDayFactor(f);
    this.world.setNight(f);
    this.stage.followShadow(this.player.position.x, this.player.position.z);

    this.hud.update(this.state);

    if (this.state.isDead() && !this.creationOpen) this.handleDeath();

    this.persistAccum += dt;
    if (this.persistAccum > 4) {
      this.state.persist();
      this.persistAccum = 0;
    }

    this.stage.render();
    requestAnimationFrame(this.loop);
  };

  // --- modal bookkeeping ----------------------------------------------------

  private isBlockingModal(): boolean {
    return (
      this.creationOpen ||
      this.combatOpen ||
      this.dialogue.isOpen() ||
      this.buildingPanel.isOpen() ||
      this.job.isOpen()
    );
  }
  private isModalOpen(): boolean {
    return this.isBlockingModal() || this.inventory.isOpen();
  }

  // --- input ----------------------------------------------------------------

  private onKey(code: string): void {
    if (code === 'KeyI') {
      if (this.inventory.isOpen()) this.inventory.close();
      else if (!this.isBlockingModal()) this.inventory.open(this.state);
      return;
    }
    if (this.isModalOpen() || this.state.isDead()) return;
    if (code === 'Escape') {
      if (this.interior) this.exitInterior();
      return;
    }
    switch (code) {
      case 'KeyQ': if (!this.interior) this.talkNearest(); break;
      case 'KeyE': this.interactKey(); break;
      case 'KeyF': this.useHand('left'); break;
      case 'KeyG': this.useHand('right'); break;
    }
  }

  /** E: enter a building, or (inside) use the counter / step outside. */
  private interactKey(): void {
    if (this.interior) {
      if (this.distXZ(this.interior.exit) < 2.4) this.exitInterior();
      else if (this.distXZ(this.interior.counter) < 2.6) this.openServices(this.interior.def);
      return;
    }
    const door = this.world.nearestDoor(this.player.position.x, this.player.position.z, 2.6);
    if (door) this.enterInterior(door.def, door.door);
  }

  private onClick(ev: { ndcX: number; ndcY: number; button: number }): void {
    if (this.isModalOpen() || this.state.isDead()) return;
    if (ev.button === 2) {
      this.useHand('right');
      return;
    }
    this.pointer.set(ev.ndcX, ev.ndcY);
    this.raycaster.setFromCamera(this.pointer, this.stage.camera);

    // Inside a building: click the floor to walk.
    if (this.interior) {
      const hit = this.raycaster.intersectObject(this.interior.floor);
      if (hit.length) {
        this.player.setMoveTarget(hit[0].point.x, hit[0].point.z);
      }
      return;
    }

    // NPCs first.
    const npcHits = this.raycaster.intersectObjects(this.npcs.map((n) => n.group), true);
    if (npcHits.length) {
      const npc = this.findNpc(npcHits[0].object);
      if (npc) {
        this.pending = { type: 'npc', entity: npc };
        this.player.setMoveTarget(npc.position.x, npc.position.z);
        return;
      }
    }
    // Buildings next.
    const bHits = this.raycaster.intersectObjects(this.world.buildings.map((b) => b.mesh));
    if (bHits.length) {
      const inst = this.world.buildings.find((b) => b.mesh === bHits[0].object);
      if (inst) {
        this.pending = { type: 'door', building: inst };
        this.player.setMoveTarget(inst.door.x, inst.door.z);
        return;
      }
    }
    // Ground = click-to-move.
    const gHit = this.raycaster.intersectObject(this.world.groundPlane);
    if (gHit.length) {
      this.pending = null;
      this.player.setMoveTarget(gHit[0].point.x, gHit[0].point.z);
    }
  }

  private findNpc(obj: THREE.Object3D | null): NPCEntity | null {
    let o: THREE.Object3D | null = obj;
    while (o) {
      const npc = this.npcs.find((n) => n.group === o);
      if (npc) return npc;
      o = o.parent;
    }
    return null;
  }

  // --- interaction ----------------------------------------------------------

  private resolvePending(): void {
    if (!this.pending || this.interior) return;
    if (this.pending.type === 'npc') {
      const npc = this.pending.entity;
      if (this.dist(npc.position) < INTERACT_RANGE) {
        this.pending = null;
        this.openDialogue(npc);
      }
    } else if (this.pending.type === 'door') {
      const b = this.pending.building;
      if (Math.hypot(b.door.x - this.player.position.x, b.door.z - this.player.position.z) < 2.4) {
        this.pending = null;
        this.enterInterior(b.def, b.door);
      }
    }
  }

  private updatePrompt(): void {
    if (this.interior) {
      if (this.distXZ(this.interior.exit) < 2.4) this.hud.setPrompt('Press <b>E</b> to step outside');
      else if (this.distXZ(this.interior.counter) < 2.6) this.hud.setPrompt('Press <b>E</b> for service');
      else this.hud.setPrompt(null);
      return;
    }
    const npc = this.nearestNpcInRange();
    if (npc) {
      this.hud.setPrompt(`Press <b>Q</b> to talk to ${npc.spec.name}`);
      return;
    }
    const door = this.world.nearestDoor(this.player.position.x, this.player.position.z, 2.6);
    if (door) {
      this.hud.setPrompt(`Press <b>E</b> to enter <b>${door.def.name}</b>`);
      return;
    }
    this.hud.setPrompt(null);
  }

  private talkNearest(): void {
    const npc = this.nearestNpcInRange();
    if (npc) this.openDialogue(npc);
  }

  // --- interiors ------------------------------------------------------------

  private enterInterior(def: BuildingDef, returnPos: { x: number; z: number }): void {
    if (this.interior) return;
    this.pending = null;
    this.interiorReturn = { x: returnPos.x, z: returnPos.z };
    this.interior = buildInterior(def);
    this.stage.scene.add(this.interior.group);
    const e = this.interior.entrance;
    this.player.position.set(e.x, 0, e.z);
    this.player.group.position.copy(this.player.position);
    this.player.moveTarget = null;
    this.player.heading = Math.PI; // face the counter
    this.player.group.rotation.y = Math.PI;
    this.state.setPosition(returnPos.x, returnPos.z); // a save resolves outside
    this.toast.show(`Entered ${def.name}. Walk to the counter (E) or the EXIT.`, 'info', 2600);
  }

  private exitInterior(): void {
    if (!this.interior) return;
    this.stage.scene.remove(this.interior.group);
    this.interior = null;
    this.player.position.set(this.interiorReturn.x, 0, this.interiorReturn.z);
    this.player.group.position.copy(this.player.position);
    this.player.moveTarget = null;
    this.cam.distance = 11;
    this.state.setPosition(this.interiorReturn.x, this.interiorReturn.z);
  }

  private nearestNpcInRange(): NPCEntity | null {
    let best: NPCEntity | null = null;
    let bestD = INTERACT_RANGE;
    for (const npc of this.npcs) {
      const d = this.dist(npc.position);
      if (d < bestD) {
        bestD = d;
        best = npc;
      }
    }
    return best;
  }
  private dist(p: THREE.Vector3): number {
    return Math.hypot(p.x - this.player.position.x, p.z - this.player.position.z);
  }
  private distXZ(p: { x: number; z: number }): number {
    return Math.hypot(p.x - this.player.position.x, p.z - this.player.position.z);
  }

  private openDialogue(npc: NPCEntity): void {
    const tree = TREES[npc.spec.tree];
    this.dialogue.open(npc.spec.name, tree, {
      rollCheck: (skill, dc, label) => this.dice.roll(skill, dc, label),
      applyOutcome: (o) => this.applyOutcome(o),
      skillValue: (key) => this.state.skills[key],
      onFight: () => this.startCombat(npc),
      onClose: () => this.state.persist(),
    });
  }

  private openServices(def: BuildingDef): void {
    const ctx: BuildingCtx = {
      state: this.state,
      toast: this.toast,
      dice: this.dice,
      startBurgerShift: (onDone) =>
        this.job.open({
          title: 'Greasy Spoon — Lunch Rush',
          instruction: 'Tap every burger to finish your shift. The more you flip, the more you earn.',
          rounds: 10,
          emoji: '🍔',
          onDone,
        }),
      refreshAppearance: () => this.player.setAppearance(this.state.character.appearance),
    };
    this.buildingPanel.open(def, ctx);
  }

  private applyOutcome(o: CheckOutcome): void {
    if (o.money) this.state.addCash(o.money);
    if (o.skill) this.state.addSkill(o.skill.key, o.skill.amount);
    if (o.health) {
      if (o.health > 0) this.state.heal(o.health);
      else this.state.damage(-o.health);
    }
    if (o.hunger) this.state.eat(o.hunger);
    if (o.giveItem) this.addToPack({ defId: o.giveItem, qty: 1 });
    const kind = o.money && o.money > 0 ? 'good' : o.money && o.money < 0 ? 'bad' : 'info';
    this.toast.show(o.text, kind);
    this.state.persist();
  }

  // --- items ----------------------------------------------------------------

  private useHand(slot: HandSlot): void {
    const stack = this.state.character.hands[slot];
    if (!stack) {
      this.toast.show(`Your ${slot} hand is empty.`, 'info', 1400);
      return;
    }
    const def = getItem(stack.defId);
    if (def.food) {
      this.state.eat(def.food.hunger ?? 0, def.food.health ?? 0);
      stack.qty -= 1;
      if (stack.qty <= 0) this.state.character.hands[slot] = null;
      this.state.emit();
      this.toast.show(`Ate the ${def.name}.`, 'good');
    } else {
      this.toast.show(`You can't use the ${def.name} here.`, 'info', 1600);
    }
  }

  private addToPack(stack: ItemStack): boolean {
    const pack = this.state.character.pack;
    const free = pack.findIndex((s) => s === null);
    if (free === -1) {
      this.toast.show('Your pack is full.', 'bad');
      return false;
    }
    pack[free] = stack;
    this.state.emit();
    return true;
  }

  // --- combat ---------------------------------------------------------------

  private startCombat(npc: NPCEntity): void {
    this.combatOpen = true;
    const c = this.state.character;
    this.combat.open({
      player: {
        name: this.state.account.id.name || 'You',
        str: c.skills.str,
        tool: c.skills.tool,
        hp: Math.ceil(c.health),
        maxHp: c.maxHealth,
      },
      enemy: {
        name: npc.spec.name,
        str: npc.spec.combat.str,
        tool: npc.spec.combat.tool,
        hp: npc.spec.combat.maxHp,
        maxHp: npc.spec.combat.maxHp,
      },
      playerMoves: movesFromHands(c),
      enemyMoves: enemyMoves(npc.spec.combat.weapon),
      enemyEmoji: npc.spec.hostile ? '🦹' : '🧍',
      onEnd: (r) => this.endCombat(npc, r),
    });
  }

  private endCombat(npc: NPCEntity, r: CombatResult): void {
    this.combatOpen = false;
    const c = this.state.character;
    c.health = clamp(r.playerHp, 0, c.maxHealth);
    if (c.health <= 0) c.alive = false;
    this.state.emit();

    if (r.result === 'win') {
      const loot = randInt(15, 45);
      this.state.addCash(loot);
      this.state.addSkill('str', 1);
      this.toast.show(`You beat ${npc.spec.name}! Looted $${loot} and gained +1 STR.`, 'good');
      this.removeNpc(npc);
    } else if (r.result === 'fled') {
      this.toast.show('You slipped away from the fight.', 'info');
    }
    this.state.persist();
  }

  private removeNpc(npc: NPCEntity): void {
    const i = this.npcs.indexOf(npc);
    if (i >= 0) this.npcs.splice(i, 1);
    this.stage.scene.remove(npc.group);
  }

  // --- death / respawn ------------------------------------------------------

  private handleDeath(): void {
    this.creationOpen = true;
    this.pending = null;
    this.toast.show('You died on the streets of LA.', 'bad', 4000);
    this.creation.open(
      { mode: 'respawn', existingId: this.state.account.id, deaths: this.state.account.deaths },
      (res) => {
        if (this.interior) {
          this.stage.scene.remove(this.interior.group);
          this.interior = null;
          this.cam.distance = 11;
        }
        this.state.respawn(res.skills, res.appearance);
        this.player.setAppearance(this.state.character.appearance);
        this.player.position.set(SPAWN.x, 0, SPAWN.z);
        this.player.group.position.copy(this.player.position);
        this.state.setPosition(SPAWN.x, SPAWN.z);
        this.creationOpen = false;
        this.toast.show('A new life begins. Back to the streets.', 'info');
      },
    );
  }

  // --- day / night ----------------------------------------------------------

  private dayFactor(): number {
    const h = this.state.gameHour;
    const ramp = 1.5;
    if (h <= DAY_START_HOUR - ramp || h >= NIGHT_START_HOUR + ramp) return 0;
    if (h >= DAY_START_HOUR + ramp && h <= NIGHT_START_HOUR - ramp) return 1;
    if (h < DAY_START_HOUR + ramp) return clamp((h - (DAY_START_HOUR - ramp)) / (2 * ramp), 0, 1);
    return clamp((NIGHT_START_HOUR + ramp - h) / (2 * ramp), 0, 1);
  }

  // --- networking (optional) ------------------------------------------------

  private setupNet(): void {
    const url = NetClient.serverUrl();
    if (!url) return;
    this.net.onAdd = (p) => this.addRemote(p);
    this.net.onChange = (p) => {
      const r = this.remotes.get(p.id);
      if (r) {
        r.target.set(p.x, 0, p.z);
        r.heading = p.heading;
      } else {
        this.addRemote(p);
      }
    };
    this.net.onRemove = (id) => {
      const r = this.remotes.get(id);
      if (r) {
        this.stage.scene.remove(r.char.group);
        this.remotes.delete(id);
      }
    };
    const c = this.state.character;
    void this.net.connect(url, {
      name: this.state.account.id.name || 'Hobo',
      appearance: c.appearance,
      x: c.pos.x,
      z: c.pos.z,
    });
  }

  private addRemote(p: RemotePlayer): void {
    const char = new Character(p.appearance);
    char.group.add(Character.nameSprite(p.name, '#9fd0ff'));
    char.group.position.set(p.x, 0, p.z);
    this.stage.scene.add(char.group);
    this.remotes.set(p.id, { char, target: new THREE.Vector3(p.x, 0, p.z), heading: p.heading });
  }

  private netTick(dt: number): void {
    if (!this.net.enabled) return;
    this.netSendAccum += dt;
    if (this.netSendAccum > 0.1) {
      this.net.sendMove(this.player.position.x, this.player.position.z, this.player.heading);
      this.netSendAccum = 0;
    }
  }

  private updateRemotes(dt: number): void {
    for (const r of this.remotes.values()) {
      r.char.group.position.lerp(r.target, Math.min(1, dt * 8));
      r.char.group.rotation.y = r.heading;
      const moving = r.char.group.position.distanceToSquared(r.target) > 0.02;
      r.char.update(dt, moving ? 1.6 : 0);
    }
  }
}

// --- combat move derivation -------------------------------------------------

function movesFromHands(c: CharacterData): MoveDef[] {
  const attacks: MoveDef[] = [];
  for (const slot of [c.hands.left, c.hands.right]) {
    const def = maybeItem(slot?.defId);
    if (def?.weapon) {
      def.weapon.moves.forEach((name, i) => {
        attacks.push({
          id: `${def.id}_${name}`,
          label: name,
          base: Math.round(def.weapon!.base * (i === 0 ? 1 : 0.75)),
          usesTool: def.weapon!.usesTool,
        });
      });
    }
  }
  if (attacks.length === 0) {
    attacks.push({ id: 'punch', label: 'punch', base: 6, usesTool: false });
    attacks.push({ id: 'kick', label: 'kick', base: 9, usesTool: false });
  }
  return attacks.slice(0, 2);
}

function enemyMoves(weaponId?: string): MoveDef[] {
  const def = maybeItem(weaponId);
  if (def?.weapon) {
    return def.weapon.moves.slice(0, 2).map((name, i) => ({
      id: `${def.id}_${name}`,
      label: name,
      base: Math.round(def.weapon!.base * (i === 0 ? 1 : 0.75)),
      usesTool: def.weapon!.usesTool,
    }));
  }
  return [
    { id: 'punch', label: 'punch', base: 6, usesTool: false },
    { id: 'kick', label: 'kick', base: 9, usesTool: false },
  ];
}
