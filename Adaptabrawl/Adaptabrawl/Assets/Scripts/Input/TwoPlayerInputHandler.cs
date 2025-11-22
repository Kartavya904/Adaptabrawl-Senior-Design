using UnityEngine;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Data;

/// <summary>
/// Handles input for both players in a local 2-player match.
/// Player 1: WASD + F/G/R/T keys
/// Player 2: Arrow Keys + NumPad
/// </summary>
public class TwoPlayerInputHandler : MonoBehaviour
{
    [Header("Player References")]
    [Tooltip("Reference to the fighter spawner")]
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
        // Wait a frame for fighters to spawn
        yield return new WaitForSeconds(0.1f);
        
        if (spawner != null)
        {
            if (spawner.Player1 != null)
            {
                p1AnimBridge = spawner.Player1.GetComponent<AnimationBridge>();
                if (p1AnimBridge == null)
                    Debug.LogWarning("Player 1 missing AnimationBridge component!");
                else
                    Debug.Log("✓ Player 1 input initialized");
            }
            
            if (spawner.Player2 != null)
            {
                p2AnimBridge = spawner.Player2.GetComponent<AnimationBridge>();
                if (p2AnimBridge == null)
                    Debug.LogWarning("Player 2 missing AnimationBridge component!");
                else
                    Debug.Log("✓ Player 2 input initialized");
            }
        }
        else
        {
            Debug.LogError("FighterSpawner not assigned to TwoPlayerInputHandler!");
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
        if (fighter == null) return;
        
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
        
        // TODO: Implement movement and defensive moves
    }
    
    void HandlePlayer2Input()
    {
        if (spawner?.Player2 == null || p2AnimBridge == null) return;
        
        FighterDef fighter = spawner.Player2.FighterDef;
        if (fighter == null) return;
        
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
        
        // TODO: Implement movement and defensive moves
    }
}

