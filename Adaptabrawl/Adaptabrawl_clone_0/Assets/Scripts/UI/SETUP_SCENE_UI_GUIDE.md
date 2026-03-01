# Setup Scene - Unity Editor Guide

This guide explains how to wire up the new Pre-Game Setup flow in the Unity Editor. **We have deliberately not modified your GameScene scripts**, so your gameplay team can safely pull data from the new static variables when they are ready.

---

## 1. Scene Renaming & Build Settings
We suggest renaming your `CharacterSelect` scene to `SetupScene` to reflect its new unified purpose.
1. Open Unity and locate `CharacterSelect.unity`.
2. Rename it to `SetupScene.unity`.
3. Open **File** > **Build Settings** and ensure `SetupScene` replaces `CharacterSelect` at Index 1.

---

## 2. Setting Up the Canvas Panels
Inside the `SetupScene`, you need three separate UI Panels on your Canvas:

1. **ControllerConfigPanel**: Contains UI for Keyboard/Gamepad selection.
2. **CharacterSelectPanel**: Contains your existing character selection UI.
3. **ArenaSelectPanel**: Contains UI for picking the stage.

*By default, only the `ControllerConfigPanel` should be active when the scene starts. The others should be disabled.*

---

## 3. Wiring the Scripts

### A. SetupSceneManager
1. Create an empty GameObject in the scene called `SetupManager`.
2. Attach the `SetupSceneManager.cs` script to it.
3. Drag the three UI panels (`ControllerConfigPanel`, `CharacterSelectPanel`, `ArenaSelectPanel`) into the script's inspector slots.

### B. ControllerConfigUI
1. Attach `ControllerConfigUI.cs` to the `ControllerConfigPanel`.
2. Assign the `SetupSceneManager` reference in the inspector.
3. Assign all the text, toggle buttons, and ready buttons for Player 1 and Player 2.
4. Assign the "Continue" to Character Select button.

### C. CharacterSelectUI (Update)
1. Select your `CharacterSelectPanel`.
2. Ensure `CharacterSelectUI.cs` is attached.
3. You will see a new slot for `SetupSceneManager`. Drag your `SetupManager` GameObject here. 
4. The "Start Match" button will automatically route to the Arena Select panel now.

### D. ArenaSelectUI
1. Attach `ArenaSelectUI.cs` to the `ArenaSelectPanel`.
2. Assign the `SetupSceneManager` reference.
3. Assign the UI text element for the Arena name, the Left/Right selection arrows, and the "Start Match" button.

---

## 4. How the Game Scene Retrieves the Data
Your gameplay programmers can now spawn the players and the map without needing to modify the flow.

**Characters:**
```csharp
FighterDef p1 = CharacterSelectData.selectedFighter1;
FighterDef p2 = CharacterSelectData.selectedFighter2;
```

**Arena / Map:**
```csharp
string chosenArena = ArenaSelectData.selectedArenaName;
// Use chosenArena to determine which prefab to spawn or which stage variables to load!
```
