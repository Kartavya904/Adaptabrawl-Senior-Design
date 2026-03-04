# Fighter Prefab Setup Guide

## Overview

Your fighter framework now supports drag-and-drop Shinabro prefabs! This allows you to easily create fighters using the pre-made character models from the Shinabro asset pack.

## Available Shinabro Prefabs

Located in: `Assets/Shinabro/Platform_Animation/Prefabs/`

### Character Prefabs:

- **Player_Base** - Base character without weapons
- **Player_Fighter** - Unarmed martial artist
- **Player_Sword&Shield** - Sword and shield warrior
- **Player_Hammer** - Heavy hammer wielder
- **Player_DualBlades** - Dual blade fighter
- **Player_Bow** - Archer
- **Player_Pistol** - Gunslinger
- **Player_Magic** - Magic user
- **Player_Spear** - Spear fighter
- **Player_Staff** - Staff wielder
- **Player_Rapier** - Fencer
- **Player_DoubleBlades** - Double blade specialist
- **Player_Claymore** - Large sword wielder

## How to Create a New Fighter

### Step 1: Create a Fighter Definition

1. In Unity, right-click in the Project window
2. Select: `Create > Adaptabrawl > Fighter Definition`
3. Name it (e.g., "Fighter_Hammer", "Fighter_Rapier")

### Step 2: Assign the Shinabro Prefab

1. Select your newly created Fighter Definition
2. In the Inspector, find the **"Visual Prefab"** field
3. Drag one of the Shinabro prefabs from `Assets/Shinabro/Platform_Animation/Prefabs/` into this field
   - Example: Drag `Player_Hammer.prefab` for a hammer fighter

### Step 3: Configure Fighter Stats

Configure the fighter's properties in the Inspector:

#### Basic Info

- **Fighter Name**: Display name (e.g., "Heavy Hammer")
- **Description**: Brief description of playstyle
- **Portrait**: Character icon (optional)

#### Base Stats

- **Max Health**: Starting health (default: 100)
- **Move Speed**: Ground movement speed
- **Jump Force**: Jump height
- **Dash Speed**: Dash velocity
- **Dash Duration**: How long dashes last
- **Weight**: Affects knockback resistance (higher = harder to knock back)

#### Combat Stats

- **Base Damage Multiplier**: Affects all damage output
- **Base Defense Multiplier**: Affects damage received
- **Armor Break Threshold**: Hits needed to break armor

#### Moveset

- **Light Attack**: Assign a MoveDef for light attacks
- **Heavy Attack**: Assign a MoveDef for heavy attacks
- **Special Moves**: Array of special MoveDefs

### Step 4: Use Your Fighter

The fighter can now be spawned using the FighterFactory:

```csharp
// Load your fighter definition
FighterDef myFighter = Resources.Load<FighterDef>("Path/To/Your/FighterDef");

// Create the fighter at a position
FighterController fighter = FighterFactory.CreateFighter(myFighter, new Vector3(0, 0, 0));
```

## What Happens Behind the Scenes

When you create a fighter:

1. **Prefab Instantiation**: The Shinabro prefab is instantiated with all its animations, models, and existing components
2. **Component Integration**: Adaptabrawl components are automatically added:
   - FighterController (main controller)
   - CombatFSM (combat state machine)
   - HitboxManager (attack hitboxes)
   - DamageSystem (damage calculation)
   - Hurtbox (damage receiving)
   - MovementController (movement logic)
   - StatusEffectSystem (buffs/debuffs)
   - AttackSystem, DefenseSystem, EvadeSystem
   - PlayerInputHandler (input processing)
3. **Physics Setup**: Rigidbody2D is configured for proper physics
4. **Collider Verification**: Ensures collision detection works

## Example Fighter Configurations

### Aggressive Striker (Fighter)

- Prefab: `Player_Fighter`
- Max Health: 100
- Move Speed: 6
- Weight: 1.2
- Base Damage Multiplier: 1.1
- Strategy: Pressure with frame traps

### Mobile Duelist (Rapier)

- Prefab: `Player_Rapier`
- Max Health: 90
- Move Speed: 7.5
- Weight: 0.8
- Base Damage Multiplier: 0.9
- Strategy: Hit and run, dodge cancels

### Heavy Tank (Hammer)

- Prefab: `Player_Hammer`
- Max Health: 120
- Move Speed: 4.5
- Weight: 1.5
- Base Damage Multiplier: 1.3
- Strategy: Armor through attacks

### Balanced Warrior (Sword & Shield)

- Prefab: `Player_Sword&Shield`
- Max Health: 110
- Move Speed: 5.5
- Weight: 1.1
- Base Damage Multiplier: 1.0
- Base Defense Multiplier: 1.2
- Strategy: Defensive play with counters

## Fallback System

If you don't assign a prefab:

- The system creates a simple placeholder GameObject
- A white sprite is added for visibility
- All Adaptabrawl components are still added
- The fighter will function, but without Shinabro visuals/animations

## Tips

1. **Match Fighter Style to Weapon**: Choose prefabs that match your fighter's moveset (e.g., use `Player_Hammer` for heavy, slow fighters)

2. **Animation Controllers**: The Shinabro prefabs come with animation controllers - you can reference them in your move definitions for proper animations

3. **Weapon Prefabs**: Separate weapon prefabs are available if you want to swap weapons dynamically:

   - `Weapon_Bow`, `Weapon_Hammer`, `Weapon_Sword`, etc.

4. **Test in Editor**: You can drag fighter prefabs directly into scenes during testing

5. **Organizational Tip**: Create a folder structure:
   ```
   Assets/
     Fighters/
       Definitions/
         Fighter_Hammer.asset
         Fighter_Rapier.asset
         Fighter_Magic.asset
   ```

## Common Issues

### Issue: Fighter has no visuals

**Solution**: Make sure you've assigned a prefab in the "Visual Prefab" field

### Issue: Fighter falls through the ground

**Solution**: Check that your scene has proper collision layers and ground colliders

### Issue: Animations don't play

**Solution**: Verify the Shinabro prefab's Animator component is intact and has a controller assigned

### Issue: Multiple fighters interfere with each other

**Solution**: Ensure each fighter has unique collision layers or tags as needed

## Next Steps

1. Create several Fighter Definitions with different Shinabro prefabs
2. Test different stat combinations
3. Create custom MoveDefs for each fighter's playstyle
4. Fine-tune the adaptation rules for dynamic gameplay

For move creation, see: `MoveDef` documentation
For combat mechanics, see: `COMBAT_SYSTEM.md`
