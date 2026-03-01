# Adaptabrawl Complete System Overview

## 🎮 Your Complete Fighting Game Framework

Congratulations! You now have a **fully integrated, Inspector-configurable fighting game system** that combines:
- ✅ Shinabro animations
- ✅ Automatic hitbox/hurtbox generation
- ✅ Pre-configured attack libraries
- ✅ Frame-perfect combat
- ✅ Drag-and-drop fighter creation

**Everything configured through the Unity Inspector—no manual GameObject creation, no coding required!**

---

## 🚀 What You Can Do Now

### Create a Complete Fighter in 3 Minutes

**Step 1: Generate Moves (60 seconds)**
```
1. Adaptabrawl → Move Library Generator
2. Select Weapon Type (e.g., Fighter, Hammer, Rapier)
3. Click "Generate Move Library"
Result: 16+ attacks with hitboxes created!
```

**Step 2: Create Fighter (60 seconds)**
```
1. Adaptabrawl → Fighter Setup Wizard
2. Select Shinabro Prefab (e.g., Player_Fighter)
3. Choose Stats Preset (Fast/Balanced/Tank)
4. Create Fighter Definition
Result: Fighter with hurtboxes created!
```

**Step 3: Assign Moves (60 seconds)**
```
1. Open FighterDef
2. Moveset section:
   - Light Attack: Drag Attack1 from library
   - Heavy Attack: Drag Attack3 from library
   - Special Moves: Drag skills from library
Result: Fully playable fighter!
```

**Total Time: 3 minutes**
**Result: Complete fighter with 16+ moves, all with hitboxes!**

---

## 📦 System Components Overview

### 1. Fighter System
**Location:** `Assets/Scripts/Data/FighterDef.cs`

**Features:**
- Drag-and-drop Shinabro prefabs
- Configurable stats (health, speed, damage)
- Automatic hurtbox generation
- Multiple hurtbox zones (body, head, etc.)
- Damage multipliers per body part

**Tools:**
- Fighter Setup Wizard
- Custom Inspector with presets
- Scene view visual editing

### 2. Move System
**Location:** `Assets/Scripts/Data/MoveDef.cs` + `AnimatedMoveDef.cs`

**Features:**
- Animation integration
- Multiple hitbox support
- Frame-perfect timing
- Sweetspot mechanics
- Combo chains
- Auto-calculated frame data

**Tools:**
- Move Library Generator
- Custom Inspector with visual preview
- Hitbox preset templates

### 3. Hitbox/Hurtbox System
**Location:** `Assets/Scripts/Gameplay/Combat/HitboxHurtboxSpawner.cs`

**Features:**
- Automatic creation on spawn
- Inspector-only configuration
- Scene view visual editing
- Damage multipliers
- Frame-based activation

**Tools:**
- Scene view gizmo editor
- Handles for positioning/resizing
- Real-time preview

### 4. Animation Bridge
**Location:** `Assets/Scripts/Gameplay/AnimationBridge.cs`

**Features:**
- Syncs animations with combat
- Triggers hitboxes during animations
- Combo window management
- Frame tracking
- Event system

### 5. Move Libraries
**Location:** `Assets/Scripts/Data/MoveLibrary.cs`

**Features:**
- Pre-organized movesets
- 16+ moves per weapon type
- Combo chains pre-configured
- Easy assignment to fighters

**Tools:**
- Move Library Generator (auto-creates all moves)

---

## 🎯 12 Weapon Types Supported

| Weapon | Style | Speed | Damage |
|--------|-------|-------|--------|
| **Unarmed Fighter** | Martial arts | Fast | Low-Medium |
| **Sword & Shield** | Balanced | Medium | Medium |
| **Hammer** | Heavy | Slow | High |
| **Dual Blades** | Assassin | Very Fast | Medium |
| **Bow** | Ranged | Medium | Medium |
| **Pistol** | Ranged | Fast | Medium |
| **Magic** | Caster | Medium | High |
| **Spear** | Mid-range | Medium | Medium |
| **Staff** | Hybrid | Medium | Medium |
| **Rapier** | Fencer | Fast | Medium |
| **Double Blades** | Berserker | Fast | Medium-High |
| **Claymore** | Knight | Slow | Very High |

Each weapon has **16+ unique attacks** with appropriate hitboxes!

---

## 📊 Attack Types Available

### Ground Attacks
- **Attack 1**: Quick jab (8 damage)
- **Attack 2**: Medium hit (10 damage)
- **Attack 3**: Heavy finisher (15 damage) with sweetspot

### Aerial Attacks
- **Jump Attack 1, 2, 3**: Air combos with downward angles

### Special Skills
- **Skill 1 - Float**: Launcher attack
- **Skill 2 - Slow**: Slowing effect
- **Skill 3 - Stun**: Stun attack
- **Skill 4 - Push**: High knockback
- **Skill 5 - Pull**: Pull enemy
- **Skill 6 - Move**: Dash attack
- **Skill 7 - Around**: 360° spin
- **Skill 8 - Air**: Aerial special

