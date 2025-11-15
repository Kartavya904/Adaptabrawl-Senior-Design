using UnityEngine;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Combat;
using Adaptabrawl.Attack;
using Adaptabrawl.Defend;
using Adaptabrawl.Evade;

// Note: This requires Mirror Networking package
// Install via: Window > Package Manager > Add package from git URL: https://github.com/vis2k/Mirror.git
namespace Adaptabrawl.Networking
{
    // [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkFighter : MonoBehaviour // : NetworkBehaviour
    {
        [Header("References")]
        private FighterController fighterController;
        private MovementController movementController;
        private CombatFSM combatFSM;
        private AttackSystem attackSystem;
        private DefenseSystem defenseSystem;
        private EvadeSystem evadeSystem;
        
        [Header("Network Sync")]
        private Vector2 networkPosition;
        private float networkRotation;
        private CombatState networkCombatState;
        
        private void Awake()
        {
            fighterController = GetComponent<FighterController>();
            movementController = GetComponent<MovementController>();
            combatFSM = GetComponent<CombatFSM>();
            attackSystem = GetComponent<AttackSystem>();
            defenseSystem = GetComponent<DefenseSystem>();
            evadeSystem = GetComponent<EvadeSystem>();
        }
        
        // Mirror networking methods (commented out until Mirror is installed)
        /*
        public override void OnStartAuthority()
        {
            // Enable input for local player
            var inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.enabled = true;
            }
        }
        
        [Command]
        private void CmdMove(Vector2 moveInput)
        {
            if (movementController != null)
            {
                movementController.SetMoveInput(moveInput);
            }
        }
        
        [Command]
        private void CmdJump()
        {
            if (movementController != null)
            {
                movementController.Jump();
            }
        }
        
        [Command]
        private void CmdLightAttack()
        {
            if (attackSystem != null)
            {
                attackSystem.OnLightAttackInput(true);
            }
        }
        
        [Command]
        private void CmdHeavyAttack()
        {
            if (attackSystem != null)
            {
                attackSystem.OnHeavyAttackInput(true);
            }
        }
        
        [Command]
        private void CmdBlock(bool held)
        {
            if (defenseSystem != null)
            {
                defenseSystem.OnBlockInput(held);
            }
        }
        
        [Command]
        private void CmdParry()
        {
            if (defenseSystem != null)
            {
                defenseSystem.OnParryInput(true);
            }
        }
        
        [Command]
        private void CmdDodge()
        {
            if (evadeSystem != null)
            {
                evadeSystem.OnDodgeInput(true);
            }
        }
        
        [ClientRpc]
        private void RpcSyncPosition(Vector2 position)
        {
            if (!hasAuthority)
            {
                // Interpolate to network position
                transform.position = Vector2.Lerp(transform.position, position, Time.deltaTime * 10f);
            }
        }
        
        [ClientRpc]
        private void RpcSyncCombatState(CombatState state)
        {
            if (!hasAuthority && combatFSM != null)
            {
                // Sync combat state
            }
        }
        
        [ClientRpc]
        private void RpcTakeDamage(float damage)
        {
            if (fighterController != null)
            {
                fighterController.TakeDamage(damage);
            }
        }
        */
        
        // Placeholder methods for when Mirror is installed
        public void SendMoveInput(Vector2 input)
        {
            // CmdMove(input);
        }
        
        public void SendJump()
        {
            // CmdJump();
        }
        
        public void SendLightAttack()
        {
            // CmdLightAttack();
        }
        
        public void SendHeavyAttack()
        {
            // CmdHeavyAttack();
        }
        
        public void SendBlock(bool held)
        {
            // CmdBlock(held);
        }
        
        public void SendParry()
        {
            // CmdParry();
        }
        
        public void SendDodge()
        {
            // CmdDodge();
        }
    }
}

