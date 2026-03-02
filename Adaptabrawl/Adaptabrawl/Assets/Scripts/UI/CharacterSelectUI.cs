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
        [Tooltip("Shown in the character slot when a FighterDef has no portrait assigned (e.g. runtime-created fighters).")]
        [SerializeField] private Sprite defaultPortrait;

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
            if (player1LeftButton != null) player1LeftButton.onClick.AddListener(() => RequestChangeSelection(-1, 1));
            if (player1RightButton != null) player1RightButton.onClick.AddListener(() => RequestChangeSelection(1, 1));
            if (player1ConfirmButton != null) player1ConfirmButton.onClick.AddListener(() => RequestConfirmSelection(1));

            if (player2LeftButton != null) player2LeftButton.onClick.AddListener(() => RequestChangeSelection(-1, 2));
            if (player2RightButton != null) player2RightButton.onClick.AddListener(() => RequestChangeSelection(1, 2));
            if (player2ConfirmButton != null) player2ConfirmButton.onClick.AddListener(() => RequestConfirmSelection(2));

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

        private void RequestChangeSelection(int direction, int targetPlayer)
        {
            if (setupManager == null || availableFighters.Count == 0) return;
            if (NetworkManager.Singleton != null)
                setupManager.ChangeCharacterServerRpc(NetworkManager.Singleton.LocalClientId, direction, availableFighters.Count, targetPlayer);
            else
                setupManager.LocalChangeCharacter(direction, availableFighters.Count, targetPlayer);
        }

        private void RequestConfirmSelection(int targetPlayer)
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton != null)
                setupManager.ToggleCharacterReadyServerRpc(NetworkManager.Singleton.LocalClientId, targetPlayer);
            else
                setupManager.LocalToggleCharacterReady(targetPlayer);
        }

        private void RequestGoBack()
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsServer)
                    setupManager.GoBackToControllerServerRpc();
            }
            else
            {
                setupManager.GoBackToControllerLocal();
            }
        }

        private void UpdateUI()
        {
            if (setupManager == null) return;

            bool networked = NetworkManager.Singleton != null;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isClient = networked && !isHost;
            bool isLocal = CharacterSelectData.isLocalMatch;

            // Indices and ready state: from network or local mirror
            int p1Idx = networked ? setupManager.p1FighterIndex.Value : setupManager.LocalP1FighterIndex;
            int p2Idx = networked ? setupManager.p2FighterIndex.Value : setupManager.LocalP2FighterIndex;
            bool r1 = networked ? setupManager.p1CharacterReady.Value : setupManager.LocalP1CharacterReady;
            bool r2 = networked ? setupManager.p2CharacterReady.Value : setupManager.LocalP2CharacterReady;

            // Lock input: Host = P1 only, Client = P2 only; local match or offline = both
            bool p1CanInteract = isHost || isLocal || !networked;
            bool p2CanInteract = isClient || isLocal || !networked;
            if (player1LeftButton != null) player1LeftButton.interactable = p1CanInteract && !r1;
            if (player1RightButton != null) player1RightButton.interactable = p1CanInteract && !r1;
            if (player1ConfirmButton != null) player1ConfirmButton.interactable = p1CanInteract;
            if (player2LeftButton != null) player2LeftButton.interactable = p2CanInteract && !r2;
            if (player2RightButton != null) player2RightButton.interactable = p2CanInteract && !r2;
            if (player2ConfirmButton != null) player2ConfirmButton.interactable = p2CanInteract;
            if (backButton != null) backButton.interactable = !networked || isHost;

            // Render fighter name, portrait, and persist selection for game scene
            if (availableFighters.Count > 0)
            {
                if (p1Idx >= 0 && p1Idx < availableFighters.Count)
                {
                    var fighter1 = availableFighters[p1Idx];
                    if (player1FighterName != null) player1FighterName.text = fighter1 != null ? fighter1.fighterName : "No Fighter";
                    if (player1FighterImage != null)
                    {
                        player1FighterImage.sprite = (fighter1 != null && fighter1.portrait != null) ? fighter1.portrait : defaultPortrait;
                        player1FighterImage.color = player1FighterImage.sprite != null ? Color.white : new Color(0.4f, 0.4f, 0.45f, 0.9f);
                        player1FighterImage.enabled = true;
                    }
                    CharacterSelectData.selectedFighter1 = fighter1;
                }

                if (p2Idx >= 0 && p2Idx < availableFighters.Count)
                {
                    var fighter2 = availableFighters[p2Idx];
                    if (player2FighterName != null) player2FighterName.text = fighter2 != null ? fighter2.fighterName : "No Fighter";
                    if (player2FighterImage != null)
                    {
                        player2FighterImage.sprite = (fighter2 != null && fighter2.portrait != null) ? fighter2.portrait : defaultPortrait;
                        player2FighterImage.color = player2FighterImage.sprite != null ? Color.white : new Color(0.4f, 0.4f, 0.45f, 0.9f);
                        player2FighterImage.enabled = true;
                    }
                    CharacterSelectData.selectedFighter2 = fighter2;
                }
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
