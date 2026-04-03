using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Adaptabrawl.Gameplay;

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

        private bool _countdownRunning;

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
        }

        private void Update()
        {
            if (setupManager == null || _countdownRunning) return;

            bool networked = NetworkManager.Singleton != null;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isLocal = CharacterSelectData.isLocalMatch;
            bool p1Ready = networked ? setupManager.p1ArenaReady.Value : setupManager.LocalP1ArenaReady;
            bool p2Ready = networked ? setupManager.p2ArenaReady.Value : setupManager.LocalP2ArenaReady;

            // LobbyContext is the source of truth for device type; fall back to setupManager
            int p1CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                          : (networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex);
            int p2CtrlIdx = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                          : (networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex);

            if (!p1Ready && (isHost || isLocal || !networked))
            {
                bool p1Confirm = false;
                if (p1CtrlIdx == 1 && LobbyContext.TryGetGamepadForPlayer(1, p1CtrlIdx, p2CtrlIdx, out var gp1))
                    p1Confirm = gp1.buttonNorth.wasPressedThisFrame;
                else if (p1CtrlIdx != 1)
                    p1Confirm = UnityEngine.Input.GetKeyDown(KeyCode.Space);
                if (p1Confirm) RequestReady(1);
            }
            else if (p1Ready && (isHost || isLocal || !networked))
            {
                bool p1Unready = false;
                if (p1CtrlIdx == 1 && LobbyContext.TryGetGamepadForPlayer(1, p1CtrlIdx, p2CtrlIdx, out var gp1u))
                    p1Unready = gp1u.buttonEast.wasPressedThisFrame;
                else if (p1CtrlIdx != 1)
                    p1Unready = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
                if (p1Unready) RequestReady(1);
            }

            if (!p2Ready && (!isHost || isLocal || !networked))
            {
                bool p2Confirm = false;
                if (p2CtrlIdx == 1 && LobbyContext.TryGetGamepadForPlayer(2, p1CtrlIdx, p2CtrlIdx, out var gp2))
                    p2Confirm = gp2.buttonNorth.wasPressedThisFrame;
                else if (p2CtrlIdx != 1)
                    p2Confirm = UnityEngine.Input.GetKeyDown(KeyCode.Return);
                if (p2Confirm) RequestReady(2);
            }
            else if (p2Ready && (!isHost || isLocal || !networked))
            {
                bool p2Unready = false;
                if (p2CtrlIdx == 1 && LobbyContext.TryGetGamepadForPlayer(2, p1CtrlIdx, p2CtrlIdx, out var gp2u))
                    p2Unready = gp2u.buttonEast.wasPressedThisFrame;
                else if (p2CtrlIdx != 1)
                    p2Unready = UnityEngine.Input.GetKeyDown(KeyCode.Backspace);
                if (p2Unready) RequestReady(2);
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

            if (NetworkManager.Singleton != null)
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

            if (NetworkManager.Singleton != null)
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

            if (NetworkManager.Singleton != null)
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
        /// Sets the arena background Image's source sprite to the one at the given arena index.
        /// availableArenas and arenaBackgrounds are aligned: 0, 1, 2.
        /// </summary>
        private void ApplyArenaBackground(int arenaIndex)
        {
            if (arenaBackgroundImage == null) return;
            if (arenaBackgrounds == null || arenaBackgrounds.Count == 0) return;

            int idx = Mathf.Clamp(arenaIndex, 0, arenaBackgrounds.Count - 1);
            Sprite s = arenaBackgrounds[idx];
            if (s != null)
            {
                arenaBackgroundImage.sprite = s;
                arenaBackgroundImage.enabled = true;
            }
        }

        private void UpdateUI()
        {
            if (setupManager == null) return;

            bool networked = NetworkManager.Singleton != null;
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

            if (availableArenas.Count > 0 && aIdx >= 0 && aIdx < availableArenas.Count)
                LobbyContext.Instance?.SetLastArenaSelection(aIdx, availableArenas[aIdx]);
            else
                LobbyContext.Instance?.SetLastArenaSelection(aIdx, "");

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

            // Host (or local instance) actually triggers the scene load.
            string gameSceneName = CharacterSelectData.isLocalMatch ? "GameScene" : "OnlineGameScene";
            if (NetworkManager.Singleton != null)
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
    }
}
