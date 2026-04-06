using UnityEngine;
using Unity.Netcode;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;

namespace Adaptabrawl.Input
{
    public class OnlineTwoPlayerInputHandler : NetworkBehaviour
    {
        [Header("Player References")]
        [Tooltip("Direct reference to Player 1's GameObject (Host controls this). Drag Player_DualBlades here.")]
        public GameObject player1Object;

        [Tooltip("Direct reference to Player 2's GameObject (Client controls this). Drag Player_Hammer here.")]
        public GameObject player2Object;

        private PlayerController_Platform p1Controller;
        private PlayerController_Platform p2Controller;

        [Header("Network Smoothing")]
        public float positionLerpSpeed = 15f;

        private Vector3 targetP1Position;
        private Quaternion targetP1Rotation;
        private Vector3 targetP2Position;
        private Quaternion targetP2Rotation;

        private bool rolesApplied = false;

        void Start()
        {
            // Destroy offline conflicting input handlers
            PlayerInputHandler[] localInputs = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
            foreach (var input in localInputs)
                Destroy(input);

            TwoPlayerInputHandler[] dualInputs = FindObjectsByType<TwoPlayerInputHandler>(FindObjectsSortMode.None);
            foreach (var input in dualInputs)
                Destroy(input);

            InitializePlayers();

            if (IsSpawned && !rolesApplied)
                ApplyNetworkRoles();
        }

