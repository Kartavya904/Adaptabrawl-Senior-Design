# Match Setup Wizard - Quick Guide

## 🚀 Set Up a Complete Match in 30 Seconds!

The **Match Setup Wizard** automatically creates everything you need for a playable 2-player match.

---

## ✨ What It Does

✅ Creates ground platform  
✅ Sets up spawn points  
✅ Configures camera  
✅ Creates health bar UI  
✅ Adds win/lose panel  
✅ Spawns both fighters  
✅ Configures input for both players  
✅ Sets up match manager  
✅ Connects all components automatically  

**Result: Press Play and you can immediately play a match!**

---

## 📖 How to Use

### Step 1: Create Your Fighters First

Before using the wizard, you need to create 2 fighters:

**Quick Method:**
```
1. Adaptabrawl → Fighter Setup Wizard
2. Select prefab (e.g., Player_Fighter)
3. Name it, choose preset
4. Create
5. Repeat for second fighter
```

**Alternative:**
- Use the Move Library Generator to create moves
- Then assign moves to FighterDef assets

### Step 2: Open Match Setup Wizard

```
Menu Bar → Adaptabrawl → Match Setup Wizard
```

### Step 3: Select Fighters

```
Player 1 Fighter: Drag your first FighterDef
Player 2 Fighter: Drag your second FighterDef
```

### Step 4: Click Setup!

```
Click: "🎮 Setup Complete Match!"
```

**That's it!** The wizard does everything else.

### Step 5: Press Play

```
Unity → Play Button
```

**Your match is ready to play!**

---

## 🎮 Default Controls

### Player 1 (Left Side)
```
Attacks:
  F - Light Attack
  G - Heavy Attack
  R - Special 1
  T - Special 2

Defense:
  Left Shift - Block
  Space - Dodge

Movement:
  WASD (not yet implemented)
```

### Player 2 (Right Side)
```
Attacks:
  NumPad 1 - Light Attack
  NumPad 2 - Heavy Attack
  NumPad 4 - Special 1
  NumPad 5 - Special 2

Defense:
  Right Shift - Block
  NumPad 0 - Dodge

Movement:
  Arrow Keys (not yet implemented)
```

---

## 🔍 What Gets Created

### Scene Objects
- **Ground** - Platform at bottom of screen
- **Player1SpawnPoint** - Left spawn position (-3, 0, 0)
- **Player2SpawnPoint** - Right spawn position (3, 0, 0)
- **GameManager** - Contains all management scripts
- **Main Camera** - Configured for 2D fighting game

### UI Elements
- **Player 1 Health Bar** - Top-left corner
- **Player 2 Health Bar** - Top-right corner
- **Player Names** - Display fighter names
- **Win Panel** - Shows winner at end of match

### Components Added
- **FighterSpawner** - Spawns both fighters
- **TwoPlayerInputHandler** - Handles all inputs
- **MatchManager** - Manages health and win conditions

### Automatic Connections
All components are automatically wired together:
- Fighters linked to spawner
- Spawner linked to input handler
- Health bars linked to match manager
- Win panel linked to match manager
- **Everything just works!**

---

## 🎯 Usage Example

```
Complete Workflow (2 minutes):

1. Open Match Setup Wizard
2. Drag Fighter1.asset to Player 1 slot
3. Drag Fighter2.asset to Player 2 slot
4. Click "Setup Complete Match!"
5. Press Play

Result: Playable match!
```

---

## ⚙️ Customizing After Setup

### Change Fighter Stats
```
1. Select FighterDef asset
2. Modify stats in Inspector
3. Values take effect immediately
```

### Change Controls
```
1. Select GameManager in scene
2. Find TwoPlayerInputHandler component
3. Modify key codes in Inspector
```

### Adjust UI
```
1. Find MatchUI in Hierarchy
2. Modify health bars, colors, positions
3. Customize win panel appearance
```

### Modify Match Rules
```
1. Select GameManager
2. Find MatchManager component
3. Adjust:
   - Match Start Delay
   - Restart Delay
```

---

## 🔄 Running the Wizard Multiple Times

**Safe to run multiple times!**

The wizard will:
- Update existing GameManager
- Keep existing ground if found
- Update spawn points
- Recreate UI cleanly

**Use it to:**
- Switch fighters
- Reset scene setup
- Update after changes

---

## 🆘 Troubleshooting

