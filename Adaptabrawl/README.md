# Adaptabrawl Fighting Game

A data-driven 2D fighting game built with Unity, featuring the Shinabro animation system and modular fighter creation.

## Overview

Adaptabrawl is a complete fighting game framework with:

- **12 weapon types** with unique movesets
- **ScriptableObject-based architecture** for easy fighter creation
- **Frame-perfect combat system** with hitbox/hurtbox mechanics
- **Inspector-driven workflow** - no manual GameObject creation needed
- **Shinabro animation integration** for professional character animations

## Features

- ✅ Drag-and-drop fighter creation through Unity Inspector
- ✅ Automatic hitbox/hurtbox generation
- ✅ Pre-configured attack libraries (16+ moves per weapon type)
- ✅ Visual Scene editing with gizmos
- ✅ Combo system with customizable chains
- ✅ Sweetspot mechanics for skilled play
- ✅ Status effects (stun, slow, knockback, etc.)
- ✅ Two-player local multiplayer support

## Quick Start

### Requirements

- Unity 2021.3 or later
- Universal Render Pipeline (URP)
- Input System package

### Getting Started

1. **Create a Fighter** (2 minutes)

   - Go to `Adaptabrawl → Fighter Setup Wizard`
   - Select a Shinabro prefab
   - Choose stats preset (Fast/Balanced/Tank)
   - Click "Create Fighter Definition"

2. **Generate Moves** (1 minute)

   - Go to `Adaptabrawl → Move Library Generator`
   - Select weapon type
   - Click "Generate Move Library"
   - Result: 16+ attacks with hitboxes created!

3. **Assign Moves** (1 minute)
   - Open your Fighter Definition
   - Drag moves from library to moveset slots
   - Ready to play!

For detailed setup instructions, see `COMPLETE_SYSTEM_OVERVIEW.md` and `QUICK_START.md`.

## Project Structure

```
Adaptabrawl/
├── Assets/
│   ├── Scenes/              # Game scenes
│   ├── Scripts/             # Core game systems
│   │   ├── Data/           # ScriptableObject definitions
│   │   ├── Gameplay/       # Combat and movement systems
│   │   ├── Fighters/       # Fighter factory and controllers
│   │   └── Editor/         # Custom Unity Editor tools
│   ├── Shinabro/           # Character animations and prefabs
│   └── Moves/              # Pre-configured move libraries
├── COMPLETE_SYSTEM_OVERVIEW.md  # Comprehensive system documentation
└── QUICK_START.md               # 5-minute setup guide

```

## Supported Weapon Types

| Weapon          | Style        | Speed     | Damage      |
| --------------- | ------------ | --------- | ----------- |
| Unarmed Fighter | Martial arts | Fast      | Low-Medium  |
| Sword & Shield  | Balanced     | Medium    | Medium      |
| Hammer          | Heavy        | Slow      | High        |
| Dual Blades     | Assassin     | Very Fast | Medium      |
| Bow             | Ranged       | Medium    | Medium      |
| Pistol          | Ranged       | Fast      | Medium      |
| Magic           | Caster       | Medium    | High        |
| Spear           | Mid-range    | Medium    | Medium      |
| Staff           | Hybrid       | Medium    | Medium      |
| Rapier          | Fencer       | Fast      | Medium      |
| Double Blades   | Berserker    | Fast      | Medium-High |
| Claymore        | Knight       | Slow      | Very High   |

## Architecture

### Data-Driven Design

- **FighterDef** - ScriptableObject defining fighter stats, hurtboxes, and movesets
- **MoveDef** - ScriptableObject defining attacks with frame data and hitboxes
- **AnimatedMoveDef** - Extended MoveDef with animation integration

### Key Systems

- **FighterFactory** - Spawns fighters with all components configured
- **HitboxHurtboxSpawner** - Manages collision detection for combat
- **AnimationBridge** - Syncs combat with Shinabro animations
- **MovementController** - Handles player movement and ground detection
- **OptimizedTwoPlayerInput** - Separate keybinds for two players

## Controls

### Player 1

- Movement: Arrow Keys
- Light Attack: F
- Heavy Attack: G
- Special 1: R
- Special 2: T

### Player 2

- Movement: NumPad (8/5/4/6)
- Light Attack: NumPad 1
- Heavy Attack: NumPad 2
- Special 1: NumPad 3
- Special 2: NumPad Plus

## Development

### Creating Custom Fighters

1. Use Fighter Setup Wizard or create FighterDef manually
2. Configure stats (health, speed, weight)
3. Set up hurtboxes in Inspector
4. Assign moves from libraries or create custom ones

### Creating Custom Moves

1. Right-click → `Create → Adaptabrawl → Animated Move Definition`
2. Configure frame data (startup, active, recovery)
3. Set damage and knockback
4. Add hitboxes with Scene view gizmo editor

### Visual Editing

- Scene view shows hurtboxes (red) and hitboxes (green)
- Drag handles to reposition
- Drag edge dots to resize
- Changes save automatically

## Team

Senior Design Project - Fall 2025

## License

[Add your license here]

## Acknowledgments

- Shinabro animation system for character animations
- Unity Technologies for the game engine

