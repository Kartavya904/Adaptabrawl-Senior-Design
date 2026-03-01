using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using Adaptabrawl.UI;

namespace Adaptabrawl.Gameplay
{
    public class LocalGameManager : MonoBehaviour
    {
        [Header("Match Settings")]
        #pragma warning disable CS0414 // Field is assigned but never used (configured in GameManager)
        [SerializeField] private int roundsToWin = 2;
        [SerializeField] private float roundDuration = 99f;
        #pragma warning restore CS0414
        [SerializeField] private float roundEndDelay = 3f;
        
        [Header("Fighter Spawn")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;
        [SerializeField] private GameObject fighterPrefab;
        
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        
        private FighterController player1;
        private FighterController player2;
        private Adaptabrawl.UI.MatchResults matchResults;
        
        private void Start()
        {
            InitializeLocalMatch();
        }
        
        private void InitializeLocalMatch()
        {
            // Get selected fighters from character select
            FighterDef fighter1Def = CharacterSelectData.selectedFighter1;
            FighterDef fighter2Def = CharacterSelectData.selectedFighter2;
            
            // If no fighters selected, create default fighters
            if (fighter1Def == null || fighter2Def == null)
            {
                Debug.LogWarning("No fighters selected, using defaults");
                // Create default fighters using FighterFactory
                fighter1Def = FighterFactory.CreateStrikerFighter();
                fighter2Def = FighterFactory.CreateElusiveFighter();
            }
            
            // Spawn fighters
            SpawnFighters(fighter1Def, fighter2Def);
            
            // Initialize game manager
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            
            if (gameManager != null)
            {
                // Configure game manager with match settings
                // Note: GameManager has its own serialized fields, but we can override if needed
                // For now, GameManager uses its own settings, but these fields are available for future use
                
                // Subscribe to game manager events
                gameManager.OnMatchEnd += OnMatchEnd;
                gameManager.OnRoundEnd += OnRoundEnd;
            }
            
            // Initialize match results
            matchResults = new Adaptabrawl.UI.MatchResults
            {
                player1 = player1,
                player2 = player2,
                player1Wins = 0,
                player2Wins = 0,
                roundWinners = new System.Collections.Generic.List<FighterController>(),
                totalRounds = 0
            };
        }
        
        private void SpawnFighters(FighterDef fighter1Def, FighterDef fighter2Def)
        {
            // Spawn Player 1
            if (player1SpawnPoint != null && fighter1Def != null)
            {
                GameObject p1Obj = CreateFighter(fighter1Def, player1SpawnPoint.position);
                player1 = p1Obj.GetComponent<FighterController>();
                
                // Setup player 1 input (typically Player 1 uses keyboard/gamepad 1)
                SetupPlayerInput(p1Obj, 1);
            }
            
            // Spawn Player 2
            if (player2SpawnPoint != null && fighter2Def != null)
            {
                GameObject p2Obj = CreateFighter(fighter2Def, player2SpawnPoint.position);
                player2 = p2Obj.GetComponent<FighterController>();
                
                // Setup player 2 input (typically Player 2 uses gamepad 2 or different keys)
                SetupPlayerInput(p2Obj, 2);
            }
        }
        
        private GameObject CreateFighter(FighterDef fighterDef, Vector3 position)
        {
            // Use FighterFactory to create fighter
            FighterController controller = FighterFactory.CreateFighter(fighterDef, position);
            return controller != null ? controller.gameObject : null;
        }
        
        private void SetupPlayerInput(GameObject fighterObj, int playerNumber)
        {
            // Setup input handler for the player
            var inputHandler = fighterObj.GetComponent<Input.PlayerInputHandler>();
            if (inputHandler == null)
                inputHandler = fighterObj.AddComponent<Input.PlayerInputHandler>();
            
            // Configure input for player number
            // This would require a method on PlayerInputHandler to set player index
            // inputHandler.SetPlayerIndex(playerNumber);
        }
        
        private void OnRoundEnd(FighterController winner)
        {
            if (matchResults == null) return;
            
            matchResults.totalRounds++;
            matchResults.roundWinners.Add(winner);
            
            if (winner == player1)
                matchResults.player1Wins++;
            else if (winner == player2)
                matchResults.player2Wins++;
        }
        
        private void OnMatchEnd(FighterController winner)
        {
            if (matchResults == null) return;
            
            matchResults.winner = winner;
            
            // Store results and transition to results scene
            Adaptabrawl.UI.MatchResultsData.SetResults(matchResults, true);
            
            // Transition to results scene after delay
            StartCoroutine(TransitionToResultsAfterDelay());
        }
        
        private System.Collections.IEnumerator TransitionToResultsAfterDelay()
        {
            yield return new WaitForSeconds(roundEndDelay);
            SceneManager.LoadScene("MatchResults");
        }
        
        public void RestartMatch()
        {
            // Reset match
            if (gameManager != null)
                gameManager.Rematch();
        }
    }
}

