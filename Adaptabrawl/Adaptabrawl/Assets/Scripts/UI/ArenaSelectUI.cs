using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

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
            "TrainingStage",
            "IndustrialRooftops",
            "MistyForest"
        };

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI arenaNameText;
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [SerializeField] private Button readyButton; 
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button backButton;
        
        [Header("Status Displays")]
        [SerializeField] private TextMeshProUGUI p1StatusText;
        [SerializeField] private TextMeshProUGUI p2StatusText;

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            // Convert buttons to trigger networked ServerRPCs
            if (leftArrowButton != null) leftArrowButton.onClick.AddListener(() => RequestChangeArena(-1));
            if (rightArrowButton != null) rightArrowButton.onClick.AddListener(() => RequestChangeArena(1));
            
            if (readyButton != null) readyButton.onClick.AddListener(RequestReady);
            if (cancelButton != null) cancelButton.onClick.AddListener(RequestCancel);
            
            // Re-enabled back button
            if (backButton != null)
            {
                backButton.gameObject.SetActive(true);
                backButton.onClick.AddListener(RequestGoBack);
            }
            
            if (setupManager != null)
            {
                setupManager.OnArenaConfigChanged += UpdateUI;
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (setupManager != null) setupManager.OnArenaConfigChanged -= UpdateUI;
        }

        private void RequestChangeArena(int direction)
        {
            if (setupManager != null && NetworkManager.Singleton != null && availableArenas.Count > 0)
            {
                setupManager.ChangeArenaServerRpc(NetworkManager.Singleton.LocalClientId, direction, availableArenas.Count);
            }
        }

        private void RequestReady()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                bool isHost = NetworkManager.Singleton.IsServer;
                bool amIReady = isHost ? setupManager.p1ArenaReady.Value : setupManager.p2ArenaReady.Value;
                
                // If already ready, ignore. This prevents double-clicks or shared-button misfires.
                if (amIReady) return;
                
                setupManager.ToggleArenaReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        private void RequestCancel()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                setupManager.CancelArenaOverrideServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        private void RequestGoBack()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                // Only Host has the authority to move everyone backward scenes
                if (NetworkManager.Singleton.IsServer)
                {
                    setupManager.GoBackToCharacterServerRpc();
                }
            }
        }

        private void UpdateUI()
        {
            if (setupManager == null || NetworkManager.Singleton == null) return;

            bool isHost = NetworkManager.Singleton.IsServer;
            int aIdx = setupManager.arenaIndex.Value;
            bool p1Ready = setupManager.p1ArenaReady.Value;
            bool p2Ready = setupManager.p2ArenaReady.Value;

            // Render Current Choice
            if (availableArenas.Count > 0 && arenaNameText != null)
            {
                arenaNameText.text = availableArenas[aIdx];
                ArenaSelectData.selectedArenaName = availableArenas[aIdx];
            }
            
            // Interaction Lock: If EITHER player is "Ready", the arena cannot be changed
            bool isArenaLocked = p1Ready || p2Ready;
            
            if (leftArrowButton != null) leftArrowButton.interactable = !isArenaLocked;
            if (rightArrowButton != null) rightArrowButton.interactable = !isArenaLocked;
            if (backButton != null) backButton.interactable = isHost; // Only host can hit back

            // Personal Button State
            bool amIReady = isHost ? p1Ready : p2Ready;
            bool opponentReady = isHost ? p2Ready : p1Ready;

            if (readyButton != null)
            {
                readyButton.gameObject.SetActive(!amIReady);
                readyButton.interactable = true;
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(isArenaLocked);
                cancelButton.interactable = true;
            }

            // Public Status Rendering
            if (p1StatusText != null)
            {
                p1StatusText.text = p1Ready ? "P1: READY" : "P1: DECIDING...";
                p1StatusText.color = p1Ready ? Color.green : Color.yellow;
            }

            if (p2StatusText != null)
            {
                p2StatusText.text = p2Ready ? "P2: READY" : "P2: DECIDING...";
                p2StatusText.color = p2Ready ? Color.green : Color.yellow;
            }
        }
    }
}
