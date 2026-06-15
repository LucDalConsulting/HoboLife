// What happens when you press E at a building's door. Each building kind exposes
// its own actions: study/train to raise skills, work a shift, heal, bank, gamble.

import { randInt } from '../core/rng';
import type { BuildingDef } from '../data/city';
import type { GameState } from '../state/GameState';
import type { DiceOverlay } from './DiceOverlay';
import type { Toast } from './Toast';
import { clear, el } from './dom';

export interface BuildingCtx {
  state: GameState;
  toast: Toast;
  dice: DiceOverlay;
  startBurgerShift: (onDone: (hits: number) => void) => void;
  refreshAppearance: () => void;
}

export class BuildingPanel {
  private overlay: HTMLElement;
  private panel: HTMLElement;
  private open_ = false;
  private def?: BuildingDef;
  private ctx?: BuildingCtx;

  constructor() {
    this.panel = el('div', { class: 'panel' });
    this.overlay = el('div', { class: 'overlay' }, [this.panel]);
    this.overlay.style.display = 'none';
    this.overlay.addEventListener('click', (e) => {
      if (e.target === this.overlay) this.close();
    });
    document.body.append(this.overlay);
  }

  isOpen(): boolean {
    return this.open_;
  }

  open(def: BuildingDef, ctx: BuildingCtx): void {
    this.def = def;
    this.ctx = ctx;
    this.open_ = true;
    this.render();
    this.overlay.style.display = 'flex';
  }

  close(): void {
    this.open_ = false;
    this.overlay.style.display = 'none';
    this.ctx?.state.persist();
  }

  private render(): void {
    if (!this.def || !this.ctx) return;
    const { state } = this.ctx;
    clear(this.panel);
    this.panel.append(
      el('div', { class: 'row spread' }, [el('h1', {}, [this.def.name]), this.closeBtn()]),
      el('div', { class: 'row spread', style: 'color:var(--muted);font-size:13px;margin-bottom:6px' }, [
        el('span', {}, [state.isDay() ? '🟢 Open (daytime)' : '🔴 Closed at night']),
        el('span', {}, [`Cash: $${state.character.cash.toLocaleString()} · Bank: $${state.account.bank.toLocaleString()}`]),
      ]),
    );

    const actions = el('div', { class: 'col' });
    switch (this.def.kind) {
      case 'university': this.university(actions); break;
      case 'gym': this.gym(actions); break;
      case 'diner': this.diner(actions); break;
      case 'hospital': this.hospital(actions); break;
      case 'bank': this.bank(actions); break;
      case 'casino': this.casino(actions); break;
      case 'clothing': this.clothing(actions); break;
      default: this.comingSoon(actions); break;
    }
    this.panel.append(actions);
  }

  private closeBtn(): HTMLElement {
    const b = el('button', {}, ['✕']);
    b.addEventListener('click', () => this.close());
    return b;
  }

  private action(label: string, sub: string, fn: () => void, disabled = false): HTMLElement {
    const btn = el('button', { style: 'text-align:left; padding:12px 14px' }, [
      el('div', { style: 'font-weight:600' }, [label]),
      el('div', { style: 'font-size:12px;color:var(--muted)' }, [sub]),
    ]) as HTMLButtonElement;
    btn.disabled = disabled;
    btn.addEventListener('click', fn);
    return btn;
  }

  // --- building kinds -------------------------------------------------------

  private university(out: HTMLElement): void {
    const s = this.ctx!.state;
    out.append(
      el('p', {}, [`Study to raise Intelligence. Current INT: ${s.skills.int}.`]),
      this.action('📚 Study (2 hrs)', 'Raises Intelligence', () => {
        if (!s.isDay()) return this.ctx!.toast.show('The university is closed at night.', 'bad');
        const g = randInt(2, 4);
        s.addSkill('int', g);
        s.skipHours(2);
        this.ctx!.toast.show(`You hit the books. +${g} INT`, 'good');
        this.render();
      }, !s.isDay()),
    );
  }

  private gym(out: HTMLElement): void {
    const s = this.ctx!.state;
    out.append(
      el('p', {}, [`Lift to raise Strength (and max health). Current STR: ${s.skills.str}.`]),
      this.action('🏋️ Train (2 hrs)', 'Raises Strength', () => {
        if (!s.isDay()) return this.ctx!.toast.show('The gym is closed at night.', 'bad');
        const g = randInt(2, 4);
        s.addSkill('str', g);
        s.skipHours(2);
        this.ctx!.toast.show(`You pump iron. +${g} STR`, 'good');
        this.render();
      }, !s.isDay()),
    );
  }

