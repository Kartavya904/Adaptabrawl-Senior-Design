using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Defend
{
    public class DefenseSystem : MonoBehaviour
    {
        [Header("References")]
        private FighterController fighterController;
        private CombatFSM combatFSM;
        private MovementController movementController;
        
        [Header("Block")]
        [SerializeField] private MoveDef blockMove;
        private bool isBlocking = false;
        private bool blockInputHeld = false;
        
        [Header("Parry")]
        [SerializeField] private MoveDef parryMove;
        [SerializeField] private float parryWindow = 0.2f; // Seconds
        private float parryTimer = 0f;
        private bool isParrying = false;
        
        [Header("Events")]
        public System.Action OnBlockStart;
        public System.Action OnBlockEnd;
        public System.Action OnParrySuccess;
        
        private void Start()
        {
            fighterController = GetComponent<FighterController>();
            combatFSM = GetComponent<CombatFSM>();
            movementController = GetComponent<MovementController>();
            
            // Create block move if not assigned
            if (blockMove == null)
            {
                blockMove = CreateBlockMove();
            }
            
            // Create parry move if not assigned
            if (parryMove == null)
            {
                parryMove = CreateParryMove();
            }
        }
        
        private void Update()
        {
            UpdateBlock();
            UpdateParry();
        }
        
        public void OnBlockInput(bool held)
        {
            blockInputHeld = held;
        }
        
        public void OnParryInput(bool pressed)
        {
            if (pressed && !isParrying && combatFSM != null && combatFSM.CanAct)
            {
                StartParry();
            }
        }
        
        private void UpdateBlock()
        {
            if (blockInputHeld && !isBlocking && combatFSM != null && combatFSM.CanAct)
            {
                StartBlock();
            }
            else if (!blockInputHeld && isBlocking)
            {
                EndBlock();
            }
        }
        
        private void StartBlock()
        {
            if (combatFSM == null || blockMove == null) return;
            
            isBlocking = true;
            combatFSM.TryStartMove(blockMove);
            OnBlockStart?.Invoke();
        }
        
        private void EndBlock()
        {
            isBlocking = false;
            OnBlockEnd?.Invoke();
            
            // End block state if in blocking state
            if (combatFSM != null && combatFSM.CurrentState == CombatState.Blocking)
            {
                // Will transition to idle when move ends
            }
        }
        
        private void StartParry()
        {
            if (combatFSM == null || parryMove == null) return;
            
            isParrying = true;
            parryTimer = parryWindow;
            combatFSM.TryStartMove(parryMove);
        }
        
        private void UpdateParry()
        {
            if (isParrying)
            {
                parryTimer -= Time.deltaTime;
                if (parryTimer <= 0f)
                {
                    isParrying = false;
                }
            }
        }
        
        public void OnParryHit()
        {
            // Called when parry successfully counters an attack
            OnParrySuccess?.Invoke();
            isParrying = false;
            
            // Apply counter attack bonus or stun to opponent
            // This would be handled by the combat system
        }
        
        private MoveDef CreateBlockMove()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Block";
            move.moveType = MoveType.Block;
            move.startupFrames = 0;
            move.activeFrames = 999; // Infinite while held
            move.recoveryFrames = 5;
            move.damage = 0f;
            move.canCancelIntoDodge = true;
            return move;
        }
        
        private MoveDef CreateParryMove()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Parry";
            move.moveType = MoveType.Parry;
            move.startupFrames = 2;
            move.activeFrames = Mathf.RoundToInt(parryWindow * 60f); // Convert to frames
            move.recoveryFrames = 10;
            move.damage = 0f;
            move.invincibilityFrames = move.activeFrames;
            return move;
        }
        
        public bool IsBlocking => isBlocking;
        public bool IsParrying => isParrying;
    }
}

