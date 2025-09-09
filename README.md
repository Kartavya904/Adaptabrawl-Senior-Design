# Adaptabrawl
_A 2D multiplayer fighter where attack, defense, and evasion all matter—and adapt._

## Team
- Saarthak Sinha (CS)
- Kartavya Singh (CS)
- Kanav Shetty (CS)

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