  private diner(out: HTMLElement): void {
    const s = this.ctx!.state;
    out.append(
      el('p', {}, ['Flip burgers for a shift, or grab a bite.']),
      this.action('🍔 Work a shift', 'Burger-flip mini-game · pays more the more you flip', () => {
        if (!s.isDay()) return this.ctx!.toast.show('The diner only hires during the day.', 'bad');
        this.overlay.style.display = 'none';
        this.ctx!.startBurgerShift((hits) => {
          const pay = hits * 8 + Math.floor(s.skills.tool / 5);
          s.addCash(pay);
          s.skipHours(8);
          this.ctx!.toast.show(`Shift done — flipped ${hits}. +$${pay}`, 'good');
          this.overlay.style.display = 'flex';
          this.render();
        });
      }, !s.isDay()),
      this.action('🍔 Eat a cheeseburger ($6)', '+35 hunger', () => {
        if (!s.spendCash(6)) return this.ctx!.toast.show("You can't afford that.", 'bad');
        s.eat(35);
        this.ctx!.toast.show('Delicious. +35 hunger', 'good');
        this.render();
      }),
    );
  }

  private hospital(out: HTMLElement): void {
    const s = this.ctx!.state;
    const c = s.character;
    const missing = Math.ceil(c.maxHealth - c.health);
    const cost = missing; // $1 per HP
    out.append(
      el('p', {}, [`Health: ${Math.ceil(c.health)} / ${c.maxHealth}.`]),
      this.action(
        missing > 0 ? `🏥 Get patched up ($${cost})` : '🏥 Full health',
        missing > 0 ? `Restores ${missing} HP` : 'Nothing to heal',
        () => {
          if (missing <= 0) return;
          if (!s.spendCash(cost)) return this.ctx!.toast.show("You can't afford treatment.", 'bad');
          s.heal(missing);
          this.ctx!.toast.show('Patched up. Full health.', 'good');
          this.render();
        },
        missing <= 0,
      ),
    );
  }

  private bank(out: HTMLElement): void {
    const s = this.ctx!.state;
    out.append(
      el('p', {}, ['Money in the bank survives death. (Loans & credit coming in v0.2.)']),
      this.action('🏦 Deposit all cash', `Move $${s.character.cash} to the bank`, () => {
        const amt = s.character.cash;
        if (amt <= 0) return this.ctx!.toast.show('No cash to deposit.', 'bad');
        s.deposit(amt);
        this.ctx!.toast.show(`Deposited $${amt}.`, 'good');
        this.render();
      }, s.character.cash <= 0),
      this.action('🏧 Withdraw $50', `Bank: $${s.account.bank}`, () => {
        if (!s.withdraw(Math.min(50, s.account.bank))) return this.ctx!.toast.show('Nothing to withdraw.', 'bad');
        this.ctx!.toast.show('Withdrew cash.', 'good');
        this.render();
      }, s.account.bank <= 0),
    );
  }

  private casino(out: HTMLElement): void {
    const s = this.ctx!.state;
    out.append(
      el('p', {}, ['Bet $10 on a hand of poker. Your Charisma sways the dealer (DC 50).']),
      this.action('🃏 Play a hand ($10)', 'Win $25 on success', async () => {
        if (!s.spendCash(10)) return this.ctx!.toast.show("You're broke.", 'bad');
        const res = await this.ctx!.dice.roll(s.skills.cha, 50, 'Poker (CHA)');
        if (res.success) {
          s.addCash(25);
          this.ctx!.toast.show('You read the table perfectly. +$25', 'good');
        } else {
          this.ctx!.toast.show('The house takes it. -$10', 'bad');
        }
        s.skipHours(1);
        this.render();
      }, s.character.cash < 10),
    );
  }

  private clothing(out: HTMLElement): void {
    const s = this.ctx!.state;
    out.append(
      el('p', {}, ['Look less like a hobo. (Full wardrobe system in v0.2.)']),
      this.action('👕 Buy a designer shirt ($25)', 'Updates your look', () => {
        if (!s.spendCash(25)) return this.ctx!.toast.show("You can't afford it.", 'bad');
        s.character.appearance.shirt = '#f4d35e';
        this.ctx!.refreshAppearance();
        s.emit();
        this.ctx!.toast.show('Sharp. New shirt equipped.', 'good');
        this.render();
      }, s.character.cash < 25),
    );
  }

  private comingSoon(out: HTMLElement): void {
    out.append(
      el('p', {}, [
        `${SLOGAN[this.def!.kind] ?? 'This place'} opens for business in a future update. ` +
          'For now, peek inside and keep building your skills and bankroll elsewhere.',
      ]),
      this.action('🚧 Coming in v0.2', 'Cars, housing, lawyers, insurance & more', () => this.close()),
    );
  }
}

const SLOGAN: Partial<Record<BuildingDef['kind'], string>> = {
  cardealer: 'The car lot',
  realtor: 'The realty office',
  pawn: 'The pawn shop',
};
