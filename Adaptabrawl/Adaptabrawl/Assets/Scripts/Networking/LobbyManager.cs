using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;

namespace Adaptabrawl.Networking
{
    public class LobbyManager : MonoBehaviour
    {
        [Header("Lobby Settings")]
#pragma warning disable CS0414
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private string gameSceneName = "GameScene";
#pragma warning restore CS0414

        [Header("LAN discovery")]
        [Tooltip("UDP port for room lookup (broadcast). Must match on all devices; allow through firewall if needed.")]
        [SerializeField] private int discoveryPort = 7788;

        [Tooltip("How long the joining device waits for a host reply on the LAN.")]
        [SerializeField] private int joinDiscoveryTimeoutMs = 15000;

        [Tooltip("If the game UDP port is busy (e.g. second Editor), try the next ports. Skips Discovery Port.")]
        [SerializeField] private int gamePortBindAttempts = 24;

        [Tooltip("UDP port for LAN room-list beacons (must match LanLobbyRoomListService on PublicRoomLobbyContext).")]
        [SerializeField] private int beaconPort = LanRoomDiscovery.DefaultBeaconPort;

        [Header("Room Code")]
        private string currentRoomCode = "";

        [Header("Party room (new flow)")]
        [Tooltip("When true, the host loads SetupScene as soon as a second player connects (no Ready step). Leave false for the classic LobbyScene UI.")]
        [SerializeField]
        private bool autoStartWhenBothPlayersConnected;

        /// <summary>Shown on host waiting UI; guest can type this IP in the join field if discovery fails.</summary>
        public string LastHostLanIpv4 { get; private set; } = "";

        public ushort LastHostGamePort { get; private set; }

        [Header("Player States")]
        private bool isHost = false;
        private bool isReady = false;
        private bool opponentReady = false;
        private bool matchIsStarting = false;

        private CancellationTokenSource _joinCts;

        [Header("Events")]
        public System.Action<string> OnRoomCodeGenerated;
        public System.Action OnWaitingForOpponent;
        public System.Action OnRoomJoined;
        public System.Action<string> OnRoomJoinFailed;
        public System.Action OnPlayerReady;
        public System.Action OnOpponentReady;
        public System.Action OnMatchStart;
        public System.Action OnDisconnected;

        private void Start()
        {
            // Initialization happens when StartHost or StartClient is called.
        }

        private void OnDestroy()
        {
            LanRoomDiscovery.StopHostResponder();
            _joinCts?.Cancel();
            _joinCts?.Dispose();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.CustomMessagingManager?.UnregisterNamedMessageHandler("ReadyState");
            }
        }

        public void CreateRoom()
        {
            isHost = true;
            currentRoomCode = GenerateRandomCode();
            OnRoomCodeGenerated?.Invoke(currentRoomCode);

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                Debug.LogError("[LobbyManager] No NetworkManager in scene.");
                return;
            }

