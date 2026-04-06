using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using Adaptabrawl.Gameplay;

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
        [SerializeField] private GameObject localJoinPanel;
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
        public System.Action OnArenaCountdownRequested;

        private void Awake()
        {
            if (GetComponent<SetupFlowBackInput>() == null)
                gameObject.AddComponent<SetupFlowBackInput>();
        }

        /// <summary>3,2,1 while waiting to open character select; 0 = finished / hide.</summary>
        public System.Action<int> OnControllerToCharacterCountdownTick;
        /// <summary>3,2,1 while waiting to open arena select; 0 = finished / hide.</summary>
        public System.Action<int> OnCharacterToArenaCountdownTick;

        public bool ControllerPhaseCountdownActive { get; private set; }
        public bool CharacterPhaseCountdownActive { get; private set; }

        private void Start()
        {
            // "Change Characters" rematch: skip controller config, jump straight to character select
            if (MatchResultsData.rematchSkipToCharacterSelect)
            {
                MatchResultsData.rematchSkipToCharacterSelect = false;
                _localP1ControllerIndex = CharacterSelectData.finalP1ControllerIndex;
                _localP2ControllerIndex = CharacterSelectData.finalP2ControllerIndex;
                LobbyContext.Instance?.SetInputDevices(_localP1ControllerIndex, _localP2ControllerIndex);

                if (LobbyContext.Instance != null)
                {
                    _localP1FighterIndex = LobbyContext.Instance.p1LastFighterIndex;
                    _localP2FighterIndex = LobbyContext.Instance.p2LastFighterIndex;
                }

                ResetCharacterLockInStateForCharacterPhase();
                ShowCharacterSelect();
                TryPushRematchFighterIndicesToNetwork();
                return;
            }

            if (LobbyContext.CurrentMatchIsLocal())
            {
                ShowLocalJoin();
            }
            else
            {
                ShowControllerConfig();
            }
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

        // --- NETWORK RPC COMMANDS (LOCAL JOIN) ---
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ConfirmLocalPlayer2JoinedServerRpc()
        {
            if (IsServer)
            {
                AdvanceToControllerConfigClientRpc();
            }
        }

        [ClientRpc]
        private void AdvanceToControllerConfigClientRpc()
        {
            StartCoroutine(AdvanceToControllerConfigDelay());
        }

        private IEnumerator AdvanceToControllerConfigDelay()
        {
            // Match local join + other setup phases (3s before the next panel).
            yield return new WaitForSeconds(3.0f);
            ShowControllerConfig();
        }

        // --- NETWORK RPC COMMANDS (CONTROLLER) ---

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleControllerServerRpc(ulong clientId, int targetPlayer = 0)
        {
            if (targetPlayer == 1 || (targetPlayer == 0 && clientId == NetworkManager.ServerClientId))
            {
                if (p1ControllerReady.Value) return;
                int next = (p1ControllerIndex.Value + 1) % 2;
                if (CanApplyControllerChoice(next, p2ControllerIndex.Value))
                    p1ControllerIndex.Value = next;
            }

            if (targetPlayer == 2 || (targetPlayer == 0 && clientId != NetworkManager.ServerClientId))
            {
                if (p2ControllerReady.Value) return;
                int next = (p2ControllerIndex.Value + 1) % 2;
                if (CanApplyControllerChoice(p1ControllerIndex.Value, next))
                    p2ControllerIndex.Value = next;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleReadyServerRpc(ulong clientId, int targetPlayer = 0)
        {
            if (targetPlayer == 1 || (targetPlayer == 0 && clientId == NetworkManager.ServerClientId))
            {
                p1ControllerReady.Value = !p1ControllerReady.Value;
            }

            if (targetPlayer == 2 || (targetPlayer == 0 && clientId != NetworkManager.ServerClientId))
            {
                p2ControllerReady.Value = !p2ControllerReady.Value;
            }

            if (IsServer && p1ControllerReady.Value && p2ControllerReady.Value)
            {
                LobbyContext.Instance?.SetInputDevices(p1ControllerIndex.Value, p2ControllerIndex.Value);
                CharacterSelectData.finalP1ControllerIndex = p1ControllerIndex.Value;
                CharacterSelectData.finalP2ControllerIndex = p2ControllerIndex.Value;
                BeginControllerToCharacterCountdownClientRpc();
            }
        }

        [ClientRpc]
        private void BeginControllerToCharacterCountdownClientRpc()
        {
            StartCoroutine(CoControllerToCharacterCountdown());
        }

        private IEnumerator CoControllerToCharacterCountdown()
        {
            if (ControllerPhaseCountdownActive) yield break;
            ControllerPhaseCountdownActive = true;
            for (int i = 3; i > 0; i--)
            {
                OnControllerToCharacterCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
            OnControllerToCharacterCountdownTick?.Invoke(0);
            ResetCharacterLockInStateForCharacterPhase();
            ShowCharacterSelect();
            ControllerPhaseCountdownActive = false;
        }

        // --- LOCAL (NON-NETWORKED) FALLBACKS ---
        // Used when NetworkManager is not running (standalone local play).

        // Mirror fields for local play (NetworkVariables won't sync without a server)
        private int _localP1ControllerIndex;
        private int _localP2ControllerIndex;
        private bool _localP1ControllerReady;
        private bool _localP2ControllerReady;

        public void LocalToggleController(int targetPlayer)
        {
            if (targetPlayer == 1 && !_localP1ControllerReady)
            {
                int next = (_localP1ControllerIndex + 1) % 2;
                if (CanApplyControllerChoice(next, _localP2ControllerIndex))
                    _localP1ControllerIndex = next;
            }
            else if (targetPlayer == 2 && !_localP2ControllerReady)
            {
                int next = (_localP2ControllerIndex + 1) % 2;
                if (CanApplyControllerChoice(_localP1ControllerIndex, next))
                    _localP2ControllerIndex = next;
            }
            OnControllerConfigChanged?.Invoke();
        }

        public void LocalToggleReady(int targetPlayer)
        {
            if (targetPlayer == 1) _localP1ControllerReady = !_localP1ControllerReady;
            if (targetPlayer == 2) _localP2ControllerReady = !_localP2ControllerReady;

            OnControllerConfigChanged?.Invoke();

            // Both ready locally → persist final device choices, 3s countdown, then character select
            if (_localP1ControllerReady && _localP2ControllerReady)
            {
                LobbyContext.Instance?.SetInputDevices(_localP1ControllerIndex, _localP2ControllerIndex);
                CharacterSelectData.finalP1ControllerIndex = _localP1ControllerIndex;
                CharacterSelectData.finalP2ControllerIndex = _localP2ControllerIndex;
                StartCoroutine(CoLocalControllerToCharacterCountdown());
            }
        }

        private IEnumerator CoLocalControllerToCharacterCountdown()
        {
            if (ControllerPhaseCountdownActive) yield break;
            ControllerPhaseCountdownActive = true;
            for (int i = 3; i > 0; i--)
            {
                OnControllerToCharacterCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
            OnControllerToCharacterCountdownTick?.Invoke(0);
            ResetCharacterLockInStateForCharacterPhase();
            ShowCharacterSelect();
            ControllerPhaseCountdownActive = false;
        }

        // Accessors so ControllerConfigUI can read local state the same way as networked
        public int LocalP1ControllerIndex => _localP1ControllerIndex;
        public int LocalP2ControllerIndex => _localP2ControllerIndex;
        public bool LocalP1ControllerReady => _localP1ControllerReady;
        public bool LocalP2ControllerReady => _localP2ControllerReady;

        public bool CanToggleControllerChoice(int playerNumber, bool networked)
        {
            int p1 = networked ? p1ControllerIndex.Value : _localP1ControllerIndex;
            int p2 = networked ? p2ControllerIndex.Value : _localP2ControllerIndex;

            if (playerNumber == 1)
                return CanApplyControllerChoice((p1 + 1) % 2, p2);
            if (playerNumber == 2)
                return CanApplyControllerChoice(p1, (p2 + 1) % 2);

            return false;
        }

        // Mirror fields for character phase when NetworkManager is not running
        private int _localP1FighterIndex;
        private int _localP2FighterIndex;
        private bool _localP1CharacterReady;
        private bool _localP2CharacterReady;

        public int LocalP1FighterIndex => _localP1FighterIndex;
        public int LocalP2FighterIndex => _localP2FighterIndex;
        public bool LocalP1CharacterReady => _localP1CharacterReady;
        public bool LocalP2CharacterReady => _localP2CharacterReady;

        public void LocalChangeCharacter(int direction, int maxFighters, int targetPlayer)
        {
            if (maxFighters <= 0) return;
            if (targetPlayer == 1 && !_localP1CharacterReady)
                _localP1FighterIndex = (_localP1FighterIndex + direction + maxFighters) % maxFighters;
            if (targetPlayer == 2 && !_localP2CharacterReady)
                _localP2FighterIndex = (_localP2FighterIndex + direction + maxFighters) % maxFighters;
            OnCharacterConfigChanged?.Invoke();
        }

        /// <summary>
        /// Keeps stored indices valid if the fighter roster size changed (e.g. after a build update).
        /// </summary>
        public void ClampLocalFighterIndicesToRoster(int fighterCount)
        {
            if (fighterCount <= 0) return;
            int max = fighterCount - 1;
            _localP1FighterIndex = Mathf.Clamp(_localP1FighterIndex, 0, max);
            _localP2FighterIndex = Mathf.Clamp(_localP2FighterIndex, 0, max);
            OnCharacterConfigChanged?.Invoke();
        }

        /// <summary>
        /// Server-only: clamp networked fighter indices to roster size.
        /// </summary>
        public void ServerClampFighterIndicesToRoster(int fighterCount)
        {
            if (!IsServer || fighterCount <= 0) return;
            int max = fighterCount - 1;
            p1FighterIndex.Value = Mathf.Clamp(p1FighterIndex.Value, 0, max);
            p2FighterIndex.Value = Mathf.Clamp(p2FighterIndex.Value, 0, max);
        }

        private void TryPushRematchFighterIndicesToNetwork()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || LobbyContext.Instance == null)
                return;
            var ctx = LobbyContext.Instance;
            if (IsSpawned)
            {
                p1FighterIndex.Value = ctx.p1LastFighterIndex;
                p2FighterIndex.Value = ctx.p2LastFighterIndex;
            }
            else
            {
                StartCoroutine(PushRematchFighterIndicesWhenSpawned(ctx));
            }
        }

        private IEnumerator PushRematchFighterIndicesWhenSpawned(LobbyContext ctx)
        {
            yield return null;
            for (int i = 0; i < 120 && !IsSpawned; i++)
                yield return null;
            if (!IsSpawned || ctx == null) yield break;
            p1FighterIndex.Value = ctx.p1LastFighterIndex;
            p2FighterIndex.Value = ctx.p2LastFighterIndex;
        }

        public void LocalToggleCharacterReady(int targetPlayer)
        {
            if (targetPlayer == 1) _localP1CharacterReady = !_localP1CharacterReady;
            if (targetPlayer == 2) _localP2CharacterReady = !_localP2CharacterReady;
            OnCharacterConfigChanged?.Invoke();
            if (_localP1CharacterReady && _localP2CharacterReady)
                StartCoroutine(CoLocalCharacterToArenaCountdown());
        }

        private IEnumerator CoLocalCharacterToArenaCountdown()
        {
            if (CharacterPhaseCountdownActive) yield break;
            CharacterPhaseCountdownActive = true;
            for (int i = 3; i > 0; i--)
            {
                OnCharacterToArenaCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
            OnCharacterToArenaCountdownTick?.Invoke(0);
            ShowArenaSelect();
            CharacterPhaseCountdownActive = false;
        }

        /// <summary>
        /// Called when Back is pressed on Character Select in local play (no network).
        /// </summary>
        public void GoBackToControllerLocal()
        {
            _localP1CharacterReady = false;
            _localP2CharacterReady = false;
            ShowControllerConfig();
        }

        /// <summary>
        /// Called by LocalJoinUI right after P2 joins so ControllerConfig
        /// shows the correct pre-filled device types (0=Keyboard, 1=Gamepad).
        /// </summary>
        public void SetLocalDevices(int p1Index, int p2Index)
        {
            _localP1ControllerIndex = p1Index;
            _localP2ControllerIndex = p2Index;
            OnControllerConfigChanged?.Invoke();
        }

        // Mirror fields for arena phase when NetworkManager is not running
        private int _localArenaIndex;
        private bool _localP1ArenaReady;
        private bool _localP2ArenaReady;

        public int LocalArenaIndex => _localArenaIndex;
        public bool LocalP1ArenaReady => _localP1ArenaReady;
        public bool LocalP2ArenaReady => _localP2ArenaReady;

        /// <summary>
        /// Local-only arena change (no networking). Used for pure local matches.
        /// </summary>
        public void LocalChangeArena(int direction, int maxArenas)
        {
            if (maxArenas <= 0) return;
            if (_localP1ArenaReady || _localP2ArenaReady) return;

            _localArenaIndex = (_localArenaIndex + direction + maxArenas) % maxArenas;
            OnArenaConfigChanged?.Invoke();
        }

        /// <summary>
        /// Local-only ready toggle for a specific player. Countdown starts when both are ready.
        /// </summary>
        public void LocalToggleArenaReady(int targetPlayer)
        {
            if (targetPlayer == 1)
                _localP1ArenaReady = !_localP1ArenaReady;
            else if (targetPlayer == 2)
                _localP2ArenaReady = !_localP2ArenaReady;

            OnArenaConfigChanged?.Invoke();

            if (_localP1ArenaReady && _localP2ArenaReady)
            {
                // Start local countdown (no networking).
                BeginArenaCountdownLocal();
            }
        }

        /// <summary>
        /// Local-only cancel: clears both ready flags so arena can be changed again.
        /// </summary>
        public void LocalCancelArenaOverride()
        {
            _localP1ArenaReady = false;
            _localP2ArenaReady = false;
            OnArenaConfigChanged?.Invoke();
        }

        /// <summary>
        /// Local-only back from arena to character selection.
        /// </summary>
        public void GoBackToCharacterLocal()
        {
            _localP1ArenaReady = false;
            _localP2ArenaReady = false;
            ShowCharacterSelect();
        }

        /// <summary>
        /// Called by the Back button on ControllerConfigPanel in local play.
        /// Resets local ready/device state and returns to the LocalJoin panel.
        /// </summary>
        public void GoBackToLocalJoin()
        {
            // Reset all local mirror state
            _localP1ControllerIndex = 0;
            _localP2ControllerIndex = 0;
            _localP1ControllerReady = false;
            _localP2ControllerReady = false;

            // Return to Main Menu
            UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
        }



        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ChangeCharacterServerRpc(ulong clientId, int direction, int maxFighters, int targetPlayer = 0)
        {
            if (targetPlayer == 1 || (targetPlayer == 0 && clientId == NetworkManager.ServerClientId))
            {
                if (p1CharacterReady.Value) return;
                p1FighterIndex.Value = (p1FighterIndex.Value + direction + maxFighters) % maxFighters;
            }

            if (targetPlayer == 2 || (targetPlayer == 0 && clientId != NetworkManager.ServerClientId))
            {
                if (p2CharacterReady.Value) return;
                p2FighterIndex.Value = (p2FighterIndex.Value + direction + maxFighters) % maxFighters;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ToggleCharacterReadyServerRpc(ulong clientId, int targetPlayer = 0)
        {
            if (targetPlayer == 1 || (targetPlayer == 0 && clientId == NetworkManager.ServerClientId))
            {
                p1CharacterReady.Value = !p1CharacterReady.Value;
            }

            if (targetPlayer == 2 || (targetPlayer == 0 && clientId != NetworkManager.ServerClientId))
            {
                p2CharacterReady.Value = !p2CharacterReady.Value;
            }

            if (IsServer && p1CharacterReady.Value && p2CharacterReady.Value)
            {
                BeginCharacterToArenaCountdownClientRpc();
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
        private void BeginCharacterToArenaCountdownClientRpc()
        {
            StartCoroutine(CoCharacterToArenaCountdown());
        }

        private IEnumerator CoCharacterToArenaCountdown()
        {
            if (CharacterPhaseCountdownActive) yield break;
            CharacterPhaseCountdownActive = true;
            for (int i = 3; i > 0; i--)
            {
                OnCharacterToArenaCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }
            OnCharacterToArenaCountdownTick?.Invoke(0);
            ShowArenaSelect();
            CharacterPhaseCountdownActive = false;
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
                p1ArenaReady.Value = !p1ArenaReady.Value;
            }
            else
            {
                p2ArenaReady.Value = !p2ArenaReady.Value;
            }

            // Server triggers a countdown when BOTH players are ready on the same arena.
            if (IsServer && p1ArenaReady.Value && p2ArenaReady.Value)
            {
                Debug.Log("[SetupSceneManager] Both players agree. Starting arena countdown...");
                BeginArenaCountdownClientRpc();
            }
        }

        private void BeginArenaCountdownLocal()
        {
            OnArenaCountdownRequested?.Invoke();
        }

        [ClientRpc]
        private void BeginArenaCountdownClientRpc()
        {
            OnArenaCountdownRequested?.Invoke();
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

        public void ShowLocalJoin()
        {
            SetPanels(true, false, false, false);
        }

        public void ShowControllerConfig()
        {
            SetPanels(false, true, false, false);
        }

        public void ShowCharacterSelect()
        {
            SetPanels(false, false, true, false);
        }

        /// <summary>
        /// Clears lock-in when starting a <b>new</b> character phase (after controller ready countdown or rematch skip).
        /// Not used when returning from arena — players keep their picks until they unlock.
        /// </summary>
        private void ResetCharacterLockInStateForCharacterPhase()
        {
            _localP1CharacterReady = false;
            _localP2CharacterReady = false;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer && IsSpawned)
            {
                p1CharacterReady.Value = false;
                p2CharacterReady.Value = false;
            }
            OnCharacterConfigChanged?.Invoke();
        }

        public void ShowArenaSelect()
        {
            SetPanels(false, false, false, true);
        }

        private void SetPanels(bool join, bool controller, bool character, bool arena)
        {
            if (localJoinPanel != null) localJoinPanel.SetActive(join);
            if (controllerConfigPanel != null) controllerConfigPanel.SetActive(controller);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(character);
            if (arenaSelectPanel != null) arenaSelectPanel.SetActive(arena);
        }

        private static bool CanApplyControllerChoice(int p1Device, int p2Device)
        {
            return LobbyContext.IsControllerConfigurationValid(p1Device, p2Device);
        }

        public bool IsLocalJoinPanelActive => localJoinPanel != null && localJoinPanel.activeInHierarchy;
        public bool IsControllerConfigPanelActive => controllerConfigPanel != null && controllerConfigPanel.activeInHierarchy;
        public bool IsCharacterSelectPanelActive => characterSelectPanel != null && characterSelectPanel.activeInHierarchy;
        public bool IsArenaSelectPanelActive => arenaSelectPanel != null && arenaSelectPanel.activeInHierarchy;
    }
}
