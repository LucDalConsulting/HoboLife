# HoboLife — Build Progress

A 3D third-person life-sim (Stick RPG × RuneScape × D&D × Pokémon) built in **Unity 6 (URP)**.
The Unity project lives in [`unity/`](unity/). Design: [`docs/DESIGN.md`](docs/DESIGN.md).

## ▶ How to play (right now)

Double-click **`unity/Build/HoboLife.exe`** (Windows). Or open `unity/` in Unity 6 and press Play.

**Controls:** `WASD` move · drag mouse = orbit camera · scroll = zoom · `Q` talk to nearest
person · `E` enter a building · `1–4` pick dialogue options.

## ✅ Done (overnight build — v0.1 vertical slice)

| # | Milestone | What works |
|---|---|---|
| 1 | **Character** | Blocky low-poly hobo (head/torso/arms/legs + underwear) with procedural walk/idle animation, driven by movement speed — no rig/clips. |
| 2 | **Stats HUD + clock** | uGUI HUD: health, hunger, 4 skill bars (0–999), money, in-game clock (1 real hr = 1 game day) + day/night light cycle. Balance numbers ported from the web prototype. |
| 3 | **Dice** | On-screen d10 roll popup: `skill × roll vs DC`, gold crit on 10, red auto-fail on 1. |
| 4 | **NPCs + dialogue** | 5 wandering NPCs (4 pedestrians + a thug). Press Q → numbered dialogue → dice-gated outcomes (panhandle DC 30 → +$12, charm DC 50 → +1 CHA, calm-thug DC 60). |
| 5 | **LA city** | Walkable block: road cross + sidewalks + 10 named landmark buildings (University, Gym, Bank, Diner, Hospital, Casino, Clothing, Car Dealer, Realtor, Pawn). Enter with E for first-pass services (study +INT, gym +STR, diner food, hospital heal). |
| 6 | **Save / load** | JSON save keyed to an auto-generated **ID card** (name/SSN/DOB). Death (starvation) respawns a fresh hobo but the **ID card + bank balance persist**. Autosaves every 30s + on quit. |
| 7 | **Windows build** | One-command Standalone build → `Build/HoboLife.exe` (menu *HoboLife → Build Windows*). |

Every milestone is committed to `main` and was verified in Play mode through the Unity MCP connector.

## 🔧 How it's built (hands-free pipeline)

- **Code/scenes:** edited as files + an **AssetDatabase.Refresh(ForceUpdate)** recompile; scenes
  assembled by editor builder scripts (`Assets/Editor/HoboLife*Builder.cs`) that auto-run on reload.
- **Driving Unity:** the **CoplayDev unity-mcp** connector (stdio bridge on port 6400) — read scene,
  console, run Play mode, capture screenshots, build — no screen control needed. Keep Unity open.
- **Version control:** every step committed + pushed to GitHub.

## ⏭ Next / known gaps

- **Art:** characters/buildings are stylized primitives. Higgsfield was down all session — concept
  art + realistic textures are queued for when it's back. A real rigged character (free **Quaternius**
  CC0 pack, or **Mixamo** with your Adobe login) can replace the procedural one without touching
  `ThirdPersonController`.
- Combat (Pokémon-style screen), job mini-games, full banking/realtor/casino, inventory grid, and
  character creation (set your own name/SSN/skills) are scoped but not yet built.
- Minor: the humanoid's head sits a little low; easy proportion tweak.
