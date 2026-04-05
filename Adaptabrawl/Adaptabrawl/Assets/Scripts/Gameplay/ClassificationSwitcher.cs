using UnityEngine;
using Adaptabrawl.Data;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Manages mid-match classification switching. Every <see cref="switchInterval"/>
    /// seconds each player's character is randomly swapped to a different FighterDef
    /// from the roster (full visual prefab swap). Attach to the same GameObject as
    /// GameManager or LocalGameManager, or any scene-level coordinator.
    /// </summary>
    public class ClassificationSwitcher : MonoBehaviour
    {
        [Header("Roster")]
        [Tooltip("All fighters available for random switching. Loaded from Resources/Fighters/ if empty.")]
        [SerializeField] private FighterDef[] fighterRoster;

        [Header("Timing")]
        [Tooltip("Seconds between classification switches.")]
        [SerializeField] private float switchInterval = 45f;

        private float[] playerTimers;
        private FighterController[] players;
        private FighterDef[] initialDefs; // What players selected in character select
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
            for (int i = 0; i < fighters.Length; i++)
            {
                playerTimers[i] = switchInterval;
                initialDefs[i] = fighters[i].FighterDef;
            }

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
            if (playerTimers == null) return;
            for (int i = 0; i < playerTimers.Length; i++)
                playerTimers[i] = switchInterval;
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
            if (paused || players == null) return;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || players[i].IsDead) continue;

                playerTimers[i] -= Time.deltaTime;
                if (playerTimers[i] <= 0f)
                {
                    SwitchClassification(i);
                    playerTimers[i] = switchInterval;
                }
            }
        }

        private void SwitchClassification(int playerIndex)
        {
            if (fighterRoster == null || fighterRoster.Length == 0) return;

            var oldDef = players[playerIndex].FighterDef;
            FighterDef newDef = null;

            // Pick a different character if possible
            if (fighterRoster.Length > 1)
            {
                int attempts = 0;
                do
                {
                    newDef = fighterRoster[Random.Range(0, fighterRoster.Length)];
                    attempts++;
                } 
                while (newDef == oldDef && attempts < 100);
            }
            else
            {
                newDef = fighterRoster[0];
            }

            players[playerIndex].SwapClassification(newDef);
            OnClassificationChanged?.Invoke(players[playerIndex], oldDef, newDef);

            Debug.Log($"[ClassificationSwitcher] P{playerIndex + 1}: " +
                      $"'{oldDef?.fighterName}' → '{newDef.fighterName}'");
        }
    }
}
