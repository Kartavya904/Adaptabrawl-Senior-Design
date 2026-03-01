using UnityEngine;
using UnityEngine.InputSystem;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Data;

/// <summary>
/// Handles input for both players in a local 2-player match.
/// Supports both Keyboard mapping AND Gamepads via the new Input System.
/// Player 1: WASD / Gamepad 1
/// Player 2: Arrow Keys / Gamepad 2
/// </summary>
public class TwoPlayerInputHandler : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Direct reference to Player 1's FighterController (assign in Inspector)")]
    public FighterController player1Controller;

    [Tooltip("Direct reference to Player 2's FighterController (assign in Inspector)")]
    public FighterController player2Controller;
    
    [Header("Player 1 Controls (Keyboard)")]
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
    
    [Header("Player 2 Controls (Keyboard)")]
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

    [Header("Gamepad Assignment")]
    [Tooltip("Which gamepad index Player 1 uses (0 = first controller plugged in)")]
    public int p1GamepadIndex = 0;
    
    [Tooltip("Which gamepad index Player 2 uses")]
    public int p2GamepadIndex = 1;
    
    private AnimationBridge p1AnimBridge;
    private AnimationBridge p2AnimBridge;
    
    private MovementController p1Movement;
    private MovementController p2Movement;
    
    void Start()
    {
        InitializePlayers();
    }
    
    public void InitializePlayers()
    {
        if (player1Controller != null)
        {
            p1AnimBridge = player1Controller.GetComponent<AnimationBridge>();
            p1Movement = player1Controller.GetComponent<MovementController>();
            
            if (p1AnimBridge == null)
                Debug.LogWarning("Player 1 missing AnimationBridge component!");
            else
                Debug.Log("✓ Player 1 input initialized");
        }
        else
        {
            Debug.LogWarning("Player 1 Controller not assigned to TwoPlayerInputHandler!");
        }
        
        if (player2Controller != null)
        {
            p2AnimBridge = player2Controller.GetComponent<AnimationBridge>();
            p2Movement = player2Controller.GetComponent<MovementController>();
            
            if (p2AnimBridge == null)
                Debug.LogWarning("Player 2 missing AnimationBridge component!");
            else
                Debug.Log("✓ Player 2 input initialized");
        }
        else
        {
            Debug.LogWarning("Player 2 Controller not assigned to TwoPlayerInputHandler!");
        }
    }
    
    void Update()
    {
        HandlePlayer1Input();
        HandlePlayer2Input();
    }
    
    void HandlePlayer1Input()
    {
        if (player1Controller == null || p1AnimBridge == null) return;
        
        FighterDef fighter = player1Controller.FighterDef;
        if (fighter == null) return;

        Gamepad pad = Gamepad.all.Count > p1GamepadIndex ? Gamepad.all[p1GamepadIndex] : null;

        // ---- Movement logic ----
        if (p1Movement != null)
        {
            Vector2 moveInput = Vector2.zero;
            
            // Keyboard Movement
            if (Input.GetKey(p1Left)) moveInput.x -= 1;
            if (Input.GetKey(p1Right)) moveInput.x += 1;
            if (Input.GetKey(p1Up)) moveInput.y += 1;
            if (Input.GetKey(p1Down)) moveInput.y -= 1;

            // Gamepad Movement
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.05f) moveInput += stick;
                
                Vector2 dpad = pad.dpad.ReadValue();
                if (dpad.sqrMagnitude > 0.05f) moveInput += dpad;
            }

            // Clamp so we don't go too fast diagonally
            moveInput.x = Mathf.Clamp(moveInput.x, -1f, 1f);
            moveInput.y = Mathf.Clamp(moveInput.y, -1f, 1f);
            
            p1Movement.SetMoveInput(moveInput);

            bool jumpPressed = Input.GetKeyDown(p1Up) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (jumpPressed) p1Movement.Jump();
            
            bool dodgePressed = Input.GetKeyDown(p1Dodge) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            if (dodgePressed) p1Movement.Dash(moveInput);
        }
        
        // ---- Attack & Special Logic ----
        bool lightPressed = Input.GetKeyDown(p1LightAttack) || (pad != null && pad.buttonWest.wasPressedThisFrame);
        if (lightPressed && fighter.lightAttack != null)
        {
            AnimatedMoveDef animMove = fighter.lightAttack as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
                p1AnimBridge.PlayMove(animMove);
        }
        
        bool heavyPressed = Input.GetKeyDown(p1HeavyAttack) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        if (heavyPressed && fighter.heavyAttack != null)
        {
            AnimatedMoveDef animMove = fighter.heavyAttack as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
                p1AnimBridge.PlayMove(animMove);
        }
        
        bool special1Pressed = Input.GetKeyDown(p1Special1) || (pad != null && pad.leftShoulder.wasPressedThisFrame);
        if (special1Pressed && fighter.specialMoves != null && fighter.specialMoves.Length > 0)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[0] as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
                p1AnimBridge.PlayMove(animMove);
        }
        
        bool special2Pressed = Input.GetKeyDown(p1Special2) || (pad != null && pad.rightShoulder.wasPressedThisFrame);
        if (special2Pressed && fighter.specialMoves != null && fighter.specialMoves.Length > 1)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[1] as AnimatedMoveDef;
            if (animMove != null && p1AnimBridge.CanPlayMove())
                p1AnimBridge.PlayMove(animMove);
        }
    }
    
    void HandlePlayer2Input()
    {
        if (player2Controller == null || p2AnimBridge == null) return;
        
        FighterDef fighter = player2Controller.FighterDef;
        if (fighter == null) return;

        Gamepad pad = Gamepad.all.Count > p2GamepadIndex ? Gamepad.all[p2GamepadIndex] : null;

        // ---- Movement logic ----
        if (p2Movement != null)
        {
            Vector2 moveInput = Vector2.zero;
            
            // Keyboard Movement
            if (Input.GetKey(p2Left)) moveInput.x -= 1;
            if (Input.GetKey(p2Right)) moveInput.x += 1;
            if (Input.GetKey(p2Up)) moveInput.y += 1;
            if (Input.GetKey(p2Down)) moveInput.y -= 1;

            // Gamepad Movement
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.05f) moveInput += stick;
                
                Vector2 dpad = pad.dpad.ReadValue();
                if (dpad.sqrMagnitude > 0.05f) moveInput += dpad;
            }

            // Clamp so we don't go too fast diagonally
            moveInput.x = Mathf.Clamp(moveInput.x, -1f, 1f);
            moveInput.y = Mathf.Clamp(moveInput.y, -1f, 1f);
            
            p2Movement.SetMoveInput(moveInput);

            bool jumpPressed = Input.GetKeyDown(p2Up) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (jumpPressed) p2Movement.Jump();
            
            bool dodgePressed = Input.GetKeyDown(p2Dodge) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            if (dodgePressed) p2Movement.Dash(moveInput);
        }
        
        // ---- Attack & Special Logic ----
        bool lightPressed = Input.GetKeyDown(p2LightAttack) || (pad != null && pad.buttonWest.wasPressedThisFrame);
        if (lightPressed && fighter.lightAttack != null)
        {
            AnimatedMoveDef animMove = fighter.lightAttack as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
                p2AnimBridge.PlayMove(animMove);
        }
        
        bool heavyPressed = Input.GetKeyDown(p2HeavyAttack) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        if (heavyPressed && fighter.heavyAttack != null)
        {
            AnimatedMoveDef animMove = fighter.heavyAttack as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
                p2AnimBridge.PlayMove(animMove);
        }
        
        bool special1Pressed = Input.GetKeyDown(p2Special1) || (pad != null && pad.leftShoulder.wasPressedThisFrame);
        if (special1Pressed && fighter.specialMoves != null && fighter.specialMoves.Length > 0)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[0] as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
                p2AnimBridge.PlayMove(animMove);
        }
        
        bool special2Pressed = Input.GetKeyDown(p2Special2) || (pad != null && pad.rightShoulder.wasPressedThisFrame);
        if (special2Pressed && fighter.specialMoves != null && fighter.specialMoves.Length > 1)
        {
            AnimatedMoveDef animMove = fighter.specialMoves[1] as AnimatedMoveDef;
            if (animMove != null && p2AnimBridge.CanPlayMove())
                p2AnimBridge.PlayMove(animMove);
        }
    }
}

