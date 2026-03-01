# Adaptabrawl Setup Guide

This guide will help you set up all the scenes and connect everything properly in Unity.

## Prerequisites

- Unity Editor (LTS version recommended)
- All scripts are already in place
- TextMeshPro package installed (should be included)

## Scene Setup Steps

### Step 1: Create Missing Scenes

You need to create the following scenes in `Assets/Scenes/`:

1. **CharacterSelect.unity** - New scene
2. **LobbyScene.unity** - New scene  
3. **SettingsScene.unity** - New scene
4. **MatchResults.unity** - New scene

**To create a new scene:**
1. Right-click in `Assets/Scenes/` folder
2. Create → Scene
3. Name it appropriately
4. Save (Ctrl+S)

### Step 2: Add Scenes to Build Settings

1. Open **File → Build Settings**
2. Click **Add Open Scenes** for each scene, or drag scenes from Project window
3. Ensure scenes are in this order:
   - **StartScene** (index 0) - Must be first!
   - **CharacterSelect** (index 1)
   - **LobbyScene** (index 2)
   - **SettingsScene** (index 3)
   - **GameScene** (index 4)
   - **MatchResults** (index 5)

### Step 3: Setup StartScene

1. Open `StartScene.unity`
2. Create a Canvas if one doesn't exist:
   - Right-click Hierarchy → UI → Canvas
3. Create UI elements:
   - **Main Menu Panel** (GameObject with Image component)
   - **Play Options Panel** (GameObject with Image component, initially disabled)
   - **Play Button** (UI → Button)
   - **Online Button** (UI → Button)
   - **Settings Button** (UI → Button)
   - **Quit Button** (UI → Button)
   - **Local Play Button** (UI → Button, in Play Options Panel)
   - **Back Button** (UI → Button, in Play Options Panel)
4. Add `MainMenu` script to a GameObject (or Canvas)
5. Assign all button references in the inspector
6. Assign panel references

### Step 4: Setup CharacterSelect Scene

1. Open `CharacterSelect.unity`
2. Create Canvas
3. Create UI structure:
   - **Player 1 Section:**
     - Fighter name text (TextMeshPro)
     - Fighter image (Image)
     - Left/Right navigation buttons
     - Confirm button
     - Ready status text
   - **Player 2 Section:** (same as Player 1)
   - **Start Button** (disabled until both ready)
   - **Back Button**
4. Add `CharacterSelectUI` script to a GameObject
5. Assign all UI references
6. Create FighterDef ScriptableObjects:
   - Right-click in Project → Create → Adaptabrawl → Fighter Definition
   - Create at least 2 fighters (e.g., "Striker", "Elusive")
   - Assign these to the `availableFighters` list in CharacterSelectUI

### Step 5: Setup LobbyScene

1. Open `LobbyScene.unity`
2. Create Canvas
3. Create UI panels:
   - **Create Room Panel:**
     - Create Room button
     - Room code display text
   - **Join Room Panel:**
     - Room code input field (TMP_InputField)
     - Join button
   - **Lobby Panel:**
     - Room code display
     - Ready button
     - Ready status text (player and opponent)
     - Cancel button
4. Add `LobbyManager` script to a GameObject
5. Add `LobbyUI` script to a GameObject
6. Assign all references between scripts and UI

### Step 6: Setup SettingsScene

1. Open `SettingsScene.unity`
2. Create Canvas
3. Create settings UI:
   - **Audio Section:**
     - Master Volume slider + text
     - Music Volume slider + text
     - SFX Volume slider + text
   - **Video Section:**
     - Quality dropdown
     - Resolution dropdown
     - VSync toggle
     - FPS dropdown
   - **Accessibility Section:**
     - UI Scale slider + text
     - Color Blind Mode toggle
     - Show Hitboxes toggle
   - **Navigation:**
     - Back button
     - Apply button
     - Reset to Defaults button
4. Add `SettingsUI` script to a GameObject
5. Create SettingsManager GameObject:
   - Add `SettingsManager` script
   - This should be a singleton (DontDestroyOnLoad)
6. Assign all UI references

### Step 7: Setup GameScene

1. Open `GameScene.unity`
2. Create game structure:
   - **Ground/Platform** (Sprite or 3D object with collider)
   - **Player 1 Spawn Point** (Empty GameObject with Transform)
   - **Player 2 Spawn Point** (Empty GameObject with Transform)
   - **Camera** (with `FightingGameCamera` script)
