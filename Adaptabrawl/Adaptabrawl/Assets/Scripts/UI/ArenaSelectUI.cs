using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;

namespace Adaptabrawl.UI
{
    public static class ArenaSelectData
    {
        public static string selectedArenaName = "TrainingStage"; // Default
    }

    public class ArenaSelectUI : MonoBehaviour
    {
        [Header("Scene Manager")]
        [SerializeField] private SetupSceneManager setupManager;

        [Header("Arena List")]
        [SerializeField] private List<string> availableArenas = new List<string>
        {
            // 0 = default
            "Cascade Sanctum",     // Bright valley, waterfalls
            "Ashen Crucible",      // Lava / volcanic arena
            "Aetherfall Citadel"   // Night castle with blue magic
        };

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI arenaNameText;
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [SerializeField] private Button p1ReadyButton;
        [SerializeField] private Button p2ReadyButton;
        [SerializeField] private Button backButton;
        
        [Header("Status Displays")]
        [SerializeField] private TextMeshProUGUI p1StatusText;
        [SerializeField] private TextMeshProUGUI p2StatusText;

        [Header("Arena Background")]
        [SerializeField] private Image arenaBackgroundImage;
        [SerializeField] private List<Sprite> arenaBackgrounds = new List<Sprite>();

        [Header("Ready Button Labels (assign TMP text inside each ready button)")]
        [SerializeField] private TextMeshProUGUI p1ReadyButtonText;
        [SerializeField] private TextMeshProUGUI p2ReadyButtonText;

        [Header("Countdown")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Tooltip("Optional vertical focus order. If empty: arena arrows → P1 ready → P2 ready → back.")]
        [SerializeField] private Selectable[] arenaFocusOrder;

        private bool _countdownRunning;

        public bool IsCountdownActive => _countdownRunning;

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            // Convert buttons to trigger networked ServerRPCs
            if (leftArrowButton != null) leftArrowButton.onClick.AddListener(() => RequestChangeArena(-1));
            if (rightArrowButton != null) rightArrowButton.onClick.AddListener(() => RequestChangeArena(1));
            
            if (p1ReadyButton != null) p1ReadyButton.onClick.AddListener(() => RequestReady(1));
            if (p2ReadyButton != null) p2ReadyButton.onClick.AddListener(() => RequestReady(2));
            
            // Re-enabled back button
            if (backButton != null)
            {
                backButton.gameObject.SetActive(true);
                backButton.onClick.AddListener(RequestGoBack);
            }
            
            if (setupManager != null)
            {
                setupManager.OnArenaConfigChanged += UpdateUI;
                setupManager.OnArenaCountdownRequested += StartArenaCountdown;
            }

            SyncLobbyDevicesFromSetupManager();
            UpdateUI();
            WireArenaMenuNavigation();
        }

        private void WireArenaMenuNavigation()
        {
            Selectable[] order = arenaFocusOrder != null && arenaFocusOrder.Length > 0
                ? arenaFocusOrder
                : new Selectable[] { leftArrowButton, rightArrowButton, p1ReadyButton, p2ReadyButton, backButton };

            var list = new List<Selectable>();
            foreach (var s in order)
            {
                if (s != null) list.Add(s);
            }

            if (list.Count == 0) return;
            MenuNavigationGroup.ApplyVerticalChain(list, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(list);
        }

        private void Update()
        {
            if (setupManager == null || _countdownRunning) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isLocal = CharacterSelectData.isLocalMatch;
            bool p1Ready = networked ? setupManager.p1ArenaReady.Value : setupManager.LocalP1ArenaReady;
            bool p2Ready = networked ? setupManager.p2ArenaReady.Value : setupManager.LocalP2ArenaReady;

            int p1CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                          : (networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex);
            int p2CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                          : (networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex);

            if (!networked)
            {
                if (!p1Ready && WasReadyPressedForPlayer(1, p1CtrlIdx, p2CtrlIdx))
                {
                    RequestReady(1);
                    return;
                }

                if (!p2Ready && WasReadyPressedForPlayer(2, p1CtrlIdx, p2CtrlIdx))
                    RequestReady(2);

                return;
            }

            if (!p1Ready && (isHost || isLocal || !networked) && WasReadyPressedForPlayer(1, p1CtrlIdx, p2CtrlIdx))
            {
                RequestReady(1);
                return;
            }

            if (!p2Ready && (!isHost || isLocal || !networked) && WasReadyPressedForPlayer(2, p1CtrlIdx, p2CtrlIdx))
            {
                RequestReady(2);
            }
        }

        private void OnEnable()
        {
            SyncLobbyDevicesFromSetupManager();
            // When the Arena Select panel becomes active, refresh UI so the background image is set.
            UpdateUI();
        }

        private void SyncLobbyDevicesFromSetupManager()
        {
            if (setupManager == null) return;
            var lobby = LobbyContext.EnsureExists();
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            int p1 = networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex;
            int p2 = networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex;
            lobby.SetInputDevices(p1, p2);
        }

        private void OnDestroy()
        {
            if (setupManager != null)
            {
                setupManager.OnArenaConfigChanged -= UpdateUI;
                setupManager.OnArenaCountdownRequested -= StartArenaCountdown;
            }
        }

        private void RequestChangeArena(int direction)
        {
            if (setupManager == null || availableArenas.Count == 0) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                setupManager.ChangeArenaServerRpc(NetworkManager.Singleton.LocalClientId, direction, availableArenas.Count);
            }
            else
            {
                setupManager.LocalChangeArena(direction, availableArenas.Count);
            }
        }

