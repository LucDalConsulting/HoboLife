# HoboLife — Unity Build Plan

A milestone plan for the **local Claude Code** to follow when porting HoboLife to
Unity. The game design is in [`DESIGN.md`](DESIGN.md); the web build in
[`../client/`](../client/) is a working reference for every mechanic. Ready-made
starter scripts are in [`../unity-starter/Scripts/`](../unity-starter/Scripts/).

> Target: Unity 6 (LTS), URP to start (upgrade to HDRP for more realism later).
> Work in small, committed steps. Confirm each milestone runs in Play mode.

## Milestone 0 — Bridge & project
- Install/configure a Unity MCP server so Claude can see and drive the editor.
- Create the project under `unity/` (URP 3D). Add the starter scripts to
  `Assets/Scripts/` (copy from `unity-starter/Scripts/`).

## Milestone 1 — Move around (proves the loop)
- A ground plane + a few box "buildings", a capsule/player with a
  `CharacterController` + `ThirdPersonController.cs`.
- Main Camera with `OrbitCamera.cs` targeting the player (drag = 360° orbit,
  scroll = zoom). Verify W/A/S/D move correctly (no reversed strafe).
- A free third-person character + animations from the Asset Store / Mixamo;
  wire an Animator (idle/walk/run by speed).

## Milestone 2 — Stats & HUD
- `PlayerStats.cs` on the player. UI Toolkit HUD: 4 skill bars, health, hunger,
  money, an in-game clock (1 real hour = 1 game day), and two hand slots.
- `DiceCheck.cs` drives an on-screen d10 roll for any gated action.

## Milestone 3 — The city (LA look)
- Build a walkable LA block: roads, sidewalks, street props. Use free realistic
  assets (Fab/Quixel Megascans, Asset Store city kits) and bake lighting.
- Add interiors you can walk into (matches the web build's store interiors).

## Milestone 4 — NPCs, dialogue, jobs, combat
- NavMesh NPCs that wander; press a key to talk → numbered dialogue → dice-gated
  outcomes (port the trees from `client/src/data/dialogue.ts`).
- One job mini-game; building services (study/gym/diner/bank/hospital/casino).
- A combat encounter (the web build uses a Pokémon-style screen; in 3D this can
  be a simple lock-on melee — your call with the user).

## Milestone 5 — Save, polish, build
- Save/load keyed to the ID card (port `client/src/state/`).
- Post-processing (bloom, AO, color grading), nicer materials.
- Produce a Windows build (`.exe`) so the user can double-click to play.

## Reference map (web → Unity)
| Web prototype | Use it for |
| --- | --- |
| `client/src/core/skillcheck.ts` | Dice math (ported in `DiceCheck.cs`) |
| `client/src/core/constants.ts` | Balance numbers (skills, hunger, time) |
| `client/src/data/` | City layout, NPCs, dialogue, items |
| `client/src/ui/` | HUD/dialogue/combat/inventory behaviour |
| `client/src/state/` | Save model, death/respawn, account/ID |
