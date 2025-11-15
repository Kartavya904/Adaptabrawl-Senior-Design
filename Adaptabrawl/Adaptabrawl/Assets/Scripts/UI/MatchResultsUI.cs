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
        
        [Header("Player 1 Results")]
        [SerializeField] private TextMeshProUGUI player1NameText;
        [SerializeField] private TextMeshProUGUI player1WinsText;
        [SerializeField] private Image player1Portrait;
        
        [Header("Player 2 Results")]
        [SerializeField] private TextMeshProUGUI player2NameText;
        [SerializeField] private TextMeshProUGUI player2WinsText;
        [SerializeField] private Image player2Portrait;
        
        [Header("Buttons")]
        [SerializeField] private Button rematchButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button characterSelectButton;
        
        [Header("Animation")]
        [SerializeField] private float resultsDelay = 2f;
        [SerializeField] private Animator resultsAnimator;
        
        private void Start()
        {
            // Setup button listeners
            if (rematchButton != null)
                rematchButton.onClick.AddListener(Rematch);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
            if (characterSelectButton != null)
                characterSelectButton.onClick.AddListener(ReturnToCharacterSelect);
            
            // Hide results initially
            if (resultsPanel != null)
                resultsPanel.SetActive(false);
            
            // Load match results data
            LoadMatchResults();
        }
        
        private void LoadMatchResults()
        {
            // Get results from MatchResultsData
            if (MatchResultsData.hasResults)
            {
                StartCoroutine(ShowResultsAfterDelay());
            }
            else
            {
                // No results data, return to menu
                Debug.LogWarning("No match results data found!");
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
        }
        
        private string GetFighterName(FighterController fighter)
        {
            if (fighter == null) return "Unknown";
            if (fighter.FighterDef != null)
                return fighter.FighterDef.fighterName;
            return fighter.name;
        }
        
        private void Rematch()
        {
            // Store rematch flag
            MatchResultsData.rematchRequested = true;
            
            // Return to character select or directly to game
            if (MatchResultsData.isLocalMatch)
            {
                SceneManager.LoadScene("CharacterSelect");
            }
            else
            {
                // For online, return to lobby
                SceneManager.LoadScene("LobbyScene");
            }
        }
        
        private void ReturnToMainMenu()
        {
            MatchResultsData.Clear();
            SceneManager.LoadScene("StartScene");
        }
        
        private void ReturnToCharacterSelect()
        {
            MatchResultsData.Clear();
            SceneManager.LoadScene("CharacterSelect");
        }
    }
    
    // Static class to pass match results between scenes
    public static class MatchResultsData
    {
        public static bool hasResults = false;
        public static bool rematchRequested = false;
        public static bool isLocalMatch = false;
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

