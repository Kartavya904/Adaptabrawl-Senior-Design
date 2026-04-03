using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Adaptabrawl.Gameplay;
using System.Collections.Generic;

namespace Adaptabrawl.UI
{
    public class MatchResultsUI : MonoBehaviour
    {
        [Header("Results Display")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI matchScoreText;
        [SerializeField] private TextMeshProUGUI roundResultsText;
        [Tooltip("Optional. Shows arena from GameContext when assigned.")]
        [SerializeField] private TextMeshProUGUI arenaNameText;

        [Header("Player 1 Results")]
        [SerializeField] private TextMeshProUGUI player1NameText;
        [SerializeField] private TextMeshProUGUI player1WinsText;
        [SerializeField] private Image player1Portrait;

        [Header("Player 2 Results")]
        [SerializeField] private TextMeshProUGUI player2NameText;
        [SerializeField] private TextMeshProUGUI player2WinsText;
        [SerializeField] private Image player2Portrait;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button rematchDifferentButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Button Labels")]
        [SerializeField] private TextMeshProUGUI continueButtonText;
        [SerializeField] private TextMeshProUGUI rematchButtonText;
        [SerializeField] private TextMeshProUGUI rematchDifferentButtonText;

        [Header("Animation")]
        [SerializeField] private float resultsDelay = 2f;
        [SerializeField] private Animator resultsAnimator;

        private void Start()
        {
            // Setup button listeners
            if (continueButton != null)
                continueButton.onClick.AddListener(Continue);

            if (rematchButton != null)
                rematchButton.onClick.AddListener(Rematch);

            if (rematchDifferentButton != null)
                rematchDifferentButton.onClick.AddListener(RematchDifferentCharacters);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            // Hide results initially
            if (resultsPanel != null)
                resultsPanel.SetActive(false);

            // Load match results data
            LoadMatchResults();
        }

        private void LoadMatchResults()
        {
            bool fromStatic = MatchResultsData.hasResults;
            bool fromContext = GameContext.Instance != null && GameContext.Instance.TryGetLatestFinishedMatch(out _);
            if (fromStatic || fromContext)
                StartCoroutine(ShowResultsAfterDelay());
            else
            {
                Debug.LogWarning("[MatchResultsUI] No MatchResultsData and no GameContext match history.");
                ReturnToMainMenu();
            }
        }

        private System.Collections.IEnumerator ShowResultsAfterDelay()
        {
            yield return new WaitForSeconds(resultsDelay);

            if (resultsPanel != null)
                resultsPanel.SetActive(true);

            DisplayResults();

            // Play animation if available
            if (resultsAnimator != null)
                resultsAnimator.SetTrigger("ShowResults");
        }

        private void DisplayResults()
        {
            if (GameContext.Instance != null && GameContext.Instance.TryGetLatestFinishedMatch(out var rec))
            {
                DisplayResultsFromGameContext(rec);
                ApplyButtonLabels(rec.localMatch);
                return;
            }

            if (!MatchResultsData.hasResults || MatchResultsData.results == null)
            {
                Debug.LogWarning("[MatchResultsUI] No GameContext history and no MatchResultsData.");
                return;
            }

            var results = MatchResultsData.results;

            // Display winner
            if (winnerText != null)
            {
                if (results.winner != null)
                {
                    winnerText.text = $"{GetFighterName(results.winner)} WINS!";
                    winnerText.color = results.winner == results.player1 ? Color.cyan : Color.yellow;
                }
                else
                {
                    winnerText.text = "DRAW!";
                    winnerText.color = Color.white;
                }
            }

            // Display match score
            if (matchScoreText != null)
            {
                matchScoreText.text = $"{results.player1Wins} - {results.player2Wins}";
            }

            if (arenaNameText != null)
                arenaNameText.gameObject.SetActive(false);

            // Display player 1 info
            if (player1NameText != null)
                player1NameText.text = GetFighterName(results.player1);
            if (player1WinsText != null)
                player1WinsText.text = $"Wins: {results.player1Wins}";

            // Display player 2 info
            if (player2NameText != null)
                player2NameText.text = GetFighterName(results.player2);
            if (player2WinsText != null)
                player2WinsText.text = $"Wins: {results.player2Wins}";

            // Display round-by-round results
            if (roundResultsText != null)
            {
                string roundText = "Round Results:\n";
                for (int i = 0; i < results.roundWinners.Count; i++)
                {
                    var roundWinner = results.roundWinners[i];
                    string winnerName = roundWinner != null ? GetFighterName(roundWinner) : "Draw";
                    roundText += $"Round {i + 1}: {winnerName}\n";
                }
                roundResultsText.text = roundText;
            }

            ApplyButtonLabels(MatchResultsData.isLocalMatch);
        }

        private void DisplayResultsFromGameContext(FinishedMatchRecord rec)
        {
            if (rec == null) return;

            if (arenaNameText != null)
            {
                bool hasArena = !string.IsNullOrWhiteSpace(rec.arenaName);
                arenaNameText.gameObject.SetActive(hasArena);
                if (hasArena)
                    arenaNameText.text = $"Arena: {rec.arenaName}";
            }

            if (winnerText != null)
            {
                if (rec.outcome == "Draw")
                {
                    winnerText.text = "DRAW!";
                    winnerText.color = Color.white;
                }
                else if (rec.outcome == "P1")
                {
                    winnerText.text = $"{rec.p1FighterName} WINS!";
                    winnerText.color = Color.cyan;
                }
                else if (rec.outcome == "P2")
                {
                    winnerText.text = $"{rec.p2FighterName} WINS!";
                    winnerText.color = Color.yellow;
                }
                else
                {
                    winnerText.text = "MATCH OVER";
                    winnerText.color = Color.white;
                }
            }

            if (matchScoreText != null)
                matchScoreText.text = $"{rec.p1FinalWins} - {rec.p2FinalWins}";

            if (player1NameText != null)
                player1NameText.text = $"{rec.p1DisplayName}\n{rec.p1FighterName}";
            if (player1WinsText != null)
                player1WinsText.text = $"Wins: {rec.p1FinalWins}";

            if (player2NameText != null)
                player2NameText.text = $"{rec.p2DisplayName}\n{rec.p2FighterName}";
            if (player2WinsText != null)
                player2WinsText.text = $"Wins: {rec.p2FinalWins}";

            if (roundResultsText != null)
            {
                var snap = rec.roundWinnerCodesSnapshot;
                string roundText = "Round Results:\n";
                if (snap != null && snap.Count > 0)
                {
                    for (int i = 0; i < snap.Count; i++)
                    {
                        int code = snap[i];
                        string winnerName = code == 1 ? rec.p1FighterName : code == 2 ? rec.p2FighterName : "Draw";
                        roundText += $"Round {i + 1}: {winnerName}\n";
                    }
                }
                else
                    roundText += "(No round log)\n";
                roundResultsText.text = roundText;
            }
        }

        private void ApplyButtonLabels(bool isLocal)
        {
            // Set button labels based on match type
            if (continueButtonText != null)
                continueButtonText.text = isLocal ? "Back to Setup" : "Back to Lobby";
            if (rematchButtonText != null)
                rematchButtonText.text = "Rematch";
            if (rematchDifferentButtonText != null)
                rematchDifferentButtonText.text = "Change Characters";
        }

        private string GetFighterName(FighterController fighter)
        {
            if (fighter == null) return "Unknown";
            if (fighter.FighterDef != null)
                return fighter.FighterDef.fighterName;
            return fighter.name;
        }

        // Back to lobby (online) or start of SetupScene (local) — network session stays alive
        private void Continue()
        {
            bool isLocal = MatchResultsData.isLocalMatch;
            MatchResultsData.Clear();
            TransitionTo(isLocal ? "SetupScene" : "LobbyScene");
        }

        // Same characters — skip all setup and reload game scene directly
        private void Rematch()
        {
            MatchResultsData.Clear();
            string gameScene = CharacterSelectData.isLocalMatch ? "GameScene" : "OnlineGameScene";
            TransitionTo(gameScene);
        }

        // Goes back to CharacterSelect in SetupScene, skipping controller config
        private void RematchDifferentCharacters()
        {
            MatchResultsData.rematchSkipToCharacterSelect = true;
            TransitionTo("SetupScene");
        }

        private void ReturnToMainMenu()
        {
            MatchResultsData.Clear();
            TransitionTo("StartScene");
        }

        private void TransitionTo(string sceneName)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }

    // Static class to pass match results between scenes
    public static class MatchResultsData
    {
        public static bool hasResults = false;
        public static bool rematchRequested = false;
        public static bool isLocalMatch = false;
        public static bool rematchSkipToCharacterSelect = false;
        public static MatchResults results;

        public static void SetResults(MatchResults matchResults, bool local)
        {
            results = matchResults;
            hasResults = true;
            isLocalMatch = local;
        }

        public static void Clear()
        {
            hasResults = false;
            rematchRequested = false;
            isLocalMatch = false;
            rematchSkipToCharacterSelect = false;
            results = null;
        }
    }

    // Data structure for match results
    [System.Serializable]
    public class MatchResults
    {
        public FighterController player1;
        public FighterController player2;
        public FighterController winner;
        public int player1Wins;
        public int player2Wins;
        public List<FighterController> roundWinners = new List<FighterController>();
        public int totalRounds;
    }
}

