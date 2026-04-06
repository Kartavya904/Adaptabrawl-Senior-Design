using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Adaptabrawl.Gameplay;
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

        [Tooltip("Optional: lists room codes heard on LAN (PublicRoomLobbyContext scanner). Wire a TMP under Join panel.")]
        [SerializeField] private TextMeshProUGUI discoveredLanRoomsText;

        [Tooltip("Optional: pick a row to fill the join box with host IPv4:port (bypasses UDP discovery if firewall blocks it).")]
        [SerializeField] private TMP_Dropdown discoveredLanRoomDropdown;
        
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

        private void OnEnable()
        {
            var ctx = PublicRoomLobbyContext.EnsureExists();
            ctx.CurrentRoomsChanged += OnPublicRoomsChanged;
            OnPublicRoomsChanged(ctx.CurrentPublicRooms);
        }

        private void OnDisable()
        {
            if (PublicRoomLobbyContext.Instance != null)
                PublicRoomLobbyContext.Instance.CurrentRoomsChanged -= OnPublicRoomsChanged;
        }
        
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
                lobbyManager.OnHostBindFailed += OnHostBindFailed;
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

            if (roomCodeInput != null && roomCodeInput.placeholder is TextMeshProUGUI ph)
                ph.text = "6-digit code or host IP (e.g. 192.168.1.5:7777)";
                
            // Ensure we start on the Choice panel
            ShowChoicePanel();
        }

        private void LateUpdate()
        {
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame()) return;
            if (BackInputUtility.IsTextInputFocused()) return;

            if (lobbyPanel != null && lobbyPanel.activeSelf)
            {
                if (isReady)
                    ToggleReady();
                else
                    CancelLobby();
                return;
            }

            if (waitingRoomPanel != null && waitingRoomPanel.activeSelf)
            {
                CancelLobby();
                return;
            }

            if (joinRoomPanel != null && joinRoomPanel.activeSelf)
            {
                ShowChoicePanel();
                return;
            }

            if (createRoomPanel != null && createRoomPanel.activeSelf)
            {
                ShowChoicePanel();
                return;
            }

            if (choicePanel != null && choicePanel.activeSelf)
                SceneManager.LoadScene("StartScene");
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
                lobbyManager.OnHostBindFailed -= OnHostBindFailed;
            }
        }

        private void OnHostBindFailed(string message)
        {
            if (joinErrorText != null)
            {
                joinErrorText.text = message;
                joinErrorText.color = Color.red;
            }

            if (roomCodeText != null)
            {
                roomCodeText.text = message;
                roomCodeText.color = Color.red;
            }

            ShowCreatePanel();
        }
        
        private void CreateRoom()
        {
            if (lobbyManager != null)
            {
                PublicRoomLobbyContext.EnsureExists().SetLanRoomListActive(false);
                lobbyManager.CreateRoom();
            }
        }
        
        private void JoinRoom()
        {
            if (lobbyManager == null || roomCodeInput == null)
                return;

            var raw = roomCodeInput.text.Trim();
            if (string.IsNullOrEmpty(raw))
                return;

            if (joinErrorText != null)
                joinErrorText.text = "";

            if (LanAddressHints.LooksLikeIpv4WithOptionalPort(raw, out var hostIp, out var hostPort))
            {
                lobbyManager.JoinRoomByDirectIpv4(hostIp, hostPort);
                return;
            }

            var code = raw.ToUpperInvariant();
            if (code.Length != 6)
            {
                if (joinErrorText != null)
                {
                    joinErrorText.text = "Use a 6-digit code, or the host’s IPv4 like 192.168.1.5 (add :PORT if not 7777).";
                    joinErrorText.color = Color.red;
                }
                return;
            }

            lobbyManager.JoinRoom(code);
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
                roomCodeText.color = Color.white;
            }

            if (joinErrorText != null)
                joinErrorText.text = "";
            
            // Waiting-room copy is set in OnWaitingForOpponent once the host socket is up (includes IP:port for direct join).

            if (lobbyRoomCodeText != null)
            {
                lobbyRoomCodeText.text = code;
            }
        }
        
        private void OnWaitingForOpponent()
        {
            ShowWaitingRoomPanel();
            if (waitingRoomCodeText != null && lobbyManager != null)
            {
                var code = lobbyManager.CurrentRoomCode;
                var ip = lobbyManager.LastHostLanIpv4;
                var port = lobbyManager.LastHostGamePort;
                if (string.IsNullOrEmpty(ip))
                    ip = "see ipconfig → IPv4";
                waitingRoomCodeText.text =
                    $"<size=40>ROOM CODE</size>\n<size=120><color=#FFD700>{code}</color></size>\n\n" +
                    $"<size=26>Friend sees “no room”? Same box on Join, type:\n<color=#FFD700>{ip}:{port}</color></size>\n\n" +
                    "<size=28><i>Waiting for opponent…</i></size>";
            }
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
            lobbyManager?.CancelPendingJoin();
            HideAllPanels();
            if (choicePanel != null) choicePanel.SetActive(true);
        }
        
        private void ShowCreatePanel()
        {
            lobbyManager?.CancelPendingJoin();
            HideAllPanels();
            if (createRoomPanel != null) createRoomPanel.SetActive(true);
        }
        
        private void ShowJoinPanel()
        {
            lobbyManager?.CancelPendingJoin();
            HideAllPanels();
            if (joinErrorText != null) joinErrorText.text = "";
            if (joinRoomPanel != null) joinRoomPanel.SetActive(true);

            var ctx = PublicRoomLobbyContext.EnsureExists();
            ctx.SetLanRoomListActive(true);
            ctx.RequestRoomListRefresh();
        }

        private void OnPublicRoomsChanged(IReadOnlyList<LanAdvertisedRoom> rooms)
        {
            var ctx = PublicRoomLobbyContext.Instance;
            var interval = ctx != null ? ctx.PublicRoomScanIntervalSeconds : 5f;

            if (discoveredLanRoomsText != null)
                discoveredLanRoomsText.text = PublicRoomLobbyContext.FormatRoomsForDisplay(rooms, interval);

            RefreshLanRoomDropdown(rooms);
        }

        private void RefreshLanRoomDropdown(IReadOnlyList<LanAdvertisedRoom> rooms)
        {
            if (discoveredLanRoomDropdown == null)
                return;

            discoveredLanRoomDropdown.onValueChanged.RemoveListener(OnLanRoomDropdownValueChanged);

            var options = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("LAN rooms (pick to fill IP:port)")
            };

            if (rooms != null)
            {
                foreach (var r in rooms)
                {
                    var label = $"{r.RoomCode}  {r.HostIpv4}:{r.GamePort}";
                    options.Add(new TMP_Dropdown.OptionData(label));
                }
            }

            discoveredLanRoomDropdown.ClearOptions();
            discoveredLanRoomDropdown.AddOptions(options);
            discoveredLanRoomDropdown.value = 0;
            discoveredLanRoomDropdown.RefreshShownValue();
            discoveredLanRoomDropdown.onValueChanged.AddListener(OnLanRoomDropdownValueChanged);
        }

        private void OnLanRoomDropdownValueChanged(int index)
        {
            if (discoveredLanRoomDropdown == null || index <= 0 || roomCodeInput == null)
                return;

            var ctx = PublicRoomLobbyContext.Instance;
            var rooms = ctx?.CurrentPublicRooms;
            if (rooms == null || index - 1 >= rooms.Count)
                return;

            var r = rooms[index - 1];
            roomCodeInput.text = $"{r.HostIpv4}:{r.GamePort}";
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

