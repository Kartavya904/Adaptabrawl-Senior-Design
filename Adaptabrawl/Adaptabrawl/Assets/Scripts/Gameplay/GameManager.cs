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
        [SerializeField] private float roundDuration = 120f; // Seconds (2 minutes of active round time)
        [SerializeField] private float roundEndDelay = 3f;
        [SerializeField] private float preRoundBuffer = 3f; // Seconds fighters are frozen before round starts

        [Header("Audio (Announcer)")]
        [SerializeField] private AudioClip countdownBeepClip;
        [SerializeField] private AudioClip fightStartClip;
        [SerializeField] private AudioClip roundEndClip;
        private AudioSource _audioSource;

        [Header("Players")]
        [SerializeField] private List<FighterController> players = new List<FighterController>();

        [Header("Round State")]
        private int currentRound = 1;
        private Dictionary<FighterController, int> roundWins = new Dictionary<FighterController, int>();
        private List<FighterController> roundWinners = new List<FighterController>();
        private float roundTimer = 0f;
        private bool roundActive = false;
        private bool roundEnding = false;
        private FighterController roundWinner = null;

        [Header("Events")]
        public System.Action<int> OnRoundStart; // round number
        public System.Action<FighterController> OnRoundEnd; // winner
        public System.Action<FighterController> OnMatchEnd; // winner
        public System.Action<float> OnRoundTimerUpdate; // remaining time
        public System.Action<int> OnRoundCountdown; // 3, 2, 1, 0 (FIGHT)

        private bool _initialized;
        private bool _inPreRoundBuffer; // true while the countdown is running; blocks win detection
        private ClassificationSwitcher _classificationSwitcher;

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

        public void TriggerHitStop(float duration = 0.1f)
        {
            StartCoroutine(HitStopRoutine(duration));
        }

        private System.Collections.IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f; // Slow down to simulate hitstop
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        public void InitializeMatch()
        {
            if (_initialized) return;
            _initialized = true;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            if (countdownBeepClip == null) countdownBeepClip = Resources.Load<AudioClip>("SFX/beep");
            if (fightStartClip == null)    fightStartClip    = Resources.Load<AudioClip>("SFX/fight");
            if (roundEndClip == null)      roundEndClip      = Resources.Load<AudioClip>("SFX/matchover");

            // Find all fighters
            players.Clear();
            players.AddRange(FindObjectsByType<FighterController>(FindObjectsSortMode.None));

            // Initialize round wins
            foreach (var player in players)
            {
                EnsureRoundWinEntry(player);

                // Subscribe to death events
                player.OnDeath += () => OnPlayerDeath(player);
            }

            // Find classification switcher in scene. May be null on first call;
            // ClassificationSwitcher.Initialize() calls RegisterClassificationSwitcher() to ensure the ref is set.
            _classificationSwitcher = FindFirstObjectByType<ClassificationSwitcher>();

            StartRound();
        }

        /// <summary>
        /// Called by ClassificationSwitcher.Initialize() to register itself.
        /// Guarantees the reference is valid even if the switcher is created after InitializeMatch().
        /// </summary>
        public void RegisterClassificationSwitcher(ClassificationSwitcher switcher)
        {
            _classificationSwitcher = switcher;
            Debug.Log("[GameManager] ClassificationSwitcher registered.");

            // If we are already past the pre-round buffer (round is live),
            // resume the switcher immediately since Resume() was already called on null.
            if (roundActive && !_inPreRoundBuffer)
            {
                _classificationSwitcher.Resume();
                Debug.Log("[GameManager] Round already live - auto-resumed switcher.");
            }
        }


        private void StartRound()
        {
            EnsureRoundWinEntries();
            roundTimer = roundDuration;
            roundWinner = null;
            roundEnding = false;
            _inPreRoundBuffer = true; // block win detection before anything else

            // Every round starts from each player's original character selection with fresh switch timers.
            if (_classificationSwitcher != null)
            {
                _classificationSwitcher.RestoreInitialClassifications();
                _classificationSwitcher.ResetTimers();
            }

            // Reset fighters (restores health, colliders)
            foreach (var player in players)
                if (player != null) player.ResetForNewRound();

            // Lock fighters — they cannot move or fight during the countdown
            foreach (var player in players)
                if (player != null) player.LockInput();

            // Timer starts ticking immediately
            roundActive = true;
            OnRoundStart?.Invoke(currentRound);

            // Pause classification switcher during pre-round buffer
            if (_classificationSwitcher != null)
                _classificationSwitcher.Pause();

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
                if (_audioSource != null && countdownBeepClip != null)
                    _audioSource.PlayOneShot(countdownBeepClip, 0.8f);
                yield return new WaitForSeconds(1f);
            }
            OnRoundCountdown?.Invoke(0); // 0 = FIGHT!
            if (_audioSource != null && fightStartClip != null)
                _audioSource.PlayOneShot(fightStartClip, 1f);

            _inPreRoundBuffer = false;

            // Release fighters — round is live
            foreach (var player in players)
                if (player != null) player.UnlockInput();

            // Resume classification switcher now that the round is live
            if (_classificationSwitcher != null)
                _classificationSwitcher.Resume();
        }

        private void UpdateRoundTimer()
        {
            // Pre-round countdown should not consume playable round time.
            if (_inPreRoundBuffer)
            {
                OnRoundTimerUpdate?.Invoke(roundTimer);
                return;
            }

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
            if (_inPreRoundBuffer || roundEnding) return; // don't end the round during the countdown

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
            if (!roundActive || _inPreRoundBuffer || roundEnding) return;

            FighterController winner = players.FirstOrDefault(p => p != deadPlayer && p != null && !p.IsDead);
            StartCoroutine(KOSlowdownRoutine(winner));
        }

        private System.Collections.IEnumerator KOSlowdownRoutine(FighterController winner)
        {
            roundEnding = true;
            roundActive = false; // block further death processing
            Time.timeScale = 0.12f;
            yield return new WaitForSecondsRealtime(0.45f);
            Time.timeScale = 1f;
            EndRound(winner);
        }

        private void EndRoundByTime()
        {
            if (roundEnding) return;
            roundEnding = true;

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
            if (!roundActive && !roundEnding) return;

            roundActive = false;
            roundEnding = true;
            roundWinner = winner;
            if (_audioSource != null && roundEndClip != null)
                _audioSource.PlayOneShot(roundEndClip, 1f);
            EnsureRoundWinEntries();

            // Pause switching during round-end
            if (_classificationSwitcher != null)
                _classificationSwitcher.Pause();

            if (winner != null)
            {
                EnsureRoundWinEntry(winner);
                roundWins[winner]++;
            }

            // Track round winner
            roundWinners.Add(winner);

            OnRoundEnd?.Invoke(winner);

            // Check if match is over
            if (winner != null && GetRoundWinsForPlayer(winner) >= roundsToWin)
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
            Time.timeScale = 1f;
            EnsureRoundWinEntries();
            OnMatchEnd?.Invoke(winner);

            // Create match results
            var matchResults = new Adaptabrawl.UI.MatchResults
            {
                player1 = players.Count > 0 ? players[0] : null,
                player2 = players.Count > 1 ? players[1] : null,
                winner = winner,
                player1Wins = players.Count > 0 ? GetRoundWinsForPlayer(players[0]) : 0,
                player2Wins = players.Count > 1 ? GetRoundWinsForPlayer(players[1]) : 0,
                roundWinners = new System.Collections.Generic.List<FighterController>(roundWinners),
                totalRounds = currentRound
            };

            // Store results — pass isLocalMatch from CharacterSelectData so Rematch routes correctly
            bool isLocal = Adaptabrawl.UI.CharacterSelectData.isLocalMatch;
            Adaptabrawl.UI.MatchResultsData.SetResults(matchResults, isLocal);

            if (Adaptabrawl.UI.SceneTransitionManager.Instance != null)
                Adaptabrawl.UI.SceneTransitionManager.Instance.TransitionToScene("MatchResults");
            else
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
            roundActive = false;
            roundEnding = false;
            roundWins.Clear();
            roundWinners.Clear();
            foreach (var player in players)
            {
                EnsureRoundWinEntry(player);
            }

            StartRound();
        }

        private void EnsureRoundWinEntries()
        {
            foreach (FighterController player in players)
                EnsureRoundWinEntry(player);
        }

        private void EnsureRoundWinEntry(FighterController player)
        {
            if (player == null || roundWins.ContainsKey(player))
                return;

            roundWins[player] = 0;
        }

        private int GetRoundWinsForPlayer(FighterController player)
        {
            if (player == null)
                return 0;

            EnsureRoundWinEntry(player);
            return roundWins[player];
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
