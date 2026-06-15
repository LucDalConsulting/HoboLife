# HoboLife — Design Document

This is the living design spec for HoboLife. v0.1 implements the **vertical
slice** marked ✅ below; everything else is roadmap.

## 1. Pitch

A 3D, third-person, multiplayer life-sim. You spawn as a homeless person in a
fictitious Los Angeles with no clothes (underwear only), no money, and just
**20 skill points** to allocate. Through study, jobs, charm, crime, gambling,
and combat you raise four skills toward **999 each** and build wealth, climbing
from hobo to whatever you want to be — investment banker, crime boss, business
owner, family man.

Inspirations:
- **Stick RPG** — stat-gated jobs, repeatable training to raise stats, money loop.
- **RuneScape** — third-person orbit camera, click-to-move, click-to-interact, simple animation.
- **D&D** — dice rolls decide success/failure of actions.
- **Pokémon** — turn-based battle screen for combat.
- **Minecraft** — grid-based storage / inventory.
- **Among Us** — small job mini-games instead of full job simulation.

## 2. The four skills (0–999)

| Skill | Raised by | Gates / powers |
| --- | --- | --- |
| **Intelligence** | Studying at university | White-collar jobs, business, investing, smart dialogue |
| **Charisma** | Social actions, dating | Dating/marriage, talking your way past checks, sales, cheating at cards |
| **Strength** | The gym, winning fights | Combat damage & survivability, intimidation, max health |
| **Tool skill** | Practice & tool-based jobs | Driving/license, guns, knives, forklifts/cranes/machinery |

Character creation gives **20 points** to distribute across the four (each 0–100
at creation; cap 999 in play). Goal: 999 in all four = maxed character.

## 3. Survival

- **Health**: starts at 100. Max health scales with Strength. Items (fruit
  smoothie +5, nutritionist IV drip = temporary boost) can raise it.
- **Hunger**: a bar that decays over time. At 0, health drains quickly → death.
  Eat (restaurants / food items) to refill.
- **Death**: respawn as a brand-new hobo with fresh (re-rolled/re-allocated)
  stats and **lose carried items, clothes, and the 2 hand items**. Everything
  tied to your **ID card** — bank balance, houses, cars, stored items — persists.

## 4. Identity & persistence

Every player has a **government ID card** created at account creation:
- Name, **SSN** (auto-generated, permanent), date of birth.
- Tied to the login; never changes.
- All owned assets attach to the ID and survive death.

v0.1 stores the account + save in the browser's local storage. Later versions
move the canonical save server-side (keyed by ID) so it follows you across
devices and supports multiplayer authority.

## 5. The dice / skill-check system (core mechanic)

Every uncertain action resolves through a **d10** roll shown top-right:

```
effective = skillPoints × roll
  • roll of 10  → ×10 then doubled  (i.e. skillPoints × 20)  [best]
  • roll of 1   → automatic FAIL regardless of stats          [worst]
success = effective ≥ requiredLevel (the task's hidden DC)
```

Example: a date requires **50** charisma. With 5 CHA and a roll of 9 →
`5 × 9 = 45` → fail. With 10 CHA and a roll of 9 → `10 × 9 = 90` → success.

Implemented in [`client/src/core/skillcheck.ts`](../client/src/core/skillcheck.ts).
Used by dialogue checks, jobs (interviews), crime, gambling, and combat.

## 6. Combat (Pokémon-style)

Entering a fight opens a battle screen: both combatants on platforms, both HP
bars visible, a rolling log, and **4 move options** derived from what's in your
hands at the moment combat starts:

- Empty hands → Punch, Kick (+ Guard, Run).
- Knife → Stab, Slash (+ Guard, Run).
- Gun → Shoot, Pistol-whip (+ Guard, Run).
- One of each → mix of the above.

Damage is a dice-scaled formula weighted by **Strength** and **Tool skill** (for
weapons). **Run** chance scales with Strength, the HP gap, and the roll. Reduce
the opponent to 0 HP to win; losing can drop you to the hospital or kill you.

Formula lives in [`client/src/core/combatmath.ts`](../client/src/core/combatmath.ts).

## 7. Inventory (Minecraft-style)

- Two hands; **left-click uses left hand, right-click uses right hand**.
- Some items require two hands.
- Grid storage: backpacks, car trunk (while in the car), house closet (clothes)
  and safe (weapons) — each is a chest-like grid you move items in and out of.

## 8. Economy & city

A walkable LA city block where most things are interactable (some areas blocked):
university (study → INT), gym (train → STR), bank (loans/credit/interest),
restaurants, clothing stores, hospital, casino, car dealers, realtor,
lawyers, insurance, jobs (with mini-games), plus streets full of NPCs.

Money paths: jobs, gambling, robbery/crime, stocks, flipping houses, owning a
business that NPC customers patronize.

## 9. Time

In-game clock where **1 real hour = 1 full 24h game day**. No sleeping. Jobs and
most businesses operate only in daytime. The game is built for relaxed, idle /
second-monitor play (RuneScape-grind feel); tabbing away is fine.

## 10. Controls

`WASD` move · left-click ground to walk · right-drag orbit camera · wheel zoom ·
`Q` talk · `E` interact/enter · left/right-click use hand items · `1–4` choose
options · `I` inventory.

---

## Roadmap

### v0.1 — Vertical slice ✅ (this build)
- [x] Stack: Three.js + TS + Vite client, Colyseus presence server, PWA + version stamp.
- [x] Character creation: name, DOB, auto SSN, 20-point allocation, appearance colors, ID card.
- [x] LA city block: ground, roads, buildings with labels, simple collision.
- [x] Third-person orbit camera + click-to-move + WASD; humanoid with walk animation.
- [x] HUD: health, hunger, money, 4 skills, in-game clock, two hand slots.
- [x] Day/night cycle on the game clock.
- [x] Dice skill-check engine + on-screen d10 roll.
- [x] NPCs that wander; `Q` to talk; numbered dialogue with dice-gated outcomes (panhandle, etc.).
- [x] One job mini-game (daytime only) that pays money.
- [x] Grid inventory + hand items.
- [x] Death & respawn with asset persistence.
- [x] Pokémon-style combat screen.
- [x] Save/load keyed to the ID card; Colyseus presence so friends share the city.

### v0.2 — Depth
- [ ] University study loop & gym training loop with daily caps.
- [ ] Bank: accounts, loans, student loans, interest, credit.
- [ ] More jobs across the income ladder, each with its own mini-game and stat gate.
- [ ] Buying/renting housing (realtor) with monthly rent auto-deduction.
- [ ] Clothing stores & the wardrobe/appearance system tied to owned clothes.

### v0.3 — World & social
- [ ] 200+ NPCs: friendly/hostile, buyers, teachers, bosses, employees, customers.
- [ ] Dating → marriage → kids.
- [ ] Cars / skateboards / bikes; driving; theft risk.
- [ ] Casino games; stock market.

### v0.4 — Systems & online
- [ ] Server-authoritative saves keyed to ID (cross-device).
- [ ] Player-vs-player interaction, lawsuits, businesses with NPC staff/customers.
- [ ] More cities unlock; travel.

### Hosting plan (≤ $10/mo)
- **Client**: static build → Cloudflare Pages / GitHub Pages / Netlify (free).
- **Server**: Colyseus on Fly.io / Render / Railway (free tier or ~$5/mo).
- **DB**: SQLite on the server now; Neon/Supabase Postgres free tier later.
- **Updates**: push to GitHub → CI builds → static host redeploys; clients get a
  "new version available — reload" banner from the version stamp.
