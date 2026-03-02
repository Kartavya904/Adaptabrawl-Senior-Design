# Complete Match Setup Guide

## 🎮 From Zero to Playable Match in 15 Minutes

This guide takes you through **every step** to set up a complete 2-player fighting game match.

**What you'll have at the end:**
- 2 fighters in a scene
- Input controls for both players
- Health bars
- Win/lose conditions
- Fully playable match!

---

## 📋 Prerequisites

Before starting, make sure you have:
- Unity project open
- Adaptabrawl system imported
- Shinabro assets in `Assets/Shinabro/` folder

---

## 🚀 Part 1: Create Your Fighters (5 minutes)

### Step 1.1: Generate Moves for Fighter 1

1. **Open Move Library Generator**
   - Menu bar → `Adaptabrawl` → `Move Library Generator`

2. **Configure Generator**
   ```
   Weapon Type: Unarmed_Fighter
   Generate Hitboxes: ✓ (checked)
   Auto-Calculate Frames: ✓ (checked)
   Setup Combo Chains: ✓ (checked)
   Attack 1 Damage: 8
   Attack 2 Damage: 10
   Attack 3 Damage: 15
   Save Path: Assets/Moves/Fighter1/
   ```

3. **Click "Generate Move Library"**
   - Wait for progress bar
   - Success dialog will appear

4. **Result:** You now have `MoveLibrary_Unarmed_Fighter.asset` with 16+ moves!

### Step 1.2: Create Fighter 1

1. **Open Fighter Setup Wizard**
   - Menu bar → `Adaptabrawl` → `Fighter Setup Wizard`

2. **Configure Fighter**
   ```
   Step 1 - Select Shinabro Prefab:
     Click "Browse Shinabro Prefabs"
     Select: Player_Fighter

   Step 2 - Basic Information:
     Fighter Name: Fighter1
     Description: Fast martial artist

   Step 3 - Choose Stats Preset:
     Preset: Fast
   ```

3. **Click "Create Fighter Definition"**
   - Save as: `Assets/Fighters/Fighter1.asset`

4. **Result:** You now have `Fighter1.asset` with default hurtboxes!

### Step 1.3: Assign Moves to Fighter 1

1. **Open Fighter1.asset** (should auto-select after creation)

2. **Scroll to "Moveset" section**

3. **Assign moves:**
   ```
   Light Attack: 
     Navigate to Assets/Moves/Fighter1/
     Drag: Unarmed_Fighter_Attack1

   Heavy Attack:
     Drag: Unarmed_Fighter_Attack3

   Special Moves:
     Click + to add element
     Drag: Unarmed_Fighter_Skill1
     Click + to add element
     Drag: Unarmed_Fighter_Skill6
   ```

4. **Save** (Ctrl+S or Cmd+S)

### Step 1.4: Repeat for Fighter 2

1. **Generate moves for Hammer**
   ```
   Adaptabrawl → Move Library Generator
   Weapon Type: Hammer
   Save Path: Assets/Moves/Fighter2/
   Generate
   ```

2. **Create Fighter 2**
   ```
   Adaptabrawl → Fighter Setup Wizard
   Prefab: Player_Hammer
   Fighter Name: Fighter2
   Preset: Tank
   Save as: Assets/Fighters/Fighter2.asset
   ```

3. **Assign moves to Fighter 2**
   ```
   Open Fighter2.asset
   Moveset:
     Light Attack: Hammer_Attack1
     Heavy Attack: Hammer_Attack3
     Special Moves: [Hammer_Skill4, Hammer_Skill7]
   Save
   ```

**✅ Checkpoint:** You now have 2 complete fighters with movesets!

---

## 🏟️ Part 2: Set Up the Game Scene (5 minutes)

### Step 2.1: Create/Open Your Game Scene

1. **Create new scene** or open existing
   - File → New Scene
   - Or: Open `Assets/Scenes/GameScene.unity`

2. **Save scene**
   - File → Save As
   - Name: `FightingMatch.unity`
   - Location: `Assets/Scenes/`

### Step 2.2: Create the Ground

1. **Create ground platform**
   ```
   Hierarchy → Right-click → 2D Object → Sprite → Square
   Name it: "Ground"
   ```

