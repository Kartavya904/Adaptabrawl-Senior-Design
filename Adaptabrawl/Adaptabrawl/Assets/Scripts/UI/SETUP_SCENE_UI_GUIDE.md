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

Inside the `SetupScene`, you need four separate UI Panels on your Canvas:

1. **LocalJoinPanel**: Contains UI prompting Player 2 to join (Local Play only).
2. **ControllerConfigPanel**: Contains UI for Keyboard/Gamepad selection.
3. **CharacterSelectPanel**: Contains your existing character selection UI.
4. **ArenaSelectPanel**: Contains UI for picking the stage.

_By default, if you test offline/locally, only the `LocalJoinPanel` should be active when the scene starts. If you load online, `ControllerConfigPanel` starts._

---

## 3. Wiring the Scripts

### A. SetupSceneManager

1. Create an empty GameObject in the scene called `SetupManager`.
2. Attach the `SetupSceneManager.cs` script to it.
3. Drag the four UI panels (`LocalJoinPanel`, `ControllerConfigPanel`, `CharacterSelectPanel`, `ArenaSelectPanel`) into the script's inspector slots.

### A2. LocalJoinUI (New)

1. Attach `LocalJoinUI.cs` to the `LocalJoinPanel`.
2. Assign the `SetupSceneManager` reference.
3. Assign a `P2 Join Button` and optionally status text for P1/P2.

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

#### Making characters show up on Character Select

- **Available Fighters (required)**  
  In the Inspector, find **Character Select UI** → **Fighter Selection** → **Available Fighters**.
  - Click **+** to add elements.
  - Drag your **FighterDef** assets (e.g. Sharp Tooth, or any created via the Fighter Setup Wizard) into the list.
  - If the list is empty at runtime, only default Striker/Elusive are used and your custom characters won’t appear in the carousel.

- **Where the character preview appears (recommended: world anchors)**  
  To show the 3D model on character select, use **Preview World Anchors**: create two empty GameObjects (e.g. **P1 Preview Anchor**, **P2 Preview Anchor**), place them where the camera sees the scene (e.g. P1 at **(-3, 0, 0)**, P2 at **(3, 0, 0)**), and assign to **Player 1 Preview World Anchor** and **Player 2 Preview World Anchor**. The 3D preview is parented there so the main camera renders it.  
  The script can also use a container. Assign **one** of these per player (container preferred):
  - **Player 1 Fighter Container** / **Player 2 Fighter Container**: Empty `Transform` (or GameObject) under the Canvas where the preview should appear (e.g. under the character portrait area).
  - **Player 1 Fighter Image** / **Player 2 Fighter Image**: If you don’t set a Container, the preview is parented to the Image’s transform and the Image is hidden when a fighter is selected.  
    So: either assign the **Fighter Container** for each player, or the **Fighter Image**; the preview will show in that area.

- **Preview size**  
  If the character is too big or too small in the slot:
  - **Preview Scale**: world-space scale (e.g. `0.5`).
  - **Preview Scale In UI**: multiplier when the container is under a Canvas (e.g. `50`–`200`). Tweak until the figure fits the slot.

- **Placeholder when no prefab**  
  If a fighter has no prefab (e.g. runtime-only Striker/Elusive), assign **Preview Placeholder Sprite** so a silhouette or icon shows instead of nothing.

- **3D model (e.g. Sharp Tooth)**  
  If the canvas is **Screen Space - Overlay**, the preview can end up in screen coordinates where the main camera doesn’t draw it. Use **world anchors** (empty GameObjects in the scene) (e.g. "P1 Preview Anchor" and "P2 Preview Anchor"), place them where the camera sees the scene (e.g. X = -3 and X = 3, Y = 0). Assign them to **Player 1 Preview World Anchor** and **Player 2 Preview World Anchor**. The 3D preview will be parented there and the main camera will show it. If the preview shows in Scene view but not in **Game view**, add a **Raw Image** in each placeholder and assign to **Player 1/2 Preview Raw Image** so the 3D is rendered into the UI. Use **Preview Camera Orthographic Size** (e.g. `2.5`) to control how much of the fighter is visible. The preview animation (including the initial delay) starts only when the Character Select panel is visible.

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
