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

            currentHealth = Mathf.Max(0f, currentHealth - damage);
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
            OnDeath?.Invoke();
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
    }
}

