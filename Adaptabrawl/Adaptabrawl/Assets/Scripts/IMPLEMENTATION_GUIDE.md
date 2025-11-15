# Adaptabrawl Implementation Guide

This document provides an overview of the implemented systems and how to use them.

## Project Structure

### Core Systems

#### 1. Data Layer (`Scripts/Data/`)
- **FighterDef.cs**: ScriptableObject for fighter definitions (stats, movesets, adaptation rules)
- **MoveDef.cs**: ScriptableObject for move definitions (frame data, damage, hitboxes)
- **StatusDef.cs**: ScriptableObject for status effect definitions (poison, buffs, debuffs)
- **ConditionDef.cs**: ScriptableObject for adaptive condition definitions (stage modifiers, weather)

#### 2. Gameplay Systems (`Scripts/Gameplay/`)
- **FighterController.cs**: Main controller for fighters (health, facing, initialization)
- **MovementController.cs**: Handles movement, jumping, dashing, ground detection
- **CombatFSM.cs**: Combat state machine (idle, startup, active, recovery, etc.)
- **StatusEffectSystem.cs**: Manages status effects (application, stacking, timers, DoT)
- **AdaptiveConditionSystem.cs**: Manages match conditions and modifiers
- **GameManager.cs**: Match management, rounds, win conditions, rematch

#### 3. Combat Systems (`Scripts/Gameplay/Combat/`)
- **CombatState.cs**: Enum for combat states
- **HitboxManager.cs**: Manages hitbox activation and collision detection
- **Hurtbox.cs**: Component for hurtbox detection
- **DamageSystem.cs**: Handles damage calculation, knockback, hitstop, armor break

#### 4. Action Systems
- **Attack/AttackSystem.cs**: Light and heavy attack handling
- **Defend/DefenseSystem.cs**: Block and parry mechanics
- **Evade/EvadeSystem.cs**: Dodge with invincibility frames

#### 5. Input System (`Scripts/Input/`)
- **PlayerInputHandler.cs**: Unity Input System integration for all player inputs

#### 6. UI Systems (`Scripts/UI/`)
- **HUDManager.cs**: Health bars, status icons, condition banners
- **StatusIcon.cs**: Individual status effect icon with timer/stack display
- **LobbyUI.cs**: Lobby interface (create/join room, ready states)

#### 7. Networking (`Scripts/Networking/`)
- **NetworkFighter.cs**: Network synchronization for fighters (Mirror-ready)
- **NetworkManager.cs**: Network session management (Mirror-ready)
- **LobbyManager.cs**: Room code system, ready states, match start

#### 8. Camera (`Scripts/Camera/`)
- **FightingGameCamera.cs**: 2D camera that follows both players, adjusts based on distance

#### 9. VFX (`Scripts/VFX/`)
- **VFXManager.cs**: Visual effects for hits, blocks, status effects

#### 10. Settings (`Scripts/Settings/`)
- **SettingsManager.cs**: Audio, video, accessibility settings with persistence

#### 11. Fighters (`Scripts/Fighters/`)
- **FighterFactory.cs**: Factory for creating fighters, includes Striker and Elusive definitions

## Setup Instructions

### 1. Install Mirror Networking (Optional)
If you want to use networking, install Mirror:
1. Open Unity Package Manager
2. Click "+" > "Add package from git URL"
3. Enter: `https://github.com/vis2k/Mirror.git`
4. Uncomment Mirror code in `NetworkFighter.cs` and `NetworkManager.cs`

### 2. Create Fighter ScriptableObjects
1. Right-click in Project > Create > Adaptabrawl > Fighter Definition
2. Configure stats, moves, adaptation rules
3. Assign to FighterController components

### 3. Create Move ScriptableObjects
1. Right-click in Project > Create > Adaptabrawl > Move Definition
2. Configure frame data, damage, hitboxes
3. Assign to FighterDef movesets

### 4. Setup Scene
1. Create two Fighter GameObjects
2. Add FighterController component
3. Assign FighterDef ScriptableObject
4. Add all required components (they should auto-add via FighterFactory)
5. Add HUDManager to scene
6. Add GameManager to scene
7. Add FightingGameCamera to scene

### 5. Setup Input
1. Ensure InputSystem_Actions.inputactions is in Assets
2. Add PlayerInputHandler to fighters
3. Configure input bindings in Input Actions asset

## Usage Examples

### Creating a Fighter Programmatically
```csharp
using Adaptabrawl.Fighters;
using Adaptabrawl.Data;

// Create Striker fighter
FighterDef striker = FighterFactory.CreateStrikerFighter();
FighterController fighter = FighterFactory.CreateFighter(
    striker, 
    new Vector3(-5f, 0f, 0f), 
    facingRight: true
);
```

### Applying a Status Effect
```csharp
var statusSystem = fighter.GetComponent<StatusEffectSystem>();
statusSystem.ApplyStatus(poisonStatusDef, stacks: 1, duration: 5f);
```

### Activating a Condition
```csharp
var conditionSystem = FindObjectOfType<AdaptiveConditionSystem>();
conditionSystem.ActivateCondition(slipperyFloorCondition);
```

### Starting a Match
```csharp
var gameManager = FindObjectOfType<GameManager>();
// Match starts automatically when GameManager initializes
```

## Key Features Implemented

✅ **Combat System**
- Frame-perfect combat FSM
- Hit/hurtbox detection
- Damage calculation with modifiers
- Hitstop and knockback
- Armor and armor break
- Cancel windows and input buffering

✅ **Movement System**
- Ground-based movement
- Jumping with air control
- Dashing with cooldown
- Ground detection
- Friction and physics

✅ **Status Effects**
- Poison (DoT)
- Heavy Attack state
- Low HP state
- Stacking and timers
- Visual indicators

✅ **Adaptive Conditions**
- Stage modifiers
- Weather effects
- Match modifiers
- Transparent stat changes
- UI banners

✅ **Networking (Structure)**
- Mirror-ready architecture
- Host-authoritative model
- Client prediction hooks
- Room code system
- Lobby management

✅ **UI Systems**
- Health bars
- Status icons with timers
- Condition banners
- Lobby interface

✅ **Settings**
- Audio settings
- Video settings
- Accessibility options
- Input remapping structure

## Next Steps

1. **Install Mirror Networking** if you want online play
2. **Create Visual Assets** (sprites, animations, VFX prefabs)
3. **Create Audio Assets** (hit sounds, music, etc.)
4. **Build UI Prefabs** in Unity Editor
5. **Create ScriptableObject Assets** for fighters, moves, statuses, conditions
6. **Test and Balance** frame data and damage values
7. **Add Animations** to fighters
8. **Polish VFX/SFX** for better feedback

## Notes

- All systems are designed to work offline first
- Networking code is structured but requires Mirror package
- Some systems have placeholder implementations that need asset assignment
- Performance optimization should be done after content is added
- The project follows a data-driven design with ScriptableObjects

## Troubleshooting

**Fighters not moving?**
- Check that MovementController is attached
- Verify Input System is set up correctly
- Check PlayerInputHandler is enabled

**Combat not working?**
- Ensure CombatFSM, HitboxManager, and DamageSystem are attached
- Check that FighterDef and MoveDef are assigned
- Verify hit/hurtbox layers are set correctly

**Status effects not showing?**
- Check HUDManager is in scene
- Verify StatusIcon prefab is assigned
- Ensure StatusEffectSystem is attached to fighters

**Networking not working?**
- Install Mirror Networking package
- Uncomment Mirror code in NetworkFighter and NetworkManager
- Configure network settings