            var transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[LobbyManager] NetworkManager needs UnityTransport.");
                return;
            }

            LastHostLanIpv4 = "";
            LastHostGamePort = 0;

            ushort baseGamePort = transport.ConnectionData.Port;
            ushort gamePort = 0;

            if (nm.IsListening)
                nm.Shutdown();

            for (var attempt = 0; attempt < gamePortBindAttempts; attempt++)
            {
                var sum = (int)baseGamePort + attempt;
                if (sum > ushort.MaxValue)
                    break;
                var candidate = (ushort)sum;
                if (candidate == discoveryPort)
                    continue;

                transport.SetConnectionData(true, "127.0.0.1", candidate, "0.0.0.0");

                if (!nm.StartHost())
                {
                    nm.Shutdown();
                    continue;
                }

                gamePort = candidate;
                if (attempt > 0)
                    Debug.Log($"[LobbyManager] Game port {baseGamePort} busy; hosting on {gamePort}.");
                break;
            }

            if (gamePort == 0)
            {
                isHost = false;
                currentRoomCode = "";
                if (nm.IsListening)
                    nm.Shutdown();
                Debug.LogError(
                    "[LobbyManager] StartHost failed: UDP game port in use. Close the other Unity/clone/build or raise Game Port Bind Attempts.");
                return;
            }

            LastHostGamePort = gamePort;
            LastHostLanIpv4 = LanAddressHints.GetPrimaryLanIpv4();

            Debug.Log(
                $"[LobbyManager] LAN host 0.0.0.0:{gamePort}, discovery {discoveryPort}, room {currentRoomCode}. Guest can also join with IP {LastHostLanIpv4}:{gamePort}");
            LanRoomDiscovery.StartHostResponder(currentRoomCode, gamePort, discoveryPort, beaconPort);

            nm.OnClientConnectedCallback += OnClientConnected;
            nm.CustomMessagingManager.RegisterNamedMessageHandler("ReadyState", ReceiveReadyMessage);
            OnWaitingForOpponent?.Invoke();
        }

        private string GenerateRandomCode()
        {
            return UnityEngine.Random.Range(100000, 999999).ToString();
        }

        /// <summary>Stops an in-progress LAN lookup (e.g. user left the join screen).</summary>
        public void CancelPendingJoin()
        {
            _joinCts?.Cancel();
        }

        /// <summary>Join via room-code discovery, or use <see cref="JoinRoomByDirectIpv4"/>.</summary>
        public void JoinRoom(string roomCode)
        {
            roomCode = roomCode?.Trim() ?? "";
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                OnRoomJoinFailed?.Invoke("Enter a 6-digit code, or the host’s IP (e.g. 192.168.1.5 or 192.168.1.5:7777).");
                return;
            }

            _joinCts?.Cancel();
            _joinCts?.Dispose();
            _joinCts = new CancellationTokenSource();
            JoinRoomAsync(roomCode, _joinCts.Token);
        }

        /// <summary>Bypasses UDP discovery; use when broadcast/multicast never reaches this PC (firewall, some routers).</summary>
        public void JoinRoomByDirectIpv4(string hostIpv4, int optionalGamePort = 0)
        {
            hostIpv4 = hostIpv4?.Trim() ?? "";
            if (string.IsNullOrEmpty(hostIpv4))
            {
                OnRoomJoinFailed?.Invoke("Host IP is empty.");
                return;
            }

            if (!IPAddress.TryParse(hostIpv4, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
            {
                OnRoomJoinFailed?.Invoke("Invalid IPv4 address.");
                return;
            }

            _joinCts?.Cancel();
            _joinCts?.Dispose();
            _joinCts = null;

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                OnRoomJoinFailed?.Invoke("No NetworkManager in scene.");
                return;
            }

            var transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                OnRoomJoinFailed?.Invoke("Missing UnityTransport.");
                return;
            }

            ushort port = optionalGamePort > 0 ? (ushort)optionalGamePort : transport.ConnectionData.Port;
            transport.SetConnectionData(true, hostIpv4, port);

            if (nm.StartClient())
            {
                currentRoomCode = "DIRECT";
                Debug.Log($"[LobbyManager] Direct client → {hostIpv4}:{port}");
                nm.OnClientConnectedCallback += OnClientConnected;
            }
            else
                OnRoomJoinFailed?.Invoke("Failed to start client (wrong IP/port or host not ready).");
        }

        private async void JoinRoomAsync(string roomCode, CancellationToken ct)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                OnRoomJoinFailed?.Invoke("No NetworkManager in scene.");
                return;
            }

            var transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                OnRoomJoinFailed?.Invoke("Missing UnityTransport.");
                return;
            }

            try
            {
                var (ok, hostIp, hostPort) = await LanRoomDiscovery.DiscoverHostAsync(
                    roomCode,
                    discoveryPort,
                    joinDiscoveryTimeoutMs,
                    ct);

                if (ct.IsCancellationRequested)
                    return;

                if (!ok)
                {
                    Debug.LogWarning(
                        "[LobbyManager] Discovery timed out. Same router (Wi‑Fi or Ethernet OK), both Private, correct code. " +
                        "Windows often never shows a firewall prompt—manually allow Unity/your game for UDP (Private) or open inbound UDP for discovery + game ports.");
                    OnRoomJoinFailed?.Invoke(
                        "No room found by automatic search (UDP blocked or isolated Wi‑Fi). Ask the host for the IP on their waiting screen (or ipconfig), then type in this box: 192.168.x.x or 192.168.x.x:PORT and press Join.");
                    return;
                }

                transport.SetConnectionData(true, hostIp, hostPort);

                if (nm.StartClient())
                {
                    currentRoomCode = roomCode;
                    Debug.Log($"[LobbyManager] Client connecting to {hostIp}:{hostPort}...");
                    nm.OnClientConnectedCallback += OnClientConnected;
                }
                else
                    OnRoomJoinFailed?.Invoke("Failed to start client connection.");
            }
            catch (TaskCanceledException)
            {
                // join superseded or scene unloaded
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                OnRoomJoinFailed?.Invoke("Join failed unexpectedly.");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log("[LobbyManager] An opponent joined our room!");
                    OnRoomJoined?.Invoke();
                    TryAutoStartWhenPartyFull();
                }
            }
            else
            {
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log("[LobbyManager] Successfully connected to the Host!");
                    OnRoomJoined?.Invoke();

                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                        "ReadyState",
                        ReceiveReadyMessage);
                }
            }
        }

        private void TryAutoStartWhenPartyFull()
        {
            if (!autoStartWhenBothPlayersConnected)
                return;

            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || !nm.IsListening)
                return;

            if (nm.ConnectedClientsIds.Count < 2)
                return;

            StartCoroutine(CoAutoStartMatchNextFrame());
        }

        private System.Collections.IEnumerator CoAutoStartMatchNextFrame()
        {
            yield return null;
            StartMatch();
        }

        public void SetReady(bool ready)
        {
            isReady = ready;
            OnPlayerReady?.Invoke();

            SendReadyState(ready);

            if (isReady && opponentReady)
                StartMatch();
        }

        private void SendReadyState(bool ready)
        {
            if (!NetworkManager.Singleton.IsListening)
                return;

            using (var writer = new FastBufferWriter(1, Allocator.Temp))
            {
                writer.WriteValueSafe(ready);

                if (NetworkManager.Singleton.IsServer)
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ReadyState", writer);
                else
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                        "ReadyState",
                        NetworkManager.ServerClientId,
                        writer);
            }
        }

        private void ReceiveReadyMessage(ulong senderId, FastBufferReader messagePayload)
        {
            messagePayload.ReadValueSafe(out bool opponentIsReady);
            OnOpponentReadyReceived(opponentIsReady);
        }

        public void OnOpponentReadyReceived(bool ready)
        {
            opponentReady = ready;
            if (ready)
                OnOpponentReady?.Invoke();

            if (isReady && opponentReady)
                StartMatch();
        }

        public void StartMatch()
        {
            if (matchIsStarting)
                return;
            matchIsStarting = true;

            OnMatchStart?.Invoke();

            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log("[LobbyManager] Host loading SetupScene...");
                NetworkManager.Singleton.SceneManager.LoadScene("SetupScene", LoadSceneMode.Single);
            }
        }

        public void Disconnect()
        {
            LanRoomDiscovery.StopHostResponder();
            _joinCts?.Cancel();
            _joinCts?.Dispose();
            _joinCts = null;

            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();

            isHost = false;
            isReady = false;
            opponentReady = false;
            currentRoomCode = "";
            LastHostLanIpv4 = "";
            LastHostGamePort = 0;
            OnDisconnected?.Invoke();
            matchIsStarting = false;

            Debug.Log("[LobbyManager] Network connection completely shut down.");
        }

        public string CurrentRoomCode => currentRoomCode;
        public bool IsHost => isHost;
        public bool IsReady => isReady;
        public bool OpponentReady => opponentReady;
        public bool AutoStartWhenBothPlayersConnected => autoStartWhenBothPlayersConnected;
    }
}
