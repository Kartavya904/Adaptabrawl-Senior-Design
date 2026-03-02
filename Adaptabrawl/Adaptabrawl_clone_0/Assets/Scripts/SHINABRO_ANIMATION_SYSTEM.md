# Shinabro Animation Integration Guide

## 🎬 Complete Animation System with Auto-Generated Moves!

This system automatically integrates Shinabro animations with Adaptabrawl's combat framework, creating fully functional attacks with hitboxes **in seconds**!

---

## ✨ What This System Does

✅ **Auto-Generates All Attacks** - Create 16+ moves with hitboxes instantly  
✅ **Animation Syncing** - Hitboxes trigger perfectly with animations  
✅ **Inspector Configuration** - Modify everything without code  
✅ **Combo Chains** - Automatic attack combo setup  
✅ **Frame-Perfect Timing** - Hitboxes activate on exact animation frames  
✅ **Pre-Configured Hitboxes** - Appropriate hitboxes for each attack type  

---

## 🚀 Quick Start (60 Seconds!)

### Step 1: Generate Move Library (30 seconds)
1. Go to **`Adaptabrawl → Move Library Generator`**
2. Select **Weapon Type** (e.g., `Unarmed_Fighter`, `Hammer`, `Rapier`)
3. Choose save location
4. Click **"Generate Move Library"**
5. Done! 16+ moves with hitboxes created!

### Step 2: Assign to Fighter (30 seconds)
1. Open your **FighterDef**
2. In **"Moveset"** section:
   - **Light Attack**: Drag `Attack1` from library
   - **Heavy Attack**: Drag `Attack3` from library
   - **Special Moves**: Drag skills from library
3. Done! Fighter ready to use!

---

## 📦 System Components

### 1. AnimatedMoveDef
Extended MoveDef that connects to Shinabro animations.

**Key Features:**
- Links to animation clips
- Auto-calculates frame data
- Combo chain support
- Movement during animation

### 2. AnimationBridge
Plays animations and triggers hitboxes during combat.

**Responsibilities:**
- Triggers Unity Animator
- Activates/deactivates hitboxes
- Manages combo windows
- Tracks animation state

### 3. MoveLibrary
Container for all moves of a weapon type.

**Contains:**
- 3 ground attacks (combo)
- 3 aerial attacks
- 8 special skills
- Defensive moves
- Utility moves

### 4. HitboxPresets
Pre-configured hitbox templates.

**Presets Include:**
- Quick attacks (jabs, punches)
- Medium attacks (standard hits)
- Heavy attacks (finishers with sweetspots)
- Aerial attacks (downward angle)
- Launchers (uppercuts)
- Spin attacks (AOE)
- And more!

### 5. MoveLibraryGenerator (Editor Tool)
**The Power Tool!** Generates complete move libraries automatically.

---

## 🎮 Available Weapon Types

| Weapon | Description | Folder |
|--------|-------------|--------|
| **Unarmed_Fighter** | Martial artist, fast punches/kicks | `09_Fighter` |
| **SwordAndShield** | Balanced warrior | `01_Sword&Shield` |
| **Hammer** | Heavy, slow, high damage | `02_Hammer` |
| **DualBlades** | Fast dual wielding | `03_DualBlades` |
| **Bow** | Ranged archer | `04_Bow` |
| **Pistol** | Ranged gunslinger | `05_Pistol` |
| **Magic** | Spell caster | `06_Magic` |
| **Spear** | Medium range | `07_Spear` |
| **Staff** | Monk/mage hybrid | `08_Staff` |
| **Rapier** | Precise fencer | `10_Rapier` |
| **DoubleBlades** | Aggressive dual wielder | `11_DoubleBlades` |
| **Claymore** | Greatsword knight | `12_Claymore` |

---

## 🎯 Attack Types Generated

### Ground Attacks (Combo Chain)
```
Attack1 → Attack2 → Attack3
 (Quick)  (Medium)  (Heavy Finisher)
```

**Attack 1:**
- Fast startup (25% through animation)
- Small hitbox
- Low damage (8)
- Can combo into Attack 2

**Attack 2:**
- Medium startup (30% through animation)
- Medium hitbox
- Medium damage (10)
- Can combo into Attack 3

**Attack 3:**
- Slow startup (35% through animation)
- Large hitbox with sweetspot
- High damage (15)
- Combo finisher

### Aerial Attacks
```
JumpAttack1, JumpAttack2, JumpAttack3
```
- Downward-angled hitboxes
- Meteor smash potential
- Good for juggling

### Special Skills

**Skill 1 - Float:**
- Launches enemy upward
- Multi-hit launcher hitboxes
- Opens air combo opportunities

**Skill 2 - Slow:**
- Applies slow status effect
- Stun-type hitbox (low knockback)

**Skill 3 - Stun:**
- Stuns enemy in place
- Minimal knockback
- Combo starter

**Skill 4 - Push:**
- High knockback, lower damage
- Pushes enemy away
- Space control

**Skill 5 - Pull:**
- Pulls enemy toward you
- Setup for combos

