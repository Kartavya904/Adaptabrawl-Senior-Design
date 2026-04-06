using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Escape + controller Circle/B: unready players in order, then go to the previous setup screen.
    /// Attach to the same object as <see cref="SetupSceneManager"/> or any active object in SetupScene.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SetupFlowBackInput : MonoBehaviour
    {
        private SetupSceneManager _setup;
        private ArenaSelectUI _arenaUi;

        private void LateUpdate()
        {
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame())
                return;
            if (BackInputUtility.IsTextInputFocused())
                return;

            _setup ??= FindFirstObjectByType<SetupSceneManager>();
            if (_setup == null) return;

            if (_setup.IsLocalJoinPanelActive)
            {
                SceneManager.LoadScene("StartScene");
                return;
            }

            if (_setup.IsControllerConfigPanelActive)
            {
                HandleControllerConfigBack();
                return;
            }

            if (_setup.IsCharacterSelectPanelActive)
            {
                HandleCharacterSelectBack();
                return;
            }

            if (_setup.IsArenaSelectPanelActive)
            {
                HandleArenaBack();
            }
        }

        private void HandleControllerConfigBack()
        {
            if (_setup.ControllerPhaseCountdownActive) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            int p1Idx = networked ? _setup.p1ControllerIndex.Value : _setup.LocalP1ControllerIndex;
            int p2Idx = networked ? _setup.p2ControllerIndex.Value : _setup.LocalP2ControllerIndex;
            bool r1 = networked ? _setup.p1ControllerReady.Value : _setup.LocalP1ControllerReady;
            bool r2 = networked ? _setup.p2ControllerReady.Value : _setup.LocalP2ControllerReady;

            if (r1 && WasBackPressedForPlayer(1, p1Idx, p2Idx))
            {
                RequestControllerToggleReady(1, networked);
                return;
            }

            if (r2 && WasBackPressedForPlayer(2, p1Idx, p2Idx))
            {
                RequestControllerToggleReady(2, networked);
                return;
            }

            if (!r1 && !r2 && CharacterSelectData.isLocalMatch)
                _setup.GoBackToLocalJoin();
        }

        private void RequestControllerToggleReady(int player, bool networked)
        {
            if (networked)
            {
                var nm = NetworkManager.Singleton;
                if (nm == null) return;
                if (player == 1 && !nm.IsServer) return;
                if (player == 2 && nm.IsServer) return;
                _setup.ToggleReadyServerRpc(nm.LocalClientId, player);
            }
            else
            {
                _setup.LocalToggleReady(player);
            }
        }

        private void HandleCharacterSelectBack()
        {
            if (_setup.CharacterPhaseCountdownActive) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool isLocal = CharacterSelectData.isLocalMatch;
            bool isServer = networked && NetworkManager.Singleton.IsServer;
            bool isClientOnly = networked && !isServer;

            int p1Dev = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                : (networked ? _setup.p1ControllerIndex.Value : _setup.LocalP1ControllerIndex);
            int p2Dev = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                : (networked ? _setup.p2ControllerIndex.Value : _setup.LocalP2ControllerIndex);

            bool r1 = networked ? _setup.p1CharacterReady.Value : _setup.LocalP1CharacterReady;
            bool r2 = networked ? _setup.p2CharacterReady.Value : _setup.LocalP2CharacterReady;

            if (r1 && WasBackPressedForPlayer(1, p1Dev, p2Dev))
            {
                RequestCharacterToggleReady(1, networked, isServer, isClientOnly);
                return;
            }

            if (r2 && WasBackPressedForPlayer(2, p1Dev, p2Dev))
            {
                RequestCharacterToggleReady(2, networked, isServer, isClientOnly);
                return;
            }

            if (!r1 && !r2)
            {
                if (networked && !isServer) return;
                if (networked)
                    _setup.GoBackToControllerServerRpc();
                else
                    _setup.GoBackToControllerLocal();
            }
        }

        private void RequestCharacterToggleReady(int player, bool networked, bool isServer, bool isClientOnly)
        {
            if (networked)
            {
                var nm = NetworkManager.Singleton;
                if (nm == null) return;
                if (player == 1 && !isServer) return;
                if (player == 2 && !isClientOnly) return;
                _setup.ToggleCharacterReadyServerRpc(nm.LocalClientId, player);
            }
            else
            {
                _setup.LocalToggleCharacterReady(player);
            }
        }

        private void HandleArenaBack()
        {
            _arenaUi ??= FindFirstObjectByType<ArenaSelectUI>();
            if (_arenaUi != null && _arenaUi.IsCountdownActive) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool isServer = networked && NetworkManager.Singleton.IsServer;
            bool isClientOnly = networked && !isServer;

            int p1Dev = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice
                : (networked ? _setup.p1ControllerIndex.Value : _setup.LocalP1ControllerIndex);
            int p2Dev = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice
                : (networked ? _setup.p2ControllerIndex.Value : _setup.LocalP2ControllerIndex);

            bool p1R = networked ? _setup.p1ArenaReady.Value : _setup.LocalP1ArenaReady;
            bool p2R = networked ? _setup.p2ArenaReady.Value : _setup.LocalP2ArenaReady;

            if (p1R && WasBackPressedForPlayer(1, p1Dev, p2Dev))
            {
                RequestArenaToggleReady(networked, isServer);
                return;
            }

            if (p2R && WasBackPressedForPlayer(2, p1Dev, p2Dev))
            {
                RequestArenaToggleReady(networked, isClientOnly);
                return;
            }

            if (!p1R && !p2R)
            {
                if (networked && !isServer) return;
                if (networked)
                    _setup.GoBackToCharacterServerRpc();
                else
                    _setup.GoBackToCharacterLocal();
            }
        }

        private void RequestArenaToggleReady(bool networked, bool asHostSide)
        {
            if (networked)
            {
                var nm = NetworkManager.Singleton;
                if (nm == null) return;
                ulong id = asHostSide ? NetworkManager.ServerClientId : nm.LocalClientId;
                if (asHostSide && !nm.IsServer) return;
                if (!asHostSide && nm.IsServer) return;
                _setup.ToggleArenaReadyServerRpc(id);
            }
            else
            {
                int p = asHostSide ? 1 : 2;
                _setup.LocalToggleArenaReady(p);
            }
        }

        private static bool WasBackPressedForPlayer(int playerNumber, int p1Device, int p2Device)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            if (playerNumber == 1 && p1Device == 1)
                return bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.BackCancel, LobbyContext.GetGamepadListIndexForPlayer(1, p1Device, p2Device));

            if (playerNumber == 2 && p2Device == 1)
                return bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.BackCancel, LobbyContext.GetGamepadListIndexForPlayer(2, p1Device, p2Device));

            bool dualKeyboard = LobbyContext.IsDualKeyboardMode(p1Device, p2Device);
            var profile = ControlBindingProfileResolver.ResolveGlobalKeyboardProfile(playerNumber, dualKeyboard);
            return bindings.WasActionPressedThisFrame(profile, ControlActionId.BackCancel);
        }
    }
}
