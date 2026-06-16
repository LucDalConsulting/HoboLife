# HoboLife — Build Progress

A 3D third-person life-sim (Stick RPG × RuneScape × D&D × Pokémon) built in **Unity 6 (URP)**.
The Unity project lives in [`unity/`](unity/). Design: [`docs/DESIGN.md`](docs/DESIGN.md).

## ▶ How to play

Double-click **`unity/Build/HoboLife.exe`** (Windows). Or open `unity/` in Unity 6 and press Play.

**Controls:** `WASD` move · drag mouse = orbit camera · scroll = zoom · `Q` talk · `E` enter a
building · `I` inventory · `V` drive an owned vehicle · left/right-click = use left/right hand item ·
`1–4` dialogue / move options.

## ✅ Built so far (v0.4 — 20 systems incl. real assets + real skeletal animation)

| # | System | What works |
|---|---|---|
| 1 | **Character** | Real **rigged, skinned** character (glTFast-imported GLB) driven by a 1-D idle/walk/run blend tree off movement speed. Mesh is a free CC0 placeholder; a generated hobo swaps in by re-pointing one path. |
| 2 | **Stats HUD + clock** | Health, hunger, 4 skill bars (0–999), money, in-game clock (1 real hr = 1 game day) + day/night light. |
| 3 | **Dice** | On-screen d10 roll: `skill × roll vs DC`, gold crit (10), red auto-fail (1). |
| 4 | **NPCs + dialogue** | Wandering NPCs; Q → numbered dialogue → dice-gated outcomes (panhandle, charm, thug). |
| 5 | **LA city** | Road cross + sidewalks + 10 named landmark buildings around a plaza. |
| 6 | **Save / load** | JSON save keyed to an auto-generated **ID card** (name/SSN/DOB); death resets the character but the ID card + bank persist. |
| 7 | **Windows build** | One-command `.exe` (menu *HoboLife → Build Windows*). |
| 8 | **Inventory** | Two hands + a 12-slot grid pack (I); left/right-click uses a hand; food is eaten; weapons set combat moves. |
| 9 | **Combat** | Pokémon-style battle: both HP bars, log, 4 moves from the weapons in hand (Punch/Kick, Stab/Slash, Shoot/Pistol-whip) + Guard + Run; dice-scaled damage; losing → death. |
| 10 | **Character creation** | Pre-game screen: name + DOB (auto SSN), allocate the **20 starting points**, pick a look. Shown only on a brand-new save. |
| 11 | **Building services** | Enter (E) any landmark: study (+INT), gym (+STR), **work a burger-flip mini-game** for cash, bank deposit/withdraw/loans, casino gamble, hospital heal, shops. |
| 12 | **Vehicles** | Buy a car ($2000, needs a license via a Tool driving test) or skateboard; **V** to drive 3× / 1.6× faster. |
| 13 | **Dating → marriage** | Romance tree on dateable NPCs: flirt/date/propose (CHA-gated); marrying records your spouse on the account. |
| 14 | **Cinematic look** | Post-processing (ACES tonemapping, bloom, color grading, vignette), warm soft-shadowed sun, atmospheric fog. |
| 15 | **Real textures** | Procedurally-generated window facades (some lit warm), asphalt roads, concrete sidewalks — buildings read as buildings. |
| 16 | **Rounded character** | Capsule/sphere humanoid with a face, hands, feet, proper proportions; weightier animation (body bob, lean, breathing, idle head turn). |
| 17 | **LA props** | Palm trees + glowing street lamps lining the streets. |
| 18 | **Real CC0 world assets** | Downloaded **Poly Haven** (public-domain) assets, wired in autonomously: a real **HDRI sky** (clouds + image-based ambient light) and **PBR asphalt + concrete** (normal-mapped) on the roads/sidewalks. |
| 19 | **Real skeletal animation** | Official **glTFast** importer added; a rigged character GLB (14 clips) drives an AnimatorController blend tree (idle/walk/run) wired to the CharacterController — first real bone animation, replacing the procedural bob. |
| 20 | **Street life** | Painted crosswalks on all four intersection approaches, dashed lane lines, and parked cars along the curbs. |

Every system is committed to `main` and verified in Play mode through the Unity MCP connector.

## 🔧 How it's built (hands-free pipeline)

- Code edited as files + `AssetDatabase.Refresh(ForceUpdate)` recompiles; scenes assembled by editor
  builder scripts. Driven via the **CoplayDev unity-mcp** connector (read scene/console, run Play
  mode, screenshot, build) — no screen control. Every step committed + pushed to GitHub.

## ⏭ Next / known gaps

- **Art pass:** done so far — post-FX, real CC0 sky + street textures, real **rigged skeletal
  animation** (idle/walk/run), palms + lamps, crosswalks + parked cars. The character mesh is a
  CC0 **placeholder** (robot); swapping in a proper **hobo** is just re-pointing `GlbPath` in
  `HoboLifeCharacterRig`. Blocked on a character source: **Higgsfield** is back online but its
  account errors `User not found` (needs the connector re-authenticated), or **Mixamo** (Adobe
  login) for a mocap human. Still wanted: building interiors, signage, Higgsfield concept art.
- **Economy depth:** stock market, real-estate (buy/rent housing + monthly rent), owning a business
  with NPC customers, insurance, lawyers/prison.
- **More:** kids/family beyond marriage, 200+ pooled NPCs (perf), wardrobe/clothing meshes, the
  car as a drivable physical vehicle, and online multiplayer (Colyseus presence, per the design).
- Minor polish: humanoid head sits a touch low; combat/job UIs are functional but plain.
