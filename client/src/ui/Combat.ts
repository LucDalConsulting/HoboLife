// Pokémon-style turn-based battle. Both HP bars are shown; your four buttons are
// two attacks (derived from your hands), Guard, and Run. Damage and escape come
// from combatmath. Combat is authoritative over local HP and reports the result.

import {
  resolveAttack,
  resolveRun,
  type Combatant,
  type MoveDef,
} from '../core/combatmath';
import { clear, el } from './dom';

export interface CombatResult {
  result: 'win' | 'lose' | 'fled';
  playerHp: number;
  enemyName: string;
}

interface OpenOpts {
  player: Combatant;
  enemy: Combatant;
  playerMoves: MoveDef[];
  enemyMoves: MoveDef[];
  enemyEmoji?: string;
  onEnd: (r: CombatResult) => void;
}

const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

export class Combat {
  private root: HTMLElement;
  private enemyHpBar: HTMLElement;
  private playerHpBar: HTMLElement;
  private enemyName: HTMLElement;
  private playerName: HTMLElement;
  private enemyHpText: HTMLElement;
  private playerHpText: HTMLElement;
  private enemySprite: HTMLElement;
  private moves: HTMLElement;
  private log: HTMLElement;

  private opts?: OpenOpts;
  private player!: Combatant;
  private enemy!: Combatant;
  private guarding = false;
  private over = false;
  private busy = false;

  constructor() {
    this.enemyName = el('span');
    this.enemyHpText = el('span');
    this.enemyHpBar = el('div');
    this.playerName = el('span');
    this.playerHpText = el('span');
    this.playerHpBar = el('div');
    this.enemySprite = el('div', { class: 'fighter enemy-sprite' }, ['🧟']);
    this.moves = el('div', { class: 'moves' });
    this.log = el('div', { class: 'log' });

    const enemyBox = el('div', { class: 'enemy-box' }, [
      el('div', { class: 'hpbox' }, [
        el('div', { class: 'nm' }, [this.enemyName, this.enemyHpText]),
        el('div', { class: 'hpbar' }, [this.enemyHpBar]),
      ]),
    ]);
    const playerBox = el('div', { class: 'player-box' }, [
      el('div', { class: 'hpbox' }, [
        el('div', { class: 'nm' }, [this.playerName, this.playerHpText]),
        el('div', { class: 'hpbar' }, [this.playerHpBar]),
      ]),
    ]);

    const arena = el('div', { class: 'arena' }, [
      this.enemySprite,
      el('div', { class: 'fighter player-sprite' }, ['🧍']),
      enemyBox,
      playerBox,
    ]);
    const menu = el('div', { class: 'menu' }, [this.moves, this.log]);
    this.root = el('div', { id: 'combat' }, [arena, menu]);
    document.body.append(this.root);
  }

  open(opts: OpenOpts): void {
    this.opts = opts;
    this.player = { ...opts.player };
    this.enemy = { ...opts.enemy };
    this.guarding = false;
    this.over = false;
    this.busy = false;
    this.enemyName.textContent = this.enemy.name;
    this.playerName.textContent = this.player.name;
    this.enemySprite.textContent = opts.enemyEmoji ?? '🧟';
    clear(this.log);
    this.write(`A fight breaks out with ${this.enemy.name}!`);
    this.renderMoves();
    this.updateBars();
    this.root.style.display = 'block';
  }

  private renderMoves(): void {
    clear(this.moves);
    this.opts!.playerMoves.forEach((m) => this.moves.append(this.moveBtn(cap(m.label), () => this.playerAttack(m))));
    this.moves.append(this.moveBtn('🛡 Guard', () => this.playerGuard()));
    this.moves.append(this.moveBtn('🏃 Run', () => this.playerRun()));
  }

  private moveBtn(label: string, fn: () => void): HTMLButtonElement {
    const b = el('button', {}, [label]) as HTMLButtonElement;
    b.addEventListener('click', () => {
      if (!this.busy && !this.over) fn();
    });
    return b;
  }

  private async playerAttack(move: MoveDef): Promise<void> {
    this.busy = true;
    const r = resolveAttack(move, this.player);
    if (r.miss) this.write(`You ${move.label}… and whiff! (rolled a 1)`);
    else this.write(`You ${move.label}${r.crit ? ' — CRITICAL' : ''} for ${r.damage}.`);
    this.enemy.hp = Math.max(0, this.enemy.hp - r.damage);
    this.updateBars();
    if (this.enemy.hp <= 0) return this.end('win');
    await sleep(650);
    await this.enemyTurn();
    this.busy = false;
  }

  private async playerGuard(): Promise<void> {
    this.busy = true;
    this.guarding = true;
    this.write('You brace for the next hit. 🛡');
    await sleep(450);
    await this.enemyTurn();
    this.busy = false;
  }

  private async playerRun(): Promise<void> {
    this.busy = true;
    const r = resolveRun(this.player, this.enemy);
    if (r.success) {
      this.write(`You slip away! (rolled ${r.roll})`);
      await sleep(700);
      return this.end('fled');
    }
    this.write(`You try to flee but can't break away. (rolled ${r.roll})`);
    await sleep(500);
    await this.enemyTurn();
    this.busy = false;
  }

  private async enemyTurn(): Promise<void> {
    if (this.over) return;
    const moveSet = this.opts!.enemyMoves;
    const move = moveSet[Math.floor(Math.random() * moveSet.length)];
    const r = resolveAttack(move, this.enemy);
    let dmg = r.damage;
    if (this.guarding) {
      dmg = Math.round(dmg * 0.5);
      this.guarding = false;
    }
    if (r.miss) this.write(`${this.enemy.name} ${move.label}s and misses!`);
    else this.write(`${this.enemy.name} ${move.label}s you${r.crit ? ' — CRITICAL' : ''} for ${dmg}.`);
    this.player.hp = Math.max(0, this.player.hp - dmg);
    this.updateBars();
    if (this.player.hp <= 0) this.end('lose');
  }

  private updateBars(): void {
    const e = Math.max(0, (this.enemy.hp / this.enemy.maxHp) * 100);
    const p = Math.max(0, (this.player.hp / this.player.maxHp) * 100);
    this.enemyHpBar.style.width = e + '%';
    this.playerHpBar.style.width = p + '%';
    this.enemyHpBar.style.background = e < 30 ? 'var(--bad)' : 'var(--good)';
    this.playerHpBar.style.background = p < 30 ? 'var(--bad)' : 'var(--good)';
    this.enemyHpText.textContent = `${Math.ceil(this.enemy.hp)}/${this.enemy.maxHp}`;
    this.playerHpText.textContent = `${Math.ceil(this.player.hp)}/${this.player.maxHp}`;
  }

  private write(line: string): void {
    this.log.append(el('div', {}, [line]));
    this.log.scrollTop = this.log.scrollHeight;
  }

  private end(result: CombatResult['result']): void {
    if (this.over) return;
    this.over = true;
    const msg = result === 'win' ? `${this.enemy.name} goes down. You win!` : result === 'lose' ? 'You black out…' : 'You got away.';
    this.write(msg);
    setTimeout(() => {
      this.root.style.display = 'none';
      this.opts!.onEnd({ result, playerHp: Math.ceil(this.player.hp), enemyName: this.enemy.name });
    }, 1100);
  }
}

function cap(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
