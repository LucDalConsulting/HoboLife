// Branching dialogue UI. Options either navigate the tree, leave, start a fight,
// or run a skill check whose success/fail outcome is applied by the host (Game).

import type { SkillKey } from '../core/constants';
import type { SkillCheckResult } from '../core/skillcheck';
import type { CheckOutcome, DialogueOption, DialogueTree } from '../data/dialogue';
import { clear, el } from './dom';

export interface DialogueHandlers {
  rollCheck(skill: number, dc: number, label: string): Promise<SkillCheckResult>;
  applyOutcome(o: CheckOutcome): void;
  skillValue(key: SkillKey): number;
  onFight(): void;
  onClose(): void;
}

export class Dialogue {
  private root: HTMLElement;
  private who: HTMLElement;
  private say: HTMLElement;
  private opts: HTMLElement;
  private tree?: DialogueTree;
  private nodeId = '';
  private handlers?: DialogueHandlers;
  private busy = false;
  private open_ = false;

  constructor() {
    this.who = el('div', { class: 'who' });
    this.say = el('div', { class: 'say' });
    this.opts = el('div', { class: 'opts' });
    this.root = el('div', { id: 'dialogue' }, [this.who, this.say, this.opts]);
    document.body.append(this.root);
    document.addEventListener('keydown', this.onKey);
  }

  isOpen(): boolean {
    return this.open_;
  }

  open(npcName: string, tree: DialogueTree, handlers: DialogueHandlers): void {
    this.tree = tree;
    this.handlers = handlers;
    this.nodeId = tree.root;
    this.who.textContent = npcName;
    this.busy = false;
    this.open_ = true;
    this.root.style.display = 'block';
    this.renderNode();
  }

  close(): void {
    this.open_ = false;
    this.root.style.display = 'none';
  }

  private renderNode(): void {
    if (!this.tree) return;
    const node = this.tree.nodes[this.nodeId];
    this.say.textContent = node.text;
    clear(this.opts);
    node.options.forEach((opt, i) => {
      const btn = el('button', { class: 'opt' }, [
        el('span', { class: 'num' }, [`${i + 1}`]),
        el('span', {}, [opt.label]),
      ]);
      btn.addEventListener('click', () => this.choose(i));
      this.opts.append(btn);
    });
  }

  private onKey = (e: KeyboardEvent) => {
    if (!this.open_ || this.busy) return;
    const map: Record<string, number> = {
      Digit1: 0, Digit2: 1, Digit3: 2, Digit4: 3,
      Numpad1: 0, Numpad2: 1, Numpad3: 2, Numpad4: 3,
    };
    if (e.code in map) {
      e.preventDefault();
      this.choose(map[e.code]);
    } else if (e.code === 'Escape') {
      this.leave();
    }
  };

  private leave(): void {
    this.close();
    this.handlers?.onClose();
  }

  private async choose(index: number): Promise<void> {
    if (!this.tree || !this.handlers || this.busy) return;
    const node = this.tree.nodes[this.nodeId];
    const opt: DialogueOption | undefined = node.options[index];
    if (!opt) return;
    const eff = opt.effect;

    if (eff.type === 'leave') {
      this.leave();
      return;
    }
    if (eff.type === 'fight') {
      this.close();
      this.handlers.onFight();
      return;
    }
    if (eff.type === 'goto') {
      this.nodeId = eff.node;
      this.renderNode();
      return;
    }
    // skill check
    this.busy = true;
    const skill = this.handlers.skillValue(eff.skill);
    const res = await this.handlers.rollCheck(skill, eff.dc, opt.label);
    const outcome: CheckOutcome = res.success ? eff.success : eff.fail;
    this.handlers.applyOutcome(outcome);
    this.busy = false;
    if (outcome.end) {
      this.leave();
    } else {
      this.renderNode();
    }
  }
}
