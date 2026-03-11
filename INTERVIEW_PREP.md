# Adaptabrawl — Interview Prep Guide

> **Context:** University of Cincinnati Senior Design project (Fall 2025 – Spring 2026)
> **Your role:** Combat/Systems & Tooling (Saarthak Sinha)

---

## What Is Adaptabrawl? (The 30-Second Pitch)

Adaptabrawl is a 2D fighting game — think Street Fighter or Smash Bros — that my team built from scratch as our Senior Design project. What makes it different is the **adaptive combat** angle: the game changes on you mid-match through environmental conditions (like a slippery floor or low visibility fog) and character status effects (like being poisoned or low on health), and every one of those changes is shown clearly on screen so players always know what's happening. Two players can fight locally on one machine, or online over the internet using room codes.

---

## The Problem We Solved

Most fighting games push players toward one dominant strategy — either you always attack, always block, or always run away. We wanted a game where you're constantly reassessing and switching gears. The core challenge was: **how do you make a game that rewards adaptability without becoming confusing or unfair?**

Our answer was to make every modifier transparent. If the stage condition slows down your movement, a banner tells you. If you're poisoned, an icon and countdown timer appear on your HUD. Nothing is hidden.

---

## What We Built

### The Full Game Loop
From the moment you launch the game, every screen is connected:

```
Main Menu → Character Select → [Local or Online]
   ↓               ↓                    ↓
Settings       Pick Fighter          Lobby (room code)
                   ↓                    ↓
              Fight (multi-round match with HUD)
                   ↓
             Match Results → Rematch or go back
```

### Key Features (plain English)

| Feature | What it means |
|---|---|
| **Local 1v1** | Two players on one keyboard/controller |
| **Online 1v1** | Play with a friend over the internet using a 6-character room code |
| **Multi-round matches** | Best-of-X rounds with a configurable timer (like a real fighting game) |
| **Health bars** | Real-time HP display that drains as you take damage |
| **Round-win bubbles** | Visual indicator showing how many rounds each player has won |
| **mm:ss match timer** | Countdown clock that triggers a win-by-health rule when it hits zero |
| **Status effects** | Poison, stagger, low-HP state — each shown with an icon + timer |
| **Adaptive conditions** | Stage/weather modifiers that change how the match plays out |
| **Character selection** | Two distinct fighters with their own movesets |
| **Death animation** | Health slider drains to zero, then a death animation plays |
| **Pause menu** | Escape during a match to pause, go to settings, or quit |
| **Settings** | Audio volume, video quality, FPS cap, accessibility options (color-blind mode, UI scale) |

---

## Your Specific Contributions

Your role was **Combat/Systems & Tooling** — essentially the engine that makes the game _feel_ like a fighting game.

### What you personally owned:

1. **Combat State Machine (CombatFSM)**
   Every attack goes through three phases: startup (winding up), active (the hit window), recovery (cool-down after). You designed and implemented this so attacks feel weighty and punishable — a core fighting game concept.

2. **Hit/Hurtbox System**
   The invisible shapes that determine when a punch actually lands. You overhauled this system — attaching hitboxes and hurtboxes to the character's "Stander" child object — so collision detection is accurate and frame-perfect.

3. **Damage System**
   Health values, damage calculation, the logic that bridges an attack connecting to the health bar updating on screen (bridging the "Shinabro" damage system to the FighterController).

4. **Health Bar & HUD**
   Simplified the health UI to a clean slider system, wired it to real-time health values, and made it update correctly during combat.

5. **Round & Match System**
   Multi-round matches, configurable round duration, detecting when a round ends (by death or timer), tracking round wins per player, and transitioning to the next round or end screen.

6. **Death & Round Transitions**
   Death animation playback, health draining to zero on death, inter-round resets, and preventing health overflow bugs.

7. **Timer (mm:ss format)**
   The in-game countdown clock that displays in minutes and seconds.

8. **Two Starter Fighters**
   Built the character definitions (via ScriptableObjects — think of them as data files describing each character) for the Striker (pressure/frame traps) and the Elusive (mobile/dodge-focused) fighters.

9. **Status Effect System**
   Poison (damage over time), heavy-attack state (slow + armor), low-HP state — with stacking, timers, and UI icon display.

10. **Adaptive Conditions System**
    The framework that applies match-wide modifiers (slippery floor = reduced friction, thick fog = reduced projectile speed) and discloses them clearly to players.

---

## What to Demo

