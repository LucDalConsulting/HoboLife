// Character creation (also reused on death to rebuild the mortal character).
// New players also mint a permanent ID card (name, DOB, SSN).

import {
  CREATION_POINTS,
  SKILL_KEYS,
  SKILL_LABELS,
  type SkillKey,
} from '../core/constants';
import type { Appearance, IDCard, Skills } from '../core/types';
import { defaultAppearance, generateSSN } from '../state/persistence';
import { clear, el } from './dom';

export interface CreationResult {
  id: IDCard;
  skills: Skills;
  appearance: Appearance;
}

interface OpenOpts {
  mode: 'new' | 'respawn';
  existingId?: IDCard;
  deaths?: number;
}

const SKINS = ['#f1c8a0', '#e8b98a', '#d49a6a', '#a9744f', '#7a4f33', '#5a3825'];
const HAIRS = ['#1c1410', '#3b2412', '#6b4423', '#b07b3a', '#d9d2c5', '#c0392b'];
const SHIRTS = ['#6b8f3a', '#3a6ea5', '#c0392b', '#8e44ad', '#e67e22', '#16a085'];
const PANTS = ['#2c3e50', '#34495e', '#4a3b2a', '#1f2a36', '#3d3d3d', '#5b4636'];

export class CharacterCreation {
  private overlay: HTMLElement;
  private skills: Skills = { int: 0, cha: 0, str: 0, tool: 0 };
  private appearance: Appearance = defaultAppearance();
  private ssn = generateSSN();
  private name = '';
  private dob = '1995-06-15';
  private onDone: (r: CreationResult) => void = () => {};
  private opts: OpenOpts = { mode: 'new' };

  // dynamic refs
  private pointsLeftEl!: HTMLElement;
  private startBtn!: HTMLButtonElement;
  private valueEls: Record<SkillKey, HTMLElement> = {} as Record<SkillKey, HTMLElement>;
  private barEls: Record<SkillKey, HTMLElement> = {} as Record<SkillKey, HTMLElement>;
  private idSsnEl?: HTMLElement;

  constructor() {
    this.overlay = el('div', { class: 'overlay' });
    this.overlay.style.display = 'none';
    this.overlay.style.zIndex = '90';
    document.body.append(this.overlay);
  }

  get pointsLeft(): number {
    return CREATION_POINTS - (this.skills.int + this.skills.cha + this.skills.str + this.skills.tool);
  }

  open(opts: OpenOpts, onDone: (r: CreationResult) => void): void {
    this.opts = opts;
    this.onDone = onDone;
    this.skills = { int: 0, cha: 0, str: 0, tool: 0 };
    this.appearance = defaultAppearance();
    if (opts.mode === 'new') {
      this.ssn = generateSSN();
      this.name = '';
      this.dob = '1995-06-15';
    } else if (opts.existingId) {
      this.ssn = opts.existingId.ssn;
      this.name = opts.existingId.name;
      this.dob = opts.existingId.dob;
    }
    this.render();
    this.overlay.style.display = 'flex';
  }

  close(): void {
    this.overlay.style.display = 'none';
  }

