using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace Adaptabrawl.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int roundsToWin = 2;
        [SerializeField] private float roundDuration = 99f; // Seconds
        [SerializeField] private float roundEndDelay = 3f;
        
        [Header("Players")]
        [SerializeField] private List<FighterController> players = new List<FighterController>();
        
        [Header("Round State")]
        private int currentRound = 1;
        private Dictionary<FighterController, int> roundWins = new Dictionary<FighterController, int>();
        private List<FighterController> roundWinners = new List<FighterController>();
        private float roundTimer = 0f;
        private bool roundActive = false;
        private FighterController roundWinner = null;
        
        [Header("Events")]
        public System.Action<int> OnRoundStart; // round number
        public System.Action<FighterController> OnRoundEnd; // winner
        public System.Action<FighterController> OnMatchEnd; // winner
        public System.Action<float> OnRoundTimerUpdate; // remaining time
        
        private void Start()
        {
            InitializeMatch();
        }
        
        private void Update()
        {
            if (roundActive)
            {
                UpdateRoundTimer();
                CheckWinConditions();
            }
        }
        
        private void InitializeMatch()
        {
            // Find all fighters
            players.Clear();
            players.AddRange(FindObjectsByType<FighterController>(FindObjectsSortMode.None));
            
            // Initialize round wins
            foreach (var player in players)
            {
                roundWins[player] = 0;
                
                // Subscribe to death events
                player.OnDeath += () => OnPlayerDeath(player);
            }
            
            StartRound();
        }
        
        private void StartRound()
        {
            roundActive = true;
            roundTimer = roundDuration;
            roundWinner = null;
            
            // Reset fighters
            foreach (var player in players)
            {
                if (player != null)
                {
                    // Reset health, position, etc.
                    // This would require a Reset method on FighterController
                }
            }
            
            OnRoundStart?.Invoke(currentRound);
        }
        
        private void UpdateRoundTimer()
        {
            roundTimer -= Time.deltaTime;
            OnRoundTimerUpdate?.Invoke(roundTimer);
            
            if (roundTimer <= 0f)
            {
                // Time's up - determine winner by health
                EndRoundByTime();
            }
        }
        
        private void CheckWinConditions()
        {
            // Check if any player is dead
            foreach (var player in players)
            {
                if (player != null && player.IsDead)
                {
                    EndRound(player);
                    return;
                }
            }
        }
        
        private void OnPlayerDeath(FighterController deadPlayer)
        {
            if (!roundActive) return;
            
            // Find the winner (the other player)
            FighterController winner = players.FirstOrDefault(p => p != deadPlayer && !p.IsDead);
            if (winner != null)
            {
                EndRound(winner);
            }
        }
        
        private void EndRoundByTime()
        {
            // Determine winner by health percentage
            FighterController winner = null;
            float highestHealth = 0f;
            
            foreach (var player in players)
            {
                if (player != null && !player.IsDead)
                {
                    float healthPercent = player.CurrentHealth / player.MaxHealth;
                    if (healthPercent > highestHealth)
                    {
                        highestHealth = healthPercent;
                        winner = player;
                    }
                }
            }
            
            if (winner != null)
            {
                EndRound(winner);
            }
            else
            {
                // Draw - no winner
                EndRound(null);
            }
        }
        
        private void EndRound(FighterController winner)
        {
            if (!roundActive) return;
            
            roundActive = false;
            roundWinner = winner;
            
            if (winner != null)
            {
                roundWins[winner]++;
            }
            
            // Track round winner
            roundWinners.Add(winner);
            
            OnRoundEnd?.Invoke(winner);
            
            // Check if match is over
            if (winner != null && roundWins[winner] >= roundsToWin)
            {
                EndMatch(winner);
            }
            else
            {
                // Start next round after delay
                StartCoroutine(StartNextRoundAfterDelay());
            }
        }
        
        private System.Collections.IEnumerator StartNextRoundAfterDelay()
        {
            yield return new WaitForSeconds(roundEndDelay);
            currentRound++;
            StartRound();
        }
        
        private void EndMatch(FighterController winner)
        {
            OnMatchEnd?.Invoke(winner);
            
            // Create match results
            var matchResults = new Adaptabrawl.UI.MatchResults
            {
                player1 = players.Count > 0 ? players[0] : null,
                player2 = players.Count > 1 ? players[1] : null,
                winner = winner,
                player1Wins = players.Count > 0 && roundWins.ContainsKey(players[0]) ? roundWins[players[0]] : 0,
                player2Wins = players.Count > 1 && roundWins.ContainsKey(players[1]) ? roundWins[players[1]] : 0,
                roundWinners = new System.Collections.Generic.List<FighterController>(roundWinners),
                totalRounds = currentRound
            };
            
            // Store results
            Adaptabrawl.UI.MatchResultsData.SetResults(matchResults, true);
            
            // Transition to results scene after delay
            StartCoroutine(TransitionToResultsAfterDelay());
        }
        
        private System.Collections.IEnumerator TransitionToResultsAfterDelay()
        {
            yield return new WaitForSeconds(roundEndDelay);
            SceneManager.LoadScene("MatchResults");
        }
        
        public void Rematch()
        {
            // Reset match
            currentRound = 1;
            roundWins.Clear();
            roundWinners.Clear();
            foreach (var player in players)
            {
                roundWins[player] = 0;
            }
            
            StartRound();
        }
        
        public void ReturnToMenu()
        {
            SceneManager.LoadScene("StartScene");
        }
        
        // Public getters
        public int CurrentRound => currentRound;
        public float RoundTimer => roundTimer;
        public bool RoundActive => roundActive;
        public FighterController RoundWinner => roundWinner;
        public Dictionary<FighterController, int> RoundWins => new Dictionary<FighterController, int>(roundWins);
    }
}

