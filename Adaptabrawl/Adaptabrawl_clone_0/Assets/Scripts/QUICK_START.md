# Adaptabrawl Quick Start Guide

## 🎮 Complete Fighter Setup in 5 Minutes

This guide shows you how to create a fully functional fighter with Shinabro prefabs, complete with hitboxes and hurtboxes—all configured through the Inspector!

---

## Step 1: Create a Fighter (2 minutes)

### Method A: Using the Wizard (Recommended)
1. Go to **`Adaptabrawl → Fighter Setup Wizard`**
2. Click **"Browse Shinabro Prefabs"** or drag from:
   - `Assets/Shinabro/Platform_Animation/Prefabs/`
   - Choose: `Player_Fighter`, `Player_Hammer`, `Player_Rapier`, etc.
3. Enter a **Fighter Name** (e.g., "Heavy Hammer")
4. Choose a **Stats Preset**:
   - **Fast**: Low health, high speed (hit-and-run)
   - **Balanced**: Medium everything (all-around)
   - **Tank**: High health, slow speed (defensive)
5. Click **"Create Fighter Definition"**
6. Save it in your project (e.g., `Assets/Fighters/Fighter_Hammer.asset`)

### Method B: Manual Creation
1. Right-click in Project → **`Create → Adaptabrawl → Fighter Definition`**
2. Name it (e.g., `Fighter_Rapier`)
3. In Inspector:
   - **Fighter Prefab**: Drag a Shinabro prefab
   - **Fighter Name**: Enter display name
   - **Stats**: Configure health, speed, etc.

**Result:** You now have a Fighter Definition with default hurtboxes!

---

## Step 2: Configure Hurtboxes (1 minute)

Hurtboxes define where your fighter can be hit.

### Default Setup (Already Done!)
Your FighterDef comes with:
- **Body hurtbox**: `(0, 0)`, Size: `(1, 2)`, Damage: ×1.0
- **Head hurtbox**: `(0, 1.5)`, Size: `(0.5, 0.5)`, Damage: ×1.2 (headshots!)

### Customizing (Optional)
1. Select your FighterDef
2. Scroll to **"Hurtbox Configuration"**
3. Expand any hurtbox to edit:
   - **Offset**: Move it (e.g., `(0, 2)` for tall character)
   - **Size**: Resize it (e.g., `(1.2, 2.2)` for big character)
   - **Damage Multiplier**: Change damage taken (e.g., `1.5` for weak point)
4. Click **"Add Hurtbox"** for more areas (legs, arms, etc.)

**Quick Presets:**
- **Tall Fighter**: Move head hurtbox up to `(0, 2)`
- **Tank Fighter**: Increase body size to `(1.2, 2.2)`, reduce damage to `0.9`
- **Critical Spots**: Add small hurtboxes with high damage multipliers

---

## Step 3: Create Attacks (2 minutes)

### Create a Light Attack
1. Right-click → **`Create → Adaptabrawl → Move Definition`**
2. Name it `LightAttack_Punch`
3. In Inspector:

**Basic Info:**
- **Move Name**: "Quick Punch"
- **Move Type**: Light Attack

**Frame Data:**
- **Startup**: 4 frames (fast startup)
- **Active**: 2 frames (hitbox active briefly)
- **Recovery**: 8 frames (quick recovery)

**Combat Properties:**
- **Damage**: 10
- **Knockback Force**: 3
- **Hitstun Frames**: 12