  private render(): void {
    clear(this.overlay);
    const isNew = this.opts.mode === 'new';
    const panel = el('div', { class: 'panel' });

    panel.append(
      el('h1', {}, [isNew ? 'Welcome to HoboLife' : 'You Died.']),
      el('h2', {}, [
        isNew
          ? 'Build your character. You start with nothing but 20 skill points.'
          : `Death #${(this.opts.deaths ?? 0) + 1}. Your bank, home and stored items survive — rebuild your body.`,
      ]),
    );

    if (isNew) {
      const nameInput = el('input', { type: 'text', value: this.name, maxLength: 24, placeholder: 'e.g. Dusty Pockets' }) as HTMLInputElement;
      nameInput.addEventListener('input', () => {
        this.name = nameInput.value;
        this.refresh();
      });
      const dobInput = el('input', { type: 'date', value: this.dob, max: '2010-01-01' }) as HTMLInputElement;
      dobInput.addEventListener('input', () => {
        this.dob = dobInput.value;
        this.refresh();
      });
      panel.append(
        el('div', { class: 'col' }, [
          el('label', { class: 'field' }, ['Legal name', nameInput]),
          el('label', { class: 'field' }, ['Date of birth', dobInput]),
        ]),
      );
    }

    // ID card preview
    this.idSsnEl = el('span', { class: 'ssn' }, [this.ssn]);
    const regen = el('button', {}, ['↻']);
    regen.style.padding = '2px 8px';
    regen.title = 'Generate a new SSN';
    if (isNew) {
      regen.addEventListener('click', () => {
        this.ssn = generateSSN();
        if (this.idSsnEl) this.idSsnEl.textContent = this.ssn;
      });
    }
    panel.append(
      el('div', { class: 'idcard' }, [
        el('div', { class: 'row spread' }, [el('b', {}, ['🪪 CALIFORNIA ID']), el('span', {}, [isNew ? 'NEW' : 'ON FILE'])]),
        el('div', {}, ['Name: ', el('b', {}, [this.name || '—'])]),
        el('div', { class: 'row spread' }, [
          el('span', {}, ['SSN: ', this.idSsnEl]),
          isNew ? regen : el('span'),
        ]),
        el('div', {}, ['DOB: ', this.dob]),
      ]),
    );

    // Skill allocation
    this.pointsLeftEl = el('b', {}, [String(this.pointsLeft)]);
    const alloc = el('div', { class: 'skill-alloc' });
    for (const k of SKILL_KEYS) {
      const val = el('div', { style: 'text-align:center' }, [String(this.skills[k])]);
      this.valueEls[k] = val;
      const minus = el('button', {}, ['−']);
      const plus = el('button', {}, ['+']);
      minus.addEventListener('click', () => this.adjust(k, -1));
      plus.addEventListener('click', () => this.adjust(k, +1));
      const fill = el('div', { style: `width:0%; background: var(--${k})` });
      this.barEls[k] = fill;
      alloc.append(
        el('div', { class: 'skrow' }, [
          el('span', {}, [SKILL_LABELS[k]]),
          el('div', { class: 'bar' }, [fill]),
          minus,
          val,
          plus,
        ]),
      );
    }
    panel.append(
      el('div', { class: 'row spread', style: 'margin-top:8px' }, [
        el('span', { class: 'points-left' }, ['Points left: ', this.pointsLeftEl]),
        el('span', { style: 'color:var(--muted);font-size:12px' }, ['Goal in play: 999 each']),
      ]),
      alloc,
    );

    // Appearance
    panel.append(el('h2', { style: 'margin-top:6px' }, ['Appearance']));
    panel.append(this.swatchRow('Skin', SKINS, 'skin'));
    panel.append(this.swatchRow('Hair', HAIRS, 'hair'));
    panel.append(this.swatchRow('Shirt', SHIRTS, 'shirt'));
    panel.append(this.swatchRow('Pants', PANTS, 'pants'));

    // Start
    this.startBtn = el('button', { class: 'primary', style: 'width:100%; margin-top:18px; padding:12px' }, [
      isNew ? 'Hit the streets →' : 'Respawn →',
    ]) as HTMLButtonElement;
    this.startBtn.addEventListener('click', () => this.finish());
    panel.append(this.startBtn);

    this.overlay.append(panel);
    this.refresh();
  }

  private swatchRow(label: string, colors: string[], key: keyof Appearance): HTMLElement {
    const row = el('div', { class: 'row', style: 'margin:4px 0' }, [
      el('span', { style: 'width:60px;color:var(--muted);font-size:13px' }, [label]),
    ]);
    const swatches = el('div', { class: 'swatches' });
    const paint = () => {
      swatches.querySelectorAll('.swatch').forEach((s) => {
        const sw = s as HTMLElement;
        sw.classList.toggle('sel', sw.dataset.color === this.appearance[key]);
      });
    };
    for (const c of colors) {
      const sw = el('div', { class: 'swatch' });
      sw.style.background = c;
      sw.dataset.color = c;
      sw.addEventListener('click', () => {
        this.appearance[key] = c;
        paint();
      });
      swatches.append(sw);
    }
    row.append(swatches);
    setTimeout(paint, 0);
    return row;
  }

  private adjust(k: SkillKey, delta: number): void {
    const next = this.skills[k] + delta;
    if (next < 0) return;
    if (delta > 0 && this.pointsLeft <= 0) return;
    if (next > CREATION_POINTS) return;
    this.skills[k] = next;
    this.refresh();
  }

  private refresh(): void {
    for (const k of SKILL_KEYS) {
      this.valueEls[k].textContent = String(this.skills[k]);
      this.barEls[k].style.width = (this.skills[k] / CREATION_POINTS) * 100 + '%';
    }
    this.pointsLeftEl.textContent = String(this.pointsLeft);
    const isNew = this.opts.mode === 'new';
    const ready = this.pointsLeft === 0 && (!isNew || (this.name.trim().length > 0 && !!this.dob));
    this.startBtn.disabled = !ready;
  }

  private finish(): void {
    const id: IDCard =
      this.opts.mode === 'new'
        ? { name: this.name.trim(), ssn: this.ssn, dob: this.dob }
        : this.opts.existingId!;
    this.close();
    this.onDone({ id, skills: { ...this.skills }, appearance: { ...this.appearance } });
  }
}
