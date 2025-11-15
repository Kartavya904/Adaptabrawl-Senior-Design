# Adaptabrawl Scene Flow Documentation

This document describes the complete scene flow and how all scenes connect in the Adaptabrawl game.

## Scene List

1. **StartScene** - Main menu
2. **CharacterSelect** - Character/fighter selection for local matches
3. **LobbyScene** - Online matchmaking and lobby
4. **SettingsScene** - Game settings (audio, video, accessibility)
5. **GameScene** - Main gameplay scene
6. **MatchResults** - Post-match results and statistics

## Scene Flow Diagram

```
StartScene (Main Menu)
    ├── Play Local → CharacterSelect → GameScene → MatchResults → (Rematch/CharacterSelect/Main Menu)
    ├── Play Online → LobbyScene → CharacterSelect → GameScene → MatchResults → (Rematch/Lobby/Main Menu)
    ├── Settings → SettingsScene → (Back to StartScene)
    └── Quit → Exit Game
```

## Detailed Scene Descriptions

### 1. StartScene (Main Menu)

**Script:** `MainMenu.cs`

**Purpose:** Entry point of the game, provides navigation to all major game modes.

**UI Elements:**

- Play Button (shows play options)
- Online Button (direct to online)
- Settings Button
- Quit Button

**Play Options Panel:**

- Local Play Button
- Online Play Button
- Back Button

**Scene Transitions:**

- `PlayLocal()` → Loads `CharacterSelect`
- `PlayOnline()` → Loads `LobbyScene`
- `OpenSettings()` → Loads `SettingsScene`
- `QuitGame()` → Exits application

---

### 2. CharacterSelect

**Script:** `CharacterSelectUI.cs`

**Purpose:** Allows players to select their fighters before a match.

**Features:**

- Player 1 fighter selection (left/right navigation)
- Player 2 fighter selection (left/right navigation)
- Ready confirmation for each player
- Fighter preview (name, portrait)
- Start match button (enabled when both players ready)

**Data Storage:**

- Uses `CharacterSelectData` static class to pass selected fighters to GameScene
- Stores: `selectedFighter1`, `selectedFighter2`, `isLocalMatch`

**Scene Transitions:**

- `StartMatch()` → Loads `GameScene` (with selected fighters)
- `ReturnToMenu()` → Loads `StartScene`

---

### 3. LobbyScene

**Scripts:** `LobbyManager.cs`, `LobbyUI.cs`

**Purpose:** Online matchmaking system with room codes.

**Features:**

- Create Room (generates 6-character room code)
- Join Room (enter room code)
- Ready system (both players must be ready)
- Room code display
- Player ready status indicators

**Scene Transitions:**

- `StartMatch()` → Loads `CharacterSelect` (for online matches)
- `Disconnect()` → Returns to `StartScene`

**Note:** After character selection in online mode, the game proceeds to GameScene.

---

### 4. SettingsScene

**Scripts:** `SettingsManager.cs`, `SettingsUI.cs`

**Purpose:** Configure game settings.

**Settings Categories:**

**Audio:**

- Master Volume
- Music Volume
- SFX Volume

**Video:**

- Quality Level
- Resolution
- VSync
- Target FPS (30/60/120/144/Unlimited)

**Accessibility:**

- UI Scale
- Color Blind Mode
- Show Hitboxes

**Scene Transitions:**

- `GoBack()` → Loads `StartScene`
- Can also be accessed from pause menu during gameplay

**Persistence:** All settings are saved to PlayerPrefs and persist between sessions.

---

### 5. GameScene

**Scripts:**

- `GameManager.cs` - Match management, rounds, win conditions
- `LocalGameManager.cs` - Local match initialization and fighter spawning
- `HUDManager.cs` - In-game UI (health bars, status icons)
- `PauseMenu.cs` - Pause functionality
- `FightingGameCamera.cs` - Camera system

**Purpose:** Main gameplay scene where matches take place.

**Initialization Flow:**

1. `LocalGameManager` reads selected fighters from `CharacterSelectData`
2. Spawns fighters at designated spawn points
3. Sets up player input handlers
4. Initializes `GameManager` for match management
5. Subscribes to match events

**Match Flow:**

1. Round starts
2. Players fight
3. Round ends (timeout or KO)
4. Check if match is won (rounds to win reached)
5. If match continues, start next round
6. If match ends, transition to MatchResults

**Pause Menu:**

- Accessible via Escape key
- Options: Resume, Settings, Main Menu, Quit

**Scene Transitions:**

- Match End → Loads `MatchResults`
- Pause → Settings → `SettingsScene` (returns after)
- Pause → Main Menu → `StartScene`

---

### 6. MatchResults

**Script:** `MatchResultsUI.cs`

**Purpose:** Display match results and statistics.

**Displayed Information:**

