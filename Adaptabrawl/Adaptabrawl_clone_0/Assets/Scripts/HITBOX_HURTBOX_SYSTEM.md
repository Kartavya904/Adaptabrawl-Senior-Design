# Hitbox & Hurtbox System Guide

## Overview
The Adaptabrawl hitbox and hurtbox system provides **automatic, Inspector-configurable collision detection** for all fighters and attacks. You can define hitboxes and hurtboxes entirely through the Inspector—no manual GameObject creation needed!

## Key Features
✅ **Automatic Generation**: Hitboxes and hurtboxes are automatically created when fighters spawn  
✅ **Inspector-Only Configuration**: All modifications done through the Inspector  
✅ **Visual Editing**: Scene view gizmos and handles for intuitive positioning  
✅ **Multiple Hitboxes**: Support for complex attacks with multiple, timed hitboxes  
✅ **Damage Multipliers**: Different body parts can take different damage (headshots, weak points)  
✅ **Frame-Perfect Timing**: Hitboxes activate on specific frames  
✅ **Prefab Compatible**: Works seamlessly with Shinabro prefabs  

---

## Hurtboxes (Where Fighter Can Be Hit)

### Configuring Hurtboxes

Hurtboxes are defined in **FighterDef** and automatically created when the fighter spawns.

#### Step 1: Open Your Fighter Definition
1. Select your FighterDef asset in the Project window
2. Scroll to the **"Hurtbox Configuration"** section

#### Step 2: Add/Edit Hurtboxes
The Inspector provides an easy interface:
- **Add Hurtbox**: Creates a new hurtbox
- **Reset to Default**: Restores default configuration (body + head)
- **Delete (×)**: Removes a specific hurtbox

#### Hurtbox Properties

| Property | Description | Example |
|----------|-------------|---------|
| **Name** | Identification label | "Head", "Body", "Legs" |
| **Offset** | Position relative to fighter center | `(0, 1.5)` for head |
| **Size** | Dimensions (width, height) | `(1, 2)` for body |
| **Active by Default** | Is this hurtbox enabled at spawn? | Usually `true` |
| **Damage Multiplier** | Multiplier for damage received | `1.2` = 20% more damage |
| **Editor Color** | Color in Scene view | Red for critical areas |

#### Example Configurations

**Standard Fighter:**
```
Hurtbox 1 - Body
  Offset: (0, 0)
  Size: (1, 2)
  Damage Multiplier: 1.0

Hurtbox 2 - Head
  Offset: (0, 1.5)
  Size: (0.5, 0.5)
  Damage Multiplier: 1.2
```

**Tall Fighter (Spear/Staff):**
```
Hurtbox 1 - Torso
  Offset: (0, 0.5)
  Size: (0.8, 1.5)
  Damage Multiplier: 1.0

Hurtbox 2 - Head
  Offset: (0, 2)
  Size: (0.5, 0.5)
  Damage Multiplier: 1.3

Hurtbox 3 - Legs
  Offset: (0, -1)
  Size: (0.6, 1)
  Damage Multiplier: 0.8
```

**Heavy Fighter (Tank):**
```
Hurtbox 1 - Body
  Offset: (0, 0)
  Size: (1.2, 2.2)
  Damage Multiplier: 0.9 (armored)

Hurtbox 2 - Head
  Offset: (0, 1.8)
  Size: (0.6, 0.6)
  Damage Multiplier: 1.1
```

### Visual Editing in Scene View

1. **Select** a GameObject with `HitboxHurtboxSpawner` component
2. **Scene view** will display colored wireframe boxes
3. **Click and drag** the position handles to move hurtboxes
4. **Click dots** on edges to resize
5. Changes are **automatically saved** to the FighterDef

---

## Hitboxes (Where Fighter Hits Enemies)

### Configuring Hitboxes

Hitboxes are defined in **MoveDef** (your attack definitions) and automatically spawned during attacks.