2. **Configure Ground**
   ```
   Inspector:
     Transform:
       Position: (0, -3, 0)
       Scale: (20, 1, 1)
     
     Add Component: Box Collider 2D
       Size: Leave default
     
     Sprite Renderer:
       Color: Gray or brown
   ```

3. **Set layer**
   ```
   Top of Inspector:
     Layer: Ground (or Default)
   ```

### Step 2.3: Spawn Fighter 1

1. **Create empty GameObject**
   ```
   Hierarchy → Right-click → Create Empty
   Name: "Player1"
   ```

2. **Set position**
   ```
   Transform:
     Position: (-3, 0, 0)
     Rotation: (0, 0, 0)
     Scale: (1, 1, 1)
   ```

3. **Add Fighter components** (using script)
   ```
   Create new C# script: SpawnFighter.cs
   ```

   **OR manually (easier for now):**

4. **Use Fighter Factory via Inspector helper**
   
   Create a simple spawner script:

### Step 2.4: Create Fighter Spawner Script

**Create:** `Assets/Scripts/Gameplay/FighterSpawner.cs`

```csharp
using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using Adaptabrawl.Gameplay;

public class FighterSpawner : MonoBehaviour
{
    [Header("Fighter Configurations")]
    public FighterDef player1Fighter;
    public FighterDef player2Fighter;
    
    [Header("Spawn Positions")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;
    
    [Header("Player Tags")]
    public string player1Tag = "Player1";
    public string player2Tag = "Player2";
    
    private FighterController player1Controller;
    private FighterController player2Controller;
    
    public FighterController Player1 => player1Controller;
    public FighterController Player2 => player2Controller;
    
    void Start()
    {
        SpawnFighters();
    }
    
    void SpawnFighters()
    {
        // Spawn Player 1
        if (player1Fighter != null && player1SpawnPoint != null)
        {
            player1Controller = FighterFactory.CreateFighter(
                player1Fighter, 
                player1SpawnPoint.position, 
                facingRight: true
            );
            player1Controller.gameObject.tag = player1Tag;
            player1Controller.gameObject.name = "Player1_" + player1Fighter.fighterName;
            
            Debug.Log($"Spawned Player 1: {player1Fighter.fighterName}");
        }
        
        // Spawn Player 2
        if (player2Fighter != null && player2SpawnPoint != null)
        {
            player2Controller = FighterFactory.CreateFighter(
                player2Fighter, 
                player2SpawnPoint.position, 
                facingRight: false
            );
            player2Controller.gameObject.tag = player2Tag;
            player2Controller.gameObject.name = "Player2_" + player2Fighter.fighterName;
            
            Debug.Log($"Spawned Player 2: {player2Fighter.fighterName}");
        }
    }
}
```

### Step 2.5: Set Up Spawner in Scene

1. **Create Game Manager**
   ```
   Hierarchy → Right-click → Create Empty
   Name: "GameManager"
   Position: (0, 0, 0)
   ```

2. **Add FighterSpawner component**
   ```
   Select GameManager
   Inspector → Add Component → FighterSpawner
   ```

3. **Create spawn points**
   ```
   Hierarchy → Right-click → Create Empty
   Name: "Player1SpawnPoint"
   Position: (-3, 0, 0)
   
   Hierarchy → Right-click → Create Empty
   Name: "Player2SpawnPoint"
   Position: (3, 0, 0)
   ```

4. **Assign references**
   ```
   Select GameManager
   Inspector → FighterSpawner:
     Player 1 Fighter: Drag Fighter1.asset
     Player 2 Fighter: Drag Fighter2.asset
     Player 1 Spawn Point: Drag Player1SpawnPoint
     Player 2 Spawn Point: Drag Player2SpawnPoint
   ```

### Step 2.6: Set Up Camera

1. **Select Main Camera**

2. **Configure for 2D fighting game**
   ```
   Inspector:
     Transform:
       Position: (0, 0, -10)
       Rotation: (0, 0, 0)
     
     Camera:
       Projection: Orthographic
       Size: 5 (adjust to see both fighters)
       Background: Black or sky blue
   ```

