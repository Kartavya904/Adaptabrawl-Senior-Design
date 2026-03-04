# Hitbox & Hurtbox Patterns Reference

Visual reference guide for common hitbox/hurtbox configurations. Copy these patterns directly into your Inspector!

---

## Hurtbox Patterns (FighterDef)

### Pattern 1: Standard Humanoid
**Use for:** Most characters, balanced gameplay

```
Hurtbox 1: Body
  Name: Body
  Offset: (0, 0)
  Size: (1, 2)
  Damage Multiplier: 1.0
  Active: true

Hurtbox 2: Head
  Name: Head
  Offset: (0, 1.5)
  Size: (0.5, 0.5)
  Damage Multiplier: 1.2
  Active: true
```

**Visual:**
```
      [H]    ← Head (small, +20% damage)
       |
       |
      [B]    ← Body (main hurtbox)
       |
       |
```

---

### Pattern 2: Tank/Heavy Fighter
**Use for:** Hammer, Claymore, armored characters

```
Hurtbox 1: Armored Body
  Name: Body
  Offset: (0, 0)
  Size: (1.3, 2.3)
  Damage Multiplier: 0.85
  Active: true

Hurtbox 2: Helmet
  Name: Head
  Offset: (0, 1.8)
  Size: (0.7, 0.7)
  Damage Multiplier: 1.0
  Active: true
```

**Visual:**
```
     [HHH]   ← Larger head, normal damage
       |
      [BB]   ← Large body, reduced damage
      [BB]
       |
```

**Key:** Bigger target, but takes less damage

---

### Pattern 3: Agile/Evasive Fighter
**Use for:** Rapier, Dual Blades, assassin types

```
Hurtbox 1: Torso
  Name: Body
  Offset: (0, 0.3)
  Size: (0.7, 1.4)
  Damage Multiplier: 1.0
  Active: true

Hurtbox 2: Head
  Name: Head
  Offset: (0, 1.4)
  Size: (0.4, 0.4)
  Damage Multiplier: 1.4
  Active: true

Hurtbox 3: Legs
  Name: Legs
  Offset: (0, -0.7)
  Size: (0.6, 0.8)
  Damage Multiplier: 0.9
  Active: true
```

**Visual:**
```
      [H]    ← Small head, high damage
       |
      [B]    ← Narrow body
       |
      [L]    ← Legs take slightly less
```

**Key:** Smaller target, harder to hit, but fragile when hit

---

### Pattern 4: Tall Fighter
**Use for:** Spear, Staff, long-range characters

```
Hurtbox 1: Upper Body
  Name: Chest
  Offset: (0, 1)
  Size: (0.8, 1.2)
  Damage Multiplier: 1.0
  Active: true

Hurtbox 2: Head
  Name: Head
  Offset: (0, 2.2)
  Size: (0.5, 0.5)
  Damage Multiplier: 1.3
  Active: true

Hurtbox 3: Lower Body
  Name: Legs
  Offset: (0, -0.5)
  Size: (0.7, 1)
  Damage Multiplier: 0.85
  Active: true
```

**Visual:**
```
      [H]    ← Head way up (+30% damage)
       |
       |
      [C]    ← Chest
       |
      [L]    ← Legs protected (-15%)
       |
```

**Key:** Tall profile, head exposed, legs safe

---

## Hitbox Patterns (MoveDef)

### Pattern 1: Simple Punch/Kick
**Use for:** Basic light attacks

```
Hitbox: Primary
  Name: Primary
  Offset: (0.8, 0)
  Size: (1, 0.8)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 1.0
  Knockback Multiplier: 1.0
```

**Visual:**
```
        [===]
   (•)  [===]  → Single hitbox in front
        [===]
```

---

### Pattern 2: Sword Slash (Sweetspot)
**Use for:** Weapon attacks with tipper mechanics

```
Hitbox 1: Sweetspot (Blade Tip)
  Name: Sweetspot
  Offset: (1.5, 0.2)
  Size: (0.5, 0.5)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 1.5
  Knockback Multiplier: 1.3
  Is Sweetspot: true

Hitbox 2: Standard (Mid Blade)
  Name: Standard
  Offset: (1.0, 0)
  Size: (0.7, 0.9)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 1.0
  Knockback Multiplier: 1.0

Hitbox 3: Sourspot (Hilt)
  Name: Sourspot
  Offset: (0.5, 0)
  Size: (0.5, 0.7)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 0.7
  Knockback Multiplier: 0.7
```

**Visual:**
```
           [S]     ← Sweetspot (×1.5 damage)
          [==]
   (•)===[==]      ← Standard (×1.0)
          [s]      ← Sourspot (×0.7)
```

**Key:** Rewards precision, tip hits hardest

---

### Pattern 3: Wide Swing (Heavy Attack)
**Use for:** Hammer, Claymore heavy attacks

