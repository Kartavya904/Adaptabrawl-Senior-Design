using UnityEngine;
using Unity.Netcode;

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
                p1Controller.keyLeft   = KeyCode.A;
                p1Controller.keyRight  = KeyCode.D;
                p1Controller.keyJump   = KeyCode.W;
                p1Controller.keyCrouch = KeyCode.S;
                p1Controller.keySprint = KeyCode.LeftShift;
                p1Controller.keyAttack = KeyCode.F;
                p1Controller.keyBlock  = KeyCode.G;
                p1Controller.keyDodge  = KeyCode.Space;

                p2Controller.isNetworkControlled = true;
            }
            else if (IsClient)
            {
                p1Controller.isNetworkControlled = true;

                p2Controller.isNetworkControlled = false;
                p2Controller.keyLeft   = KeyCode.A;
                p2Controller.keyRight  = KeyCode.D;
                p2Controller.keyJump   = KeyCode.W;
                p2Controller.keyCrouch = KeyCode.S;
                p2Controller.keySprint = KeyCode.LeftShift;
                p2Controller.keyAttack = KeyCode.F;
                p2Controller.keyBlock  = KeyCode.G;
                p2Controller.keyDodge  = KeyCode.Space;
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
            bool left      = UnityEngine.Input.GetKey(p1Controller.keyLeft);
            bool right     = UnityEngine.Input.GetKey(p1Controller.keyRight);
            bool crouch    = UnityEngine.Input.GetKey(p1Controller.keyCrouch);
            bool sprint    = UnityEngine.Input.GetKey(p1Controller.keySprint);
            bool jump      = UnityEngine.Input.GetKeyDown(p1Controller.keyJump);
            bool attack    = UnityEngine.Input.GetKeyDown(p1Controller.keyAttack);
            bool block     = UnityEngine.Input.GetKey(p1Controller.keyBlock);
            bool blockDown = UnityEngine.Input.GetKeyDown(p1Controller.keyBlock);
            bool blockUp   = UnityEngine.Input.GetKeyUp(p1Controller.keyBlock);
            bool dodge     = UnityEngine.Input.GetKeyDown(p1Controller.keyDodge);

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
            bool left      = UnityEngine.Input.GetKey(p2Controller.keyLeft);
            bool right     = UnityEngine.Input.GetKey(p2Controller.keyRight);
            bool crouch    = UnityEngine.Input.GetKey(p2Controller.keyCrouch);
            bool sprint    = UnityEngine.Input.GetKey(p2Controller.keySprint);
            bool jump      = UnityEngine.Input.GetKeyDown(p2Controller.keyJump);
            bool attack    = UnityEngine.Input.GetKeyDown(p2Controller.keyAttack);
            bool block     = UnityEngine.Input.GetKey(p2Controller.keyBlock);
            bool blockDown = UnityEngine.Input.GetKeyDown(p2Controller.keyBlock);
            bool blockUp   = UnityEngine.Input.GetKeyUp(p2Controller.keyBlock);
            bool dodge     = UnityEngine.Input.GetKeyDown(p2Controller.keyDodge);

            UpdateP2StateServerRpc(
                p2Controller.transform.position,
                p2Controller.transform.rotation,
                left, right, crouch, sprint, jump, attack, block, blockDown, blockUp, dodge
            );
        }

        [ServerRpc(RequireOwnership = false)]
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
    }
}
