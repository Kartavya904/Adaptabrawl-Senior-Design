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

        // Controllers
        private PlayerController_Platform p1Controller;
        private PlayerController_Platform p2Controller;

        [Header("Network Smoothing")]
        public float positionLerpSpeed = 15f;
        public float rotationLerpSpeed = 15f;

        private Vector3 targetP1Position;
        private Quaternion targetP1Rotation;
        private Vector3 targetP2Position;
        private Quaternion targetP2Rotation;

        private bool rolesApplied = false;

        // How often to log the RPC heartbeat (in seconds)
        private float debugLogInterval = 2f;
        private float debugLogTimer = 0f;

        void Start()
        {
            Debug.Log($"[ONLINE-INPUT] Start() called. IsServer={IsServer} IsClient={IsClient} IsSpawned={IsSpawned}");
            Debug.Log($"[ONLINE-INPUT] player1Object={(player1Object != null ? player1Object.name : "NULL")}  player2Object={(player2Object != null ? player2Object.name : "NULL")}");

            // Destroy offline conflicting input handlers
            PlayerInputHandler[] localInputs = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
            foreach (var input in localInputs)
            {
                Debug.Log($"[ONLINE-INPUT] Destroying offline PlayerInputHandler on {input.gameObject.name}");
                Destroy(input);
            }

            TwoPlayerInputHandler[] dualInputs = FindObjectsByType<TwoPlayerInputHandler>(FindObjectsSortMode.None);
            foreach (var input in dualInputs)
            {
                Debug.Log($"[ONLINE-INPUT] Destroying offline TwoPlayerInputHandler on {input.gameObject.name}");
                Destroy(input);
            }

            InitializePlayers();

            if (IsSpawned && !rolesApplied)
            {
                Debug.Log("[ONLINE-INPUT] Already spawned at Start(), applying roles now.");
                ApplyNetworkRoles();
            }
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
                    Debug.Log($"[ONLINE-INPUT] P1 controller found on '{player1Object.name}'. Current keyLeft={p1Controller.keyLeft}  isNetworkControlled={p1Controller.isNetworkControlled}");
                }
                else
                    Debug.LogError($"[ONLINE-INPUT] player1Object '{player1Object.name}' is missing PlayerController_Platform!");
            }
            else
                Debug.LogError("[ONLINE-INPUT] player1Object is NOT ASSIGNED in the Inspector!");

            if (player2Object != null)
            {
                p2Controller = player2Object.GetComponentInChildren<PlayerController_Platform>();
                if (p2Controller != null)
                {
                    targetP2Position = p2Controller.transform.position;
                    targetP2Rotation = p2Controller.transform.rotation;
                    Debug.Log($"[ONLINE-INPUT] P2 controller found on '{player2Object.name}'. Current keyLeft={p2Controller.keyLeft}  isNetworkControlled={p2Controller.isNetworkControlled}");
                }
                else
                    Debug.LogError($"[ONLINE-INPUT] player2Object '{player2Object.name}' is missing PlayerController_Platform!");
            }
            else
                Debug.LogError("[ONLINE-INPUT] player2Object is NOT ASSIGNED in the Inspector!");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[ONLINE-INPUT] OnNetworkSpawn() — IsServer={IsServer}  IsClient={IsClient}  IsHost={IsHost}");
            Debug.Log($"[ONLINE-INPUT]   p1Controller={(p1Controller != null ? p1Controller.name : "NULL")}  p2Controller={(p2Controller != null ? p2Controller.name : "NULL")}");

            if (p1Controller == null || p2Controller == null)
            {
                Debug.LogWarning("[ONLINE-INPUT] Controllers null at OnNetworkSpawn, re-running InitializePlayers()...");
                InitializePlayers();
            }

            ApplyNetworkRoles();
        }

        private void ApplyNetworkRoles()
        {
            Debug.Log($"[ONLINE-INPUT] ApplyNetworkRoles() — IsServer={IsServer}  IsClient={IsClient}  IsHost={IsHost}");

            if (p1Controller == null || p2Controller == null)
            {
                Debug.LogError("[ONLINE-INPUT] ApplyNetworkRoles() FAILED — one or both controllers are null. Check Inspector assignments!");
                return;
            }

            if (IsServer) // true for host too
            {
                // Host runs P1 (DualBlades) locally with WASD
                p1Controller.isNetworkControlled = false;
                p1Controller.keyLeft   = KeyCode.A;
                p1Controller.keyRight  = KeyCode.D;
                p1Controller.keyJump   = KeyCode.W;
                p1Controller.keyCrouch = KeyCode.S;
                p1Controller.keySprint = KeyCode.LeftShift;
                p1Controller.keyAttack = KeyCode.F;
                p1Controller.keyBlock  = KeyCode.G;
                p1Controller.keyDodge  = KeyCode.Space;

                // Host receives P2 (Hammer) inputs via ServerRpc from client
                p2Controller.isNetworkControlled = true;

                Debug.Log("[ONLINE-INPUT] ✅ HOST roles set: P1(DualBlades)=LOCAL(WASD)  P2(Hammer)=NETWORK");
            }
            else if (IsClient)
            {
                // Client receives P1 (DualBlades) inputs via ClientRpc from host
                p1Controller.isNetworkControlled = true;

                // Client runs P2 (Hammer) locally with WASD
                p2Controller.isNetworkControlled = false;
                p2Controller.keyLeft   = KeyCode.A;
                p2Controller.keyRight  = KeyCode.D;
                p2Controller.keyJump   = KeyCode.W;
                p2Controller.keyCrouch = KeyCode.S;
                p2Controller.keySprint = KeyCode.LeftShift;
                p2Controller.keyAttack = KeyCode.F;
                p2Controller.keyBlock  = KeyCode.G;
                p2Controller.keyDodge  = KeyCode.Space;

                Debug.Log("[ONLINE-INPUT] ✅ CLIENT roles set: P1(DualBlades)=NETWORK  P2(Hammer)=LOCAL(WASD)");
                Debug.Log($"[ONLINE-INPUT]   P1 isNetworkControlled={p1Controller.isNetworkControlled}  P2 isNetworkControlled={p2Controller.isNetworkControlled}");
            }

            rolesApplied = true;
        }

        void Update()
        {
            if (!IsSpawned || p1Controller == null || p2Controller == null) return;

            debugLogTimer += Time.deltaTime;
            bool doLog = debugLogTimer >= debugLogInterval;
            if (doLog) debugLogTimer = 0f;

            if (IsServer)
            {
                if (doLog) Debug.Log($"[ONLINE-INPUT][HOST] Sending P1 inputs. P1 pos={p1Controller.transform.position}  P1 netControlled={p1Controller.isNetworkControlled}");
                SendP1InputsToClients();
                SmoothPlayer(p2Controller, targetP2Position, targetP2Rotation);
            }
            else if (IsClient)
            {
                if (doLog) Debug.Log($"[ONLINE-INPUT][CLIENT] Sending P2 inputs. P2 pos={p2Controller.transform.position}  P2 netControlled={p2Controller.isNetworkControlled}  P1 netControlled={p1Controller.isNetworkControlled}");
                SendP2InputsToServer();
                SmoothPlayer(p1Controller, targetP1Position, targetP1Rotation);
            }
        }

        // ─── Host → Clients ──────────────────────────────────────────────────────

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

            if (left || right)
                Debug.Log($"[ONLINE-INPUT][HOST] P1 moving: left={left} right={right} — sending to Clients via RPC");

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

            if (left || right)
                Debug.Log($"[ONLINE-INPUT][CLIENT] Received P1 movement RPC: left={left} right={right}  p1Controller={(p1Controller != null ? p1Controller.name : "NULL")}");

            targetP1Position = position;
            targetP1Rotation = rotation;

            if (p1Controller == null)
            {
                Debug.LogError("[ONLINE-INPUT][CLIENT] p1Controller is NULL when receiving RPC! Roles may not have been applied.");
                return;
            }

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

        // ─── Client → Host ───────────────────────────────────────────────────────

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

            if (left || right)
                Debug.Log($"[ONLINE-INPUT][CLIENT] P2 moving: left={left} right={right} — sending to Host via RPC");

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
            if (left || right)
                Debug.Log($"[ONLINE-INPUT][HOST] Received P2 movement ServerRpc: left={left} right={right}  p2Controller={(p2Controller != null ? p2Controller.name : "NULL")}");

            targetP2Position = position;
            targetP2Rotation = rotation;

            if (p2Controller == null)
            {
                Debug.LogError("[ONLINE-INPUT][HOST] p2Controller is NULL when receiving ServerRpc!");
                return;
            }

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

        // ─── Position smoothing ──────────────────────────────────────────────────

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
