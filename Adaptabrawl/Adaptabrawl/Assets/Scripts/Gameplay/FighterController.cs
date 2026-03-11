using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    [RequireComponent(typeof(CombatFSM))]
    [RequireComponent(typeof(StatusEffectSystem))]
    public class FighterController : MonoBehaviour
    {
        [Header("Fighter Data")]
        [SerializeField] private FighterDef fighterDef;

        [Header("Components")]
        private CombatFSM combatFSM;
        private StatusEffectSystem statusSystem;
        private MovementController movementController;

        [Header("Health")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxHealth;

        private bool _initialized;
        
        [Header("Facing")]
        [SerializeField] private bool facingRight = true;
        
        [Header("Events")]
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnDeath;
        public System.Action<bool> OnFacingChanged;
        
        public FighterDef FighterDef => fighterDef;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool FacingRight => facingRight;
        public bool IsDead => currentHealth <= 0f;
        
        private void Awake()
        {
            combatFSM = GetComponent<CombatFSM>();
            statusSystem = GetComponent<StatusEffectSystem>();
            movementController = GetComponent<MovementController>();
            
            if (movementController == null)
                movementController = gameObject.AddComponent<MovementController>();
        }
        
        private void Start()
        {
            // Only initialize here if SetFighterDef() hasn't already done it.
            if (!_initialized)
                InitializeFighter();
        }

        private void InitializeFighter()
        {
            if (fighterDef == null)
            {
                Debug.LogError("FighterController: FighterDef is not assigned!");
                return;
            }

            _initialized = true;
            maxHealth = fighterDef.maxHealth;
            currentHealth = maxHealth;
            Debug.Log($"[FighterController] '{fighterDef.fighterName}' initialized — maxHealth={maxHealth}, currentHealth={currentHealth}");

            // Initialize movement
            if (movementController != null)
            {
                movementController.Initialize(fighterDef);
            }
            
            // Initialize status system
            if (statusSystem != null)
            {
                statusSystem.Initialize(this);
            }
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            // Allow health to go negative (overkill) so a large hit on a low-health
            // fighter always triggers death — the Slider's minValue clamps the display.
            currentHealth -= damage;
            Debug.Log($"[FighterController] '{(fighterDef != null ? fighterDef.fighterName : gameObject.name)}' took {damage} damage — health now {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }
        
        public void Heal(float amount)
        {
            if (IsDead) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        private void Die()
        {
            // Force health to exactly 0 and push a final event so the slider clears immediately
            currentHealth = 0f;
            OnHealthChanged?.Invoke(0f, maxHealth);

            OnDeath?.Invoke();

            // Trigger the Shinabro death animation on the Stander child.
            // pcp.Die() is private, so we replicate its key steps here.
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null && !pcp.isDead)
            {
                pcp.isDead = true;

                var standerAnim = pcp.GetComponent<Animator>();
                if (standerAnim != null)
                {
                    int deathHash = Animator.StringToHash(pcp.deathAnimationState);
                    if (standerAnim.HasState(0, deathHash))
                        standerAnim.CrossFadeInFixedTime(pcp.deathAnimationState, 0.15f);
                    else
                    {
                        // No "Death" state found — freeze the animator in its current pose.
                        standerAnim.speed = 0f;
                        Debug.LogWarning($"[FighterController] Animator has no '{pcp.deathAnimationState}' state — freezing pose instead.");
                    }
                }

                // Disable bone colliders so dead characters don't block attacks
                foreach (var col in pcp.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                // Freeze the Rigidbody so the corpse doesn't slide
                var rb3d = pcp.GetComponent<Rigidbody>();
                if (rb3d != null) rb3d.isKinematic = true;
            }

            // Disable combat and movement
            if (combatFSM != null)
                combatFSM.enabled = false;
            if (movementController != null)
                movementController.enabled = false;
        }
        
        public void SetFacing(bool right)
        {
            if (facingRight != right)
            {
                facingRight = right;
                transform.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
                OnFacingChanged?.Invoke(right);
            }
        }
        
        public void SetFighterDef(FighterDef def)
        {
            fighterDef = def;
            _initialized = false;
            InitializeFighter();
        }

        /// <summary>
        /// Called by GameManager between rounds. Restores full health, re-enables
        /// CombatFSM and MovementController (disabled by Die()), and resets the
        /// Shinabro PlayerController_Platform so it can fight again.
        /// </summary>
        public void ResetForNewRound()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Re-enable systems disabled by Die()
            if (combatFSM != null) combatFSM.enabled = true;
            if (movementController != null) movementController.enabled = true;

            // Reset Shinabro's own health/death state on the Stander child
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                pcp.currentHealth = pcp.maxHealth;
                pcp.isDead        = false;

                // Restore animator speed — Die() may have frozen it to 0 as a fallback
                // when no "Death" animation state exists.
                var standerAnim = pcp.GetComponent<Animator>();
                if (standerAnim != null)
                    standerAnim.speed = 1f;
            }

            Debug.Log($"[FighterController] '{(fighterDef != null ? fighterDef.fighterName : gameObject.name)}' reset for new round.");
        }
    }
}