### Issue: "Select both fighters" error
**Solution:** 
- Make sure you've assigned FighterDef assets to both slots
- Create fighters first using Fighter Setup Wizard

### Issue: Fighters don't spawn
**Solution:**
- Check FighterDef has fighterPrefab assigned
- Verify spawn points exist in scene
- Check Console for error messages

### Issue: No damage when hitting
**Solution:**
- Fighters need MoveDef attacks assigned
- Use Move Library Generator to create moves
- Assign moves to FighterDef's moveset

### Issue: Attacks don't play animations
**Solution:**
- Moves must be AnimatedMoveDef type (not regular MoveDef)
- Use Move Library Generator to create proper moves
- Check fighters have Animator component

### Issue: UI doesn't show
**Solution:**
- Check Canvas exists in scene
- Verify MatchManager has UI references assigned
- Look for MatchUI in Hierarchy

---

## 🎨 Comparison: Manual vs Wizard

### Manual Setup (from guide)
```
Time: ~15 minutes
Steps: 50+
Prone to errors
Need to remember connections
Manual wiring required
```

### Wizard Setup
```
Time: ~30 seconds
Steps: 3
Automatic
All connections handled
One-click solution
✨ Magic!
```

---

## 📋 Pre-Wizard Checklist

Before running the wizard, make sure you have:

- [ ] At least 2 FighterDef assets created
- [ ] Fighters have Shinabro prefabs assigned
- [ ] Fighters have moves assigned (light/heavy at minimum)
- [ ] Moves are AnimatedMoveDef type (not plain MoveDef)
- [ ] Scene is open in Unity

**Pro Tip:** Use these wizards in order:
1. **Move Library Generator** - Create moves
2. **Fighter Setup Wizard** - Create fighters
3. **Match Setup Wizard** - Set up match ← You are here!

---

## 🎯 Complete Example

### Full Setup from Scratch (5 minutes total)

**Step 1: Create Moves (1 minute)**
```
Adaptabrawl → Move Library Generator
Weapon Type: Unarmed_Fighter
Generate
```

**Step 2: Create Fighter 1 (1 minute)**
```
Adaptabrawl → Fighter Setup Wizard
Prefab: Player_Fighter
Name: Brawler
Preset: Fast
Create
Assign moves from library
```

**Step 3: Create Fighter 2 (1 minute)**
```
Generate moves for Hammer
Create Fighter with Player_Hammer prefab
Name: Tank
Preset: Tank
Assign hammer moves
```

**Step 4: Setup Match (30 seconds)**
```
Adaptabrawl → Match Setup Wizard
Player 1: Brawler
Player 2: Tank
Setup Complete Match!
```

**Step 5: Play! (instant)**
```
Press Play button
Fight!
```

---

## ✨ Advanced Tips

### Multiple Match Scenes
Create different scenes for different stages:
```
Scene1_Arena.unity - Arena stage
Scene2_Rooftop.unity - Rooftop stage
Scene3_Dojo.unity - Dojo stage

Run wizard in each scene with same fighters
Different aesthetics, same functionality!
```

### Tournament Setup
Create multiple scenes for bracket progression:
```
Match1.unity - Semifinal 1
Match2.unity - Semifinal 2
Match3.unity - Final

Winners advance to next scene
```

### Quick Testing
Use wizard to rapidly test different fighter matchups:
```
1. Run wizard with Fighter A vs Fighter B
2. Test balance
3. Run wizard again with Fighter C vs Fighter D
4. Compare
5. Iterate
```

---

## 🎉 Summary

**Before Wizard:**
- 50+ manual steps
- 15+ minutes
- Error-prone
- Tedious connections

**With Wizard:**
- 3 steps
- 30 seconds
- Automatic
- One click!

**The Match Setup Wizard is the fastest way to get from fighters to playable match!**

---

## 📚 Related Documentation

- **MATCH_SETUP_GUIDE.md** - Manual setup (for reference)
- **FIGHTER_PREFAB_SETUP.md** - Creating fighters
- **SHINABRO_ANIMATION_SYSTEM.md** - Move creation
- **QUICK_START.md** - Getting started
- **COMPLETE_SYSTEM_OVERVIEW.md** - Full system guide

---

## 💡 Remember

**The wizard handles everything technical so you can focus on designing fun fighters!**

Just create your fighters, run the wizard, and start playing. It's that simple! 🎮⚔️