        public void InitializePlayers()
        {
            if (player1Object != null)
            {
                p1Controller = player1Object.GetComponentInChildren<PlayerController_Platform>();
                if (p1Controller != null)
                {
                    targetP1Position = p1Controller.transform.position;
                    targetP1Rotation = p1Controller.transform.rotation;
                }
            }

            if (player2Object != null)
            {
                p2Controller = player2Object.GetComponentInChildren<PlayerController_Platform>();
                if (p2Controller != null)
                {
                    targetP2Position = p2Controller.transform.position;
                    targetP2Rotation = p2Controller.transform.rotation;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (p1Controller == null || p2Controller == null)
                InitializePlayers();
            ApplyNetworkRoles();
        }

        private void ApplyNetworkRoles()
        {
            if (p1Controller == null || p2Controller == null) return;

            if (IsServer)
            {
                p1Controller.isNetworkControlled = false;
                p2Controller.isNetworkControlled = true;
            }
            else if (IsClient)
            {
                p1Controller.isNetworkControlled = true;
                p2Controller.isNetworkControlled = false;
            }

            rolesApplied = true;
        }

        void Update()
        {
            if (!IsSpawned || p1Controller == null || p2Controller == null) return;

            if (IsServer)
            {
                SendP1InputsToClients();
                SmoothPlayer(p2Controller, targetP2Position, targetP2Rotation);
            }
            else if (IsClient)
            {
                SendP2InputsToServer();
                SmoothPlayer(p1Controller, targetP1Position, targetP1Rotation);
            }
        }

        private void SendP1InputsToClients()
        {
            SampleConfiguredPlayer(1, out bool left, out bool right, out bool crouch, out bool sprint,
                out bool jump, out bool attack, out bool block, out bool blockDown, out bool blockUp, out bool dodge);

            UpdateP1StateClientRpc(
                p1Controller.transform.position,
                p1Controller.transform.rotation,
                left, right, crouch, sprint, jump, attack, block, blockDown, blockUp, dodge
            );
        }

        [ClientRpc]
        private void UpdateP1StateClientRpc(
            Vector3 position, Quaternion rotation,
            bool left, bool right, bool crouch, bool sprint,
            bool jump, bool attack, bool block, bool blockDown, bool blockUp, bool dodge)
        {
            if (IsServer) return;

            targetP1Position = position;
            targetP1Rotation = rotation;

            if (p1Controller == null) return;

            p1Controller.netLeft   = left;
            p1Controller.netRight  = right;
            p1Controller.netCrouch = crouch;
            p1Controller.netSprint = sprint;

            if (jump)      p1Controller.netJump      = true;
            if (attack)    p1Controller.netAttack     = true;
            if (blockDown) p1Controller.netBlockDown  = true;
            if (blockUp)   p1Controller.netBlockUp    = true;
            if (dodge)     p1Controller.netDodge      = true;
            p1Controller.netBlock = block;
        }

        private void SendP2InputsToServer()
        {
            SampleConfiguredPlayer(2, out bool left, out bool right, out bool crouch, out bool sprint,
                out bool jump, out bool attack, out bool block, out bool blockDown, out bool blockUp, out bool dodge);

            UpdateP2StateServerRpc(
                p2Controller.transform.position,
                p2Controller.transform.rotation,
                left, right, crouch, sprint, jump, attack, block, blockDown, blockUp, dodge
            );
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void UpdateP2StateServerRpc(
            Vector3 position, Quaternion rotation,
            bool left, bool right, bool crouch, bool sprint,
            bool jump, bool attack, bool block, bool blockDown, bool blockUp, bool dodge)
        {
            targetP2Position = position;
            targetP2Rotation = rotation;

            if (p2Controller == null) return;

            p2Controller.netLeft   = left;
            p2Controller.netRight  = right;
            p2Controller.netCrouch = crouch;
            p2Controller.netSprint = sprint;

            if (jump)      p2Controller.netJump      = true;
            if (attack)    p2Controller.netAttack     = true;
            if (blockDown) p2Controller.netBlockDown  = true;
            if (blockUp)   p2Controller.netBlockUp    = true;
            if (dodge)     p2Controller.netDodge      = true;
            p2Controller.netBlock = block;
        }

        private void SmoothPlayer(PlayerController_Platform controller, Vector3 targetPos, Quaternion targetRot)
        {
            float diff = Vector3.Distance(controller.transform.position, targetPos);
            if (diff > 0.5f)
            {
                controller.transform.position = Vector3.Lerp(
                    controller.transform.position,
                    new Vector3(targetPos.x, targetPos.y, controller.transform.position.z),
                    Time.deltaTime * positionLerpSpeed
                );
            }
        }

        private static void SampleConfiguredPlayer(int playerNumber, out bool left, out bool right, out bool crouch, out bool sprint,
            out bool jump, out bool attack, out bool block, out bool blockDown, out bool blockUp, out bool dodge)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            var lobby = LobbyContext.Instance;
            int p1Device = lobby != null ? lobby.p1InputDevice : 0;
            int p2Device = lobby != null ? lobby.p2InputDevice : 0;
            bool dualKeyboard = LobbyContext.IsDualKeyboardMode(p1Device, p2Device);

            bool useController = playerNumber == 1 ? p1Device == 1 : p2Device == 1;
            if (useController)
            {
                int gamepadIndex = LobbyContext.GetGamepadListIndexForPlayer(playerNumber, p1Device, p2Device);
                left = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.MoveLeft, gamepadIndex);
                right = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.MoveRight, gamepadIndex);
                crouch = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.Crouch, gamepadIndex);
                sprint = gamepadIndex >= 0 && gamepadIndex < UnityEngine.InputSystem.Gamepad.all.Count && UnityEngine.InputSystem.Gamepad.all[gamepadIndex] != null
                    && UnityEngine.InputSystem.Gamepad.all[gamepadIndex].leftTrigger.isPressed;
                jump = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Jump, gamepadIndex);
                attack = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Attack, gamepadIndex);
                block = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.Block, gamepadIndex);
                blockDown = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Block, gamepadIndex);
                blockUp = bindings.WasActionReleasedThisFrame(ControlProfileId.Controller, ControlActionId.Block, gamepadIndex);
                dodge = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Dodge, gamepadIndex);
                return;
            }

            ControlProfileId profile = ControlBindingProfileResolver.ResolveGameplayKeyboardProfile(playerNumber, dualKeyboard);
            left = bindings.IsActionHeld(profile, ControlActionId.MoveLeft);
            right = bindings.IsActionHeld(profile, ControlActionId.MoveRight);
            crouch = bindings.IsActionHeld(profile, ControlActionId.Crouch);
            sprint = dualKeyboard && playerNumber == 2
                ? UnityEngine.Input.GetKey(KeyCode.RightControl)
                : UnityEngine.Input.GetKey(KeyCode.LeftShift);
            jump = bindings.WasActionPressedThisFrame(profile, ControlActionId.Jump);
            attack = bindings.WasActionPressedThisFrame(profile, ControlActionId.Attack);
            block = bindings.IsActionHeld(profile, ControlActionId.Block);
            blockDown = bindings.WasActionPressedThisFrame(profile, ControlActionId.Block);
            blockUp = bindings.WasActionReleasedThisFrame(profile, ControlActionId.Block);
            dodge = bindings.WasActionPressedThisFrame(profile, ControlActionId.Dodge);
        }
    }
}
