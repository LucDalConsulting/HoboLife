# HoboLife on Unity — Setup Guide (Windows)

This guide gets a **local Claude Code** running on your PC with **Unity**, so it
can see and build the game properly (the cloud Claude in the web chat can't touch
your computer). You do the one-time install below; after that, you mostly just
**talk to your local Claude** and it does the work.

> You won't need to write code. The hardest part is the install — take it slow,
> one step at a time. If anything errors, paste the error to your local Claude
> Code and it'll fix it.

---

## What you'll end up with
- **Unity 6** (the game engine) on your PC.
- **Claude Code** running locally, inside this project folder.
- A **Unity MCP bridge** so Claude can see and drive the Unity editor.
- This **GitHub repo** holding everything (the design doc + the Unity project).

---

## Part 1 — Install the tools (one time)

1. **Node.js** (needed to run Claude Code)
   - Go to https://nodejs.org and install the **LTS** version (click through the defaults).

2. **Claude Code (local)**
   - Open **Command Prompt** (press Start, type `cmd`, hit Enter) and run:
     ```
     npm install -g @anthropic-ai/claude-code
     ```
   - Official instructions (if you want them): https://code.claude.com/docs
   - Sign in when it asks (same account as the web app).

3. **Unity Hub + Unity 6**
   - Download **Unity Hub**: https://unity.com/download
   - Open Unity Hub → **Installs** → **Install Editor** → pick **Unity 6 (LTS)**.
   - When it asks for modules, that's fine to leave default. (Windows Build Support is included.)

4. **Git** (to sync with GitHub)
   - Install from https://git-scm.com/download/win (click through defaults).

---

## Part 2 — Get this project onto your PC

1. Open **Command Prompt** and run (this downloads the repo):
   ```
   git clone https://github.com/LucDalConsulting/HoboLife.git
   cd HoboLife
   ```
2. Create the Unity project **inside the `unity` folder**:
   - Open **Unity Hub** → **Projects** → **New project**.
   - Template: **Universal 3D (URP)** — easiest to start; we can upgrade the
     visuals later. (HDRP looks more realistic but is heavier; we'll switch when
     you're comfortable.)
   - **Project name:** `HoboLife`
   - **Location:** browse to the `HoboLife/unity` folder you just cloned.
   - Click **Create project**. (First open takes a few minutes.)

---

## Part 3 — Start your local Claude Code

1. In **Command Prompt**, make sure you're in the project folder:
   ```
   cd HoboLife
   ```
2. Start it:
   ```
   claude
   ```
3. You now have a local Claude Code with full access to the project. 🎉

---

## Part 4 — Hand it off (copy–paste this to your local Claude)

Paste this whole message into your local Claude Code to kick everything off:

```
You are taking over the HoboLife project locally. Read docs/DESIGN.md for the
full game design (a 3D third-person life-sim: Stick RPG x RuneScape x D&D x
Pokémon, starting as a hobo in LA) and docs/UNITY_BUILD_PLAN.md for the
milestone plan to follow. There is a web prototype in client/ that captures the
mechanics — use it as a design reference only; we are now rebuilding in UNITY for
realistic graphics. Ready-made starter C# scripts are in unity-starter/Scripts/
(third-person controller, orbit camera, stats, dice check) — copy them into the
Unity project's Assets/Scripts.

Please:
1. Set up a Unity MCP bridge so you can see and control the Unity editor
   (install a current open-source Unity MCP server and add it to my Claude Code
   MCP config). Walk me through any clicks I must do in Unity.
2. In the unity/ folder's Unity project, scaffold the first playable slice:
   a third-person player with a follow camera (drag to orbit 360, WASD/click to
   move), a small LA city block I can walk around, and the 4 skills + health/
   hunger HUD from the design.
3. Use free realistic assets (Unity Asset Store / Fab / Quixel) for the city and
   character to push toward the GTA-style look.
4. Commit your work to this Git repo in small steps and explain what you did in
   plain English. I am not a programmer.

Start by confirming you can read docs/DESIGN.md, then set up the Unity MCP bridge.
```

From here, just talk to it in plain English ("make the streets wider", "add a
car", "the character clips through walls"), and it builds and commits.

---

## Using Higgsfield for visuals
[Higgsfield](https://higgsfield.ai) is an AI **image/video generator** — it does
not build the game itself, but it's great for:
- **Concept art** ("a rainy LA street at night, GTA style") to guide the look.
- **Textures / reference images** you can hand to your local Claude to apply in Unity.
- A **launch trailer** later.
Generate images/clips there, save them into the repo (e.g. an `art/` folder),
and tell your local Claude to use them.

---

## Honest expectations
- **Photoreal "GTA" graphics** are a multi-year AAA effort. With Unity + free
  realistic assets we can get a genuinely good-looking game — just not literally GTA.
- This is a marathon, not 10 days. But with a local Claude in the editor, every
  change is something you can *see*, which is exactly what was missing before.

Stuck on any step? Paste the exact error or a screenshot to your local Claude
Code — that's now your build partner.