If you can show the game, walk through this order:

1. **Launch → Main Menu** — Show it's a real, navigable app
2. **Character Select** — Pick two fighters, show the preview system
3. **Start a local match** — Two fighters spawn on a stage
4. **Take some hits** — Show the health bars drain, HUD updates in real time
5. **Win a round** — Show the round-win bubbles fill in, inter-round reset happens
6. **Win the match** — Death animation, match results screen
7. **Settings page** — Show audio/video/accessibility options exist

If you can't run the game: show the **GitHub commit history** — it tells a story of iterative development. Point out commits like:
- "Death animation, slider-to-zero on death"
- "HUD: mm:ss timer and circle-sprite round-win bubbles"
- "Overhaul combat collision system: Stander-attached hitboxes/hurtboxes"
- "Fix round transition, round duration config, and health overflow"

---

## How to Explain the Tech (Non-Technically)

**"What engine did you use?"**
> We used Unity — it's the same engine used for games like Hollow Knight and Cities: Skylines. We wrote everything in C#.

**"How does online play work?"**
> We use a host-authoritative model. One player acts as the server — their machine is the source of truth — and the other player's inputs are sent over the internet. To keep it feeling responsive, we predict what will happen locally and correct it if the server disagrees. We used Unity's Relay service so players don't need to open ports; you just share a 6-digit room code.

**"What are ScriptableObjects?"**
> Think of them as data files that live outside the code. Instead of hardcoding that a punch does 10 damage and takes 8 frames to start, we put that in a configuration file. That way, a designer can tweak numbers without touching code — and we can add new characters just by creating a new file.

**"What's a state machine in this context?"**
> Imagine a traffic light — it can only be in one state at a time (red, yellow, green), and there are rules for when it switches. Our fighters work the same way: you can only be in one combat state at once (idle, attacking, blocking, hurt, etc.), and the state machine enforces the rules about when you can cancel or chain moves.

**"How did you handle the hitboxes?"**
> Every attack has an invisible rectangle (the hitbox) that activates during the "active frames" of an attack. Each fighter has a permanent hurtbox — where they can be hit. When a hitbox overlaps a hurtbox, the game registers a hit. We attach these to the character at runtime so they follow the character's movement precisely.

---

## Likely Interview Questions & Talking Points

**"Tell me about a challenge you faced."**
> The combat hit detection was broken for a while — attacks weren't registering even when they visually connected. The root cause was that hitboxes were attached to the wrong object in the hierarchy, so they weren't following the character's actual position. I fixed it by overhauling how hitboxes are attached, tying them to the Stander child object.

**"How did you work as a team?"**
> We split into clear ownership areas — I owned combat systems, Kartavya owned networking, Kanav owned UI/UX, and Yash owned QA and CI/CD. We met almost every Friday and Sunday. We used GitHub with feature branches and PRs so we could review each other's changes before merging.

**"What would you do differently?"**
> I'd invest earlier in a determinism layer for networking. We built combat locally first and added networking later, which created some friction. Starting with "every frame must produce the same result given the same input" as a constraint would have made the networking integration smoother.

**"What did you learn?"**
> How to build layered real-time systems — specifically the interplay between input → state machine → physics → networking → UI. Every layer has to be tight because latency or a missed frame compounds through the whole stack.

**"Why a fighting game for a senior design project?"**
> Fighting games are deceptively complex systems problems. At the surface it's just two characters punching each other, but underneath you have real-time physics, frame-accurate collision, network synchronization, state management, and UI that all have to work together at 60 FPS. It's a good test of everything you'd encounter in production software.

---

## Quick Facts to Know Cold

| | |
|---|---|
| **Project duration** | Fall 2025 – Spring 2026 (two semesters) |
| **Team size** | 4 people |
| **Engine** | Unity LTS 6000.2.6f2 |
| **Language** | C# 12.0 |
| **Networking** | Mirror Framework / Unity NGO + Relay |
| **Platform** | Windows & macOS |
| **Budget** | $0 — all free/open-source tools |
| **Repo** | github.com/Kartavya904/Adaptabrawl-Senior-Design |

---

## Things to NOT Over-Explain

- Don't go deep on ScriptableObject asset pipelines unless asked
- Don't explain rollback netcode in detail — just say "client prediction with server correction"
- Don't enumerate every file you wrote — talk about systems, not scripts
- Don't apologize for unfinished features — frame everything as "what's complete" and "what's next"
