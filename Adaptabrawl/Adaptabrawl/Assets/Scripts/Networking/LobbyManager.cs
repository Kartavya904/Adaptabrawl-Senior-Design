using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Networking
{
    public class LobbyManager : MonoBehaviour
    {
        /// <summary>Preferred NGO / Unity Transport UDP listen port for LAN hosts. Direct IP join without :port assumes this.</summary>
        public const ushort DefaultLanGamePort = LanUdpPorts.GamePrimary;

        /// <summary>Second listen port when <see cref="DefaultLanGamePort"/> is already taken on the same PC (two Editor/build windows).</summary>
        public const ushort SameMachineFallbackLanGamePort = LanUdpPorts.GameCompanion;

        [Header("Lobby Settings")]
#pragma warning disable CS0414
        [SerializeField] private int maxPlayers = 2;
        [SerializeField] private string gameSceneName = "GameScene";
#pragma warning restore CS0414

        [Header("LAN game port")]
        [Tooltip("Preferred UDP port for hosting (default 7777). Room code / discovery advertise the port that actually binds.")]
        [SerializeField]
        private ushort gameListenPort = DefaultLanGamePort;

        [Tooltip("If the preferred port is already in use on this PC (e.g. second game window), try this port next so both instances can run.")]
        [SerializeField]
        private ushort sameMachineFallbackGamePort = SameMachineFallbackLanGamePort;

        [SerializeField]
        private bool trySameMachineFallbackPort = true;

        [Tooltip("After releasing the network stack, wait this long so the OS can free UDP before binding again.")]
        [SerializeField]
        [Range(0f, 2f)]
        private float secondsToWaitAfterShutdownBeforeBind = 0.25f;

        [Header("LAN discovery")]
        [Tooltip("How long the joining device waits for a host reply on the LAN.")]
        [SerializeField] private int joinDiscoveryTimeoutMs = 15000;

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
        /// <summary>Fired when the host cannot bind any candidate game port on this machine.</summary>
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

            if (gameListenPort == 0 || gameListenPort == sameMachineFallbackGamePort)
            {
                FailHostSetup("Invalid game port configuration (primary and fallback must differ).");
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
            var candidates = BuildHostPortCandidates();
            ushort chosenPort = 0;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                transport.SetConnectionData(true, "127.0.0.1", candidate, "0.0.0.0");

                if (nm.StartHost())
                {
                    chosenPort = candidate;
                    if (i > 0)
                    {
                        Debug.Log(
                            $"[LobbyManager] Port {gameListenPort} is in use on this PC — hosting this window on {chosenPort}. " +
                            "To join the other session on this machine, use Join a room with that window’s code (it stays on " +
                            $"{gameListenPort}). This window’s guests must use this window’s code or {LanAddressHints.GetPrimaryLanIpv4()}:{chosenPort}.");
                    }

                    break;
                }

                nm.Shutdown();
                yield return null;
                if (secondsToWaitAfterShutdownBeforeBind > 0f)
                    yield return new WaitForSecondsRealtime(secondsToWaitAfterShutdownBeforeBind);
            }

            if (chosenPort == 0)
            {
                isHost = false;
                currentRoomCode = "";
                var tried = string.Join(", ", candidates);
                var msg =
                    $"Could not start host on UDP port(s): {tried}. Close extra Unity Play / build windows on this PC, or free those ports, then try again.";
                Debug.LogError("[LobbyManager] " + msg);
                OnHostBindFailed?.Invoke(msg);
                _createRoomRoutine = null;
                yield break;
            }

            isHost = true;
            currentRoomCode = roomCode;
            LastHostGamePort = chosenPort;
            LastHostLanIpv4 = LanAddressHints.GetPrimaryLanIpv4();

            var companionUdp = GetCompanionLanServicePort(chosenPort);
            Debug.Log(
                $"[LobbyManager] LAN host game UDP 0.0.0.0:{chosenPort}, discovery/beacon UDP *:{companionUdp}, room {currentRoomCode}. " +
                $"Guest can join with code or {LastHostLanIpv4}:{chosenPort}");

            LanRoomDiscovery.StartHostResponder(currentRoomCode, chosenPort, companionUdp);

            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.CustomMessagingManager?.UnregisterNamedMessageHandler("ReadyState");
            nm.CustomMessagingManager.RegisterNamedMessageHandler("ReadyState", ReceiveReadyMessage);

            OnRoomCodeGenerated?.Invoke(currentRoomCode);
            OnWaitingForOpponent?.Invoke();
            _createRoomRoutine = null;
        }

        private List<ushort> BuildHostPortCandidates()
        {
            var list = new List<ushort> { gameListenPort };
            if (!trySameMachineFallbackPort)
                return list;

            if (sameMachineFallbackGamePort == 0 || sameMachineFallbackGamePort == gameListenPort)
                return list;

            list.Add(sameMachineFallbackGamePort);
            return list;
        }

        /// <summary>UDP port for FIND/beacons: the game port’s companion (7777 ↔ 7778).</summary>
        private int GetCompanionLanServicePort(ushort chosenGamePort) =>
            chosenGamePort == gameListenPort ? sameMachineFallbackGamePort : gameListenPort;

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

        /// <summary>Join via 6-digit code (prefers LAN room list IP:port), then UDP discovery fallback.</summary>
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

            if (TryJoinUsingLanRoomList(roomCode))
                return;

            _joinCts = new CancellationTokenSource();
            JoinRoomAsync(roomCode, _joinCts.Token);
        }

        /// <summary>Uses <see cref="PublicRoomLobbyContext.CurrentPublicRooms"/> (same data as the on-screen LAN list) to direct-connect.</summary>
        private bool TryJoinUsingLanRoomList(string sixDigitCode)
        {
            var ctx = PublicRoomLobbyContext.Instance;
            var rooms = ctx?.CurrentPublicRooms;
            if (rooms == null || rooms.Count == 0)
                return false;

            for (var i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                var listCode = r.RoomCode?.Trim() ?? "";
                if (listCode.Length != 6)
                    continue;
                if (!string.Equals(listCode, sixDigitCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                JoinRoomByDirectIpv4(r.HostIpv4, r.GamePort);
                return true;
            }

            return false;
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
                nm.OnClientConnectedCallback -= OnClientConnected;
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
                        "No room found by automatic search (UDP blocked or isolated Wi‑Fi). Ask the host for their IPv4 (ipconfig) and the port shown under the room code (often 7777), or use the 6-digit code again.");
                    return;
                }

                transport.SetConnectionData(true, hostIp, hostPort);

                if (nm.StartClient())
                {
                    currentRoomCode = roomCode;
                    Debug.Log($"[LobbyManager] Client connecting to {hostIp}:{hostPort}...");
                    nm.OnClientConnectedCallback -= OnClientConnected;
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
