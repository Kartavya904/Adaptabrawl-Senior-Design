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
        
        [Header("Dodge")]
        [SerializeField] private MoveDef dodgeMove;
        [SerializeField] private float dodgeCooldown = 1f;
        private float dodgeCooldownTimer = 0f;
        private bool isDodging = false;
        
        [Header("Events")]
        public System.Action<Vector2> OnDodgeStart;
        public System.Action OnDodgeEnd;
        
        private void Start()
        {
            fighterController = GetComponent<FighterController>();
            combatFSM = GetComponent<CombatFSM>();
            movementController = GetComponent<MovementController>();
            
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
        
        public void TryDodge(Vector2 direction = default)
        {
            if (combatFSM == null || !combatFSM.CanAct) return;
            if (dodgeCooldownTimer > 0f) return;
            
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
            
            // Start dodge
            isDodging = true;
            dodgeCooldownTimer = dodgeCooldown;
            
            if (combatFSM != null && dodgeMove != null)
            {
                combatFSM.TryStartMove(dodgeMove);
            }
            
            // Apply dodge movement
            if (movementController != null)
            {
                movementController.Dash(direction);
            }
            
            OnDodgeStart?.Invoke(direction);
            
            // End dodge after move completes
            StartCoroutine(EndDodgeAfterMove());
        }
        
        private System.Collections.IEnumerator EndDodgeAfterMove()
        {
            // Wait for dodge move to complete
            yield return new WaitForSeconds(dodgeMove != null ? dodgeMove.totalFrames / 60f : 0.5f);
            
            isDodging = false;
            OnDodgeEnd?.Invoke();
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
        public bool CanDodge => dodgeCooldownTimer <= 0f && !isDodging;
    }
}

