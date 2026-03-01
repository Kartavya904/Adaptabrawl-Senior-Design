using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;
using Adaptabrawl.Data;
using Adaptabrawl.Fighters;

namespace Adaptabrawl.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("Scene Manager")]
        [SerializeField] private SetupSceneManager setupManager;

        [Header("Fighter Selection")]
        [SerializeField] private List<FighterDef> availableFighters = new List<FighterDef>();
        
        [Header("Player 1 UI (Host)")]
        [SerializeField] private Transform player1FighterContainer;
        [SerializeField] private TextMeshProUGUI player1FighterName;
        [SerializeField] private Image player1FighterImage;
        [SerializeField] private Button player1LeftButton;
        [SerializeField] private Button player1RightButton;
        [SerializeField] private Button player1ConfirmButton;
        [SerializeField] private TextMeshProUGUI player1ReadyText;
        
        [Header("Player 2 UI (Client)")]
        [SerializeField] private Transform player2FighterContainer;
        [SerializeField] private TextMeshProUGUI player2FighterName;
        [SerializeField] private Image player2FighterImage;
        [SerializeField] private Button player2LeftButton;
        [SerializeField] private Button player2RightButton;
        [SerializeField] private Button player2ConfirmButton;
        [SerializeField] private TextMeshProUGUI player2ReadyText;
        
        [Header("Navigation")]
        [SerializeField] private Button startButton; // Deprecated, Server loads automatically
        [SerializeField] private Button backButton;
        
        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            // Load available fighters
            LoadAvailableFighters();
            
            // Setup Network RPC button listeners
            if (player1LeftButton != null) player1LeftButton.onClick.AddListener(() => RequestChangeSelection(-1));
            if (player1RightButton != null) player1RightButton.onClick.AddListener(() => RequestChangeSelection(1));
            if (player1ConfirmButton != null) player1ConfirmButton.onClick.AddListener(RequestConfirmSelection);
            
            if (player2LeftButton != null) player2LeftButton.onClick.AddListener(() => RequestChangeSelection(-1));
            if (player2RightButton != null) player2RightButton.onClick.AddListener(() => RequestChangeSelection(1));
            if (player2ConfirmButton != null) player2ConfirmButton.onClick.AddListener(RequestConfirmSelection);
            
            if (startButton != null) startButton.gameObject.SetActive(false); // Server handles transition automatically
            
            if (backButton != null)
            {
                backButton.gameObject.SetActive(true);
                backButton.onClick.AddListener(RequestGoBack);
            }
            
            if (setupManager != null)
            {
                setupManager.OnCharacterConfigChanged += UpdateUI;
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (setupManager != null) setupManager.OnCharacterConfigChanged -= UpdateUI;
        }
        
        private void LoadAvailableFighters()
        {
            if (availableFighters.Count == 0)
            {
                availableFighters.Add(FighterFactory.CreateStrikerFighter());
                availableFighters.Add(FighterFactory.CreateElusiveFighter());
            }
        }
        
        private void RequestChangeSelection(int direction)
        {
            if (setupManager != null && NetworkManager.Singleton != null && availableFighters.Count > 0)
            {
                setupManager.ChangeCharacterServerRpc(NetworkManager.Singleton.LocalClientId, direction, availableFighters.Count);
            }
        }
        
        private void RequestConfirmSelection()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                setupManager.ToggleCharacterReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        private void RequestGoBack()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                // Only Host has the authority to move everyone backward scenes
                if (NetworkManager.Singleton.IsServer)
                {
                    setupManager.GoBackToControllerServerRpc();
                }
            }
        }
        
        private void UpdateUI()
        {
            if (setupManager == null || NetworkManager.Singleton == null) return;

            bool isHost = NetworkManager.Singleton.IsServer;
            bool isClient = !isHost;

            // Lock Input: Host can only click P1, Client can only click P2
            if (player1LeftButton != null) player1LeftButton.interactable = isHost && !setupManager.p1CharacterReady.Value;
            if (player1RightButton != null) player1RightButton.interactable = isHost && !setupManager.p1CharacterReady.Value;
            if (player1ConfirmButton != null) player1ConfirmButton.interactable = isHost;

            if (player2LeftButton != null) player2LeftButton.interactable = isClient && !setupManager.p2CharacterReady.Value;
            if (player2RightButton != null) player2RightButton.interactable = isClient && !setupManager.p2CharacterReady.Value;
            if (player2ConfirmButton != null) player2ConfirmButton.interactable = isClient;

            if (backButton != null) backButton.interactable = isHost;

            // Render absolute truths from the Server Array
            int p1Idx = setupManager.p1FighterIndex.Value;
            int p2Idx = setupManager.p2FighterIndex.Value;
            bool r1 = setupManager.p1CharacterReady.Value;
            bool r2 = setupManager.p2CharacterReady.Value;

            if (availableFighters.Count > 0)
            {
                if (p1Idx < availableFighters.Count)
                {
                    var fighter1 = availableFighters[p1Idx];
                    if (player1FighterName != null) player1FighterName.text = fighter1 != null ? fighter1.fighterName : "No Fighter";
                    CharacterSelectData.selectedFighter1 = fighter1;
                }
                
                if (p2Idx < availableFighters.Count)
                {
                    var fighter2 = availableFighters[p2Idx];
                    if (player2FighterName != null) player2FighterName.text = fighter2 != null ? fighter2.fighterName : "No Fighter";
                    CharacterSelectData.selectedFighter2 = fighter2;
                }
                CharacterSelectData.isLocalMatch = true;
            }
            
            if (player1ReadyText != null)
            {
                player1ReadyText.text = r1 ? "READY" : "SELECT";
                player1ReadyText.color = r1 ? Color.green : Color.white;
            }
            
            if (player2ReadyText != null)
            {
                player2ReadyText.text = r2 ? "READY" : "SELECT";
                player2ReadyText.color = r2 ? Color.green : Color.white;
            }
        }
    }

    // Static class to pass data between scenes
    public static class CharacterSelectData
    {
        public static Adaptabrawl.Data.FighterDef selectedFighter1;
        public static Adaptabrawl.Data.FighterDef selectedFighter2;
        public static bool isLocalMatch = false;
    }
}
