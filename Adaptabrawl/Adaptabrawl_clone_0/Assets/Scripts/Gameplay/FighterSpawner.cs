using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using Adaptabrawl.Gameplay;

/// <summary>
/// Spawns both fighters at their designated spawn points.
/// Automatically called at game start.
/// </summary>
public class FighterSpawner : MonoBehaviour
{
    [Header("Fighter Configurations")]
    [Tooltip("Fighter definition for Player 1")]
    public FighterDef player1Fighter;
    
    [Tooltip("Fighter definition for Player 2")]
    public FighterDef player2Fighter;
    
    [Header("Spawn Positions")]
    [Tooltip("Where Player 1 spawns")]
    public Transform player1SpawnPoint;
    
    [Tooltip("Where Player 2 spawns")]
    public Transform player2SpawnPoint;
    
    [Header("Player Tags")]
    public string player1Tag = "Player1";
    public string player2Tag = "Player2";
    
    [Header("Runtime References")]
    [SerializeField] private FighterController player1Controller;
    [SerializeField] private FighterController player2Controller;
    
    /// <summary>
    /// Gets the Player 1 FighterController instance
    /// </summary>
    public FighterController Player1 => player1Controller;
    
    /// <summary>
    /// Gets the Player 2 FighterController instance
    /// </summary>
    public FighterController Player2 => player2Controller;
    
    void Start()
    {
        SpawnFighters();
    }
    
    /// <summary>
    /// Spawns both fighters at their spawn points
    /// </summary>
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
            
            if (player1Controller != null)
            {
                player1Controller.gameObject.tag = player1Tag;
                player1Controller.gameObject.name = "Player1_" + player1Fighter.fighterName;
                
                Debug.Log($"✓ Spawned Player 1: {player1Fighter.fighterName} at {player1SpawnPoint.position}");
            }
            else
            {
                Debug.LogError("Failed to spawn Player 1!");
            }
        }
        else
        {
            Debug.LogWarning("Player 1 fighter or spawn point not assigned!");
        }
        
        // Spawn Player 2
        if (player2Fighter != null && player2SpawnPoint != null)
        {
            player2Controller = FighterFactory.CreateFighter(
                player2Fighter, 
                player2SpawnPoint.position, 
                facingRight: false
            );
            
            if (player2Controller != null)
            {
                player2Controller.gameObject.tag = player2Tag;
                player2Controller.gameObject.name = "Player2_" + player2Fighter.fighterName;
                
                Debug.Log($"✓ Spawned Player 2: {player2Fighter.fighterName} at {player2SpawnPoint.position}");
            }
            else
            {
                Debug.LogError("Failed to spawn Player 2!");
            }
        }
        else
        {
            Debug.LogWarning("Player 2 fighter or spawn point not assigned!");
        }
        
        // Verify both spawned successfully
        if (player1Controller != null && player2Controller != null)
        {
            Debug.Log("✓ Both fighters spawned successfully! Match ready to start.");
        }
    }
    
    /// <summary>
    /// Respawns both fighters (useful for rematches)
    /// </summary>
    public void RespawnFighters()
    {
        // Destroy existing fighters
        if (player1Controller != null)
        {
            Destroy(player1Controller.gameObject);
            player1Controller = null;
        }
        
        if (player2Controller != null)
        {
            Destroy(player2Controller.gameObject);
            player2Controller = null;
        }
        
        // Spawn new fighters
        SpawnFighters();
    }
}

