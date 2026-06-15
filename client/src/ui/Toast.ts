import { el } from './dom';

export type ToastKind = 'info' | 'good' | 'bad';

export class Toast {
  private root: HTMLElement;

  constructor() {
    this.root = el('div', { id: 'toasts' });
    document.body.append(this.root);
  }

  show(text: string, kind: ToastKind = 'info', ms = 2600): void {
    const t = el('div', { class: `toast ${kind === 'info' ? '' : kind}` }, [text]);
    this.root.append(t);
    setTimeout(() => {
      t.style.transition = 'opacity 0.3s';
      t.style.opacity = '0';
      setTimeout(() => t.remove(), 320);
    }, ms);
  }
}
