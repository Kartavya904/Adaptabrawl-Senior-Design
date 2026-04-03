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
        [SerializeField] private int joinDiscoveryTimeoutMs = 8000;

        [Header("Room Code")]
        private string currentRoomCode = "";

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

            ushort gamePort = transport.ConnectionData.Port;
            // Listen on all interfaces; local host client still connects via loopback.
            transport.SetConnectionData(true, "127.0.0.1", gamePort, "0.0.0.0");

            if (nm.StartHost())
            {
                Debug.Log(
                    $"[LobbyManager] LAN host on 0.0.0.0:{gamePort}, discovery UDP {discoveryPort}. Room code: {currentRoomCode}");
                LanRoomDiscovery.StartHostResponder(currentRoomCode, gamePort, discoveryPort);

                nm.OnClientConnectedCallback += OnClientConnected;
                nm.CustomMessagingManager.RegisterNamedMessageHandler("ReadyState", ReceiveReadyMessage);
                OnWaitingForOpponent?.Invoke();
            }
            else
            {
                isHost = false;
                currentRoomCode = "";
                Debug.LogError("[LobbyManager] StartHost failed.");
            }
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

        public void JoinRoom(string roomCode)
        {
            roomCode = roomCode?.Trim() ?? "";
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                OnRoomJoinFailed?.Invoke("Invalid Room Code Format.");
                return;
            }

            _joinCts?.Cancel();
            _joinCts?.Dispose();
            _joinCts = new CancellationTokenSource();
            JoinRoomAsync(roomCode, _joinCts.Token);
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
                    OnRoomJoinFailed?.Invoke(
                        "Could not find that room on this network. Use the same Wi‑Fi, check the code, and make sure the host created the room.");
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
            OnDisconnected?.Invoke();
            matchIsStarting = false;

            Debug.Log("[LobbyManager] Network connection completely shut down.");
        }

        public string CurrentRoomCode => currentRoomCode;
        public bool IsHost => isHost;
        public bool IsReady => isReady;
        public bool OpponentReady => opponentReady;
    }
}
