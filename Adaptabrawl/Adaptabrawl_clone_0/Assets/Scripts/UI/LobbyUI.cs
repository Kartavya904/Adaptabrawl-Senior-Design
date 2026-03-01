using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Networking;

namespace Adaptabrawl.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbyManager lobbyManager;
        
        [Header("Choice Panel UI")]
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button showCreatePanelButton;
        [SerializeField] private Button showJoinPanelButton;
        
        [Header("Create Room UI")]
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button backFromCreateButton;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        
        [Header("Join Room UI")]
        [SerializeField] private GameObject joinRoomPanel;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button backFromJoinButton;
        [SerializeField] private TextMeshProUGUI joinErrorText;
        
        [Header("Waiting Room UI (Host Only)")]
        [SerializeField] private GameObject waitingRoomPanel;
        [SerializeField] private TextMeshProUGUI waitingRoomCodeText;
        [SerializeField] private Button cancelWaitingButton;
        
        [Header("Lobby UI (Both Connected)")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TextMeshProUGUI lobbyRoomCodeText;
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyStatusText;
        [SerializeField] private TextMeshProUGUI opponentReadyStatusText;
        [SerializeField] private Button cancelButton;
        
        private bool isReady = false;
        
        private void Start()
        {
            if (lobbyManager == null)
                lobbyManager = FindFirstObjectByType<LobbyManager>();
            
            if (lobbyManager != null)
            {
                lobbyManager.OnRoomCodeGenerated += OnRoomCodeGenerated;
                lobbyManager.OnWaitingForOpponent += OnWaitingForOpponent;
                lobbyManager.OnRoomJoined += OnRoomJoined;
                lobbyManager.OnRoomJoinFailed += OnRoomJoinFailed;
                lobbyManager.OnPlayerReady += OnPlayerReady;
                lobbyManager.OnOpponentReady += OnOpponentReady;
                lobbyManager.OnMatchStart += OnMatchStart;
                lobbyManager.OnDisconnected += OnDisconnected;
            }
            
            // Setup Choice Path buttons
            if (showCreatePanelButton != null)
                showCreatePanelButton.onClick.AddListener(ShowCreatePanel);
                
            if (showJoinPanelButton != null)
                showJoinPanelButton.onClick.AddListener(ShowJoinPanel);
            
            // Setup Create panel buttons
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(CreateRoom);
            if (backFromCreateButton != null)
                backFromCreateButton.onClick.AddListener(ShowChoicePanel);
            
            // Setup Join panel buttons    
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(JoinRoom);
            if (backFromJoinButton != null)
                backFromJoinButton.onClick.AddListener(ShowChoicePanel);
            
            // Setup Waiting Room buttons
            if (cancelWaitingButton != null)
                cancelWaitingButton.onClick.AddListener(CancelLobby);
            
            // Setup Lobby buttons
            if (readyButton != null)
                readyButton.onClick.AddListener(ToggleReady);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelLobby);
                
            // Ensure error text starts empty
            if (joinErrorText != null) joinErrorText.text = "";
                
            // Ensure we start on the Choice panel
            ShowChoicePanel();
        }
        
        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnRoomCodeGenerated -= OnRoomCodeGenerated;
                lobbyManager.OnWaitingForOpponent -= OnWaitingForOpponent;
                lobbyManager.OnRoomJoined -= OnRoomJoined;
                lobbyManager.OnRoomJoinFailed -= OnRoomJoinFailed;
                lobbyManager.OnPlayerReady -= OnPlayerReady;
                lobbyManager.OnOpponentReady -= OnOpponentReady;
                lobbyManager.OnMatchStart -= OnMatchStart;
                lobbyManager.OnDisconnected -= OnDisconnected;
            }
        }
        
        private void CreateRoom()
        {
            if (lobbyManager != null)
            {
                lobbyManager.CreateRoom();
            }
        }
        
        private void JoinRoom()
        {
            if (lobbyManager != null && roomCodeInput != null)
            {
                string code = roomCodeInput.text.ToUpper();
                if (code.Length == 6)
                {
                    lobbyManager.JoinRoom(code);
                }
            }
        }
        
        private void ToggleReady()
        {
            isReady = !isReady;
            if (lobbyManager != null)
            {
                lobbyManager.SetReady(isReady);
            }
            
            UpdateReadyUI();
        }
        
        private void CancelLobby()
        {
            if (lobbyManager != null)
            {
                lobbyManager.Disconnect();
            }
        }
        
        private void OnDisconnected()
        {
            // Reset ready state locally
            isReady = false;
            UpdateReadyUI();
            
            if (opponentReadyStatusText != null)
            {
                opponentReadyStatusText.text = "Opponent: Waiting...";
                opponentReadyStatusText.color = Color.gray;
            }
            
            if (joinErrorText != null) joinErrorText.text = "";
            
            // Return to choice menu
            ShowChoicePanel();
        }
        
        private void OnRoomCodeGenerated(string code)
        {
            if (roomCodeText != null)
            {
                roomCodeText.text = $"Room Code: {code}";
            }
            
            if (waitingRoomCodeText != null)
            {
                waitingRoomCodeText.text = $"<size=40>YOUR ROOM CODE:</size>\n<size=120><color=#FFD700>{code}</color></size>\n\n<size=30><i>Waiting for opponent to join...</i></size>";

            }
            
            if (lobbyRoomCodeText != null)
            {
                lobbyRoomCodeText.text = code;
            }
        }
        
        private void OnWaitingForOpponent()
        {
            ShowWaitingRoomPanel();
        }
        
        private void OnRoomJoined()
        {
            ShowLobbyPanel();
        }
        
        private void OnRoomJoinFailed(string errorMessage)
        {
            if (joinErrorText != null)
            {
                joinErrorText.text = errorMessage;
                joinErrorText.color = Color.red;
            }
            Debug.LogError($"[LobbyUI] Join Failed: {errorMessage}");
        }
        
        private void OnPlayerReady()
        {
            UpdateReadyUI();
        }
        
        private void OnOpponentReady()
        {
            if (opponentReadyStatusText != null)
            {
                opponentReadyStatusText.text = "Opponent: Ready";
                opponentReadyStatusText.color = Color.green;
            }
        }
        
        private void OnMatchStart()
        {
            // Match is starting, scene will change
        }
        
        // --- PANEL VISIBILITY CONTROLLERS ---
        private void HideAllPanels()
        {
            if (choicePanel != null) choicePanel.SetActive(false);
            if (createRoomPanel != null) createRoomPanel.SetActive(false);
            if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
            if (waitingRoomPanel != null) waitingRoomPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
        }

        private void ShowLobbyPanel()
        {
            HideAllPanels();
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
        }
        
        private void ShowWaitingRoomPanel()
        {
            HideAllPanels();
            if (waitingRoomPanel != null) waitingRoomPanel.SetActive(true);
        }
        
        private void ShowChoicePanel()
        {
            HideAllPanels();
            if (choicePanel != null) choicePanel.SetActive(true);
        }
        
        private void ShowCreatePanel()
        {
            HideAllPanels();
            if (createRoomPanel != null) createRoomPanel.SetActive(true);
        }
        
        private void ShowJoinPanel()
        {
            HideAllPanels();
            if (joinErrorText != null) joinErrorText.text = "";
            if (joinRoomPanel != null) joinRoomPanel.SetActive(true);
        }
        
        private void UpdateReadyUI()
        {
            if (readyStatusText != null)
            {
                readyStatusText.text = isReady ? "Ready" : "Not Ready";
                readyStatusText.color = isReady ? Color.green : Color.red;
            }
            
            if (readyButton != null)
            {
                var colors = readyButton.colors;
                colors.normalColor = isReady ? Color.green : Color.white;
                readyButton.colors = colors;
            }
        }
    }
}

