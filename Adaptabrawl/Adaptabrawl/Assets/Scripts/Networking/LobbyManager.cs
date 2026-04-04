using System.Collections;
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
        /// <summary>Fixed NGO / Unity Transport UDP listen port for LAN matches. Host and direct-join clients assume this port.</summary>
        public const ushort DefaultLanGamePort = 7777;

        [Header("Lobby Settings")]
#pragma warning disable CS0414
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private string gameSceneName = "GameScene";
#pragma warning restore CS0414

        [Header("LAN game port")]
        [Tooltip("Host always binds this UDP port (default 7777). Room codes and discovery advertise this port so joins stay in sync.")]
        [SerializeField]
        private ushort gameListenPort = DefaultLanGamePort;

        [Tooltip("After releasing the network stack, wait this long so the OS can free UDP 7777 before binding again.")]
        [SerializeField]
        [Range(0f, 2f)]
        private float secondsToWaitAfterShutdownBeforeBind = 0.25f;

        [Header("LAN discovery")]
        [Tooltip("UDP port for room lookup (broadcast). Must match on all devices; allow through firewall if needed.")]
        [SerializeField] private int discoveryPort = 7788;

        [Tooltip("How long the joining device waits for a host reply on the LAN.")]
        [SerializeField] private int joinDiscoveryTimeoutMs = 15000;

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
        /// <summary>Fired when the host cannot bind <see cref="gameListenPort"/> (usually another process already uses UDP 7777).</summary>
        public System.Action<string> OnHostBindFailed;

        private Coroutine _createRoomRoutine;

        private void Start()
        {
            // Initialization happens when StartHost or StartClient is called.
        }

        private void OnDestroy()
        {
            if (_createRoomRoutine != null)
            {
                StopCoroutine(_createRoomRoutine);
                _createRoomRoutine = null;
            }

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
            if (_createRoomRoutine != null)
                StopCoroutine(_createRoomRoutine);
            _createRoomRoutine = StartCoroutine(CreateRoomCoroutine());
        }

        private IEnumerator CreateRoomCoroutine()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                Debug.LogError("[LobbyManager] No NetworkManager in scene.");
                FailHostSetup("No NetworkManager in this scene.");
                _createRoomRoutine = null;
                yield break;
            }

            var transport = nm.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[LobbyManager] NetworkManager needs UnityTransport.");
                FailHostSetup("NetworkManager is missing Unity Transport.");
                _createRoomRoutine = null;
                yield break;
            }

            if (gameListenPort == 0 || gameListenPort == discoveryPort)
            {
                FailHostSetup("Invalid game port configuration.");
                _createRoomRoutine = null;
                yield break;
            }

            LanRoomDiscovery.StopHostResponder();

            if (nm.IsListening)
                nm.Shutdown();

            yield return null;

            if (secondsToWaitAfterShutdownBeforeBind > 0f)
                yield return new WaitForSecondsRealtime(secondsToWaitAfterShutdownBeforeBind);

            LastHostLanIpv4 = "";
            LastHostGamePort = 0;
            isHost = false;
            currentRoomCode = "";

            var roomCode = GenerateRandomCode();
            transport.SetConnectionData(true, "127.0.0.1", gameListenPort, "0.0.0.0");

            if (!nm.StartHost())
            {
                nm.Shutdown();
                isHost = false;
                currentRoomCode = "";
                var msg =
                    $"Could not start host on UDP port {gameListenPort}. Another window (second Unity Play, build, or other app) may already be using that port. " +
                    "Close every other copy of this game or Editor play session on this PC, then try again. " +
                    $"Games on a different PC on the same Wi‑Fi still use port {gameListenPort} on their own machine — that is fine.";
                Debug.LogError("[LobbyManager] " + msg);
                OnHostBindFailed?.Invoke(msg);
                _createRoomRoutine = null;
                yield break;
            }

            isHost = true;
            currentRoomCode = roomCode;
            LastHostGamePort = gameListenPort;
            LastHostLanIpv4 = LanAddressHints.GetPrimaryLanIpv4();

            Debug.Log(
                $"[LobbyManager] LAN host 0.0.0.0:{gameListenPort}, discovery {discoveryPort}, room {currentRoomCode}. " +
                $"Guest can join with code or {LastHostLanIpv4}:{gameListenPort}");

            LanRoomDiscovery.StartHostResponder(currentRoomCode, gameListenPort, discoveryPort, beaconPort);

            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.CustomMessagingManager?.UnregisterNamedMessageHandler("ReadyState");
            nm.CustomMessagingManager.RegisterNamedMessageHandler("ReadyState", ReceiveReadyMessage);

            OnRoomCodeGenerated?.Invoke(currentRoomCode);
            OnWaitingForOpponent?.Invoke();
            _createRoomRoutine = null;
        }

        private void FailHostSetup(string message)
        {
            isHost = false;
            currentRoomCode = "";
            OnHostBindFailed?.Invoke(message);
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

            ushort port = optionalGamePort > 0 ? (ushort)optionalGamePort : gameListenPort;
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
                        $"No room found by automatic search (UDP blocked or isolated Wi‑Fi). Ask the host for their IPv4 (ipconfig), then type 192.168.x.x:{gameListenPort} here, or use the 6-digit code again.");
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
            if (_createRoomRoutine != null)
            {
                StopCoroutine(_createRoomRoutine);
                _createRoomRoutine = null;
            }

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
        public ushort GameListenPort => gameListenPort;
    }
}
