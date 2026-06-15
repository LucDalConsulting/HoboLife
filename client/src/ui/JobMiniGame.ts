// Among-Us-style job mini-game. v0.1 ships the burger flip: tap the burgers as
// they appear to complete a shift. Relaxed by design — you can't fail, you just
// earn more for finishing. Returns how many you hit.

import { randFloat } from '../core/rng';
import { clear, el } from './dom';

interface OpenOpts {
  title: string;
  instruction: string;
  rounds: number;
  emoji?: string;
  onDone: (hits: number) => void;
  onCancel?: () => void;
}

export class JobMiniGame {
  private overlay: HTMLElement;
  private panel: HTMLElement;
  private open_ = false;
  private hits = 0;
  private opts?: OpenOpts;

  constructor() {
    this.panel = el('div', { class: 'panel minigame' });
    this.overlay = el('div', { class: 'overlay' }, [this.panel]);
    this.overlay.style.display = 'none';
    this.overlay.style.zIndex = '65';
    document.body.append(this.overlay);
  }

  isOpen(): boolean {
    return this.open_;
  }

  open(opts: OpenOpts): void {
    this.opts = opts;
    this.hits = 0;
    this.open_ = true;
    this.render();
    this.overlay.style.display = 'flex';
    this.spawn();
  }

  private render(): void {
    if (!this.opts) return;
    clear(this.panel);
    this.progress = el('div', { style: 'width:0%' });
    this.area = el('div', { class: 'burger-area' });
    const quit = el('button', {}, ['Clock out early']);
    quit.addEventListener('click', () => this.finish(true));
    this.panel.append(
      el('h1', {}, [this.opts.title]),
      el('p', {}, [this.opts.instruction]),
      el('div', { class: 'progress' }, [this.progress]),
      this.area,
      el('div', { class: 'row spread' }, [
        el('span', { style: 'color:var(--muted)' }, [`${this.hits} / ${this.opts.rounds}`]),
        quit,
      ]),
    );
  }

  private progress!: HTMLElement;
  private area!: HTMLElement;

  private spawn(): void {
    if (!this.opts) return;
    const btn = el('button', { class: 'burger-btn' }, [this.opts.emoji ?? '🍔']) as HTMLButtonElement;
    const place = () => {
      btn.style.left = randFloat(6, 78) + '%';
      btn.style.top = randFloat(6, 70) + '%';
    };
    place();
    btn.addEventListener('click', () => {
      this.hits++;
      btn.remove();
      this.update();
      if (this.hits >= this.opts!.rounds) this.finish(false);
      else this.spawn();
    });
    this.area.append(btn);
  }

  private update(): void {
    if (!this.opts) return;
    this.progress.style.width = (this.hits / this.opts.rounds) * 100 + '%';
    const counter = this.panel.querySelector('.row.spread span');
    if (counter) counter.textContent = `${this.hits} / ${this.opts.rounds}`;
  }

  private finish(cancelled: boolean): void {
    if (!this.open_) return;
    this.open_ = false;
    this.overlay.style.display = 'none';
    const o = this.opts!;
    if (cancelled && this.hits === 0 && o.onCancel) o.onCancel();
    else o.onDone(this.hits);
  }
}
