using System;
using Adaptabrawl.Data;
using Adaptabrawl.UI;
using UnityEngine;

namespace Adaptabrawl.Gameplay
{
    public enum QuickMatchDifficultyTier
    {
        Dummy,
        Trainer,
        Extreme
    }

    public enum QuickMatchPlayerRole
    {
        Human,
        Cpu
    }

    [Serializable]
    public class QuickMatchMatchConfig
    {
        public QuickMatchDifficultyTier difficulty = QuickMatchDifficultyTier.Trainer;
        public QuickMatchPlayerRole player1Role = QuickMatchPlayerRole.Human;
        public QuickMatchPlayerRole player2Role = QuickMatchPlayerRole.Cpu;
        public int player1InputDevice;
        public int player2InputDevice;
        public int arenaIndex;
        public string arenaName = "Cascade Sanctum";
        public Sprite arenaSprite;
        public FighterDef player1Fighter;
        public FighterDef player2Fighter;
        public int opponentPreviewIndex;
        public int opponentSelectionSeed;
        public bool opponentSelectionWasRandomized = true;
        public string player1DisplayName = "Player 1";
        public string player2DisplayName = "CPU";
        public string player2PolicyId = "";
        public bool enableTrainingMode;

        public QuickMatchMatchConfig Clone()
        {
            return new QuickMatchMatchConfig
            {
                difficulty = difficulty,
                player1Role = player1Role,
                player2Role = player2Role,
                player1InputDevice = player1InputDevice,
                player2InputDevice = player2InputDevice,
                arenaIndex = arenaIndex,
                arenaName = arenaName,
                arenaSprite = arenaSprite,
                player1Fighter = player1Fighter,
                player2Fighter = player2Fighter,
                opponentPreviewIndex = opponentPreviewIndex,
                opponentSelectionSeed = opponentSelectionSeed,
                opponentSelectionWasRandomized = opponentSelectionWasRandomized,
                player1DisplayName = player1DisplayName,
                player2DisplayName = player2DisplayName,
                player2PolicyId = player2PolicyId,
                enableTrainingMode = enableTrainingMode
            };
        }

        public QuickMatchPlayerRole GetRoleForPlayer(int playerNumber)
        {
            return playerNumber == 2 ? player2Role : player1Role;
        }
    }

    /// <summary>
    /// Persistent single-player Quick Match session state.
    /// Keeps the dedicated Quick Match flow isolated from the existing local/online setup scenes.
    /// </summary>
    public sealed class QuickMatchSession : MonoBehaviour
    {
        public static QuickMatchSession Instance { get; private set; }

        [SerializeField] private bool isQuickMatchActive;
        [SerializeField] private QuickMatchMatchConfig currentConfig = new QuickMatchMatchConfig();

        public bool IsQuickMatchActive => isQuickMatchActive;

        public QuickMatchMatchConfig CurrentConfig => currentConfig != null ? currentConfig.Clone() : new QuickMatchMatchConfig();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static QuickMatchSession EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var existing = FindFirstObjectByType<QuickMatchSession>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var go = new GameObject("QuickMatchSession");
            return go.AddComponent<QuickMatchSession>();
        }

        public bool TryGetCurrentConfig(out QuickMatchMatchConfig config)
        {
            if (!isQuickMatchActive || currentConfig == null)
            {
                config = null;
                return false;
            }

            config = currentConfig.Clone();
            return true;
        }

        public void Activate(QuickMatchMatchConfig config)
        {
            currentConfig = config != null ? config.Clone() : new QuickMatchMatchConfig();
            isQuickMatchActive = true;
            ApplyConfigToLegacySessionState();
        }

        public void ClearSession()
        {
            isQuickMatchActive = false;
            currentConfig = new QuickMatchMatchConfig();
        }

        public bool IsCpuControlled(int playerNumber)
        {
            if (!isQuickMatchActive || currentConfig == null)
                return false;

            return currentConfig.GetRoleForPlayer(playerNumber) == QuickMatchPlayerRole.Cpu;
        }

        public QuickMatchPlayerRole GetRoleForPlayer(int playerNumber)
        {
            if (!isQuickMatchActive || currentConfig == null)
                return QuickMatchPlayerRole.Human;

            return currentConfig.GetRoleForPlayer(playerNumber);
        }

        public void ReapplyCurrentConfig()
        {
            if (!isQuickMatchActive || currentConfig == null)
                return;

            ApplyConfigToLegacySessionState();
        }

        private void ApplyConfigToLegacySessionState()
        {
            var lobby = LobbyContext.EnsureExists();
            lobby.Init(localMatch: true);
            lobby.SetPlayerDisplayNames(currentConfig.player1DisplayName, currentConfig.player2DisplayName);
            lobby.SetInputDevices(currentConfig.player1InputDevice, currentConfig.player2InputDevice);
            lobby.SetP1Fighter(currentConfig.player1Fighter);
            lobby.SetP2Fighter(currentConfig.player2Fighter);
            lobby.SetLastArenaSelection(currentConfig.arenaIndex, currentConfig.arenaName, currentConfig.arenaSprite);

            CharacterSelectData.isLocalMatch = true;
            CharacterSelectData.selectedFighter1 = currentConfig.player1Fighter;
            CharacterSelectData.selectedFighter2 = currentConfig.player2Fighter;
            CharacterSelectData.finalP1ControllerIndex = currentConfig.player1InputDevice;
            CharacterSelectData.finalP2ControllerIndex = currentConfig.player2InputDevice;
            ArenaSelectData.selectedArenaName = currentConfig.arenaName;
        }
    }
}
