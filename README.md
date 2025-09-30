# Adaptabrawl
_Adaptabrawl is a 2D fighting game that refuses to over‑index on a single style._ Instead, it blends attack, defense, and evasion—and then adapts them live through match conditions (e.g., stage/weather modifiers) and readable status effects (e.g., poisoned, staggered/punched, heavy‑attack windup, low‑HP state). The result is a fast, legible combat loop where players switch gears fluidly rather than committing to one-dimensional play.

## Team Members

| Name           | Major            | Email   | Focus/Role (tentative)   |
| -------------- | ---------------- | ------- | ------------------------ |
| Kartavya Singh | Computer Science | **singhk6@mail.uc.edu**  | Netcode & Infrastructure |
| Saarthak Sinha | Computer Science | **sinhas6@mail.uc.edu**  | Combat/Systems & Tooling |
| Kanav Shetty   | Computer Science | **shettykv@mail.uc.edu** | UX/UI, VFX/SFX, Stages   |
| Yash Ballabh   | Computer Science | **ballabyh@mail.uc.edu** | Tools, CI/CD & QA        | 

## Vision
Most fighters over-index on a single style. Adaptabrawl blends attack, defense, and evasion, then layers in **adaptive conditions** (stage/weather/match modifiers) and **clear status effects** (e.g., poisoned, staggered/punched, heavy-attack state, low-HP state).

## Core Features (MVP → Beta)
- Local & **online** 1v1 (modes beyond 1v1 in stretch)
- Movement, light/heavy attacks, block/parry, dodge
- Status effects with readable UI (icons + timers)
- Adaptive conditions that modify stats/moves in fair, disclosed ways
- Controller + keyboard support

## Tech Stack (finalize in Week 2)
- **Primary:** Unity (LTS) with Netcode + Relay/Lobby
- **Fallback:** Godot 4 (GDScript) with ENet-based multiplayer
- Builds: Windows & macOS

## Getting Started
```bash
git clone https://github.com/Kartavya904/Adaptabrawl-Senior-Design.git
# Unity: Open via Unity Hub (LTS); press Play
# Godot (if used): godot4 -e to edit, godot4 to run
