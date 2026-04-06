using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Evade
{
    public class EvadeSystem : MonoBehaviour
    {
        [Header("References")]
        private FighterController fighterController;
        private CombatFSM combatFSM;
        private MovementController movementController;
        private Animator combatAnimator;
        
        [Header("Dodge")]
        [SerializeField] private MoveDef dodgeMove;
        [SerializeField] private float dodgeCooldown = 1f;
        private float dodgeCooldownTimer = 0f;
        private bool isDodging = false;
        private Coroutine endDodgeRoutine;
        
        [Header("Events")]
        public System.Action<Vector2> OnDodgeStart;
        public System.Action OnDodgeEnd;
        
        private void Start()
        {
            fighterController = GetComponent<FighterController>();
            combatFSM = GetComponent<CombatFSM>();
            movementController = GetComponent<MovementController>();
            ResolveCombatAnimator();
            
            // Create dodge move if not assigned
            if (dodgeMove == null)
            {
                dodgeMove = CreateDodgeMove();
            }
        }
        
        private void Update()
        {
            if (dodgeCooldownTimer > 0f)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }
        }
        
        public void OnDodgeInput(bool pressed)
        {
            if (pressed && !isDodging && dodgeCooldownTimer <= 0f)
            {
                TryDodge();
            }
        }
        
        public bool TryDodge(Vector2 direction = default)
        {
            ResolveCombatAnimator();

            if (combatFSM == null || !combatFSM.CanAct) return false;
            if (dodgeCooldownTimer > 0f) return false;
            if (isDodging || IsDodgeAnimationPlaying()) return false;
            
            // Use movement input direction if no direction specified
            if (direction.magnitude < 0.1f && movementController != null)
            {
                direction = movementController.MoveInput;
                if (direction.magnitude < 0.1f)
                {
                    // Default to backward dodge
                    direction = fighterController != null && fighterController.FacingRight 
                        ? Vector2.left 
                        : Vector2.right;
                }
            }
            
            // Normalize direction
            direction = direction.normalized;

            if (dodgeMove == null || !combatFSM.TryStartMove(dodgeMove))
                return false;

            // Start dodge
            isDodging = true;
            dodgeCooldownTimer = dodgeCooldown;
            
            // Apply dodge movement
            if (movementController != null)
            {
                movementController.Dash(direction);
            }
            
            OnDodgeStart?.Invoke(direction);
            
            // End dodge after the live dodge animation completes.
            if (endDodgeRoutine != null)
                StopCoroutine(endDodgeRoutine);
            endDodgeRoutine = StartCoroutine(EndDodgeAfterMove());
            return true;
        }
        
        private System.Collections.IEnumerator EndDodgeAfterMove()
        {
            float fallbackDuration = dodgeMove != null ? dodgeMove.totalFrames / 60f : 0.5f;
            float elapsed = 0f;

            while (elapsed < fallbackDuration)
            {
                elapsed += Time.deltaTime;
                if (!IsDodgeAnimationPlaying())
                    break;
                yield return null;
            }

            isDodging = false;
            endDodgeRoutine = null;
            OnDodgeEnd?.Invoke();
        }

        private void ResolveCombatAnimator()
        {
            if (combatAnimator != null && combatAnimator.runtimeAnimatorController != null)
                return;

            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                combatAnimator = pcp.GetComponent<Animator>();
                if (combatAnimator != null && combatAnimator.runtimeAnimatorController != null)
                    return;
            }

            combatAnimator = GetComponentInChildren<Animator>();
        }

        private bool IsDodgeAnimationPlaying()
        {
            if (combatAnimator == null || !combatAnimator.isActiveAndEnabled)
                return false;

            AnimatorStateInfo stateInfo = combatAnimator.GetCurrentAnimatorStateInfo(0);
            if (combatAnimator.IsInTransition(0))
                return stateInfo.IsName("Dodge") || stateInfo.IsName("DodgeRoll") || stateInfo.IsTag("Dodge");

            return stateInfo.IsName("Dodge")
                || stateInfo.IsName("DodgeRoll")
                || stateInfo.IsTag("Dodge");
        }
        
        private MoveDef CreateDodgeMove()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Dodge";
            move.moveType = MoveType.Dodge;
            move.startupFrames = 2;
            move.activeFrames = 8; // Invincibility frames
            move.recoveryFrames = 5;
            move.damage = 0f;
            move.invincibilityFrames = move.activeFrames;
            move.canCancelIntoDodge = false;
            return move;
        }
        
        public bool IsDodging => isDodging;
        public bool CanDodge => dodgeCooldownTimer <= 0f && !isDodging && !IsDodgeAnimationPlaying();
    }
}