        private void RequestReady(int targetPlayer)
        {
            if (setupManager == null) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                bool isHost = NetworkManager.Singleton.IsServer;
                
                // Lock: host can only toggle P1, client can only toggle P2
                if ((targetPlayer == 1 && !isHost) || (targetPlayer == 2 && isHost))
                    return;
                
                setupManager.ToggleArenaReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                // Pure local: either button toggles its respective player; game starts when both are ready.
                setupManager.LocalToggleArenaReady(targetPlayer);
            }
        }

        private void RequestGoBack()
        {
            if (setupManager == null) return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                // Only Host has the authority to move everyone backward scenes
                if (NetworkManager.Singleton.IsServer)
                {
                    setupManager.GoBackToCharacterServerRpc();
                }
            }
            else
            {
                setupManager.GoBackToCharacterLocal();
            }
        }

        /// <summary>
        /// Writes the arena index/name/sprite from <see cref="SetupSceneManager"/> into
        /// <see cref="LobbyContext"/> so the game scene can read it on all peers.
        /// </summary>
        private void PushCurrentArenaToLobbyContext()
        {
            if (setupManager == null) return;
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            int aIdx = networked ? setupManager.arenaIndex.Value : setupManager.LocalArenaIndex;
            TryGetArenaSpriteForIndex(aIdx, out Sprite sprite);
            if (availableArenas.Count > 0 && aIdx >= 0 && aIdx < availableArenas.Count)
                LobbyContext.EnsureExists().SetLastArenaSelection(aIdx, availableArenas[aIdx], sprite);
            else
                LobbyContext.EnsureExists().SetLastArenaSelection(aIdx, "", null);
        }

        /// <summary>
        /// Sets the arena background Image's source sprite to the one at the given arena index.
        /// availableArenas and arenaBackgrounds are aligned: 0, 1, 2.
        /// </summary>
        private void ApplyArenaBackground(int arenaIndex)
        {
            if (arenaBackgroundImage == null) return;
            if (!TryGetArenaSpriteForIndex(arenaIndex, out Sprite s) || s == null) return;

            arenaBackgroundImage.sprite = s;
            arenaBackgroundImage.enabled = true;
        }

        private bool TryGetArenaSpriteForIndex(int arenaIndex, out Sprite sprite)
        {
            sprite = null;
            if (arenaBackgrounds == null || arenaBackgrounds.Count == 0) return false;
            int idx = Mathf.Clamp(arenaIndex, 0, arenaBackgrounds.Count - 1);
            sprite = arenaBackgrounds[idx];
            return true;
        }

        private void UpdateUI()
        {
            if (setupManager == null) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool isHost = networked && NetworkManager.Singleton.IsServer;

            int aIdx = networked ? setupManager.arenaIndex.Value : setupManager.LocalArenaIndex;
            bool p1Ready = networked ? setupManager.p1ArenaReady.Value : setupManager.LocalP1ArenaReady;
            bool p2Ready = networked ? setupManager.p2ArenaReady.Value : setupManager.LocalP2ArenaReady;

            // Render Current Choice
            if (availableArenas.Count > 0 && arenaNameText != null)
            {
                arenaNameText.text = availableArenas[aIdx];
                ArenaSelectData.selectedArenaName = availableArenas[aIdx];
            }

            TryGetArenaSpriteForIndex(aIdx, out Sprite arenaSprite);
            if (availableArenas.Count > 0 && aIdx >= 0 && aIdx < availableArenas.Count)
                LobbyContext.EnsureExists().SetLastArenaSelection(aIdx, availableArenas[aIdx], arenaSprite);
            else
                LobbyContext.EnsureExists().SetLastArenaSelection(aIdx, "", null);

            // Set the panel's Image source to the sprite for the selected arena (elements 0, 1, 2).
            ApplyArenaBackground(aIdx);

            // Interaction Lock: If EITHER player is "Ready", the arena cannot be changed
            bool isArenaLocked = p1Ready || p2Ready;
            
            if (leftArrowButton != null) leftArrowButton.interactable = !isArenaLocked;
            if (rightArrowButton != null) rightArrowButton.interactable = !isArenaLocked;
            bool bothReady = p1Ready && p2Ready;

            // Back button:
            // - Online: only host can go back, and not once both players are ready.
            // - Local (no network): always allowed until both players are ready.
            if (backButton != null)
            {
                if (networked)
                    backButton.interactable = !bothReady && isHost;
                else
                    backButton.interactable = !bothReady;
            }

            // Button interactivity: in online play, each client can only toggle their own ready;
            // in local play, both buttons are active. Disabled when both players are ready.
            if (p1ReadyButton != null)
            {
                if (bothReady)
                    p1ReadyButton.interactable = false;
                else if (networked)
                    p1ReadyButton.interactable = isHost;
                else
                    p1ReadyButton.interactable = true;
            }

            if (p2ReadyButton != null)
            {
                if (bothReady)
                    p2ReadyButton.interactable = false;
                else if (networked)
                    p2ReadyButton.interactable = !isHost;
                else
                    p2ReadyButton.interactable = true;
            }

            int p1CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                          : (networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex);
            int p2CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                          : (networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex);
            string p1Dev = LobbySetupInputHints.DeviceLabel(p1CtrlIdx);
            string p2Dev = LobbySetupInputHints.DeviceLabel(p2CtrlIdx);

            if (p1StatusText != null)
            {
                p1StatusText.text = p1Ready ? $"P1: READY ({p1Dev})" : $"P1: choosing… ({p1Dev})";
                p1StatusText.color = p1Ready ? Color.green : Color.yellow;
            }

            if (p2StatusText != null)
            {
                p2StatusText.text = p2Ready ? $"P2: READY ({p2Dev})" : $"P2: choosing… ({p2Dev})";
                p2StatusText.color = p2Ready ? Color.green : Color.yellow;
            }

            if (p1ReadyButtonText != null)
                p1ReadyButtonText.text = LobbySetupInputHints.ArenaReadyButtonRich(p1Ready, p1CtrlIdx, true);
            if (p2ReadyButtonText != null)
                p2ReadyButtonText.text = LobbySetupInputHints.ArenaReadyButtonRich(p2Ready, p2CtrlIdx, false);
        }

        private void StartArenaCountdown()
        {
            if (_countdownRunning) return;
            StartCoroutine(ArenaCountdownRoutine());
        }

        private System.Collections.IEnumerator ArenaCountdownRoutine()
        {
            _countdownRunning = true;

            // Disable all buttons once countdown begins.
            if (leftArrowButton != null) leftArrowButton.interactable = false;
            if (rightArrowButton != null) rightArrowButton.interactable = false;
            if (backButton != null) backButton.interactable = false;
            if (p1ReadyButton != null) p1ReadyButton.interactable = false;
            if (p2ReadyButton != null) p2ReadyButton.interactable = false;

            if (countdownText != null)
                countdownText.gameObject.SetActive(true);

            for (int i = 3; i > 0; i--)
            {
                if (countdownText != null)
                    countdownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            // Ensure every machine has the final arena in LobbyContext before the game scene loads
            // (covers edge cases where UI did not run another UpdateUI pass).
            PushCurrentArenaToLobbyContext();

            // Host (or local instance) actually triggers the scene load.
            string gameSceneName = CharacterSelectData.isLocalMatch ? "GameScene" : "OnlineGameScene";
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
                }
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private static bool WasReadyPressedForPlayer(int playerNumber, int p1Device, int p2Device)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            if (playerNumber == 1 && p1Device == 1 && LobbyContext.TryGetGamepadForPlayer(1, p1Device, p2Device, out var g1))
                return bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.ReadyUp, g1 != null ? LobbyContext.GetGamepadListIndexForPlayer(1, p1Device, p2Device) : -1);

            if (playerNumber == 2 && p2Device == 1 && LobbyContext.TryGetGamepadForPlayer(2, p1Device, p2Device, out var g2))
                return bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.ReadyUp, g2 != null ? LobbyContext.GetGamepadListIndexForPlayer(2, p1Device, p2Device) : -1);

            bool dualKeyboard = LobbyContext.IsDualKeyboardMode(p1Device, p2Device);
            var profile = ControlBindingProfileResolver.ResolveGlobalKeyboardProfile(playerNumber, dualKeyboard);
            return bindings.WasActionPressedThisFrame(profile, ControlActionId.ReadyUp);
        }
    }
}