**Skill 6 - Move:**
- Dash attack
- Elongated forward hitbox
- High damage

**Skill 7 - Around:**
- 360° spinning attack
- 4 hitboxes covering all sides
- AOE damage

**Skill 8 - Air:**
- Aerial special
- High damage
- Can spike downward

### Defensive Moves
- **Dodge**: Quick sidestep
- **Dodge Roll**: Invincibility frames
- **Dodge Attack**: Counter attack
- **Block**: Reduces damage
- **Crouch**: Low profile
- **Crouch Attack**: Low attack

---

## ⚙️ Inspector Configuration

### AnimatedMoveDef Inspector

```
Animation Integration:
  ├─ Animation Clip: (Shinabro animation)
  ├─ Animator Trigger: "Attack1"
  └─ Parameter Type: Trigger

Auto-Sync Settings:
  ├─ Auto Calculate Frames: ✓
  ├─ Hitbox Activation Time: 0.25 (25% through animation)
  ├─ Hitbox Duration: 0.15 (15% of animation)
  └─ Recovery Percentage: 0.30 (30% after hit)

Combat Properties:
  ├─ Damage: 10
  ├─ Knockback Force: 5
  ├─ Hitstop Frames: 3
  ├─ Hitstun Frames: 12
  └─ Blockstun Frames: 8

Hitbox Configuration:
  └─ Hitbox Definitions: [Array of hitboxes]
      ├─ Primary: (0.8, 0), Size: (1, 0.8)
      └─ Sweetspot: (1.5, 0), Size: (0.5, 0.5), Damage: ×1.5

Combo System:
  ├─ Can Combo: ✓
  ├─ Next Combo Move: Attack2
  └─ Combo Window: 0.5s
```

All values editable in Inspector!

---

## 🔧 Advanced Customization

### Adjusting Hitbox Timing

**Make hitbox activate earlier:**
```
Hitbox Activation Time: 0.25 → 0.20
(Activates at 20% instead of 25%)
```

**Make hitbox last longer:**
```
Hitbox Duration: 0.15 → 0.25
(Active for 25% of animation)
```

### Adjusting Damage

Open any move in Inspector and change **"Damage"** field.

**Quick Balance:**
- Light Attack: 6-10 damage
- Medium Attack: 10-15 damage
- Heavy Attack: 15-25 damage
- Special: 15-30 damage

### Adding More Hitboxes

1. Select move in Inspector
2. Scroll to **"Hitbox Configuration"**
3. Click **"Add Hitbox"**
4. Configure:
   - Offset (position)
   - Size
   - Active frames
   - Damage multiplier
5. Done!

### Changing Combo Chains

1. Open **MoveLibrary** asset
2. Select `attack1`
3. In Inspector:
   - **Can Combo**: ✓
   - **Next Combo Move**: Drag attack you want to chain to
   - **Combo Window**: Time window to input next attack
4. Repeat for other attacks

---

## 📊 Frame Data Explained

### How Auto-Calculate Works

```
Animation Length: 1.0 seconds (60 frames @ 60 FPS)

Startup Frames = 60 × Hitbox Activation Time
               = 60 × 0.25 = 15 frames

Active Frames  = 60 × Hitbox Duration
               = 60 × 0.15 = 9 frames

Recovery Frames = 60 × Recovery Percentage
                = 60 × 0.30 = 18 frames
```

**Result:** Attack takes 15 frames to hit, hitbox active for 9 frames, 18 frames to recover.

### Manual Override

Uncheck **"Auto Calculate Frames"** to set manually:
- **Startup Frames**: 15
- **Active Frames**: 9  
- **Recovery Frames**: 18

---

## 🎨 Hitbox Types Reference

### Quick Attack Hitbox
```
Offset: (0.8, 0)
Size: (0.9, 0.7)
Damage: ×1.0
Use: Fast jabs, light attacks
```

### Medium Attack Hitbox
```
Offset: (1.0, 0.2)
Size: (1.1, 0.9)
Damage: ×1.0
Use: Standard attacks
```

### Heavy Attack Hitbox (with Sweetspot)
```
Sweetspot:
  Offset: (1.5, 0.3)
  Size: (0.6, 0.6)
  Damage: ×1.5

Standard:
  Offset: (1.0, 0.1)
  Size: (1.3, 1.1)
  Damage: ×1.0

Use: Finishers, charged attacks
```

### Aerial Attack Hitbox
```
Offset: (0.7, -0.5)
Size: (1.0, 1.2)
Damage: ×1.1
Knockback: Downward (0.5, -1)
Use: Jump attacks, spikes
```

### Launcher Hitbox
```
Ground:
  Offset: (0.7, 0)
  Knockback: (0.5, 1.5) ← upward

Upper:
  Offset: (0.8, 1.5)
  Knockback: (0.3, 2.0) ← strong upward

Use: Uppercuts, launchers
```

### Spin Attack Hitbox (4-part)
```
Front → Side → Back → Side
Each activates sequentially
360° coverage

Use: Spinning attacks, AOE
```

---

## 🎬 Workflow Example

### Creating a Custom Fighter