3. Create UI Canvas:
   - **HUD Elements:**
     - Player 1 health bar (Slider)
     - Player 1 health text
     - Player 1 status container (Horizontal Layout Group)
     - Player 2 health bar
     - Player 2 health text
     - Player 2 status container
     - Round timer text
     - Condition banner (initially disabled)
   - **Pause Menu Panel** (initially disabled):
     - Resume button
     - Settings button
     - Main Menu button
     - Quit button
4. Add scripts:
   - **LocalGameManager** - Assign spawn points
   - **GameManager** - Match management
   - **HUDManager** - Assign all HUD references
   - **PauseMenu** - Assign pause panel and buttons
5. Create StatusIcon prefab:
   - Create UI → Image
   - Add `StatusIcon` script
   - Add text for timer/stack display
   - Save as prefab
   - Assign to HUDManager's statusIconPrefab

### Step 8: Setup MatchResults Scene

1. Open `MatchResults.unity`
2. Create Canvas
3. Create results UI:
   - **Results Panel:**
     - Winner text (large, prominent)
     - Match score text
     - Round results text
     - Player 1 info (name, wins, portrait)
     - Player 2 info (name, wins, portrait)
   - **Buttons:**
     - Rematch button
     - Main Menu button
     - Character Select button
4. Add `MatchResultsUI` script
5. Assign all UI references
6. Initially disable results panel (script will enable it)

## Component Configuration

### SettingsManager Singleton Setup

1. Create empty GameObject named "SettingsManager"
2. Add `SettingsManager` script
3. In script's `Awake()`, it will automatically become singleton
4. Consider making it DontDestroyOnLoad so settings persist

### SceneTransitionManager (Optional)

1. Create empty GameObject named "SceneTransitionManager"
2. Add `SceneTransitionManager` script
3. Create fade in/out animation (optional)
4. Assign animator if using transitions

## Testing Checklist

After setup, test the following flows:

- [ ] StartScene → Play Local → CharacterSelect → GameScene → MatchResults
- [ ] StartScene → Play Online → LobbyScene → CharacterSelect → GameScene → MatchResults
- [ ] StartScene → Settings → Change settings → Back → Verify settings persist
- [ ] GameScene → Pause → Settings → Back → Resume
- [ ] GameScene → Pause → Main Menu → StartScene
- [ ] MatchResults → Rematch → CharacterSelect
- [ ] MatchResults → Main Menu → StartScene

## Common Issues and Solutions

### Issue: Scenes not loading
**Solution:** Ensure all scenes are added to Build Settings and scene names match exactly (case-sensitive).

### Issue: Character selection not working
**Solution:** 
- Ensure FighterDef ScriptableObjects are created
- Assign them to `availableFighters` list in CharacterSelectUI
- Check that buttons are assigned correctly

### Issue: Settings not persisting
**Solution:**
- Ensure SettingsManager is a singleton
- Check that PlayerPrefs are being saved (SettingsManager.SaveSettings())
- Verify settings are loaded in Awake()

### Issue: Match results not showing
**Solution:**
- Ensure GameManager is calling MatchResultsData.SetResults()
- Check that MatchResults scene name matches exactly
- Verify MatchResultsUI is reading from MatchResultsData

### Issue: Fighters not spawning
**Solution:**
- Check that spawn points are assigned in LocalGameManager
- Verify FighterFactory.CreateFighter() is working
- Ensure FighterDefs are properly initialized

## Next Steps

After completing setup:

1. **Create FighterDef Assets:**
   - Use FighterFactory.CreateStrikerFighter() and CreateElusiveFighter() as reference
   - Create ScriptableObject assets for each fighter
   - Configure moves, stats, and abilities

2. **Create MoveDef Assets:**
   - Create ScriptableObject assets for each move
   - Configure frame data, damage, hitboxes

3. **Setup Input Actions:**
   - Configure Unity Input System actions
   - Assign to PlayerInputHandler components

4. **Add Visual Assets:**
   - Fighter sprites/animations
   - UI sprites and backgrounds
   - VFX prefabs
   - Status effect icons

5. **Add Audio:**
   - Hit sounds
   - Block sounds
   - Music tracks
   - UI sounds

6. **Test and Balance:**
   - Test all combat mechanics
   - Balance frame data
   - Adjust damage values
   - Test status effects and conditions

## Additional Resources

- See `SCENE_FLOW.md` for detailed scene flow documentation
- See `IMPLEMENTATION_GUIDE.md` for system documentation
- See `IMPLEMENTATION_SUMMARY.md` for feature overview

