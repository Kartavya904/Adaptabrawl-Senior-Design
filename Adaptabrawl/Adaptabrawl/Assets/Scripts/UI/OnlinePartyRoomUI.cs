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
        private const string HostConnectedMarkup =
            "<size=30><b>PLAYER 1</b></size>\n<size=22>You · this device · Host</size>\n<size=18><color=#4A4A4A>Connected</color></size>";
        private const string GuestConnectedMarkup =
            "<size=30><b>PLAYER 2</b></size>\n<size=22>Friend · remote device</size>\n<size=18><color=#4A4A4A>Connected</color></size>";
        private const string GuestWaitingMarkup =
            "<size=30><b>PLAYER 2</b></size>\n<size=22>Waiting for friend…</size>\n<size=18><color=#5E5E5E>Share the code or use Join on their PC</color></size>";

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

            // Must not scan LAN room list (binds UDP 7777+7778) until after host binds game + discovery on those ports.
            PublicRoomLobbyContext.EnsureExists().SetLanRoomListActive(false);

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
                statusBannerText.color = new Color(0.16f, 0.16f, 0.16f, 1f);
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
                    "This screen starts a host on each PC. Only one person is the host — everyone else must tap Join and enter the host’s 6-digit code or the direct IP:port below. " +
                    "Same Wi‑Fi does not auto-merge two hosts; discovery can also fail if the router blocks device-to-device traffic.";
            if (statusBannerText != null)
                statusBannerText.text = "Creating your room…";
        }

        private void ApplyPlayerSlots(bool waitingForGuest, bool guestJoined = false)
        {
            if (player1SlotText != null)
                player1SlotText.text = HostConnectedMarkup;

            if (player2SlotText != null)
            {
                if (guestJoined)
                    player2SlotText.text = GuestConnectedMarkup;
                else if (waitingForGuest)
                    player2SlotText.text = GuestWaitingMarkup;
            }
        }

        private void OnRoomCodeGenerated(string code)
        {
            if (roomCodeBigText != null)
                roomCodeBigText.text = code;
            if (statusBannerText != null)
            {
                statusBannerText.text = "Room ready — waiting for player 2";
                statusBannerText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
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
                    $"<size=18>Direct join fallback</size>\n<size=26><b>{ip}:{port}</b></size>\n<size=16><color=#5E5E5E>Same private network · allow UDP in Windows Firewall if needed</color></size>";
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
                joinModalErrorText.color = new Color(0.18f, 0.18f, 0.18f, 1f);
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

            StartCoroutine(CoEnableLanRoomListAfterLeavingHostIfNeeded());
        }

        /// <summary>
        /// Room list listens on 7777/7778; if we are still hosting, release those ports before scanning.
        /// </summary>
        private IEnumerator CoEnableLanRoomListAfterLeavingHostIfNeeded()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening && nm.IsServer && lobbyManager != null)
            {
                lobbyManager.Disconnect();
                yield return new WaitForSecondsRealtime(0.35f);
            }

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
            PublicRoomLobbyContext.EnsureExists().SetLanRoomListActive(false);
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
                        joinModalErrorText.color = new Color(0.18f, 0.18f, 0.18f, 1f);
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
