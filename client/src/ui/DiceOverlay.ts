// The top-right d10 that rolls whenever you attempt a skill-gated action.
// Shows the shuffle, settles on the real face, displays the maths, then resolves.

import { resolveSkillCheck, describeCheck, type SkillCheckResult } from '../core/skillcheck';
import { el } from './dom';

export class DiceOverlay {
  private root: HTMLElement;
  private die: HTMLElement;
  private cap: HTMLElement;

  constructor() {
    this.die = el('div', { class: 'die' }, ['?']);
    this.cap = el('div', { class: 'cap' });
    this.root = el('div', { id: 'dice' }, [this.die, this.cap]);
    document.body.append(this.root);
  }

  /**
   * Roll against a difficulty. Resolves with the full result after the animation
   * (kept under ~2.5s total, well under the 5s budget).
   */
  roll(skill: number, required: number, label = ''): Promise<SkillCheckResult> {
    const result = resolveSkillCheck(skill, required);
    this.root.style.display = 'block';
    this.die.className = 'die rolling';
    this.cap.innerHTML = label ? `<div>${label}</div><div class="res">rolling…</div>` : '<div class="res">rolling…</div>';

    return new Promise((resolve) => {
      const shuffle = setInterval(() => {
        this.die.textContent = String(1 + Math.floor(Math.random() * 10));
      }, 70);

      setTimeout(() => {
        clearInterval(shuffle);
        this.die.textContent = String(result.roll);
        this.die.className = 'die' + (result.critical ? ' crit' : result.autoFail ? ' fail' : '');
        const verdict = result.success
          ? `<span style="color:#7be0a8">SUCCESS</span>`
          : `<span style="color:#ff8d76">FAIL</span>`;
        this.cap.innerHTML =
          (label ? `<div>${label}</div>` : '') +
          `<div style="margin:2px 0">${describeCheck(result).replace(/→.*/, '')}</div>` +
          `<div class="res">${verdict}</div>`;

        setTimeout(() => {
          this.root.style.display = 'none';
          resolve(result);
        }, 1100);
      }, 1100);
    });
  }
}
