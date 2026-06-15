// Minecraft-style inventory: two hand slots plus a carried grid. Click a grid
// item to equip it to a free hand; click a hand item to stow it back.

import type { GameState } from '../state/GameState';
import type { ItemStack } from '../core/types';
import { getItem, maybeItem } from '../data/items';
import { clear, el } from './dom';

export class Inventory {
  private overlay: HTMLElement;
  private body: HTMLElement;
  private state?: GameState;
  private open_ = false;

  constructor() {
    this.body = el('div', { class: 'panel' });
    this.overlay = el('div', { class: 'overlay' }, [this.body]);
    this.overlay.style.display = 'none';
    this.overlay.addEventListener('click', (e) => {
      if (e.target === this.overlay) this.close();
    });
    document.body.append(this.overlay);
  }

  isOpen(): boolean {
    return this.open_;
  }

  toggle(state: GameState): void {
    if (this.open_) this.close();
    else this.open(state);
  }

  open(state: GameState): void {
    this.state = state;
    this.open_ = true;
    this.render();
    this.overlay.style.display = 'flex';
  }

  close(): void {
    this.open_ = false;
    this.overlay.style.display = 'none';
    this.state?.persist();
  }

  private render(): void {
    if (!this.state) return;
    const c = this.state.character;
    clear(this.body);
    this.body.append(
      el('div', { class: 'row spread' }, [el('h1', {}, ['Inventory']), this.closeBtn()]),
      el('h2', {}, ['Hands']),
    );

    const handsGrid = el('div', { class: 'grid', style: 'grid-template-columns: repeat(2, 52px)' });
    handsGrid.append(this.handCell('left', c.hands.left));
    handsGrid.append(this.handCell('right', c.hands.right));
    this.body.append(handsGrid);

    this.body.append(el('h2', {}, ['Backpack']));
    const grid = el('div', { class: 'grid' });
    c.pack.forEach((stack, i) => grid.append(this.packCell(i, stack)));
    this.body.append(grid);

    this.body.append(
      el('p', { style: 'font-size:12px;margin-top:10px' }, [
        'Click a backpack item to equip it · click a hand item to stow it · use items with F/G or the HUD slots.',
      ]),
    );
  }

  private closeBtn(): HTMLElement {
    const b = el('button', {}, ['✕']);
    b.addEventListener('click', () => this.close());
    return b;
  }

  private handCell(slot: 'left' | 'right', stack: ItemStack | null): HTMLElement {
    const def = maybeItem(stack?.defId);
    const cell = el('div', { class: 'cell hands' }, [def ? def.icon : '']);
    cell.title = def ? `${def.name} (${slot} hand) — click to stow` : `${slot} hand (empty)`;
    if (stack) cell.append(el('span', { class: 'qty' }, [stack.qty > 1 ? `x${stack.qty}` : '']));
    cell.addEventListener('click', () => this.stow(slot));
    return cell;
  }

  private packCell(index: number, stack: ItemStack | null): HTMLElement {
    const def = maybeItem(stack?.defId);
    const cell = el('div', { class: 'cell' }, [def ? def.icon : '']);
    cell.title = def ? `${def.name} — click to equip` : 'empty';
    if (stack) cell.append(el('span', { class: 'qty' }, [stack.qty > 1 ? `x${stack.qty}` : '']));
    cell.addEventListener('click', () => this.equip(index));
    return cell;
  }

  private equip(index: number): void {
    if (!this.state) return;
    const c = this.state.character;
    const stack = c.pack[index];
    if (!stack) return;
    const def = getItem(stack.defId);

    if (def.twoHanded) {
      // Stow whatever is in hands, then hold the two-hander in the left hand.
      this.returnToPack(c.hands.left);
      this.returnToPack(c.hands.right);
      c.hands.left = stack;
      c.hands.right = null;
    } else if (!c.hands.left) {
      c.hands.left = stack;
    } else if (!c.hands.right) {
      c.hands.right = stack;
    } else {
      // Both full — swap with left.
      const old = c.hands.left;
      c.hands.left = stack;
      c.pack[index] = old;
      this.afterChange();
      return;
    }
    c.pack[index] = null;
    this.afterChange();
  }

  private stow(slot: 'left' | 'right'): void {
    if (!this.state) return;
    const c = this.state.character;
    const stack = c.hands[slot];
    if (!stack) return;
    if (this.returnToPack(stack)) c.hands[slot] = null;
    this.afterChange();
  }

  private returnToPack(stack: ItemStack | null): boolean {
    if (!stack || !this.state) return true;
    const c = this.state.character;
    const free = c.pack.findIndex((s) => s === null);
    if (free === -1) return false;
    c.pack[free] = stack;
    return true;
  }

  private afterChange(): void {
    this.state?.emit();
    this.render();
  }
}
