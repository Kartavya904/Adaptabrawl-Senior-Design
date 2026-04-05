using UnityEngine;
using Adaptabrawl.Data;
using System.Collections.Generic;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Manages mid-match classification switching. Every <see cref="switchInterval"/>
    /// seconds each player's character is swapped to another FighterDef from the
    /// roster (full visual prefab swap). The queue is rebuilt every round so players
    /// start on their selected fighter, then rotate through other fighters.
    /// </summary>
    public class ClassificationSwitcher : MonoBehaviour
    {
        [Header("Roster")]
        [Tooltip("All fighters available for switching. Loaded from Resources/Fighters/ if empty.")]
        [SerializeField] private FighterDef[] fighterRoster;

        [Header("Timing")]
        [Tooltip("Seconds between classification switches.")]
        [SerializeField] private float switchInterval = 30f;
        [Tooltip("Maximum swaps per player per round. 3 means a 2-minute round at 30s interval gives 4 total fighters including the selected starter.")]
        [SerializeField] private int maxSwitchesPerRound = 3;

        private float[] playerTimers;
        private FighterController[] players;
        private FighterDef[] initialDefs; // What players selected in character select
        private int[] switchesThisRound;
        private Queue<FighterDef>[] plannedSwitchQueues;
        private bool paused = true;       // Starts paused until explicitly resumed

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
            playerTimers = new float[fighters.Length];
            initialDefs = new FighterDef[fighters.Length];
            switchesThisRound = new int[fighters.Length];
            plannedSwitchQueues = new Queue<FighterDef>[fighters.Length];
            for (int i = 0; i < fighters.Length; i++)
                initialDefs[i] = fighters[i].FighterDef;

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
            if (playerTimers == null || players == null) return;

            for (int i = 0; i < playerTimers.Length; i++)
            {
                playerTimers[i] = switchInterval;
                switchesThisRound[i] = 0;

                FighterDef roundStartDef = initialDefs != null && i < initialDefs.Length ? initialDefs[i] : null;
                if (players[i] != null && players[i].FighterDef != null)
                    roundStartDef = players[i].FighterDef;

                plannedSwitchQueues[i] = BuildRoundSwitchQueue(roundStartDef);
            }
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
            if (playerTimers == null || index < 0 || index >= playerTimers.Length) return 0f;
            return playerTimers[index];
        }

        private void Update()
        {
            if (paused || players == null || playerTimers == null) return;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || players[i].IsDead || !HasPendingSwitches(i)) continue;

                playerTimers[i] -= Time.deltaTime;
                if (playerTimers[i] <= 0f)
                {
                    SwitchClassification(i);
                    playerTimers[i] = switchInterval;
                }
            }
        }

        private bool HasPendingSwitches(int playerIndex)
        {
            if (plannedSwitchQueues == null || playerIndex < 0 || playerIndex >= plannedSwitchQueues.Length)
                return false;

            if (maxSwitchesPerRound > 0 && switchesThisRound[playerIndex] >= maxSwitchesPerRound)
                return false;

            return plannedSwitchQueues[playerIndex] != null && plannedSwitchQueues[playerIndex].Count > 0;
        }

        private Queue<FighterDef> BuildRoundSwitchQueue(FighterDef roundStartDef)
        {
            var queue = new Queue<FighterDef>();
            if (fighterRoster == null || fighterRoster.Length == 0)
                return queue;

            var candidates = new List<FighterDef>();
            foreach (var fighter in fighterRoster)
            {
                if (fighter == null || fighter == roundStartDef || candidates.Contains(fighter))
                    continue;
                candidates.Add(fighter);
            }

            // Randomize order once per round so players see all available alternates without repeats.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                FighterDef temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }

            int maxQueueLength = candidates.Count;
            if (maxSwitchesPerRound > 0)
                maxQueueLength = Mathf.Min(maxQueueLength, maxSwitchesPerRound);

            for (int i = 0; i < maxQueueLength; i++)
                queue.Enqueue(candidates[i]);

            return queue;
        }

        private void SwitchClassification(int playerIndex)
        {
            if (!HasPendingSwitches(playerIndex)) return;

            var oldDef = players[playerIndex].FighterDef;
            var queue = plannedSwitchQueues[playerIndex];
            FighterDef newDef = queue.Dequeue();

            if (newDef == null || newDef == oldDef) return;

            players[playerIndex].SwapClassification(newDef);
            switchesThisRound[playerIndex]++;
            OnClassificationChanged?.Invoke(players[playerIndex], oldDef, newDef);

            Debug.Log($"[ClassificationSwitcher] P{playerIndex + 1}: " +
                      $"'{oldDef?.fighterName}' → '{newDef.fighterName}'");
        }
    }
}