### Defensive
- **Dodge, Dodge Roll**: Evasion
- **Block**: Defense
- **Counter Attacks**: Dodge attack, crouch attack

---

## 🛠️ Quick Reference

### Creating Fighters
```
Menu: Adaptabrawl → Fighter Setup Wizard
Or: Right-click → Create → Adaptabrawl → Fighter Definition
```

### Generating Moves
```
Menu: Adaptabrawl → Move Library Generator
Select weapon → Generate → Done!
```

### Creating Custom Moves
```
Right-click → Create → Adaptabrawl → Animated Move Definition
Configure in Inspector
```

### Editing Hitboxes
```
Select fighter in scene
Scene view shows gizmos
Drag handles to edit
```

### Editing Hurtboxes
```
Select FighterDef
Hurtbox Configuration section
Add/Edit/Delete in Inspector
```

---

## 📚 Documentation Index

### Getting Started
1. **QUICK_START.md** - 5-minute tutorial
2. **FIGHTER_PREFAB_SETUP.md** - Fighter creation guide
3. **HITBOX_HURTBOX_SYSTEM.md** - Hitbox system guide
4. **SHINABRO_ANIMATION_SYSTEM.md** - Animation integration guide

### Reference
5. **HITBOX_PATTERNS.md** - Copy-paste hitbox templates
6. **COMPLETE_SYSTEM_OVERVIEW.md** - This file!

---

## 🎨 Customization Quick Guide

### Adjust Fighter Stats
```
1. Open FighterDef
2. Base Stats section:
   - Max Health: 100
   - Move Speed: 5
   - Weight: 1.0
3. Save
```

### Adjust Attack Damage
```
1. Open move (e.g., Attack1)
2. Combat Properties:
   - Damage: 8 → 10
3. Save
```

### Add Sweetspot to Attack
```
1. Open move
2. Hitbox Configuration
3. Click "Add Hitbox"
4. Configure:
   - Offset: (1.5, 0) (farther out)
   - Size: (0.5, 0.5) (smaller)
   - Damage Multiplier: 1.5 (50% bonus)
5. Save
```

### Change Hurtbox Damage
```
1. Open FighterDef
2. Hurtbox Configuration
3. Select hurtbox (e.g., Head)
4. Damage Multiplier: 1.2 → 1.5 (50% more damage)
5. Save
```

### Adjust Hitbox Timing
```
1. Open AnimatedMoveDef
2. Auto-Sync Settings:
   - Hitbox Activation Time: 0.25 → 0.20 (earlier)
   - Hitbox Duration: 0.15 → 0.25 (longer)
3. Save
```

---

## 🎯 Workflow Examples

### Example 1: Fast Glass Cannon
```
Fighter Setup:
  - Prefab: Player_Rapier
  - Max Health: 80 (low)
  - Move Speed: 8 (very fast)
  - Weight: 0.7 (light)

Hurtboxes:
  - Body: (0, 0), Size (0.8, 1.8) - smaller
  - Head: Damage ×1.5 - fragile

Moves:
  - Generate Rapier library
  - Increase damage by 20%
  - Reduce startup frames

Result: Fast, precise, but fragile
```

### Example 2: Slow Tank
```
Fighter Setup:
  - Prefab: Player_Hammer
  - Max Health: 140 (high)
  - Move Speed: 4 (slow)
  - Weight: 1.6 (heavy)

Hurtboxes:
  - Body: (0, 0), Size (1.3, 2.3) - larger
  - Damage ×0.85 - armored

Moves:
  - Generate Hammer library
  - Increase damage by 30%
  - Add more active frames

Result: Slow, tanky, devastating
```

### Example 3: Balanced All-Rounder
```
Fighter Setup:
  - Prefab: Player_Sword&Shield
  - Max Health: 110
  - Move Speed: 5.5
  - Weight: 1.1

Hurtboxes:
  - Default configuration

Moves:
  - Generate Sword&Shield library
  - Keep default values
  - Add defensive moves

Result: Balanced for all situations
```

---

## 🔧 Advanced Features

### Combo System
```
Attack1 (fast) 
  → Attack2 (medium)
    → Attack3 (heavy finisher)
```

Set up in Inspector:
- Can Combo: ✓
- Next Combo Move: [Drag next attack]
- Combo Window: 0.5s

### Sweetspot Mechanics
```
Tipper Hitbox:
  - Offset: (1.8, 0) - far out
  - Size: (0.3, 0.3) - tiny
  - Damage: ×2.0 - huge reward

Standard Hitbox:
  - Offset: (1.0, 0)
  - Size: (1.0, 0.8)
  - Damage: ×1.0
```

