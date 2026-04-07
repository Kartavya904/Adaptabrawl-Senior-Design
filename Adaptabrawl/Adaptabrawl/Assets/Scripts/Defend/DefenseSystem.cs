using System.Collections;
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
        private Animator combatAnimator;
        
        [Header("Block")]
        [SerializeField] private MoveDef blockMove;
        [SerializeField] private float blockAttackRecoveryDelay = 0.05f;
        private bool isBlocking = false;
        private bool blockInputHeld = false;
        private Coroutine blockReleaseRoutine;
        
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
            ResolveCombatAnimator();
            
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

            if (held && blockReleaseRoutine != null)
            {
                StopCoroutine(blockReleaseRoutine);
                blockReleaseRoutine = null;
                combatFSM?.SetAttackSuppressed(false);
            }
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

            if (blockReleaseRoutine != null)
                StopCoroutine(blockReleaseRoutine);

            blockReleaseRoutine = StartCoroutine(FinishBlockRelease());
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

        private IEnumerator FinishBlockRelease()
        {
            if (combatFSM == null)
            {
                blockReleaseRoutine = null;
                yield break;
            }

            ResolveCombatAnimator();
            combatFSM.SetAttackSuppressed(true);
            combatFSM.EndCurrentMoveIf(blockMove);

            while (IsBlockAnimationPlaying())
                yield return null;

            if (blockAttackRecoveryDelay > 0f)
                yield return new WaitForSeconds(blockAttackRecoveryDelay);

            combatFSM.SetAttackSuppressed(false);
            combatFSM.TryConsumeBufferedMove();
            blockReleaseRoutine = null;
        }

        private void ResolveCombatAnimator()
        {
            if (combatAnimator != null && combatAnimator.runtimeAnimatorController != null)
                return;

            PlayerController_Platform pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                combatAnimator = pcp.GetComponent<Animator>();
                if (combatAnimator != null && combatAnimator.runtimeAnimatorController != null)
                    return;
            }

            combatAnimator = GetComponentInChildren<Animator>();
        }

        private bool IsBlockAnimationPlaying()
        {
            if (combatAnimator == null || !combatAnimator.isActiveAndEnabled)
                return false;

            AnimatorStateInfo stateInfo = combatAnimator.GetCurrentAnimatorStateInfo(0);
            if (combatAnimator.IsInTransition(0))
                return stateInfo.IsName("Block") || stateInfo.IsTag("Block");

            return stateInfo.IsName("Block") || stateInfo.IsTag("Block");
        }
    }
}
