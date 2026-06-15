// Keyboard + mouse input. World interaction is gated by `enabled` so that open
// modals (dialogue, inventory, combat) don't move the character behind them.

export interface ClickEvent {
  /** Normalised device coords (-1..1). */
  ndcX: number;
  ndcY: number;
  button: number; // 0 left, 2 right
}

export class Input {
  private down = new Set<string>();
  private keyCbs: ((code: string) => void)[] = [];
  private clickCbs: ((ev: ClickEvent) => void)[] = [];

  /** Accumulated camera orbit/zoom; the camera consumes these each frame. */
  orbitDX = 0;
  orbitDY = 0;
  zoomDelta = 0;

  /** When false, world clicks and camera drag are ignored (a modal is open). */
  enabled = true;

  private dragging = false;
  private dragMoved = 0;
  private leftDownMoved = 0;
  private leftDown = false;
  private el: HTMLElement;

  constructor(el: HTMLElement) {
    this.el = el;
    window.addEventListener('keydown', this.onKeyDown);
    window.addEventListener('keyup', this.onKeyUp);
    el.addEventListener('contextmenu', (e) => e.preventDefault());
    el.addEventListener('pointerdown', this.onPointerDown);
    window.addEventListener('pointermove', this.onPointerMove);
    window.addEventListener('pointerup', this.onPointerUp);
    el.addEventListener('wheel', this.onWheel, { passive: false });
  }

  isDown(code: string): boolean {
    return this.down.has(code);
  }

  onKey(cb: (code: string) => void): void {
    this.keyCbs.push(cb);
  }
  onClick(cb: (ev: ClickEvent) => void): void {
    this.clickCbs.push(cb);
  }

  private onKeyDown = (e: KeyboardEvent) => {
    // Don't swallow typing in form fields (character creation inputs).
    const t = e.target as HTMLElement;
    if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA')) return;
    if (!this.down.has(e.code)) {
      for (const cb of this.keyCbs) cb(e.code);
    }
    this.down.add(e.code);
  };
  private onKeyUp = (e: KeyboardEvent) => {
    this.down.delete(e.code);
  };

  private onPointerDown = (e: PointerEvent) => {
    if (e.button === 2) {
      this.dragging = true;
      this.dragMoved = 0;
    } else if (e.button === 0) {
      this.leftDown = true;
      this.leftDownMoved = 0;
    }
  };

  private onPointerMove = (e: PointerEvent) => {
    if (this.dragging && this.enabled) {
      this.orbitDX += e.movementX;
      this.orbitDY += e.movementY;
      this.dragMoved += Math.abs(e.movementX) + Math.abs(e.movementY);
    }
    if (this.leftDown) {
      this.leftDownMoved += Math.abs(e.movementX) + Math.abs(e.movementY);
    }
  };

  private onPointerUp = (e: PointerEvent) => {
    if (e.button === 2) {
      const wasClick = this.dragMoved < 6;
      this.dragging = false;
      if (wasClick && this.enabled) this.emitClick(e, 2);
    } else if (e.button === 0) {
      const wasClick = this.leftDownMoved < 6;
      this.leftDown = false;
      if (wasClick && this.enabled) this.emitClick(e, 0);
    }
  };

  private emitClick(e: PointerEvent, button: number) {
    const rect = this.el.getBoundingClientRect();
    const ndcX = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    const ndcY = -(((e.clientY - rect.top) / rect.height) * 2 - 1);
    for (const cb of this.clickCbs) cb({ ndcX, ndcY, button });
  }

  private onWheel = (e: WheelEvent) => {
    if (!this.enabled) return;
    e.preventDefault();
    this.zoomDelta += e.deltaY;
  };
}
