using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
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
        
        [Header("Room Code")]
        private string currentRoomCode = "";
        
        [Header("Player States")]
        private bool isHost = false;
        private bool isReady = false;
        private bool opponentReady = false;
        private bool matchIsStarting = false;

        
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
            
            if (NetworkManager.Singleton.StartHost())
            {
                Debug.Log($"[LobbyManager] Local Host started! Room Code: {currentRoomCode}");
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                
                // Host registers the custom message receiver early
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ReadyState", ReceiveReadyMessage);
                
                // Host is now waiting for someone to join
                OnWaitingForOpponent?.Invoke();
            }
        }
        
        private string GenerateRandomCode()
        {
            // Generate a random 6-digit number as a string
            return UnityEngine.Random.Range(100000, 999999).ToString();
        }
        
        public void JoinRoom(string roomCode)
        {
            // For now, since it's local host, we just make sure they typed "something" 6 digits long
            // In a real relay server, this code would be used to find the specific host match.
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                OnRoomJoinFailed?.Invoke("Invalid Room Code Format.");
                return;
            }
            
            currentRoomCode = roomCode;
            
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("[LobbyManager] Client attempting to connect to 127.0.0.1...");
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
            else
            {
                OnRoomJoinFailed?.Invoke("Failed to start client connection.");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                // We are the Host. Did someone else join?
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log("[LobbyManager] An opponent joined our room!");
                    OnRoomJoined?.Invoke(); 
                }
            }
            else
            {
                // We are the Client. Did we successfully connect?
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log("[LobbyManager] Successfully connected to the Host!");
                    OnRoomJoined?.Invoke();

                    // Client registers custom message receiver once connected
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("ReadyState", ReceiveReadyMessage);
                }
            }
        }
        
        public void SetReady(bool ready)
        {
            isReady = ready;
            OnPlayerReady?.Invoke();
            
            SendReadyState(ready);
            
            if (isReady && opponentReady) StartMatch();
        }

        private void SendReadyState(bool ready)
        {
            if (!NetworkManager.Singleton.IsListening) return;

            using (var writer = new FastBufferWriter(1, Allocator.Temp))
            {
                writer.WriteValueSafe(ready);
                
                if (NetworkManager.Singleton.IsServer)
                {
                    // Host sends its ready state to the Client
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ReadyState", writer);
                }
                else
                {
                    // Client sends its ready state back to the Host (ServerClientId)
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("ReadyState", NetworkManager.ServerClientId, writer);
                }
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
            if (ready) OnOpponentReady?.Invoke();
            
            if (isReady && opponentReady) StartMatch();
        }
        
        public void StartMatch()
        {
            if (matchIsStarting) return; // Ignore if we are already loading!
            matchIsStarting = true;       // Lock the door!
            
            OnMatchStart?.Invoke();
            
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log("[LobbyManager] Host loading SetupScene...");
                NetworkManager.Singleton.SceneManager.LoadScene("SetupScene", LoadSceneMode.Single);
            }
        }

        
        public void Disconnect()
        {
            if (NetworkManager.Singleton != null)
            {
                // Force the NetworkManager to completely shut down and reset
                NetworkManager.Singleton.Shutdown();
            }
            
            isHost = false;
            isReady = false;
            opponentReady = false;
            currentRoomCode = "";
            OnDisconnected?.Invoke();
            matchIsStarting = false;

            Debug.Log("[LobbyManager] Network connection completely shut down.");
        }

        
        // Public getters
        public string CurrentRoomCode => currentRoomCode;
        public bool IsHost => isHost;
        public bool IsReady => isReady;
        public bool OpponentReady => opponentReady;
    }
}
