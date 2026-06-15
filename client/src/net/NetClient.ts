// Optional multiplayer presence via Colyseus. Entirely no-op unless a server URL
// is configured (VITE_SERVER_URL). Loaded dynamically and wrapped defensively so
// that single-player is never affected if the server is missing or incompatible.

import type { Appearance } from '../core/types';

export interface RemotePlayer {
  id: string;
  name: string;
  x: number;
  z: number;
  heading: number;
  appearance: Appearance;
}

export interface Identity {
  name: string;
  appearance: Appearance;
  x: number;
  z: number;
}

export class NetClient {
  enabled = false;
  onAdd: (p: RemotePlayer) => void = () => {};
  onChange: (p: RemotePlayer) => void = () => {};
  onRemove: (id: string) => void = () => {};

  private room: any = null;

  static serverUrl(): string | undefined {
    const env = (import.meta as any).env ?? {};
    return env.VITE_SERVER_URL || undefined;
  }

  async connect(url: string, identity: Identity): Promise<void> {
    try {
      const mod: any = await import('colyseus.js');
      const client = new mod.Client(url);
      this.room = await client.joinOrCreate('city', {
        name: identity.name,
        appearance: identity.appearance,
        x: identity.x,
        z: identity.z,
      });
      this.enabled = true;

      const players = this.room.state.players;
      const mySession = this.room.sessionId;

      players.onAdd((player: any, key: string) => {
        if (key === mySession) return;
        this.onAdd(toRemote(key, player));
        player.onChange = () => this.onChange(toRemote(key, player));
        try {
          player.listen?.('x', () => this.onChange(toRemote(key, player)));
        } catch {
          /* schema variant — ignore */
        }
      });
      players.onRemove((_player: any, key: string) => this.onRemove(key));
      // eslint-disable-next-line no-console
      console.info('[net] connected to city room as', mySession);
    } catch (e) {
      console.warn('[net] multiplayer disabled:', e);
      this.enabled = false;
      this.room = null;
    }
  }

  sendMove(x: number, z: number, heading: number): void {
    if (this.room) {
      try {
        this.room.send('move', { x, z, heading });
      } catch {
        /* ignore transient send errors */
      }
    }
  }

  dispose(): void {
    try {
      this.room?.leave();
    } catch {
      /* ignore */
    }
    this.room = null;
    this.enabled = false;
  }
}

function toRemote(id: string, p: any): RemotePlayer {
  return {
    id,
    name: p.name ?? 'Player',
    x: p.x ?? 0,
    z: p.z ?? 0,
    heading: p.heading ?? 0,
    appearance: {
      skin: p.skin ?? '#e8b98a',
      hair: p.hair ?? '#3b2412',
      shirt: p.shirt ?? '#3a6ea5',
      pants: p.pants ?? '#2c3e50',
    },
  };
}