- Winner announcement
- Match score (e.g., "2 - 1")
- Round-by-round results
- Player 1 info (name, wins)
- Player 2 info (name, wins)

**Buttons:**

- Rematch → Returns to `CharacterSelect` (or `LobbyScene` for online)
- Main Menu → Returns to `StartScene`
- Character Select → Returns to `CharacterSelect`

**Data Source:**

- Uses `MatchResultsData` static class populated by `GameManager`
- Contains: players, winner, round wins, round winners list

**Scene Transitions:**

- `Rematch()` → Loads `CharacterSelect` (or `LobbyScene` for online)
- `ReturnToMainMenu()` → Loads `StartScene`
- `ReturnToCharacterSelect()` → Loads `CharacterSelect`

---

## Data Flow Between Scenes

### Character Selection → Game Scene

**Data Carrier:** `CharacterSelectData` (static class)

```csharp
CharacterSelectData.selectedFighter1 = FighterDef
CharacterSelectData.selectedFighter2 = FighterDef
CharacterSelectData.isLocalMatch = bool
```

**Usage in GameScene:**

- `LocalGameManager` reads this data in `InitializeLocalMatch()`
- Creates fighters using `FighterFactory.CreateFighter()`

---

### Game Scene → Match Results

**Data Carrier:** `MatchResultsData` (static class)

```csharp
MatchResultsData.results = MatchResults
MatchResultsData.hasResults = bool
MatchResultsData.isLocalMatch = bool
MatchResultsData.rematchRequested = bool
```

**MatchResults Structure:**

```csharp
{
    player1: FighterController,
    player2: FighterController,
    winner: FighterController,
    player1Wins: int,
    player2Wins: int,
    roundWinners: List<FighterController>,
    totalRounds: int
}
```

**Usage in MatchResults:**

- `MatchResultsUI` reads this data in `LoadMatchResults()`
- Displays all match statistics

---

## Settings Persistence

**Storage:** PlayerPrefs

**Settings Saved:**

- MasterVolume (float)
- MusicVolume (float)
- SFXVolume (float)
- TargetFPS (int)
- VSync (int: 0/1)
- QualityLevel (int)
- UIScale (float)
- ColorBlindMode (int: 0/1)
- ShowHitboxes (int: 0/1)

**Loading:**

- `SettingsManager` loads settings in `Awake()`
- Settings persist across all scenes via singleton pattern

---

## Scene Setup Checklist

### StartScene

- [ ] MainMenu script attached to GameObject
- [ ] UI Canvas with buttons configured
- [ ] Button references assigned in inspector

### CharacterSelect

- [ ] CharacterSelectUI script attached
- [ ] Fighter list populated with FighterDef assets
- [ ] UI elements for both players configured
- [ ] Navigation buttons assigned

### LobbyScene

- [ ] LobbyManager script attached
- [ ] LobbyUI script attached
- [ ] UI panels for create/join/lobby configured
- [ ] Room code display text assigned

### SettingsScene

- [ ] SettingsUI script attached
- [ ] SettingsManager singleton in scene (or DontDestroyOnLoad)
- [ ] All sliders, dropdowns, toggles assigned
- [ ] Back button configured

### GameScene

- [ ] LocalGameManager script attached
- [ ] GameManager script attached
- [ ] HUDManager script attached
- [ ] PauseMenu script attached
- [ ] FightingGameCamera script attached
- [ ] Player spawn points assigned
- [ ] HUD UI elements configured

### MatchResults

- [ ] MatchResultsUI script attached
- [ ] Results display UI configured
- [ ] Navigation buttons assigned

---

## Build Settings

Ensure all scenes are added to Build Settings in this order:

1. StartScene (index 0)
2. CharacterSelect
3. LobbyScene
4. SettingsScene
5. GameScene
6. MatchResults

**To add scenes to build:**

1. File → Build Settings
2. Add Open Scenes (or drag scenes from Project window)
3. Ensure StartScene is at index 0 (first scene to load)

---

## Additional Notes

### Online vs Local Flow

**Local Match:**
StartScene → CharacterSelect → GameScene → MatchResults

**Online Match:**
StartScene → LobbyScene → CharacterSelect → GameScene → MatchResults

The main difference is the lobby step for online matches. Character selection happens after lobby connection for online matches.

### Pause Menu Integration

The pause menu can be accessed from GameScene and provides quick access to settings without leaving the match. When returning from settings, the game resumes from where it was paused.

### Rematch Flow

After a match ends:

- **Rematch Button:** Returns to character select (or lobby for online) to start a new match
- **Main Menu Button:** Returns to main menu
- **Character Select Button:** Returns to character select

The rematch flow preserves the match type (local/online) to route players correctly.

---

## Future Enhancements

Potential additions to the scene flow:

- Training Mode scene
- Tutorial/Onboarding scene
- Replay Viewer scene
- Profile/Statistics scene
- Customization scene (fighter skins, etc.)