### Multi-Hit Attacks
```
4 hitboxes:
  - Hit 1: Frames 0-2, Damage ×0.4
  - Hit 2: Frames 3-5, Damage ×0.4
  - Hit 3: Frames 6-8, Damage ×0.5
  - Finisher: Frames 9+, Damage ×0.8, High knockback
```

### Status Effects
```
In MoveDef:
  Status Effects On Hit:
    - Slow (50% speed reduction, 3s)
    - Stun (immobilized, 1s)
    - Float (launched upward)
```

---

## 🎓 Best Practices

### 1. Use the Generators
Don't create manually! Use:
- Fighter Setup Wizard
- Move Library Generator

### 2. Start with Presets
Use preset stat configurations:
- Fast
- Balanced
- Tank

Then customize as needed.

### 3. Test Incrementally
1. Create fighter
2. Generate moves
3. Test basic attacks
4. Add special moves
5. Balance iteratively

### 4. Visualize Everything
Use Scene view gizmos to see:
- Hurtbox placement
- Hitbox reach
- Attack ranges

### 5. Balance Through Iteration
Playtest → Adjust values in Inspector → Repeat

### 6. Document Your Fighters
Keep notes on:
- Intended playstyle
- Strengths/weaknesses
- Combo routes

---

## 📊 Performance

### System Efficiency
- **Hitboxes**: Created/destroyed per attack (minimal overhead)
- **Hurtboxes**: Created once on spawn (persistent)
- **Frame calculations**: Done once per move
- **Memory**: Lightweight ScriptableObject architecture

### Optimization Tips
- Use object pooling for frequent attacks
- Minimize hitbox count (1-3 per attack ideal)
- Reuse MoveDefs across fighters when appropriate

---

## 🆘 Common Issues

### "Hitboxes not appearing"
- Check HitboxHurtboxSpawner component exists
- Verify "Show Gizmos" is enabled
- Confirm hitbox definitions are not empty

### "Animation not playing"
- Check Animator component has correct controller
- Verify animator trigger name matches MoveDef
- Ensure AnimationBridge component exists

### "Combo not working"
- Check "Can Combo" is enabled
- Verify "Next Combo Move" is assigned
- Confirm combo window is reasonable (0.3-0.6s)

### "Wrong damage values"
- Check move damage in MoveDef
- Verify fighter damage multiplier in FighterDef
- Confirm hurtbox damage multiplier

### "Moves not generated"
- Verify Shinabro folder path
- Check weapon type folder exists
- Ensure save path is valid

---

## 🎉 What You've Accomplished

You now have:

✅ **Complete fighter creation system** - Drag-and-drop prefabs  
✅ **Automatic hitbox/hurtbox generation** - Inspector-configured  
✅ **16+ attacks per weapon type** - Auto-generated with appropriate hitboxes  
✅ **12 weapon types** - All Shinabro weapons supported  
✅ **Frame-perfect combat** - Animation-synced hitboxes  
✅ **Combo system** - Automatic chain setup  
✅ **Sweetspot mechanics** - Reward precision  
✅ **Visual editing** - Scene view gizmos  
✅ **Custom Inspector tools** - Professional workflow  
✅ **Complete documentation** - Everything explained  

**You can now build a complete roster of unique fighters in minutes!**

---

## 🚀 Next Steps

### Immediate
1. **Generate move libraries** for 2-3 weapon types
2. **Create fighters** using Fighter Setup Wizard
3. **Playtest** and get a feel for the system

### Short-term
4. **Customize moves** in Inspector based on playtesting
5. **Balance damage values** across fighters
6. **Experiment with combos** and attack chains

### Long-term
7. **Create unique fighters** with custom stat distributions
8. **Design advanced combos** with special moves
9. **Build your game** with the complete framework!

---

## 💡 Pro Tips

1. **Generate all weapon libraries first** - Gives you a complete move pool
2. **Start with balanced stats** - Easier to tune than fix extremes
3. **Use sweetspots** - Adds skill ceiling and depth
4. **Test combos early** - Core gameplay feel
5. **Iterate in Inspector** - Fast workflow
6. **Document everything** - Future you will thank you
7. **Have fun!** - It's a game framework, enjoy creating!

---

## 📖 Support

### Documentation
- **QUICK_START.md** - Start here!
- **SHINABRO_ANIMATION_SYSTEM.md** - Animation integration
- **HITBOX_HURTBOX_SYSTEM.md** - Combat system
- **HITBOX_PATTERNS.md** - Configuration templates

### Tools
- **Fighter Setup Wizard** - Create fighters
- **Move Library Generator** - Generate attacks
- **Custom Inspectors** - Edit everything
- **Scene View Gizmos** - Visual editing

---

## 🎮 You're Ready!

You have everything needed to create a complete fighting game:
- ✅ Fighter creation
- ✅ Move generation
- ✅ Hitbox/hurtbox system
- ✅ Animation integration
- ✅ Inspector workflow
- ✅ Visual tools
- ✅ Documentation

**Start creating and have fun!** 🚀⚔️