**Hitbox Configuration:**
- Click **"Add Hitbox"**
- **Name**: "Primary"
- **Offset**: `(0.8, 0)` (in front of fighter)
- **Size**: `(1.0, 0.8)`
- **Active Start Frame**: 0
- **Active End Frame**: -1 (uses move's active frames)
- **Damage Multiplier**: 1.0

### Create a Heavy Attack
1. Right-click → **`Create → Adaptabrawl → Move Definition`**
2. Name it `HeavyAttack_Slam`
3. Configure:

**Frame Data:**
- **Startup**: 12 frames (slower)
- **Active**: 4 frames (longer active)
- **Recovery**: 20 frames (punishable)

**Combat Properties:**
- **Damage**: 25
- **Knockback Force**: 8
- **Hitstun Frames**: 30

**Hitbox Configuration:**
- Click **"Add Hitbox"**
- **Name**: "Sweetspot"
- **Offset**: `(1.2, 0.2)`
- **Size**: `(0.8, 0.8)`
- **Damage Multiplier**: 1.5 (sweetspot!)
- Click **"Add Hitbox"** again
- **Name**: "Standard"
- **Offset**: `(0.8, 0)`
- **Size**: `(1.0, 1.0)`
- **Damage Multiplier**: 1.0

**Result:** Your heavy attack now has a sweetspot for skilled players!

---

## Step 4: Assign Moves to Fighter

1. Select your **FighterDef**
2. Scroll to **"Moveset"**
3. Drag your moves:
   - **Light Attack**: `LightAttack_Punch`
   - **Heavy Attack**: `HeavyAttack_Slam`

---

## Step 5: Spawn and Test

### Option A: Through Code
```csharp
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using UnityEngine;

public class TestSpawner : MonoBehaviour
{
    public FighterDef fighterDef;
    
    void Start()
    {
        // Spawn fighter at position
        FighterFactory.CreateFighter(fighterDef, new Vector3(0, 0, 0), facingRight: true);
    }
}
```

### Option B: In Scene
1. Drag your FighterDef into the scene
2. Unity will instantiate the prefab with all components

**What Happens Automatically:**
- ✅ Shinabro prefab instantiated with animations
- ✅ All combat components added
- ✅ Hurtboxes created from FighterDef
- ✅ Hitboxes ready to spawn during attacks
- ✅ Physics configured
- ✅ Everything working!

---

## Visual Editing (Bonus!)

### Scene View Gizmos
1. Place fighter in scene
2. Select the fighter GameObject
3. Scene view shows:
   - **Red boxes**: Hurtboxes (where you can be hit)
   - **Green boxes**: Hitboxes (when attacking)
4. **Drag handles** to reposition
5. **Drag edge dots** to resize
6. Changes save automatically!

### Inspector Preview
1. Select a **MoveDef** (attack)
2. Scroll to **"Hitbox Visual Preview"**
3. See layout of all hitboxes
4. Adjust **Preview Scale** slider to zoom

---

## Complete Example: Create a "Fast Duelist"

**1. Fighter Setup**
- **Prefab**: `Player_Rapier`
- **Name**: "Swift Duelist"
- **Preset**: Fast
- **Result**: 90 HP, 7.5 speed, 0.8 weight

**2. Hurtboxes** (keep defaults)
- Body: Normal damage
- Head: +20% damage

**3. Light Attack**
- **Name**: "Quick Thrust"
- **Frames**: 3 startup, 2 active, 6 recovery
- **Damage**: 8
- **Hitbox**: Small, precise `(0.9, 0)`, size `(0.8, 0.6)`

**4. Heavy Attack**
- **Name**: "Lunge Strike"
- **Frames**: 8 startup, 3 active, 15 recovery
- **Damage**: 18
- **Hitbox 1 - Tip**: `(1.5, 0)`, size `(0.4, 0.4)`, damage ×1.5 (sweetspot!)
- **Hitbox 2 - Blade**: `(1.0, 0)`, size `(0.6, 0.8)`, damage ×1.0

**5. Test**: Fast, precise fighter with rewarding sweetspot timing!

---

## Common Modifications (Inspector Only!)

### Make Fighter Tankier
1. Open FighterDef
2. Increase **Max Health**: `100 → 130`
3. Increase **Weight**: `1.0 → 1.4`
4. Hurtbox Body **Damage Multiplier**: `1.0 → 0.85`

### Add Sweetspot to Attack
1. Open MoveDef
2. Click **"Add Hitbox"**
3. Configure as small, high-damage area
4. Set **Damage Multiplier**: `1.5` or higher
5. Position farther from fighter (tip of weapon)

### Make Attack Faster
1. Open MoveDef
2. Reduce **Startup Frames**: `12 → 8`
3. Reduce **Recovery Frames**: `20 → 15`
4. Adjust **Damage** to compensate

### Add Multi-Hit Attack
1. Open MoveDef
2. Add multiple hitboxes with different timing:
   - Hitbox 1: Active frame 3-5
   - Hitbox 2: Active frame 8-10
   - Hitbox 3: Active frame 13-15

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **Fighter has no visuals** | Assign a Shinabro prefab in FighterDef |
| **Can't see hurtboxes in scene** | Toggle "Show Gizmos" on HitboxHurtboxSpawner |
| **Attacks don't hit** | Check hitbox offset is positive X (in front) |
| **Wrong damage values** | Verify damage multipliers in hurtbox/hitbox configs |
| **Fighter falls through floor** | Add ground colliders to your scene |

---

## Available Shinabro Prefabs

All in: `Assets/Shinabro/Platform_Animation/Prefabs/`

| Prefab | Style | Suggested Stats |
|--------|-------|-----------------|
| **Player_Fighter** | Martial artist | Fast, low damage |
| **Player_Sword&Shield** | Warrior | Balanced, defensive |
| **Player_Hammer** | Heavy fighter | Tank, high damage |
| **Player_DualBlades** | Assassin | Very fast, medium damage |
| **Player_Bow** | Archer | Ranged, low health |
| **Player_Pistol** | Gunslinger | Ranged, medium |
| **Player_Magic** | Mage | Ranged, low health, high damage |
| **Player_Spear** | Lancer | Medium range, balanced |
| **Player_Staff** | Monk | Balanced, magic attacks |
| **Player_Rapier** | Fencer | Fast, precise |
| **Player_DoubleBlades** | Berserker | Fast, aggressive |
| **Player_Claymore** | Knight | Tank, slow, high damage |

---

## Key Files & Locations

**Your Assets:**
- **Fighters**: Store FighterDef assets here
- **Moves**: Store MoveDef assets here

**Shinabro Assets:**
- **Prefabs**: `Assets/Shinabro/Platform_Animation/Prefabs/`
- **Animations**: `Assets/Shinabro/Platform_Animation/Animation/`

**Scripts:**
- **Data**: `Assets/Scripts/Data/` (FighterDef, MoveDef)
- **Combat**: `Assets/Scripts/Gameplay/Combat/` (Hitbox system)
- **Factory**: `Assets/Scripts/Fighters/FighterFactory.cs`

**Documentation:**
- **Prefab Setup**: `Assets/Scripts/FIGHTER_PREFAB_SETUP.md`
- **Hitbox System**: `Assets/Scripts/HITBOX_HURTBOX_SYSTEM.md`
- **This Guide**: `Assets/Scripts/QUICK_START.md`

---

## Next Steps

1. ✅ **Create 2-3 fighters** with different Shinabro prefabs
2. ✅ **Create 3-4 moves** per fighter (light, heavy, special)
3. ✅ **Test in scene** with visual gizmos
4. ✅ **Iterate on balance** by adjusting Inspector values
5. ✅ **Add status effects, specials, adaptations** (advanced features)

---

## Pro Tips

💡 **Start Simple**: Use defaults, then customize  
💡 **Visual Testing**: Always check Scene view gizmos  
💡 **Frame Data**: Lower frames = faster/safer, higher = slower/riskier  
💡 **Sweetspots**: Reward precision with 1.3-1.5× multipliers  
💡 **Hurtbox Strategy**: Small hurtboxes = harder to hit (elusive), large = easier (tank)  
💡 **Iterate Fast**: All Inspector changes = no recompile!  

---

## Summary

You now have:
- ✅ **Automatic hitbox/hurtbox generation**
- ✅ **Complete Inspector configuration**
- ✅ **Visual Scene editing**
- ✅ **Shinabro prefab integration**
- ✅ **Multiple fighters and attacks**

**All without manually creating a single hitbox GameObject!**

Ready to build your fighting game! 🎮⚔️

