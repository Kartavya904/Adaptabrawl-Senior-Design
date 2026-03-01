using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Acts as the main conductor for the Pre-Game Setup flow:
    /// Controller Configuration -> Character Selection -> Arena Selection.
    /// Manages the activation and deactivation of the respective UI panels over the network.
    /// </summary>
    public class SetupSceneManager : NetworkBehaviour
    {
        [Header("Setup Panels")]
        [SerializeField] private GameObject controllerConfigPanel;
        [SerializeField] private GameObject characterSelectPanel;
        [SerializeField] private GameObject arenaSelectPanel;

        [Header("Network Data - Controller Phase")]
        public NetworkVariable<int> p1ControllerIndex = new NetworkVariable<int>(0);
        public NetworkVariable<int> p2ControllerIndex = new NetworkVariable<int>(0);
        public NetworkVariable<bool> p1ControllerReady = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> p2ControllerReady = new NetworkVariable<bool>(false);

        [Header("Network Data - Character Phase")]
        public NetworkVariable<int> p1FighterIndex = new NetworkVariable<int>(0);
        public NetworkVariable<int> p2FighterIndex = new NetworkVariable<int>(0);
        public NetworkVariable<bool> p1CharacterReady = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> p2CharacterReady = new NetworkVariable<bool>(false);

        [Header("Network Data - Arena Phase")]
        public NetworkVariable<int> arenaIndex = new NetworkVariable<int>(0);
        public NetworkVariable<bool> p1ArenaReady = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> p2ArenaReady = new NetworkVariable<bool>(false);

        public System.Action OnControllerConfigChanged;
        public System.Action OnCharacterConfigChanged;
        public System.Action OnArenaConfigChanged;

        private void Start()
        {
            ShowControllerConfig();
        }

        public override void OnNetworkSpawn()
        {
            // Controller Listeners
            p1ControllerIndex.OnValueChanged += (oldVal, newVal) => OnControllerConfigChanged?.Invoke();
            p2ControllerIndex.OnValueChanged += (oldVal, newVal) => OnControllerConfigChanged?.Invoke();
            p1ControllerReady.OnValueChanged += (oldVal, newVal) => OnControllerConfigChanged?.Invoke();
            p2ControllerReady.OnValueChanged += (oldVal, newVal) => OnControllerConfigChanged?.Invoke();

            // Character Listeners
            p1FighterIndex.OnValueChanged += (oldVal, newVal) => OnCharacterConfigChanged?.Invoke();
            p2FighterIndex.OnValueChanged += (oldVal, newVal) => OnCharacterConfigChanged?.Invoke();
            p1CharacterReady.OnValueChanged += (oldVal, newVal) => OnCharacterConfigChanged?.Invoke();
            p2CharacterReady.OnValueChanged += (oldVal, newVal) => OnCharacterConfigChanged?.Invoke();

            // Arena Listeners
            arenaIndex.OnValueChanged += (oldVal, newVal) => OnArenaConfigChanged?.Invoke();
            p1ArenaReady.OnValueChanged += (oldVal, newVal) => OnArenaConfigChanged?.Invoke();
            p2ArenaReady.OnValueChanged += (oldVal, newVal) => OnArenaConfigChanged?.Invoke();
        }

        // --- NETWORK RPC COMMANDS (CONTROLLER) ---
        
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleControllerServerRpc(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                if (p1ControllerReady.Value) return; 
                p1ControllerIndex.Value = (p1ControllerIndex.Value + 1) % 2;
            }
            else
            {
                if (p2ControllerReady.Value) return;
                p2ControllerIndex.Value = (p2ControllerIndex.Value + 1) % 2;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleReadyServerRpc(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                p1ControllerReady.Value = !p1ControllerReady.Value;
            }
            else
            {
                p2ControllerReady.Value = !p2ControllerReady.Value;
            }

            if (IsServer && p1ControllerReady.Value && p2ControllerReady.Value)
            {
                AdvanceToCharacterSelectClientRpc(); 
            }
        }

        [ClientRpc]
        private void AdvanceToCharacterSelectClientRpc()
        {
            ShowCharacterSelect();
        }

        // --- NETWORK RPC COMMANDS (CHARACTER) ---

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ChangeCharacterServerRpc(ulong clientId, int direction, int maxFighters)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                if (p1CharacterReady.Value) return; 
                p1FighterIndex.Value = (p1FighterIndex.Value + direction + maxFighters) % maxFighters;
            }
            else
            {
                if (p2CharacterReady.Value) return;
                p2FighterIndex.Value = (p2FighterIndex.Value + direction + maxFighters) % maxFighters;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleCharacterReadyServerRpc(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                p1CharacterReady.Value = !p1CharacterReady.Value;
            }
            else
            {
                p2CharacterReady.Value = !p2CharacterReady.Value;
            }

            if (IsServer && p1CharacterReady.Value && p2CharacterReady.Value)
            {
                AdvanceToArenaSelectClientRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void GoBackToControllerServerRpc()
        {
            // Reset character readies so we don't instantly jump forward again
            p1CharacterReady.Value = false;
            p2CharacterReady.Value = false;
            p1ControllerReady.Value = false;
            p2ControllerReady.Value = false;
            ReturnToControllerClientRpc();
        }

        [ClientRpc]
        private void ReturnToControllerClientRpc()
        {
            ShowControllerConfig();
        }

        [ClientRpc]
        private void AdvanceToArenaSelectClientRpc()
        {
            ShowArenaSelect();
        }

        // --- NETWORK RPC COMMANDS (ARENA & GAME LAUNCH) ---

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ChangeArenaServerRpc(ulong clientId, int direction, int maxArenas)
        {
            if (p1ArenaReady.Value || p2ArenaReady.Value) return;
            arenaIndex.Value = (arenaIndex.Value + direction + maxArenas) % maxArenas;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void CancelArenaOverrideServerRpc(ulong clientId)
        {
            Debug.Log($"[SetupSceneManager] CancelArenaOverrideServerRpc called by Client {clientId}");
            // If anyone hits Cancel, it forces BOTH players to unready so they can discuss.
            p1ArenaReady.Value = false;
            p2ArenaReady.Value = false;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleArenaReadyServerRpc(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                p1ArenaReady.Value = true; // explicitly true, cancellation handled by override
            }
            else
            {
                p2ArenaReady.Value = true;
            }

            // Server automatically launches the game if BOTH players are ready on the exact same arena
            if (IsServer && p1ArenaReady.Value && p2ArenaReady.Value)
            {
                Debug.Log("[SetupSceneManager] Both players agree. Host is loading GameScene...");
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void GoBackToCharacterServerRpc()
        {
            p1ArenaReady.Value = false;
            p2ArenaReady.Value = false;
            p1CharacterReady.Value = false;
            p2CharacterReady.Value = false;
            ReturnToCharacterClientRpc();
        }

        [ClientRpc]
        private void ReturnToCharacterClientRpc()
        {
            ShowCharacterSelect();
        }

        // --- PANEL VISIBILITY CONTROLLERS ---

        public void ShowControllerConfig()
        {
            SetPanels(true, false, false);
        }

        public void ShowCharacterSelect()
        {
            SetPanels(false, true, false);
        }

        public void ShowArenaSelect()
        {
            SetPanels(false, false, true);
        }

        private void SetPanels(bool controller, bool character, bool arena)
        {
            if (controllerConfigPanel != null) controllerConfigPanel.SetActive(controller);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(character);
            if (arenaSelectPanel != null) arenaSelectPanel.SetActive(arena);
        }
    }
}
