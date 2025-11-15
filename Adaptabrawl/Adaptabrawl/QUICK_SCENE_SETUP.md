# Quick Scene Setup Guide

## Current Scene Status

**Existing Scenes:**
- ✅ `Assets/Scenes/StartScene.unity` - Main menu (exists)
- ✅ `Assets/Scenes/GameScene.unity` - Gameplay scene (exists)

**Missing Scenes (Need to Create):**
- ⚠️ `Assets/Scenes/CharacterSelect.unity` - Character selection
- ⚠️ `Assets/Scenes/LobbyScene.unity` - Online lobby
- ⚠️ `Assets/Scenes/SettingsScene.unity` - Settings menu
- ⚠️ `Assets/Scenes/MatchResults.unity` - Match results screen

## Quick Steps to Create Missing Scenes

### Step 1: Create the Scene Files

1. In Unity Editor, navigate to `Assets/Scenes/` folder in Project window
2. Right-click in the folder → **Create → Scene**
3. Name it `CharacterSelect` and save (Ctrl+S)
4. Repeat for:
   - `LobbyScene`
   - `SettingsScene`
   - `MatchResults`

### Step 2: Add Scenes to Build Settings

1. Open **File → Build Settings**
2. For each new scene:
   - Open the scene in Unity
   - Click **Add Open Scenes** button in Build Settings
3. Ensure scenes are in this order (drag to reorder):
   - **StartScene** (index 0) - Must be first!
   - **CharacterSelect** (index 1)
   - **LobbyScene** (index 2)
   - **SettingsScene** (index 3)
   - **GameScene** (index 4)
   - **MatchResults** (index 5)

### Step 3: Basic Scene Setup

For each new scene, you need to:

1. **Create a Canvas:**
   - Right-click Hierarchy → **UI → Canvas**
   - This is required for all UI scenes

2. **Add the Script Component:**
   - Create empty GameObject (Right-click Hierarchy → Create Empty)
   - Name it appropriately (e.g., "CharacterSelectManager")
   - Add the corresponding script:
     - `CharacterSelect` → Add `CharacterSelectUI` component
     - `LobbyScene` → Add `LobbyManager` and `LobbyUI` components
     - `SettingsScene` → Add `SettingsUI` component
     - `MatchResults` → Add `MatchResultsUI` component

3. **Setup UI Elements:**
   - See detailed instructions in `Assets/Scripts/SETUP_GUIDE.md`

## Scene File Locations

All scenes should be in:
```
Adaptabrawl/Adaptabrawl/Assets/Scenes/
```

Current structure:
```
Assets/Scenes/
├── StartScene.unity ✅
├── GameScene.unity ✅
├── CharacterSelect.unity ⚠️ (create this)
├── LobbyScene.unity ⚠️ (create this)
├── SettingsScene.unity ⚠️ (create this)
└── MatchResults.unity ⚠️ (create this)
```

## Verification

After creating scenes, verify:
1. All 6 scenes exist in `Assets/Scenes/` folder
2. All scenes are added to Build Settings
3. StartScene is at index 0 in Build Settings
4. Each scene has a Canvas (for UI scenes)
5. Each scene has its script component attached

## Next Steps

Once scenes are created:
1. Follow `Assets/Scripts/SETUP_GUIDE.md` for detailed UI setup
2. Assign UI element references in each scene's script components
3. Test scene transitions

