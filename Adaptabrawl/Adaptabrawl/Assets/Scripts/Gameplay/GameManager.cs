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
        [SerializeField] private float preRoundBuffer = 3f; // Seconds fighters are frozen before round starts

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
        public System.Action<int> OnRoundCountdown; // 3, 2, 1, 0 (FIGHT)

        private bool _initialized;
        private bool _inPreRoundBuffer; // true while the countdown is running; blocks win detection

        private void Start()
        {
            // Wait one frame so LocalGameManager can spawn fighters before we search for them.
            StartCoroutine(InitializeIfNotTriggered());
        }

        private System.Collections.IEnumerator InitializeIfNotTriggered()
        {
            yield return null;
            if (!_initialized) InitializeMatch();
        }

        private void Update()
        {
            if (roundActive)
            {
                UpdateRoundTimer();
                CheckWinConditions();
            }
        }

        public void InitializeMatch()
        {
            if (_initialized) return;
            _initialized = true;

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
            roundTimer = roundDuration;
            roundWinner = null;
            _inPreRoundBuffer = true; // block win detection before anything else

            // Reset fighters (restores health, colliders)
            foreach (var player in players)
                if (player != null) player.ResetForNewRound();

            // Lock fighters — they cannot move or fight during the countdown
            foreach (var player in players)
                if (player != null) player.LockInput();

            // Timer starts ticking immediately
            roundActive = true;
            OnRoundStart?.Invoke(currentRound);

            StartCoroutine(PreRoundBufferRoutine());
        }

        private System.Collections.IEnumerator PreRoundBufferRoutine()
        {
            // Yield one frame so the HUD finishes subscribing to health events,
            // then push a health refresh so the bars show full from the start.
            yield return null;
            foreach (var player in players)
                if (player != null) player.BroadcastHealth();

            // Fire the 3-2-1-FIGHT countdown events
            int steps = Mathf.Max(1, Mathf.RoundToInt(preRoundBuffer));
            for (int i = steps; i > 0; i--)
            {
                OnRoundCountdown?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
            OnRoundCountdown?.Invoke(0); // 0 = FIGHT!

            _inPreRoundBuffer = false;

            // Release fighters — round is live
            foreach (var player in players)
                if (player != null) player.UnlockInput();
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
            if (_inPreRoundBuffer) return; // don't end the round during the countdown

            // Find a dead player this frame (poll-based backup for the event path).
            FighterController dead = null;
            foreach (var player in players)
            {
                if (player != null && player.IsDead)
                {
                    dead = player;
                    break;
                }
            }

            if (dead == null) return;

            // Award the round to the surviving player (null = draw).
            FighterController winner = players.FirstOrDefault(p => p != dead && p != null && !p.IsDead);
            EndRound(winner);
        }

        private void OnPlayerDeath(FighterController deadPlayer)
        {
            if (!roundActive || _inPreRoundBuffer) return;

            // Find the winner (the other player).
            FighterController winner = players.FirstOrDefault(p => p != deadPlayer && p != null && !p.IsDead);
            EndRound(winner);
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
            // Brief pause so the round-end result is visible before walk-back
            yield return new WaitForSeconds(roundEndDelay);

            // Walk all fighters back to their spawn positions before starting
            int doneCount = 0;
            int playerCount = 0;
            foreach (var player in players)
            {
                if (player != null)
                {
                    playerCount++;
                    player.StartReturnToSpawn(() => doneCount++);
                }
            }

            if (playerCount > 0)
                yield return new WaitUntil(() => doneCount >= playerCount);

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

        /// <summary>
        /// Override match settings before calling InitializeMatch().
        /// LocalGameManager calls this so its Inspector fields are the single
        /// source of truth for round duration and rounds-to-win.
        /// </summary>
        public void Configure(int newRoundsToWin, float newRoundDuration, float newRoundEndDelay, float newPreRoundBuffer)
        {
            roundsToWin    = newRoundsToWin;
            roundDuration  = newRoundDuration;
            roundEndDelay  = newRoundEndDelay;
            preRoundBuffer = newPreRoundBuffer;
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

