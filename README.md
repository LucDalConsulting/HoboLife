# HoboLife

A browser-based, 3D, third-person multiplayer life-sim RPG. Start as a hobo in
the city with nothing but your underwear and 20 skill points — claw your way up
through study, work, charm, crime, and combat to max out your skills (999 each)
and get rich.

Inspired by **Stick RPG**, viewed and animated in the simple third-person style
of **RuneScape**, with **D&D**-style dice checks and **Pokémon**-style combat.

> Status: **v0.1 — playable vertical slice.** See [`docs/DESIGN.md`](docs/DESIGN.md)
> for the full design and roadmap.

---

## Why a web game?

| Requirement | How the web stack solves it |
| --- | --- |
| Accessible on many devices | Runs in any modern browser — desktop, laptop, tablet, phone |
| Cheap / no-server hosting | The game **client** is a static site → hosts **free** (Cloudflare Pages, GitHub Pages, Netlify) |
| Frequent updates everyone gets | Web app — updates are **instant on reload**, no per-user download; a version banner prompts a refresh |
| Multiplayer with friends | Optional [Colyseus](https://colyseus.io/) server (open-source) on a tiny/free instance (≤ $10/mo) |
| Version control | Git + GitHub, semver version stamp baked into each build |

The client runs fully **offline / single-player** out of the box. Multiplayer
presence (seeing your friends walk around the same city) turns on automatically
when a Colyseus server URL is configured.

---

## Quick start (local)

```bash
cd client
npm install
npm run dev
```

Open the printed URL (default http://localhost:5173). The game runs entirely in
your browser; your character is saved to local storage.

### Build for production

```bash
cd client
npm run build      # outputs static files to client/dist/
npm run preview    # serve the production build locally
```

Deploy `client/dist/` to any static host.

### Optional multiplayer server

```bash
cd server
npm install
npm run dev        # starts a Colyseus server on ws://localhost:2567
```

Then set `VITE_SERVER_URL=ws://localhost:2567` in `client/.env.local` and reload
the client to see friends in the same city.

---

## Controls

| Input | Action |
| --- | --- |
| **W A S D** | Move (relative to camera) |
| **Left-click ground** | Click-to-move (RuneScape style) |
| **Right-drag** | Orbit the camera |
| **Mouse wheel** | Zoom in/out |
| **Q** | Talk to the nearest NPC |
| **E** | Interact / enter a building |
| **Left-click** | Use the item in your **left** hand |
| **Right-click** | Use the item in your **right** hand |
| **1 2 3 4** | Pick dialogue / combat options |
| **I** | Open inventory |

---

## Repository layout

```
client/   Three.js + TypeScript + Vite game client (the playable game)
server/   Colyseus multiplayer presence server (optional)
docs/     Design document & roadmap
```

See [`docs/DESIGN.md`](docs/DESIGN.md) for the complete game design, the dice
formula, the combat model, the economy, and the version roadmap.