```
Hitbox: Heavy Swing
  Name: Swing
  Offset: (1, 0.3)
  Size: (1.5, 1.5)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 1.3
  Knockback Multiplier: 1.5
```

**Visual:**
```
        [=====]
   (•)  [=====]  → Large, powerful hitbox
        [=====]
        [=====]
```

**Key:** Big, slow, high damage

---

### Pattern 4: Multi-Hit Combo
**Use for:** Rapid combo attacks, magic spells

```
Hitbox 1: First Hit
  Name: Hit1
  Offset: (0.8, 0.5)
  Size: (0.8, 0.7)
  Active Start: 0
  Active End: 3
  Damage Multiplier: 0.6
  Knockback Multiplier: 0.3

Hitbox 2: Second Hit
  Name: Hit2
  Offset: (0.9, 0)
  Size: (0.9, 0.8)
  Active Start: 5
  Active End: 8
  Damage Multiplier: 0.7
  Knockback Multiplier: 0.4

Hitbox 3: Final Hit
  Name: Finisher
  Offset: (1.1, 0.2)
  Size: (1.2, 1.0)
  Active Start: 12
  Active End: 15
  Damage Multiplier: 1.2
  Knockback Multiplier: 1.5
```

**Visual (Timeline):**
```
Frame 0-3:   [H1]      ← Small hit
             
Frame 5-8:    [H2]     ← Medium hit
             
Frame 12-15:   [H3]    ← Big finisher
```

**Key:** Combo builds up to strong finisher

---

### Pattern 5: Uppercut/Launcher
**Use for:** Anti-air, launcher attacks

```
Hitbox 1: Ground Level
  Name: Ground
  Offset: (0.6, 0)
  Size: (0.9, 1.0)
  Active Start: 0
  Active End: 5
  Damage Multiplier: 1.0
  Knockback Direction: (0.5, 1)

Hitbox 2: Upper Arc
  Name: Arc
  Offset: (0.8, 1.5)
  Size: (1.0, 0.8)
  Active Start: 3
  Active End: 8
  Damage Multiplier: 1.2
  Knockback Direction: (0.3, 1.5)
```

**Visual:**
```
        [A2]↑   ← Upper hitbox (launcher)
         |
   (•)  [A1]    ← Ground hitbox
```

**Key:** Launches enemies upward

---

### Pattern 6: Spinning Attack (360°)
**Use for:** Whirlwind, spin attacks

```
Hitbox 1: Front
  Name: Front
  Offset: (1.2, 0)
  Size: (0.8, 1.2)
  Active Start: 0
  Active End: 4
  Damage Multiplier: 1.0

Hitbox 2: Side
  Name: Side
  Offset: (0, 1.2)
  Size: (1.2, 0.8)
  Active Start: 5
  Active End: 9
  Damage Multiplier: 1.0

Hitbox 3: Back
  Name: Back
  Offset: (-1.2, 0)
  Size: (0.8, 1.2)
  Active Start: 10
  Active End: 14
  Damage Multiplier: 1.1

Hitbox 4: Side2
  Name: Side2
  Offset: (0, -1.2)
  Size: (1.2, 0.8)
  Active Start: 15
  Active End: 19
  Damage Multiplier: 1.0
```

**Visual (Sequence):**
```
Frame 0-4:   →[F]     Front
Frame 5-9:    [S]↑    Side
Frame 10-14: [B]←     Back (stronger)
Frame 15-19:  ↓[S]    Side
```

**Key:** Hits all around over time

---

### Pattern 7: Projectile Attack
**Use for:** Fireballs, arrows, bullets

```
Hitbox: Projectile
  Name: Projectile
  Offset: (1.5, 0)
  Size: (0.6, 0.6)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 1.0
  
MoveDef Settings:
  Is Projectile: true
  Projectile Prefab: [Assign prefab]
```

**Visual:**
```
   (•) ===○→  Spawns projectile
```

**Key:** Creates independent projectile GameObject

---

### Pattern 8: Charge Attack (Growing Hitbox)
**Use for:** Charged punches, power-ups

```
Hitbox 1: Uncharged
  Name: Weak
  Offset: (0.9, 0)
  Size: (0.8, 0.8)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 0.7

(Player holds longer, switches to:)

Hitbox 2: Charged
  Name: Strong
  Offset: (1.3, 0)
  Size: (1.5, 1.5)
  Active Start: 0
  Active End: -1
  Damage Multiplier: 1.8
```

**Visual:**
```
Quick:  (•) [H]→     Small, weak
                     
Charged: (•) [HHH]→  Big, powerful
```

**Key:** Reward patience with power

---

## Frame Timing Patterns

### Fast Light Attack
```
Startup: 3-5 frames
Active: 2-3 frames
Recovery: 6-10 frames
Total: ~12-18 frames (0.2-0.3s @ 60 FPS)
```

### Medium Attack
```
Startup: 6-8 frames
Active: 3-4 frames
Recovery: 10-15 frames
Total: ~19-27 frames (0.3-0.45s)
```