#### Step 1: Open Your Move Definition
1. Select your MoveDef asset (e.g., `LightAttack_01`)
2. Scroll to **"Hitbox Configuration"** section

#### Step 2: Add/Edit Hitboxes
The Inspector provides:
- **Add Hitbox**: Creates a new hitbox for this move
- **Clear All**: Removes all hitboxes
- **Visual Preview**: Shows hitbox layout relative to fighter

#### Hitbox Properties

| Property | Description | Example |
|----------|-------------|---------|
| **Name** | Identification label | "Primary", "Sweetspot", "Late" |
| **Offset** | Position relative to fighter | `(0.8, 0)` = in front |
| **Size** | Dimensions (width, height) | `(1.2, 1.0)` |
| **Active Start Frame** | When hitbox activates (0 = startup begins) | `5` = frame 5 |
| **Active End Frame** | When hitbox deactivates (-1 = auto) | `8` or `-1` |
| **Damage Multiplier** | Multiplier for this specific hitbox | `1.5` = sweetspot |
| **Knockback Multiplier** | Knockback strength modifier | `1.2` = stronger |
| **Knockback Direction Override** | Custom knockback (0,0 = use move's) | `(1, 0.5)` = up-right |
| **Is Sweetspot** | Special effects/sound? | `true` for tipper hits |
| **Editor Color** | Color in Scene view | Green for active |

#### Multiple Hitboxes Example

**Sword Slash (3 hitboxes for different ranges):**
```
Hitbox 1 - Blade Tip (Sweetspot)
  Offset: (1.5, 0.2)
  Size: (0.4, 0.4)
  Active: Frame 3-5
  Damage Multiplier: 1.5
  Knockback Multiplier: 1.3
  Is Sweetspot: true

Hitbox 2 - Mid Blade
  Offset: (1.0, 0)
  Size: (0.6, 0.8)
  Active: Frame 3-6
  Damage Multiplier: 1.0
  Knockback Multiplier: 1.0

Hitbox 3 - Hilt
  Offset: (0.5, 0)
  Size: (0.5, 0.6)
  Active: Frame 3-6
  Damage Multiplier: 0.7
  Knockback Multiplier: 0.7
```

**Multi-Hit Attack (hits at different times):**
```
Hitbox 1 - First Strike
  Offset: (0.8, 0.5)
  Size: (1.0, 0.8)
  Active: Frame 3-5
  Damage Multiplier: 0.8

Hitbox 2 - Second Strike
  Offset: (0.9, 0)
  Size: (1.1, 1.0)
  Active: Frame 8-10
  Damage Multiplier: 0.9

Hitbox 3 - Final Strike
  Offset: (1.2, 0.2)
  Size: (1.3, 1.2)
  Active: Frame 15-18
  Damage Multiplier: 1.3
  Knockback Multiplier: 1.5
```

### Visual Preview

The **Hitbox Visual Preview** in the MoveDef Inspector shows:
- Fighter position (cyan circle)
- All hitboxes with their relative positions
- Color-coded by gizmo color
- Labels with damage multipliers

Adjust the **Preview Scale** slider to zoom in/out.

---

## How It Works (Technical)

### System Architecture

```
FighterController (on fighter GameObject)
    ↓
HitboxHurtboxSpawner (auto-added by FighterFactory)
    ↓ reads from
FighterDef.hurtboxes[] → Creates hurtbox GameObjects on Start()
    ↓
    ├─ Hurtbox GameObject 1 (Body)
    │   └─ BoxCollider2D + Hurtbox component
    ├─ Hurtbox GameObject 2 (Head)
    │   └─ BoxCollider2D + Hurtbox component
    └─ ...

When attack starts:
    ↓
MoveDef.hitboxDefinitions[] → Creates hitbox GameObjects
    ↓
    ├─ Hitbox GameObject 1 (Primary)
    │   └─ BoxCollider2D (enabled/disabled by frame)
    ├─ Hitbox GameObject 2 (Sweetspot)
    └─ ...
```

### Damage Calculation Flow

```
Hit Detection
    ↓
Hitbox collides with Hurtbox
    ↓
HitboxManager.HandleHit()
    ↓
Get hurtbox damage multiplier (e.g., 1.2 for head)
    ↓
DamageSystem.DealDamage(target, move, hurtboxMultiplier)
    ↓
Final Damage = move.damage × fighter.damageMultiplier × hurtboxMultiplier × statusEffects
```

### Component Responsibilities

| Component | Responsibility |
|-----------|----------------|
| **HitboxHurtboxSpawner** | Creates and manages hitbox/hurtbox GameObjects |
| **HitboxManager** | Handles collision detection and hit events |
| **DamageSystem** | Calculates and applies damage |
| **Hurtbox** | Marks GameObject as hittable, stores owner |

---

## Workflow Examples

### Creating a New Fighter

1. **Create FighterDef**: `Right-click → Create → Adaptabrawl → Fighter Definition`
2. **Assign prefab**: Drag a Shinabro prefab to "Fighter Prefab" field
3. **Configure hurtboxes**:
   - Use default (body + head) or customize
   - Adjust sizes in Scene view if needed
4. **Test**: Spawn fighter in game—hurtboxes automatically created!

### Creating a New Attack

1. **Create MoveDef**: `Right-click → Create → Adaptabrawl → Move Definition`
2. **Set frame data**: Startup, Active, Recovery frames
3. **Add hitbox**: Click "Add Hitbox" button
4. **Configure hitbox**:
   - Set offset (where it appears)
   - Set size (how big)
   - Set active frames (when it's dangerous)
   - Set damage multiplier (sweetspots)
5. **Preview**: Check Visual Preview to see layout
6. **Test**: Assign to fighter's moveset and test in game!

### Adjusting Existing Hitboxes

**Quick Inspector Adjustments:**
1. Select MoveDef in Project
2. Expand hitbox you want to change
3. Modify values:
   - Offset: Move it forward/back/up/down
   - Size: Make it bigger/smaller
   - Frames: Change timing
   - Multipliers: Adjust damage/knockback
4. Changes apply immediately

**Visual Scene Adjustments:**
1. Place fighter in scene
2. Select fighter GameObject
3. View hurtboxes in Scene view
4. Drag handles to reposition/resize
5. Saved to FighterDef automatically

---

## Advanced Features

### Conditional Hurtboxes

Disable hurtboxes during certain moves (e.g., invincibility frames):

```csharp
// In your attack script:
HitboxHurtboxSpawner spawner = GetComponent<HitboxHurtboxSpawner>();
spawner.SetHurtboxActive("Head", false); // Disable head hurtbox
// ... during dodge/counter ...
spawner.SetHurtboxActive("Head", true); // Re-enable
```

### Custom Hitbox Activation

For complex timing beyond frame-based:

```csharp
HitboxHurtboxSpawner spawner = GetComponent<HitboxHurtboxSpawner>();
var hitboxes = spawner.SpawnHitboxesForMove(myMoveDef);

// Manually control activation
foreach (var hitbox in hitboxes)
{
    hitbox.collider.enabled = myCustomCondition;
}
```

### Query Hitbox/Hurtbox Data

```csharp
HitboxHurtboxSpawner spawner = GetComponent<HitboxHurtboxSpawner>();

// Get all hurtboxes
List<HurtboxInstance> hurtboxes = spawner.GetHurtboxes();

// Get active hitboxes
List<HitboxInstance> hitboxes = spawner.GetHitboxes();

// Check damage multiplier
float mult = spawner.GetHurtboxDamageMultiplier(collider);
```

---

## Debugging & Visualization

### Scene View Gizmos

**Enable/Disable:**
- Select GameObject with `HitboxHurtboxSpawner`
- Toggle "Show Gizmos" in Inspector
- Toggle "Show Labels" for text overlays

**Color Coding:**
- **Red/Orange**: Hurtboxes (vulnerable areas)
- **Green/Yellow**: Hitboxes (attack areas)
- **Brighter = Active**, **Dimmer = Inactive**

### Runtime Debugging

The Inspector shows runtime information when game is playing:
- **Active Hurtboxes**: Current count
- **Active Hitboxes**: Current count (during attack)

### Common Issues

| Issue | Solution |
|-------|----------|
| **Hurtboxes not appearing** | Ensure FighterDef has hurtboxes defined |
| **Hitboxes not hitting** | Check layer masks in HitboxManager |
| **Wrong damage** | Verify damage multipliers in hurtbox/hitbox defs |
| **Timing feels off** | Adjust activeStartFrame/activeEndFrame |
| **Can't see in Scene** | Enable "Show Gizmos" on HitboxHurtboxSpawner |

---

## Quick Reference

### Default Hurtbox Setup
```
Body: (0, 0), Size: (1, 2), Damage: ×1.0
Head: (0, 1.5), Size: (0.5, 0.5), Damage: ×1.2
```

### Default Hitbox Setup
```
Primary: (0.5, 0), Size: (1, 1), Frames: 0 to -1, Damage: ×1.0
```

### Frame Timing
- **Frame 0**: Attack starts (startup begins)
- **Startup Frames**: Pre-hit frames (vulnerable)
- **Active Frames**: Hitbox is dangerous
- **Recovery Frames**: Post-hit frames (vulnerable)

### Multiplier Guidelines
- **0.5-0.8**: Weak/glancing hits
- **1.0**: Normal hits
- **1.2-1.5**: Strong/critical hits
- **1.5+**: Sweetspots/finishers

---

## Integration with Shinabro Prefabs

The system works seamlessly with Shinabro prefabs:

1. **Visual models** from prefab
2. **Combat logic** from Adaptabrawl (hitboxes/hurtboxes)
3. **Automatic setup** by FighterFactory

No manual GameObject placement needed—everything configured through ScriptableObjects!

---

## Editor Tools

### Menu Items
- **Adaptabrawl → Fighter Setup Wizard**: Quick fighter creation with prefabs
- **Create → Adaptabrawl → Fighter Definition**: Manual fighter creation
- **Create → Adaptabrawl → Move Definition**: Create attack definition

### Inspector Features
- **FighterDef Editor**: Hurtbox management with add/delete/reset
- **MoveDef Editor**: Hitbox management with visual preview
- **HitboxHurtboxSpawner Editor**: Scene view handles and gizmos

---

## Best Practices

1. **Start with defaults**: Use preset hurtboxes, adjust as needed
2. **Test early**: Place fighter in scene and verify hurtbox placement
3. **Use sweetspots**: Add 1.3-1.5× multiplier hitboxes for skill-based damage
4. **Frame data matters**: Fast attacks = few active frames, slow = many
5. **Visual feedback**: Use different gizmo colors to distinguish hitbox types
6. **Iterate quickly**: All changes in Inspector = no code recompilation

---

## Examples by Fighter Type

### Fast Fighter (Rapier/Dual Blades)
- Small, precise hurtboxes
- Quick, small hitboxes (active 2-3 frames)
- Multiple hitboxes for combo attacks

### Heavy Fighter (Hammer/Claymore)
- Large hurtboxes (bigger target)
- Large hitboxes with high damage multipliers
- Long active frames (slower but safer)

### Ranged Fighter (Bow/Pistol)
- Standard hurtboxes
- Projectile hitboxes (isProjectile = true)
- Small melee hitbox for close range

### Magic User
- Standard or slightly smaller hurtboxes
- Large area-of-effect hitboxes
- Multiple timed hitboxes for spell effects

---

## Summary

✅ **Hurtboxes** configured in **FighterDef** (per character)  
✅ **Hitboxes** configured in **MoveDef** (per attack)  
✅ **Both** automatically created at runtime  
✅ **All** edited through Inspector  
✅ **Visual** editing with Scene view gizmos  
✅ **Zero** manual GameObject creation needed  

**You only need to change values in the Inspector!**

