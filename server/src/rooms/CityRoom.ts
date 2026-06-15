// The shared LA city room. Holds every connected player's position/appearance
// and relays movement so friends see each other walk around the same world.

import { Room, type Client } from 'colyseus';
import { MapSchema, Schema, type } from '@colyseus/schema';

export class PlayerSchema extends Schema {
  @type('number') x = 0;
  @type('number') z = 0;
  @type('number') heading = 0;
  @type('string') name = 'Hobo';
  @type('string') skin = '#e8b98a';
  @type('string') hair = '#3b2412';
  @type('string') shirt = '#3a6ea5';
  @type('string') pants = '#2c3e50';
}

export class CityState extends Schema {
  @type({ map: PlayerSchema }) players = new MapSchema<PlayerSchema>();
}

interface JoinOptions {
  name?: string;
  x?: number;
  z?: number;
  appearance?: { skin?: string; hair?: string; shirt?: string; pants?: string };
}

interface MoveMessage {
  x: number;
  z: number;
  heading: number;
}

export class CityRoom extends Room<CityState> {
  maxClients = 50;

  onCreate(): void {
    this.setState(new CityState());

    this.onMessage('move', (client: Client, data: MoveMessage) => {
      const p = this.state.players.get(client.sessionId);
      if (!p) return;
      // Basic sanity clamp to keep things on the map.
      p.x = clamp(data.x, -60, 60);
      p.z = clamp(data.z, -60, 60);
      p.heading = data.heading ?? 0;
    });
  }

  onJoin(client: Client, options: JoinOptions = {}): void {
    const p = new PlayerSchema();
    p.name = (options.name ?? 'Hobo').slice(0, 24);
    p.x = clamp(options.x ?? 0, -60, 60);
    p.z = clamp(options.z ?? 0, -60, 60);
    const a = options.appearance ?? {};
    if (a.skin) p.skin = a.skin;
    if (a.hair) p.hair = a.hair;
    if (a.shirt) p.shirt = a.shirt;
    if (a.pants) p.pants = a.pants;
    this.state.players.set(client.sessionId, p);
    console.log(`[city] ${p.name} joined (${this.clients.length} online)`);
  }

  onLeave(client: Client): void {
    this.state.players.delete(client.sessionId);
  }
}

function clamp(v: number, min: number, max: number): number {
  return Number.isFinite(v) ? Math.min(max, Math.max(min, v)) : 0;
}
