using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Networking;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// LAN party room: auto-host with room code, player slots, and a join-by-code modal.
    /// When both players are connected, <see cref="LobbyManager"/> loads SetupScene (controller config first).
    /// </summary>
    public class OnlinePartyRoomUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbyManager lobbyManager;

        [Header("Main layout")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI roomCodeBigText;
        [SerializeField] private TextMeshProUGUI directConnectText;
        [SerializeField] private TextMeshProUGUI player1SlotText;
        [SerializeField] private TextMeshProUGUI player2SlotText;
        [SerializeField] private TextMeshProUGUI statusBannerText;

        [Header("Actions")]
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button backToMenuButton;

        [Header("Join modal")]
        [SerializeField] private GameObject joinModalBackdrop;
        [SerializeField] private GameObject joinModalPanel;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private Button joinModalConfirmButton;
        [SerializeField] private Button joinModalCancelButton;
        [SerializeField] private TextMeshProUGUI joinModalErrorText;

        [Tooltip("Optional LAN room list (same as classic lobby).")]
        [SerializeField] private TextMeshProUGUI discoveredLanRoomsText;

        [SerializeField] private TMP_Dropdown discoveredLanRoomDropdown;

        private bool _hostingStarted;
        private bool _modalOpen;

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
                lobbyManager = GetComponent<LobbyManager>();
            if (lobbyManager == null)
                lobbyManager = FindFirstObjectByType<LobbyManager>();

            if (lobbyManager != null)
            {
                lobbyManager.OnRoomCodeGenerated += OnRoomCodeGenerated;
                lobbyManager.OnWaitingForOpponent += OnWaitingForOpponent;
                lobbyManager.OnRoomJoined += OnRoomJoined;
                lobbyManager.OnRoomJoinFailed += OnRoomJoinFailed;
                lobbyManager.OnDisconnected += OnDisconnected;
                lobbyManager.OnMatchStart += OnMatchStart;
                lobbyManager.OnHostBindFailed += OnHostBindFailed;
            }

            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(OpenJoinModal);
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(BackToMainMenu);
            if (joinModalConfirmButton != null)
                joinModalConfirmButton.onClick.AddListener(SubmitJoinFromModal);
            if (joinModalCancelButton != null)
                joinModalCancelButton.onClick.AddListener(CloseJoinModal);

            if (joinCodeInput != null && joinCodeInput.placeholder is TextMeshProUGUI ph)
                ph.text = "6-digit code or 192.168.x.x (port 7777 if omitted)";

            if (joinModalErrorText != null)
                joinModalErrorText.text = "";

            SetModalVisible(false);
            RefreshStaticCopy();
            ApplyPlayerSlots(waitingForGuest: true);

            var ctx = PublicRoomLobbyContext.EnsureExists();
            ctx.SetLanRoomListActive(true);
            ctx.RequestRoomListRefresh();

            StartCoroutine(CoStartHostingWhenReady());
        }

        private IEnumerator CoStartHostingWhenReady()
        {
            yield return null;
            if (_hostingStarted || lobbyManager == null)
                yield break;
            _hostingStarted = true;
            lobbyManager.CreateRoom();
        }

        private void LateUpdate()
        {
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame()) return;
            if (BackInputUtility.IsTextInputFocused()) return;

            if (_modalOpen)
            {
                CloseJoinModal();
                return;
            }

            BackToMainMenu();
        }

        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnRoomCodeGenerated -= OnRoomCodeGenerated;
                lobbyManager.OnWaitingForOpponent -= OnWaitingForOpponent;
                lobbyManager.OnRoomJoined -= OnRoomJoined;
                lobbyManager.OnRoomJoinFailed -= OnRoomJoinFailed;
                lobbyManager.OnDisconnected -= OnDisconnected;
                lobbyManager.OnMatchStart -= OnMatchStart;
                lobbyManager.OnHostBindFailed -= OnHostBindFailed;
            }
        }

        private void OnHostBindFailed(string message)
        {
            if (statusBannerText != null)
            {
                statusBannerText.text = message;
                statusBannerText.color = new Color(1f, 0.55f, 0.45f);
            }

            if (roomCodeBigText != null)
                roomCodeBigText.text = "— — — — — —";
            if (directConnectText != null)
                directConnectText.text = "";
            _hostingStarted = false;
        }

        private void RefreshStaticCopy()
        {
            if (titleText != null)
                titleText.text = "ONLINE PARTY";
            if (subtitleText != null)
                subtitleText.text =
                    "You are hosting on this device. Share the code with a friend on the same Wi‑Fi or LAN (Ethernet counts). " +
                    "They can enter it here with Join a room. Two windows on one PC: first uses port 7777; if this one uses 7778, join the other session with Join a room and its code.";
            if (statusBannerText != null)
                statusBannerText.text = "Creating your room…";
        }

        private void ApplyPlayerSlots(bool waitingForGuest, bool guestJoined = false)
        {
            if (player1SlotText != null)
            {
                player1SlotText.text =
                    "<size=32><color=#66FF99>PLAYER 1</color></size>\n<size=26>You · this device · Host</size>\n<size=22><color=#AAAAAA>Connected</color></size>";
            }

            if (player2SlotText != null)
            {
                if (guestJoined)
                {
                    player2SlotText.text =
                        "<size=32><color=#66CCFF>PLAYER 2</color></size>\n<size=26>Friend · remote device</size>\n<size=22><color=#66FF99>Connected</color></size>";
                }
                else if (waitingForGuest)
                {
                    player2SlotText.text =
                        "<size=32><color=#888888>PLAYER 2</color></size>\n<size=26>Waiting for friend…</size>\n<size=22><color=#FFCC66>Share the code or use Join on their PC</color></size>";
                }
            }
        }

        private void OnRoomCodeGenerated(string code)
        {
            if (roomCodeBigText != null)
                roomCodeBigText.text = code;
            if (statusBannerText != null)
            {
                statusBannerText.text = "Room ready — waiting for player 2";
                statusBannerText.color = new Color(1f, 0.84f, 0.35f);
            }
        }

        private void OnWaitingForOpponent()
        {
            if (directConnectText != null && lobbyManager != null)
            {
                var ip = lobbyManager.LastHostLanIpv4;
                var port = lobbyManager.LastHostGamePort;
                if (string.IsNullOrEmpty(ip))
                    ip = "your IPv4 (ipconfig)";
                directConnectText.text =
                    $"<size=24>Direct join (if code search fails)</size>\n<size=30><color=#FFD080>{ip}:{port}</color></size>\n<size=22><color=#AAAAAA>Same private network · allow UDP in Windows Firewall if needed</color></size>";
            }
        }

        private void OnRoomJoined()
        {
            if (_modalOpen)
                CloseJoinModal();

            ApplyPlayerSlots(waitingForGuest: false, guestJoined: true);
            if (statusBannerText != null)
                statusBannerText.text = "Both players in — loading character setup…";
        }

        private void OnRoomJoinFailed(string message)
        {
            if (joinModalErrorText != null)
            {
                joinModalErrorText.text = message;
                joinModalErrorText.color = new Color(1f, 0.45f, 0.45f);
            }
        }

        private void OnDisconnected()
        {
            if (joinModalErrorText != null)
                joinModalErrorText.text = "";
            _hostingStarted = false;
            if (statusBannerText != null)
                statusBannerText.text = "Disconnected";
            ApplyPlayerSlots(waitingForGuest: true);
            if (roomCodeBigText != null)
                roomCodeBigText.text = "— — — — — —";
            if (directConnectText != null)
                directConnectText.text = "";
        }

        private void OnMatchStart()
        {
            // Scene load is driven by the host via NetworkManager.
        }

        public void OpenJoinModal()
        {
            lobbyManager?.CancelPendingJoin();
            SetModalVisible(true);
            if (joinModalErrorText != null)
                joinModalErrorText.text = "";

            var ctx = PublicRoomLobbyContext.EnsureExists();
            ctx.SetLanRoomListActive(true);
            ctx.RequestRoomListRefresh();

            if (joinCodeInput != null)
            {
                joinCodeInput.ActivateInputField();
                joinCodeInput.Select();
            }
        }

        public void CloseJoinModal()
        {
            SetModalVisible(false);
            lobbyManager?.CancelPendingJoin();
            TryRehostIfIdleAfterClosingJoin();
        }

        private void TryRehostIfIdleAfterClosingJoin()
        {
            if (lobbyManager == null)
                return;
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening)
                return;
            lobbyManager.CreateRoom();
        }

        private void SetModalVisible(bool visible)
        {
            _modalOpen = visible;
            if (joinModalBackdrop != null)
                joinModalBackdrop.SetActive(visible);
            if (joinModalPanel != null)
                joinModalPanel.SetActive(visible);
            if (mainPanel != null)
            {
                var cg = mainPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.alpha = visible ? 0.35f : 1f;
            }
        }

        public void SubmitJoinFromModal()
        {
            if (lobbyManager == null || joinCodeInput == null)
                return;

            var raw = joinCodeInput.text.Trim();
            if (string.IsNullOrEmpty(raw))
                return;

            if (joinModalErrorText != null)
                joinModalErrorText.text = "";

            StartCoroutine(CoJoinAfterLeaveHost(raw));
        }

        private IEnumerator CoJoinAfterLeaveHost(string raw)
        {
            lobbyManager.Disconnect();
            yield return new WaitForSecondsRealtime(0.35f);

            if (LanAddressHints.LooksLikeIpv4WithOptionalPort(raw, out var hostIp, out var hostPort))
                lobbyManager.JoinRoomByDirectIpv4(hostIp, hostPort);
            else
            {
                var code = raw.ToUpperInvariant();
                if (code.Length != 6)
                {
                    if (joinModalErrorText != null)
                    {
                        joinModalErrorText.text =
                            "Use a 6-digit code, or IPv4 like 192.168.1.5 (add :7777 only if the host changed the default port).";
                        joinModalErrorText.color = Color.red;
                    }
                    yield break;
                }

                lobbyManager.JoinRoom(code);
            }

            // Modal stays open until OnRoomJoined; errors go to joinModalErrorText via OnRoomJoinFailed.
        }

        public void BackToMainMenu()
        {
            lobbyManager?.Disconnect();
            SceneManager.LoadScene("StartScene");
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
                new TMP_Dropdown.OptionData("LAN rooms (tap to fill IP:port)")
            };

            if (rooms != null)
            {
                foreach (var r in rooms)
                    options.Add(new TMP_Dropdown.OptionData($"{r.RoomCode}  {r.HostIpv4}:{r.GamePort}"));
            }

            discoveredLanRoomDropdown.ClearOptions();
            discoveredLanRoomDropdown.AddOptions(options);
            discoveredLanRoomDropdown.value = 0;
            discoveredLanRoomDropdown.RefreshShownValue();
            discoveredLanRoomDropdown.onValueChanged.AddListener(OnLanRoomDropdownValueChanged);
        }

        private void OnLanRoomDropdownValueChanged(int index)
        {
            if (discoveredLanRoomDropdown == null || index <= 0 || joinCodeInput == null)
                return;

            var ctx = PublicRoomLobbyContext.Instance;
            var rooms = ctx?.CurrentPublicRooms;
            if (rooms == null || index - 1 >= rooms.Count)
                return;

            var r = rooms[index - 1];
            joinCodeInput.text = $"{r.HostIpv4}:{r.GamePort}";
        }
    }
}
