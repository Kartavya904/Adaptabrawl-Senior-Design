using UnityEngine;
using UnityEngine.InputSystem;
using Adaptabrawl.Data;
using Adaptabrawl.UI;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Persistent DontDestroyOnLoad singleton that carries lobby state across all scenes:
    /// player names, input devices, selected fighters, and match type.
    /// Created once on Play Local / Play Online and survives until the application quits.
    /// All UI panels read from and write to this object instead of scattered static fields.
    /// </summary>
    public class LobbyContext : MonoBehaviour
    {
        public static LobbyContext Instance { get; private set; }

        [Header("Player Display Names")]
        public string p1Name = "Player 1";
        public string p2Name = "Player 2";

        [Header("Input Devices  (0 = Keyboard, 1 = Gamepad)")]
        public int p1InputDevice = 0;
        public int p2InputDevice = 0;

        [Header("Selected Characters")]
        public FighterDef p1Fighter;
        public FighterDef p2Fighter;

        [Tooltip("Last roster index each player had (for rematch / Change Characters).")]
        public int p1LastFighterIndex;
        public int p2LastFighterIndex;

        [Tooltip("Last arena list index chosen in setup (for reference / future rematch).")]
        public int lastArenaIndex;

        [Header("Match Info")]
        public bool isLocalMatch;

        private const string DefaultP1Name = "Player 1";
        private const string DefaultP2Name = "Player 2";

        // ── Lifecycle ──────────────────────────────────────────────────────────

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

        /// <summary>
        /// Creates LobbyContext if it doesn't already exist. Safe to call every time.
        /// </summary>
        public static LobbyContext EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("LobbyContext");
            return go.AddComponent<LobbyContext>();
        }

        // ── Initialisation ─────────────────────────────────────────────────────

        /// <summary>Call when a new match session starts (Play Local / Play Online).</summary>
        public void Init(bool localMatch)
        {
            isLocalMatch = localMatch;
            p1Name = DefaultP1Name;
            p2Name = DefaultP2Name;
            p1InputDevice = 0;
            p2InputDevice = 0;
            p1Fighter = null;
            p2Fighter = null;
            p1LastFighterIndex = 0;
            p2LastFighterIndex = 0;
            lastArenaIndex = 0;

            // Sync legacy static class
            CharacterSelectData.isLocalMatch = localMatch;
        }

        /// <summary>
        /// Empty or whitespace names become Player 1 / Player 2.
        /// </summary>
        public void SetPlayerDisplayNames(string player1Name, string player2Name)
        {
            p1Name = string.IsNullOrWhiteSpace(player1Name) ? DefaultP1Name : player1Name.Trim();
            p2Name = string.IsNullOrWhiteSpace(player2Name) ? DefaultP2Name : player2Name.Trim();
        }

        public void SetLastFighterIndices(int index1, int index2)
        {
            p1LastFighterIndex = index1;
            p2LastFighterIndex = index2;
        }

        public void SetLastArenaIndex(int arenaIdx)
        {
            lastArenaIndex = arenaIdx;
        }

        /// <summary>
        /// Maps P1/P2 to physical gamepads: first "gamepad" player uses Gamepad.all[0], second uses Gamepad.all[1].
        /// If only one player uses a pad, they always get the first connected device.
        /// </summary>
        public static bool TryGetGamepadForPlayer(int playerOneOrTwo, int p1UsesGamepad01, int p2UsesGamepad01, out Gamepad pad)
        {
            pad = null;
            bool p1g = p1UsesGamepad01 == 1;
            bool p2g = p2UsesGamepad01 == 1;
            if (playerOneOrTwo == 1 && !p1g) return false;
            if (playerOneOrTwo == 2 && !p2g) return false;

            int slot = (playerOneOrTwo == 1) ? 0 : (p1g ? 1 : 0);
            if (Gamepad.all.Count <= slot) return false;
            pad = Gamepad.all[slot];
            return pad != null;
        }

        // ── Setters (write through to legacy statics for backward compat) ──────

        public void SetInputDevices(int p1Device, int p2Device)
        {
            p1InputDevice = p1Device;
            p2InputDevice = p2Device;
            CharacterSelectData.finalP1ControllerIndex = p1Device;
            CharacterSelectData.finalP2ControllerIndex = p2Device;
        }

        public void SetP1Fighter(FighterDef fighter)
        {
            p1Fighter = fighter;
            CharacterSelectData.selectedFighter1 = fighter;
        }

        public void SetP2Fighter(FighterDef fighter)
        {
            p2Fighter = fighter;
            CharacterSelectData.selectedFighter2 = fighter;
        }
    }
}
