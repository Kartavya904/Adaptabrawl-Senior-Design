using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Adaptabrawl.AI;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;
using Adaptabrawl.UI;

namespace Adaptabrawl.Gameplay
{
    public class LocalGameManager : MonoBehaviour
    {
        [Header("Match Settings")]
        [Tooltip("Rounds a player must win to win the match. Forwarded to GameManager.")]
        [SerializeField] private int roundsToWin = 2;
        [Tooltip("Duration of each round in seconds. Forwarded to GameManager.")]
        [SerializeField] private float roundDuration = 120f;
        [Tooltip("Seconds between round end and the next round starting.")]
        [SerializeField] private float roundEndDelay = 3f;
        [Tooltip("Seconds fighters are frozen at the start of each round before they can move or fight.")]
        [SerializeField] private float preRoundBuffer = 3f;

        [Header("Fighter Spawn")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;
        [SerializeField] private GameObject fighterPrefab;
        [Tooltip("Scale applied to spawned fighters. 1 = prefab scale.")]
        [SerializeField] private float spawnScale = 0.7f;

        [Header("Test Override (optional)")]
        [Tooltip("Fallback fighter for P1 used ONLY when no character was selected (e.g. opening Game Scene directly). CharacterSelectData always takes priority when set.")]
        [SerializeField] private FighterDef testFighterP1;
        [Tooltip("Fallback fighter for P2 used ONLY when no character was selected.")]
        [SerializeField] private FighterDef testFighterP2;

        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private FighterController player1;
        private FighterController player2;
        private ClassificationSwitcher classificationSwitcher;
        private GameSceneFighterCoordinator sceneCoordinator;
        private Adaptabrawl.UI.MatchResults matchResults;
        private QuickMatchMatchConfig activeQuickMatchConfig;

        /// <summary>Fired once both fighters are spawned. Subscribe in Start() or later.</summary>
        public System.Action<FighterController, FighterController> OnFightersSpawned;

        public FighterController Player1 => player1;
        public FighterController Player2 => player2;

        private void Start()
        {
            InitializeMatchSession();
        }

        private void InitializeMatchSession()
        {
            QuickMatchMatchConfig quickMatchConfig = null;
            if (QuickMatchSession.Instance != null)
                QuickMatchSession.Instance.TryGetCurrentConfig(out quickMatchConfig);
            activeQuickMatchConfig = quickMatchConfig;

            // CharacterSelectData takes priority (set by the character select screen).
            // Test overrides are only used when CharacterSelectData is null (direct scene testing).
            FighterDef fighter1Def = quickMatchConfig != null && quickMatchConfig.player1Fighter != null
                ? quickMatchConfig.player1Fighter
                : CharacterSelectData.selectedFighter1 != null
                ? CharacterSelectData.selectedFighter1
                : testFighterP1;

            FighterDef fighter2Def = quickMatchConfig != null && quickMatchConfig.player2Fighter != null
                ? quickMatchConfig.player2Fighter
                : CharacterSelectData.selectedFighter2 != null
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

            // Ensure fighters on Layer 3 actually physically push each other!
            Physics2D.IgnoreLayerCollision(3, 3, false);

            // Spawn fighters first so GameManager.InitializeMatch() can find them.
            SpawnFighters(fighter1Def, fighter2Def);
            ConfigureInputSources(quickMatchConfig);
            SubscribeToFighterRebindEvents();

            GameContext.EnsureExists().BeginLocalMatch(player1, player2);

            // Bootstrap GameManager — try the serialized reference, then the same GameObject,
            // then the scene, and finally add one if nothing exists.
            if (gameManager == null)
                gameManager = GetComponent<GameManager>();
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                gameManager = gameObject.AddComponent<GameManager>();
                Debug.LogWarning("[LocalGameManager] No GameManager found — added one automatically. " +
                    "Consider adding a GameManager component to the scene's GameManager GameObject.");
            }

            // Push LocalGameManager's Inspector settings to GameManager so the designer
            // only needs to adjust one place (this component).
            gameManager.Configure(roundsToWin, roundDuration, roundEndDelay, preRoundBuffer);

            // Subscribe to events, then call InitializeMatch() explicitly so the GM
            // picks up the fighters we just spawned (avoids the 1-frame-delay race).
            gameManager.OnMatchEnd += OnMatchEnd;
            gameManager.OnRoundEnd += OnRoundEnd;
            gameManager.InitializeMatch();

            // Online matches previously used a separate scene without the local-only random swap flow.
            // Keep that behaviour by leaving classification switching disabled for networked online fights.
            if (!IsOnlineNetworkMatch())
            {
                classificationSwitcher = GetComponent<ClassificationSwitcher>();
                if (classificationSwitcher == null)
                    classificationSwitcher = gameObject.AddComponent<ClassificationSwitcher>();
                classificationSwitcher.Initialize(new FighterController[] { player1, player2 });
                // Switcher starts paused — GameManager will resume it after the pre-round buffer
            }
            else
            {
                classificationSwitcher = GetComponent<ClassificationSwitcher>();
                if (classificationSwitcher != null)
                    classificationSwitcher.enabled = false;
            }

            sceneCoordinator = GetComponent<GameSceneFighterCoordinator>();
            if (sceneCoordinator == null)
                sceneCoordinator = gameObject.AddComponent<GameSceneFighterCoordinator>();
            sceneCoordinator.Initialize(player1, player2);

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
                GameObject p1Obj = CreateFighter(fighter1Def, player1SpawnPoint.position, true);
                player1 = p1Obj.GetComponent<FighterController>();
                player1?.SetPlayerNumber(1);
                ApplySpawnSetup(p1Obj);
                player1?.SetSpawnPosition(player1SpawnPoint.position);
            }

