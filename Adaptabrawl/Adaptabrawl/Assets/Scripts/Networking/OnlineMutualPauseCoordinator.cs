using Unity.Netcode;
using UnityEngine;
using Adaptabrawl.UI;

namespace Adaptabrawl.Networking
{
    /// <summary>
    /// Server-authoritative mutual pause for online matches. Place on the same in-scene NetworkObject as
    /// <see cref="Input.OnlineTwoPlayerInputHandler"/> (e.g. OnlineSyncManager).
    /// </summary>
    public class OnlineMutualPauseCoordinator : NetworkBehaviour
    {
        private const byte P1Bit = 1;
        private const byte P2Bit = 2;

        private NetworkVariable<byte> _pauseRequests = new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> _menuOpen = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public bool MenuOpen => _menuOpen.Value;

        public override void OnNetworkSpawn()
        {
            _pauseRequests.OnValueChanged += OnRequestsChanged;
            _menuOpen.OnValueChanged += OnMenuChanged;
            PushUi(_pauseRequests.Value, _menuOpen.Value);
        }

        public override void OnNetworkDespawn()
        {
            _pauseRequests.OnValueChanged -= OnRequestsChanged;
            _menuOpen.OnValueChanged -= OnMenuChanged;
            if (MatchPauseController.Instance != null)
                MatchPauseController.Instance.ApplyOnlineCoordinatorState(false, 0);
        }

        private void OnRequestsChanged(byte _, byte __) => PushUi(_pauseRequests.Value, _menuOpen.Value);
        private void OnMenuChanged(bool _, bool __) => PushUi(_pauseRequests.Value, _menuOpen.Value);

        private static void PushUi(byte requests, bool menuOpen)
        {
            if (MatchPauseController.Instance != null)
                MatchPauseController.Instance.ApplyOnlineCoordinatorState(menuOpen, requests);
        }

        /// <summary>Local player pressed pause / Options: toggle intent or no-op while menu is open (resume uses Esc / button).</summary>
        public void TogglePauseIntentFromLocalPlayer()
        {
            if (!IsSpawned) return;
            TogglePauseIntentServerRpc();
        }

        public void RequestResumeFromLocalPlayer()
        {
            if (!IsSpawned) return;
            RequestResumeServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void TogglePauseIntentServerRpc(RpcParams rpcParams = default)
        {
            if (_menuOpen.Value)
                return;

            ulong sender = rpcParams.Receive.SenderClientId;
            bool isHostPlayer = sender == NetworkManager.ServerClientId;
            byte bit = isHostPlayer ? P1Bit : P2Bit;

            if ((_pauseRequests.Value & bit) != 0)
                _pauseRequests.Value = (byte)(_pauseRequests.Value & ~bit);
            else
                _pauseRequests.Value = (byte)(_pauseRequests.Value | bit);

            if ((_pauseRequests.Value & (P1Bit | P2Bit)) == (P1Bit | P2Bit))
            {
                _menuOpen.Value = true;
                _pauseRequests.Value = 0;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestResumeServerRpc()
        {
            _menuOpen.Value = false;
            _pauseRequests.Value = 0;
        }
    }
}