**✅ Checkpoint:** Scene is set up! Press Play to test spawning (won't move yet).

---

## 🎮 Part 3: Set Up Input System (3 minutes)

### Step 3.1: Create Input Handler

**Create:** `Assets/Scripts/Input/TwoPlayerInputHandler.cs`

```csharp
using UnityEngine;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Data;

public class TwoPlayerInputHandler : MonoBehaviour
{
    [Header("Player References")]
    public FighterSpawner spawner;
    
    [Header("Player 1 Controls (WASD + Keys)")]
    public KeyCode p1Left = KeyCode.A;
    public KeyCode p1Right = KeyCode.D;
    public KeyCode p1Up = KeyCode.W;
    public KeyCode p1Down = KeyCode.S;
    public KeyCode p1LightAttack = KeyCode.F;
    public KeyCode p1HeavyAttack = KeyCode.G;
    public KeyCode p1Special1 = KeyCode.R;
    public KeyCode p1Special2 = KeyCode.T;
    public KeyCode p1Block = KeyCode.LeftShift;
    public KeyCode p1Dodge = KeyCode.Space;
    
    [Header("Player 2 Controls (Arrow Keys + NumPad)")]
    public KeyCode p2Left = KeyCode.LeftArrow;
    public KeyCode p2Right = KeyCode.RightArrow;
    public KeyCode p2Up = KeyCode.UpArrow;
    public KeyCode p2Down = KeyCode.DownArrow;
    public KeyCode p2LightAttack = KeyCode.Keypad1;
    public KeyCode p2HeavyAttack = KeyCode.Keypad2;
    public KeyCode p2Special1 = KeyCode.Keypad4;
    public KeyCode p2Special2 = KeyCode.Keypad5;
    public KeyCode p2Block = KeyCode.RightShift;
    public KeyCode p2Dodge = KeyCode.Keypad0;
    
    private AnimationBridge p1AnimBridge;
    private AnimationBridge p2AnimBridge;
    
    void Start()
    {
        StartCoroutine(InitializeAfterSpawn());
    }
    
    System.Collections.IEnumerator InitializeAfterSpawn()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (spawner != null)
        {
            if (spawner.Player1 != null)
                p1AnimBridge = spawner.Player1.GetComponent<AnimationBridge>();
            
            if (spawner.Player2 != null)
                p2AnimBridge = spawner.Player2.GetComponent<AnimationBridge>();
        }
    }
    
    void Update()
    {
        HandlePlayer1Input();
        HandlePlayer2Input();
    }
    
    void HandlePlayer1Input()
    {
        if (spawner?.Player1 == null || p1AnimBridge == null) return;
        
        FighterDef fighter = spawner.Player1.FighterDef;
        
        // Light Attack
        if (Input.GetKeyDown(p1LightAttack) && fighter.lightAttack != null)
        {
            AnimatedMoveDef animMove = fighter.lightAttack as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
            {
                p1AnimBridge.PlayMove(animMove);
            }
        }
        
        // Heavy Attack
        if (Input.GetKeyDown(p1HeavyAttack) && fighter.heavyAttack != null)
        {
            AnimatedMoveDef animMove = fighter.heavyAttack as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
            {
                p1AnimBridge.PlayMove(animMove);
            }
        }
        
        // Special 1
        if (Input.GetKeyDown(p1Special1) && fighter.specialMoves != null && fighter.specialMoves.Length > 0)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[0] as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
            {
                p1AnimBridge.PlayMove(animMove);
            }
        }
        
        // Special 2
        if (Input.GetKeyDown(p1Special2) && fighter.specialMoves != null && fighter.specialMoves.Length > 1)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[1] as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
            {
                p1AnimBridge.PlayMove(animMove);
            }
        }
    }
    
    void HandlePlayer2Input()
    {
        if (spawner?.Player2 == null || p2AnimBridge == null) return;
        
        FighterDef fighter = spawner.Player2.FighterDef;
        
        // Light Attack
        if (Input.GetKeyDown(p2LightAttack) && fighter.lightAttack != null)
        {
            AnimatedMoveDef animMove = fighter.lightAttack as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
            {
                p2AnimBridge.PlayMove(animMove);
            }
        }
        
        // Heavy Attack
        if (Input.GetKeyDown(p2HeavyAttack) && fighter.heavyAttack != null)
        {
            AnimatedMoveDef animMove = fighter.heavyAttack as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
            {
                p2AnimBridge.PlayMove(animMove);
            }
        }
        
        // Special 1
        if (Input.GetKeyDown(p2Special1) && fighter.specialMoves != null && fighter.specialMoves.Length > 0)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[0] as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
            {
                p2AnimBridge.PlayMove(animMove);
            }
        }
        
        // Special 2
        if (Input.GetKeyDown(p2Special2) && fighter.specialMoves != null && fighter.specialMoves.Length > 1)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[1] as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
            {
                p2AnimBridge.PlayMove(animMove);
            }
        }
    }
}
```

### Step 3.2: Add Input Handler to Scene

1. **Select GameManager**

2. **Add component**
   ```
   Inspector → Add Component → TwoPlayerInputHandler
   ```

3. **Assign spawner reference**
   ```
   Inspector → TwoPlayerInputHandler:
     Spawner: Drag GameManager (itself, since it has FighterSpawner)
   ```

**✅ Checkpoint:** Press Play and test attacks!
- Player 1: F (light), G (heavy), R/T (specials)
- Player 2: NumPad 1 (light), NumPad 2 (heavy), NumPad 4/5 (specials)

---

## 💊 Part 4: Add Health Bars UI (3 minutes)

### Step 4.1: Create Canvas

1. **Create UI Canvas**
   ```
   Hierarchy → Right-click → UI → Canvas
   Name: "MatchUI"
   ```

2. **Configure Canvas**
   ```
   Inspector:
     Canvas:
       Render Mode: Screen Space - Overlay
     
     Canvas Scaler:
       UI Scale Mode: Scale With Screen Size
       Reference Resolution: 1920 x 1080
   ```

### Step 4.2: Create Player 1 Health Bar

1. **Create panel for P1**
   ```
   Right-click Canvas → UI → Panel
   Name: "Player1HealthPanel"
   ```

2. **Position panel**
   ```
   Rect Transform:
     Anchor Preset: Top-Left
     Pos X: 100
     Pos Y: -50
     Width: 400
     Height: 40
   ```

3. **Add background**
   ```
   Panel → Image:
     Color: Dark red (R:0.3, G:0, B:0, A:1)
   ```

4. **Create health bar fill**
   ```
   Right-click Player1HealthPanel → UI → Image
   Name: "HealthFill"
   ```

5. **Configure fill**
   ```
   Rect Transform:
     Anchor: Stretch (hold Alt+Shift, click stretch)
     Left, Top, Right, Bottom: All 0
   
   Image:
     Color: Green (R:0, G:1, B:0)
     Image Type: Filled
     Fill Method: Horizontal
     Fill Origin: Left
     Fill Amount: 1
   ```

6. **Add player name text**
   ```
   Right-click Player1HealthPanel → UI → Text - TextMeshPro
   Name: "PlayerName"
   ```

7. **Configure text**
   ```
   Rect Transform: Stretch to fill
   
   TextMeshPro:
     Text: "Fighter 1"
     Font Size: 24
     Alignment: Center + Middle
     Color: White
   ```

### Step 4.3: Create Player 2 Health Bar

1. **Duplicate Player 1 panel**
   ```
   Select Player1HealthPanel
   Ctrl+D (or Cmd+D)
   Rename: "Player2HealthPanel"
   ```

2. **Reposition**
   ```
   Rect Transform:
     Anchor Preset: Top-Right
     Pos X: -100
     Pos Y: -50
   ```

3. **Update text**
   ```
   PlayerName text: "Fighter 2"
   ```

### Step 4.4: Create Match Manager Script

**Create:** `Assets/Scripts/Gameplay/MatchManager.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchManager : MonoBehaviour
{
    [Header("References")]
    public FighterSpawner spawner;
    
    [Header("UI References")]
    public Image player1HealthFill;
    public Image player2HealthFill;
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player2NameText;
    public GameObject winPanel;
    public TextMeshProUGUI winText;
    
    [Header("Match Settings")]
    public float matchStartDelay = 1f;
    
    private bool matchActive = false;
    private bool matchEnded = false;
    
    void Start()
    {
        StartCoroutine(InitializeMatch());
    }
    
    System.Collections.IEnumerator InitializeMatch()
    {
        yield return new WaitForSeconds(matchStartDelay);
        
        if (spawner != null)
        {
            // Subscribe to health changes
            if (spawner.Player1 != null)
            {
                spawner.Player1.OnHealthChanged += UpdatePlayer1Health;
                spawner.Player1.OnDeath += OnPlayer1Death;
                player1NameText.text = spawner.Player1.FighterDef.fighterName;
            }
            
            if (spawner.Player2 != null)
            {
                spawner.Player2.OnHealthChanged += UpdatePlayer2Health;
                spawner.Player2.OnDeath += OnPlayer2Death;
                player2NameText.text = spawner.Player2.FighterDef.fighterName;
            }
            
            matchActive = true;
            
            if (winPanel != null)
                winPanel.SetActive(false);
        }
    }
    
    void UpdatePlayer1Health(float current, float max)
    {
        if (player1HealthFill != null)
        {
            player1HealthFill.fillAmount = current / max;
        }
    }
    
    void UpdatePlayer2Health(float current, float max)
    {
        if (player2HealthFill != null)
        {
            player2HealthFill.fillAmount = current / max;
        }
    }
    
    void OnPlayer1Death()
    {
        if (matchEnded) return;
        EndMatch("Player 2 Wins!");
    }
    
    void OnPlayer2Death()
    {
        if (matchEnded) return;
        EndMatch("Player 1 Wins!");
    }
    
    void EndMatch(string winner)
    {
        matchEnded = true;
        matchActive = false;
        
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            winText.text = winner;
        }
        
        Debug.Log($"Match Over! {winner}");
        
        // Optionally restart after delay
        StartCoroutine(RestartAfterDelay(5f));
    }
    
    System.Collections.IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}
```

### Step 4.5: Create Win Panel

1. **Create win panel**
   ```
   Right-click Canvas → UI → Panel
   Name: "WinPanel"
   ```

2. **Position**
   ```
   Rect Transform:
     Anchor: Center
     Width: 600
     Height: 300
   ```

3. **Style**
   ```
   Image:
     Color: Semi-transparent black (R:0, G:0, B:0, A:0.8)
   ```

4. **Add win text**
   ```
   Right-click WinPanel → UI → Text - TextMeshPro
   Name: "WinText"
   ```

5. **Configure**
   ```
   TextMeshPro:
     Text: "Player 1 Wins!"
     Font Size: 72
     Alignment: Center + Middle
     Color: Gold
   ```

### Step 4.6: Wire Up Match Manager

1. **Select GameManager**

2. **Add MatchManager component**
   ```
   Inspector → Add Component → MatchManager
   ```

3. **Assign references**
   ```
   Spawner: Drag GameManager
   Player 1 Health Fill: Drag Player1HealthPanel/HealthFill
   Player 2 Health Fill: Drag Player2HealthPanel/HealthFill
   Player 1 Name Text: Drag Player1HealthPanel/PlayerName
   Player 2 Name Text: Drag Player2HealthPanel/PlayerName
   Win Panel: Drag WinPanel
   Win Text: Drag WinPanel/WinText
   ```

**✅ Checkpoint:** Press Play - health bars should update and show winner!

---

## 🎯 Part 5: Final Setup & Testing (2 minutes)

### Step 5.1: Configure Layers for Collision

1. **Create layers**
   ```
   Edit → Project Settings → Tags and Layers
   
   User Layer 8: Fighter
   User Layer 9: Hitbox
   User Layer 10: Ground
   ```

2. **Set layer collision matrix**
   ```
   Edit → Project Settings → Physics 2D
   Layer Collision Matrix:
     Fighter ✓ Ground
     Fighter ✓ Fighter
     Hitbox ✓ Fighter
   ```

### Step 5.2: Assign Layers

1. **Ground**
   ```
   Select Ground GameObject
   Inspector → Layer: Ground
   ```

2. **Fighters** (will be set automatically by spawner, but verify)
   ```
   After spawning, check:
     Player1 → Layer: Fighter
     Player2 → Layer: Fighter
   ```

### Step 5.3: Save Everything

1. **Save scene**
   ```
   File → Save (Ctrl+S / Cmd+S)
   ```

2. **Save project**
   ```
   File → Save Project
   ```

---

## 🎮 Controls Reference

### Player 1 (Left Side)
```
Movement:
  W - Up
  A - Left  
  S - Down
  D - Right

Attacks:
  F - Light Attack
  G - Heavy Attack
  R - Special 1
  T - Special 2
  
Defense:
  Left Shift - Block
  Space - Dodge
```

### Player 2 (Right Side)
```
Movement:
  Arrow Keys

Attacks:
  NumPad 1 - Light Attack
  NumPad 2 - Heavy Attack
  NumPad 4 - Special 1
  NumPad 5 - Special 2
  
Defense:
  Right Shift - Block
  NumPad 0 - Dodge
```

---

## ▶️ Testing Your Match

### Step 1: Press Play

1. **Click Play button** in Unity Editor

2. **Watch fighters spawn**
   - Should see both fighters appear
   - Health bars should show full (green)
   - Names should display

### Step 2: Test Attacks

1. **Player 1 attacks**
   - Press F (light attack)
   - Press G (heavy attack)
   - Watch animations play

2. **Player 2 attacks**
   - Press NumPad 1 (light attack)
   - Press NumPad 2 (heavy attack)

### Step 3: Test Combat

1. **Move fighters close**
   - Use movement keys
   - Get in attack range

2. **Hit each other**
   - Land attacks
   - Watch health bars decrease
   - See hit effects

3. **Play until someone dies**
   - Health reaches 0
   - Win panel shows
   - Scene restarts after 5 seconds

---

## 🐛 Troubleshooting

### Issue: Fighters don't spawn
**Solution:**
- Check FighterSpawner has FighterDef assets assigned
- Verify spawn points exist and are assigned
- Check Console for error messages

### Issue: Attacks don't work
**Solution:**
- Verify moves are assigned to FighterDef
- Check TwoPlayerInputHandler is on GameManager
- Ensure spawner reference is assigned
- Moves must be AnimatedMoveDef type

### Issue: No damage/health doesn't decrease
**Solution:**
- Check HitboxManager component exists on fighters
- Verify DamageSystem component exists
- Check collision layers are set up correctly
- Ensure hitboxes have correct layer

### Issue: Health bars don't update
**Solution:**
- Verify MatchManager has UI references assigned
- Check health bar Image component is set to "Filled" type
- Ensure event subscriptions happen (check Console)

### Issue: Fighters fall through ground
**Solution:**
- Add BoxCollider2D to Ground
- Set Ground layer in collision matrix
- Ensure fighters have Rigidbody2D

### Issue: Can't see fighters
**Solution:**
- Adjust Camera Size (increase to 5-8)
- Check Camera Position (should be at z: -10)
- Verify spawn points are in camera view

---

## 🎨 Optional Enhancements

### Add Round Timer
```csharp
// In MatchManager
public float roundTime = 90f;
private float currentTime;

void Update()
{
    if (matchActive)
    {
        currentTime += Time.deltaTime;
        if (currentTime >= roundTime)
        {
            EndMatch("Time Out!");
        }
    }
}
```

### Add Pause Menu
```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        Time.timeScale = Time.timeScale == 0 ? 1 : 0;
        pausePanel.SetActive(Time.timeScale == 0);
    }
}
```

### Add Sound Effects
```csharp
// In MoveDef
public AudioClip attackSound;

// In AnimationBridge.OnHitboxActivate
AudioSource.PlayClipAtPoint(move.attackSound, transform.position);
```

### Add Camera Shake on Hit
```csharp
// Create CameraShake script
public void Shake(float intensity)
{
    StartCoroutine(ShakeCoroutine(intensity));
}
```

---

## ✅ Checklist

Before saying "it works":

- [ ] Both fighters spawn correctly
- [ ] Health bars show and are full
- [ ] Player 1 can attack with F/G keys
- [ ] Player 2 can attack with NumPad keys
- [ ] Attacks play animations
- [ ] Attacks deal damage when hitting
- [ ] Health bars decrease when hit
- [ ] Win panel shows when someone dies
- [ ] Scene restarts after match ends
- [ ] No errors in Console

---

## 🎉 You Did It!

**Congratulations! You now have a fully functional 2-player fighting game match!**

### What You've Accomplished:
✅ Created 2 complete fighters with movesets  
✅ Set up a game scene with spawn points  
✅ Implemented 2-player input system  
✅ Added health bars and UI  
✅ Created win/lose conditions  
✅ Built a complete match flow  

### Next Steps:
- Add more fighters with different weapons
- Create multiple stages/arenas
- Implement combo counters
- Add special effects
- Create character select screen
- Add more game modes

**You're ready to expand and polish your fighting game!** 🥊🎮