            if (player2SpawnPoint != null && fighter2Def != null)
            {
                GameObject p2Obj = CreateFighter(fighter2Def, player2SpawnPoint.position, false);
                player2 = p2Obj.GetComponent<FighterController>();
                player2?.SetPlayerNumber(2);
                ApplySpawnSetup(p2Obj);
                player2?.SetSpawnPosition(player2SpawnPoint.position);
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

        private GameObject CreateFighter(FighterDef fighterDef, Vector3 position, bool facingRight)
        {
            // Use FighterFactory to create fighter
            FighterController controller = FighterFactory.CreateFighter(fighterDef, position, facingRight);
            return controller != null ? controller.gameObject : null;
        }

        private void ConfigureInputSources(QuickMatchMatchConfig quickMatchConfig)
        {
            ConfigureInputSourceForPlayer(player1, player2, 1, quickMatchConfig);
            ConfigureInputSourceForPlayer(player2, player1, 2, quickMatchConfig);
        }

        private void SubscribeToFighterRebindEvents()
        {
            if (player1 != null)
                player1.OnFighterDefinitionChanged += OnPlayerFighterDefinitionChanged;

            if (player2 != null)
                player2.OnFighterDefinitionChanged += OnPlayerFighterDefinitionChanged;
        }

        private void UnsubscribeFromFighterRebindEvents()
        {
            if (player1 != null)
                player1.OnFighterDefinitionChanged -= OnPlayerFighterDefinitionChanged;

            if (player2 != null)
                player2.OnFighterDefinitionChanged -= OnPlayerFighterDefinitionChanged;
        }

        private void OnPlayerFighterDefinitionChanged(FighterController changedFighter, FighterDef _)
        {
            if (changedFighter == null)
                return;

            if (changedFighter == player1)
            {
                ConfigureInputSourceForPlayer(player1, player2, 1, activeQuickMatchConfig);
                return;
            }

            if (changedFighter == player2)
                ConfigureInputSourceForPlayer(player2, player1, 2, activeQuickMatchConfig);
        }

        private void ConfigureInputSourceForPlayer(FighterController fighter, FighterController opponent, int playerNumber, QuickMatchMatchConfig quickMatchConfig)
        {
            if (fighter == null)
                return;

            var pcp = fighter.GetPlayerController();
            if (pcp == null)
            {
                Debug.LogWarning($"[LocalGameManager] No PlayerController_Platform found under P{playerNumber} — input not configured.");
                return;
            }

            bool cpuControlled = quickMatchConfig != null
                && quickMatchConfig.GetRoleForPlayer(playerNumber) == QuickMatchPlayerRole.Cpu;
            int padIndex = cpuControlled ? -1 : PickGamepadForPlayer(playerNumber);
            pcp.ConfigureForPlayer(playerNumber, padIndex);

            if (!cpuControlled)
            {
                ResetInjectedInput(pcp);
                return;
            }

            QuickMatchHeuristicModel model = null;
            if (quickMatchConfig != null)
                QuickMatchModelStore.TryLoadChampion(quickMatchConfig.difficulty, out model, out _);

            var cpuController = fighter.GetComponent<QuickMatchCpuController>();
            if (cpuController == null)
                cpuController = fighter.gameObject.AddComponent<QuickMatchCpuController>();

            cpuController.Initialize(
                fighter,
                opponent,
                quickMatchConfig != null ? quickMatchConfig.difficulty : QuickMatchDifficultyTier.Trainer,
                model);
        }

        /// <summary>
        /// Uses lobby / character-select device choice: keyboard vs controller, and maps
        /// the first and second controller users to <see cref="Gamepad.all"/> slots.
        /// </summary>
        private static int PickGamepadForPlayer(int playerNumber)
        {
            int p1Dev = LobbyContext.Instance != null
                ? LobbyContext.Instance.p1InputDevice
                : CharacterSelectData.finalP1ControllerIndex;
            int p2Dev = LobbyContext.Instance != null
                ? LobbyContext.Instance.p2InputDevice
                : CharacterSelectData.finalP2ControllerIndex;

            int idx = LobbyContext.GetGamepadListIndexForPlayer(playerNumber, p1Dev, p2Dev);
            if (idx < 0) return -1;

            if (UnityEngine.InputSystem.Gamepad.all.Count <= idx)
                return -1;

            return idx;
        }

        private static void ResetInjectedInput(PlayerController_Platform platformController)
        {
            if (platformController == null)
                return;

            platformController.isNetworkControlled = false;
            platformController.netLeft = false;
            platformController.netRight = false;
            platformController.netCrouch = false;
            platformController.netSprint = false;
            platformController.netJump = false;
            platformController.netAttack = false;
            platformController.netBlock = false;
            platformController.netBlockDown = false;
            platformController.netBlockUp = false;
            platformController.netDodge = false;

            if (platformController.netSkills == null || platformController.netSkills.Length < 8)
                platformController.netSkills = new bool[8];

            for (int i = 0; i < platformController.netSkills.Length; i++)
                platformController.netSkills[i] = false;
        }

        private void OnRoundEnd(FighterController winner)
        {
            GameContext.Instance?.RecordRoundEnd(winner);

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
            if (gameManager == null || player1 == null || player2 == null) return;
            var rw = gameManager.RoundWins;
            int p1w = rw.ContainsKey(player1) ? rw[player1] : 0;
            int p2w = rw.ContainsKey(player2) ? rw[player2] : 0;
            GameContext.Instance?.FinalizeMatch(winner, p1w, p2w, gameManager.CurrentRound);
        }

        private void OnDestroy()
        {
            UnsubscribeFromFighterRebindEvents();

            if (gameManager != null)
            {
                gameManager.OnRoundEnd -= OnRoundEnd;
                gameManager.OnMatchEnd -= OnMatchEnd;
            }
        }

        public void RestartMatch()
        {
            // Reset match
            if (gameManager != null)
                gameManager.Rematch();
        }

        private static bool IsOnlineNetworkMatch()
        {
            return NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsListening
                && !LobbyContext.CurrentMatchIsLocal();
        }
    }
}
