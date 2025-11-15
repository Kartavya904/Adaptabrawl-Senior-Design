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
        
        [Header("Create Room UI")]
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        
        [Header("Join Room UI")]
        [SerializeField] private GameObject joinRoomPanel;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinRoomButton;
        
        [Header("Lobby UI")]
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
                lobbyManager.OnRoomJoined += OnRoomJoined;
                lobbyManager.OnRoomJoinFailed += OnRoomJoinFailed;
                lobbyManager.OnOpponentReady += OnOpponentReady;
                lobbyManager.OnMatchStart += OnMatchStart;
            }
            
            // Setup buttons
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(CreateRoom);
            
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(JoinRoom);
            
            if (readyButton != null)
                readyButton.onClick.AddListener(ToggleReady);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelLobby);
        }
        
        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnRoomCodeGenerated -= OnRoomCodeGenerated;
                lobbyManager.OnRoomJoined -= OnRoomJoined;
                lobbyManager.OnRoomJoinFailed -= OnRoomJoinFailed;
                lobbyManager.OnOpponentReady -= OnOpponentReady;
                lobbyManager.OnMatchStart -= OnMatchStart;
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
            
            // Return to main menu
            ShowCreateJoinPanels();
        }
        
        private void OnRoomCodeGenerated(string code)
        {
            if (roomCodeText != null)
            {
                roomCodeText.text = $"Room Code: {code}";
            }
            
            if (lobbyRoomCodeText != null)
            {
                lobbyRoomCodeText.text = code;
            }
            
            ShowLobbyPanel();
        }
        
        private void OnRoomJoined()
        {
            ShowLobbyPanel();
        }
        
        private void OnRoomJoinFailed()
        {
            // Show error message
            Debug.LogError("Failed to join room!");
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
        
        private void ShowLobbyPanel()
        {
            if (createRoomPanel != null)
                createRoomPanel.SetActive(false);
            if (joinRoomPanel != null)
                joinRoomPanel.SetActive(false);
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);
        }
        
        private void ShowCreateJoinPanels()
        {
            if (lobbyPanel != null)
                lobbyPanel.SetActive(false);
            if (createRoomPanel != null)
                createRoomPanel.SetActive(true);
            if (joinRoomPanel != null)
                joinRoomPanel.SetActive(true);
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

