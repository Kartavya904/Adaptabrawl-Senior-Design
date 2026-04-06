using UnityEngine;
using Adaptabrawl.Data;
using System.Collections.Generic;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Manages mid-match classification switching. Swaps are primarily driven by the
    /// shared <see cref="switchInterval"/>, but the first two swaps can happen early
    /// when match health thresholds are crossed. The queue is rebuilt every round so
    /// players start on their selected fighter, then rotate through other fighters.
    /// </summary>
    public class ClassificationSwitcher : MonoBehaviour
    {
        private const float FirstSwapHealthThreshold = 0.5f;
        private const float SecondSwapSingleHealthThreshold = 0.25f;
        private const float SecondSwapBothHealthThreshold = 0.5f;

        [Header("Roster")]
        [Tooltip("All fighters available for switching. Loaded from Resources/Fighters/ if empty.")]
        [SerializeField] private FighterDef[] fighterRoster;

        [Header("Timing")]
        [Tooltip("Seconds between classification switches.")]
        [SerializeField] private float switchInterval = 30f;
        [Tooltip("Seconds before swap to play warning cue.")]
        [SerializeField] private float warningTime = 3.0f;
        [Tooltip("Minimum time that must pass after a swap before another swap can happen.")]
        [SerializeField] private float minimumSecondsBetweenSwaps = 10f;
        [Tooltip("Maximum swaps per player per round. 3 means a 2-minute round at 30s interval gives 4 total fighters including the selected starter.")]
        [SerializeField] private int maxSwitchesPerRound = 3;

        private float sharedTimer;
        private float roundElapsedTime;
        private float lastSwapTime;
        private int swapEventsThisRound;
        private FighterController[] players;
        private FighterDef[] initialDefs; // What players selected in character select
        private int[] switchesThisRound;
        private Queue<FighterDef>[] plannedSwitchQueues;
        private bool paused = true;       // Starts paused until explicitly resumed
        private bool hasPlayedWarning;

        [Header("Audio")]
        [SerializeField] private AudioClip preswapWarningClip;
        private AudioSource _audioSource;

        /// <summary>
        /// Fires when a player's classification changes.
        /// Args: (fighter, oldDef, newDef)
        /// </summary>
        public System.Action<FighterController, FighterDef, FighterDef> OnClassificationChanged;

        /// <summary>
        /// Call once after fighters are spawned. Loads the roster if not
        /// already assigned in the Inspector.
        /// </summary>
        public void Initialize(FighterController[] fighters)
        {
            players = fighters;
            initialDefs = new FighterDef[fighters.Length];
            switchesThisRound = new int[fighters.Length];
            plannedSwitchQueues = new Queue<FighterDef>[fighters.Length];
            for (int i = 0; i < fighters.Length; i++)
                initialDefs[i] = fighters[i].FighterDef;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            if (preswapWarningClip == null)
                preswapWarningClip = Resources.Load<AudioClip>("SFX/preswap_warning");

            // Load roster from Resources if not assigned
            if (fighterRoster == null || fighterRoster.Length == 0)
            {
                fighterRoster = Resources.LoadAll<FighterDef>("Fighters");
                if (fighterRoster.Length == 0)
                {
                    // Fallback: use the two initial defs
                    fighterRoster = initialDefs;
                    Debug.LogWarning("[ClassificationSwitcher] No roster found in Resources/Fighters/. " +
                                     "Using initially selected fighters as roster.");
                }
                else
                {
                    Debug.Log($"[ClassificationSwitcher] Loaded {fighterRoster.Length} fighters from Resources/Fighters/.");
                }
            }

            ResetTimers();

            Debug.Log($"[ClassificationSwitcher] Initialized with {players.Length} players, " +
                      $"{fighterRoster.Length} roster entries, interval={switchInterval}s.");

            // Self-register with GameManager so it can pause/resume us
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.RegisterClassificationSwitcher(this);
        }

        /// <summary>Pause the timer (e.g. during pre-round buffer, round end).</summary>
        public void Pause()
        {
            paused = true;
            Debug.Log("[ClassificationSwitcher] Paused.");
        }

        /// <summary>Resume the timer (e.g. when FIGHT! is announced).</summary>
        public void Resume()
        {
            paused = false;
            Debug.Log("[ClassificationSwitcher] Resumed.");
        }

        /// <summary>
        /// Reset all timers to the full interval. Called at the start of each round
        /// so both players get a fresh 30-second window with their current classification.
        /// </summary>
        public void ResetTimers()
        {
            if (players == null || switchesThisRound == null) return;

            BuildRoundSwitchPlans();

            for (int i = 0; i < switchesThisRound.Length; i++)
                switchesThisRound[i] = 0;

            sharedTimer = switchInterval;
            roundElapsedTime = 0f;
            lastSwapTime = -minimumSecondsBetweenSwaps;
            swapEventsThisRound = 0;
            hasPlayedWarning = false;
        }

        /// <summary>
        /// Restore each player to the classification they originally selected
        /// in character select. Called at the beginning of each new round.
        /// </summary>
        public void RestoreInitialClassifications()
        {
            if (players == null || initialDefs == null) return;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || initialDefs[i] == null) continue;
                if (players[i].FighterDef != initialDefs[i])
                {
                    players[i].SwapClassification(initialDefs[i]);
                    Debug.Log($"[ClassificationSwitcher] P{i + 1} restored to '{initialDefs[i].fighterName}'.");
                }
            }
        }

        /// <summary>Current remaining time for a given player index.</summary>
        public float GetTimerForPlayer(int index)
        {
            if (players == null || index < 0 || index >= players.Length) return 0f;
            return HasPendingSwitches(index) ? sharedTimer : 0f;
        }

        private void Update()
        {
            if (paused || players == null || switchesThisRound == null) return;
            if (!HasAnyPendingSwitches()) return;

            roundElapsedTime += Time.deltaTime;

            if (HasDeadPlayer())
                return;

            sharedTimer = Mathf.Max(0f, sharedTimer - Time.deltaTime);

            if (!hasPlayedWarning && sharedTimer > 0f && sharedTimer <= warningTime)
            {
                hasPlayedWarning = true;
                if (_audioSource != null && preswapWarningClip != null)
                    _audioSource.PlayOneShot(preswapWarningClip, 0.65f);
            }

            if (sharedTimer <= 0f)
            {
                TriggerSwapEvent("timed");
                return;
            }

            if (ShouldTriggerFirstHealthSwap())
            {
                TriggerSwapEvent("early first swap health trigger");
                return;
            }

            if (ShouldTriggerSecondHealthSwap())
                TriggerSwapEvent("early second swap health trigger");
        }

        private bool HasPendingSwitches(int playerIndex)
        {
            if (plannedSwitchQueues == null || playerIndex < 0 || playerIndex >= plannedSwitchQueues.Length)
                return false;

            if (maxSwitchesPerRound > 0 && switchesThisRound[playerIndex] >= maxSwitchesPerRound)
                return false;

            return plannedSwitchQueues[playerIndex] != null && plannedSwitchQueues[playerIndex].Count > 0;
        }

        private bool HasAnyPendingSwitches()
        {
            if (players == null)
                return false;

            for (int i = 0; i < players.Length; i++)
            {
                if (HasPendingSwitches(i))
                    return true;
            }

            return false;
        }

        private bool HasDeadPlayer()
        {
            if (players == null)
                return false;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].IsDead)
                    return true;
            }

            return false;
        }

        private bool CanTriggerSwapNow()
        {
            return roundElapsedTime - lastSwapTime >= minimumSecondsBetweenSwaps;
        }

        private bool ShouldTriggerFirstHealthSwap()
        {
            if (players == null || players.Length < 2 || swapEventsThisRound != 0 || !CanTriggerSwapNow())
                return false;

            return GetHealthRatio(players[0]) <= FirstSwapHealthThreshold ||
                   GetHealthRatio(players[1]) <= FirstSwapHealthThreshold;
        }

        private bool ShouldTriggerSecondHealthSwap()
        {
            if (players == null || players.Length < 2 || swapEventsThisRound != 1 || !CanTriggerSwapNow())
                return false;

            float player1HealthRatio = GetHealthRatio(players[0]);
            float player2HealthRatio = GetHealthRatio(players[1]);

            bool eitherCritical = player1HealthRatio <= SecondSwapSingleHealthThreshold ||
                                  player2HealthRatio <= SecondSwapSingleHealthThreshold;
            bool bothBelowHalf = player1HealthRatio <= SecondSwapBothHealthThreshold &&
                                 player2HealthRatio <= SecondSwapBothHealthThreshold;

            return eitherCritical || bothBelowHalf;
        }

        private static float GetHealthRatio(FighterController player)
        {
            if (player == null || player.MaxHealth <= 0f)
                return 1f;

            return player.CurrentHealth / player.MaxHealth;
        }

        private void BuildRoundSwitchPlans()
        {
            if (plannedSwitchQueues == null || players == null)
                return;

            for (int i = 0; i < plannedSwitchQueues.Length; i++)
                plannedSwitchQueues[i] = new Queue<FighterDef>();

            if (fighterRoster == null || fighterRoster.Length == 0)
                return;

            if (players.Length != 2)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    FighterDef roundStartDef = initialDefs != null && i < initialDefs.Length ? initialDefs[i] : null;
                    if (players[i] != null && players[i].FighterDef != null)
                        roundStartDef = players[i].FighterDef;
                    plannedSwitchQueues[i] = BuildIndependentRoundSwitchQueue(roundStartDef);
                }
                return;
            }

            FighterDef p1Start = initialDefs != null && initialDefs.Length > 0 ? initialDefs[0] : null;
            FighterDef p2Start = initialDefs != null && initialDefs.Length > 1 ? initialDefs[1] : null;
            if (players[0] != null && players[0].FighterDef != null) p1Start = players[0].FighterDef;
            if (players[1] != null && players[1].FighterDef != null) p2Start = players[1].FighterDef;

            int switchCount = maxSwitchesPerRound > 0 ? maxSwitchesPerRound : Mathf.Max(0, fighterRoster.Length - 1);
            FighterDef currentP1 = p1Start;
            FighterDef currentP2 = p2Start;

            for (int step = 0; step < switchCount; step++)
            {
                var p1Candidates = GetCandidatesForRoundStep(currentP1);
                var p2Candidates = GetCandidatesForRoundStep(currentP2);

                ShuffleInPlace(p1Candidates);
                ShuffleInPlace(p2Candidates);

                FighterDef chosenP1 = null;
                FighterDef chosenP2 = null;

                foreach (var candidate1 in p1Candidates)
                {
                    foreach (var candidate2 in p2Candidates)
                    {
                        if (candidate1 == null || candidate2 == null)
                            continue;
                        if (candidate1 == candidate2)
                            continue;

                        chosenP1 = candidate1;
                        chosenP2 = candidate2;
                        break;
                    }

                    if (chosenP1 != null && chosenP2 != null)
                        break;
                }

                if (chosenP1 == null || chosenP2 == null)
                    break;

                plannedSwitchQueues[0].Enqueue(chosenP1);
                plannedSwitchQueues[1].Enqueue(chosenP2);
                currentP1 = chosenP1;
                currentP2 = chosenP2;
            }
        }

        private Queue<FighterDef> BuildIndependentRoundSwitchQueue(FighterDef roundStartDef)
        {
            var queue = new Queue<FighterDef>();
            var candidates = GetCandidatesForRoundStep(roundStartDef);
            ShuffleInPlace(candidates);

            int maxQueueLength = candidates.Count;
            if (maxSwitchesPerRound > 0)
                maxQueueLength = Mathf.Min(maxQueueLength, maxSwitchesPerRound);

            for (int i = 0; i < maxQueueLength; i++)
                queue.Enqueue(candidates[i]);

            return queue;
        }

        private List<FighterDef> GetCandidatesForRoundStep(FighterDef currentDef)
        {
            var candidates = new List<FighterDef>();
            foreach (var fighter in fighterRoster)
            {
                if (fighter == null || fighter == currentDef || candidates.Contains(fighter))
                    continue;
                candidates.Add(fighter);
            }

            return candidates;
        }

        private static void ShuffleInPlace(List<FighterDef> fighters)
        {
            for (int i = fighters.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                FighterDef temp = fighters[i];
                fighters[i] = fighters[j];
                fighters[j] = temp;
            }
        }

        private void TriggerSwapEvent(string reason)
        {
            bool swappedAtLeastOnePlayer = false;

            for (int i = 0; i < players.Length; i++)
            {
                swappedAtLeastOnePlayer |= SwitchClassification(i);
            }

            if (!swappedAtLeastOnePlayer)
                return;

            swapEventsThisRound++;
            lastSwapTime = roundElapsedTime;
            sharedTimer = switchInterval;
            hasPlayedWarning = false;

            Debug.Log($"[ClassificationSwitcher] Triggered {reason} at {roundElapsedTime:F1}s. " +
                      $"Next timed swap scheduled in {switchInterval:F1}s.");
        }

        private bool SwitchClassification(int playerIndex)
        {
            if (!HasPendingSwitches(playerIndex)) return false;
            if (players[playerIndex] == null || players[playerIndex].IsDead) return false;

            var oldDef = players[playerIndex].FighterDef;
            var queue = plannedSwitchQueues[playerIndex];
            FighterDef newDef = queue.Dequeue();

            if (newDef == null || newDef == oldDef) return false;

            players[playerIndex].SwapClassification(newDef);
            switchesThisRound[playerIndex]++;
            OnClassificationChanged?.Invoke(players[playerIndex], oldDef, newDef);

            Debug.Log($"[ClassificationSwitcher] P{playerIndex + 1}: " +
                      $"'{oldDef?.fighterName}' → '{newDef.fighterName}'");

            return true;
        }
    }
}