**Goal:** Create a fast Rapier fighter with precise attacks

**Step 1: Generate Move Library**
```
1. Adaptabrawl → Move Library Generator
2. Weapon Type: Rapier
3. Attack 1 Damage: 7 (fast, low)
4. Attack 2 Damage: 9 (medium)
5. Attack 3 Damage: 14 (finisher)
6. Generate!
```

**Step 2: Customize Moves**
```
1. Open Attack1 from library
2. Adjust:
   - Hitbox Activation: 0.20 (faster!)
   - Hitbox Size: (0.8, 0.6) (smaller)
3. Open Attack3
4. Add second hitbox for tipper mechanic:
   - Offset: (2.0, 0)
   - Size: (0.3, 0.3)
   - Damage Multiplier: 2.0 (big reward!)
```

**Step 3: Assign to Fighter**
```
1. Open FighterDef
2. Fighter Prefab: Player_Rapier
3. Moveset:
   - Light Attack: Rapier_Attack1
   - Heavy Attack: Rapier_Attack3
   - Special Moves: [Rapier_Skill6, Rapier_Skill1]
4. Done!
```

**Result:** Fast rapier fighter with precise tipper mechanics!

---

## 🔄 Combo System

### How Combos Work

1. **Player presses attack** → Attack1 starts
2. **Attack1 reaches 60% of animation** → Combo window opens
3. **Player presses attack again within window** → Attack2 queued
4. **Attack1 completes** → Attack2 starts immediately
5. **Repeat for Attack3**

### Combo Window Configuration

```csharp
// In AnimatedMoveDef Inspector:
Can Combo: true
Next Combo Move: Attack2
Combo Window: 0.5 seconds

// Combo opens at 60% of animation
// Player has 0.5s to input next attack
```

### Custom Combo Chains

Create any chain you want:
```
Attack1 → Attack2 → Attack3 → Skill1 → Attack1
   ↓
 Skill6 (alternate branch)
```

Just set **"Next Combo Move"** in Inspector!

---

## 🎮 Testing in Unity

### Testing a Move

1. Place fighter in scene
2. Select fighter GameObject
3. Find **AnimationBridge** component
4. In Inspector, assign a move to test
5. Play scene
6. Watch animation play with hitboxes!

### Debug Visualization

Enable **"Show Debug Info"** on AnimationBridge to see:
- When hitboxes activate
- Frame numbers
- Combo windows

---

## 📋 Complete Feature List

### What You Get

**✅ 12 Weapon Types** - All Shinabro weapons supported  
**✅ 16+ Attacks Per Weapon** - Ground, aerial, specials  
**✅ Auto-Generated Hitboxes** - Appropriate for each attack  
**✅ Combo Chains** - Automatic setup  
**✅ Frame Data** - Auto-calculated from animations  
**✅ Inspector Editing** - Modify everything  
**✅ Hitbox Presets** - 10+ templates  
**✅ Move Libraries** - Pre-organized movesets  
**✅ Animation Bridge** - Seamless integration  
**✅ Sweetspot System** - Reward precision  
**✅ Status Effects** - Float, Stun, Slow, etc.  
**✅ Movement During Attacks** - Dash attacks, etc.  
**✅ Full Documentation** - This guide!  

---

## 🆘 Troubleshooting

| Issue | Solution |
|-------|----------|
| **Moves not generating** | Check Shinabro folder path is correct |
| **Hitboxes not appearing** | Verify HitboxHurtboxSpawner component exists |
| **Animation not playing** | Check Animator has correct parameters |
| **Combo not chaining** | Verify "Can Combo" is checked |
| **Wrong damage values** | Adjust in Move Inspector |
| **Hitbox timing off** | Adjust "Hitbox Activation Time" |

---

## 🎓 Best Practices

### 1. Use the Generator First
Don't manually create moves! Use the Move Library Generator for the base, then customize.

### 2. Test Incrementally
Generate moves for one weapon, test, then do another.

### 3. Customize Gradually
Start with generated values, playtest, then adjust.

### 4. Use Sweetspots
Add sweetspot hitboxes to reward precision (1.5-2.0× multiplier).

### 5. Balance Combos
First hit should be fast, last hit should be powerful but risky.

### 6. Visualize Everything
Use Scene view gizmos to see exactly where hitboxes are.

---

## 📖 Related Documentation

- **HITBOX_HURTBOX_SYSTEM.md** - Detailed hitbox/hurtbox guide
- **HITBOX_PATTERNS.md** - Hitbox configuration templates
- **FIGHTER_PREFAB_SETUP.md** - Fighter creation guide
- **QUICK_START.md** - 5-minute setup tutorial

---

## 🎉 Summary

You now have a **complete animation integration system** that:

✅ Auto-generates **16+ attacks** per weapon type  
✅ Creates **appropriate hitboxes** automatically  
✅ **Inspector-configurable** - no coding needed  
✅ **Frame-perfect** timing with animations  
✅ **Combo chains** automatically set up  
✅ **12 weapon types** supported  
✅ **10+ hitbox presets** included  

**From animation to playable attack in 60 seconds!** 🚀

