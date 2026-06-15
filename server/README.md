# HoboLife — Multiplayer Server

A tiny [Colyseus](https://colyseus.io/) server that hosts the shared **city**
room so you and your friends see each other walking around the same LA. It only
syncs presence (position, heading, appearance) — all gameplay/economy logic runs
client-side in v0.1.

It's intentionally minimal so it fits a free / ~$5-a-month instance (well under
the $10/mo budget). The game client works fully **offline without this server**;
multiplayer just turns on when the client points at it.

## Run locally

```bash
cd server
npm install
npm run dev        # ws://localhost:2567
```

Then in `client/.env.local`:

```
VITE_SERVER_URL=ws://localhost:2567
```

Reload the client — open it in two browser tabs and you'll see two characters.

## Deploy (≤ $10/mo)

Any host that runs a long-lived Node process works (Colyseus needs a persistent
WebSocket connection, so pure serverless/static won't do for this part):

- **Fly.io / Render / Railway** — free tier or ~$5/mo. A `Dockerfile` is included.
- Set `PORT` via the platform; the server reads `process.env.PORT`.
- Point the client at the deployed URL: `VITE_SERVER_URL=wss://your-app.example.com`.

```bash
# example: build & run the container
docker build -t hobolife-server .
docker run -p 2567:2567 hobolife-server
```

## Protocol

- Room name: `city`.
- Join options: `{ name, x, z, appearance: { skin, hair, shirt, pants } }`.
- Client → server message `move`: `{ x, z, heading }`.
- State: `players` map keyed by session id, each `{ x, z, heading, name, skin, hair, shirt, pants }`.

## Roadmap

v0.4 promotes this to the authoritative save store keyed by the ID card
(cross-device persistence) and adds player-vs-player interactions. See
[`../docs/DESIGN.md`](../docs/DESIGN.md).