### Heavy/Slow Attack
```
Startup: 12-20 frames
Active: 4-6 frames
Recovery: 20-30 frames
Total: ~36-56 frames (0.6-0.9s)
```

### Super Fast (Jab)
```
Startup: 2-3 frames
Active: 1-2 frames
Recovery: 5-8 frames
Total: ~8-13 frames (0.13-0.22s)
```

---

## Damage Multiplier Guidelines

### Hurtbox Multipliers
```
0.7  - Heavily armored (tank chest)
0.8  - Armored
0.85 - Slightly armored
0.9  - Protected area (legs, arms)
1.0  - Standard (body)
1.1  - Exposed area
1.2  - Weak point (head)
1.3  - Critical area (head of glass cannon)
1.5+ - Super critical (weakspot mechanic)
```

### Hitbox Multipliers
```
0.5  - Glancing blow
0.7  - Sourspot (hilt, late hit)
0.8  - Weak hit
1.0  - Standard hit
1.2  - Strong hit
1.3  - Power hit
1.5  - Sweetspot (blade tip)
1.8+ - Super sweetspot / charged / finisher
```

---

## Size Guidelines

### Hurtbox Sizes (2D)
```
Small Fighter:  Body (0.8, 1.8), Head (0.4, 0.4)
Normal Fighter: Body (1.0, 2.0), Head (0.5, 0.5)
Large Fighter:  Body (1.3, 2.3), Head (0.7, 0.7)
```

### Hitbox Sizes (2D)
```
Jab:       (0.7, 0.6)
Punch:     (1.0, 0.8)
Kick:      (1.2, 0.9)
Weapon:    (1.5, 1.0)
Heavy:     (1.8, 1.5)
Projectile: (0.5, 0.5)
```

---

## Offset Guidelines

### Hurtbox Offsets (Y-axis)
```
Legs:  -0.5 to -1.0
Body:   0.0
Chest:  0.5 to 1.0
Head:   1.5 to 2.0
```

### Hitbox Offsets (X-axis, facing right)
```
Point-blank:  0.3 to 0.5
Close:        0.6 to 0.8
Medium:       0.9 to 1.2
Long-reach:   1.3 to 1.8
```

---

## Quick Copy Templates

### Balanced Fighter Template
```
FighterDef:
  Max Health: 100
  Move Speed: 5
  Weight: 1.0
  Hurtboxes:
    - Body: (0, 0), (1, 2), ×1.0
    - Head: (0, 1.5), (0.5, 0.5), ×1.2

MoveDef Light:
  Startup: 5, Active: 3, Recovery: 10
  Damage: 10
  Hitbox: (0.8, 0), (1, 0.8), ×1.0

MoveDef Heavy:
  Startup: 12, Active: 4, Recovery: 20
  Damage: 25
  Hitbox: (1.0, 0), (1.3, 1.1), ×1.2
```

### Speed Fighter Template
```
FighterDef:
  Max Health: 90
  Move Speed: 7
  Weight: 0.8
  Hurtboxes:
    - Body: (0, 0), (0.8, 1.8), ×1.0
    - Head: (0, 1.4), (0.4, 0.4), ×1.3

MoveDef Light:
  Startup: 3, Active: 2, Recovery: 6
  Damage: 8
  Hitbox: (0.7, 0), (0.8, 0.7), ×1.0

MoveDef Heavy:
  Startup: 8, Active: 3, Recovery: 15
  Damage: 18
  Hitbox 1: (1.2, 0), (0.5, 0.5), ×1.5 (sweetspot)
  Hitbox 2: (0.8, 0), (0.8, 0.8), ×1.0
```

### Tank Fighter Template
```
FighterDef:
  Max Health: 130
  Move Speed: 4
  Weight: 1.5
  Hurtboxes:
    - Body: (0, 0), (1.3, 2.3), ×0.85
    - Head: (0, 1.8), (0.7, 0.7), ×1.0

MoveDef Light:
  Startup: 6, Active: 3, Recovery: 12
  Damage: 12
  Hitbox: (0.9, 0), (1.1, 0.9), ×1.0

MoveDef Heavy:
  Startup: 18, Active: 6, Recovery: 30
  Damage: 35
  Hitbox: (1.2, 0.2), (1.8, 1.6), ×1.3
```

---

## Testing Checklist

When creating hitboxes/hurtboxes:

- [ ] Hurtboxes visible in Scene view
- [ ] Hurtbox sizes match character visuals
- [ ] Head hurtbox has damage multiplier ≥ 1.1
- [ ] Hitboxes spawn during attacks
- [ ] Hitbox timing feels responsive
- [ ] Sweetspots reward precision
- [ ] Large attacks have appropriate startup
- [ ] Fast attacks have small hitboxes
- [ ] Damage values feel balanced
- [ ] Knockback direction is correct

---

Use these patterns as starting points, then iterate based on playtesting!

