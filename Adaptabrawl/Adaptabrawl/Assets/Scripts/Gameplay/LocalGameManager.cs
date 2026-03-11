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
        [Tooltip("Scale applied to spawned fighters. Use 0.5 for smaller characters to match typical 2D arena size. 1 = prefab scale.")]
        [SerializeField] private float spawnScale = 0.5f;

        [Header("Test Override (optional)")]
        [Tooltip("Fallback fighter for P1 used ONLY when no character was selected (e.g. opening Game Scene directly). CharacterSelectData always takes priority when set.")]
        [SerializeField] private FighterDef testFighterP1;
        [Tooltip("Fallback fighter for P2 used ONLY when no character was selected.")]
        [SerializeField] private FighterDef testFighterP2;

        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private FighterController player1;
        private FighterController player2;
        private Adaptabrawl.UI.MatchResults matchResults;

        /// <summary>Fired once both fighters are spawned. Subscribe in Start() or later.</summary>
        public System.Action<FighterController, FighterController> OnFightersSpawned;

        public FighterController Player1 => player1;
        public FighterController Player2 => player2;

        private void Start()
        {
            InitializeLocalMatch();
        }

        private void InitializeLocalMatch()
        {
            // CharacterSelectData takes priority (set by the character select screen).
            // Test overrides are only used when CharacterSelectData is null (direct scene testing).
            FighterDef fighter1Def = CharacterSelectData.selectedFighter1 != null
                ? CharacterSelectData.selectedFighter1
                : testFighterP1;

            FighterDef fighter2Def = CharacterSelectData.selectedFighter2 != null
                ? CharacterSelectData.selectedFighter2
                : testFighterP2;

            if (fighter1Def == null || fighter2Def == null)
            {
                if (fighter1Def == null) fighter1Def = FighterFactory.CreateStrikerFighter();
                if (fighter2Def == null) fighter2Def = FighterFactory.CreateElusiveFighter();
                Debug.LogWarning("[LocalGameManager] No fighter selected — using Striker/Elusive defaults. " +
                    "Assign FighterDef assets to 'Test Fighter P1/P2' on this component for direct scene testing, " +
                    "or go through the character select screen.");
            }
            else
            {
                Debug.Log($"[LocalGameManager] Spawning P1='{fighter1Def.fighterName}' P2='{fighter2Def.fighterName}' " +
                    $"(source: {(CharacterSelectData.selectedFighter1 != null ? "CharacterSelectData" : "Test Override")})");
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

            // Notify HUD that fighters are ready
            OnFightersSpawned?.Invoke(player1, player2);

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
            if (player1SpawnPoint != null && fighter1Def != null)
            {
                GameObject p1Obj = CreateFighter(fighter1Def, player1SpawnPoint.position);
                player1 = p1Obj.GetComponent<FighterController>();
                ApplySpawnSetup(p1Obj);
                SetupPlayerInput(p1Obj, 1);
            }

            if (player2SpawnPoint != null && fighter2Def != null)
            {
                GameObject p2Obj = CreateFighter(fighter2Def, player2SpawnPoint.position);
                player2 = p2Obj.GetComponent<FighterController>();
                ApplySpawnSetup(p2Obj);
                SetupPlayerInput(p2Obj, 2);
            }
        }

        /// <summary>
        /// Applies spawn scale and ensures the fighter stays on the ground like the scene's Player_Hammer
        /// (same layer for collision, scale so character isn't too big).
        /// </summary>
        private void ApplySpawnSetup(GameObject fighterObj)
        {
            if (fighterObj == null) return;
            fighterObj.transform.localScale = Vector3.one * Mathf.Max(0.01f, spawnScale);
            // Use same layer as scene's Player_Hammer (layer 3) so collision with default ground works
            fighterObj.layer = 3;
            foreach (Transform child in fighterObj.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 3;
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
            // The GameManager will handle creating the MatchResults object and transitioning to the scene.
            // LocalGameManager merely sets up local overrides.
        }

        public void RestartMatch()
        {
            // Reset match
            if (gameManager != null)
                gameManager.Rematch();
        }
    }
}

