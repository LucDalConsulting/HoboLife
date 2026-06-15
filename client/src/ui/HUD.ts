// The always-on heads-up display: survival bars, money, the four skills, the
// in-game clock with day/night indicator, and the two hand slots.

import {
  HUNGER_MAX,
  SKILL_KEYS,
  SKILL_LABELS,
  SKILL_MAX,
  SKILL_SHORT,
  VERSION,
  type SkillKey,
} from '../core/constants';
import { maybeItem } from '../data/items';
import type { GameState } from '../state/GameState';
import type { HandSlot } from '../core/types';
import { el } from './dom';

const SKILL_COLOR: Record<SkillKey, string> = {
  int: 'var(--int)',
  cha: 'var(--cha)',
  str: 'var(--str)',
  tool: 'var(--tool)',
};

export class HUD {
  root: HTMLElement;
  onUseHand: (slot: HandSlot) => void = () => {};

  private hpFill: HTMLElement;
  private hpText: HTMLElement;
  private hungerFill: HTMLElement;
  private hungerText: HTMLElement;
  private cashEl: HTMLElement;
  private clockBig: HTMLElement;
  private clockDay: HTMLElement;
  private clockSun: HTMLElement;
  private skillEls: Record<SkillKey, HTMLElement> = {} as Record<SkillKey, HTMLElement>;
  private leftSlot: HTMLElement;
  private rightSlot: HTMLElement;
  private prompt: HTMLElement;

  constructor() {
    // --- top-left: survival + skills ---
    this.hpFill = el('div', { style: 'background: var(--hp)' });
    this.hpText = el('span');
    this.hungerFill = el('div', { style: 'background: var(--hunger)' });
    this.hungerText = el('span');

    const skillsGrid = el('div', { class: 'skills-mini' });
    for (const k of SKILL_KEYS) {
      const v = el('b', {}, ['0']);
      this.skillEls[k] = v;
      skillsGrid.append(
        el('div', { class: 's' }, [el('small', {}, [SKILL_SHORT[k] + ' ']), v]) as Node,
      );
      v.style.color = SKILL_COLOR[k];
    }

    const card = el('div', { class: 'hud-card' }, [
      el('div', { class: 'statline' }, [el('span', {}, ['❤ Health']), this.hpText]),
      el('div', { class: 'statbar' }, [this.hpFill]),
      el('div', { class: 'statline' }, [el('span', {}, ['🍔 Hunger']), this.hungerText]),
      el('div', { class: 'statbar' }, [this.hungerFill]),
      skillsGrid,
    ]);
    const topLeft = el('div', { class: 'hud-topleft' }, [card]);

    // --- top-right: clock ---
    this.clockBig = el('div', { class: 'big' }, ['9:00 AM']);
    this.clockDay = el('div', { class: 'day' }, ['Day 1']);
    this.clockSun = el('span', { class: 'sun' }, ['☀️']);
    const clock = el('div', { class: 'clock' }, [
      el('div', { class: 'row spread' }, [this.clockBig, this.clockSun]),
      this.clockDay,
    ]);
    const topRight = el('div', { class: 'hud-topright' }, [clock]);

    // --- bottom: hands + cash ---
    this.cashEl = el('div', { class: 'cash' }, ['$0']);
    this.leftSlot = this.makeSlot('left', 'F');
    this.rightSlot = this.makeSlot('right', 'G');
    const hands = el('div', { class: 'hands' }, [this.leftSlot, this.rightSlot]);
    const bottom = el('div', { class: 'hud-bottom' }, [this.cashEl, hands]);

    const help = el('div', { class: 'hud-help' }, [
      'WASD / click = move · drag = camera · wheel = zoom',
      el('br'),
      'Q talk · E enter · I inventory · F/G use hands',
    ]);
    const version = el('div', { class: 'hud-version' }, [`HoboLife v${VERSION}`]);

    this.prompt = el('div', { class: 'interact-prompt' });
    this.prompt.style.display = 'none';

    this.root = el('div', { id: 'hud' }, [topLeft, topRight, bottom, help, version]);
    document.body.append(this.root);
    document.body.append(this.prompt);
  }

  private makeSlot(slot: HandSlot, key: string): HTMLElement {
    const node = el('div', { class: 'handslot pointer' }, [
      el('span', { class: 'icon' }),
      el('span', { class: 'key' }, [key]),
    ]);
    node.title = `${slot} hand — click or press ${key} to use`;
    node.addEventListener('click', () => this.onUseHand(slot));
    return node;
  }

  setPrompt(text: string | null): void {
    if (!text) {
      this.prompt.style.display = 'none';
    } else {
      this.prompt.innerHTML = text;
      this.prompt.style.display = 'block';
    }
  }

  update(state: GameState): void {
    const c = state.character;
    const hpPct = Math.max(0, (c.health / c.maxHealth) * 100);
    this.hpFill.style.width = hpPct + '%';
    this.hpText.textContent = `${Math.ceil(c.health)} / ${c.maxHealth}`;

    const huPct = Math.max(0, (c.hunger / HUNGER_MAX) * 100);
    this.hungerFill.style.width = huPct + '%';
    this.hungerText.textContent = `${Math.ceil(c.hunger)} / ${HUNGER_MAX}`;
    this.hungerFill.style.background = c.hunger < 20 ? 'var(--bad)' : 'var(--hunger)';

    this.cashEl.textContent = '$' + c.cash.toLocaleString();

    for (const k of SKILL_KEYS) {
      this.skillEls[k].textContent = `${c.skills[k]}`;
      this.skillEls[k].title = `${SKILL_LABELS[k]} ${c.skills[k]}/${SKILL_MAX}`;
    }

    // clock
    const h = Math.floor(state.gameHour);
    const m = Math.floor((state.gameHour - h) * 60);
    const ampm = h < 12 ? 'AM' : 'PM';
    const hr12 = ((h + 11) % 12) + 1;
    this.clockBig.textContent = `${hr12}:${String(m).padStart(2, '0')} ${ampm}`;
    this.clockDay.textContent = `Day ${state.dayNumber}`;
    this.clockSun.textContent = state.isDay() ? '☀️' : '🌙';

    this.paintSlot(this.leftSlot, c.hands.left?.defId);
    this.paintSlot(this.rightSlot, c.hands.right?.defId);
  }

  private paintSlot(node: HTMLElement, defId?: string): void {
    const icon = node.querySelector('.icon') as HTMLElement;
    const def = maybeItem(defId);
    icon.textContent = def ? def.icon : '';
    node.title = def ? def.name : 'empty hand';
  }
}
